using UnityEngine;

namespace Runner.Enemy
{
    public class SimpleEnemyRagdoll : MonoBehaviour
    {
        [Header("Body Parts")]
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform headTransform;
        [SerializeField] private GameObject separatedHeadPrefab;

        [Header("Head Launch")]
        [SerializeField] private float headLaunchForce = 12f;
        [SerializeField] private float headUpwardForce = 8f;
        [SerializeField] private float headSpinForce = 15f;

        [Header("Body Fall")]
        [SerializeField] private float fallDuration = 0.5f;
        [SerializeField] private AnimationCurve fallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Settings")]
        [SerializeField] private float stayDuration = 2f;

        private bool isActive;
        private float timer;
        private float fallTimer;
        private Vector3 startBodyRotation;
        private Vector3 targetBodyRotation;

        private Quaternion originalBodyRotation;
        private Vector3 originalBodyPosition;
        private Vector3 originalHeadLocalPos;
        private Quaternion originalHeadLocalRot;
        private Vector3 originalHeadScale;
        private bool headHidden;
        private bool isInitialized;

        public bool IsActive => isActive;

        private void Awake()
        {
            StoreOriginalState();
        }

        private void StoreOriginalState()
        {
            if (isInitialized) return;

            if (bodyRoot != null)
            {
                originalBodyRotation = bodyRoot.localRotation;
                originalBodyPosition = bodyRoot.localPosition;
            }

            if (headTransform != null)
            {
                originalHeadLocalPos = headTransform.localPosition;
                originalHeadLocalRot = headTransform.localRotation;
                originalHeadScale = headTransform.localScale;
            }

            isInitialized = true;
        }

        public void Activate(Vector3 hitDirection)
        {
            if (isActive) return;

            if (!isInitialized)
            {
                StoreOriginalState();
            }

            isActive = true;
            timer = stayDuration;
            fallTimer = 0f;

            LaunchHead(hitDirection);
            StartBodyFall(hitDirection);
        }

        private void LaunchHead(Vector3 direction)
        {
            if (headTransform != null)
            {
                headTransform.gameObject.SetActive(false);
                headHidden = true;
            }

            if (separatedHeadPrefab != null && RagdollPartPool.Instance != null)
            {
                Vector3 spawnPos = headTransform != null
                    ? headTransform.position
                    : transform.position + Vector3.up * 1.5f;

                Quaternion spawnRot = headTransform != null
                    ? headTransform.rotation
                    : Quaternion.identity;

                Vector3 force = (direction + Vector3.up * headUpwardForce).normalized * headLaunchForce;

                Vector3 torque = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ) * headSpinForce;

                RagdollPartPool.Instance.SpawnHead(spawnPos, spawnRot, force, torque);
            }
        }

        private void StartBodyFall(Vector3 direction)
        {
            if (bodyRoot == null) return;

            startBodyRotation = bodyRoot.localEulerAngles;

            float fallDirection = direction.x > 0f ? 1f : -1f;

            targetBodyRotation = new Vector3(
                90f * fallDirection,
                startBodyRotation.y + Random.Range(-30f, 30f),
                startBodyRotation.z
            );
        }

        private void Update()
        {
            if (!isActive) return;

            UpdateBodyFall();

            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                isActive = false;
            }
        }

        private void UpdateBodyFall()
        {
            if (bodyRoot == null) return;
            if (fallTimer >= fallDuration) return;

            fallTimer += Time.deltaTime;
            float t = fallTimer / fallDuration;
            float curveValue = fallCurve.Evaluate(t);

            Vector3 currentRotation = Vector3.Lerp(startBodyRotation, targetBodyRotation, curveValue);
            bodyRoot.localEulerAngles = currentRotation;
        }

        public void ResetRagdoll()
        {
            isActive = false;
            timer = 0f;
            fallTimer = 0f;

            if (bodyRoot != null)
            {
                bodyRoot.localRotation = originalBodyRotation;
                bodyRoot.localPosition = originalBodyPosition;
            }

            if (headTransform != null)
            {
                headTransform.localPosition = originalHeadLocalPos;
                headTransform.localRotation = originalHeadLocalRot;
                headTransform.localScale = originalHeadScale;

                if (headHidden)
                {
                    headTransform.gameObject.SetActive(true);
                    headHidden = false;
                }
            }
        }

        public void ForceReset()
        {
            StopAllCoroutines();
            ResetRagdoll();
        }

#if UNITY_EDITOR
        [ContextMenu("Store Original State")]
        private void EditorStoreState()
        {
            isInitialized = false;
            StoreOriginalState();
            Debug.Log("Original state stored!");
        }

        [ContextMenu("Reset Ragdoll")]
        private void EditorReset()
        {
            ResetRagdoll();
            Debug.Log("Reset complete!");
        }

        [ContextMenu("Test Activate")]
        private void EditorTest()
        {
            Activate(Vector3.forward);
        }
#endif
    }
}