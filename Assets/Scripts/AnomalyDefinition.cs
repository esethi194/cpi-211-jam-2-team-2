using UnityEngine;

namespace Contrast
{
    [CreateAssetMenu(menuName = "Contrast/Anomaly Definition")]

    public class AnomalyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string anomalyId; // Unique name for each anomaly IE "missing_plate"
        public AnomalyCategory category;

        [Header("Limits")]
        [Range(0f, 1f)] public float weight = 0.25f; // Weighted change for anomaly spawning
        public float minGameMinute = 0f;
        public float maxGameMinute = 99f; //Lock before / after a certain time
        public bool allowRepeatInSameRoom = true;
        public float globalCooldownSeconds = 60f; // Wait this long after fixing before can spawn again
        public float perRoomCooldownSeconds = 90f; 

        [Header("Spawn Rules")]
        public bool requiresSpecificSpawnPoints = true;
        [Tooltip("If empty and requiresSpecificSpawnPoints the anomaly WILL NOT spawn")]
        public string[] allowedSpawnPointTags; //Use the SpawnPoint.tagId to filter

        [Header("Setup")]
        public GameObject anomalyPrefab;
        public AudioClip spawnSfx;
        public AudioClip resolveSfx;

    }
}
