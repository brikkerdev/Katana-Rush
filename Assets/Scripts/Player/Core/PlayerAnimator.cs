using UnityEngine;
using Runner.Player.Core;

namespace Runner.Player
{
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Layer Settings")]
        [SerializeField] private string baseLayerName = "Base Layer";
        [SerializeField] private string combatLayerName = "Combat Layer";
        [SerializeField] private string additiveLayerName = "Additive Layer";

        [Header("Blend Settings")]
        [SerializeField] private float blockBlendSpeed = 10f;
        [SerializeField] private float combatLayerWeight = 1f;
        [SerializeField] private float additiveLayerWeight = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private Player _player;
        private PlayerController _controller;

        private int baseLayerIndex;
        private int combatLayerIndex;
        private int additiveLayerIndex;

        private float targetCombatWeight;
        private float currentCombatWeight;

        private static readonly int IsRunning = Animator.StringToHash("IsRunning");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int IsDashing = Animator.StringToHash("IsDashing");
        private static readonly int IsBlocking = Animator.StringToHash("IsBlocking");
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");

        private static readonly int JumpTrigger = Animator.StringToHash("Jump");
        private static readonly int DashTrigger = Animator.StringToHash("Dash");
        private static readonly int SlashTrigger = Animator.StringToHash("Slash");
        private static readonly int BlockHitTrigger = Animator.StringToHash("BlockHit");
        private static readonly int DieTrigger = Animator.StringToHash("Die");
        private static readonly int ReviveTrigger = Animator.StringToHash("Revive");

        public bool IsBlockingActive { get; private set; }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }

            CacheLayerIndices();
        }

        private void CacheLayerIndices()
        {
            if (animator == null) return;

            baseLayerIndex = animator.GetLayerIndex(baseLayerName);
            combatLayerIndex = animator.GetLayerIndex(combatLayerName);
            additiveLayerIndex = animator.GetLayerIndex(additiveLayerName);

            if (baseLayerIndex == -1) baseLayerIndex = 0;
            if (combatLayerIndex == -1) combatLayerIndex = 1;
            if (additiveLayerIndex == -1) additiveLayerIndex = 2;
        }

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

            UpdateBaseParameters();
        }

        private void UpdateBaseParameters()
        {
            animator.SetBool(IsRunning, _player.IsRunning);
            animator.SetBool(IsGrounded, _controller.IsGrounded);
            animator.SetBool(IsDashing, _controller.IsDashing);
            animator.SetFloat(Speed, _controller.CurrentSpeed);
        }

        public void PlayJumpAnimation()
        {
            if (animator == null) return;
            ClearMovementTriggers();
            animator.SetTrigger(JumpTrigger);
        }

        public void PlayDashAnimation()
        {
            if (animator == null) return;
            ClearMovementTriggers();
            animator.SetTrigger(DashTrigger);
        }

        public void PlaySlashAnimation()
        {
            if (animator == null) return;
            animator.SetTrigger(SlashTrigger);
        }

        public void SetBlocking(bool blocking)
        {
            if (animator == null) return;
            if (IsBlockingActive == blocking) return;

            IsBlockingActive = blocking;
            animator.SetBool(IsBlocking, blocking);

            targetCombatWeight = blocking ? combatLayerWeight : 0f;

            if (showDebug)
            {
                Debug.Log($"[PlayerAnimator] Blocking: {blocking}");
            }
        }

        public void PlayBlockHitReaction()
        {
            if (animator == null) return;

            if (additiveLayerIndex >= 0 && additiveLayerIndex < animator.layerCount)
            {
                animator.SetTrigger(BlockHitTrigger);
            }
        }

        public void PlayDeathAnimation()
        {
            if (animator == null) return;

            ClearMovementTriggers();
            targetCombatWeight = 0f;
            animator.SetBool(IsBlocking, false);
            animator.SetTrigger(DieTrigger);
        }

        public void PlayReviveAnimation()
        {
            if (animator == null) return;
            ClearMovementTriggers();
            animator.SetTrigger(ReviveTrigger);
        }

        private void ClearMovementTriggers()
        {
            if (animator == null) return;
            animator.ResetTrigger(JumpTrigger);
            animator.ResetTrigger(DashTrigger);
            animator.ResetTrigger(SlashTrigger);
        }

        public void ResetAnimator()
        {
            if (animator == null) return;

            animator.ResetTrigger(JumpTrigger);
            animator.ResetTrigger(DashTrigger);
            animator.ResetTrigger(SlashTrigger);
            animator.ResetTrigger(BlockHitTrigger);
            animator.ResetTrigger(DieTrigger);
            animator.ResetTrigger(ReviveTrigger);

            animator.SetBool(IsRunning, false);
            animator.SetBool(IsGrounded, true);
            animator.SetBool(IsDashing, false);
            animator.SetBool(IsBlocking, false);
            animator.SetFloat(Speed, 0f);

            IsBlockingActive = false;
            targetCombatWeight = 0f;
            currentCombatWeight = 0f;

            if (combatLayerIndex >= 0 && combatLayerIndex < animator.layerCount)
            {
                animator.SetLayerWeight(combatLayerIndex, 0f);
            }
        }

        public void ForceUpdateAnimator()
        {
            if (animator == null) return;
            animator.Update(0f);
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
            {
                animator = GetComponent<Animator>();
            }
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebug) return;
            if (animator == null) return;

            GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 210, 240));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>ANIMATOR STATE</b>");
            GUILayout.Label($"Running: {_player?.IsRunning}");
            GUILayout.Label($"Grounded: {_controller?.IsGrounded}");
            GUILayout.Label($"Dashing: {_controller?.IsDashing}");
            GUILayout.Label($"Blocking: {IsBlockingActive}");
            GUILayout.Label($"Combat Weight: {currentCombatWeight:F2}");

            if (combatLayerIndex >= 0 && combatLayerIndex < animator.layerCount)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(combatLayerIndex);
                GUILayout.Label($"Combat State: {stateInfo.shortNameHash}");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif
    }
}