using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Runner.Core;

namespace Runner.UI
{
    public class GameplayScreen : UIScreen
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private string distanceFormat = "{0:F0}m";

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

        private int currentCoins;
        private float displayedDistance;
        private float distanceAnimationSpeed = 50f;
        private bool isCountingDown;
        private int lastDistanceMilestone;

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

            if (countdownContainer != null && !isCountingDown)
            {
                countdownContainer.SetActive(false);
            }
        }

        private void ResetDisplay()
        {
            displayedDistance = 0f;
            currentCoins = 0;
            lastDistanceMilestone = 0;
            UpdateDistanceDisplay(0f);
            UpdateCoinsDisplay(0);

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
            }
        }

        private void UpdateDistanceDisplay(float distance)
        {
            if (distanceText != null)
            {
                distanceText.text = string.Format(distanceFormat, distance);
            }
        }

        public void AddCoins(int amount)
        {
            currentCoins += amount;
            UpdateCoinsDisplay(currentCoins);
        }

        private void UpdateCoinsDisplay(int coins)
        {
            if (coinsText != null)
            {
                coinsText.text = coins.ToString();
            }
        }

        public void ShowMultiplier(int multiplier)
        {
            if (multiplierContainer == null) return;

            multiplierContainer.SetActive(multiplier > 1);

            if (multiplierText != null)
            {
                multiplierText.text = $"x{multiplier}";
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
                }

                Game.Instance?.Sound?.PlayCountdownTick();

                yield return new WaitForSecondsRealtime(1f);
            }

            if (countdownText != null)
            {
                countdownText.text = LocalizationController.Singleton.GetText(go_key);
            }

            Game.Instance?.Sound?.PlayCountdownGo();

            yield return new WaitForSecondsRealtime(0.3f);

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

            if (countdownContainer != null)
            {
                countdownContainer.SetActive(false);
            }

            if (pauseButton != null)
            {
                pauseButton.SetInteractable(true);
            }
        }

        private void OnPauseClicked()
        {
            if (isCountingDown) return;
            UIManager.Instance?.PauseGame();
        }

        private void OnDestroy()
        {
            if (pauseButton != null)
            {
                pauseButton.OnClick -= OnPauseClicked;
            }
        }
    }
}