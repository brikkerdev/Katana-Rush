using UnityEngine;
using Runner.Core;
using Runner.Player.Core;
using Runner.Collectibles;

namespace Runner.Player
{
    public class PlayerCollision : MonoBehaviour
    {
        [Header("Tags")]
        [SerializeField] private string obstacleTag = "Obstacle";
        [SerializeField] private string collectibleTag = "Collectable";
        [SerializeField] private string enemyTag = "Enemy";

        [Header("Detection")]
        [SerializeField] private float detectionRadius = 1.2f;
        [SerializeField] private Vector3 detectionOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private LayerMask enemyLayer = -1;

        private Player player;
        private PlayerController controller;
        private Collider[] hitBuffer = new Collider[16];

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
                Game.Instance?.AddScore(100);
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
                if (controller.IsInvincible) return;
                Game.Instance?.GameOver();
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