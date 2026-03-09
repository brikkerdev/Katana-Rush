using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Runner.LevelGeneration;
using Runner.Enemy;
using Runner.Collectibles;
using Runner.Environment;

public class LevelSegmentFactory : EditorWindow
{
    private string _biomeName = "Desert";
    private string _levelName = "Level 1";
    private string _savePath = "Assets/Prefabs/Levels";

    private float _segmentLength = 100f;
    private float _difficultyWeight = 0f;
    private bool _allowConsecutive = true;

    private float _revealDuration = 0.6f;
    private float _dropDistance = 8f;
    private float _overshoot = 1.2f;
    private bool _staggerChildren = false;
    private float _staggerDelay = 0.05f;
    private int _maxStaggeredChildren = 10;

    private Material _groundMaterial;
    private Vector3 _planePosition = new Vector3(0, 0, 50);
    private Vector3 _planeScale = new Vector3(1.5f, 1, 10);
    private Vector3 _backgroundPosition = new Vector3(0, 0, 10);

    private List<SpawnPointDef> _enemySpawns = new List<SpawnPointDef>();
    private List<CollectibleSpawnDef> _collectibleSpawns = new List<CollectibleSpawnDef>();
    private List<SpawnPointDef> _obstacleSpawns = new List<SpawnPointDef>();

    private Vector2 _scrollPos;
    private bool _foldSegment = true;
    private bool _foldReveal = true;
    private bool _foldGround = true;
    private bool _foldEnemies = true;
    private bool _foldCollectibles = true;
    private bool _foldObstacles = true;

    private GameObject _templatePrefab;
    private bool _useTemplate = false;

    private BiomeData _targetBiome;
    private bool _addToBiome = false;
    private bool _isStartNode = true;
    private float _nodeWeight = 1f;
    private int _nodeCooldown = 0;

    // ─── Valid placement constants ───
    public static readonly float[] ValidLaneX = { -5f, 0f, 5f };
    public static readonly float[] ValidHeightY = { -17.5f, -15f, 2.5f, 5f, 7.5f, 12.5f, 17.5f, 22.5f };
    public const float MinSpawnZ = 10f;
    public const float MaxSpawnZ = 97.5f;
    public const float MinZSpacing = 5f;
    public const float SegmentLength = 100f;

    public const string CubePrefabPath = "Assets/Models/Prototyping/cube.prefab";
    public const string SlopePrefabPath = "Assets/Models/Prototyping/slope.prefab";

    // ─── Biome template config for headless creation ───
    public static readonly Dictionary<string, BiomeConfig> BiomeConfigs = new Dictionary<string, BiomeConfig>
    {
        ["Desert"] = new BiomeConfig
        {
            templatePath = "Assets/Prefabs/Levels/Desert/Level 1.prefab",
            biomePath = "Assets/Prefabs/Levels/Desert/Desert.asset",
            validEnemyY = new float[] { 2.5f, 7.5f },
            validCollectibleY = new float[] { 2.5f, 7.5f, 12.5f, 17.5f },
            defaultPoolWeight = 2f,
            defaultPoolCooldown = 3,
        },
        ["Waves"] = new BiomeConfig
        {
            templatePath = "Assets/Prefabs/Levels/Waves/Level 1.prefab",
            biomePath = "Assets/Prefabs/Levels/Waves/Waves.asset",
            validEnemyY = new float[] { 2.5f, 17.5f },
            validCollectibleY = new float[] { 2.5f, 7.5f, 12.5f, 17.5f, 22.5f },
            defaultPoolWeight = 2f,
            defaultPoolCooldown = 3,
        },
        ["SnowBlock"] = new BiomeConfig
        {
            templatePath = "Assets/Prefabs/Levels/SnowBlock/Level 1.prefab",
            biomePath = "Assets/Prefabs/Levels/SnowBlock/SnowBlock.asset",
            validEnemyY = new float[] { 2.5f, 7.5f },
            validCollectibleY = new float[] { -17.5f, -15f, 2.5f, 7.5f },
            defaultPoolWeight = 2f,
            defaultPoolCooldown = 2,
        },
        ["Roofs"] = new BiomeConfig
        {
            templatePath = "Assets/Prefabs/Levels/Roofs/Level 1.prefab",
            biomePath = "Assets/Prefabs/Levels/Roofs/Roofs.asset",
            validEnemyY = new float[] { 2.5f, 7.5f },
            validCollectibleY = new float[] { 2.5f, 7.5f },
            defaultPoolWeight = 1f,
            defaultPoolCooldown = 0,
        },
        ["Mountains"] = new BiomeConfig
        {
            templatePath = "Assets/Prefabs/Levels/Mountains/Level 1.prefab",
            biomePath = "Assets/Prefabs/Levels/Mountains/Mountains.asset",
            validEnemyY = new float[] { 7.5f, 12.5f },
            validCollectibleY = new float[] { 2.5f, 5f, 7.5f, 12.5f },
            defaultPoolWeight = 1f,
            defaultPoolCooldown = 0,
        },
        ["Metro"] = new BiomeConfig
        {
            templatePath = "Assets/Prefabs/Levels/Metro/Level 1.prefab",
            biomePath = "Assets/Prefabs/Levels/Metro/Metro.asset",
            validEnemyY = new float[] { 2.5f, 7.5f },
            validCollectibleY = new float[] { 2.5f, 7.5f, 12.5f },
            defaultPoolWeight = 1f,
            defaultPoolCooldown = 0,
        },
    };

    [MenuItem("Tools/Level Segment Factory")]
    public static void ShowWindow()
    {
        var window = GetWindow<LevelSegmentFactory>("Level Segment Factory");
        window.minSize = new Vector2(420, 600);
    }

