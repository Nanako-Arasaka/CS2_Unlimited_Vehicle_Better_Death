using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using System.Collections.Generic;

namespace DeathcareTune
{
    [FileLocation(nameof(DeathcareTune))]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";

        private bool m_EnableTune = true;
        private int m_ProcessingMultiplier = 4;
        private int m_HearseFleetMultiplier = 3;
        private int m_StorageMultiplier = 3;

        public Setting(IMod mod) : base(mod)
        {
        }

        [SettingsUISection(kSection)]
        public bool EnableTune
        {
            get => m_EnableTune;
            set
            {
                if (m_EnableTune == value) return;
                m_EnableTune = value;
                Systems.DeathcareTuneSystem.RequestReapply();
            }
        }

        [SettingsUISlider(min = 1, max = 20, step = 1, scalarMultiplier = 1, unit = "x")]
        [SettingsUISection(kSection)]
        public int ProcessingMultiplier
        {
            get => m_ProcessingMultiplier;
            set
            {
                value = System.Math.Max(1, System.Math.Min(20, value));
                if (m_ProcessingMultiplier == value) return;
                m_ProcessingMultiplier = value;
                Systems.DeathcareTuneSystem.RequestReapply();
            }
        }

        [SettingsUISlider(min = 1, max = 10, step = 1, scalarMultiplier = 1, unit = "x")]
        [SettingsUISection(kSection)]
        public int HearseFleetMultiplier
        {
            get => m_HearseFleetMultiplier;
            set
            {
                value = System.Math.Max(1, System.Math.Min(10, value));
                if (m_HearseFleetMultiplier == value) return;
                m_HearseFleetMultiplier = value;
                Systems.DeathcareTuneSystem.RequestReapply();
            }
        }

        [SettingsUISlider(min = 1, max = 10, step = 1, scalarMultiplier = 1, unit = "x")]
        [SettingsUISection(kSection)]
        public int StorageMultiplier
        {
            get => m_StorageMultiplier;
            set
            {
                value = System.Math.Max(1, System.Math.Min(10, value));
                if (m_StorageMultiplier == value) return;
                m_StorageMultiplier = value;
                Systems.DeathcareTuneSystem.RequestReapply();
            }
        }

        [SettingsUIButton]
        [SettingsUIConfirmation("DeathcareTune.PREPARE_UNLOAD_CONFIRM",
            "Turn tuning off, restore every scale factor to 1x and write the original prefab values back? Save a copy only after the status reports restored.")]
        [SettingsUISection(kSection)]
        public bool PrepareUnload
        {
            set
            {
                if (!Systems.DeathcareTuneSystem.HasLoadedCity)
                {
                    Mod.s_Log.Warn("[准备卸载] 请先加载测试城市");
                    return;
                }
                EnableTune = false;
                ProcessingMultiplier = 1;
                HearseFleetMultiplier = 1;
                StorageMultiplier = 1;
                Systems.DeathcareTuneSystem.RequestReapply();
                ApplyAndSave();
            }
        }

        [SettingsUIMultilineText("")]
        [SettingsUISection(kSection)]
        public string TuneStatus
        {
            get
            {
                string status = Translate(Systems.DeathcareTuneSystem.StatusKey);
                string detail = Systems.DeathcareTuneSystem.DetailKey;
                if (string.IsNullOrEmpty(detail)) return status;
                string[] parts = detail.Split('|');
                return status + "\n" + Translate("Facilities") + ": " + parts[0] + "   " +
                    Translate("Conflicts") + ": " + parts[1];
            }
        }

        private static string Translate(string key)
        {
            var manager = GameManager.instance?.localizationManager;
            return manager?.activeDictionary != null &&
                   manager.activeDictionary.TryGetValue("DeathcareTune.STATUS." + key, out string text)
                ? text : key;
        }

