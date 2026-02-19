using UnityEngine;
using TMPro;
using DG.Tweening;
using Runner.Core;

namespace Runner.UI
{
    public class GameplayScreen : UIScreen
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private int scoreDigits = 7;

        [SerializeField] private UIButton pauseButton;

        [Header("Multiplier")]
        [SerializeField] private GameObject multiplierContainer;
        [SerializeField] private TextMeshProUGUI multiplierText;

        [Header("Power-Up Timers")]
        [SerializeField] private PowerUpTimerBar magnetTimerBar;
        [SerializeField] private PowerUpTimerBar multiplierTimerBar;
        [SerializeField] private PowerUpTimerBar speedBoostTimerBar;

        [Header("Countdown")]
        [SerializeField] private GameObject countdownContainer;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private string go_key = "ui_go";

        [Header("Distance Milestones")]
        [SerializeField] private float distanceMilestoneInterval = 100f;

        [Header("Tween Settings")]
        [SerializeField] private float coinPunchScale = 0.25f;
        [SerializeField] private float scoreCountDuration = 0.3f;

        private int currentCoins;
        private int displayedScore;
        private bool isCountingDown;
        private int lastScoreValue;
        private Color scoreBaseColor;

        private Tween scoreAnimTween;
        private Tween scoreColorTween;
        private Tween scoreShakeTween;
        private Tween coinPunchTween;
        private Tween multiplierScaleTween;
        private Tween multiplierPunchTween;

        private bool magnetActive;
        private bool multiplierActive;
        private bool speedBoostActive;

        public bool IsCountingDown => isCountingDown;

        protected override void Awake()
        {
            base.Awake();
            screenType = ScreenType.Gameplay;

            if (pauseButton != null)
            {
                pauseButton.OnClick += OnPauseClicked;
            }
        }

        private void CacheBaseColor()
        {
            if (scoreText != null)
            {
                scoreBaseColor = scoreText.color;
            }
            else
            {
                scoreBaseColor = Color.white;
            }
        }

        protected override void OnShow()
        {
            base.OnShow();

            bool isResumingFromPause = Game.Instance != null &&
                                       Game.Instance.State == GameState.Paused;

            if (!isResumingFromPause)
            {
                ResetDisplay();
            }

            SyncWithGameState();
            SubscribeToGameEvents();

            if (countdownContainer != null && !isCountingDown)
            {
                countdownContainer.SetActive(false);
            }
        }

        public override void Hide(bool instant = false)
        {
            UnsubscribeFromGameEvents();
            KillGameplayTweens();
            base.Hide(instant);
        }

        private void SubscribeToGameEvents()
        {
            if (Game.Instance == null) return;
            Game.Instance.OnScoreChanged += HandleScoreChanged;
            Game.Instance.OnMultiplierChanged += HandleMultiplierChanged;
            Game.Instance.OnMagnetChanged += HandleMagnetChanged;
            Game.Instance.OnSpeedBoostChanged += HandleSpeedBoostChanged;
        }

        private void UnsubscribeFromGameEvents()
        {
            if (Game.Instance == null) return;
            Game.Instance.OnScoreChanged -= HandleScoreChanged;
            Game.Instance.OnMultiplierChanged -= HandleMultiplierChanged;
            Game.Instance.OnMagnetChanged -= HandleMagnetChanged;
            Game.Instance.OnSpeedBoostChanged -= HandleSpeedBoostChanged;
        }

        private void SyncWithGameState()
        {
            if (Game.Instance == null) return;

            scoreAnimTween?.Kill();
            displayedScore = Game.Instance.Score;
            lastScoreValue = displayedScore;
            UpdateScoreDisplay(displayedScore);
            ShowMultiplier(Game.Instance.ScoreMultiplier);
        }

        private void ResetDisplay()
        {
            currentCoins = 0;
            displayedScore = 0;
            lastScoreValue = 0;

            UpdateCoinsDisplay(0);
            UpdateScoreDisplay(0);

            if (scoreText != null)
            {
                scoreColorTween?.Kill();
            }

            if (multiplierContainer != null)
            {
                multiplierContainer.SetActive(false);
            }

            magnetActive = false;
            multiplierActive = false;
            speedBoostActive = false;
            HideAllTimerBars();
        }

