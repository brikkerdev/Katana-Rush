using UnityEngine;
using System;
using System.Collections;

namespace Runner.UI
{
    public enum ScreenType
    {
        None,
        MainMenu,
        Gameplay,
        Pause,
        GameOver,
        Settings,
        Inventory,
        Loading
    }

    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] protected ScreenType screenType;
        [SerializeField] protected float fadeDuration = 0.3f;
        [SerializeField] protected bool disableOnHide = true;

        protected CanvasGroup canvasGroup;
        protected bool isVisible;
        protected Coroutine fadeCoroutine;

        public ScreenType Type => screenType;
        public bool IsVisible => isVisible;

        public event Action OnShowStarted;
        public event Action OnShowCompleted;
        public event Action OnHideStarted;
        public event Action OnHideCompleted;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public virtual void Show(bool instant = false)
        {
            StopFadeCoroutine();

            gameObject.SetActive(true);
            OnShowStarted?.Invoke();

            if (instant || fadeDuration <= 0)
            {
                SetVisibleImmediate(true);
                OnShow();
                OnShowCompleted?.Invoke();
            }
            else
            {
                fadeCoroutine = StartCoroutine(FadeRoutine(1f, () =>
                {
                    OnShow();
                    OnShowCompleted?.Invoke();
                }));
            }
        }

        public virtual void Hide(bool instant = false)
        {
            OnHideStarted?.Invoke();
            StopFadeCoroutine();

            if (instant || fadeDuration <= 0 || !gameObject.activeInHierarchy)
            {
                SetVisibleImmediate(false);
                OnHide();
                OnHideCompleted?.Invoke();

                if (disableOnHide)
                {
                    gameObject.SetActive(false);
                }
            }
            else
            {
                fadeCoroutine = StartCoroutine(FadeRoutine(0f, () =>
                {
                    OnHide();
                    OnHideCompleted?.Invoke();

                    if (disableOnHide)
                    {
                        gameObject.SetActive(false);
                    }
                }));
            }
        }

        private void SetVisibleImmediate(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            isVisible = visible;
        }

        private void StopFadeCoroutine()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
        }

        private IEnumerator FadeRoutine(float targetAlpha, Action onComplete)
        {
            bool fadingIn = targetAlpha > 0.5f;

            if (fadingIn)
            {
                canvasGroup.blocksRaycasts = true;
            }

            canvasGroup.interactable = false;

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            canvasGroup.interactable = fadingIn;
            canvasGroup.blocksRaycasts = fadingIn;
            isVisible = fadingIn;

            onComplete?.Invoke();
            fadeCoroutine = null;
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        protected virtual void OnDisable()
        {
            StopFadeCoroutine();
        }
    }
}