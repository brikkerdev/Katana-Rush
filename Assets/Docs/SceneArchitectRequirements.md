# SceneArchitect -- Independent Unity Package Requirements

## 1. Package Identity & Distribution

### 1.1 UPM Package Metadata

- **Name**: `com.katana.scenearchitect`
- **Display Name**: SceneArchitect
- **Version**: `0.1.0` (initial)
- **Unity compatibility**: `2021.3` minimum (LTS baseline)
- **Description**: Programmatic scene and prefab editing with automatic reference resolution
- **Keywords**: `editor`, `scene`, `prefab`, `automation`, `references`

### 1.2 Installation Methods

- **Git URL** (primary): `https://github.com/<owner>/SceneArchitect.git`
  - Users add via Package Manager > Add package from Git URL
- **Local / embedded**: copy into `Packages/com.katana.scenearchitect/` for development
- **Tarball**: `npm pack` output for offline distribution
- No Unity Asset Store dependency; no third-party dependencies

### 1.3 Why a Package Instead of Editor Scripts

- Reusable across multiple Unity projects without copy-pasting
- Version-controlled independently from game code
- Clear API boundary: the package never references project-specific types -- it discovers them via reflection and Unity serialization
- Testable in isolation with synthetic prefabs/scenes
- Updatable via Package Manager without merge conflicts

---

## 2. Folder Structure

```
Packages/com.katana.scenearchitect/
  package.json
  README.md
  CHANGELOG.md
  LICENSE.md
  Editor/
    com.katana.scenearchitect.Editor.asmdef
    Core/
      SceneOps.cs            -- scene open/save/find/add/remove/reparent
      PrefabOps.cs           -- prefab create/open/edit/save/add/remove
      ComponentOps.cs        -- add/remove/copy components, set fields
      AssetSearcher.cs       -- project & scene search by type/name/tag
      ReferenceResolver.cs   -- automatic reference wiring engine
      ResolverStrategies.cs  -- pluggable matching strategies
      BatchProcessor.cs      -- multi-target operation runner
      TypeCache.cs           -- cached type lookups via reflection
    UI/
      SceneArchitectWindow.cs     -- main tabbed editor window
      Tabs/
        SceneTab.cs               -- scene hierarchy tree view
        PrefabTab.cs              -- prefab hierarchy editor
        ReferencesTab.cs          -- reference scanner & resolver UI
        BatchTab.cs               -- batch operations UI
      Drawers/
        ResolveResultDrawer.cs    -- custom drawing for resolve results
        OperationPreviewDrawer.cs -- preview panel for batch ops
      Styles/
        SceneArchitectStyles.cs   -- shared GUIStyle/skin definitions
        SceneArchitectStyles.uss  -- UIToolkit stylesheet (future)
    Scripting/
      ScriptRunner.cs        -- JSON script parser & executor
      Operations/
        ISceneOperation.cs   -- operation interface
        OpenSceneOp.cs
        SaveSceneOp.cs
        AddGameObjectOp.cs
        RemoveGameObjectOp.cs
        AddComponentOp.cs
        SetFieldOp.cs
        ResolveReferencesOp.cs
        EditPrefabOp.cs
        BatchOp.cs
    Config/
      SceneArchitectSettings.cs       -- settings ScriptableObject
      SceneArchitectSettingsProvider.cs -- Project Settings integration
  Tests/
    Editor/
      com.katana.scenearchitect.Editor.Tests.asmdef
      SceneOpsTests.cs
      PrefabOpsTests.cs
      ComponentOpsTests.cs
      AssetSearcherTests.cs
      ReferenceResolverTests.cs
      BatchProcessorTests.cs
      ScriptRunnerTests.cs
      TestFixtures/          -- synthetic prefabs, scenes, SOs for tests
  Samples~/
    BasicSetup/
      SampleResolveScript.json
      README.md
  Documentation~/
    SceneArchitect.md
```

---

## 3. The Independence Problem: No Project Type References

This is the critical design constraint. The package cannot `using Runner.Core;` or reference any project assembly. All type interaction must go through:

### 3.1 Unity Serialization APIs (primary path)

- `SerializedObject` / `SerializedProperty` -- read/write any field without knowing the type at compile time
- `SerializedProperty.objectReferenceValue` -- assign references
- `SerializedProperty.type`, `propertyType`, `FindProperty()` -- introspect field types
- This is exactly what the existing factories already do (e.g., `so.FindProperty("segmentLength").floatValue = value`)

### 3.2 Reflection (for type discovery)

