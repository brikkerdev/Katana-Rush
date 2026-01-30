using UnityEngine;
using TMPro;
using Runner.Core;

namespace Runner.UI
{
    public class MainMenuScreen : UIScreen
    {
        [Header("Tap To Start")]
        [SerializeField] private TapToStartHandler tapHandler;
        [SerializeField] private TextMeshProUGUI tapToStartText;
        [SerializeField] private string tapToStartMessage = "TAP TO START";

        [Header("Menu Buttons")]
        [SerializeField] private UIButton settingsButton;
        [SerializeField] private UIButton inventoryButton;
        [SerializeField] private UIButton shopButton;
        [SerializeField] private UIButton leaderboardButton;

        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI coinsText;

        protected override void Awake()
        {
            base.Awake();
            screenType = ScreenType.MainMenu;
            SetupButtons();
            SetupTapHandler();
        }

        private void SetupButtons()
        {
            if (settingsButton != null)
                settingsButton.OnClick += OnSettingsClicked;

            if (inventoryButton != null)
                inventoryButton.OnClick += OnInventoryClicked;

            if (shopButton != null)
                shopButton.OnClick += OnShopClicked;

            if (leaderboardButton != null)
                leaderboardButton.OnClick += OnLeaderboardClicked;
        }

        private void SetupTapHandler()
        {
            if (tapHandler != null)
            {
                tapHandler.OnTapToStart += OnTapToStart;
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            UpdatePlayerInfo();

            if (tapHandler != null)
            {
                tapHandler.IsEnabled = true;
            }

            if (tapToStartText != null)
            {
                tapToStartText.text = tapToStartMessage;
            }
        }

        protected override void OnHide()
        {
            base.OnHide();

            if (tapHandler != null)
            {
                tapHandler.IsEnabled = false;
            }
        }

        private void UpdatePlayerInfo()
        {
            if (highScoreText != null)
            {
                int highScore = PlayerPrefs.GetInt("HighScore", 0);
                highScoreText.text = $"BEST: {highScore}m";
            }

            if (coinsText != null)
            {
                int coins = PlayerPrefs.GetInt("Coins", 0);
                coinsText.text = coins.ToString();
            }
        }

        private void OnTapToStart()
        {
            UIManager.Instance?.StartGame();
        }

        private void OnSettingsClicked()
        {
            UIManager.Instance?.ShowScreen(ScreenType.Settings);
        }

        private void OnInventoryClicked()
        {
            UIManager.Instance?.ShowScreen(ScreenType.Inventory);
        }

        private void OnShopClicked()
        {
            Debug.Log("Shop clicked");
        }

        private void OnLeaderboardClicked()
        {
            Debug.Log("Leaderboard clicked");
        }

        private void OnDestroy()
        {
            if (tapHandler != null)
                tapHandler.OnTapToStart -= OnTapToStart;

            if (settingsButton != null)
                settingsButton.OnClick -= OnSettingsClicked;

            if (inventoryButton != null)
                inventoryButton.OnClick -= OnInventoryClicked;

            if (shopButton != null)
                shopButton.OnClick -= OnShopClicked;

            if (leaderboardButton != null)
                leaderboardButton.OnClick -= OnLeaderboardClicked;
        }
    }
}