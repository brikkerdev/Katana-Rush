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

        private HashSet<int> childNodeIndices;

        public string BiomeName { get => biomeName; set => biomeName = value; }
        public Color DebugColor { get => debugColor; set => debugColor = value; }
        public SegmentNodeData[] SegmentNodes { get => segmentNodes; set => segmentNodes = value; }
        public BiomeTransition[] Transitions => transitions;
        public GameObject EnvironmentPrefab { get => environmentPrefab; set => environmentPrefab = value; }
        public Vector3 EnvironmentOffset => environmentOffset;
        public GameObject BackgroundImagePrefab { get => backgroundImagePrefab; set => backgroundImagePrefab = value; }
        public Vector3 BackgroundImageOffset => backgroundImageOffset;
        public float BackgroundImageMoveSpeed => backgroundImageMoveSpeed;
        public float MinLength { get => minLength; set => minLength = value; }
        public float MaxLength { get => maxLength; set => maxLength = value; }
        public float MinDifficulty { get => minDifficulty; set => minDifficulty = value; }
        public float MaxDifficulty { get => maxDifficulty; set => maxDifficulty = value; }
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
        public Vector2 EditorScrollPosition { get => editorScrollPosition; set => editorScrollPosition = value; }
        public float EditorZoom { get => editorZoom; set => editorZoom = value; }

        public float GetForcedTimeValue()
        {
            switch (timeOverride)
            {
                case TimeOverrideMode.ForceDay: return 0.5f;
                case TimeOverrideMode.ForceNight: return 0.0f;
                case TimeOverrideMode.ForceSunrise: return 0.25f;
                case TimeOverrideMode.ForceSunset: return 0.75f;
                case TimeOverrideMode.Custom: return forcedTime;
                default: return -1f;
            }
        }

        public float GetRandomLength()
        {
            return Random.Range(minLength, maxLength);
        }

        private void BuildChildNodeSet()
        {
            childNodeIndices = new HashSet<int>();

            if (segmentNodes == null) return;

            for (int i = 0; i < segmentNodes.Length; i++)
            {
                var node = segmentNodes[i];
                if (node == null || !node.HasConnections) continue;

                foreach (int childIndex in node.Connections)
                {
                    if (childIndex >= 0 && childIndex < segmentNodes.Length)
                    {
                        childNodeIndices.Add(childIndex);
                    }
                }
            }
        }

        private bool IsValidEntryPoint(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= segmentNodes.Length) return false;

            var node = segmentNodes[nodeIndex];
            if (node == null || node.Segment == null) return false;

            if (childNodeIndices != null && childNodeIndices.Contains(nodeIndex))
                return false;

            return true;
        }

        public List<LevelSegment> GenerateSegmentOrder(int totalSlots)
        {
            List<LevelSegment> result = new List<LevelSegment>();

            if (segmentNodes == null || segmentNodes.Length == 0)
                return result;

            BuildChildNodeSet();

            ResetNodeCooldowns();

            int safety = 500;

            while (result.Count < totalSlots && safety > 0)
            {
                safety--;
                int remaining = totalSlots - result.Count;

                int entryIndex = PickEntryPointNode(remaining);

                if (entryIndex < 0)
                {
                    entryIndex = PickEntryPointNodeIgnoringCooldown(remaining);

                    if (entryIndex < 0)
                    {
                        Debug.LogWarning("[BiomeData] No entry point nodes available at all!");
                        break;
                    }
                }

                var entryNode = segmentNodes[entryIndex];

                if (entryNode.HasConnections)
                {
                    int maxChain = GetMaxChainLength(entryIndex, new HashSet<int>());

                    if (maxChain > remaining)
                    {
                        int standaloneIndex = PickStandaloneEntryPoint();

                        if (standaloneIndex >= 0)
                        {
                            AddNodeToResult(result, standaloneIndex);
                            TickCooldowns();
                            continue;
                        }

                        AddNodeToResult(result, entryIndex);
                        TickCooldowns();
                        continue;
                    }

                    int currentIndex = entryIndex;
                    while (result.Count < totalSlots && currentIndex >= 0)
                    {
                        AddNodeToResult(result, currentIndex);
                        TickCooldowns();

                        currentIndex = PickFromConnections(currentIndex);
                    }
                }
                else
                {
                    AddNodeToResult(result, entryIndex);
                    TickCooldowns();
                }
            }

            if (result.Count < totalSlots)
            {
                Debug.LogWarning($"[BiomeData] Could only generate {result.Count}/{totalSlots} segments for {biomeName}");
            }

            return result;
        }

        private void AddNodeToResult(List<LevelSegment> result, int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= segmentNodes.Length) return;

            var node = segmentNodes[nodeIndex];
            if (node.Segment == null) return;

            result.Add(node.Segment);

            node.CurrentCooldown = Mathf.Max(node.Cooldown, 1);
        }

        private void TickCooldowns()
        {
            if (segmentNodes == null) return;

            for (int i = 0; i < segmentNodes.Length; i++)
            {
                if (segmentNodes[i] != null && segmentNodes[i].CurrentCooldown > 0)
                    segmentNodes[i].CurrentCooldown--;
            }
        }

        private int PickEntryPointNode(int remainingSlots)
        {
            List<int> candidates = new List<int>();
            List<float> weights = new List<float>();

            for (int i = 0; i < segmentNodes.Length; i++)
            {
                if (!IsValidEntryPoint(i)) continue;

                var node = segmentNodes[i];
                if (node.CurrentCooldown > 0) continue;

                if (node.HasConnections)
                {
                    int maxChain = GetMaxChainLength(i, new HashSet<int>());
                    if (maxChain > remainingSlots)
                        continue;
                }

                candidates.Add(i);
                weights.Add(node.Weight);
            }

            return WeightedRandomPick(candidates, weights);
        }

        private int PickEntryPointNodeIgnoringCooldown(int remainingSlots)
        {
            List<int> candidates = new List<int>();
            List<float> weights = new List<float>();

            for (int i = 0; i < segmentNodes.Length; i++)
            {
                if (!IsValidEntryPoint(i)) continue;

                var node = segmentNodes[i];

                if (node.HasConnections)
                {
                    int maxChain = GetMaxChainLength(i, new HashSet<int>());
                    if (maxChain > remainingSlots)
                        continue;
                }

                candidates.Add(i);
                weights.Add(node.Weight);
            }

            return WeightedRandomPick(candidates, weights);
        }

        private int PickStandaloneEntryPoint()
        {
            List<int> candidates = new List<int>();
            List<float> weights = new List<float>();

            for (int i = 0; i < segmentNodes.Length; i++)
            {
                if (!IsValidEntryPoint(i)) continue;

                var node = segmentNodes[i];
                if (node.CurrentCooldown > 0) continue;
                if (node.HasConnections) continue;

                candidates.Add(i);
                weights.Add(node.Weight);
            }

            return WeightedRandomPick(candidates, weights);
        }

        private int PickFromConnections(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= segmentNodes.Length) return -1;

            var node = segmentNodes[nodeIndex];
            if (!node.HasConnections) return -1;

            List<int> candidates = new List<int>();
            List<float> weights = new List<float>();

            foreach (int childIndex in node.Connections)
            {
                if (childIndex < 0 || childIndex >= segmentNodes.Length) continue;
                var child = segmentNodes[childIndex];
                if (child == null || child.Segment == null) continue;
                if (child.CurrentCooldown > 0) continue;

                candidates.Add(childIndex);
                weights.Add(child.Weight);
            }

            int picked = WeightedRandomPick(candidates, weights);
            if (picked >= 0) return picked;

            candidates.Clear();
            weights.Clear();

            foreach (int childIndex in node.Connections)
            {
                if (childIndex < 0 || childIndex >= segmentNodes.Length) continue;
                var child = segmentNodes[childIndex];
                if (child == null || child.Segment == null) continue;

                candidates.Add(childIndex);
                weights.Add(child.Weight);
            }

            return WeightedRandomPick(candidates, weights);
        }

        public int GetMaxChainLength(int nodeIndex, HashSet<int> visited)
        {
            if (nodeIndex < 0 || nodeIndex >= segmentNodes.Length) return 0;
            if (visited.Contains(nodeIndex)) return 0;

            var node = segmentNodes[nodeIndex];
            if (node == null || node.Segment == null) return 0;

            visited.Add(nodeIndex);

            if (!node.HasConnections)
            {
                visited.Remove(nodeIndex);
                return 1;
            }

            int maxChildChain = 0;
            foreach (int childIndex in node.Connections)
            {
                int childChain = GetMaxChainLength(childIndex, visited);
                if (childChain > maxChildChain)
                    maxChildChain = childChain;
            }

            visited.Remove(nodeIndex);
            return 1 + maxChildChain;
        }

        private int WeightedRandomPick(List<int> candidates, List<float> weights)
        {
            if (candidates.Count == 0) return -1;
            if (candidates.Count == 1) return candidates[0];

            float totalWeight = 0f;
            for (int i = 0; i < weights.Count; i++)
                totalWeight += weights[i];

            if (totalWeight <= 0f)
                return candidates[Random.Range(0, candidates.Count)];

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                    return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }

        public void ResetNodeCooldowns()
        {
            if (segmentNodes == null) return;
            foreach (var node in segmentNodes)
            {
                if (node != null)
                    node.CurrentCooldown = 0;
            }
        }

        public int FindNodeIndexForSegment(LevelSegment segment)
        {
            if (segmentNodes == null || segment == null) return -1;
            for (int i = 0; i < segmentNodes.Length; i++)
            {
                if (segmentNodes[i] != null && segmentNodes[i].Segment == segment)
                    return i;
            }
            return -1;
        }

        public BiomeTransition GetTransitionTo(BiomeData targetBiome)
        {
            if (transitions == null) return null;
            foreach (var transition in transitions)
            {
                if (transition.TargetBiome == targetBiome)
                    return transition;
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
                    validBiomes.Add(target);
            }

            if (validBiomes.Count == 0)
                return transitions[Random.Range(0, transitions.Length)].TargetBiome;

            return validBiomes[Random.Range(0, validBiomes.Count)];
        }

        public bool IsNodeEndNode(int nodeIndex)
        {
            if (segmentNodes == null || nodeIndex < 0 || nodeIndex >= segmentNodes.Length)
                return false;
            return segmentNodes[nodeIndex].IsEndNode;
        }

        public int GetEndNodeIndex()
        {
            if (segmentNodes == null || segmentNodes.Length == 0) return -1;
            for (int i = 0; i < segmentNodes.Length; i++)
            {
                if (segmentNodes[i] != null && segmentNodes[i].IsEndNode)
                    return i;
            }
            return -1;
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
                    segmentNodes[i] = new SegmentNodeData(i);
                else
                    segmentNodes[i].NodeIndex = i;
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

        public int NodeIndex { get => nodeIndex; set => nodeIndex = value; }
        public LevelSegment Segment { get => segment; set => segment = value; }
        public string NodeName { get => nodeName; set => nodeName = value; }
        public Vector2 NodePosition { get => nodePosition; set => nodePosition = value; }
        public bool IsStartNode { get => isStartNode; set => isStartNode = value; }
        public bool IsEndNode { get => isEndNode; set => isEndNode = value; }
        public int[] Connections { get => connections; set => connections = value; }
        public float Weight { get => weight; set => weight = value; }
        public int Cooldown { get => cooldown; set => cooldown = value; }
        public int CurrentCooldown { get => currentCooldown; set => currentCooldown = value; }
        public bool HasConnections => connections != null && connections.Length > 0;

        public SegmentNodeData(int index)
        {
            nodeIndex = index;
            nodeName = $"Segment {index}";
            nodePosition = new Vector2(100 + (index % 4) * 280, 100 + (index / 4) * 180);
        }

        public void AddConnection(int targetNodeIndex)
        {
            if (connections == null) connections = new int[0];
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i] == targetNodeIndex) return;
            }
            int[] newConnections = new int[connections.Length + 1];
            for (int i = 0; i < connections.Length; i++)
                newConnections[i] = connections[i];
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
                    newConnections.Add(connections[i]);
            }
            connections = newConnections.ToArray();
        }

        public bool HasConnectionTo(int targetNodeIndex)
        {
            if (connections == null) return false;
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i] == targetNodeIndex) return true;
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
