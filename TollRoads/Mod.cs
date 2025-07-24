using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.Pathfind;
using Game.SceneFlow;
using Game.Simulation;
using HarmonyLib;

namespace TollRoads
{
    public class Mod : IMod
    {
        public static string Id = nameof(TollRoads);
        public static ILog log = LogManager.GetLogger($"{nameof(TollRoads)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public static Setting? Settings { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            Settings = new Setting(this);
            Settings.RegisterKeyBindings();
            Settings.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));

            AssetDatabase.global.LoadSettings(nameof(TollRoads), Settings, new Setting(this));

            updateSystem.UpdateAt<TollRoadsToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<TollRoadsUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<TollLanesModifiedSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateBefore<TollRoadsRevenueSystem, TrafficFlowSystem>(SystemUpdatePhase.GameSimulation);

            var harmony = new Harmony("daancingbanana.tollroads");
            harmony.PatchAll();
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }
        }
    }
}
