using UnityEngine;
using DG.Tweening;
using Runner.LevelGeneration;

namespace Runner.Environment
{
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance { get; private set; }

        [Header("Lights")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Light moonLight;

        [Header("Time")]
        [SerializeField] private float cycleDurationSeconds = 300f;
        [SerializeField, Range(0f, 1f)] private float currentTime = 0.25f;
        [SerializeField] private Vector2 offsetRotation;

        [Header("Sun")]
        [SerializeField] private float maxSunIntensity = 1.2f;
        [SerializeField] private Gradient sunColor;

        [Header("Moon")]
        [SerializeField] private float maxMoonIntensity = 0.4f;
        [SerializeField] private Gradient moonColor;

        [Header("Ambient")]
        [SerializeField] private float minAmbientIntensity = 0.15f;
        [SerializeField] private float maxAmbientIntensity = 1f;
        [SerializeField] private Gradient ambientColor;

        [Header("Debug")]
        [SerializeField] private bool showDebug;

        private Transform pivot;
        private float startTime;
        private bool isPaused;
        private float overrideTargetTime;
        private float timeBeforeOverride;
        private bool isOverrideActive;
        private Tween timeTransitionTween;

        public float CurrentTime => currentTime;
        public float CurrentHour => currentTime * 24f;
        public bool IsDay => currentTime > 0.25f && currentTime < 0.75f;
        public bool IsPaused => isPaused;
        public bool IsOverrideActive => isOverrideActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupDefaultGradients();
        }

        public void Initialize(Light sun, Light moon)
        {
            sunLight = sun;
            moonLight = moon;

            startTime = currentTime;
            CreatePivot();
            UpdateLighting();

            if (showDebug)
            {
                Debug.Log($"[DayNightCycle] Initialized at {CurrentHour:F1}h");
            }
        }

        private void CreatePivot()
        {
            var pivotGO = new GameObject("CelestialPivot");
            pivotGO.transform.SetParent(transform);
            pivotGO.transform.localPosition = Vector3.zero;
            pivot = pivotGO.transform;

            if (sunLight != null)
            {
                sunLight.transform.SetParent(pivot);
                sunLight.transform.localPosition = Vector3.zero;
                sunLight.transform.localRotation = Quaternion.identity;
            }

            if (moonLight != null)
            {
                moonLight.transform.SetParent(pivot);
                moonLight.transform.localPosition = Vector3.zero;
                moonLight.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            }
        }

