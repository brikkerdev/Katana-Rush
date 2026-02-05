using UnityEngine;
using System.Collections;

namespace Runner.CameraSystem
{
    public class CameraEffects : MonoBehaviour
    {
        [Header("Shake Settings")]
        [SerializeField] private float defaultShakeDuration = 0.3f;
        [SerializeField] private float defaultShakeIntensity = 0.2f;
        [SerializeField] private AnimationCurve shakeFalloff;

        [Header("Death Effect")]
        [SerializeField] private float deathShakeDuration = 0.5f;
        [SerializeField] private float deathShakeIntensity = 0.5f;
        [SerializeField] private float deathSlowMotionDuration = 0.8f;
        [SerializeField] private float deathTimeScale = 0.2f;

        [Header("Impact Effect")]
        [SerializeField] private float impactShakeDuration = 0.15f;
        [SerializeField] private float impactShakeIntensity = 0.15f;

        [Header("Dash Effect")]
        [SerializeField] private float dashFOVBoost = 5f;
        [SerializeField] private float dashFOVDuration = 0.3f;

        [Header("Landing Effect")]
        [SerializeField] private float landingShakeDuration = 0.1f;
        [SerializeField] private float landingShakeIntensity = 0.08f;

        private Player.Player player;
        private Camera mainCamera;
        private Vector3 originalLocalPosition;
        private float originalFOV;

        private Coroutine shakeCoroutine;
        private Coroutine slowMotionCoroutine;
        private Coroutine fovCoroutine;

        private bool isInitialized;

        private void Awake()
        {
            mainCamera = GetComponent<Camera>();
            if (mainCamera == null)
            {
                mainCamera = GetComponentInChildren<Camera>();
            }

            if (shakeFalloff == null || shakeFalloff.keys.Length == 0)
            {
                shakeFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            }
        }

        public void Initialize(Player.Player playerRef)
        {
            player = playerRef;
            originalLocalPosition = transform.localPosition;

            if (mainCamera != null)
            {
                originalFOV = mainCamera.fieldOfView;
            }

            isInitialized = true;
            SubscribeToPlayerEvents();
        }

        private void SubscribeToPlayerEvents()
        {
            if (player?.Controller != null)
            {
                // Subscribe to relevant events from player controller
                // player.Controller.OnDash += PlayDashEffect;
                // player.Controller.OnLand += PlayLandingEffect;
            }
        }

        public void PlayDeathEffect()
        {
            Shake(deathShakeDuration, deathShakeIntensity);
            PlaySlowMotion(deathSlowMotionDuration, deathTimeScale);
        }

        public void PlayImpactEffect()
        {
            Shake(impactShakeDuration, impactShakeIntensity);
        }

        public void PlayDashEffect()
        {
            PunchFOV(dashFOVBoost, dashFOVDuration);
        }

        public void PlayLandingEffect()
        {
            Shake(landingShakeDuration, landingShakeIntensity);
        }

        public void Shake(float duration = -1f, float intensity = -1f)
        {
            if (!isInitialized) return;

            if (duration < 0f) duration = defaultShakeDuration;
            if (intensity < 0f) intensity = defaultShakeIntensity;

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                transform.localPosition = originalLocalPosition;
            }

            shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, intensity));
        }

        private IEnumerator ShakeCoroutine(float duration, float intensity)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float progress = elapsed / duration;
                float currentIntensity = intensity * shakeFalloff.Evaluate(progress);

                Vector3 shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f * currentIntensity,
                    (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f * currentIntensity,
                    0f
                );

                transform.localPosition = originalLocalPosition + shakeOffset;

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            transform.localPosition = originalLocalPosition;
            shakeCoroutine = null;
        }

        public void PlaySlowMotion(float duration, float timeScale)
        {
            if (slowMotionCoroutine != null)
            {
                StopCoroutine(slowMotionCoroutine);
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }

            slowMotionCoroutine = StartCoroutine(SlowMotionCoroutine(duration, timeScale));
        }

        private IEnumerator SlowMotionCoroutine(float duration, float targetTimeScale)
        {
            // Quickly enter slow motion
            float enterDuration = 0.1f;
            float elapsed = 0f;

            while (elapsed < enterDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / enterDuration;
                Time.timeScale = Mathf.Lerp(1f, targetTimeScale, t);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                yield return null;
            }

            Time.timeScale = targetTimeScale;
            Time.fixedDeltaTime = 0.02f * targetTimeScale;

            yield return new WaitForSecondsRealtime(duration);

            // Smoothly exit slow motion
            elapsed = 0f;
            float exitDuration = 0.3f;

            while (elapsed < exitDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / exitDuration;
                Time.timeScale = Mathf.Lerp(targetTimeScale, 1f, t);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                yield return null;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            slowMotionCoroutine = null;
        }

        public void PunchFOV(float amount, float duration)
        {
            if (mainCamera == null) return;

            if (fovCoroutine != null)
            {
                StopCoroutine(fovCoroutine);
            }

            fovCoroutine = StartCoroutine(FOVPunchCoroutine(amount, duration));
        }

        private IEnumerator FOVPunchCoroutine(float amount, float duration)
        {
            float startFOV = mainCamera.fieldOfView;
            float targetFOV = startFOV + amount;
            float halfDuration = duration * 0.5f;

            // Punch out
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                mainCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
                yield return null;
            }

            // Return
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                mainCamera.fieldOfView = Mathf.Lerp(targetFOV, startFOV, t);
                yield return null;
            }

            mainCamera.fieldOfView = startFOV;
            fovCoroutine = null;
        }

        public void StopAllEffects()
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }

            if (slowMotionCoroutine != null)
            {
                StopCoroutine(slowMotionCoroutine);
                slowMotionCoroutine = null;
            }

            if (fovCoroutine != null)
            {
                StopCoroutine(fovCoroutine);
                fovCoroutine = null;
            }

            transform.localPosition = originalLocalPosition;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            if (mainCamera != null)
            {
                mainCamera.fieldOfView = originalFOV;
            }
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        private void OnDestroy()
        {
            StopAllEffects();
        }
    }
}