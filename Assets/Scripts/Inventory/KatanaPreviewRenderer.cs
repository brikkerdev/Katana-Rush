using UnityEngine;

namespace Runner.Inventory
{
    public class KatanaPreviewRenderer : MonoBehaviour
    {
        [Header("Render Setup")]
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Transform katanaHolder;
        [SerializeField] private RenderTexture renderTexture;

        [Header("Preview Settings")]
        [SerializeField] private Vector3 katanaOffset = Vector3.zero;
        [SerializeField] private Vector3 katanaRotation = new Vector3(0f, 45f, 0f);
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private bool autoRotate = true;

        [Header("Locked Visual")]
        [SerializeField] private Material lockedMaterial;

        public GameObject currentKatanaInstance;
        private bool isLocked;
        private bool isActive;
        private Renderer[] katanaRenderers;
        private Material[] originalMaterials;

        public RenderTexture RenderTexture => renderTexture;
        public bool IsActive => isActive;

        private void Awake()
        {
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(512, 512, 16);
                renderTexture.antiAliasing = 4;
            }

            if (previewCamera != null)
            {
                previewCamera.targetTexture = renderTexture;
                previewCamera.enabled = false;
            }

            isActive = false;
        }

        private void Update()
        {
            if (!isActive) return;

            if (autoRotate && katanaHolder != null && currentKatanaInstance != null)
            {
                katanaHolder.Rotate(Vector3.right, rotationSpeed * Time.unscaledDeltaTime);
            }
        }

        public void Activate()
        {
            if (isActive) return;

            isActive = true;

            if (previewCamera != null)
            {
                previewCamera.enabled = true;
            }

            if (katanaHolder != null)
            {
                katanaHolder.gameObject.SetActive(true);
            }
        }

        public void Deactivate()
        {
            if (!isActive) return;

            isActive = false;

            if (previewCamera != null)
            {
                previewCamera.enabled = false;
            }

            if (katanaHolder != null)
            {
                katanaHolder.gameObject.SetActive(false);
            }
        }

        public void ShowKatana(Katana katana, bool locked)
        {
            ClearCurrentKatana();

            if (katana == null || katana.ModelPrefab == null)
            {
                return;
            }

            isLocked = locked;

            currentKatanaInstance = Instantiate(katana.ModelPrefab, katanaHolder);
            currentKatanaInstance.transform.localPosition = katanaOffset;
            currentKatanaInstance.transform.localRotation = Quaternion.Euler(katanaRotation);

            katanaRenderers = currentKatanaInstance.GetComponentsInChildren<Renderer>();

            if (locked && lockedMaterial != null)
            {
                originalMaterials = new Material[katanaRenderers.Length];

                for (int i = 0; i < katanaRenderers.Length; i++)
                {
                    originalMaterials[i] = katanaRenderers[i].material;
                    katanaRenderers[i].material = lockedMaterial;
                }
            }

            katanaHolder.localRotation = Quaternion.identity;
        }

        public void ClearCurrentKatana()
        {
            if (currentKatanaInstance != null)
            {
                Destroy(currentKatanaInstance);
                currentKatanaInstance = null;
            }

            katanaRenderers = null;
            originalMaterials = null;
        }

        private void OnDestroy()
        {
            ClearCurrentKatana();

            if (renderTexture != null)
            {
                renderTexture.Release();
            }
        }
    }
}