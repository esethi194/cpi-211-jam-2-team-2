using System.Collections;
using System.Linq;
using UnityEngine;

namespace Contrast
{
    public class MovedObjectAnomaly : MonoBehaviour, IAnomaly
    {
        [Header("Target (leave empty in prefab; auto-found in room)")]
        public Transform targetProp;

        [Header("Marker Selection")]
        public string destinationGroupId = "laptop";
        public bool chooseFarthest = true;

        [Header("Motion")]
        public float moveOutDuration = 0.7f;
        public float moveBackDuration = 0.7f;
        public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool copyRotation = true;

        [Header("Auto-resolve (seconds, 0 = disable)")]
        public float autoResolveAfterSeconds = 0f;

        public AnomalyDefinition Definition { get; private set; }
        public RoomController Room { get; private set; }
        public SpawnPoint Point { get; private set; }
        public bool IsResolved { get; private set; }

        private System.Action<IAnomaly> _onResolved;
        private Vector3 _origPos; private Quaternion _origRot;
        private Transform _chosenMarker; private Coroutine _motion; private float _t;

        public void Initialize(AnomalyRuntimeContext ctx)
        {
            Definition = ctx.definition; Room = ctx.room; Point = ctx.spawnPoint; _onResolved = ctx.onResolved;
            Debug.Log($"[MovedObjectAnomaly] Init in room '{Room.roomId}'. Target set? {(targetProp ? "yes" : "no")}");

            if (!targetProp)
            {
                var tag = Room.GetComponentsInChildren<MovableTarget>(true).FirstOrDefault();
                if (tag) targetProp = tag.transform;
            }

            if (!targetProp)
            {
                Debug.LogError($"[MovedObjectAnomaly] No target found in room '{Room.roomId}'. Add MovableTarget to the laptop.");
                IsResolved = true; _onResolved?.Invoke(this); return;
            }

            _origPos = targetProp.position; _origRot = targetProp.rotation;
            Debug.Log($"[MovedObjectAnomaly] Target '{targetProp.name}' at {_origPos}");

            var allMarkers = Room.GetComponentsInChildren<DestinationMarker>(true);
            Debug.Log($"[MovedObjectAnomaly] Found {allMarkers.Length} DestinationMarkers under room '{Room.roomId}'.");

            foreach (var m in allMarkers)
                Debug.Log($"  - marker '{m.name}' group='{m.groupId}' pos={m.transform.position}");

            var markers = allMarkers.Where(m => string.IsNullOrEmpty(destinationGroupId) || m.groupId == destinationGroupId)
                                    .Select(m => m.transform).ToList();

            var candidates = markers.Where(m => Vector3.Distance(m.position, _origPos) > 0.01f).ToList();
            if (candidates.Count == 0) candidates = markers; // fallback so we still do something

            Debug.Log($"[MovedObjectAnomaly] Group-filtered markers: {markers.Count}, candidates (not same spot): {candidates.Count}");

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[MovedObjectAnomaly] No DestinationMarker(group='{destinationGroupId}') under room '{Room.roomId}'.");
                IsResolved = true; _onResolved?.Invoke(this); return;
            }

            _chosenMarker = chooseFarthest
                ? candidates.OrderByDescending(m => Vector3.Distance(m.position, _origPos)).First()
                : candidates[Random.Range(0, candidates.Count)];

            Debug.Log($"[MovedObjectAnomaly] Chosen marker '{_chosenMarker.name}' at {_chosenMarker.position}, dist={Vector3.Distance(_origPos, _chosenMarker.position):F2}");

            _motion = StartCoroutine(MoveOverTime(_origPos, _chosenMarker.position, _origRot, _chosenMarker.rotation, moveOutDuration));
        }


        private void Update()
        {
            if (IsResolved || autoResolveAfterSeconds <= 0f) return;
            _t += Time.deltaTime; if (_t >= autoResolveAfterSeconds) ForceResolve();
        }

        public void ForceResolve()
        {
            if (IsResolved) return;
            if (_motion != null) StopCoroutine(_motion);
            _motion = StartCoroutine(ResolveRoutine());
        }

        private IEnumerator ResolveRoutine()
        {
            yield return MoveOverTime(targetProp.position, _origPos, targetProp.rotation, _origRot, moveBackDuration);
            IsResolved = true;
            _onResolved?.Invoke(this);
        }

        private IEnumerator MoveOverTime(Vector3 fromPos, Vector3 toPos, Quaternion fromRot, Quaternion toRot, float duration)
        {
            duration = Mathf.Max(0.0001f, duration);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                float k = (moveCurve != null && moveCurve.length > 0) ? moveCurve.Evaluate(u) : u;
                targetProp.position = Vector3.LerpUnclamped(fromPos, toPos, k);
                if (copyRotation) targetProp.rotation = Quaternion.SlerpUnclamped(fromRot, toRot, k);
                yield return null;
            }
            targetProp.position = toPos; if (copyRotation) targetProp.rotation = toRot;
        }
    }
}
