// Assets/Scripts/AnomalyScripts/MovedObjectAnomaly.cs
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Contrast
{
    /// <summary>
    /// Generic "move this object to one of its destination markers, then back on resolve".
    /// - Auto-finds MovableTarget under the room using targetGroupId
    /// - Filters DestinationMarkers by destinationGroupId
    /// - Prevents double-control with a static lock
    /// </summary>
    public class MovedObjectAnomaly : MonoBehaviour, IAnomaly
    {
        [Header("Target selection")]
        [Tooltip("Leave empty on the prefab; the script will auto-find the MovableTarget in this room matching targetGroupId.")]
        public Transform targetProp;

        [Tooltip("Which MovableTarget in the room to control (e.g., \"laptop\" or \"mirror\").")]
        public string targetGroupId = "laptop";

        [Header("Destination markers")]
        [Tooltip("Only DestinationMarkers with this groupId will be considered (e.g., \"laptop\" or \"mirror\").")]
        public string destinationGroupId = "laptop";

        [Tooltip("If true, prefer the farthest valid marker from the original position; otherwise pick random.")]
        public bool chooseFarthest = true;

        [Header("Motion")]
        public float moveOutDuration = 0.7f;
        public float moveBackDuration = 0.7f;
        public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool copyRotation = true;

        [Header("Auto-resolve (seconds; 0 = only on report/capture)")]
        public float autoResolveAfterSeconds = 0f;

        // IAnomaly
        public AnomalyDefinition Definition { get; private set; }
        public RoomController Room { get; private set; }
        public SpawnPoint Point { get; private set; }
        public bool IsResolved { get; private set; }

        // Runtime
        private System.Action<IAnomaly> _onResolved;
        private Vector3 _origPos;
        private Quaternion _origRot;
        private Transform _chosenMarker;
        private Coroutine _motion;
        private float _timer;

        // Prevent two anomalies from grabbing the same target simultaneously.
        private static readonly HashSet<Transform> s_lockedTargets = new HashSet<Transform>();

        public void Initialize(AnomalyRuntimeContext ctx)
        {
            Definition = ctx.definition;
            Room = ctx.room;
            Point = ctx.spawnPoint;
            _onResolved = ctx.onResolved;

            // 1) Find the correct MovableTarget (by group) under this room
            if (!targetProp)
            {
                var targets = Room.GetComponentsInChildren<MovableTarget>(true);
                var match = string.IsNullOrEmpty(targetGroupId)
                    ? targets.FirstOrDefault()
                    : targets.FirstOrDefault(t => t.targetGroupId == targetGroupId);
                if (match) targetProp = match.transform;
            }

            if (!targetProp)
            {
                Debug.LogError($"[MovedObjectAnomaly] No MovableTarget with group='{targetGroupId}' under room '{Room.roomId}'.");
                Abort();
                return;
            }

            // Lock the target so no other anomaly controls it concurrently
            if (s_lockedTargets.Contains(targetProp))
            {
                Debug.LogWarning($"[MovedObjectAnomaly] Target '{targetProp.name}' already controlled. Aborting spawn.");
                Abort();
                return;
            }
            s_lockedTargets.Add(targetProp);

            // 2) Cache original transform
            _origPos = targetProp.position;
            _origRot = targetProp.rotation;

            // 3) Collect destination markers for this object (by group)
            var markers = Room.GetComponentsInChildren<DestinationMarker>(true)
                              .Where(m => string.IsNullOrEmpty(destinationGroupId) || m.groupId == destinationGroupId)
                              .Select(m => m.transform)
                              .ToList();

            // Prefer markers that are not essentially the same spot
            var candidates = markers.Where(m => Vector3.Distance(m.position, _origPos) > 0.01f).ToList();
            if (candidates.Count == 0) candidates = markers; // fallback if only same-spot markers exist

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[MovedObjectAnomaly] No DestinationMarker with group='{destinationGroupId}' under room '{Room.roomId}'.");
                ReleaseLock();
                Abort();
                return;
            }

            _chosenMarker = chooseFarthest
                ? candidates.OrderByDescending(m => Vector3.Distance(m.position, _origPos)).First()
                : candidates[Random.Range(0, candidates.Count)];

            // 4) Animate to destination
            _motion = StartCoroutine(MoveOverTime(_origPos, _chosenMarker.position, _origRot, _chosenMarker.rotation, moveOutDuration));
        }

        private void Update()
        {
            if (IsResolved || autoResolveAfterSeconds <= 0f) return;
            _timer += Time.deltaTime;
            if (_timer >= autoResolveAfterSeconds)
                ForceResolve();
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
            ReleaseLock();
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

                if (targetProp)
                {
                    targetProp.position = Vector3.LerpUnclamped(fromPos, toPos, k);
                    if (copyRotation)
                        targetProp.rotation = Quaternion.SlerpUnclamped(fromRot, toRot, k);
                }
                yield return null;
            }

            if (targetProp)
            {
                targetProp.position = toPos;
                if (copyRotation) targetProp.rotation = toRot;
            }
        }

        private void Abort()
        {
            IsResolved = true;
            _onResolved?.Invoke(this);
        }

        private void ReleaseLock()
        {
            if (targetProp) s_lockedTargets.Remove(targetProp);
        }

        private void OnDestroy()
        {
            // Safety: if something destroys this without resolve, release lock and restore
            if (!IsResolved)
            {
                ReleaseLock();
                if (targetProp)
                {
                    targetProp.position = _origPos;
                    targetProp.rotation = _origRot;
                }
            }
        }
    }
}
