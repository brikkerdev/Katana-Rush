using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace Runner.UI
{
    public class LoadingScreen : UIScreen
    {
        [Header("UI Elements")]
        [SerializeField] private Image progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI tipText;

        [Header("Settings")]
        [SerializeField] private float minDisplayTime = 1f;
        [SerializeField] private float progressSmoothSpeed = 5f;
        [SerializeField] private bool showTips = true;

        [Header("Tips")]
        [SerializeField]
        private string[] tips = new string[]
        {
            "Dash through enemies to defeat them!",
            "Block incoming bullets with your sword!",
            "Change lanes to avoid obstacles!",
            "Double jump to reach higher platforms!",
            "Collect coins to unlock new katanas!"
        };

        [Header("Loading Animation")]
        [SerializeField] private GameObject loadingIcon;
        [SerializeField] private float rotationSpeed = 180f;

        private float targetProgress;
        private float currentProgress;
        private float displayTimer;
        private bool isLoading;
        private Action onLoadingComplete;

        public bool IsLoading => isLoading;
        public float Progress => currentProgress;

        protected override void Awake()
        {
            base.Awake();
            screenType = ScreenType.None;
        }

        private void Update()
        {
            if (!isLoading) return;

            UpdateProgress();
            UpdateLoadingIcon();
        }

        private void UpdateProgress()
        {
            if (Mathf.Abs(currentProgress - targetProgress) > 0.001f)
            {
                currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.unscaledDeltaTime * progressSmoothSpeed);
                UpdateProgressUI();
            }
        }

        private void UpdateLoadingIcon()
        {
            if (loadingIcon != null)
            {
                loadingIcon.transform.Rotate(0f, 0f, -rotationSpeed * Time.unscaledDeltaTime);
            }
        }

        private void UpdateProgressUI()
        {
            if (progressBar != null)
            {
                progressBar.fillAmount = currentProgress;
            }

            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(currentProgress * 100)}%";
            }
        }

        public void StartLoading(string message = "Loading...", Action onComplete = null)
        {
            onLoadingComplete = onComplete;
            isLoading = true;
            targetProgress = 0f;
            currentProgress = 0f;
            displayTimer = 0f;

            if (loadingText != null)
            {
                loadingText.text = message;
            }

            if (showTips && tipText != null && tips.Length > 0)
            {
                tipText.text = tips[UnityEngine.Random.Range(0, tips.Length)];
                tipText.gameObject.SetActive(true);
            }
            else if (tipText != null)
            {
                tipText.gameObject.SetActive(false);
            }

            UpdateProgressUI();
            Show(true);
        }

        public void SetProgress(float progress)
        {
            targetProgress = Mathf.Clamp01(progress);
        }

        public void SetMessage(string message)
        {
            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }

        public void CompleteLoading()
        {
            targetProgress = 1f;
            StartCoroutine(CompleteLoadingRoutine());
        }

        private IEnumerator CompleteLoadingRoutine()
        {
            while (currentProgress < 0.99f)
            {
                yield return null;
            }

            currentProgress = 1f;
            UpdateProgressUI();

            float remainingTime = minDisplayTime - displayTimer;
            if (remainingTime > 0f)
            {
                yield return new WaitForSecondsRealtime(remainingTime);
            }

            isLoading = false;

            Hide(false);

            yield return new WaitForSecondsRealtime(fadeDuration);

            onLoadingComplete?.Invoke();
            onLoadingComplete = null;
        }

        public IEnumerator LoadWithProgress(Func<float> getProgress, Action onComplete = null)
        {
            onLoadingComplete = onComplete;
            isLoading = true;
            targetProgress = 0f;
            currentProgress = 0f;
            displayTimer = 0f;

            Show(true);

            while (getProgress() < 1f)
            {
                SetProgress(getProgress());
                displayTimer += Time.unscaledDeltaTime;
                yield return null;
            }

            CompleteLoading();
        }

        protected override void OnShow()
        {
            base.OnShow();
            displayTimer = 0f;
        }

        protected override void OnHide()
        {
            base.OnHide();
            isLoading = false;
        }
    }
}