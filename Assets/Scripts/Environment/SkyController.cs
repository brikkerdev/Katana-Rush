using UnityEngine;
using Runner.LevelGeneration;

namespace Runner.Environment
{
    public class SkyController : MonoBehaviour
    {
        public static SkyController Instance { get; private set; }

        [Header("Sky Material")]
        [SerializeField] private Material skyMaterial;

        [Header("Textures")]
        [SerializeField] private Texture2D sunTexture;
        [SerializeField] private Texture2D moonTexture;
        [SerializeField] private Cubemap starsCubemap;
        [SerializeField] private Texture2D cloudsTexture;

        [Header("Default Day Colors")]
        [SerializeField] private Color daySkyTop = new Color(0.4f, 0.7f, 1f);
        [SerializeField] private Color daySkyHorizon = new Color(0.8f, 0.9f, 1f);

        [Header("Default Night Colors")]
        [SerializeField] private Color nightSkyTop = new Color(0.02f, 0.02f, 0.08f);
        [SerializeField] private Color nightSkyHorizon = new Color(0.1f, 0.1f, 0.2f);

        [Header("Default Sunset Colors")]
        [SerializeField] private Color sunsetTop = new Color(0.5f, 0.3f, 0.5f);
        [SerializeField] private Color sunsetHorizon = new Color(1f, 0.5f, 0.2f);
        [SerializeField] private Color sunsetGlow = new Color(1f, 0.3f, 0.1f);

        [Header("Transition")]
        [SerializeField] private float colorTransitionSpeed = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private DayNightCycle dayNightCycle;
        private BiomeManager biomeManager;

        private Color currentBiomeDayTint = Color.white;
        private Color currentBiomeNightTint = Color.white;
        private Color targetBiomeDayTint = Color.white;
        private Color targetBiomeNightTint = Color.white;

        private static readonly int TimeOfDayID = Shader.PropertyToID("_TimeOfDay");
        private static readonly int SunDirectionID = Shader.PropertyToID("_SunDirection");
        private static readonly int MoonDirectionID = Shader.PropertyToID("_MoonDirection");
        private static readonly int BiomeTintDayID = Shader.PropertyToID("_BiomeTintDay");
        private static readonly int BiomeTintNightID = Shader.PropertyToID("_BiomeTintNight");
        private static readonly int BiomeTintStrengthID = Shader.PropertyToID("_BiomeTintStrength");
        private static readonly int DaySkyColorTopID = Shader.PropertyToID("_DaySkyColorTop");
        private static readonly int DaySkyColorHorizonID = Shader.PropertyToID("_DaySkyColorHorizon");
        private static readonly int NightSkyColorTopID = Shader.PropertyToID("_NightSkyColorTop");
        private static readonly int NightSkyColorHorizonID = Shader.PropertyToID("_NightSkyColorHorizon");
        private static readonly int SunsetColorTopID = Shader.PropertyToID("_SunsetColorTop");
        private static readonly int SunsetColorHorizonID = Shader.PropertyToID("_SunsetColorHorizon");
        private static readonly int SunsetColorGlowID = Shader.PropertyToID("_SunsetColorGlow");

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

            CreateSkyMaterial();
            SetupTextures();
            SetDefaultColors();
            ApplySkybox();

            if (biomeManager != null)
            {
                biomeManager.OnBiomeChanged += OnBiomeChanged;

                if (biomeManager.VisualCurrentBiome != null)
                {
                    ApplyBiomeSkyColors(biomeManager.VisualCurrentBiome, true);
                }
            }

            if (showDebug)
            {
                Debug.Log("[SkyController] Initialized");
            }
        }

        private void CreateSkyMaterial()
        {
            if (skyMaterial != null) return;

            Shader skyShader = Shader.Find("Runner/ProceduralSky");

            if (skyShader == null)
            {
                Debug.LogError("[SkyController] ProceduralSky shader not found!");
                return;
            }

            skyMaterial = new Material(skyShader);
            skyMaterial.name = "ProceduralSkyMaterial";
        }

        private void SetupTextures()
        {
            if (skyMaterial == null) return;

            if (sunTexture != null)
                skyMaterial.SetTexture("_SunTex", sunTexture);

            if (moonTexture != null)
                skyMaterial.SetTexture("_MoonTex", moonTexture);

            if (starsCubemap != null)
                skyMaterial.SetTexture("_StarsCubemap", starsCubemap);

            if (cloudsTexture != null)
                skyMaterial.SetTexture("_CloudsTex", cloudsTexture);
        }

