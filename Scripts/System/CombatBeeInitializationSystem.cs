using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace CombatBee
{
    [UpdateInGroup(typeof(CombatBeeSystemGroup))]
    public partial struct CombatBeeInitializationSystem : ISystem, ISystemStartStop
    {
        float2 minGridPos;
        RandomSingletonCB randomCom;
        float fieldXSize;
        float fieldZSize;
        Entity ResourcePrefab;
        Entity BeePrefab;
        int startResourceCount;
        float3 colorZero;
        float3 colorOne;
        int startBeeCount;
        float resourceSize;
        float maxBeeSize;
        float minBeeSize;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConfigCOMCB>();
        }
        //[BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            state.Enabled = false;
            randomCom = SystemAPI.GetSingleton<RandomSingletonCB>();
            var config = SystemAPI.GetSingleton<ConfigCOMCB>();
            var fieldCom = SystemAPI.GetSingleton<FieldCOM>();
            ResourcePrefab = config.ResourcePrefab;
            BeePrefab = config.BeePrefab;
            startResourceCount = config.initialResourceCount;
            startBeeCount = config.initialBeeCount;
            resourceSize = config.resourceSize;
            maxBeeSize = config.maxBeeSize;
            minBeeSize = config.minBeeSize;
            colorZero = config.teamColorZero;
            colorOne = config.teamColorOne;
            //CombatBeeSpawnerClass.Init(BeePrefab, config.ParticlePrefab, minBeeSize,maxBeeSize,colorZero, colorOne);

            /*// initializing grid info and stack heights
            var entityForGridInfo = state.EntityManager.CreateEntity();
            var entityForStackHeight = state.EntityManager.CreateEntity();
            fieldXSize = fieldCom.size.x;
            fieldZSize = fieldCom.size.z;
            var xCount = (int)(fieldXSize / config.resourceSize);
            var yCount = (int)(fieldZSize / config.resourceSize);
            var xSize = fieldXSize / xCount;
            var ySize = fieldZSize / yCount;
            minGridPos = new float2((xCount - 1f) * -.5f * xSize, (yCount - 1f) * -.5f * ySize);
            var gridInfo = new GridInfoCOMCB
            {
                GridXCount = xCount,
                GridYCount = yCount,
                CellXSize = xSize,
                CellYSize = ySize,
                minGridPos = minGridPos,
            };
            GridSimpleFunction.Init(xCount, yCount, xSize, ySize, minGridPos, fieldCom.size.y, resourceSize);


            state.EntityManager.AddComponentData(entityForGridInfo, gridInfo);
            var stackHeightsBuffer = state.EntityManager.AddBuffer<StackHeightCOMCB>(entityForStackHeight);
            stackHeightsBuffer.Length = xCount * yCount;
            for(int i = 0; i < stackHeightsBuffer.Length; i++)
            {
                stackHeightsBuffer[i] = new StackHeightCOMCB { stackHeight = 0 };
            }
            //下面spawn resource的逻辑可能要放到Update里面，防止因为AddComponentData造成莫名错误
            //spawn resource 
            SpawnResource(startResourceCount, ref state);
            //spawn bee
            SpawnBeeAtInitialization(startBeeCount / 2, 0, colorOne, ref state);
            SpawnBeeAtInitialization(startBeeCount / 2, 1, colorTwo, ref state);
            CombatBeeSpawnerClass.SpawnBee(ref state, 1, 1);*/
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            // initializing grid info and stack heights
            var entityForGridInfo = state.EntityManager.CreateEntity();
            var entityForStackHeight = state.EntityManager.CreateEntity();
            var config = SystemAPI.GetSingleton<ConfigCOMCB>();
            var fieldCom = SystemAPI.GetSingleton<FieldCOM>();
            fieldXSize = fieldCom.size.x;
            fieldZSize = fieldCom.size.z;
            var xCount = (int)(fieldXSize / config.resourceSize);
            var yCount = (int)(fieldZSize / config.resourceSize);
            var xSize = fieldXSize / xCount;
            var ySize = fieldZSize / yCount;
            minGridPos = new float2((xCount - 1f) * -.5f * xSize, (yCount - 1f) * -.5f * ySize);
/*            var gridInfo = new GridInfoCOMCB
            {
                GridXCount = xCount,
                GridYCount = yCount,
                CellXSize = xSize,
                CellYSize = ySize,
                minGridPos = minGridPos,
            };*/
            //GridSimpleFunction.Init(xCount, yCount, xSize, ySize, minGridPos.x, minGridPos.y, fieldCom.size.y, resourceSize);


            //state.EntityManager.AddComponentData(entityForGridInfo, gridInfo);
            var stackHeightsBuffer = state.EntityManager.AddBuffer<StackHeightCOMCB>(entityForStackHeight);
            stackHeightsBuffer.Length = xCount * yCount;
            for (int i = 0; i < stackHeightsBuffer.Length; i++)
            {
                stackHeightsBuffer[i] = new StackHeightCOMCB { stackHeight = 0 };
            }
            //下面spawn resource的逻辑可能要放到Update里面，防止因为AddComponentData造成莫名错误
            //spawn resource 
            SpawnResource(startResourceCount, ref state);
            //spawn bee
            SpawnBeeAtInitialization(startBeeCount / 2, 0, colorZero, ref state);
            SpawnBeeAtInitialization(startBeeCount / 2, 1, colorOne, ref state);
            //CombatBeeSpawnerClass.SpawnBee(ref state, 1, 1);
            //var tmpQueue = new NativeQueue<Entity>(Allocator.Temp);
            //CombatBeeSpawnerClass.SpawnBlood(ref state, ref tmpQueue, new float3(8, 8, 8), new float3(1, 1, 1));

            var entityForBeePool = state.EntityManager.CreateEntity();
            var entityForParticlePool = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entityForBeePool, new BeePool { pool = new NativeQueue<Entity>(Allocator.Persistent) });
            state.EntityManager.AddComponentData(entityForParticlePool, new ParticlePool { pool = new NativeQueue<Entity>(Allocator.Persistent) });
        }
        public void OnStopRunning(ref SystemState state)
        {
        }

        public void SpawnResource(int amount, ref SystemState state)
        {
            var resources = state.EntityManager.Instantiate(ResourcePrefab, amount, Allocator.Temp);
            float height = randomCom.random.NextFloat(1, 3);
            for (int i = 0; i < amount; i++)
            {
                float3 pos = new float3(minGridPos.x * .25f + randomCom.random.NextFloat(0, 1) * fieldXSize * .25f, height, minGridPos.y + randomCom.random.NextFloat(0, 1) * fieldZSize);
                state.EntityManager.SetComponentData<LocalTransform>(resources[i], new LocalTransform { Position = pos , Rotation = quaternion.identity, Scale = resourceSize});
            }
        }


        private void SpawnBeeAtInitialization(int amount, int teamID, float3 color, ref SystemState state)
        { 
            var bees = state.EntityManager.Instantiate(BeePrefab, amount, Allocator.Temp);
            float3 pos = new float3(1, 0, 0) * (-fieldXSize * .4f + fieldXSize * .8f * teamID);
            for (int i = 0; i < amount; i++)
            {
                state.EntityManager.SetComponentEnabled<BeeDead>(bees[i], false);
                state.EntityManager.SetComponentData(bees[i], new BeePosition { position = pos });
                state.EntityManager.SetComponentData(bees[i], new LocalTransform { Position = pos, Rotation = quaternion.identity, Scale = randomCom.random.NextFloat(minBeeSize, maxBeeSize) });
                state.EntityManager.SetSharedComponent(bees[i],new TeamCB { teamID = teamID });
                state.EntityManager.SetComponentData(bees[i], new URPMaterialPropertyBaseColor { Value = new float4(color, 1) });
            }
        }
    }
}