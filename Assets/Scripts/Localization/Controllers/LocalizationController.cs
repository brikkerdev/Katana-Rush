using System;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationController : MonoBehaviour
{
    public static LocalizationController Singleton;

    [SerializeField] private LocalizationConfig config;
    private LocalizationDatabase database;

    public LocalizationDatabase Database => database;

    [SerializeField] private string defaultLanguageCode = "en";
    [SerializeField] private bool autoDetectLanguage = true;

    private string currentLanguageCode;

    public string CurrentLanguageCode => currentLanguageCode;

    public event Action LocalizationChangedEvent;

    private const string PrefKey = "LanguageCode";
    private const string FirstLaunchKey = "FirstLaunch";

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        Singleton = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        database = new LocalizationDatabase();
        if (config != null && !string.IsNullOrEmpty(config.jsonFolderPath))
        {
            database.LoadFromPath(config.jsonFolderPath);
        }

        bool isFirstLaunch = PlayerPrefs.GetInt(FirstLaunchKey, 1) == 1;

        if (isFirstLaunch && autoDetectLanguage)
        {
            currentLanguageCode = DetectSystemLanguage();
            PlayerPrefs.SetInt(FirstLaunchKey, 0);
            PlayerPrefs.SetString(PrefKey, currentLanguageCode);
            PlayerPrefs.Save();
        }
        else
        {
            currentLanguageCode = PlayerPrefs.GetString(PrefKey, defaultLanguageCode);
        }

        ValidateLanguageCode();
    }

    private void ValidateLanguageCode()
    {
        if (database == null || database.Languages == null) return;

        bool exists = false;
        foreach (var l in database.Languages)
        {
            if (l != null && l.code == currentLanguageCode)
            {
                exists = true;
                break;
            }
        }

        if (!exists && database.Languages.Count > 0 && database.Languages[0] != null)
        {
            currentLanguageCode = database.Languages[0].code;
            PlayerPrefs.SetString(PrefKey, currentLanguageCode);
            PlayerPrefs.Save();
        }
    }

    private string DetectSystemLanguage()
    {
        if (database == null || database.Languages == null)
        {
            return defaultLanguageCode;
        }

        string systemCode = GetSystemLanguageCode();

        foreach (var language in database.Languages)
        {
            if (language != null && language.code == systemCode)
            {
                return systemCode;
            }
        }

        return defaultLanguageCode;
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

    public void ChangeLanguage(string languageCode)
    {
        if (currentLanguageCode == languageCode) return;

        currentLanguageCode = languageCode;
        PlayerPrefs.SetString(PrefKey, currentLanguageCode);
        PlayerPrefs.Save();

        LocalizationChangedEvent?.Invoke();
    }

    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        if (database == null) return key;

        if (database.TryGet(key, currentLanguageCode, out var value))
        {
            return value;
        }

        return key;
    }

    public string GetText(string key, params object[] args)
    {
        string text = GetText(key);

        if (args != null && args.Length > 0)
        {
            try
            {
                return string.Format(text, args);
            }
            catch
            {
                return text;
            }
        }

        return text;
    }

    public bool HasKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        if (database == null) return false;

        return database.TryGet(key, currentLanguageCode, out _);
    }

    public List<string> GetAvailableLanguageCodes()
    {
        List<string> codes = new List<string>();

        if (database != null && database.Languages != null)
        {
            foreach (var language in database.Languages)
            {
                if (language != null)
                {
                    codes.Add(language.code);
                }
            }
        }

        return codes;
    }

    public string GetLanguageDisplayName(string code)
    {
        if (database == null || database.Languages == null) return code;

        foreach (var language in database.Languages)
        {
            if (language != null && language.code == code)
            {
                return language.displayName ?? code;
            }
        }

        return code;
    }

    private void OnDestroy()
    {
        if (Singleton == this)
        {
            Singleton = null;
        }
    }
}
