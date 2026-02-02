using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Runner.UI
{
    public class RarityTab : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private GameObject selectedIndicator;

        [Header("Colors")]
        [SerializeField] private Color selectedColor = Color.white;
        [SerializeField] private Color unselectedColor = new Color(0.5f, 0.5f, 0.5f);

        private int tabIndex;
        private bool isSelected;

        public int TabIndex => tabIndex;

        public event Action<int> OnTabClicked;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        public void Setup(int index, string label)
        {
            tabIndex = index;

            if (labelText != null)
            {
                labelText.text = label;
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(selected);
            }

            if (background != null)
            {
                background.color = selected ? selectedColor : unselectedColor;
            }

            if (labelText != null)
            {
                labelText.color = selected ? Color.black : Color.white;
            }
        }

        private void HandleClick()
        {
            OnTabClicked?.Invoke(tabIndex);
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