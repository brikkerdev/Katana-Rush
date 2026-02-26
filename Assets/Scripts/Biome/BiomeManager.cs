using Runner.Environment;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runner.LevelGeneration
{
    public class BiomeManager : MonoBehaviour
    {
        public static BiomeManager Instance { get; private set; }

        [Header("Environment")]
        [SerializeField] private float environmentPreSpawnDistance = 50f;
        [SerializeField] private float environmentDespawnDistance = 100f;

        [Header("Background")]
        [SerializeField] private BiomeBackgroundController backgroundController;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private Transform player;

        private BiomeData currentBiome;
        private BiomeData nextBiome;
        private float currentBiomeTargetLength;
        private float spawnedLengthInCurrentBiome;
        private float currentBiomeStartZ;
        private float currentBiomeEndZ;
        private bool transitionSegmentSpawned;
        private bool environmentSpawnedForNextBiome;

        private BiomeData visualCurrentBiome;
        private BiomeData visualNextBiome;
        private float visualTransitionZ;
        private bool visualTransitionPending;

        private List<BiomeEnvironment> activeEnvironments = new List<BiomeEnvironment>();

        public event Action<BiomeData> OnBiomeChanged;
        public event Action<BiomeData, BiomeData> OnBiomeTransitionStarted;

        public BiomeData CurrentBiome => currentBiome;
        public BiomeData VisualCurrentBiome => visualCurrentBiome;
        public BiomeData NextBiome => nextBiome;
        public float CurrentBiomeStartZ => currentBiomeStartZ;
        public float CurrentBiomeEndZ => currentBiomeEndZ;
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize(Transform playerTransform, BiomeData startingBiome)
        {
            player = playerTransform;

            if (startingBiome == null)
            {
                Debug.LogError("[BiomeManager] Starting biome is null!");
                return;
            }

            if (player == null)
            {
                Debug.LogError("[BiomeManager] Player transform is null!");
                return;
            }

            SetupBiome(startingBiome, 0f);
            PrepareNextBiome();

            visualCurrentBiome = startingBiome;
            visualNextBiome = null;
            visualTransitionZ = 0f;
            visualTransitionPending = false;

            SpawnEnvironmentForBiome(currentBiome, 0f);

            if (backgroundController != null)
            {
                backgroundController.Initialize(player);
                backgroundController.SpawnBackgroundForBiome(currentBiome, 0f);
            }

            if (DayNightCycle.Instance != null)
            {
                DayNightCycle.Instance.ApplyBiomeTimeOverride(visualCurrentBiome);
            }

            IsInitialized = true;

            if (showDebug)
            {
                Debug.Log($"[BiomeManager] Initialized with {currentBiome.BiomeName}");
            }
        }

        private void SetupBiome(BiomeData biome, float startZ)
        {
            currentBiome = biome;
            currentBiomeStartZ = startZ;
            currentBiomeTargetLength = biome.GetRandomLength();
            currentBiomeEndZ = startZ + currentBiomeTargetLength;
            spawnedLengthInCurrentBiome = 0f;
            transitionSegmentSpawned = false;
            environmentSpawnedForNextBiome = false;
        }

        private void PrepareNextBiome()
        {
            float difficulty = player != null ? player.position.z / 1000f : 0f;
            nextBiome = currentBiome.GetRandomNextBiome(difficulty);

            if (showDebug && nextBiome != null)
            {
                Debug.Log($"[BiomeManager] Next biome prepared: {nextBiome.BiomeName}");
            }
        }

        private void Update()
        {
            if (!IsInitialized || player == null) return;

            CheckVisualTransition();
            CheckEnvironmentPreSpawn();
            CleanupOldEnvironments();
        }

        private void CheckVisualTransition()
        {
            if (!visualTransitionPending) return;
            if (visualNextBiome == null) return;

            if (player.position.z >= visualTransitionZ)
            {
                StartVisualTransition();
            }
        }

        private void StartVisualTransition()
        {
            visualCurrentBiome = visualNextBiome;

            visualTransitionPending = false;
            visualNextBiome = null;

            if (backgroundController != null && visualCurrentBiome != null)
            {
                backgroundController.TriggerMoveDown();
                backgroundController.SpawnBackgroundForBiome(visualCurrentBiome, player.position.z, true);
            }

            OnBiomeChanged?.Invoke(visualCurrentBiome);

            if (DayNightCycle.Instance != null)
            {
                DayNightCycle.Instance.ApplyBiomeTimeOverride(visualCurrentBiome);
            }

            if (showDebug)
            {
                Debug.Log($"[BiomeManager] Visual transition to {visualCurrentBiome.BiomeName} at Z={player.position.z}");
            }
        }

        private void CheckEnvironmentPreSpawn()
        {
            if (environmentSpawnedForNextBiome) return;
            if (nextBiome == null) return;

            SpawnEnvironmentForBiome(nextBiome, currentBiomeEndZ);
            environmentSpawnedForNextBiome = true;
        }

        public LevelSegment GetNextSegment(LevelSegment lastSegment)
        {
            if (transitionSegmentSpawned)
            {
                SwitchToNextBiome();
            }

            if (ShouldSpawnTransitionSegment())
            {
                return GetTransitionSegment(lastSegment);
            }

            return GetRegularSegment(lastSegment);
        }

        private bool ShouldSpawnTransitionSegment()
        {
            if (nextBiome == null) return false;
            return spawnedLengthInCurrentBiome >= currentBiomeTargetLength - 100;
        }

        private LevelSegment GetRegularSegment(LevelSegment lastSegment)
        {
            LevelSegment segment = currentBiome.GetRandomSegment(lastSegment);

            if (segment == null)
            {
                Debug.LogError($"[BiomeManager] No segments in {currentBiome.BiomeName}!");
                return null;
            }

            spawnedLengthInCurrentBiome += segment.Length;
            return segment;
        }

        private LevelSegment GetTransitionSegment(LevelSegment lastSegment)
        {
            var transition = currentBiome.GetTransitionTo(nextBiome);

            if (transition == null)
            {
                return GetRegularSegment(lastSegment);
            }

            LevelSegment exitSegment = transition.GetRandomExitSegment();

            if (exitSegment == null)
            {
                return GetRegularSegment(lastSegment);
            }

            transitionSegmentSpawned = true;
            spawnedLengthInCurrentBiome += exitSegment.Length;

            ScheduleVisualTransition(nextBiome, currentBiomeEndZ);

            if (backgroundController != null)
            {
                backgroundController.TriggerMoveUp(currentBiomeEndZ);
            }

            OnBiomeTransitionStarted?.Invoke(currentBiome, nextBiome);

            if (showDebug)
            {
                Debug.Log($"[BiomeManager] Transition segment to {nextBiome.BiomeName}, visual at Z={currentBiomeEndZ}");
            }

            return exitSegment;
        }

        private void ScheduleVisualTransition(BiomeData toBiome, float atZ)
        {
            visualNextBiome = toBiome;
            visualTransitionZ = atZ;
            visualTransitionPending = true;

            if (showDebug)
            {
                Debug.Log($"[BiomeManager] Scheduled visual transition to {toBiome.BiomeName} at Z={atZ}");
            }
        }

        private void SwitchToNextBiome()
        {
            if (nextBiome == null)
            {
                Debug.LogError("[BiomeManager] No next biome!");
                return;
            }

            float newStartZ = currentBiomeStartZ + currentBiomeTargetLength;

            if (showDebug)
            {
                Debug.Log($"[BiomeManager] Segment generation switched to {nextBiome.BiomeName} at Z={newStartZ}");
            }

            SetupBiome(nextBiome, newStartZ);
            PrepareNextBiome();
        }

        private void SpawnEnvironmentForBiome(BiomeData biome, float zPosition)
        {
            if (biome == null)
            {
                Debug.LogError("[BiomeManager] Cannot spawn environment - biome is null");
                return;
            }

            if (biome.EnvironmentPrefab == null)
            {
                if (showDebug)
                    Debug.Log($"[BiomeManager] {biome.BiomeName} has no environment prefab");
                return;
            }

            GameObject go = Instantiate(biome.EnvironmentPrefab, transform);
            go.name = $"Environment_{biome.BiomeName}_{zPosition}";

            BiomeEnvironment environment = go.GetComponent<BiomeEnvironment>();
            if (environment == null)
                environment = go.AddComponent<BiomeEnvironment>();

            Vector3 position = new Vector3(
                biome.EnvironmentOffset.x,
                biome.EnvironmentOffset.y,
                zPosition + biome.EnvironmentOffset.z
            );

            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;
            go.SetActive(true);

            environment.Setup(biome, zPosition);
            activeEnvironments.Add(environment);

            EnvironmentReveal reveal = go.GetComponent<EnvironmentReveal>();
            if (reveal != null)
            {
                reveal.PlayReveal(0.1f);
            }

            if (showDebug)
                Debug.Log($"[BiomeManager] Spawned {biome.BiomeName} environment at Z={zPosition}");
        }

        private void CleanupOldEnvironments()
        {
            for (int i = activeEnvironments.Count - 1; i >= 0; i--)
            {
                var environment = activeEnvironments[i];

                if (environment == null)
                {
                    activeEnvironments.RemoveAt(i);
                    continue;
                }

                if (player.position.z - environment.SpawnZ > environmentDespawnDistance)
                {
                    if (showDebug)
                    {
                        Debug.Log($"[BiomeManager] Despawned environment at Z={environment.SpawnZ}");
                    }

                    Destroy(environment.gameObject);
                    activeEnvironments.RemoveAt(i);
                }
            }
        }

        public void Reset(BiomeData startingBiome)
        {
            Reset(startingBiome, 0f);
        }

        public void Reset(BiomeData startingBiome, float startZ)
        {
            for (int i = activeEnvironments.Count - 1; i >= 0; i--)
            {
                if (activeEnvironments[i] != null)
                {
                    Destroy(activeEnvironments[i].gameObject);
                }
            }
            activeEnvironments.Clear();

            if (startingBiome == null)
            {
                Debug.LogError("[BiomeManager] Reset called with null startingBiome!");
                return;
            }

            SetupBiome(startingBiome, startZ);
            PrepareNextBiome();

            visualCurrentBiome = startingBiome;
            visualNextBiome = null;
            visualTransitionZ = startZ;
            visualTransitionPending = false;

            SpawnEnvironmentForBiome(currentBiome, startZ);

            if (backgroundController != null)
            {
                backgroundController.ResetBackground();
                backgroundController.SpawnBackgroundForBiome(startingBiome, startZ);
            }

            if (DayNightCycle.Instance != null)
            {
                DayNightCycle.Instance.ApplyBiomeTimeOverride(visualCurrentBiome);
            }

            OnBiomeChanged?.Invoke(visualCurrentBiome);

            if (showDebug)
            {
                Debug.Log($"[BiomeManager] Reset complete at Z={startZ}");
            }
        }

        public void ResetAtDeath(BiomeData currentBiomeAtDeath, float deathZ, BiomeData nextBiomeAtDeath)
        {
            for (int i = activeEnvironments.Count - 1; i >= 0; i--)
            {
                if (activeEnvironments[i] != null)
                {
                    Destroy(activeEnvironments[i].gameObject);
                }
            }
            activeEnvironments.Clear();

            if (currentBiomeAtDeath == null)
            {
                Debug.LogError("[BiomeManager] ResetAtDeath called with null currentBiomeAtDeath!");
                return;
            }

            SetupBiome(currentBiomeAtDeath, deathZ);
            
            // Don't call PrepareNextBiome() - we want to continue from the next biome that was active at death
            if (nextBiomeAtDeath != null)
            {
                nextBiome = nextBiomeAtDeath;
            }

            visualCurrentBiome = currentBiomeAtDeath;
            visualNextBiome = nextBiomeAtDeath;
            visualTransitionZ = deathZ;
            visualTransitionPending = false;

            SpawnEnvironmentForBiome(currentBiome, deathZ);

            if (backgroundController != null)
            {
                backgroundController.ResetBackground();
                backgroundController.SpawnBackgroundForBiome(currentBiomeAtDeath, deathZ);
            }

            if (DayNightCycle.Instance != null)
            {
                DayNightCycle.Instance.ApplyBiomeTimeOverride(visualCurrentBiome);
            }

            OnBiomeChanged?.Invoke(visualCurrentBiome);

            if (showDebug)
            {
                Debug.Log($"[BiomeManager] ResetAtDeath complete at Z={deathZ}, biome={currentBiomeAtDeath.BiomeName}");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebug || !IsInitialized || player == null) return;

            Gizmos.color = currentBiome != null ? currentBiome.DebugColor : Color.white;
            Vector3 start = new Vector3(-10f, 0f, currentBiomeStartZ);
            Vector3 end = new Vector3(-10f, 0f, currentBiomeEndZ);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 2f);
            Gizmos.DrawWireSphere(end, 2f);

            Gizmos.color = Color.green;
            float spawnedEnd = currentBiomeStartZ + spawnedLengthInCurrentBiome;
            Gizmos.DrawWireSphere(new Vector3(-10f, 1f, spawnedEnd), 1f);

            if (visualTransitionPending)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(new Vector3(-10f, 2f, visualTransitionZ), 1.5f);
            }

            Gizmos.color = Color.magenta;
            foreach (var env in activeEnvironments)
            {
                if (env != null)
                {
                    Gizmos.DrawWireCube(env.transform.position, Vector3.one * 5f);
                }
            }
        }
#endif
    }
}