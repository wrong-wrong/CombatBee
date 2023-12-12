using Unity.Entities;
using UnityEngine;

namespace CombatBee
{
    public class ToCreateSystemManuallyMono : MonoBehaviour
    {
        public void Start()
        {
            var CBParticleLogicUpdateSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<CBParticleLogicUpdateSystem>();
            var CBVelocityUpdateSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<CBVelocityUpdateSystem>();
            var fixedSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            fixedSystem.AddSystemToUpdateList(CBParticleLogicUpdateSystem);
            fixedSystem.AddSystemToUpdateList(CBVelocityUpdateSystem);
            fixedSystem.Timestep = 1 / 144f;
        }
    }
}