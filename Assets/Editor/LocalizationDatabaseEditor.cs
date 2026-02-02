#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LocalizationDatabase))]
public class LocalizationDatabaseEditor : Editor
{
    private string newKey = "";
    private Language newLanguage;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var db = (LocalizationDatabase)target;

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

        // Add key
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Add New Key", EditorStyles.boldLabel);
        newKey = EditorGUILayout.TextField("Key", newKey);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newKey)))
        {
            if (GUILayout.Button("Add Key"))
            {
                AddKey(db, newKey.Trim());
                newKey = "";
            }
        }
        EditorGUILayout.EndVertical();

        // Add language
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Add New Language", EditorStyles.boldLabel);
        newLanguage = (Language)EditorGUILayout.ObjectField("Language Asset", newLanguage, typeof(Language), false);

        using (new EditorGUI.DisabledScope(newLanguage == null))
        {
            if (GUILayout.Button("Add Language To Database"))
            {
                AddLanguage(db, newLanguage);
                newLanguage = null;
            }
        }
        EditorGUILayout.EndVertical();

        // Fix / normalize
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Maintenance", EditorStyles.boldLabel);
        if (GUILayout.Button("Ensure Every Entry Has All Languages"))
        {
            EnsureAllLanguages(db);
        }
        if (GUILayout.Button("Remove Duplicate Keys (keep first)"))
        {
            RemoveDuplicateKeysKeepFirst(db);
        }
        EditorGUILayout.EndVertical();
    }

    private static void AddKey(LocalizationDatabase db, string key)
    {
        if (db.entries.Any(e => e != null && e.key == key))
        {
            Debug.LogWarning($"Key '{key}' already exists.");
            return;
        }

        var entry = new LocalizationDatabase.Entry { key = key };

        foreach (var lang in db.languages)
        {
            if (lang == null || string.IsNullOrEmpty(lang.code)) continue;
            entry.translations.Add(new LocalizationDatabase.Translation
            {
                languageCode = lang.code,
                text = ""
            });
        }

        db.entries.Add(entry);
        EditorUtility.SetDirty(db);
    }

    private static void AddLanguage(LocalizationDatabase db, Language lang)
    {
        if (lang == null || string.IsNullOrEmpty(lang.code))
        {
            Debug.LogWarning("Language asset is missing or has an empty code.");
            return;
        }

        if (db.languages.Any(l => l != null && l.code == lang.code))
        {
            Debug.LogWarning($"Language code '{lang.code}' already exists in database.");
            return;
        }

        db.languages.Add(lang);

        // Add empty translation slot for all entries
        foreach (var entry in db.entries)
        {
            if (entry == null) continue;

            bool has = entry.translations.Any(t => t.languageCode == lang.code);
            if (!has)
            {
                entry.translations.Add(new LocalizationDatabase.Translation
                {
                    languageCode = lang.code,
                    text = ""
                });
            }
        }

        EditorUtility.SetDirty(db);
    }

    private static void EnsureAllLanguages(LocalizationDatabase db)
    {
        foreach (var entry in db.entries)
        {
            if (entry == null) continue;

            foreach (var lang in db.languages)
            {
                if (lang == null || string.IsNullOrEmpty(lang.code)) continue;

                bool has = entry.translations.Any(t => t.languageCode == lang.code);
                if (!has)
                {
                    entry.translations.Add(new LocalizationDatabase.Translation
                    {
                        languageCode = lang.code,
                        text = ""
                    });
                }
            }
        }

        EditorUtility.SetDirty(db);
        Debug.Log("Ensured all entries contain all languages.");
    }

    private static void RemoveDuplicateKeysKeepFirst(LocalizationDatabase db)
    {
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int i = db.entries.Count - 1; i >= 0; i--)
        {
            var e = db.entries[i];
            if (e == null || string.IsNullOrEmpty(e.key)) continue;

            if (!seen.Add(e.key))
                db.entries.RemoveAt(i);
        }

        EditorUtility.SetDirty(db);
        Debug.Log("Removed duplicate keys (kept first occurrence).");
    }
}
#endif