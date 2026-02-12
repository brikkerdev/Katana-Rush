using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Runner.Core;
using Runner.Inventory;
using Runner.Save;
using Runner.Input;

namespace Runner.UI
{
    public class InventoryScreen : UIScreen
    {
        [Header("Preview")]
        [SerializeField] private KatanaPreviewRenderer previewRenderer;
        [SerializeField] private RawImage previewImage;

        [Header("Pages")]
        [SerializeField] private SwipePagesView pagesView;
        [SerializeField] private List<RectTransform> rarityPages = new List<RectTransform>();
        [SerializeField] private List<Transform> rarityGrids = new List<Transform>();

        [Header("Tabs")]
        [SerializeField] private Transform tabContainer;
        [SerializeField] private RarityTab tabPrefab;

        [Header("Info Panel - Owned")]
        [SerializeField] private GameObject ownedInfoPanel;
        [SerializeField] private TextMeshProUGUI katanaNameText;
        [SerializeField] private LocalizationUIText katanaNameLocalized;
        [SerializeField] private UICircularStaminaBar dashBar;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private LocalizationUIText descriptionLocalized;

        [Header("Info Panel - Locked")]
        [SerializeField] private GameObject lockedInfoPanel;

        [Header("Info Panel - Challenge")]
        [SerializeField] private GameObject challengeInfoPanel;
        [SerializeField] private LocalizationUIText challengeDescriptionLocalized;
        [SerializeField] private ChallengeProgressBar challengeProgressBar;

        [Header("Currency")]
        [SerializeField] private CoinDisplay coinDisplay;

        [Header("Buttons - Random")]
        [SerializeField] private UIButton buyRandomButton;
        [SerializeField] private TextMeshProUGUI buyRandomLabelText;
        [SerializeField] private TextMeshProUGUI buyRandomPriceText;

        [Header("Buttons - Specific")]
        [SerializeField] private UIButton buySpecificButton;
        [SerializeField] private TextMeshProUGUI buySpecificLabelText;
        [SerializeField] private TextMeshProUGUI buySpecificPriceText;

        [Header("Buttons")]
        [SerializeField] private UIButton backButton;

        [Header("Grid Settings")]
        [SerializeField] private KatanaSlot slotPrefab;
        [SerializeField] private KatanaSlot emptySlotPrefab;
        [SerializeField] private int slotsPerPage = 9;

        [Header("Roulette")]
        [SerializeField] private GridRouletteAnimator rouletteAnimator;

        [Header("Rarity Colors")]
        [SerializeField] private Color commonColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color rareColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color epicColor = new Color(0.7f, 0.3f, 1f);
        [SerializeField] private Color legendaryColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color challengeColor = new Color(1f, 0.3f, 0.3f);

        [Header("Localization Keys")]
        [SerializeField] private string buyRandomKey = "ui_inventory_buy_random";
        [SerializeField] private string buyNowKey = "ui_inventory_buy_now";

        private readonly KatanaRarity[] rarityOrder =
        {
            KatanaRarity.Common,
            KatanaRarity.Rare,
            KatanaRarity.Epic,
            KatanaRarity.Legendary,
            KatanaRarity.Challenge
        };

        private readonly List<RarityTab> tabs = new List<RarityTab>();
        private readonly Dictionary<KatanaRarity, List<KatanaSlot>> slotsByRarity = new Dictionary<KatanaRarity, List<KatanaSlot>>();

        private int currentPageIndex;
        private KatanaRarity currentRarity = KatanaRarity.Common;
        private KatanaSlot selectedSlot;
        private Katana selectedKatana;
        private bool isProcessing;

        protected override void Awake()
        {
            base.Awake();
            screenType = ScreenType.Inventory;

            HideAllInfoPanels();

            if (previewImage != null)
                previewImage.gameObject.SetActive(false);

            SetupButtons();
            SetupTabs();
            SetupRarityMaps();

            if (rouletteAnimator != null)
                rouletteAnimator.OnRouletteComplete += OnRouletteComplete;

            if (pagesView != null)
                pagesView.OnPageChanged += OnPageChanged;
        }

        private void SetupRarityMaps()
        {
            slotsByRarity.Clear();
            for (int i = 0; i < rarityOrder.Length; i++)
                slotsByRarity[rarityOrder[i]] = new List<KatanaSlot>();
        }

