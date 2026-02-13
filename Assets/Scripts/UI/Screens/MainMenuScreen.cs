using UnityEngine;
using TMPro;
using DG.Tweening;
using Runner.Save;

namespace Runner.UI
{
    public class MainMenuScreen : UIScreen
    {
        [Header("Tap To Start")]
        [SerializeField] private TapToStartHandler tapHandler;
        [SerializeField] private TextMeshProUGUI tapToStartText;

        [Header("Menu Buttons")]
        [SerializeField] private UIButton settingsButton;
        [SerializeField] private UIButton inventoryButton;

        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI coinsText;

        [Header("Tween Settings")]
        [SerializeField] private float tapPulseMin = 0.4f;
        [SerializeField] private float tapPulseDuration = 0.8f;
        [SerializeField] private float buttonPopDuration = 0.4f;
        [SerializeField] private float buttonPopDelay = 0.15f;

        private Tween tapPulseTween;

        protected override void Awake()
        {
            base.Awake();
            screenType = ScreenType.MainMenu;
            SetupButtons();
            SetupTapHandler();
        }

        private void SetupButtons()
        {
            if (settingsButton != null)
                settingsButton.OnClick += OnSettingsClicked;

            if (inventoryButton != null)
                inventoryButton.OnClick += OnInventoryClicked;
        }

        private void SetupTapHandler()
        {
            if (tapHandler != null)
            {
                tapHandler.OnTapToStart += OnTapToStart;
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            UpdatePlayerInfo();

            if (tapHandler != null)
            {
                tapHandler.IsEnabled = true;
            }

            if (tapToStartText != null)
            {
                StartTapPulse();
            }

            AnimateElementsIn();
        }

        protected override void OnHide()
        {
            base.OnHide();

            if (tapHandler != null)
            {
                tapHandler.IsEnabled = false;
            }

            KillMenuTweens();
        }

        private void StartTapPulse()
        {
            tapPulseTween?.Kill();

            if (tapToStartText == null) return;

            Color c = tapToStartText.color;
            tapToStartText.color = new Color(c.r, c.g, c.b, 1f);

            tapPulseTween = DOTween.To(
                () => tapToStartText.color.a,
                a => tapToStartText.color = new Color(
                    tapToStartText.color.r,
                    tapToStartText.color.g,
                    tapToStartText.color.b,
                    a),
                tapPulseMin,
                tapPulseDuration
            )
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetEase(Ease.InOutSine);
        }

        private void AnimateElementsIn()
        {
            if (highScoreText != null)
            {
                highScoreText.transform.localScale = Vector3.zero;
                highScoreText.transform
                    .DOScale(1f, buttonPopDuration)
                    .SetUpdate(true)
                    .SetEase(Ease.OutBack)
                    .SetDelay(0.1f);
            }

            if (coinsText != null)
            {
                coinsText.transform.localScale = Vector3.zero;
                coinsText.transform
                    .DOScale(1f, buttonPopDuration)
                    .SetUpdate(true)
                    .SetEase(Ease.OutBack)
                    .SetDelay(0.2f);
            }

            if (settingsButton != null)
            {
                settingsButton.transform.localScale = Vector3.zero;
                settingsButton.transform
                    .DOScale(1f, buttonPopDuration)
                    .SetUpdate(true)
                    .SetEase(Ease.OutBack)
                    .SetDelay(buttonPopDelay);
            }

            if (inventoryButton != null)
            {
                inventoryButton.transform.localScale = Vector3.zero;
                inventoryButton.transform
                    .DOScale(1f, buttonPopDuration)
                    .SetUpdate(true)
                    .SetEase(Ease.OutBack)
                    .SetDelay(buttonPopDelay * 2f);
            }
        }

        private void KillMenuTweens()
        {
            tapPulseTween?.Kill();
            tapPulseTween = null;

            if (tapToStartText != null) tapToStartText.transform.DOKill();
            if (highScoreText != null) highScoreText.transform.DOKill();
            if (coinsText != null) coinsText.transform.DOKill();
            if (settingsButton != null) settingsButton.transform.DOKill();
            if (inventoryButton != null) inventoryButton.transform.DOKill();
        }

        private void UpdatePlayerInfo()
        {
            if (highScoreText != null)
            {
                int highScore = SaveManager.GetHighScore();
                highScoreText.text = $"BEST: {highScore}m";
            }

            if (coinsText != null)
            {
                int coins = SaveManager.GetCoins();
                coinsText.text = coins.ToString();
            }
        }

        private void OnTapToStart()
        {
            if (tapToStartText != null)
            {
                tapPulseTween?.Kill();
                tapToStartText.transform
                    .DOScale(1.3f, 0.15f)
                    .SetUpdate(true)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        tapToStartText.transform
                            .DOScale(0f, 0.1f)
                            .SetUpdate(true)
                            .SetEase(Ease.InQuad);
                    });
            }

            UIManager.Instance?.StartGame();
        }

        private void OnSettingsClicked()
        {
            UIManager.Instance?.ShowScreen(ScreenType.Settings);
        }

        private void OnInventoryClicked()
        {
            UIManager.Instance?.ShowScreen(ScreenType.Inventory);
        }

        private new void OnDestroy()
        {
            KillMenuTweens();

            if (tapHandler != null)
                tapHandler.OnTapToStart -= OnTapToStart;

            if (settingsButton != null)
                settingsButton.OnClick -= OnSettingsClicked;

            if (inventoryButton != null)
                inventoryButton.OnClick -= OnInventoryClicked;

            base.OnDestroy();
        }
    }
}