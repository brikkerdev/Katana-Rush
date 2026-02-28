using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ComponentPrefabSelectorWindow : EditorWindow
{
    private Type componentType;
    private UnityEngine.Object targetObject;
    private string propertyPath;
    private int arrayIndex = -1;
    private List<PrefabEntry> prefabs = new List<PrefabEntry>();
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private Texture2D selectedTexture;
    private Texture2D normalTexture;

    private class PrefabEntry
    {
        public GameObject prefab;
        public Component component;
        public string path;
        public string name;
        public Texture2D preview;
    }

    public static void Show(SerializedProperty property, Type componentType)
    {
        Show(property, -1, componentType);
    }

    public static void Show(SerializedProperty property, int arrayIndex, Type componentType)
    {
        if (property == null) return;
        if (property.serializedObject == null) return;
        if (property.serializedObject.targetObject == null) return;
        if (componentType == null) return;

        ComponentPrefabSelectorWindow window = GetWindow<ComponentPrefabSelectorWindow>(true, "Select " + componentType.Name + " Prefab");
        window.minSize = new Vector2(400, 500);
        window.componentType = componentType;
        window.targetObject = property.serializedObject.targetObject;
        window.propertyPath = property.propertyPath;
        window.arrayIndex = arrayIndex;
        window.CreateTextures();
        window.RefreshPrefabList();
        window.ShowUtility();
    }

    private void CreateTextures()
    {
        selectedTexture = new Texture2D(1, 1);
        selectedTexture.SetPixel(0, 0, new Color(0.3f, 0.5f, 0.8f, 0.3f));
        selectedTexture.Apply();

        normalTexture = new Texture2D(1, 1);
        normalTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
        normalTexture.Apply();
    }

    private SerializedProperty GetProperty()
    {
        if (targetObject == null) return null;
        if (string.IsNullOrEmpty(propertyPath)) return null;

        SerializedObject so = new SerializedObject(targetObject);
        if (so == null) return null;

        return so.FindProperty(propertyPath);
    }

    private UnityEngine.Object GetCurrentValue()
    {
        SerializedProperty prop = GetProperty();
        if (prop == null) return null;

        if (arrayIndex >= 0)
        {
            if (prop.isArray && arrayIndex < prop.arraySize)
            {
                SerializedProperty element = prop.GetArrayElementAtIndex(arrayIndex);
                return element != null ? element.objectReferenceValue : null;
            }
            return null;
        }

        return prop.objectReferenceValue;
    }

    private void RefreshPrefabList()
    {
        prefabs.Clear();

        if (componentType == null) return;

        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        if (guids == null) return;

        for (int i = 0; i < guids.Length; i++)
        {
            string guid = guids[i];
            if (string.IsNullOrEmpty(guid)) continue;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            Component comp = prefab.GetComponent(componentType);
            if (comp == null) continue;

            PrefabEntry entry = new PrefabEntry();
            entry.prefab = prefab;
            entry.component = comp;
            entry.path = path;
            entry.name = prefab.name;
            entry.preview = AssetPreview.GetAssetPreview(prefab);

            prefabs.Add(entry);
        }

        prefabs.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
    }

    private void OnGUI()
    {
        if (targetObject == null)
        {
            Close();
            return;
        }

        if (string.IsNullOrEmpty(propertyPath))
        {
            Close();
            return;
        }

        if (componentType == null)
        {
            Close();
            return;
        }

        DrawSearchBar();
        DrawInfo();
        DrawClearButton();
        DrawPrefabList();
        DrawRefreshButton();
    }

    private void DrawSearchBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Search:", GUILayout.Width(50));

        string newFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);
        if (newFilter != null)
        {
            searchFilter = newFilter;
        }

        if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
        {
            searchFilter = "";
            GUI.FocusControl(null);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawInfo()
    {
        string typeName = "Unknown";
        if (componentType != null)
        {
            typeName = componentType.Name;
        }

        int count = 0;
        if (prefabs != null)
        {
            count = prefabs.Count;
        }

        string targetInfo = arrayIndex >= 0 ? $" (Element {arrayIndex})" : " (Single)";
        EditorGUILayout.LabelField("Found " + count + " prefabs with " + typeName + targetInfo, EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(5);
    }

    private void DrawClearButton()
    {
        if (GUILayout.Button("Clear (None)", GUILayout.Height(25)))
        {
            ApplySelection(null);
        }

        EditorGUILayout.Space(5);
    }

    private void DrawPrefabList()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (prefabs == null)
        {
            EditorGUILayout.EndScrollView();
            return;
        }

        string lowerFilter = "";
        if (!string.IsNullOrEmpty(searchFilter))
        {
            lowerFilter = searchFilter.ToLowerInvariant();
        }

        UnityEngine.Object currentValue = GetCurrentValue();

        for (int i = 0; i < prefabs.Count; i++)
        {
            PrefabEntry entry = prefabs[i];
            if (entry == null) continue;

            if (!string.IsNullOrEmpty(lowerFilter))
            {
                bool nameMatch = false;
                bool pathMatch = false;

                if (!string.IsNullOrEmpty(entry.name))
                {
                    nameMatch = entry.name.ToLowerInvariant().Contains(lowerFilter);
                }

                if (!string.IsNullOrEmpty(entry.path))
                {
                    pathMatch = entry.path.ToLowerInvariant().Contains(lowerFilter);
                }

                if (!nameMatch && !pathMatch) continue;
            }

            DrawPrefabEntry(entry, currentValue);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawPrefabEntry(PrefabEntry entry, UnityEngine.Object currentValue)
    {
        if (entry == null) return;

        bool isSelected = false;
        if (currentValue != null && entry.component != null)
        {
            isSelected = currentValue == entry.component;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        if (isSelected && selectedTexture != null)
        {
            GUI.DrawTexture(GUILayoutUtility.GetRect(0, 0), selectedTexture, ScaleMode.StretchToFill);
        }

        DrawPreview(entry);
        DrawEntryInfo(entry);

        GUILayout.FlexibleSpace();

        DrawEntryButtons(entry, isSelected);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPreview(PrefabEntry entry)
    {
        if (entry == null) return;

        if (entry.preview != null)
        {
            GUILayout.Label(entry.preview, GUILayout.Width(50), GUILayout.Height(50));
            return;
        }

        GUIContent prefabIcon = EditorGUIUtility.IconContent("Prefab Icon");
        if (prefabIcon != null)
        {
            GUILayout.Label(prefabIcon, GUILayout.Width(50), GUILayout.Height(50));
            return;
        }

        GUILayout.Label("", GUILayout.Width(50), GUILayout.Height(50));
    }

    private void DrawEntryInfo(PrefabEntry entry)
    {
        if (entry == null) return;

        EditorGUILayout.BeginVertical();

        string displayName = "Unknown";
        if (!string.IsNullOrEmpty(entry.name))
        {
            displayName = entry.name;
        }

        string displayPath = "";
        if (!string.IsNullOrEmpty(entry.path))
        {
            displayPath = entry.path;
        }

        EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(displayPath, EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    private void DrawEntryButtons(PrefabEntry entry, bool isSelected)
    {
        if (entry == null) return;

        string selectText = "Select";
        if (isSelected)
        {
            selectText = "Selected";
        }

        if (GUILayout.Button(selectText, GUILayout.Width(80), GUILayout.Height(40)))
        {
            ApplySelection(entry);
        }

        if (GUILayout.Button("Ping", GUILayout.Width(40), GUILayout.Height(40)))
        {
            if (entry.prefab != null)
            {
                EditorGUIUtility.PingObject(entry.prefab);
            }
        }
    }

    private void DrawRefreshButton()
    {
        EditorGUILayout.Space(5);

        if (GUILayout.Button("Refresh List", GUILayout.Height(25)))
        {
            RefreshPrefabList();
        }
    }

    private void ApplySelection(PrefabEntry entry)
    {
        if (targetObject == null) return;
        if (string.IsNullOrEmpty(propertyPath)) return;

        SerializedObject so = new SerializedObject(targetObject);
        if (so == null) return;

        SerializedProperty prop = so.FindProperty(propertyPath);
        if (prop == null) return;

        if (arrayIndex >= 0)
        {
            if (prop.isArray && arrayIndex < prop.arraySize)
            {
                SerializedProperty element = prop.GetArrayElementAtIndex(arrayIndex);
                if (element != null)
                {
                    if (entry != null)
                    {
                        element.objectReferenceValue = entry.component;
                    }
                    else
                    {
                        element.objectReferenceValue = null;
                    }
                }
            }
        }
        else
        {
            if (entry != null)
            {
                prop.objectReferenceValue = entry.component;
            }
            else
            {
                prop.objectReferenceValue = null;
            }
        }

        so.ApplyModifiedProperties();
        Close();
    }

    private void OnDestroy()
    {
        targetObject = null;
        propertyPath = null;
        componentType = null;
        arrayIndex = -1;

        if (prefabs != null)
        {
            prefabs.Clear();
        }

        if (selectedTexture != null)
        {
            DestroyImmediate(selectedTexture);
            selectedTexture = null;
        }

        if (normalTexture != null)
        {
            DestroyImmediate(normalTexture);
            normalTexture = null;
        }
    }
}