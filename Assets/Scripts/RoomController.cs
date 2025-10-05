using System.Collections.Generic;
using UnityEngine;

namespace Contrast
{
    public class RoomController : MonoBehaviour
    {
        public string roomId = "room";
        public int maxConcurrentAnomalies = 2;

        [HideInInspector] public List<SpawnPoint> spawnPoints = new();
        [HideInInspector] public int activeCount = 0;
    }
}