        private void SetupDefaultGradients()
        {
            if (sunColor == null || sunColor.colorKeys.Length == 0)
            {
                sunColor = new Gradient();
                sunColor.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.3f, 0.3f, 0.5f), 0f),
                        new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.25f),
                        new GradientColorKey(new Color(1f, 1f, 0.95f), 0.5f),
                        new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.75f),
                        new GradientColorKey(new Color(0.3f, 0.3f, 0.5f), 1f)
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }

            if (moonColor == null || moonColor.colorKeys.Length == 0)
            {
                moonColor = new Gradient();
                moonColor.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.6f, 0.7f, 0.9f), 0f),
                        new GradientColorKey(new Color(0.6f, 0.7f, 0.9f), 1f)
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }

            if (ambientColor == null || ambientColor.colorKeys.Length == 0)
            {
                ambientColor = new Gradient();
                ambientColor.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.15f, 0.17f, 0.3f), 0f),
                        new GradientColorKey(new Color(0.7f, 0.5f, 0.4f), 0.25f),
                        new GradientColorKey(new Color(0.7f, 0.75f, 0.9f), 0.5f),
                        new GradientColorKey(new Color(0.7f, 0.5f, 0.4f), 0.75f),
                        new GradientColorKey(new Color(0.15f, 0.17f, 0.3f), 1f)
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }
        }

        private void Update()
        {
            if (!isPaused)
            {
                AdvanceTime();
            }
            UpdateLighting();
        }

        private void AdvanceTime()
        {
            currentTime += Time.deltaTime / cycleDurationSeconds;
            if (currentTime >= 1f) currentTime -= 1f;
        }

        private void UpdateLighting()
        {
            UpdatePivotRotation();
            UpdateSun();
            UpdateMoon();
            UpdateAmbient();
        }

        private void UpdatePivotRotation()
        {
            if (pivot == null) return;

            float angle = currentTime * 360f - 90f;
            pivot.localRotation = Quaternion.Euler(angle, offsetRotation.x, offsetRotation.y);
        }

        private void UpdateSun()
        {
            if (sunLight == null) return;

            float intensity = CalculateSunIntensity();
            sunLight.intensity = intensity;
            sunLight.color = sunColor.Evaluate(currentTime);
            sunLight.enabled = intensity > 0.01f;
        }

        private float CalculateSunIntensity()
        {
            if (currentTime < 0.2f || currentTime > 0.8f)
                return 0f;

            if (currentTime < 0.3f)
                return Mathf.InverseLerp(0.2f, 0.3f, currentTime) * maxSunIntensity;

            if (currentTime > 0.7f)
                return Mathf.InverseLerp(0.8f, 0.7f, currentTime) * maxSunIntensity;

            float peakFactor = 1f - Mathf.Abs(currentTime - 0.5f) * 2f;
            return Mathf.Lerp(0.7f, 1f, peakFactor) * maxSunIntensity;
        }

        private void UpdateMoon()
        {
            if (moonLight == null) return;

            float intensity = CalculateMoonIntensity();
            moonLight.intensity = intensity;
            moonLight.color = moonColor.Evaluate(currentTime);
            moonLight.enabled = intensity > 0.01f;
        }

        private float CalculateMoonIntensity()
        {
            if (currentTime > 0.3f && currentTime < 0.7f)
                return 0f;

            if (currentTime >= 0.7f && currentTime <= 0.8f)
                return Mathf.InverseLerp(0.7f, 0.8f, currentTime) * maxMoonIntensity;

            if (currentTime >= 0.2f && currentTime <= 0.3f)
                return Mathf.InverseLerp(0.3f, 0.2f, currentTime) * maxMoonIntensity;

            return maxMoonIntensity;
        }

        private void UpdateAmbient()
        {
            float intensity = CalculateAmbientIntensity();
            Color color = ambientColor.Evaluate(currentTime) * intensity;

            float brightness = (color.r + color.g + color.b) / 3f;
            if (brightness < 0.05f)
            {
                color *= 0.05f / Mathf.Max(brightness, 0.001f);
            }

            RenderSettings.ambientLight = color;
        }

        private float CalculateAmbientIntensity()
        {
            if (currentTime < 0.2f || currentTime > 0.8f)
                return minAmbientIntensity;

            if (currentTime < 0.3f)
                return Mathf.Lerp(minAmbientIntensity, maxAmbientIntensity, Mathf.InverseLerp(0.2f, 0.3f, currentTime));

            if (currentTime > 0.7f)
                return Mathf.Lerp(maxAmbientIntensity, minAmbientIntensity, Mathf.InverseLerp(0.7f, 0.8f, currentTime));

            float peakFactor = 1f - Mathf.Abs(currentTime - 0.5f) * 2.5f;
            return Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(peakFactor)) * maxAmbientIntensity;
        }

        public void ApplyBiomeTimeOverride(BiomeData biome)
        {
            if (biome == null) return;

            KillTimeTransition();

            if (!biome.HasTimeOverride)
            {
                if (isOverrideActive)
                {
                    ReleaseTimeOverride(biome.TimeTransitionDuration);
                }
                return;
            }

            float targetTime = biome.GetForcedTimeValue();
            if (targetTime < 0f) return;

            if (!isOverrideActive)
            {
                timeBeforeOverride = currentTime;
            }

            isOverrideActive = true;
            overrideTargetTime = targetTime;

            float duration = biome.TimeTransitionDuration;

            if (duration <= 0f)
            {
                currentTime = targetTime;
                isPaused = biome.PauseCycleDuringOverride;
                UpdateLighting();
                return;
            }

            float from = currentTime;
            float to = targetTime;

            float forwardDist = (to - from + 1f) % 1f;
            float backwardDist = (from - to + 1f) % 1f;
            bool goForward = forwardDist <= backwardDist;

            timeTransitionTween = DOTween.To(
                () => currentTime,
                x =>
                {
                    currentTime = x % 1f;
                    if (currentTime < 0f) currentTime += 1f;
                },
                goForward ? from + forwardDist : from - backwardDist,
                duration
            )
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                currentTime = targetTime;
                isPaused = biome.PauseCycleDuringOverride;
            });

            if (showDebug)
            {
                Debug.Log($"[DayNightCycle] Override to {biome.TimeOverride} (time={targetTime:F2}) over {duration}s");
            }
        }

        public void ReleaseTimeOverride(float transitionDuration = 2f)
        {
            if (!isOverrideActive) return;

            KillTimeTransition();

            isOverrideActive = false;
            isPaused = false;

            if (transitionDuration <= 0f)
            {
                return;
            }

            if (showDebug)
            {
                Debug.Log($"[DayNightCycle] Releasing override, resuming cycle");
            }
        }

        private void KillTimeTransition()
        {
            timeTransitionTween?.Kill();
            timeTransitionTween = null;
        }

        public void SetTime(float normalizedTime)
        {
            currentTime = Mathf.Repeat(normalizedTime, 1f);
            UpdateLighting();
        }

        public void SetHour(float hour)
        {
            SetTime(hour / 24f);
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Resume()
        {
            if (!isOverrideActive)
            {
                isPaused = false;
            }
        }

        public void Reset()
        {
            KillTimeTransition();

            currentTime = startTime;
            isPaused = false;
            isOverrideActive = false;

            UpdateLighting();
        }

        private void OnDestroy()
        {
            KillTimeTransition();
            if (Instance == this) Instance = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && pivot != null)
            {
                UpdateLighting();
            }
        }

        private void OnDrawGizmos()
        {
            if (!showDebug) return;

            if (sunLight != null && sunLight.enabled)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(sunLight.transform.position, sunLight.transform.forward * 5f);
            }

            if (moonLight != null && moonLight.enabled)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(moonLight.transform.position, moonLight.transform.forward * 5f);
            }
        }
#endif
    }
}