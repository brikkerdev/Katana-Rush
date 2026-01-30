using UnityEngine;
using Runner.Player.Data;

namespace Runner.Player.Movement
{
    public class JumpHandler
    {
        private MovementSettings settings;
        private float jumpForce;
        private float gravity;
        private int maxJumps;

        private int jumpsRemaining;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private bool jumpBuffered;
        private bool isJumping;
        private float jumpTime;

        public int JumpsRemaining => jumpsRemaining;
        public int MaxJumps => maxJumps;
        public bool IsJumping => isJumping;
        public bool HasCoyoteTime => coyoteTimer > 0f;
        public bool HasBufferedJump => jumpBuffered && jumpBufferTimer > 0f;

        public void Initialize(MovementSettings movementSettings, PlayerPreset preset)
        {
            settings = movementSettings;
            ApplyPreset(preset);
        }

        public void ApplyPreset(PlayerPreset preset)
        {
            jumpForce = preset.JumpForce;
            gravity = preset.Gravity;
            maxJumps = preset.MaxJumps;
            jumpsRemaining = maxJumps;
        }

        public void Update(float deltaTime, bool isGrounded)
        {
            if (isGrounded)
            {
                coyoteTimer = settings.coyoteTime;
                jumpsRemaining = maxJumps;

                if (isJumping)
                {
                    isJumping = false;
                }
            }
            else
            {
                if (coyoteTimer > 0f)
                {
                    coyoteTimer -= deltaTime;
                }
            }

            if (jumpBuffered)
            {
                jumpBufferTimer -= deltaTime;

                if (jumpBufferTimer <= 0f)
                {
                    jumpBuffered = false;
                }
            }

            if (isJumping)
            {
                jumpTime += deltaTime;
            }
        }

        public void BufferJump()
        {
            jumpBuffered = true;
            jumpBufferTimer = settings.jumpBufferTime;
        }

        public bool TryJump(out float verticalVelocity)
        {
            verticalVelocity = 0f;

            bool canJump = HasCoyoteTime || jumpsRemaining > 0;

            if (!canJump) return false;

            if (coyoteTimer > 0f)
            {
                coyoteTimer = 0f;
            }

            jumpsRemaining--;
            verticalVelocity = jumpForce;
            isJumping = true;
            jumpTime = 0f;
            jumpBuffered = false;

            return true;
        }

        public bool TryConsumeBufferedJump(out float verticalVelocity)
        {
            verticalVelocity = 0f;

            if (!HasBufferedJump) return false;

            return TryJump(out verticalVelocity);
        }

        public float ApplyGravity(float currentVelocityY, float deltaTime)
        {
            float multiplier = currentVelocityY < 0f ? settings.fallMultiplier : 1f;
            float newVelocity = currentVelocityY + gravity * multiplier * deltaTime;
            return Mathf.Max(newVelocity, settings.maxFallSpeed);
        }

        public void Reset()
        {
            jumpsRemaining = maxJumps;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            jumpBuffered = false;
            isJumping = false;
            jumpTime = 0f;
        }
    }
}