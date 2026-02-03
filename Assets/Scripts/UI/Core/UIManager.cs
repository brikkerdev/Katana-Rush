using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Runner.Core;
using Runner.Save;

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
        [SerializeField] private LoadingScreen loadingScreen;
        [SerializeField] private InventoryScreen inventoryScreen;

        [Header("Loading Settings")]
        [SerializeField] private float initialLoadDelay = 0.5f;
        [SerializeField] private float restartLoadDelay = 0.3f;

        private Dictionary<ScreenType, UIScreen> screens;
        private ScreenType currentScreen;
        private ScreenType previousScreen;
        private int coinsCollectedThisRun;
        private bool isInitialized;

        public ScreenType CurrentScreen => currentScreen;
        public bool IsLoading => loadingScreen != null && loadingScreen.IsLoading;

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
            RegisterScreen(inventoryScreen);

            HideAllScreensImmediate();
        }

        private void Start()
        {
            StartCoroutine(InitialLoadRoutine());
        }

        private IEnumerator InitialLoadRoutine()
        {
            if (loadingScreen != null)
            {
                loadingScreen.StartLoading("Initializing...");
            }

            yield return null;

            if (loadingScreen != null)
            {
                loadingScreen.SetMessage("Loading Resources...");
                loadingScreen.SetProgress(0.1f);
            }

            yield return new WaitForSecondsRealtime(initialLoadDelay * 0.3f);

            if (loadingScreen != null)
            {
                loadingScreen.SetMessage("Preparing Game...");
                loadingScreen.SetProgress(0.3f);
            }

            while (Game.Instance == null || Game.Instance.State == GameState.Initializing)
            {
                yield return null;
            }

            if (loadingScreen != null)
            {
                loadingScreen.SetMessage("Loading Level...");
                loadingScreen.SetProgress(0.6f);
            }

            yield return new WaitForSecondsRealtime(initialLoadDelay * 0.3f);

            if (loadingScreen != null)
            {
                loadingScreen.SetMessage("Almost Ready...");
                loadingScreen.SetProgress(0.9f);
            }

            yield return new WaitForSecondsRealtime(initialLoadDelay * 0.2f);

            if (loadingScreen != null)
            {
                loadingScreen.CompleteLoading();

                while (loadingScreen.IsLoading)
                {
                    yield return null;
                }
            }

            ShowScreen(ScreenType.MainMenu, true);
            SubscribeToGameEvents();
            isInitialized = true;
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
            if (IsLoading) return;

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
            if (IsLoading) return;

            coinsCollectedThisRun = 0;
            SaveManager.AddGamePlayed();
            ShowScreen(ScreenType.Gameplay);
            Game.Instance.StartGame();
        }

        public void PauseGame()
        {
            if (Game.Instance == null) return;
            if (Game.Instance.State != GameState.Playing) return;
            if (IsLoading) return;

            Game.Instance.PauseGame();
            ShowScreen(ScreenType.Pause);
        }

        public void ResumeGame()
        {
            if (Game.Instance == null) return;
            if (Game.Instance.State != GameState.Paused) return;
            if (IsLoading) return;

            ShowScreen(ScreenType.Gameplay);
            Game.Instance.ResumeGame();
        }

        public void RestartGame()
        {
            if (Game.Instance == null) return;
            if (IsLoading) return;

            StartCoroutine(RestartGameRoutine());
        }

        private IEnumerator RestartGameRoutine()
        {
            if (loadingScreen != null)
            {
                loadingScreen.StartLoading("Restarting...");
                loadingScreen.SetProgress(0.2f);
            }

            yield return new WaitForSecondsRealtime(restartLoadDelay * 0.2f);

            Time.timeScale = 1f;
            coinsCollectedThisRun = 0;

            if (loadingScreen != null)
            {
                loadingScreen.SetMessage("Resetting Level...");
                loadingScreen.SetProgress(0.5f);
            }

            Game.Instance.RestartGame();

            yield return new WaitForSecondsRealtime(restartLoadDelay * 0.3f);

            if (loadingScreen != null)
            {
                loadingScreen.SetMessage("Ready!");
                loadingScreen.SetProgress(0.9f);
            }

            yield return new WaitForSecondsRealtime(restartLoadDelay * 0.2f);

            if (loadingScreen != null)
            {
                loadingScreen.CompleteLoading();

                while (loadingScreen.IsLoading)
                {
                    yield return null;
                }
            }

            SaveManager.AddGamePlayed();
            ShowScreen(ScreenType.Gameplay);
            Game.Instance.StartGame();
        }

        public void GoToMainMenu()
        {
            if (Game.Instance == null) return;
            if (IsLoading) return;

            StartCoroutine(GoToMainMenuRoutine());
        }

        private IEnumerator GoToMainMenuRoutine()
        {
            if (loadingScreen != null)
            {
                loadingScreen.StartLoading("Returning to Menu...");
                loadingScreen.SetProgress(0.3f);
            }

            yield return new WaitForSecondsRealtime(restartLoadDelay * 0.3f);

            Time.timeScale = 1f;

            if (loadingScreen != null)
            {
                loadingScreen.SetMessage("Resetting...");
                loadingScreen.SetProgress(0.6f);
            }

            Game.Instance.RestartGame();

            yield return new WaitForSecondsRealtime(restartLoadDelay * 0.3f);

            if (loadingScreen != null)
            {
                loadingScreen.SetProgress(0.9f);
            }

            yield return new WaitForSecondsRealtime(restartLoadDelay * 0.2f);

            if (loadingScreen != null)
            {
                loadingScreen.CompleteLoading();

                while (loadingScreen.IsLoading)
                {
                    yield return null;
                }
            }

            ShowScreen(ScreenType.MainMenu);
        }

        public void ShowGameOver()
        {
            if (IsLoading) return;

            float distance = Game.Instance != null ? Game.Instance.RunDistance : 0f;

            SaveManager.AddDistance(distance);
            SaveManager.TrySetHighScore((int)distance);
            SaveManager.SaveIfDirty();

            if (gameOverScreen != null)
            {
                gameOverScreen.Setup(distance, coinsCollectedThisRun);
            }

            ShowScreen(ScreenType.GameOver);
        }

        public void NotifyCoinsCollected(int amount)
        {
            coinsCollectedThisRun += amount;

            if (gameplayScreen != null)
            {
                gameplayScreen.AddCoins(amount);
            }
        }

        public void ShowLoading(string message, System.Action onComplete = null)
        {
            if (loadingScreen != null)
            {
                loadingScreen.StartLoading(message, onComplete);
            }
        }

        public void SetLoadingProgress(float progress)
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetProgress(progress);
            }
        }

        public void CompleteLoading()
        {
            if (loadingScreen != null)
            {
                loadingScreen.CompleteLoading();
            }
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