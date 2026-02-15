using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Runner.LevelGeneration;

namespace Runner.Environment
{
    public class FogController : MonoBehaviour
    {
        public static FogController Instance { get; private set; }

        [Header("Fog Settings")]
        [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;
        [SerializeField] private float fogStartDistance = 10f;
        [SerializeField] private float fogEndDistance = 100f;
        [SerializeField] private float fogDensity = 0.02f;

        [Header("Night Adjustments")]
        [SerializeField] private float nightFogDensityMultiplier = 1.3f;
        [SerializeField] private float nightFogColorDarkening = 0.3f;

        [Header("Dawn/Dusk Adjustments")]
        [SerializeField] private Color dawnDuskFogTint = new Color(1f, 0.7f, 0.5f);
        [SerializeField] private float dawnDuskTintStrength = 0.3f;

        [Header("Transition")]
        [SerializeField] private float transitionSpeed = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;
        [SerializeField] private bool fogEnabled = true;

        private DayNightCycle dayNightCycle;
        private BiomeManager biomeManager;

        private Color baseFogColor;
        private float baseFogDensity;

        private Color currentFogColor;
        private float currentFogDensity;
        private float currentFogStart;
        private float currentFogEnd;

        private Color targetFogColor;
        private float targetFogDensity;
        private float targetFogStart;
        private float targetFogEnd;

        private bool isInitialized;

        public bool IsInitialized => isInitialized;

        public bool FogEnabled
        {
            get => fogEnabled;
            set
            {
                fogEnabled = value;
                RenderSettings.fog = value;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize(DayNightCycle dayNight, BiomeManager biome)
        {
            dayNightCycle = dayNight;
            biomeManager = biome;

            // Enable Unity built-in fog
            RenderSettings.fog = fogEnabled;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = fogEndDistance;
            RenderSettings.fogDensity = fogDensity;

            if (biomeManager != null)
            {
                biomeManager.OnBiomeChanged += OnBiomeChanged;

                if (biomeManager.VisualCurrentBiome != null)
                    SetBaseFogFromBiome(biomeManager.VisualCurrentBiome);
            }
            else
            {
                baseFogColor = new Color(0.7f, 0.8f, 0.9f);
                baseFogDensity = fogDensity;
            }

            ApplyFogImmediate();
            isInitialized = true;

            if (showDebug)
                Debug.Log("[FogController] Initialized with Unity built-in fog");
        }

        private void Update()
        {
            if (!isInitialized || !fogEnabled) return;

            CalculateTargetFog();
            UpdateFogTransition();
            ApplyFogToRenderSettings();
        }

        private void CalculateTargetFog()
        {
            float timeOfDay = dayNightCycle != null ? dayNightCycle.CurrentTime : 0.5f;

            float dayFactor = CalculateDayFactor(timeOfDay);
            float nightFactor = 1f - dayFactor;
            float dawnDuskFactor = CalculateDawnDuskFactor(timeOfDay);

            targetFogDensity = baseFogDensity * Mathf.Lerp(1f, nightFogDensityMultiplier, nightFactor);

            float distanceMultiplier = Mathf.Lerp(1f, 0.7f, nightFactor);
            targetFogStart = fogStartDistance * distanceMultiplier;
            targetFogEnd = fogEndDistance * distanceMultiplier;

            Color nightAdjustedFog = baseFogColor * Mathf.Lerp(1f, nightFogColorDarkening, nightFactor);
            targetFogColor = Color.Lerp(nightAdjustedFog, dawnDuskFogTint * baseFogColor,
                dawnDuskFactor * dawnDuskTintStrength);
        }

        private float CalculateDayFactor(float time)
        {
            if (time < 0.2f || time > 0.8f) return 0f;
            if (time < 0.3f) return Mathf.InverseLerp(0.2f, 0.3f, time);
            if (time > 0.7f) return 1f - Mathf.InverseLerp(0.7f, 0.8f, time);
            return 1f;
        }

        private float CalculateDawnDuskFactor(float time)
        {
            float dawn = 0f;
            float dusk = 0f;

            if (time >= 0.2f && time <= 0.35f)
            {
                dawn = time < 0.275f
                    ? Mathf.InverseLerp(0.2f, 0.275f, time)
                    : 1f - Mathf.InverseLerp(0.275f, 0.35f, time);
            }

            if (time >= 0.65f && time <= 0.8f)
            {
                dusk = time < 0.725f
                    ? Mathf.InverseLerp(0.65f, 0.725f, time)
                    : 1f - Mathf.InverseLerp(0.725f, 0.8f, time);
            }

            return Mathf.Max(dawn, dusk);
        }

        private void UpdateFogTransition()
        {
            float speed = transitionSpeed * Time.deltaTime;
            currentFogColor = Color.Lerp(currentFogColor, targetFogColor, speed);
            currentFogDensity = Mathf.Lerp(currentFogDensity, targetFogDensity, speed);
            currentFogStart = Mathf.Lerp(currentFogStart, targetFogStart, speed);
            currentFogEnd = Mathf.Lerp(currentFogEnd, targetFogEnd, speed);
        }

        private void ApplyFogToRenderSettings()
        {
            RenderSettings.fogColor = currentFogColor;
            RenderSettings.fogDensity = currentFogDensity;
            RenderSettings.fogStartDistance = currentFogStart;
            RenderSettings.fogEndDistance = currentFogEnd;
        }

        private void ApplyFogImmediate()
        {
            CalculateTargetFog();
            currentFogColor = targetFogColor;
            currentFogDensity = targetFogDensity;
            currentFogStart = targetFogStart;
            currentFogEnd = targetFogEnd;
            ApplyFogToRenderSettings();
        }

        private void OnBiomeChanged(BiomeData biome)
        {
            if (biome == null) return;
            SetBaseFogFromBiome(biome);

            if (showDebug)
                Debug.Log($"[FogController] Biome changed to {biome.BiomeName}");
        }

        private void SetBaseFogFromBiome(BiomeData biome)
        {
            baseFogColor = biome.FogColor;
            baseFogDensity = biome.FogDensity;
            FogEnabled = biome.OverrideFog;
        }

        public void SetFogDirect(Color color, float density, float start, float end, bool immediate = false)
        {
            baseFogColor = color;
            baseFogDensity = density;
            fogStartDistance = start;
            fogEndDistance = end;
            if (immediate) ApplyFogImmediate();
        }

        public void Reset()
        {
            if (biomeManager != null && biomeManager.VisualCurrentBiome != null)
            {
                SetBaseFogFromBiome(biomeManager.VisualCurrentBiome);
                ApplyFogImmediate();
            }

            if (showDebug)
                Debug.Log("[FogController] Reset");
        }

        private void OnDestroy()
        {
            if (biomeManager != null)
                biomeManager.OnBiomeChanged -= OnBiomeChanged;

            if (Instance == this) Instance = null;
        }
    }
}