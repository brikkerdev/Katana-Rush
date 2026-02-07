using UnityEngine;
using UnityEngine.UI;

namespace Runner.UI
{
    [RequireComponent(typeof(Graphic))]
    public class UICircularStaminaBar : MonoBehaviour
    {
        [SerializeField] private float radius = 0.4f;
        [SerializeField] private float thickness = 0.08f;
        [SerializeField] private float smoothness = 0.01f;
        [SerializeField] private float outlineThickness = 0.008f;

        [SerializeField, Range(1, 10)] private int segmentCount = 3;
        [SerializeField, Range(0f, 0.45f)] private float segmentGap = 0.10f;

        [SerializeField] private Color readyColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color rechargeColor = new Color(0.8f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color rechargeBackgroundColor = new Color(0.3f, 0.3f, 0.1f, 0.8f);
        [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color useEffectColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.8f);

        [SerializeField] private bool pulseActive;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseIntensity = 0.3f;
        [SerializeField] private float glowIntensity = 0.2f;

        private Graphic graphic;
        private Material runtimeMat;

        private float[] values = new float[10];
        private float[] useValues = new float[10];
        private float rechargeProgress;
        private int rechargeSegment = -1;

        private static readonly int RadiusID = Shader.PropertyToID("_Radius");
        private static readonly int ThicknessID = Shader.PropertyToID("_Thickness");
        private static readonly int SmoothnessID = Shader.PropertyToID("_Smoothness");
        private static readonly int OutlineThicknessID = Shader.PropertyToID("_OutlineThickness");
        private static readonly int SegmentCountID = Shader.PropertyToID("_SegmentCount");
        private static readonly int SegmentGapID = Shader.PropertyToID("_SegmentGap");
        private static readonly int SegmentValuesID = Shader.PropertyToID("_SegmentValues");
        private static readonly int SegmentValues2ID = Shader.PropertyToID("_SegmentValues2");
        private static readonly int SegmentValues3ID = Shader.PropertyToID("_SegmentValues3");
        private static readonly int UseEffectValuesID = Shader.PropertyToID("_UseEffectValues");
        private static readonly int UseEffectValues2ID = Shader.PropertyToID("_UseEffectValues2");
        private static readonly int UseEffectValues3ID = Shader.PropertyToID("_UseEffectValues3");
        private static readonly int RechargeProgressID = Shader.PropertyToID("_RechargeProgress");
        private static readonly int RechargeSegmentID = Shader.PropertyToID("_RechargeSegment");
        private static readonly int ReadyColorID = Shader.PropertyToID("_ReadyColor");
        private static readonly int RechargeColorID = Shader.PropertyToID("_RechargeColor");
        private static readonly int RechargeBackgroundColorID = Shader.PropertyToID("_RechargeBackgroundColor");
        private static readonly int EmptyColorID = Shader.PropertyToID("_EmptyColor");
        private static readonly int UseEffectColorID = Shader.PropertyToID("_UseEffectColor");
        private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
        private static readonly int PulseSpeedID = Shader.PropertyToID("_PulseSpeed");
        private static readonly int PulseIntensityID = Shader.PropertyToID("_PulseIntensity");
        private static readonly int PulseActiveID = Shader.PropertyToID("_PulseActive");
        private static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");

        private void Awake()
        {
            graphic = GetComponent<Graphic>();

            var shader = Shader.Find("UI/CircularStaminaBar");
            if (shader == null)
            {
                enabled = false;
                return;
            }

            runtimeMat = new Material(shader);
            graphic.material = runtimeMat;

            for (int i = 0; i < 10; i++)
            {
                values[i] = 1f;
                useValues[i] = 1f;
            }

            Apply();
        }

        private void OnDestroy()
        {
            if (runtimeMat != null)
                Destroy(runtimeMat);
        }

        private void Update()
        {
            if (runtimeMat == null) return;
            if (pulseActive || glowIntensity > 0.0001f)
                Apply();
        }

        public void SetMaxDashes(int maxDashes)
        {
            segmentCount = Mathf.Clamp(maxDashes, 1, 10);

            for (int i = 0; i < 10; i++)
            {
                values[i] = i < segmentCount ? 1f : 0f;
                useValues[i] = values[i];
            }

            rechargeProgress = 0f;
            rechargeSegment = -1;
            Apply();
        }

        public void SetSegmentGap(float gap)
        {
            segmentGap = Mathf.Clamp(gap, 0f, 0.45f);
            Apply();
        }

        public void SetPulse(bool active)
        {
            pulseActive = active;
            Apply();
        }

        private void Apply()
        {
            runtimeMat.SetFloat(RadiusID, radius);
            runtimeMat.SetFloat(ThicknessID, thickness);
            runtimeMat.SetFloat(SmoothnessID, smoothness);
            runtimeMat.SetFloat(OutlineThicknessID, outlineThickness);

            runtimeMat.SetInt(SegmentCountID, segmentCount);
            runtimeMat.SetFloat(SegmentGapID, segmentGap);

            runtimeMat.SetVector(SegmentValuesID, new Vector4(values[0], values[1], values[2], values[3]));
            runtimeMat.SetVector(SegmentValues2ID, new Vector4(values[4], values[5], values[6], values[7]));
            runtimeMat.SetVector(SegmentValues3ID, new Vector4(values[8], values[9], 1f, 1f));

            runtimeMat.SetVector(UseEffectValuesID, new Vector4(useValues[0], useValues[1], useValues[2], useValues[3]));
            runtimeMat.SetVector(UseEffectValues2ID, new Vector4(useValues[4], useValues[5], useValues[6], useValues[7]));
            runtimeMat.SetVector(UseEffectValues3ID, new Vector4(useValues[8], useValues[9], 1f, 1f));

            runtimeMat.SetFloat(RechargeProgressID, rechargeProgress);
            runtimeMat.SetInt(RechargeSegmentID, rechargeSegment);

            runtimeMat.SetColor(ReadyColorID, readyColor);
            runtimeMat.SetColor(RechargeColorID, rechargeColor);
            runtimeMat.SetColor(RechargeBackgroundColorID, rechargeBackgroundColor);
            runtimeMat.SetColor(EmptyColorID, emptyColor);
            runtimeMat.SetColor(UseEffectColorID, useEffectColor);
            runtimeMat.SetColor(OutlineColorID, outlineColor);

            runtimeMat.SetFloat(PulseSpeedID, pulseSpeed);
            runtimeMat.SetFloat(PulseIntensityID, pulseIntensity);
            runtimeMat.SetFloat(PulseActiveID, pulseActive ? 1f : 0f);
            runtimeMat.SetFloat(GlowIntensityID, glowIntensity);
        }
    }
}