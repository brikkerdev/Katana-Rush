using UnityEngine;
using System.Collections.Generic;
using Runner.LevelGeneration;

namespace Runner.Environment
{
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("Obstacle Prefabs")]
        [SerializeField] private DestructibleObstacle[] obstaclePrefabs;

        [Header("Pool Settings")]
        [SerializeField] private int poolSizePerType = 10;

        [Header("Spawn Settings")]
        [SerializeField] private int minObstaclesPerSegment = 1;
        [SerializeField] private int maxObstaclesPerSegment = 3;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private Queue<DestructibleObstacle> obstaclePool;
        private List<DestructibleObstacle> activeObstacles;
        private Transform poolContainer;
        private bool isInitialized;

        public IReadOnlyList<DestructibleObstacle> ActiveObstacles => activeObstacles;
        public int ActiveCount => activeObstacles != null ? activeObstacles.Count : 0;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized) return;

            obstaclePool = new Queue<DestructibleObstacle>();
            activeObstacles = new List<DestructibleObstacle>();

            poolContainer = new GameObject("ObstaclePool").transform;
            poolContainer.SetParent(transform);

            InitializePool();
            isInitialized = true;

            if (showDebug)
            {
                Debug.Log($"[ObstacleSpawner] Initialized with {obstaclePrefabs.Length} prefab types");
            }
        }

        private void InitializePool()
        {
            if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
            {
                Debug.LogWarning("[ObstacleSpawner] No obstacle prefabs assigned!");
                return;
            }

            foreach (var prefab in obstaclePrefabs)
            {
                if (prefab == null) continue;

                for (int i = 0; i < poolSizePerType; i++)
                {
                    DestructibleObstacle obstacle = Instantiate(prefab, poolContainer);
                    obstacle.gameObject.SetActive(false);
                    obstacle.name = $"{prefab.name}_Pool_{i}";
                    obstaclePool.Enqueue(obstacle);
                }

                if (showDebug)
                {
                    Debug.Log($"[ObstacleSpawner] Created pool for {prefab.name}: {poolSizePerType} obstacles");
                }
            }
        }

        public void SpawnObstaclesForSegment(LevelSegment segment)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (segment == null)
            {
                Debug.LogWarning("[ObstacleSpawner] Segment is null!");
                return;
            }

            ObstacleSpawnPoint[] spawnPoints = segment.ObstacleSpawnPoints;

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                spawnPoints = segment.GetComponentsInChildren<ObstacleSpawnPoint>(true);
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                if (showDebug)
                {
                    Debug.Log($"[ObstacleSpawner] No spawn points in segment {segment.name}");
                }
                return;
            }

            int obstaclesToSpawn = Random.Range(minObstaclesPerSegment, maxObstaclesPerSegment + 1);

            List<ObstacleSpawnPoint> availablePoints = new List<ObstacleSpawnPoint>(spawnPoints);
            ShuffleList(availablePoints);

            int spawned = 0;

            if (showDebug)
            {
                Debug.Log($"[ObstacleSpawner] Segment {segment.name}: {availablePoints.Count} spawn points, target {obstaclesToSpawn} obstacles");
            }

            foreach (var point in availablePoints)
            {
                if (point == null) continue;

                if (spawned >= obstaclesToSpawn && !point.AlwaysSpawn) continue;

                bool shouldSpawn = point.AlwaysSpawn || Random.value <= point.SpawnChance;

                if (shouldSpawn)
                {
                    bool success = SpawnObstacleAtPoint(point);
                    if (success)
                    {
                        spawned++;
                    }
                }
            }

            if (showDebug)
            {
                Debug.Log($"[ObstacleSpawner] Spawned {spawned} obstacles in segment {segment.name}");
            }
        }

        private bool SpawnObstacleAtPoint(ObstacleSpawnPoint point)
        {
            if (point == null) return false;

            DestructibleObstacle obstacle = GetObstacleFromPool();

            if (obstacle == null)
            {
                if (showDebug)
                {
                    Debug.LogWarning("[ObstacleSpawner] No obstacle available from pool!");
                }
                return false;
            }

            Vector3 worldPosition = point.Position;
            Quaternion worldRotation = point.Rotation;

            obstacle.transform.position = worldPosition;
            obstacle.transform.rotation = worldRotation;
            obstacle.gameObject.SetActive(true);
            obstacle.OnObstacleDestroyed -= HandleObstacleDestroyed;
            obstacle.OnObstacleDestroyed += HandleObstacleDestroyed;

            activeObstacles.Add(obstacle);

            if (showDebug)
            {
                Debug.Log($"[ObstacleSpawner] Spawned {obstacle.name} at {worldPosition}");
            }

            return true;
        }

        private DestructibleObstacle GetObstacleFromPool()
        {
            if (obstaclePool.Count > 0)
            {
                return obstaclePool.Dequeue();
            }

            // Pool is empty - expand it
            if (obstaclePrefabs != null && obstaclePrefabs.Length > 0 && obstaclePrefabs[0] != null)
            {
                DestructibleObstacle newObstacle = Instantiate(obstaclePrefabs[0], poolContainer);
                newObstacle.gameObject.SetActive(false);
                return newObstacle;
            }

            if (showDebug)
            {
                Debug.LogWarning("[ObstacleSpawner] Pool is empty and cannot expand!");
            }

            return null;
        }

        private void ReturnObstacleToPool(DestructibleObstacle obstacle)
        {
            if (obstacle == null) return;

            obstacle.OnObstacleDestroyed -= HandleObstacleDestroyed;

            obstacle.gameObject.SetActive(false);
            obstacle.transform.SetParent(poolContainer);

            activeObstacles.Remove(obstacle);
            obstaclePool.Enqueue(obstacle);
        }

        private void HandleObstacleDestroyed(int scoreReward)
        {
            // Score is already handled in DestructibleObstacle
            // This callback can be used for additional effects if needed
        }

        public void DespawnObstaclesBeforeZ(float z)
        {
            for (int i = activeObstacles.Count - 1; i >= 0; i--)
            {
                if (activeObstacles[i] == null)
                {
                    activeObstacles.RemoveAt(i);
                    continue;
                }

                if (activeObstacles[i].transform.position.z < z)
                {
                    ReturnObstacleToPool(activeObstacles[i]);
                }
            }
        }

        public void DespawnAllObstacles()
        {
            for (int i = activeObstacles.Count - 1; i >= 0; i--)
            {
                if (activeObstacles[i] != null)
                {
                    ReturnObstacleToPool(activeObstacles[i]);
                }
            }

            activeObstacles.Clear();
        }

        public void ResetAllObstacles()
        {
            for (int i = activeObstacles.Count - 1; i >= 0; i--)
            {
                if (activeObstacles[i] != null)
                {
                    activeObstacles[i].gameObject.SetActive(false);
                    ReturnObstacleToPool(activeObstacles[i]);
                }
            }

            activeObstacles.Clear();

            if (showDebug)
            {
                Debug.Log("[ObstacleSpawner] All obstacles reset");
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
            if (activeObstacles == null) return;

            foreach (var obstacle in activeObstacles)
            {
                if (obstacle == null) continue;

                Gizmos.color = obstacle.IsDestroyed ? Color.gray : new Color(1f, 0.5f, 0f);
                Gizmos.DrawWireSphere(obstacle.transform.position, 0.5f);
            }
        }
#endif
    }
}
