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
        [SerializeField] private float monoSpaceEm = 0.6f;

        [SerializeField] private UIButton pauseButton;

        [Header("Multiplier")]
        [SerializeField] private GameObject multiplierContainer;
        [SerializeField] private TextMeshProUGUI multiplierText;

        [Header("Countdown")]
        [SerializeField] private GameObject countdownContainer;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private string go_key = "ui_go";

        [Header("Distance Milestones")]
        [SerializeField] private float distanceMilestoneInterval = 100f;

        [Header("Tween Settings")]
        [SerializeField] private float coinPunchScale = 0.25f;
        [SerializeField] private float scoreCountDuration = 0.3f;

        [Header("Score Heat")]
        [SerializeField] private Color scoreHeatColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color scoreMaxHeatColor = new Color(1f, 0.3f, 0.1f);
        [SerializeField] private float heatThreshold = 50f;
        [SerializeField] private float maxHeatThreshold = 150f;
        [SerializeField] private float heatCooldownSpeed = 30f;
        [SerializeField] private float heatColorDuration = 0.3f;

        [Header("Score Shake")]
        [SerializeField] private float shakeStrength = 5f;
        [SerializeField] private int shakeVibrato = 10;
        [SerializeField] private float shakeDuration = 0.4f;
        [SerializeField] private float shakeRandomness = 90f;
        [SerializeField] private float shakeThreshold = 100f;
        [SerializeField] private float shakeCooldown = 0.3f;

        private int currentCoins;
        private int displayedScore;
        private float displayedDistance;
        private float distanceAnimationSpeed = 50f;
        private bool isCountingDown;
        private int lastDistanceMilestone;
        [SerializeField] private float scoreHeat;
        private int lastScoreValue;
        private Color currentScoreTargetColor;
        private bool isHeated;
        private bool isMaxHeated;
        private float lastShakeTime;
        private Color scoreBaseColor;

        private Tween scoreAnimTween;
        private Tween scoreColorTween;
        private Tween scoreShakeTween;
        private Tween coinPunchTween;
        private Tween multiplierScaleTween;
        private Tween multiplierPunchTween;

        public bool IsCountingDown => isCountingDown;

        protected override void Awake()
        {
            base.Awake();
            screenType = ScreenType.Gameplay;
            CacheBaseColor();
            currentScoreTargetColor = scoreBaseColor;

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

        private Color GetCurrentBaseColor()
        {
            if (scoreText == null) return scoreBaseColor;

            if (DayNightUiController.Instance != null)
            {
                return scoreText.color;
            }

            return scoreBaseColor;
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
        }

        private void UnsubscribeFromGameEvents()
        {
            if (Game.Instance == null) return;
            Game.Instance.OnScoreChanged -= HandleScoreChanged;
            Game.Instance.OnMultiplierChanged -= HandleMultiplierChanged;
        }

        private void SyncWithGameState()
        {
            if (Game.Instance == null) return;

            scoreAnimTween?.Kill();
            displayedScore = Game.Instance.Score;
            lastScoreValue = displayedScore;
            scoreHeat = 0f;
            isHeated = false;
            isMaxHeated = false;
            UpdateScoreDisplay(displayedScore);
            ShowMultiplier(Game.Instance.ScoreMultiplier);
        }

        private void ResetDisplay()
        {
            displayedDistance = 0f;
            currentCoins = 0;
            displayedScore = 0;
            lastDistanceMilestone = 0;
            scoreHeat = 0f;
            lastScoreValue = 0;
            isHeated = false;
            isMaxHeated = false;
            lastShakeTime = -shakeCooldown;

            CacheBaseColor();
            currentScoreTargetColor = scoreBaseColor;

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
        }

        private void Update()
        {
            if (!isVisible) return;
            if (Game.Instance == null) return;
            if (Game.Instance.State != GameState.Playing) return;
            if (isCountingDown) return;

            UpdateScoreHeat();
        }

        private void UpdateScoreHeat()
        {
            if (scoreHeat <= 0f) return;

            scoreHeat = Mathf.MoveTowards(scoreHeat, 0f, heatCooldownSpeed * Time.deltaTime);

            if (isMaxHeated && scoreHeat < maxHeatThreshold)
            {
                isMaxHeated = false;
                TransitionScoreColor(scoreHeatColor);
            }

            if (isHeated && scoreHeat < heatThreshold * 0.5f)
            {
                isHeated = false;
                isMaxHeated = false;
                ReturnToBaseColor();
            }
        }

        private void ReturnToBaseColor()
        {
            if (scoreText == null) return;

            currentScoreTargetColor = GetCurrentBaseColor();
            scoreColorTween?.Kill();

            scoreColorTween = scoreText
                .DOColor(currentScoreTargetColor, heatColorDuration)
                .SetUpdate(true)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (DayNightUiController.Instance != null && scoreText != null && !isHeated)
                    {
                        DayNightUiController.Instance.RegisterText(scoreText);
                    }
                });

            if (DayNightUiController.Instance != null)
            {
                DayNightUiController.Instance.UnregisterText(scoreText);
            }
        }

        private void HandleScoreChanged(int newScore)
        {
            int delta = newScore - lastScoreValue;
            lastScoreValue = newScore;

            scoreHeat += delta;

            if (scoreHeat >= maxHeatThreshold && !isMaxHeated)
            {
                isMaxHeated = true;
                isHeated = true;
                TransitionScoreColor(scoreMaxHeatColor);
                TryShakeScore();
            }
            else if (scoreHeat >= shakeThreshold)
            {
                TryShakeScore();

                if (!isMaxHeated && !isHeated)
                {
                    isHeated = true;
                    TransitionScoreColor(scoreHeatColor);
                }
            }
            else if (scoreHeat >= heatThreshold && !isHeated)
            {
                isHeated = true;
                TransitionScoreColor(scoreHeatColor);
            }

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

        private void TryShakeScore()
        {
            if (scoreText == null) return;
            if (Time.unscaledTime - lastShakeTime < shakeCooldown) return;

            lastShakeTime = Time.unscaledTime;

            scoreShakeTween?.Kill();
            scoreText.rectTransform.anchoredPosition = Vector2.zero;

            float intensity = Mathf.Clamp01((scoreHeat - shakeThreshold) / (maxHeatThreshold - shakeThreshold));
            float currentShakeStrength = Mathf.Lerp(shakeStrength * 0.5f, shakeStrength, intensity);

            scoreShakeTween = scoreText.rectTransform
                .DOShakeAnchorPos(
                    shakeDuration,
                    currentShakeStrength,
                    shakeVibrato,
                    shakeRandomness,
                    false,
                    true,
                    ShakeRandomnessMode.Harmonic
                )
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (scoreText != null)
                        scoreText.rectTransform.anchoredPosition = Vector2.zero;
                });
        }

        private void TransitionScoreColor(Color targetColor)
        {
            if (scoreText == null) return;
            if (currentScoreTargetColor == targetColor) return;

            if (DayNightUiController.Instance != null)
            {
                DayNightUiController.Instance.UnregisterText(scoreText);
            }

            currentScoreTargetColor = targetColor;
            scoreColorTween?.Kill();

            scoreColorTween = scoreText
                .DOColor(targetColor, heatColorDuration)
                .SetUpdate(true)
                .SetEase(Ease.OutQuad);
        }

        private void UpdateScoreDisplay(int score)
        {
            if (scoreText != null)
            {
                string padded = score.ToString().PadLeft(scoreDigits, '0');
                scoreText.text = $"<mspace={monoSpaceEm}em>{padded}</mspace>";
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

                if (!isHeated && !isMaxHeated && DayNightUiController.Instance != null)
                {
                    DayNightUiController.Instance.RegisterText(scoreText);
                }
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