        private void HandleScoreChanged(int newScore)
        {
            int delta = newScore - lastScoreValue;
            lastScoreValue = newScore;

            scoreAnimTween?.Kill();

            scoreAnimTween = DOTween.To(
                () => (float)displayedScore,
                x =>
                {
                    displayedScore = Mathf.RoundToInt(x);
                    UpdateScoreDisplay(displayedScore);
                },
                newScore,
                scoreCountDuration
            ).SetUpdate(true).SetEase(Ease.OutQuad);
        }

        private void UpdateScoreDisplay(int score)
        {
            if (scoreText != null)
            {
                string padded = score.ToString().PadLeft(scoreDigits, '0');
                scoreText.text = $"<mspace=0.6em>{padded}</mspace>";
            }
        }

        public void AddCoins(int amount)
        {
            currentCoins += amount;
            UpdateCoinsDisplay(currentCoins);

            if (coinsText != null)
            {
                coinPunchTween?.Kill();
                coinsText.transform.localScale = Vector3.one;
                coinPunchTween = coinsText.transform
                    .DOPunchScale(Vector3.one * coinPunchScale, 0.3f, 6, 0.5f)
                    .SetUpdate(true);
            }
        }

        private void UpdateCoinsDisplay(int coins)
        {
            if (coinsText != null)
            {
                coinsText.text = coins.ToString();
            }
        }

        private void HandleMultiplierChanged(int multiplier)
        {
            ShowMultiplier(multiplier);

            if (multiplier > 1)
            {
                multiplierActive = true;
                if (multiplierTimerBar != null)
                    multiplierTimerBar.Show();
            }
            else
            {
                multiplierActive = false;
                if (multiplierTimerBar != null)
                    multiplierTimerBar.Hide();
            }
        }

        private void HandleMagnetChanged(bool active)
        {
            magnetActive = active;
            if (magnetTimerBar != null)
            {
                if (active)
                    magnetTimerBar.Show();
                else
                    magnetTimerBar.Hide();
            }
        }

        private void HandleSpeedBoostChanged(bool active)
        {
            speedBoostActive = active;
            if (speedBoostTimerBar != null)
            {
                if (active)
                    speedBoostTimerBar.Show();
                else
                    speedBoostTimerBar.Hide();
            }
        }

