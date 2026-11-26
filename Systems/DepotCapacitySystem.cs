using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using VehicleCapUnlock.Core;

namespace VehicleCapUnlock.Systems
{
    public partial class DepotCapacitySystem : GameSystemBase
    {
        private sealed class Snapshot
        {
            public PrefabBase Prefab;
            public OwnedValue<TransportDepotData> Value;
        }

        private static readonly HashSet<DepotCapacitySystem> s_Instances = new HashSet<DepotCapacitySystem>();
        private readonly Dictionary<Entity, Snapshot> m_Originals = new Dictionary<Entity, Snapshot>();
        private readonly PendingWork m_Work = new PendingWork();
        private bool m_Active = true;
        private bool m_GameLoaded;
        private bool m_PrepareUnload;
        private bool m_UnsafeFailure;
        private string m_Status = "NoCity";
        private EntityQuery m_DepotQuery;
        private EntityQuery m_PrefabQuery;
        private PrefabSystem m_PrefabSystem;

        public static string StatusKey
        {
            get { foreach (var s in s_Instances) if (s.m_GameLoaded && s.m_Active) return s.m_Status; return "NoCity"; }
        }

        public static void RequestReapply()
        {
            foreach (var s in s_Instances)
            { s.m_PrepareUnload = false; s.m_Work.Request(); s.m_Status = s.m_UnsafeFailure ? "Failed" : "Pending"; }
        }

        public static void RequestPrepareUnload()
        {
            foreach (var s in s_Instances) if (s.m_GameLoaded && s.m_Active)
            { s.m_PrepareUnload = true; s.m_Work.Request(); s.m_Status = s.m_UnsafeFailure ? "Failed" : "Pending"; }
        }

        public static void ResumeAll()
        {
            foreach (var s in s_Instances) { s.m_Active = true; s.m_Work.Request(); }
        }

        public static bool RestoreAllNow()
        {
            bool complete = true;
            foreach (var s in s_Instances) { s.m_Active = false; complete &= s.TryRestore(); }
            return complete;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_DepotQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(),
                ComponentType.ReadWrite<TransportDepotData>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            m_PrefabQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            s_Instances.Add(this);
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_GameLoaded = mode == GameMode.Game;
            m_PrepareUnload = false;
            m_Work.Request();
            m_Status = m_UnsafeFailure ? "Failed" : m_GameLoaded ? "Pending" : "NoCity";
        }

        protected override void OnUpdate()
        {
            if (!m_Active || !m_GameLoaded || !m_Work.Pending || m_UnsafeFailure) return;
            try { if (Apply()) m_Work.Complete(); }
            catch (Exception e)
            {
                bool rollbackComplete = !(e is WriteBatchException batch) || batch.RollbackSucceeded;
                m_UnsafeFailure |= !rollbackComplete;
                m_Work.Fail(rollbackComplete);
                m_Status = m_Work.Failed ? "Failed" : "Pending";
                Mod.s_Log.Error(e, $"[车库失败] attempt={m_Work.Attempts}; retry={m_Work.Pending}; rollback={rollbackComplete}");
            }
        }

        protected override void OnDestroy()
        {
            TryRestore();
            m_Originals.Clear();
            s_Instances.Remove(this);
            base.OnDestroy();
        }

