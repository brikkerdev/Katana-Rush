using Runner.Audio;
using Runner.CameraSystem;
using Runner.Effects;
using Runner.Enemy;
using Runner.Environment;
using Runner.Input;
using Runner.Inventory;
using Runner.LevelGeneration;
using Runner.Player;
using Runner.Player.Core;
using Runner.Player.Data;
using Runner.Save;
using Runner.UI;
using System;
using UnityEngine;

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
        [SerializeField] private SoundController soundControllerPrefab;
        [SerializeField] private DayNightUiController dayNightUIControllerPrefab;

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

        [Header("Score Settings")]
        [SerializeField] private int scoreMilestoneInterval = 1000;

        [Header("Powerup Settings")]
        [SerializeField] private float speedBoostMultiplier = 1.3f;

        [Header("Distance Score")]
        [SerializeField] private float metersPerScore = 1f;

        private float lastScoredZ;

        public InputReader InputReader { get; private set; }
        public Player.Player Player { get; private set; }
        public ParticleController ParticleController { get; private set; }
        public SoundController Sound { get; private set; }
        public LevelGenerator LevelGenerator { get; private set; }
        public InventoryManager InventoryManager { get; private set; }
        public BulletPool BulletPool { get; private set; }
        public BiomeManager BiomeManager { get; private set; }
        public DayNightCycle DayNightCycle { get; private set; }
        public SkyController SkyController { get; private set; }
        public FogController FogController { get; private set; }
        public CameraManager CameraManager => cameraManager;
        public CameraEffects CameraEffects => cameraEffects;
        public DayNightUiController DayNightUIController { get; private set; }

        public GameState State { get; private set; }
        public float GameSpeed { get; private set; } = 1f;
        public float RunDistance => Player != null ? Player.transform.position.z - startZ : 0f;
        public int Score { get; private set; }
        public int ScoreMultiplier { get; private set; } = 1;
        public bool IsMagnetActive { get; private set; }
        public bool IsSpeedBoostActive { get; private set; }


        private int lastMilestone;
        private float multiplierTimer;
        private float magnetTimer;
        private float speedBoostTimer;
        private float multiplierDuration;
        private float magnetDuration;
        private float speedBoostDuration;
        private float lastDeathZ = 0f;
        private float startZ = 0f;
        private BiomeData deathBiome;
        private BiomeData deathNextBiome;

        public float MultiplierTimeRemaining => multiplierTimer;
        public float MultiplierDuration => multiplierDuration;
        public float MagnetTimeRemaining => magnetTimer;
        public float MagnetDuration => magnetDuration;
        public float SpeedBoostTimeRemaining => speedBoostTimer;
        public float SpeedBoostDuration => speedBoostDuration;

        public event Action OnGameInitialized;
        public event Action OnGameStarted;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action OnGameOver;
        public event Action OnGameRestarted;
        public event Action<BiomeData> OnBiomeChanged;
        public event Action<int> OnMultiplierChanged;
        public event Action<bool> OnMagnetChanged;
        public event Action<bool> OnSpeedBoostChanged;
        public event Action<int> OnScoreChanged;

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
            CreateSoundController();
            CreateParticleController();
            CreateInventoryManager();
            CreateInputReader();
            CreateBulletPool();
            CreateDayNightCycle();
            CreateDayNightUIController();
            CreatePlayer();
            CreateBiomeManager();
            CreateLevelGenerator();
            CreateFogController();
            CreateSkyController();
            InitializeCamera();
            SubscribeToEvents();

            Sound?.StartMusic();

            State = GameState.Ready;
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

        // Add method
        private void CreateDayNightUIController()
        {
            if (DayNightUiController.Instance != null)
            {
                DayNightUIController = DayNightUiController.Instance;
                DayNightUIController.Initialize(DayNightCycle);
                return;
            }

            if (dayNightUIControllerPrefab != null)
            {
                DayNightUIController = Instantiate(dayNightUIControllerPrefab);
            }
            else
            {
                var go = new GameObject("DayNightUIController");
                DayNightUIController = go.AddComponent<DayNightUiController>();
            }

            DayNightUIController.name = "DayNightUIController";
            DayNightUIController.Initialize(DayNightCycle);
        }

        private void CreateSoundController()
        {
            if (SoundController.Instance != null)
            {
                Sound = SoundController.Instance;
                return;
            }

            if (soundControllerPrefab != null)
            {
                Sound = Instantiate(soundControllerPrefab);
            }
            else
            {
                var go = new GameObject("SoundController");
                Sound = go.AddComponent<SoundController>();
            }

            Sound.name = "SoundController";

            float savedSfx = PlayerPrefs.GetFloat("SFXVolume", 1f);
            float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 1f);

            Sound.SfxVolume = savedSfx;
            Sound.UiVolume = savedSfx;
            Sound.MusicVolume = savedMusic;
            Sound.MasterVolume = 1f;
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
                FogController.Initialize(DayNightCycle, BiomeManager);
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

        private void FixedUpdate()
        {
            if (State != GameState.Playing) return;

            UpdateDistanceScore();
            UpdatePowerups();
        }

        private void UpdateDistanceScore()
        {
            if (Player == null) return;

            float currentZ = Player.transform.position.z;
            float delta = currentZ - lastScoredZ;

            if (delta >= metersPerScore)
            {
                int points = Mathf.FloorToInt(delta / metersPerScore);
                lastScoredZ += points * metersPerScore;
                AddScore(points);
            }
        }

        private void UpdatePowerups()
        {
            float dt = Time.deltaTime;

            if (ScoreMultiplier > 1)
            {
                multiplierTimer -= dt;
                if (multiplierTimer <= 0f)
                {
                    ScoreMultiplier = 1;
                    OnMultiplierChanged?.Invoke(ScoreMultiplier);
                }
            }

            if (IsMagnetActive)
            {
                magnetTimer -= dt;
                if (magnetTimer <= 0f)
                {
                    IsMagnetActive = false;
                    OnMagnetChanged?.Invoke(false);
                }
            }

            if (IsSpeedBoostActive)
            {
                speedBoostTimer -= dt;
                if (speedBoostTimer <= 0f)
                {
                    IsSpeedBoostActive = false;
                    GameSpeed = 1f;
                    OnSpeedBoostChanged?.Invoke(false);
                }
            }
        }

        private void HandlePlayerDeath()
        {
            State = GameState.GameOver;
            lastDeathZ = Player != null ? Player.transform.position.z : 0f;
            
            // Save the current biome state at death
            if (BiomeManager != null)
            {
                deathBiome = BiomeManager.CurrentBiome;
                deathNextBiome = BiomeManager.NextBiome;
            }
            
            cameraManager?.SetState(CameraState.Death);
            cameraEffects?.PlayDeathEffect();
            Sound?.PlayDeath();
            Sound?.PlayGameOver();
            Sound?.SetMusicGameplay(false);
            ResetPowerups();
            SaveManager.ResetKillStreak();
            SaveManager.AddDistance(RunDistance);
            SaveManager.SaveIfDirty();
            OnGameOver?.Invoke();
        }

        private void HandlePlayerRevive()
        {
            State = GameState.Playing;
            cameraManager?.SetState(CameraState.Gameplay);
            Sound?.PlayRevive();
            Sound?.SetMusicGameplay(true);
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
            Sound?.PlayBiomeTransition();
            OnBiomeChanged?.Invoke(biome);
        }

        public void StartGame()
        {
            if (State != GameState.Ready) return;

            State = GameState.Playing;
            Score = 0;
            lastMilestone = 0;
            lastScoredZ = Player != null ? Player.transform.position.z : 0f;
            ResetPowerups();
            SaveManager.ResetKillStreak();

            Player?.StartRunning();
            InputReader?.EnableGameplayInput();
            cameraManager?.SetState(CameraState.Gameplay);

            Sound?.PlayGameStart();
            Sound?.SetMusicGameplay(true);
            OnScoreChanged?.Invoke(Score);
            OnGameStarted?.Invoke();
        }

        public void PauseGame()
        {
            if (State != GameState.Playing) return;

            State = GameState.Paused;
            Time.timeScale = 0f;
            InputReader?.DisableGameplayInput();

            Sound?.PlayPause();
            OnGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;

            State = GameState.Playing;
            Time.timeScale = 1f;
            InputReader?.EnableGameplayInput();

            Sound?.PlayResume();
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

            Sound?.SetMusicGameplay(false);
            OnGameOver?.Invoke();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            Score = 0;
            lastMilestone = 0;
            lastScoredZ = 0f;
            startZ = lastDeathZ;
            ResetPowerups();
            SaveManager.ResetKillStreak();

            BulletPool?.ReturnAllBullets();
            Player?.Reset();

            // Use the saved biome state from death, or fall back to starting biome
            if (deathBiome != null && lastDeathZ > 0f)
            {
                BiomeManager?.ResetAtDeath(deathBiome, lastDeathZ, deathNextBiome);
            }
            else
            {
                BiomeData startingBiome = sceneSetup?.StartingBiome;
                BiomeManager?.Reset(startingBiome);
            }

            LevelGenerator?.Reset(lastDeathZ);
            FogController?.Reset();
            SkyController?.Reset();

            if (Player != null)
            {
                // If we have a valid death position, spawn there; otherwise use spawn point
                if (lastDeathZ > 0f)
                {
                    Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
                    Player.transform.position = new Vector3(spawnPos.x, spawnPos.y, lastDeathZ);
                }
                else if (playerSpawnPoint != null)
                {
                    Player.transform.position = playerSpawnPoint.position;
                }
            }

            // Clear death state for next game
            lastDeathZ = 0f;
            deathBiome = null;
            deathNextBiome = null;

            cameraManager?.SetState(CameraState.Menu);
            Sound?.SetMusicGameplay(false);
            State = GameState.Ready;

            OnGameRestarted?.Invoke();
        }

        public void AddScore(int amount)
        {
            int multipliedAmount = amount * ScoreMultiplier;
            Score += multipliedAmount;

            OnScoreChanged?.Invoke(Score);

            int currentMilestone = Score / scoreMilestoneInterval;
            if (currentMilestone > lastMilestone)
            {
                lastMilestone = currentMilestone;
                Sound?.PlayMilestone();
            }
        }

        public void ActivateMultiplier(int multiplier, float duration)
        {
            ScoreMultiplier = multiplier;
            multiplierTimer = duration;
            multiplierDuration = duration;
            Sound?.PlayPowerupCollect();
            OnMultiplierChanged?.Invoke(ScoreMultiplier);
        }

        public void ActivateMagnet(float duration)
        {
            IsMagnetActive = true;
            magnetTimer = duration;
            magnetDuration = duration;
            Sound?.PlayPowerupCollect();
            OnMagnetChanged?.Invoke(true);
        }

        public void ActivateSpeedBoost(float duration)
        {
            IsSpeedBoostActive = true;
            speedBoostTimer = duration;
            speedBoostDuration = duration;
            GameSpeed = speedBoostMultiplier;
            Sound?.PlayPowerupCollect();
            OnSpeedBoostChanged?.Invoke(true);
        }

        private void ResetPowerups()
        {
            ScoreMultiplier = 1;
            multiplierTimer = 0f;
            multiplierDuration = 0f;
            IsMagnetActive = false;
            magnetTimer = 0f;
            magnetDuration = 0f;
            IsSpeedBoostActive = false;
            speedBoostTimer = 0f;
            speedBoostDuration = 0f;
            GameSpeed = 1f;
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