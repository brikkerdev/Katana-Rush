using UnityEngine;
using System.Collections.Generic;
using Runner.LevelGeneration;

namespace Runner.Collectibles
{
    public class CollectibleSpawner : MonoBehaviour
    {
        [Header("Collectible Prefabs")]
        [SerializeField] private Collectible coinPrefab;
        [SerializeField] private Collectible speedBoostPrefab;
        [SerializeField] private Collectible magnetPrefab;
        [SerializeField] private Collectible multiplierPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int coinPoolSize = 100;
        [SerializeField] private int powerUpPoolSize = 10;

        [Header("Spawn Settings")]
        [SerializeField] private float powerUpChance = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private Dictionary<CollectibleType, Queue<Collectible>> collectiblePools;
        private List<Collectible> activeCollectibles;
        private Transform poolContainer;
        private bool isInitialized;

        public IReadOnlyList<Collectible> ActiveCollectibles => activeCollectibles;
        public int ActiveCount => activeCollectibles != null ? activeCollectibles.Count : 0;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized) return;

            collectiblePools = new Dictionary<CollectibleType, Queue<Collectible>>();
            activeCollectibles = new List<Collectible>();

            poolContainer = new GameObject("CollectiblePool").transform;
            poolContainer.SetParent(transform);

            InitializePools();
            isInitialized = true;

