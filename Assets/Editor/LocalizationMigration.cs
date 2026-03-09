#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class LocalizationMigration
{
    private const string StreamingAssetsPath = "Assets/StreamingAssets";
    private const string LocalizationFolder = "Localization";
    private const string LanguagesFileName = "languages.json";

    [MenuItem("Tools/Localization/Migrate to JSON")]
    public static void MigrateToJson()
    {
        string assetPath = "Assets/Scriptables/Localization/LanguageDatabase.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        if (asset == null)
        {
            assetPath = AssetDatabase.GUIDToAssetPath("5d27c865bf91e5046b142d49b4d5acd5");
            if (string.IsNullOrEmpty(assetPath))
            {
                var guids = AssetDatabase.FindAssets("t:LocalizationDatabase LanguageDatabase");
                if (guids.Length == 0)
                    guids = AssetDatabase.FindAssets("LanguageDatabase");
                if (guids.Length > 0)
                    assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            }
        }

        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("[LocalizationMigration] Could not find LanguageDatabase.asset");
            return;
        }

        string fullPath = Path.Combine(Application.dataPath, "..", assetPath).Replace("\\", "/");
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[LocalizationMigration] File not found: {fullPath}");
            return;
        }

        string content = File.ReadAllText(fullPath);

        var languages = LoadLanguagesFromAsset(content);
        var entries = ParseEntriesFromAsset(content);

        string outputDir = Path.Combine(StreamingAssetsPath, LocalizationFolder);
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        WriteLanguagesJson(outputDir, languages);
        WriteTranslationJsonFiles(outputDir, languages, entries);

        AssetDatabase.Refresh();
        Debug.Log($"[LocalizationMigration] Migrated {entries.Count} keys to {outputDir}");
    }

    private static List<LocalizationDatabase.LanguageInfo> LoadLanguagesFromAsset(string content)
    {
        var result = new List<LocalizationDatabase.LanguageInfo>();
        string enPath = AssetDatabase.GUIDToAssetPath("fb62616145ecf4c4fbfd674081237649");
        string ruPath = AssetDatabase.GUIDToAssetPath("c3ec938de9dbd8f47b650be06af52a0e");

        result.Add(LoadLanguageFromAsset(enPath, "en", "English"));
        result.Add(LoadLanguageFromAsset(ruPath, "ru", "Русский"));
        return result;
    }

    private static LocalizationDatabase.LanguageInfo LoadLanguageFromAsset(string path, string defaultCode, string defaultDisplay)
    {
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            return new LocalizationDatabase.LanguageInfo { code = defaultCode, displayName = defaultDisplay };
        try
        {
            string yaml = System.IO.File.ReadAllText(path);
            string code = defaultCode, display = defaultDisplay;
            if (yaml.Contains("code:"))
            {
                int i = yaml.IndexOf("code:");
                int end = yaml.IndexOf('\n', i);
                code = end > 0 ? yaml.Substring(i + 5, end - i - 5).Trim() : defaultCode;
            }
            if (yaml.Contains("displayName:"))
            {
                int i = yaml.IndexOf("displayName:");
                int end = yaml.IndexOf('\n', i);
                display = end > 0 ? yaml.Substring(i + 12, end - i - 12).Trim().Trim('"') : defaultDisplay;
            }
            return new LocalizationDatabase.LanguageInfo { code = code, displayName = display };
        }
        catch { }
        return new LocalizationDatabase.LanguageInfo { code = defaultCode, displayName = defaultDisplay };
    }

    private static List<ParsedEntry> ParseEntriesFromAsset(string content)
    {
        var entries = new List<ParsedEntry>();
        int i = content.IndexOf("entries:");
        if (i < 0) return entries;

        i = content.IndexOf("\n", i) + 1;
        while (i < content.Length)
        {
            string line = ReadLine(content, ref i);
            if (string.IsNullOrWhiteSpace(line)) continue;

            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("- key:"))
            {
                string key = trimmed.Substring(6).Trim();
                SkipTranslationsHeader(content, ref i);
                var translations = ParseTranslations(content, ref i);
                entries.Add(new ParsedEntry { key = key, translations = translations });
            }
        }

        return entries;
    }

    private static void SkipTranslationsHeader(string content, ref int i)
    {
        while (i < content.Length)
        {
            string line = ReadLine(content, ref i);
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.TrimStart().StartsWith("translations:"))
                return;
        }
    }

    private static Dictionary<string, string> ParseTranslations(string content, ref int i)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        while (i < content.Length)
        {
            int lineStart = i;
            string line = ReadLine(content, ref i);
            if (string.IsNullOrWhiteSpace(line)) continue;

            int indent = GetIndent(line);
            string trimmed = line.TrimStart();

            if (indent <= 2 && !trimmed.StartsWith("- languageCode:"))
            {
                i = lineStart;
                break;
            }

            if (trimmed.StartsWith("- languageCode:"))
            {
                string langCode = trimmed.Substring(15).Trim();
                string text = ReadTextValue(content, ref i);
                result[langCode] = text;
            }
        }
        return result;
    }

    private static string ReadTextValue(string content, ref int i)
    {
        if (i >= content.Length) return "";
        string line = ReadLine(content, ref i);
        string trimmed = line.TrimStart();
        if (!trimmed.StartsWith("text:")) return "";

        string valuePart = trimmed.Substring(5).TrimStart();
        if (valuePart.StartsWith("\""))
        {
            var sb = new StringBuilder();
            valuePart = valuePart.Substring(1);
            while (true)
            {
                int endQuote = valuePart.IndexOf('"');
                if (endQuote >= 0)
                {
                    sb.Append(UnescapeUnityString(valuePart.Substring(0, endQuote)));
                    break;
                }
                sb.Append(UnescapeUnityString(valuePart));
                if (i >= content.Length) break;
                line = ReadLine(content, ref i);
                valuePart = line.TrimStart();
            }
            return sb.ToString();
        }
        return UnescapeUnityString(valuePart);
    }

    private static string ReadLine(string content, ref int i)
    {
        int start = i;
        while (i < content.Length && content[i] != '\n')
            i++;
        string line = content.Substring(start, i - start);
        if (i < content.Length) i++;
        return line;
    }

    private static int GetIndent(string line)
    {
        int n = 0;
        while (n < line.Length && (line[n] == ' ' || line[n] == '\t'))
            n++;
        return n;
    }

    private static string UnescapeUnityString(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                if (s[i + 1] == 'u' && i + 5 < s.Length)
                {
                    string hex = s.Substring(i + 2, 4);
                    if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int code))
                    {
                        sb.Append((char)code);
                        i += 5;
                        continue;
                    }
                }
                else if (s[i + 1] == 'n') { sb.Append('\n'); i++; continue; }
                else if (s[i + 1] == 'r') { sb.Append('\r'); i++; continue; }
                else if (s[i + 1] == 't') { sb.Append('\t'); i++; continue; }
                else if (s[i + 1] == '"') { sb.Append('"'); i++; continue; }
                else if (s[i + 1] == '\\') { sb.Append('\\'); i++; continue; }
            }
            sb.Append(s[i]);
        }
        return sb.ToString();
    }

    private static void WriteLanguagesJson(string outputDir, List<LocalizationDatabase.LanguageInfo> languages)
    {
        string json = LocalizationDatabase.SerializeLanguages(languages);
        File.WriteAllText(Path.Combine(outputDir, LanguagesFileName), json);
    }

    private static void WriteTranslationJsonFiles(string outputDir, List<LocalizationDatabase.LanguageInfo> languages,
        List<ParsedEntry> entries)
    {
        foreach (var lang in languages)
        {
            if (string.IsNullOrEmpty(lang.code)) continue;

            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var e in entries)
            {
                if (e.translations.TryGetValue(lang.code, out var text))
                    dict[e.key] = text;
                else
                    dict[e.key] = "";
            }

            string json = LocalizationDatabase.SerializeTranslations(dict);
            File.WriteAllText(Path.Combine(outputDir, $"{lang.code}.json"), json);
        }
    }

    private class ParsedEntry
    {
        public string key;
        public Dictionary<string, string> translations;
    }
}
#endif
