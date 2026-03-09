using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LocalizationDatabase
{
    [Serializable]
    public class LanguageInfo
    {
        public string code;
        public string displayName;
    }

    public IReadOnlyList<LanguageInfo> Languages => _languages;
    private List<LanguageInfo> _languages = new();

    // languageCode -> (key -> text)
    private Dictionary<string, Dictionary<string, string>> _translations = new(StringComparer.Ordinal);

    private const string LanguagesFileName = "languages.json";

    public bool TryGet(string key, string languageCode, out string value)
    {
        value = null;
        if (string.IsNullOrEmpty(key)) return false;

        if (_translations.TryGetValue(languageCode, out var langDict) &&
            langDict.TryGetValue(key, out var text) && !string.IsNullOrEmpty(text))
        {
            value = text;
            return true;
        }

        return false;
    }

    public bool HasKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        foreach (var langDict in _translations.Values)
        {
            if (langDict.ContainsKey(key))
                return true;
        }

        return false;
    }

    public void AddEntry(string key, params (string languageCode, string text)[] translations)
    {
        if (string.IsNullOrEmpty(key)) return;

        foreach (var (langCode, text) in translations)
        {
            if (string.IsNullOrEmpty(langCode)) continue;

            if (!_translations.TryGetValue(langCode, out var dict))
            {
                dict = new Dictionary<string, string>(StringComparer.Ordinal);
                _translations[langCode] = dict;
            }

            dict[key] = text ?? "";
        }
    }

    public List<string> GetAvailableLanguageCodes()
    {
        var codes = new List<string>();
        foreach (var lang in _languages)
        {
            if (lang != null && !string.IsNullOrEmpty(lang.code))
                codes.Add(lang.code);
        }
        return codes;
    }

    public string GetLanguageDisplayName(string code)
    {
        foreach (var lang in _languages)
        {
            if (lang != null && lang.code == code)
                return lang.displayName ?? code;
        }
        return code;
    }

    public bool LoadFromPath(string folderPath)
    {
        _languages.Clear();
        _translations.Clear();

        string basePath = ResolveBasePath(folderPath);
        if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath))
        {
            Debug.LogWarning($"[Localization] Path not found: {basePath}");
            return false;
        }

        string languagesPath = Path.Combine(basePath, LanguagesFileName);
        if (!File.Exists(languagesPath))
        {
            Debug.LogWarning($"[Localization] {LanguagesFileName} not found at {languagesPath}");
            return false;
        }

        try
        {
            string languagesJson = File.ReadAllText(languagesPath);
            var wrapped = TryWrapRootArray(languagesJson);
            var languagesArray = JsonUtility.FromJson<LanguageInfoArray>(wrapped);
            if (languagesArray?.languages != null)
            {
                _languages = languagesArray.languages;
            }

            foreach (var lang in _languages)
            {
                if (lang == null || string.IsNullOrEmpty(lang.code)) continue;

                string langPath = Path.Combine(basePath, $"{lang.code}.json");
                if (!File.Exists(langPath))
                {
                    _translations[lang.code] = new Dictionary<string, string>(StringComparer.Ordinal);
                    continue;
                }

                string langJson = File.ReadAllText(langPath);
                var wrappedLang = TryWrapRootObject(langJson);
                var dict = JsonUtility.FromJson<StringDictionary>(wrappedLang);
                var map = new Dictionary<string, string>(StringComparer.Ordinal);
                if (dict?.entries != null)
                {
                    foreach (var e in dict.entries)
                    {
                        if (e != null && !string.IsNullOrEmpty(e.key))
                            map[e.key] = e.value ?? "";
                    }
                }
                _translations[lang.code] = map;
            }

            return _languages.Count > 0;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Localization] Failed to load: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    public static string GetLocalizationBasePath(string folderPath) => ResolveBasePath(folderPath);

    private static string ResolveBasePath(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
            folderPath = "Localization";

        folderPath = folderPath.Trim('/', '\\');

#if UNITY_EDITOR
        string streamingAssets = Path.Combine(Application.dataPath, "StreamingAssets");
#else
        string streamingAssets = Application.streamingAssetsPath;
#endif
        return Path.Combine(streamingAssets, folderPath);
    }

    private static string TryWrapRootArray(string json)
    {
        string t = json.Trim();
        if (t.StartsWith("["))
            return "{\"languages\":" + json + "}";
        return json;
    }

    private static string TryWrapRootObject(string json)
    {
        string t = json.Trim();
        if (t.StartsWith("{") && t.Contains("\"entries\""))
            return json;
        if (t.StartsWith("{"))
            return ParseFlatObjectToWrapper(json);
        return json;
    }

    private static string ParseFlatObjectToWrapper(string json)
    {
        try
        {
            var entries = new List<string>();
            int i = 0;
            while (i < json.Length)
            {
                int keyStart = json.IndexOf('"', i);
                if (keyStart < 0) break;
                int keyEnd = json.IndexOf('"', keyStart + 1);
                if (keyEnd < 0) break;
                string key = UnescapeJson(json.Substring(keyStart + 1, keyEnd - keyStart - 1));
                int colon = json.IndexOf(':', keyEnd);
                if (colon < 0) break;
                int valStart = json.IndexOf('"', colon);
                if (valStart < 0) break;
                int valEnd = valStart + 1;
                while (valEnd < json.Length)
                {
                    if (json[valEnd] == '\\') { valEnd += 2; continue; }
                    if (json[valEnd] == '"') break;
                    valEnd++;
                }
                string value = valEnd < json.Length ? UnescapeJson(json.Substring(valStart + 1, valEnd - valStart - 1)) : "";
                entries.Add("{\"key\":\"" + EscapeJson(key) + "\",\"value\":\"" + EscapeJson(value) + "\"}");
                i = valEnd + 1;
                int comma = json.IndexOf(',', i);
                if (comma < 0) break;
                i = comma + 1;
            }
            return "{\"entries\":[" + string.Join(",", entries) + "]}";
        }
        catch
        {
            return json;
        }
    }

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    private static string UnescapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");
    }

    [Serializable]
    private class LanguageInfoArray
    {
        public List<LanguageInfo> languages;
    }

    [Serializable]
    private class StringDictionary
    {
        public List<KeyValueEntry> entries;
    }

    [Serializable]
    private class KeyValueEntry
    {
        public string key;
        public string value;
    }

    public static string SerializeLanguages(List<LanguageInfo> languages)
    {
        return JsonUtility.ToJson(new LanguageInfoArray { languages = languages }, false);
    }

    public static string SerializeTranslations(Dictionary<string, string> dict)
    {
        var entries = new List<KeyValueEntry>();
        if (dict != null)
        {
            foreach (var kv in dict)
                entries.Add(new KeyValueEntry { key = kv.Key, value = kv.Value ?? "" });
        }
        return JsonUtility.ToJson(new StringDictionary { entries = entries }, false);
    }
}
