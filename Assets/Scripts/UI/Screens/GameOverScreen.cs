using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Runner.Save;

namespace Runner.UI
{
    public class GameOverScreen : UIScreen
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI finalDistanceText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI coinsCollectedText;
        [SerializeField] private GameObject newHighScoreIndicator;

        [Header("Buttons")]
        [SerializeField] private UIButton mainMenuButton;

        [Header("Animation")]
        [SerializeField] private float scoreCountDuration = 1f;
        [SerializeField] private float delayBeforeButtons = 0.5f;

        private float finalDistance;
        private int coinsCollected;
        private bool isNewHighScore;

        protected override void Awake()
        {
            base.Awake();
            screenType = ScreenType.GameOver;

            if (mainMenuButton != null)
                mainMenuButton.OnClick += OnMainMenuClicked;
        }

        public void Setup(float distance, int coins)
        {
            finalDistance = distance;
            coinsCollected = coins;

            int previousHighScore = SaveManager.GetHighScore();
            isNewHighScore = (int)distance > previousHighScore;
        }

        protected override void OnShow()
        {
            base.OnShow();
            StartCoroutine(AnimateResults());
        }

        private IEnumerator AnimateResults()
        {
            SetButtonsInteractable(false);

            if (newHighScoreIndicator != null)
            {
                newHighScoreIndicator.SetActive(false);
            }

            if (finalDistanceText != null)
            {
                finalDistanceText.text = "0m";
            }

            if (coinsCollectedText != null)
            {
                coinsCollectedText.text = "0";
            }

            yield return new WaitForSecondsRealtime(0.3f);

            float elapsed = 0f;
            while (elapsed < scoreCountDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / scoreCountDuration;
                t = 1f - Mathf.Pow(1f - t, 3f);

                if (finalDistanceText != null)
                {
                    int displayDistance = (int)(finalDistance * t);
                    finalDistanceText.text = $"{displayDistance}m";
                }

                if (coinsCollectedText != null)
                {
                    int displayCoins = (int)(coinsCollected * t);
                    coinsCollectedText.text = displayCoins.ToString();
                }

                yield return null;
            }

            if (finalDistanceText != null)
            {
                finalDistanceText.text = $"{(int)finalDistance}m";
            }

            if (coinsCollectedText != null)
            {
                coinsCollectedText.text = coinsCollected.ToString();
            }

            if (highScoreText != null)
            {
                int highScore = SaveManager.GetHighScore();
                highScoreText.text = $"BEST: {highScore}m";
            }

            if (isNewHighScore && newHighScoreIndicator != null)
            {
                newHighScoreIndicator.SetActive(true);
            }

            yield return new WaitForSecondsRealtime(delayBeforeButtons);

            SetButtonsInteractable(true);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (mainMenuButton != null) mainMenuButton.SetInteractable(interactable);
        }

        private void OnMainMenuClicked()
        {
            UIManager.Instance?.GoToMainMenu();
        }

        private void OnDestroy()
        {
            if (mainMenuButton != null) mainMenuButton.OnClick -= OnMainMenuClicked;
        }
    }
}