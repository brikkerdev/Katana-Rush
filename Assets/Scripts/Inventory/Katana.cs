using UnityEngine;
using Runner.Player.Data;

namespace Runner.Inventory
{
    public enum KatanaRarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Challenge
    }

    public enum ChallengeType
    {
        None,
        TotalDistance,
        EnemiesKilled,
        DashesUsed,
        GamesPlayed,
        CoinsCollected
    }

    [System.Serializable]
    public class ChallengeRequirement
    {
        public ChallengeType type;
        public float targetValue;
        public string descriptionKey;
    }

    [CreateAssetMenu(fileName = "Katana", menuName = "Runner/Weapons/Katana")]
    public class Katana : ScriptableObject
    {
        [Header("Info")]
        [SerializeField] private string id;
        [SerializeField] private string nameKey;
        [SerializeField] private string descriptionKey;
        [SerializeField] private Sprite icon;
        [SerializeField] private KatanaRarity rarity = KatanaRarity.Common;

        [Header("Stats")]
        [SerializeField] private PlayerPreset playerPreset;
        [SerializeField][Range(1, 5)] private int dashCount = 3;

        [Header("Visual")]
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private Material bladeMaterial;

        [Header("Effects")]
        [SerializeField] private GameObject slashEffectPrefab;
        [SerializeField] private GameObject trailEffectPrefab;

        [Header("Challenge (Only for Challenge Rarity)")]
        [SerializeField] private ChallengeRequirement challengeRequirement;

        public string Id => id;
        public string NameKey => nameKey;
        public string DescriptionKey => descriptionKey;
        public Sprite Icon => icon;
        public KatanaRarity Rarity => rarity;
        public PlayerPreset PlayerPreset => playerPreset;
        public int DashCount => dashCount;
        public GameObject ModelPrefab => modelPrefab;
        public Material BladeMaterial => bladeMaterial;
        public GameObject SlashEffectPrefab => slashEffectPrefab;
        public GameObject TrailEffectPrefab => trailEffectPrefab;
        public ChallengeRequirement ChallengeRequirement => challengeRequirement;
        public bool IsChallenge => rarity == KatanaRarity.Challenge;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString();
            }
        }
    }
}