using Game;
using Game.Common;
using Game.Net;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Game.City;
using Game.Simulation;
using Game.Economy;
using Game.Prefabs;
using Unity.Burst;

namespace TollRoads
{
    [BurstCompile]
    public partial class TollRoadsRevenueSystem : GameSystemBase
    {
        private struct TollRevenueJob : IJobChunk
        {
            [ReadOnly]
            public Entity city;

            public ComponentTypeHandle<TollLane> tollLaneType;

            [ReadOnly]
            public ComponentTypeHandle<Owner> ownerType;

            public ComponentLookup<PlayerMoney> playerMoneyLookup;

            [ReadOnly]
            public ComponentLookup<PrefabRef> prefabRefLookup;

            [ReadOnly]
            public ComponentLookup<CarData> carDataLookup;

            [ReadOnly]
            public ComponentLookup<Game.Net.CarLane> carLaneLookup;

            [ReadOnly]
            public ComponentLookup<Owner> ownerLookup;

            public BufferLookup<Resources> resourcesLookup;

            [ReadOnly]
            public BufferLookup<Game.Net.SubLane> subLanesLookup;

            [ReadOnly]
            public BufferLookup<LaneObject> laneObjectsLookup;

            public NativeQueue<ServiceFeeSystem.FeeEvent>.ParallelWriter feeQueue;

            [ReadOnly]
            public bool isNight;

            [ReadOnly]
            public bool resetRevenue;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Owner> owners = chunk.GetNativeArray(ref ownerType);
                NativeArray<TollLane> tollLanes = chunk.GetNativeArray(ref tollLaneType);
                for (int i = 0; i < tollLanes.Length; i++)
                {
                    int revenue = 0;
                    int volume = 0;
                    TollLane tollLane = tollLanes[i];
                    if (subLanesLookup.TryGetBuffer(owners[i].m_Owner, out var sublanes))
                    {
                        NativeHashSet<Entity> currFrameVehicles = new NativeHashSet<Entity>(tollLanes[i].vehicles.Count, Allocator.Temp);
                        foreach (var lane in sublanes)
                        {
                            if (carLaneLookup.HasComponent(lane.m_SubLane) && laneObjectsLookup.TryGetBuffer(lane.m_SubLane, out var laneObjects))
                            {
                                foreach (var obj in laneObjects)
                                {
                                    if (!tollLane.vehicles.Contains(obj.m_LaneObject)) // entering vehicles
                                    {
                                        int toll = isNight ? tollLane.nightToll : tollLane.toll;
                                        if (IsTruck(obj.m_LaneObject))
                                            toll = (int)(toll * tollLane.truckMultiplier);

                                        revenue += toll;
                                        volume++;
                                        TollDriver(obj.m_LaneObject, tollLane.toll);
                                        tollLane.vehicles.Add(obj.m_LaneObject);
                                    }

                                    currFrameVehicles.Add(obj.m_LaneObject);
                                }
                            }
                        }

                        foreach (var vehicle in tollLane.vehicles)
                        {
                            if (!currFrameVehicles.Contains(vehicle)) // exiting vehicles
                                tollLane.vehicles.Remove(vehicle);
                        }
                    }

                    RegisterRevenue(revenue);
                    tollLane.nextRevenue += revenue;
                    tollLane.nextVolume += volume;

                    // set revenue and volume vars for UI
                    if (resetRevenue)
                    {
                        tollLane.revenue = tollLane.nextRevenue;
                        tollLane.nextRevenue = 0;

                        tollLane.volume = tollLane.nextVolume;
                        tollLane.nextVolume = 0;
                    }

                    tollLanes[i] = tollLane;
                }
            }

            private void RegisterRevenue(int revenue)
            {
                PlayerMoney value2 = playerMoneyLookup[city];
                value2.Add(revenue);
                playerMoneyLookup[city] = value2;
                feeQueue.Enqueue(new ServiceFeeSystem.FeeEvent
                {
                    m_Amount = 1f,
                    m_Cost = revenue,
                    m_Resource = PlayerResource.Parking,
                    m_Outside = false
                });
            }

            private void TollDriver(Entity vehicle, int toll)
            {
                if (ownerLookup.TryGetComponent(vehicle, out var owner) && resourcesLookup.HasBuffer(owner.m_Owner))
                {
                    EconomyUtils.AddResources(Resource.Money, toll, resourcesLookup[owner.m_Owner]);
                }
            }

            private bool IsTruck(Entity vehicle)
            {
                if (prefabRefLookup.TryGetComponent(vehicle, out var prefabRef) && carDataLookup.TryGetComponent(prefabRef.m_Prefab, out var carData))
                {
                    return carData.m_SizeClass == Game.Vehicles.SizeClass.Large;
                }
                return false;
            }
        }

        private EntityQuery tollLanesQuery;
        private ServiceFeeSystem serviceFeeSystem;
        private TimeSystem timeSystem;
        private CitySystem citySystem;
        private SimulationSystem simulationSystem;
        private const int REVENUE_RESET_INDEX = 1024;

        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info("OnCreate " + nameof(TollRoadsRevenueSystem));
            serviceFeeSystem = World.GetOrCreateSystemManaged<ServiceFeeSystem>();
            citySystem = World.GetOrCreateSystemManaged<CitySystem>();
            timeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();

            tollLanesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[2]
                {
                    ComponentType.ReadOnly<TollLane>(),
                    ComponentType.ReadOnly<Owner>()
                },
                None = new ComponentType[3]
                {
                    ComponentType.ReadOnly<Created>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                }
            });
            RequireForUpdate(tollLanesQuery);
        }

        protected override void OnUpdate()
        {
            float normalizedTime = timeSystem.normalizedTime;
            bool isNight = normalizedTime < 0.25f || normalizedTime >= 11f / 12f;
            JobHandle deps;
            JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new TollRevenueJob
            {
                isNight = isNight,
                resetRevenue = simulationSystem.frameIndex % REVENUE_RESET_INDEX == 0,
                city = citySystem.City,
                ownerType = SystemAPI.GetComponentTypeHandle<Owner>(isReadOnly: true),
                tollLaneType = SystemAPI.GetComponentTypeHandle<TollLane>(),
                playerMoneyLookup = SystemAPI.GetComponentLookup<PlayerMoney>(),
                carLaneLookup = SystemAPI.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true),
                ownerLookup = SystemAPI.GetComponentLookup<Owner>(isReadOnly: true),
                prefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                carDataLookup = SystemAPI.GetComponentLookup<CarData>(isReadOnly: true),
                resourcesLookup = SystemAPI.GetBufferLookup<Resources>(),
                subLanesLookup = SystemAPI.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true),
                laneObjectsLookup = SystemAPI.GetBufferLookup<LaneObject>(isReadOnly: true),
                feeQueue = serviceFeeSystem.GetFeeQueue(out deps).AsParallelWriter()
            }, tollLanesQuery, JobHandle.CombineDependencies(base.Dependency, deps));
            serviceFeeSystem.AddQueueWriter(jobHandle);
            base.Dependency = jobHandle;
        }
    }
}
