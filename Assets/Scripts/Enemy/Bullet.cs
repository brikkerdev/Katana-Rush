using UnityEngine;
using Runner.Player;
using Runner.Player.Core;
using Runner.Effects;
using Runner.Inventory;
using Runner.Inventory.Abilities;

namespace Runner.Enemy
{
    public class Bullet : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifetime = 5f;

        [Header("Deflection Settings")]
        [SerializeField] private float deflectedSpeed = 25f;
        [SerializeField] private float deflectedLifetime = 2f;
        [SerializeField] private float deflectionAngleRange = 45f;
        [SerializeField] private bool canDamageEnemiesWhenDeflected = false;

        [Header("Collision")]
        [SerializeField] private float collisionCheckRadius = 0.2f;
        [SerializeField] private LayerMask collisionLayers = -1;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Lethality")]
        [SerializeField] private bool isLethal = false;

        [Header("Whiz Settings")]
        [SerializeField] private float whizDistance = 3f;
        [SerializeField] private float whizCooldown = 0.5f;

        [Header("Visual")]
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material deflectedMaterial;
        [SerializeField] private Material lethalMaterial;

        private Vector3 direction;
        private float lifetimeTimer;
        private float currentSpeed;
        private bool isActive;
        private bool isDeflected;
        private bool wasLethal;
        private Transform cachedTransform;
        private Collider[] hitBuffer = new Collider[8];
        private Collider[] enemySearchBuffer = new Collider[16];

        private Transform targetPlayer;
        private Enemy sourceEnemy;

        private bool hasPlayedWhiz;
        private float whizTimer;

        public bool IsLethal => isLethal;
        public bool IsActive => isActive;
        public bool IsDeflected => isDeflected;
        public bool CanDamageEnemies => canDamageEnemiesWhenDeflected && isDeflected;
        public Vector3 Direction => direction;
        public float Speed => currentSpeed;

        private void Awake()
        {
            cachedTransform = transform;
        }

        private void Update()
        {
            if (!isActive) return;

            float deltaTime = Time.deltaTime;

            cachedTransform.position += direction * currentSpeed * deltaTime;

            lifetimeTimer -= deltaTime;
            if (lifetimeTimer <= 0f)
            {
                Core.Game.Instance?.Sound?.PlayBulletExpire(cachedTransform.position);
                Deactivate();
                return;
            }

            CheckCollisions();
            CheckWhiz();
        }

        private void CheckWhiz()
        {
            if (isDeflected) return;
            if (targetPlayer == null) return;

            if (whizTimer > 0f)
            {
                whizTimer -= Time.deltaTime;
                return;
            }

            float sqrDist = (cachedTransform.position - targetPlayer.position).sqrMagnitude;
            float sqrWhiz = whizDistance * whizDistance;

            if (sqrDist <= sqrWhiz && !hasPlayedWhiz)
            {
                hasPlayedWhiz = true;
                whizTimer = whizCooldown;
                Core.Game.Instance?.Sound?.PlayBulletWhiz(cachedTransform.position);
            }
        }

        private void CheckCollisions()
        {
            Vector3 position = cachedTransform.position;

            int hitCount = Physics.OverlapSphereNonAlloc(
                position,
                collisionCheckRadius,
                hitBuffer,
                collisionLayers,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = hitBuffer[i];
                if (col == null) continue;

                if (col.CompareTag("Player"))
                {
                    HandlePlayerCollision(col);
                    return;
                }

                if (col.CompareTag("Enemy"))
                {
                    if (isDeflected && canDamageEnemiesWhenDeflected)
                    {
                        HandleEnemyCollision(col);
                        return;
                    }
                    continue;
                }

                SpawnHitEffect(false);
                Core.Game.Instance?.Sound?.PlayBulletImpact(position, isLethal);
                Deactivate();
                return;
            }

            CheckBlockArea();
        }

