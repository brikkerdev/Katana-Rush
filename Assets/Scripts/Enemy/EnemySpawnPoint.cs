using UnityEngine;

namespace Runner.Enemy
{
    public class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] private bool alwaysSpawn = false;
        [SerializeField, Range(0f, 1f)] private float spawnChance = 0.5f;
        [SerializeField] private EnemyType allowedType = EnemyType.Static;
        [SerializeField] private bool useLocalOffset = false;
        [SerializeField] private Vector3 localOffset = Vector3.zero;

        public bool AlwaysSpawn => alwaysSpawn;
        public float SpawnChance => spawnChance;
        public EnemyType AllowedType => allowedType;

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
            Gizmos.color = alwaysSpawn ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(Position, 0.4f);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Position, transform.forward * 1f);

            UnityEditor.Handles.Label(Position + Vector3.up * 0.6f, $"{allowedType}\n{spawnChance:P0}");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(Position, 0.5f);
        }
#endif
    }
}