#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LocalizationConfig))]
public class LocalizationConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Open Localization Table Editor"))
        {
            LocalizationDatabaseWindow.Open();
        }
    }
}
#endif
