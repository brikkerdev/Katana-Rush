using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using DG.Tweening;
using Runner.Inventory;

namespace Runner.UI
{
    public class KatanaSlot : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image selectionBorder;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private Image equippedIndicator;
        [SerializeField] private GameObject emptyOverlay;

        [Header("Rarity Colors")]
        [SerializeField] private Color commonColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color rareColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color epicColor = new Color(0.7f, 0.3f, 1f);
        [SerializeField] private Color legendaryColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color challengeColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        [Header("Lock Settings")]
        [SerializeField] private Color lockedIconColor = Color.black;
        [SerializeField] private float lockedIconAlpha = 0.8f;

        [Header("Selection Animation")]
        [SerializeField] private float selectionPulseScale = 1.1f;
        [SerializeField] private float selectionPulseDuration = 0.15f;

        private Katana katana;
        private bool isOwned;
        private bool isSelected;
        private bool isEquipped;
        private bool isEmpty;
        private bool isInteractable = true;
        private Tweener currentTween;

        public Katana Katana => katana;
        public bool IsOwned => isOwned;
        public bool IsSelected => isSelected;
        public bool IsEmpty => isEmpty;

        public event Action<KatanaSlot> OnSlotClicked;

        public void Setup(Katana katanaData, bool owned, bool equipped)
        {
            katana = katanaData;
            isOwned = owned;
            isEquipped = equipped;
            isSelected = false;
            isEmpty = false;

            if (emptyOverlay != null)
            {
                emptyOverlay.SetActive(false);
            }

            UpdateVisual();
        }

        public void SetupEmpty()
        {
            katana = null;
            isOwned = false;
            isEquipped = false;
            isSelected = false;
            isEmpty = true;
            isInteractable = false;

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = emptyColor;
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(false);
            }

            if (equippedIndicator != null)
            {
                equippedIndicator.gameObject.SetActive(false);
            }

            if (selectionBorder != null)
            {
                selectionBorder.gameObject.SetActive(false);
            }

            if (emptyOverlay != null)
            {
                emptyOverlay.SetActive(true);
            }
        }

        public void SetSelected(bool selected, bool animated = false)
        {
            if (isEmpty) return;

            isSelected = selected;

            if (selectionBorder != null)
            {
                selectionBorder.gameObject.SetActive(selected);

                if (selected && animated)
                {
                    PlaySelectionAnimation();
                }
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (isEmpty) return;
            isInteractable = interactable;
        }

        public void FlashSelection()
        {
            if (isEmpty) return;
            if (selectionBorder == null) return;

            selectionBorder.gameObject.SetActive(true);

            currentTween?.Kill();
            currentTween = selectionBorder.DOFade(1f, 0.1f).From(0f).SetUpdate(true);
        }

        public void HideSelection()
        {
            currentTween?.Kill();

            if (selectionBorder != null)
            {
                selectionBorder.gameObject.SetActive(false);
            }
        }

        private void PlaySelectionAnimation()
        {
            if (selectionBorder == null) return;

            currentTween?.Kill();

            selectionBorder.transform.localScale = Vector3.one;
            currentTween = selectionBorder.transform
                .DOPunchScale(Vector3.one * (selectionPulseScale - 1f), selectionPulseDuration, 1, 0f)
                .SetUpdate(true);
        }

        public void UpdateVisual()
        {
            if (isEmpty) return;
            if (katana == null) return;

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(true);
                iconImage.sprite = katana.Icon;

                if (isOwned)
                {
                    iconImage.color = Color.white;
                }
                else
                {
                    Color lockedColor = lockedIconColor;
                    lockedColor.a = lockedIconAlpha;
                    iconImage.color = lockedColor;
                }
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = GetRarityColor(katana.Rarity);
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!isOwned);
            }

            if (equippedIndicator != null)
            {
                equippedIndicator.gameObject.SetActive(isEquipped);
            }

            if (selectionBorder != null)
            {
                selectionBorder.gameObject.SetActive(isSelected);
            }
        }

        public void SetEquipped(bool equipped)
        {
            if (isEmpty) return;

            isEquipped = equipped;

            if (equippedIndicator != null)
            {
                equippedIndicator.gameObject.SetActive(equipped);
            }
        }

        public void SetOwned(bool owned)
        {
            if (isEmpty) return;

            isOwned = owned;
            UpdateVisual();
        }

        private Color GetRarityColor(KatanaRarity rarity)
        {
            switch (rarity)
            {
                case KatanaRarity.Common: return commonColor;
                case KatanaRarity.Rare: return rareColor;
                case KatanaRarity.Epic: return epicColor;
                case KatanaRarity.Legendary: return legendaryColor;
                case KatanaRarity.Challenge: return challengeColor;
                default: return commonColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isEmpty) return;
            if (!isInteractable) return;
            OnSlotClicked?.Invoke(this);
        }

        private void OnDestroy()
        {
            currentTween?.Kill();
        }
    }
}