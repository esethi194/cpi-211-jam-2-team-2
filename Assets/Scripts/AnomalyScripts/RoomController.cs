using System.Collections.Generic;
using UnityEngine;

namespace Contrast
{
    [DisallowMultipleComponent]
    public class RoomController : MonoBehaviour
    {
        [Header("Identity")]
        public string roomId = "room";

        [Header("Limits")]
        public int maxConcurrentAnomalies = 2;

        [HideInInspector] public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
        [HideInInspector] public int activeCount = 0; // updated by AnomalyManager

        private void Awake()
        {
            RefreshSpawnPoints();
        }

        /// <summary>Scan children and cache all SpawnPoint components.</summary>
        public void RefreshSpawnPoints()
        {
            spawnPoints.Clear();
            var found = GetComponentsInChildren<SpawnPoint>(true);
            if (found != null && found.Length > 0)
            {
                for (int i = 0; i < found.Length; i++)
                    spawnPoints.Add(found[i]);
            }
        }

#if UNITY_EDITOR
        // Keep the cache fresh in editor when you add/remove points.
        private void OnValidate()
        {
            if (!Application.isPlaying)
                RefreshSpawnPoints();
        }

        private void Reset()
        {
            // Handy default: use object name as room id
            roomId = gameObject.name.ToLowerInvariant();
            RefreshSpawnPoints();
        }

        private void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.25f,
                $"Room: {roomId}\nSpawnPoints: {spawnPoints.Count}\nActive: {activeCount}/{maxConcurrentAnomalies}"
            );
        }
#endif
    }
}
