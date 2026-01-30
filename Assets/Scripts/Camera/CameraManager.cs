using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace Runner.CameraSystem
{
    public class CameraManager : MonoBehaviour
    {
        [Header("Virtual Cameras")]
        [SerializeField] private CinemachineCamera menuCamera;
        [SerializeField] private CinemachineCamera gameplayCamera;
        [SerializeField] private CinemachineCamera deathCamera;

        [Header("References")]
        [SerializeField] private CinemachineBrain brain;
        [SerializeField] private CameraTarget cameraTarget;
        [SerializeField] private CameraShake cameraShake;

        [Header("Priority Settings")]
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 10;

        [Header("Blend Settings")]
        [SerializeField] private float menuToGameplayBlendTime = 1f;
        [SerializeField] private float gameplayToDeathBlendTime = 0.5f;
        [SerializeField] private float deathToGameplayBlendTime = 1f;

        private CameraState _currentState;
        private CinemachineCamera _activeCamera;
        private Transform _player;

        public CameraState CurrentState => _currentState;
        public CinemachineCamera ActiveCamera => _activeCamera;

        public void Initialize(Transform player)
        {
            _player = player;

            if (cameraTarget != null)
            {
                cameraTarget.Initialize(player);
            }

            SetupCameraTargets();
            SetState(CameraState.Menu);
        }

        private void SetupCameraTargets()
        {
            Transform target = cameraTarget != null ? cameraTarget.transform : _player;

            if (gameplayCamera != null)
            {
                gameplayCamera.Follow = target;
                gameplayCamera.LookAt = target;
            }

            if (deathCamera != null)
            {
                deathCamera.Follow = _player;
                deathCamera.LookAt = _player;
            }
        }

        public void SetState(CameraState newState)
        {
            if (_currentState == newState) return;

            CameraState previousState = _currentState;
            _currentState = newState;

            UpdateBlendTime(previousState, newState);
            ActivateCameraForState(newState);
        }

        private void UpdateBlendTime(CameraState from, CameraState to)
        {
            if (brain == null) return;

            float blendTime = 1f;

            if (from == CameraState.Menu && to == CameraState.Gameplay)
                blendTime = menuToGameplayBlendTime;
            else if (from == CameraState.Gameplay && to == CameraState.Death)
                blendTime = gameplayToDeathBlendTime;
            else if (from == CameraState.Death && to == CameraState.Gameplay)
                blendTime = deathToGameplayBlendTime;

            brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.EaseInOut,
                blendTime
            );
        }

        private void ActivateCameraForState(CameraState state)
        {
            SetAllCamerasInactive();

            switch (state)
            {
                case CameraState.Menu:
                    ActivateCamera(menuCamera);
                    break;
                case CameraState.Gameplay:
                case CameraState.Revive:
                    ActivateCamera(gameplayCamera);
                    break;
                case CameraState.Death:
                    ActivateCamera(deathCamera);
                    break;
            }
        }

        private void SetAllCamerasInactive()
        {
            if (menuCamera != null) menuCamera.Priority = inactivePriority;
            if (gameplayCamera != null) gameplayCamera.Priority = inactivePriority;
            if (deathCamera != null) deathCamera.Priority = inactivePriority;
        }

        private void ActivateCamera(CinemachineCamera cam)
        {
            if (cam == null) return;

            cam.Priority = activePriority;
            _activeCamera = cam;
        }

        public void ShakeCamera(float intensity, float duration)
        {
            if (cameraShake != null)
            {
                cameraShake.Shake(intensity, duration);
            }
        }

        public void ShakeCamera(CameraShakePreset preset)
        {
            if (cameraShake != null)
            {
                cameraShake.Shake(preset);
            }
        }

        public void SetFollowOffset(Vector3 offset, float duration = 0.5f)
        {
            if (cameraTarget != null)
            {
                cameraTarget.SetOffset(offset, duration);
            }
        }

        public void ResetFollowOffset(float duration = 0.5f)
        {
            if (cameraTarget != null)
            {
                cameraTarget.ResetOffset(duration);
            }
        }

        public void SetFieldOfView(float fov, float duration = 0.3f)
        {
            StartCoroutine(AnimateFOV(fov, duration));
        }

        private IEnumerator AnimateFOV(float targetFOV, float duration)
        {
            if (_activeCamera == null) yield break;

            float startFOV = _activeCamera.Lens.FieldOfView;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _activeCamera.Lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
                yield return null;
            }

            _activeCamera.Lens.FieldOfView = targetFOV;
        }

        private void OnValidate()
        {
            if (brain == null)
                brain = FindFirstObjectByType<CinemachineBrain>();
        }
    }
}