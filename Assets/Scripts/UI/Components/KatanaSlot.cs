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
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private Image equippedIndicator;
        [SerializeField] private GameObject emptyOverlay;
        [SerializeField] private Image selectionBorder;
        [SerializeField] private Image highlightBorder;

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

        [Header("Animation Settings")]
        [SerializeField] private float clickScale = 0.9f;
        [SerializeField] private float clickDuration = 0.1f;
        [SerializeField] private Color highlightColor = Color.white;
        [SerializeField] private float winPulseScale = 1.15f;
        [SerializeField] private float winPulseDuration = 0.3f;
        [SerializeField] private float selectionPulseScale = 1.05f;
        [SerializeField] private float selectionPulseDuration = 0.15f;

        private Katana katana;
        private bool isOwned;
        private bool isEquipped;
        private bool isEmpty;
        private bool isInteractable = true;
        private bool isSelected;
        private Tweener currentTween;
        private Sequence winSequence;

        public Katana Katana => katana;
        public bool IsOwned => isOwned;
        public bool IsEmpty => isEmpty;
        public bool IsSelected => isSelected;

        public event Action<KatanaSlot> OnSlotClicked;

        public void Setup(Katana katanaData, bool owned, bool equipped)
        {
            katana = katanaData;
            isOwned = owned;
            isEquipped = equipped;
            isEmpty = false;
            isSelected = false;

            if (emptyOverlay != null)
            {
                emptyOverlay.SetActive(false);
            }

            if (highlightBorder != null)
            {
                highlightBorder.gameObject.SetActive(false);
            }

            if (selectionBorder != null)
            {
                selectionBorder.gameObject.SetActive(false);
            }

            UpdateVisual();
        }

        public void SetupEmpty()
        {
            katana = null;
            isOwned = false;
            isEquipped = false;
            isEmpty = true;
            isInteractable = false;
            isSelected = false;

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

            if (highlightBorder != null)
            {
                highlightBorder.gameObject.SetActive(false);
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

        public void SetInteractable(bool interactable)
        {
            if (isEmpty) return;
            isInteractable = interactable;
        }

        /// <summary>
        /// Sets selection state (for preview display)
        /// </summary>
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

        private void PlaySelectionAnimation()
        {
            if (selectionBorder == null) return;

            currentTween?.Kill();

            selectionBorder.transform.localScale = Vector3.one;
            currentTween = selectionBorder.transform
                .DOPunchScale(Vector3.one * (selectionPulseScale - 1f), selectionPulseDuration, 1, 0f)
                .SetUpdate(true);
        }

        /// <summary>
        /// Sets highlight state for roulette animation
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (isEmpty) return;

            if (highlightBorder != null)
            {
                highlightBorder.gameObject.SetActive(highlighted);
                highlightBorder.color = highlightColor;
            }
            else if (backgroundImage != null)
            {
                // Fallback: tint the background
                if (highlighted)
                {
                    backgroundImage.color = Color.Lerp(GetRarityColor(katana.Rarity), Color.white, 0.5f);
                }
                else
                {
                    backgroundImage.color = GetRarityColor(katana.Rarity);
                }
            }
        }

        /// <summary>
        /// Clears any highlight or animation state (for roulette)
        /// </summary>
        public void ClearHighlight()
        {
            currentTween?.Kill();
            winSequence?.Kill();

            if (highlightBorder != null)
            {
                highlightBorder.gameObject.SetActive(false);
            }

            if (backgroundImage != null && katana != null)
            {
                backgroundImage.color = GetRarityColor(katana.Rarity);
            }

            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Shows win effect when slot is selected in roulette
        /// </summary>
        public void ShowWinEffect()
        {
            if (isEmpty) return;

            winSequence?.Kill();
            currentTween?.Kill();

            // Show highlight
            if (highlightBorder != null)
            {
                highlightBorder.gameObject.SetActive(true);
                highlightBorder.color = highlightColor;
            }

            // Pulse animation
            transform.localScale = Vector3.one;
            winSequence = DOTween.Sequence()
                .Append(transform.DOScale(winPulseScale, winPulseDuration * 0.5f).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(1f, winPulseDuration * 0.5f).SetEase(Ease.InQuad))
                .SetLoops(2)
                .SetUpdate(true);
        }

        private void PlayClickAnimation()
        {
            currentTween?.Kill();

            transform.localScale = Vector3.one;
            currentTween = transform
                .DOPunchScale(Vector3.one * (clickScale - 1f), clickDuration, 1, 0f)
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

            PlayClickAnimation();
            OnSlotClicked?.Invoke(this);
        }

        private void OnDestroy()
        {
            currentTween?.Kill();
            winSequence?.Kill();
        }
    }
}