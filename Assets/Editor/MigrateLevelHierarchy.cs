using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Runner.LevelGeneration;

public static class MigrateLevelHierarchy
{
    const string LevelChildName = "Level";
    const string SearchFolder = "Assets/Prefabs/Levels";

    [MenuItem("Tools/Migrate Level Hierarchy")]
    public static void Run()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { SearchFolder });
        int migrated = 0;
        int skipped = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                EditorUtility.DisplayProgressBar("Migrating Level Hierarchy",
                    $"Processing {path}...", (float)i / guids.Length);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<LevelSegment>() == null)
                {
                    skipped++;
                    continue;
                }

                if (MigratePrefab(path))
                    migrated++;
                else
                    skipped++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MigrateLevelHierarchy] Done. Migrated: {migrated}, Skipped: {skipped}");
    }

    static bool MigratePrefab(string assetPath)
    {
        var root = PrefabUtility.LoadPrefabContents(assetPath);
        if (root == null) return false;

        try
        {
            var env = root.transform.Find("Environment");
            if (env == null)
            {
                Debug.LogWarning($"[MigrateLevelHierarchy] No Environment in {assetPath}, skipping");
                return false;
            }

            if (env.Find(LevelChildName) != null)
            {
                Debug.Log($"[MigrateLevelHierarchy] {assetPath} already has Level child, skipping");
                return false;
            }

            var toMove = new List<Transform>();
            for (int i = 0; i < env.childCount; i++)
            {
                var child = env.GetChild(i);
                if (IsPrimitive(child.name))
                    toMove.Add(child);
            }

            if (toMove.Count == 0)
            {
                Debug.Log($"[MigrateLevelHierarchy] {assetPath} has no cubes/slopes to move, skipping");
                return false;
            }

            var levelGO = new GameObject(LevelChildName);
            levelGO.transform.SetParent(env, false);
            levelGO.transform.localPosition = Vector3.zero;
            levelGO.transform.localRotation = Quaternion.identity;
            levelGO.transform.localScale = Vector3.one;

            foreach (var child in toMove)
                child.SetParent(levelGO.transform, true);

            PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            Debug.Log($"[MigrateLevelHierarchy] Migrated {assetPath}: moved {toMove.Count} primitives into Level");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static bool IsPrimitive(string name)
    {
        string lower = name.ToLowerInvariant();
        return lower.StartsWith("cube") || lower.StartsWith("slope");
    }
}
