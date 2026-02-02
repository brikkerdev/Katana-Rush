#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LocalizationDatabaseWindow : EditorWindow
{
    private LocalizationDatabase db;

    private Vector2 scroll;
    private string search = "";

    private string newKey = "";
    private Language newLanguageAsset;

    // Table layout
    private const float KeyColWidth = 240f;
    private const float CellMinWidth = 180f;
    private const float RowHeight = 20f;

    [MenuItem("Tools/Localization/Database Table Editor")]
    public static void Open()
    {
        GetWindow<LocalizationDatabaseWindow>("Localization Table");
    }

    private void OnGUI()
    {
        DrawTopBar();

        if (db == null)
        {
            EditorGUILayout.HelpBox("Assign a LocalizationDatabase asset to edit.", MessageType.Info);
            return;
        }

        if (db.languages == null) db.languages = new List<Language>();
        if (db.entries == null) db.entries = new List<LocalizationDatabase.Entry>();

        DrawTools();

        EditorGUILayout.Space(6);
        DrawTable();
    }

    private void DrawTopBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            db = (LocalizationDatabase)EditorGUILayout.ObjectField(
                new GUIContent("Database"),
                db,
                typeof(LocalizationDatabase),
                false,
                GUILayout.MinWidth(350)
            );

            GUILayout.FlexibleSpace();

            search = GUILayout.TextField(search, GUI.skin.FindStyle("ToolbarSearchTextField"), GUILayout.Width(260));
            if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSearchCancelButton")))
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
                        AddKey(db, newKey.Trim());
                        newKey = "";
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                newLanguageAsset = (Language)EditorGUILayout.ObjectField("Add Language", newLanguageAsset, typeof(Language), false);
                using (new EditorGUI.DisabledScope(newLanguageAsset == null))
                {
                    if (GUILayout.Button("Add Column", GUILayout.Width(110)))
                    {
                        AddLanguage(db, newLanguageAsset);
                        newLanguageAsset = null;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ensure All Cells Exist"))
                {
                    EnsureAllLanguages(db);
                    MarkDirty(db);
                }

                if (GUILayout.Button("Sort Keys A→Z"))
                {
                    db.entries = db.entries
                        .Where(e => e != null)
                        .OrderBy(e => e.key, StringComparer.Ordinal)
                        .ToList();
                    MarkDirty(db);
                }
            }
        }
    }

    private void DrawTable()
    {
        // filtered entries
        var entries = FilterEntries(db.entries, search);

        // Header
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Key", EditorStyles.boldLabel, GUILayout.Width(KeyColWidth));

            for (int i = 0; i < db.languages.Count; i++)
            {
                var lang = db.languages[i];
                if (lang == null) continue;

                var header = $"{lang.code}\n{lang.displayName}";
                var w = Mathf.Max(CellMinWidth, 120f);

                // Column header with small remove button
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(w)))
                {
                    EditorGUILayout.LabelField(header, EditorStyles.miniBoldLabel, GUILayout.Width(w));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        var oldColor = GUI.color;
                        GUI.color = new Color(1f, 0.7f, 0.7f);
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            RemoveLanguage(db, lang.code);
                            MarkDirty(db);
                            GUI.FocusControl(null);
                            GUI.color = oldColor;
                            break;
                        }
                        GUI.color = oldColor;
                    }
                }
            }

            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.Space(4);

        // Scroll view of rows
        scroll = EditorGUILayout.BeginScrollView(scroll);

        // Light grid background
        var bg = new GUIStyle("box");
        using (new EditorGUILayout.VerticalScope(bg))
        {
            for (int r = 0; r < entries.Count; r++)
            {
                var entry = entries[r];
                if (entry == null) continue;

                EnsureEntryHasAllLanguages(db, entry);

                using (new EditorGUILayout.HorizontalScope(GUILayout.Height(RowHeight)))
                {
                    // Key cell + delete row
                    using (new EditorGUILayout.HorizontalScope(GUILayout.Width(KeyColWidth)))
                    {
                        entry.key = EditorGUILayout.TextField(entry.key, GUILayout.Width(KeyColWidth - 60));

                        var oldColor = GUI.color;
                        GUI.color = new Color(1f, 0.75f, 0.75f);
                        if (GUILayout.Button("X", GUILayout.Width(28)))
                        {
                            if (EditorUtility.DisplayDialog("Delete key?",
                                    $"Delete localization key:\n\n{entry.key}",
                                    "Delete", "Cancel"))
                            {
                                db.entries.Remove(entry);
                                MarkDirty(db);
                                GUI.color = oldColor;
                                break;
                            }
                        }
                        GUI.color = oldColor;
                    }

                    // Translation cells
                    for (int c = 0; c < db.languages.Count; c++)
                    {
                        var lang = db.languages[c];
                        if (lang == null) continue;

                        var w = Mathf.Max(CellMinWidth, 120f);

                        var t = GetOrCreateTranslation(entry, lang.code);
                        EditorGUI.BeginChangeCheck();

                        // Single-line table cells (Android Studio table vibe).
                        // If you want multiline, switch to EditorGUILayout.TextArea and adjust row height.
                        string newText = EditorGUILayout.TextField(t.text, GUILayout.Width(w));

                        if (EditorGUI.EndChangeCheck())
                        {
                            t.text = newText;
                            MarkDirty(db);
                        }
                    }

                    GUILayout.FlexibleSpace();
                }

                // Subtle row separator
                var rect = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax, rect.width, 1), new Color(0, 0, 0, 0.12f));
            }
        }

        EditorGUILayout.EndScrollView();

        // Extra safety: if you renamed keys, duplicates can happen.
        // You can add a warning below.
        var duplicates = FindDuplicateKeys(db.entries);
        if (duplicates.Count > 0)
        {
            EditorGUILayout.HelpBox("Duplicate keys detected:\n" + string.Join(", ", duplicates), MessageType.Warning);
        }
    }

    // ---------- Data helpers ----------

    private static List<LocalizationDatabase.Entry> FilterEntries(List<LocalizationDatabase.Entry> entries, string search)
    {
        if (entries == null) return new List<LocalizationDatabase.Entry>();
        if (string.IsNullOrWhiteSpace(search))
            return entries.Where(e => e != null).ToList();

        search = search.Trim();

        return entries
            .Where(e => e != null && !string.IsNullOrEmpty(e.key) &&
                        e.key.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
    }

    private static void AddKey(LocalizationDatabase db, string key)
    {
        if (db.entries.Any(e => e != null && e.key == key))
        {
            EditorUtility.DisplayDialog("Key exists", $"Key '{key}' already exists.", "OK");
            return;
        }

        var entry = new LocalizationDatabase.Entry { key = key, translations = new List<LocalizationDatabase.Translation>() };

        foreach (var lang in db.languages)
        {
            if (lang == null || string.IsNullOrEmpty(lang.code)) continue;
            entry.translations.Add(new LocalizationDatabase.Translation { languageCode = lang.code, text = "" });
        }

        db.entries.Add(entry);
        MarkDirty(db);
    }

    private static void AddLanguage(LocalizationDatabase db, Language lang)
    {
        if (lang == null || string.IsNullOrEmpty(lang.code))
        {
            EditorUtility.DisplayDialog("Invalid language", "Language asset is null or has empty code.", "OK");
            return;
        }

        if (db.languages.Any(l => l != null && l.code == lang.code))
        {
            EditorUtility.DisplayDialog("Language exists", $"Language code '{lang.code}' already exists.", "OK");
            return;
        }

        db.languages.Add(lang);

        // Add empty translation cell to every entry
        foreach (var entry in db.entries)
        {
            if (entry == null) continue;
            GetOrCreateTranslation(entry, lang.code);
        }

        MarkDirty(db);
    }

    private static void RemoveLanguage(LocalizationDatabase db, string langCode)
    {
        // remove from languages list
        db.languages.RemoveAll(l => l != null && l.code == langCode);

        // remove from each entry
        foreach (var entry in db.entries)
        {
            entry?.translations?.RemoveAll(t => t.languageCode == langCode);
        }

        MarkDirty(db);
    }

    private static void EnsureAllLanguages(LocalizationDatabase db)
    {
        foreach (var entry in db.entries)
        {
            if (entry == null) continue;
            EnsureEntryHasAllLanguages(db, entry);
        }
    }

    private static void EnsureEntryHasAllLanguages(LocalizationDatabase db, LocalizationDatabase.Entry entry)
    {
        entry.translations ??= new List<LocalizationDatabase.Translation>();

        foreach (var lang in db.languages)
        {
            if (lang == null || string.IsNullOrEmpty(lang.code)) continue;
            GetOrCreateTranslation(entry, lang.code);
        }
    }

    private static LocalizationDatabase.Translation GetOrCreateTranslation(LocalizationDatabase.Entry entry, string langCode)
    {
        entry.translations ??= new List<LocalizationDatabase.Translation>();

        for (int i = 0; i < entry.translations.Count; i++)
        {
            if (entry.translations[i].languageCode == langCode)
                return entry.translations[i];
        }

        var t = new LocalizationDatabase.Translation { languageCode = langCode, text = "" };
        entry.translations.Add(t);
        return t;
    }

    private static void MarkDirty(UnityEngine.Object obj)
    {
        EditorUtility.SetDirty(obj);
        // ensures saving works even if only nested lists changed
        AssetDatabase.SaveAssets();
    }

    private static List<string> FindDuplicateKeys(List<LocalizationDatabase.Entry> entries)
    {
        var duplicates = new List<string>();
        if (entries == null) return duplicates;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var dup = new HashSet<string>(StringComparer.Ordinal);

        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrEmpty(e.key)) continue;
            if (!seen.Add(e.key)) dup.Add(e.key);
        }

        duplicates.AddRange(dup);
        duplicates.Sort(StringComparer.Ordinal);
        return duplicates;
    }
}
#endif