        public void ShowMultiplier(int multiplier)
        {
            if (multiplierContainer == null) return;

            multiplierScaleTween?.Kill();
            multiplierPunchTween?.Kill();

            if (multiplier > 1)
            {
                if (multiplierText != null)
                {
                    multiplierText.text = $"x{multiplier}";
                }

                if (!multiplierContainer.activeSelf)
                {
                    multiplierContainer.SetActive(true);
                    multiplierContainer.transform.localScale = Vector3.zero;
                    multiplierScaleTween = multiplierContainer.transform
                        .DOScale(1f, 0.4f)
                        .SetUpdate(true)
                        .SetEase(Ease.OutBack);
                }
                else
                {
                    multiplierContainer.transform.localScale = Vector3.one;
                    multiplierPunchTween = multiplierContainer.transform
                        .DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f)
                        .SetUpdate(true);
                }
            }
            else
            {
                multiplierScaleTween = multiplierContainer.transform
                    .DOScale(0f, 0.3f)
                    .SetUpdate(true)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => multiplierContainer.SetActive(false));
            }
        }

        public void ShowCountdown(int seconds)
        {
            StartCoroutine(CountdownRoutine(seconds));
        }

        private System.Collections.IEnumerator CountdownRoutine(int seconds)
        {
            isCountingDown = true;

            if (pauseButton != null)
            {
                pauseButton.SetInteractable(false);
            }

            if (countdownContainer != null)
            {
                countdownContainer.SetActive(true);
            }

            for (int i = seconds; i > 0; i--)
            {
                if (countdownText != null)
                {
                    countdownText.text = i.ToString();
                    countdownText.transform.DOKill();
                    countdownText.transform.localScale = Vector3.one * 1.8f;
                    countdownText.transform
                        .DOScale(1f, 0.5f)
                        .SetUpdate(true)
                        .SetEase(Ease.OutBack);
                }

                Game.Instance?.Sound?.PlayCountdownTick();

                yield return new WaitForSecondsRealtime(1f);
            }

            if (countdownText != null)
            {
                countdownText.text = LocalizationController.Singleton.GetText(go_key);
                countdownText.transform.DOKill();
                countdownText.transform.localScale = Vector3.zero;
                countdownText.transform
                    .DOScale(1.3f, 0.2f)
                    .SetUpdate(true)
                    .SetEase(Ease.OutBack);
            }

            Game.Instance?.Sound?.PlayCountdownGo();

            yield return new WaitForSecondsRealtime(0.3f);

            if (countdownText != null)
            {
                countdownText.transform.DOKill();
                countdownText.transform
                    .DOScale(0f, 0.15f)
                    .SetUpdate(true)
                    .SetEase(Ease.InBack);
            }

            yield return new WaitForSecondsRealtime(0.2f);

            if (countdownContainer != null)
            {
                countdownContainer.SetActive(false);
            }

            if (pauseButton != null)
            {
                pauseButton.SetInteractable(true);
            }

            isCountingDown = false;
        }

        public void CancelCountdown()
        {
            StopAllCoroutines();
            isCountingDown = false;

            if (countdownText != null)
            {
                countdownText.transform.DOKill();
            }

            if (countdownContainer != null)
            {
                countdownContainer.SetActive(false);
            }

            if (pauseButton != null)
            {
                pauseButton.SetInteractable(true);
            }
        }

        private void KillGameplayTweens()
        {
            scoreAnimTween?.Kill();
            scoreColorTween?.Kill();
            scoreShakeTween?.Kill();
            coinPunchTween?.Kill();
            multiplierScaleTween?.Kill();
            multiplierPunchTween?.Kill();

            if (scoreText != null)
            {
                scoreText.transform.DOKill();
                scoreText.rectTransform.anchoredPosition = Vector2.zero;
            }
            if (coinsText != null) coinsText.transform.DOKill();
            if (countdownText != null) countdownText.transform.DOKill();
            if (multiplierContainer != null) multiplierContainer.transform.DOKill();
        }

        private void OnPauseClicked()
        {
            if (isCountingDown) return;
            UIManager.Instance?.PauseGame();
        }

        private void Update()
        {
            UpdateTimerBars();
        }

        private void UpdateTimerBars()
        {
            if (Game.Instance == null) return;

            if (magnetActive && magnetTimerBar != null && Game.Instance.MagnetDuration > 0f)
            {
                magnetTimerBar.SetFill(Game.Instance.MagnetTimeRemaining / Game.Instance.MagnetDuration);
            }

            if (multiplierActive && multiplierTimerBar != null && Game.Instance.MultiplierDuration > 0f)
            {
                multiplierTimerBar.SetFill(Game.Instance.MultiplierTimeRemaining / Game.Instance.MultiplierDuration);
            }

            if (speedBoostActive && speedBoostTimerBar != null && Game.Instance.SpeedBoostDuration > 0f)
            {
                speedBoostTimerBar.SetFill(Game.Instance.SpeedBoostTimeRemaining / Game.Instance.SpeedBoostDuration);
            }
        }

        private void HideAllTimerBars()
        {
            if (magnetTimerBar != null) magnetTimerBar.gameObject.SetActive(false);
            if (multiplierTimerBar != null) multiplierTimerBar.gameObject.SetActive(false);
            if (speedBoostTimerBar != null) speedBoostTimerBar.gameObject.SetActive(false);
        }

        private new void OnDestroy()
        {
            UnsubscribeFromGameEvents();
            KillGameplayTweens();

            if (pauseButton != null)
            {
                pauseButton.OnClick -= OnPauseClicked;
            }

            base.OnDestroy();
        }
    }
}