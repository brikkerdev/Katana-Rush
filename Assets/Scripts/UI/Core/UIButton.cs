using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using Runner.Core;

namespace Runner.UI
{
    [RequireComponent(typeof(Button))]
    public class UIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler
    {
        [Header("Animation")]
        [SerializeField] private float scaleOnPress = 0.95f;

        [Header("Sound Settings")]
        [SerializeField] private bool playClickSound = true;
        [SerializeField] private bool playHoverSound = false;
        [SerializeField] private bool playDisabledSound = true;
        [SerializeField] private UIButtonSoundType soundType = UIButtonSoundType.Click;
        [SerializeField] private AudioClip overrideClickSound;
        [SerializeField] private AudioClip overrideHoverSound;

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
            if (playClickSound)
            {
                if (overrideClickSound != null)
                {
                    Game.Instance?.Sound?.PlayUI(overrideClickSound, 0.6f);
                }
                else
                {
                    PlaySoundByType();
                }
            }

            OnClick?.Invoke();
        }

        private void PlaySoundByType()
        {
            switch (soundType)
            {
                case UIButtonSoundType.Click:
                    Game.Instance?.Sound?.PlayButtonClick();
                    break;
                case UIButtonSoundType.Back:
                    Game.Instance?.Sound?.PlayButtonBack();
                    break;
                case UIButtonSoundType.Tab:
                    Game.Instance?.Sound?.PlayTabSwitch();
                    break;
                case UIButtonSoundType.Purchase:
                    Game.Instance?.Sound?.PlayPurchaseSuccess();
                    break;
                case UIButtonSoundType.Equip:
                    Game.Instance?.Sound?.PlayEquip();
                    break;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable)
            {
                if (playDisabledSound)
                {
                    Game.Instance?.Sound?.PlayButtonDisabled();
                }
                return;
            }

            isPressed = true;
            transform.localScale = originalScale * scaleOnPress;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            transform.localScale = originalScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!button.interactable) return;

            if (playHoverSound)
            {
                if (overrideHoverSound != null)
                {
                    Game.Instance?.Sound?.PlayUI(overrideHoverSound, 0.3f);
                }
                else
                {
                    Game.Instance?.Sound?.PlayButtonHover();
                }
            }
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

    public enum UIButtonSoundType
    {
        Click,
        Back,
        Tab,
        Purchase,
        Equip
    }
}