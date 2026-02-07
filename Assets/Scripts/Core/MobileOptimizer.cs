using UnityEngine;

namespace Runner.Core
{
    public class MobileOptimizer : MonoBehaviour
    {
        [Header("Target Frame Rate")]
        [SerializeField] private int targetFPS = 60;

        [Header("Physics")]
        [SerializeField] private float physicsUpdateRate = 0.02f;

        [Header("Debug")]
        [SerializeField] private bool showFPS = true;

        private float deltaTime;
        private float fps;

        private void Awake()
        {
            Application.targetFrameRate = targetFPS;
            QualitySettings.vSyncCount = 0;

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
        }
#endif
    }
}