        private bool Apply()
        {
            EntityManager.CompleteAllTrackedJobs();
            var setting = Mod.s_Setting;
            int multiplier = !m_PrepareUnload && setting != null && setting.EnableUnlock && setting.ScaleDepotCapacity
                ? Math.Max(1, Math.Min(10, setting.DepotMultiplier)) : 1;
            if (multiplier == 1)
            {
                bool restored = RestoreOwned();
                m_Status = restored ? (m_PrepareUnload ? "Prepared" : "Applied") : "Conflict";
                return true;
            }
            if (m_DepotQuery.IsEmptyIgnoreFilter) return false;
            var purposes = ClassifyDepots();
            var writes = new List<IReversibleWrite>();
            var commits = new List<Action>();
            var capacities = new List<int>();
            var samples = new List<string>(6);
            int skipped = 0, conflicts = 0, eligibleCount = 0, candidates = 0;
            int unresolved = 0, wrongType = 0, wrongRole = 0, nonPositive = 0;
            using (var entities = m_DepotQuery.ToEntityArray(Allocator.Temp))
            {
                candidates = entities.Length;
                foreach (Entity entity in entities)
                {
                    var current = EntityManager.GetComponentData<TransportDepotData>(entity);
                    if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefab) || prefab == null)
                    {
                        capacities.Add(current.m_VehicleCapacity);
                        skipped++;
                        unresolved++;
                        continue;
                    }
                    bool known = m_Originals.TryGetValue(entity, out var snapshot) && ReferenceEquals(snapshot.Prefab, prefab);
                    if (known && !snapshot.Value.Accepts(current))
                    {
                        capacities.Add(current.m_VehicleCapacity);
                        Mod.s_Log.Warn($"[车库冲突] entity={entity}; prefab={prefab.name}; 未覆盖后续修改");
                        conflicts++;
                        continue;
                    }
                    purposes.TryGetValue(entity, out var purpose);
                    bool typeOk = IsSupportedType(current.m_TransportType);
                    bool roleOk = DepotSafety.IsPassengerEligible(purpose);
                    bool scaleOk = DepotSafety.TryScale(current.m_VehicleCapacity, multiplier, out _);
                    bool eligible = typeOk && roleOk && scaleOk;
                    if (!known && !eligible)
                    {
                        capacities.Add(current.m_VehicleCapacity);
                        skipped++;
                        if (!typeOk) wrongType++;
                        else if (!roleOk) wrongRole++;
                        else nonPositive++;
                        if (samples.Count < 6)
                            samples.Add($"{prefab.name}/{current.m_TransportType}/{(int)purpose}/cap={current.m_VehicleCapacity}");
                        continue;
                    }
                    if (eligible) eligibleCount++;
                    if (!known)
                    {
                        snapshot = new Snapshot { Prefab = prefab, Value = new OwnedValue<TransportDepotData>(current) };
                        m_Originals[entity] = snapshot;
                    }
                    var target = current;
                    if (eligible && DepotSafety.TryScale(snapshot.Value.Original.m_VehicleCapacity, multiplier, out int capacity))
                        target.m_VehicleCapacity = capacity;
                    else
                    {
                        // A now-ambiguous family is not enlarged; restore only our own last write.
                        target.m_VehicleCapacity = snapshot.Value.Original.m_VehicleCapacity;
                        skipped++;
                        if (samples.Count < 6)
                            samples.Add($"{prefab.name}/{current.m_TransportType}/{(int)purpose}/cap={current.m_VehicleCapacity}");
                    }
                    capacities.Add(target.m_VehicleCapacity);
                    AddWrite(writes, commits, entity, snapshot, current, target);
                }
            }
            if (!DepotSafety.FitsCombinedBudget(capacities))
            {
                m_Status = "Conflict";
                Mod.s_Log.Warn("[车库容量拒绝] 发现负容量或 prefab 总容量超出 int 合并预算；本次未写入任何车库");
                return true;
            }
            SafeWriteBatch.Execute(writes);
            foreach (var commit in commits) commit();
            m_Status = conflicts > 0 ? "Conflict" : (eligibleCount > 0 ? "Applied" : "Skipped");
            Mod.s_Log.Info($"[车库完成] multiplier={multiplier}; candidates={candidates}; eligible={eligibleCount}; " +
                $"changes={writes.Count}; skipped={skipped}; conflicts={conflicts}");
            if (skipped > 0)
                Mod.s_Log.Info($"[车库筛选] 无法解析={unresolved}; 类型不支持={wrongType}; 含货运或无客运={wrongRole}; " +
                    $"容量非正={nonPositive}; 样例(名称/类型/purpose/cap)={string.Join(" | ", samples)}");
            return true;
        }

        private bool TryRestore()
        {
            if (!World.IsCreated || m_Originals.Count == 0) return true;
            try { EntityManager.CompleteAllTrackedJobs(); return RestoreOwned(); }
            catch (Exception e) { m_Status = "Failed"; Mod.s_Log.Error(e, "[车库还原失败] 不应继续保存或卸载"); return false; }
        }

        private bool RestoreOwned()
        {
            var writes = new List<IReversibleWrite>();
            var commits = new List<Action>();
            int conflicts = 0;
            foreach (var pair in m_Originals)
            {
                Entity entity = pair.Key;
                Snapshot snapshot = pair.Value;
                if (!EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity) ||
                    !EntityManager.HasComponent<TransportDepotData>(entity)) continue;
                if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefab) || !ReferenceEquals(prefab, snapshot.Prefab))
                {
                    if (snapshot.Value.HasChanges)
                    {
                        conflicts++;
                        Mod.s_Log.Warn($"[车库还原冲突] entity={entity}; prefab 身份改变，未报告还原成功");
                    }
                    continue;
                }
                var current = EntityManager.GetComponentData<TransportDepotData>(entity);
                if (!snapshot.Value.Accepts(current))
                {
                    conflicts++;
                    Mod.s_Log.Warn($"[车库还原冲突] entity={entity}; prefab={prefab.name}; 保留其他系统的数据");
                    continue;
                }
                AddWrite(writes, commits, entity, snapshot, current, snapshot.Value.Original);
            }
            SafeWriteBatch.Execute(writes);
            foreach (var commit in commits) commit();
            Mod.s_Log.Info($"[车库还原] changes={writes.Count}; conflicts={conflicts}; complete={conflicts == 0}");
            if (conflicts > 0) m_Status = "Conflict";
            return conflicts == 0;
        }

        private void AddWrite(List<IReversibleWrite> writes, List<Action> commits, Entity entity,
            Snapshot snapshot, TransportDepotData before, TransportDepotData after)
        {
            if (!before.Equals(after))
                writes.Add(new ValueWrite<TransportDepotData>(() => ReadDepot(entity, snapshot.Prefab),
                    value => EntityManager.SetComponentData(entity, value), before, after));
            commits.Add(() => snapshot.Value.Commit(after));
        }

        private TransportDepotData ReadDepot(Entity entity, PrefabBase prefab)
        {
            if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(entity, out var found) || !ReferenceEquals(found, prefab))
                throw new InvalidOperationException("Depot prefab identity changed");
            return EntityManager.GetComponentData<TransportDepotData>(entity);
        }

        private Dictionary<Entity, DepotPurpose> ClassifyDepots()
        {
            var graph = new DepotPurposeGraph<Entity>();
            var passengerTypes = new HashSet<TransportType>();
            var cargoTypes = new HashSet<TransportType>();
            using (var entities = m_PrefabQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity entity in entities)
                {
                    if (!EntityManager.HasComponent<TransportLineData>(entity)) continue;
                    var line = EntityManager.GetComponentData<TransportLineData>(entity);
                    if (line.m_PassengerTransport) passengerTypes.Add(line.m_TransportType);
                    if (line.m_CargoTransport) cargoTypes.Add(line.m_TransportType);
                }
                foreach (Entity entity in entities)
                {
                    bool depot = EntityManager.HasComponent<TransportDepotData>(entity);
                    DepotPurpose purpose = DepotPurpose.None;
                    bool transportNode = depot;
                    if (EntityManager.HasComponent<CargoTransportStationData>(entity))
                    { purpose |= DepotPurpose.Cargo; transportNode = true; }
                    if (EntityManager.HasComponent<PublicTransportStationData>(entity))
                    { purpose |= DepotPurpose.Passenger; transportNode = true; }
                    if (EntityManager.HasBuffer<SubObject>(entity))
                    {
                        var children = EntityManager.GetBuffer<SubObject>(entity, true);
                        foreach (var child in children) if (child.m_Prefab != Entity.Null) graph.Connect(entity, child.m_Prefab);
                    }
                    if (EntityManager.HasComponent<ServiceUpgradeData>(entity))
                    {
                        var upgrade = EntityManager.GetComponentData<ServiceUpgradeData>(entity);
                        if (!upgrade.m_ForbidMultiple && depot && EntityManager.GetComponentData<TransportDepotData>(entity).m_VehicleCapacity != 0)
                            purpose |= DepotPurpose.RepeatableUpgrade;
                        bool hasParent = false;
                        if (EntityManager.HasBuffer<ServiceUpgradeBuilding>(entity))
                        {
                            foreach (var parent in EntityManager.GetBuffer<ServiceUpgradeBuilding>(entity, true))
                            {
                                Entity building = parent.m_Building;
                                if (!EntityManager.Exists(building) || !EntityManager.HasComponent<PrefabData>(building) ||
                                    EntityManager.HasComponent<Deleted>(building) || EntityManager.HasComponent<Temp>(building))
                                { purpose |= DepotPurpose.Unknown; continue; }
                                graph.Connect(building, entity);
                                hasParent = true;
                            }
                        }
                        if (!hasParent) purpose |= DepotPurpose.Unknown;
                    }
                    if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefab) || prefab == null)
                    { graph.Add(entity, purpose | DepotPurpose.Unknown, transportNode); continue; }
                    // TryGet also recognizes derived components; Has<T> checks exact types only.
                    if (prefab.TryGet<CargoTransportStation>(out _)) { purpose |= DepotPurpose.Cargo; transportNode = true; }
                    if (prefab.TryGet<TransportStation>(out _)) { purpose |= DepotPurpose.Passenger; transportNode = true; }
                    if (depot)
                    {
                        var data = EntityManager.GetComponentData<TransportDepotData>(entity);
                        // Type alone is sufficient only when available line data has no cargo use.
                        if (passengerTypes.Contains(data.m_TransportType) && !cargoTypes.Contains(data.m_TransportType))
                            purpose |= DepotPurpose.Passenger;
                    }
                    if (prefab.TryGet<ServiceUpgrade>(out _) && !EntityManager.HasComponent<ServiceUpgradeData>(entity))
                        purpose |= DepotPurpose.Unknown;
                    graph.Add(entity, purpose, transportNode);
                }
            }
            return graph.Resolve();
        }

        private static bool IsSupportedType(TransportType type) => type == TransportType.Bus || type == TransportType.Tram ||
            type == TransportType.Subway || type == TransportType.Train || type == TransportType.Ship ||
            type == TransportType.Ferry || type == TransportType.Airplane;
    }
}