        private void SetupButtons()
        {
            if (buyRandomButton != null)
                buyRandomButton.OnClick += OnBuyRandomClicked;

            if (buySpecificButton != null)
                buySpecificButton.OnClick += OnBuySpecificClicked;

            if (backButton != null)
                backButton.OnClick += OnBackClicked;
        }

        private void SetupTabs()
        {
            if (tabContainer == null || tabPrefab == null) return;

            for (int i = 0; i < rarityOrder.Length; i++)
            {
                var tab = Instantiate(tabPrefab, tabContainer);
                tab.Setup(i, GetRarityColor(rarityOrder[i]));
                tab.OnTabClicked += OnTabClicked;
                tabs.Add(tab);
            }
        }

        protected override void OnShow()
        {
            base.OnShow();

            InputReader.Instance?.DisableGameplayInput();
            InventoryManager.Instance?.TryUnlockCompletedChallenges();

            Canvas.ForceUpdateCanvases();

            if (pagesView != null && rarityPages != null && rarityPages.Count > 0)
            {
                pagesView.SetPages(rarityPages, true);
                pagesView.Rebuild();
            }

            RefreshAllGrids();

            selectedSlot = null;
            selectedKatana = null;

            SelectEquippedKatana();

            UpdateCoinDisplay();
            UpdateButtons();

            if (previewImage != null)
                previewImage.gameObject.SetActive(true);

            isProcessing = false;
        }

        protected override void OnHide()
        {
            base.OnHide();

            InputReader.Instance?.EnableGameplayInput();
            rouletteAnimator?.Cancel();

            if (selectedSlot != null)
            {
                selectedSlot.SetSelected(false);
                selectedSlot = null;
            }

            selectedKatana = null;

            if (previewImage != null)
                previewImage.gameObject.SetActive(false);
        }

        private void OnPageChanged(int index)
        {
            currentPageIndex = Mathf.Clamp(index, 0, rarityOrder.Length - 1);
            currentRarity = rarityOrder[currentPageIndex];

            for (int i = 0; i < tabs.Count; i++)
                tabs[i].SetSelected(i == currentPageIndex);

            UpdateButtons();
        }

        private void OnTabClicked(int index)
        {
            if (isProcessing) return;
            Game.Instance?.Sound?.PlayTabSwitch();
            SelectPageByIndex(index, false);
        }

        private void SelectPageByIndex(int index, bool instant)
        {
            index = Mathf.Clamp(index, 0, rarityOrder.Length - 1);

            if (pagesView != null)
                pagesView.GoToPage(index, instant);
            else
                OnPageChanged(index);
        }

        private void SelectEquippedKatana()
        {
            if (InventoryManager.Instance == null)
            {
                SelectPageByIndex(0, true);
                HideAllInfoPanels();
                return;
            }

            Katana equipped = InventoryManager.Instance.EquippedKatana;

            if (equipped == null)
            {
                SelectPageByIndex(0, true);
                SelectFirstOwnedKatana();
                return;
            }

            int pageIndex = GetRarityPageIndex(equipped.Rarity);
            SelectPageByIndex(pageIndex, true);

            KatanaSlot equippedSlot = FindSlotForKatana(equipped);
            if (equippedSlot != null) SelectSlot(equippedSlot);
            else DisplayKatana(equipped);
        }

        private int GetRarityPageIndex(KatanaRarity rarity)
        {
            for (int i = 0; i < rarityOrder.Length; i++)
                if (rarityOrder[i] == rarity) return i;
            return 0;
        }

