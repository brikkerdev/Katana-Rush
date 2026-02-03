using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Runner.Save;

namespace Runner.UI
{
    public class ChallengeProgressBar : MonoBehaviour
    {
        [SerializeField] private BarUIVisualElement bar;
        [SerializeField] private TextMeshProUGUI progressText;

        public void SetProgress(float current, float target)
        {
            float progress = Mathf.Clamp01(current / target);

            if (bar != null)
            {
                bar.SetValue(target, current);
            }

            if (progressText != null)
            {
                string currentStr = FormatValue(current);
                string targetStr = FormatValue(target);
                progressText.text = $"{currentStr} / {targetStr}";
            }
        }

        private string FormatValue(float value)
        {
            if (value >= 1000000)
            {
                return $"{value / 1000000f:F1}m";
            }
            else if (value >= 100000)
            {
                return $"{value / 1000f:F0}k";
            }
            else if (value >= 10000)
            {
                return $"{value / 1000f:F1}k";
            }
            else if (value >= 1000)
            {
                return $"{value:N0}";
            }
            else
            {
                return $"{value:F0}";
            }
        }
    }
}