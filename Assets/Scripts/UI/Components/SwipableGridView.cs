using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace Runner.UI
{
    public class SwipeableGridView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform viewport;

        [Header("Settings")]
        [SerializeField] private float swipeThreshold = 50f;
        [SerializeField] private float snapDuration = 0.3f;
        [SerializeField] private float pageWidth = 400f;

        private int currentPage;
        private int totalPages;
        private float dragStartX;
        private float contentStartX;
        private bool isDragging;
        private float snapVelocity;
        private float targetX;
        private bool isSnapping;

        public int CurrentPage => currentPage;
        public int TotalPages => totalPages;

        public event Action<int> OnPageChanged;

        public void SetPageCount(int count)
        {
            totalPages = count;
            currentPage = Mathf.Clamp(currentPage, 0, Mathf.Max(0, totalPages - 1));
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
            targetX = -currentPage * pageWidth;

            if (instant)
            {
                Vector2 pos = content.anchoredPosition;
                pos.x = targetX;
                content.anchoredPosition = pos;
                isSnapping = false;
            }
            else
            {
                isSnapping = true;
            }

            OnPageChanged?.Invoke(currentPage);
        }

        private void Update()
        {
            if (isSnapping && !isDragging)
            {
                Vector2 pos = content.anchoredPosition;
                pos.x = Mathf.SmoothDamp(pos.x, targetX, ref snapVelocity, snapDuration);
                content.anchoredPosition = pos;

                if (Mathf.Abs(pos.x - targetX) < 0.5f)
                {
                    pos.x = targetX;
                    content.anchoredPosition = pos;
                    isSnapping = false;
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            isSnapping = false;
            dragStartX = eventData.position.x;
            contentStartX = content.anchoredPosition.x;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            float delta = eventData.position.x - dragStartX;
            Vector2 pos = content.anchoredPosition;
            pos.x = contentStartX + delta;

            float minX = -(totalPages - 1) * pageWidth;
            pos.x = Mathf.Clamp(pos.x, minX - pageWidth * 0.3f, pageWidth * 0.3f);

            content.anchoredPosition = pos;
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
    }
}