using UnityEngine;
using System;
using System.Collections.Generic;

namespace Runner.LevelGeneration
{
    public class BiomeManager : MonoBehaviour
    {
        public static BiomeManager Instance { get; private set; }

        [Header("Environment")]
        [SerializeField] private float environmentPreSpawnDistance = 50f;
        [SerializeField] private float environmentDespawnDistance = 100f;

        [Header("Fog")]
        [SerializeField] private float fogTransitionSpeed = 1f;

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

        private Color currentFogColor;
        private float currentFogDensity;
        private Color targetFogColor;
        private float targetFogDensity;
        private Color currentAmbientColor;
        private Color targetAmbientColor;

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
            ApplyBiomeSettings(currentBiome, true);

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
            UpdateFogTransition();
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

            targetFogColor = visualCurrentBiome.FogColor;
            targetFogDensity = visualCurrentBiome.FogDensity;
            targetAmbientColor = visualCurrentBiome.AmbientColor * visualCurrentBiome.AmbientIntensity;

            if (visualCurrentBiome.SkyboxMaterial != null)
            {
                RenderSettings.skybox = visualCurrentBiome.SkyboxMaterial;
            }

            visualTransitionPending = false;
            visualNextBiome = null;

            OnBiomeChanged?.Invoke(visualCurrentBiome);

            if (showDebug)
            {
                Debug.Log($"[BiomeManager] Visual transition to {visualCurrentBiome.BiomeName} at Z={player.position.z}");
            }
        }

        private void UpdateFogTransition()
        {
            float speed = fogTransitionSpeed * Time.deltaTime;

            currentFogColor = Color.Lerp(currentFogColor, targetFogColor, speed);
            currentFogDensity = Mathf.Lerp(currentFogDensity, targetFogDensity, speed);
            currentAmbientColor = Color.Lerp(currentAmbientColor, targetAmbientColor, speed);

            RenderSettings.fogColor = currentFogColor;
            RenderSettings.fogDensity = currentFogDensity;
            RenderSettings.ambientLight = currentAmbientColor;
        }

        private void CheckEnvironmentPreSpawn()
        {
            if (environmentSpawnedForNextBiome) return;
            if (nextBiome == null) return;

            float distanceToEnd = currentBiomeEndZ - player.position.z;

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
                {
                    Debug.Log($"[BiomeManager] {biome.BiomeName} has no environment prefab");
                }
                return;
            }

            GameObject go = Instantiate(biome.EnvironmentPrefab, transform);
            go.name = $"Environment_{biome.BiomeName}_{zPosition}";

            BiomeEnvironment environment = go.GetComponent<BiomeEnvironment>();
            if (environment == null)
            {
                environment = go.AddComponent<BiomeEnvironment>();
            }

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

            if (showDebug)
            {
                Debug.Log($"[BiomeManager] Spawned {biome.BiomeName} environment at Z={zPosition}");
            }
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

        private void ApplyBiomeSettings(BiomeData biome, bool immediate)
        {
            targetFogColor = biome.FogColor;
            targetFogDensity = biome.FogDensity;
            targetAmbientColor = biome.AmbientColor * biome.AmbientIntensity;

            if (biome.SkyboxMaterial != null)
            {
                RenderSettings.skybox = biome.SkyboxMaterial;
            }

            if (immediate)
            {
                currentFogColor = targetFogColor;
                currentFogDensity = targetFogDensity;
                currentAmbientColor = targetAmbientColor;

                RenderSettings.fog = biome.OverrideFog;
                RenderSettings.fogColor = currentFogColor;
                RenderSettings.fogDensity = currentFogDensity;
                RenderSettings.ambientLight = currentAmbientColor;
            }
        }

        public void Reset(BiomeData startingBiome)
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

            SetupBiome(startingBiome, 0f);
            PrepareNextBiome();

            visualCurrentBiome = startingBiome;
            visualNextBiome = null;
            visualTransitionZ = 0f;
            visualTransitionPending = false;

            SpawnEnvironmentForBiome(currentBiome, 0f);
            ApplyBiomeSettings(currentBiome, true);

            if (showDebug)
            {
                Debug.Log("[BiomeManager] Reset complete");
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
                UnityEditor.Handles.Label(
                    new Vector3(-8f, 3f, visualTransitionZ),
                    $"Visual transition to {visualNextBiome?.BiomeName}"
                );
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