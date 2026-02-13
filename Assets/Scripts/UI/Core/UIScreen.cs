using UnityEngine;
using System;
using DG.Tweening;

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
        [SerializeField] protected float showScaleFrom = 0.95f;

        protected CanvasGroup canvasGroup;
        protected bool isVisible;
        private Tween fadeTween;
        private Tween scaleTween;

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
            KillTweens();

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
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = false;

                transform.localScale = Vector3.one * showScaleFrom;
                scaleTween = transform
                    .DOScale(1f, fadeDuration)
                    .SetUpdate(true)
                    .SetEase(Ease.OutCubic);

                fadeTween = canvasGroup
                    .DOFade(1f, fadeDuration)
                    .SetUpdate(true)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        canvasGroup.interactable = true;
                        isVisible = true;
                        OnShow();
                        OnShowCompleted?.Invoke();
                        fadeTween = null;
                        scaleTween = null;
                    });
            }
        }

        public virtual void Hide(bool instant = false)
        {
            OnHideStarted?.Invoke();
            KillTweens();

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
                canvasGroup.interactable = false;

                scaleTween = transform
                    .DOScale(showScaleFrom, fadeDuration)
                    .SetUpdate(true)
                    .SetEase(Ease.InCubic);

                fadeTween = canvasGroup
                    .DOFade(0f, fadeDuration)
                    .SetUpdate(true)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        canvasGroup.blocksRaycasts = false;
                        isVisible = false;
                        OnHide();
                        OnHideCompleted?.Invoke();

                        if (disableOnHide)
                        {
                            gameObject.SetActive(false);
                        }

                        fadeTween = null;
                        scaleTween = null;
                    });
            }
        }

        private void SetVisibleImmediate(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            transform.localScale = Vector3.one;
            isVisible = visible;
        }

        private void KillTweens()
        {
            fadeTween?.Kill();
            fadeTween = null;
            scaleTween?.Kill();
            scaleTween = null;
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        protected virtual void OnDisable()
        {
            KillTweens();
        }

        protected virtual void OnDestroy()
        {
            KillTweens();
        }
    }
}