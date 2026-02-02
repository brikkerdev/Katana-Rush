using System;
using UnityEngine;

public class LocalizationController : MonoBehaviour
{
    public static LocalizationController Singleton;

    public LocalizationDatabase database;

    [SerializeField] private string currentLanguageCode = "en";
    public string CurrentLanguageCode => currentLanguageCode;

    public event Action LocalizationChangedEvent;

    private const string PrefKey = "LanguageCode";

    void Awake()
    {
        Singleton = this;

        currentLanguageCode = PlayerPrefs.GetString(PrefKey, currentLanguageCode);

        // Optional: validate code exists
        if (database != null && database.languages != null)
        {
            bool exists = database.languages.Exists(l => l != null && l.code == currentLanguageCode);
            if (!exists && database.languages.Count > 0 && database.languages[0] != null)
            {
                currentLanguageCode = database.languages[0].code;
                PlayerPrefs.SetString(PrefKey, currentLanguageCode);
            }
        }
    }

    public void ChangeLanguage(string languageCode)
    {
        currentLanguageCode = languageCode;
        PlayerPrefs.SetString(PrefKey, currentLanguageCode);
        OnLocalizationChanged();
    }

    public string GetText(string key)
    {
        if (database == null) return key;

        if (database.TryGet(key, currentLanguageCode, out var value))
            return value;

        return key; // fallback: show key
    }

    public void OnLocalizationChanged()
    {
        LocalizationChangedEvent?.Invoke();
    }
}