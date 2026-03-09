using System;
using System.Collections.Generic;
using System.IO;
using Runner.Inventory;
using Runner.Player.Data;
using Runner.Save;
using UnityEditor;
using UnityEngine;

namespace Runner.Editor
{
    public class KatanaFactory : EditorWindow
    {
        private const string JsonPath = "Assets/Scriptables/Katanas/katana_definitions.json";
        private const string KatanaBasePath = "Assets/Scriptables/Katanas";

        private Vector2 _scroll;
        private List<KatanaDefinition> _definitions;
        private string _statusMessage;
        private MessageType _statusType;
        private KatanaDatabase _katanaDatabase;
        private LocalizationConfig _localizationConfig;

        [MenuItem("Tools/Katana Factory/Import from JSON")]
        public static void ShowWindow()
        {
            GetWindow<KatanaFactory>("Katana Factory");
        }

        private void OnEnable()
        {
            FindDatabases();
            LoadDefinitions();
        }

        private void FindDatabases()
        {
            var guids = AssetDatabase.FindAssets("t:KatanaDatabase");
            if (guids.Length > 0)
                _katanaDatabase = AssetDatabase.LoadAssetAtPath<KatanaDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));

            guids = AssetDatabase.FindAssets("t:LocalizationConfig");
            if (guids.Length > 0)
                _localizationConfig = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private void LoadDefinitions()
        {
            _definitions = null;
            var fullPath = Path.Combine(Application.dataPath, "..", JsonPath);
            if (!File.Exists(fullPath))
            {
                _statusMessage = $"JSON file not found at {JsonPath}";
                _statusType = MessageType.Warning;
                return;
            }

            try
            {
                var json = File.ReadAllText(fullPath);
                _definitions = JsonHelper.FromJsonArray<KatanaDefinition>(json);
                _statusMessage = $"Loaded {_definitions.Count} definition(s)";
                _statusType = MessageType.Info;
            }
            catch (Exception e)
            {
                _statusMessage = $"JSON parse error: {e.Message}";
                _statusType = MessageType.Error;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Katana Factory", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Reads katana_definitions.json and creates Katana + Preset assets, " +
                "adds localization entries, and registers them in the KatanaDatabase.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            _katanaDatabase = (KatanaDatabase)EditorGUILayout.ObjectField(
                "Katana Database", _katanaDatabase, typeof(KatanaDatabase), false);
            _localizationConfig = (LocalizationConfig)EditorGUILayout.ObjectField(
                "Localization Config", _localizationConfig, typeof(LocalizationConfig), false);

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload JSON"))
                LoadDefinitions();
            if (GUILayout.Button("Open JSON"))
                System.Diagnostics.Process.Start(Path.Combine(Application.dataPath, "..", JsonPath));
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.HelpBox(_statusMessage, _statusType);

            if (_definitions == null || _definitions.Count == 0)
                return;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"Definitions ({_definitions.Count})", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _definitions.Count; i++)
            {
                var def = _definitions[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"{def.name}  ({def.rarity})", EditorStyles.miniLabel);

                EditorGUILayout.LabelField("Name EN", def.name_en);
                EditorGUILayout.LabelField("Description EN", def.description_en);
                EditorGUILayout.LabelField("Base Speed", def.preset?.baseSpeed.ToString() ?? "?");
                EditorGUILayout.LabelField("Max Speed", def.preset?.maxSpeed.ToString() ?? "?");
                EditorGUILayout.LabelField("Max Dashes", def.preset?.maxDashes.ToString() ?? "?");

                var issues = Validate(def);
                if (issues.Count > 0)
                    EditorGUILayout.HelpBox(string.Join("\n", issues), MessageType.Warning);

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);

