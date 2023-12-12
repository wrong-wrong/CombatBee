using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace CombatBee
{
    public struct TeamCB : ISharedComponentData
    {
        public int teamID;
    }
    public struct BeeVelocity : IComponentData 
    { 
        public float3 velocity;
    }

    public struct BeePosition : IComponentData
    {
        public float3 position;
    }

    public struct BeeSmoothMovement : IComponentData
    {
        public float3 smoothPosition;
        public float3 smoothDirection;
    }

    public struct BeeDead : IComponentData, IEnableableComponent
    {
        public float deathTimer;
    }

    public struct BeeEnemyTarget : IComponentData, IEnableableComponent
    {
        public Entity enemy;
    }
    public struct BeeResourceTarget : IComponentData, IEnableableComponent
    {
        public Entity resource;
    }
    public struct BeeStateHoldingResource : IComponentData, IEnableableComponent
    {

    }
    public struct BeeStateAttacking : IComponentData, IEnableableComponent //if bee's enemy within range of attackDistance , this component should be enabled
    {

    }


    public class BeeAuthoring : MonoBehaviour
    {
        public GameObject BeePrefab;
        public class Baker : Baker<BeeAuthoring>
        {
            public override void Bake(BeeAuthoring authoring)
            {
                var entity = GetEntity(authoring.BeePrefab, TransformUsageFlags.Dynamic);
                AddComponent<TeamCB>(entity);
                AddComponent<BeeVelocity>(entity);
                AddComponent<BeePosition>(entity);
                AddComponent<BeeSmoothMovement>(entity);
                
                AddComponent<IsInPoolCB>(entity);
                AddComponent(entity, new BeeEnemyTarget { enemy = Entity.Null });
                AddComponent(entity, new BeeResourceTarget { resource = Entity.Null });
                AddComponent<BeeStateHoldingResource>(entity);
                AddComponent<BeeStateAttacking>(entity);
                AddComponent(entity, new BeeDead { deathTimer = 1f });
                AddComponent<URPMaterialPropertyBaseColor>(entity);
                SetComponentEnabled<BeeStateAttacking>(entity, false);
                SetComponentEnabled<BeeStateHoldingResource>(entity, false);
                SetComponentEnabled<BeeEnemyTarget>(entity, false);
                SetComponentEnabled<BeeResourceTarget>(entity, false);
                SetComponentEnabled<IsInPoolCB>(entity, false);
                SetComponentEnabled<BeeDead>(entity, false);
            }
        }
    }
}