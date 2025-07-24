using Colossal.UI.Binding;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace TollRoads
{
    public partial class TollRoadsUISystem : ExtendedUISystemBase
    {
        private ToolSystem toolSystem;
        private DefaultToolSystem defaultToolSystem;
        private TollRoadsToolSystem tollRoadsToolSystem;
        private EntityQuery tollLaneQuery;
        private ComponentLookup<TollLane> tollLaneLookup;

        private ValueBindingHelper<bool> tollRoadsToolEnabled;
        private ValueBindingHelper<TollRoadUIBinder[]> tollRoadsUIBinder;
        private TriggerBinding tollRoadsToolToggle;

        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info("OnCreate " + nameof(TollRoadsUISystem));
            toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            defaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            tollRoadsToolSystem = World.GetOrCreateSystemManaged<TollRoadsToolSystem>();
            tollLaneQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<TollLane>(),
                }
            });

            tollRoadsToolEnabled = CreateBinding("TollRoadsToolEnabled", false);
            tollRoadsUIBinder = CreateBinding("GetTollRoads", new TollRoadUIBinder[0]);

            CreateTrigger("ToggleTool", () => ToggleTool());
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            var tollLaneEntities = tollLaneQuery.ToEntityArray(Allocator.Temp);
            var binder = new TollRoadUIBinder[tollLaneEntities.Length];
            for (int i = 0; i < tollLaneEntities.Length; i++)
            {
                binder[i] = new TollRoadUIBinder
                {
                    Entity = tollLaneEntities[i].Index,
                    Toll = tollLaneLookup[tollLaneEntities[i]].toll,
                    Revenue = tollLaneLookup[tollLaneEntities[i]].revenue,
                    Name = "test",
                };
            }
            tollRoadsUIBinder.Value = binder;
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
            tollRoadsToolEnabled.Value = true;
        }

        private void ClearTool()
        {
            toolSystem.selected = Entity.Null;
            toolSystem.activeTool = defaultToolSystem;
        }
        public void OnToolDisabled()
        {
            tollRoadsToolEnabled.Value = false;
        }
    }
}
