using UnityEngine;
using System;
using Runner.Player.Data;

namespace Runner.Player.Movement
{
    public class DashHandler
    {
        private MovementSettings settings;

        private int maxDashes;
        private float dashDuration;
        private float dashSpeedMultiplier;
        private float dashRegenTime;
        private float dashRegenDelay;
        private bool dashInvincible;

        private int dashesRemaining;
        private bool isDashing;
        private float dashTimer;
        private float dashProgress;
        private float regenTimer;
        private float delayTimer;
        private bool isRegenerating;

        public int DashesRemaining => dashesRemaining;
        public int MaxDashes => maxDashes;
        public bool IsDashing => isDashing;
        public bool IsInvincible => isDashing && dashInvincible;
        public float DashProgress => dashProgress;
        public float RegenProgress => isRegenerating ? regenTimer / dashRegenTime : 0f;
        public bool CanDash => dashesRemaining > 0 && !isDashing;

        public event Action<int, int> OnDashCountChanged;
        public event Action<float> OnRegenProgressChanged;
        public event Action OnDashStarted;
        public event Action OnDashEnded;

        public void Initialize(MovementSettings movementSettings, PlayerPreset preset)
        {
            settings = movementSettings;
            ApplyPreset(preset);
        }

        public void ApplyPreset(PlayerPreset preset)
        {
            maxDashes = preset.MaxDashes;
            dashDuration = preset.DashDuration;
            dashSpeedMultiplier = preset.DashSpeedMultiplier;
            dashRegenTime = preset.DashRegenTime;
            dashRegenDelay = preset.DashRegenDelay;
            dashInvincible = preset.DashInvincible;

            dashesRemaining = Mathf.Min(dashesRemaining, maxDashes);
            if (dashesRemaining <= 0)
            {
                dashesRemaining = maxDashes;
            }

            OnDashCountChanged?.Invoke(dashesRemaining, maxDashes);
        }

        public bool TryDash()
        {
            if (!CanDash) return false;

            dashesRemaining--;
            isDashing = true;
            dashTimer = 0f;
            dashProgress = 0f;

            delayTimer = dashRegenDelay;
            regenTimer = 0f;
            isRegenerating = false;

            OnDashCountChanged?.Invoke(dashesRemaining, maxDashes);
            OnRegenProgressChanged?.Invoke(0f);
            OnDashStarted?.Invoke();

            return true;
        }

        public float Update(float deltaTime, float baseSpeed)
        {
            float speedMultiplier = 1f;

            if (isDashing)
            {
                dashTimer += deltaTime;
                dashProgress = dashTimer / dashDuration;

                float curveValue = settings.dashCurve.Evaluate(dashProgress);
                speedMultiplier = 1f + (dashSpeedMultiplier - 1f) * curveValue;

                if (dashTimer >= dashDuration)
                {
                    EndDash();
                }
            }

            UpdateRegen(deltaTime);

            return speedMultiplier;
        }

        private void EndDash()
        {
            isDashing = false;
            dashProgress = 0f;
            OnDashEnded?.Invoke();
        }

        private void UpdateRegen(float deltaTime)
        {
            if (dashesRemaining >= maxDashes)
            {
                isRegenerating = false;
                return;
            }

            if (delayTimer > 0f)
            {
                delayTimer -= deltaTime;
                return;
            }

            isRegenerating = true;
            regenTimer += deltaTime;

            OnRegenProgressChanged?.Invoke(RegenProgress);

            if (regenTimer >= dashRegenTime)
            {
                dashesRemaining++;
                regenTimer = 0f;

                if (dashesRemaining >= maxDashes)
                {
                    isRegenerating = false;
                }

                OnDashCountChanged?.Invoke(dashesRemaining, maxDashes);
                OnRegenProgressChanged?.Invoke(0f);
            }
        }

        public void RestoreAllDashes()
        {
            dashesRemaining = maxDashes;
            regenTimer = 0f;
            delayTimer = 0f;
            isRegenerating = false;

            OnDashCountChanged?.Invoke(dashesRemaining, maxDashes);
            OnRegenProgressChanged?.Invoke(0f);
        }

        public void Reset()
        {
            dashesRemaining = maxDashes;
            isDashing = false;
            dashTimer = 0f;
            dashProgress = 0f;
            regenTimer = 0f;
            delayTimer = 0f;
            isRegenerating = false;

            OnDashCountChanged?.Invoke(dashesRemaining, maxDashes);
            OnRegenProgressChanged?.Invoke(0f);
        }
    }
}