- `System.Type.GetType()`, `AppDomain.CurrentDomain.GetAssemblies()` -- find types by string name
- `TypeCache.GetTypesDerivedFrom<T>()` -- Unity's fast type cache for editor scripts
- `typeof(Component).IsAssignableFrom(type)` -- check if a type is a valid component
- Used by `AssetSearcher` to find "all GameObjects with a component of type X" without importing X

### 3.3 AssetDatabase Queries

- `AssetDatabase.FindAssets($"t:{typeName}")` -- find assets by type name string
- `AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)` -- load without knowing concrete type
- `AssetDatabase.GUIDToAssetPath()` / `AssetDatabase.AssetPathToGUID()` -- path resolution

### 3.4 Scene Hierarchy Traversal

- `UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()`
- Recursive `transform.GetChild(i)` traversal
- `GameObject.GetComponent(System.Type)` -- get component by reflected type

### 3.5 Assembly Definition Implications

The Editor `.asmdef` references only:
- `UnityEditor` (implicit)
- `UnityEngine` (implicit)

No references to any project assembly (`Assembly-CSharp`, `Assembly-CSharp-Editor`, etc.). This guarantees the package compiles independently.

---

## 4. Core API Design

### 4.1 SceneOps

```csharp
namespace SceneArchitect
{
    public static class SceneOps
    {
        // Scene lifecycle
        static Scene OpenScene(string path, OpenSceneMode mode);
        static void SaveScene(Scene scene);
        static Scene NewScene(NewSceneSetup setup);
        
        // Hierarchy queries
        static GameObject Find(string path);               // "Parent/Child/Grandchild"
        static GameObject[] FindByName(string name);
        static GameObject[] FindByTag(string tag);
        static GameObject[] FindByComponent(string typeName);
        static GameObject[] FindByComponent(System.Type type);
        static GameObject[] GetRootObjects();
        
        // Hierarchy mutations
        static GameObject AddGameObject(string name, Transform parent = null);
        static void Remove(GameObject go);
        static void Reparent(GameObject go, Transform newParent);
        static GameObject Duplicate(GameObject go);
        static void SetTransform(GameObject go, Vector3? pos, Quaternion? rot, Vector3? scale);
    }
}
```

### 4.2 PrefabOps

```csharp
namespace SceneArchitect
{
    public static class PrefabOps
    {
        // Prefab editing (uses LoadPrefabContents/UnloadPrefabContents pattern)
        static PrefabEditScope Open(string assetPath);  // IDisposable scope
        static string SaveAs(GameObject root, string path);
        static void Instantiate(string prefabPath, Transform parent = null);
        
        // Within an open prefab context
        static GameObject AddChild(GameObject parent, string name);
        static void RemoveChild(GameObject parent, string childPath);
        static Component AddComponent(GameObject go, string typeName);
        static void RemoveComponent(GameObject go, string typeName);
    }
    
    // Disposable scope for safe prefab editing
    public class PrefabEditScope : IDisposable
    {
        public GameObject Root { get; }
        public void Save();
        public void Dispose();  // calls UnloadPrefabContents if not saved
    }
}
```

### 4.3 ComponentOps

```csharp
namespace SceneArchitect
{
    public static class ComponentOps
    {
        // Add/remove by type name string (no compile-time dependency)
        static Component Add(GameObject go, string typeName);
        static void Remove(GameObject go, string typeName);
        static void Remove(Component component);
        
        // Generic field setter via SerializedProperty
        static void SetField(UnityEngine.Object target, string fieldName, object value);
        static object GetField(UnityEngine.Object target, string fieldName);
        static void SetFields(UnityEngine.Object target, Dictionary<string, object> values);
        
        // Copy all serialized fields from one component to another of the same type
        static void CopyValues(Component source, Component destination);
        
        // Introspection: list all serialized fields and their types
        static FieldInfo[] GetSerializedFields(UnityEngine.Object target);
    }
}
```

### 4.4 ReferenceResolver

