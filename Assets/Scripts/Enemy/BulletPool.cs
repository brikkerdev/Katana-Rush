using UnityEngine;
using System.Collections.Generic;

namespace Runner.Enemy
{
    public class BulletPool : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private Bullet normalBulletPrefab;
        [SerializeField] private Bullet lethalBulletPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int normalPoolSize = 30;
        [SerializeField] private int lethalPoolSize = 10;

        [Header("Performance")]
        [SerializeField] private int maxActiveBullets = 50;
        [SerializeField] private float cleanupInterval = 1f;

        private Bullet[] normalPool;
        private Bullet[] lethalPool;
        private int normalPoolIndex;
        private int lethalPoolIndex;

        private List<Bullet> activeBullets;
        private Transform poolContainer;
        private float cleanupTimer;

        private static BulletPool instance;
        public static BulletPool Instance => instance;

        public int ActiveBulletCount => activeBullets?.Count ?? 0;

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
            activeBullets = new List<Bullet>(maxActiveBullets);

            poolContainer = new GameObject("BulletPoolContainer").transform;
            poolContainer.SetParent(transform);

            if (normalBulletPrefab != null)
            {
                normalPool = new Bullet[normalPoolSize];

                for (int i = 0; i < normalPoolSize; i++)
                {
                    Bullet bullet = Instantiate(normalBulletPrefab, poolContainer);
                    bullet.gameObject.SetActive(false);
                    normalPool[i] = bullet;
                }

                normalPoolIndex = 0;
            }

            if (lethalBulletPrefab != null)
            {
                lethalPool = new Bullet[lethalPoolSize];

                for (int i = 0; i < lethalPoolSize; i++)
                {
                    Bullet bullet = Instantiate(lethalBulletPrefab, poolContainer);
                    bullet.gameObject.SetActive(false);
                    lethalPool[i] = bullet;
                }

                lethalPoolIndex = 0;
            }
        }

        private void Update()
        {
            cleanupTimer -= Time.deltaTime;

            if (cleanupTimer <= 0f)
            {
                cleanupTimer = cleanupInterval;
                CleanupInactiveBullets();
            }
        }

        private void CleanupInactiveBullets()
        {
            for (int i = activeBullets.Count - 1; i >= 0; i--)
            {
                Bullet bullet = activeBullets[i];

                if (bullet == null || !bullet.IsActive)
                {
                    activeBullets.RemoveAt(i);
                }
            }
        }

        public Bullet GetBullet(bool lethal = false)
        {
            if (activeBullets.Count >= maxActiveBullets)
            {
                DeactivateOldestBullet();
            }

            Bullet[] pool = lethal ? lethalPool : normalPool;
            ref int index = ref (lethal ? ref lethalPoolIndex : ref normalPoolIndex);

            if (pool == null || pool.Length == 0) return null;

            int startIndex = index;

            do
            {
                Bullet bullet = pool[index];
                index = (index + 1) % pool.Length;

                if (bullet != null && !bullet.IsActive)
                {
                    activeBullets.Add(bullet);
                    return bullet;
                }
            }
            while (index != startIndex);

            return null;
        }

        private void DeactivateOldestBullet()
        {
            if (activeBullets.Count == 0) return;

            Bullet oldest = activeBullets[0];
            if (oldest != null)
            {
                oldest.Deactivate();
            }
            activeBullets.RemoveAt(0);
        }

        public List<Bullet> GetDeflectedBullets()
        {
            List<Bullet> deflected = new List<Bullet>();

            for (int i = 0; i < activeBullets.Count; i++)
            {
                Bullet bullet = activeBullets[i];

                if (bullet != null && bullet.IsActive && bullet.IsDeflected)
                {
                    deflected.Add(bullet);
                }
            }

            return deflected;
        }

        public int GetDeflectedBulletCount()
        {
            int count = 0;

            for (int i = 0; i < activeBullets.Count; i++)
            {
                Bullet bullet = activeBullets[i];

                if (bullet != null && bullet.IsActive && bullet.IsDeflected)
                {
                    count++;
                }
            }

            return count;
        }

        public void DespawnBulletsBeforeZ(float z)
        {
            for (int i = activeBullets.Count - 1; i >= 0; i--)
            {
                Bullet bullet = activeBullets[i];

                if (bullet == null)
                {
                    activeBullets.RemoveAt(i);
                    continue;
                }

                if (bullet.transform.position.z < z)
                {
                    bullet.Deactivate();
                    activeBullets.RemoveAt(i);
                }
            }
        }

        public void ReturnAllBullets()
        {
            for (int i = activeBullets.Count - 1; i >= 0; i--)
            {
                Bullet bullet = activeBullets[i];

                if (bullet != null)
                {
                    bullet.Deactivate();
                }
            }

            activeBullets.Clear();
        }
    }
}