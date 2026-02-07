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

        private readonly List<string> languageCodes = new List<string>();
        private ScreenType returnToScreen = ScreenType.MainMenu;

        private const string MusicKey = "MusicVolume";
        private const string SfxKey = "SFXVolume";
        private const string VibrationKey = "Vibration";
        private const string GraphicsPresetKey = "GraphicsPreset";
        private const string QualityLevelKey = "QualityLevel";

        private static readonly string[] QualityPresetKeys =
        {
            "ui_settings_quality_low",
            "ui_settings_quality_medium",
            "ui_settings_quality_high"
        };

        protected override void Awake()
        {
            base.Awake();
            screenType = ScreenType.Settings;
            SetupUI();
        }

        private void OnEnable()
        {
            if (LocalizationController.Singleton != null)
                LocalizationController.Singleton.LocalizationChangedEvent += RefreshLocalizedUI;
        }

        private void OnDisable()
        {
            if (LocalizationController.Singleton != null)
                LocalizationController.Singleton.LocalizationChangedEvent -= RefreshLocalizedUI;
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
            RefreshLocalizedUI();
        }

        public void SetReturnScreen(ScreenType screen)
        {
            returnToScreen = screen;
        }

        protected override void OnShow()
        {
            base.OnShow();
            SetupLanguageDropdown();
            RefreshLocalizedUI();
            LoadSettings();
        }

        private string L(string key)
        {
            if (LocalizationController.Singleton == null) return key;
            return LocalizationController.Singleton.GetText(key);
        }

        private void RefreshLocalizedUI()
        {
            SetupQualityDropdownLocalized();
        }

        private void SetupQualityDropdownLocalized()
        {
            if (qualityDropdown == null) return;

            int currentPreset = PlayerPrefs.GetInt(GraphicsPresetKey, 1);
            currentPreset = Mathf.Clamp(currentPreset, 0, 2);

            qualityDropdown.ClearOptions();

            var options = new List<TMP_Dropdown.OptionData>(3);
            options.Add(new TMP_Dropdown.OptionData(L(QualityPresetKeys[0])));
            options.Add(new TMP_Dropdown.OptionData(L(QualityPresetKeys[1])));
            options.Add(new TMP_Dropdown.OptionData(L(QualityPresetKeys[2])));

            qualityDropdown.AddOptions(options);
            qualityDropdown.SetValueWithoutNotify(currentPreset);
            qualityDropdown.RefreshShownValue();
        }

        private void SetupLanguageDropdown()
        {
            if (languageDropdown == null) return;
            if (LocalizationController.Singleton == null) return;
            if (LocalizationController.Singleton.database == null) return;

            languageDropdown.ClearOptions();
            languageCodes.Clear();

            var options = new List<TMP_Dropdown.OptionData>();

            foreach (var language in LocalizationController.Singleton.database.languages)
            {
                if (language == null) continue;
                options.Add(new TMP_Dropdown.OptionData(language.displayName));
                languageCodes.Add(language.code);
            }

            languageDropdown.AddOptions(options);
        }

        private void LoadSettings()
        {
            if (musicSlider != null)
                musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MusicKey, 1f));

            if (sfxSlider != null)
                sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SfxKey, 1f));

            if (vibrationToggle != null)
                vibrationToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(VibrationKey, 1) == 1);

            ApplySavedGraphicsPreset();
            LoadLanguageSelection();
        }

        private void ApplySavedGraphicsPreset()
        {
            int preset = PlayerPrefs.GetInt(GraphicsPresetKey, 1);
            preset = Mathf.Clamp(preset, 0, 2);

            int qualityIndex = GetQualityIndexForPreset(preset);
            QualitySettings.SetQualityLevel(qualityIndex, true);

            PlayerPrefs.SetInt(QualityLevelKey, qualityIndex);
            PlayerPrefs.Save();

            if (qualityDropdown != null)
            {
                qualityDropdown.SetValueWithoutNotify(preset);
                qualityDropdown.RefreshShownValue();
            }
        }

        private int GetQualityIndexForPreset(int preset)
        {
            int count = QualitySettings.names != null ? QualitySettings.names.Length : 0;
            if (count <= 0) return 0;
            if (count == 1) return 0;

            int low = 0;
            int high = count - 1;
            int medium = (count - 1) / 2;

            return preset switch
            {
                0 => low,
                1 => medium,
                2 => high,
                _ => medium
            };
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
                languageDropdown.RefreshShownValue();
            }
        }

        private void SaveSettings()
        {
            PlayerPrefs.Save();
        }

        private void OnMusicVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat(MusicKey, value);
            SaveSettings();
        }

        private void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat(SfxKey, value);
            SaveSettings();
        }

        private void OnVibrationChanged(bool enabled)
        {
            PlayerPrefs.SetInt(VibrationKey, enabled ? 1 : 0);
            SaveSettings();
        }

        private void OnQualityChanged(int presetIndex)
        {
            presetIndex = Mathf.Clamp(presetIndex, 0, 2);

            int qualityIndex = GetQualityIndexForPreset(presetIndex);
            QualitySettings.SetQualityLevel(qualityIndex, true);

            PlayerPrefs.SetInt(GraphicsPresetKey, presetIndex);
            PlayerPrefs.SetInt(QualityLevelKey, qualityIndex);
            SaveSettings();
        }

        private void OnLanguageChanged(int index)
        {
            if (index < 0 || index >= languageCodes.Count) return;
            if (LocalizationController.Singleton == null) return;

            string languageCode = languageCodes[index];
            LocalizationController.Singleton.ChangeLanguage(languageCode);
            LoadLanguageSelection();
        }

        private void OnCloseClicked()
        {
            UIManager.Instance?.ShowScreen(returnToScreen);
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
            if (index < 0) index = 0;

            languageDropdown.value = index;
        }

        private string GetSystemLanguageCode()
        {
            SystemLanguage systemLang = Application.systemLanguage;

            return systemLang switch
            {
                SystemLanguage.English => "en",
                SystemLanguage.Russian => "ru",
                SystemLanguage.German => "de",
                SystemLanguage.French => "fr",
                SystemLanguage.Spanish => "es",
                SystemLanguage.Italian => "it",
                SystemLanguage.Portuguese => "pt",
                SystemLanguage.Japanese => "ja",
                SystemLanguage.Korean => "ko",
                SystemLanguage.Chinese or SystemLanguage.ChineseSimplified => "zh-CN",
                SystemLanguage.ChineseTraditional => "zh-TW",
                SystemLanguage.Arabic => "ar",
                SystemLanguage.Turkish => "tr",
                SystemLanguage.Polish => "pl",
                SystemLanguage.Dutch => "nl",
                SystemLanguage.Ukrainian => "uk",
                _ => "en"
            };
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