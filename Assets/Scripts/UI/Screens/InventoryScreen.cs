using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Runner.Inventory;
using Runner.Save;

namespace Runner.UI
{
    public class InventoryScreen : UIScreen
    {
        [Header("Preview")]
        [SerializeField] private KatanaPreviewRenderer previewRenderer;
        [SerializeField] private RawImage previewImage;

        [Header("Info Panel - Owned")]
        [SerializeField] private GameObject ownedInfoPanel;
        [SerializeField] private TextMeshProUGUI katanaNameText;
        [SerializeField] private LocalizationUIText katanaNameLocalized;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private DashIndicator dashIndicator;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private LocalizationUIText descriptionLocalized;
        [SerializeField] private Image rarityBadge;

        [Header("Info Panel - Locked")]
        [SerializeField] private GameObject lockedInfoPanel;

        [Header("Info Panel - Challenge")]
        [SerializeField] private GameObject challengeInfoPanel;
        [SerializeField] private TextMeshProUGUI challengeKatanaNameText;
        [SerializeField] private LocalizationUIText challengeNameLocalized;
        [SerializeField] private LocalizationUIText challengeDescriptionLocalized;
        [SerializeField] private ChallengeProgressBar challengeProgressBar;

        [Header("No Selection Panel")]
        [SerializeField] private GameObject noSelectionPanel;

        [Header("Currency")]
        [SerializeField] private CoinDisplay coinDisplay;

        [Header("Buttons")]
        [SerializeField] private UIButton buyRandomButton;
        [SerializeField] private TextMeshProUGUI buyRandomPriceText;
        [SerializeField] private UIButton buySelectedButton;
        [SerializeField] private TextMeshProUGUI buySelectedPriceText;
        [SerializeField] private UIButton backButton;

        [Header("Tabs")]
        [SerializeField] private Transform tabContainer;
        [SerializeField] private RarityTab tabPrefab;

        [Header("Pages")]
        [SerializeField] private Transform pagesContainer;
        [SerializeField] private GameObject pageTemplate;

        [Header("Grid Settings")]
        [SerializeField] private KatanaSlot slotPrefab;
        [SerializeField] private KatanaSlot emptySlotPrefab;
        [SerializeField] private int slotsPerPage = 9;

        [Header("Roulette")]
        [SerializeField] private GridRouletteAnimator rouletteAnimator;

        [Header("Swipe Settings")]
        [SerializeField] private float swipeThreshold = 50f;
        [SerializeField] private float pageTransitionDuration = 0.3f;

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

        private readonly KatanaRarity[] rarityOrder = new KatanaRarity[]
        {
            KatanaRarity.Common,
            KatanaRarity.Rare,
            KatanaRarity.Epic,
            KatanaRarity.Legendary,
            KatanaRarity.Challenge
        };

        private List<RarityTab> tabs = new List<RarityTab>();
        private List<GameObject> pages = new List<GameObject>();
        private Dictionary<KatanaRarity, List<KatanaSlot>> slotsByRarity = new Dictionary<KatanaRarity, List<KatanaSlot>>();
        private int currentPageIndex = 0;
        private KatanaRarity currentRarity = KatanaRarity.Common;
        private Katana selectedKatana;
        private KatanaSlot selectedSlot;
        private bool isProcessing;

        protected override void Awake()
        {
            base.Awake();
            screenType = ScreenType.Inventory;

            SetupButtons();
            SetupTabs();
            SetupPages();

            if (rouletteAnimator != null)
            {
                rouletteAnimator.OnRouletteComplete += OnRouletteComplete;
            }
        }

        private void SetupButtons()
        {
            if (buyRandomButton != null)
                buyRandomButton.OnClick += OnBuyRandomClicked;

            if (buySelectedButton != null)
                buySelectedButton.OnClick += OnBuySelectedClicked;

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
                Color color = GetRarityColor(rarityOrder[i]);
                tab.Setup(i, label, color);
                tab.OnTabClicked += OnTabClicked;
                tabs.Add(tab);
            }
        }

        private void SetupPages()
        {
            if (pagesContainer == null || pageTemplate == null) return;

            pageTemplate.SetActive(false);

            for (int i = 0; i < rarityOrder.Length; i++)
            {
                GameObject page = Instantiate(pageTemplate, pagesContainer);
                page.name = $"Page_{rarityOrder[i]}";
                page.SetActive(false);
                pages.Add(page);

                slotsByRarity[rarityOrder[i]] = new List<KatanaSlot>();
            }
        }

        protected override void OnShow()
        {
            base.OnShow();

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.TryUnlockCompletedChallenges();
            }

            RefreshAllGrids();

            selectedKatana = null;
            selectedSlot = null;

            SelectPageByIndex(0, true);
            UpdateCoinDisplay();
            ClearSelection();

