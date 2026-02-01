using UnityEngine;
using Runner.Player;
using Runner.Player.Core;

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

        [Header("Lethality")]
        [SerializeField] private bool isLethal = false;

        [Header("Visual")]
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material deflectedMaterial;
        [SerializeField] private Material lethalMaterial;

        [Header("Effects")]
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private ParticleSystem lethalHitEffect;
        [SerializeField] private ParticleSystem deflectEffect;

        private Vector3 direction;
        private float lifetimeTimer;
        private float currentSpeed;
        private bool isActive;
        private bool isDeflected;
        private bool wasLethal;
        private Transform cachedTransform;
        private Collider[] hitBuffer = new Collider[8];

        private Transform targetPlayer;
        private Enemy sourceEnemy;

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
                Deactivate();
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
                    if (isDeflected && canDamageEnemiesWhenDeflected)
                    {
                        HandleEnemyCollision(col);
                        return;
                    }
                    continue;
                }

                PlayHitEffect(false);
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
            if (player == null)
            {
                player = targetPlayer.GetComponentInParent<Player.Player>();
            }

            if (player == null) return;
            if (!player.IsBlocking) return;

            PlayerBlockDetector blockDetector = player.BlockDetector;
            if (blockDetector == null) return;

            Vector3 blockCenter = blockDetector.BlockCenter;
            float blockRadius = blockDetector.BlockRadius;

            float sqrDistance = (cachedTransform.position - blockCenter).sqrMagnitude;
            float sqrRadius = blockRadius * blockRadius;

            if (sqrDistance <= sqrRadius)
            {
                Deflect(player);
            }
        }

        private void HandlePlayerCollision(Collider playerCollider)
        {
            Player.Player player = playerCollider.GetComponent<Player.Player>();
            if (player == null)
            {
                player = playerCollider.GetComponentInParent<Player.Player>();
            }

            if (player == null)
            {
                PlayHitEffect(false);
                Deactivate();
                return;
            }

            PlayerController controller = player.Controller;

            if (controller == null)
            {
                PlayHitEffect(false);
                Deactivate();
                return;
            }

            if (controller.IsInvincible)
            {
                PlayHitEffect(false);
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
                PlayHitEffect(true);
                Core.Game.Instance?.GameOver();
                Deactivate();
                return;
            }

            Deflect(player);
        }

        private void HandleEnemyCollision(Collider enemyCollider)
        {
            Enemy enemy = enemyCollider.GetComponent<Enemy>();
            if (enemy == null)
            {
                enemy = enemyCollider.GetComponentInParent<Enemy>();
            }

            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(1f);
            }

            PlayHitEffect(false);
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

            if (direction.sqrMagnitude > 0.001f)
            {
                cachedTransform.rotation = Quaternion.LookRotation(direction);
            }

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
            {
                cachedTransform.rotation = Quaternion.LookRotation(direction);
            }

            if (trail != null)
            {
                trail.startColor = Color.magenta;
                trail.endColor = new Color(1f, 0f, 1f, 0f);
            }
        }

        private void Deflect(Player.Player player)
        {
            player.BlockDetector?.OnBulletDeflected();

            Vector3 playerForward = player.transform.forward;
            Vector3 playerRight = player.transform.right;
            Vector3 playerUp = Vector3.up;

            float horizontalAngle = Random.Range(-deflectionAngleRange, deflectionAngleRange);
            float verticalAngle = Random.Range(-deflectionAngleRange * 0.5f, deflectionAngleRange * 0.5f);

            Quaternion horizontalRotation = Quaternion.AngleAxis(horizontalAngle, playerUp);
            Quaternion verticalRotation = Quaternion.AngleAxis(verticalAngle, playerRight);

            direction = (verticalRotation * horizontalRotation * playerForward).normalized;
            currentSpeed = deflectedSpeed;
            lifetimeTimer = deflectedLifetime;
            isDeflected = true;
            isLethal = false;

            if (direction.sqrMagnitude > 0.001f)
            {
                cachedTransform.rotation = Quaternion.LookRotation(direction);
            }

            cachedTransform.position += direction * 0.5f;

            UpdateVisual();
            PlayDeflectEffect();
        }

        private void UpdateVisual()
        {
            if (meshRenderer == null) return;

            Material targetMaterial = null;

            if (isDeflected && deflectedMaterial != null)
            {
                targetMaterial = deflectedMaterial;
            }
            else if (isLethal && lethalMaterial != null)
            {
                targetMaterial = lethalMaterial;
            }
            else if (normalMaterial != null)
            {
                targetMaterial = normalMaterial;
            }

            if (targetMaterial != null)
            {
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

        private void PlayHitEffect(bool lethal)
        {
            ParticleSystem effect = lethal ? lethalHitEffect : hitEffect;

            if (effect != null)
            {
                effect.transform.position = cachedTransform.position;
                effect.Play();
            }
        }

        private void PlayDeflectEffect()
        {
            if (deflectEffect != null)
            {
                deflectEffect.transform.position = cachedTransform.position;
                deflectEffect.Play();
            }
        }

        public void Deactivate()
        {
            isActive = false;

            if (trail != null)
            {
                trail.enabled = false;
            }

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

            if (trail != null)
            {
                trail.Clear();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, collisionCheckRadius);
        }
#endif
    }
}