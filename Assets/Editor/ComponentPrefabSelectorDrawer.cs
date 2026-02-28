using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ComponentPrefabSelectorAttribute))]
public class ComponentPrefabSelectorDrawer : PropertyDrawer
{
    private List<GameObject> cachedPrefabs;
    private string[] cachedDisplayNames;
    private Component[] cachedComponents;
    private Type cachedType;
    private double lastCacheTime;
    private const double CACHE_DURATION = 5.0;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property == null) return;

        ComponentPrefabSelectorAttribute attr = attribute as ComponentPrefabSelectorAttribute;

        Type componentType = null;

        if (attr != null && attr.ComponentType != null)
        {
            componentType = attr.ComponentType;
        }
        else if (fieldInfo != null)
        {
            Type fieldType = fieldInfo.FieldType;
            
            if (fieldType.IsArray)
            {
                componentType = fieldType.GetElementType();
            }
            else if (fieldType.IsGenericType)
            {
                componentType = fieldType.GetGenericArguments()[0];
            }
            else
            {
                componentType = fieldType;
            }
        }

        if (componentType == null)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        float dropdownWidth = 20f;
        float selectWidth = 30f;
        float spacing = 2f;

        float fieldWidth = position.width - selectWidth - dropdownWidth - spacing * 2;

        Rect fieldRect = new Rect(position.x, position.y, fieldWidth, position.height);
        Rect dropdownRect = new Rect(fieldRect.xMax + spacing, position.y, dropdownWidth, position.height);
        Rect selectRect = new Rect(dropdownRect.xMax + spacing, position.y, selectWidth, position.height);

        EditorGUI.PropertyField(fieldRect, property, label);

        if (EditorGUI.DropdownButton(dropdownRect, new GUIContent("v"), FocusType.Passive))
        {
            ShowQuickMenu(property, componentType);
        }

        if (GUI.Button(selectRect, "..."))
        {
            ComponentPrefabSelectorWindow.Show(property, componentType);
        }

        EditorGUI.EndProperty();
    }

    private void ShowQuickMenu(SerializedProperty property, Type componentType)
    {
        if (property == null) return;
        if (componentType == null) return;

        RefreshCache(componentType);

        GenericMenu menu = new GenericMenu();

        bool isNoneSelected = property.objectReferenceValue == null;

        menu.AddItem(new GUIContent("None"), isNoneSelected, () =>
        {
            if (property != null && property.serializedObject != null)
            {
                property.objectReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            }
        });

        menu.AddSeparator("");

        if (cachedPrefabs == null || cachedPrefabs.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No prefabs with " + componentType.Name));
            menu.ShowAsContext();
            return;
        }

        int maxItems = 30;
        int count = cachedPrefabs.Count;
        if (count > maxItems)
        {
            count = maxItems;
        }

        for (int i = 0; i < count; i++)
        {
            if (cachedComponents == null) continue;
            if (i >= cachedComponents.Length) continue;
            if (cachedDisplayNames == null) continue;
            if (i >= cachedDisplayNames.Length) continue;

            Component component = cachedComponents[i];
            string displayName = cachedDisplayNames[i];

            if (component == null) continue;
            if (string.IsNullOrEmpty(displayName)) continue;

            bool isSelected = property.objectReferenceValue == component;

            int capturedIndex = i;
            menu.AddItem(new GUIContent(displayName), isSelected, () =>
            {
                if (property == null) return;
                if (property.serializedObject == null) return;
                if (cachedComponents == null) return;
                if (capturedIndex >= cachedComponents.Length) return;

                property.objectReferenceValue = cachedComponents[capturedIndex];
                property.serializedObject.ApplyModifiedProperties();
            });
        }

        if (cachedPrefabs.Count > maxItems)
        {
            int remaining = cachedPrefabs.Count - maxItems;
            menu.AddDisabledItem(new GUIContent("... and " + remaining + " more (use Browse)"));
        }

        menu.ShowAsContext();
    }

    private void RefreshCache(Type componentType)
    {
        if (componentType == null) return;

        double currentTime = EditorApplication.timeSinceStartup;

        bool typeMatch = cachedType == componentType;
        bool timeValid = currentTime - lastCacheTime < CACHE_DURATION;
        bool cacheExists = cachedPrefabs != null;

        if (cacheExists && typeMatch && timeValid)
        {
            return;
        }

        cachedType = componentType;
        lastCacheTime = currentTime;
        cachedPrefabs = new List<GameObject>();

        List<string> displayNames = new List<string>();
        List<Component> components = new List<Component>();

        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        if (guids == null)
        {
            cachedDisplayNames = new string[0];
            cachedComponents = new Component[0];
            return;
        }

        for (int i = 0; i < guids.Length; i++)
        {
            string guid = guids[i];
            if (string.IsNullOrEmpty(guid)) continue;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            Component component = prefab.GetComponent(componentType);
            if (component == null) continue;

            cachedPrefabs.Add(prefab);
            components.Add(component);
            displayNames.Add(prefab.name);
        }

        cachedDisplayNames = displayNames.ToArray();
        cachedComponents = components.ToArray();
    }
}
