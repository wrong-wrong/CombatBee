using System;
using Unity.Entities;
using Random = Unity.Mathematics.Random;
namespace CombatBee
{
    public struct RandomSingletonCB : IComponentData
    {
        public Random random;
    }
    public class CombatBeeRandomSingletonAuthoring : Singleton<CombatBeeRandomSingletonAuthoring>
    {
        public uint seed = 1;
        public bool isUsingSeed;
        public class Baker : Baker<CombatBeeRandomSingletonAuthoring>
        {
            public override void Bake(CombatBeeRandomSingletonAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                if (authoring.isUsingSeed)
                {
                    AddComponent(entity, new RandomSingletonCB
                    {
                        random = new Random(CombatBeeRandomSingletonAuthoring.Instance.seed),
                    });
                }
                else
                {
                    AddComponent(entity, new RandomSingletonCB
                    {
                        random = new Random((uint)DateTime.Now.Ticks),
                    });
                }

            }
        }
    }
}