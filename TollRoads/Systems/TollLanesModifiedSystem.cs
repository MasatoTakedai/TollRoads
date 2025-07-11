using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Game.Pathfind;
using Game.Tools;
using Colossal.Serialization.Entities;

namespace TollRoads
{
    public partial class TollLanesModifiedSystem : GameSystemBase
    {
        [BurstCompile]
        private struct UpdateTollEdgeJob : IJob
        {
            // adapted from UpdateEdgeJob from LanesModifiedSystem and only kept lines for CarLanes
            // custom code start

            [ReadOnly]
            public ComponentTypeHandle<TollLane> tollLaneType;

            public PathSpecification AddTollCosts(PathSpecification pathSpec, TollLane tollLane)
            {
                PathSpecification newPathSpec = pathSpec;
                newPathSpec.m_Costs.m_Value += new float4(0, 0, tollLane.toll, 0);
                return newPathSpec;
            }

            // custom code end


            [ReadOnly]
            public NativeList<ArchetypeChunk> m_Chunks;

            [ReadOnly]
            public ComponentLookup<Lane> m_LaneData;

            [ReadOnly]
            public ComponentLookup<Density> m_DensityData;

            [ReadOnly]
            public ComponentLookup<NetLaneData> m_NetLaneData;

            [ReadOnly]
            public ComponentLookup<CarLaneData> m_CarLaneData;

            [ReadOnly]
            public ComponentLookup<PathfindCarData> m_CarPathfindData;

            [ReadOnly]
            public ComponentLookup<PathfindTransportData> m_TransportPathfindData;

            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public ComponentTypeHandle<Owner> m_OwnerType;

            [ReadOnly]
            public ComponentTypeHandle<Lane> m_LaneType;

            [ReadOnly]
            public ComponentTypeHandle<SlaveLane> m_SlaveLaneType;

            [ReadOnly]
            public ComponentTypeHandle<Curve> m_CurveType;

            [ReadOnly]
            public ComponentTypeHandle<Game.Net.CarLane> m_CarLaneType;

            [ReadOnly]
            public ComponentTypeHandle<EdgeLane> m_EdgeLaneType;

            [ReadOnly]
            public ComponentTypeHandle<Game.Net.TrackLane> m_TrackLaneType;

            [ReadOnly]
            public ComponentTypeHandle<LaneConnection> m_LaneConnectionType;

            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

            [WriteOnly]
            public NativeArray<UpdateActionData> m_Actions;

