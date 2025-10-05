using UnityEngine;

namespace Contrast
{
    public class DestinationMarker : MonoBehaviour
    {
        [Tooltip("Logical group like 'default', 'high', 'low', etc.")]
        public string groupId = "default";
    }
}