        private void SetDefaultColors()
        {
            if (skyMaterial == null) return;

            skyMaterial.SetColor(DaySkyColorTopID, daySkyTop);
            skyMaterial.SetColor(DaySkyColorHorizonID, daySkyHorizon);
            skyMaterial.SetColor(NightSkyColorTopID, nightSkyTop);
            skyMaterial.SetColor(NightSkyColorHorizonID, nightSkyHorizon);
            skyMaterial.SetColor(SunsetColorTopID, sunsetTop);
            skyMaterial.SetColor(SunsetColorHorizonID, sunsetHorizon);
            skyMaterial.SetColor(SunsetColorGlowID, sunsetGlow);
        }

        private void ApplySkybox()
        {
            if (skyMaterial == null) return;
            RenderSettings.skybox = skyMaterial;
        }

        private void Update()
        {
            if (skyMaterial == null) return;

            UpdateTimeOfDay();
            UpdateCelestialDirections();
            UpdateBiomeTint();
        }

        private void UpdateTimeOfDay()
        {
            float time = 0.5f;

            if (dayNightCycle != null)
            {
                time = dayNightCycle.CurrentTime;
            }

            skyMaterial.SetFloat(TimeOfDayID, time);
        }

        private void UpdateCelestialDirections()
        {
            if (dayNightCycle == null) return;

            float time = dayNightCycle.CurrentTime;
            float angle = time * 360f - 90f;

            Vector3 sunDir = Quaternion.Euler(angle, 0f, 0f) * Vector3.forward;
            Vector3 moonDir = Quaternion.Euler(angle + 180f, 0f, 0f) * Vector3.forward;

            skyMaterial.SetVector(SunDirectionID, sunDir);
            skyMaterial.SetVector(MoonDirectionID, moonDir);
        }

        private void UpdateBiomeTint()
        {
            float speed = colorTransitionSpeed * Time.deltaTime;

            currentBiomeDayTint = Color.Lerp(currentBiomeDayTint, targetBiomeDayTint, speed);
            currentBiomeNightTint = Color.Lerp(currentBiomeNightTint, targetBiomeNightTint, speed);

            skyMaterial.SetColor(BiomeTintDayID, currentBiomeDayTint);
            skyMaterial.SetColor(BiomeTintNightID, currentBiomeNightTint);
        }

        private void OnBiomeChanged(BiomeData biome)
        {
            if (biome == null) return;
            ApplyBiomeSkyColors(biome, false);
        }

        private void ApplyBiomeSkyColors(BiomeData biome, bool immediate)
        {
            if (biome.SkyDayTint != Color.clear)
            {
                targetBiomeDayTint = biome.SkyDayTint;
            }
            else
            {
                targetBiomeDayTint = Color.white;
            }

            if (biome.SkyNightTint != Color.clear)
            {
                targetBiomeNightTint = biome.SkyNightTint;
            }
            else
            {
                targetBiomeNightTint = Color.white;
            }

            float tintStrength = biome.SkyTintStrength;
            skyMaterial.SetFloat(BiomeTintStrengthID, tintStrength);

            if (immediate)
            {
                currentBiomeDayTint = targetBiomeDayTint;
                currentBiomeNightTint = targetBiomeNightTint;
                skyMaterial.SetColor(BiomeTintDayID, currentBiomeDayTint);
                skyMaterial.SetColor(BiomeTintNightID, currentBiomeNightTint);
            }

            if (showDebug)
            {
                Debug.Log($"[SkyController] Applied sky colors for {biome.BiomeName}");
            }
        }

        public void SetTimeOfDay(float time)
        {
            if (skyMaterial == null) return;
            skyMaterial.SetFloat(TimeOfDayID, Mathf.Clamp01(time));
        }

        public void SetCloudSettings(float opacity, float speed)
        {
            if (skyMaterial == null) return;
            skyMaterial.SetFloat("_CloudsOpacity", opacity);
            skyMaterial.SetFloat("_CloudsSpeed", speed);
        }

        public void SetStarsIntensity(float intensity)
        {
            if (skyMaterial == null) return;
            skyMaterial.SetFloat("_StarsIntensity", intensity);
        }

        public void Reset()
        {
            currentBiomeDayTint = Color.white;
            currentBiomeNightTint = Color.white;
            targetBiomeDayTint = Color.white;
            targetBiomeNightTint = Color.white;

            if (skyMaterial != null)
            {
                skyMaterial.SetFloat(BiomeTintStrengthID, 0f);
                skyMaterial.SetFloat(TimeOfDayID, 0.5f);
            }

            if (biomeManager != null && biomeManager.VisualCurrentBiome != null)
            {
                ApplyBiomeSkyColors(biomeManager.VisualCurrentBiome, true);
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