            public void Execute()
            {
                int num = 0;
                for (int i = 0; i < m_Chunks.Length; i++)
                {
                    ArchetypeChunk archetypeChunk = m_Chunks[i];
                    NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
                    NativeArray<Lane> nativeArray2 = archetypeChunk.GetNativeArray(ref m_LaneType);
                    NativeArray<Curve> nativeArray3 = archetypeChunk.GetNativeArray(ref m_CurveType);
                    NativeArray<PrefabRef> nativeArray4 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
                    NativeArray<Game.Net.CarLane> nativeArray5 = archetypeChunk.GetNativeArray(ref m_CarLaneType);
                    NativeArray<Game.Net.TrackLane> nativeArray6 = archetypeChunk.GetNativeArray(ref m_TrackLaneType);
                    NativeArray<TollLane> tollLanes = archetypeChunk.GetNativeArray(ref tollLaneType);
                    if (nativeArray5.Length != 0 && !archetypeChunk.Has(ref m_SlaveLaneType))
                    {
                        NativeArray<Owner> nativeArray7 = archetypeChunk.GetNativeArray(ref m_OwnerType);
                        NativeArray<LaneConnection> nativeArray8 = archetypeChunk.GetNativeArray(ref m_LaneConnectionType);
                        archetypeChunk.Has(ref m_EdgeLaneType);
                        if (nativeArray6.Length != 0)
                        {
                            for (int j = 0; j < nativeArray5.Length; j++)
                            {
                                Lane lane = nativeArray2[j];
                                Curve curve = nativeArray3[j];
                                Game.Net.CarLane carLane = nativeArray5[j];
                                Game.Net.TrackLane trackLaneData = nativeArray6[j];
                                PrefabRef prefabRef = nativeArray4[j];
                                NetLaneData netLaneData = m_NetLaneData[prefabRef.m_Prefab];
                                CarLaneData carLaneData = m_CarLaneData[prefabRef.m_Prefab];
                                PathfindCarData carPathfindData = m_CarPathfindData[netLaneData.m_PathfindPrefab];
                                PathfindTransportData transportPathfindData = m_TransportPathfindData[netLaneData.m_PathfindPrefab];
                                float num2 = 0.01f;
                                if (nativeArray7.Length != 0)
                                {
                                    Owner owner = nativeArray7[j];
                                    if (m_DensityData.HasComponent(owner.m_Owner))
                                    {
                                        num2 = math.max(num2, m_DensityData[owner.m_Owner].m_Density);
                                    }
                                }
                                if (nativeArray8.Length != 0)
                                {
                                    CheckLaneConnections(ref lane, nativeArray8[j]);
                                }
                                UpdateActionData value = new UpdateActionData
                                {
                                    m_Owner = nativeArray[j],
                                    m_StartNode = lane.m_StartNode,
                                    m_MiddleNode = lane.m_MiddleNode,
                                    m_EndNode = lane.m_EndNode,
                                    m_Specification = AddTollCosts(PathUtils.GetCarDriveSpecification(curve, carLane, trackLaneData, carLaneData, carPathfindData, num2), tollLanes[j]),
                                    m_Location = PathUtils.GetLocationSpecification(curve)
                                };
                                if (carLaneData.m_RoadTypes == RoadTypes.Car)
                                {
                                    value.m_SecondaryStartNode = value.m_StartNode;
                                    value.m_SecondaryEndNode = value.m_EndNode;
                                    value.m_SecondarySpecification = PathUtils.GetTaxiDriveSpecification(curve, carLane, carPathfindData, transportPathfindData, num2);
                                }
                                m_Actions[num++] = value;
                            }
                            continue;
                        }
                        for (int k = 0; k < nativeArray5.Length; k++)
                        {
                            Lane lane2 = nativeArray2[k];
                            Curve curve2 = nativeArray3[k];
                            Game.Net.CarLane carLane2 = nativeArray5[k];
                            PrefabRef prefabRef2 = nativeArray4[k];
                            NetLaneData netLaneData2 = m_NetLaneData[prefabRef2.m_Prefab];
                            CarLaneData carLaneData2 = m_CarLaneData[prefabRef2.m_Prefab];
                            PathfindCarData carPathfindData2 = m_CarPathfindData[netLaneData2.m_PathfindPrefab];
                            PathfindTransportData transportPathfindData2 = m_TransportPathfindData[netLaneData2.m_PathfindPrefab];
                            float num3 = 0.01f;
                            if (nativeArray7.Length != 0)
                            {
                                Owner owner2 = nativeArray7[k];
                                if (m_DensityData.HasComponent(owner2.m_Owner))
                                {
                                    num3 = math.max(num3, m_DensityData[owner2.m_Owner].m_Density);
                                }
                            }
                            if (nativeArray8.Length != 0)
                            {
                                CheckLaneConnections(ref lane2, nativeArray8[k]);
                            }
                            UpdateActionData value2 = new UpdateActionData
                            {
                                m_Owner = nativeArray[k],
                                m_StartNode = lane2.m_StartNode,
                                m_MiddleNode = lane2.m_MiddleNode,
                                m_EndNode = lane2.m_EndNode,
                                m_Specification = AddTollCosts(PathUtils.GetCarDriveSpecification(curve2, carLane2, carLaneData2, carPathfindData2, num3), tollLanes[k]),
                                m_Location = PathUtils.GetLocationSpecification(curve2)
                            };
                            if (carLaneData2.m_RoadTypes == RoadTypes.Car)
                            {
                                value2.m_SecondaryStartNode = value2.m_StartNode;
                                value2.m_SecondaryEndNode = value2.m_EndNode;
                                value2.m_SecondarySpecification = PathUtils.GetTaxiDriveSpecification(curve2, carLane2, carPathfindData2, transportPathfindData2, num3);
                            }
                            m_Actions[num++] = value2;
                        }
                        continue;
                    }
                }
            }

            private void CheckLaneConnections(ref Lane lane, LaneConnection laneConnection)
            {
                if (m_LaneData.HasComponent(laneConnection.m_StartLane))
                {
                    lane.m_StartNode = new PathNode(m_LaneData[laneConnection.m_StartLane].m_MiddleNode, laneConnection.m_StartPosition);
                }
                if (m_LaneData.HasComponent(laneConnection.m_EndLane))
                {
                    lane.m_EndNode = new PathNode(m_LaneData[laneConnection.m_EndLane].m_MiddleNode, laneConnection.m_EndPosition);
                }
            }
        }

