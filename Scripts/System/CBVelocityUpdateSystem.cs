using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace CombatBee
{
    //[UpdateInGroup(typeof(CombatBeeSystemGroup))]
    //[UpdateAfter(typeof(CombatBeeInitializationSystem))]
    //[UpdateBefore(typeof(CBPositionUpdateSystem))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [DisableAutoCreation]
    public partial struct CBVelocityUpdateSystem : ISystem, ISystemStartStop
    {
        private EntityQuery aliveQuery;
        private EntityQuery teamZeroQuery;
        private EntityQuery teamOneQuery;
        private EntityQuery resQuery;
        EntityStorageInfoLookup entityStorageInfoLookup;
        float gravity;
        float fieldSizeX;
        float grabDistanceSqr;
        float aggression;
        float attackDistanceSquared;
        float carryForce;
        float chaseForce;
        float attackForce;
        float hitDistanceSquared;
        float flightJitter;
        float OneMinusDamping;
        float teamAttraction;
        float teamRepulsion;
        float3 bloodColor;
        Entity ParticlePrefab;
        int GridXCount;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConfigCOMCB>();
            state.RequireForUpdate<BeePool>();
            aliveQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TeamCB>().WithDisabled<IsInPoolCB>().WithDisabled<BeeDead>().Build(ref state);

            //teamZeroQuery.AddSharedComponentFilter<TeamCB>(new TeamCB { teamID = 0 });
            //teamOneQuery = teamZeroQuery;
            //teamOneQuery.AddSharedComponentFilter<TeamCB>(new TeamCB { teamID = 1 });
            //teamZeroQuery = aliveQuery
            resQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<ResGridCoordinateCB>().Build(ref state);
        }

        public void OnStartRunning(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<ConfigCOMCB>();
            grabDistanceSqr = config.grabDistance * config.grabDistance;
            aggression = config.aggression;
            attackDistanceSquared = config.attackDistance * config.attackDistance;
            chaseForce = config.chaseForce;
            attackForce = config.chaseForce;
            hitDistanceSquared = config.hitDistance * config.hitDistance;
            carryForce = config.carryForce;
            flightJitter = config.flightJitter;
            OneMinusDamping = 1f - config.damping;
            teamAttraction = config.teamAttraction;
            teamRepulsion = config.teamRepulsion;
            bloodColor = config.bloodColor;
            ParticlePrefab = config.ParticlePrefab;
            entityStorageInfoLookup = SystemAPI.GetEntityStorageInfoLookup();
            var field = SystemAPI.GetSingleton<FieldCOM>();
            gravity = field.gravity;
            fieldSizeX = field.size.x;
            GridXCount = (int)(fieldSizeX / config.resourceSize);
            //var particlePool = SystemAPI.GetSingletonRW<ParticlePool>();
            //CombatBeeSpawnerClass.SpawnBlood(ref state, ref particlePool.ValueRW.pool, new float3(10, 10, 10), new float3(.6f, .6f, 0.6f), 6f, 8);

        }
        public void OnStopRunning(ref SystemState state)
        {

        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            //var beePool = SystemAPI.GetSingletonRW<BeePool>();
            var particlePool = SystemAPI.GetSingletonRW<ParticlePool>();
            var random = SystemAPI.GetSingletonRW<RandomSingletonCB>();
            //the logic of dead bees should be update in the main thread
            foreach (var (beeDeadCom, beeVelCom, beePosCom, entity) in SystemAPI.Query<RefRW<BeeDead>, RefRW<BeeVelocity>, RefRO<BeePosition>>()
                .WithDisabled<IsInPoolCB>()
                .WithEntityAccess())
            {
                if (random.ValueRW.random.NextFloat(0, 1) < (beeDeadCom.ValueRO.deathTimer - .5f) * .5f)
                {
                    SpawnBlood(ref state, bloodColor, ParticlePrefab, particlePool, random, beePosCom.ValueRO.position, float3.zero);// ref particlePool.ValueRW.pool, beePosCom.ValueRO.position, float3.zero);
                }
                beeVelCom.ValueRW.velocity += gravity * deltaTime;
                if ((beeDeadCom.ValueRW.deathTimer -= deltaTime / 3f) < 0f)
                {
                    state.EntityManager.SetComponentEnabled<ShouldBeRecycledToPoolCB>(entity, true);
                    //state.EntityManager.SetComponentEnabled<BeeDead>(entity, false);
                    //state.EntityManager.SetComponentEnabled<IsInPoolCB>(entity, true);
                }
            }

            var beePositionLookup = SystemAPI.GetComponentLookup<BeePosition>(true);
            teamZeroQuery = aliveQuery;
            teamZeroQuery.AddSharedComponentFilter<TeamCB>(new TeamCB { teamID = 0 });
            var teamZeroEntityArray = teamZeroQuery.ToEntityArray(Allocator.TempJob);
            teamZeroQuery.ResetFilter();
            //aliveQuery.AddSharedComponentFilter<TeamCB>(new TeamCB { teamID = 1 });
            teamOneQuery = aliveQuery;
            teamOneQuery.AddSharedComponentFilter<TeamCB>(new TeamCB { teamID = 1 });
            var teamOneEntityArray = aliveQuery.ToEntityArray(Allocator.TempJob);
            teamOneQuery.ResetFilter();

            //var teamZeroEntityArray = teamZeroQuery.ToEntityArray(Allocator.TempJob);
            //var teamOneEntityArray = teamOneQuery.ToEntityArray(Allocator.TempJob);
            
            state.Dependency = new AliveBeeVelModifyJob
            {
                beePositionLookup = beePositionLookup,
                deltaTime = deltaTime,
                flightJitter = flightJitter,
                OneMinusDamping = OneMinusDamping,
                random = random,
                teamAttraction = teamAttraction,
                teamRepulsion = teamRepulsion,
                teamZeroEntityArray = teamZeroEntityArray,
                teamOneEntityArray = teamOneEntityArray,
                teamOneLen = teamOneEntityArray.Length,
                teamZeroLen = teamZeroEntityArray.Length,
            }.ScheduleParallel(state.Dependency);

            var beeResLookup = SystemAPI.GetComponentLookup<BeeResourceTarget>();
            var beeEnemyLookup = SystemAPI.GetComponentLookup<BeeEnemyTarget>();
            var resourceEntityArray = resQuery.ToEntityArray(Allocator.TempJob);
            state.Dependency = new BeeNoTargetJob
            {
                aggression = aggression,
                random = random,
                teamOneArray = teamOneEntityArray,
                teamZeroArray = teamZeroEntityArray,
                resourceArray = resourceEntityArray,
                resTarLookup = beeResLookup,
                enemyTarLookup = beeEnemyLookup,
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            //Debug.Log("TeamOneLen" + teamOneEntityArray.Length);
            //Debug.Log("TeamZeroLen" + teamZeroEntityArray.Length);
            teamZeroEntityArray.Dispose();
            teamOneEntityArray.Dispose();
            resourceEntityArray.Dispose();
            //the logic of BeesHasEnemyTarget should be update in the main thread
            particlePool = SystemAPI.GetSingletonRW<ParticlePool>();
            foreach (var (beeVel, beeEnemyTar, beePos,entity) in SystemAPI.Query<RefRW<BeeVelocity>,RefRW<BeeEnemyTarget>, RefRO<BeePosition>>()
                .WithDisabled<BeeDead>()
                .WithDisabled<IsInPoolCB>()
                .WithEntityAccess())
            {
                if(beeEnemyTar.ValueRW.enemy == Entity.Null)
                {
                    state.EntityManager.SetComponentEnabled<BeeEnemyTarget>(entity,false);
                }
                else
                {   
                    var enemy = beeEnemyTar.ValueRW.enemy;
                    var enemyPos = SystemAPI.GetComponent<BeePosition>(enemy).position;
                    var beePosVal = beePos.ValueRO.position;
                    var delta = enemyPos - beePosVal;
                    float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                    if(sqrDist > attackDistanceSquared)
                    {
                        beeVel.ValueRW.velocity += delta * (chaseForce * deltaTime / math.sqrt(sqrDist));
                    }
                    else
                    {
                        state.EntityManager.SetComponentEnabled<BeeStateAttacking>(entity,true);
                        beeVel.ValueRW.velocity += delta * (attackForce * deltaTime / math.sqrt(sqrDist));
                        if(sqrDist < hitDistanceSquared)
                        {
                            state.EntityManager.SetComponentEnabled<BeeStateAttacking>(entity, false);
                            //spawn blood
                            //CombatBeeSpawnerClass.SpawnBlood(ref state, ref particlePool.ValueRW.pool, enemyPos, beeVel.ValueRO.velocity * .35f, 2f, 6);
                            SpawnBlood(ref state, bloodColor, ParticlePrefab, particlePool, random, enemyPos, beeVel.ValueRO.velocity * .35f, 2f, 6);
/*                            Debug.Log(enemyPos.ToString());
                            Debug.Log(beePosVal.ToString());
                            Debug.Log(sqrDist.ToString());
                            Debug.Log(hitDistanceSquared.ToString());*/
                            state.EntityManager.SetComponentEnabled<BeeDead>(enemy, true);
                            var oldVelOfEnemy = state.EntityManager.GetComponentData<BeeVelocity>(enemy).velocity;
                            state.EntityManager.SetComponentData(enemy, new BeeVelocity { velocity = oldVelOfEnemy *.5f });
                            state.EntityManager.SetComponentEnabled<BeeEnemyTarget>(entity, false);
                            
                        }
                    }
                }
            }

            EntityCommandBuffer ecbForJobHoldingRes = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new BeesHoldingResJob
            {
                ecb = ecbForJobHoldingRes.AsParallelWriter(),
                fieldSizeX = fieldSizeX,
                carryForce = carryForce,
                deltaTime = deltaTime,
                //resIsCarriedLookup = resIsCarriedLookup,
                //stateHoldingLookup = stateHoldingLp,
                //resourceTargetLookup = beeResLp,
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            ecbForJobHoldingRes.Playback(state.EntityManager);
            ecbForJobHoldingRes.Dispose();
            //ecb.Playback(state.EntityManager);

            EntityCommandBuffer ecbForJobWithResTar = new EntityCommandBuffer(Allocator.TempJob);

            var localTransLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var resGridCoorLookup = SystemAPI.GetComponentLookup<ResGridCoordinateCB>(true);
            var resIsCarriedLookup = SystemAPI.GetComponentLookup<ResIsCarriedCB>(false);
            var waitToCheckLookup = SystemAPI.GetComponentLookup<ResWaitToCheck>(false);
            var resStackLp = SystemAPI.GetComponentLookup<ResStackCB>(false);
            var beeEneLp = SystemAPI.GetComponentLookup<BeeEnemyTarget>(false);
            var stackHeights = SystemAPI.GetSingletonBuffer<StackHeightCOMCB>(false);
            var stateHoldingLp = SystemAPI.GetComponentLookup<BeeStateHoldingResource>(false);
            var beeResLp = SystemAPI.GetComponentLookup<BeeResourceTarget>(false);

            entityStorageInfoLookup.Update(ref state);
            state.Dependency = new BeesWithResTargetJob
            {
                entityStorageInfoLookup = entityStorageInfoLookup,
                //ecb = ecbForJobWithResTar.AsParallelWriter(),
                deltaTime = deltaTime,
                chaseForce = chaseForce,
                grabDistanceSqr = grabDistanceSqr,

                localTransformLookup = localTransLookup,
                resIsCarriedLookup = resIsCarriedLookup,
                resGridCoordinateLookup = resGridCoorLookup,

                waitToCheckLookup = waitToCheckLookup,
                resStackLookup = resStackLp,
                beeResTargetLookup = beeResLp,
                beeEnemyTarLookup = beeEneLp,
                stackHeightsLookup = stackHeights,
                stateHoldingLookup = stateHoldingLp,
                GridXCount = GridXCount,
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            ecbForJobWithResTar.Playback(state.EntityManager);
            ecbForJobWithResTar.Dispose();

        }
        [BurstCompile]
        void SpawnBlood(ref SystemState state, float3 color,Entity ParticlePrefab, RefRW<ParticlePool> Pool, RefRW<RandomSingletonCB> Random,float3 position, float3 velocity, float velocityJitter = 6f, int count = 1)
        {
            var pool = Pool.ValueRW.pool;
            float3 one = new float3(1, 1, 1);
            for (int i = 0; i < count; i++)
            {
                //Debug.Log(pool.Count);
                Entity blood;
                if (pool.Count > 0)
                {
                    blood = pool.Dequeue();
                    //Debug.Log(pool.Count);
                }
                else
                {
                    blood = state.EntityManager.Instantiate(ParticlePrefab);
                }

                //var color = Random.ColorHSV(-.05f, .05f, .75f, 1f, .3f, .8f); 
                state.EntityManager.SetComponentEnabled<IsInPoolCB>(blood, false);
                state.EntityManager.SetComponentEnabled<ParticleLifetimeComCB>(blood, true);
                state.EntityManager.SetComponentEnabled<ParticleStuckComCB>(blood, false);
                state.EntityManager.SetComponentData(blood, new ParticleLifetimeComCB { lifetime = 1f });
                state.EntityManager.SetComponentData(blood, new ParticlePositionComCB { position = position });
                state.EntityManager.SetComponentData(blood, new ParticleVelocityComCB { velocity = velocity + Random.ValueRW.random.NextFloat3(-1,1) * velocityJitter });
                state.EntityManager.SetComponentData(blood, new ParticleLifeDurationComCB { duration = Random.ValueRW.random.NextFloat(3f, 5f) });
                state.EntityManager.SetComponentData(blood, new ParticleSizeComCB { size = one * Random.ValueRW.random.NextFloat(.1f, .2f) });
                state.EntityManager.SetComponentData(blood, new URPMaterialPropertyBaseColor { Value = new float4(color.x, color.y, color.z, 1) });
                state.EntityManager.SetComponentData(blood, new LocalTransform { Position = new float3(0, -40, 0), Rotation = quaternion.identity, Scale = 1f });

            }
        }
    }



    [WithDisabled(typeof(BeeDead))]
    [WithDisabled(typeof(IsInPoolCB))]
    [BurstCompile]
    partial struct AliveBeeVelModifyJob : IJobEntity
    {
        public float OneMinusDamping; //should stands for 1f - damping;
        public float flightJitter;
        public float deltaTime;
        public float teamAttraction;
        public float teamRepulsion;
        public int teamOneLen;
        public int teamZeroLen;
        [NativeDisableUnsafePtrRestriction] public RefRW<RandomSingletonCB> random;
        [ReadOnly]
        public NativeArray<Entity> teamOneEntityArray;
        [ReadOnly]
        public NativeArray<Entity> teamZeroEntityArray;
        [ReadOnly]
        public ComponentLookup<BeePosition> beePositionLookup;
        public void Execute(Entity entity,ref BeeVelocity beeVelCom, in TeamCB teamCom)
        {

            var beePos = beePositionLookup[entity].position;
            var vel = beeVelCom.velocity;
            vel += new float3(random.ValueRW.random.NextFloat(-1, 1), random.ValueRW.random.NextFloat(-1, 1), random.ValueRW.random.NextFloat(-1, 1)) * (flightJitter * deltaTime);
            vel *= 0.98f;
            float3 attractiveFriendPos;
            float3 repellentFridendPos;
            if (teamCom.teamID == 0)
            {
                if (teamZeroLen > 0)
                {
                    attractiveFriendPos = beePositionLookup[teamZeroEntityArray[random.ValueRW.random.NextInt(0, teamZeroLen)]].position;
                    repellentFridendPos = beePositionLookup[teamZeroEntityArray[random.ValueRW.random.NextInt(0, teamZeroLen)]].position;
                    var delta = attractiveFriendPos - beePos;
                    float dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                    if(dist>0)vel += delta * (teamAttraction * deltaTime / dist);
                    
                    delta = repellentFridendPos - beePos;
                    dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                    if(dist>0)vel -= delta * (teamRepulsion * deltaTime / dist);
                }
            }
            else
            {
                if (teamOneLen > 0)
                {
                    attractiveFriendPos = beePositionLookup[teamOneEntityArray[random.ValueRW.random.NextInt(0, teamOneLen)]].position;
                    repellentFridendPos = beePositionLookup[teamOneEntityArray[random.ValueRW.random.NextInt(0, teamOneLen)]].position;
                    var delta = attractiveFriendPos - beePos;
                    float dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                    if (dist > 0) vel += delta * (teamAttraction * deltaTime / dist);

                    delta = repellentFridendPos - beePos;
                    dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                    if (dist > 0) vel -= delta * (teamRepulsion * deltaTime / dist);
                }
            }

            beeVelCom.velocity = vel;
        }
    }


    [WithDisabled(typeof(IsInPoolCB))]
    [WithDisabled(typeof(BeeDead))]
    [WithDisabled(typeof(BeeResourceTarget))]
    [WithDisabled(typeof(BeeEnemyTarget))]
    [BurstCompile]
    partial struct BeeNoTargetJob : IJobEntity
    {
        [NativeDisableUnsafePtrRestriction]
        public RefRW<RandomSingletonCB> random;
        [ReadOnly]
        public NativeArray<Entity> teamOneArray;
        [ReadOnly]
        public NativeArray<Entity> teamZeroArray;
        [ReadOnly]
        public NativeArray<Entity> resourceArray;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<BeeResourceTarget> resTarLookup;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<BeeEnemyTarget> enemyTarLookup;
 
        public float aggression;
        public void Execute(Entity entity, in TeamCB teamCom)
        {
            if(random.ValueRW.random.NextFloat(0,1) < aggression)
            {
                //get a random enemy
                if(teamCom.teamID == 1)
                {
                    int len = teamZeroArray.Length;
                    if(len > 0)
                    {
                        enemyTarLookup[entity] = new BeeEnemyTarget { enemy = teamZeroArray[random.ValueRW.random.NextInt(0,len)] }; //len exclusive
                        enemyTarLookup.SetComponentEnabled(entity, true);
                    }
                    
                }
                else
                {
                    int len = teamOneArray.Length;
                    if (len > 0)
                    {
                        enemyTarLookup[entity] = new BeeEnemyTarget { enemy = teamOneArray[random.ValueRW.random.NextInt(0, len)] }; //len exclusive
                        enemyTarLookup.SetComponentEnabled(entity, true);
                    }
                }
            }
            else
            {
                //get a random resource
                int len = resourceArray.Length;
                if(len > 0)
                {
                    resTarLookup[entity] = new BeeResourceTarget { resource = resourceArray[random.ValueRW.random.NextInt(0, len)] };
                    resTarLookup.SetComponentEnabled (entity, true);
                }
            }
        }
    }

    [WithDisabled(typeof(IsInPoolCB))]
    [WithDisabled(typeof(BeeDead))]
    [WithAll(typeof(BeeResourceTarget))]
    [WithDisabled(typeof(BeeStateHoldingResource))]
    [BurstCompile]
    partial struct BeesWithResTargetJob : IJobEntity 
    {
        [NativeDisableParallelForRestriction]
        [ReadOnly]
        public ComponentLookup<LocalTransform> localTransformLookup;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<ResIsCarriedCB> resIsCarriedLookup;
        [NativeDisableParallelForRestriction]
        [ReadOnly]
        public ComponentLookup<ResGridCoordinateCB> resGridCoordinateLookup;

        //public EntityCommandBuffer.ParallelWriter ecb;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<ResWaitToCheck> waitToCheckLookup;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<ResStackCB> resStackLookup;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<BeeResourceTarget> beeResTargetLookup;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<BeeEnemyTarget> beeEnemyTarLookup;
        [NativeDisableParallelForRestriction]
        public DynamicBuffer<StackHeightCOMCB> stackHeightsLookup;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<BeeStateHoldingResource> stateHoldingLookup;


        [ReadOnly]
        public EntityStorageInfoLookup entityStorageInfoLookup;


        public float grabDistanceSqr;
        public float chaseForce;
        public float deltaTime;
        public int GridXCount;
        public void Execute(Entity entity, ref BeeVelocity velocityCom, in TeamCB teamCom, in BeePosition beePositionCom) 
        {
            var res = beeResTargetLookup[entity].resource;
            if(entityStorageInfoLookup.Exists(res)== false)
            {
                beeResTargetLookup.SetComponentEnabled(entity, false);
                return;
            }
            if(res == Entity.Null)
            {
                beeResTargetLookup.SetComponentEnabled(entity, false);
                return;
            }
            
            //var res = resTargetCom.resource;
            if (resIsCarriedLookup.IsComponentEnabled(res)) // held by someone
            {
                var resIsCarriedCom = resIsCarriedLookup[res];
                if (resIsCarriedCom.holderBeeTeamId == teamCom.teamID)
                {
                    //ecb.SetComponentEnabled<BeeResourceTarget>(0,entity, false);
                    beeResTargetLookup.SetComponentEnabled(entity, false);
                }
                else
                {
                    beeEnemyTarLookup[entity] = new BeeEnemyTarget { enemy = resIsCarriedCom.Bee };
                    beeEnemyTarLookup.SetComponentEnabled(entity, true);
                    //ecb.SetComponentEnabled<BeeEnemyTarget>(0,entity, true);
                }
            }
            else
            {
                //try figure out if is topOfStack
                var gridCoor = resGridCoordinateLookup[res];
                var idx = gridCoor.gridX + gridCoor.gridY * GridXCount;
                var stackHeight = stackHeightsLookup[idx];

                var resStackCom = resStackLookup[res];
                if (resStackCom.stacked && resStackCom.stackIndex != stackHeight.stackHeight - 1)//not top of stack
                {
                    beeResTargetLookup.SetComponentEnabled(entity, false);
                    //ecb.SetComponentEnabled<BeeResourceTarget>(0, entity, false);
                }
                else
                {
                    var targetPos = localTransformLookup[res].Position;
                    var delta = targetPos - beePositionCom.position;
                    var squaredDist = delta.x*delta.x + delta.y*delta.y + delta.z * delta.z;
                    if(squaredDist > grabDistanceSqr)
                    {
                        velocityCom.velocity += delta * (chaseForce * deltaTime / math.sqrt(squaredDist));
                    }
                    else
                    {
                        //GrabResource
                        //update bee
                        //ecb.SetComponentEnabled<BeeStateHoldingResource>(0, entity, true);
                        stateHoldingLookup.SetComponentEnabled(entity, true);
                        //update stackHeight
                        stackHeight.stackHeight -= 1;
                        stackHeightsLookup[idx] = stackHeight;
                        //update resource
                        resStackLookup[res] = new ResStackCB { stacked = false, stackIndex = 0 };
                        resIsCarriedLookup[res] = new ResIsCarriedCB { Bee = entity, holderBeeTeamId = teamCom.teamID };
                        resIsCarriedLookup.SetComponentEnabled(res, true);
                        //ecb.SetComponentEnabled<ResStackCB>(0, res, false);
                        resStackLookup.SetComponentEnabled(res, false);
                        //ecb.SetComponentEnabled<ResWaitToCheck>(0, res, true);
                        waitToCheckLookup.SetComponentEnabled(res, true);
                    }
                }
            }
            
            
            
                //ecb.SetComponentEnabled<BeeResourceTarget>(0, entity, false);
            
        }
/*        int GridPosToIdx(int gridX, int gridY)
        {
            return gridY * GridXCount + gridX;
        }*/

    }

    [WithDisabled(typeof(IsInPoolCB))]
    [WithDisabled(typeof(BeeDead))]
    [WithAll(typeof(BeeStateHoldingResource))]
    [BurstCompile]
    partial struct BeesHoldingResJob : IJobEntity   //resource.holder = null  means  ResIsCarried disabled
    {                                               //bee.resourceTarget = null  means  BeeResourceTarget disabled & stateHolding disabled
        public float fieldSizeX;
        public float deltaTime;
        public float carryForce;
        public EntityCommandBuffer.ParallelWriter ecb;
/*        [NativeDisableParallelForRestriction]
        public ComponentLookup<ResIsCarriedCB> resIsCarriedLookup;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<BeeStateHoldingResource> stateHoldingLookup;*/
        //[NativeDisableParallelForRestriction]
        //public ComponentLookup<BeeResourceTarget> resourceTargetLookup;

        public void Execute(Entity entity, ref BeeVelocity velocityCom, in BeePosition positionCom, in TeamCB teamCom, in BeeResourceTarget resTarget)
        {
            float3 targetPos = new float3(-fieldSizeX * .45f + fieldSizeX * .9f * teamCom.teamID, 0f, positionCom.position.z);
            var delta = targetPos - positionCom.position;
            var dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
            velocityCom.velocity += (targetPos - positionCom.position) * (carryForce * deltaTime / dist);
            if(dist < 1f)
            {
                //resIsCarriedLookup.SetComponentEnabled(resTarget.resource, false);
                //stateHoldingLookup.SetComponentEnabled(entity, false);
                //resourceTargetLookup.SetComponentEnabled(entity, false);
                ecb.SetComponentEnabled<ResIsCarriedCB>(0, resTarget.resource, false);
                ecb.SetComponentEnabled<BeeStateHoldingResource>(0, entity, false);
                ecb.SetComponentEnabled<BeeResourceTarget>(0,entity, false);
            }
        } 
    }

    

}