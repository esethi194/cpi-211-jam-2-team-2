using UnityEngine;

namespace Contrast
{
    public class DebugAnomalyHotkeys : MonoBehaviour
    {
        public AnomalyManager manager;
        public string roomId = "room";
        public string anomalyId = "cube_moved_test";

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
                manager?.ForceSpawn(roomId, anomalyId);  // Force a spawn now

            if (Input.GetKeyDown(KeyCode.R))
                manager?.ForceResolveAll();              // Resolve everything
        }
    }
}
