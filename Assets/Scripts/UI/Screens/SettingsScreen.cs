using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Runner.UI
{
    public class SettingsScreen : UIScreen
    {
        [Header("Audio")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle vibrationToggle;

        [Header("Graphics")]
        [SerializeField] private TMP_Dropdown qualityDropdown;

        [Header("Buttons")]
        [SerializeField] private UIButton closeButton;
        [SerializeField] private UIButton resetButton;

        protected override void Awake()
        {
            base.Awake();
            screenType = ScreenType.Settings;
            SetupUI();
        }

        private void SetupUI()
        {
            if (closeButton != null)
                closeButton.OnClick += OnCloseClicked;

            if (resetButton != null)
                resetButton.OnClick += OnResetClicked;

            if (musicSlider != null)
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            if (vibrationToggle != null)
                vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);

            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }

        protected override void OnShow()
        {
            base.OnShow();
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (musicSlider != null)
                musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);

            if (sfxSlider != null)
                sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

            if (vibrationToggle != null)
                vibrationToggle.isOn = PlayerPrefs.GetInt("Vibration", 1) == 1;

            if (qualityDropdown != null)
                qualityDropdown.value = QualitySettings.GetQualityLevel();
        }

        private void SaveSettings()
        {
            PlayerPrefs.Save();
        }

        private void OnMusicVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("MusicVolume", value);
            SaveSettings();
        }

        private void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
            SaveSettings();
        }

        private void OnVibrationChanged(bool enabled)
        {
            PlayerPrefs.SetInt("Vibration", enabled ? 1 : 0);
            SaveSettings();
        }

        private void OnQualityChanged(int index)
        {
            QualitySettings.SetQualityLevel(index);
        }

        private void OnCloseClicked()
        {
            UIManager.Instance?.HideScreen(ScreenType.Settings);
        }

        private void OnResetClicked()
        {
            if (musicSlider != null) musicSlider.value = 1f;
            if (sfxSlider != null) sfxSlider.value = 1f;
            if (vibrationToggle != null) vibrationToggle.isOn = true;
            if (qualityDropdown != null) qualityDropdown.value = 1;
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.OnClick -= OnCloseClicked;
            if (resetButton != null) resetButton.OnClick -= OnResetClicked;
        }
    }
}