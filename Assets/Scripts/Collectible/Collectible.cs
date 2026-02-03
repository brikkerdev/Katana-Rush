using UnityEngine;
using Runner.Save;

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

    public class Collectible : MonoBehaviour, IResettable
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

            if (visualRoot != null && rotationSpeed > 0f)
            {
                visualRoot.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }

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
            bobTimer = Random.value * Mathf.PI * 2f;

            if (visualRoot != null)
            {
                visualRoot.SetActive(true);
            }

            gameObject.SetActive(true);
        }

        public void Collect()
        {
            if (isCollected) return;

            isCollected = true;

            ApplyCollectEffect();

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
                    SaveManager.AddCoins(value);
                    UI.UIManager.Instance?.NotifyCoinsCollected(value);
                    break;

                case CollectibleType.HealthPack:
                    break;

                case CollectibleType.SpeedBoost:
                    break;

                case CollectibleType.Magnet:
                    break;

                case CollectibleType.Multiplier:
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
    }
}