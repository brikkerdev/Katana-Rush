using UnityEngine;
using DG.Tweening;

namespace Runner.LevelGeneration
{
    /// <summary>
    /// Controls the biome background image movement - moves up when entering biome,
    /// moves down and disappears when leaving biome.
    /// </summary>
    public class BiomeBackgroundController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float visibleY = 0f;
        [SerializeField] private float hiddenY = -15f;
        
        private Transform player;
        private GameObject currentBackgroundImage;
        private BiomeData currentBiomeData;
        private float targetY;
        private bool isMovingUp;
        private bool isMovingDown;
        private float transitionZ;
        
        private Vector3 initialPosition;
        
        public void Initialize(Transform playerTransform)
        {
            player = playerTransform;
            targetY = hiddenY;
            
            // Start from hidden position
            Vector3 pos = transform.position;
            pos.y = hiddenY;
            transform.position = pos;
        }
        
        public void SpawnBackgroundForBiome(BiomeData biome, float atZ, bool startHidden = false)
        {
            if (biome == null || biome.BackgroundImagePrefab == null)
            {
                return;
            }
            
            // Destroy existing background if any
            if (currentBackgroundImage != null)
            {
                Destroy(currentBackgroundImage);
            }
            
            currentBiomeData = biome;
            
            // Instantiate the background image
            currentBackgroundImage = Instantiate(biome.BackgroundImagePrefab, transform);
            currentBackgroundImage.name = $"Background_{biome.BiomeName}";
            
            // Get move speed from biome data if available
            moveSpeed = biome.BackgroundImageMoveSpeed > 0 ? biome.BackgroundImageMoveSpeed : 2f;
            
            // Set initial position - either at hidden Y or visible Y based on startHidden
            float startY = startHidden ? hiddenY + biome.BackgroundImageOffset.y : visibleY + biome.BackgroundImageOffset.y;
            Vector3 position = new Vector3(
                biome.BackgroundImageOffset.x,
                startY,
                atZ + biome.BackgroundImageOffset.z
            );
            
            currentBackgroundImage.transform.position = position;
            initialPosition = position;
            
            // Schedule the move up when player reaches the transition Z
            transitionZ = atZ;
            
            if (biome.BackgroundImagePrefab != null)
            {
                currentBackgroundImage.SetActive(true);
            }
            
            // If starting hidden, animate to visible
            if (startHidden)
            {
                targetY = visibleY + biome.BackgroundImageOffset.y;
                isMovingUp = true;
                
                // Use DoTween for smooth animation from hidden to visible
                float duration = moveSpeed > 0 ? 1f / moveSpeed : 0.5f;
                currentBackgroundImage.transform.DOKill();
                currentBackgroundImage.transform.DOMoveY(targetY, duration).SetEase(Ease.OutQuad);
                
#if UNITY_EDITOR
                Debug.Log($"[BiomeBackgroundController] Spawned background for {biome.BiomeName} at hidden Y, animating to visible");
#endif
            }
            else
            {
                targetY = visibleY + biome.BackgroundImageOffset.y;
                isMovingUp = true;
            }
            
#if UNITY_EDITOR
            Debug.Log($"[BiomeBackgroundController] Spawned background for {biome.BiomeName} at Z={atZ}");
#endif
        }
        
        public void TriggerMoveUp(float atZ)
        {
            if (currentBackgroundImage == null || currentBiomeData == null)
            {
                return;
            }
            
            // Don't move if transitioning to the same biome (same background prefab)
            if (currentBiomeData.BackgroundImagePrefab == null)
            {
                return;
            }
            
            // Check if we're already showing this biome's background - don't move if same biome
            Vector3 currentPos = currentBackgroundImage.transform.position;
            float targetVisibleY = visibleY + currentBiomeData.BackgroundImageOffset.y;
            if (Mathf.Approximately(currentPos.y, targetVisibleY))
            {
#if UNITY_EDITOR
                Debug.Log("[BiomeBackgroundController] Already showing this biome's background - skipping move");
#endif
                return;
            }
            
            transitionZ = atZ;
            isMovingUp = true;
            isMovingDown = false;
            targetY = targetVisibleY;
            
            // Use DoTween for smooth animation
            float duration = moveSpeed > 0 ? 1f / moveSpeed : 0.5f;
            currentBackgroundImage.transform.DOKill();
            currentBackgroundImage.transform.DOMoveY(targetVisibleY, duration).SetEase(Ease.OutQuad);
            
#if UNITY_EDITOR
            Debug.Log($"[BiomeBackgroundController] Moving up at Z={atZ}");
#endif
        }
        
        public void TriggerMoveDown()
        {
            if (currentBackgroundImage == null)
            {
                return;
            }
            
            isMovingUp = false;
            isMovingDown = true;
            targetY = hiddenY + (currentBiomeData != null ? currentBiomeData.BackgroundImageOffset.y : 0f);
            
            // Use DoTween for smooth animation
            float duration = moveSpeed > 0 ? 1f / moveSpeed : 0.5f;
            currentBackgroundImage.transform.DOKill();
            currentBackgroundImage.transform.DOMoveY(targetY, duration).SetEase(Ease.InQuad).OnComplete(() => {
                if (currentBackgroundImage != null)
                {
                    Destroy(currentBackgroundImage);
                    currentBackgroundImage = null;
                    currentBiomeData = null;
                }
                isMovingDown = false;
            });
            
#if UNITY_EDITOR
            Debug.Log("[BiomeBackgroundController] Moving down");
#endif
        }
        
        private void Update()
        {
            if (currentBackgroundImage == null) return;
            
            // Move with player in Z direction
            if (player != null)
            {
                Vector3 currentPos = currentBackgroundImage.transform.position;
                
                // Calculate Z offset based on player position
                float zOffset = currentBiomeData != null ? currentBiomeData.BackgroundImageOffset.z : 0f;
                float targetZ = player.position.z + zOffset;
                
                // Smoothly follow player in Z
                currentPos.z = Mathf.Lerp(currentPos.z, targetZ, 5f * Time.deltaTime);
                
                currentBackgroundImage.transform.position = currentPos;
            }
        }
        
        public void ResetBackground()
        {
            isMovingUp = false;
            isMovingDown = false;
            targetY = hiddenY;
            
            if (currentBackgroundImage != null)
            {
                Destroy(currentBackgroundImage);
                currentBackgroundImage = null;
            }
            
            currentBiomeData = null;
        }
        
        private void OnDestroy()
        {
            if (currentBackgroundImage != null)
            {
                Destroy(currentBackgroundImage);
            }
        }
    }
}
