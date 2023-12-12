using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace CombatBee
{

    public struct ParticlePositionComCB : IComponentData
    {
        public float3 position;
    }
    public struct ParticleVelocityComCB : IComponentData
    {
        public float3 velocity;
    }
    public struct ParticleSizeComCB : IComponentData
    {   
        public float3 size; 
    }
    public struct ParticleLifetimeComCB : IComponentData,IEnableableComponent
    { public float lifetime; }
    public struct ParticleLifeDurationComCB : IComponentData
    {
        public float duration;
    }
    public struct ParticleIsBloodComCB : IComponentData, IEnableableComponent
    {

    }
    public struct ParticleStuckComCB : IComponentData, IEnableableComponent
    { 
    
    }

    public class ParticleAuthoring : MonoBehaviour
    {
        public class Baker: Baker<ParticleAuthoring>
        {
            public override void Bake(ParticleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ParticlePositionComCB>(entity);
                AddComponent<ParticleSizeComCB>(entity);
                AddComponent<ParticleVelocityComCB>(entity);
                AddComponent(entity, new ParticleLifetimeComCB { lifetime = 1f }); // lifetime is used as percentage to multiply some data
                AddComponent(entity, new ParticleLifeDurationComCB { duration = 4f });
                AddComponent<ParticleIsBloodComCB>(entity);
                AddComponent<IsInPoolCB>(entity);
                AddComponent<URPMaterialPropertyBaseColor>(entity);
                AddComponent<ParticleStuckComCB>(entity);
                SetComponentEnabled<IsInPoolCB>(entity, false);
                SetComponentEnabled<ParticleStuckComCB>(entity, false);
            }
        }
    }

}