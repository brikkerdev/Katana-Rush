using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Localization/Database")]
public class LocalizationDatabase : ScriptableObject
{
    public List<Language> languages = new();
    public List<Entry> entries = new();

    // Fast lookup cache (rebuilt on demand)
    [NonSerialized] private Dictionary<string, Entry> _byKey;

    public bool TryGet(string key, string languageCode, out string value)
    {
        if (string.IsNullOrEmpty(key))
        {
            value = null;
            return false;
        }

        _byKey ??= BuildKeyMap();

        if (_byKey.TryGetValue(key, out var entry))
        {
            return entry.TryGet(languageCode, out value);
        }

        value = null;
        return false;
    }

    public void RebuildCache() => _byKey = BuildKeyMap();

    private Dictionary<string, Entry> BuildKeyMap()
    {
        var map = new Dictionary<string, Entry>(StringComparer.Ordinal);
        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrEmpty(e.key)) continue;
            // If duplicates exist, last one wins; you can enforce uniqueness in editor tooling.
            map[e.key] = e;
        }
        return map;
    }

    [Serializable]
    public class Entry
    {
        public string key;
        public List<Translation> translations = new();

        public bool TryGet(string languageCode, out string value)
        {
            for (int i = 0; i < translations.Count; i++)
            {
                if (translations[i].languageCode == languageCode)
                {
                    value = translations[i].text;
                    return !string.IsNullOrEmpty(value);
                }
            }
            value = null;
            return false;
        }
    }

    [Serializable]
    public class Translation
    {
        public string languageCode; // "en", "fr", ...
        [TextArea] public string text;
    }
}