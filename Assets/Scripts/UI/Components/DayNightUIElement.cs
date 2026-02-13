using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Runner.UI
{
    public class DayNightUIElement : MonoBehaviour
    {
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private TMP_Text targetText;

        private void Start()
        {
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            if (targetText == null)
            {
                targetText = GetComponent<TMP_Text>();
            }

            Register();
        }

        private void Register()
        {
            if (DayNightUiController.Instance == null) return;

            if (targetGraphic != null)
            {
                DayNightUiController.Instance.RegisterGraphic(targetGraphic);
            }

            if (targetText != null)
            {
                DayNightUiController.Instance.RegisterText(targetText);
            }
        }

        private void OnDestroy()
        {
            if (DayNightUiController.Instance == null) return;

            if (targetGraphic != null)
            {
                DayNightUiController.Instance.UnregisterGraphic(targetGraphic);
            }

            if (targetText != null)
            {
                DayNightUiController.Instance.UnregisterText(targetText);
            }
        }
    }
}