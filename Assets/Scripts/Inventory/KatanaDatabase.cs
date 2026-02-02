using UnityEngine;
using System.Collections.Generic;

namespace Runner.Inventory
{
    [CreateAssetMenu(fileName = "KatanaDatabase", menuName = "Runner/Katana Database")]
    public class KatanaDatabase : ScriptableObject
    {
        [SerializeField] private List<Katana> katanas = new List<Katana>();

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
                case KatanaRarity.Common: return 250;
                case KatanaRarity.Rare: return 500;
                case KatanaRarity.Epic: return 1000;
                case KatanaRarity.Legendary: return 2000;
                default: return 0;
            }
        }

        public int GetDirectPurchasePrice(KatanaRarity rarity)
        {
            return GetRarityPrice(rarity) * 2;
        }
    }
}