using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Runner.UI
{
    public sealed class SwipePagesView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField] private List<RectTransform> pages = new List<RectTransform>();

        [SerializeField, Range(0.05f, 0.5f)] private float swipeThresholdNormalized = 0.18f;
        [SerializeField] private float flickVelocity = 1200f;
        [SerializeField, Range(0f, 0.5f)] private float elasticityNormalized = 0.18f;
        [SerializeField, Range(0.05f, 0.6f)] private float snapDuration = 0.25f;

        private int currentPage;
        private bool dragging;

        private float pageWidth;
        private float dragStartPointerX;
        private float dragStartContentX;

        private float lastPointerX;
        private float lastPointerTime;
        private float pointerVelocity;

        private Coroutine snapRoutine;

        public int CurrentPage => currentPage;
        public int PageCount => pages != null ? pages.Count : 0;
        public float PageWidth => pageWidth;

        public event Action<int> OnPageChanged;

        private void Reset()
        {
            viewport = transform as RectTransform;
        }

        private void Awake()
        {
            EnsureContent();
        }

        private void OnEnable()
        {
            Rebuild();
        }

        public void SetPages(List<RectTransform> pageList, bool rebuild = true)
        {
            pages = pageList ?? new List<RectTransform>();
            if (rebuild) Rebuild();
        }

        public void Rebuild()
        {
            if (viewport == null) return;

            Canvas.ForceUpdateCanvases();

            pageWidth = viewport.rect.width;
            if (pageWidth <= 1f) pageWidth = ((RectTransform)transform).rect.width;

            EnsureContent();

            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.localScale = Vector3.one;

            int count = PageCount;
            currentPage = Mathf.Clamp(currentPage, 0, Mathf.Max(0, count - 1));

            content.sizeDelta = new Vector2(pageWidth * count, 0f);

            for (int i = 0; i < count; i++)
            {
                var page = pages[i];
                if (page == null) continue;

                page.SetParent(content, false);

                page.anchorMin = new Vector2(0f, 0f);
                page.anchorMax = new Vector2(0f, 1f);
                page.pivot = new Vector2(0f, 0.5f);
                page.sizeDelta = new Vector2(pageWidth, 0f);
                page.anchoredPosition = new Vector2(i * pageWidth, 0f);
                page.localScale = Vector3.one;
                page.gameObject.SetActive(true);
            }

            SetContentX(-currentPage * pageWidth);
        }

        public void GoToPage(int page, bool instant = false)
        {
            int target = Mathf.Clamp(page, 0, Mathf.Max(0, PageCount - 1));

            float targetX = -target * pageWidth;
            float currentX = content != null ? content.anchoredPosition.x : targetX;

            bool pageChanged = target != currentPage;

            currentPage = target;

            if (instant)
            {
                StopSnap();
                SetContentX(targetX);
                if (pageChanged) OnPageChanged?.Invoke(currentPage);
                return;
            }

            if (Mathf.Abs(currentX - targetX) > 0.01f)
                StartSnap(targetX);
            else
                SetContentX(targetX);

            if (pageChanged) OnPageChanged?.Invoke(currentPage);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (viewport == null || content == null) return;
            if (PageCount <= 1) return;

            dragging = true;

            StopSnap();

            dragStartPointerX = eventData.position.x;
            dragStartContentX = content.anchoredPosition.x;

            lastPointerX = dragStartPointerX;
            lastPointerTime = Time.unscaledTime;
            pointerVelocity = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging) return;

            float pointerX = eventData.position.x;
            float delta = pointerX - dragStartPointerX;
            float targetX = dragStartContentX + delta;

            float minX = -(PageCount - 1) * pageWidth;
            float maxX = 0f;
            float elasticity = pageWidth * elasticityNormalized;

            targetX = Mathf.Clamp(targetX, minX - elasticity, maxX + elasticity);
            SetContentX(targetX);

            float now = Time.unscaledTime;
            float dt = Mathf.Max(0.0001f, now - lastPointerTime);
            pointerVelocity = (pointerX - lastPointerX) / dt;

            lastPointerX = pointerX;
            lastPointerTime = now;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            dragging = false;

            float delta = eventData.position.x - dragStartPointerX;
            float normalized = delta / Mathf.Max(1f, pageWidth);

            int target = currentPage;

            if (Mathf.Abs(pointerVelocity) >= flickVelocity)
            {
                target = pointerVelocity > 0f ? currentPage - 1 : currentPage + 1;
            }
            else if (Mathf.Abs(normalized) >= swipeThresholdNormalized)
            {
                target = normalized > 0f ? currentPage - 1 : currentPage + 1;
            }

            GoToPage(target, false);
        }

        private void EnsureContent()
        {
            if (viewport == null) return;
            if (content != null) return;

            var existing = viewport.Find("Content");
            if (existing != null)
            {
                content = existing as RectTransform;
                if (content != null) return;
            }

            var go = new GameObject("Content", typeof(RectTransform));
            content = go.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
        }

        private void SetContentX(float x)
        {
            if (content == null) return;
            var p = content.anchoredPosition;
            p.x = x;
            p.y = 0f;
            content.anchoredPosition = p;
        }

        private void StartSnap(float targetX)
        {
            StopSnap();
            snapRoutine = StartCoroutine(SnapRoutine(targetX));
        }

        private void StopSnap()
        {
            if (snapRoutine == null) return;
            StopCoroutine(snapRoutine);
            snapRoutine = null;
        }

        private IEnumerator SnapRoutine(float targetX)
        {
            float startX = content.anchoredPosition.x;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, snapDuration);
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                float x = Mathf.Lerp(startX, targetX, eased);
                SetContentX(x);
                yield return null;
            }

            SetContentX(targetX);
            snapRoutine = null;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled) return;
            if (viewport == null) return;

            float w = viewport.rect.width;
            if (Mathf.Abs(w - pageWidth) > 1f)
                Rebuild();
        }
    }
}