```csharp
namespace SceneArchitect
{
    public static class ReferenceResolver
    {
        // Scan a target for all unresolved (null) object reference fields
        static ResolveResult[] Scan(UnityEngine.Object target, ResolveScope scope);
        
        // Attempt to resolve all null references
        static ResolveReport ResolveAll(UnityEngine.Object target, ResolveScope scope, 
                                         ResolveConfig config = null);
        
        // Resolve a single field
        static bool ResolveField(UnityEngine.Object target, string fieldName, 
                                  ResolveScope scope, ResolveConfig config = null);
    }
    
    public class ResolveResult
    {
        public string FieldName;
        public string ExpectedTypeName;
        public SerializedProperty Property;
        public UnityEngine.Object[] Candidates;     // all matches found
        public UnityEngine.Object BestMatch;         // top candidate (null if ambiguous)
        public ResolveConfidence Confidence;          // Exact, High, Medium, Ambiguous, None
        public string MatchReason;                    // "type-unique", "name-match", etc.
    }
    
    public enum ResolveConfidence { Exact, High, Medium, Ambiguous, None }
    
    public class ResolveScope
    {
        public bool SearchScene;
        public bool SearchProject;
        public string[] FolderFilters;       // e.g., {"Assets/Prefabs/", "Assets/Scriptables/"}
    }
    
    public class ResolveConfig
    {
        public Dictionary<string, string> ExplicitMappings;  // fieldName -> assetPath
        public string[] StripSuffixes;       // default: {"Prefab","Controller","Manager",...}
        public bool ResolveNonNull;          // re-resolve fields that already have a value
        public bool PopulateArrays;          // auto-fill array/list fields
    }
}
```

### 4.5 Matching Strategies (pluggable)

```csharp
namespace SceneArchitect
{
    public interface IResolveStrategy
    {
        int Priority { get; }      // lower = tried first
        string Name { get; }
        bool TryResolve(SerializedProperty prop, System.Type expectedType,
                        UnityEngine.Object[] candidates, out UnityEngine.Object match);
    }
    
    // Built-in strategies:
    // 1. ExplicitMappingStrategy  (priority 0) -- exact path from config
    // 2. TypeUniqueStrategy       (priority 10) -- only one candidate of that type
    // 3. ExactNameStrategy        (priority 20) -- field name == asset name (case-insensitive)
    // 4. FuzzyNameStrategy        (priority 30) -- tokenized name matching
    // 5. TagStrategy              (priority 40) -- match by GameObject tag
    
    // Users can register custom strategies at runtime:
    // ReferenceResolver.RegisterStrategy(new MyCustomStrategy());
}
```

---

## 5. The `package.json`

```json
{
  "name": "com.katana.scenearchitect",
  "version": "0.1.0",
  "displayName": "SceneArchitect",
  "description": "Programmatic scene and prefab editing with automatic reference resolution. Edit hierarchies, wire up serialized fields, and batch-process assets from code or JSON scripts.",
  "unity": "2021.3",
  "author": {
    "name": "KatanaRush",
    "url": ""
  },
  "keywords": [
    "editor",
    "scene",
    "prefab",
    "automation",
    "references",
    "tooling"
  ],
  "type": "tool",
  "samples": [
    {
      "displayName": "Basic Setup",
      "description": "Sample JSON scripts and usage examples",
      "path": "Samples~/BasicSetup"
    }
  ]
}
```

---

## 6. Assembly Definition Files

### Editor asmdef (`Editor/com.katana.scenearchitect.Editor.asmdef`)

```json
{
  "name": "com.katana.scenearchitect.Editor",
  "rootNamespace": "SceneArchitect",
  "references": [],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "autoReferenced": true,
  "defineConstraints": [],
  "noEngineReferences": false
}
```

Key: `references` is empty -- no dependency on `Assembly-CSharp` or any project assembly. `includePlatforms` is `["Editor"]` only.

### Tests asmdef (`Tests/Editor/com.katana.scenearchitect.Editor.Tests.asmdef`)

