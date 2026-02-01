using System;
using UnityEngine;
using Runner.Player.Core;
using Runner.Player.Visual;

namespace Runner.Player
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerCollision playerCollision;
        [SerializeField] private PlayerVisual playerVisual;
        [SerializeField] private PlayerAnimator animator;
        [SerializeField] private PlayerBlockDetector blockDetector;

        public PlayerController Controller => controller;
        public PlayerVisual Visual => playerVisual;
        public PlayerAnimator Animator => animator;
        public PlayerBlockDetector BlockDetector => blockDetector;
        public PlayerState State { get; private set; }
        public bool IsAlive => State != PlayerState.Dead;
        public bool IsRunning => State == PlayerState.Running;
        public bool IsBlocking => blockDetector != null && blockDetector.IsBlocking;

        public event Action OnPlayerDeath;
        public event Action OnPlayerRevive;

        public void Initialize()
        {
            State = PlayerState.Idle;

            GatherComponents();

            controller.Initialize(this);

            if (playerCollision != null)
            {
                playerCollision.Initialize(this);
            }

            if (animator != null)
            {
                animator.Initialize(this);
            }

            if (blockDetector != null)
            {
                blockDetector.Initialize(this);
            }
        }

        private void GatherComponents()
        {
            if (controller == null)
            {
                controller = GetComponent<PlayerController>();
            }

            if (playerCollision == null)
            {
                playerCollision = GetComponent<PlayerCollision>();
            }

            if (playerVisual == null)
            {
                playerVisual = GetComponentInChildren<PlayerVisual>();
            }

            if (animator == null)
            {
                animator = GetComponent<PlayerAnimator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<PlayerAnimator>();
                }
            }

            if (blockDetector == null)
            {
                blockDetector = GetComponent<PlayerBlockDetector>();
                if (blockDetector == null)
                {
                    blockDetector = GetComponentInChildren<PlayerBlockDetector>();
                }
            }
        }

        public void StartRunning()
        {
            if (State == PlayerState.Dead) return;

            State = PlayerState.Running;
            controller.EnableInput();
        }

        public void Die()
        {
            if (State == PlayerState.Dead) return;

            State = PlayerState.Dead;
            controller.DisableInput();
            OnPlayerDeath?.Invoke();
        }

        public void Revive()
        {
            State = PlayerState.Running;
            controller.EnableInput();
            controller.RestoreDashes();
            OnPlayerRevive?.Invoke();
        }

        public void Reset()
        {
            State = PlayerState.Idle;
            controller.ResetController();

            if (playerVisual != null)
            {
                playerVisual.Reset();
            }

            if (animator != null)
            {
                animator.ResetAnimator();
                animator.PlayReviveAnimation();
            }
        }

        private void OnValidate()
        {
            GatherComponents();
        }
    }

    public enum PlayerState
    {
        Idle,
        Running,
        Dead
    }
}