        private KatanaSlot FindSlotForKatana(Katana katana)
        {
            if (katana == null) return null;

            if (slotsByRarity.TryGetValue(katana.Rarity, out var slots))
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot != null && !slot.IsEmpty && slot.Katana == katana)
                        return slot;
                }
            }

            return null;
        }

        private void SelectFirstOwnedKatana()
        {
            if (InventoryManager.Instance == null)
            {
                HideAllInfoPanels();
                return;
            }

            for (int i = 0; i < rarityOrder.Length; i++)
            {
                var slots = slotsByRarity[rarityOrder[i]];
                for (int j = 0; j < slots.Count; j++)
                {
                    var slot = slots[j];
                    if (slot != null && !slot.IsEmpty && slot.IsOwned)
                    {
                        SelectPageByIndex(i, true);
                        SelectSlot(slot);
                        return;
                    }
                }
            }

            HideAllInfoPanels();
        }

        private Transform GetGridForIndex(int i)
        {
            if (rarityGrids != null && i >= 0 && i < rarityGrids.Count && rarityGrids[i] != null)
                return rarityGrids[i];

            if (rarityPages != null && i >= 0 && i < rarityPages.Count && rarityPages[i] != null)
                return rarityPages[i];

            return null;
        }

        private void RefreshAllGrids()
        {
            ClearAllGrids();
            if (InventoryManager.Instance == null) return;

            for (int i = 0; i < rarityOrder.Length; i++)
            {
                KatanaRarity rarity = rarityOrder[i];
                var katanas = InventoryManager.Instance.GetKatanasByRarity(rarity);
                Transform gridContainer = GetGridForIndex(i);
                if (gridContainer == null) continue;

                int katanaIndex = 0;

                for (int slotIndex = 0; slotIndex < slotsPerPage; slotIndex++)
                {
                    if (katanaIndex < katanas.Count)
                    {
                        Katana katana = katanas[katanaIndex];
                        KatanaSlot slot = Instantiate(slotPrefab, gridContainer);
                        bool owned = InventoryManager.Instance.IsKatanaOwned(katana);
                        bool equipped = InventoryManager.Instance.EquippedKatana == katana;

                        slot.Setup(katana, owned, equipped);
                        slot.OnSlotClicked += OnSlotClicked;
                        slotsByRarity[rarity].Add(slot);

                        katanaIndex++;
                    }
                    else
                    {
                        CreateEmptySlot(gridContainer, rarity);
                    }
                }
            }
        }

        private void CreateEmptySlot(Transform parent, KatanaRarity rarity)
        {
            if (emptySlotPrefab != null)
            {
                KatanaSlot emptySlot = Instantiate(emptySlotPrefab, parent);
                emptySlot.SetupEmpty();
                slotsByRarity[rarity].Add(emptySlot);
                return;
            }

            var emptyObj = new GameObject("EmptySlot");
            emptyObj.transform.SetParent(parent, false);
            var rt = emptyObj.AddComponent<RectTransform>();
            rt.localScale = Vector3.one;
            var img = emptyObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);
        }

        private void ClearAllGrids()
        {
            foreach (var kvp in slotsByRarity)
            {
                foreach (var slot in kvp.Value)
                {
                    if (slot != null)
                    {
                        slot.OnSlotClicked -= OnSlotClicked;
                        Destroy(slot.gameObject);
                    }
                }
                kvp.Value.Clear();
            }

            for (int i = 0; i < rarityOrder.Length; i++)
            {
                Transform grid = GetGridForIndex(i);
                if (grid == null) continue;

                for (int c = grid.childCount - 1; c >= 0; c--)
                    Destroy(grid.GetChild(c).gameObject);
            }
        }

        private void OnSlotClicked(KatanaSlot slot)
        {
            if (isProcessing) return;
            if (slot == null) return;
            if (slot.IsEmpty) return;

            if (slot == selectedSlot && selectedKatana != null)
            {
                if (slot.IsOwned)
                {
                    EquipAndClose(slot.Katana);
                }
                return;
            }

            SelectSlot(slot);
        }

        private void SelectSlot(KatanaSlot slot)
        {
            if (selectedSlot != null)
                selectedSlot.SetSelected(false);

            selectedSlot = slot;
            selectedKatana = slot.Katana;

            slot.SetSelected(true, true);
            DisplayKatana(slot.Katana);
            UpdateButtons();
        }

        private void EquipAndClose(Katana katana)
        {
            if (katana == null) return;
            if (InventoryManager.Instance == null) return;

            InventoryManager.Instance.EquipKatana(katana);
            Game.Instance?.Sound?.PlayEquip();
            UIManager.Instance?.ShowScreen(ScreenType.MainMenu);
        }

        private void DisplayKatana(Katana katana)
        {
            if (katana == null)
            {
                HideAllInfoPanels();
                return;
            }

            bool owned = InventoryManager.Instance != null && InventoryManager.Instance.IsKatanaOwned(katana);

            if (previewRenderer != null)
                previewRenderer.ShowKatana(katana, !owned);

            if (previewImage != null && previewRenderer != null)
                previewImage.texture = previewRenderer.RenderTexture;

            if (owned) ShowOwnedInfo(katana);
            else if (katana.IsChallenge) ShowChallengeInfo(katana);
            else ShowLockedInfo(katana);
        }

        private void HideAllInfoPanels()
        {
            if (ownedInfoPanel != null) ownedInfoPanel.SetActive(false);
            if (lockedInfoPanel != null) lockedInfoPanel.SetActive(false);
            if (challengeInfoPanel != null) challengeInfoPanel.SetActive(false);
        }

        private void ShowOwnedInfo(Katana katana)
        {
            HideAllInfoPanels();
            if (ownedInfoPanel != null) ownedInfoPanel.SetActive(true);

            if (katanaNameLocalized != null) katanaNameLocalized.Key = katana.NameKey;
            else if (katanaNameText != null) katanaNameText.text = GetLocalizedText(katana.NameKey);

            if (dashBar != null)
                dashBar.SetMaxDashes(katana.MaxDashes);

            if (descriptionLocalized != null) descriptionLocalized.Key = katana.DescriptionKey;
            else if (descriptionText != null) descriptionText.text = GetLocalizedText(katana.DescriptionKey);
        }

        private void ShowLockedInfo(Katana katana)
        {
            HideAllInfoPanels();
            if (lockedInfoPanel != null) lockedInfoPanel.SetActive(true);

            if (dashBar != null)
                dashBar.SetMaxDashes(katana.MaxDashes);

            if (descriptionLocalized != null) descriptionLocalized.Key = katana.DescriptionKey;
            else if (descriptionText != null) descriptionText.text = GetLocalizedText(katana.DescriptionKey);
        }

        private void ShowChallengeInfo(Katana katana)
        {
            HideAllInfoPanels();
            if (challengeInfoPanel != null) challengeInfoPanel.SetActive(true);

            if (katana.ChallengeRequirement != null)
            {
                if (challengeDescriptionLocalized != null)
                    challengeDescriptionLocalized.Key = katana.ChallengeRequirement.descriptionKey;

                if (challengeProgressBar != null)
                {
                    float current = SaveManager.GetChallengeValue(katana.ChallengeRequirement.type);
                    float target = katana.ChallengeRequirement.targetValue;
                    challengeProgressBar.SetProgress(current, target);
                }
            }

            if (dashBar != null)
                dashBar.SetMaxDashes(katana.MaxDashes);
        }

        private void UpdateButtons()
        {
            if (buyRandomLabelText != null)
                buyRandomLabelText.text = GetLocalizedText(buyRandomKey);

            if (buySpecificLabelText != null)
                buySpecificLabelText.text = GetLocalizedText(buyNowKey);

            int coins = SaveManager.GetCoins();

            int randomPrice = 0;
            if (InventoryManager.Instance?.Database != null)
                randomPrice = InventoryManager.Instance.Database.GetRarityPrice(currentRarity);

            var unowned = InventoryManager.Instance?.GetUnownedKatanasByRarity(currentRarity);
            bool hasUnowned = unowned != null && unowned.Count > 0;

            bool isChallengePage = currentRarity == KatanaRarity.Challenge;

            if (buyRandomButton != null)
            {
                bool canBuyRandom = !isChallengePage && hasUnowned && coins >= randomPrice && !isProcessing;
                buyRandomButton.SetInteractable(canBuyRandom);
                buyRandomButton.gameObject.SetActive(!isChallengePage);
            }

            if (buyRandomPriceText != null)
            {
                buyRandomPriceText.text = randomPrice.ToString();
                buyRandomPriceText.color = coins >= randomPrice ? Color.white : Color.red;
            }

            bool selectedOwned = selectedKatana != null && InventoryManager.Instance != null && InventoryManager.Instance.IsKatanaOwned(selectedKatana);
            bool selectedChallenge = selectedKatana != null && selectedKatana.IsChallenge;
            bool showSpecific = selectedKatana != null && !selectedOwned && !selectedChallenge;

            int selectedRarityPrice = 0;
            if (selectedKatana != null && InventoryManager.Instance?.Database != null)
                selectedRarityPrice = InventoryManager.Instance.Database.GetRarityPrice(selectedKatana.Rarity);

            int specificPrice = selectedRarityPrice * 2;

            if (buySpecificButton != null)
            {
                buySpecificButton.gameObject.SetActive(showSpecific);
                if (showSpecific)
                {
                    bool canBuySpecific = coins >= specificPrice && !isProcessing;
                    buySpecificButton.SetInteractable(canBuySpecific);
                }
            }

            if (buySpecificPriceText != null)
            {
                buySpecificPriceText.text = specificPrice.ToString();
                buySpecificPriceText.color = coins >= specificPrice ? Color.white : Color.red;
            }

            for (int i = 0; i < tabs.Count; i++)
                tabs[i].SetInteractable(!isProcessing);
        }

        private void UpdateCoinDisplay()
        {
            coinDisplay?.UpdateDisplay();
        }

        private void OnBuyRandomClicked()
        {
            if (isProcessing) return;
            if (currentRarity == KatanaRarity.Challenge) return;
            if (InventoryManager.Instance == null) return;

            if (InventoryManager.Instance.TryPurchaseRandom(currentRarity, out Katana result))
            {
                isProcessing = true;
                Game.Instance?.Sound?.PlayPurchaseSuccess();
                UpdateButtons();
                UpdateCoinDisplay();

                if (selectedSlot != null)
                {
                    selectedSlot.SetSelected(false);
                    selectedSlot = null;
                    selectedKatana = null;
                }

                var slots = GetKatanaSlotsOnly(currentRarity);
                rouletteAnimator.StartRoulette(slots, result);
            }
            else
            {
                Game.Instance?.Sound?.PlayPurchaseFail();
            }
        }

        private void OnBuySpecificClicked()
        {
            if (isProcessing) return;
            if (selectedKatana == null) return;
            if (InventoryManager.Instance == null) return;
            if (selectedKatana.IsChallenge) return;
            if (InventoryManager.Instance.IsKatanaOwned(selectedKatana)) return;

            int rarityPrice = InventoryManager.Instance.Database != null ? InventoryManager.Instance.Database.GetRarityPrice(selectedKatana.Rarity) : 0;
            int specificPrice = rarityPrice * 2;

            if (!SaveManager.SpendCoins(specificPrice))
            {
                Game.Instance?.Sound?.PlayPurchaseFail();
                return;
            }

            InventoryManager.Instance.UnlockKatana(selectedKatana);
            Game.Instance?.Sound?.PlayPurchaseSuccess();
            Game.Instance?.Sound?.PlayUnlock();

            UpdateCoinDisplay();

            if (selectedSlot != null && !selectedSlot.IsEmpty && selectedSlot.Katana == selectedKatana)
                selectedSlot.SetOwned(true);

            UpdateButtons();
            DisplayKatana(selectedKatana);
        }

        private List<KatanaSlot> GetKatanaSlotsOnly(KatanaRarity rarity)
        {
            var result = new List<KatanaSlot>();
            foreach (var slot in slotsByRarity[rarity])
                if (slot != null && !slot.IsEmpty)
                    result.Add(slot);
            return result;
        }

        private void OnRouletteComplete(Katana result)
        {
            InventoryManager.Instance?.UnlockKatana(result);
            Game.Instance?.Sound?.PlayUnlock();
            StartCoroutine(FinishRouletteRoutine(result));
        }

        private IEnumerator FinishRouletteRoutine(Katana result)
        {
            yield return new WaitForSecondsRealtime(0.5f);

            var slots = slotsByRarity[currentRarity];
            foreach (var slot in slots)
            {
                if (slot != null && !slot.IsEmpty && slot.Katana == result)
                {
                    slot.SetOwned(true);
                    slot.ClearHighlight();
                    SelectSlot(slot);
                    break;
                }
            }

            isProcessing = false;
            UpdateButtons();
        }

        private void OnBackClicked()
        {
            if (isProcessing) return;
            UIManager.Instance?.ShowScreen(ScreenType.MainMenu);
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

        private string GetLocalizedText(string key)
        {
            if (LocalizationController.Singleton != null)
                return LocalizationController.Singleton.GetText(key);
            return key;
        }

        private void OnDestroy()
        {
            if (buyRandomButton != null)
                buyRandomButton.OnClick -= OnBuyRandomClicked;

            if (buySpecificButton != null)
                buySpecificButton.OnClick -= OnBuySpecificClicked;

            if (backButton != null)
                backButton.OnClick -= OnBackClicked;

            if (rouletteAnimator != null)
                rouletteAnimator.OnRouletteComplete -= OnRouletteComplete;

            if (pagesView != null)
                pagesView.OnPageChanged -= OnPageChanged;

            foreach (var tab in tabs)
                if (tab != null)
                    tab.OnTabClicked -= OnTabClicked;

            ClearAllGrids();
        }
    }
}