        private void CheckBlockArea()
        {
            if (isDeflected) return;
            if (targetPlayer == null) return;

            Player.Player player = targetPlayer.GetComponent<Player.Player>();
            if (player == null) player = targetPlayer.GetComponentInParent<Player.Player>();
            if (player == null) return;
            if (!player.IsBlocking) return;

            PlayerBlockDetector blockDetector = player.BlockDetector;
            if (blockDetector == null) return;

            Vector3 blockCenter = blockDetector.BlockCenter;
            float blockRadius = blockDetector.BlockRadius;

            float sqrDistance = (cachedTransform.position - blockCenter).sqrMagnitude;
            float sqrRadius = blockRadius * blockRadius;

            if (sqrDistance <= sqrRadius)
                Deflect(player);
        }

        private void HandlePlayerCollision(Collider playerCollider)
        {
            Player.Player player = playerCollider.GetComponent<Player.Player>();
            if (player == null) player = playerCollider.GetComponentInParent<Player.Player>();

            if (player == null)
            {
                SpawnHitEffect(false);
                Core.Game.Instance?.Sound?.PlayBulletImpact(cachedTransform.position, false);
                Deactivate();
                return;
            }

            PlayerController controller = player.Controller;

            if (controller == null)
            {
                SpawnHitEffect(false);
                Core.Game.Instance?.Sound?.PlayBulletImpact(cachedTransform.position, false);
                Deactivate();
                return;
            }

            if (controller.IsInvincible)
            {
                SpawnHitEffect(false);
                Core.Game.Instance?.Sound?.PlayBulletImpact(cachedTransform.position, false);
                Deactivate();
                return;
            }

            if (player.IsBlocking && !isDeflected)
            {
                Deflect(player);
                return;
            }

            if (isLethal)
            {
                SpawnHitEffect(true);
                Core.Game.Instance?.Sound?.PlayBulletImpact(cachedTransform.position, true);
                Core.Game.Instance?.GameOver();
                Deactivate();
                return;
            }

            Deflect(player);
        }

        private void HandleEnemyCollision(Collider enemyCollider)
        {
            Enemy enemy = enemyCollider.GetComponent<Enemy>();
            if (enemy == null) enemy = enemyCollider.GetComponentInParent<Enemy>();

            if (enemy != null && !enemy.IsDead)
                enemy.TakeDamage(1f, direction);

            SpawnHitEffect(false);
            Core.Game.Instance?.Sound?.PlayBulletImpact(cachedTransform.position, false);
            Deactivate();
        }

        public void Setup(Vector3 startPosition, Vector3 targetDirection, bool lethal = false)
        {
            cachedTransform.position = startPosition;
            direction = targetDirection.normalized;
            isLethal = lethal;
            wasLethal = lethal;
            lifetimeTimer = lifetime;
            currentSpeed = speed;
            isActive = true;
            isDeflected = false;
            sourceEnemy = null;
            targetPlayer = null;
            hasPlayedWhiz = false;
            whizTimer = 0f;

            if (direction.sqrMagnitude > 0.001f)
                cachedTransform.rotation = Quaternion.LookRotation(direction);

            UpdateVisual();

            if (trail != null)
            {
                trail.Clear();
                trail.enabled = true;
            }

            gameObject.SetActive(true);
        }

        public void SetTarget(Transform player)
        {
            targetPlayer = player;
        }

        public void SetSourceEnemy(Enemy enemy)
        {
            sourceEnemy = enemy;
        }

        public void RedirectToTarget(Vector3 newDirection, float newSpeed, bool canDamageEnemies)
        {
            direction = newDirection.normalized;
            currentSpeed = newSpeed;
            canDamageEnemiesWhenDeflected = canDamageEnemies;
            lifetimeTimer = deflectedLifetime;

            if (direction.sqrMagnitude > 0.001f)
                cachedTransform.rotation = Quaternion.LookRotation(direction);

            if (trail != null)
            {
                trail.startColor = Color.magenta;
                trail.endColor = new Color(1f, 0f, 1f, 0f);
            }
        }

