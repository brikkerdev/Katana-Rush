using UnityEngine;

namespace Runner.Player.Data
{
    [CreateAssetMenu(fileName = "MovementSettings", menuName = "Runner/Movement Settings")]
    public class MovementSettings : ScriptableObject
    {
        [Header("Lane Layout")]
        public int laneCount = 3;
        public float laneDistance = 3f;

        [Header("Lane Switch Animation")]
        public float laneSwitchDuration = 0.15f;
        public AnimationCurve laneSwitchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Lane Visual - Tilt")]
        public float maxTiltAngle = 20f;
        public float tiltSpeed = 10f;
        public AnimationCurve tiltCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Jump Animation")]
        public AnimationCurve jumpCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(0.5f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -2f, 0f)
        );

        [Header("Jump Visual - Squash & Stretch")]
        public float jumpStretchY = 1.2f;
        public float jumpSquashXZ = 0.85f;
        public float landSquashY = 0.7f;
        public float landStretchXZ = 1.2f;
        public float squashStretchDuration = 0.1f;
        public AnimationCurve squashStretchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Dash Animation")]
        public AnimationCurve dashCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Dash Visual")]
        public float dashStretchZ = 1.3f;
        public float dashSquashXY = 0.9f;

        [Header("Slide Settings")]
        public float slideDuration = 0.8f;
        public float slideColliderHeight = 0.5f;
        public float slideColliderCenterY = 0.25f;
        public float slideCooldown = 0.2f;
        public float slideSpeedBoost = 1.15f;

        [Header("Ground Check")]
        public float groundCheckRadius = 0.25f;
        public float groundCheckOffset = 0.1f;
        public LayerMask groundLayer = -1;

        [Header("Input Timing")]
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.15f;

        [Header("Physics")]
        public float fallMultiplier = 2.5f;
        public float maxFallSpeed = -40f;
    }
}