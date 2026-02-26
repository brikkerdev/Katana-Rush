using UnityEngine;
using Runner.Enemy;
using Runner.Collectibles;
using Runner.Environment;

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

        [Header("Obstacle Spawn Points")]
        [SerializeField] private ObstacleSpawnPoint[] obstacleSpawnPoints;

        public float Length => segmentLength;
        public float DifficultyWeight => difficultyWeight;
        public bool AllowConsecutive => allowConsecutive;
        public EnemySpawnPoint[] EnemySpawnPoints => enemySpawnPoints;
        public CollectibleSpawnPoint[] CollectibleSpawnPoints => collectibleSpawnPoints;
        public ObstacleSpawnPoint[] ObstacleSpawnPoints => obstacleSpawnPoints;
        public int EnemySpawnPointCount => enemySpawnPoints != null ? enemySpawnPoints.Length : 0;
        public int CollectibleSpawnPointCount => collectibleSpawnPoints != null ? collectibleSpawnPoints.Length : 0;
        public int ObstacleSpawnPointCount => obstacleSpawnPoints != null ? obstacleSpawnPoints.Length : 0;

        public void CollectSpawnPoints()
        {
            enemySpawnPoints = GetComponentsInChildren<EnemySpawnPoint>(true);
            collectibleSpawnPoints = GetComponentsInChildren<CollectibleSpawnPoint>(true);
            obstacleSpawnPoints = GetComponentsInChildren<ObstacleSpawnPoint>(true);
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
                collectibleSpawnPoints == null || collectibleSpawnPoints.Length == 0 ||
                obstacleSpawnPoints == null || obstacleSpawnPoints.Length == 0)
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

            // Obstacle spawn points
            if (obstacleSpawnPoints != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f);
                foreach (var sp in obstacleSpawnPoints)
                {
                    if (sp != null)
                    {
                        Gizmos.DrawWireSphere(sp.transform.position, 0.35f);
                    }
                }
            }
        }

        [ContextMenu("Collect All Spawn Points")]
        private void CollectAllSpawnPoints()
        {
            CollectSpawnPoints();
            Debug.Log($"Found {EnemySpawnPointCount} enemy spawn points, {CollectibleSpawnPointCount} collectible spawn points, and {ObstacleSpawnPointCount} obstacle spawn points");
        }
#endif
    }
}