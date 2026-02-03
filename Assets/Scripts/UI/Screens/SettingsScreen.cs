using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

        [Header("Language")]
        [SerializeField] private TMP_Dropdown languageDropdown;

        [Header("Buttons")]
        [SerializeField] private UIButton closeButton;
        [SerializeField] private UIButton resetButton;

        private List<string> languageCodes = new List<string>();

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

            if (languageDropdown != null)
                languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

            SetupLanguageDropdown();
            SetupQualityDropdown();
        }

        private void SetupLanguageDropdown()
        {
            if (languageDropdown == null) return;
            if (LocalizationController.Singleton == null) return;
            if (LocalizationController.Singleton.database == null) return;

            languageDropdown.ClearOptions();
            languageCodes.Clear();

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

            foreach (var language in LocalizationController.Singleton.database.languages)
            {
                if (language == null) continue;

                options.Add(new TMP_Dropdown.OptionData(language.displayName));
                languageCodes.Add(language.code);
            }

            languageDropdown.AddOptions(options);
        }

        private void SetupQualityDropdown()
        {
            if (qualityDropdown == null) return;

            qualityDropdown.ClearOptions();

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            string[] qualityNames = QualitySettings.names;

            foreach (var name in qualityNames)
            {
                options.Add(new TMP_Dropdown.OptionData(name));
            }

            qualityDropdown.AddOptions(options);
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

            LoadLanguageSelection();
        }

        private void LoadLanguageSelection()
        {
            if (languageDropdown == null) return;
            if (LocalizationController.Singleton == null) return;

            string currentCode = LocalizationController.Singleton.CurrentLanguageCode;
            int index = languageCodes.IndexOf(currentCode);

            if (index >= 0)
            {
                languageDropdown.SetValueWithoutNotify(index);
            }
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
            PlayerPrefs.SetInt("QualityLevel", index);
            SaveSettings();
        }

        private void OnLanguageChanged(int index)
        {
            if (index < 0 || index >= languageCodes.Count) return;
            if (LocalizationController.Singleton == null) return;

            string languageCode = languageCodes[index];
            LocalizationController.Singleton.ChangeLanguage(languageCode);
        }

        private void OnCloseClicked()
        {
            UIManager.Instance?.ShowScreen(ScreenType.MainMenu);
        }

        private void OnResetClicked()
        {
            if (musicSlider != null) musicSlider.value = 1f;
            if (sfxSlider != null) sfxSlider.value = 1f;
            if (vibrationToggle != null) vibrationToggle.isOn = true;
            if (qualityDropdown != null) qualityDropdown.value = 1;

            ResetLanguageToDefault();
        }

        private void ResetLanguageToDefault()
        {
            if (languageDropdown == null) return;
            if (LocalizationController.Singleton == null) return;
            if (languageCodes.Count == 0) return;

            string defaultCode = GetSystemLanguageCode();
            int index = languageCodes.IndexOf(defaultCode);

            if (index < 0)
            {
                index = 0;
            }

            languageDropdown.value = index;
        }

        private string GetSystemLanguageCode()
        {
            SystemLanguage systemLang = Application.systemLanguage;

            switch (systemLang)
            {
                case SystemLanguage.English:
                    return "en";
                case SystemLanguage.Russian:
                    return "ru";
                case SystemLanguage.German:
                    return "de";
                case SystemLanguage.French:
                    return "fr";
                case SystemLanguage.Spanish:
                    return "es";
                case SystemLanguage.Italian:
                    return "it";
                case SystemLanguage.Portuguese:
                    return "pt";
                case SystemLanguage.Japanese:
                    return "ja";
                case SystemLanguage.Korean:
                    return "ko";
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                    return "zh-CN";
                case SystemLanguage.ChineseTraditional:
                    return "zh-TW";
                case SystemLanguage.Arabic:
                    return "ar";
                case SystemLanguage.Turkish:
                    return "tr";
                case SystemLanguage.Polish:
                    return "pl";
                case SystemLanguage.Dutch:
                    return "nl";
                case SystemLanguage.Ukrainian:
                    return "uk";
                default:
                    return "en";
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.OnClick -= OnCloseClicked;

            if (resetButton != null)
                resetButton.OnClick -= OnResetClicked;

            if (musicSlider != null)
                musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

            if (vibrationToggle != null)
                vibrationToggle.onValueChanged.RemoveListener(OnVibrationChanged);

            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);

            if (languageDropdown != null)
                languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        }
    }
}