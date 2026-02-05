using UnityEngine;
using Runner.LevelGeneration;

namespace Runner.Environment
{
    public class FogController : MonoBehaviour
    {
        public static FogController Instance { get; private set; }

        [Header("Fog Mode")]
        [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;

        [Header("Night Adjustments")]
        [SerializeField] private float nightFogDensityMultiplier = 0.7f;
        [SerializeField] private float nightFogColorDarkening = 0.3f;
        [SerializeField] private float nightAmbientDarkening = 0.4f;

        [Header("Dawn/Dusk Adjustments")]
        [SerializeField] private Color dawnDuskFogTint = new Color(1f, 0.7f, 0.5f);
        [SerializeField] private float dawnDuskTintStrength = 0.3f;

        [Header("Transition")]
        [SerializeField] private float transitionSpeed = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private DayNightCycle dayNightCycle;
        private BiomeManager biomeManager;

        private Color baseFogColor;
        private float baseFogDensity;
        private Color baseAmbientColor;

        private Color currentFogColor;
        private float currentFogDensity;
        private Color currentAmbientColor;

        private Color targetFogColor;
        private float targetFogDensity;
        private Color targetAmbientColor;

        private bool isInitialized;

        public bool IsInitialized => isInitialized;

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

            if (biomeManager != null)
            {
                biomeManager.OnBiomeChanged += OnBiomeChanged;

                if (biomeManager.VisualCurrentBiome != null)
                {
                    SetBaseFogFromBiome(biomeManager.VisualCurrentBiome);
                    ApplyFogImmediate();
                }
            }

            RenderSettings.fogMode = fogMode;
            RenderSettings.fog = true;

            isInitialized = true;

            if (showDebug)
            {
                Debug.Log("[FogController] Initialized");
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            CalculateTargetFog();
            UpdateFogTransition();
            ApplyFog();
        }

        private void CalculateTargetFog()
        {
            float timeOfDay = dayNightCycle != null ? dayNightCycle.CurrentTime : 0.5f;

            float dayFactor = CalculateDayFactor(timeOfDay);
            float nightFactor = 1f - dayFactor;
            float dawnDuskFactor = CalculateDawnDuskFactor(timeOfDay);

            targetFogDensity = baseFogDensity * Mathf.Lerp(1f, nightFogDensityMultiplier, nightFactor);

            Color nightAdjustedFog = baseFogColor * Mathf.Lerp(1f, nightFogColorDarkening, nightFactor);

            Color dawnDuskTintedFog = Color.Lerp(nightAdjustedFog, dawnDuskFogTint * baseFogColor, dawnDuskFactor * dawnDuskTintStrength);

            targetFogColor = dawnDuskTintedFog;

            targetAmbientColor = baseAmbientColor * Mathf.Lerp(1f, nightAmbientDarkening, nightFactor);

            Color dawnDuskAmbient = Color.Lerp(targetAmbientColor, dawnDuskFogTint * baseAmbientColor, dawnDuskFactor * 0.2f);
            targetAmbientColor = dawnDuskAmbient;
        }

        private float CalculateDayFactor(float time)
        {
            if (time < 0.2f || time > 0.8f)
                return 0f;

            if (time < 0.3f)
                return Mathf.InverseLerp(0.2f, 0.3f, time);

            if (time > 0.7f)
                return 1f - Mathf.InverseLerp(0.7f, 0.8f, time);

            return 1f;
        }

        private float CalculateDawnDuskFactor(float time)
        {
            float dawn = 0f;
            float dusk = 0f;

            if (time >= 0.2f && time <= 0.35f)
            {
                if (time < 0.275f)
                    dawn = Mathf.InverseLerp(0.2f, 0.275f, time);
                else
                    dawn = 1f - Mathf.InverseLerp(0.275f, 0.35f, time);
            }

            if (time >= 0.65f && time <= 0.8f)
            {
                if (time < 0.725f)
                    dusk = Mathf.InverseLerp(0.65f, 0.725f, time);
                else
                    dusk = 1f - Mathf.InverseLerp(0.725f, 0.8f, time);
            }

            return Mathf.Max(dawn, dusk);
        }

        private void UpdateFogTransition()
        {
            float speed = transitionSpeed * Time.deltaTime;

            currentFogColor = Color.Lerp(currentFogColor, targetFogColor, speed);
            currentFogDensity = Mathf.Lerp(currentFogDensity, targetFogDensity, speed);
            currentAmbientColor = Color.Lerp(currentAmbientColor, targetAmbientColor, speed);
        }

        private void ApplyFog()
        {
            RenderSettings.fogColor = currentFogColor;
            RenderSettings.fogDensity = currentFogDensity;
            RenderSettings.ambientLight = currentAmbientColor;
        }

        private void ApplyFogImmediate()
        {
            CalculateTargetFog();

            currentFogColor = targetFogColor;
            currentFogDensity = targetFogDensity;
            currentAmbientColor = targetAmbientColor;

            ApplyFog();
        }

        private void OnBiomeChanged(BiomeData biome)
        {
            if (biome == null) return;

            SetBaseFogFromBiome(biome);

            if (showDebug)
            {
                Debug.Log($"[FogController] Biome changed to {biome.BiomeName}");
            }
        }

        private void SetBaseFogFromBiome(BiomeData biome)
        {
            baseFogColor = biome.FogColor;
            baseFogDensity = biome.FogDensity;
            baseAmbientColor = biome.AmbientColor * biome.AmbientIntensity;

            RenderSettings.fog = biome.OverrideFog;
        }

        public void SetFogDirect(Color color, float density, Color ambient, bool immediate = false)
        {
            baseFogColor = color;
            baseFogDensity = density;
            baseAmbientColor = ambient;

            if (immediate)
            {
                ApplyFogImmediate();
            }
        }

        public void Reset()
        {
            if (biomeManager != null && biomeManager.VisualCurrentBiome != null)
            {
                SetBaseFogFromBiome(biomeManager.VisualCurrentBiome);
                ApplyFogImmediate();
            }

            if (showDebug)
            {
                Debug.Log("[FogController] Reset");
            }
        }

        private void OnDestroy()
        {
            if (biomeManager != null)
            {
                biomeManager.OnBiomeChanged -= OnBiomeChanged;
            }

            if (Instance == this) Instance = null;
        }
    }
}