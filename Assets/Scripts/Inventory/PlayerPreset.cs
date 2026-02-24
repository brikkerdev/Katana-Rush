using UnityEngine;

namespace Runner.Player.Data
{
    [CreateAssetMenu(fileName = "PlayerPreset", menuName = "Runner/Player Preset")]
    public class PlayerPreset : ScriptableObject
    {
        [Header("Speed")]
        [Tooltip("Starting run speed")]
        [SerializeField] private float baseSpeed = 12f;

        [Tooltip("Maximum run speed")]
        [SerializeField] private float maxSpeed = 30f;

        [Tooltip("Speed increase per second")]
        [SerializeField] private float speedAcceleration = 0.5f;

        [Tooltip("Speed of lane switching")]
        [SerializeField] private float laneSwitchSpeed = 15f;

        [Header("Jump")]
        [Tooltip("Vertical velocity applied on jump (physics-based fallback)")]
        [SerializeField] private float jumpForce = 16f;

        [Tooltip("Maximum number of jumps (1 = no double jump, 2 = double jump)")]
        [SerializeField] private int maxJumps = 2;

        [Tooltip("Gravity force")]
        [SerializeField] private float gravity = -45f;

        [Tooltip("Jump duration in seconds (Subway Surfers style - 0 = calculate from force/gravity)")]
        [SerializeField] private float jumpDuration = 0.5f;

        [Tooltip("Jump height in units (Subway Surfers style - 0 = calculate from force/gravity)")]
        [SerializeField] private float jumpHeight = 3f;

        [Header("Dash")]
        [Tooltip("Maximum number of dashes before needing to regenerate")]
        [SerializeField] private int maxDashes = 3;

        [Tooltip("Duration of dash in seconds")]
        [SerializeField] private float dashDuration = 0.25f;

        [Tooltip("Speed multiplier during dash")]
        [SerializeField] private float dashSpeedMultiplier = 2.5f;

        [Tooltip("Time to regenerate one dash")]
        [SerializeField] private float dashRegenTime = 3f;

        [Tooltip("Delay before dash regeneration starts")]
        [SerializeField] private float dashRegenDelay = 1f;

        [Tooltip("Is player invincible during dash")]
        [SerializeField] private bool dashInvincible = true;

        [Header("Combat")]
        [Tooltip("Damage dealt when dashing into enemy")]
        [SerializeField] private float dashDamage = 1f;

        public float BaseSpeed => baseSpeed;
        public float MaxSpeed => maxSpeed;
        public float SpeedAcceleration => speedAcceleration;
        public float LaneSwitchSpeed => laneSwitchSpeed;

        public float JumpForce => jumpForce;
        public int MaxJumps => maxJumps;
        public float Gravity => gravity;
        public float JumpDuration => jumpDuration;
        public float JumpHeight => jumpHeight;

        public int MaxDashes => maxDashes;
        public float DashDuration => dashDuration;
        public float DashSpeedMultiplier => dashSpeedMultiplier;
        public float DashRegenTime => dashRegenTime;
        public float DashRegenDelay => dashRegenDelay;
        public bool DashInvincible => dashInvincible;

        public float DashDamage => dashDamage;

        public static PlayerPreset CreateDefault()
        {
            var preset = CreateInstance<PlayerPreset>();
            preset.name = "DefaultPreset";
            return preset;
        }

        public void CopyFrom(PlayerPreset other)
        {
            if (other == null) return;

            baseSpeed = other.baseSpeed;
            maxSpeed = other.maxSpeed;
            speedAcceleration = other.speedAcceleration;
            laneSwitchSpeed = other.laneSwitchSpeed;

            jumpForce = other.jumpForce;
            maxJumps = other.maxJumps;
            gravity = other.gravity;
            jumpDuration = other.jumpDuration;
            jumpHeight = other.jumpHeight;

            maxDashes = other.maxDashes;
            dashDuration = other.dashDuration;
            dashSpeedMultiplier = other.dashSpeedMultiplier;
            dashRegenTime = other.dashRegenTime;
            dashRegenDelay = other.dashRegenDelay;
            dashInvincible = other.dashInvincible;

            dashDamage = other.dashDamage;
        }
    }
}