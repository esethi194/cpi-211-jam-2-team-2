namespace Contrast
{
    using UnityEngine;

    [DisallowMultipleComponent]
    public class MovableTarget : MonoBehaviour
    {
        [Tooltip("Logical group for this movable object (e.g., 'laptop', 'mirror').")]
        public string targetGroupId = "laptop";

        // Set/cleared at runtime by the active anomaly controlling this object.
        [System.NonSerialized] public IAnomaly currentOwner;
    }
}
