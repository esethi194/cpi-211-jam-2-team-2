using UnityEngine;

namespace Contrast
{
    public interface IAnomaly
    {
        void Initialize(IAnomalyRuntimeContext ctx);
        void ForceResolve(); //Called if player reports correctly
        bool IsResolved { get; }
        AnomalyDefinition Definition { get; }
        RoomController Room { get; }
        SpawnPoint Point { get; }
    }

    public struct AnomalyRuntimeContext
    {
        public AnomalyDefinition definition;
        public RoomController room;
        public SpawnPoint spawnPoint;
        public System.Action<IAnomaly> onResolved; //Callback to notify manager when resolved
    }
}
