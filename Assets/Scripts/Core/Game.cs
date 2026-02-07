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
using Runner.Environment;
using Runner.Effects;
using Runner.Player.Core;

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
        [SerializeField] private BiomeManager biomeManagerPrefab;
        [SerializeField] private DayNightCycle dayNightCyclePrefab;
        [SerializeField] private SkyController skyControllerPrefab;
        [SerializeField] private FogController fogControllerPrefab;
        [SerializeField] private ParticleController particleControllerPrefab;

        [Header("Scene References")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private CameraManager cameraManager;
        [SerializeField] private CameraEffects cameraEffects;
        [SerializeField] private SceneSetup sceneSetup;
        [SerializeField] private Light sunLight;
        [SerializeField] private Light moonLight;

        [Header("Settings")]
        [SerializeField] private bool useDayNightCycle = true;
        [SerializeField] private bool useSkyController = true;
        [SerializeField] private bool useFogController = true;

        public InputReader InputReader { get; private set; }
        public Player.Player Player { get; private set; }
        public ParticleController ParticleController { get; private set; }
        public LevelGenerator LevelGenerator { get; private set; }
        public InventoryManager InventoryManager { get; private set; }
        public BulletPool BulletPool { get; private set; }
        public BiomeManager BiomeManager { get; private set; }
        public DayNightCycle DayNightCycle { get; private set; }
        public SkyController SkyController { get; private set; }
        public FogController FogController { get; private set; }
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
        public event Action<BiomeData> OnBiomeChanged;

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
            State = GameState.Initializing;

            FindSceneReferences();
            CreateParticleController();
            CreateInventoryManager();
            CreateInputReader();
            CreateBulletPool();
            CreateDayNightCycle();
            CreatePlayer();
            CreateBiomeManager();
            CreateLevelGenerator();
            CreateFogController();
            CreateSkyController();
            InitializeCamera();
            SubscribeToEvents();

            State = GameState.Ready;
            OnGameInitialized?.Invoke();
        }

        private void FindSceneReferences()
        {
            if (sceneSetup == null)
            {
                sceneSetup = FindFirstObjectByType<SceneSetup>();
            }

            if (sceneSetup != null)
            {
                if (sunLight == null) sunLight = sceneSetup.SunLight;
                if (moonLight == null) moonLight = sceneSetup.MoonLight;
            }
        }

        private void CreateParticleController()
        {
            if (ParticleController.Instance != null)
            {
                ParticleController = ParticleController.Instance;
                return;
            }

            if (particleControllerPrefab == null) return;

            ParticleController = Instantiate(particleControllerPrefab);
            ParticleController.name = "ParticleController";
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

        private void CreateDayNightCycle()
        {
            if (!useDayNightCycle) return;

            if (DayNightCycle.Instance != null)
            {
                DayNightCycle = DayNightCycle.Instance;
                return;
            }

            if (dayNightCyclePrefab != null)
            {
                DayNightCycle = Instantiate(dayNightCyclePrefab);
            }
            else
            {
                var go = new GameObject("DayNightCycle");
                DayNightCycle = go.AddComponent<DayNightCycle>();
            }

            DayNightCycle.name = "DayNightCycle";
            DayNightCycle.Initialize(sunLight, moonLight);
        }

        private void CreatePlayer()
        {
            if (playerPrefab == null) return;

            Vector3 spawnPosition = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
            Player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            Player.name = "Player";
            Player.Initialize();
        }

        private void CreateBiomeManager()
        {
            if (BiomeManager.Instance != null)
            {
                BiomeManager = BiomeManager.Instance;
            }
            else if (biomeManagerPrefab != null)
            {
                BiomeManager = Instantiate(biomeManagerPrefab);
            }
            else
            {
                var go = new GameObject("BiomeManager");
                BiomeManager = go.AddComponent<BiomeManager>();
            }

            BiomeManager.name = "BiomeManager";

            BiomeData startingBiome = sceneSetup?.StartingBiome;
            if (startingBiome == null)
            {
                Debug.LogError("[Game] No starting biome in SceneSetup!");
                return;
            }

            BiomeManager.Initialize(Player.transform, startingBiome);
        }

        private void CreateLevelGenerator()
        {
            if (levelGeneratorPrefab != null)
            {
                LevelGenerator = Instantiate(levelGeneratorPrefab);
            }
            else
            {
                var go = new GameObject("LevelGenerator");
                LevelGenerator = go.AddComponent<LevelGenerator>();
            }

            LevelGenerator.name = "LevelGenerator";
            LevelGenerator.Initialize(Player.transform, BiomeManager);
        }

        private void CreateFogController()
        {
            if (!useFogController) return;

            if (FogController.Instance != null)
            {
                FogController = FogController.Instance;
                return;
            }

            if (fogControllerPrefab != null)
            {
                FogController = Instantiate(fogControllerPrefab);
            }
            else
            {
                var go = new GameObject("FogController");
                FogController = go.AddComponent<FogController>();
            }

            FogController.name = "FogController";
            FogController.Initialize(DayNightCycle, BiomeManager);
        }

        private void CreateSkyController()
        {
            if (!useSkyController) return;

            if (SkyController.Instance != null)
            {
                SkyController = SkyController.Instance;
                return;
            }

            if (skyControllerPrefab != null)
            {
                SkyController = Instantiate(skyControllerPrefab);
            }
            else
            {
                var go = new GameObject("SkyController");
                SkyController = go.AddComponent<SkyController>();
            }

            SkyController.name = "SkyController";
            SkyController.Initialize(DayNightCycle, BiomeManager);
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

            if (BiomeManager != null)
            {
                BiomeManager.OnBiomeChanged += HandleBiomeChanged;
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

            if (BiomeManager != null)
            {
                BiomeManager.OnBiomeChanged -= HandleBiomeChanged;
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

        private void HandleBiomeChanged(BiomeData biome)
        {
            OnBiomeChanged?.Invoke(biome);
        }

        public void StartGame()
        {
            if (State != GameState.Ready) return;

            State = GameState.Playing;
            Score = 0;

            Player?.StartRunning();
            InputReader?.EnableGameplayInput();
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
            if (State == GameState.GameOver) return;

            State = GameState.GameOver;
            Player?.Die();
            InputReader?.DisableGameplayInput();
            cameraManager?.SetState(CameraState.Death);
            cameraEffects?.PlayDeathEffect();

            OnGameOver?.Invoke();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            Score = 0;

            BulletPool?.ReturnAllBullets();
            Player?.Reset();

            BiomeData startingBiome = sceneSetup?.StartingBiome;
            BiomeManager?.Reset(startingBiome);

            LevelGenerator?.Reset();
            FogController?.Reset();
            SkyController?.Reset();

            if (Player != null && playerSpawnPoint != null)
            {
                Player.transform.position = playerSpawnPoint.position;
            }

            cameraManager?.SetState(CameraState.Menu);
            State = GameState.Ready;

            OnGameRestarted?.Invoke();
        }

        public void AddScore(int amount)
        {
            Score += amount;
        }

        [ContextMenu("Add 1000 money")]
        public void AddMoney()
        {
            SaveManager.AddCoins(1000);
        }

        public void SetGameSpeed(float speed)
        {
            GameSpeed = Mathf.Clamp(speed, 0.5f, 3f);
        }

        public BiomeData GetCurrentBiome()
        {
            return BiomeManager?.CurrentBiome;
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