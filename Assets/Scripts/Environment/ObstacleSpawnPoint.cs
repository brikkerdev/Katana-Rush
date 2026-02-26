using UnityEngine;

namespace Runner.Environment
{
    public class ObstacleSpawnPoint : MonoBehaviour
    {
        [SerializeField] private bool alwaysSpawn = false;
        [SerializeField, Range(0f, 1f)] private float spawnChance = 0.5f;
        [SerializeField] private bool useLocalOffset = false;
        [SerializeField] private Vector3 localOffset = Vector3.zero;

        public bool AlwaysSpawn => alwaysSpawn;
        public float SpawnChance => spawnChance;

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

        public Vector3 GetWorldPosition()
        {
            return transform.position;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = alwaysSpawn ? new Color(1f, 0.5f, 0f) : new Color(1f, 0.7f, 0.3f);
            Gizmos.DrawWireSphere(Position, 0.4f);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Position, transform.forward * 1f);

            UnityEditor.Handles.Label(Position + Vector3.up * 0.6f, $"Obstacle\n{spawnChance:P0}");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(Position, 0.5f);
        }
#endif
    }
}
