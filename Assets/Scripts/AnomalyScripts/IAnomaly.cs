using UnityEngine;

namespace Contrast
{
    public interface IAnomaly
    {
        void Initialize(AnomalyRuntimeContext ctx);
        void ForceResolve();              // Called if player reports correctly or for cleanup
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
        public System.Action<IAnomaly> onResolved; // notify manager when done
    }
}
