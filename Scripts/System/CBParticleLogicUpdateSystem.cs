using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace CombatBee
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [DisableAutoCreation]
    public partial struct CBParticleLogicUpdateSystem : ISystem, ISystemStartStop
    {
        float gravity;
        float3 fieldSize;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConfigCOMCB>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            var fieldConfig = SystemAPI.GetSingleton<FieldCOM>();
            gravity = fieldConfig.gravity;
            fieldSize = fieldConfig.size;
        }
        public void OnStopRunning(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var stuckLookup = SystemAPI.GetComponentLookup<ParticleStuckComCB>();
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new ParticleLogicUpdateJob
            {
                deltaTime = SystemAPI.Time.DeltaTime,
                gravity = gravity,
                fieldSize = fieldSize,
                ecb = ecb.AsParallelWriter(),
                stuckLookup = stuckLookup
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

        }
    }


    [WithDisabled(typeof(IsInPoolCB))]
    [BurstCompile]
    public partial struct ParticleLogicUpdateJob : IJobEntity
    {
        
        public float deltaTime;
        public float gravity;
        public float3 fieldSize;
        public EntityCommandBuffer.ParallelWriter ecb;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<ParticleStuckComCB> stuckLookup;

        public void Execute(Entity entity, [ChunkIndexInQuery] int sortkey,ref ParticlePositionComCB positionCom, ref ParticleSizeComCB sizeCom, 
            ref ParticleVelocityComCB velocityCom, ref ParticleLifetimeComCB lifetimeCom, ref URPMaterialPropertyBaseColor color, in ParticleLifeDurationComCB durationCom)
        {
            lifetimeCom.lifetime -= deltaTime / durationCom.duration;
            if(lifetimeCom.lifetime < 0f)
            {
                ecb.SetComponentEnabled<ParticleLifetimeComCB>(sortkey, entity, false);
            }
            if (stuckLookup.IsComponentEnabled(entity) == false)
            {
                var newVelocity = velocityCom.velocity;
                var newPosition = positionCom.position;
                var newSize = sizeCom.size;
                newVelocity.y += gravity * deltaTime;
                newPosition += newVelocity * deltaTime;

                if(math.abs(newPosition.x) > fieldSize.x * .5f)
                {
                    newPosition.x = fieldSize.x * .5f * math.sign(newPosition.x);
                    float splat = math.abs(newVelocity.x * .3f) + 1f;
                    newSize.y *= splat;
                    newSize.z *= splat;
                    stuckLookup.SetComponentEnabled(entity, true);
                }
                if (math.abs(newPosition.y) > fieldSize.y * .5f)
                {
                    newPosition.y = fieldSize.y * .5f * math.sign(newPosition.y);
                    float splat = math.abs(newVelocity.y * .3f) + 1f;
                    newSize.x *= splat;
                    newSize.z *= splat;
                    stuckLookup.SetComponentEnabled(entity, true);
                }
                if (math.abs(newPosition.z) > fieldSize.z * .5f)
                {
                    newPosition.z = fieldSize.z * .5f * math.sign(newPosition.z);
                    float splat = math.abs(newVelocity.z * .3f) + 1f;
                    newSize.y *= splat;
                    newSize.x *= splat;
                    stuckLookup.SetComponentEnabled(entity, true);
                }
                sizeCom.size = newSize;
                velocityCom.velocity = newVelocity;
                positionCom.position = newPosition;
            }
            //color.Value.w = lifetimeCom.lifetime;

        }
    }
}