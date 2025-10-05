using UnityEngine;

namespace Contrast
{
    public class SpawnPoint : MonoBehaviour
    {
        [Tooltip("Logical ID like 'kitchen_table', 'hallway_floor', etc.")]
        public string tagId;
        [Tooltip("Optional room link if not found via parent RoomController.")]
        public RoomController roomOverride;
    }
}
