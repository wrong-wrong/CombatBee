using JetBrains.Annotations;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CombatBee
{
    public struct FieldCOM : IComponentData
    {
        public float3 size;
        public float gravity;
    }
    public class FieldAuthoring : MonoBehaviour
    {
        public float gravity = -20f;
        public Vector3 size;

        public void Start()
        {
            size = GameObject.Find("FieldGizmo").transform.localScale;
        }
        public class Baker : Baker<FieldAuthoring>
        {
            public override void Bake(FieldAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new FieldCOM
                {
                    size = authoring.size,
                    gravity = authoring.gravity,
                });
            }
        }
    }
}