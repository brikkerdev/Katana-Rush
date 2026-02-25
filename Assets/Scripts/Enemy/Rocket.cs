using UnityEngine;
using Runner.Player;
using Runner.Player.Core;
using Runner.Effects;

namespace Runner.Enemy
{
    public class Rocket : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float speed = 15f;
        [SerializeField] private float lifetime = 8f;

        [Header("Explosion Settings")]
        [SerializeField] private float explosionRadius = 3f;
        [SerializeField] private float explosionDamage = 1f;
        [SerializeField] private bool isLethal = true;

        [Header("Collision")]
        [SerializeField] private float collisionCheckRadius = 0.3f;
        [SerializeField] private LayerMask collisionLayers = -1;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Visual")]
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private ParticleSystem engineParticles;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material rocketMaterial;

        private Vector3 direction;
        private float lifetimeTimer;
        private float currentSpeed;
        private bool isActive;
        private Transform cachedTransform;
        private Collider[] hitBuffer = new Collider[8];

        private Transform targetPlayer;
        private Enemy sourceEnemy;

        public bool IsLethal => isLethal;
        public bool IsActive => isActive;
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
                Explode(false);
                return;
            }

            CheckCollisions();
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
                    // Don't collide with other enemies (rockets are friendly fire disabled)
                    continue;
                }

                // Hit wall or other object
                Explode(false);
                return;
            }
        }

        private void HandlePlayerCollision(Collider playerCollider)
        {
            Player.Player player = playerCollider.GetComponent<Player.Player>();
            if (player == null) player = playerCollider.GetComponentInParent<Player.Player>();

            if (player == null)
            {
                Explode(false);
                return;
            }

            PlayerController controller = player.Controller;

            if (controller == null)
            {
                Explode(false);
                return;
            }

            if (controller.IsInvincible)
            {
                Explode(false);
                return;
            }

            // Rockets cannot be deflected - they always explode
            Explode(true);
        }

        public void Setup(Vector3 startPosition, Vector3 targetDirection, bool lethal = true)
        {
            cachedTransform.position = startPosition;
            direction = targetDirection.normalized;
            isLethal = lethal;
            lifetimeTimer = lifetime;
            currentSpeed = speed;
            isActive = true;
            sourceEnemy = null;
            targetPlayer = null;

            if (direction.sqrMagnitude > 0.001f)
                cachedTransform.rotation = Quaternion.LookRotation(direction);

            UpdateVisual();

            if (trail != null)
            {
                trail.Clear();
                trail.enabled = true;
            }

            if (engineParticles != null)
            {
                engineParticles.Play();
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

        private void Explode(bool hitPlayer)
        {
            Vector3 explosionPosition = cachedTransform.position;

            // Check for player in explosion radius
            int hitCount = Physics.OverlapSphereNonAlloc(
                explosionPosition,
                explosionRadius,
                hitBuffer,
                playerLayer,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = hitBuffer[i];
                if (col == null) continue;

                if (col.CompareTag("Player"))
                {
                    Player.Player player = col.GetComponent<Player.Player>();
                    if (player == null) player = col.GetComponentInParent<Player.Player>();

                    if (player != null)
                    {
                        PlayerController controller = player.Controller;

                        if (controller != null && !controller.IsInvincible)
                        {
                            if (isLethal)
                            {
                                Core.Game.Instance?.Sound?.PlayRocketExplosion(explosionPosition, true);
                                Core.Game.Instance?.GameOver();
                            }
                            else
                            {
                                Core.Game.Instance?.Sound?.PlayRocketExplosion(explosionPosition, false);
                            }
                        }
                    }
                }
            }

            // Spawn explosion effect
            if (ParticleController.Instance != null)
            {
                if (isLethal)
                    ParticleController.Instance.SpawnLethalHitEffect(explosionPosition, direction);
                else
                    ParticleController.Instance.SpawnRocketExplosionEffect(explosionPosition, direction);
            }

            // Play explosion sound
            Core.Game.Instance?.Sound?.PlayRocketExplosion(explosionPosition, isLethal);

            Deactivate();
        }

        private void UpdateVisual()
        {
            if (meshRenderer != null && rocketMaterial != null)
            {
                meshRenderer.sharedMaterial = rocketMaterial;
            }

            if (trail != null)
            {
                trail.startColor = new Color(1f, 0.5f, 0f, 1f); // Orange
                trail.endColor = new Color(1f, 0f, 0f, 0f);
            }
        }

        public void Deactivate()
        {
            isActive = false;

            if (trail != null)
                trail.enabled = false;

            if (engineParticles != null)
                engineParticles.Stop();

            gameObject.SetActive(false);
        }

        public void ResetRocket()
        {
            isActive = false;
            lifetimeTimer = 0f;
            currentSpeed = speed;
            direction = Vector3.zero;
            isLethal = true;
            sourceEnemy = null;
            targetPlayer = null;

            if (trail != null)
                trail.Clear();

            if (engineParticles != null)
                engineParticles.Stop();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, collisionCheckRadius);

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
#endif
    }
}
