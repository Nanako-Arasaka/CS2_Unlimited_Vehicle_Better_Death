using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.SceneFlow;
using System.Collections.Generic;

namespace VehicleCapUnlock
{
    [FileLocation(nameof(VehicleCapUnlock))]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";

        private bool m_EnableUnlock;
        private int m_VehicleLimitMultiplier = 1;
        private bool m_ScaleDepotCapacity;
        private int m_DepotMultiplier = 1;

        public Setting(IMod mod) : base(mod)
        {
        }

        [SettingsUISection(kSection)]
        public bool EnableUnlock
        {
            get => m_EnableUnlock;
            set
            {
                if (m_EnableUnlock == value) return;
                m_EnableUnlock = value;
                Systems.LineCeilingSystem.RequestReapply();
                Systems.DepotCapacitySystem.RequestReapply();
            }
        }

        [SettingsUISlider(min = 1, max = 10, step = 1, scalarMultiplier = 1, unit = "x")]
        [SettingsUISection(kSection)]
        public int VehicleLimitMultiplier
        {
            get => m_VehicleLimitMultiplier;
            set
            {
                value = System.Math.Max(1, System.Math.Min(10, value));
                if (m_VehicleLimitMultiplier == value) return;
                m_VehicleLimitMultiplier = value;
                Systems.LineCeilingSystem.RequestReapply();
            }
        }

        [SettingsUISection(kSection)]
        public bool ScaleDepotCapacity
        {
            get => m_ScaleDepotCapacity;
            set
            {
                if (m_ScaleDepotCapacity == value) return;
                m_ScaleDepotCapacity = value;
                Systems.DepotCapacitySystem.RequestReapply();
            }
        }

        [SettingsUISlider(min = 1, max = 10, step = 1, scalarMultiplier = 1, unit = "x")]
        [SettingsUISection(kSection)]
        public int DepotMultiplier
        {
            get => m_DepotMultiplier;
            set
            {
                value = System.Math.Max(1, System.Math.Min(10, value));
                if (m_DepotMultiplier == value) return;
                m_DepotMultiplier = value;
                Systems.DepotCapacitySystem.RequestReapply();
            }
        }

        [SettingsUIButton]
        [SettingsUIConfirmation("VehicleCapUnlock.PREPARE_UNLOAD_CONFIRM",
            "Disable both features, restore owned ranges/capacities and clamp this city's saved line adjustments to their initial bounds? Save a copy only after both statuses report prepared.")]
        [SettingsUISection(kSection)]
        public bool PrepareUnload
        {
            set
            {
                if (!Systems.LineCeilingSystem.HasLoadedCity)
                {
                    Mod.s_Log.Warn("[准备卸载] 请先加载测试城市");
                    return;
                }
                EnableUnlock = false;
                VehicleLimitMultiplier = 1;
                ScaleDepotCapacity = false;
                DepotMultiplier = 1;
                Systems.LineCeilingSystem.RequestPrepareUnload();
                Systems.DepotCapacitySystem.RequestPrepareUnload();
                ApplyAndSave();
            }
        }

        [SettingsUIMultilineText("")]
        [SettingsUISection(kSection)]
        public string SafetyStatus => Translate("Lines") + ": " + Translate(Systems.LineCeilingSystem.StatusKey) +
            "\n" + Translate("Depots") + ": " + Translate(Systems.DepotCapacitySystem.StatusKey);

        private static string Translate(string key)
        {
            var manager = GameManager.instance?.localizationManager;
            return manager?.activeDictionary != null && manager.activeDictionary.TryGetValue("VehicleCapUnlock.STATUS." + key, out string text)
                ? text : key;
        }

