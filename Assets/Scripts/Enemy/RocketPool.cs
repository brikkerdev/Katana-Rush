using UnityEngine;
using System.Collections.Generic;

namespace Runner.Enemy
{
    public class RocketPool : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private Rocket rocketPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int poolSize = 20;

        [Header("Performance")]
        [SerializeField] private int maxActiveRockets = 30;
        [SerializeField] private float cleanupInterval = 1f;

        private Rocket[] rocketPool;
        private int poolIndex;

        private List<Rocket> activeRockets;
        private Transform poolContainer;
        private float cleanupTimer;

        private static RocketPool instance;
        public static RocketPool Instance => instance;

        public int ActiveRocketCount => activeRockets?.Count ?? 0;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Initialize();
        }

        private void Initialize()
        {
            activeRockets = new List<Rocket>(maxActiveRockets);

            poolContainer = new GameObject("RocketPoolContainer").transform;
            poolContainer.SetParent(transform);

            if (rocketPrefab != null)
            {
                rocketPool = new Rocket[poolSize];

                for (int i = 0; i < poolSize; i++)
                {
                    Rocket rocket = Instantiate(rocketPrefab, poolContainer);
                    rocket.gameObject.SetActive(false);
                    rocketPool[i] = rocket;
                }

                poolIndex = 0;
            }
        }

        private void Update()
        {
            cleanupTimer -= Time.deltaTime;

            if (cleanupTimer <= 0f)
            {
                cleanupTimer = cleanupInterval;
                CleanupInactiveRockets();
            }
        }

        private void CleanupInactiveRockets()
        {
            for (int i = activeRockets.Count - 1; i >= 0; i--)
            {
                Rocket rocket = activeRockets[i];

                if (rocket == null || !rocket.IsActive)
                {
                    activeRockets.RemoveAt(i);
                }
            }
        }

        public Rocket GetRocket()
        {
            if (activeRockets.Count >= maxActiveRockets)
            {
                DeactivateOldestRocket();
            }

            if (rocketPool == null || rocketPool.Length == 0) return null;

            int startIndex = poolIndex;

            do
            {
                Rocket rocket = rocketPool[poolIndex];
                poolIndex = (poolIndex + 1) % rocketPool.Length;

                if (rocket != null && !rocket.IsActive)
                {
                    activeRockets.Add(rocket);
                    return rocket;
                }
            }
            while (poolIndex != startIndex);

            return null;
        }

        private void DeactivateOldestRocket()
        {
            if (activeRockets.Count == 0) return;

            Rocket oldest = activeRockets[0];
            if (oldest != null)
            {
                oldest.Deactivate();
            }
            activeRockets.RemoveAt(0);
        }

        public void DespawnRocketsBeforeZ(float z)
        {
            for (int i = activeRockets.Count - 1; i >= 0; i--)
            {
                Rocket rocket = activeRockets[i];

                if (rocket == null)
                {
                    activeRockets.RemoveAt(i);
                    continue;
                }

                if (rocket.transform.position.z < z)
                {
                    rocket.Deactivate();
                    activeRockets.RemoveAt(i);
                }
            }
        }

        public void ReturnAllRockets()
        {
            for (int i = activeRockets.Count - 1; i >= 0; i--)
            {
                Rocket rocket = activeRockets[i];

                if (rocket != null)
                {
                    rocket.Deactivate();
                }
            }

            activeRockets.Clear();
        }
    }
}
