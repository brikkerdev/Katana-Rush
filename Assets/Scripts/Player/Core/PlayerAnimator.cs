using Runner.Player.Core;
using UnityEngine;

namespace Runner.Player
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private Player _player;
        private PlayerController _controller;

        private static readonly int IsRunning = Animator.StringToHash("IsRunning");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int IsJumping = Animator.StringToHash("IsJumping");
        private static readonly int IsDashing = Animator.StringToHash("IsDashing");
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int JumpTrigger = Animator.StringToHash("Jump");
        private static readonly int DashTrigger = Animator.StringToHash("Dash");
        private static readonly int DieTrigger = Animator.StringToHash("Die");
        private static readonly int ReviveTrigger = Animator.StringToHash("Revive");

        public void Initialize(Player player)
        {
            _player = player;
            _controller = player.Controller;

            _player.OnPlayerDeath += PlayDeathAnimation;
            _player.OnPlayerRevive += PlayReviveAnimation;
        }

        private void Update()
        {
            if (animator == null) return;
            if (_controller == null) return;

            animator.SetBool(IsRunning, _player.IsRunning);
            animator.SetBool(IsGrounded, _controller.IsGrounded);
            animator.SetBool(IsDashing, _controller.IsDashing);
            animator.SetFloat(Speed, _controller.CurrentSpeed);
        }

        public void PlayJumpAnimation()
        {
            animator?.SetTrigger(JumpTrigger);
        }

        public void PlayDashAnimation()
        {
            animator?.SetTrigger(DashTrigger);
        }

        public void PlayDeathAnimation()
        {
            animator?.SetTrigger(DieTrigger);
        }

        public void PlayReviveAnimation()
        {
            animator?.SetTrigger(ReviveTrigger);
        }

        public void ResetAnimator()
        {
            if (animator == null) return;

            // Reset all triggers to prevent stuck states
            animator.ResetTrigger(JumpTrigger);
            animator.ResetTrigger(DashTrigger);
            animator.ResetTrigger(DieTrigger);
            animator.ResetTrigger(ReviveTrigger);

            // Reset to idle state
            animator.SetBool(IsRunning, false);
            animator.SetBool(IsGrounded, true);
            animator.SetBool(IsDashing, false);
            animator.SetFloat(Speed, 0f);
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.OnPlayerDeath -= PlayDeathAnimation;
                _player.OnPlayerRevive -= PlayReviveAnimation;
            }
        }

        private void OnValidate()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
        }
    }
}