using Colossal.Serialization.Entities;
using DeathcareTune.Core;
using Game;
using Game.Common;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace DeathcareTune.Systems
{
    /// <summary>
    /// 殡葬调优系统。
    ///
    /// 数据位置（已用反编译核实）：<c>DeathcareFacilityData</c> 挂在 prefab 实体上，
    /// 模拟层的 <c>DeathcareFacilityAISystem.DeathcareFacilityTickJob</c> 每次 tick 用
    /// 建筑的 <c>PrefabRef</c> 去 <c>ComponentLookup&lt;DeathcareFacilityData&gt;</c> 里取值。
    /// 所以改 prefab 即刻对全部已有建筑生效，不需要逐栋建筑改，也不会写进建筑实例。
    ///
    /// 本系统只做一件事：把原版基线值乘以倍率写回 prefab，并在卸载/关闭时还原。
    /// 不改任何 job 的控制流，不依赖 Harmony。
    /// </summary>
    public partial class DeathcareTuneSystem : GameSystemBase
    {
        private struct Owned
        {
            public DeathcareFacilityData Baseline;
            public DeathcareFacilityData Applied;
            public string Name;
        }

        private static readonly List<DeathcareTuneSystem> s_Instances = new List<DeathcareTuneSystem>();

        private readonly Dictionary<Entity, Owned> m_Owned = new Dictionary<Entity, Owned>();
        private readonly List<Entity> m_Dead = new List<Entity>();

        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_Query;

        private bool m_Active = true;
        private bool m_GameLoaded;
        private bool m_Pending = true;
        private bool m_UnsafeFailure;
        private string m_Status = "NoCity";
        private int m_PrefabCount;
        private int m_ConflictCount;

        public static string StatusKey
        {
            get
            {
                foreach (var s in s_Instances) if (s.m_GameLoaded && s.m_Active) return s.m_Status;
                return "NoCity";
            }
        }

        public static bool HasLoadedCity
        {
            get
            {
                foreach (var s in s_Instances) if (s.m_GameLoaded && s.m_Active) return true;
                return false;
            }
        }

        /// <summary>状态行附带的计数，例如「4 个设施；冲突 0」。</summary>
        public static string DetailKey
        {
            get
            {
                foreach (var s in s_Instances)
                    if (s.m_GameLoaded && s.m_Active)
                        return s.m_Status == "NoCity" ? "" : s.m_PrefabCount + "|" + s.m_ConflictCount;
                return "";
            }
        }

        public static void RequestReapply()
        {
            foreach (var s in s_Instances)
            {
                s.m_Pending = true;
                if (s.m_Status != "Failed") s.m_Status = s.m_GameLoaded ? "Pending" : "NoCity";
            }
        }

        public static void ResumeAll()
        {
            foreach (var s in s_Instances) { s.m_Active = true; s.m_Pending = true; }
        }

        /// <summary>同步还原，必须在 ECS World 仍然可用时调用。</summary>
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
            m_Query = GetEntityQuery(
                ComponentType.ReadOnly<PrefabData>(),
                ComponentType.ReadWrite<DeathcareFacilityData>(),
                ComponentType.Exclude<Deleted>());
            s_Instances.Add(this);
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_GameLoaded = mode == GameMode.Game;
            m_Pending = true;
            m_Status = m_UnsafeFailure ? "Failed" : m_GameLoaded ? "Pending" : "NoCity";
        }

        protected override void OnUpdate()
        {
            if (!m_Active || !m_GameLoaded || !m_Pending) return;
            m_Pending = false;
            try
            {
                Apply();
            }
            catch (Exception e)
            {
                m_UnsafeFailure = true;
                m_Status = "Failed";
                Mod.s_Log.Error(e, "[殡葬] 应用失败，状态不可信，请勿在此状态下保存");
            }
        }

        protected override void OnDestroy()
        {
            TryRestore();
            s_Instances.Remove(this);
            base.OnDestroy();
        }

        private void Apply()
        {
            var setting = Mod.s_Setting;
            bool enable = setting != null && setting.EnableTune;
            int rateMul = enable ? Clamp(setting.ProcessingMultiplier, 1, 20) : 1;
            int fleetMul = enable ? Clamp(setting.HearseFleetMultiplier, 1, 10) : 1;
            int storeMul = enable ? Clamp(setting.StorageMultiplier, 1, 10) : 1;

            if (m_Query.IsEmptyIgnoreFilter)
            {
                // prefab 尚未建立（例如仍在主菜单装配阶段），下帧重试。
                m_Pending = true;
                m_Status = "Pending";
                return;
            }

            PruneMissing();
            EntityManager.CompleteAllTrackedJobs();

            int applied = 0, conflicts = 0, changed = 0, capped = 0;
            using (var entities = m_Query.ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    var current = EntityManager.GetComponentData<DeathcareFacilityData>(entity);
                    Owned owned;
                    if (!m_Owned.TryGetValue(entity, out owned))
                    {
                        owned = new Owned { Baseline = current, Applied = current, Name = SafeName(entity) };
                        m_Owned[entity] = owned;
                        Mod.s_Log.Info($"[基线] {owned.Name}: rate={Fmt(owned.Baseline.m_ProcessingRate)} " +
                            $"hearses={owned.Baseline.m_HearseCapacity} storage={owned.Baseline.m_StorageCapacity} " +
                            $"longTerm={owned.Baseline.m_LongTermStorage}");
                    }
                    else if (!owned.Applied.Equals(current))
                    {
                        // 有人在我们之后改过同一份数据。不覆盖，只报告。
                        conflicts++;
                        Mod.s_Log.Warn($"[冲突] {owned.Name}: 现值(x={Fmt(current.m_ProcessingRate)}, " +
                            $"h={current.m_HearseCapacity}, s={current.m_StorageCapacity}) 与本次写入不符，跳过");
                        continue;
                    }

                    var target = Scale(owned.Baseline, rateMul, fleetMul, storeMul);
                    if (ScaleMath.WasRateCapped(owned.Baseline.m_ProcessingRate, rateMul) ||
                        ScaleMath.WasCountCapped(owned.Baseline.m_HearseCapacity, fleetMul) ||
                        ScaleMath.WasCountCapped(owned.Baseline.m_StorageCapacity, storeMul))
                    {
                        capped++;
                        Mod.s_Log.Warn($"[上限] {owned.Name}: 倍率结果被安全上限截断");
                    }

                    if (!target.Equals(current))
                    {
                        EntityManager.SetComponentData(entity, target);
                        changed++;
                    }

                    owned.Applied = target;
                    m_Owned[entity] = owned;
                    applied++;
                }
            }

            m_PrefabCount = m_Owned.Count;
            m_ConflictCount = conflicts;
            m_Status = conflicts > 0 ? "Conflict" : "Applied";
            Mod.s_Log.Info($"[殡葬] 设施={applied} 变更={changed} 冲突={conflicts} 截断={capped} " +
                $"倍率: rate=x{rateMul} fleet=x{fleetMul} storage=x{storeMul} enable={enable}");
        }

        private static DeathcareFacilityData Scale(DeathcareFacilityData baseline, int rateMul, int fleetMul, int storeMul)
        {
            var target = baseline;
            target.m_ProcessingRate = ScaleMath.ScaleRate(baseline.m_ProcessingRate, rateMul);
            target.m_HearseCapacity = ScaleMath.ScaleCount(baseline.m_HearseCapacity, fleetMul);
            target.m_StorageCapacity = ScaleMath.ScaleCount(baseline.m_StorageCapacity, storeMul);
            return target;
        }

        private void PruneMissing()
        {
            m_Dead.Clear();
            foreach (var pair in m_Owned)
                if (!EntityManager.Exists(pair.Key))
                    m_Dead.Add(pair.Key);
            foreach (var entity in m_Dead) m_Owned.Remove(entity);
            if (m_Dead.Count > 0) Mod.s_Log.Info($"[清理] {m_Dead.Count} 个 prefab 已不存在，移出受管列表");
        }

        private bool TryRestore()
        {
            if (!World.IsCreated) return true;
            bool complete = true;
            try
            {
                EntityManager.CompleteAllTrackedJobs();
                foreach (var pair in m_Owned)
                {
                    var entity = pair.Key;
                    var owned = pair.Value;
                    if (!EntityManager.Exists(entity)) continue;
                    var current = EntityManager.GetComponentData<DeathcareFacilityData>(entity);
                    if (!current.Equals(owned.Applied))
                    {
                        complete = false;
                        Mod.s_Log.Warn($"[还原跳过] {owned.Name}: 数据已被其他来源修改，不覆盖");
                        continue;
                    }
                    if (!current.Equals(owned.Baseline))
                    {
                        EntityManager.SetComponentData(entity, owned.Baseline);
                        Mod.s_Log.Info($"[还原] {owned.Name}: 恢复 rate={Fmt(owned.Baseline.m_ProcessingRate)} " +
                            $"hearses={owned.Baseline.m_HearseCapacity} storage={owned.Baseline.m_StorageCapacity}");
                    }
                }
                m_Owned.Clear();
                m_PrefabCount = 0;
                m_ConflictCount = 0;
                m_Status = m_GameLoaded ? "Restored" : "NoCity";
            }
            catch (Exception e)
            {
                complete = false;
                m_Status = "Failed";
                Mod.s_Log.Error(e, "[还原失败] 不应把本次退出视为完成卸载清理");
            }
            return complete;
        }

        private string SafeName(Entity entity)
        {
            try
            {
                string name = m_PrefabSystem.GetPrefabName(entity);
                return string.IsNullOrEmpty(name) ? "prefab#" + entity.Index : name;
            }
            catch
            {
                return "prefab#" + entity.Index;
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        private static string Fmt(float value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
