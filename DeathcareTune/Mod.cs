using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace DeathcareTune
{
    public class Mod : IMod
    {
        public static readonly ILog s_Log =
            LogManager.GetLogger(nameof(DeathcareTune)).SetShowsErrorsInUI(true);

        public static Setting s_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            s_Log.Info("DeathcareTune: OnLoad");

            s_Setting = new Setting(this);
            s_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(s_Setting));
            GameManager.instance.localizationManager.AddSource("zh-HANS", new LocaleZH(s_Setting));
            AssetDatabase.global.LoadSettings(nameof(DeathcareTune), s_Setting, new Setting(this));

            // 只在城市加载完成 / 设置变更时干一次活，之后空转。
            updateSystem.UpdateAt<Systems.DeathcareTuneSystem>(SystemUpdatePhase.GameSimulation);
            Systems.DeathcareTuneSystem.ResumeAll();

            s_Log.Info("DeathcareTune: system registered");
        }

        public void OnDispose()
        {
            s_Log.Info("DeathcareTune: OnDispose");
            // 在 ECS World 仍然可用时同步还原。
            bool restored = Systems.DeathcareTuneSystem.RestoreAllNow();
            if (!restored)
                s_Log.Warn("[还原未完成] 有 prefab 的数据已被其他来源修改，未覆盖；不应把本次退出视为完成卸载清理");
            s_Setting?.UnregisterInOptionsUI();
            s_Setting = null;
        }
    }
}