```json
{
  "name": "com.katana.scenearchitect.Editor.Tests",
  "rootNamespace": "SceneArchitect.Tests",
  "references": [
    "com.katana.scenearchitect.Editor",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": ["Editor"],
  "optionalUnityReferences": ["TestAssemblies"],
  "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

---

## 7. Settings Integration (Project Settings, not ScriptableObject asset)

Instead of a ScriptableObject the user must find and create, use `SettingsProvider` to appear in **Edit > Project Settings > SceneArchitect**:

- **Default resolve scope**: Scene / Project / Custom folders
- **Strip suffixes list**: `["Prefab", "Controller", "Manager", "Instance", "Component", "SO"]`
- **Logging verbosity**: Quiet / Normal / Verbose
- **Auto-backup before batch ops**: on/off
- **Custom folder mappings**: `{ "prefabs": "Assets/Prefabs", "scriptables": "Assets/Scriptables" }`

Settings stored via `EditorPrefs` or a JSON file in `ProjectSettings/SceneArchitectSettings.json` (not in Assets, avoids cluttering the project).

---

## 8. JSON Scripting API

### 8.1 Schema

```json
{
  "version": "1.0",
  "operations": [
    {
      "op": "open_scene",
      "path": "Assets/Scenes/SampleScene.unity"
    },
    {
      "op": "find",
      "query": { "component": "Game" },
      "store_as": "game_obj"
    },
    {
      "op": "resolve_references",
      "target": "$game_obj",
      "scope": { "scene": true, "project": true, "folders": ["Assets/Prefabs"] },
      "overrides": {
        "playerPrefab": "Assets/Prefabs/Player/Player.prefab"
      }
    },
    {
      "op": "edit_prefab",
      "path": "Assets/Prefabs/Levels/Desert/Level 1.prefab",
      "actions": [
        { "action": "add_child", "parent": "Spawnpoints", "name": "NewSpawn" },
        { "action": "add_component", "target": "Spawnpoints/NewSpawn", "type": "EnemySpawnPoint" },
        { "action": "set_field", "target": "Spawnpoints/NewSpawn", "component": "EnemySpawnPoint", 
          "fields": { "spawnChance": 0.8, "alwaysSpawn": false } }
      ]
    },
    {
      "op": "batch",
      "filter": { "folder": "Assets/Prefabs/Levels/Desert", "pattern": "Level*", "has_component": "LevelSegment" },
      "actions": [
        { "action": "set_field", "component": "LevelSegment", "fields": { "allowConsecutive": true } }
      ]
    },
    {
      "op": "save_scene"
    }
  ]
}
```

### 8.2 Execution

- Menu item: **Tools > SceneArchitect > Run Script...**  (file picker)
- Menu item: **Tools > SceneArchitect > Run Script from Path** (text field)
- C# API: `ScriptRunner.Execute(string jsonPath)` / `ScriptRunner.Execute(ScriptDefinition script)`
- Variables (`$game_obj`) allow referencing results of earlier operations

---

## 9. Development Workflow

### 9.1 Local Development (in this project)

During development, the package lives as an **embedded package**:

```
KatanaRush/
  Assets/        -- game code (Assembly-CSharp)
  Packages/
    com.katana.scenearchitect/    -- the package (embedded)
    manifest.json                 -- references the local path
