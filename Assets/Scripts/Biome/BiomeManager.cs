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
        private float currentBiomeStartZ;
        private float currentBiomeEndZ;
        private float spawnedLengthInCurrentBiome;
        private bool environmentSpawnedForNextBiome;

        private BiomeData visualCurrentBiome;
        private BiomeData visualNextBiome;
        private float visualTransitionZ;
        private bool visualTransitionPending;

        [Header("Biome Repetition")]
        [SerializeField] private float repeatProbability = 0.5f;
        private int consecutiveRepeats = 0;

        private List<BiomeEnvironment> activeEnvironments = new List<BiomeEnvironment>();

        private const int SEGMENTS_PER_BIOME = 16;
        private const int REGULAR_SEGMENTS = 15;
        private const float SEGMENT_LENGTH = 100f;
        private const float BIOME_LENGTH = SEGMENTS_PER_BIOME * SEGMENT_LENGTH;

        private List<LevelSegment> preGeneratedSegments = new List<LevelSegment>();
        private int currentSegmentIndex = 0;

        public event Action<BiomeData> OnBiomeChanged;
        public event Action<BiomeData, BiomeData> OnBiomeTransitionStarted;

        public BiomeData CurrentBiome => currentBiome;
        public BiomeData VisualCurrentBiome => visualCurrentBiome;
        public BiomeData NextBiome => nextBiome;
        public float CurrentBiomeStartZ => currentBiomeStartZ;
        public float CurrentBiomeEndZ => currentBiomeEndZ;
        public float RemainingBiomeLength => BIOME_LENGTH - spawnedLengthInCurrentBiome;
        public int RemainingSegmentCount => SEGMENTS_PER_BIOME - currentSegmentIndex;
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

            PrepareNextBiomeFrom(startingBiome);
            SetupBiome(startingBiome, 0f);

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

            ApplyVisualOverrides(visualCurrentBiome);

            IsInitialized = true;

            if (showDebug)
                Debug.Log($"[BiomeManager] Initialized with {currentBiome.BiomeName}");
        }

        private void SetupBiome(BiomeData biome, float startZ)
        {
            currentBiome = biome;
            currentBiomeStartZ = startZ;
            currentBiomeEndZ = startZ + BIOME_LENGTH;
            spawnedLengthInCurrentBiome = 0f;
            environmentSpawnedForNextBiome = false;

            PreGenerateBiomeSegments();
        }

        private void PreGenerateBiomeSegments()
        {
            preGeneratedSegments.Clear();
            currentSegmentIndex = 0;

            if (currentBiome == null)
            {
                if (showDebug)
                    Debug.LogWarning("[BiomeManager] Current biome is null during pre-generation");
                return;
            }

            // Use the biome's own generation logic to fill REGULAR_SEGMENTS slots
            List<LevelSegment> generated = currentBiome.GenerateSegmentOrder(REGULAR_SEGMENTS);
            preGeneratedSegments.AddRange(generated);

            // Add transition segment as the 16th
            AddTransitionSegment();

            if (showDebug)
            {
                Debug.Log($"[BiomeManager] Pre-generated {preGeneratedSegments.Count} segments for {currentBiome.BiomeName}");
                for (int i = 0; i < preGeneratedSegments.Count; i++)
                {
                    Debug.Log($"  [{i}] {(preGeneratedSegments[i] != null ? preGeneratedSegments[i].name : "NULL")}");
                }
            }
        }

        private void AddTransitionSegment()
        {
            if (nextBiome == null)
            {
                // No next biome — add a regular segment if possible
                if (preGeneratedSegments.Count > 0)
                    preGeneratedSegments.Add(preGeneratedSegments[preGeneratedSegments.Count - 1]);
                return;
            }

            var transition = currentBiome.GetTransitionTo(nextBiome);
            if (transition != null)
            {
                LevelSegment exitSegment = transition.GetRandomExitSegment();
                if (exitSegment != null)
                {
                    preGeneratedSegments.Add(exitSegment);
                    return;
                }
            }

            // Fallback: duplicate last segment
            if (preGeneratedSegments.Count > 0)
                preGeneratedSegments.Add(preGeneratedSegments[preGeneratedSegments.Count - 1]);
        }

        private void PrepareNextBiomeFrom(BiomeData fromBiome)
        {
            if (fromBiome == null) return;

            float difficulty = player != null ? player.position.z / 1000f : 0f;
            BiomeData potentialNextBiome = fromBiome.GetRandomNextBiome(difficulty);

            bool shouldRepeat = UnityEngine.Random.value < repeatProbability;

            if (shouldRepeat)
            {
                nextBiome = fromBiome;
                repeatProbability *= 0.5f;
                consecutiveRepeats++;

                if (showDebug)
                    Debug.Log($"[BiomeManager] Repeating biome: {fromBiome.BiomeName}, new probability: {repeatProbability}");
            }
            else
            {
                nextBiome = potentialNextBiome ?? fromBiome;
                repeatProbability = 0.5f;
                consecutiveRepeats = 0;

                if (showDebug && nextBiome != null)
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
            if (!visualTransitionPending || visualNextBiome == null) return;

            if (player.position.z >= visualTransitionZ)
                PerformVisualTransition();
        }

        private void PerformVisualTransition()
        {
            BiomeData previousVisual = visualCurrentBiome;
            visualCurrentBiome = visualNextBiome;
            visualTransitionPending = false;
            visualNextBiome = null;

            if (backgroundController != null && visualCurrentBiome != null)
            {
                backgroundController.TriggerMoveDown();
                backgroundController.SpawnBackgroundForBiome(visualCurrentBiome, player.position.z, true);
            }

            ApplyVisualOverrides(visualCurrentBiome);
            OnBiomeChanged?.Invoke(visualCurrentBiome);

            if (showDebug)
                Debug.Log($"[BiomeManager] Visual transition: {previousVisual?.BiomeName} -> {visualCurrentBiome.BiomeName} at Z={player.position.z}");
        }

        private void ApplyVisualOverrides(BiomeData biome)
        {
            if (biome == null) return;

            if (DayNightCycle.Instance != null)
                DayNightCycle.Instance.ApplyBiomeTimeOverride(biome);

            if (FogController.Instance != null)
                FogController.Instance.ApplyBiomeOverride(biome);
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
            if (currentSegmentIndex >= SEGMENTS_PER_BIOME)
                SwitchToNextBiome();

            return GetNextPreGeneratedSegment();
        }

        private LevelSegment GetNextPreGeneratedSegment()
        {
            if (currentSegmentIndex >= preGeneratedSegments.Count)
            {
                if (showDebug)
                    Debug.LogWarning("[BiomeManager] No more pre-generated segments!");
                return null;
            }

            LevelSegment segment = preGeneratedSegments[currentSegmentIndex];
            currentSegmentIndex++;
            spawnedLengthInCurrentBiome += segment.Length;

            if (showDebug)
                Debug.Log($"[BiomeManager] Segment {currentSegmentIndex}/{SEGMENTS_PER_BIOME}: {segment.name}");

            return segment;
        }

        private void ScheduleVisualTransition(BiomeData toBiome, float atZ)
        {
            visualNextBiome = toBiome;
            visualTransitionZ = atZ;
            visualTransitionPending = true;

            if (showDebug)
                Debug.Log($"[BiomeManager] Scheduled visual transition to {toBiome.BiomeName} at Z={atZ}");
        }

        private void SwitchToNextBiome()
        {
            if (nextBiome == null)
            {
                Debug.LogError("[BiomeManager] No next biome!");
                return;
            }

            float newStartZ = currentBiomeEndZ;
            BiomeData switchingToBiome = nextBiome;

            if (showDebug)
                Debug.Log($"[BiomeManager] Switching to {switchingToBiome.BiomeName} at Z={newStartZ}");

            OnBiomeTransitionStarted?.Invoke(currentBiome, switchingToBiome);
            ScheduleVisualTransition(switchingToBiome, newStartZ);

            if (backgroundController != null)
                backgroundController.TriggerMoveUp(newStartZ);

            PrepareNextBiomeFrom(switchingToBiome);
            SetupBiome(switchingToBiome, newStartZ);
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
                reveal.PlayReveal(0.1f);

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
                        Debug.Log($"[BiomeManager] Despawned environment at Z={environment.SpawnZ}");

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
            CleanupAllEnvironments();

            if (startingBiome == null)
            {
                Debug.LogError("[BiomeManager] Reset called with null startingBiome!");
                return;
            }

            repeatProbability = 0.5f;
            consecutiveRepeats = 0;

            PrepareNextBiomeFrom(startingBiome);
            SetupBiome(startingBiome, startZ);

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

            ApplyVisualOverrides(visualCurrentBiome);
            OnBiomeChanged?.Invoke(visualCurrentBiome);

            if (showDebug)
                Debug.Log($"[BiomeManager] Reset complete at Z={startZ}");
        }

        public void ResetAtDeath(BiomeData biomeToStartWith)
        {
            CleanupAllEnvironments();

            if (biomeToStartWith == null)
            {
                Debug.LogError("[BiomeManager] ResetAtDeath called with null biome!");
                return;
            }

            repeatProbability = 0.5f;
            consecutiveRepeats = 0;

            PrepareNextBiomeFrom(biomeToStartWith);
            SetupBiome(biomeToStartWith, 0f);

            visualCurrentBiome = biomeToStartWith;
            visualNextBiome = null;
            visualTransitionZ = 0f;
            visualTransitionPending = false;

            SpawnEnvironmentForBiome(currentBiome, 0f);

            if (backgroundController != null)
            {
                backgroundController.ResetBackground();
                backgroundController.SpawnBackgroundForBiome(biomeToStartWith, 0f);
            }

            ApplyVisualOverrides(visualCurrentBiome);
            OnBiomeChanged?.Invoke(visualCurrentBiome);

            if (showDebug)
                Debug.Log($"[BiomeManager] ResetAtDeath complete - starting with {biomeToStartWith.BiomeName}");
        }

        private void CleanupAllEnvironments()
        {
            for (int i = activeEnvironments.Count - 1; i >= 0; i--)
            {
                if (activeEnvironments[i] != null)
                    Destroy(activeEnvironments[i].gameObject);
            }
            activeEnvironments.Clear();
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
                    Gizmos.DrawWireCube(env.transform.position, Vector3.one * 5f);
            }
        }
#endif
    }
}