using UnityEngine;
using System.Collections;
using Runner.Player.Core;

namespace Runner.CameraSystem
{
    public class CameraTarget : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private Vector3 baseOffset = new Vector3(0f, 2f, 0f);
        [SerializeField] private float followSmoothTime = 0.1f;

        [Header("Look Ahead")]
        [SerializeField] private bool useLookAhead = true;
        [SerializeField] private float lookAheadDistance = 5f;
        [SerializeField] private float lookAheadSmooth = 0.5f;

        [Header("Speed Based Offset")]
        [SerializeField] private bool useSpeedBasedOffset = true;
        [SerializeField] private float maxSpeedOffset = 3f;
        [SerializeField] private float speedForMaxOffset = 25f;

        private Transform _player;
        private Vector3 _currentOffset;
        private Vector3 _targetOffset;
        private Vector3 _velocity;
        private float _currentLookAhead;
        private Coroutine _offsetCoroutine;

        public void Initialize(Transform player)
        {
            _player = player;
            _currentOffset = baseOffset;
            _targetOffset = baseOffset;

            transform.position = _player.position + baseOffset;
        }

        private void LateUpdate()
        {
            if (_player == null) return;

            UpdateOffset();
            UpdatePosition();
        }

        private void UpdateOffset()
        {
            Vector3 offset = _targetOffset;

            if (useSpeedBasedOffset)
            {
                var controller = _player.GetComponent<PlayerController>();
                if (controller != null)
                {
                    float speedRatio = controller.CurrentSpeed / speedForMaxOffset;
                    float additionalOffset = Mathf.Lerp(0f, maxSpeedOffset, speedRatio);
                    offset.z += additionalOffset;
                }
            }

            _currentOffset = Vector3.Lerp(_currentOffset, offset, Time.deltaTime * 10f);
        }

        private void UpdatePosition()
        {
            Vector3 targetPosition = _player.position + _currentOffset;

            if (useLookAhead)
            {
                _currentLookAhead = Mathf.Lerp(_currentLookAhead, lookAheadDistance, Time.deltaTime / lookAheadSmooth);
                targetPosition.z += _currentLookAhead;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _velocity,
                followSmoothTime
            );
        }

        public void SetOffset(Vector3 offset, float duration)
        {
            if (_offsetCoroutine != null)
                StopCoroutine(_offsetCoroutine);

            _offsetCoroutine = StartCoroutine(AnimateOffset(offset, duration));
        }

        public void ResetOffset(float duration)
        {
            SetOffset(baseOffset, duration);
        }

        private IEnumerator AnimateOffset(Vector3 targetOffset, float duration)
        {
            Vector3 startOffset = _targetOffset;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _targetOffset = Vector3.Lerp(startOffset, targetOffset, t);
                yield return null;
            }

            _targetOffset = targetOffset;
        }

        public void SnapToPlayer()
        {
            if (_player == null) return;
            transform.position = _player.position + _currentOffset;
            _velocity = Vector3.zero;
        }
    }
}