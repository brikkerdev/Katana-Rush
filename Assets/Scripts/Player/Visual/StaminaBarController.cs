using UnityEngine;
using Runner.Player;
using Runner.Player.Core;
using Runner.Core;

namespace Runner.UI
{
    public class StaminaBarController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CircularStaminaBar staminaBar;
        [SerializeField] private Transform followTarget;

        [Header("Position")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.5f, 0f);
        [SerializeField] private bool followPlayer = true;
        [SerializeField] private bool billboardToCamera = true;

        [Header("Scale")]
        [SerializeField] private float baseScale = 1f;
        [SerializeField] private bool scaleWithDistance = false;
        [SerializeField] private float minScale = 0.5f;
        [SerializeField] private float maxScale = 1.5f;
        [SerializeField] private float scaleDistance = 10f;

        private Player.Player player;
        private PlayerController playerController;
        private Camera mainCamera;
        private int lastDashCount = -1;
        private int lastMaxDashes = -1;
        private bool isGameActive;

        private void Start()
        {
            mainCamera = Camera.main;

            if (staminaBar == null)
            {
                staminaBar = GetComponentInChildren<CircularStaminaBar>();
            }

            HideBar();
            SubscribeToGameEvents();
            FindPlayer();
        }

        private void SubscribeToGameEvents()
        {
            if (Game.Instance == null) return;

            Game.Instance.OnGameStarted += HandleGameStarted;
            Game.Instance.OnGameOver += HandleGameOver;
            Game.Instance.OnGamePaused += HandleGamePaused;
            Game.Instance.OnGameResumed += HandleGameResumed;
            Game.Instance.OnGameRestarted += HandleGameRestarted;
        }

        private void UnsubscribeFromGameEvents()
        {
            if (Game.Instance == null) return;

            Game.Instance.OnGameStarted -= HandleGameStarted;
            Game.Instance.OnGameOver -= HandleGameOver;
            Game.Instance.OnGamePaused -= HandleGamePaused;
            Game.Instance.OnGameResumed -= HandleGameResumed;
            Game.Instance.OnGameRestarted -= HandleGameRestarted;
        }

        private void HandleGameStarted()
        {
            isGameActive = true;

            FindPlayer();
            ResetBar();
            ShowBar();
        }

        private void HandleGameOver()
        {
            isGameActive = false;
            HideBar();
        }

        private void HandleGamePaused()
        {
            HideBar();
        }

        private void HandleGameResumed()
        {
            if (isGameActive)
            {
                ShowBar();
            }
        }

        private void HandleGameRestarted()
        {
            isGameActive = false;

            UnsubscribeFromPlayerEvents();

            player = null;
            playerController = null;
            lastDashCount = -1;
            lastMaxDashes = -1;

            HideBar();
        }

        private void FindPlayer()
        {
            if (player != null) return;

            if (Game.Instance != null && Game.Instance.Player != null)
            {
                SetPlayer(Game.Instance.Player);
                return;
            }

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                Player.Player foundPlayer = playerObj.GetComponent<Player.Player>();

                if (foundPlayer != null)
                {
                    SetPlayer(foundPlayer);
                }
            }
        }

        public void SetPlayer(Player.Player newPlayer)
        {
            UnsubscribeFromPlayerEvents();

            player = newPlayer;

            if (player != null)
            {
                playerController = player.Controller;
                followTarget = player.transform;

                SubscribeToPlayerEvents();

                if (isGameActive)
                {
                    InitializeBar();
                }
            }
        }

        private void SubscribeToPlayerEvents()
        {
            if (playerController == null) return;

            playerController.OnDashCountChanged += HandleDashCountChanged;
            playerController.OnDashRegenProgress += HandleRegenProgress;
        }

        private void UnsubscribeFromPlayerEvents()
        {
            if (playerController == null) return;

            playerController.OnDashCountChanged -= HandleDashCountChanged;
            playerController.OnDashRegenProgress -= HandleRegenProgress;
        }

        private void InitializeBar()
        {
            if (staminaBar == null || playerController == null) return;

            int maxDashes = playerController.MaxDashes;
            int currentDashes = playerController.DashesRemaining;

            staminaBar.RestoreAll();
            staminaBar.SetSegmentCount(maxDashes);
            staminaBar.SetDashCount(currentDashes, maxDashes);

            lastDashCount = currentDashes;
            lastMaxDashes = maxDashes;
        }

        private void HandleDashCountChanged(int current, int max)
        {
            if (staminaBar == null) return;
            if (!isGameActive) return;

            if (max != lastMaxDashes)
            {
                staminaBar.SetSegmentCount(max);
                lastMaxDashes = max;
            }

            if (current < lastDashCount)
            {
                int usedIndex = current;
                staminaBar.OnDashUsed(usedIndex);
            }
            else if (current > lastDashCount)
            {
                int restoredIndex = current - 1;
                staminaBar.OnDashRestored(restoredIndex);
            }

            staminaBar.SetDashCount(current, max);
            lastDashCount = current;
        }

        private void HandleRegenProgress(float progress)
        {
            if (staminaBar == null) return;
            if (!isGameActive) return;

            staminaBar.SetRechargeProgress(progress);
        }

        private void LateUpdate()
        {
            if (!isGameActive) return;

            if (player == null)
            {
                FindPlayer();
                return;
            }

            UpdatePosition();
            UpdateRotation();
            UpdateScale();
        }

        private void UpdatePosition()
        {
            if (!followPlayer || followTarget == null) return;

            transform.position = followTarget.position + offset;
        }

        private void UpdateRotation()
        {
            if (!billboardToCamera || mainCamera == null) return;

            Vector3 lookDirection = mainCamera.transform.position - transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(-lookDirection);
            }
        }

        private void UpdateScale()
        {
            if (!scaleWithDistance || mainCamera == null) return;

            float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
            float t = Mathf.InverseLerp(0f, scaleDistance, distance);
            float scale = Mathf.Lerp(maxScale, minScale, t) * baseScale;

            transform.localScale = Vector3.one * scale;
        }

        public void ResetBar()
        {
            if (staminaBar != null)
            {
                staminaBar.RestoreAll();
            }

            lastDashCount = -1;
            lastMaxDashes = -1;

            if (playerController != null && isGameActive)
            {
                InitializeBar();
            }
        }

        public void ShowBar()
        {
            if (staminaBar != null)
            {
                staminaBar.ShowBar();
                staminaBar.gameObject.SetActive(true);
            }
        }

        public void HideBar()
        {
            if (staminaBar != null)
            {
                staminaBar.HideBar();
            }
        }

        public void ForceHide()
        {
            if (staminaBar != null)
            {
                staminaBar.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromPlayerEvents();
            UnsubscribeFromGameEvents();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (followTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(followTarget.position + offset, 0.2f);
                Gizmos.DrawLine(followTarget.position, followTarget.position + offset);
            }
        }
#endif
    }
}