            GUI.enabled = _katanaDatabase != null;
            if (GUILayout.Button("Import All", GUILayout.Height(32)))
                ImportAll();
            GUI.enabled = true;
        }

        private List<string> Validate(KatanaDefinition def)
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(def.name))
                issues.Add("Name is empty");
            if (def.preset == null)
                issues.Add("Preset is null");
            if (!string.IsNullOrEmpty(def.iconPath) && AssetDatabase.LoadMainAssetAtPath(def.iconPath) == null)
                issues.Add($"Icon not found: {def.iconPath}");
            if (!string.IsNullOrEmpty(def.modelPrefabPath) && AssetDatabase.LoadMainAssetAtPath(def.modelPrefabPath) == null)
                issues.Add($"Model not found: {def.modelPrefabPath}");
            if (!string.IsNullOrEmpty(def.bladeMaterialPath) && AssetDatabase.LoadMainAssetAtPath(def.bladeMaterialPath) == null)
                issues.Add($"Blade material not found: {def.bladeMaterialPath}");
            if (!string.IsNullOrEmpty(def.slashEffectPath) && AssetDatabase.LoadMainAssetAtPath(def.slashEffectPath) == null)
                issues.Add($"Slash effect not found: {def.slashEffectPath}");
            if (!string.IsNullOrEmpty(def.trailEffectPath) && AssetDatabase.LoadMainAssetAtPath(def.trailEffectPath) == null)
                issues.Add($"Trail effect not found: {def.trailEffectPath}");
            if (!string.IsNullOrEmpty(def.abilityPath) && AssetDatabase.LoadMainAssetAtPath(def.abilityPath) == null)
                issues.Add($"Ability not found: {def.abilityPath}");
            return issues;
        }

        private void ImportAll()
        {
            int created = 0;
            int skipped = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var def in _definitions)
                {
                    if (string.IsNullOrWhiteSpace(def.name) || def.preset == null)
                    {
                        Debug.LogWarning($"[KatanaFactory] Skipping invalid definition: {def.name}");
                        skipped++;
                        continue;
                    }

                    if (KatanaAlreadyExists(def.name))
                    {
                        Debug.LogWarning($"[KatanaFactory] Katana '{def.name}' already exists, skipping");
                        skipped++;
                        continue;
                    }

                    CreateKatana(def);
                    created++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            _statusMessage = $"Done! Created {created}, skipped {skipped}";
            _statusType = MessageType.Info;
            Debug.Log($"[KatanaFactory] {_statusMessage}");
        }

        private bool KatanaAlreadyExists(string katanaName)
        {
            var rarity = ParseRarity(katanaName);
            string folder = GetRarityFolder(rarity);
            string assetPath = $"{folder}/{katanaName}_Katana.asset";
            return AssetDatabase.LoadMainAssetAtPath(assetPath) != null;
        }

        private bool KatanaAlreadyExists(KatanaDefinition def)
        {
            var rarity = ParseRarity(def.rarity);
            string folder = GetRarityFolder(rarity);
            string assetPath = $"{folder}/{def.name}_Katana.asset";
            return AssetDatabase.LoadMainAssetAtPath(assetPath) != null;
        }

        private void CreateKatana(KatanaDefinition def)
        {
            var rarity = ParseRarity(def.rarity);
            string folder = GetRarityFolder(rarity);
            EnsureFolder(folder);

            var preset = CreatePreset(def, folder);
            var katana = CreateKatanaAsset(def, rarity, preset, folder);

            RegisterInDatabase(katana);
            AddLocalizationEntries(def);

            Debug.Log($"[KatanaFactory] Created katana '{def.name}' ({rarity}) at {folder}");
        }

        private PlayerPreset CreatePreset(KatanaDefinition def, string folder)
        {
            var preset = ScriptableObject.CreateInstance<PlayerPreset>();
            string assetPath = $"{folder}/{def.name}_Preset.asset";
            AssetDatabase.CreateAsset(preset, assetPath);

            var so = new SerializedObject(preset);
            SetFloat(so, "baseSpeed", def.preset.baseSpeed);
            SetFloat(so, "maxSpeed", def.preset.maxSpeed);
            SetFloat(so, "speedAcceleration", def.preset.speedAcceleration);
            SetFloat(so, "laneSwitchSpeed", def.preset.laneSwitchSpeed);
            SetFloat(so, "jumpForce", def.preset.jumpForce);
            SetInt(so, "maxJumps", def.preset.maxJumps);
            SetFloat(so, "gravity", def.preset.gravity);
            SetFloat(so, "jumpDuration", def.preset.jumpDuration);
            SetFloat(so, "jumpHeight", def.preset.jumpHeight);
            SetInt(so, "maxDashes", def.preset.maxDashes);
            SetFloat(so, "dashDuration", def.preset.dashDuration);
            SetFloat(so, "dashSpeedMultiplier", def.preset.dashSpeedMultiplier);
            SetFloat(so, "dashRegenTime", def.preset.dashRegenTime);
            SetFloat(so, "dashRegenDelay", def.preset.dashRegenDelay);
            SetBool(so, "dashInvincible", def.preset.dashInvincible);
            SetFloat(so, "dashDamage", def.preset.dashDamage);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(preset);
            return preset;
        }

        private Katana CreateKatanaAsset(KatanaDefinition def, KatanaRarity rarity, PlayerPreset preset, string folder)
        {
            var katana = ScriptableObject.CreateInstance<Katana>();
            string assetPath = $"{folder}/{def.name}_Katana.asset";
            AssetDatabase.CreateAsset(katana, assetPath);

            string nameKey = $"katana_name_{def.name.ToLowerInvariant()}";
            string descKey = $"katana_description_{def.name.ToLowerInvariant()}";

            var so = new SerializedObject(katana);
            so.FindProperty("nameKey").stringValue = nameKey;
            so.FindProperty("descriptionKey").stringValue = descKey;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("playerPreset").objectReferenceValue = preset;

            if (!string.IsNullOrEmpty(def.iconPath))
            {
                var icon = AssetDatabase.LoadAssetAtPath<Sprite>(def.iconPath);
                if (icon != null)
                    so.FindProperty("icon").objectReferenceValue = icon;
                else
                    Debug.LogWarning($"[KatanaFactory] Icon not found at '{def.iconPath}' for {def.name}");
            }

            if (!string.IsNullOrEmpty(def.modelPrefabPath))
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(def.modelPrefabPath);
                if (model != null)
                    so.FindProperty("modelPrefab").objectReferenceValue = model;
                else
                    Debug.LogWarning($"[KatanaFactory] Model prefab not found at '{def.modelPrefabPath}' for {def.name}");
            }

            if (!string.IsNullOrEmpty(def.bladeMaterialPath))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(def.bladeMaterialPath);
                if (mat != null)
                    so.FindProperty("bladeMaterial").objectReferenceValue = mat;
                else
                    Debug.LogWarning($"[KatanaFactory] Blade material not found at '{def.bladeMaterialPath}' for {def.name}");
            }

            if (!string.IsNullOrEmpty(def.slashEffectPath))
            {
                var fx = AssetDatabase.LoadAssetAtPath<GameObject>(def.slashEffectPath);
                if (fx != null)
                    so.FindProperty("slashEffectPrefab").objectReferenceValue = fx;
                else
                    Debug.LogWarning($"[KatanaFactory] Slash effect not found at '{def.slashEffectPath}' for {def.name}");
            }

            if (!string.IsNullOrEmpty(def.trailEffectPath))
            {
                var fx = AssetDatabase.LoadAssetAtPath<GameObject>(def.trailEffectPath);
                if (fx != null)
                    so.FindProperty("trailEffectPrefab").objectReferenceValue = fx;
                else
                    Debug.LogWarning($"[KatanaFactory] Trail effect not found at '{def.trailEffectPath}' for {def.name}");
            }

            if (!string.IsNullOrEmpty(def.abilityPath))
            {
                var ability = AssetDatabase.LoadAssetAtPath<KatanaAbility>(def.abilityPath);
                if (ability != null)
                    so.FindProperty("ability").objectReferenceValue = ability;
                else
                    Debug.LogWarning($"[KatanaFactory] Ability not found at '{def.abilityPath}' for {def.name}");
            }

            if (def.challenge != null && rarity == KatanaRarity.Challenge)
            {
                var challengeProp = so.FindProperty("challengeRequirement");
                challengeProp.FindPropertyRelative("type").enumValueIndex = def.challenge.type;
                challengeProp.FindPropertyRelative("targetValue").floatValue = def.challenge.targetValue;
                challengeProp.FindPropertyRelative("descriptionKey").stringValue = def.challenge.descriptionKey ?? "";
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(katana);
            return katana;
        }

        private void RegisterInDatabase(Katana katana)
        {
            if (_katanaDatabase == null) return;

            var so = new SerializedObject(_katanaDatabase);
            var list = so.FindProperty("katanas");
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = katana;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_katanaDatabase);
        }

        private void AddLocalizationEntries(KatanaDefinition def)
        {
            if (_localizationConfig == null || string.IsNullOrEmpty(_localizationConfig.jsonFolderPath)) return;

            string basePath = LocalizationDatabase.GetLocalizationBasePath(_localizationConfig.jsonFolderPath);
            if (!Directory.Exists(basePath)) return;

            string nameKey = $"katana_name_{def.name.ToLowerInvariant()}";
            string descKey = $"katana_description_{def.name.ToLowerInvariant()}";

            foreach (string langCode in new[] { "en", "ru" })
            {
                string path = Path.Combine(basePath, $"{langCode}.json");
                var dict = LoadTranslationDict(path);
                bool changed = false;
                if (!dict.ContainsKey(nameKey))
                {
                    dict[nameKey] = langCode == "en" ? (def.name_en ?? def.name) : (def.name_ru ?? def.name);
                    changed = true;
                }
                if (!dict.ContainsKey(descKey))
                {
                    dict[descKey] = langCode == "en" ? (def.description_en ?? "-") : (def.description_ru ?? "-");
                    changed = true;
                }
                if (def.challenge != null && !string.IsNullOrEmpty(def.challenge.descriptionKey) && !dict.ContainsKey(def.challenge.descriptionKey))
                {
                    dict[def.challenge.descriptionKey] = langCode == "en" ? (def.challenge.description_en ?? "") : (def.challenge.description_ru ?? "");
                    changed = true;
                }
                if (changed)
                    File.WriteAllText(path, LocalizationDatabase.SerializeTranslations(dict));
            }

            AssetDatabase.Refresh();
        }

        private static Dictionary<string, string> LoadTranslationDict(string path)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!File.Exists(path)) return dict;
            try
            {
                string json = File.ReadAllText(path);
                var wrapped = json.TrimStart().StartsWith("{") && json.Contains("\"entries\"") ? json : "{\"entries\":[]}";
                var sd = JsonUtility.FromJson<StringDictWrapper>(wrapped);
                if (sd?.entries != null)
                    foreach (var e in sd.entries)
                        if (e != null && !string.IsNullOrEmpty(e.key))
                            dict[e.key] = e.value ?? "";
            }
            catch { }
            return dict;
        }

        [Serializable]
        private class StringDictWrapper { public List<KeyVal> entries; }
        [Serializable]
        private class KeyVal { public string key; public string value; }

        #region Helpers

        private static KatanaRarity ParseRarity(string rarity)
        {
            if (Enum.TryParse<KatanaRarity>(rarity, true, out var result))
                return result;
            Debug.LogWarning($"[KatanaFactory] Unknown rarity '{rarity}', defaulting to Common");
            return KatanaRarity.Common;
        }

        private static string GetRarityFolder(KatanaRarity rarity) =>
            $"{KatanaBasePath}/{rarity}";

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            var parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void SetFloat(SerializedObject so, string name, float value)
        {
            var prop = so.FindProperty(name);
            if (prop != null) prop.floatValue = value;
        }

        private static void SetInt(SerializedObject so, string name, int value)
        {
            var prop = so.FindProperty(name);
            if (prop != null) prop.intValue = value;
        }

        private static void SetBool(SerializedObject so, string name, bool value)
        {
            var prop = so.FindProperty(name);
            if (prop != null) prop.boolValue = value;
        }

        #endregion
    }

    #region JSON Data Classes

    [Serializable]
    public class KatanaDefinition
    {
        public string name;
        public string rarity;
        public string name_en;
        public string name_ru;
        public string description_en;
        public string description_ru;
        public PresetDefinition preset;
        public string iconPath;
        public string modelPrefabPath;
        public string bladeMaterialPath;
        public string slashEffectPath;
        public string trailEffectPath;
        public string abilityPath;
        public ChallengeDefinition challenge;
    }

    [Serializable]
    public class PresetDefinition
    {
        public float baseSpeed = 20;
        public float maxSpeed = 40;
        public float speedAcceleration = 0.1f;
        public float laneSwitchSpeed = 10;
        public float jumpForce = 24;
        public int maxJumps;
        public float gravity = -50;
        public float jumpDuration;
        public float jumpHeight;
        public int maxDashes = 3;
        public float dashDuration = 0.4f;
        public float dashSpeedMultiplier = 3;
        public float dashRegenTime = 1;
        public float dashRegenDelay = 1;
        public bool dashInvincible = true;
        public float dashDamage = 1;
    }

    [Serializable]
    public class ChallengeDefinition
    {
        public int type;
        public float targetValue;
        public string descriptionKey;
        public string description_en;
        public string description_ru;
    }

    public static class JsonHelper
    {
        [Serializable]
        private class Wrapper<T> { public T[] items; }

        public static List<T> FromJsonArray<T>(string json)
        {
            string wrapped = $"{{\"items\":{json}}}";
            var wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return new List<T>(wrapper.items);
        }
    }

    #endregion
}
