using UnityEngine;
using UnityEngine.UI;
using System;

namespace Runner.UI
{
    public class RarityTab : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image circleImage;
        [SerializeField] private Image selectedRingImage;

        [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private float selectedScale = 1.12f;

        private int tabIndex;
        private bool isSelected;
        private Color rarityColor;

        public int TabIndex => tabIndex;

        public event Action<int> OnTabClicked;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        public void Setup(int index, Color color)
        {
            tabIndex = index;
            rarityColor = color;

            if (circleImage != null)
                circleImage.color = unselectedColor;

            if (selectedRingImage != null)
            {
                selectedRingImage.color = rarityColor;
                selectedRingImage.gameObject.SetActive(false);
            }

            transform.localScale = Vector3.one;
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (circleImage != null)
                circleImage.color = selected ? rarityColor : unselectedColor;

            if (selectedRingImage != null)
                selectedRingImage.gameObject.SetActive(selected);

            transform.localScale = selected ? Vector3.one * selectedScale : Vector3.one;
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        private void HandleClick()
        {
            OnTabClicked?.Invoke(tabIndex);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }
    }
}