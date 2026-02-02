using UnityEngine;
using System;
using System.Collections.Generic;
using Runner.Player.Data;
using Runner.Save;

namespace Runner.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Database")]
        [SerializeField] private KatanaDatabase katanaDatabase;

        [Header("Defaults")]
        [SerializeField] private PlayerPreset defaultPreset;
        [SerializeField] private Katana starterKatana;

        private Katana equippedKatana;
        private PlayerPreset activePreset;

        public Katana EquippedKatana => equippedKatana;
        public PlayerPreset ActivePreset => activePreset;
        public KatanaDatabase Database => katanaDatabase;

        public event Action<Katana> OnKatanaEquipped;
        public event Action<PlayerPreset> OnPresetChanged;
        public event Action<Katana> OnKatanaUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            LoadInventory();

            if (equippedKatana == null)
            {
                if (starterKatana != null)
                {
                    UnlockKatana(starterKatana);
                    EquipKatana(starterKatana);
                }
                else
                {
                    activePreset = defaultPreset != null ? defaultPreset : PlayerPreset.CreateDefault();
                }
            }
        }

        public void EquipKatana(Katana katana)
        {
            if (katana == null) return;

            if (!IsKatanaOwned(katana))
            {
                return;
            }

            equippedKatana = katana;

            if (katana.PlayerPreset != null)
            {
                activePreset = katana.PlayerPreset;
            }
            else
            {
                activePreset = defaultPreset != null ? defaultPreset : PlayerPreset.CreateDefault();
            }

            SaveManager.SetEquippedKatana(katana.Id);

            OnKatanaEquipped?.Invoke(katana);
            OnPresetChanged?.Invoke(activePreset);
        }

        public void UnlockKatana(Katana katana)
        {
            if (katana == null) return;

            if (IsKatanaOwned(katana)) return;

            SaveManager.UnlockKatana(katana.Id);

            OnKatanaUnlocked?.Invoke(katana);
        }

        public bool IsKatanaOwned(Katana katana)
        {
            if (katana == null) return false;
            return SaveManager.IsKatanaOwned(katana.Id);
        }

        public Katana GetKatanaById(string id)
        {
            return katanaDatabase != null ? katanaDatabase.GetKatanaById(id) : null;
        }

        public List<Katana> GetKatanasByRarity(KatanaRarity rarity)
        {
            return katanaDatabase != null ? katanaDatabase.GetKatanasByRarity(rarity) : new List<Katana>();
        }

        public List<Katana> GetOwnedKatanas()
        {
            List<Katana> owned = new List<Katana>();

            if (katanaDatabase == null) return owned;

            foreach (var katana in katanaDatabase.AllKatanas)
            {
                if (IsKatanaOwned(katana))
                {
                    owned.Add(katana);
                }
            }

            return owned;
        }

        public List<Katana> GetUnownedKatanasByRarity(KatanaRarity rarity)
        {
            List<Katana> unowned = new List<Katana>();

            var katanas = GetKatanasByRarity(rarity);

            foreach (var katana in katanas)
            {
                if (!IsKatanaOwned(katana))
                {
                    unowned.Add(katana);
                }
            }

            return unowned;
        }

        public bool CanAfford(int price)
        {
            return SaveManager.GetCoins() >= price;
        }

        public bool TryPurchaseRandom(KatanaRarity rarity, out Katana result)
        {
            result = null;

            if (rarity == KatanaRarity.Challenge) return false;

            int price = katanaDatabase.GetRarityPrice(rarity);

            if (!CanAfford(price)) return false;

            var unowned = GetUnownedKatanasByRarity(rarity);

            if (unowned.Count == 0) return false;

            SaveManager.SpendCoins(price);

            result = unowned[UnityEngine.Random.Range(0, unowned.Count)];
            UnlockKatana(result);

            return true;
        }

        public bool TryPurchaseDirect(Katana katana, out bool success)
        {
            success = false;

            if (katana == null) return false;
            if (katana.Rarity == KatanaRarity.Challenge) return false;
            if (IsKatanaOwned(katana)) return false;

            int price = katanaDatabase.GetDirectPurchasePrice(katana.Rarity);

            if (!CanAfford(price)) return false;

            SaveManager.SpendCoins(price);
            UnlockKatana(katana);
            success = true;

            return true;
        }

        public bool CheckChallengeComplete(Katana katana)
        {
            if (katana == null || !katana.IsChallenge) return false;
            if (katana.ChallengeRequirement == null) return false;

            float current = SaveManager.GetChallengeValue(katana.ChallengeRequirement.type);
            return current >= katana.ChallengeRequirement.targetValue;
        }

        public float GetChallengeProgress(Katana katana)
        {
            if (katana == null || !katana.IsChallenge) return 0f;
            if (katana.ChallengeRequirement == null) return 0f;

            float current = SaveManager.GetChallengeValue(katana.ChallengeRequirement.type);
            float target = katana.ChallengeRequirement.targetValue;

            return Mathf.Clamp01(current / target);
        }

        public void TryUnlockCompletedChallenges()
        {
            var challengeKatanas = GetKatanasByRarity(KatanaRarity.Challenge);

            foreach (var katana in challengeKatanas)
            {
                if (!IsKatanaOwned(katana) && CheckChallengeComplete(katana))
                {
                    UnlockKatana(katana);
                }
            }
        }

        private void LoadInventory()
        {
            string equippedId = SaveManager.GetEquippedKatanaId();

            if (!string.IsNullOrEmpty(equippedId))
            {
                Katana katana = GetKatanaById(equippedId);

                if (katana != null && IsKatanaOwned(katana))
                {
                    equippedKatana = katana;
                    activePreset = katana.PlayerPreset != null ? katana.PlayerPreset : defaultPreset;
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}