        private void Deflect(Player.Player player)
        {
            player.BlockDetector?.OnBulletDeflected();
            player.Controller?.OnBulletBlocked();

            bool targeted = false;

            if (AbilityManager.Instance != null && AbilityManager.Instance.HasTargetedDeflect)
            {
                var deflectAbility = AbilityManager.Instance.TargetedDeflect;

                if (Random.value <= deflectAbility.TargetChance)
                {
                    Enemy nearestEnemy = FindNearestEnemy(deflectAbility.SearchRadius);

                    if (nearestEnemy != null)
                    {
                        Vector3 toEnemy = (nearestEnemy.transform.position + Vector3.up - cachedTransform.position).normalized;
                        direction = toEnemy;
                        currentSpeed = deflectAbility.DeflectSpeed;
                        canDamageEnemiesWhenDeflected = deflectAbility.CanDamageEnemies;
                        targeted = true;
                    }
                }
            }

            if (!targeted)
            {
                Vector3 playerForward = player.transform.forward;
                Vector3 playerRight = player.transform.right;
                Vector3 playerUp = Vector3.up;

                float horizontalAngle = Random.Range(-deflectionAngleRange, deflectionAngleRange);
                float verticalAngle = Random.Range(-deflectionAngleRange * 0.5f, deflectionAngleRange * 0.5f);

                Quaternion horizontalRotation = Quaternion.AngleAxis(horizontalAngle, playerUp);
                Quaternion verticalRotation = Quaternion.AngleAxis(verticalAngle, playerRight);

                direction = (verticalRotation * horizontalRotation * playerForward).normalized;
                currentSpeed = deflectedSpeed;
            }

            lifetimeTimer = deflectedLifetime;
            isDeflected = true;
            isLethal = false;

            if (direction.sqrMagnitude > 0.001f)
                cachedTransform.rotation = Quaternion.LookRotation(direction);

            cachedTransform.position += direction * 0.5f;

            UpdateVisual();
            SpawnDeflectEffect();

            Core.Game.Instance?.Sound?.PlayDeflect();
        }

        private Enemy FindNearestEnemy(float searchRadius)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                cachedTransform.position,
                searchRadius,
                enemySearchBuffer,
                enemyLayer,
                QueryTriggerInteraction.Ignore
            );

            Enemy nearest = null;
            float nearestSqrDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = enemySearchBuffer[i];
                if (col == null) continue;

                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy == null) enemy = col.GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDead) continue;
                if (enemy == sourceEnemy) continue;

                float sqrDist = (enemy.transform.position - cachedTransform.position).sqrMagnitude;
                if (sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private void UpdateVisual()
        {
            if (meshRenderer != null)
            {
                Material targetMaterial = null;

                if (isDeflected && deflectedMaterial != null) targetMaterial = deflectedMaterial;
                else if (isLethal && lethalMaterial != null) targetMaterial = lethalMaterial;
                else if (normalMaterial != null) targetMaterial = normalMaterial;

                if (targetMaterial != null)
                    meshRenderer.sharedMaterial = targetMaterial;
            }

            if (trail != null)
            {
                Color startColor;
                Color endColor;

                if (isDeflected)
                {
                    startColor = Color.cyan;
                    endColor = new Color(0f, 1f, 1f, 0f);
                }
                else if (isLethal)
                {
                    startColor = Color.red;
                    endColor = new Color(1f, 0f, 0f, 0f);
                }
                else
                {
                    startColor = Color.yellow;
                    endColor = new Color(1f, 1f, 0f, 0f);
                }

                trail.startColor = startColor;
                trail.endColor = endColor;
            }
        }

        private void SpawnHitEffect(bool lethal)
        {
            if (ParticleController.Instance == null) return;

            if (lethal) ParticleController.Instance.SpawnLethalHitEffect(cachedTransform.position, direction);
            else ParticleController.Instance.SpawnHitEffect(cachedTransform.position, direction);
        }

        private void SpawnDeflectEffect()
        {
            if (ParticleController.Instance == null) return;
            ParticleController.Instance.SpawnDeflectEffect(cachedTransform.position, direction);
        }

        public void Deactivate()
        {
            isActive = false;

            if (trail != null)
                trail.enabled = false;

            gameObject.SetActive(false);
        }

        public void ResetBullet()
        {
            isActive = false;
            isDeflected = false;
            lifetimeTimer = 0f;
            currentSpeed = speed;
            direction = Vector3.zero;
            isLethal = false;
            wasLethal = false;
            sourceEnemy = null;
            targetPlayer = null;
            hasPlayedWhiz = false;
            whizTimer = 0f;

            if (trail != null)
                trail.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, collisionCheckRadius);

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, whizDistance);
        }
#endif
    }
}