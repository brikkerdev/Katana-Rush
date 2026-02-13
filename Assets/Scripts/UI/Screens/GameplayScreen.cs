using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Runner.Core;

namespace Runner.UI
{
    public class GameplayScreen : UIScreen
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private string distanceFormat = "{0:F0}m";
        [SerializeField] private string scoreFormat = "{0}";

        [Header("Buttons")]
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
        [SerializeField] private float scorePunchScale = 0.15f;
        [SerializeField] private float coinPunchScale = 0.25f;
        [SerializeField] private float scoreCountDuration = 0.3f;

        private int currentCoins;
        private int displayedScore;
        private float displayedDistance;
        private float distanceAnimationSpeed = 50f;
        private bool isCountingDown;
        private int lastDistanceMilestone;

        private Tween scoreAnimTween;
        private Tween scorePunchTween;
        private Tween coinPunchTween;
        private Tween multiplierScaleTween;
        private Tween multiplierPunchTween;

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
            UpdateScoreDisplay(displayedScore);
            ShowMultiplier(Game.Instance.ScoreMultiplier);
        }

        private void ResetDisplay()
        {
            displayedDistance = 0f;
            currentCoins = 0;
            displayedScore = 0;
            lastDistanceMilestone = 0;

            UpdateDistanceDisplay(0f);
            UpdateCoinsDisplay(0);
            UpdateScoreDisplay(0);

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

            UpdateDistance();
        }

        private void UpdateDistance()
        {
            float actualDistance = Game.Instance.RunDistance;

            if (displayedDistance < actualDistance)
            {
                displayedDistance = Mathf.MoveTowards(
                    displayedDistance,
                    actualDistance,
                    distanceAnimationSpeed * Time.deltaTime
                );
                UpdateDistanceDisplay(displayedDistance);
            }

            int currentMilestone = (int)(actualDistance / distanceMilestoneInterval);
            if (currentMilestone > lastDistanceMilestone)
            {
                lastDistanceMilestone = currentMilestone;
                Game.Instance?.Sound?.PlayDistanceMilestone();

                if (distanceText != null)
                {
                    distanceText.transform.DOKill();
                    distanceText.transform.localScale = Vector3.one;
                    distanceText.transform
                        .DOPunchScale(Vector3.one * 0.2f, 0.4f, 6, 0.5f)
                        .SetUpdate(true);
                }
            }
        }

        private void UpdateDistanceDisplay(float distance)
        {
            if (distanceText != null)
            {
                distanceText.text = string.Format(distanceFormat, distance);
            }
        }

        private void HandleScoreChanged(int newScore)
        {
            scoreAnimTween?.Kill();

            int startScore = displayedScore;

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

            if (scoreText != null && newScore > 0)
            {
                scorePunchTween?.Kill();
                scoreText.transform.localScale = Vector3.one;
                scorePunchTween = scoreText.transform
                    .DOPunchScale(Vector3.one * scorePunchScale, 0.25f, 5, 0.5f)
                    .SetUpdate(true);
            }
        }

        private void UpdateScoreDisplay(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = string.Format(scoreFormat, score);
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
            scorePunchTween?.Kill();
            coinPunchTween?.Kill();
            multiplierScaleTween?.Kill();
            multiplierPunchTween?.Kill();

            if (scoreText != null) scoreText.transform.DOKill();
            if (coinsText != null) coinsText.transform.DOKill();
            if (distanceText != null) distanceText.transform.DOKill();
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