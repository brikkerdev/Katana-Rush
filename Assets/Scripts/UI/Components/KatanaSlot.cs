using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
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

        [Header("Rarity Colors")]
        [SerializeField] private Color commonColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color rareColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color epicColor = new Color(0.7f, 0.3f, 1f);
        [SerializeField] private Color legendaryColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color challengeColor = new Color(1f, 0.3f, 0.3f);

        [Header("Lock Settings")]
        [SerializeField] private Color lockedIconColor = Color.black;
        [SerializeField] private float lockedIconAlpha = 0.8f;

        private Katana katana;
        private bool isOwned;
        private bool isSelected;
        private bool isEquipped;

        public Katana Katana => katana;
        public bool IsOwned => isOwned;

        public event Action<KatanaSlot> OnSlotClicked;

        public void Setup(Katana katanaData, bool owned, bool equipped)
        {
            katana = katanaData;
            isOwned = owned;
            isEquipped = equipped;

            UpdateVisual();
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (selectionBorder != null)
            {
                selectionBorder.gameObject.SetActive(selected);
            }
        }

        private void UpdateVisual()
        {
            if (katana == null) return;

            if (iconImage != null)
            {
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
            OnSlotClicked?.Invoke(this);
        }
    }
}