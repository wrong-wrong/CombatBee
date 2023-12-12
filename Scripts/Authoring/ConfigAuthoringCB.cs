using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CombatBee
{
    public struct ConfigCOMCB : IComponentData
    {
        public float3 PoolPosition;

        public Entity ResourcePrefab;
        public Entity ParticlePrefab;
        public Entity BeePrefab;

        public int initialResourceCount;
        public int beesPerResource;
        public float resourceSize;
        public float snapStiffness;
        public float carryStiffness;
        public float spawnRate;

        public int initialBeeCount;
        public float3 spawnEffectColor;
        public float3 bloodColor;
        public float3 teamColorZero;
        public float3 teamColorOne;
        public float minBeeSize;
        public float maxBeeSize;
        public float speedStretch;
        public float rotationStiffness;

        public float aggression;
        public float flightJitter;
        public float teamAttraction;
        public float teamRepulsion;

        public float damping;
        public float chaseForce;
        public float carryForce;
        public float grabDistance;
        public float attackDistance;
        public float attackForce;
        public float hitDistance;
        public float maxSpawnSpeed;
    }
    public class ConfigAuthoringCB : MonoBehaviour
    {
        public GameObject ResourcePrefab;
        public GameObject ParticlePrefab;
        public GameObject BeePrefab;

        public float3 PoolPosition;

        [Space(10)]
        [Header("Resource config")]
        public int initialResourceCount;
        public int beesPerResource;
        public float resourceSize;
        public float snapStiffness;
        public float carryStiffness;
        public float spawnRate = .1f;

        [Space(10)]
        [Header("Bee config")]
        public int initialBeeCount;
        public Color SpawnEffect;
        public Color blood;
        public Color teamColorOne;
        public Color teamColorTwo;
        public float minBeeSize;
        public float maxBeeSize;
        public float speedStretch;
        public float rotationStiffness;
        [Space(10)]
        [Range(0f, 1f)]
        public float aggression;
        public float flightJitter;
        public float teamAttraction;
        public float teamRepulsion;
        [Range(0f, 1f)]
        public float damping;
        public float chaseForce;
        public float carryForce;
        public float grabDistance;
        public float attackDistance;
        public float attackForce;
        public float hitDistance;
        public float maxSpawnSpeed;


        public class Baker : Baker<ConfigAuthoringCB>
        {
            public override void Bake(ConfigAuthoringCB authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ConfigCOMCB
                {
                    PoolPosition = authoring.PoolPosition,

                    ResourcePrefab = GetEntity(authoring.ResourcePrefab, TransformUsageFlags.Dynamic),
                    ParticlePrefab = GetEntity(authoring.ParticlePrefab, TransformUsageFlags.Dynamic),
                    BeePrefab = GetEntity(authoring.BeePrefab, TransformUsageFlags.Dynamic),
                    initialResourceCount = authoring.initialResourceCount,
                    beesPerResource = authoring.beesPerResource,
                    resourceSize = authoring.resourceSize,
                    snapStiffness = authoring.snapStiffness,
                    carryStiffness = authoring.carryStiffness,
                    spawnRate = authoring.spawnRate,

                    initialBeeCount = authoring.initialBeeCount,
                    teamColorZero = new float3(authoring.teamColorOne.r, authoring.teamColorOne.g, authoring.teamColorOne.b),
                    teamColorOne = new float3(authoring.teamColorTwo.r, authoring.teamColorTwo.g, authoring.teamColorTwo.b),
                    minBeeSize = authoring.minBeeSize,
                    maxBeeSize = authoring.maxBeeSize,
                    speedStretch = authoring.speedStretch,
                    rotationStiffness = authoring.rotationStiffness,

                    aggression = authoring.aggression,
                    flightJitter = authoring.flightJitter,
                    teamAttraction = authoring.teamAttraction,
                    teamRepulsion = authoring.teamRepulsion,

                    damping = authoring.damping,
                    chaseForce = authoring.chaseForce,
                    carryForce = authoring.carryForce,
                    grabDistance = authoring.grabDistance,
                    attackDistance = authoring.attackDistance,
                    attackForce = authoring.attackForce,
                    hitDistance = authoring.hitDistance,
                    maxSpawnSpeed = authoring.maxSpawnSpeed,

                    bloodColor = new float3 (authoring.blood.r, authoring.blood.g, authoring.blood.b),
                    spawnEffectColor = new float3(authoring.SpawnEffect.r, authoring.SpawnEffect.g, authoring.SpawnEffect.b),
                }) ;
            }
        }
    }
}