```

`manifest.json` entry:
```json
{
  "dependencies": {
    "com.katana.scenearchitect": "file:com.katana.scenearchitect"
  }
}
```

### 9.2 Publishing

When ready, move the package folder to its own Git repository. Other projects install via:

```
"com.katana.scenearchitect": "https://github.com/<owner>/SceneArchitect.git"
```

Or with a specific version tag:

```
"com.katana.scenearchitect": "https://github.com/<owner>/SceneArchitect.git#v0.1.0"
```

### 9.3 Versioning

Follow Semantic Versioning:
- `0.x.y` -- pre-1.0, breaking changes allowed between minor versions
- Maintain `CHANGELOG.md` with `[Unreleased]`, `[0.1.0]`, etc.
- Git tags: `v0.1.0`, `v0.2.0`, etc.

---

## 10. Safety & Undo

- All mutations go through `Undo.RecordObject` / `Undo.RegisterCreatedObjectUndo`
- `ScriptRunner` wraps entire script in `Undo.IncrementCurrentGroup()` / named group so the whole script is one undo step
- Dry-run mode: `ScriptRunner.Execute(json, dryRun: true)` returns a report without applying
- Logging: `[SceneArchitect]` prefix, respects verbosity setting

---

## 11. Testing Strategy

Tests live in `Tests/Editor/` and use Unity Test Framework (`[Test]`, `[UnityTest]`).

### Test fixtures (synthetic, shipped with the package)

- `TestPrefabA.prefab` -- simple hierarchy with known components
- `TestPrefabB.prefab` -- nested prefab variant
- `TestScene.unity` -- minimal scene with a few GameObjects
- `TestScriptableA.asset` -- ScriptableObject for reference resolution tests

### Test categories

- **SceneOps**: open/find/add/remove/reparent in a test scene
- **PrefabOps**: create/edit/save prefabs, verify hierarchy changes persist
- **ComponentOps**: add/remove components by type name, set/get fields of every property type
- **AssetSearcher**: find by type name, name pattern, folder filter
- **ReferenceResolver**: test all 5 strategies against known fixtures, verify correct assignment and ambiguity handling
- **BatchProcessor**: apply field changes to multiple prefabs, verify all were modified
- **ScriptRunner**: execute a sample JSON script, verify end state

---

## 12. Constraints & Compatibility

- **Editor-only**: all code under `Editor/` with `includePlatforms: ["Editor"]`
- **No runtime code**: no `Runtime/` folder, no runtime assembly. The package is pure tooling.
- **Unity 2021.3+**: uses `PrefabUtility.LoadPrefabContents` (2018.3+), `TypeCache` (2019.2+), `SettingsProvider` (2018.3+)
- **No project type imports**: all type interaction through reflection + `SerializedObject`
- **Nested prefab safe**: checks `PrefabUtility.IsPartOfPrefabInstance` before mutations, preserves overrides
- **No `using` of any project namespace**: the asmdef has zero assembly references beyond Unity's own

---

## 13. How to Use SceneArchitect

### 13.1 Opening the Tool

- **Menu**: Tools > SceneArchitect
- **Shortcut**: `Ctrl+Shift+A`
- **Settings**: Edit > Project Settings > SceneArchitect

### 13.2 Editor Window Tabs

#### Scene Tab

Find and manipulate GameObjects in the currently open scene.

- **Find by Path**: enter a hierarchy path like `Game/Player/Model` and hit Find
- **Search by Name**: finds all GameObjects with matching name across the scene
- **Search by Component**: enter a component type name (e.g., `LevelSegment`, `Enemy`) to find all GameObjects carrying it
- **Add GameObject**: create a new empty GameObject under an optional parent
- **Selected Object**: duplicate, delete, or reparent any scene object

#### Prefab Tab

Edit prefab assets without opening them in the scene.

1. Drag a prefab into the Target Prefab field
2. View its full hierarchy with component annotations
3. **Add Child**: specify a name and optional parent path (e.g., `Spawnpoints`) to add a child inside the prefab
4. **Remove Child**: specify the child path (e.g., `Spawnpoints/OldSpawn`) to remove it
5. **Add/Remove Component**: enter a type name (e.g., `EnemySpawnPoint`, `BoxCollider`) and the target child path
6. **Instantiate**: place the prefab into the current scene (packed or unpacked)

#### References Tab

The core feature -- automatic reference resolution.

1. Drag a GameObject (from scene) or an asset (ScriptableObject, prefab) into the target field
2. Configure scope:
   - **Search Scene**: look for components/objects in the open scene hierarchy
   - **Search Project**: search `AssetDatabase` for prefabs, ScriptableObjects, materials, etc.
   - **Folder Filter**: restrict project search to specific folders (semicolon-separated, e.g., `Assets/Prefabs;Assets/Scriptables`)
3. Click **Scan** to see all serialized object reference fields and their current state
4. Each field shows:
   - Current value (or "null")
   - Expected type
   - Number of candidates found
   - Best match and the matching strategy used
   - Confidence level (Exact / High / Medium / Ambiguous / None)
5. Click **Resolve All** to automatically assign all unambiguous matches
6. Click **Apply** on individual fields to resolve them one at a time

**Example -- wiring up `Game.cs`:**

1. Open `SampleScene.unity`
2. Select the Game object in the scene
3. Open SceneArchitect > References tab
4. Drag the Game object into Target
5. Enable Search Scene + Search Project
6. Click Scan -- all 19 serialized fields are listed
7. Click Resolve All -- fields like `playerPrefab`, `cameraManager`, `sceneSetup` are auto-matched

#### Batch Tab

Apply operations across many prefabs at once.

1. **Filter**: set folder (`Assets/Prefabs/Levels/Desert`), name pattern (`Level`), and/or required component (`LevelSegment`)
2. Click **Preview Matches** to see which prefabs would be affected
3. **Select operation**:
   - **Set Field**: change a field value on a component across all matching prefabs
   - **Add Component**: add a component type to all matching prefabs
   - **Remove Component**: remove a component type from all matching prefabs
   - **Resolve References**: run the reference resolver on all matching prefabs
4. Click **Execute Batch**

**Example -- set `allowConsecutive = true` on all Desert levels:**

1. Folder: `Assets/Prefabs/Levels/Desert`
2. Has Component: `LevelSegment`
3. Operation: Set Field
4. Component Type: `LevelSegment`
5. Field Name: `allowConsecutive`
6. Value: `true`
7. Preview Matches, then Execute Batch

---

### 13.3 C# API Usage

All APIs are in the `SceneArchitect` namespace. Use them from any editor script, custom `EditorWindow`, or menu command. The package never references project types directly -- pass type names as strings.

#### Scene Operations

```csharp
using SceneArchitect;

// Open a scene
SceneOps.OpenScene("Assets/Scenes/SampleScene.unity");

// Find GameObjects
var game = SceneOps.Find("Game");                           // by hierarchy path
var enemies = SceneOps.FindByComponent("Enemy");            // by component type name
var tagged = SceneOps.FindByTag("Ground");                  // by tag
var named = SceneOps.FindByName("Player");                  // by name

// Mutate hierarchy
var newGo = SceneOps.AddGameObject("SpawnPoint", game.transform);
SceneOps.SetTransform(newGo, position: new Vector3(0, 5, 0));
SceneOps.Reparent(newGo, anotherParent);
SceneOps.Duplicate(existingObject);
SceneOps.Remove(objectToDelete);

