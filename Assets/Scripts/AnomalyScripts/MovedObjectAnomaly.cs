// Assets/Scripts/Anomalies/MovedObjectAnomaly.cs
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Contrast
{
    public class MovedObjectAnomaly : MonoBehaviour, IAnomaly
    {
        [Header("Target")]
        public Transform targetProp;                // leave empty in the prefab; we auto-find
        [Header("Marker Selection")]
        public string destinationGroupId = "default";
        public bool chooseFarthest = true;
        [Header("Motion")]
        public float moveOutDuration = 0.7f, moveBackDuration = 0.7f;
        public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool copyRotation = false;
        [Header("Auto-resolve (optional)")]
        public float autoResolveAfterSeconds = 0f;

        public AnomalyDefinition Definition { get; private set; }
        public RoomController Room { get; private set; }
        public SpawnPoint Point { get; private set; }
        public bool IsResolved { get; private set; }

        System.Action<IAnomaly> _onResolved;
        Vector3 _origPos; Quaternion _origRot; Transform _origParent;
        Transform _chosenMarker; Coroutine _motion; float _t;

        public void Initialize(AnomalyRuntimeContext ctx)
        {
            Definition = ctx.definition; Room = ctx.room; Point = ctx.spawnPoint; _onResolved = ctx.onResolved;

            // Auto-find the target in this room if not assigned
            if (!targetProp)
            {
                var marker = Room.GetComponentsInChildren<MovableTarget>(true).FirstOrDefault();
                if (marker) targetProp = marker.transform;
            }

            if (!targetProp)
            {
                Debug.LogError("[MovedObjectAnomaly] No target found (add MovableTarget to your cube).", this);
                IsResolved = true; _onResolved?.Invoke(this); return;
            }

            _origParent = targetProp.parent; _origPos = targetProp.position; _origRot = targetProp.rotation;

            var markers = Room.GetComponentsInChildren<DestinationMarker>(true)
                              .Where(m => string.IsNullOrEmpty(destinationGroupId) || m.groupId == destinationGroupId)
                              .Select(m => m.transform).Where(t => Vector3.Distance(t.position, _origPos) > 0.01f).ToList();

            if (markers.Count == 0)
            {
                Debug.LogWarning($"[MovedObjectAnomaly] No DestinationMarker found in room '{Room.roomId}'.");
                IsResolved = true; _onResolved?.Invoke(this); return;
            }

            _chosenMarker = chooseFarthest
                ? markers.OrderByDescending(m => Vector3.Distance(m.position, _origPos)).First()
                : markers[Random.Range(0, markers.Count)];

            _motion = StartCoroutine(MoveOverTime(targetProp, _origPos, _chosenMarker.position, _origRot, _chosenMarker.rotation, moveOutDuration));
        }

        void Update()
        {
            if (IsResolved || autoResolveAfterSeconds <= 0f) return;
            _t += Time.deltaTime; if (_t >= autoResolveAfterSeconds) ForceResolve();
        }

        public void ForceResolve()
        {
            if (IsResolved) return;
            if (_motion != null) StopCoroutine(_motion);
            _motion = StartCoroutine(MoveBackThenFinish());
        }

        IEnumerator MoveBackThenFinish()
        {
            yield return MoveOverTime(targetProp, targetProp.position, _origPos, targetProp.rotation, _origRot, moveBackDuration);
            IsResolved = true; _onResolved?.Invoke(this);
        }

        IEnumerator MoveOverTime(Transform tr, Vector3 fromPos, Vector3 toPos, Quaternion fromRot, Quaternion toRot, float duration)
        {
            duration = Mathf.Max(0.0001f, duration);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                float k = (moveCurve != null && moveCurve.length > 0) ? moveCurve.Evaluate(u) : u;
                tr.position = Vector3.LerpUnclamped(fromPos, toPos, k);
                if (copyRotation) tr.rotation = Quaternion.SlerpUnclamped(fromRot, toRot, k);
                yield return null;
            }
            tr.position = toPos; if (copyRotation) tr.rotation = toRot;
        }
    }
}
