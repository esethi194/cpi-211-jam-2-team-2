namespace Contrast
{
    using UnityEngine;

    [DisallowMultipleComponent]
    public class MovableTarget : MonoBehaviour
    {
        [Tooltip("Logical group for this movable object (e.g., 'laptop', 'mirror').")]
        public string targetGroupId = "default";
    }
}
