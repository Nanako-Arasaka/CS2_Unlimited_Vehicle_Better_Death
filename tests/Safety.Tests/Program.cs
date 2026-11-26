using System;
using System.Collections.Generic;
using VehicleCapUnlock.Core;

internal static class Program
{
    private static int s_Checks;
    private static int Main()
    {
        try
        {
            CheckBatches(); CheckOwnershipAndRetries(); CheckCapacities(); CheckPurposeGraphs();
            Console.WriteLine($"PASS: {s_Checks} safety assertions; fault injection, rollback ownership, bounded retries, capacity budgets and passenger/cargo families.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }

    private static void CheckBatches()
    {
        for (int count = 1; count <= 12; count++)
        for (int failure = -1; failure < count; failure++)
        for (int afterWrite = 0; afterWrite <= 1; afterWrite++)
        {
            var values = new int[count];
            var writes = new List<IReversibleWrite>();
            for (int i = 0; i < count; i++)
            {
                int slot = i;
                values[slot] = slot + 1;
                writes.Add(new ValueWrite<int>(() => values[slot], v =>
                {
                    bool fail = slot == failure && v == 1000 + slot;
                    if (fail && afterWrite == 0) throw new InvalidOperationException("Injected before write");
                    values[slot] = v;
                    if (fail) throw new InvalidOperationException("Injected after write");
                }, slot + 1, 1000 + slot));
            }
            if (failure == -1) SafeWriteBatch.Execute(writes);
            else Assert(ExpectBatchFailure(writes).RollbackSucceeded, "Rollback must include a throwing setter");
            for (int i = 0; i < count; i++)
                Assert(values[i] == (failure == -1 ? 1000 + i : i + 1), "Batch left partial changes");
        }
        SafeWriteBatch.Execute(new List<IReversibleWrite>());

        int first = 1, second = 99, calls = 0;
        var conflict = new List<IReversibleWrite>
        {
            new ValueWrite<int>(() => first, v => { first = v; calls++; }, 1, 5),
            new ValueWrite<int>(() => second, v => { second = v; calls++; }, 2, 10)
        };
        try { SafeWriteBatch.Execute(conflict); throw new Exception("Preflight conflict accepted"); }
        catch (InvalidOperationException) { }
        Assert(first == 1 && second == 99 && calls == 0, "Preflight must finish before any writes");

        second = 2;
        var externalChange = new List<IReversibleWrite>
        {
            new ValueWrite<int>(() => first, v => first = v, 1, 5),
            new ValueWrite<int>(() => second, v => { first = 777; throw new InvalidOperationException("External change"); }, 2, 10)
        };
        Assert(!ExpectBatchFailure(externalChange).RollbackSucceeded, "External value must block rollback success");
        Assert(first == 777 && second == 2, "Rollback overwrote another system's value");

        first = 1;
        var rollbackFailure = new List<IReversibleWrite>
        {
            new ValueWrite<int>(() => first, v => first = v, 1, 5),
            new ValueWrite<int>(() => second, v =>
            {
                if (v == 2) throw new InvalidOperationException("Rollback rejected");
                second = v; throw new InvalidOperationException("Failed after write");
            }, 2, 10)
        };
        Assert(!ExpectBatchFailure(rollbackFailure).RollbackSucceeded, "Rollback exception must be reported");
        Assert(first == 1 && second == 10, "Rollback must continue after one rollback fails");
    }

    private static void CheckOwnershipAndRetries()
    {
        var value = new OwnedValue<int>(20);
        Assert(value.Accepts(20) && !value.Accepts(60), "Initial ownership incorrect");
        value.Commit(100);
        Assert(value.Accepts(100) && value.Accepts(20), "Last write or exact baseline reset rejected");
        Assert(!value.Accepts(60), "A later mode/third-party value must not be overwritten");
        value.Commit(value.Original);
        Assert(!value.HasChanges && !value.Accepts(100), "Restored writes must not remain owned");

        var work = new PendingWork();
        Assert(work.Pending, "Initial request missing");
        work.Fail(true); Assert(work.Pending && work.Attempts == 1, "First retry missing");
        work.Fail(true); Assert(work.Pending && work.Attempts == 2, "Second retry missing");
        work.Fail(true); Assert(!work.Pending && work.Failed, "Retries must stop after three failures");
        work.Request(); Assert(work.Pending && !work.Failed && work.Attempts == 0, "Request must reset ordinary failures");
        work.Fail(false); Assert(!work.Pending && work.Failed, "Incomplete rollback must stop immediately");
        work.Complete(); Assert(!work.Pending && !work.Failed && work.Attempts == 0, "Completion state incorrect");
    }

    private static void CheckCapacities()
    {
        Assert(!DepotSafety.TryScale(0, 5, out int zero) && zero == 0, "Zero became nonzero");
        Assert(!DepotSafety.TryScale(-1, 5, out int negative) && negative == -1, "Sentinel value changed");
        Assert(!DepotSafety.TryScale(20, 0, out _) && !DepotSafety.TryScale(20, 11, out _), "Invalid multiplier accepted");
        Assert(DepotSafety.TryScale(20, 5, out int normal) && normal == 100, "Normal scaling failed");
        Assert(DepotSafety.TryScale(int.MaxValue, 1, out int max) && max == int.MaxValue, "Exact boundary rejected");
        Assert(!DepotSafety.TryScale(int.MaxValue, 2, out int same) && same == int.MaxValue, "Overflow must be rejected, not clamped");
        Assert(DepotSafety.FitsCombinedBudget(new[] { int.MaxValue - 20, 20, 0 }), "Valid combined boundary rejected");
        Assert(!DepotSafety.FitsCombinedBudget(new[] { int.MaxValue - 20, 21 }), "Combined overflow accepted");
        Assert(!DepotSafety.FitsCombinedBudget(new[] { 10, -1 }), "Negative capacity accepted");
        var random = new Random(172026);
        for (int i = 0; i < 1000; i++)
        {
            int original = random.Next(1, int.MaxValue), multiplier = random.Next(1, 11);
            long expected = (long)original * multiplier;
            bool accepted = DepotSafety.TryScale(original, multiplier, out int target);
            Assert(accepted == (expected <= int.MaxValue), "Overflow decision incorrect");
            Assert(target == (accepted ? (int)expected : original), "Rejected capacity must remain untouched");
        }
    }

    private static void CheckPurposeGraphs()
    {
        var graph = new DepotPurposeGraph<string>();
        graph.Add("passenger", DepotPurpose.Passenger, true);
        graph.Add("cargo", DepotPurpose.Cargo, true);
        graph.Add("lamp", DepotPurpose.None, false);
        graph.Add("passengerDepot", DepotPurpose.None, true);
        graph.Add("cargoDepot", DepotPurpose.None, true);
        graph.Connect("passenger", "lamp"); graph.Connect("cargo", "lamp");
        graph.Connect("passenger", "passengerDepot"); graph.Connect("cargo", "cargoDepot");
        var result = graph.Resolve();
        Assert(DepotSafety.IsPassengerEligible(result["passengerDepot"]), "Shared decoration tainted passenger terminal");
        Assert(!DepotSafety.IsPassengerEligible(result["cargoDepot"]), "Cargo depot accepted");
        graph.Add("middle", DepotPurpose.None, false); graph.Add("nested", DepotPurpose.None, true);
        graph.Connect("passenger", "middle"); graph.Connect("middle", "nested");
        Assert(DepotSafety.IsPassengerEligible(graph.Resolve()["nested"]), "Nested passenger depot not resolved");
        graph.Connect("cargo", "nested");
        Assert(!DepotSafety.IsPassengerEligible(graph.Resolve()["passengerDepot"]), "Mixed terminal family accepted");

        var upgrades = new DepotPurposeGraph<int>();
        upgrades.Add(1, DepotPurpose.Passenger, true); upgrades.Add(2, DepotPurpose.None, true);
        upgrades.Connect(1, 2); upgrades.Connect(2, 1);
        Assert(DepotSafety.IsPassengerEligible(upgrades.Resolve()[2]), "Cyclic upgrade graph did not converge");
        upgrades.Add(2, DepotPurpose.RepeatableUpgrade, true);
        Assert(DepotSafety.IsPassengerEligible(upgrades.Resolve()[1]), "Repeatable capacity must not veto a passenger depot");
        Assert(!DepotSafety.IsPassengerEligible(DepotPurpose.None), "Unknown standalone depot accepted");
        Assert(DepotSafety.IsPassengerEligible(DepotPurpose.Passenger | DepotPurpose.Unknown), "Unproven provenance must not veto a passenger depot");
        Assert(!DepotSafety.IsPassengerEligible(DepotPurpose.Passenger | DepotPurpose.Cargo), "Shared cargo/passenger terminal accepted");
        Assert(!DepotSafety.IsPassengerEligible(DepotPurpose.Cargo), "Cargo-only terminal accepted");
    }

    private static WriteBatchException ExpectBatchFailure(List<IReversibleWrite> writes)
    {
        try { SafeWriteBatch.Execute(writes); }
        catch (WriteBatchException error) { return error; }
        throw new Exception("Injected failure was not observed");
    }
    private static void Assert(bool condition, string message)
    { s_Checks++; if (!condition) throw new InvalidOperationException(message); }
}
