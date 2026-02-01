using UnityEngine;
using Runner.Player.Core;
using Runner.Enemy;

namespace Runner.Player
{
    public class PlayerBlockDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float detectionAngle = 120f;
        [SerializeField] private float detectionHeight = 2f;
        [SerializeField] private LayerMask bulletLayer;

        [Header("Block Area")]
        [SerializeField] private float blockRadius = 1.5f;
        [SerializeField] private Vector3 blockOffset = new Vector3(0f, 1f, 0.5f);

        [Header("Block Settings")]
        [SerializeField] private float blockExitDelay = 0.3f;

        [Header("Performance")]
        [SerializeField] private float detectionInterval = 0.05f;
        [SerializeField] private int maxBulletsToCheck = 8;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private Player player;
        private PlayerController controller;
        private PlayerAnimator playerAnimator;
        private Transform cachedTransform;

        private Collider[] bulletBuffer;
        private bool isBlocking;
        private float blockExitTimer;
        private int bulletsInRange;
        private float detectionTimer;

        private Vector3 cachedCenter;
        private Vector3 cachedForward;

        public bool IsBlocking => isBlocking;
        public int BulletsInRange => bulletsInRange;
        public float BlockRadius => blockRadius;

        public Vector3 BlockCenter
        {
            get
            {
                if (cachedTransform == null) return Vector3.zero;
                return cachedTransform.position + cachedTransform.TransformDirection(blockOffset);
            }
        }

        public void Initialize(Player playerRef)
        {
            player = playerRef;
            controller = player.Controller;
            playerAnimator = player.Animator;
            cachedTransform = transform;
            bulletBuffer = new Collider[maxBulletsToCheck];
        }

        private void Update()
        {
            if (player == null) return;

            if (!player.IsRunning)
            {
                if (isBlocking)
                {
                    SetBlockState(false);
                }
                return;
            }

            if (controller != null && controller.IsDashing)
            {
                if (isBlocking)
                {
                    SetBlockState(false);
                }
                return;
            }

            detectionTimer -= Time.deltaTime;

            if (detectionTimer <= 0f)
            {
                detectionTimer = detectionInterval;
                DetectIncomingBullets();
            }

            UpdateBlockState();
        }

        private void DetectIncomingBullets()
        {
            bulletsInRange = 0;

            cachedCenter = cachedTransform.position + Vector3.up * (detectionHeight * 0.5f);
            cachedForward = cachedTransform.forward;

            int hitCount = Physics.OverlapSphereNonAlloc(
                cachedCenter,
                detectionRange,
                bulletBuffer,
                bulletLayer,
                QueryTriggerInteraction.Collide
            );

            float halfAngle = detectionAngle * 0.5f;
            float cosHalfAngle = Mathf.Cos(halfAngle * Mathf.Deg2Rad);

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = bulletBuffer[i];
                if (col == null) continue;

                Bullet bullet = col.GetComponent<Bullet>();
                if (bullet == null) continue;
                if (!bullet.IsActive || bullet.IsDeflected) continue;

                if (IsBulletIncomingFast(bullet, cosHalfAngle))
                {
                    bulletsInRange++;
                }
            }
        }

        private bool IsBulletIncomingFast(Bullet bullet, float cosHalfAngle)
        {
            Vector3 bulletPos = bullet.transform.position;
            Vector3 toBullet = bulletPos - cachedCenter;

            float sqrDistance = toBullet.sqrMagnitude;
            float sqrRange = detectionRange * detectionRange;

            if (sqrDistance > sqrRange || sqrDistance < 0.25f) return false;

            float heightDiff = Mathf.Abs(bulletPos.y - cachedCenter.y);
            if (heightDiff > detectionHeight) return false;

            Vector3 toBulletNormalized = toBullet.normalized;
            float dot = Vector3.Dot(cachedForward, toBulletNormalized);

            if (dot < cosHalfAngle) return false;

            Vector3 bulletDirection = bullet.Direction;
            if (bulletDirection.sqrMagnitude < 0.01f) return false;

            Vector3 toPlayer = -toBulletNormalized;
            float bulletDot = Vector3.Dot(bulletDirection, toPlayer);

            return bulletDot > 0.2f;
        }

        private void UpdateBlockState()
        {
            if (bulletsInRange > 0)
            {
                if (!isBlocking)
                {
                    SetBlockState(true);
                }
                blockExitTimer = blockExitDelay;
            }
            else if (isBlocking)
            {
                blockExitTimer -= Time.deltaTime;
                if (blockExitTimer <= 0f)
                {
                    SetBlockState(false);
                }
            }
        }

        private void SetBlockState(bool blocking)
        {
            if (isBlocking == blocking) return;

            isBlocking = blocking;
            playerAnimator?.SetBlocking(blocking);
        }

        public void OnBulletDeflected()
        {
            playerAnimator?.PlayBlockHitReaction();
            blockExitTimer = blockExitDelay;
        }

        public bool IsPositionInBlockArea(Vector3 position)
        {
            if (!isBlocking) return false;

            Vector3 blockCenter = BlockCenter;
            float sqrDistance = (position - blockCenter).sqrMagnitude;
            float sqrRadius = blockRadius * blockRadius;

            return sqrDistance <= sqrRadius;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Transform t = transform;
            Vector3 center = t.position + Vector3.up * (detectionHeight * 0.5f);

            Gizmos.color = isBlocking ? new Color(0f, 1f, 1f, 0.3f) : new Color(0.5f, 0.5f, 1f, 0.2f);
            Gizmos.DrawWireSphere(center, detectionRange);

            Gizmos.color = Color.cyan;
            Vector3 leftDir = Quaternion.Euler(0, -detectionAngle * 0.5f, 0) * t.forward;
            Vector3 rightDir = Quaternion.Euler(0, detectionAngle * 0.5f, 0) * t.forward;
            Gizmos.DrawRay(center, leftDir * detectionRange);
            Gizmos.DrawRay(center, rightDir * detectionRange);

            Vector3 blockCenter = t.position + t.TransformDirection(blockOffset);
            Gizmos.color = isBlocking ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(blockCenter, blockRadius);
        }
#endif
    }
}