        public override void SetDefaults()
        {
            EnableUnlock = false;
            VehicleLimitMultiplier = 1;
            ScaleDepotCapacity = false;
            DepotMultiplier = 1;
        }
    }

    internal static class LocaleKeys
    {
        public const string ModName = "VehicleCapUnlock.NAME";
        public const string Section = "VehicleCapUnlock.SECTION";
        public const string EnableUnlock = "VehicleCapUnlock.OPTION.EnableUnlock";
        public const string EnableUnlockDesc = "VehicleCapUnlock.OPTION.EnableUnlock.DESC";
        public const string Multiplier = "VehicleCapUnlock.OPTION.Multiplier";
        public const string MultiplierDesc = "VehicleCapUnlock.OPTION.Multiplier.DESC";
        public const string ScaleDepot = "VehicleCapUnlock.OPTION.ScaleDepot";
        public const string ScaleDepotDesc = "VehicleCapUnlock.OPTION.ScaleDepot.DESC";
        public const string DepotMultiplier = "VehicleCapUnlock.OPTION.DepotMultiplier";
        public const string DepotMultiplierDesc = "VehicleCapUnlock.OPTION.DepotMultiplier.DESC";
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Vehicle Cap Unlock" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableUnlock)), "Unlock vehicle count ceiling" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableUnlock)),
                  "Raises the per-line 'Assigned Vehicles' ceiling. Each line is still set individually, exactly like vanilla." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VehicleLimitMultiplier)), "Line vehicle ceiling" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VehicleLimitMultiplier)),
                  "How far the per-line slider can be pushed, relative to the vanilla maximum. 1x = vanilla untouched." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ScaleDepotCapacity)), "Scale depot capacity" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ScaleDepotCapacity)),
                  "Optional and off by default. Only proven passenger-only facilities are enlarged; cargo, shared and unknown facilities are skipped." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DepotMultiplier)), "Depot capacity" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DepotMultiplier)),
                  "Scales proven passenger-only depot families. Cargo, shared/unknown facilities and repeatable capacity upgrades are skipped." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PrepareUnload)), "Prepare current city for removal" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PrepareUnload)), "Disables both features, restores owned data and clamps saved per-line adjustments. Resume simulation, wait for both statuses to report prepared, then save a COPY before removing the mod. Conflicts or failures block completion." },
                { "VehicleCapUnlock.PREPARE_UNLOAD_CONFIRM", "Disable both features, restore owned data and clamp saved per-line adjustments? This reduces above-limit settings in the current city. Save a copy only after both statuses report prepared." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SafetyStatus)), "Current status" },
                { "VehicleCapUnlock.STATUS.Lines", "Lines" },
                { "VehicleCapUnlock.STATUS.Depots", "Depots" },
                { "VehicleCapUnlock.STATUS.NoCity", "No city loaded" },
                { "VehicleCapUnlock.STATUS.Pending", "Pending: resume simulation" },
                { "VehicleCapUnlock.STATUS.Applied", "Settings applied (removal not prepared)" },
                { "VehicleCapUnlock.STATUS.Prepared", "Prepared: save a copy before removal" },
                { "VehicleCapUnlock.STATUS.Conflict", "Conflict: completion blocked; see mod log" },
                { "VehicleCapUnlock.STATUS.Skipped", "Nothing eligible: no depot was resized; see mod log" },
                { "VehicleCapUnlock.STATUS.Failed", "Failed: do not save; see mod log" },
            };
        }

        public void Unload() { }
    }

    public class LocaleZH : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleZH(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "出车数量上限解锁" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "主要" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableUnlock)), "解锁出车数量上限" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableUnlock)),
                  "抬高每条线路「配车数量」滑块的上限。调节方式与原版完全一致，仍然是逐条线路单独设置。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VehicleLimitMultiplier)), "线路出车上限倍率" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VehicleLimitMultiplier)),
                  "滑块能拉到原版上限的多少倍。1 倍 = 完全不改动原版。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ScaleDepotCapacity)), "同步放大车库容量" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ScaleDepotCapacity)),
                  "可选功能，默认关闭。仅扩容能确认属于客运的设施；共用车库、货运设施或用途不明的资产会跳过。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DepotMultiplier)), "车库容量倍率" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DepotMultiplier)),
                  "相对于首次基线扩容。客货混用、用途未知或可重复容量升级的设施会跳过；零值、负值和溢出容量不会被强制改成其他值。" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PrepareUnload)), "准备卸载（当前城市）" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PrepareUnload)), "关闭两项功能，还原本 mod 仍持有的数据，并把当前城市超限的逐线路设置收回初始范围。恢复模拟，等待两项状态均显示已准备，再另存副本后移除 mod。有冲突或失败时不能视为完成。" },
                { "VehicleCapUnlock.PREPARE_UNLOAD_CONFIRM", "关闭两项功能、还原受管数据并收回当前城市超限的逐线路设置？这会减少当前城市的超限配车设置。等待两项状态均为已准备后，再另存副本。" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SafetyStatus)), "当前状态" },
                { "VehicleCapUnlock.STATUS.Lines", "线路" },
                { "VehicleCapUnlock.STATUS.Depots", "车库" },
                { "VehicleCapUnlock.STATUS.NoCity", "尚未加载城市" },
                { "VehicleCapUnlock.STATUS.Pending", "等待处理，请恢复模拟" },
                { "VehicleCapUnlock.STATUS.Applied", "已按设置处理（未准备卸载）" },
                { "VehicleCapUnlock.STATUS.Prepared", "已准备，请另存副本后卸载" },
                { "VehicleCapUnlock.STATUS.Conflict", "存在冲突，未完成，请查看 mod 日志" },
                { "VehicleCapUnlock.STATUS.Skipped", "没有合格车库，未做任何改动，请查看 mod 日志" },
                { "VehicleCapUnlock.STATUS.Failed", "处理失败，请勿保存，查看 mod 日志" },
            };
        }

        public void Unload() { }
    }
}
