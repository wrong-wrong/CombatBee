using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor.Build.Content;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace CombatBee
{
    [UpdateInGroup(typeof(CombatBeeSystemGroup))]
    [UpdateAfter(typeof(CombatBeeInitializationSystem))]
    //position update should be later than the speed update
    //[UpdateAfter(typeof(CBVelocityUpdateSystem))]
    public partial struct CBPositionUpdateSystem : ISystem, ISystemStartStop
    {
        Entity BeePrefab;
        Entity ParticlePrefab;
        float ResourceSize;
        float carryStiffness;
        float snapStiffness;
        float gravity;
        float rotationStiffness;
        float3 fieldSize;
        int beePerResource;
        float minBeeSize;
        float maxBeeSize;
        float3 teamZeroColor;
        float3 teamOneColor;
        float3 spawnEffectColor;
        int GridXCount;
        int GridYCount;
        float CellXSize;
        float CellYSize;
        float MinGridPosX;
        float MinGridPosY;
        float FieldYSize;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConfigCOMCB>();
        }
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<ConfigCOMCB>();
            BeePrefab = config.BeePrefab;
            beePerResource = config.beesPerResource;
            ResourceSize = config.resourceSize;
            carryStiffness = config.carryStiffness;
            snapStiffness = config.snapStiffness;
            minBeeSize = config.minBeeSize;
            maxBeeSize = config.maxBeeSize;
            teamOneColor = config.teamColorOne;
            teamZeroColor = config.teamColorZero;
            ParticlePrefab = config.ParticlePrefab;
            spawnEffectColor = config.spawnEffectColor;
            var fieldCom = SystemAPI.GetSingleton<FieldCOM>();
            gravity = fieldCom.gravity;
            fieldSize = fieldCom.size; 
            var fieldXSize = fieldCom.size.x;
            var fieldZSize = fieldCom.size.z;
            GridXCount = (int)(fieldXSize / config.resourceSize);
            GridYCount = (int)(fieldZSize / config.resourceSize);
            CellXSize = fieldXSize / GridXCount;
            CellYSize = fieldZSize / GridYCount;
            MinGridPosX = (GridXCount - 1f) * -.5f * CellXSize;
            MinGridPosY = (GridYCount - 1f) * -.5f * CellYSize;
            FieldYSize = fieldSize.y;
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            var stateAttackingLookup = SystemAPI.GetComponentLookup<BeeStateAttacking>(true);
            var stateResourceHoldingLookup = SystemAPI.GetComponentLookup<BeeStateHoldingResource>(true);
            state.Dependency = new BeePositionUpdateAndVelocityJob
            {
                deltaTime = SystemAPI.Time.DeltaTime,
                resourceSize = ResourceSize,
                fieldSize = fieldSize,
                rotationStiffness = rotationStiffness,
                stateAttackingLookup = stateAttackingLookup,
                stateHoldingResourceLookup = stateResourceHoldingLookup,
            }.ScheduleParallel(state.Dependency);

            var beePosLookup = SystemAPI.GetComponentLookup<BeePosition>(true);
            var beeVelLookup = SystemAPI.GetComponentLookup<BeeVelocity>(true);
            var beeDeadLookup = SystemAPI.GetComponentLookup<BeeDead>(true);
            var resIsCarriedLookup = SystemAPI.GetComponentLookup<ResIsCarriedCB>(false);
            state.Dependency = new ResourceCarriedByBeeJob
            {
                beeDeadLookUp = beeDeadLookup,
                resIsCarriedLookup = resIsCarriedLookup,
                resourceSize = ResourceSize,
                carryStiffness = carryStiffness,
                deltaTime = deltaTime,
                beePositionLookup = beePosLookup,
                beeVelocityLookUp = beeVelLookup,
            }.ScheduleParallel(state.Dependency);
            
            var stackHeightBuffer = SystemAPI.GetSingletonBuffer<StackHeightCOMCB>(true);
            var resStackLookup= SystemAPI.GetComponentLookup<ResStackCB>();
            var resFallingJobHandle = new ResourceFallingJob
            {
                stackHeights = stackHeightBuffer,
                resStackLookup = resStackLookup,
                deltaTime = deltaTime,
                snapStiffness = snapStiffness,
                gravity = gravity,
                fieldSize = fieldSize,
                CellXSize = CellXSize,
                CellYSize = CellYSize,
                GridXCount = GridXCount,
                GridYCount = GridYCount,
                FieldYSize = FieldYSize,
                MinGridPosX = MinGridPosX,
                MinGridPosY = MinGridPosY,
                ResourceSize = ResourceSize,
            }.ScheduleParallel(state.Dependency);
            resFallingJobHandle.Complete();

            //after the position is updated, the landing logic of resource, should be handled in this OnUpdate, either spawning bees or update stackHeights
            //should query for entities with enabled ResWaitToCheck, && disabled isMoving & disabled resCarried
            var random = SystemAPI.GetSingletonRW<RandomSingletonCB>();
            stackHeightBuffer = SystemAPI.GetSingletonBuffer<StackHeightCOMCB>(false);
            var beePool = SystemAPI.GetSingletonRW<BeePool>();
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            var particlePool = SystemAPI.GetSingletonRW<ParticlePool>();
            foreach (var (resStackCom, localTransform, gridCoor,entity) in SystemAPI.Query<RefRW<ResStackCB>, RefRO<LocalTransform>, RefRO<ResGridCoordinateCB>>()
                .WithDisabled<ResIsCarriedCB>()
                .WithAll<ResWaitToCheck>()
                .WithEntityAccess())
            {
                state.EntityManager.SetComponentEnabled<ResWaitToCheck>(entity, false);
                //decide to spawn or stack
                var pos = localTransform.ValueRO.Position;
                if (math.abs(pos.x) > fieldSize.x * .4f)//spawn
                {
                    int team = 0;
                    if (pos.x > 0f)
                    {
                        team = 1;
                    }
                    //CombatBeeSpawnerClass.SpawnBee(ref state, ref beePool.ValueRW.pool, ref ecb ,beePerResource, team, localTransform.ValueRO.Position);
                    SpawnBee(ref state, BeePrefab,beePool, random, ref ecb, beePerResource, team, localTransform.ValueRO.Position);
                    SpawnSpawnEffect(ref state, spawnEffectColor, ParticlePrefab, particlePool, random, pos, float3.zero, 5);
/*                    for (int i = 0; i < beePerResource; i++)
                    {
                        Entity bee;
                        if (beePool.ValueRW.pool.Count > 0)
                        {
                            bee = beePool.ValueRW.pool.Dequeue();
                            //Debug.Log(beePool.ValueRW.pool.Count);
                        }
                        else
                        {
                            bee = ecb.Instantiate(BeePrefab);
                        }

                        ecb.SetComponent(bee, new LocalTransform { Position = pos, Rotation = quaternion.identity, Scale = random.ValueRW.random.NextFloat(minBeeSize, maxBeeSize) });
                        ecb.SetSharedComponent(bee, new TeamCB { teamID = team });
                        ecb.SetComponent(bee, new BeeDead { deathTimer = 1f });

                        if (team == 0)
                        {
                            //set color
                            ecb.SetComponent(bee, new MaterialColor { Value = new float4(teamZeroColor, 1f) });
                        }
                        else
                        {
                            ecb.SetComponent(bee, new MaterialColor { Value = new float4(teamOneColor, 1f) });
                        }
                        ecb.SetComponent(bee, new BeePosition { position = pos });
                        //set pos
                        ecb.SetComponentEnabled<IsInPoolCB>(bee, false);
                        ecb.SetComponentEnabled<BeeDead>(bee, false);
                        //state.EntityManager.SetComponentData(bees[i], new LocalTransform { Position = pos, Rotation = quaternion.identity, Scale = 3});
                        //state.EntityManager.SetSharedComponent(bees[i], new TeamCB { teamID = teamID });
                    }*/
                    ecb.DestroyEntity(entity);
                }
                else//stack
                {
                    resStackCom.ValueRW.stacked = true;
                    int idx = GridPosToIdx(gridCoor.ValueRO.gridX, gridCoor.ValueRO.gridY);
                    var stackHeightCom = stackHeightBuffer[idx];
                    resStackCom.ValueRW.stackIndex = stackHeightCom.stackHeight;
                    stackHeightCom.stackHeight += 1;
                    stackHeightBuffer[idx] = stackHeightCom;
                }
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public void OnStopRunning(ref SystemState state)
        {

        }
        [BurstCompile]
        void SpawnBee(ref SystemState state,Entity BeePrefab, RefRW<BeePool> beePool, RefRW<RandomSingletonCB> Random, ref EntityCommandBuffer ecb, int amount, int teamID, float3 pos)
        {
            var pool = beePool.ValueRW.pool;
            for (int i = 0; i < amount; i++)
            {
                Entity bee;

                if (pool.Count > 0)
                {
                    bee = pool.Dequeue();
                    //Debug.Log(pool.Count);
                }
                else
                {
                    bee = state.EntityManager.Instantiate(BeePrefab);
                    //bee = ecb.Instantiate(BeePrefab);
                }
                state.EntityManager.SetComponentData(bee, new BeeResourceTarget { resource = Entity.Null });
                state.EntityManager.SetComponentData(bee, new LocalTransform { Position = pos, Rotation = quaternion.identity, Scale = Random.ValueRW.random.NextFloat(minBeeSize, maxBeeSize) });
                state.EntityManager.SetComponentData(bee, new BeeDead { deathTimer = 1f });
                //state.EntityManager.SetSharedComponent(bee, new TeamCB { teamID = teamID });
                //ecb.SetComponent(bee, new LocalTransform { Position = pos, Rotation = quaternion.identity, Scale = Random.Range(minBeeSize, maxBeeSize) });
                //ecb.SetComponent(bee, new BeeDead { deathTimer = 1f });
                ecb.SetSharedComponent(bee, new TeamCB { teamID = teamID });

                if (teamID == 0)
                {
                    state.EntityManager.SetComponentData(bee, new URPMaterialPropertyBaseColor { Value = new float4(teamZeroColor, 1f) });
                    //set color
                    //ecb.SetComponent(bee, new MaterialColor { Value = new float4(teamZeroColor, 1f) });
                }
                else
                {
                    state.EntityManager.SetComponentData(bee, new URPMaterialPropertyBaseColor { Value = new float4(teamOneColor, 1f) });

                    //ecb.SetComponent(bee, new MaterialColor { Value = new float4(teamOneColor, 1f) });  
                }
                state.EntityManager.SetComponentData(bee, new BeePosition { position = pos });
                //ecb.SetComponent(bee, new BeePosition { position = pos });
                //set pos
                state.EntityManager.SetComponentEnabled<IsInPoolCB>(bee, false);
                state.EntityManager.SetComponentEnabled<BeeDead>(bee, false);
                //ecb.SetComponentEnabled<IsInPoolCB>(bee, false);
                //ecb.SetComponentEnabled<BeeDead>(bee, false);

            }
        }

        [BurstCompile]
        void SpawnSpawnEffect(ref SystemState state, float3 color, Entity ParticlePrefab, RefRW<ParticlePool> Pool, RefRW<RandomSingletonCB> Random, float3 position, float3 velocity, int count = 1)
        {
            var pool = Pool.ValueRW.pool;
            float3 one = new float3(1, 1, 1);
            for (int i = 0; i < count; i++)
            {
                //Debug.Log(pool.Count);
                Entity particle;
                if (pool.Count > 0)
                {
                    particle = pool.Dequeue();
                    //Debug.Log(pool.Count);
                }
                else
                {
                    particle = state.EntityManager.Instantiate(ParticlePrefab);
                }

                //var color = Random.ColorHSV(-.05f, .05f, .75f, 1f, .3f, .8f); 
                state.EntityManager.SetComponentEnabled<IsInPoolCB>(particle, false);
                state.EntityManager.SetComponentEnabled<ParticleLifetimeComCB>(particle, true);
                state.EntityManager.SetComponentEnabled<ParticleStuckComCB>(particle, false);
                state.EntityManager.SetComponentEnabled<ParticleIsBloodComCB>(particle, false);
                state.EntityManager.SetComponentData(particle, new ParticleLifetimeComCB { lifetime = 1f });
                state.EntityManager.SetComponentData(particle, new ParticlePositionComCB { position = position });
                state.EntityManager.SetComponentData(particle, new ParticleVelocityComCB { velocity = velocity + Random.ValueRW.random.NextFloat3(-1, 1) * 5f });
                state.EntityManager.SetComponentData(particle, new ParticleLifeDurationComCB { duration = Random.ValueRW.random.NextFloat(.25f,.5f) });
                state.EntityManager.SetComponentData(particle, new ParticleSizeComCB { size = one * Random.ValueRW.random.NextFloat(1f,2f) });
                state.EntityManager.SetComponentData(particle, new URPMaterialPropertyBaseColor { Value = new float4(color.x,color.y,color.z, 1) });
                state.EntityManager.SetComponentData(particle, new LocalTransform { Position = new float3(0, -40, 0), Rotation = quaternion.identity, Scale = 1f });

            }
        }
        [BurstCompile]
        int GridPosToIdx(int gridX, int gridY)
        {
            return gridY * GridXCount + gridX;
        }

        [BurstCompile]
        void GetGridIndex(float3 pos, out int gridX, out int gridY)
        {
            gridX = (int)math.floor((pos.x - MinGridPosX + CellXSize * .5f) / CellXSize);
            gridY = (int)math.floor((pos.z - MinGridPosY + CellYSize * .5f) / CellYSize);

            gridX = math.clamp(gridX, 0, GridXCount - 1);
            gridY = math.clamp(gridY, 0, GridYCount - 1);
        }
        [BurstCompile]
        float GetStackPos(int height)     //differ from the sample, since only the y value of Position is used, we only cal y
        {
            return -FieldYSize * .5f + (height + .5f) * ResourceSize;
        }
        [BurstCompile]
        float3 NearestSnappedPos(float3 pos)
        {
            int x, y;
            GetGridIndex(pos, out x, out y);
            return new float3(MinGridPosX + x * CellXSize, pos.y, MinGridPosY + y * CellYSize);
        }

    }

    [WithDisabled(typeof(IsInPoolCB))]
    //[WithDisabled(typeof(BeeDead))]
    [BurstCompile]
    partial struct BeePositionUpdateAndVelocityJob : IJobEntity
    {
        public float resourceSize;
        public float deltaTime;
        public float rotationStiffness;
        public float3 fieldSize;
        [NativeDisableParallelForRestriction]
        [ReadOnly]
        public ComponentLookup<BeeStateHoldingResource> stateHoldingResourceLookup;
        [NativeDisableParallelForRestriction]
        [ReadOnly]
        public ComponentLookup<BeeStateAttacking> stateAttackingLookup;
        public void Execute(Entity entity, ref BeePosition beePosition, ref BeeVelocity beeVelocity, ref BeeSmoothMovement beeSmoothMovement) 
        {
            var newPosition = beePosition.position;
            var newVelocity = beeVelocity.velocity;
            newPosition += deltaTime * newVelocity;

            if(math.abs(newPosition.x) > fieldSize.x * .5f)
            {
                newPosition.x = (fieldSize.x * .5f) * math.sign(newPosition.x);
                newVelocity.x *= -.5f;
                newVelocity.y *= .8f;
                newVelocity.z *= .8f;
            }
            if (math.abs(newPosition.z) > fieldSize.z * .5f)
            {
                newPosition.z = (fieldSize.z * .5f) * math.sign(newPosition.z);
                newVelocity.z *= -.5f;
                newVelocity.y *= .8f;
                newVelocity.x *= .8f;
            }
            float resourceModifier = 0f;
            if (stateHoldingResourceLookup.IsComponentEnabled(entity))
            {
                resourceModifier = resourceSize;
            }
            if(math.abs(newPosition.y) > fieldSize.y * .5f - resourceModifier)
            {
                newPosition.y = (fieldSize.y * .5f - resourceModifier) * math.sign(newPosition.y);
                newVelocity.y *= -.5f;
                newVelocity.z *= .8f;
                newVelocity.x *= .8f;
            }

            beePosition.position = newPosition;
            beeVelocity.velocity = newVelocity;

            float3 oldSmoothPos = beeSmoothMovement.smoothPosition;
            if(stateAttackingLookup.IsComponentEnabled(entity))
            {
                beeSmoothMovement.smoothPosition = newPosition;
            }
            else
            {
                beeSmoothMovement.smoothPosition = math.lerp(beeSmoothMovement.smoothPosition, newPosition, deltaTime * rotationStiffness);
            }
            beeSmoothMovement.smoothDirection = beeSmoothMovement.smoothPosition - oldSmoothPos;
            
        }
    }

/*    [WithAll(typeof(BeeDead))]
    [BurstCompile]
    partial struct DeadBeePosAndVelUpdateJob : IJobEntity
    {
        public float gravity;
    }*/
    
    [WithAll(typeof(ResIsCarriedCB))]   //resource with isCarries enabled should meaning that there IS bee carrying them
    [BurstCompile]
    partial struct ResourceCarriedByBeeJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        [ReadOnly]
        public ComponentLookup<BeePosition> beePositionLookup;
        [NativeDisableParallelForRestriction]
        [ReadOnly]
        public ComponentLookup<BeeVelocity> beeVelocityLookUp;
        [NativeDisableParallelForRestriction]
        [ReadOnly]
        public ComponentLookup<BeeDead> beeDeadLookUp;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<ResIsCarriedCB> resIsCarriedLookup;
        public float resourceSize;
        public float deltaTime;
        public float carryStiffness;
        public void Execute(Entity entity, ref LocalTransform localTransform, ref ResVelocityCB resVelocityCom)
        {
            var resIsCarriedCom = resIsCarriedLookup[entity];
            if (beeDeadLookUp.IsComponentEnabled(resIsCarriedCom.Bee) == true)
            {
                resIsCarriedLookup.SetComponentEnabled(entity, false);
            }
            var beeLocalTransform = beePositionLookup[resIsCarriedCom.Bee];
            float3 targetPos = beeLocalTransform.position - new float3(0, (resourceSize), 0);
            resVelocityCom.velocity = beeVelocityLookUp[resIsCarriedCom.Bee].velocity;
            localTransform.Position = math.lerp(localTransform.Position, targetPos, carryStiffness * deltaTime);
        }
    }

    [WithDisabled(typeof(ResStackCB))]
    [WithDisabled(typeof(ResIsCarriedCB))]
    [BurstCompile]
    partial struct ResourceFallingJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public DynamicBuffer<StackHeightCOMCB> stackHeights;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<ResStackCB> resStackLookup;
        public float deltaTime;
        public float snapStiffness;
        public float gravity;
        public float3 fieldSize;
        public int GridXCount;
        public int GridYCount;
        public float CellXSize;
        public float CellYSize;
        public float MinGridPosX;
        public float MinGridPosY;
        public float FieldYSize;
        public float ResourceSize;
        public void Execute(Entity entity, ref LocalTransform localTransform, ref ResVelocityCB velocityCom, ref ResGridCoordinateCB gridCoordinateCom)
        {
            var newPosition = math.lerp(localTransform.Position, NearestSnappedPos(localTransform.Position), snapStiffness * deltaTime);
            var newVelocity = velocityCom.velocity ;
            newVelocity.y += gravity * deltaTime;
            newPosition += newVelocity * deltaTime;
            //should be update later

            for(int j = 0; j < 3; ++j)
            {
                if(math.abs(newPosition[j]) > fieldSize[j] * .5f)
                {
                    newPosition[j] = fieldSize[j] * .5f * math.sign(newPosition[j]);
                    newVelocity[j] *= 0f;
                    newVelocity[(j + 1) % 3] *= .8f;
                    newVelocity[(j + 2) % 3] *= .8f;
                }
            }

            GetGridIndex(newPosition, out gridCoordinateCom.gridX, out gridCoordinateCom.gridY);
            //before GetStackPos, need to access stackheights
            // needs to convert gridX, gridY to one dimension index to stackHeights, idx = i * col + j;
            var targetStackHeightCom = stackHeights[GridPosToIdx(gridCoordinateCom.gridX, gridCoordinateCom.gridY)];
            float floorY = GetStackPos(targetStackHeightCom.stackHeight);

            if (newPosition.y < floorY)
            {
                newPosition.y = floorY;
                resStackLookup.SetComponentEnabled(entity, true);
                //other logic is left to the main thread function - OnUpdate 
            }
            localTransform.Position = newPosition;
            velocityCom.velocity = newVelocity;
        }
        int GridPosToIdx(int gridX, int gridY)
        {
            return gridY * GridXCount + gridX;
        }

        //[BurstCompile]
        void GetGridIndex(float3 pos, out int gridX, out int gridY)
        {
            gridX = (int)math.floor((pos.x - MinGridPosX + CellXSize * .5f) / CellXSize);
            gridY = (int)math.floor((pos.z - MinGridPosY + CellYSize * .5f) / CellYSize);

            gridX = math.clamp(gridX, 0, GridXCount - 1);
            gridY = math.clamp(gridY, 0, GridYCount - 1);
        }
        //[BurstCompile]
        float GetStackPos(int height)     //differ from the sample, since only the y value of Position is used, we only cal y
        {
            return -FieldYSize * .5f + (height + .5f) * ResourceSize;
        }
        //[BurstCompile]
        float3 NearestSnappedPos(float3 pos)
        {
            int x, y;
            GetGridIndex(pos, out x, out y);
            return new float3(MinGridPosX + x * CellXSize, pos.y, MinGridPosY + y * CellYSize);
        }
    }
}