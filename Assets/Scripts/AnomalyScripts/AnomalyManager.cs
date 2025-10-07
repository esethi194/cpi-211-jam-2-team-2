// Assets/Scripts/AnomalyScripts/AnomalyManager.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Contrast
{
    [DisallowMultipleComponent]
    public class AnomalyManager : MonoBehaviour
    {
        [Header("Rooms & Catalog")]
        public List<RoomController> rooms = new List<RoomController>();
        public List<AnomalyDefinition> anomalyCatalog = new List<AnomalyDefinition>();

        [Header("Spawn Rules")]
        [Tooltip("Max anomalies across all rooms at once.")]
        public int globalMaxConcurrent = 2;

        [Tooltip("Base seconds between spawn attempts.")]
        public float baseSpawnInterval = 6f;

        [Tooltip("Random +/- seconds added to each interval.")]
        public float spawnJitter = 2f;

        [Tooltip("Game minutes advanced per real-time second.")]
        public float gameMinutesPerSecond = 1f;
        
        [Tooltip("The time when the player wins")]
        public float winTime = 7f*60f;
        
        [Tooltip("Anomalies to state")]
        public const int roamAnoma = 2;
        public const int stalkAnoma = 4;
        public const int chargeAnoma = 5;

        [Header("Randomness")]
        [Tooltip("If true, use the fixed seed below for deterministic runs.")]
        public bool seedRng = false;
        public int rngSeed = 12345;

        [Header("Anti-Repeat (optional)")]
        [Tooltip("Block re-spawning the same anomalyId globally within this window (seconds). 0 = disabled.")]
        public float antiRepeatWindowSeconds = 0f;

        [Header("Debug")]
        public bool debugLogs = true;

        [ContextMenu("DEBUG: ForceSpawn Vase in Kitchen")]
        void DebugForceVaseKitchen() => ForceSpawn("room_kitchen", "move_vase");

        
        // ---- runtime state ----
        private readonly List<IAnomaly> _active = new List<IAnomaly>();
        private readonly Dictionary<string, float> _globalCooldownUntil = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _perRoomCooldownUntil = new Dictionary<string, float>(); // key: roomId|anomId
        private readonly Dictionary<string, float> _lastSpawnAt = new Dictionary<string, float>();           // for anti-repeat

        private System.Random _rng;
        private float _nextSpawnAt;
        private float _gameMinutes;

        // ---------- Unity ----------
        private void Awake()
        {
            _rng = seedRng ? new System.Random(rngSeed)
                           : new System.Random(unchecked(System.Environment.TickCount * 397) ^ GetInstanceID());
            ScheduleNextSpawn(initial: true);
        }

        private void Update()
        {
            // advance game clock
            _gameMinutes += Time.deltaTime * Mathf.Max(0f, gameMinutesPerSecond);
            gameTimetoWin(_gameMinutes);
            anomaliesToMonsterState();

            // spawn tick
            if (Time.time >= _nextSpawnAt)
            {
                TrySpawnOne();
                ScheduleNextSpawn(initial: false);
            }
        }

        // ---------- Spawning ----------
        private void TrySpawnOne()
        {
            D($"Tick: active={_active.Count}/{globalMaxConcurrent}, rooms={rooms.Count}");
            if (_active.Count >= globalMaxConcurrent) { D("Blocked: globalMaxConcurrent"); return; }

            // 1) pick an eligible room
            var eligibleRooms = rooms.Where(r => r && r.activeCount < r.maxConcurrentAnomalies).ToList();
            D($"Eligible rooms: {eligibleRooms.Count}");
            if (eligibleRooms.Count == 0) { D("No eligible rooms."); return; }

            var room = eligibleRooms[_rng.Next(eligibleRooms.Count)];
            D($"Picked room: {room.roomId} (active {room.activeCount}/{room.maxConcurrentAnomalies}, spawnPoints={room.spawnPoints.Count})", room);

            // 2) filter candidates
            var candidates = new List<AnomalyDefinition>();
            var debugReasons = new List<string>();

            foreach (var a in anomalyCatalog)
            {
                if (a == null) continue;
                bool ok = true;

                if (!(_gameMinutes >= a.minGameMinute && _gameMinutes <= a.maxGameMinute))
                { debugReasons.Add($"{a.anomalyId}:time_gate"); ok = false; }

                if (ok && IsInAntiRepeat(a.anomalyId))
                { debugReasons.Add($"{a.anomalyId}:anti_repeat"); ok = false; }

                if (ok && IsGloballyCoolingDown(a))
                { debugReasons.Add($"{a.anomalyId}:global_cd"); ok = false; }

                if (ok && IsPerRoomCoolingDown(room, a))
                { debugReasons.Add($"{a.anomalyId}:room_cd"); ok = false; }

                if (ok && !a.allowRepeatInSameRoom && IsTypeActiveInRoom(room, a.anomalyId))
                { debugReasons.Add($"{a.anomalyId}:already_active_in_room"); ok = false; }

                if (ok && !HasValidSpawnPoint(room, a))
                { debugReasons.Add($"{a.anomalyId}:no_spawnpoint"); ok = false; }

                if (ok && !RoomSupportsAnomaly(room, a))
                { debugReasons.Add($"{a.anomalyId}:room_missing_target/markers"); ok = false; }

                if (ok) candidates.Add(a);
            }

            D($"Candidates after filters: {candidates.Count} (catalog={anomalyCatalog.Count})");
            if (candidates.Count == 0) { D("Blocked: zero candidates. Sample: " + string.Join(", ", debugReasons.Take(6))); return; }

            // 3) weighted pick (shuffle first to remove list-order bias)
            var pick = WeightedPick(candidates, a => Mathf.Max(0f, a.weight));
            if (pick == null) { D("WeightedPick returned null"); return; }

            D($"Picked anomaly: id='{pick.anomalyId}', requiresSP={pick.requiresSpecificSpawnPoints}, allowed=[{(pick.allowedSpawnPointTags == null ? "null" : string.Join(",", pick.allowedSpawnPointTags))}]");

            // 4) choose a spawnpoint if required
            SpawnPoint sp = null;
            if (pick.requiresSpecificSpawnPoints)
            {
                var matches = room.spawnPoints.Where(p => pick.allowedSpawnPointTags != null &&
                                                          pick.allowedSpawnPointTags.Contains(p.tagId)).ToList();
                D($"Matching spawn points in room: {matches.Count}");
                if (matches.Count == 0) { D("No matching spawn points; abort."); return; }
                sp = matches[_rng.Next(matches.Count)];
            }
            else
            {
                D("No specific spawn point required; using room transform.");
            }

            // 5) spawn
            SpawnAnomaly(room, sp, pick);
        }

        private void SpawnAnomaly(RoomController room, SpawnPoint sp, AnomalyDefinition def)
        {
            var pos = (sp ? sp.transform.position : room.transform.position);
            var rot = (sp ? sp.transform.rotation : room.transform.rotation);
            D($"Instantiating '{def.anomalyId}' at {(sp ? $"SP '{sp.tagId}'" : "room origin")} pos={pos}", room);

            var go = Instantiate(def.anomalyPrefab, pos, rot, room.transform);
            var anomaly = go.GetComponent<IAnomaly>();
            if (anomaly == null)
            {
                Debug.LogError($"[AnomalyManager] Prefab for '{def.anomalyId}' lacks IAnomaly.", def.anomalyPrefab);
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

            // Only count if it didn't abort/resolve during Initialize
            if (!anomaly.IsResolved)
            {
                _active.Add(anomaly);
                room.activeCount++;
                _lastSpawnAt[def.anomalyId] = Time.time;
                D($"Spawned '{def.anomalyId}'. Active now={_active.Count}");
            }
            else
            {
                D($"'{def.anomalyId}' aborted during Initialize; not tracking as active.");
                var mb = anomaly as MonoBehaviour;
                if (mb) Destroy(mb.gameObject);
            }
        }

        private void HandleResolved(IAnomaly anomaly)
        {
            if (anomaly == null) return;

            var def = anomaly.Definition;
            var room = anomaly.Room;

            // remove from active list
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_active[i], anomaly))
                {
                    _active.RemoveAt(i);
                    break;
                }
            }

            if (room) room.activeCount = Mathf.Max(0, room.activeCount - 1);

            // set cooldowns
            if (def != null)
            {
                if (def.globalCooldownSeconds > 0f)
                    _globalCooldownUntil[def.anomalyId] = Time.time + def.globalCooldownSeconds;

                if (room && def.perRoomCooldownSeconds > 0f)
                {
                    string key = MakeRoomKey(room.roomId, def.anomalyId);
                    _perRoomCooldownUntil[key] = Time.time + def.perRoomCooldownSeconds;
                }
            }

            // destroy instance GameObject
            var mb = anomaly as MonoBehaviour;
            if (mb) Destroy(mb.gameObject);

            D($"Resolved '{def?.anomalyId ?? "unknown"}' in room '{room?.roomId ?? "null"}'. Active now={_active.Count}");
        }

        // ---------- Helpers ----------
        private void ScheduleNextSpawn(bool initial)
        {
            float baseTime = baseSpawnInterval;
            float jitter = spawnJitter > 0f ? (float)(_rng.NextDouble() * 2.0 - 1.0) * spawnJitter : 0f;
            _nextSpawnAt = Time.time + Mathf.Max(0.05f, baseTime + jitter);
            if (initial) D($"Next spawn in ~{baseTime + jitter:0.00}s");
        }

        private bool IsTypeActiveInRoom(RoomController room, string anomalyId)
        {
            foreach (var inst in _active)
            {
                if (inst == null || inst.Definition == null) continue;
                if (inst.Room == room && inst.Definition.anomalyId == anomalyId)
                    return true;
            }
            return false;
        }

        private bool IsGloballyCoolingDown(AnomalyDefinition def)
        {
            if (def == null || def.globalCooldownSeconds <= 0f) return false;
            return _globalCooldownUntil.TryGetValue(def.anomalyId, out float until) && Time.time < until;
        }

        private bool IsPerRoomCoolingDown(RoomController room, AnomalyDefinition def)
        {
            if (!room || def == null || def.perRoomCooldownSeconds <= 0f) return false;
            string key = MakeRoomKey(room.roomId, def.anomalyId);
            return _perRoomCooldownUntil.TryGetValue(key, out float until) && Time.time < until;
        }

        private bool IsInAntiRepeat(string anomalyId)
        {
            if (antiRepeatWindowSeconds <= 0f) return false;
            if (!_lastSpawnAt.TryGetValue(anomalyId, out float when)) return false;
            return (Time.time - when) < antiRepeatWindowSeconds;
        }

        private static string MakeRoomKey(string roomId, string anomId) => $"{roomId}|{anomId}";

        private bool HasValidSpawnPoint(RoomController room, AnomalyDefinition def)
        {
            if (!room || def == null) return false;
            if (!def.requiresSpecificSpawnPoints) return true;
            if (def.allowedSpawnPointTags == null || def.allowedSpawnPointTags.Length == 0) return false;
            return room.spawnPoints.Any(p => def.allowedSpawnPointTags.Contains(p.tagId));
        }

        /// <summary>
        /// Prevents wasted spawns: only consider anomalies for a room if that room
        /// actually contains the required MovableTarget + DestinationMarkers for that prefab.
        /// Logs what's missing for faster scene wiring.
        /// </summary>
        private bool RoomSupportsAnomaly(RoomController room, AnomalyDefinition def)
        {
            if (!room || def == null || !def.anomalyPrefab) return false;

            var move = def.anomalyPrefab.GetComponent<MovedObjectAnomaly>();
            if (move)
            {
                bool hasTarget = room.GetComponentsInChildren<MovableTarget>(true)
                                     .Any(t => t.targetGroupId == move.targetGroupId);
                bool hasMarker = room.GetComponentsInChildren<DestinationMarker>(true)
                                     .Any(m => m.groupId == move.destinationGroupId);

                if (!hasTarget || !hasMarker)
                {
                    D($"room_support: {def.anomalyId} target('{move.targetGroupId}')={hasTarget}, markers('{move.destinationGroupId}')={hasMarker}", room);
                    return false;
                }
            }

            // If you later add other anomaly types, add checks here.

            return true;
        }

        private AnomalyDefinition WeightedPick(List<AnomalyDefinition> items, System.Func<AnomalyDefinition, float> weightFn)
        {
            if (items == null || items.Count == 0) return null;

            // Shuffle to remove list order bias (esp. when weights tie/zero)
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }

            double total = 0;
            foreach (var i in items) total += Mathf.Max(0f, weightFn(i));

            if (total <= 0)
                return items[_rng.Next(items.Count)];

            double r = _rng.NextDouble() * total;
            double cum = 0;
            foreach (var i in items)
            {
                cum += Mathf.Max(0f, weightFn(i));
                if (r <= cum) return i;
            }
            return items[items.Count - 1];
        }

        private void D(string msg, Object ctx = null)
        {
            if (debugLogs) Debug.Log($"[AnomalyManager] {msg}", ctx ? ctx : this);
        }

        // ---------- Debug / Test ----------
        [ContextMenu("Force Resolve All")]
        public void ForceResolveAll()
        {
            var snapshot = _active.ToList(); // avoid mutation during iteration
            foreach (var a in snapshot)
                a?.ForceResolve();
        }

        /// <summary>Force-spawn a specific anomalyId in a specific roomId (ignores cooldowns & filters except spawn point rules).</summary>
        public bool ForceSpawn(string roomId, string anomalyId)
        {
            var room = rooms.FirstOrDefault(r => r && r.roomId == roomId);
            var def = anomalyCatalog.FirstOrDefault(a => a && a.anomalyId == anomalyId);

            if (!room || !def) { D($"ForceSpawn failed: room or anomaly not found (room='{roomId}', anomaly='{anomalyId}')"); return false; }

            SpawnPoint sp = null;
            if (def.requiresSpecificSpawnPoints)
            {
                var list = room.spawnPoints.Where(p => def.allowedSpawnPointTags != null &&
                                                       def.allowedSpawnPointTags.Contains(p.tagId)).ToList();
                if (list.Count == 0) { D("ForceSpawn: no matching spawn points"); return false; }
                sp = list[_rng.Next(list.Count)];
            }

            SpawnAnomaly(room, sp, def);
            return true;
        }

        /// <summary>
        /// Resolve by exact anomaly id, by target group (for move anomalies), or by category (fallback).
        /// Pass anomalyOrTargetId = "laptop" / "mirror" / "vase" to target a specific prop.
        /// </summary>
        public bool CheckReport(string roomId, AnomalyCategory? category, string anomalyOrTargetId)
        {
            var room = rooms.FirstOrDefault(r => r && r.roomId == roomId);
            if (!room)
            {
                D($"CheckReport: room '{roomId}' not found");
                return false;
            }

            IAnomaly match = null;

            foreach (var a in _active)
            {
                if (a == null || a.IsResolved || a.Room != room || a.Definition == null) continue;

                // 1) exact anomaly id
                if (!string.IsNullOrEmpty(anomalyOrTargetId) && a.Definition.anomalyId == anomalyOrTargetId)
                {
                    match = a; break;
                }

                // 2) target group id (e.g., "laptop", "mirror", "vase")
                if (!string.IsNullOrEmpty(anomalyOrTargetId))
                {
                    var mb = a as MonoBehaviour;
                    var move = mb ? mb.GetComponent<MovedObjectAnomaly>() : null;
                    if (move && move.targetGroupId == anomalyOrTargetId)
                    {
                        match = a; break;
                    }
                }

                // 3) category fallback
                if (match == null && category.HasValue && a.Definition.category == category.Value)
                {
                    match = a; break;
                }

                // 4) any active in room if nothing specified
                if (match == null && !category.HasValue && string.IsNullOrEmpty(anomalyOrTargetId))
                {
                    match = a; break;
                }
            }

            if (match != null)
            {
                D($"CheckReport: resolving '{match.Definition?.anomalyId}' in room '{roomId}'");
                match.ForceResolve();
                return true;
            }

            D($"CheckReport: no match in '{roomId}' (category={(category.HasValue ? category.ToString() : "null")}, id/target={anomalyOrTargetId ?? "null"})");
            return false;
        }
        
        //time to win
        public void gameTimetoWin(float game_time)
        {
            if (game_time >= winTime)
            {
                GameManager.instance.GameWin();
            }
        }
        // monster state based on anomalies
        public void anomaliesToMonsterState()
        {
            switch (_active.Count)
            {
                case 0:
                    GameManager.instance.RegisterMonsterState(0);
                    break;
                case roamAnoma:
                    GameManager.instance.RegisterMonsterState(1);
                    break;
                case stalkAnoma:
                    GameManager.instance.RegisterMonsterState(2);
                    break;
                case chargeAnoma:
                    GameManager.instance.RegisterMonsterState(3);
                    break;
            }
        }
        
        
        // Optional helper to list what�s active in a room
        public void DebugListActive(string roomId)
        {
            var room = rooms.FirstOrDefault(r => r && r.roomId == roomId);
            if (!room) { D($"DebugListActive: room '{roomId}' not found"); return; }
            foreach (var a in _active.Where(x => x != null && x.Room == room && !x.IsResolved))
                D($"Active in {roomId}: {a.Definition?.anomalyId}");
        }
    }
}
