using UnityEngine;
using System.Collections.Generic;

namespace Runner.Inventory
{
    [CreateAssetMenu(fileName = "KatanaDatabase", menuName = "Runner/Katana Database")]
    public class KatanaDatabase : ScriptableObject
    {
        [SerializeField] private List<Katana> katanas = new List<Katana>();

        [Header("Prices")]
        [SerializeField] private int commonPrice = 250;
        [SerializeField] private int rarePrice = 500;
        [SerializeField] private int epicPrice = 1000;
        [SerializeField] private int legendaryPrice = 2000;

        public IReadOnlyList<Katana> AllKatanas => katanas;

        public List<Katana> GetKatanasByRarity(KatanaRarity rarity)
        {
            List<Katana> result = new List<Katana>();

            foreach (var katana in katanas)
            {
                if (katana != null && katana.Rarity == rarity)
                {
                    result.Add(katana);
                }
            }

            return result;
        }

        public Katana GetKatanaById(string id)
        {
            foreach (var katana in katanas)
            {
                if (katana != null && katana.Id == id)
                {
                    return katana;
                }
            }
            return null;
        }

        public int GetRarityPrice(KatanaRarity rarity)
        {
            switch (rarity)
            {
                case KatanaRarity.Common: return commonPrice;
                case KatanaRarity.Rare: return rarePrice;
                case KatanaRarity.Epic: return epicPrice;
                case KatanaRarity.Legendary: return legendaryPrice;
                default: return 0;
            }
        }

        public int GetDirectPurchasePrice(KatanaRarity rarity)
        {
            return GetRarityPrice(rarity) * 2;
        }
    }
}