using UnityEngine;
using Runner.Player.Data;

namespace Runner.Inventory
{
    public enum KatanaRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [CreateAssetMenu(fileName = "Katana", menuName = "Runner/Weapons/Katana")]
    public class Katana : ScriptableObject
    {
        [Header("Info")]
        [SerializeField] private string id;
        [SerializeField] private string katanaName;
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private KatanaRarity rarity = KatanaRarity.Common;

        [Header("Stats")]
        [SerializeField] private PlayerPreset playerPreset;

        [Header("Visual")]
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private Material bladeMaterial;

        [Header("Effects")]
        [SerializeField] private GameObject slashEffectPrefab;
        [SerializeField] private GameObject trailEffectPrefab;

        public string Id => id;
        public string KatanaName => katanaName;
        public string Description => description;
        public Sprite Icon => icon;
        public KatanaRarity Rarity => rarity;
        public PlayerPreset PlayerPreset => playerPreset;
        public GameObject ModelPrefab => modelPrefab;
        public Material BladeMaterial => bladeMaterial;
        public GameObject SlashEffectPrefab => slashEffectPrefab;
        public GameObject TrailEffectPrefab => trailEffectPrefab;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString();
            }
        }
    }
}