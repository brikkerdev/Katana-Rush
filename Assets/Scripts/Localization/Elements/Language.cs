using UnityEngine;

[CreateAssetMenu(menuName = "Localization/Language")]
public class Language : ScriptableObject
{
    // Stable identifier. Do NOT change after shipping (it’s your save key).
    public string code = "en";
    public string displayName = "English";
}