    // ══════════════════════════════════════════════════════════
    //  HEADLESS PUBLIC API — for AI agents and automation
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a level prefab by cloning a template, replacing spawn points,
    /// validating, saving, and optionally registering in a BiomeData asset.
    /// Returns the saved prefab path, or null on failure.
    /// </summary>
    public static string CreateLevel(LevelDefinition def)
    {
        var errors = Validate(def);
        if (errors.Count > 0)
        {
            foreach (var e in errors)
                Debug.LogError($"[LevelSegmentFactory] Validation: {e}");
            return null;
        }

        string templatePath = def.templatePrefabPath;
        if (string.IsNullOrEmpty(templatePath) && BiomeConfigs.TryGetValue(def.biomeName, out var cfg))
            templatePath = cfg.templatePath;

        if (string.IsNullOrEmpty(templatePath) || AssetDatabase.LoadAssetAtPath<GameObject>(templatePath) == null)
        {
            Debug.LogError($"[LevelSegmentFactory] Template not found: {templatePath}");
            return null;
        }

        string savePath = def.savePath ?? "Assets/Prefabs/Levels";
        string folderPath = $"{savePath}/{def.biomeName}";
        EnsureFolder(folderPath);

        string prefabPath = $"{folderPath}/{def.levelName}.prefab";

        var templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
        var root = (GameObject)PrefabUtility.InstantiatePrefab(templatePrefab);
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        root.name = def.levelName;

        var spawnpointsTf = root.transform.Find("Spawnpoints");
        var collectiblesTf = root.transform.Find("Collectibles");

        ClearChildren(spawnpointsTf);
        ClearChildren(collectiblesTf);

        foreach (var esp in def.enemySpawns)
        {
            string spName = esp.enemyType == EnemyType.RocketLauncher
                ? $"SpawnpointRocket ({spawnpointsTf.childCount})"
                : $"Spawnpoint ({spawnpointsTf.childCount})";
            CreateEnemySpawn(spawnpointsTf, spName, esp.position, esp.spawnChance,
                esp.alwaysSpawn, esp.enemyType);
        }

        foreach (var osp in def.obstacleSpawns)
        {
            CreateObstacleSpawn(spawnpointsTf,
                $"ObstacleSpawnpoint ({spawnpointsTf.childCount})",
                osp.position, osp.spawnChance, osp.alwaysSpawn);
        }

        int coinIdx = 0, coinLineIdx = 0, magnetIdx = 0, x2Idx = 0, diamondIdx = 0, fragmentIdx = 0;
        foreach (var csp in def.collectibleSpawns)
        {
            string spName = GetCollectibleSpawnName(csp.collectibleType,
                ref coinIdx, ref coinLineIdx, ref magnetIdx,
                ref x2Idx, ref diamondIdx, ref fragmentIdx);
            CreateCollectibleSpawn(collectiblesTf, spName, csp.position, csp.spawnChance,
                csp.alwaysSpawn, csp.collectibleType, csp.groupCount, csp.groupSpacing, csp.groupPattern);
        }

        if (def.geometry != null && def.geometry.Count > 0)
            PlaceGeometry(root, def.geometry);

        var segment = root.GetComponent<LevelSegment>();
        if (segment == null) segment = root.AddComponent<LevelSegment>();
        var reveal = root.GetComponent<SegmentReveal>();
        if (reveal == null) reveal = root.AddComponent<SegmentReveal>();

        ApplySegmentFields(segment, def.segmentLength, def.difficultyWeight, def.allowConsecutive);
        var envTransform = root.transform.Find("Environment");
        if (envTransform != null)
            ApplyRevealFields(reveal, envTransform);

        segment.CollectSpawnPoints();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();

        var savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (savedPrefab == null)
        {
            Debug.LogError($"[LevelSegmentFactory] Failed to save prefab at {prefabPath}");
            return null;
        }

        if (def.registerInBiome)
        {
            string biomePath = def.biomeAssetPath;
            if (string.IsNullOrEmpty(biomePath) && BiomeConfigs.TryGetValue(def.biomeName, out var bc))
                biomePath = bc.biomePath;

            var biome = AssetDatabase.LoadAssetAtPath<BiomeData>(biomePath);
            if (biome != null)
            {
                var seg = savedPrefab.GetComponent<LevelSegment>();
                if (seg != null)
                    RegisterInBiome(biome, seg, def.isStartNode, def.nodeWeight, def.nodeCooldown);
            }
            else
            {
                Debug.LogWarning($"[LevelSegmentFactory] BiomeData not found at {biomePath}, skipping registration");
            }
        }

        Debug.Log($"[LevelSegmentFactory] Created prefab: {prefabPath}");
        return prefabPath;
    }

