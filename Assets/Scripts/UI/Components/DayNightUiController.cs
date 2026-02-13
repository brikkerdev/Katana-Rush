using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Runner.Environment;

namespace Runner.UI
{
    public class DayNightUiController : MonoBehaviour
    {
        public static DayNightUiController Instance { get; private set; }

        [Header("Colors")]
        [SerializeField] private Color dayColor = Color.black;
        [SerializeField] private Color nightColor = Color.white;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 1f;
        [SerializeField] private Ease transitionEase = Ease.InOutSine;

        [Header("Settings")]
        [SerializeField] private float checkInterval = 0.5f;

        private DayNightCycle dayNightCycle;
        private Graphic[] graphics;
        private TMP_Text[] texts;
        private Tween[] graphicTweens;
        private Tween[] textTweens;
        private bool wasDay;
        private float checkTimer;
        private bool isInitialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Initialize(DayNightCycle cycle)
        {
            dayNightCycle = cycle;

            graphics = new Graphic[0];
            texts = new TMP_Text[0];
            graphicTweens = new Tween[0];
            textTweens = new Tween[0];

            if (dayNightCycle != null)
            {
                wasDay = dayNightCycle.IsDay;
            }
            else
            {
                wasDay = true;
            }

            isInitialized = true;
        }

        public void RegisterGraphic(Graphic graphic)
        {
            if (graphic == null) return;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == graphic) return;
            }

            int oldLength = graphics.Length;
            System.Array.Resize(ref graphics, oldLength + 1);
            System.Array.Resize(ref graphicTweens, oldLength + 1);
            graphics[oldLength] = graphic;

            Color targetColor = GetCurrentColor();
            graphic.color = targetColor;
        }

        public void RegisterText(TMP_Text text)
        {
            if (text == null) return;

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == text) return;
            }

            int oldLength = texts.Length;
            System.Array.Resize(ref texts, oldLength + 1);
            System.Array.Resize(ref textTweens, oldLength + 1);
            texts[oldLength] = text;

            Color targetColor = GetCurrentColor();
            text.color = targetColor;
        }

        public void UnregisterGraphic(Graphic graphic)
        {
            if (graphic == null) return;

            int index = -1;
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == graphic)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0) return;

            graphicTweens[index]?.Kill();

            for (int i = index; i < graphics.Length - 1; i++)
            {
                graphics[i] = graphics[i + 1];
                graphicTweens[i] = graphicTweens[i + 1];
            }

            System.Array.Resize(ref graphics, graphics.Length - 1);
            System.Array.Resize(ref graphicTweens, graphicTweens.Length - 1);
        }

        public void UnregisterText(TMP_Text text)
        {
            if (text == null) return;

            int index = -1;
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == text)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0) return;

            textTweens[index]?.Kill();

            for (int i = index; i < texts.Length - 1; i++)
            {
                texts[i] = texts[i + 1];
                textTweens[i] = textTweens[i + 1];
            }

            System.Array.Resize(ref texts, texts.Length - 1);
            System.Array.Resize(ref textTweens, textTweens.Length - 1);
        }

        private void Update()
        {
            if (!isInitialized) return;
            if (dayNightCycle == null) return;

            checkTimer += Time.deltaTime;

            if (checkTimer < checkInterval) return;

            checkTimer = 0f;
            CheckDayNightChange();
        }

        private void CheckDayNightChange()
        {
            bool isDay = dayNightCycle.IsDay;

            if (isDay != wasDay)
            {
                wasDay = isDay;
                TransitionAll(isDay ? dayColor : nightColor);
            }
        }

        private Color GetCurrentColor()
        {
            if (dayNightCycle == null) return dayColor;
            return dayNightCycle.IsDay ? dayColor : nightColor;
        }

        private void TransitionAll(Color color)
        {
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;

                graphicTweens[i]?.Kill();
                graphicTweens[i] = graphics[i].DOColor(color, transitionDuration).SetEase(transitionEase);
            }

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null) continue;

                textTweens[i]?.Kill();
                textTweens[i] = texts[i].DOColor(color, transitionDuration).SetEase(transitionEase);
            }
        }

        private void SetAllImmediate(Color color)
        {
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;

                graphicTweens[i]?.Kill();
                graphics[i].color = color;
            }

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null) continue;

                textTweens[i]?.Kill();
                texts[i].color = color;
            }
        }

        public void ForceUpdate()
        {
            if (dayNightCycle == null) return;

            wasDay = dayNightCycle.IsDay;
            SetAllImmediate(wasDay ? dayColor : nightColor);
        }

        public void SetColors(Color day, Color night)
        {
            dayColor = day;
            nightColor = night;
            ForceUpdate();
        }

        private void KillAllTweens()
        {
            if (graphicTweens != null)
            {
                for (int i = 0; i < graphicTweens.Length; i++)
                {
                    graphicTweens[i]?.Kill();
                }
            }

            if (textTweens != null)
            {
                for (int i = 0; i < textTweens.Length; i++)
                {
                    textTweens[i]?.Kill();
                }
            }
        }

        private void OnDestroy()
        {
            KillAllTweens();

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}