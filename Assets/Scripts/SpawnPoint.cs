using UnityEngine;

namespace Contrast
{
    public class SpawnPoint : MonoBehavior
    {
        [Tooltip("AnomalyID ex 'missing_plate'")]
        public; string tagId;
        Tooltip("Optional room link if not in parent RoomController")
        public RoomController roomOverride;
    }
}
