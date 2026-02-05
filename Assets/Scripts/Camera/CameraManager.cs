using UnityEngine;
using System;

namespace Runner.CameraSystem
{
    public class CameraManager : MonoBehaviour
    {
        [Header("State Configurations")]
        [SerializeField] private CameraStateConfig menuConfig;
        [SerializeField] private CameraStateConfig gameplayConfig;
        [SerializeField] private CameraStateConfig deathConfig;

        [Header("Follow Settings")]
        [SerializeField] private float positionSmoothSpeed = 10f;
        [SerializeField] private float rotationSmoothSpeed = 8f;
        [SerializeField] private float stateTransitionSpeed = 3f;

        [Header("Look Ahead")]
        [SerializeField] private bool useLookAhead = true;
        [SerializeField] private float lookAheadDistance = 5f;
        [SerializeField] private float lookAheadSmoothSpeed = 3f;

        [Header("Boundaries")]
        [SerializeField] private bool clampHorizontal = true;
        [SerializeField] private float horizontalClampRange = 0.5f;

        private Transform target;
        private CameraState currentState = CameraState.Menu;
        private CameraStateConfig currentConfig;
        private CameraStateConfig targetConfig;

        private Vector3 currentOffset;
        private Vector3 currentLookAhead;
        private Quaternion currentRotation;
        private Vector3 velocity = Vector3.zero;
        private Vector3 lastTargetPosition;
        private bool isInitialized;

        public CameraState CurrentState => currentState;
        public Transform Target => target;

        public event Action<CameraState> OnStateChanged;

        private void Awake()
        {
            InitializeDefaultConfigs();
        }

        private void InitializeDefaultConfigs()
        {
            if (menuConfig == null)
            {
                menuConfig = new CameraStateConfig
                {
                    offset = new Vector3(0f, 4f, -8f),
                    rotation = new Vector3(12f, 0f, 0f),
                    fieldOfView = 60f
                };
            }

            if (gameplayConfig == null)
            {
                gameplayConfig = new CameraStateConfig
                {
                    offset = new Vector3(0f, 5f, -10f),
                    rotation = new Vector3(15f, 0f, 0f),
                    fieldOfView = 65f
                };
            }

            if (deathConfig == null)
            {
                deathConfig = new CameraStateConfig
                {
                    offset = new Vector3(0f, 8f, -12f),
                    rotation = new Vector3(30f, 0f, 0f),
                    fieldOfView = 55f
                };
            }
        }

        public void Initialize(Transform playerTransform)
        {
            target = playerTransform;
            currentState = CameraState.Menu;
            currentConfig = menuConfig;
            targetConfig = menuConfig;

            currentOffset = menuConfig.offset;
            currentRotation = Quaternion.Euler(menuConfig.rotation);

            if (target != null)
            {
                transform.position = target.position + currentOffset;
                transform.rotation = currentRotation;
                lastTargetPosition = target.position;
            }

            ApplyFieldOfView(menuConfig.fieldOfView);
            isInitialized = true;
        }

        public void SetState(CameraState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            targetConfig = GetConfigForState(newState);
            OnStateChanged?.Invoke(newState);
        }

        private CameraStateConfig GetConfigForState(CameraState state)
        {
            return state switch
            {
                CameraState.Menu => menuConfig,
                CameraState.Gameplay => gameplayConfig,
                CameraState.Death => deathConfig,
                CameraState.Cinematic => gameplayConfig,
                _ => gameplayConfig
            };
        }

        private void LateUpdate()
        {
            if (!isInitialized || target == null) return;

            UpdateConfiguration();
            UpdatePosition();
            UpdateRotation();
            UpdateFieldOfView();

            lastTargetPosition = target.position;
        }

        private void UpdateConfiguration()
        {
            float transitionDelta = stateTransitionSpeed * Time.deltaTime;

            currentOffset = Vector3.Lerp(currentOffset, targetConfig.offset, transitionDelta);
        }

        private void UpdatePosition()
        {
            Vector3 basePosition = target.position;

            // Apply horizontal clamping for lanes
            if (clampHorizontal)
            {
                float clampedX = Mathf.Clamp(
                    transform.position.x,
                    basePosition.x - horizontalClampRange,
                    basePosition.x + horizontalClampRange
                );
                basePosition.x = Mathf.Lerp(transform.position.x, clampedX, positionSmoothSpeed * Time.deltaTime);
            }

            // Calculate look ahead
            Vector3 lookAheadOffset = Vector3.zero;
            if (useLookAhead && currentState == CameraState.Gameplay)
            {
                Vector3 targetLookAhead = Vector3.forward * lookAheadDistance;
                currentLookAhead = Vector3.Lerp(
                    currentLookAhead,
                    targetLookAhead,
                    lookAheadSmoothSpeed * Time.deltaTime
                );
                lookAheadOffset = currentLookAhead;
            }
            else
            {
                currentLookAhead = Vector3.Lerp(
                    currentLookAhead,
                    Vector3.zero,
                    lookAheadSmoothSpeed * Time.deltaTime
                );
            }

            Vector3 targetPosition = basePosition + currentOffset + lookAheadOffset;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                1f / positionSmoothSpeed
            );
        }

        private void UpdateRotation()
        {
            Quaternion targetRotation = Quaternion.Euler(targetConfig.rotation);
            currentRotation = Quaternion.Slerp(
                currentRotation,
                targetRotation,
                rotationSmoothSpeed * Time.deltaTime
            );
            transform.rotation = currentRotation;
        }

        private void UpdateFieldOfView()
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.fieldOfView = Mathf.Lerp(
                    cam.fieldOfView,
                    targetConfig.fieldOfView,
                    stateTransitionSpeed * Time.deltaTime
                );
            }
        }

        private void ApplyFieldOfView(float fov)
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.fieldOfView = fov;
            }
        }

        public void SnapToTarget()
        {
            if (target == null) return;

            currentOffset = targetConfig.offset;
            currentRotation = Quaternion.Euler(targetConfig.rotation);
            transform.position = target.position + currentOffset;
            transform.rotation = currentRotation;
            velocity = Vector3.zero;
            ApplyFieldOfView(targetConfig.fieldOfView);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                lastTargetPosition = target.position;
            }
        }

        public void AddOffset(Vector3 additionalOffset, float duration = 0f)
        {
            if (duration <= 0f)
            {
                currentOffset += additionalOffset;
            }
            else
            {
                StartCoroutine(TempOffsetCoroutine(additionalOffset, duration));
            }
        }

        private System.Collections.IEnumerator TempOffsetCoroutine(Vector3 offset, float duration)
        {
            Vector3 originalOffset = currentOffset;
            currentOffset += offset;

            yield return new WaitForSeconds(duration);

            float elapsed = 0f;
            float returnDuration = 0.3f;
            Vector3 startOffset = currentOffset;

            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                currentOffset = Vector3.Lerp(startOffset, originalOffset, elapsed / returnDuration);
                yield return null;
            }

            currentOffset = originalOffset;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (target == null) return;

            Gizmos.color = Color.cyan;
            Vector3 targetPos = target.position + (targetConfig?.offset ?? gameplayConfig.offset);
            Gizmos.DrawWireSphere(targetPos, 0.5f);
            Gizmos.DrawLine(target.position, targetPos);
        }
#endif
    }

    [System.Serializable]
    public class CameraStateConfig
    {
        public Vector3 offset = new Vector3(0f, 5f, -10f);
        public Vector3 rotation = new Vector3(15f, 0f, 0f);
        public float fieldOfView = 60f;
    }
}