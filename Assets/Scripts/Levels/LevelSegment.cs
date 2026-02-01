using UnityEngine;
using Runner.Enemy;
using Runner.Collectibles;

namespace Runner.LevelGeneration
{
    public class LevelSegment : MonoBehaviour
    {
        [Header("Segment Settings")]
        [SerializeField] private float segmentLength = 30f;
        [SerializeField, Range(0f, 1f)] private float difficultyWeight = 0f;
        [SerializeField] private bool allowConsecutive = true;

        [Header("Enemy Spawn Points")]
        [SerializeField] private EnemySpawnPoint[] enemySpawnPoints;

        [Header("Collectible Spawn Points")]
        [SerializeField] private CollectibleSpawnPoint[] collectibleSpawnPoints;

        public float Length => segmentLength;
        public float DifficultyWeight => difficultyWeight;
        public bool AllowConsecutive => allowConsecutive;
        public EnemySpawnPoint[] EnemySpawnPoints => enemySpawnPoints;
        public CollectibleSpawnPoint[] CollectibleSpawnPoints => collectibleSpawnPoints;
        public int EnemySpawnPointCount => enemySpawnPoints != null ? enemySpawnPoints.Length : 0;
        public int CollectibleSpawnPointCount => collectibleSpawnPoints != null ? collectibleSpawnPoints.Length : 0;

        public void CollectSpawnPoints()
        {
            enemySpawnPoints = GetComponentsInChildren<EnemySpawnPoint>(true);
            collectibleSpawnPoints = GetComponentsInChildren<CollectibleSpawnPoint>(true);
        }

        public void ResetSegment()
        {
            // Reset all resettable objects in segment
            IResettable[] resettables = GetComponentsInChildren<IResettable>(true);
            foreach (var resettable in resettables)
            {
                resettable.Reset();
            }
        }

        private void OnValidate()
        {
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0 ||
                collectibleSpawnPoints == null || collectibleSpawnPoints.Length == 0)
            {
                CollectSpawnPoints();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Segment bounds
            Gizmos.color = Color.green;
            Vector3 start = transform.position;
            Vector3 end = transform.position + Vector3.forward * segmentLength;
            Vector3 center = (start + end) / 2f;

            Gizmos.DrawWireCube(center, new Vector3(15f, 0.1f, segmentLength));

            // Start/End markers
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, start + Vector3.up * 2f);
            Gizmos.DrawLine(end, end + Vector3.up * 2f);

            // Enemy spawn points
            if (enemySpawnPoints != null)
            {
                Gizmos.color = Color.red;
                foreach (var sp in enemySpawnPoints)
                {
                    if (sp != null)
                    {
                        Gizmos.DrawWireSphere(sp.transform.position, 0.5f);
                    }
                }
            }

            // Collectible spawn points
            if (collectibleSpawnPoints != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var sp in collectibleSpawnPoints)
                {
                    if (sp != null)
                    {
                        Gizmos.DrawWireSphere(sp.transform.position, 0.4f);
                    }
                }
            }
        }

        [ContextMenu("Collect All Spawn Points")]
        private void CollectAllSpawnPoints()
        {
            CollectSpawnPoints();
            Debug.Log($"Found {EnemySpawnPointCount} enemy spawn points and {CollectibleSpawnPointCount} collectible spawn points");
        }
#endif
    }
}