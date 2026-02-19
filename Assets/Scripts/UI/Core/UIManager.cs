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

        [Header("Resume Settings")]
        [SerializeField] private int resumeCountdownSeconds = 3;

        private Dictionary<ScreenType, UIScreen> screens;
        private ScreenType currentScreen;
        private ScreenType previousScreen;
        private int coinsCollectedThisRun;
        private bool isInitialized;
        private bool isResuming;

        public ScreenType CurrentScreen => currentScreen;
        public bool IsLoading => loadingScreen != null && loadingScreen.IsLoading;
        public bool IsResuming => isResuming;

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
            if (isResuming) return;

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
            if (isResuming && gameplayScreen != null)
            {
                gameplayScreen.CancelCountdown();
                isResuming = false;
            }

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

            if (type == ScreenType.Settings && settingsScreen != null)
            {
                settingsScreen.SetReturnScreen(currentScreen);
            }

            bool isOpeningPanel = type == ScreenType.Settings ||
                                  type == ScreenType.Inventory ||
                                  type == ScreenType.Pause;

            bool isClosingPanel = currentScreen == ScreenType.Settings ||
                                  currentScreen == ScreenType.Inventory ||
                                  currentScreen == ScreenType.Pause;

            if (currentScreen != ScreenType.None &&
                currentScreen != type &&
                screens.ContainsKey(currentScreen) &&
                screens[currentScreen] != null)
            {
                if (isClosingPanel && !isOpeningPanel)
                    Game.Instance?.Sound?.PlayPanelClose();

                screens[currentScreen].Hide(instant);
            }

            previousScreen = currentScreen;
            currentScreen = type;

            if (screens[type] != null)
            {
                if (isOpeningPanel)
                    Game.Instance?.Sound?.PlayPanelOpen();

                screens[type].Show(instant);
            }
        }

        public void HideScreen(ScreenType type)
        {
            if (!screens.ContainsKey(type)) return;
            if (screens[type] == null) return;

            Game.Instance?.Sound?.PlayPanelClose();
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
            if (isResuming) return;

            // Disable player input when pausing to prevent dash from tap
            Game.Instance.Player?.Controller?.DisableInput();

            Game.Instance.PauseGame();
            ShowScreen(ScreenType.Pause);
        }

        public void ResumeGame()
        {
            if (Game.Instance == null) return;
            if (Game.Instance.State != GameState.Paused) return;
            if (IsLoading) return;
            if (isResuming) return;

            StartCoroutine(ResumeGameRoutine());
        }

        private IEnumerator ResumeGameRoutine()
        {
            isResuming = true;

            ShowScreen(ScreenType.Gameplay);

            if (gameplayScreen != null && resumeCountdownSeconds > 0)
            {
                gameplayScreen.ShowCountdown(resumeCountdownSeconds);

                yield return new WaitForSecondsRealtime(resumeCountdownSeconds + 0.5f);
            }

            if (Game.Instance != null && Game.Instance.State == GameState.Paused)
            {
                Game.Instance.ResumeGame();
                
                // Re-enable player input after countdown completes
                Game.Instance.Player?.Controller?.EnableInput();
            }

            isResuming = false;
        }

        public void RestartGame()
        {
            if (Game.Instance == null) return;
            if (IsLoading) return;

            if (isResuming && gameplayScreen != null)
            {
                gameplayScreen.CancelCountdown();
                isResuming = false;
            }

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

            if (isResuming && gameplayScreen != null)
            {
                gameplayScreen.CancelCountdown();
                isResuming = false;
            }

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
            int score = Game.Instance != null ? Game.Instance.Score : 0;

            bool isNewHighScore = SaveManager.TrySetHighScore(score);
            SaveManager.AddDistance(distance);
            SaveManager.SaveIfDirty();

            if (gameOverScreen != null)
            {
                gameOverScreen.Setup(distance, coinsCollectedThisRun, score);
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