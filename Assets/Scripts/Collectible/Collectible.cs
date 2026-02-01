using UnityEngine;
using Runner.Collectibles;

namespace Runner.LevelGeneration
{
    public interface IResettable
    {
        void Reset();
    }
}

namespace Runner.Collectibles
{
    public class Collectible : MonoBehaviour, Runner.LevelGeneration.IResettable
    {
        [Header("Settings")]
        [SerializeField] private CollectibleType collectibleType = CollectibleType.Coin;
        [SerializeField] private int value = 1;
        [SerializeField] private float magnetRadius = 0f;

        [Header("Visual")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private bool isCollected;
        private Vector3 originalPosition;
        private float bobTimer;

        public CollectibleType Type => collectibleType;
        public int Value => value;
        public bool IsCollected => isCollected;
        public float MagnetRadius => magnetRadius;

        private void Awake()
        {
            originalPosition = transform.localPosition;

            if (visualRoot == null)
            {
                visualRoot = transform.childCount > 0 ? transform.GetChild(0).gameObject : gameObject;
            }
        }

        private void Update()
        {
            if (isCollected) return;

            // Rotation
            if (visualRoot != null && rotationSpeed > 0f)
            {
                visualRoot.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }

            // Bobbing
            if (bobHeight > 0f)
            {
                bobTimer += Time.deltaTime * bobSpeed;
                float yOffset = Mathf.Sin(bobTimer) * bobHeight;
                transform.localPosition = originalPosition + Vector3.up * yOffset;
            }
        }

        public void Setup(Vector3 position)
        {
            transform.position = position;
            originalPosition = transform.localPosition;
            isCollected = false;
            bobTimer = Random.value * Mathf.PI * 2f; // Random start phase

            if (visualRoot != null)
            {
                visualRoot.SetActive(true);
            }

            gameObject.SetActive(true);

            if (showDebug)
            {
                Debug.Log($"[Collectible] Setup {collectibleType} at {position}");
            }
        }

        public void Collect()
        {
            if (isCollected) return;

            isCollected = true;

            // Apply effect based on type
            ApplyCollectEffect();

            if (showDebug)
            {
                Debug.Log($"[Collectible] Collected {collectibleType}, value: {value}");
            }

            // Hide
            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        private void ApplyCollectEffect()
        {
            switch (collectibleType)
            {
                case CollectibleType.Coin:
                case CollectibleType.CoinGroup:
                    UI.UIManager.Instance?.AddCoins(value);
                    break;

                case CollectibleType.HealthPack:
                    // Add health if you have health system
                    break;

                case CollectibleType.SpeedBoost:
                    // Apply speed boost
                    break;

                case CollectibleType.Magnet:
                    // Activate magnet effect
                    break;

                case CollectibleType.Multiplier:
                    // Activate score multiplier
                    break;
            }
        }

        public void Reset()
        {
            isCollected = false;
            transform.localPosition = originalPosition;
            bobTimer = 0f;

            if (visualRoot != null)
            {
                visualRoot.SetActive(true);
            }

            gameObject.SetActive(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            if (other.CompareTag("Player"))
            {
                Collect();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
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
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            if (magnetRadius > 0f)
            {
                Gizmos.color = Color.magenta * 0.3f;
                Gizmos.DrawWireSphere(transform.position, magnetRadius);
            }
        }
#endif
    }
}