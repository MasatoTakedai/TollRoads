using Colossal.UI.Binding;
using Game.Rendering;
using Game.Simulation;
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
        private CameraUpdateSystem cameraUpdateSystem;
        private TimeSystem timeSystem;

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
            cameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
            timeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            tollLaneLookup = SystemAPI.GetComponentLookup<TollLane>();
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
            CreateTrigger<Entity>("NavigateTo", e => NavigateTo(e));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            var tollLaneEntities = tollLaneQuery.ToEntityArray(Allocator.Temp);
            var binder = new TollRoadUIBinder[tollLaneEntities.Length];
            for (int i = 0; i < tollLaneEntities.Length; i++)
            {
                TollLane tollLane = tollLaneLookup[tollLaneEntities[i]];
                binder[i] = new TollRoadUIBinder
                {
                    Entity = tollLaneEntities[i],
                    Toll = GetCurrentToll(tollLane),
                    Revenue = tollLane.revenue,
                    Volume = tollLane.volume,
                    Name = "test" + i,
                };
            }
            tollRoadsUIBinder.Value = binder;
        }

        private int GetCurrentToll(TollLane tollLane)
        {
            float normalizedTime = timeSystem.normalizedTime;
            bool isNight = normalizedTime < 0.25f || normalizedTime >= 11f / 12f;

            return isNight ? tollLane.nightToll : tollLane.toll;
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

        private void NavigateTo(Entity entity)
        {
            if (cameraUpdateSystem.orbitCameraController != null && entity != Entity.Null)
            {
                cameraUpdateSystem.orbitCameraController.followedEntity = entity;
                cameraUpdateSystem.orbitCameraController.TryMatchPosition(cameraUpdateSystem.activeCameraController);
                cameraUpdateSystem.activeCameraController = cameraUpdateSystem.orbitCameraController;
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
