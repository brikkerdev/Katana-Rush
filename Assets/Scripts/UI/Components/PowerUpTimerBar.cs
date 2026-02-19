using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Runner.UI
{
    [RequireComponent(typeof(Graphic))]
    public class PowerUpTimerBar : MonoBehaviour
    {
        [SerializeField] private float radius = 0.4f;
        [SerializeField] private float thickness = 0.1f;
        [SerializeField] private Color fillColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.8f);
        [SerializeField] private Image iconImage;

        [SerializeField] private Shader shader;
        
        private Graphic graphic;
        private Material runtimeMat;
        private CanvasGroup canvasGroup;

        private static readonly int FillID = Shader.PropertyToID("_Fill");
        private static readonly int RadiusID = Shader.PropertyToID("_Radius");
        private static readonly int ThicknessID = Shader.PropertyToID("_Thickness");
        private static readonly int FillColorID = Shader.PropertyToID("_FillColor");
        private static readonly int BackgroundColorID = Shader.PropertyToID("_BackgroundColor");
        private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");

        private void Awake()
        {
            graphic = GetComponent<Graphic>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            if (shader == null)
            {
                enabled = false;
                return;
            }

            runtimeMat = new Material(shader);
            graphic.material = runtimeMat;

            runtimeMat.SetFloat(RadiusID, radius);
            runtimeMat.SetFloat(ThicknessID, thickness);
            runtimeMat.SetColor(FillColorID, fillColor);
            runtimeMat.SetColor(BackgroundColorID, backgroundColor);
            runtimeMat.SetColor(OutlineColorID, outlineColor);
            runtimeMat.SetFloat(FillID, 1f);

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (runtimeMat != null)
                Destroy(runtimeMat);
        }

        public void SetFill(float normalized)
        {
            if (runtimeMat != null)
                runtimeMat.SetFloat(FillID, Mathf.Clamp01(normalized));
        }

        public void Show()
        {
            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
        }

        public void Hide()
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, 0.25f).SetUpdate(true).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }

        public void SetFillColor(Color color)
        {
            fillColor = color;
            if (runtimeMat != null)
                runtimeMat.SetColor(FillColorID, fillColor);
        }

        public void SetIcon(Sprite sprite)
        {
            if (iconImage != null && sprite != null)
                iconImage.sprite = sprite;
        }
    }
}
