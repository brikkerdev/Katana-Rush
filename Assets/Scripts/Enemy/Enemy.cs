using UnityEngine;
using System;
using Runner.Core;

namespace Runner.Enemy
{
    public enum EnemyType
    {
        Static,
        Patrol,
        Chase,
        Shooter,
        Sniper
    }

    public class Enemy : MonoBehaviour
    {
        [Header("Base Settings")]
        [SerializeField] private EnemyType enemyType = EnemyType.Static;
        [SerializeField] private float maxHealth = 1f;
        [SerializeField] private float collisionRadius = 0.5f;

        [Header("Shooting Settings")]
        [SerializeField] private bool canShoot = false;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRange = 30f;
        [SerializeField] private float fireRate = 1f;
        [SerializeField] private float fireDelay = 0.5f;
        [SerializeField] private float bulletSpeed = 20f;
        [SerializeField] private bool shootsLethalBullets = false;

        [Header("Aim Prediction")]
        [SerializeField] private bool usePrediction = true;
        [SerializeField] private AimPredictionSettings predictionSettings;

        [Header("Visual")]
        [SerializeField] private GameObject visualRoot;

        [Header("Effects")]
        [SerializeField] private ParticleSystem deathEffect;
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private ParticleSystem muzzleFlash;

        private bool isDead;
        private float currentHealth;
        private Vector3 spawnPosition;

        private Transform playerTarget;
        private float fireTimer;
        private float initialDelay;
        private bool canFire;
        private EnemyAnimator enemyAnimator;
        private EnemyAimPredictor aimPredictor;
        private Transform cachedTransform;
        private Vector3 cachedFirePosition;

        private float sqrFireRange;

        public EnemyType Type => enemyType;
        public bool IsDead => isDead;
        public float CollisionRadius => collisionRadius;

        public event Action<Enemy> OnDeath;
        public event Action<Enemy> OnHit;

        private void Awake()
        {
            cachedTransform = transform;
            currentHealth = maxHealth;
            sqrFireRange = fireRange * fireRange;

            if (visualRoot == null && cachedTransform.childCount > 0)
            {
                visualRoot = cachedTransform.GetChild(0).gameObject;
            }

            enemyAnimator = GetComponentInChildren<EnemyAnimator>();

            if (predictionSettings == null)
            {
                predictionSettings = new AimPredictionSettings();
            }

            if (enemyType == EnemyType.Shooter || enemyType == EnemyType.Sniper)
            {
                canShoot = true;
            }

            if (enemyType == EnemyType.Sniper)
            {
                shootsLethalBullets = true;
                predictionSettings.predictionAccuracy = 0.95f;
                predictionSettings.randomSpread = 0.05f;
            }
        }

        private void Start()
        {
            if (enemyAnimator != null)
            {
                enemyAnimator.Initialize(this);
            }

            SetupAimPredictor();
        }

        private void SetupAimPredictor()
        {
            if (!usePrediction || !canShoot) return;

            aimPredictor = GetComponent<EnemyAimPredictor>();
            if (aimPredictor == null)
            {
                aimPredictor = gameObject.AddComponent<EnemyAimPredictor>();
            }
        }

        private void Update()
        {
            if (isDead || !canShoot || playerTarget == null) return;
            if (Game.Instance.State != GameState.Playing)
            {
                return;
            }

            UpdateShooting();
        }

        private void UpdateShooting()
        {
            if (!canFire)
            {
                initialDelay -= Time.deltaTime;
                if (initialDelay <= 0f)
                {
                    canFire = true;
                }
                return;
            }

            Vector3 toPlayer = playerTarget.position - cachedTransform.position;

            if (toPlayer.sqrMagnitude > sqrFireRange) return;

            float dot = Vector3.Dot(-cachedTransform.forward, toPlayer.normalized);
            if (dot < 0f) return;

            fireTimer -= Time.deltaTime;

            if (fireTimer <= 0f)
            {
                Fire();
                fireTimer = 1f / fireRate;
            }
        }

        private void Fire()
        {
            if (BulletPool.Instance == null || playerTarget == null) return;

            Bullet bullet = BulletPool.Instance.GetBullet(shootsLethalBullets);
            if (bullet == null) return;

            cachedFirePosition = firePoint != null ? firePoint.position : cachedTransform.position + Vector3.up;

            Vector3 direction;

            if (usePrediction && aimPredictor != null && aimPredictor.IsReady())
            {
                direction = aimPredictor.GetAimDirection(cachedFirePosition, bulletSpeed);
            }
            else
            {
                Vector3 targetPos = playerTarget.position;
                targetPos.y += 1f;
                direction = (targetPos - cachedFirePosition).normalized;

                float spread = predictionSettings.randomSpread;
                direction.x += UnityEngine.Random.Range(-spread, spread);
                direction.y += UnityEngine.Random.Range(-spread * 0.5f, spread * 0.5f);
                direction.Normalize();
            }

            bullet.Setup(cachedFirePosition, direction, shootsLethalBullets);
            bullet.SetTarget(playerTarget);
            bullet.SetSourceEnemy(this);

            if (muzzleFlash != null)
            {
                muzzleFlash.transform.position = cachedFirePosition;
                muzzleFlash.transform.rotation = Quaternion.LookRotation(direction);
                muzzleFlash.Play();
            }

            enemyAnimator?.PlayFireAnimation();
        }

        public void Setup(Vector3 position, Quaternion rotation)
        {
            spawnPosition = position;
            cachedTransform.position = position;
            cachedTransform.rotation = rotation;

            isDead = false;
            currentHealth = maxHealth;
            fireTimer = 0f;
            initialDelay = fireDelay;
            canFire = false;

            FindPlayer();

            if (visualRoot != null)
            {
                visualRoot.SetActive(true);
            }

            gameObject.SetActive(true);
            enemyAnimator?.ResetAnimator();
        }

        private void FindPlayer()
        {
            if (playerTarget != null) return;

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;

                if (aimPredictor != null)
                {
                    aimPredictor.Initialize(playerTarget);
                }
            }
        }

        public void SetPlayer(Transform player)
        {
            playerTarget = player;
            aimPredictor?.Initialize(player);
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;

            OnHit?.Invoke(this);

            if (hitEffect != null)
            {
                hitEffect.Play();
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Die()
        {
            if (isDead) return;

            isDead = true;

            if (deathEffect != null)
            {
                deathEffect.Play();
            }

            OnDeath?.Invoke(this);

            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }

            Invoke(nameof(Deactivate), 0.1f);
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }

        public void ResetEnemy()
        {
            CancelInvoke();

            isDead = false;
            currentHealth = maxHealth;
            fireTimer = 0f;
            initialDelay = fireDelay;
            canFire = false;

            if (visualRoot != null)
            {
                visualRoot.SetActive(true);
            }

            enemyAnimator?.ResetAnimator();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isDead ? Color.gray : Color.red;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);

            if (canShoot)
            {
                Gizmos.color = shootsLethalBullets ? new Color(1f, 0f, 0f, 0.3f) : new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, fireRange);

                if (firePoint != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(firePoint.position, 0.1f);
                    Gizmos.DrawRay(firePoint.position, transform.forward * 2f);

                    if (usePrediction && aimPredictor != null && playerTarget != null)
                    {
                        aimPredictor.DrawPredictionGizmos(firePoint.position, bulletSpeed);
                    }
                }
            }
        }
#endif
    }
}