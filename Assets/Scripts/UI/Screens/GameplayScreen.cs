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

        private int currentCoins;
        private float displayedDistance;
        private float distanceAnimationSpeed = 50f;

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
            ResetDisplay();
        }

        private void ResetDisplay()
        {
            displayedDistance = 0f;
            currentCoins = 0;
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

        private void OnPauseClicked()
        {
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