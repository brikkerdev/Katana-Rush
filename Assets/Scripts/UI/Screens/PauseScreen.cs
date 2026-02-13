using Runner.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner.UI
{
    public class PauseScreen : UIScreen
    {
        [Header("Buttons")]
        [SerializeField] private UIButton resumeButton;
        [SerializeField] private UIButton settingsButton;
        [SerializeField] private UIButton mainMenuButton;

        [Header("Info")]
        [SerializeField] private TextMeshProUGUI currentDistanceText;

        protected override void Awake()
        {
            base.Awake();
            screenType = ScreenType.Pause;

            if (resumeButton != null)
                resumeButton.OnClick += OnResumeClicked;

            if (settingsButton != null)
                settingsButton.OnClick += OnSettingsClicked;

            if (mainMenuButton != null)
                mainMenuButton.OnClick += OnMainMenuClicked;
        }

        protected override void OnShow()
        {
            base.OnShow();
            UpdateInfo();
        }

        private void UpdateInfo()
        {
            if (currentDistanceText != null && Game.Instance != null)
            {
                currentDistanceText.text = $"{Game.Instance.RunDistance:F0}m";
            }
        }

        private void OnResumeClicked()
        {
            UIManager.Instance?.ResumeGame();
        }

        private void OnSettingsClicked()
        {
            UIManager.Instance?.ShowScreen(ScreenType.Settings);
        }

        private void OnMainMenuClicked()
        {
            UIManager.Instance?.GoToMainMenu();
        }

        private void OnDestroy()
        {
            if (resumeButton != null) resumeButton.OnClick -= OnResumeClicked;
            if (settingsButton != null) settingsButton.OnClick -= OnSettingsClicked;
            if (mainMenuButton != null) mainMenuButton.OnClick -= OnMainMenuClicked;
        }
    }
}