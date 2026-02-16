using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;
using Runner.Core;
using Runner.Save;

namespace Runner.UI
{
    public class GameOverScreen : UIScreen
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI finalDistanceText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI coinsCollectedText;
        [SerializeField] private GameObject newHighScoreIndicator;

        [Header("Buttons")]
        [SerializeField] private UIButton mainMenuButton;

        [Header("Animation")]
        [SerializeField] private float scoreCountDuration = 1f;
        [SerializeField] private float delayBeforeButtons = 0.5f;
        [SerializeField] private float elementPopDelay = 0.12f;
        [SerializeField] private float elementPopDuration = 0.4f;

        private float finalDistance;
        private int finalScore;
        private int coinsCollected;
        private bool isNewHighScore;

        private Tween distanceCountTween;
        private Tween scoreCountTween;
        private Tween coinsCountTween;

        protected override void Awake()
        {
            base.Awake();
            screenType = ScreenType.GameOver;

            if (mainMenuButton != null)
                mainMenuButton.OnClick += OnMainMenuClicked;
        }

        public void Setup(float distance, int coins, int score)
        {
            finalDistance = distance;
            coinsCollected = coins;
            finalScore = score;

            int previousHighScore = SaveManager.GetHighScore();
            isNewHighScore = (int)distance > previousHighScore;
        }

        protected override void OnShow()
        {
            base.OnShow();
            StartCoroutine(AnimateResults());
        }

        public override void Hide(bool instant = false)
        {
            StopAllCoroutines();
            KillResultTweens();
            base.Hide(instant);
        }

        private IEnumerator AnimateResults()
        {
            SetButtonsInteractable(false);

            if (newHighScoreIndicator != null)
            {
                newHighScoreIndicator.SetActive(false);
            }

            SetupResultElement(finalDistanceText, "0m");
            SetupResultElement(finalScoreText, "0");
            SetupResultElement(coinsCollectedText, "0");

            if (highScoreText != null)
            {
                highScoreText.transform.localScale = Vector3.zero;
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.transform.localScale = Vector3.zero;
            }

            yield return new WaitForSecondsRealtime(0.3f);

            PopInElement(finalDistanceText, 0f);
            PopInElement(finalScoreText, elementPopDelay);
            PopInElement(coinsCollectedText, elementPopDelay * 2f);

            yield return new WaitForSecondsRealtime(elementPopDelay * 3f + elementPopDuration);

            distanceCountTween = CreateCountTween(finalDistanceText, 0, (int)finalDistance, scoreCountDuration, "m");
            scoreCountTween = CreateCountTween(finalScoreText, 0, finalScore, scoreCountDuration, "");
            coinsCountTween = CreateCountTween(coinsCollectedText, 0, coinsCollected, scoreCountDuration, "");

            float countElapsed = 0f;
            while (countElapsed < scoreCountDuration)
            {
                countElapsed += Time.unscaledDeltaTime;
                Game.Instance?.Sound?.PlayScoreTick();
                yield return null;
            }

            if (finalDistanceText != null)
            {
                finalDistanceText.text = $"{(int)finalDistance}m";
                finalDistanceText.transform.DOKill();
                finalDistanceText.transform.localScale = Vector3.one;
                finalDistanceText.transform
                    .DOPunchScale(Vector3.one * 0.1f, 0.2f, 4, 0.5f)
                    .SetUpdate(true);
            }

            if (finalScoreText != null)
            {
                finalScoreText.text = finalScore.ToString();
                finalScoreText.transform.DOKill();
                finalScoreText.transform.localScale = Vector3.one;
                finalScoreText.transform
                    .DOPunchScale(Vector3.one * 0.1f, 0.2f, 4, 0.5f)
                    .SetUpdate(true);
            }

            if (coinsCollectedText != null)
            {
                coinsCollectedText.text = coinsCollected.ToString();
                coinsCollectedText.transform.DOKill();
                coinsCollectedText.transform.localScale = Vector3.one;
                coinsCollectedText.transform
                    .DOPunchScale(Vector3.one * 0.1f, 0.2f, 4, 0.5f)
                    .SetUpdate(true);
            }

            yield return new WaitForSecondsRealtime(0.2f);

            if (highScoreText != null)
            {
                int highScore = SaveManager.GetHighScore();
                highScoreText.text = $"BEST: {highScore}";
                highScoreText.transform
                    .DOScale(1f, elementPopDuration)
                    .SetUpdate(true)
                    .SetEase(Ease.OutBack);
            }

            if (isNewHighScore && newHighScoreIndicator != null)
            {
                newHighScoreIndicator.SetActive(true);
                newHighScoreIndicator.transform.localScale = Vector3.zero;
                newHighScoreIndicator.transform
                    .DOScale(1f, 0.6f)
                    .SetUpdate(true)
                    .SetEase(Ease.OutElastic);
                Game.Instance?.Sound?.PlayNewHighScore();
            }

            yield return new WaitForSecondsRealtime(delayBeforeButtons);

            SetButtonsInteractable(true);

            if (mainMenuButton != null)
            {
                mainMenuButton.transform
                    .DOScale(1f, 0.35f)
                    .SetUpdate(true)
                    .SetEase(Ease.OutBack);
            }
        }

        private void SetupResultElement(TextMeshProUGUI text, string initialValue)
        {
            if (text == null) return;
            text.text = initialValue;
            text.transform.localScale = Vector3.zero;
        }

        private void PopInElement(TextMeshProUGUI text, float delay)
        {
            if (text == null) return;
            text.transform
                .DOScale(1f, elementPopDuration)
                .SetUpdate(true)
                .SetEase(Ease.OutBack)
                .SetDelay(delay);
        }

        private Tween CreateCountTween(TextMeshProUGUI text, int from, int to, float duration, string suffix)
        {
            if (text == null || to <= from) return null;

            float current = from;
            return DOTween.To(
                () => current,
                x =>
                {
                    current = x;
                    text.text = $"{Mathf.RoundToInt(x)}{suffix}";
                },
                (float)to,
                duration
            ).SetUpdate(true).SetEase(Ease.OutCubic);
        }

        private void KillResultTweens()
        {
            distanceCountTween?.Kill();
            scoreCountTween?.Kill();
            coinsCountTween?.Kill();

            if (finalDistanceText != null) finalDistanceText.transform.DOKill();
            if (finalScoreText != null) finalScoreText.transform.DOKill();
            if (coinsCollectedText != null) coinsCollectedText.transform.DOKill();
            if (highScoreText != null) highScoreText.transform.DOKill();
            if (newHighScoreIndicator != null) newHighScoreIndicator.transform.DOKill();
            if (mainMenuButton != null) mainMenuButton.transform.DOKill();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (mainMenuButton != null) mainMenuButton.SetInteractable(interactable);
        }

        private void OnMainMenuClicked()
        {
            UIManager.Instance?.GoToMainMenu();
        }

        private new void OnDestroy()
        {
            KillResultTweens();

            if (mainMenuButton != null) mainMenuButton.OnClick -= OnMainMenuClicked;

            base.OnDestroy();
        }
    }
}