// Save
SceneOps.SaveOpenScenes();
```

#### Prefab Editing

```csharp
using SceneArchitect;

// Edit a prefab safely with IDisposable scope
using (var scope = PrefabOps.Open("Assets/Prefabs/Levels/Desert/Level 1.prefab"))
{
    // Add a child
    var spawnGo = PrefabOps.AddChild(scope.Root, "NewEnemySpawn");
    
    // Find existing child
    var spawnpoints = PrefabOps.FindInPrefab(scope.Root, "Spawnpoints");
    
    // Add a component by type name (no using/import needed)
    PrefabOps.AddComponent(spawnGo, "EnemySpawnPoint");
    
    // Remove a child
    PrefabOps.RemoveChild(scope.Root, "OldChild");
    
    // Save changes back to disk
    scope.Save();
}
// scope.Dispose() automatically calls UnloadPrefabContents

// Create a brand-new prefab
var root = new GameObject("MyPrefab");
root.AddComponent<BoxCollider>();
PrefabOps.SaveAs(root, "Assets/Prefabs/MyPrefab.prefab");
Object.DestroyImmediate(root);

// Instantiate into scene
PrefabOps.Instantiate("Assets/Prefabs/Player/Player.prefab");
PrefabOps.InstantiateUnpacked("Assets/Prefabs/Player/Player.prefab");  // fully unpacked
```

#### Component Operations

```csharp
using SceneArchitect;

// Add/remove components by string name
ComponentOps.Add(gameObject, "Rigidbody");
ComponentOps.Add(gameObject, "Runner.Enemy.EnemySpawnPoint");  // fully qualified also works
ComponentOps.Remove(gameObject, "Rigidbody");

// Set fields on any component (uses SerializedObject internally)
ComponentOps.SetField(myComponent, "spawnChance", 0.8f);
ComponentOps.SetField(myComponent, "alwaysSpawn", true);
ComponentOps.SetField(myComponent, "segmentLength", 100f);

// Set multiple fields at once
ComponentOps.SetFields(myComponent, new Dictionary<string, object>
{
    { "spawnChance", 0.5f },
    { "alwaysSpawn", false },
    { "allowedType", 2 }  // enum by index
});

// Read a field value
float chance = (float)ComponentOps.GetField(myComponent, "spawnChance");

// Introspect all serialized fields
var fields = ComponentOps.GetSerializedFields(myComponent);
foreach (var f in fields)
    Debug.Log($"{f.Name} ({f.TypeName}): isArray={f.IsArray}");

// Find only object reference fields (useful for resolver debugging)
var refs = ComponentOps.GetObjectReferenceFields(myComponent);
foreach (var r in refs)
    Debug.Log($"{r.Name}: {(r.IsNull ? "NULL" : r.CurrentValue.name)}");

// Copy all values between components of the same type
ComponentOps.CopyValues(sourceComponent, destinationComponent);
```

#### Reference Resolver

```csharp
using SceneArchitect;

// 1. Scan -- see what needs resolving without changing anything
var results = ReferenceResolver.Scan(gameObject, ResolveScope.All);
foreach (var r in results)
{
    Debug.Log($"{r.FieldName} ({r.ExpectedTypeName}): " +
              $"null={r.IsNull}, candidates={r.Candidates.Length}, " +
              $"best={r.BestMatch?.name}, confidence={r.Confidence}");
}

// 2. Resolve all null fields automatically
var report = ReferenceResolver.ResolveAll(gameObject, ResolveScope.All);
Debug.Log($"Resolved {report.Resolved}, unresolved {report.Unresolved}");

// 3. Resolve with explicit overrides for ambiguous fields
var config = new ResolveConfig
{
    ExplicitMappings = new Dictionary<string, string>
    {
        { "playerPrefab", "Assets/Prefabs/Player/Player.prefab" },
        { "skyControllerPrefab", "Assets/Prefabs/Controllers/SkyController.prefab" }
    },
    StripSuffixes = new[] { "Prefab", "Controller", "Manager" },
    ResolveNonNull = false  // skip fields that already have a value
};
ReferenceResolver.ResolveAll(gameObject, ResolveScope.All, config);

// 4. Resolve a single field
bool resolved = ReferenceResolver.ResolveField(gameObject, "cameraManager", ResolveScope.SceneOnly);

// 5. Scope control
var scope = new ResolveScope
{
    SearchScene = true,
    SearchProject = true,
    FolderFilters = new[] { "Assets/Prefabs/Controllers", "Assets/Scriptables" }
};
ReferenceResolver.ResolveAll(gameObject, scope);

