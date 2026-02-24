using UnityEngine;
using System;

namespace Runner.Core
{
    [Serializable]
    public class GraphicsPreset
    {
        public string name;
        public int targetFPS;
        public int antiAliasing;
        public ShadowQuality shadowQuality;
        public ShadowResolution shadowResolution;
        public float shadowDistance;
        public int shadowCascades;
        public bool softParticles;
        public bool realtimeReflectionProbes;
        public float lodBias;
        public int pixelLightCount;
        public bool vsync;
        
        public enum ShadowQuality
        {
            Disabled = 0,
            HardOnly = 1,
            All = 2
        }
    }
    
    public class GraphicsSettingsManager : MonoBehaviour
    {
        public static GraphicsSettingsManager Instance { get; private set; }
        
        [Header("Presets")]
        [SerializeField] private GraphicsPreset lowPreset;
        [SerializeField] private GraphicsPreset mediumPreset;
        [SerializeField] private GraphicsPreset highPreset;
        
        [Header("Settings")]
        [SerializeField] private bool applyOnStart = true;
        
        public GraphicsPreset CurrentPreset { get; private set; }
        public int CurrentPresetIndex { get; private set; } = 1;
        
        public event Action<int> OnGraphicsPresetChanged;
        
        private const string GraphicsPresetKey = "GraphicsPreset";
        
        private GraphicsPreset[] presets;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            SetupDefaultPresets();
        }
        
        private void SetupDefaultPresets()
        {
            // Low preset - optimized for older devices
            lowPreset = new GraphicsPreset
            {
                name = "Low",
                targetFPS = 180,
                antiAliasing = 0,
                shadowQuality = GraphicsPreset.ShadowQuality.Disabled,
                shadowResolution = ShadowResolution.Low,
                shadowDistance = 20f,
                shadowCascades = 1,
                softParticles = false,
                realtimeReflectionProbes = false,
                lodBias = 1f,
                pixelLightCount = 1,
                vsync = false
            };
            
            // Medium preset - balanced
            mediumPreset = new GraphicsPreset
            {
                name = "Medium",
                targetFPS = 180,
                antiAliasing = 2,
                shadowQuality = GraphicsPreset.ShadowQuality.HardOnly,
                shadowResolution = ShadowResolution.Medium,
                shadowDistance = 120f,
                shadowCascades = 2,
                softParticles = false,
                realtimeReflectionProbes = false,
                lodBias = 1.5f,
                pixelLightCount = 2,
                vsync = false
            };
            
            // High preset - maximum quality
            highPreset = new GraphicsPreset
            {
                name = "High",
                targetFPS = 180,
                antiAliasing = 4,
                shadowQuality = GraphicsPreset.ShadowQuality.All,
                shadowResolution = ShadowResolution.High,
                shadowDistance = 240f,
                shadowCascades = 3,
                softParticles = true,
                realtimeReflectionProbes = true,
                lodBias = 2f,
                pixelLightCount = 2,
                vsync = false
            };
            
            presets = new GraphicsPreset[] { lowPreset, mediumPreset, highPreset };
        }
        
        private void Start()
        {
            if (applyOnStart)
            {
                ApplySavedPreset();
            }
        }
        
        public void ApplySavedPreset()
        {
            int savedPreset = PlayerPrefs.GetInt(GraphicsPresetKey, 1);
            SetPreset(savedPreset, false);
        }
        
        public void SetPreset(int presetIndex, bool save = true)
        {
            presetIndex = Mathf.Clamp(presetIndex, 0, 2);
            
            if (presets == null || presets.Length < 3)
            {
                SetupDefaultPresets();
            }
            
            CurrentPreset = presets[presetIndex];
            CurrentPresetIndex = presetIndex;
            
            ApplyPreset(CurrentPreset);
            
            if (save)
            {
                PlayerPrefs.SetInt(GraphicsPresetKey, presetIndex);
                PlayerPrefs.Save();
            }
            
            OnGraphicsPresetChanged?.Invoke(presetIndex);
        }
        
        public void ApplyPreset(GraphicsPreset preset)
        {
            if (preset == null) return;
            
            // Apply FPS
            Application.targetFrameRate = preset.targetFPS;
            
            // Apply VSync
            QualitySettings.vSyncCount = preset.vsync ? 1 : 0;
            
            // Apply Anti-Aliasing
            QualitySettings.antiAliasing = preset.antiAliasing;
            
            // Apply Shadows
            QualitySettings.shadows = (ShadowQuality)preset.shadowQuality;
            QualitySettings.shadowResolution = preset.shadowResolution;
            QualitySettings.shadowDistance = preset.shadowDistance;
            QualitySettings.shadowCascades = preset.shadowCascades;
            
            // Apply other settings
            QualitySettings.softParticles = preset.softParticles;
            QualitySettings.realtimeReflectionProbes = preset.realtimeReflectionProbes;
            QualitySettings.lodBias = preset.lodBias;
            QualitySettings.pixelLightCount = preset.pixelLightCount;
        }
        
        public GraphicsPreset GetPreset(int index)
        {
            index = Mathf.Clamp(index, 0, 2);
            
            if (presets == null || presets.Length < 3)
            {
                SetupDefaultPresets();
            }
            
            return presets[index];
        }
        
        public int GetPresetCount()
        {
            return presets != null ? presets.Length : 3;
        }
        
        public string[] GetPresetNames()
        {
            if (presets == null || presets.Length < 3)
            {
                SetupDefaultPresets();
            }
            
            return new string[] { presets[0].name, presets[1].name, presets[2].name };
        }
        
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