            if (showDebug)
            {
                Debug.Log("[CollectibleSpawner] Initialized");
            }
        }

        private void InitializePools()
        {
            // Coins
            if (coinPrefab != null)
            {
                CreatePool(CollectibleType.Coin, coinPrefab, coinPoolSize);
                CreatePool(CollectibleType.CoinGroup, coinPrefab, coinPoolSize); // Use same coin prefab
            }

            // Power-ups
            if (speedBoostPrefab != null)
                CreatePool(CollectibleType.SpeedBoost, speedBoostPrefab, powerUpPoolSize);

            if (magnetPrefab != null)
                CreatePool(CollectibleType.Magnet, magnetPrefab, powerUpPoolSize);

            if (multiplierPrefab != null)
                CreatePool(CollectibleType.Multiplier, multiplierPrefab, powerUpPoolSize);
        }

        private void CreatePool(CollectibleType type, Collectible prefab, int size)
        {
            if (!collectiblePools.ContainsKey(type))
            {
                collectiblePools[type] = new Queue<Collectible>();
            }

            for (int i = 0; i < size; i++)
            {
                Collectible collectible = Instantiate(prefab, poolContainer);
                collectible.gameObject.SetActive(false);
                collectible.name = $"{type}_Pool_{i}";
                collectiblePools[type].Enqueue(collectible);
            }

            if (showDebug)
            {
                Debug.Log($"[CollectibleSpawner] Created pool for {type}: {size} items");
            }
        }

        public void SpawnCollectiblesForSegment(LevelSegment segment)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (segment == null)
            {
                Debug.LogWarning("[CollectibleSpawner] Segment is null!");
                return;
            }

            CollectibleSpawnPoint[] spawnPoints = segment.CollectibleSpawnPoints;

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                spawnPoints = segment.GetComponentsInChildren<CollectibleSpawnPoint>(true);
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                if (showDebug)
                {
                    Debug.Log($"[CollectibleSpawner] No spawn points in segment {segment.name}");
                }
                return;
            }

            int spawned = 0;

            foreach (var point in spawnPoints)
            {
                if (point == null) continue;

                bool shouldSpawn = point.AlwaysSpawn || Random.value <= point.SpawnChance;

                if (!shouldSpawn) continue;

                if (point.Type == CollectibleType.CoinGroup)
                {
                    spawned += SpawnCoinGroup(point);
                }
                else
                {
                    if (SpawnCollectibleAtPoint(point))
                    {
                        spawned++;
                    }
                }
            }

            if (showDebug)
            {
                Debug.Log($"[CollectibleSpawner] Spawned {spawned} collectibles in segment {segment.name}");
            }
        }

        private bool SpawnCollectibleAtPoint(CollectibleSpawnPoint point)
        {
            Collectible collectible = GetCollectibleFromPool(point.Type);

            if (collectible == null)
            {
                // Fallback to coin if specific type unavailable
                if (point.Type != CollectibleType.Coin)
                {
                    collectible = GetCollectibleFromPool(CollectibleType.Coin);
                }

                if (collectible == null)
                {
                    if (showDebug)
                    {
                        Debug.LogWarning($"[CollectibleSpawner] No collectible available for type {point.Type}");
                    }
                    return false;
                }
            }

            collectible.Setup(point.Position);
            activeCollectibles.Add(collectible);

            return true;
        }

        private int SpawnCoinGroup(CollectibleSpawnPoint point)
        {
            Vector3[] positions = point.GetGroupPositions();
            int spawned = 0;

            foreach (var pos in positions)
            {
                Collectible coin = GetCollectibleFromPool(CollectibleType.Coin);

                if (coin == null)
                {
                    if (showDebug)
                    {
                        Debug.LogWarning("[CollectibleSpawner] No coins available for group");
                    }
                    break;
                }

                coin.Setup(pos);
                activeCollectibles.Add(coin);
                spawned++;
            }

            return spawned;
        }

        private Collectible GetCollectibleFromPool(CollectibleType type)
        {
            // Try to get from specific pool
            if (collectiblePools.TryGetValue(type, out Queue<Collectible> pool) && pool.Count > 0)
            {
                return pool.Dequeue();
            }

            // For coin groups, use coin pool
            if (type == CollectibleType.CoinGroup)
            {
                if (collectiblePools.TryGetValue(CollectibleType.Coin, out pool) && pool.Count > 0)
                {
                    return pool.Dequeue();
                }
            }

            return null;
        }

        private void ReturnCollectibleToPool(Collectible collectible)
        {
            if (collectible == null) return;

            collectible.Reset();
            collectible.gameObject.SetActive(false);
            activeCollectibles.Remove(collectible);

            CollectibleType type = collectible.Type;

            // Coins from coin groups go back to coin pool
            if (type == CollectibleType.CoinGroup)
            {
                type = CollectibleType.Coin;
            }

            if (collectiblePools.TryGetValue(type, out Queue<Collectible> pool))
            {
                pool.Enqueue(collectible);
            }
        }

        public void DespawnCollectiblesBeforeZ(float z)
        {
            for (int i = activeCollectibles.Count - 1; i >= 0; i--)
            {
                if (activeCollectibles[i] == null)
                {
                    activeCollectibles.RemoveAt(i);
                    continue;
                }

                if (activeCollectibles[i].transform.position.z < z)
                {
                    ReturnCollectibleToPool(activeCollectibles[i]);
                }
            }
        }

        public void DespawnAllCollectibles()
        {
            for (int i = activeCollectibles.Count - 1; i >= 0; i--)
            {
                if (activeCollectibles[i] != null)
                {
                    ReturnCollectibleToPool(activeCollectibles[i]);
                }
            }

            activeCollectibles.Clear();
        }

        /// <summary>
        /// Get all collectibles within magnet range of a position
        /// </summary>
        public List<Collectible> GetCollectiblesInRange(Vector3 position, float range)
        {
            List<Collectible> inRange = new List<Collectible>();

            foreach (var collectible in activeCollectibles)
            {
                if (collectible == null || collectible.IsCollected) continue;

                float distance = Vector3.Distance(position, collectible.transform.position);
                if (distance <= range)
                {
                    inRange.Add(collectible);
                }
            }

            return inRange;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebug) return;
            if (activeCollectibles == null) return;

            foreach (var collectible in activeCollectibles)
            {
                if (collectible == null) continue;

                Gizmos.color = collectible.IsCollected ? Color.gray : Color.yellow;
                Gizmos.DrawWireSphere(collectible.transform.position, 0.3f);
            }
        }
#endif
    }
}