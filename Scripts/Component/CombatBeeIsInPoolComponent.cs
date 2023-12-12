using Unity.Collections;
using Unity.Entities;

namespace CombatBee
{
    public struct IsInPoolCB : IComponentData, IEnableableComponent
    {

    }
    public struct BeePool : IComponentData 
    {
        public NativeQueue<Entity> pool;
    }
    public struct ParticlePool : IComponentData
    {
        public NativeQueue<Entity> pool;
    }

}