using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CombatBee
{
    public struct ResGridCoordinateCB : IComponentData
    {
        public int gridX;
        public int gridY;
    }
    public struct ResStackCB : IComponentData,IEnableableComponent
    {
        public int stackIndex;
        public bool stacked;
    }
    public struct ResVelocityCB : IComponentData
    {
        public float3 velocity;
    }
    public struct ResIsCarriedCB : IComponentData, IEnableableComponent
    {
        public Entity Bee;
        public int holderBeeTeamId;
    }

    public struct ResWaitToCheck : IComponentData, IEnableableComponent
    {

    }
    public class ResourceAuthoring : MonoBehaviour
    {
        public class Baker : Baker<ResourceAuthoring>
        {
            public override void Bake(ResourceAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ResStackCB>(entity);
                AddComponent<ResVelocityCB>(entity);
                AddComponent<ResIsCarriedCB>(entity);
                AddComponent<ResWaitToCheck>(entity);
                AddComponent<ResGridCoordinateCB>(entity);
                SetComponentEnabled<ResStackCB>(entity, false);
                SetComponentEnabled<ResIsCarriedCB>(entity, false);
                SetComponentEnabled<ResWaitToCheck>(entity, true);
            }
        }
    }
}