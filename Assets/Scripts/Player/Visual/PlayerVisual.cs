using UnityEngine;
using Runner.Player.Data;
using Runner.Player.Core;

namespace Runner.Player.Visual
{
    public class PlayerVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform meshTransform;

        [Header("Katana Mount")]
        [SerializeField] private Transform katanaMountPoint;

        private MovementSettings settings;
        private PlayerController controller;

        private float currentTilt;
        private Vector3 currentScale;
        private Vector3 targetScale;
        private float scaleTimer;
        private bool isScaling;

        private GameObject currentKatanaVisual;

        public Transform KatanaMountPoint => katanaMountPoint;

        public void Initialize(MovementSettings movementSettings, PlayerController playerController)
        {
            settings = movementSettings;
            controller = playerController;

            currentScale = Vector3.one;
            targetScale = Vector3.one;

            if (modelRoot == null)
            {
                modelRoot = transform;
            }

            if (meshTransform == null && modelRoot != null)
            {
                meshTransform = modelRoot.GetChild(0);
            }

            EnsureVisible();
        }

        private void EnsureVisible()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.enabled = true;
                r.gameObject.SetActive(true);
            }

            if (modelRoot != null)
            {
                modelRoot.gameObject.SetActive(true);
            }

            if (meshTransform != null)
            {
                meshTransform.gameObject.SetActive(true);
            }
        }

        private void LateUpdate()
        {
            if (settings == null) return;

            UpdateTilt();
            UpdateScale();
        }

        private void UpdateTilt()
        {
            if (modelRoot == null) return;

            float targetTilt = 0f;

            if (controller != null)
            {
                targetTilt = controller.GetLaneTiltAngle();
            }

            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * settings.tiltSpeed);

            Vector3 euler = modelRoot.localEulerAngles;
            euler.z = currentTilt;
            modelRoot.localEulerAngles = euler;
        }

        private void UpdateScale()
        {
            if (meshTransform == null) return;

            if (isScaling)
            {
                scaleTimer += Time.deltaTime;
                float progress = scaleTimer / settings.squashStretchDuration;

                if (progress >= 1f)
                {
                    currentScale = Vector3.one;
                    isScaling = false;
                }
                else
                {
                    float curveValue = settings.squashStretchCurve.Evaluate(progress);
                    currentScale = Vector3.Lerp(targetScale, Vector3.one, curveValue);
                }
            }

            meshTransform.localScale = currentScale;
        }

        public void PlayJumpSquash()
        {
            targetScale = new Vector3(
                settings.jumpSquashXZ,
                settings.jumpStretchY,
                settings.jumpSquashXZ
            );

            currentScale = targetScale;
            scaleTimer = 0f;
            isScaling = true;
        }

        public void PlayLandSquash()
        {
            targetScale = new Vector3(
                settings.landStretchXZ,
                settings.landSquashY,
                settings.landStretchXZ
            );

            currentScale = targetScale;
            scaleTimer = 0f;
            isScaling = true;
        }

        public void PlayDashStretch()
        {
            targetScale = new Vector3(
                settings.dashSquashXY,
                settings.dashSquashXY,
                settings.dashStretchZ
            );

            currentScale = targetScale;
            scaleTimer = 0f;
            isScaling = true;
        }

        public void SetKatanaVisual(GameObject katanaPrefab)
        {
            if (currentKatanaVisual != null)
            {
                Destroy(currentKatanaVisual);
            }

            if (katanaPrefab == null) return;
            if (katanaMountPoint == null) return;

            currentKatanaVisual = Instantiate(katanaPrefab, katanaMountPoint);
            currentKatanaVisual.transform.localPosition = Vector3.zero;
            currentKatanaVisual.transform.localRotation = Quaternion.identity;
        }

        public void Reset()
        {
            currentTilt = 0f;
            currentScale = Vector3.one;
            targetScale = Vector3.one;
            scaleTimer = 0f;
            isScaling = false;

            if (modelRoot != null)
            {
                modelRoot.localEulerAngles = Vector3.zero;
            }

            if (meshTransform != null)
            {
                meshTransform.localScale = Vector3.one;
            }

            EnsureVisible();
        }
    }
}