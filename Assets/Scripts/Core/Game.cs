using UnityEngine;
using System;
using Runner.Input;
using Runner.Player;
using Runner.LevelGeneration;
using Runner.CameraSystem;
using Runner.Inventory;
using Runner.Player.Data;
using Runner.Enemy;
using Runner.Save;

namespace Runner.Core
{
    public class Game : MonoBehaviour
    {
        public static Game Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private InputReader inputReaderPrefab;
        [SerializeField] private Player.Player playerPrefab;
        [SerializeField] private LevelGenerator levelGeneratorPrefab;
        [SerializeField] private InventoryManager inventoryManagerPrefab;
        [SerializeField] private BulletPool bulletPoolPrefab;

        [Header("Scene References")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private CameraManager cameraManager;
        [SerializeField] private CameraEffects cameraEffects;

        [Header("Initialization")]
        [SerializeField] private bool initializeOnAwake = true;

        public InputReader InputReader { get; private set; }
        public Player.Player Player { get; private set; }
        public LevelGenerator LevelGenerator { get; private set; }
        public InventoryManager InventoryManager { get; private set; }
        public BulletPool BulletPool { get; private set; }
        public CameraManager CameraManager => cameraManager;
        public CameraEffects CameraEffects => cameraEffects;
        public GameState State { get; private set; }
        public float GameSpeed { get; private set; } = 1f;
        public float RunDistance => Player != null ? Player.transform.position.z : 0f;
        public int Score { get; private set; }

        public event Action OnGameInitialized;
        public event Action OnGameStarted;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action OnGameOver;
        public event Action OnGameRestarted;

        private float initializationProgress;
        public float InitializationProgress => initializationProgress;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            if (State != GameState.Initializing && State != default)
            {
                return;
            }

            State = GameState.Initializing;
            initializationProgress = 0f;

            CreateInventoryManager();
            initializationProgress = 0.15f;

            CreateInputReader();
            initializationProgress = 0.3f;

            CreateBulletPool();
            initializationProgress = 0.45f;

            CreateLevelGenerator();
            initializationProgress = 0.6f;

            CreatePlayer();
            initializationProgress = 0.75f;

            InitializeCamera();
            initializationProgress = 0.9f;

            SubscribeToEvents();
            initializationProgress = 1f;

            State = GameState.Ready;
            OnGameInitialized?.Invoke();
        }

        [ContextMenu("Add 1000 coins")]
        public void AddCoins()
        {
            SaveManager.AddCoins(1000);
        }

        private void CreateInventoryManager()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager = InventoryManager.Instance;
                return;
            }

            if (inventoryManagerPrefab == null) return;

            InventoryManager = Instantiate(inventoryManagerPrefab);
            InventoryManager.name = "InventoryManager";
        }

        private void CreateInputReader()
        {
            if (InputReader.Instance != null)
            {
                InputReader = InputReader.Instance;
                return;
            }

            if (inputReaderPrefab == null) return;

            InputReader = Instantiate(inputReaderPrefab);
            InputReader.name = "InputReader";
        }

        private void CreateBulletPool()
        {
            if (BulletPool.Instance != null)
            {
                BulletPool = BulletPool.Instance;
                return;
            }

            if (bulletPoolPrefab == null) return;

            BulletPool = Instantiate(bulletPoolPrefab);
            BulletPool.name = "BulletPool";
        }

        private void CreateLevelGenerator()
        {
            if (levelGeneratorPrefab == null) return;

            LevelGenerator = Instantiate(levelGeneratorPrefab);
            LevelGenerator.name = "LevelGenerator";
        }

        private void CreatePlayer()
        {
            if (playerPrefab == null) return;

            Vector3 spawnPosition = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
            Player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            Player.name = "Player";
            Player.Initialize();

            if (LevelGenerator != null)
            {
                LevelGenerator.SetPlayer(Player.transform);
            }
        }

        private void InitializeCamera()
        {
            if (cameraManager != null && Player != null)
            {
                cameraManager.Initialize(Player.transform);
            }

            if (cameraEffects != null && Player != null)
            {
                cameraEffects.Initialize(Player);
            }
        }

        private void SubscribeToEvents()
        {
            if (Player != null)
            {
                Player.OnPlayerDeath += HandlePlayerDeath;
                Player.OnPlayerRevive += HandlePlayerRevive;
            }

            if (InventoryManager != null)
            {
                InventoryManager.OnPresetChanged += HandlePresetChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (Player != null)
            {
                Player.OnPlayerDeath -= HandlePlayerDeath;
                Player.OnPlayerRevive -= HandlePlayerRevive;
            }

            if (InventoryManager != null)
            {
                InventoryManager.OnPresetChanged -= HandlePresetChanged;
            }
        }

        private void HandlePlayerDeath()
        {
            State = GameState.GameOver;
            cameraManager?.SetState(CameraState.Death);
            cameraEffects?.PlayDeathEffect();
            OnGameOver?.Invoke();
        }

        private void HandlePlayerRevive()
        {
            State = GameState.Playing;
            cameraManager?.SetState(CameraState.Gameplay);
        }

        private void HandlePresetChanged(PlayerPreset preset)
        {
            if (Player != null && Player.Controller != null)
            {
                Player.Controller.ApplyPreset(preset);
            }
        }

        public void StartGame()
        {
            if (State != GameState.Ready) return;

            State = GameState.Playing;
            Score = 0;

            if (Player != null)
            {
                Player.StartRunning();
            }

            if (InputReader != null)
            {
                InputReader.EnableGameplayInput();
            }

            cameraManager?.SetState(CameraState.Gameplay);
            OnGameStarted?.Invoke();
        }

        public void PauseGame()
        {
            if (State != GameState.Playing) return;

            State = GameState.Paused;
            Time.timeScale = 0f;
            InputReader?.DisableGameplayInput();
            OnGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;

            State = GameState.Playing;
            Time.timeScale = 1f;
            InputReader?.EnableGameplayInput();
            OnGameResumed?.Invoke();
        }

        public void GameOver()
        {
            if (State != GameState.Playing) return;

            State = GameState.GameOver;
            Player?.Die();
            InputReader?.DisableGameplayInput();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            Score = 0;

            BulletPool?.ReturnAllBullets();

            Player?.Reset();
            LevelGenerator?.ResetGenerator();

            if (Player != null)
            {
                Vector3 spawnPosition = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
                Player.transform.position = spawnPosition;
            }

            cameraManager?.SetState(CameraState.Menu);
            State = GameState.Ready;
            OnGameRestarted?.Invoke();
        }

        public void AddScore(int amount)
        {
            Score += amount;
        }

        public void SetGameSpeed(float speed)
        {
            GameSpeed = Mathf.Clamp(speed, 0.5f, 3f);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (Instance == this) Instance = null;
        }
    }

    public enum GameState
    {
        Initializing,
        Ready,
        Playing,
        Paused,
        GameOver
    }
}