using Colossal.UI.Binding;
using Game.Tools;
using Game.UI;
using Unity.Entities;

namespace TollRoads
{
    public partial class TollRoadsUISystem : UISystemBase
    {
        private ToolSystem toolSystem;
        private DefaultToolSystem defaultToolSystem;
        private TollRoadsToolSystem tollRoadsToolSystem;

        private ValueBinding<bool> tollRoadsToolEnabled;
        private TriggerBinding tollRoadsToolToggle;

        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info("OnCreate " + nameof(TollRoadsUISystem));
            toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            defaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            tollRoadsToolSystem = World.GetOrCreateSystemManaged<TollRoadsToolSystem>();

            tollRoadsToolEnabled = new ValueBinding<bool>(nameof(TollRoads), "TollRoadsToolEnabled", false);
            AddBinding(tollRoadsToolEnabled);

            tollRoadsToolToggle = new TriggerBinding(nameof(TollRoads), "ToggleTool", () => ToggleTool());
            AddBinding(tollRoadsToolToggle);
        }

        public void ToggleTool(bool? enable = null)
        {
            if (enable == true || (enable is null && toolSystem.activeTool is not TollRoadsToolSystem))
            {
                EnableTool();
            }
            else
            {
                ClearTool();
            }
        }

        private void EnableTool()
        {
            toolSystem.selected = Entity.Null;
            toolSystem.activeTool = tollRoadsToolSystem;
        }
        public void OnToolEnabled()
        {
            tollRoadsToolEnabled.Update(true);
        }

        private void ClearTool()
        {
            toolSystem.selected = Entity.Null;
            toolSystem.activeTool = defaultToolSystem;
        }
        public void OnToolDisabled()
        {
            tollRoadsToolEnabled.Update(false);
        }
    }
}
