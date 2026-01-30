using UnityEngine;
using UnityEngine.UI;
using Runner.Player.Core;
using Runner.Core;

namespace Runner.UI
{
    public class StaminaDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform dashIconContainer;
        [SerializeField] private GameObject dashIconPrefab;
        [SerializeField] private Image regenProgressBar;

        [Header("Colors")]
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color usedColor = new Color(1f, 1f, 1f, 0.3f);

        private Image[] dashIcons;
        private PlayerController controller;
        private int lastMaxDashes = -1;

        private void Start()
        {
            if (regenProgressBar != null)
            {
                regenProgressBar.fillAmount = 0f;
            }
        }

        private void OnEnable()
        {
            SubscribeToPlayer();
        }

        private void OnDisable()
        {
            UnsubscribeFromPlayer();
        }

        private void SubscribeToPlayer()
        {
            if (Game.Instance == null) return;
            if (Game.Instance.Player == null) return;

            controller = Game.Instance.Player.Controller;
            if (controller == null) return;

            controller.OnDashCountChanged += OnDashCountChanged;
            controller.OnDashRegenProgress += OnRegenProgress;

            CreateDashIcons(controller.MaxDashes);
            OnDashCountChanged(controller.DashesRemaining, controller.MaxDashes);
        }

        private void UnsubscribeFromPlayer()
        {
            if (controller == null) return;

            controller.OnDashCountChanged -= OnDashCountChanged;
            controller.OnDashRegenProgress -= OnRegenProgress;
            controller = null;
        }

        private void CreateDashIcons(int maxDashes)
        {
            if (dashIconContainer == null) return;
            if (dashIconPrefab == null) return;
            if (maxDashes == lastMaxDashes && dashIcons != null) return;

            for (int i = dashIconContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(dashIconContainer.GetChild(i).gameObject);
            }

            dashIcons = new Image[maxDashes];

            for (int i = 0; i < maxDashes; i++)
            {
                GameObject icon = Instantiate(dashIconPrefab, dashIconContainer);
                dashIcons[i] = icon.GetComponent<Image>();

                if (dashIcons[i] != null)
                {
                    dashIcons[i].color = availableColor;
                }
            }

            lastMaxDashes = maxDashes;
        }

        private void OnDashCountChanged(int current, int max)
        {
            if (max != lastMaxDashes)
            {
                CreateDashIcons(max);
            }

            if (dashIcons == null) return;

            for (int i = 0; i < dashIcons.Length; i++)
            {
                if (dashIcons[i] == null) continue;

                dashIcons[i].color = i < current ? availableColor : usedColor;
            }
        }

        private void OnRegenProgress(float progress)
        {
            if (regenProgressBar != null)
            {
                regenProgressBar.fillAmount = progress;
            }

            if (dashIcons == null) return;
            if (controller == null) return;

            int regenIndex = controller.DashesRemaining;
            if (regenIndex < dashIcons.Length && dashIcons[regenIndex] != null)
            {
                dashIcons[regenIndex].color = Color.Lerp(usedColor, availableColor, progress);
            }
        }

        private void Update()
        {
            if (controller == null)
            {
                SubscribeToPlayer();
            }
        }
    }
}