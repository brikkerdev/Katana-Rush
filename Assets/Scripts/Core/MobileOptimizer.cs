using UnityEngine;

namespace Runner.Core
{
    public class MobileOptimizer : MonoBehaviour
    {
        [Header("Frame Rate")]
        [SerializeField] private bool useDeviceRefreshRate = true;
        [SerializeField] private bool useGraphicsSettings = true;
        [SerializeField] private int fallbackFPS = 120;

        [Header("Physics")]
        [SerializeField] private float physicsUpdateRate = 0.02f;

        [Header("Debug")]
        [SerializeField] private bool showFPS = true;

        private float deltaTime;
        private float fps;
        private int currentFPS;

        private void Awake()
        {
            // Apply graphics settings if GraphicsSettingsManager exists
            if (useGraphicsSettings && GraphicsSettingsManager.Instance != null)
            {
                GraphicsSettingsManager.Instance.ApplySavedPreset();
            }
            
            // Get the device's maximum supported refresh rate
            int targetFPS = fallbackFPS;
            
            if (useDeviceRefreshRate)
            {
                Resolution currentRes = Screen.currentResolution;
                targetFPS = (int)currentRes.refreshRateRatio.value;
                
                // Ensure we have a valid FPS value
                if (targetFPS < 60) targetFPS = fallbackFPS;
            }
            
            // If using graphics settings, get FPS from there instead
            if (useGraphicsSettings && GraphicsSettingsManager.Instance != null)
            {
                GraphicsPreset preset = GraphicsSettingsManager.Instance.CurrentPreset;
                if (preset != null)
                {
                    targetFPS = preset.targetFPS;
                }
            }
            
            Application.targetFrameRate = targetFPS;
            QualitySettings.vSyncCount = 0;
            currentFPS = targetFPS;

            Time.fixedDeltaTime = physicsUpdateRate;

            Physics.defaultContactOffset = 0.01f;
            Physics.defaultSolverIterations = 4;
            Physics.defaultSolverVelocityIterations = 1;

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            fps = 1f / deltaTime;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!showFPS) return;

            int w = Screen.width;
            int h = Screen.height;

            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.UpperRight;
            style.fontSize = h / 40;
            style.normal.textColor = fps > 55 ? Color.green : fps > 30 ? Color.yellow : Color.red;

            Rect rect = new Rect(w - 100, 10, 90, 25);
            GUI.Label(rect, $"{fps:F0} FPS", style);
            
            // Also show target FPS
            Rect rect2 = new Rect(w - 100, 35, 90, 25);
            style.fontSize = h / 60;
            GUI.Label(rect2, $"Target: {currentFPS}", style);
        }
#endif
    }
}