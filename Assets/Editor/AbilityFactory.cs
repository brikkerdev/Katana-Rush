using System;
using System.Collections.Generic;
using System.IO;
using Runner.Inventory;
using Runner.Inventory.Abilities;
using UnityEditor;
using UnityEngine;

namespace Runner.Editor
{
    public class AbilityFactory : EditorWindow
    {
        private const string JsonPath = "Assets/Scriptables/Abilities/ability_definitions.json";
        private const string AbilityBasePath = "Assets/Scriptables/Abilities";

        private Vector2 _scroll;
        private List<AbilityDefinition> _definitions;
        private string _statusMessage;
        private MessageType _statusType;

        [MenuItem("Tools/Ability Factory/Import from JSON")]
        public static void ShowWindow()
        {
            GetWindow<AbilityFactory>("Ability Factory");
        }

        private void OnEnable()
        {
            LoadDefinitions();
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
                _definitions = JsonHelper.FromJsonArray<AbilityDefinition>(json);
                _statusMessage = $"Loaded {_definitions.Count} ability definition(s)";
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
            EditorGUILayout.LabelField("Ability Factory", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Reads ability_definitions.json and creates Ability ScriptableObject assets, " +
                "then assigns them to the specified Katana assets.",
                MessageType.Info);

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
            foreach (var def in _definitions)
            {
                EditorGUILayout.BeginVertical("box");
                string exists = AssetExists(def.assetName) ? " [EXISTS]" : "";
                EditorGUILayout.LabelField($"{def.assetName}  ({def.abilityType}){exists}", EditorStyles.miniLabel);

                if (def.assignToKatanas != null && def.assignToKatanas.Length > 0)
                    EditorGUILayout.LabelField("Assign to", string.Join(", ", def.assignToKatanas));

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Create Abilities", GUILayout.Height(32)))
                CreateAllAbilities();

            if (GUILayout.Button("Assign to Katanas", GUILayout.Height(32)))
                AssignAllToKatanas();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            if (GUILayout.Button("Create & Assign All", GUILayout.Height(36)))
            {
                CreateAllAbilities();
                AssignAllToKatanas();
            }
        }

        private bool AssetExists(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<KatanaAbility>($"{AbilityBasePath}/{assetName}.asset") != null;
        }

        private void CreateAllAbilities()
        {
            int created = 0, skipped = 0;

            EnsureFolder(AbilityBasePath);

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var def in _definitions)
                {
                    if (string.IsNullOrWhiteSpace(def.assetName) || string.IsNullOrWhiteSpace(def.abilityType))
                    {
                        Debug.LogWarning($"[AbilityFactory] Skipping invalid definition");
                        skipped++;
                        continue;
                    }

                    string assetPath = $"{AbilityBasePath}/{def.assetName}.asset";
                    if (AssetDatabase.LoadAssetAtPath<KatanaAbility>(assetPath) != null)
                    {
                        Debug.Log($"[AbilityFactory] '{def.assetName}' already exists, skipping creation");
                        skipped++;
                        continue;
                    }

                    var ability = CreateAbilityAsset(def);
                    if (ability != null)
                        created++;
                    else
                        skipped++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            _statusMessage = $"Created {created} ability asset(s), skipped {skipped}";
            _statusType = MessageType.Info;
            Debug.Log($"[AbilityFactory] {_statusMessage}");
        }

        private KatanaAbility CreateAbilityAsset(AbilityDefinition def)
        {
            KatanaAbility ability = InstantiateByType(def.abilityType);
            if (ability == null)
            {
                Debug.LogError($"[AbilityFactory] Unknown ability type: {def.abilityType}");
                return null;
            }

            string assetPath = $"{AbilityBasePath}/{def.assetName}.asset";
            AssetDatabase.CreateAsset(ability, assetPath);

            if (def.fields != null)
            {
                var so = new SerializedObject(ability);
                ApplyFields(so, def.fields);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(ability);
            Debug.Log($"[AbilityFactory] Created '{def.assetName}' ({def.abilityType})");
            return ability;
        }

        private static KatanaAbility InstantiateByType(string typeName)
        {
            switch (typeName)
            {
                case "DashDamageBoostAbility": return ScriptableObject.CreateInstance<DashDamageBoostAbility>();
                case "SlowFallAbility": return ScriptableObject.CreateInstance<SlowFallAbility>();
                case "ScoreMultiplierAbility": return ScriptableObject.CreateInstance<ScoreMultiplierAbility>();
                case "DashChainAbility": return ScriptableObject.CreateInstance<DashChainAbility>();
                case "ShieldAbility": return ScriptableObject.CreateInstance<ShieldAbility>();
                case "SpeedSurgeAbility": return ScriptableObject.CreateInstance<SpeedSurgeAbility>();
                case "LaneSwitchDashAbility": return ScriptableObject.CreateInstance<LaneSwitchDashAbility>();
                case "ExplosiveDashAbility": return ScriptableObject.CreateInstance<ExplosiveDashAbility>();
                case "CoinStreakAbility": return ScriptableObject.CreateInstance<CoinStreakAbility>();
                case "GhostStepAbility": return ScriptableObject.CreateInstance<GhostStepAbility>();
                case "DashResetOnKillAbility": return ScriptableObject.CreateInstance<DashResetOnKillAbility>();
                case "ExtendedDashAbility": return ScriptableObject.CreateInstance<ExtendedDashAbility>();
                case "DoubleCoinAbility": return ScriptableObject.CreateInstance<DoubleCoinAbility>();
                case "MagnetAuraAbility": return ScriptableObject.CreateInstance<MagnetAuraAbility>();
                case "TargetedDeflectAbility": return ScriptableObject.CreateInstance<TargetedDeflectAbility>();
                case "EnemyKillRewardAbility": return ScriptableObject.CreateInstance<EnemyKillRewardAbility>();
                default: return null;
            }
        }

        private static void ApplyFields(SerializedObject so, AbilityFields fields)
        {
            TrySetFloat(so, "damageMultiplier", fields.damageMultiplier);
            TrySetFloat(so, "gravityReduction", fields.gravityReduction);
            TrySetFloat(so, "invincibilityExtension", fields.invincibilityExtension);
            TrySetFloat(so, "scoreMultiplier", fields.scoreMultiplier);
            TrySetFloat(so, "invincibilityOnLand", fields.invincibilityOnLand);
            TrySetFloat(so, "explosionRadius", fields.explosionRadius);
            TrySetFloat(so, "explosionDamage", fields.explosionDamage);
            TrySetFloat(so, "speedBoost", fields.speedBoost);
            TrySetFloat(so, "surgeDuration", fields.surgeDuration);
            TrySetFloat(so, "invincibilityDuration", fields.invincibilityDuration);
            TrySetFloat(so, "laneSwitchDamage", fields.laneSwitchDamage);
            TrySetFloat(so, "chainWindow", fields.chainWindow);
            TrySetFloat(so, "chainSpeedBonus", fields.chainSpeedBonus);
            TrySetFloat(so, "shieldCooldown", fields.shieldCooldown);
            TrySetFloat(so, "streakWindow", fields.streakWindow);
            TrySetFloat(so, "magnetRadius", fields.magnetRadius);
            TrySetFloat(so, "targetChance", fields.targetChance);
            TrySetFloat(so, "deflectSpeed", fields.deflectSpeed);
            TrySetFloat(so, "searchRadius", fields.searchRadius);

            TrySetInt(so, "shieldHits", fields.shieldHits);
            TrySetInt(so, "maxChainStacks", fields.maxChainStacks);
            TrySetInt(so, "maxStreakMultiplier", fields.maxStreakMultiplier);
            TrySetInt(so, "coinMultiplier", fields.coinMultiplier);
            TrySetInt(so, "dashesRestoredPerKill", fields.dashesRestoredPerKill);
            TrySetInt(so, "coinsPerKill", fields.coinsPerKill);
            TrySetInt(so, "scorePerKill", fields.scorePerKill);

            TrySetBool(so, "canDamageEnemies", fields.canDamageEnemies);
        }

        private void AssignAllToKatanas()
        {
            int assigned = 0, failed = 0;

            foreach (var def in _definitions)
            {
                if (def.assignToKatanas == null || def.assignToKatanas.Length == 0)
                    continue;

                string abilityPath = $"{AbilityBasePath}/{def.assetName}.asset";
                var ability = AssetDatabase.LoadAssetAtPath<KatanaAbility>(abilityPath);
                if (ability == null)
                {
                    Debug.LogWarning($"[AbilityFactory] Ability asset not found: {abilityPath}");
                    failed += def.assignToKatanas.Length;
                    continue;
                }

                foreach (string katanaName in def.assignToKatanas)
                {
                    if (TryAssignToKatana(katanaName, ability))
                        assigned++;
                    else
                        failed++;
                }
            }

            AssetDatabase.SaveAssets();

            string msg = $"Assigned {assigned} ability ref(s)";
            if (failed > 0) msg += $", {failed} failed";
            _statusMessage = msg;
            _statusType = failed > 0 ? MessageType.Warning : MessageType.Info;
            Debug.Log($"[AbilityFactory] {_statusMessage}");
        }

        private static bool TryAssignToKatana(string katanaName, KatanaAbility ability)
        {
            string[] searchFolders = { "Assets/Scriptables/Katanas" };
            string[] guids = AssetDatabase.FindAssets($"{katanaName}_Katana t:Katana", searchFolders);

            if (guids.Length == 0)
            {
                Debug.LogWarning($"[AbilityFactory] Katana '{katanaName}' not found");
                return false;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var katana = AssetDatabase.LoadAssetAtPath<Katana>(path);
                if (katana == null) continue;

                if (!katana.name.StartsWith(katanaName + "_Katana") && katana.name != katanaName + "_Katana")
                    continue;

                var so = new SerializedObject(katana);
                so.FindProperty("ability").objectReferenceValue = ability;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(katana);
                Debug.Log($"[AbilityFactory] Assigned '{ability.name}' to katana '{katana.name}'");
                return true;
            }

            Debug.LogWarning($"[AbilityFactory] Could not match katana '{katanaName}'");
            return false;
        }

        #region Helpers

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

        private static void TrySetFloat(SerializedObject so, string name, float value)
        {
            if (value == 0f) return;
            var prop = so.FindProperty(name);
            if (prop != null) prop.floatValue = value;
        }

        private static void TrySetInt(SerializedObject so, string name, int value)
        {
            if (value == 0) return;
            var prop = so.FindProperty(name);
            if (prop != null) prop.intValue = value;
        }

        private static void TrySetBool(SerializedObject so, string name, int value)
        {
            if (value == 0) return;
            var prop = so.FindProperty(name);
            if (prop != null) prop.boolValue = value > 0;
        }

        #endregion
    }

    #region JSON Data Classes

    [Serializable]
    public class AbilityDefinition
    {
        public string assetName;
        public string abilityType;
        public AbilityFields fields;
        public string[] assignToKatanas;
    }

    [Serializable]
    public class AbilityFields
    {
        public float damageMultiplier;
        public float gravityReduction;
        public float invincibilityExtension;
        public float scoreMultiplier;
        public float invincibilityOnLand;
        public float explosionRadius;
        public float explosionDamage;
        public float speedBoost;
        public float surgeDuration;
        public float invincibilityDuration;
        public float laneSwitchDamage;
        public float chainWindow;
        public float chainSpeedBonus;
        public float shieldCooldown;
        public float streakWindow;
        public float magnetRadius;
        public float targetChance;
        public float deflectSpeed;
        public float searchRadius;

        public int shieldHits;
        public int maxChainStacks;
        public int maxStreakMultiplier;
        public int coinMultiplier;
        public int dashesRestoredPerKill;
        public int coinsPerKill;
        public int scorePerKill;

        public int canDamageEnemies;
    }

    #endregion
}
