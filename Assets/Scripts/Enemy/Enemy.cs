using UnityEngine;
using System;
using Runner.Core;
using Runner.Effects;

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
        [SerializeField] private Animator animator;

        [Header("Ragdoll")]
        [SerializeField] private bool useRagdoll = true;
        [SerializeField] private EnemyRagdoll ragdoll;
        [SerializeField] private SimpleEnemyRagdoll simpleRagdoll;

        private bool isDead;
        private float currentHealth;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Vector3 lastHitDirection;
        private bool hasBeenSetup;

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
                visualRoot = cachedTransform.GetChild(0).gameObject;

            enemyAnimator = GetComponentInChildren<EnemyAnimator>();

            if (ragdoll == null)
                ragdoll = GetComponent<EnemyRagdoll>();

            if (simpleRagdoll == null)
                simpleRagdoll = GetComponent<SimpleEnemyRagdoll>();

            if (predictionSettings == null)
                predictionSettings = new AimPredictionSettings();

            if (enemyType == EnemyType.Shooter || enemyType == EnemyType.Sniper)
                canShoot = true;

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
                enemyAnimator.Initialize(this);

            SetupAimPredictor();
        }

        private void SetupAimPredictor()
        {
            if (!usePrediction || !canShoot) return;

            aimPredictor = GetComponent<EnemyAimPredictor>();
            if (aimPredictor == null)
                aimPredictor = gameObject.AddComponent<EnemyAimPredictor>();
        }

        private void Update()
        {
            if (isDead || !canShoot || playerTarget == null) return;
            if (Game.Instance == null || Game.Instance.State != GameState.Playing) return;
            UpdateShooting();
        }

        private void UpdateShooting()
        {
            if (!canFire)
            {
                initialDelay -= Time.deltaTime;
                if (initialDelay <= 0f) canFire = true;
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

            if (ParticleController.Instance != null && firePoint != null)
                ParticleController.Instance.SpawnMuzzleFlash(firePoint, direction);

            if (enemyType == EnemyType.Sniper)
                Game.Instance?.Sound?.PlaySniperFire(cachedFirePosition);
            else
                Game.Instance?.Sound?.PlayBulletFire(cachedFirePosition, shootsLethalBullets);

            enemyAnimator?.PlayFireAnimation();
        }

        public void Setup(Vector3 position, Quaternion rotation)
        {
            ResetEnemy();

            spawnPosition = position;
            spawnRotation = rotation;
            cachedTransform.position = position;
            cachedTransform.rotation = rotation;

            isDead = false;
            currentHealth = maxHealth;
            fireTimer = 0f;
            initialDelay = fireDelay;
            canFire = false;
            lastHitDirection = Vector3.forward;

            FindPlayer();

            if (visualRoot != null) visualRoot.SetActive(true);

            if (animator != null)
            {
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);
            }

            gameObject.SetActive(true);
            hasBeenSetup = true;
        }

        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) return;

            playerTarget = playerObj.transform;
            aimPredictor?.Initialize(playerTarget);
        }

        public void SetPlayer(Transform player)
        {
            playerTarget = player;
            aimPredictor?.Initialize(player);
        }

        public void TakeDamage(float damage, Vector3 hitDirection)
        {
            if (isDead) return;

            currentHealth -= damage;
            lastHitDirection = hitDirection;

            OnHit?.Invoke(this);

            Game.Instance?.Sound?.PlayEnemyHit(cachedTransform.position);

            if (ParticleController.Instance != null)
            {
                Vector3 hitPosition = cachedTransform.position + Vector3.up;
                ParticleController.Instance.SpawnBloodEffect(hitPosition, hitDirection);
                ParticleController.Instance.SpawnHitEffect(hitPosition, hitDirection);
            }

            if (currentHealth <= 0f)
                Die(hitDirection);
        }

        public void TakeDamage(float damage)
        {
            Vector3 direction = playerTarget != null
                ? (cachedTransform.position - playerTarget.position).normalized
                : Vector3.forward;

            TakeDamage(damage, direction);
        }

        public void Die(Vector3 hitDirection)
        {
            if (isDead) return;

            isDead = true;
            lastHitDirection = hitDirection;

            if (animator != null) animator.enabled = false;

            Game.Instance?.Sound?.PlayEnemyDeath(cachedTransform.position);

            if (useRagdoll)
            {
                ActivateRagdoll(hitDirection);
                Game.Instance?.Sound?.PlayEnemyRagdoll(cachedTransform.position);
            }
            else if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }

            if (ParticleController.Instance != null)
            {
                Vector3 deathPosition = cachedTransform.position + Vector3.up * 0.5f;
                ParticleController.Instance.SpawnDeathEffect(deathPosition, hitDirection);
            }

            OnDeath?.Invoke(this);

            if (!useRagdoll)
                Invoke(nameof(Deactivate), 0.1f);
        }

        public void Die() => Die(lastHitDirection);

        private void ActivateRagdoll(Vector3 hitDirection)
        {
            if (ragdoll != null) ragdoll.ActivateRagdoll(hitDirection, 1f);
            else if (simpleRagdoll != null) simpleRagdoll.Activate(hitDirection);
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
            lastHitDirection = Vector3.forward;

            ragdoll?.ResetRagdoll();
            simpleRagdoll?.ResetRagdoll();

            if (visualRoot != null) visualRoot.SetActive(true);

            if (animator != null)
            {
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);
            }

            enemyAnimator?.ResetAnimator();

            if (hasBeenSetup)
            {
                cachedTransform.position = spawnPosition;
                cachedTransform.rotation = spawnRotation;
            }
        }

        public void FullReset()
        {
            ResetEnemy();
            playerTarget = null;
            hasBeenSetup = false;
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
                    Gizmos.DrawRay(firePoint.position, -transform.forward * 2f);

                    if (usePrediction && aimPredictor != null && playerTarget != null)
                        aimPredictor.DrawPredictionGizmos(firePoint.position, bulletSpeed);
                }
            }
        }
#endif
    }
}