        private PathfindQueueSystem pathfindQueueSystem;
        private EntityQuery updatedTollLanesQuery;
        private EntityQuery initialTollLanesQuery;
        private bool skipFirstFrame;
        private bool init;

        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info("OnCreate " + nameof(TollLanesModifiedSystem));
            pathfindQueueSystem = base.World.GetOrCreateSystemManaged<PathfindQueueSystem>();
            initialTollLanesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[2] {
                ComponentType.ReadOnly<Game.Net.CarLane>(),
                ComponentType.ReadOnly<TollLane>()
            },
                None = new ComponentType[3]
            {
                ComponentType.ReadOnly<Temp>(),
                ComponentType.ReadOnly<SlaveLane>(),
                ComponentType.ReadOnly<Deleted>()
            }
            });

            updatedTollLanesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[3]
                {
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Game.Net.CarLane>(),
                    ComponentType.ReadOnly<TollLane>()
                },
                None = new ComponentType[4]
                {
                    ComponentType.ReadOnly<Created>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<SlaveLane>()
                }
            }, new EntityQueryDesc
            {
                All = new ComponentType[3]
                {
                    ComponentType.ReadOnly<PathfindUpdated>(),
                    ComponentType.ReadOnly<Game.Net.CarLane>(),
                    ComponentType.ReadOnly<TollLane>()
                },
                None = new ComponentType[4]
                {
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<SlaveLane>()
                }
            });
        }

        protected override void OnUpdate()
        {
            // add PathfindUpdated flag to all TollLanes on first frame to run it through our custom job
            if (skipFirstFrame)
            {
                skipFirstFrame = false;
                return;
            }

            EntityQuery entityQuery;
            if (init)
                entityQuery = initialTollLanesQuery;
            else
                 entityQuery = updatedTollLanesQuery;
            int queryCount = entityQuery.CalculateEntityCount();
            if (queryCount == 0)
                return;
            init = false;

            JobHandle jobHandle = base.Dependency;
            UpdateAction action = new UpdateAction(queryCount, Allocator.Persistent);
            JobHandle outJobHandle;
            NativeList<ArchetypeChunk> chunks = entityQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
            JobHandle jobHandle2 = IJobExtensions.Schedule(new UpdateTollEdgeJob
            {
                m_Chunks = chunks,
                m_LaneData = SystemAPI.GetComponentLookup<Lane>(isReadOnly: true),
                m_DensityData = SystemAPI.GetComponentLookup<Density>(isReadOnly: true),
                m_NetLaneData = SystemAPI.GetComponentLookup<NetLaneData>(isReadOnly: true),
                m_CarLaneData = SystemAPI.GetComponentLookup<CarLaneData>(isReadOnly: true),
                m_CarPathfindData = SystemAPI.GetComponentLookup<PathfindCarData>(isReadOnly: true),
                m_TransportPathfindData = SystemAPI.GetComponentLookup<PathfindTransportData>(isReadOnly: true),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_OwnerType = SystemAPI.GetComponentTypeHandle<Owner>(isReadOnly: true),
                m_LaneType = SystemAPI.GetComponentTypeHandle<Lane>(isReadOnly: true),
                m_SlaveLaneType = SystemAPI.GetComponentTypeHandle<SlaveLane>(isReadOnly: true),
                m_CurveType = SystemAPI.GetComponentTypeHandle<Curve>(isReadOnly: true),
                m_CarLaneType = SystemAPI.GetComponentTypeHandle<Game.Net.CarLane>(isReadOnly: true),
                m_EdgeLaneType = SystemAPI.GetComponentTypeHandle<EdgeLane>(isReadOnly: true),
                m_TrackLaneType = SystemAPI.GetComponentTypeHandle<Game.Net.TrackLane>(isReadOnly: true),
                m_LaneConnectionType = SystemAPI.GetComponentTypeHandle<LaneConnection>(isReadOnly: true),
                m_PrefabRefType = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true),
                m_Actions = action.m_UpdateData,
                tollLaneType = SystemAPI.GetComponentTypeHandle<TollLane>(isReadOnly: true),
            }, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
            jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
            chunks.Dispose(jobHandle2);
            pathfindQueueSystem.Enqueue(action, jobHandle2);
            base.Dependency = jobHandle;
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            skipFirstFrame = true;
            init = true;
        }
    }
}
