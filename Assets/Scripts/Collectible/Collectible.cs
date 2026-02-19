using UnityEngine;
using Runner.Core;

namespace Runner.Collectibles
{
    public enum CollectibleType
    {
        Coin,
        CoinGroup,
        SpeedBoost,
        Magnet,
        Multiplier,
        DashRestore
    }

    public abstract class Collectible : MonoBehaviour, IResettable
    {
        [Header("Settings")]
        [SerializeField] private int value = 1;
        [SerializeField] private float effectDuration = 5f;

        [Header("Visual")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.2f;

        private bool isCollected;
        private Vector3 originalPosition;
        private float bobTimer;

        public abstract CollectibleType Type { get; }
        public int Value => value;
        public bool IsCollected => isCollected;
        public float EffectDuration => effectDuration;

        protected virtual void Awake()
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
            PlayCollectSound();

            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        protected abstract void ApplyCollectEffect();
        protected abstract void PlayCollectSound();

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
