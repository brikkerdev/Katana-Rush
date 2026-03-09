using UnityEngine;

[CreateAssetMenu(menuName = "Localization/Config")]
public class LocalizationConfig : ScriptableObject
{
    [Tooltip("Folder name relative to StreamingAssets (e.g. 'Localization')")]
    public string jsonFolderPath = "Localization";
}
