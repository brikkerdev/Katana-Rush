using UnityEngine;

namespace Runner.UI
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CircularStaminaBar : MonoBehaviour
    {
        [Header("Ring Settings")]
        [SerializeField] private float radius = 0.4f;
        [SerializeField] private float thickness = 0.08f;
        [SerializeField] private float smoothness = 0.01f;
        [SerializeField] private float outlineThickness = 0.005f;

        [Header("Segment Settings")]
        [SerializeField] private int segmentCount = 3;
        [SerializeField] private float segmentGap = 0.02f;

        [Header("Colors")]
        [SerializeField] private Color readyColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color rechargeColor = new Color(0.8f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color rechargeBackgroundColor = new Color(0.3f, 0.3f, 0.1f, 0.8f);
        [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color useEffectColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.8f);

        [Header("Effects")]
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseIntensity = 0.3f;
        [SerializeField] private float glowIntensity = 0.2f;
        [SerializeField] private float useEffectFadeSpeed = 2f;

        [Header("Animation")]
        [SerializeField] private float segmentFillSpeed = 5f;
        [SerializeField] private float segmentDrainSpeed = 8f;

        [Header("Visibility")]
        [SerializeField] private bool autoHide = true;
        [SerializeField] private float hideDelay = 2f;
        [SerializeField] private float fadeSpeed = 3f;
        [SerializeField] private float showFadeSpeed = 8f;

        private SpriteRenderer spriteRenderer;
        private Material material;
        private MaterialPropertyBlock propertyBlock;

        private float[] segmentValues;
        private float[] segmentTargets;
        private float[] useEffectValues;

        private float rechargeProgress;
        private int rechargeSegmentIndex;
        private bool isPulsing;
        private float hideTimer;
        private float currentAlpha = 0f;
        private float targetAlpha = 0f;
        private bool forceVisible;

        private bool isInitialized;

        private static readonly int RadiusID = Shader.PropertyToID("_Radius");
        private static readonly int ThicknessID = Shader.PropertyToID("_Thickness");
        private static readonly int SmoothnessID = Shader.PropertyToID("_Smoothness");
        private static readonly int SegmentCountID = Shader.PropertyToID("_SegmentCount");
        private static readonly int SegmentGapID = Shader.PropertyToID("_SegmentGap");
        private static readonly int SegmentValuesID = Shader.PropertyToID("_SegmentValues");
        private static readonly int SegmentValues2ID = Shader.PropertyToID("_SegmentValues2");
        private static readonly int RechargeProgressID = Shader.PropertyToID("_RechargeProgress");
        private static readonly int RechargeSegmentID = Shader.PropertyToID("_RechargeSegment");
        private static readonly int UseEffectValuesID = Shader.PropertyToID("_UseEffectValues");
        private static readonly int UseEffectValues2ID = Shader.PropertyToID("_UseEffectValues2");
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
        private static readonly int OutlineThicknessID = Shader.PropertyToID("_OutlineThickness");

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            propertyBlock = new MaterialPropertyBlock();

            CreateMaterial();
            InitializeArrays(segmentCount);

            currentAlpha = 0f;
            targetAlpha = 0f;
            UpdateSpriteAlpha();
        }

        private void CreateMaterial()
        {
            Shader shader = Shader.Find("Unlit/CircularStaminaBar");

            if (shader == null)
            {
                Debug.LogError("[CircularStaminaBar] Shader not found!");
                return;
            }

            material = new Material(shader);
            spriteRenderer.material = material;

            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = CreateQuadSprite();
            }
        }

        private Sprite CreateQuadSprite()
        {
            Texture2D texture = new Texture2D(4, 4);
            Color[] colors = new Color[16];

            for (int i = 0; i < 16; i++)
            {
                colors[i] = Color.white;
            }

            texture.SetPixels(colors);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private void InitializeArrays(int count)
        {
            segmentValues = new float[Mathf.Max(count, 8)];
            segmentTargets = new float[Mathf.Max(count, 8)];
            useEffectValues = new float[Mathf.Max(count, 8)];

            for (int i = 0; i < segmentValues.Length; i++)
            {
                segmentValues[i] = 1f;
                segmentTargets[i] = 1f;
                useEffectValues[i] = 1f;
            }

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || material == null) return;

            UpdateSegmentValues();
            UpdateUseEffects();
            UpdateVisibility();
            UpdateMaterial();
        }

        private void UpdateSegmentValues()
        {
            for (int i = 0; i < segmentCount; i++)
            {
                float target = segmentTargets[i];
                float current = segmentValues[i];

                if (Mathf.Abs(current - target) > 0.001f)
                {
                    float speed = target > current ? segmentFillSpeed : segmentDrainSpeed;
                    segmentValues[i] = Mathf.MoveTowards(current, target, Time.deltaTime * speed);
                }
            }
        }

        private void UpdateUseEffects()
        {
            for (int i = 0; i < segmentCount; i++)
            {
                if (useEffectValues[i] > segmentValues[i])
                {
                    useEffectValues[i] = Mathf.MoveTowards(
                        useEffectValues[i],
                        segmentValues[i],
                        Time.deltaTime * useEffectFadeSpeed
                    );
                }
            }
        }

        private void UpdateVisibility()
        {
            if (forceVisible)
            {
                targetAlpha = 1f;
            }
            else if (autoHide)
            {
                bool shouldShow = false;

                for (int i = 0; i < segmentCount; i++)
                {
                    if (segmentTargets[i] < 1f || Mathf.Abs(segmentValues[i] - segmentTargets[i]) > 0.01f)
                    {
                        shouldShow = true;
                        break;
                    }
                }

                if (shouldShow)
                {
                    targetAlpha = 1f;
                    hideTimer = hideDelay;
                }
                else
                {
                    hideTimer -= Time.deltaTime;

                    if (hideTimer <= 0f)
                    {
                        targetAlpha = 0f;
                    }
                }
            }

            float fadeSpeedToUse = targetAlpha > currentAlpha ? showFadeSpeed : fadeSpeed;
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeedToUse);

            UpdateSpriteAlpha();
        }

        private void UpdateSpriteAlpha()
        {
            Color color = spriteRenderer.color;
            color.a = currentAlpha;
            spriteRenderer.color = color;
        }

        private void UpdateMaterial()
        {
            if (currentAlpha < 0.01f) return;

            spriteRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetFloat(RadiusID, radius);
            propertyBlock.SetFloat(ThicknessID, thickness);
            propertyBlock.SetFloat(SmoothnessID, smoothness);
            propertyBlock.SetFloat(OutlineThicknessID, outlineThickness);

            propertyBlock.SetInt(SegmentCountID, segmentCount);
            propertyBlock.SetFloat(SegmentGapID, segmentGap);

            Vector4 segmentVec1 = new Vector4(
                segmentValues[0],
                segmentCount > 1 ? segmentValues[1] : 1f,
                segmentCount > 2 ? segmentValues[2] : 1f,
                segmentCount > 3 ? segmentValues[3] : 1f
            );

            Vector4 segmentVec2 = new Vector4(
                segmentCount > 4 ? segmentValues[4] : 1f,
                segmentCount > 5 ? segmentValues[5] : 1f,
                segmentCount > 6 ? segmentValues[6] : 1f,
                segmentCount > 7 ? segmentValues[7] : 1f
            );

            propertyBlock.SetVector(SegmentValuesID, segmentVec1);
            propertyBlock.SetVector(SegmentValues2ID, segmentVec2);

            Vector4 useVec1 = new Vector4(
                useEffectValues[0],
                segmentCount > 1 ? useEffectValues[1] : 1f,
                segmentCount > 2 ? useEffectValues[2] : 1f,
                segmentCount > 3 ? useEffectValues[3] : 1f
            );

            Vector4 useVec2 = new Vector4(
                segmentCount > 4 ? useEffectValues[4] : 1f,
                segmentCount > 5 ? useEffectValues[5] : 1f,
                segmentCount > 6 ? useEffectValues[6] : 1f,
                segmentCount > 7 ? useEffectValues[7] : 1f
            );

            propertyBlock.SetVector(UseEffectValuesID, useVec1);
            propertyBlock.SetVector(UseEffectValues2ID, useVec2);

            propertyBlock.SetFloat(RechargeProgressID, rechargeProgress);
            propertyBlock.SetInt(RechargeSegmentID, rechargeSegmentIndex);

            propertyBlock.SetColor(ReadyColorID, readyColor);
            propertyBlock.SetColor(RechargeColorID, rechargeColor);
            propertyBlock.SetColor(RechargeBackgroundColorID, rechargeBackgroundColor);
            propertyBlock.SetColor(EmptyColorID, emptyColor);
            propertyBlock.SetColor(UseEffectColorID, useEffectColor);
            propertyBlock.SetColor(OutlineColorID, outlineColor);

            propertyBlock.SetFloat(PulseSpeedID, pulseSpeed);
            propertyBlock.SetFloat(PulseIntensityID, pulseIntensity);
            propertyBlock.SetFloat(PulseActiveID, isPulsing ? 1f : 0f);
            propertyBlock.SetFloat(GlowIntensityID, glowIntensity);

            spriteRenderer.SetPropertyBlock(propertyBlock);
        }

        public void SetSegmentCount(int count)
        {
            segmentCount = Mathf.Clamp(count, 1, 8);

            if (segmentValues == null || segmentValues.Length < count)
            {
                InitializeArrays(count);
            }
        }

        public void SetDashCount(int current, int max)
        {
            SetSegmentCount(max);

            for (int i = 0; i < max; i++)
            {
                segmentTargets[i] = i < current ? 1f : 0f;
            }

            isPulsing = current == 0;

            int firstEmpty = current;
            if (firstEmpty < max && firstEmpty >= 0)
            {
                rechargeSegmentIndex = firstEmpty;
            }
        }

        public void SetRechargeProgress(float progress)
        {
            rechargeProgress = Mathf.Clamp01(progress);

            if (rechargeSegmentIndex >= 0 && rechargeSegmentIndex < segmentCount)
            {
                segmentValues[rechargeSegmentIndex] = progress;
                segmentTargets[rechargeSegmentIndex] = progress;
            }
        }

        public void OnDashUsed(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= segmentCount) return;

            useEffectValues[segmentIndex] = segmentValues[segmentIndex];
            segmentTargets[segmentIndex] = 0f;

            isPulsing = true;

            for (int i = 0; i < segmentCount; i++)
            {
                if (segmentTargets[i] > 0f)
                {
                    isPulsing = false;
                    break;
                }
            }

            ShowBar();
        }

        public void OnDashRestored(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= segmentCount) return;

            segmentTargets[segmentIndex] = 1f;
            segmentValues[segmentIndex] = 1f;
            useEffectValues[segmentIndex] = 1f;

            bool anyEmpty = false;
            for (int i = 0; i < segmentCount; i++)
            {
                if (segmentTargets[i] < 1f)
                {
                    anyEmpty = true;
                    rechargeSegmentIndex = i;
                    break;
                }
            }

            isPulsing = false;

            if (!anyEmpty)
            {
                rechargeSegmentIndex = -1;
            }
        }

        public void RestoreAll()
        {
            for (int i = 0; i < segmentValues.Length; i++)
            {
                segmentTargets[i] = 1f;
                segmentValues[i] = 1f;
                useEffectValues[i] = 1f;
            }

            isPulsing = false;
            rechargeProgress = 0f;
            rechargeSegmentIndex = -1;
        }

        public void ShowBar()
        {
            targetAlpha = 1f;
            hideTimer = hideDelay;
            spriteRenderer.enabled = true;
            forceVisible = false;
        }

        public void HideBar()
        {
            targetAlpha = 0f;
            spriteRenderer.enabled = false;
            forceVisible = false;
        }

        public void SetForceVisible(bool visible)
        {
            forceVisible = visible;

            if (visible)
            {
                targetAlpha = 1f;
            }
        }

        public void InstantHide()
        {
            currentAlpha = 0f;
            targetAlpha = 0f;
            UpdateSpriteAlpha();
        }

        public void InstantShow()
        {
            currentAlpha = 1f;
            targetAlpha = 1f;
            UpdateSpriteAlpha();
        }

        public void SetColors(Color ready, Color recharge, Color rechargeBackground, Color empty, Color useEffect, Color outline)
        {
            readyColor = ready;
            rechargeColor = recharge;
            rechargeBackgroundColor = rechargeBackground;
            emptyColor = empty;
            useEffectColor = useEffect;
            outlineColor = outline;
        }

        public void SetRingParameters(float newRadius, float newThickness)
        {
            radius = newRadius;
            thickness = newThickness;
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            if (!isInitialized) return;

            SetSegmentCount(segmentCount);
        }

        [ContextMenu("Test Use Dash")]
        private void TestUseDash()
        {
            for (int i = segmentCount - 1; i >= 0; i--)
            {
                if (segmentTargets[i] > 0f)
                {
                    OnDashUsed(i);
                    break;
                }
            }
        }

        [ContextMenu("Test Restore Dash")]
        private void TestRestoreDash()
        {
            for (int i = 0; i < segmentCount; i++)
            {
                if (segmentTargets[i] < 1f)
                {
                    OnDashRestored(i);
                    break;
                }
            }
        }

        [ContextMenu("Restore All")]
        private void TestRestoreAll()
        {
            RestoreAll();
        }

        [ContextMenu("Show Bar")]
        private void TestShowBar()
        {
            ShowBar();
        }

        [ContextMenu("Hide Bar")]
        private void TestHideBar()
        {
            HideBar();
        }
#endif
    }
}