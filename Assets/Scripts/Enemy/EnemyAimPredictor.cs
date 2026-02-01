using UnityEngine;

namespace Runner.Enemy
{
    [System.Serializable]
    public class AimPredictionSettings
    {
        [Range(0f, 1f)]
        public float predictionAccuracy = 0.8f;
        public float maxPredictionTime = 1f;
        public bool predictLaneChanges = true;
        public bool predictJumps = true;

        [Range(0f, 2f)]
        public float distanceAccuracyFalloff = 0.5f;
        public float minAccuracyDistance = 5f;
        public float maxAccuracyDistance = 30f;

        [Range(0f, 1f)]
        public float randomSpread = 0.1f;
        public float verticalSpreadMultiplier = 0.5f;
    }

    public class EnemyAimPredictor : MonoBehaviour
    {
        [SerializeField] private AimPredictionSettings settings;

        private Transform playerTransform;
        private Runner.Player.Core.PlayerController playerController;
        private Vector3 lastPlayerPosition;
        private Vector3 playerVelocity;
        private float velocityUpdateTimer;
        private bool isInitialized;

        private float cachedPlayerSpeed;
        private float cachedLaneDistance;
        private int cachedMaxLane;
        private float cachedGravity;

        private const float VELOCITY_UPDATE_INTERVAL = 0.15f;

        private void Awake()
        {
            if (settings == null)
            {
                settings = new AimPredictionSettings();
            }
        }

        public void Initialize(Transform player)
        {
            playerTransform = player;
            isInitialized = false;

            if (player == null) return;

            playerController = player.GetComponentInParent<Runner.Player.Core.PlayerController>();

            if (playerController != null)
            {
                CachePlayerSettings();
            }

            lastPlayerPosition = player.position;
            playerVelocity = Vector3.zero;
            isInitialized = true;
        }

        private void CachePlayerSettings()
        {
            if (playerController.Settings != null)
            {
                cachedLaneDistance = playerController.Settings.laneDistance;
                cachedMaxLane = playerController.Settings.laneCount / 2;
            }

            if (playerController.CurrentPreset != null)
            {
                cachedGravity = playerController.CurrentPreset.Gravity;
            }
            else
            {
                cachedGravity = -30f;
            }
        }

        private void Update()
        {
            if (!isInitialized || playerTransform == null) return;

            velocityUpdateTimer -= Time.deltaTime;

            if (velocityUpdateTimer <= 0f)
            {
                velocityUpdateTimer = VELOCITY_UPDATE_INTERVAL;
                Vector3 currentPos = playerTransform.position;
                playerVelocity = (currentPos - lastPlayerPosition) / VELOCITY_UPDATE_INTERVAL;
                lastPlayerPosition = currentPos;

                if (playerController != null)
                {
                    cachedPlayerSpeed = playerController.CurrentSpeed;
                }
            }
        }

        public Vector3 GetAimDirection(Vector3 firePosition, float bulletSpeed)
        {
            if (!isInitialized || playerTransform == null)
            {
                return Vector3.forward;
            }

            Vector3 currentPlayerPos = playerTransform.position;
            currentPlayerPos.y += 1f;

            Vector3 toPlayer = currentPlayerPos - firePosition;
            float distance = toPlayer.magnitude;

            if (distance < 1f)
            {
                return toPlayer.normalized;
            }

            float timeToTarget = Mathf.Min(distance / bulletSpeed, settings.maxPredictionTime);

            Vector3 predictedPosition = currentPlayerPos;
            predictedPosition.z += cachedPlayerSpeed * timeToTarget * settings.predictionAccuracy;

            if (settings.predictLaneChanges)
            {
                float xVelocity = playerVelocity.x;
                if (Mathf.Abs(xVelocity) > 0.5f)
                {
                    float predictedX = predictedPosition.x + xVelocity * timeToTarget * settings.predictionAccuracy;
                    float maxX = cachedMaxLane * cachedLaneDistance;
                    predictedPosition.x = Mathf.Clamp(predictedX, -maxX, maxX);
                }
            }

            if (settings.predictJumps && playerController != null && !playerController.IsGrounded)
            {
                float yVelocity = playerVelocity.y;
                float predictedY = predictedPosition.y + yVelocity * timeToTarget + 0.5f * cachedGravity * timeToTarget * timeToTarget;
                predictedPosition.y = Mathf.Max(predictedY, 1f);
            }

            float accuracyMultiplier = 1f;
            if (distance > settings.minAccuracyDistance)
            {
                float t = Mathf.InverseLerp(settings.minAccuracyDistance, settings.maxAccuracyDistance, distance);
                accuracyMultiplier = 1f - (t * settings.distanceAccuracyFalloff);
            }

            float spread = settings.randomSpread * (2f - accuracyMultiplier);
            predictedPosition.x += Random.Range(-spread, spread);
            predictedPosition.y += Random.Range(-spread * settings.verticalSpreadMultiplier, spread * settings.verticalSpreadMultiplier);

            Vector3 direction = predictedPosition - firePosition;

            if (direction.sqrMagnitude < 0.001f)
            {
                return Vector3.forward;
            }

            return direction.normalized;
        }

        public bool IsReady()
        {
            return isInitialized && playerTransform != null;
        }

#if UNITY_EDITOR
        public void DrawPredictionGizmos(Vector3 firePosition, float bulletSpeed)
        {
            if (!isInitialized || playerTransform == null) return;

            Vector3 currentPos = playerTransform.position + Vector3.up;

            Vector3 toPlayer = currentPos - firePosition;
            float distance = toPlayer.magnitude;
            float timeToTarget = Mathf.Min(distance / bulletSpeed, settings.maxPredictionTime);

            Vector3 predictedPos = currentPos;
            predictedPos.z += cachedPlayerSpeed * timeToTarget * settings.predictionAccuracy;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentPos, 0.3f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(predictedPos, 0.4f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(currentPos, predictedPos);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(firePosition, predictedPos);
        }
#endif
    }
}