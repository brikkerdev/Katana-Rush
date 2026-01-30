using UnityEngine;

namespace Runner.Player.Data
{
    [CreateAssetMenu(fileName = "MovementSettings", menuName = "Runner/Movement Settings")]
    public class MovementSettings : ScriptableObject
    {
        [Header("Lane Layout")]
        [Tooltip("Number of lanes")]
        public int laneCount = 3;

        [Tooltip("Distance between lanes")]
        public float laneDistance = 3f;

        [Header("Lane Switch Animation")]
        [Tooltip("Duration of lane switch")]
        public float laneSwitchDuration = 0.15f;

        [Tooltip("Curve for lane switch interpolation")]
        public AnimationCurve laneSwitchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Lane Visual - Tilt")]
        [Tooltip("Maximum tilt angle when switching lanes")]
        public float maxTiltAngle = 20f;

        [Tooltip("Speed of tilt interpolation")]
        public float tiltSpeed = 10f;

        [Tooltip("Curve for tilt animation")]
        public AnimationCurve tiltCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Jump Animation")]
        [Tooltip("Curve for jump height over time (normalized)")]
        public AnimationCurve jumpCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(0.5f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -2f, 0f)
        );

        [Header("Jump Visual - Squash & Stretch")]
        [Tooltip("Scale multiplier when starting jump (Y stretch)")]
        public float jumpStretchY = 1.2f;

        [Tooltip("Scale multiplier when starting jump (XZ squash)")]
        public float jumpSquashXZ = 0.85f;

        [Tooltip("Scale multiplier when landing (Y squash)")]
        public float landSquashY = 0.7f;

        [Tooltip("Scale multiplier when landing (XZ stretch)")]
        public float landStretchXZ = 1.2f;

        [Tooltip("Duration of squash/stretch effect")]
        public float squashStretchDuration = 0.1f;

        [Tooltip("Curve for squash/stretch recovery")]
        public AnimationCurve squashStretchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Dash Animation")]
        [Tooltip("Curve for dash speed over time (normalized)")]
        public AnimationCurve dashCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Dash Visual")]
        [Tooltip("Z stretch during dash")]
        public float dashStretchZ = 1.3f;

        [Tooltip("XY squash during dash")]
        public float dashSquashXY = 0.9f;

        [Header("Ground Check")]
        [Tooltip("Radius of ground check sphere")]
        public float groundCheckRadius = 0.25f;

        [Tooltip("Offset from player position for ground check")]
        public float groundCheckOffset = 0.1f;

        [Tooltip("Layer mask for ground detection")]
        public LayerMask groundLayer = -1;

        [Header("Input Timing")]
        [Tooltip("Time after leaving ground where jump is still allowed")]
        public float coyoteTime = 0.1f;

        [Tooltip("Time before landing where jump input is buffered")]
        public float jumpBufferTime = 0.15f;

        [Header("Physics")]
        [Tooltip("Multiplier for falling speed (makes falls feel snappier)")]
        public float fallMultiplier = 2.5f;

        [Tooltip("Maximum fall velocity")]
        public float maxFallSpeed = -40f;
    }
}