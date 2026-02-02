using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Runner.Save;

namespace Runner.UI
{
    public class CoinDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private Image coinIcon;

        private void OnEnable()
        {
            UpdateDisplay();
            SaveManager.OnDataChanged += UpdateDisplay;
        }

        private void OnDisable()
        {
            SaveManager.OnDataChanged -= UpdateDisplay;
        }

        public void UpdateDisplay()
        {
            if (coinText != null)
            {
                coinText.text = SaveManager.GetCoins().ToString();
            }
        }
    }
}