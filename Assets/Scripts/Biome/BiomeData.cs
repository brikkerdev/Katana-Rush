using UnityEngine;
using System.Collections.Generic;

namespace Runner.LevelGeneration
{
    [CreateAssetMenu(fileName = "BiomeData", menuName = "Runner/Biome Data")]
    public class BiomeData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string biomeName = "New Biome";
        [SerializeField] private Color debugColor = Color.white;

        [Header("Segments")]
        [SerializeField] private LevelSegment[] segments;

        [Header("Transitions")]
        [SerializeField] private BiomeTransition[] transitions;

        [Header("Environment")]
        [SerializeField] private GameObject environmentPrefab;
        [SerializeField] private Vector3 environmentOffset = Vector3.zero;

        [Header("Background Image")]
        [SerializeField] private GameObject backgroundImagePrefab;
        [SerializeField] private Vector3 backgroundImageOffset = new Vector3(0f, 10f, 0f);
        [SerializeField] private float backgroundImageMoveSpeed = 2f;

        [Header("Length")]
        [SerializeField] private float minLength = 200f;
        [SerializeField] private float maxLength = 400f;

        [Header("Difficulty")]
        [SerializeField, Range(0f, 1f)] private float minDifficulty = 0f;
        [SerializeField, Range(0f, 1f)] private float maxDifficulty = 1f;
        [SerializeField] private float enemySpawnMultiplier = 1f;

        [Header("Fog")]
        [SerializeField] private bool overrideFog = true;
        [SerializeField] private Color fogColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private float fogDensity = 0.02f;

        [Header("Lighting")]
        [SerializeField] private Color ambientColor = Color.gray;
        [SerializeField] private float ambientIntensity = 1f;

        [Header("Sky")]
        [SerializeField] private Color skyDayTint = Color.white;
        [SerializeField] private Color skyNightTint = Color.white;
        [SerializeField, Range(0f, 1f)] private float skyTintStrength = 0f;

        [Header("Day/Night Override")]
        [SerializeField] private TimeOverrideMode timeOverride = TimeOverrideMode.None;
        [SerializeField, Range(0f, 1f)] private float forcedTime = 0.5f;
        [SerializeField] private bool pauseCycleDuringOverride = true;
        [SerializeField] private float timeTransitionDuration = 2f;

        public string BiomeName => biomeName;
        public Color DebugColor => debugColor;
        public LevelSegment[] Segments => segments;
        public BiomeTransition[] Transitions => transitions;
        public GameObject EnvironmentPrefab => environmentPrefab;
        public Vector3 EnvironmentOffset => environmentOffset;
        public GameObject BackgroundImagePrefab => backgroundImagePrefab;
        public Vector3 BackgroundImageOffset => backgroundImageOffset;
        public float BackgroundImageMoveSpeed => backgroundImageMoveSpeed;
        public float MinLength => minLength;
        public float MaxLength => maxLength;
        public float MinDifficulty => minDifficulty;
        public float MaxDifficulty => maxDifficulty;
        public float EnemySpawnMultiplier => enemySpawnMultiplier;
        public bool OverrideFog => overrideFog;
        public Color FogColor => fogColor;
        public float FogDensity => fogDensity;
        public Color AmbientColor => ambientColor;
        public float AmbientIntensity => ambientIntensity;
        public Color SkyDayTint => skyDayTint;
        public Color SkyNightTint => skyNightTint;
        public float SkyTintStrength => skyTintStrength;
        public TimeOverrideMode TimeOverride => timeOverride;
        public float ForcedTime => forcedTime;
        public bool PauseCycleDuringOverride => pauseCycleDuringOverride;
        public float TimeTransitionDuration => timeTransitionDuration;
        public bool HasTimeOverride => timeOverride != TimeOverrideMode.None;

        public float GetForcedTimeValue()
        {
            switch (timeOverride)
            {
                case TimeOverrideMode.ForceDay:
                    return 0.5f;
                case TimeOverrideMode.ForceNight:
                    return 0.0f;
                case TimeOverrideMode.ForceSunrise:
                    return 0.25f;
                case TimeOverrideMode.ForceSunset:
                    return 0.75f;
                case TimeOverrideMode.Custom:
                    return forcedTime;
                default:
                    return -1f;
            }
        }

        public float GetRandomLength()
        {
            return Random.Range(minLength, maxLength);
        }

        public LevelSegment GetRandomSegment()
        {
            if (segments == null || segments.Length == 0) return null;
            return segments[Random.Range(0, segments.Length)];
        }

        public LevelSegment GetRandomSegment(LevelSegment exclude)
        {
            if (segments == null || segments.Length == 0) return null;
            if (segments.Length == 1) return segments[0];

            int attempts = 10;
            LevelSegment selected;

            do
            {
                selected = segments[Random.Range(0, segments.Length)];
                attempts--;
            }
            while (selected == exclude && attempts > 0);

            return selected;
        }

        public BiomeTransition GetTransitionTo(BiomeData targetBiome)
        {
            if (transitions == null) return null;

            foreach (var transition in transitions)
            {
                if (transition.TargetBiome == targetBiome)
                {
                    return transition;
                }
            }

            return null;
        }

        public BiomeData GetRandomNextBiome()
        {
            if (transitions == null || transitions.Length == 0) return null;
            return transitions[Random.Range(0, transitions.Length)].TargetBiome;
        }

        public BiomeData GetRandomNextBiome(float currentDifficulty)
        {
            if (transitions == null || transitions.Length == 0) return null;

            List<BiomeData> validBiomes = new List<BiomeData>();

            foreach (var transition in transitions)
            {
                if (transition.TargetBiome == null) continue;

                var target = transition.TargetBiome;
                if (currentDifficulty >= target.MinDifficulty && currentDifficulty <= target.MaxDifficulty)
                {
                    validBiomes.Add(target);
                }
            }

            if (validBiomes.Count == 0)
            {
                return transitions[Random.Range(0, transitions.Length)].TargetBiome;
            }

            return validBiomes[Random.Range(0, validBiomes.Count)];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (minLength > maxLength) maxLength = minLength;
            if (minDifficulty > maxDifficulty) maxDifficulty = minDifficulty;
        }
#endif
    }

    public enum TimeOverrideMode
    {
        None,
        ForceDay,
        ForceNight,
        ForceSunrise,
        ForceSunset,
        Custom
    }

    [System.Serializable]
    public class BiomeTransition
    {
        [SerializeField] private BiomeData targetBiome;
        [SerializeField] private LevelSegment[] exitSegments;

        public BiomeData TargetBiome => targetBiome;
        public LevelSegment[] ExitSegments => exitSegments;

        public LevelSegment GetRandomExitSegment()
        {
            if (exitSegments == null || exitSegments.Length == 0) return null;
            return exitSegments[Random.Range(0, exitSegments.Length)];
        }
    }
}