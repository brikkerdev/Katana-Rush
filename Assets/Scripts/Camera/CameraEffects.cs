using UnityEngine;
using System.Collections;
using Runner.Player.Core;

namespace Runner.CameraSystem
{
    public class CameraEffects : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CameraManager cameraManager;

        [Header("Speed Effect")]
        [SerializeField] private float baseFOV = 60f;
        [SerializeField] private float maxFOV = 75f;
        [SerializeField] private float speedForMaxFOV = 25f;
        [SerializeField] private float fovChangeSpeed = 2f;

        [Header("Dash Effect")]
        [SerializeField] private float dashFOV = 70f;
        [SerializeField] private float dashFOVDuration = 0.3f;

        [Header("Jump Effect")]
        [SerializeField] private Vector3 jumpOffsetAdd = new Vector3(0f, 1f, -1f);
        [SerializeField] private float jumpOffsetDuration = 0.5f;

        private float _currentTargetFOV;
        private bool _isDashFOVActive;
        private PlayerController _playerController;

        public void Initialize(Player.Player player)
        {
            _playerController = player.Controller;
            _currentTargetFOV = baseFOV;
        }

        private void Update()
        {
            if (_playerController == null) return;
            if (_isDashFOVActive) return;

            UpdateSpeedFOV();
        }

        private void UpdateSpeedFOV()
        {
            float speedRatio = _playerController.CurrentSpeed / speedForMaxFOV;
            float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedRatio);

            _currentTargetFOV = Mathf.Lerp(_currentTargetFOV, targetFOV, Time.deltaTime * fovChangeSpeed);

            if (cameraManager != null && cameraManager.ActiveCamera != null)
            {
                cameraManager.ActiveCamera.Lens.FieldOfView = _currentTargetFOV;
            }
        }

        public void PlayDashEffect()
        {
            StartCoroutine(DashFOVEffect());
            cameraManager?.ShakeCamera(CameraShakePreset.Dash);
        }

        public void PlayJumpEffect()
        {
            cameraManager?.SetFollowOffset(jumpOffsetAdd, jumpOffsetDuration * 0.5f);
            StartCoroutine(ResetOffsetAfterDelay(jumpOffsetDuration));
        }

        public void PlayLandEffect()
        {
            cameraManager?.ShakeCamera(CameraShakePreset.Land);
            cameraManager?.ResetFollowOffset(0.2f);
        }

        public void PlayDeathEffect()
        {
            cameraManager?.ShakeCamera(CameraShakePreset.Death);
        }

        public void PlayCollisionEffect(Vector3 direction)
        {
            cameraManager?.ShakeCamera(CameraShakePreset.Medium);
        }

        private IEnumerator DashFOVEffect()
        {
            _isDashFOVActive = true;

            float startFOV = _currentTargetFOV;
            float elapsed = 0f;
            float halfDuration = dashFOVDuration * 0.5f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float fov = Mathf.Lerp(startFOV, dashFOV, t);

                if (cameraManager?.ActiveCamera != null)
                    cameraManager.ActiveCamera.Lens.FieldOfView = fov;

                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float fov = Mathf.Lerp(dashFOV, startFOV, t);

                if (cameraManager?.ActiveCamera != null)
                    cameraManager.ActiveCamera.Lens.FieldOfView = fov;

                yield return null;
            }

            _isDashFOVActive = false;
        }

        private IEnumerator ResetOffsetAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            cameraManager?.ResetFollowOffset(0.3f);
        }
    }
}