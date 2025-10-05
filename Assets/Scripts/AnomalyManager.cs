// Assets/Scripts/Anomalies/AnomalyManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Contrast
{
    public class AnomalyManager : MonoBehaviour
    {
        [Header("Catalog")]
        public List<AnomalyDefinition> anomalyCatalog = new();

        [Header("Rooms")]
        public List<RoomController> rooms = new();

        [Header("Global Rules")]
        public int globalMaxConcurrent = 4;
        public float baseSpawnInterval = 30f;            // seconds
        public AnimationCurve spawnIntervalCurve;        // time(min) -> multiplier (1=base)
        public int antiRepeatBuffer = 2;                 // dont spawn an anomaly that was recently spawned
        public bool seedRng = true;
        public int rngSeed = 12345;

        [Header("Audio")]
        public AudioSource sfxSource;

        // Runtime
        private System.Random _rng;
        private readonly HashSet<IAnomaly> _active = new();
        private readonly Queue<string> _recentTypes = new();
        private readonly Dictionary<string, float> _globalCooldownUntil = new();
        private readonly Dictionary<(string roomId, string anomalyId), float> _perRoomCooldownUntil = new();

        private float _gameMinutes;                      // Game clock in minutes
        private float _spawnTimer;

        public int ActiveCount => _active.Count;

        private void Start()
        {
            _rng = seedRng ? new System.Random(rngSeed) : new System.Random();
            _spawnTimer = 0f;

            // Auto-discover rooms if not assigned
            if (rooms.Count == 0)
                rooms = FindObjectsByType<RoomController>(FindObjectsSortMode.None).ToList();
        }

        private void Update()
        {
            // Simple “clock”: 1 real sec == 1 game sec
            _gameMinutes += Time.deltaTime / 60f;

            float multiplier = spawnIntervalCurve != null && spawnIntervalCurve.length > 0
                ? Mathf.Max(0.1f, spawnIntervalCurve.Evaluate(_gameMinutes))
                : 1f;

            float interval = baseSpawnInterval * multiplier;
            _spawnTimer += Time.deltaTime;

            if (_spawnTimer >= interval)
            {
                _spawnTimer = 0f;
                TrySpawnOne();
            }
        }

        private void TrySpawnOne()
        {
            if (_active.Count >= globalMaxConcurrent) return;

            // Pick a room with capacity
            var eligibleRooms = rooms.Where(r => r.activeCount < r.maxConcurrentAnomalies).ToList();
            if (eligibleRooms.Count == 0) return;

            var room = eligibleRooms[_rng.Next(eligibleRooms.Count)];

            // Build candidate anomaly list
            var candidates = anomalyCatalog
                .Where(a => a != null
                            && _gameMinutes >= a.minGameMinute
                            && _gameMinutes <= a.maxGameMinute
                            && !IsInAntiRepeat(a.anomalyId)
                            && !IsGloballyCoolingDown(a)
                            && !IsPerRoomCoolingDown(room, a)
                            && HasValidSpawnPoint(room, a))
                .ToList();

            if (candidates.Count == 0) return;

            // Weighted random by .weight
            var pick = WeightedPick(candidates, a => a.weight);
            if (pick == null) return;

            var sp = PickSpawnPoint(room, pick);
            if (sp == null) return;

            SpawnAnomaly(room, sp, pick);
        }

        private bool HasValidSpawnPoint(RoomController room, AnomalyDefinition def)
        {
            if (!def.requiresSpecificSpawnPoints) return room.spawnPoints.Count > 0;
            if (def.allowedSpawnPointTags == null || def.allowedSpawnPointTags.Length == 0) return false;
            return room.spawnPoints.Any(p => def.allowedSpawnPointTags.Contains(p.tagId));
        }

        private SpawnPoint PickSpawnPoint(RoomController room, AnomalyDefinition def)
        {
            var list = def.requiresSpecificSpawnPoints
                ? room.spawnPoints.Where(p => def.allowedSpawnPointTags.Contains(p.tagId)).ToList()
                : room.spawnPoints;

            if (list.Count == 0) return null;
            return list[_rng.Next(list.Count)];
        }

        private bool IsInAntiRepeat(string anomalyId)
        {
            return _recentTypes.Contains(anomalyId);
        }

        private bool IsGloballyCoolingDown(AnomalyDefinition def)
        {
            if (!_globalCooldownUntil.TryGetValue(def.anomalyId, out var t)) return false;
            return Time.time < t;
        }

        private bool IsPerRoomCoolingDown(RoomController room, AnomalyDefinition def)
        {
            if (!_perRoomCooldownUntil.TryGetValue((room.roomId, def.anomalyId), out var t)) return false;
            return Time.time < t;
        }

        private void SpawnAnomaly(RoomController room, SpawnPoint sp, AnomalyDefinition def)
        {
            var go = Instantiate(def.anomalyPrefab, sp.transform.position, sp.transform.rotation, room.transform);
            var anomaly = go.GetComponent<IAnomaly>();
            if (anomaly == null)
            {
                Debug.LogError($"Prefab for {def.anomalyId} lacks IAnomaly component.", def);
                Destroy(go);
                return;
            }

            var ctx = new AnomalyRuntimeContext
            {
                definition = def,
                room = room,
                spawnPoint = sp,
                onResolved = HandleResolved
            };

            anomaly.Initialize(ctx);
            _active.Add(anomaly);
            room.activeCount++;

            if (def.spawnSfx && sfxSource) sfxSource.PlayOneShot(def.spawnSfx);

            // Track recent types for anti-repeat
            _recentTypes.Enqueue(def.anomalyId);
            while (_recentTypes.Count > Mathf.Max(antiRepeatBuffer, 0))
                _recentTypes.Dequeue();
        }

        private void HandleResolved(IAnomaly anomaly)
        {
            if (anomaly == null || !_active.Contains(anomaly)) return;

            var def = anomaly.Definition;
            var room = anomaly.Room;

            _active.Remove(anomaly);
            if (room) room.activeCount = Mathf.Max(0, room.activeCount - 1);

            // Cooldowns
            if (def != null)
            {
                _globalCooldownUntil[def.anomalyId] = Time.time + def.globalCooldownSeconds;
                if (room)
                    _perRoomCooldownUntil[(room.roomId, def.anomalyId)] = Time.time + def.perRoomCooldownSeconds;
            }

            // SFX
            if (def && def.resolveSfx && sfxSource) sfxSource.PlayOneShot(def.resolveSfx);

            // Clean object
            var mb = anomaly as MonoBehaviour;
            if (mb) Destroy(mb.gameObject);
        }

        public void ForceResolveAll()
        {
            foreach (var a in _active.ToArray())
                a.ForceResolve();
            // HandleResolved will be called by the anomaly or could be followed by a pass to cleanup
        }

        public void ForceSpawn(string roomId, string anomalyId)
        {
            var room = rooms.FirstOrDefault(r => r.roomId == roomId);
            var def = anomalyCatalog.FirstOrDefault(a => a.anomalyId == anomalyId);
            if (room == null || def == null) { Debug.LogWarning("ForceSpawn: room or anomaly not found."); return; }
            var sp = PickSpawnPoint(room, def);
            if (sp == null) { Debug.LogWarning("ForceSpawn: no valid spawn point."); return; }
            SpawnAnomaly(room, sp, def);
        }

        // Utility weighted pick
        private T WeightedPick<T>(IList<T> items, System.Func<T, float> weightSelector)
        {
            float total = 0f;
            foreach (var i in items) total += Mathf.Max(0f, weightSelector(i));
            if (total <= 0f) return items[_rng.Next(items.Count)];

            float r = (float)(_rng.NextDouble() * total);
            float accum = 0f;
            foreach (var i in items)
            {
                accum += Mathf.Max(0f, weightSelector(i));
                if (r <= accum) return i;
            }
            return items[items.Count - 1];
        }

        // Reporting API
        public bool CheckReport(string roomId, AnomalyCategory? category, string anomalyIdOrNull)
        {
            foreach (var a in _active)
            {
                bool roomMatch = a.Room != null && a.Room.roomId == roomId;
                if (!roomMatch) continue;

                bool idMatch = !string.IsNullOrEmpty(anomalyIdOrNull) && a.Definition.anomalyId == anomalyIdOrNull;
                bool catMatch = category.HasValue && a.Definition.category == category.Value;

                if ((anomalyIdOrNull != null && idMatch) || (anomalyIdOrNull == null && category.HasValue && catMatch))
                {
                    a.ForceResolve();
                    HandleResolved(a);
                    return true;
                }
            }
            return false;
        }

        public IReadOnlyCollection<IAnomaly> Active => _active;

        // Returns true if a capturable anomaly was in-frame and got resolved.
        public bool TryCapture(Camera cam, Vector3 origin, float minScreenFraction = 0.01f, float maxDistance = 30f, LayerMask occlusionMask = default)
        {
            if (_active.Count == 0 || cam == null) return false;

            // Compute camera frustum
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);

            IAnomaly best = null;
            float bestScore = 0f;

            foreach (var a in _active)
            {
                var mb = a as MonoBehaviour;
                if (mb == null) continue;
                var go = mb.gameObject;
                if (!go.activeInHierarchy) continue;

                // Prefer an explicit anchor if the anomaly provides one; else use renderer bounds center
                Vector3 worldCenter;
                Bounds worldBounds = default;
                var rend = go.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    worldBounds = rend.bounds;
                    worldCenter = worldBounds.center;
                }
                else
                {
                    worldCenter = go.transform.position;
                    worldBounds = new Bounds(worldCenter, Vector3.one * 0.5f);
                }

                // Distance gate
                float dist = Vector3.Distance(origin, worldCenter);
                if (dist > maxDistance) continue;

                // In frustum?
                if (!GeometryUtility.TestPlanesAABB(planes, worldBounds)) continue;

                // On screen and in front of camera?
                var vp = cam.WorldToViewportPoint(worldCenter);
                if (vp.z < 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) continue;

                // Occlusion check (line of sight): ray from camera to bounds center
                if (occlusionMask.value != 0)
                {
                    if (Physics.Linecast(cam.transform.position, worldCenter, out var hit, occlusionMask, QueryTriggerInteraction.Ignore))
                    {
                        // If we hit something that isn’t the anomaly hierarchy, consider it occluded
                        if (hit.transform != null && !hit.transform.IsChildOf(go.transform))
                            continue;
                    }
                }

                // A simple “how much is it on screen” proxy = projected bounds size
                float screenScore = EstimateScreenFraction(cam, worldBounds);
                if (screenScore < minScreenFraction) continue;

                // Keep the best (largest on screen)
                if (screenScore > bestScore)
                {
                    bestScore = screenScore;
                    best = a;
                }
            }

            if (best != null)
            {
                best.ForceResolve();     // anomaly will call back into HandleResolved via _onResolved
                return true;
            }

            return false;
        }

        private static float EstimateScreenFraction(Camera cam, Bounds b)
        {
            // Sample 8 corners of the AABB and take the 2D area of their screen-space bbox
            Vector3[] corners = {
        new(b.min.x, b.min.y, b.min.z), new(b.max.x, b.min.y, b.min.z),
        new(b.min.x, b.max.y, b.min.z), new(b.max.x, b.max.y, b.min.z),
        new(b.min.x, b.min.y, b.max.z), new(b.max.x, b.min.y, b.max.z),
        new(b.min.x, b.max.y, b.max.z), new(b.max.x, b.max.y, b.max.z),
    };
            Vector2 min = new(float.MaxValue, float.MaxValue);
            Vector2 max = new(float.MinValue, float.MinValue);

            foreach (var c in corners)
            {
                var vp = cam.WorldToViewportPoint(c);
                if (vp.z <= 0f) return 0f; // behind camera
                min = Vector2.Min(min, new Vector2(vp.x, vp.y));
                max = Vector2.Max(max, new Vector2(vp.x, vp.y));
            }
            var size = max - min;
            float area = Mathf.Clamp01(size.x) * Mathf.Clamp01(size.y);
            return area; // 0..1 fraction of the screen
        }

    }
}

