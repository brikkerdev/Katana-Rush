using UnityEngine;
using System.Collections.Generic;
using Runner.Player.Core;

namespace Runner.Enemy
{
    public class BulletDeflector : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool enableRedirectAbility = false;
        [SerializeField] private float redirectRange = 15f;
        [SerializeField] private float redirectSpeed = 30f;
        [SerializeField] private LayerMask enemyLayer = -1;

        [Header("Visual")]
        [SerializeField] private ParticleSystem redirectEffect;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip redirectSound;

        private PlayerController controller;
        private Transform cachedTransform;
        private Collider[] enemyBuffer = new Collider[16];

        private void Awake()
        {
            cachedTransform = transform;
        }

        private void Start()
        {
            controller = GetComponent<PlayerController>();
            if (controller == null)
            {
                controller = GetComponentInParent<PlayerController>();
            }
        }

        public void RedirectBulletsToEnemies()
        {
            if (!enableRedirectAbility) return;
            if (BulletPool.Instance == null) return;

            List<Bullet> deflectedBullets = BulletPool.Instance.GetDeflectedBullets();

            if (deflectedBullets.Count == 0) return;

            List<Enemy> nearbyEnemies = FindNearbyEnemies();

            if (nearbyEnemies.Count == 0) return;

            int enemyIndex = 0;

            for (int i = 0; i < deflectedBullets.Count; i++)
            {
                Bullet bullet = deflectedBullets[i];

                if (bullet == null || !bullet.IsActive) continue;

                Enemy targetEnemy = nearbyEnemies[enemyIndex % nearbyEnemies.Count];
                enemyIndex++;

                RedirectBullet(bullet, targetEnemy);
            }

            if (redirectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(redirectSound);
            }

            if (redirectEffect != null)
            {
                redirectEffect.Play();
            }
        }

        private List<Enemy> FindNearbyEnemies()
        {
            List<Enemy> enemies = new List<Enemy>();

            Vector3 center = cachedTransform.position;
            int hitCount = Physics.OverlapSphereNonAlloc(center, redirectRange, enemyBuffer, enemyLayer);

            Vector3 forward = cachedTransform.forward;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = enemyBuffer[i];
                if (col == null) continue;

                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy == null)
                {
                    enemy = col.GetComponentInParent<Enemy>();
                }

                if (enemy == null || enemy.IsDead) continue;

                Vector3 toEnemy = (enemy.transform.position - center).normalized;
                float dot = Vector3.Dot(forward, toEnemy);

                if (dot > 0f)
                {
                    enemies.Add(enemy);
                }
            }

            enemies.Sort((a, b) =>
            {
                float distA = (a.transform.position - center).sqrMagnitude;
                float distB = (b.transform.position - center).sqrMagnitude;
                return distA.CompareTo(distB);
            });

            return enemies;
        }

        private void RedirectBullet(Bullet bullet, Enemy target)
        {
            if (bullet == null || target == null) return;

            Vector3 targetPos = target.transform.position + Vector3.up;
            Vector3 direction = (targetPos - bullet.transform.position).normalized;

            bullet.RedirectToTarget(direction, redirectSpeed, true);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!enableRedirectAbility) return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, redirectRange);
        }
#endif
    }
}