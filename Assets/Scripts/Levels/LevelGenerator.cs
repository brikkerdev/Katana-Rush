using UnityEngine;
using System.Collections.Generic;
using Runner.Enemy;

namespace Runner.LevelGeneration
{
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Level Prefabs")]
        [SerializeField] private LevelSegment[] levelPrefabs;

        [Header("Generation Settings")]
        [SerializeField] private float viewDistance = 100f;
        [SerializeField] private float despawnDistance = 30f;
        [SerializeField] private int initialSegmentCount = 5;
        [SerializeField] private float maxDifficultyDistance = 1000f;

        [Header("Pooling")]
        [SerializeField] private bool useObjectPooling = true;
        [SerializeField] private int poolSizePerPrefab = 5;

        [Header("Enemy Spawning")]
        [SerializeField] private EnemySpawner enemySpawner;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private Transform player;
        private float nextSpawnZ;
        private LevelSegment lastSpawnedPrefab;
        private bool isInitialized;

        private List<ActiveSegment> activeSegments = new List<ActiveSegment>();
        private Dictionary<LevelSegment, Queue<LevelSegment>> segmentPools = new Dictionary<LevelSegment, Queue<LevelSegment>>();

        private struct ActiveSegment
        {
            public LevelSegment Instance;
            public LevelSegment Prefab;
            public float StartZ;
            public float EndZ;
        }

        public float CurrentDifficulty => player != null
            ? Mathf.Clamp01(player.position.z / maxDifficultyDistance)
            : 0f;

        public void SetPlayer(Transform playerTransform)
        {
            player = playerTransform;

            if (showDebug)
            {
                Debug.Log($"[LevelGenerator] Player set: {player.name}");
            }
        }

        private void Awake()
        {
            if (enemySpawner == null)
            {
                enemySpawner = GetComponentInChildren<EnemySpawner>();
            }

            if (enemySpawner == null)
            {
                enemySpawner = FindFirstObjectByType<EnemySpawner>();
            }

            if (useObjectPooling)
            {
                InitializePools();
            }
        }

        private void Start()
        {
            GenerateInitialSegments();
            isInitialized = true;

            if (showDebug)
            {
                Debug.Log($"[LevelGenerator] Initialized with {activeSegments.Count} segments");
            }
        }

        private void Update()
        {
            if (!isInitialized) return;
            if (player == null) return;

            TrySpawnSegments();
            TryDespawnSegments();
        }

        private void InitializePools()
        {
            if (levelPrefabs == null || levelPrefabs.Length == 0)
            {
                Debug.LogError("[LevelGenerator] No level prefabs assigned!");
                return;
            }

            foreach (var prefab in levelPrefabs)
            {
                if (prefab == null) continue;

                segmentPools[prefab] = new Queue<LevelSegment>();

                for (int i = 0; i < poolSizePerPrefab; i++)
                {
                    var segment = Instantiate(prefab, transform);
                    segment.gameObject.SetActive(false);
                    segment.name = $"{prefab.name}_Pool_{i}";
                    segmentPools[prefab].Enqueue(segment);
                }
            }
        }

        private void GenerateInitialSegments()
        {
            nextSpawnZ = 0f;

            for (int i = 0; i < initialSegmentCount; i++)
            {
                SpawnNextSegment();
            }
        }

        private void TrySpawnSegments()
        {
            float spawnThreshold = player.position.z + viewDistance;

            int safety = 100;
            while (nextSpawnZ < spawnThreshold && safety > 0)
            {
                SpawnNextSegment();
                safety--;
            }
        }

        private void TryDespawnSegments()
        {
            float despawnThreshold = player.position.z - despawnDistance;

            while (activeSegments.Count > 0 && activeSegments[0].EndZ < despawnThreshold)
            {
                DespawnSegment(activeSegments[0]);
                activeSegments.RemoveAt(0);
            }

            if (enemySpawner != null)
            {
                enemySpawner.DespawnEnemiesBeforeZ(despawnThreshold);
            }
        }

        private void SpawnNextSegment()
        {
            LevelSegment prefab = SelectNextPrefab();
            LevelSegment segment = GetOrCreateSegment(prefab);

            Vector3 position = new Vector3(0f, 0f, nextSpawnZ);
            segment.transform.position = position;
            segment.transform.rotation = Quaternion.identity;
            segment.gameObject.SetActive(true);

            var activeSegment = new ActiveSegment
            {
                Instance = segment,
                Prefab = prefab,
                StartZ = nextSpawnZ,
                EndZ = nextSpawnZ + segment.Length
            };

            activeSegments.Add(activeSegment);

            if (enemySpawner != null)
            {
                float playerDistance = player != null ? player.position.z : 0f;
                enemySpawner.SpawnEnemiesForSegment(segment, playerDistance);
            }

            if (showDebug)
            {
                Debug.Log($"[LevelGenerator] Spawned segment at Z={nextSpawnZ}, enemies spawned");
            }

            nextSpawnZ += segment.Length;
            lastSpawnedPrefab = prefab;
        }

        private LevelSegment SelectNextPrefab()
        {
            if (levelPrefabs == null || levelPrefabs.Length == 0)
            {
                Debug.LogError("[LevelGenerator] No level prefabs!");
                return null;
            }

            List<LevelSegment> validPrefabs = new List<LevelSegment>();

            foreach (var prefab in levelPrefabs)
            {
                if (prefab == null) continue;

                if (!prefab.AllowConsecutive && prefab == lastSpawnedPrefab)
                    continue;

                float difficultyDelta = Mathf.Abs(prefab.DifficultyWeight - CurrentDifficulty);
                if (difficultyDelta < 0.5f)
                {
                    validPrefabs.Add(prefab);
                }
            }

            if (validPrefabs.Count == 0)
            {
                return levelPrefabs[Random.Range(0, levelPrefabs.Length)];
            }

            return validPrefabs[Random.Range(0, validPrefabs.Count)];
        }

        private LevelSegment GetOrCreateSegment(LevelSegment prefab)
        {
            if (useObjectPooling && segmentPools.TryGetValue(prefab, out var pool) && pool.Count > 0)
            {
                return pool.Dequeue();
            }

            var segment = Instantiate(prefab, transform);
            segment.name = $"{prefab.name}_Instance";
            return segment;
        }

        private void DespawnSegment(ActiveSegment segment)
        {
            segment.Instance.ResetSegment();
            segment.Instance.gameObject.SetActive(false);

            if (useObjectPooling && segmentPools.TryGetValue(segment.Prefab, out var pool))
            {
                pool.Enqueue(segment.Instance);
            }
            else
            {
                Destroy(segment.Instance.gameObject);
            }
        }

        public void ResetGenerator()
        {
            if (enemySpawner != null)
            {
                enemySpawner.DespawnAllEnemies();
            }

            foreach (var segment in activeSegments)
            {
                if (segment.Instance != null)
                {
                    segment.Instance.gameObject.SetActive(false);

                    if (useObjectPooling && segmentPools.TryGetValue(segment.Prefab, out var pool))
                    {
                        pool.Enqueue(segment.Instance);
                    }
                }
            }

            activeSegments.Clear();
            nextSpawnZ = 0f;
            lastSpawnedPrefab = null;

            GenerateInitialSegments();

            if (showDebug)
            {
                Debug.Log("[LevelGenerator] Reset complete");
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebug) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(0f, 0f, nextSpawnZ), 1f);

            if (player != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(player.position, player.position + Vector3.forward * viewDistance);

                Gizmos.color = Color.red;
                Gizmos.DrawLine(player.position, player.position - Vector3.forward * despawnDistance);
            }
        }
#endif
    }
}