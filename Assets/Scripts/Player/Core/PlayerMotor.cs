using UnityEngine;
using Runner.Player.Data;

namespace Runner.Player.Core
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        private CharacterController controller;
        private MovementSettings settings;

        private float verticalVelocity;
        private bool isGrounded;
        private bool wasGrounded;

        public CharacterController Controller => controller;
        public float VerticalVelocity => verticalVelocity;
        public bool IsGrounded => isGrounded;
        public bool JustLanded => isGrounded && !wasGrounded;
        public bool JustLeftGround => !isGrounded && wasGrounded;

        public void Initialize(MovementSettings movementSettings)
        {
            controller = GetComponent<CharacterController>();
            settings = movementSettings;
        }

        public void UpdateGroundCheck()
        {
            wasGrounded = isGrounded;

            if (settings.groundLayer == 0)
            {
                isGrounded = controller.isGrounded;
            }
            else
            {
                Vector3 origin = transform.position + Vector3.up * settings.groundCheckOffset;
                isGrounded = Physics.CheckSphere(origin, settings.groundCheckRadius, settings.groundLayer);
            }
        }

        public void SetVerticalVelocity(float velocity)
        {
            verticalVelocity = velocity;
        }

        public void AddVerticalVelocity(float amount)
        {
            verticalVelocity += amount;
        }

        public void ApplyGravity(float gravity, float fallMultiplier, float deltaTime)
        {
            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
                return;
            }

            float multiplier = verticalVelocity < 0f ? fallMultiplier : 1f;
            verticalVelocity += gravity * multiplier * deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, settings.maxFallSpeed);
        }

        public void Move(float deltaX, float deltaZ, float deltaTime)
        {
            Vector3 motion = new Vector3(
                deltaX,
                verticalVelocity * deltaTime,
                deltaZ
            );

            controller.Move(motion);
        }

        public void ResetTo(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;

            verticalVelocity = 0f;
            isGrounded = true;
            wasGrounded = true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (settings == null) return;

            Vector3 origin = transform.position + Vector3.up * settings.groundCheckOffset;
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(origin, settings.groundCheckRadius);
        }
#endif
    }
}