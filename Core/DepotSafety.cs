using System;
using System.Collections.Generic;

namespace VehicleCapUnlock.Core
{
    [Flags]
    internal enum DepotPurpose { None = 0, Passenger = 1, Cargo = 2, Unknown = 4, RepeatableUpgrade = 8 }

    internal static class DepotSafety
    {
        // A facility qualifies when at least one passenger role reaches it and no cargo role does.
        // Unproven provenance (Unknown) and repeatable capacity upgrades deliberately no longer
        // veto the write: neither says anything about whether a subway/bus depot is passenger-only,
        // and requiring an exact Passenger flag excluded every vanilla depot from scaling.
        public static bool IsPassengerEligible(DepotPurpose purpose) =>
            (purpose & DepotPurpose.Passenger) != 0 && (purpose & DepotPurpose.Cargo) == 0;

        public static bool TryScale(int original, int multiplier, out int target)
        {
            target = original;
            if (original <= 0 || multiplier < 1 || multiplier > 10) return false;
            long value = (long)original * multiplier;
            if (value > int.MaxValue) return false;
            target = (int)value;
            return true;
        }

        // Conservative budget for the entire prefab set, including unchanged capacities.
        // Repeatable capacity upgrades are excluded by purpose classification.
        public static bool FitsCombinedBudget(IEnumerable<int> capacities)
        {
            long total = 0;
            foreach (int capacity in capacities)
            {
                if (capacity < 0) return false;
                total += capacity;
                if (total > int.MaxValue) return false;
            }
            return true;
        }
    }

    internal sealed class DepotPurposeGraph<T>
    {
        private readonly Dictionary<T, DepotPurpose> m_Purposes = new Dictionary<T, DepotPurpose>();
        private readonly Dictionary<T, List<T>> m_Parents = new Dictionary<T, List<T>>();
        private readonly List<KeyValuePair<T, T>> m_Edges = new List<KeyValuePair<T, T>>();
        private readonly HashSet<T> m_TransportNodes = new HashSet<T>();

        public void Add(T node, DepotPurpose purpose, bool transportNode)
        {
            m_Purposes[node] = purpose;
            if (transportNode) m_TransportNodes.Add(node);
        }

        public void Connect(T parent, T child)
        {
            m_Edges.Add(new KeyValuePair<T, T>(parent, child));
            if (!m_Parents.TryGetValue(child, out var parents)) m_Parents[child] = parents = new List<T>();
            parents.Add(parent);
        }

        public Dictionary<T, DepotPurpose> Resolve()
        {
            // Decorations shared by unrelated terminals must not connect their purposes.
            var relevant = new HashSet<T>(m_TransportNodes);
            var queue = new Queue<T>(relevant);
            while (queue.Count > 0)
            {
                T node = queue.Dequeue();
                if (!m_Parents.TryGetValue(node, out var parents)) continue;
                foreach (T parent in parents) if (relevant.Add(parent)) queue.Enqueue(parent);
            }
            var neighbours = new Dictionary<T, List<T>>();
            foreach (var edge in m_Edges)
            {
                if (!relevant.Contains(edge.Key) || !relevant.Contains(edge.Value)) continue;
                Link(neighbours, edge.Key, edge.Value);
                Link(neighbours, edge.Value, edge.Key);
            }
            var result = new Dictionary<T, DepotPurpose>(m_Purposes);
            queue = new Queue<T>(relevant);
            while (queue.Count > 0)
            {
                T node = queue.Dequeue();
                if (!neighbours.TryGetValue(node, out var linked)) continue;
                result.TryGetValue(node, out var source);
                foreach (T other in linked)
                {
                    result.TryGetValue(other, out var before);
                    DepotPurpose after = before | source;
                    if (before == after) continue;
                    result[other] = after;
                    queue.Enqueue(other);
                }
            }
            return result;
        }

        private static void Link(Dictionary<T, List<T>> neighbours, T from, T to)
        {
            if (!neighbours.TryGetValue(from, out var list)) neighbours[from] = list = new List<T>();
            list.Add(to);
        }
    }
}