    /// <summary>
    /// Validates a LevelDefinition against the design rules.
    /// Returns an empty list if everything is valid.
    /// </summary>
    public static List<string> Validate(LevelDefinition def)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(def.biomeName))
            errors.Add("biomeName is required");
        if (string.IsNullOrEmpty(def.levelName))
            errors.Add("levelName is required");

        if (def.segmentLength != SegmentLength)
            errors.Add($"segmentLength must be {SegmentLength}, got {def.segmentLength}");

        if (def.difficultyWeight < 0f || def.difficultyWeight > 1f)
            errors.Add($"difficultyWeight must be 0-1, got {def.difficultyWeight}");

        bool isHard = def.difficultyWeight >= 0.5f;
        int enemyCount = def.enemySpawns != null ? def.enemySpawns.Count : 0;
        int collectibleCount = def.collectibleSpawns != null ? def.collectibleSpawns.Count : 0;

        if (isHard)
        {
            if (enemyCount < 2 || enemyCount > 3)
                errors.Add($"Hard levels need 2-3 enemies, got {enemyCount}");
        }
        else
        {
            if (enemyCount < 2 || enemyCount > 4)
                errors.Add($"Easy levels need 2-4 enemies, got {enemyCount}");
        }

        if (collectibleCount < 1)
            errors.Add("At least one collectible is required");

        var allSpawns = new List<Vector3>();

        if (def.enemySpawns != null)
        {
            foreach (var esp in def.enemySpawns)
            {
                ValidateSpawnPosition(esp.position, "Enemy", errors);
                allSpawns.Add(esp.position);

                if (isHard && esp.enemyType == EnemyType.Static)
                    errors.Add($"Hard level (diff >= 0.5) should use RocketLauncher, not Static at Z={esp.position.z}");
                if (!isHard && esp.enemyType == EnemyType.RocketLauncher)
                    errors.Add($"Easy level (diff < 0.5) should use Static, not RocketLauncher at Z={esp.position.z}");
            }
        }

        if (def.collectibleSpawns != null)
        {
            foreach (var csp in def.collectibleSpawns)
            {
                ValidateSpawnPosition(csp.position, "Collectible", errors);
                allSpawns.Add(csp.position);
            }
        }

        ValidateZSpacing(allSpawns, errors);

        bool hasFirst = false, hasLast = false;
        foreach (var sp in allSpawns)
        {
            if (sp.z < 33f) hasFirst = true;
            if (sp.z > 66f) hasLast = true;
        }
        if (!hasFirst)
            errors.Add("Need at least one spawn in first third (Z < 33)");
        if (!hasLast)
            errors.Add("Need at least one spawn in last third (Z > 66)");

        return errors;
    }

    static void ValidateSpawnPosition(Vector3 pos, string label, List<string> errors)
    {
        bool validX = false;
        foreach (float x in ValidLaneX)
            if (Mathf.Approximately(pos.x, x)) { validX = true; break; }
        if (!validX)
            errors.Add($"{label} spawn X={pos.x} must be -5, 0, or 5");

        bool validY = false;
        foreach (float y in ValidHeightY)
            if (Mathf.Approximately(pos.y, y)) { validY = true; break; }
        if (!validY)
            errors.Add($"{label} spawn Y={pos.y} is not a valid height tier");

        if (pos.z < MinSpawnZ || pos.z > MaxSpawnZ)
            errors.Add($"{label} spawn Z={pos.z} out of range [{MinSpawnZ}, {MaxSpawnZ}]");
    }

    static void ValidateZSpacing(List<Vector3> spawns, List<string> errors)
    {
        for (int i = 0; i < spawns.Count; i++)
        {
            for (int j = i + 1; j < spawns.Count; j++)
            {
                if (!Mathf.Approximately(spawns[i].x, spawns[j].x)) continue;
                float dz = Mathf.Abs(spawns[i].z - spawns[j].z);
                if (dz > 0f && dz < MinZSpacing)
                    errors.Add($"Spawns at Z={spawns[i].z} and Z={spawns[j].z} on lane X={spawns[i].x} " +
                               $"are only {dz:F1} apart (min {MinZSpacing})");
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  PUBLIC STATIC HELPERS — for external scripts and agents
    // ══════════════════════════════════════════════════════════

    public static void RegisterInBiome(BiomeData biome, LevelSegment segment,
        bool isStartNode = true, float weight = 1f, int cooldown = 0)
    {
        var so = new SerializedObject(biome);
        var nodesProp = so.FindProperty("segmentNodes");

        int newIndex = nodesProp.arraySize;
        nodesProp.InsertArrayElementAtIndex(newIndex);
        var newNode = nodesProp.GetArrayElementAtIndex(newIndex);

        newNode.FindPropertyRelative("nodeIndex").intValue = newIndex;
        newNode.FindPropertyRelative("segment").objectReferenceValue = segment;
        newNode.FindPropertyRelative("nodeName").stringValue = $"Segment {newIndex}";
        newNode.FindPropertyRelative("nodePosition").vector2Value =
            new Vector2(100 + (newIndex % 4) * 280, 100 + (newIndex / 4) * 180);
        newNode.FindPropertyRelative("isStartNode").boolValue = isStartNode;
        newNode.FindPropertyRelative("isEndNode").boolValue = false;
        newNode.FindPropertyRelative("weight").floatValue = weight;
        newNode.FindPropertyRelative("cooldown").intValue = cooldown;

        var conns = newNode.FindPropertyRelative("connections");
        conns.ClearArray();

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(biome);
        AssetDatabase.SaveAssets();

        Debug.Log($"[LevelSegmentFactory] Registered '{segment.name}' as node {newIndex} in '{biome.BiomeName}'");
    }

    public static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    static void ClearCubeSlopeChildren(Transform levelTf)
    {
        if (levelTf == null) return;
        for (int i = levelTf.childCount - 1; i >= 0; i--)
        {
            var child = levelTf.GetChild(i);
            var name = child.name.ToLowerInvariant();
            if (name.StartsWith("cube") || name.StartsWith("slope"))
                Object.DestroyImmediate(child.gameObject);
        }
    }

    public static void CreateEnemySpawn(Transform parent, string name, Vector3 pos,
        float chance, bool alwaysSpawn, EnemyType type)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var comp = go.AddComponent<EnemySpawnPoint>();
        var so = new SerializedObject(comp);
        so.FindProperty("alwaysSpawn").boolValue = alwaysSpawn;
        so.FindProperty("spawnChance").floatValue = chance;
        so.FindProperty("allowedType").enumValueIndex = (int)type;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void CreateCollectibleSpawn(Transform parent, string name, Vector3 pos,
        float chance, bool alwaysSpawn, CollectibleType type,
        int groupCount = 5, float groupSpacing = 1.5f, GroupPattern groupPattern = GroupPattern.Line)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var comp = go.AddComponent<CollectibleSpawnPoint>();
        var so = new SerializedObject(comp);
        so.FindProperty("alwaysSpawn").boolValue = alwaysSpawn;
        so.FindProperty("spawnChance").floatValue = chance;
        so.FindProperty("collectibleType").enumValueIndex = (int)type;
        so.FindProperty("groupCount").intValue = groupCount;
        so.FindProperty("groupSpacing").floatValue = groupSpacing;
        so.FindProperty("groupPattern").enumValueIndex = (int)groupPattern;
        so.FindProperty("spawnPointUuid").stringValue = System.Guid.NewGuid().ToString();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void CreateObstacleSpawn(Transform parent, string name, Vector3 pos,
        float chance, bool alwaysSpawn)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var comp = go.AddComponent<ObstacleSpawnPoint>();
        var so = new SerializedObject(comp);
        so.FindProperty("alwaysSpawn").boolValue = alwaysSpawn;
        so.FindProperty("spawnChance").floatValue = chance;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void ApplySegmentFields(LevelSegment segment, float length, float difficulty, bool allowConsecutive)
    {
        var so = new SerializedObject(segment);
        so.FindProperty("segmentLength").floatValue = length;
        so.FindProperty("difficultyWeight").floatValue = difficulty;
        so.FindProperty("allowConsecutive").boolValue = allowConsecutive;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void ApplyRevealFields(SegmentReveal reveal, Transform visualRoot,
        float duration = 0.6f, float dropDist = 8f, float overshoot = 1.2f, bool stagger = false)
    {
        var so = new SerializedObject(reveal);
        so.FindProperty("revealDuration").floatValue = duration;
        so.FindProperty("dropDistance").floatValue = dropDist;
        so.FindProperty("overshoot").floatValue = overshoot;
        so.FindProperty("staggerChildren").boolValue = stagger;
        so.FindProperty("staggerDelay").floatValue = 0.05f;
        so.FindProperty("maxStaggeredChildren").intValue = 10;
        so.FindProperty("visualRoot").objectReferenceValue = visualRoot;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void PlaceGeometry(GameObject root, List<LevelDefinition.GeometryDef> geometry)
    {
        var envTf = root.transform.Find("Environment");
        if (envTf == null) return;

        var levelTf = envTf.Find("Level");
        if (levelTf == null)
        {
            var levelGO = new GameObject("Level");
            levelGO.transform.SetParent(envTf, false);
            levelTf = levelGO.transform;
        }
        else
        {
            ClearCubeSlopeChildren(levelTf);
        }

        var cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CubePrefabPath);
        var slopePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlopePrefabPath);

        if (cubePrefab == null)
            Debug.LogWarning($"[LevelSegmentFactory] Cube prefab not found at {CubePrefabPath}");
        if (slopePrefab == null)
            Debug.LogWarning($"[LevelSegmentFactory] Slope prefab not found at {SlopePrefabPath}");

        int cubeIdx = 0, slopeIdx = 0;

        foreach (var geo in geometry)
        {
            GameObject sourcePrefab = geo.type == 1 ? slopePrefab : cubePrefab;
            if (sourcePrefab == null) continue;

            string name = geo.type == 1
                ? $"slope ({++slopeIdx})"
                : $"cube ({++cubeIdx})";

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab, levelTf);
            instance.name = name;
            instance.transform.localPosition = geo.position;
            instance.transform.localEulerAngles = geo.rotation;
            instance.transform.localScale = Vector3.one;
        }

        Debug.Log($"[LevelSegmentFactory] Placed {cubeIdx} cubes and {slopeIdx} slopes");
    }

    public static string GetCollectibleSpawnName(CollectibleType type,
        ref int coinIdx, ref int coinLineIdx, ref int magnetIdx,
        ref int x2Idx, ref int diamondIdx, ref int fragmentIdx)
    {
        switch (type)
        {
            case CollectibleType.CoinGroup:
                return $"CoinLineSpawnpoint ({coinLineIdx++})";
            case CollectibleType.Magnet:
                return magnetIdx++ == 0 ? "magnetSpawnpoint" : $"magnetSpawnpoint ({magnetIdx - 1})";
            case CollectibleType.Multiplier:
                return x2Idx++ == 0 ? "x2Spawnpoint" : $"x2Spawnpoint ({x2Idx - 1})";
            case CollectibleType.Diamond:
                return diamondIdx++ == 0 ? "DiamondSpawnpoint" : $"DiamondSpawnpoint ({diamondIdx - 1})";
            case CollectibleType.Fragment:
                return fragmentIdx++ == 0 ? "FragmentCrystalSpawnpoint" : $"FragmentCrystalSpawnpoint ({fragmentIdx - 1})";
            default:
                return $"CoinSpawnpoint ({coinIdx++})";
        }
    }

    public static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static Mesh GetPlaneMesh()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var mesh = go.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(go);
        return mesh;
    }

    // ══════════════════════════════════════════════════════════
    //  JSON IMPORT
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Loads a LevelDefinition from a JSON file and creates the level.
    /// Returns the saved prefab path, or null on failure.
    /// </summary>
    public static string CreateLevelFromJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"[LevelSegmentFactory] JSON file not found: {jsonPath}");
            return null;
        }

        string json = File.ReadAllText(jsonPath);
        var def = JsonUtility.FromJson<LevelDefinition>(json);
        if (def == null)
        {
            Debug.LogError($"[LevelSegmentFactory] Failed to parse JSON: {jsonPath}");
            return null;
        }

        Debug.Log($"[LevelSegmentFactory] Loaded definition from {jsonPath}: " +
                  $"{def.biomeName}/{def.levelName} ({def.enemySpawns.Count} enemies, " +
                  $"{def.collectibleSpawns.Count} collectibles)");

        return CreateLevel(def);
    }

    /// <summary>
    /// Creates levels from all JSON files in a folder.
    /// Returns the number of levels successfully created.
    /// </summary>
    public static int CreateLevelsFromFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"[LevelSegmentFactory] Folder not found: {folderPath}");
            return 0;
        }

        string[] files = Directory.GetFiles(folderPath, "*.json");
        int created = 0;

        foreach (string file in files)
        {
            string result = CreateLevelFromJson(file);
            if (result != null) created++;
        }

        Debug.Log($"[LevelSegmentFactory] Batch JSON import: {created}/{files.Length} levels created from {folderPath}");
        return created;
    }

    [MenuItem("Tools/Level Segment Factory/Import Level from JSON...")]
    static void ImportLevelFromJsonDialog()
    {
        string path = EditorUtility.OpenFilePanel("Select Level Definition JSON", "Assets", "json");
        if (string.IsNullOrEmpty(path)) return;

        string result = CreateLevelFromJson(path);
        if (result != null)
        {
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(result);
            if (saved != null)
            {
                Selection.activeObject = saved;
                EditorUtility.FocusProjectWindow();
            }
            EditorUtility.DisplayDialog("Success",
                $"Created level prefab:\n{result}", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Failed",
                "Level creation failed. Check Console for details.", "OK");
        }
    }

    [MenuItem("Tools/Level Segment Factory/Import All JSON from Folder...")]
    static void ImportAllJsonFromFolderDialog()
    {
        string folder = EditorUtility.OpenFolderPanel("Select Folder with Level JSONs", "Assets", "");
        if (string.IsNullOrEmpty(folder)) return;

        int count = CreateLevelsFromFolder(folder);
        EditorUtility.DisplayDialog("Batch Import Complete",
            $"Created {count} level(s) from folder.", "OK");
    }

    // ══════════════════════════════════════════════════════════
    //  DATA STRUCTURES
    // ══════════════════════════════════════════════════════════

    [System.Serializable]
    public class LevelDefinition
    {
        public string biomeName;
        public string levelName;
        public string savePath;
        public string templatePrefabPath;
        public string biomeAssetPath;

        public float segmentLength = 100f;
        public float difficultyWeight = 0f;
        public bool allowConsecutive = true;

        public bool registerInBiome = false;
        public bool isStartNode = true;
        public float nodeWeight = 1f;
        public int nodeCooldown = 0;

        public List<EnemySpawnDef> enemySpawns = new List<EnemySpawnDef>();
        public List<CollectibleSpawnDef> collectibleSpawns = new List<CollectibleSpawnDef>();
        public List<ObstacleSpawnDef> obstacleSpawns = new List<ObstacleSpawnDef>();
        public List<GeometryDef> geometry = new List<GeometryDef>();

        [System.Serializable]
        public class GeometryDef
        {
            public int type;          // 0 = cube, 1 = slope
            public Vector3 position;
            public Vector3 rotation;  // euler angles
            // Scale is always (1,1,1)
        }

        [System.Serializable]
        public class EnemySpawnDef
        {
            public Vector3 position;
            public float spawnChance = 0.5f;
            public bool alwaysSpawn = false;
            public EnemyType enemyType = EnemyType.Static;
        }

        [System.Serializable]
        public class CollectibleSpawnDef
        {
            public Vector3 position;
            public float spawnChance = 0.7f;
            public bool alwaysSpawn = false;
            public CollectibleType collectibleType = CollectibleType.Coin;
            public int groupCount = 5;
            public float groupSpacing = 1.5f;
            public GroupPattern groupPattern = GroupPattern.Line;
        }

        [System.Serializable]
        public class ObstacleSpawnDef
        {
            public Vector3 position;
            public float spawnChance = 0.5f;
            public bool alwaysSpawn = false;
        }
    }

    public class BiomeConfig
    {
        public string templatePath;
        public string biomePath;
        public float[] validEnemyY;
        public float[] validCollectibleY;
        public float defaultPoolWeight;
        public int defaultPoolCooldown;
    }

    // ══════════════════════════════════════════════════════════
    //  EDITOR WINDOW (GUI — for manual use)
    // ══════════════════════════════════════════════════════════

    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        DrawHeader();
        EditorGUILayout.Space(8);
        DrawTemplateSection();
        EditorGUILayout.Space(4);
        DrawSegmentSettings();
        EditorGUILayout.Space(4);
        DrawRevealSettings();
        EditorGUILayout.Space(4);
        DrawGroundSettings();
        EditorGUILayout.Space(4);
        DrawEnemySpawns();
        EditorGUILayout.Space(4);
        DrawCollectibleSpawns();
        EditorGUILayout.Space(4);
        DrawObstacleSpawns();
        EditorGUILayout.Space(4);
        DrawBiomeRegistration();
        EditorGUILayout.Space(12);
        DrawCreateButton();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Level Segment Factory", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _biomeName = EditorGUILayout.TextField("Biome Folder", _biomeName);
        _levelName = EditorGUILayout.TextField("Level Name", _levelName);
        _savePath = EditorGUILayout.TextField("Save Root", _savePath);
    }

    private void DrawTemplateSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Template", EditorStyles.boldLabel);

        _useTemplate = EditorGUILayout.Toggle("Clone From Template", _useTemplate);
        if (_useTemplate)
        {
            _templatePrefab = (GameObject)EditorGUILayout.ObjectField(
                "Template Prefab", _templatePrefab, typeof(GameObject), false);

            if (_templatePrefab != null && GUILayout.Button("Load Settings From Template"))
            {
                LoadFromTemplate(_templatePrefab);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSegmentSettings()
    {
        _foldSegment = EditorGUILayout.BeginFoldoutHeaderGroup(_foldSegment, "Segment Settings");
        if (_foldSegment)
        {
            EditorGUI.indentLevel++;
            _segmentLength = EditorGUILayout.FloatField("Segment Length", _segmentLength);
            _difficultyWeight = EditorGUILayout.Slider("Difficulty Weight", _difficultyWeight, 0f, 1f);
            _allowConsecutive = EditorGUILayout.Toggle("Allow Consecutive", _allowConsecutive);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawRevealSettings()
    {
        _foldReveal = EditorGUILayout.BeginFoldoutHeaderGroup(_foldReveal, "Reveal Animation");
        if (_foldReveal)
        {
            EditorGUI.indentLevel++;
            _revealDuration = EditorGUILayout.FloatField("Duration", _revealDuration);
            _dropDistance = EditorGUILayout.FloatField("Drop Distance", _dropDistance);
            _overshoot = EditorGUILayout.FloatField("Overshoot", _overshoot);
            _staggerChildren = EditorGUILayout.Toggle("Stagger Children", _staggerChildren);
            if (_staggerChildren)
            {
                _staggerDelay = EditorGUILayout.FloatField("Stagger Delay", _staggerDelay);
                _maxStaggeredChildren = EditorGUILayout.IntField("Max Staggered", _maxStaggeredChildren);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawGroundSettings()
    {
        _foldGround = EditorGUILayout.BeginFoldoutHeaderGroup(_foldGround, "Ground Plane");
        if (_foldGround)
        {
            EditorGUI.indentLevel++;
            _groundMaterial = (Material)EditorGUILayout.ObjectField(
                "Material", _groundMaterial, typeof(Material), false);
            _planePosition = EditorGUILayout.Vector3Field("Position", _planePosition);
            _planeScale = EditorGUILayout.Vector3Field("Scale", _planeScale);
            _backgroundPosition = EditorGUILayout.Vector3Field("Background Offset", _backgroundPosition);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawEnemySpawns()
    {
        _foldEnemies = EditorGUILayout.BeginFoldoutHeaderGroup(_foldEnemies,
            $"Enemy Spawn Points ({_enemySpawns.Count})");
        if (_foldEnemies)
        {
            DrawSpawnPointList(_enemySpawns, true);
            if (GUILayout.Button("+ Add Enemy Spawn"))
            {
                _enemySpawns.Add(new SpawnPointDef
                {
                    position = new Vector3(0, 7.5f, _segmentLength * 0.5f),
                    spawnChance = 0.5f
                });
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawCollectibleSpawns()
    {
        _foldCollectibles = EditorGUILayout.BeginFoldoutHeaderGroup(_foldCollectibles,
            $"Collectible Spawn Points ({_collectibleSpawns.Count})");
        if (_foldCollectibles)
        {
            for (int i = 0; i < _collectibleSpawns.Count; i++)
            {
                EditorGUILayout.BeginVertical("helpbox");
                var def = _collectibleSpawns[i];

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"#{i}", GUILayout.Width(24));
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    _collectibleSpawns.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                def.position = EditorGUILayout.Vector3Field("Position", def.position);
                def.spawnChance = EditorGUILayout.Slider("Spawn Chance", def.spawnChance, 0f, 1f);
                def.alwaysSpawn = EditorGUILayout.Toggle("Always Spawn", def.alwaysSpawn);
                def.collectibleType = (CollectibleType)EditorGUILayout.EnumPopup("Type", def.collectibleType);

                if (def.collectibleType == CollectibleType.CoinGroup)
                {
                    def.groupCount = EditorGUILayout.IntField("Group Count", def.groupCount);
                    def.groupSpacing = EditorGUILayout.FloatField("Group Spacing", def.groupSpacing);
                    def.groupPattern = (GroupPattern)EditorGUILayout.EnumPopup("Pattern", def.groupPattern);
                }

                _collectibleSpawns[i] = def;
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add Collectible Spawn"))
            {
                _collectibleSpawns.Add(new CollectibleSpawnDef
                {
                    position = new Vector3(0, 2.5f, _segmentLength * 0.5f),
                    spawnChance = 0.7f,
                    collectibleType = CollectibleType.Coin,
                    groupCount = 5,
                    groupSpacing = 1.5f,
                    groupPattern = GroupPattern.Line
                });
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawObstacleSpawns()
    {
        _foldObstacles = EditorGUILayout.BeginFoldoutHeaderGroup(_foldObstacles,
            $"Obstacle Spawn Points ({_obstacleSpawns.Count})");
        if (_foldObstacles)
        {
            DrawSpawnPointList(_obstacleSpawns, false);
            if (GUILayout.Button("+ Add Obstacle Spawn"))
            {
                _obstacleSpawns.Add(new SpawnPointDef
                {
                    position = new Vector3(0, 0, _segmentLength * 0.5f),
                    spawnChance = 0.5f
                });
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawSpawnPointList(List<SpawnPointDef> list, bool showEnemyType)
    {
        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginVertical("helpbox");
            var def = list[i];

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"#{i}", GUILayout.Width(24));
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                list.RemoveAt(i);
                i--;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                continue;
            }
            EditorGUILayout.EndHorizontal();

            def.position = EditorGUILayout.Vector3Field("Position", def.position);
            def.spawnChance = EditorGUILayout.Slider("Spawn Chance", def.spawnChance, 0f, 1f);
            def.alwaysSpawn = EditorGUILayout.Toggle("Always Spawn", def.alwaysSpawn);
            if (showEnemyType)
                def.enemyType = (EnemyType)EditorGUILayout.EnumPopup("Enemy Type", def.enemyType);

            list[i] = def;
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawBiomeRegistration()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Biome Registration", EditorStyles.boldLabel);

        _addToBiome = EditorGUILayout.Toggle("Add to Biome Asset", _addToBiome);
        if (_addToBiome)
        {
            _targetBiome = (BiomeData)EditorGUILayout.ObjectField(
                "Biome Data", _targetBiome, typeof(BiomeData), false);
            _isStartNode = EditorGUILayout.Toggle("Is Start Node", _isStartNode);
            _nodeWeight = EditorGUILayout.FloatField("Weight", _nodeWeight);
            _nodeCooldown = EditorGUILayout.IntField("Cooldown", _nodeCooldown);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCreateButton()
    {
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        string label = _addToBiome && _targetBiome != null
            ? "Create Prefab & Add to Biome"
            : "Create Level Segment Prefab";
        if (GUILayout.Button(label, GUILayout.Height(36)))
        {
            CreatePrefabFromGUI();
        }
        GUI.backgroundColor = Color.white;
    }

    private void CreatePrefabFromGUI()
    {
        string folderPath = $"{_savePath}/{_biomeName}";
        EnsureFolder(folderPath);

        string prefabPath = $"{folderPath}/{_levelName}.prefab";
        if (File.Exists(prefabPath))
        {
            if (!EditorUtility.DisplayDialog("Overwrite?",
                $"'{prefabPath}' already exists. Overwrite?", "Yes", "Cancel"))
                return;
        }

        GameObject root;

        if (_useTemplate && _templatePrefab != null)
        {
            root = (GameObject)PrefabUtility.InstantiatePrefab(_templatePrefab);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = _levelName;
            ApplySettingsToExistingGUI(root);
        }
        else
        {
            root = BuildFromScratchGUI();
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        AssetDatabase.Refresh();

        var savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Selection.activeObject = savedPrefab;
        EditorUtility.FocusProjectWindow();

        if (_addToBiome && _targetBiome != null && savedPrefab != null)
        {
            var segment = savedPrefab.GetComponent<LevelSegment>();
            if (segment != null)
                RegisterInBiome(_targetBiome, segment, _isStartNode, _nodeWeight, _nodeCooldown);
        }

        Debug.Log($"[LevelSegmentFactory] Created prefab: {prefabPath}");
    }

    private GameObject BuildFromScratchGUI()
    {
        var root = new GameObject(_levelName);

        var segment = root.AddComponent<LevelSegment>();
        var reveal = root.AddComponent<SegmentReveal>();

        var spawnpointsGO = CreateChild(root, "Spawnpoints");
        var collectiblesGO = CreateChild(root, "Collectibles");
        CreateChild(root, "Objects");
        var environmentGO = CreateChild(root, "Environment");

        var levelGO = CreateChild(environmentGO, "Level");
        var backgroundGO = CreateChild(environmentGO, "Background");
        backgroundGO.transform.localPosition = _backgroundPosition;

        var planeGO = CreateChild(environmentGO, "Plane");
        planeGO.transform.localPosition = _planePosition;
        planeGO.transform.localScale = _planeScale;
        planeGO.layer = LayerMask.NameToLayer("Ground") >= 0 ? LayerMask.NameToLayer("Ground") : 3;
        planeGO.tag = "Ground";

        var meshFilter = planeGO.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = GetPlaneMesh();

        var meshRenderer = planeGO.AddComponent<MeshRenderer>();
        if (_groundMaterial != null)
            meshRenderer.sharedMaterial = _groundMaterial;

        var meshCollider = planeGO.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.sharedMesh;

        foreach (var def in _enemySpawns)
        {
            var sp = CreateChild(spawnpointsGO, $"Spawnpoint ({spawnpointsGO.transform.childCount})");
            sp.transform.localPosition = def.position;
            var comp = sp.AddComponent<EnemySpawnPoint>();
            ApplySpawnPointFieldsGUI(comp, def);
        }

        foreach (var def in _collectibleSpawns)
        {
            string spName = def.collectibleType == CollectibleType.CoinGroup
                ? "CoinLineSpawnpoint"
                : $"CoinSpawnpoint ({collectiblesGO.transform.childCount})";
            var sp = CreateChild(collectiblesGO, spName);
            sp.transform.localPosition = def.position;
            var comp = sp.AddComponent<CollectibleSpawnPoint>();
            ApplyCollectibleFieldsGUI(comp, def);
        }

        foreach (var def in _obstacleSpawns)
        {
            var sp = CreateChild(spawnpointsGO, $"ObstacleSpawnpoint ({spawnpointsGO.transform.childCount})");
            sp.transform.localPosition = def.position;
            var comp = sp.AddComponent<ObstacleSpawnPoint>();
            ApplyObstacleFieldsGUI(comp, def);
        }

        ApplySegmentFields(segment, _segmentLength, _difficultyWeight, _allowConsecutive);
        ApplyRevealFields(reveal, environmentGO.transform, _revealDuration, _dropDistance, _overshoot, _staggerChildren);

        segment.CollectSpawnPoints();

        return root;
    }

    private void ApplySettingsToExistingGUI(GameObject root)
    {
        var segment = root.GetComponent<LevelSegment>();
        if (segment == null) segment = root.AddComponent<LevelSegment>();

        var reveal = root.GetComponent<SegmentReveal>();
        if (reveal == null) reveal = root.AddComponent<SegmentReveal>();

        var envTransform = root.transform.Find("Environment");

        ApplySegmentFields(segment, _segmentLength, _difficultyWeight, _allowConsecutive);
        if (envTransform != null)
            ApplyRevealFields(reveal, envTransform, _revealDuration, _dropDistance, _overshoot, _staggerChildren);

        segment.CollectSpawnPoints();
    }

    private void ApplySpawnPointFieldsGUI(EnemySpawnPoint comp, SpawnPointDef def)
    {
        var so = new SerializedObject(comp);
        so.FindProperty("alwaysSpawn").boolValue = def.alwaysSpawn;
        so.FindProperty("spawnChance").floatValue = def.spawnChance;
        so.FindProperty("allowedType").enumValueIndex = (int)def.enemyType;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private void ApplyCollectibleFieldsGUI(CollectibleSpawnPoint comp, CollectibleSpawnDef def)
    {
        var so = new SerializedObject(comp);
        so.FindProperty("alwaysSpawn").boolValue = def.alwaysSpawn;
        so.FindProperty("spawnChance").floatValue = def.spawnChance;
        so.FindProperty("collectibleType").enumValueIndex = (int)def.collectibleType;
        so.FindProperty("groupCount").intValue = def.groupCount;
        so.FindProperty("groupSpacing").floatValue = def.groupSpacing;
        so.FindProperty("groupPattern").enumValueIndex = (int)def.groupPattern;
        so.FindProperty("spawnPointUuid").stringValue = System.Guid.NewGuid().ToString();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private void ApplyObstacleFieldsGUI(ObstacleSpawnPoint comp, SpawnPointDef def)
    {
        var so = new SerializedObject(comp);
        so.FindProperty("alwaysSpawn").boolValue = def.alwaysSpawn;
        so.FindProperty("spawnChance").floatValue = def.spawnChance;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private void LoadFromTemplate(GameObject prefab)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            var segment = instance.GetComponent<LevelSegment>();
            if (segment != null)
            {
                var so = new SerializedObject(segment);
                _segmentLength = so.FindProperty("segmentLength").floatValue;
                _difficultyWeight = so.FindProperty("difficultyWeight").floatValue;
                _allowConsecutive = so.FindProperty("allowConsecutive").boolValue;
            }

            var reveal = instance.GetComponent<SegmentReveal>();
            if (reveal != null)
            {
                var so = new SerializedObject(reveal);
                _revealDuration = so.FindProperty("revealDuration").floatValue;
                _dropDistance = so.FindProperty("dropDistance").floatValue;
                _overshoot = so.FindProperty("overshoot").floatValue;
                _staggerChildren = so.FindProperty("staggerChildren").boolValue;
                _staggerDelay = so.FindProperty("staggerDelay").floatValue;
                _maxStaggeredChildren = so.FindProperty("maxStaggeredChildren").intValue;
            }

            var env = instance.transform.Find("Environment");
            if (env != null)
            {
                var plane = env.Find("Plane");
                if (plane != null)
                {
                    _planePosition = plane.localPosition;
                    _planeScale = plane.localScale;
                    var mr = plane.GetComponent<MeshRenderer>();
                    if (mr != null && mr.sharedMaterial != null)
                        _groundMaterial = mr.sharedMaterial;
                }

                var bg = env.Find("Background");
                if (bg != null)
                    _backgroundPosition = bg.localPosition;
            }

            _enemySpawns.Clear();
            foreach (var esp in instance.GetComponentsInChildren<EnemySpawnPoint>(true))
            {
                var eso = new SerializedObject(esp);
                _enemySpawns.Add(new SpawnPointDef
                {
                    position = esp.transform.localPosition,
                    alwaysSpawn = eso.FindProperty("alwaysSpawn").boolValue,
                    spawnChance = eso.FindProperty("spawnChance").floatValue,
                    enemyType = (EnemyType)eso.FindProperty("allowedType").enumValueIndex
                });
            }

            _collectibleSpawns.Clear();
            foreach (var csp in instance.GetComponentsInChildren<CollectibleSpawnPoint>(true))
            {
                var cso = new SerializedObject(csp);
                _collectibleSpawns.Add(new CollectibleSpawnDef
                {
                    position = csp.transform.localPosition,
                    alwaysSpawn = cso.FindProperty("alwaysSpawn").boolValue,
                    spawnChance = cso.FindProperty("spawnChance").floatValue,
                    collectibleType = (CollectibleType)cso.FindProperty("collectibleType").enumValueIndex,
                    groupCount = cso.FindProperty("groupCount").intValue,
                    groupSpacing = cso.FindProperty("groupSpacing").floatValue,
                    groupPattern = (GroupPattern)cso.FindProperty("groupPattern").enumValueIndex
                });
            }

            _obstacleSpawns.Clear();
            foreach (var osp in instance.GetComponentsInChildren<ObstacleSpawnPoint>(true))
            {
                var oso = new SerializedObject(osp);
                _obstacleSpawns.Add(new SpawnPointDef
                {
                    position = osp.transform.localPosition,
                    alwaysSpawn = oso.FindProperty("alwaysSpawn").boolValue,
                    spawnChance = oso.FindProperty("spawnChance").floatValue
                });
            }

            Debug.Log($"[LevelSegmentFactory] Loaded settings from '{prefab.name}'");
        }
        finally
        {
            DestroyImmediate(instance);
        }
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    [System.Serializable]
    private struct SpawnPointDef
    {
        public Vector3 position;
        public float spawnChance;
        public bool alwaysSpawn;
        public EnemyType enemyType;
    }

    [System.Serializable]
    private struct CollectibleSpawnDef
    {
        public Vector3 position;
        public float spawnChance;
        public bool alwaysSpawn;
        public CollectibleType collectibleType;
        public int groupCount;
        public float groupSpacing;
        public GroupPattern groupPattern;
    }
}
