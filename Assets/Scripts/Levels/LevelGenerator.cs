using UnityEngine;
using System.Collections.Generic;
using Runner.Enemy;
using Runner.Collectibles;
using Runner.Environment;

namespace Runner.LevelGeneration
{
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Generation")]
        [SerializeField] private float viewDistance = 100f;
        [SerializeField] private float despawnDistance = 30f;
        [SerializeField] private int initialSegmentCount = 5;

        [Header("Reveal Animation")]
        [SerializeField] private bool enableRevealAnimation = true;
        [SerializeField] private float revealStaggerBetweenSegments = 0.1f;
        [SerializeField] private float initialRevealDelay = 0.2f;

        [Header("Pooling")]
        [SerializeField] private bool usePooling = true;
        [SerializeField] private int poolSizePerSegment = 3;

        [Header("Spawners")]
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] public CollectibleSpawner collectibleSpawner;
        [SerializeField] public ObstacleSpawner obstacleSpawner;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private Transform player;
        private BiomeManager biomeManager;
        private float nextSpawnZ;
        private LevelSegment lastSpawnedSegment;
        private bool isInitialized;
        private int spawnCounter;

        private List<ActiveSegment> activeSegments = new List<ActiveSegment>();
        private Dictionary<LevelSegment, Queue<LevelSegment>> segmentPools =
            new Dictionary<LevelSegment, Queue<LevelSegment>>();

        private struct ActiveSegment
        {
            public LevelSegment Instance;
            public LevelSegment Prefab;
            public float StartZ;
            public float EndZ;
        }

        public bool IsInitialized => isInitialized;

        public void Initialize(Transform playerTransform, BiomeManager manager)
        {
            player = playerTransform;
            biomeManager = manager;
            spawnCounter = 0;

            FindSpawners();
            GenerateInitialSegments();

            isInitialized = true;

            if (showDebug)
            {
                Debug.Log($"[LevelGenerator] Initialized with {activeSegments.Count} segments");
            }
        }

        private void FindSpawners()
        {
            if (enemySpawner == null)
                enemySpawner = FindFirstObjectByType<EnemySpawner>();

            if (collectibleSpawner == null)
                collectibleSpawner = FindFirstObjectByType<CollectibleSpawner>();
            
            if (obstacleSpawner == null)
                obstacleSpawner = FindFirstObjectByType<ObstacleSpawner>();
        }

        private void GenerateInitialSegments()
        {
            spawnCounter = 0;

            for (int i = 0; i < initialSegmentCount; i++)
            {
                SpawnNextSegment(isInitialSpawn: true);
            }

            spawnCounter = 0;
        }

        private void Update()
        {
            if (!isInitialized || player == null) return;

            SpawnSegmentsAhead();
            DespawnSegmentsBehind();
        }

        private void SpawnSegmentsAhead()
        {
            float threshold = player.position.z + viewDistance;
            int safety = 50;

            while (nextSpawnZ < threshold && safety > 0)
            {
                SpawnNextSegment(isInitialSpawn: false);
                safety--;
            }
        }

        private void DespawnSegmentsBehind()
        {
            float threshold = player.position.z - despawnDistance;

            while (activeSegments.Count > 0 && activeSegments[0].EndZ < threshold)
            {
                DespawnSegment(activeSegments[0]);
                activeSegments.RemoveAt(0);
            }

            enemySpawner?.DespawnEnemiesBeforeZ(threshold);
            collectibleSpawner?.DespawnCollectiblesBeforeZ(threshold);
            obstacleSpawner?.DespawnObstaclesBeforeZ(threshold);
        }

        private void SpawnNextSegment(bool isInitialSpawn)
        {
            if (biomeManager == null)
            {
                Debug.LogError("[LevelGenerator] No BiomeManager!");
                return;
            }

            LevelSegment prefab = biomeManager.GetNextSegment(lastSpawnedSegment);

            if (prefab == null)
            {
                Debug.LogError("[LevelGenerator] No segment to spawn!");
                return;
            }

            EnsureSegmentPooled(prefab);

            LevelSegment instance = GetSegmentFromPool(prefab);
            instance.transform.position = new Vector3(0f, 0f, nextSpawnZ);
            instance.transform.rotation = Quaternion.identity;
            instance.gameObject.SetActive(true);

            if (enableRevealAnimation)
            {
                float delay;

                if (isInitialSpawn)
                {
                    delay = initialRevealDelay + spawnCounter * revealStaggerBetweenSegments;
                }
                else
                {
                    delay = 0f;
                }

                PlaySegmentReveal(instance, delay);
                spawnCounter++;
            }

            var active = new ActiveSegment
            {
                Instance = instance,
                Prefab = prefab,
                StartZ = nextSpawnZ,
                EndZ = nextSpawnZ + instance.Length
            };

            activeSegments.Add(active);
            SpawnSegmentContent(instance);

            if (showDebug)
            {
                string biomeInfo = biomeManager.CurrentBiome?.BiomeName ?? "Unknown";
                Debug.Log($"[LevelGenerator] Spawned {prefab.name} at Z={nextSpawnZ} ({biomeInfo})");
            }

            nextSpawnZ += instance.Length;
            lastSpawnedSegment = prefab;
        }

        private void PlaySegmentReveal(LevelSegment segment, float delay)
        {
            SegmentReveal reveal = segment.GetComponent<SegmentReveal>();

            if (reveal == null)
            {
                reveal = segment.gameObject.AddComponent<SegmentReveal>();
            }

            reveal.PlayReveal(delay);
        }

        private void EnsureSegmentPooled(LevelSegment prefab)
        {
            if (!usePooling) return;
            if (segmentPools.ContainsKey(prefab)) return;

            segmentPools[prefab] = new Queue<LevelSegment>();

            for (int i = 0; i < poolSizePerSegment; i++)
            {
                var instance = Instantiate(prefab, transform);
                instance.gameObject.SetActive(false);
                instance.name = $"{prefab.name}_Pooled";
                segmentPools[prefab].Enqueue(instance);
            }
        }

        private LevelSegment GetSegmentFromPool(LevelSegment prefab)
        {
            if (usePooling && segmentPools.TryGetValue(prefab, out var pool) && pool.Count > 0)
            {
                return pool.Dequeue();
            }

            var instance = Instantiate(prefab, transform);
            instance.name = $"{prefab.name}_Instance";
            return instance;
        }

        private void ReturnSegmentToPool(ActiveSegment segment)
        {
            SegmentReveal reveal = segment.Instance.GetComponent<SegmentReveal>();
            reveal?.ResetReveal();

            segment.Instance.ResetSegment();
            segment.Instance.gameObject.SetActive(false);

            if (usePooling && segmentPools.TryGetValue(segment.Prefab, out var pool))
            {
                pool.Enqueue(segment.Instance);
            }
            else
            {
                Destroy(segment.Instance.gameObject);
            }
        }

        private void SpawnSegmentContent(LevelSegment segment)
        {
            float difficulty = biomeManager?.CurrentBiome?.EnemySpawnMultiplier ?? 1f;

            enemySpawner?.SpawnEnemiesForSegment(segment, player.position.z * difficulty);
            collectibleSpawner?.SpawnCollectiblesForSegment(segment);
            obstacleSpawner?.SpawnObstaclesForSegment(segment);
        }

        private void DespawnSegment(ActiveSegment segment)
        {
            ReturnSegmentToPool(segment);
        }

        public void Reset()
        {
            Reset(0f);
        }

        public void Reset(float startZ)
        {
            enemySpawner?.DespawnAllEnemies();
            collectibleSpawner?.DespawnAllCollectibles();
            obstacleSpawner?.DespawnAllObstacles();

            foreach (var segment in activeSegments)
            {
                ReturnSegmentToPool(segment);
            }
            activeSegments.Clear();

            nextSpawnZ = startZ;
            lastSpawnedSegment = null;
            spawnCounter = 0;

            GenerateInitialSegments();

            if (showDebug)
            {
                Debug.Log($"[LevelGenerator] Reset complete at Z={startZ}");
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebug) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(0f, 1f, nextSpawnZ), 1f);

            if (player != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 viewEnd = player.position + Vector3.forward * viewDistance;
                Gizmos.DrawLine(player.position, viewEnd);

                Gizmos.color = Color.red;
                Vector3 despawnEnd = player.position - Vector3.forward * despawnDistance;
                Gizmos.DrawLine(player.position, despawnEnd);
            }

            foreach (var segment in activeSegments)
            {
                Gizmos.color = Color.white;
                Vector3 center = new Vector3(0f, 0.5f, (segment.StartZ + segment.EndZ) / 2f);
                Vector3 size = new Vector3(10f, 0.1f, segment.EndZ - segment.StartZ);
                Gizmos.DrawWireCube(center, size);
            }
        }
#endif
    }
}