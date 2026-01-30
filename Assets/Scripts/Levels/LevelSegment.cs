using UnityEngine;
using Runner.Enemy;

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

        public float Length => segmentLength;
        public float DifficultyWeight => difficultyWeight;
        public bool AllowConsecutive => allowConsecutive;
        public EnemySpawnPoint[] EnemySpawnPoints => enemySpawnPoints;
        public int SpawnPointCount => enemySpawnPoints != null ? enemySpawnPoints.Length : 0;

        public void CollectSpawnPoints()
        {
            enemySpawnPoints = GetComponentsInChildren<EnemySpawnPoint>(true);
        }

        public void ResetSegment()
        {
        }

        private void OnValidate()
        {
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                CollectSpawnPoints();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 start = transform.position;
            Vector3 end = transform.position + Vector3.forward * segmentLength;
            Vector3 center = (start + end) / 2f;

            Gizmos.DrawWireCube(center, new Vector3(10f, 0.1f, segmentLength));

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, start + Vector3.up * 2f);
            Gizmos.DrawLine(end, end + Vector3.up * 2f);

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
        }
#endif
    }
}