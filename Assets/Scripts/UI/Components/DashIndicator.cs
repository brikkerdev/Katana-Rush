using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Runner.UI
{
    public class DashIndicator : MonoBehaviour
    {
        [SerializeField] private Transform container;
        [SerializeField] private GameObject dashDotPrefab;
        [SerializeField] private Color activeDotColor = Color.cyan;
        [SerializeField] private Color inactiveDotColor = new Color(0.3f, 0.3f, 0.3f);

        private Image[] dotImages;
        private int maxDots = 5;

        private void Awake()
        {
            CreateDots();
        }

        private void CreateDots()
        {
            dotImages = new Image[maxDots];

            for (int i = 0; i < maxDots; i++)
            {
                GameObject dot = Instantiate(dashDotPrefab, container);
                dotImages[i] = dot.GetComponent<Image>();
                dot.SetActive(false);
            }
        }

        public void SetDashCount(int count)
        {
            count = Mathf.Clamp(count, 0, maxDots);

            for (int i = 0; i < maxDots; i++)
            {
                if (i < count)
                {
                    dotImages[i].gameObject.SetActive(true);
                    dotImages[i].color = activeDotColor;
                }
                else
                {
                    dotImages[i].gameObject.SetActive(false);
                }
            }
        }

        public void SetHidden()
        {
            for (int i = 0; i < maxDots; i++)
            {
                dotImages[i].gameObject.SetActive(true);
                dotImages[i].color = inactiveDotColor;
            }
        }
    }
}