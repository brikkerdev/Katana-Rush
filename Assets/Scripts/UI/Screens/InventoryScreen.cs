using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Runner.Inventory;
using Runner.Save;

namespace Runner.UI
{
    public class InventoryScreen : UIScreen
    {
        [Header("Preview")]
        [SerializeField] private KatanaPreviewRenderer previewRenderer;
        [SerializeField] private RawImage previewImage;

        [Header("Info Panel")]
        [SerializeField] private TextMeshProUGUI katanaNameText;
        [SerializeField] private LocalizationUIText katanaNameLocalized;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private DashIndicator dashIndicator;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private LocalizationUIText descriptionLocalized;
        [SerializeField] private Image rarityBadge;

        [Header("Challenge Panel")]
        [SerializeField] private GameObject challengePanel;
        [SerializeField] private LocalizationUIText challengeDescriptionLocalized;
        [SerializeField] private ChallengeProgressBar challengeProgressBar;

        [Header("Locked Panel")]
        [SerializeField] private GameObject lockedInfoPanel;
        [SerializeField] private TextMeshProUGUI lockedQuestionMarks;

        [Header("Currency")]
        [SerializeField] private CoinDisplay coinDisplay;

        [Header("Buttons")]
        [SerializeField] private UIButton buyRandomButton;
        [SerializeField] private TextMeshProUGUI buyRandomPriceText;
        [SerializeField] private UIButton buySelectedButton;
        [SerializeField] private TextMeshProUGUI buySelectedPriceText;
        [SerializeField] private UIButton equipButton;
        [SerializeField] private UIButton backButton;

        [Header("Tabs")]
        [SerializeField] private Transform tabContainer;
        [SerializeField] private RarityTab tabPrefab;

        [Header("Grid")]
        [SerializeField] private SwipeableGridView gridView;
        [SerializeField] private Transform gridContainer;
        [SerializeField] private KatanaSlot slotPrefab;
        [SerializeField] private int columnsPerPage = 3;
        [SerializeField] private int rowsPerPage = 3;

        [Header("Roulette")]
        [SerializeField] private KatanaRoulette roulette;
        [SerializeField] private GameObject rouletteContainer;
        [SerializeField] private GameObject normalGridContainer;

        [Header("Rarity Colors")]
        [SerializeField] private Color commonColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color rareColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color epicColor = new Color(0.7f, 0.3f, 1f);
        [SerializeField] private Color legendaryColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color challengeColor = new Color(1f, 0.3f, 0.3f);

        [Header("Localization Keys")]
        [SerializeField] private string commonKey = "rarity_common";
        [SerializeField] private string rareKey = "rarity_rare";
        [SerializeField] private string epicKey = "rarity_epic";
        [SerializeField] private string legendaryKey = "rarity_legendary";
        [SerializeField] private string challengeKey = "rarity_challenge";

        private KatanaRarity currentRarity = KatanaRarity.Common;
        private List<RarityTab> tabs = new List<RarityTab>();
        private List<KatanaSlot> currentSlots = new List<KatanaSlot>();
        private Katana selectedKatana;
        private bool isRouletteMode;

        private readonly KatanaRarity[] rarityOrder = new KatanaRarity[]
        {
            KatanaRarity.Common,
            KatanaRarity.Rare,
            KatanaRarity.Epic,
            KatanaRarity.Legendary,
            KatanaRarity.Challenge
        };

        protected override void Awake()
        {
            base.Awake();
            screenType = ScreenType.Inventory;

            SetupButtons();
            SetupTabs();

            if (roulette != null)
            {
                roulette.OnRouletteComplete += OnRouletteComplete;
            }
        }

        private void SetupButtons()
        {
            if (buyRandomButton != null)
                buyRandomButton.OnClick += OnBuyRandomClicked;

            if (buySelectedButton != null)
                buySelectedButton.OnClick += OnBuySelectedClicked;

            if (equipButton != null)
                equipButton.OnClick += OnEquipClicked;

            if (backButton != null)
                backButton.OnClick += OnBackClicked;
        }

        private void SetupTabs()
        {
            if (tabContainer == null || tabPrefab == null) return;

            for (int i = 0; i < rarityOrder.Length; i++)
            {
                RarityTab tab = Instantiate(tabPrefab, tabContainer);
                string label = GetRarityName(rarityOrder[i]);
                tab.Setup(i, label);
                tab.OnTabClicked += OnTabClicked;
                tabs.Add(tab);
            }
        }

        protected override void OnShow()
        {
            base.OnShow();

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.TryUnlockCompletedChallenges();
            }

            SelectTab(0);
            UpdateCoinDisplay();

            if (rouletteContainer != null)
                rouletteContainer.SetActive(false);

            if (normalGridContainer != null)
                normalGridContainer.SetActive(true);

