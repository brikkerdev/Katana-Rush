using DG.Tweening;
using Runner.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner.UI
{
    public class CoinDisplay : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private Image coinImage;

        [Header("Animation")]
        [SerializeField] private bool animateOnChange = true;
        [SerializeField] private float punchScale = 1.2f;
        [SerializeField] private float punchDuration = 0.2f;

        private int displayedCoins;
        private DG.Tweening.Tweener punchTween;

        private void OnEnable()
        {
            displayedCoins = SaveManager.GetCoins();
            UpdateDisplayImmediate();
            SaveManager.OnDataChanged += OnCoinsChanged;
        }

        private void OnDisable()
        {
            SaveManager.OnDataChanged -= OnCoinsChanged;
            punchTween?.Kill();
        }

        private void OnCoinsChanged()
        {
            int newCoins = SaveManager.GetCoins();

            if (newCoins != displayedCoins)
            {
                displayedCoins = newCoins;
                UpdateDisplayImmediate();

                if (animateOnChange)
                {
                    PlayPunchAnimation();
                }
            }
        }

        public void UpdateDisplay()
        {
            displayedCoins = SaveManager.GetCoins();
            UpdateDisplayImmediate();
        }

        private void UpdateDisplayImmediate()
        {
            if (coinText != null)
            {
                coinText.text = displayedCoins.ToString();
            }
        }

        private void PlayPunchAnimation()
        {
            punchTween?.Kill();

            if (coinText != null)
            {
                coinText.transform.localScale = Vector3.one;
                punchTween = coinText.transform
                    .DOPunchScale(Vector3.one * (punchScale - 1f), punchDuration, 1, 0f)
                    .SetUpdate(true);
            }
        }

        public void SetCoins(int amount)
        {
            displayedCoins = amount;
            UpdateDisplayImmediate();

            if (animateOnChange)
            {
                PlayPunchAnimation();
            }
        }
    }
}