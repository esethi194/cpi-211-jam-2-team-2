namespace Contrast
{
    using System.Collections.Generic;
    using UnityEngine;

    public static class AnomalyTargetLock
    {
        private static readonly HashSet<Transform> _locked = new HashSet<Transform>();

        public static bool TryLock(Transform t)
        {
            if (t == null) return false;
            if (_locked.Contains(t)) return false;
            _locked.Add(t);
            return true;
        }

        public static void Release(Transform t)
        {
            if (t == null) return;
            _locked.Remove(t);
        }

        public static bool IsLocked(Transform t) => t != null && _locked.Contains(t);
    }
}
