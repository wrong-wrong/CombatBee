using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CombatBee
{
    [UpdateInGroup(typeof(CombatBeeSystemGroup))]
    [UpdateAfter(typeof(CBTransformUpdateForRenderingSystem))]
    public partial struct CBPoolRecycleSystem : ISystem, ISystemStartStop
    {
        public float3 PoolPosition;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConfigCOMCB>();
        }
        public void OnStartRunning(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<ConfigCOMCB>();
            PoolPosition = config.PoolPosition;
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var particlePool = SystemAPI.GetSingletonRW<ParticlePool>();
            var beePool = SystemAPI.GetSingletonRW<BeePool>();
            foreach(var (localToWorld, entity)in SystemAPI.Query<RefRW<LocalToWorld>>()
                .WithDisabled<IsInPoolCB>()
                .WithDisabled<ParticleLifetimeComCB>()
                .WithEntityAccess())
            {
                particlePool.ValueRW.pool.Enqueue(entity);
                localToWorld.ValueRW.Value = float4x4.Translate(PoolPosition);
                state.EntityManager.SetComponentEnabled<IsInPoolCB>(entity,true);
            }

            foreach(var (localTransform, entity) in SystemAPI.Query<RefRW<LocalTransform>>() // bees who enabled isInPool & disabled Beedead   ,should be changed into enabled BeeDead
                .WithDisabled<IsInPoolCB>()
                .WithAll<ShouldBeRecycledToPoolCB>()
                .WithEntityAccess())
            {
                localTransform.ValueRW.Position = PoolPosition;
                beePool.ValueRW.pool.Enqueue(entity);
                //state.EntityManager.SetComponentEnabled<BeeDead>(entity, true);
                state.EntityManager.SetComponentEnabled<IsInPoolCB>(entity, true);
                state.EntityManager.SetComponentEnabled<ShouldBeRecycledToPoolCB>(entity, false);
                state.EntityManager.SetComponentEnabled<BeeEnemyTarget>(entity, false);
                state.EntityManager.SetComponentEnabled<BeeResourceTarget>(entity, false);
                state.EntityManager.SetComponentEnabled<BeeStateAttacking>(entity, false);
                state.EntityManager.SetComponentEnabled<BeeStateHoldingResource>(entity, false);
            }
        }
        public void OnStopRunning(ref SystemState state)
        {

        }
    }


}