using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace Runner.UI
{
    [RequireComponent(typeof(Button))]
    public class UIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float scaleOnPress = 0.95f;

        private Button button;
        private Vector3 originalScale;
        private bool isPressed;

        public Button Button => button;
        public event Action OnClick;

        private void Awake()
        {
            button = GetComponent<Button>();
            originalScale = transform.localScale;
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            OnClick?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable) return;
            isPressed = true;
            transform.localScale = originalScale * scaleOnPress;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            transform.localScale = originalScale;
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
        }

        private void OnDisable()
        {
            if (isPressed)
            {
                transform.localScale = originalScale;
                isPressed = false;
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }
    }
}