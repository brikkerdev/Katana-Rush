using UnityEngine;

namespace Runner.Collectibles
{
    public enum CollectibleType
    {
        Coin,
        CoinGroup,
        HealthPack,
        SpeedBoost,
        Magnet,
        Multiplier
    }

    public class CollectibleSpawnPoint : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private bool alwaysSpawn = false;
        [SerializeField, Range(0f, 1f)] private float spawnChance = 0.7f;
        [SerializeField] private CollectibleType collectibleType = CollectibleType.Coin;

        [Header("Group Settings (for CoinGroup)")]
        [SerializeField] private int groupCount = 5;
        [SerializeField] private float groupSpacing = 1.5f;
        [SerializeField] private GroupPattern groupPattern = GroupPattern.Line;

        [Header("Offset")]
        [SerializeField] private bool useLocalOffset = false;
        [SerializeField] private Vector3 localOffset = Vector3.zero;

        public bool AlwaysSpawn => alwaysSpawn;
        public float SpawnChance => spawnChance;
        public CollectibleType Type => collectibleType;
        public int GroupCount => groupCount;
        public float GroupSpacing => groupSpacing;
        public GroupPattern Pattern => groupPattern;

        public Vector3 Position
        {
            get
            {
                if (useLocalOffset)
                {
                    return transform.position + transform.TransformDirection(localOffset);
                }
                return transform.position;
            }
        }

        public Quaternion Rotation => transform.rotation;

        public Vector3[] GetGroupPositions()
        {
            Vector3[] positions = new Vector3[groupCount];
            Vector3 basePos = Position;

            switch (groupPattern)
            {
                case GroupPattern.Line:
                    for (int i = 0; i < groupCount; i++)
                    {
                        positions[i] = basePos + transform.forward * (i * groupSpacing);
                    }
                    break;

                case GroupPattern.Arc:
                    float arcAngle = 120f;
                    float startAngle = -arcAngle / 2f;
                    float angleStep = arcAngle / (groupCount - 1);

                    for (int i = 0; i < groupCount; i++)
                    {
                        float angle = startAngle + (i * angleStep);
                        float rad = angle * Mathf.Deg2Rad;
                        float x = Mathf.Sin(rad) * groupSpacing * 2f;
                        float z = Mathf.Cos(rad) * groupSpacing;
                        positions[i] = basePos + new Vector3(x, 0f, z);
                    }
                    break;

                case GroupPattern.Zigzag:
                    for (int i = 0; i < groupCount; i++)
                    {
                        float xOffset = (i % 2 == 0) ? -1.5f : 1.5f;
                        positions[i] = basePos + new Vector3(xOffset, 0f, i * groupSpacing);
                    }
                    break;

                case GroupPattern.Jump:
                    for (int i = 0; i < groupCount; i++)
                    {
                        float height = Mathf.Sin((float)i / (groupCount - 1) * Mathf.PI) * 2f;
                        positions[i] = basePos + new Vector3(0f, height, i * groupSpacing);
                    }
                    break;

                case GroupPattern.Vertical:
                    for (int i = 0; i < groupCount; i++)
                    {
                        positions[i] = basePos + Vector3.up * (i * groupSpacing * 0.5f);
                    }
                    break;
            }

            return positions;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color color = collectibleType switch
            {
                CollectibleType.Coin => Color.yellow,
                CollectibleType.CoinGroup => new Color(1f, 0.8f, 0f),
                CollectibleType.HealthPack => Color.green,
                CollectibleType.SpeedBoost => Color.cyan,
                CollectibleType.Magnet => Color.magenta,
                CollectibleType.Multiplier => Color.blue,
                _ => Color.white
            };

            Gizmos.color = color;

            if (collectibleType == CollectibleType.CoinGroup)
            {
                Vector3[] positions = GetGroupPositions();
                foreach (var pos in positions)
                {
                    Gizmos.DrawWireSphere(pos, 0.3f);
                }

                // Draw connections
                Gizmos.color = color * 0.5f;
                for (int i = 0; i < positions.Length - 1; i++)
                {
                    Gizmos.DrawLine(positions[i], positions[i + 1]);
                }
            }
            else
            {
                Gizmos.DrawWireSphere(Position, 0.4f);
            }

            UnityEditor.Handles.Label(Position + Vector3.up * 0.8f, $"{collectibleType}\n{spawnChance:P0}");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(Position, 0.5f);
        }
#endif
    }

    public enum GroupPattern
    {
        Line,
        Arc,
        Zigzag,
        Jump,
        Vertical
    }
}