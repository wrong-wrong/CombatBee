/*using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;
namespace CombatBee
{
    [BurstCompile]
    public class CombatBeeSpawnerClass
    {
        static Entity BeePrefab;
        static Entity ParticlePrefab;
        static float minBeeSize;
        static float maxBeeSize;
        static float3 teamZeroColor;
        static float3 teamOneColor;
        public static void Init(Entity beePrefab, Entity particlePrefab, float MinBeeSize, float MaxBeeSize, float3 TeamZeroColor, float3 TeamOneColor)
        {
            BeePrefab = beePrefab;
            ParticlePrefab = particlePrefab;
            minBeeSize = MinBeeSize;
            maxBeeSize = MaxBeeSize;
            teamZeroColor = TeamZeroColor;
            teamOneColor = TeamOneColor;
        }
        //
        //[BurstCompile]
        public static void SpawnBee(ref SystemState state, ref NativeQueue<Entity> pool, ref EntityCommandBuffer ecb, int amount, int teamID, float3 pos)
        {
            
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
                var size = Random.Range(minBeeSize, maxBeeSize);
                state.EntityManager.SetComponentData(bee, new BeeSize { size = size });
                state.EntityManager.SetComponentData(bee, new LocalTransform { Position = pos, Rotation = quaternion.identity, Scale = size });
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
        //
        public static void SpawnBlood(ref SystemState state, ref NativeQueue<Entity> pool, float3 position, float3 velocity, float velocityJitter = 6f, int count = 1)
        {
            float3 one = new float3(1,1, 1);
            for(int i = 0;i < count;i++)
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

                var color = Random.ColorHSV(-.05f, .05f, .75f, 1f, .3f, .8f);
                state.EntityManager.SetComponentEnabled<IsInPoolCB>(blood, false);
                state.EntityManager.SetComponentEnabled<ParticleLifetimeComCB>(blood, true);
                state.EntityManager.SetComponentData(blood, new ParticlePositionComCB { position = position });
                state.EntityManager.SetComponentData(blood, new ParticleVelocityComCB { velocity = velocity + (float3)Random.insideUnitSphere * velocityJitter });
                state.EntityManager.SetComponentData(blood, new ParticleLifeDurationComCB { duration = Random.Range(3f, 5f) });
                state.EntityManager.SetComponentData(blood, new ParticleSizeComCB { size = one * Random.Range(.1f, .2f) });
                state.EntityManager.SetComponentData(blood, new URPMaterialPropertyBaseColor { Value = new float4(color.r, color.g, color.b, color.a) });

            }
        }

    }
}*/