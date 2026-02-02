using UnityEngine;
using System.Collections.Generic;
using Runner.LevelGeneration;

namespace Runner.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Enemy Prefabs")]
        [SerializeField] private Enemy[] enemyPrefabs;

        [Header("Pool Settings")]
        [SerializeField] private int poolSizePerType = 15;

        [Header("Spawn Settings")]
        [SerializeField] private int minEnemiesPerSegment = 1;
        [SerializeField] private int maxEnemiesPerSegment = 5;

        [Header("Difficulty")]
        [SerializeField] private float difficultyMultiplier = 1f;
        [SerializeField] private float maxDifficultyDistance = 1000f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private Dictionary<EnemyType, Queue<Enemy>> enemyPools;
        private List<Enemy> activeEnemies;
        private Transform poolContainer;
        private bool isInitialized;

        public IReadOnlyList<Enemy> ActiveEnemies => activeEnemies;
        public int ActiveCount => activeEnemies != null ? activeEnemies.Count : 0;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized) return;

            enemyPools = new Dictionary<EnemyType, Queue<Enemy>>();
            activeEnemies = new List<Enemy>();

            poolContainer = new GameObject("EnemyPool").transform;
            poolContainer.SetParent(transform);

            InitializePools();
            isInitialized = true;

            if (showDebug)
            {
                Debug.Log($"[EnemySpawner] Initialized with {enemyPrefabs.Length} prefab types");
            }
        }

        private void InitializePools()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                Debug.LogWarning("[EnemySpawner] No enemy prefabs assigned!");
                return;
            }

            foreach (var prefab in enemyPrefabs)
            {
                if (prefab == null) continue;

                EnemyType type = prefab.Type;

                if (!enemyPools.ContainsKey(type))
                {
                    enemyPools[type] = new Queue<Enemy>();
                }

                for (int i = 0; i < poolSizePerType; i++)
                {
                    Enemy enemy = Instantiate(prefab, poolContainer);
                    enemy.gameObject.SetActive(false);
                    enemy.name = $"{prefab.name}_Pool_{i}";
                    enemyPools[type].Enqueue(enemy);
                }

                if (showDebug)
                {
                    Debug.Log($"[EnemySpawner] Created pool for {type}: {poolSizePerType} enemies");
                }
            }
        }

        public void SpawnEnemiesForSegment(LevelSegment segment, float playerDistance)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (segment == null)
            {
                Debug.LogWarning("[EnemySpawner] Segment is null!");
                return;
            }

            EnemySpawnPoint[] spawnPoints = segment.EnemySpawnPoints;

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                spawnPoints = segment.GetComponentsInChildren<EnemySpawnPoint>(true);
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                if (showDebug)
                {
                    Debug.Log($"[EnemySpawner] No spawn points in segment {segment.name}");
                }
                return;
            }

            float difficulty = Mathf.Clamp01(playerDistance / maxDifficultyDistance);
            int adjustedMax = Mathf.RoundToInt(maxEnemiesPerSegment * (1f + difficulty * difficultyMultiplier));
            int enemiesToSpawn = Random.Range(minEnemiesPerSegment, adjustedMax + 1);

            List<EnemySpawnPoint> availablePoints = new List<EnemySpawnPoint>(spawnPoints);
            ShuffleList(availablePoints);

            int spawned = 0;

            if (showDebug)
            {
                Debug.Log($"[EnemySpawner] Segment {segment.name}: {availablePoints.Count} spawn points, target {enemiesToSpawn} enemies");
            }

            foreach (var point in availablePoints)
            {
                if (point == null) continue;

                if (spawned >= enemiesToSpawn && !point.AlwaysSpawn) continue;

                bool shouldSpawn = point.AlwaysSpawn || Random.value <= point.SpawnChance;

                if (shouldSpawn)
                {
                    bool success = SpawnEnemyAtPoint(point);
                    if (success)
                    {
                        spawned++;
                    }
                }
            }

            if (showDebug)
            {
                Debug.Log($"[EnemySpawner] Spawned {spawned} enemies in segment {segment.name}");
            }
        }

        private bool SpawnEnemyAtPoint(EnemySpawnPoint point)
        {
            if (point == null) return false;

            Enemy enemy = GetEnemyFromPool(point.AllowedType);

            if (enemy == null)
            {
                if (showDebug)
                {
                    Debug.LogWarning($"[EnemySpawner] No enemy available for type {point.AllowedType}");
                }
                return false;
            }

            Vector3 worldPosition = point.Position;
            Quaternion worldRotation = point.Rotation;

            enemy.OnDeath -= HandleEnemyDeath;
            enemy.OnDeath += HandleEnemyDeath;

            enemy.Setup(worldPosition, worldRotation);

            activeEnemies.Add(enemy);

            if (showDebug)
            {
                Debug.Log($"[EnemySpawner] Spawned {enemy.name} at {worldPosition}");
            }

            return true;
        }

        private Enemy GetEnemyFromPool(EnemyType type)
        {
            if (enemyPools.TryGetValue(type, out Queue<Enemy> pool) && pool.Count > 0)
            {
                return pool.Dequeue();
            }

            foreach (var kvp in enemyPools)
            {
                if (kvp.Value.Count > 0)
                {
                    if (showDebug)
                    {
                        Debug.Log($"[EnemySpawner] Fallback: using {kvp.Key} instead of {type}");
                    }
                    return kvp.Value.Dequeue();
                }
            }

            if (showDebug)
            {
                Debug.LogWarning($"[EnemySpawner] All pools empty!");
            }

            return null;
        }

        private void ReturnEnemyToPool(Enemy enemy)
        {
            if (enemy == null) return;

            enemy.OnDeath -= HandleEnemyDeath;

            enemy.ResetEnemy();

            enemy.gameObject.SetActive(false);

            activeEnemies.Remove(enemy);

            if (enemyPools.TryGetValue(enemy.Type, out Queue<Enemy> pool))
            {
                pool.Enqueue(enemy);
            }
        }

        private void HandleEnemyDeath(Enemy enemy)
        {
            Core.Game.Instance?.AddScore(100);

            if (showDebug)
            {
                Debug.Log($"[EnemySpawner] Enemy died, score added");
            }
        }

        public void DespawnEnemiesBeforeZ(float z)
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] == null)
                {
                    activeEnemies.RemoveAt(i);
                    continue;
                }

                if (activeEnemies[i].transform.position.z < z)
                {
                    ReturnEnemyToPool(activeEnemies[i]);
                }
            }
        }

        public void DespawnAllEnemies()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] != null)
                {
                    ReturnEnemyToPool(activeEnemies[i]);
                }
            }

            activeEnemies.Clear();
        }

        public void ResetAllEnemies()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] != null)
                {
                    activeEnemies[i].ResetEnemy();
                    ReturnEnemyToPool(activeEnemies[i]);
                }
            }

            activeEnemies.Clear();

            foreach (var pool in enemyPools.Values)
            {
                foreach (var enemy in pool)
                {
                    if (enemy != null)
                    {
                        enemy.FullReset();
                    }
                }
            }

            if (showDebug)
            {
                Debug.Log("[EnemySpawner] All enemies reset");
            }
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebug) return;
            if (activeEnemies == null) return;

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null) continue;

                Gizmos.color = enemy.IsDead ? Color.gray : Color.red;
                Gizmos.DrawWireSphere(enemy.transform.position, 0.5f);
            }
        }
#endif
    }
}