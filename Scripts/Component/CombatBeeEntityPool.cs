using Unity.Collections;
using Unity.Entities;

namespace CombatBee
{
    public struct EntityPoolCB : IComponentData
    {
        public NativeQueue<Entity> pool;
    }
}