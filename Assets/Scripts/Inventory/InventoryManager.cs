using UnityEngine;
using System;
using System.Collections.Generic;
using Runner.Player.Data;

namespace Runner.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Defaults")]
        [SerializeField] private PlayerPreset defaultPreset;
        [SerializeField] private Katana starterKatana;

        [Header("Available Katanas")]
        [SerializeField] private List<Katana> allKatanas = new List<Katana>();

        private Katana equippedKatana;
        private List<string> ownedKatanaIds = new List<string>();
        private PlayerPreset activePreset;

        public Katana EquippedKatana => equippedKatana;
        public PlayerPreset ActivePreset => activePreset;
        public IReadOnlyList<Katana> AllKatanas => allKatanas;
        public IReadOnlyList<string> OwnedKatanaIds => ownedKatanaIds;

        public event Action<Katana> OnKatanaEquipped;
        public event Action<PlayerPreset> OnPresetChanged;
        public event Action<Katana> OnKatanaUnlocked;

        private const string EQUIPPED_KATANA_KEY = "EquippedKatanaId";
        private const string OWNED_KATANAS_KEY = "OwnedKatanaIds";

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
                Debug.LogWarning($"Cannot equip katana {katana.KatanaName} - not owned");
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

            SaveInventory();

            OnKatanaEquipped?.Invoke(katana);
            OnPresetChanged?.Invoke(activePreset);
        }

        public void UnlockKatana(Katana katana)
        {
            if (katana == null) return;

            if (IsKatanaOwned(katana)) return;

            ownedKatanaIds.Add(katana.Id);
            SaveInventory();

            OnKatanaUnlocked?.Invoke(katana);
        }

        public bool IsKatanaOwned(Katana katana)
        {
            if (katana == null) return false;
            return ownedKatanaIds.Contains(katana.Id);
        }

        public Katana GetKatanaById(string id)
        {
            foreach (var katana in allKatanas)
            {
                if (katana.Id == id)
                {
                    return katana;
                }
            }
            return null;
        }

        public List<Katana> GetOwnedKatanas()
        {
            List<Katana> owned = new List<Katana>();

            foreach (var katana in allKatanas)
            {
                if (IsKatanaOwned(katana))
                {
                    owned.Add(katana);
                }
            }

            return owned;
        }

        private void SaveInventory()
        {
            if (equippedKatana != null)
            {
                PlayerPrefs.SetString(EQUIPPED_KATANA_KEY, equippedKatana.Id);
            }

            string ownedIds = string.Join(",", ownedKatanaIds);
            PlayerPrefs.SetString(OWNED_KATANAS_KEY, ownedIds);

            PlayerPrefs.Save();
        }

        private void LoadInventory()
        {
            string ownedIds = PlayerPrefs.GetString(OWNED_KATANAS_KEY, "");

            if (!string.IsNullOrEmpty(ownedIds))
            {
                ownedKatanaIds = new List<string>(ownedIds.Split(','));
            }

            string equippedId = PlayerPrefs.GetString(EQUIPPED_KATANA_KEY, "");

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