            isProcessing = false;
        }

        protected override void OnHide()
        {
            base.OnHide();

            if (rouletteAnimator != null)
            {
                rouletteAnimator.Cancel();
            }
        }

        private void RefreshAllGrids()
        {
            ClearAllGrids();

            if (InventoryManager.Instance == null) return;

            for (int i = 0; i < rarityOrder.Length; i++)
            {
                KatanaRarity rarity = rarityOrder[i];
                var katanas = InventoryManager.Instance.GetKatanasByRarity(rarity);
                Transform gridContainer = pages[i].transform;

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
            }
            else
            {
                GameObject emptyObj = new GameObject("EmptySlot");
                emptyObj.transform.SetParent(parent);

                RectTransform rt = emptyObj.AddComponent<RectTransform>();
                rt.localScale = Vector3.one;

                Image img = emptyObj.AddComponent<Image>();
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            }
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

            foreach (var page in pages)
            {
                for (int i = page.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(page.transform.GetChild(i).gameObject);
                }
            }
        }

        private void OnTabClicked(int index)
        {
            if (isProcessing) return;
            SelectPageByIndex(index, false);
        }

        private void SelectPageByIndex(int index, bool instant)
        {
            if (index < 0 || index >= rarityOrder.Length) return;

            for (int i = 0; i < pages.Count; i++)
            {
                pages[i].SetActive(i == index);
            }

            currentPageIndex = index;
            currentRarity = rarityOrder[index];

            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].SetSelected(i == index);
            }

            UpdateButtons();
        }

        private void ClearSelection()
        {
            if (selectedSlot != null)
            {
                selectedSlot.SetSelected(false);
            }

            selectedSlot = null;
            selectedKatana = null;

            HideAllInfoPanels();

            if (noSelectionPanel != null)
            {
                noSelectionPanel.SetActive(true);
            }

            UpdateButtons();
        }

        private void OnSlotClicked(KatanaSlot slot)
        {
            if (isProcessing) return;
            if (slot == null) return;
            if (slot.IsEmpty) return;

            if (slot.IsOwned && slot == selectedSlot && selectedKatana != null)
            {
                EquipKatana(slot.Katana);
                return;
            }

            SelectSlot(slot);
        }

        private void SelectSlot(KatanaSlot slot)
        {
            if (selectedSlot != null)
            {
                selectedSlot.SetSelected(false);
            }

            selectedSlot = slot;
            selectedKatana = slot.Katana;

            slot.SetSelected(true, true);

            UpdateInfoPanel();
            UpdateButtons();
        }

        private void EquipKatana(Katana katana)
        {
            if (katana == null) return;
            if (InventoryManager.Instance == null) return;
            if (!InventoryManager.Instance.IsKatanaOwned(katana)) return;
            if (InventoryManager.Instance.EquippedKatana == katana) return;

            InventoryManager.Instance.EquipKatana(katana);
            RefreshEquippedIndicators();
        }

        private void RefreshEquippedIndicators()
        {
            foreach (var kvp in slotsByRarity)
            {
                foreach (var slot in kvp.Value)
                {
                    if (slot == null || slot.IsEmpty) continue;

                    bool equipped = InventoryManager.Instance != null &&
                                    InventoryManager.Instance.EquippedKatana == slot.Katana;
                    slot.SetEquipped(equipped);
                }
            }
        }

        private void UpdateInfoPanel()
        {
            if (selectedKatana == null)
            {
                HideAllInfoPanels();
                if (noSelectionPanel != null)
                {
                    noSelectionPanel.SetActive(true);
                }
                return;
            }

            if (noSelectionPanel != null)
            {
                noSelectionPanel.SetActive(false);
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
                ShowOwnedInfo();
            }
            else if (selectedKatana.IsChallenge)
            {
                ShowChallengeInfo();
            }
            else
            {
                ShowLockedInfo();
            }
        }

        private void HideAllInfoPanels()
        {
            if (ownedInfoPanel != null) ownedInfoPanel.SetActive(false);
            if (lockedInfoPanel != null) lockedInfoPanel.SetActive(false);
            if (challengeInfoPanel != null) challengeInfoPanel.SetActive(false);
        }

        private void ShowOwnedInfo()
        {
            HideAllInfoPanels();
            if (ownedInfoPanel != null) ownedInfoPanel.SetActive(true);

            if (katanaNameLocalized != null)
            {
                katanaNameLocalized.Key = selectedKatana.NameKey;
            }
            else if (katanaNameText != null)
            {
                katanaNameText.text = GetLocalizedText(selectedKatana.NameKey);
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
                dashIndicator.SetDashCount(selectedKatana.MaxDashes);
            }

            if (descriptionLocalized != null)
            {
                descriptionLocalized.Key = selectedKatana.DescriptionKey;
            }
            else if (descriptionText != null)
            {
                descriptionText.text = GetLocalizedText(selectedKatana.DescriptionKey);
            }
        }

        private void ShowLockedInfo()
        {
            HideAllInfoPanels();
            if (lockedInfoPanel != null) lockedInfoPanel.SetActive(true);

            if (rarityText != null)
            {
                rarityText.text = GetRarityName(selectedKatana.Rarity);
                rarityText.color = GetRarityColor(selectedKatana.Rarity);
            }

            if (dashIndicator != null)
            {
                dashIndicator.SetHidden();
            }
        }

        private void ShowChallengeInfo()
        {
            HideAllInfoPanels();
            if (challengeInfoPanel != null) challengeInfoPanel.SetActive(true);

            if (challengeNameLocalized != null)
            {
                challengeNameLocalized.Key = selectedKatana.NameKey;
            }
            else if (challengeKatanaNameText != null)
            {
                challengeKatanaNameText.text = GetLocalizedText(selectedKatana.NameKey);
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

        private void UpdateButtons()
        {
            bool isChallenge = currentRarity == KatanaRarity.Challenge;
            int coins = SaveManager.GetCoins();

            int randomPrice = 0;
            int directPrice = 0;

            if (InventoryManager.Instance?.Database != null)
            {
                randomPrice = InventoryManager.Instance.Database.GetRarityPrice(currentRarity);
                directPrice = InventoryManager.Instance.Database.GetDirectPurchasePrice(currentRarity);
            }

            var unowned = InventoryManager.Instance?.GetUnownedKatanasByRarity(currentRarity);
            bool hasUnowned = unowned != null && unowned.Count > 0;

            bool selectedOwned = selectedKatana != null &&
                                 InventoryManager.Instance != null &&
                                 InventoryManager.Instance.IsKatanaOwned(selectedKatana);

            if (buyRandomButton != null)
            {
                bool canBuyRandom = !isChallenge && hasUnowned && coins >= randomPrice && !isProcessing;
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
                                      !selectedOwned && coins >= directPrice && !isProcessing;
                buySelectedButton.SetInteractable(canBuySelected);
                buySelectedButton.gameObject.SetActive(!isChallenge && selectedKatana != null && !selectedOwned);
            }

            if (buySelectedPriceText != null)
            {
                buySelectedPriceText.text = directPrice.ToString();
                buySelectedPriceText.color = coins >= directPrice ? Color.white : Color.red;
            }

            foreach (var tab in tabs)
            {
                tab.SetInteractable(!isProcessing);
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
            if (isProcessing) return;
            if (currentRarity == KatanaRarity.Challenge) return;
            if (InventoryManager.Instance == null) return;

            if (InventoryManager.Instance.TryPurchaseRandom(currentRarity, out Katana result))
            {
                isProcessing = true;
                UpdateButtons();
                UpdateCoinDisplay();

                var slots = GetKatanaSlotsOnly(currentRarity);
                rouletteAnimator.StartRoulette(slots, result);
            }
        }

        private List<KatanaSlot> GetKatanaSlotsOnly(KatanaRarity rarity)
        {
            List<KatanaSlot> result = new List<KatanaSlot>();

            foreach (var slot in slotsByRarity[rarity])
            {
                if (slot != null && !slot.IsEmpty)
                {
                    result.Add(slot);
                }
            }

            return result;
        }

        private void OnRouletteComplete(Katana result)
        {
            InventoryManager.Instance?.UnlockKatana(result);

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
                    SelectSlot(slot);
                    break;
                }
            }

            isProcessing = false;
            UpdateButtons();
        }

        private void OnBuySelectedClicked()
        {
            if (isProcessing) return;
            if (selectedKatana == null) return;
            if (currentRarity == KatanaRarity.Challenge) return;
            if (InventoryManager.Instance == null) return;

            if (InventoryManager.Instance.TryPurchaseDirect(selectedKatana))
            {
                if (selectedSlot != null)
                {
                    selectedSlot.SetOwned(true);
                }

                UpdateInfoPanel();
                UpdateButtons();
                UpdateCoinDisplay();
            }
        }

        private void OnBackClicked()
        {
            if (isProcessing) return;
            UIManager.Instance?.ShowScreen(ScreenType.MainMenu);
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

            return GetLocalizedText(key);
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
            {
                return LocalizationController.Singleton.GetText(key);
            }
            return key;
        }

        private void OnDestroy()
        {
            if (buyRandomButton != null)
                buyRandomButton.OnClick -= OnBuyRandomClicked;

            if (buySelectedButton != null)
                buySelectedButton.OnClick -= OnBuySelectedClicked;

            if (backButton != null)
                backButton.OnClick -= OnBackClicked;

            if (rouletteAnimator != null)
                rouletteAnimator.OnRouletteComplete -= OnRouletteComplete;

            foreach (var tab in tabs)
            {
                if (tab != null)
                {
                    tab.OnTabClicked -= OnTabClicked;
                }
            }

            ClearAllGrids();
        }
    }
}