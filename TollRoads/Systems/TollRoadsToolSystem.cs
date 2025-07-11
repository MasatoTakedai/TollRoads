using Colossal.Entities;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace TollRoads
{
    public partial class TollRoadsToolSystem : ToolBaseSystem
    {
        private PrefabSystem prefabSystem;
        private TollRoadsUISystem tollRoadsUISystem;
        private ToolSystem toolSystem;
        private DefaultToolSystem defaultToolSystem;
        private EntityQuery highlightedQuery;
        private Entity selectedEntity;
        private BufferLookup<Game.Net.SubLane> subLanesLookup;
        private ComponentLookup<SlaveLane> slaveLaneLookup;
        private ComponentLookup<Game.Net.CarLane> carLaneLookup;
        private ComponentLookup<TollLane> tollLaneLookup;
        private ComponentLookup<EdgeLane> edgeLaneLookup;

        private new ProxyAction applyAction;
        private new ProxyAction cancelAction;

        public override string toolID => "TollRoads Tool";
        public override PrefabBase GetPrefab() { return null; }
        public override bool TrySetPrefab(PrefabBase prefab) { return false; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info("OnCreate " + nameof(TollRoadsToolSystem));

            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            tollRoadsUISystem = World.GetOrCreateSystemManaged<TollRoadsUISystem>();
            toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            defaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            highlightedQuery = SystemAPI.QueryBuilder().WithAny<Highlighted>().Build();
            subLanesLookup = SystemAPI.GetBufferLookup<Game.Net.SubLane>();
            slaveLaneLookup = SystemAPI.GetComponentLookup<SlaveLane>();
            carLaneLookup = SystemAPI.GetComponentLookup<Game.Net.CarLane>();
            tollLaneLookup = SystemAPI.GetComponentLookup<TollLane>();
            edgeLaneLookup = SystemAPI.GetComponentLookup<EdgeLane>();

            applyAction = Mod.Settings!.GetAction(nameof(TollRoads) + "Apply");
            var builtInApplyAction = InputManager.instance.FindAction(InputManager.kToolMap, "Apply");
            Mod.log.Debug(applyAction);
            var mimicApplyBinding = applyAction.bindings.FirstOrDefault(b => b.device == InputManager.DeviceType.Mouse);
            var builtInApplyBinding = builtInApplyAction.bindings.FirstOrDefault(b => b.device == InputManager.DeviceType.Mouse);
            mimicApplyBinding.path = builtInApplyBinding.path;
            mimicApplyBinding.modifiers = builtInApplyBinding.modifiers;
            InputManager.instance.SetBinding(mimicApplyBinding, out _);

            cancelAction = Mod.Settings!.GetAction(nameof(TollRoads) + "Cancel");
            var builtInCancelAction = InputManager.instance.FindAction(InputManager.kToolMap, "Cancel");
            var mimicCancelBinding = cancelAction.bindings.FirstOrDefault(b => b.device == InputManager.DeviceType.Mouse);
            var builtInCancelBinding = builtInCancelAction.bindings.FirstOrDefault(b => b.device == InputManager.DeviceType.Mouse);
            mimicCancelBinding.path = builtInCancelBinding.path;
            mimicCancelBinding.modifiers = builtInCancelBinding.modifiers;
            InputManager.instance.SetBinding(mimicCancelBinding, out _);
        }


        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            applyAction.shouldBeEnabled = true;
            cancelAction.shouldBeEnabled = true;
            tollRoadsUISystem.OnToolEnabled();
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();

            applyAction.shouldBeEnabled = false;
            cancelAction.shouldBeEnabled = false;
            selectedEntity = Entity.Null;
            tollRoadsUISystem.OnToolDisabled();

            var entities = highlightedQuery.ToEntityArray(Allocator.Temp);
            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                EntityManager.RemoveComponent<Highlighted>(entity);
                EntityManager.AddComponent<BatchesUpdated>(entity);
            }
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();

            m_ToolRaycastSystem.netLayerMask = Layer.Road;
            m_ToolRaycastSystem.typeMask = TypeMask.Net;
            m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            bool raycastHit = HandlePicker(out Entity hoveredEntity, out RaycastHit hit);
            HandleHighlight(highlightedQuery, hoveredEntity);
            if (raycastHit)
            {
                TryHighlightEntity(hoveredEntity);
            }

            if (applyAction.WasPerformedThisFrame())
            {
                selectedEntity = hoveredEntity;
                ApplyTollsOnLane(hit);
            }
            else if (cancelAction.WasPerformedThisFrame())
            {
                if (raycastHit)
                {
                    RemoveTollsFromLane(hit);
                    selectedEntity = Entity.Null;
                }
                else
                {
                    toolSystem.selected = Entity.Null;
                    toolSystem.activeTool = defaultToolSystem;
                }
            }

            return base.OnUpdate(inputDeps);
        }

        private bool HandlePicker(out Entity entity, out RaycastHit hit)
        {
            if (!GetRaycastResult(out entity, out hit))
            {
                return false;
            }

            if (!EntityManager.TryGetComponent<PrefabRef>(entity, out var prefabRef) || EntityManager.HasComponent<Owner>(entity) || !EntityManager.HasComponent<Edge>(entity))
            {
                return false;
            }

            if (!prefabSystem.TryGetPrefab<NetGeometryPrefab>(prefabRef, out var prefab))
            {
                return false;
            }

            if (prefab is not (RoadPrefab))
            {
                return false;
            }

            if (!EntityManager.HasComponent<Road>(entity))
            {
                return false;
            }

            return true;
        }

        private void TryHighlightEntity(Entity entity)
        {
            if (entity != Entity.Null && !EntityManager.HasComponent<Highlighted>(entity))
            {
                EntityManager.AddComponent<Highlighted>(entity);
                EntityManager.AddComponent<BatchesUpdated>(entity);
            }
        }

        private void HandleHighlight(EntityQuery query, Entity hoveredEntity)
        {
            var entities = query.ToEntityArray(Allocator.Temp);

            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (entity != hoveredEntity && entity != selectedEntity)
                { 
                    EntityManager.RemoveComponent<Highlighted>(entity);
                    EntityManager.AddComponent<BatchesUpdated>(entity);
                }
            }
        }

        private void ApplyTollsOnLane(RaycastHit hit)
        {
            if (subLanesLookup.TryGetBuffer(selectedEntity, out var sublanes))
            {
                foreach (var sublane in sublanes)
                {
                    if (carLaneLookup.HasComponent(sublane.m_SubLane) && !slaveLaneLookup.HasComponent(sublane.m_SubLane))
                    {
                        // skip the second edges of roads broken into two
                        if (edgeLaneLookup.TryGetComponent(sublane.m_SubLane, out var edgeLane) && edgeLane.m_EdgeDelta.x > 0 && edgeLane.m_EdgeDelta.y > 0)
                            continue;

                        TollLane tollLane = new TollLane(20);
                        EntityManager.AddComponentData(sublane.m_SubLane, tollLane);
                        EntityManager.AddComponent<PathfindUpdated>(sublane.m_SubLane);
                    }
                }
            }
        }

        private void RemoveTollsFromLane(RaycastHit hit)
        {
            if (subLanesLookup.TryGetBuffer(selectedEntity, out var sublanes))
            {
                foreach (var sublane in sublanes)
                {
                    if (tollLaneLookup.HasComponent(sublane.m_SubLane))
                    {
                        EntityManager.RemoveComponent<TollLane>(sublane.m_SubLane);
                        EntityManager.AddComponent<PathfindUpdated>(sublane.m_SubLane);
                    }
                }
            }
        }
    }
}
