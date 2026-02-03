using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using DG.Tweening;

namespace Runner.UI
{
    public class RarityPagesView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform pagesContainer;
        [SerializeField] private RectTransform viewport;

        [Header("Settings")]
        [SerializeField] private float swipeThreshold = 50f;
        [SerializeField] private float snapDuration = 0.3f;

        private int currentPage;
        private int totalPages;
        private float pageWidth;
        private float dragStartX;
        private float containerStartX;
        private bool isDragging;
        private bool isAnimating;
        private Tweener snapTween;

        public int CurrentPage => currentPage;
        public int TotalPages => totalPages;
        public bool IsDragging => isDragging;
        public bool IsAnimating => isAnimating;

        public event Action<int> OnPageChanged;

        private void Awake()
        {
            if (viewport != null)
            {
                pageWidth = viewport.rect.width;
            }
        }

        public void Initialize(int pageCount)
        {
            totalPages = pageCount;
            currentPage = 0;

            if (viewport != null)
            {
                pageWidth = viewport.rect.width;
            }

            UpdateContainerPosition(true);
        }

        public void SetPageWidth(float width)
        {
            pageWidth = width;
        }

        public void GoToPage(int page, bool instant = false)
        {
            page = Mathf.Clamp(page, 0, Mathf.Max(0, totalPages - 1));

            if (page == currentPage && !instant) return;

            currentPage = page;
            UpdateContainerPosition(instant);
            OnPageChanged?.Invoke(currentPage);
        }

        private void UpdateContainerPosition(bool instant)
        {
            float targetX = -currentPage * pageWidth;

            snapTween?.Kill();

            if (instant)
            {
                Vector2 pos = pagesContainer.anchoredPosition;
                pos.x = targetX;
                pagesContainer.anchoredPosition = pos;
                isAnimating = false;
            }
            else
            {
                isAnimating = true;
                snapTween = pagesContainer.DOAnchorPosX(targetX, snapDuration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true)
                    .OnComplete(() => isAnimating = false);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isAnimating)
            {
                snapTween?.Kill();
                isAnimating = false;
            }

            isDragging = true;
            dragStartX = eventData.position.x;
            containerStartX = pagesContainer.anchoredPosition.x;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            float delta = eventData.position.x - dragStartX;
            Vector2 pos = pagesContainer.anchoredPosition;
            pos.x = containerStartX + delta;

            float minX = -(totalPages - 1) * pageWidth;
            float maxX = 0f;
            float elasticity = pageWidth * 0.2f;

            pos.x = Mathf.Clamp(pos.x, minX - elasticity, maxX + elasticity);

            pagesContainer.anchoredPosition = pos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;

            float delta = eventData.position.x - dragStartX;

            if (Mathf.Abs(delta) > swipeThreshold)
            {
                if (delta > 0 && currentPage > 0)
                {
                    GoToPage(currentPage - 1);
                }
                else if (delta < 0 && currentPage < totalPages - 1)
                {
                    GoToPage(currentPage + 1);
                }
                else
                {
                    GoToPage(currentPage);
                }
            }
            else
            {
                GoToPage(currentPage);
            }
        }

        private void OnDestroy()
        {
            snapTween?.Kill();
        }
    }
}