// 6. Register a custom strategy
ReferenceResolver.RegisterStrategy(new MyCustomStrategy());
```

#### Asset Searching

```csharp
using SceneArchitect;

// Find all assets of a type
var biomes = AssetSearcher.FindAssetsByType("BiomeData");
var katanas = AssetSearcher.FindAssetsByType("Katana", new[] { "Assets/Scriptables/Katanas" });

// Find prefabs containing a specific component
var levelPrefabs = AssetSearcher.FindPrefabsWithComponent("LevelSegment");
var enemyPrefabs = AssetSearcher.FindPrefabsWithComponent("Enemy",
    new[] { "Assets/Prefabs/Enemy" });

// Find assets by name
var results = AssetSearcher.FindAssetsByName("Desert", "BiomeData");

// Find components in the currently open scene
var cameras = AssetSearcher.FindSceneComponentsByType("CameraManager");
```

#### Batch Processing

```csharp
using SceneArchitect;

// Define a filter
var filter = new BatchFilter
{
    FolderPath = "Assets/Prefabs/Levels/Desert",
    NamePattern = "Level",
    HasComponent = "LevelSegment"
};

// Preview before executing
var preview = BatchProcessor.Preview(filter, "SetField");
Debug.Log($"Would affect {preview.Count} prefabs");

// Set a field on all matching prefabs
var result = BatchProcessor.SetFieldOnPrefabs(filter, "LevelSegment",
    new Dictionary<string, object> { { "segmentLength", 120f } });
Debug.Log(result);  // "Succeeded: 11, Skipped: 0, Failed: 0"

// Add a component to all matching prefabs
BatchProcessor.AddComponentToPrefabs(filter, "AudioSource");

// Remove a component from all matching prefabs
BatchProcessor.RemoveComponentFromPrefabs(filter, "AudioSource");

// Resolve references on all matching prefabs
BatchProcessor.ResolveReferencesOnPrefabs(filter, ResolveScope.All);
```

#### Type Resolution

```csharp
using SceneArchitect;

// Resolve any type by name (searches all loaded assemblies)
var type = TypeResolver.Resolve("LevelSegment");            // short name
var type2 = TypeResolver.Resolve("Runner.Enemy.Enemy");     // fully qualified
var type3 = TypeResolver.ResolveComponent("EnemySpawnPoint"); // only if it's a Component
var type4 = TypeResolver.ResolveScriptableObject("BiomeData"); // only if it's a ScriptableObject

// Get the expected type of a SerializedProperty's object reference
var expectedType = TypeResolver.GetFieldType(serializedProperty);
```

---

### 13.4 JSON Script Usage

Create a `.json` file with an array of operations. Run via **Tools > SceneArchitect > Run Script...** or from C#.

#### Full Example: Wire up a scene and batch-edit prefabs

```json
{
    "version": "1.0",
    "operations": [
        {
            "op": "open_scene",
            "path": "Assets/Scenes/SampleScene.unity"
        },
        {
            "op": "find",
            "query_component": "Game",
            "store_as": "game_obj"
        },
        {
            "op": "resolve_references",
            "target": "$game_obj",
            "scope_scene": true,
            "scope_project": true,
            "scope_folders": ["Assets/Prefabs", "Assets/Scriptables"]
        },
        {
            "op": "add_gameobject",
            "name": "DebugMarker",
            "parent": "Game",
            "position": [0, 10, 0],
            "store_as": "marker"
        },
        {
            "op": "edit_prefab",
            "path": "Assets/Prefabs/Levels/Desert/Level 1.prefab",
            "actions": [
                {
                    "action": "add_child",
                    "parent": "Spawnpoints",
                    "name": "NewEnemySpawn"
                },
                {
                    "action": "add_component",
                    "target": "Spawnpoints/NewEnemySpawn",
                    "type": "EnemySpawnPoint"
                },
                {
                    "action": "set_field",
                    "target": "Spawnpoints/NewEnemySpawn",
                    "component": "EnemySpawnPoint",
                    "fields": [
                        { "name": "spawnChance", "value": "0.7" },
                        { "name": "alwaysSpawn", "value": "false" }
                    ]
                }
            ]
        },
        {
            "op": "batch",
            "filter_folder": "Assets/Prefabs/Levels/SnowBlock",
            "filter_component": "LevelSegment",
            "actions": [
                {
                    "action": "set_field",
                    "component": "LevelSegment",
                    "fields": [
                        { "name": "allowConsecutive", "value": "true" },
                        { "name": "segmentLength", "value": "100" }
                    ]
                }
            ]
        },
        {
            "op": "save_scene"
        }
    ]
}
```

#### Running from C#

```csharp
using SceneArchitect.Scripting;

