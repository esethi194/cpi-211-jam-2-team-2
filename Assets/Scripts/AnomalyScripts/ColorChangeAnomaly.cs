using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Contrast
{

    public class ColorChangeAnomaly : MonoBehaviour, IAnomaly
    {
        [Header("Target selection")]
        [Tooltip("Leave empty on prefab; found by MovableTarget.targetGroupId under the room.")]
        public Transform targetProp;
        public string targetGroupId = "laptop";   // set per prefab ("laptop", "mirror", ...)

        [Header("Color")]
        public bool randomBrightColor = true;
        public Color anomalyColor = Color.red;
        public bool includeChildrenRenderers = true;

        [Header("Advanced")]
        public bool autoDetectProperty = true;        // _BaseColor (URP/HDRP) or _Color (Built-in)
        public string colorPropertyName = "_BaseColor";

        [Header("Auto-resolve (seconds; 0 = only on report/capture)")]
        public float autoResolveAfterSeconds = 0f;

        // IAnomaly
        public AnomalyDefinition Definition { get; private set; }
        public RoomController Room { get; private set; }
        public SpawnPoint Point { get; private set; }
        public bool IsResolved { get; private set; }

        private System.Action<IAnomaly> _onResolved;
        private float _timer;

        private static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ID_Color = Shader.PropertyToID("_Color");

        private struct RenderSlot
        {
            public Renderer renderer;
            public int colorId;
            public Color original;
            public bool valid;
        }

        private readonly List<RenderSlot> _slots = new List<RenderSlot>();
        private MaterialPropertyBlock _mpb;

        public void Initialize(AnomalyRuntimeContext ctx)
        {
            Definition = ctx.definition;
            Room = ctx.room;
            Point = ctx.spawnPoint;
            _onResolved = ctx.onResolved;

            // 1) Find target by group id (MovableTarget)
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
                Debug.LogError($"[ColorChangeAnomaly] No MovableTarget with group='{targetGroupId}' under room '{Room.roomId}'.");
                Abort(); return;
            }

            // 2) Acquire shared lock
            if (!AnomalyTargetLock.TryLock(targetProp))
            {
                Debug.LogWarning($"[ColorChangeAnomaly] Target '{targetProp.name}' already controlled. Aborting.");
                Abort(); return;
            }

            // 3) Gather renderers
            var renderers = includeChildrenRenderers
                ? targetProp.GetComponentsInChildren<Renderer>(true)
                : targetProp.GetComponents<Renderer>();

            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogWarning($"[ColorChangeAnomaly] Target '{targetProp.name}' has no Renderer. Aborting.");
                AnomalyTargetLock.Release(targetProp);
                Abort(); return;
            }

            _slots.Clear();
            foreach (var r in renderers)
            {
                if (r == null || r.sharedMaterial == null) continue;

                int pid = -1;
                if (autoDetectProperty)
                {
                    var m = r.sharedMaterial;
                    if (m.HasProperty(ID_BaseColor)) pid = ID_BaseColor;
                    else if (m.HasProperty(ID_Color)) pid = ID_Color;
                }
                else
                {
                    var id = Shader.PropertyToID(colorPropertyName);
                    if (r.sharedMaterial.HasProperty(id)) pid = id;
                }

                if (pid == -1) continue;

                var original = r.sharedMaterial.GetColor(pid);
                _slots.Add(new RenderSlot
                {
                    renderer = r,
                    colorId = pid,
                    original = original,
                    valid = true
                });
            }

            if (_slots.Count == 0)
            {
                Debug.LogWarning($"[ColorChangeAnomaly] No compatible color property found on renderers of '{targetProp.name}'.");
                AnomalyTargetLock.Release(targetProp);
                Abort(); return;
            }

            // 4) Decide color
            var newColor = randomBrightColor
                ? PickDifferentBrightColor(_slots[0].original)
                : anomalyColor;

            // 5) Apply via MPB (no material duplication)
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            foreach (var s in _slots)
            {
                if (!s.valid || s.renderer == null) continue;
                s.renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(s.colorId, newColor);
                s.renderer.SetPropertyBlock(_mpb);
            }
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

            // Restore original colors
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            foreach (var s in _slots)
            {
                if (!s.valid || s.renderer == null) continue;
                s.renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(s.colorId, s.original);
                s.renderer.SetPropertyBlock(_mpb);
            }

            IsResolved = true;
            AnomalyTargetLock.Release(targetProp);
            _onResolved?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (!IsResolved) ForceResolve(); // also releases lock
        }

        private void Abort()
        {
            IsResolved = true;
            AnomalyTargetLock.Release(targetProp);
            _onResolved?.Invoke(this);
        }

        private static Color PickDifferentBrightColor(Color original)
        {
            for (int i = 0; i < 8; i++)
            {
                var c = Random.ColorHSV(0f, 1f, 0.65f, 1f, 0.65f, 1f);
                if (ColorDistance(c, original) > 0.25f) return c;
            }
            return Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f);
        }

        private static float ColorDistance(Color a, Color b)
        {
            var dr = a.r - b.r; var dg = a.g - b.g; var db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }
    }
}
