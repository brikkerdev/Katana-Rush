using UnityEngine;
using Runner.LevelGeneration;

namespace Runner.Environment
{
    public class FogController : MonoBehaviour
    {
        public static FogController Instance { get; private set; }

        [SerializeField] private Shader fogShader;

        [Header("Fog Settings")]
        [SerializeField] private float fogStartDistance = 10f;
        [SerializeField] private float fogEndDistance = 100f;
        [SerializeField] private float fogHeightFalloff = 0.1f;
        [SerializeField] private float fogBaseHeight = 0f;

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
        private Material fogMaterial;

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
        public Material FogMaterial => fogMaterial;
        public bool FogEnabled
        {
            get => fogEnabled;
            set => fogEnabled = value;
        }

        private static readonly int FogColorId = Shader.PropertyToID("_FogColor");
        private static readonly int FogStartId = Shader.PropertyToID("_FogStart");
        private static readonly int FogEndId = Shader.PropertyToID("_FogEnd");
        private static readonly int FogDensityId = Shader.PropertyToID("_FogDensity");
        private static readonly int FogHeightFalloffId = Shader.PropertyToID("_FogHeightFalloff");
        private static readonly int FogBaseHeightId = Shader.PropertyToID("_FogBaseHeight");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateMaterial();
        }

        private void CreateMaterial()
        {
            if (fogShader == null)
            {
                Debug.LogError("[FogController] Hidden/CustomFog shader not found!");
                return;
            }

            if (!fogShader.isSupported)
            {
                Debug.LogError("[FogController] CustomFog shader is not supported!");
                return;
            }

            fogMaterial = new Material(fogShader);
            fogMaterial.hideFlags = HideFlags.HideAndDontSave;

            if (showDebug)
            {
                Debug.Log("[FogController] Material created successfully");
            }
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
                }
            }
            else
            {
                baseFogColor = new Color(0.7f, 0.8f, 0.9f);
                baseFogDensity = 1f;
            }

            ApplyFogImmediate();

            RenderSettings.fog = false;

            isInitialized = true;

            if (showDebug)
            {
                Debug.Log("[FogController] Initialized");
            }
        }

        private void Update()
        {
            if (!isInitialized) return;
            if (fogMaterial == null) return;
            if (!fogEnabled) return;

            CalculateTargetFog();
            UpdateFogTransition();
            ApplyFogToMaterial();
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
            targetFogColor = Color.Lerp(nightAdjustedFog, dawnDuskFogTint * baseFogColor, dawnDuskFactor * dawnDuskTintStrength);
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
            currentFogStart = Mathf.Lerp(currentFogStart, targetFogStart, speed);
            currentFogEnd = Mathf.Lerp(currentFogEnd, targetFogEnd, speed);
        }

        private void ApplyFogToMaterial()
        {
            if (fogMaterial == null) return;

            fogMaterial.SetColor(FogColorId, currentFogColor);
            fogMaterial.SetFloat(FogStartId, currentFogStart);
            fogMaterial.SetFloat(FogEndId, currentFogEnd);
            fogMaterial.SetFloat(FogDensityId, currentFogDensity);
            fogMaterial.SetFloat(FogHeightFalloffId, fogHeightFalloff);
            fogMaterial.SetFloat(FogBaseHeightId, fogBaseHeight);
        }

        private void ApplyFogImmediate()
        {
            CalculateTargetFog();

            currentFogColor = targetFogColor;
            currentFogDensity = targetFogDensity;
            currentFogStart = targetFogStart;
            currentFogEnd = targetFogEnd;

            ApplyFogToMaterial();
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
            fogEnabled = biome.OverrideFog;
        }

        public void SetFogDirect(Color color, float density, float start, float end, bool immediate = false)
        {
            baseFogColor = color;
            baseFogDensity = density;
            fogStartDistance = start;
            fogEndDistance = end;

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

            if (fogMaterial != null)
            {
                DestroyImmediate(fogMaterial);
            }

            if (Instance == this) Instance = null;
        }
    }
}