using UnityEngine;
using system.collections.Generic;

namespace Contrast
{
    public class RoomController : MonoBehaviour
    {
        public string roomId;
        public int maxConcurrentAnomalies = 2;

        [HideInInspector] public List<SpawnPoint> spawnPoints = new();
        [HideInInspector] public int activeCount = 0;

        private void Awake()
        {
            GetComponentsInChildren(true, spawnPoints);
            //Fallback to tags if no SpawnPoint components found

            if (spawnPoints.Count == 0)
                Debug.LogWarning($"Room '{roomId}' has no SpawnPoints defined!", this);
        }
    }
}
