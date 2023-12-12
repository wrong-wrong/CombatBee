using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace CombatBee
{
    [UpdateInGroup(typeof(CombatBeeSystemGroup))]
    [UpdateAfter(typeof(CBPositionUpdateSystem))]
    public partial struct CBTransformUpdateForRenderingSystem : ISystem,ISystemStartStop
    {
        float speedStretch;
        float3 up;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConfigCOMCB>();
        }
        public void OnStartRunning(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<ConfigCOMCB>();
            speedStretch = config.speedStretch;
            up = new float3(0,1,0);
        }
        public void OnStopRunning(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var isBloodLookup = SystemAPI.GetComponentLookup<ParticleIsBloodComCB>(false);
            state.Dependency = new ParticleTransformUpdateJob
            {
                up = up,
                SpeedStretch = speedStretch,
                isBloodLookup = isBloodLookup,
                one = new float3(1,1,1)
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new BeeTransformUpdateJob { }.ScheduleParallel(state.Dependency);    
        }
    }


    //[WithDisabled(typeof(ParticleStuckComCB))]
    [BurstCompile]
    public partial struct ParticleTransformUpdateJob : IJobEntity
    {
        public float SpeedStretch;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<ParticleIsBloodComCB> isBloodLookup;
        public float3 up;
        public float3 one;
        public void Execute(Entity entity, ref LocalToWorld localToWorld, in ParticlePositionComCB positionCom
            ,in ParticleVelocityComCB velocityCom, in ParticleSizeComCB sizeCom, in ParticleLifetimeComCB lifeCom) 
        {
            var tarQuaternion = quaternion.identity;
            var scale = sizeCom.size;
            if (isBloodLookup.IsComponentEnabled(entity))
            {
                float3 vel = velocityCom.velocity;
                tarQuaternion = quaternion.LookRotation(vel,up);
                scale.z *= 1f + math.sqrt(vel.x*vel.x + vel.y * vel.y + vel.z * vel.z) * SpeedStretch;
            }   
            scale*= lifeCom.lifetime;
            localToWorld.Value = float4x4.TRS(positionCom.position, tarQuaternion, scale);
                
        }
    }

    [WithDisabled(typeof(IsInPoolCB))]
    [BurstCompile]
    public partial struct BeeTransformUpdateJob : IJobEntity
    {
        public void Execute(ref LocalTransform localTransform, in BeePosition beePositon)
        {
            localTransform.Position = beePositon.position;
        }
    }
}