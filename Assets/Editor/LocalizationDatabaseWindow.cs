#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LocalizationDatabaseWindow : EditorWindow
{
    private LocalizationConfig config;
    private string folderPath = "Localization";

    private List<LocalizationDatabase.LanguageInfo> languages = new();
    private Dictionary<string, Dictionary<string, string>> translations = new(StringComparer.Ordinal);
    private List<string> allKeys = new();

    private Vector2 scroll;
    private string search = "";
    private string newKey = "";
    private string newLanguageCode = "";
    private string newLanguageDisplayName = "";
    private bool dirty;

    private const float KeyColWidth = 240f;
    private const float CellMinWidth = 180f;
    private const float RowHeight = 20f;

    [MenuItem("Tools/Localization/Database Table Editor")]
    public static void Open()
    {
        GetWindow<LocalizationDatabaseWindow>("Localization Table");
    }

    private string GetBasePath()
    {
        string path = config != null ? config.jsonFolderPath : folderPath;
        if (string.IsNullOrEmpty(path)) path = "Localization";
        path = path.Trim('/', '\\');
        return Path.Combine(Path.Combine(Application.dataPath, "StreamingAssets"), path);
    }

    private void LoadFromJson()
    {
        string basePath = GetBasePath();
        if (!Directory.Exists(basePath))
        {
            languages.Clear();
            translations.Clear();
            allKeys.Clear();
            return;
        }

        string languagesPath = Path.Combine(basePath, "languages.json");
        if (!File.Exists(languagesPath))
        {
            languages.Clear();
            translations.Clear();
            allKeys.Clear();
            return;
        }

        try
        {
            string json = File.ReadAllText(languagesPath);
            var wrapped = json.TrimStart().StartsWith("[") ? "{\"languages\":" + json + "}" : json;
            var arr = JsonUtility.FromJson<LanguageInfoArray>(wrapped);
            languages = arr?.languages ?? new List<LocalizationDatabase.LanguageInfo>();

            translations.Clear();
            var keySet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var lang in languages)
            {
                if (lang == null || string.IsNullOrEmpty(lang.code)) continue;
                string langPath = Path.Combine(basePath, $"{lang.code}.json");
                var dict = new Dictionary<string, string>(StringComparer.Ordinal);
                if (File.Exists(langPath))
                {
                    string langJson = File.ReadAllText(langPath);
                    var wrappedLang = langJson.TrimStart().StartsWith("{") && !langJson.Contains("\"entries\"")
                        ? ParseFlatToWrapper(langJson)
                        : langJson;
                    var sd = JsonUtility.FromJson<StringDictWrapper>(wrappedLang);
                    if (sd?.entries != null)
                        foreach (var e in sd.entries)
                            if (e != null && !string.IsNullOrEmpty(e.key))
                            {
                                dict[e.key] = e.value ?? "";
                                keySet.Add(e.key);
                            }
                }
                translations[lang.code] = dict;
            }

            allKeys = keySet.OrderBy(k => k, StringComparer.Ordinal).ToList();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Localization] Failed to load: {e.Message}");
        }
    }

    private static string ParseFlatToWrapper(string json)
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
                string key = json.Substring(keyStart + 1, keyEnd - keyStart - 1);
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
                string value = valEnd < json.Length ? json.Substring(valStart + 1, valEnd - valStart - 1) : "";
                entries.Add("{\"key\":\"" + EscapeJson(key) + "\",\"value\":\"" + EscapeJson(value) + "\"}");
                i = valEnd + 1;
                int comma = json.IndexOf(',', i);
                if (comma < 0) break;
                i = comma + 1;
            }
            return "{\"entries\":[" + string.Join(",", entries) + "]}";
        }
        catch { return json; }
    }

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    private void SaveToJson()
    {
        string basePath = GetBasePath();
        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        File.WriteAllText(Path.Combine(basePath, "languages.json"), LocalizationDatabase.SerializeLanguages(languages));

        foreach (var lang in languages)
        {
            if (lang == null || string.IsNullOrEmpty(lang.code)) continue;
            if (!translations.TryGetValue(lang.code, out var dict))
                dict = new Dictionary<string, string>(StringComparer.Ordinal);
            File.WriteAllText(Path.Combine(basePath, $"{lang.code}.json"), LocalizationDatabase.SerializeTranslations(dict));
        }

        dirty = false;
        AssetDatabase.Refresh();
    }

    private void OnEnable()
    {
        var guids = AssetDatabase.FindAssets("t:LocalizationConfig");
        if (guids.Length > 0)
            config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        LoadFromJson();
    }

    private void OnGUI()
    {
        DrawTopBar();

        if (string.IsNullOrEmpty(GetBasePath()) || !Directory.Exists(GetBasePath()))
        {
            EditorGUILayout.HelpBox("Assign LocalizationConfig or set folder path. JSON files in StreamingAssets/Localization.", MessageType.Info);
            return;
        }

        if (languages.Count == 0)
        {
            EditorGUILayout.HelpBox("No languages found. Run Tools > Localization > Migrate to JSON first, or add languages.", MessageType.Warning);
        }

        DrawTools();

        EditorGUILayout.Space(6);
        DrawTable();

        if (dirty && GUILayout.Button("Save All", GUILayout.Height(28)))
            SaveToJson();
    }

    private void DrawTopBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            config = (LocalizationConfig)EditorGUILayout.ObjectField("Config", config, typeof(LocalizationConfig), false, GUILayout.MinWidth(200));
            if (config == null)
            {
                folderPath = EditorGUILayout.TextField("Folder", folderPath, GUILayout.Width(150));
            }

            if (GUILayout.Button("Reload", GUILayout.Width(60)))
                LoadFromJson();

            GUILayout.FlexibleSpace();

            search = GUILayout.TextField(search, GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.toolbarSearchField, GUILayout.Width(200));
            if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? "OL Minus", GUILayout.Width(20)))
            {
                search = "";
                GUI.FocusControl(null);
            }
        }
    }

    private void DrawTools()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Quick actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                newKey = EditorGUILayout.TextField("New Key", newKey);
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newKey)))
                {
                    if (GUILayout.Button("Add Key", GUILayout.Width(110)))
                    {
                        AddKey(newKey.Trim());
                        newKey = "";
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                newLanguageCode = EditorGUILayout.TextField("Code", newLanguageCode, GUILayout.Width(80));
                newLanguageDisplayName = EditorGUILayout.TextField("Display Name", newLanguageDisplayName, GUILayout.Width(120));
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newLanguageCode)))
                {
                    if (GUILayout.Button("Add Language", GUILayout.Width(110)))
                    {
                        AddLanguage(newLanguageCode.Trim(), newLanguageDisplayName.Trim());
                        newLanguageCode = "";
                        newLanguageDisplayName = "";
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ensure All Cells Exist"))
                    EnsureAllCellsExist();
                if (GUILayout.Button("Sort Keys A→Z"))
                {
                    allKeys = allKeys.OrderBy(k => k, StringComparer.Ordinal).ToList();
                    MarkDirty();
                }
            }
        }
    }

    private void DrawTable()
    {
        var filtered = string.IsNullOrWhiteSpace(search)
            ? allKeys
            : allKeys.Where(k => k.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Key", EditorStyles.boldLabel, GUILayout.Width(KeyColWidth));
            foreach (var lang in languages)
            {
                if (lang == null || string.IsNullOrEmpty(lang.code)) continue;
                var w = Mathf.Max(CellMinWidth, 120f);
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(w)))
                {
                    EditorGUILayout.LabelField($"{lang.code}\n{lang.displayName}", EditorStyles.miniBoldLabel, GUILayout.Width(w));
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        RemoveLanguage(lang.code);
                        return;
                    }
                }
            }
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.Space(4);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        var bg = new GUIStyle("box");
        using (new EditorGUILayout.VerticalScope(bg))
        {
            for (int r = 0; r < filtered.Count; r++)
            {
                string key = filtered[r];
                using (new EditorGUILayout.HorizontalScope(GUILayout.Height(RowHeight)))
                {
                    EditorGUI.BeginChangeCheck();
                    string newKeyVal = EditorGUILayout.TextField(key, GUILayout.Width(KeyColWidth - 60));
                    if (EditorGUI.EndChangeCheck() && newKeyVal != key && !string.IsNullOrEmpty(newKeyVal))
                    {
                        RenameKey(key, newKeyVal);
                        key = newKeyVal;
                    }

                    if (GUILayout.Button("X", GUILayout.Width(28)))
                    {
                        if (EditorUtility.DisplayDialog("Delete key?", $"Delete: {key}", "Delete", "Cancel"))
                        {
                            RemoveKey(key);
                            return;
                        }
                    }

                    foreach (var lang in languages)
                    {
                        if (lang == null || string.IsNullOrEmpty(lang.code)) continue;
                        var w = Mathf.Max(CellMinWidth, 120f);
                        string val = GetText(key, lang.code);
                        EditorGUI.BeginChangeCheck();
                        string newVal = EditorGUILayout.TextField(val, GUILayout.Width(w));
                        if (EditorGUI.EndChangeCheck())
                        {
                            SetText(key, lang.code, newVal);
                            MarkDirty();
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
                var rect = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax, rect.width, 1), new Color(0, 0, 0, 0.12f));
            }
        }

        EditorGUILayout.EndScrollView();

        var duplicates = allKeys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            EditorGUILayout.HelpBox("Duplicate keys: " + string.Join(", ", duplicates), MessageType.Warning);
    }

    private string GetText(string key, string langCode)
    {
        if (translations.TryGetValue(langCode, out var dict) && dict.TryGetValue(key, out var t))
            return t;
        return "";
    }

    private void SetText(string key, string langCode, string text)
    {
        if (!translations.TryGetValue(langCode, out var dict))
        {
            dict = new Dictionary<string, string>(StringComparer.Ordinal);
            translations[langCode] = dict;
        }
        dict[key] = text ?? "";
        if (!allKeys.Contains(key))
        {
            allKeys.Add(key);
            allKeys.Sort(StringComparer.Ordinal);
        }
    }

    private void AddKey(string key)
    {
        if (allKeys.Contains(key))
        {
            EditorUtility.DisplayDialog("Key exists", $"Key '{key}' already exists.", "OK");
            return;
        }
        allKeys.Add(key);
        allKeys.Sort(StringComparer.Ordinal);
        foreach (var lang in languages)
        {
            if (lang == null || string.IsNullOrEmpty(lang.code)) continue;
            if (!translations.TryGetValue(lang.code, out var dict))
            {
                dict = new Dictionary<string, string>(StringComparer.Ordinal);
                translations[lang.code] = dict;
            }
            dict[key] = "";
        }
        MarkDirty();
    }

    private void AddLanguage(string code, string displayName)
    {
        if (languages.Any(l => l != null && l.code == code))
        {
            EditorUtility.DisplayDialog("Exists", $"Language '{code}' already exists.", "OK");
            return;
        }
        languages.Add(new LocalizationDatabase.LanguageInfo { code = code, displayName = string.IsNullOrEmpty(displayName) ? code : displayName });
        translations[code] = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var k in allKeys)
            translations[code][k] = "";
        MarkDirty();
    }

    private void RemoveLanguage(string langCode)
    {
        languages.RemoveAll(l => l != null && l.code == langCode);
        translations.Remove(langCode);
        MarkDirty();
    }

    private void RemoveKey(string key)
    {
        allKeys.Remove(key);
        foreach (var dict in translations.Values)
            dict.Remove(key);
        MarkDirty();
    }

    private void RenameKey(string oldKey, string newKey)
    {
        if (allKeys.Contains(newKey)) return;
        int idx = allKeys.IndexOf(oldKey);
        if (idx >= 0) allKeys[idx] = newKey;
        foreach (var dict in translations.Values)
        {
            if (dict.TryGetValue(oldKey, out var v))
            {
                dict.Remove(oldKey);
                dict[newKey] = v;
            }
        }
        allKeys.Sort(StringComparer.Ordinal);
        MarkDirty();
    }

    private void EnsureAllCellsExist()
    {
        foreach (var key in allKeys)
        {
            foreach (var lang in languages)
            {
                if (lang == null || string.IsNullOrEmpty(lang.code)) continue;
                if (!translations.TryGetValue(lang.code, out var dict))
                {
                    dict = new Dictionary<string, string>(StringComparer.Ordinal);
                    translations[lang.code] = dict;
                }
                if (!dict.ContainsKey(key))
                    dict[key] = "";
            }
        }
        MarkDirty();
    }

    private void MarkDirty() => dirty = true;

    [Serializable]
    private class LanguageInfoArray { public List<LocalizationDatabase.LanguageInfo> languages; }

    [Serializable]
    private class StringDictWrapper { public List<KeyValueEntry> entries; }

    [Serializable]
    private class KeyValueEntry { public string key; public string value; }
}
#endif
