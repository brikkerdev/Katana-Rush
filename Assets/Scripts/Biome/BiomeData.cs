using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Runner.LevelGeneration
{
    [CreateAssetMenu(fileName = "BiomeData", menuName = "Runner/Biome Data")]
    public class BiomeData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string biomeName = "New Biome";
        [SerializeField] private Color debugColor = Color.white;

        [Header("Segment Nodes (node-based generation)")]
        [SerializeField] private SegmentNodeData[] segmentNodes;

        [Header("Editor State")]
        [SerializeField] private Vector2 editorScrollPosition;
        [SerializeField] private float editorZoom = 1f;

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

        public string BiomeName 
        {
            get => biomeName;
            set => biomeName = value;
        }
        public Color DebugColor 
        {
            get => debugColor;
            set => debugColor = value;
        }
        public SegmentNodeData[] SegmentNodes 
        {
            get => segmentNodes;
            set => segmentNodes = value;
        }
        public BiomeTransition[] Transitions => transitions;
        public GameObject EnvironmentPrefab 
        {
            get => environmentPrefab;
            set => environmentPrefab = value;
        }
        public Vector3 EnvironmentOffset => environmentOffset;
        public GameObject BackgroundImagePrefab 
        {
            get => backgroundImagePrefab;
            set => backgroundImagePrefab = value;
        }
        public Vector3 BackgroundImageOffset => backgroundImageOffset;
        public float BackgroundImageMoveSpeed => backgroundImageMoveSpeed;
        public float MinLength 
        {
            get => minLength;
            set => minLength = value;
        }
        public float MaxLength 
        {
            get => maxLength;
            set => maxLength = value;
        }
        public float MinDifficulty 
        {
            get => minDifficulty;
            set => minDifficulty = value;
        }
        public float MaxDifficulty 
        {
            get => maxDifficulty;
            set => maxDifficulty = value;
        }
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

        public Vector2 EditorScrollPosition
        {
            get => editorScrollPosition;
            set => editorScrollPosition = value;
        }

        public float EditorZoom
        {
            get => editorZoom;
            set => editorZoom = value;
        }

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

        public LevelSegment GetNextSegment(int currentNodeIndex)
        {
            if (segmentNodes == null || segmentNodes.Length == 0)
            {
                return null;
            }

            if (currentNodeIndex < 0 || currentNodeIndex >= segmentNodes.Length)
            {
                return GetRandomNodeWithCooldown();
            }

            var currentNode = segmentNodes[currentNodeIndex];
            
            for (int i = 0; i < segmentNodes.Length; i++)
            {
                if (segmentNodes[i].CurrentCooldown > 0)
                {
                    segmentNodes[i].CurrentCooldown--;
                }
            }

            if (currentNode.HasConnections)
            {
                return GetWeightedChildSegment(currentNode);
            }
            
            return GetRandomNodeWithCooldown();
        }

        private LevelSegment GetWeightedChildSegment(SegmentNodeData parentNode)
        {
            if (parentNode.Connections == null || parentNode.Connections.Length == 0)
            {
                return parentNode.Segment;
            }

            float totalWeight = 0f;
            foreach (int childIndex in parentNode.Connections)
            {
                if (childIndex >= 0 && childIndex < segmentNodes.Length)
                {
                    totalWeight += segmentNodes[childIndex].Weight;
                }
            }

            if (totalWeight <= 0f)
            {
                int randomChild = parentNode.Connections[Random.Range(0, parentNode.Connections.Length)];
                if (randomChild >= 0 && randomChild < segmentNodes.Length)
                {
                    return segmentNodes[randomChild].Segment;
                }
                return parentNode.Segment;
            }

            float randomValue = Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            foreach (int childIndex in parentNode.Connections)
            {
                if (childIndex < 0 || childIndex >= segmentNodes.Length) continue;

                cumulativeWeight += segmentNodes[childIndex].Weight;
                if (randomValue <= cumulativeWeight)
                {
                    segmentNodes[childIndex].CurrentCooldown = segmentNodes[childIndex].Cooldown;
                    return segmentNodes[childIndex].Segment;
                }
            }

            return parentNode.Segment;
        }

        private LevelSegment GetRandomNodeWithCooldown()
        {
            if (segmentNodes == null || segmentNodes.Length == 0) return null;

            List<SegmentNodeData> availableNodes = new List<SegmentNodeData>();

            foreach (var node in segmentNodes)
            {
                if (node.CurrentCooldown <= 0 && node.Segment != null)
                {
                    availableNodes.Add(node);
                }
            }

            if (availableNodes.Count == 0)
            {
                return segmentNodes[Random.Range(0, segmentNodes.Length)].Segment;
            }

            float totalWeight = 0f;
            foreach (var node in availableNodes)
            {
                totalWeight += node.Weight;
            }

            if (totalWeight <= 0f)
            {
                var randomNode = availableNodes[Random.Range(0, availableNodes.Count)];
                randomNode.CurrentCooldown = randomNode.Cooldown;
                return randomNode.Segment;
            }

            float randomValue = Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            foreach (var node in availableNodes)
            {
                cumulativeWeight += node.Weight;
                if (randomValue <= cumulativeWeight)
                {
                    node.CurrentCooldown = node.Cooldown;
                    return node.Segment;
                }
            }

            return availableNodes[Random.Range(0, availableNodes.Count)].Segment;
        }

        public void ResetNodeCooldowns()
        {
            if (segmentNodes == null) return;

            foreach (var node in segmentNodes)
            {
                if (node != null)
                {
                    node.CurrentCooldown = 0;
                }
            }
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
            ValidateSegmentNodes();
        }

        private void ValidateSegmentNodes()
        {
            if (segmentNodes == null) return;

            for (int i = 0; i < segmentNodes.Length; i++)
            {
                if (segmentNodes[i] == null)
                {
                    segmentNodes[i] = new SegmentNodeData(i);
                }
                else
                {
                    segmentNodes[i].NodeIndex = i;
                }
            }
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
    public class SegmentNodeData
    {
        [SerializeField] private int nodeIndex;
        [SerializeField] private LevelSegment segment;
        [SerializeField] private string nodeName = "Node";
        [SerializeField] private Vector2 nodePosition;
        [SerializeField] private bool isStartNode;
        [SerializeField] private bool isEndNode;
        [SerializeField] private int[] connections = new int[0];
        [SerializeField] private float weight = 1f;
        [SerializeField] private int cooldown = 0;

        private int currentCooldown;

        public int NodeIndex
        {
            get => nodeIndex;
            set => nodeIndex = value;
        }

        public LevelSegment Segment 
        {
            get => segment;
            set => segment = value;
        }

        public string NodeName
        {
            get => nodeName;
            set => nodeName = value;
        }
        public Vector2 NodePosition
        {
            get => nodePosition;
            set => nodePosition = value;
        }

        public bool IsStartNode
        {
            get => isStartNode;
            set => isStartNode = value;
        }

        public bool IsEndNode
        {
            get => isEndNode;
            set => isEndNode = value;
        }

        public int[] Connections 
        {
            get => connections;
            set => connections = value;
        }

        public float Weight
        {
            get => weight;
            set => weight = value;
        }

        public int Cooldown
        {
            get => cooldown;
            set => cooldown = value;
        }

        public int CurrentCooldown
        {
            get => currentCooldown;
            set => currentCooldown = value;
        }

        public bool HasConnections => connections != null && connections.Length > 0;

        public SegmentNodeData(int index)
        {
            nodeIndex = index;
            nodeName = $"Segment {index}";
            nodePosition = new Vector2(100 + (index % 4) * 280, 100 + (index / 4) * 180);
        }

        public void AddConnection(int targetNodeIndex)
        {
            if (connections == null)
            {
                connections = new int[0];
            }

            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i] == targetNodeIndex)
                {
                    return;
                }
            }

            int[] newConnections = new int[connections.Length + 1];
            for (int i = 0; i < connections.Length; i++)
            {
                newConnections[i] = connections[i];
            }
            newConnections[connections.Length] = targetNodeIndex;
            connections = newConnections;
        }

        public void RemoveConnection(int targetNodeIndex)
        {
            if (connections == null || connections.Length == 0) return;

            List<int> newConnections = new List<int>();
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i] != targetNodeIndex)
                {
                    newConnections.Add(connections[i]);
                }
            }
            connections = newConnections.ToArray();
        }

        public bool HasConnectionTo(int targetNodeIndex)
        {
            if (connections == null) return false;

            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i] == targetNodeIndex)
                {
                    return true;
                }
            }
            return false;
        }
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