using UnityEngine;
using System.Collections.Generic;
using Runner.Core;

namespace Runner.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screens")]
        [SerializeField] private MainMenuScreen mainMenuScreen;
        [SerializeField] private GameplayScreen gameplayScreen;
        [SerializeField] private PauseScreen pauseScreen;
        [SerializeField] private GameOverScreen gameOverScreen;
        [SerializeField] private SettingsScreen settingsScreen;

        private Dictionary<ScreenType, UIScreen> screens;
        private ScreenType currentScreen;
        private ScreenType previousScreen;
        private int coinsCollectedThisRun;

        public ScreenType CurrentScreen => currentScreen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Initialize();
        }

        private void Initialize()
        {
            screens = new Dictionary<ScreenType, UIScreen>();

            RegisterScreen(mainMenuScreen);
            RegisterScreen(gameplayScreen);
            RegisterScreen(pauseScreen);
            RegisterScreen(gameOverScreen);
            RegisterScreen(settingsScreen);

            HideAllScreensImmediate();
        }

        private void Start()
        {
            ShowScreen(ScreenType.MainMenu, true);
            SubscribeToGameEvents();
        }

        private void RegisterScreen(UIScreen screen)
        {
            if (screen == null) return;
            screens[screen.Type] = screen;
        }

        private void SubscribeToGameEvents()
        {
            if (Game.Instance == null) return;

            if (Game.Instance.Player != null)
            {
                Game.Instance.Player.OnPlayerDeath += HandlePlayerDeath;
            }

            if (Game.Instance.InputReader != null)
            {
                Game.Instance.InputReader.OnPause += HandlePauseInput;
            }
        }

        private void UnsubscribeFromGameEvents()
        {
            if (Game.Instance == null) return;

            if (Game.Instance.Player != null)
            {
                Game.Instance.Player.OnPlayerDeath -= HandlePlayerDeath;
            }

            if (Game.Instance.InputReader != null)
            {
                Game.Instance.InputReader.OnPause -= HandlePauseInput;
            }
        }

        private void HandlePauseInput()
        {
            if (Game.Instance == null) return;

            if (Game.Instance.State == GameState.Playing)
            {
                PauseGame();
            }
            else if (Game.Instance.State == GameState.Paused)
            {
                ResumeGame();
            }
        }

        private void HandlePlayerDeath()
        {
            ShowGameOver();
        }

        private void HideAllScreensImmediate()
        {
            foreach (var screen in screens.Values)
            {
                if (screen != null)
                {
                    screen.Hide(true);
                }
            }
            currentScreen = ScreenType.None;
        }

        public void ShowScreen(ScreenType type, bool instant = false)
        {
            if (!screens.ContainsKey(type)) return;

            if (currentScreen != ScreenType.None &&
                currentScreen != type &&
                screens.ContainsKey(currentScreen) &&
                screens[currentScreen] != null)
            {
                screens[currentScreen].Hide(instant);
            }

            previousScreen = currentScreen;
            currentScreen = type;

            if (screens[type] != null)
            {
                screens[type].Show(instant);
            }
        }

        public void HideScreen(ScreenType type)
        {
            if (!screens.ContainsKey(type)) return;
            if (screens[type] == null) return;

            screens[type].Hide();

            if (previousScreen != ScreenType.None &&
                screens.ContainsKey(previousScreen) &&
                screens[previousScreen] != null)
            {
                currentScreen = previousScreen;
                screens[previousScreen].Show();
            }
        }

        public void StartGame()
        {
            if (Game.Instance == null) return;
            if (Game.Instance.State != GameState.Ready) return;

            coinsCollectedThisRun = 0;
            ShowScreen(ScreenType.Gameplay);
            Game.Instance.StartGame();
        }

        public void PauseGame()
        {
            if (Game.Instance == null) return;
            if (Game.Instance.State != GameState.Playing) return;

            Game.Instance.PauseGame();
            ShowScreen(ScreenType.Pause);
        }

        public void ResumeGame()
        {
            if (Game.Instance == null) return;
            if (Game.Instance.State != GameState.Paused) return;

            ShowScreen(ScreenType.Gameplay);
            Game.Instance.ResumeGame();
        }

        public void RestartGame()
        {
            if (Game.Instance == null) return;

            coinsCollectedThisRun = 0;
            Game.Instance.RestartGame();
            ShowScreen(ScreenType.Gameplay);
            Game.Instance.StartGame();
        }

        public void GoToMainMenu()
        {
            if (Game.Instance == null) return;

            Time.timeScale = 1f;
            Game.Instance.RestartGame();
            ShowScreen(ScreenType.MainMenu);
        }

        public void ShowGameOver()
        {
            float distance = Game.Instance != null ? Game.Instance.RunDistance : 0f;

            if (gameOverScreen != null)
            {
                gameOverScreen.Setup(distance, coinsCollectedThisRun);
            }

            ShowScreen(ScreenType.GameOver);
        }

        public void AddCoins(int amount)
        {
            coinsCollectedThisRun += amount;

            if (gameplayScreen != null)
            {
                gameplayScreen.AddCoins(amount);
            }

            int totalCoins = PlayerPrefs.GetInt("Coins", 0);
            totalCoins += amount;
            PlayerPrefs.SetInt("Coins", totalCoins);
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameEvents();

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}