// Run a script file
var report = ScriptRunner.Execute("Assets/Scripts/setup_scene.json");
Debug.Log($"Success: {report.Success}, Operations: {report.OperationsExecuted}");

// Dry run -- preview without applying changes
var preview = ScriptRunner.Execute("Assets/Scripts/setup_scene.json", dryRun: true);
foreach (var log in preview.Log)
    Debug.Log(log);

// Run JSON string directly
string json = @"{ ""version"": ""1.0"", ""operations"": [...] }";
ScriptRunner.ExecuteJson(json);
```

#### Available Operations

| `op` | Description | Key Fields |
|------|-------------|------------|
| `open_scene` | Open a scene file | `path` |
| `save_scene` | Save all open scenes | -- |
| `find` | Find a GameObject and store reference | `query_name`, `query_component`, `path`, `store_as` |
| `add_gameobject` | Create a new GameObject | `name`, `parent`, `components[]`, `position[]`, `store_as` |
| `remove_gameobject` | Delete a GameObject | `path` or `target` (supports `$variable`) |
| `resolve_references` | Auto-wire null fields | `target` (supports `$variable`), `scope_scene`, `scope_project`, `scope_folders[]` |
| `edit_prefab` | Open, modify, and save a prefab | `path`, `actions[]` |
| `batch` | Apply actions to multiple prefabs | `filter_folder`, `filter_pattern`, `filter_component`, `actions[]` |

#### Variable References

Operations that produce a result can store it with `store_as`. Later operations can reference it with `$variableName`:

```json
{ "op": "find", "query_component": "Game", "store_as": "game" }
{ "op": "resolve_references", "target": "$game" }
```

---

### 13.5 Project Settings

Navigate to **Edit > Project Settings > SceneArchitect** to configure defaults:

| Setting | Default | Description |
|---------|---------|-------------|
| Search Scene | true | Include open scene hierarchy in reference resolution |
| Search Project Assets | true | Include `AssetDatabase` in reference resolution |
| Custom Folder Filters | (empty) | Semicolon-separated paths to limit project search scope |
| Strip Suffixes | `Prefab,Controller,Manager,...` | Suffixes stripped from field names when matching (comma-separated) |
| Log Verbosity | Normal | Quiet / Normal / Verbose |
| Auto-Backup Before Batch Ops | false | Back up prefabs before batch modifications |

---

### 13.6 Custom Resolve Strategies

Implement `IResolveStrategy` to add project-specific matching logic:

```csharp
using SceneArchitect;
using UnityEditor;
using UnityEngine;

public class FolderBasedStrategy : IResolveStrategy
{
    public int Priority => 15;  // runs after ExplicitMapping (0), before ExactName (20)
    public string Name => "folder-based";

    public bool TryResolve(SerializedProperty prop, System.Type expectedType,
        UnityEngine.Object[] candidates, ResolveConfig config, out UnityEngine.Object match)
    {
        match = null;
        
        // Custom logic: prefer candidates from the "Controllers" folder
        foreach (var candidate in candidates)
        {
            string path = AssetDatabase.GetAssetPath(candidate);
            if (path.Contains("/Controllers/"))
            {
                match = candidate;
                return true;
            }
        }
        
        return false;
    }
}

// Register it (e.g., in an [InitializeOnLoad] class)
[InitializeOnLoad]
public static class StrategyRegistrar
{
    static StrategyRegistrar()
    {
        ReferenceResolver.RegisterStrategy(new FolderBasedStrategy());
    }
}
```

---

### 13.7 Key Concepts for AI Agents

When using SceneArchitect from AI-driven tooling (Cursor, scripts, automation):

- **All type references are strings**: pass `"LevelSegment"` or `"Runner.Enemy.Enemy"`, never a C# `typeof()`. The package resolves types via reflection at runtime.
- **`PrefabEditScope` is `IDisposable`**: always use `using` blocks. If you forget to call `Save()`, changes are discarded when the scope disposes.
- **Undo support is automatic**: all scene mutations register with Unity's Undo system. Prefab edits (via `PrefabEditScope`) save directly to disk.
- **The resolver is conservative**: it only auto-assigns when confidence is Medium or higher. Ambiguous fields are logged but left null -- use `ExplicitMappings` for those.
- **Field names are the matching key**: `playerPrefab` matches an asset named "Player" after stripping the "Prefab" suffix. `cameraManager` matches "CameraManager" after stripping "Manager". Customize via the Strip Suffixes setting.
- **Batch operations modify prefab assets on disk**: they are not undoable via `Ctrl+Z`. Use Preview first and consider enabling Auto-Backup in settings.
