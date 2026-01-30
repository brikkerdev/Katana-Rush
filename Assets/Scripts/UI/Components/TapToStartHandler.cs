using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace Runner.UI
{
    public class TapToStartHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        [SerializeField] private bool consumeButtonTaps = true;
        [SerializeField] private GameObject tapIndicator;
        [SerializeField] private float tapIndicatorPulseSpeed = 2f;

        private bool isEnabled = true;
        private bool isAnimating;

        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                isEnabled = value;
                if (tapIndicator != null)
                {
                    tapIndicator.SetActive(value);
                }
            }
        }

        public event Action OnTapToStart;

        private void Update()
        {
            if (!isEnabled || tapIndicator == null) return;

            float scale = 1f + Mathf.Sin(Time.unscaledTime * tapIndicatorPulseSpeed) * 0.1f;
            tapIndicator.transform.localScale = Vector3.one * scale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isEnabled) return;

            if (consumeButtonTaps && IsPointerOverButton(eventData))
            {
                return;
            }

            OnTapToStart?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        private bool IsPointerOverButton(PointerEventData eventData)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject == gameObject) continue;

                if (result.gameObject.GetComponent<UnityEngine.UI.Button>() != null ||
                    result.gameObject.GetComponent<UIButton>() != null)
                {
                    return true;
                }

                var selectable = result.gameObject.GetComponent<UnityEngine.UI.Selectable>();
                if (selectable != null && selectable.interactable)
                {
                    return true;
                }
            }

            return false;
        }
    }
}