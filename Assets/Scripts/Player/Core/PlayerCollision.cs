using UnityEngine;
using Runner.Core;
using Runner.Player.Core;
using Runner.Collectibles;
using Runner.Inventory;
using Runner.Save;
using Runner.Environment;

namespace Runner.Player
{
    public class PlayerCollision : MonoBehaviour
    {
        [Header("Tags")]
        [SerializeField] private string obstacleTag = "Obstacle";
        [SerializeField] private string collectibleTag = "Collectable";
        [SerializeField] private string enemyTag = "Enemy";
        [SerializeField] private string destructibleObstacleTag = "DestructibleObstacle";

        [Header("Detection")]
        [SerializeField] private float detectionRadius = 1.2f;
        [SerializeField] private Vector3 detectionOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private LayerMask enemyLayer = -1;

        private Player player;
        private PlayerController controller;
        private Collider[] hitBuffer = new Collider[16];

        /// <summary>
        /// Gets the player's current velocity for physics calculations.
        /// Used for determining impact force on obstacles.
        /// </summary>
        public Vector3 PlayerVelocity
        {
            get
            {
                if (controller == null) return Vector3.forward * 10f;
                return Vector3.forward * controller.CurrentSpeed;
            }
        }

        public void Initialize(Player playerRef)
        {
            player = playerRef;
            controller = player.Controller;
        }

        private void FixedUpdate()
        {
            if (player == null) return;
            if (!player.IsRunning) return;

            CheckCollisions();
        }

        private void CheckCollisions()
        {
            Vector3 center = transform.position + detectionOffset;
            int hitCount = Physics.OverlapSphereNonAlloc(center, detectionRadius, hitBuffer, enemyLayer);

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = hitBuffer[i];
                if (col == null) continue;
                if (col.gameObject == gameObject) continue;

                if (col.CompareTag(enemyTag))
                {
                    HandleEnemyCollision(col.gameObject);
                }

                if (col.CompareTag(collectibleTag))
                {
                    Collectible collectible = col.GetComponent<Collectible>();
                    if (collectible != null)
                    {
                        collectible.Collect();
                    }
                    col.gameObject.SetActive(false);
                }
                
                // Check for destructible obstacles in overlap sphere
                if (col.CompareTag(destructibleObstacleTag) || col.CompareTag(obstacleTag))
                {
                    HandleDestructibleObstacleCollision(col.gameObject);
                }
            }
        }
        
        private void HandleDestructibleObstacleCollision(GameObject obstacleObject)
        {
            DestructibleObstacle destructible = obstacleObject.GetComponent<DestructibleObstacle>();
            if (destructible == null) return;
            if (destructible.IsDestroyed) return;
            
            if (controller.IsDashing)
            {
                // Pass player velocity for physics-based destruction effects
                destructible.OnDashHit(PlayerVelocity);
                return;
            }
            
            // Not dashing - game over
            if (!controller.IsInvincible)
            {
                Game.Instance?.GameOver();
            }
        }

        private void HandleEnemyCollision(GameObject enemyObject)
        {
            Enemy.Enemy enemy = enemyObject.GetComponent<Enemy.Enemy>();

            if (enemy == null)
            {
                enemy = enemyObject.GetComponentInParent<Enemy.Enemy>();
            }

            if (enemy == null) return;
            if (enemy.IsDead) return;

            if (controller.IsDashing)
            {
                float damage = 1f;
                if (controller.CurrentPreset != null)
                {
                    damage = controller.CurrentPreset.DashDamage;
                }

                enemy.TakeDamage(damage);
                Game.Instance?.Sound?.PlayDash();
                Game.Instance?.AddScore(100);

                SaveManager.AddEnemyKill();

                if (AbilityManager.Instance != null && AbilityManager.Instance.HasKillReward)
                {
                    var reward = AbilityManager.Instance.KillReward;
                    if (reward.CoinsPerKill > 0)
                    {
                        int coins = reward.CoinsPerKill;
                        if (AbilityManager.Instance.HasDoubleCoin)
                        {
                            coins *= AbilityManager.Instance.GetCoinMultiplier();
                        }
                        SaveManager.AddCoins(coins);
                        UI.UIManager.Instance?.NotifyCoinsCollected(coins);
                    }
                    if (reward.ScorePerKill > 0)
                    {
                        Game.Instance?.AddScore(reward.ScorePerKill);
                    }
                }

                return;
            }

            if (controller.IsInvincible) return;

            Game.Instance?.GameOver();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (player == null) return;
            if (!player.IsRunning) return;

            if (hit.gameObject.CompareTag(obstacleTag))
            {
                // Check if it's a destructible obstacle that can be destroyed by dash
                DestructibleObstacle destructible = hit.gameObject.GetComponent<DestructibleObstacle>();
                if (destructible != null && controller.IsDashing)
                {
                    // Pass player velocity for physics-based destruction effects
                    destructible.OnDashHit(PlayerVelocity);
                    return;
                }
                
                if (controller.IsInvincible) return;
                Game.Instance?.GameOver();
            }
            
            // Also handle explicit destructible obstacle tag
            if (hit.gameObject.CompareTag(destructibleObstacleTag))
            {
                HandleDestructibleObstacleCollision(hit.gameObject);
            }

            if (hit.gameObject.CompareTag(enemyTag))
            {
                HandleEnemyCollision(hit.gameObject);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position + detectionOffset;
            Gizmos.DrawWireSphere(center, detectionRadius);
        }
#endif
    }
}