            isRouletteMode = false;
        }

        protected override void OnHide()
        {
            base.OnHide();

            if (roulette != null)
            {
                roulette.Reset();
            }
        }

        private void OnTabClicked(int index)
        {
            SelectTab(index);
        }

        private void SelectTab(int index)
        {
            if (index < 0 || index >= rarityOrder.Length) return;

            currentRarity = rarityOrder[index];

            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].SetSelected(i == index);
            }

            RefreshGrid();
            UpdateButtons();
        }

        private void RefreshGrid()
        {
            ClearGrid();

            if (InventoryManager.Instance == null) return;

            var katanas = InventoryManager.Instance.GetKatanasByRarity(currentRarity);

            foreach (var katana in katanas)
            {
                KatanaSlot slot = Instantiate(slotPrefab, gridContainer);
                bool owned = InventoryManager.Instance.IsKatanaOwned(katana);
                bool equipped = InventoryManager.Instance.EquippedKatana == katana;

                slot.Setup(katana, owned, equipped);
                slot.OnSlotClicked += OnSlotClicked;
                currentSlots.Add(slot);
            }

            int slotsPerPage = columnsPerPage * rowsPerPage;
            int pageCount = Mathf.CeilToInt((float)katanas.Count / slotsPerPage);
            pageCount = Mathf.Max(1, pageCount);

            if (gridView != null)
            {
                gridView.SetPageCount(pageCount);
                gridView.GoToPage(0, true);
            }

            if (currentSlots.Count > 0)
            {
                SelectKatana(currentSlots[0]);
            }
            else
            {
                selectedKatana = null;
                UpdateInfoPanel();
            }
        }

        private void ClearGrid()
        {
            foreach (var slot in currentSlots)
            {
                if (slot != null)
                {
                    slot.OnSlotClicked -= OnSlotClicked;
                    Destroy(slot.gameObject);
                }
            }
            currentSlots.Clear();
        }

        private void OnSlotClicked(KatanaSlot slot)
        {
            SelectKatana(slot);
        }

        private void SelectKatana(KatanaSlot slot)
        {
            foreach (var s in currentSlots)
            {
                s.SetSelected(s == slot);
            }

            selectedKatana = slot.Katana;
            UpdateInfoPanel();
            UpdateButtons();
        }

        private void UpdateInfoPanel()
        {
            if (selectedKatana == null)
            {
                ClearInfoPanel();
                return;
            }

            bool owned = InventoryManager.Instance != null &&
                         InventoryManager.Instance.IsKatanaOwned(selectedKatana);

            if (previewRenderer != null)
            {
                previewRenderer.ShowKatana(selectedKatana, !owned);
            }

            if (previewImage != null && previewRenderer != null)
            {
                previewImage.texture = previewRenderer.RenderTexture;
            }

            if (owned)
            {
                ShowOwnedKatanaInfo();
            }
            else if (selectedKatana.IsChallenge)
            {
                ShowChallengeKatanaInfo();
            }
            else
            {
                ShowLockedKatanaInfo();
            }
        }

        private void ShowOwnedKatanaInfo()
        {
            if (lockedInfoPanel != null)
                lockedInfoPanel.SetActive(false);

            if (challengePanel != null)
                challengePanel.SetActive(false);

            if (katanaNameLocalized != null)
            {
                katanaNameLocalized.Key = selectedKatana.NameKey;
                katanaNameLocalized.gameObject.SetActive(true);
            }
            else if (katanaNameText != null)
            {
                katanaNameText.text = LocalizationController.Singleton?.GetText(selectedKatana.NameKey)
                                      ?? selectedKatana.NameKey;
            }

            if (rarityText != null)
            {
                rarityText.text = GetRarityName(selectedKatana.Rarity);
                rarityText.color = GetRarityColor(selectedKatana.Rarity);
            }

            if (rarityBadge != null)
            {
                rarityBadge.color = GetRarityColor(selectedKatana.Rarity);
            }

            if (dashIndicator != null)
            {
                dashIndicator.SetDashCount(selectedKatana.DashCount);
            }

            if (descriptionLocalized != null)
            {
                descriptionLocalized.Key = selectedKatana.DescriptionKey;
                descriptionLocalized.gameObject.SetActive(true);
            }
            else if (descriptionText != null)
            {
                descriptionText.text = LocalizationController.Singleton?.GetText(selectedKatana.DescriptionKey)
                                       ?? selectedKatana.DescriptionKey;
            }
        }

        private void ShowLockedKatanaInfo()
        {
            if (challengePanel != null)
                challengePanel.SetActive(false);

            if (lockedInfoPanel != null)
                lockedInfoPanel.SetActive(true);

            if (katanaNameText != null)
            {
                katanaNameText.text = "???";
            }

            if (katanaNameLocalized != null)
            {
                katanaNameLocalized.gameObject.SetActive(false);
            }

            if (rarityText != null)
            {
                rarityText.text = GetRarityName(selectedKatana.Rarity);
                rarityText.color = GetRarityColor(selectedKatana.Rarity);
            }

            if (dashIndicator != null)
            {
                dashIndicator.SetHidden();
            }

            if (descriptionText != null)
            {
                descriptionText.text = "???";
            }

            if (descriptionLocalized != null)
            {
                descriptionLocalized.gameObject.SetActive(false);
            }
        }

        private void ShowChallengeKatanaInfo()
        {
            if (lockedInfoPanel != null)
                lockedInfoPanel.SetActive(false);

            if (challengePanel != null)
                challengePanel.SetActive(true);

            if (katanaNameLocalized != null)
            {
                katanaNameLocalized.Key = selectedKatana.NameKey;
                katanaNameLocalized.gameObject.SetActive(true);
            }
            else if (katanaNameText != null)
            {
                katanaNameText.text = LocalizationController.Singleton?.GetText(selectedKatana.NameKey)
                                      ?? selectedKatana.NameKey;
            }

            if (rarityText != null)
            {
                rarityText.text = GetRarityName(KatanaRarity.Challenge);
                rarityText.color = challengeColor;
            }

            if (dashIndicator != null)
            {
                dashIndicator.SetHidden();
            }

            if (descriptionLocalized != null)
            {
                descriptionLocalized.gameObject.SetActive(false);
            }

            if (selectedKatana.ChallengeRequirement != null)
            {
                if (challengeDescriptionLocalized != null)
                {
                    challengeDescriptionLocalized.Key = selectedKatana.ChallengeRequirement.descriptionKey;
                }

                if (challengeProgressBar != null)
                {
                    float current = SaveManager.GetChallengeValue(selectedKatana.ChallengeRequirement.type);
                    float target = selectedKatana.ChallengeRequirement.targetValue;
                    challengeProgressBar.SetProgress(current, target);
                }
            }
        }

        private void ClearInfoPanel()
        {
            if (katanaNameText != null)
                katanaNameText.text = "";

            if (descriptionText != null)
                descriptionText.text = "";

            if (dashIndicator != null)
                dashIndicator.SetDashCount(0);

            if (previewRenderer != null)
                previewRenderer.ClearCurrentKatana();
        }

        private void UpdateButtons()
        {
            bool isChallenge = currentRarity == KatanaRarity.Challenge;
            int randomPrice = InventoryManager.Instance?.Database?.GetRarityPrice(currentRarity) ?? 0;
            int directPrice = InventoryManager.Instance?.Database?.GetDirectPurchasePrice(currentRarity) ?? 0;
            int coins = SaveManager.GetCoins();

            var unowned = InventoryManager.Instance?.GetUnownedKatanasByRarity(currentRarity);
            bool hasUnowned = unowned != null && unowned.Count > 0;

            bool selectedOwned = selectedKatana != null &&
                                 InventoryManager.Instance != null &&
                                 InventoryManager.Instance.IsKatanaOwned(selectedKatana);

            bool selectedEquipped = selectedKatana != null &&
                                    InventoryManager.Instance != null &&
                                    InventoryManager.Instance.EquippedKatana == selectedKatana;

            if (buyRandomButton != null)
            {
                bool canBuyRandom = !isChallenge && hasUnowned && coins >= randomPrice;
                buyRandomButton.SetInteractable(canBuyRandom);
                buyRandomButton.gameObject.SetActive(!isChallenge);
            }

            if (buyRandomPriceText != null)
            {
                buyRandomPriceText.text = randomPrice.ToString();
                buyRandomPriceText.color = coins >= randomPrice ? Color.white : Color.red;
            }

            if (buySelectedButton != null)
            {
                bool canBuySelected = !isChallenge && selectedKatana != null &&
                                      !selectedOwned && coins >= directPrice;
                buySelectedButton.SetInteractable(canBuySelected);
                buySelectedButton.gameObject.SetActive(!isChallenge && selectedKatana != null && !selectedOwned);
            }

            if (buySelectedPriceText != null)
            {
                buySelectedPriceText.text = directPrice.ToString();
                buySelectedPriceText.color = coins >= directPrice ? Color.white : Color.red;
            }

            if (equipButton != null)
            {
                bool canEquip = selectedOwned && !selectedEquipped;
                equipButton.SetInteractable(canEquip);
                equipButton.gameObject.SetActive(selectedOwned);
            }
        }

        private void UpdateCoinDisplay()
        {
            if (coinDisplay != null)
            {
                coinDisplay.UpdateDisplay();
            }
        }

        private void OnBuyRandomClicked()
        {
            if (isRouletteMode) return;
            if (currentRarity == KatanaRarity.Challenge) return;

            var unowned = InventoryManager.Instance?.GetUnownedKatanasByRarity(currentRarity);
            if (unowned == null || unowned.Count == 0) return;

            int price = InventoryManager.Instance.Database.GetRarityPrice(currentRarity);
            if (!SaveManager.SpendCoins(price)) return;

            Katana result = unowned[Random.Range(0, unowned.Count)];

            StartRoulette(result);
        }

        private void StartRoulette(Katana result)
        {
            isRouletteMode = true;

            if (normalGridContainer != null)
                normalGridContainer.SetActive(false);

            if (rouletteContainer != null)
                rouletteContainer.SetActive(true);

            SetButtonsInteractable(false);

            var allOfRarity = InventoryManager.Instance.GetKatanasByRarity(currentRarity);

            if (roulette != null)
            {
                roulette.StartRoulette(allOfRarity, result, slotPrefab);
            }
        }

        private void OnRouletteComplete(Katana result)
        {
            InventoryManager.Instance?.UnlockKatana(result);

            StartCoroutine(EndRouletteRoutine(result));
        }

        private IEnumerator EndRouletteRoutine(Katana result)
        {
            yield return new WaitForSecondsRealtime(1.5f);

            isRouletteMode = false;

            if (rouletteContainer != null)
                rouletteContainer.SetActive(false);

            if (normalGridContainer != null)
                normalGridContainer.SetActive(true);

            roulette?.Reset();

            RefreshGrid();
            UpdateCoinDisplay();
            SetButtonsInteractable(true);

            foreach (var slot in currentSlots)
            {
                if (slot.Katana == result)
                {
                    SelectKatana(slot);
                    break;
                }
            }
        }

        private void OnBuySelectedClicked()
        {
            if (selectedKatana == null) return;
            if (currentRarity == KatanaRarity.Challenge) return;
            if (InventoryManager.Instance == null) return;

            if (InventoryManager.Instance.TryPurchaseDirect(selectedKatana, out bool success))
            {
                RefreshGrid();
                UpdateCoinDisplay();

                foreach (var slot in currentSlots)
                {
                    if (slot.Katana == selectedKatana)
                    {
                        SelectKatana(slot);
                        break;
                    }
                }
            }
        }

        private void OnEquipClicked()
        {
            if (selectedKatana == null) return;
            if (InventoryManager.Instance == null) return;

            InventoryManager.Instance.EquipKatana(selectedKatana);
            RefreshGrid();
        }

        private void OnBackClicked()
        {
            UIManager.Instance?.ShowScreen(ScreenType.MainMenu);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (buyRandomButton != null)
                buyRandomButton.SetInteractable(interactable && !isRouletteMode);

            if (buySelectedButton != null)
                buySelectedButton.SetInteractable(interactable && !isRouletteMode);

            if (equipButton != null)
                equipButton.SetInteractable(interactable);

            if (backButton != null)
                backButton.SetInteractable(interactable);

            foreach (var tab in tabs)
            {
                if (tab != null && tab.GetComponent<Button>() != null)
                {
                    tab.GetComponent<Button>().interactable = interactable;
                }
            }
        }

        private string GetRarityName(KatanaRarity rarity)
        {
            string key = rarity switch
            {
                KatanaRarity.Common => commonKey,
                KatanaRarity.Rare => rareKey,
                KatanaRarity.Epic => epicKey,
                KatanaRarity.Legendary => legendaryKey,
                KatanaRarity.Challenge => challengeKey,
                _ => commonKey
            };

            return LocalizationController.Singleton?.GetText(key) ?? rarity.ToString();
        }

        private Color GetRarityColor(KatanaRarity rarity)
        {
            return rarity switch
            {
                KatanaRarity.Common => commonColor,
                KatanaRarity.Rare => rareColor,
                KatanaRarity.Epic => epicColor,
                KatanaRarity.Legendary => legendaryColor,
                KatanaRarity.Challenge => challengeColor,
                _ => commonColor
            };
        }

        private void OnDestroy()
        {
            if (buyRandomButton != null)
                buyRandomButton.OnClick -= OnBuyRandomClicked;

            if (buySelectedButton != null)
                buySelectedButton.OnClick -= OnBuySelectedClicked;

            if (equipButton != null)
                equipButton.OnClick -= OnEquipClicked;

            if (backButton != null)
                backButton.OnClick -= OnBackClicked;

            if (roulette != null)
                roulette.OnRouletteComplete -= OnRouletteComplete;

            foreach (var tab in tabs)
            {
                if (tab != null)
                {
                    tab.OnTabClicked -= OnTabClicked;
                }
            }

            ClearGrid();
        }
    }
}