        public override void SetDefaults()
        {
            EnableTune = true;
            ProcessingMultiplier = 4;
            HearseFleetMultiplier = 3;
            StorageMultiplier = 3;
        }
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
                { m_Setting.GetSettingsLocaleID(), "Deathcare Tuning" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableTune)), "Enable deathcare tuning" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableTune)),
                  "Off restores the original prefab values. Only prefab data is written: existing crematoriums and cemeteries pick the new values up immediately, no rebuild needed." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ProcessingMultiplier)), "Cremation speed" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ProcessingMultiplier)),
                  "Multiplies how fast a facility turns stored bodies into processed ones. Cemeteries have a base rate of 0 and stay at 0." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.HearseFleetMultiplier)), "Hearses per facility" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.HearseFleetMultiplier)),
                  "Multiplies the number of hearses a facility may field at once. Vanilla hearses carry exactly one body per trip, so this is the only lever that raises pickup throughput." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StorageMultiplier)), "Body storage" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StorageMultiplier)),
                  "Multiplies how many bodies a facility can hold while waiting. Facilities with a base storage of 0 are left untouched." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PrepareUnload)), "Prepare city for mod removal" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PrepareUnload)),
                  "Turns tuning off, resets all factors to 1x and writes the original prefab values back. Wait until the status reports restored, then save a copy before removing the mod." },
                { "DeathcareTune.PREPARE_UNLOAD_CONFIRM",
                  "Turn tuning off, reset all factors to 1x and restore the original prefab values? Save a copy only after the status reports restored." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TuneStatus)), "Current status" },
                { "DeathcareTune.STATUS.NoCity", "No city loaded" },
                { "DeathcareTune.STATUS.Pending", "Pending: resume simulation" },
                { "DeathcareTune.STATUS.Applied", "Applied" },
                { "DeathcareTune.STATUS.Restored", "Restored to vanilla values" },
                { "DeathcareTune.STATUS.Conflict", "Conflict: some prefabs skipped, see mod log" },
                { "DeathcareTune.STATUS.Failed", "Failed: do not save, see mod log" },
                { "DeathcareTune.STATUS.Facilities", "Facilities" },
                { "DeathcareTune.STATUS.Conflicts", "Conflicts" },
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
                { m_Setting.GetSettingsLocaleID(), "殡葬调优" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "主要" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableTune)), "启用殡葬调优" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableTune)),
                  "关闭即还原为原版数值。只写 prefab 数据：已有的火葬场和墓地会立刻采用新数值，不用拆了重建。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ProcessingMultiplier)), "火化速度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ProcessingMultiplier)),
                  "放大设施把待处理尸体转成已处理的速率。墓地的原版速率是 0，会保持 0 不被改动。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.HearseFleetMultiplier)), "灵车数量" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.HearseFleetMultiplier)),
                  "放大单个设施同时可以派出的灵车数量。原版灵车一趟只装一具（写死在控制流里，不是数据），所以这是唯一能提高收运吞吐的杠杆。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StorageMultiplier)), "尸体存放容量" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StorageMultiplier)),
                  "放大设施在等待处理期间能存放的尸体数量。原版存放容量为 0 的设施不会被改动。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PrepareUnload)), "准备卸载（当前城市）" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PrepareUnload)),
                  "关闭调优、把所有倍率恢复为 1 倍，并把原版 prefab 数值写回。等状态显示已还原，再另存副本后移除 mod。" },
                { "DeathcareTune.PREPARE_UNLOAD_CONFIRM",
                  "关闭调优、把所有倍率恢复为 1 倍并写回原版数值？请等状态显示已还原后，再另存副本。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TuneStatus)), "当前状态" },
                { "DeathcareTune.STATUS.NoCity", "尚未加载城市" },
                { "DeathcareTune.STATUS.Pending", "等待处理，请恢复模拟" },
                { "DeathcareTune.STATUS.Applied", "已应用" },
                { "DeathcareTune.STATUS.Restored", "已还原为原版数值" },
                { "DeathcareTune.STATUS.Conflict", "存在冲突，部分设施已跳过，请查看 mod 日志" },
                { "DeathcareTune.STATUS.Failed", "处理失败，请勿保存，查看 mod 日志" },
                { "DeathcareTune.STATUS.Facilities", "设施" },
                { "DeathcareTune.STATUS.Conflicts", "冲突" },
            };
        }

        public void Unload() { }
    }
}
