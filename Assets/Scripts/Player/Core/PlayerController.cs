using UnityEngine;
using System;
using Runner.Input;
using Runner.Core;
using Runner.Inventory;
using Runner.Player.Data;
using Runner.Player.Movement;
using Runner.Player.Visual;

namespace Runner.Player.Core
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private MovementSettings movementSettings;
        [SerializeField] private PlayerPreset defaultPreset;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private Player player;
        private PlayerMotor motor;
        private PlayerVisual visual;
        private PlayerAnimator playerAnimator; // ADD THIS
        private InputReader inputReader;
        private PlayerPreset currentPreset;

        private LaneHandler laneHandler;
        private JumpHandler jumpHandler;
        private DashHandler dashHandler;

        private float currentSpeed;
        private float runDistance;
        private Vector3 startPosition;
        private bool inputEnabled;

        public event Action<int, int> OnDashCountChanged;
        public event Action<float> OnDashRegenProgress;

        public float CurrentSpeed => currentSpeed;
        public float RunDistance => runDistance;
        public bool IsGrounded => motor != null && motor.IsGrounded;
        public bool IsDashing => dashHandler != null && dashHandler.IsDashing;
        public bool IsInvincible => dashHandler != null && dashHandler.IsInvincible;
        public int CurrentLane => laneHandler != null ? laneHandler.CurrentLane : 0;
        public int JumpsRemaining => jumpHandler != null ? jumpHandler.JumpsRemaining : 0;
        public int MaxJumps => jumpHandler != null ? jumpHandler.MaxJumps : 0;
        public int DashesRemaining => dashHandler != null ? dashHandler.DashesRemaining : 0;
        public int MaxDashes => dashHandler != null ? dashHandler.MaxDashes : 0;
        public float DashRegenProgressValue => dashHandler != null ? dashHandler.RegenProgress : 0f;
        public PlayerPreset CurrentPreset => currentPreset;
        public MovementSettings Settings => movementSettings;

        public void Initialize(Player playerRef)
        {
            player = playerRef;
            startPosition = transform.position;
            inputReader = InputReader.Instance;

            SetupComponents();
            SetupHandlers();
            LoadPreset();
        }

        private void SetupComponents()
        {
            motor = GetComponent<PlayerMotor>();
            if (motor == null)
            {
                motor = gameObject.AddComponent<PlayerMotor>();
            }
            motor.Initialize(movementSettings);

            visual = GetComponentInChildren<PlayerVisual>();
            if (visual != null)
            {
                visual.Initialize(movementSettings, this);
            }

            // ADD THIS - Get animator reference
            playerAnimator = GetComponent<PlayerAnimator>();
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<PlayerAnimator>();
            }
        }

        private void SetupHandlers()
        {
            laneHandler = new LaneHandler();
            jumpHandler = new JumpHandler();
            dashHandler = new DashHandler();

            dashHandler.OnDashCountChanged += (current, max) => OnDashCountChanged?.Invoke(current, max);
            dashHandler.OnRegenProgressChanged += (progress) => OnDashRegenProgress?.Invoke(progress);
            dashHandler.OnDashStarted += OnDashStarted;
            dashHandler.OnDashEnded += OnDashEnded;
        }

        private void LoadPreset()
        {
            PlayerPreset preset = null;

            if (InventoryManager.Instance != null && InventoryManager.Instance.ActivePreset != null)
            {
                preset = InventoryManager.Instance.ActivePreset;
            }
            else if (defaultPreset != null)
            {
                preset = defaultPreset;
            }
            else
            {
                preset = PlayerPreset.CreateDefault();
            }

            ApplyPreset(preset);

            if (InventoryManager.Instance != null)
            {
                SetupKatanaVisual();
            }
        }

        public void ApplyPreset(PlayerPreset preset)
        {
            currentPreset = preset;

            laneHandler.Initialize(movementSettings, preset.LaneSwitchSpeed);
            jumpHandler.Initialize(movementSettings, preset);
            dashHandler.Initialize(movementSettings, preset);

            currentSpeed = preset.BaseSpeed;
        }

        private void SetupKatanaVisual()
        {
            if (visual == null) return;
            if (InventoryManager.Instance == null) return;
            if (InventoryManager.Instance.EquippedKatana == null) return;

            var katana = InventoryManager.Instance.EquippedKatana;
            if (katana.ModelPrefab != null)
            {
                visual.SetKatanaVisual(katana.ModelPrefab);
            }
        }

        public void EnableInput()
        {
            if (inputReader == null)
            {
                inputReader = InputReader.Instance;
            }

            if (inputReader == null) return;

            inputEnabled = true;
            inputReader.OnJump += OnJumpInput;
            inputReader.OnMoveLeft += OnMoveLeftInput;
            inputReader.OnMoveRight += OnMoveRightInput;
            inputReader.OnDash += OnDashInput;
        }

        public void DisableInput()
        {
            if (inputReader == null) return;

            inputEnabled = false;
            inputReader.OnJump -= OnJumpInput;
            inputReader.OnMoveLeft -= OnMoveLeftInput;
            inputReader.OnMoveRight -= OnMoveRightInput;
            inputReader.OnDash -= OnDashInput;
        }

        private void Update()
        {
            if (player == null) return;
            if (!player.IsRunning) return;

            float dt = Time.deltaTime;

            motor.UpdateGroundCheck();

            UpdateSpeed(dt);
            UpdateHandlers(dt);
            UpdateMovement(dt);
            UpdateRunDistance();

            CheckLanding();
            CheckBufferedJump();
        }

        private void UpdateSpeed(float dt)
        {
            if (currentSpeed < currentPreset.MaxSpeed)
            {
                currentSpeed += currentPreset.SpeedAcceleration * dt;
                currentSpeed = Mathf.Min(currentSpeed, currentPreset.MaxSpeed);
            }
        }

        private void UpdateHandlers(float dt)
        {
            laneHandler.Update(dt);
            jumpHandler.Update(dt, motor.IsGrounded);

            motor.ApplyGravity(currentPreset.Gravity, movementSettings.fallMultiplier, dt);
        }

        private void UpdateMovement(float dt)
        {
            float speed = currentSpeed;

            float dashMultiplier = dashHandler.Update(dt, currentSpeed);
            speed *= dashMultiplier;

            float gameSpeed = Game.Instance != null ? Game.Instance.GameSpeed : 1f;
            speed *= gameSpeed;

            float deltaX = laneHandler.CurrentX - transform.position.x;
            float deltaZ = speed * dt;

            motor.Move(deltaX, deltaZ, dt);
        }

        private void UpdateRunDistance()
        {
            runDistance = transform.position.z - startPosition.z;
        }

        private void CheckLanding()
        {
            if (motor.JustLanded)
            {
                visual?.PlayLandSquash();
                Game.Instance?.CameraEffects?.PlayLandEffect();
            }
        }

        private void CheckBufferedJump()
        {
            if (!motor.IsGrounded) return;

            if (jumpHandler.TryConsumeBufferedJump(out float velocity))
            {
                motor.SetVerticalVelocity(velocity);
                visual?.PlayJumpSquash();
                Game.Instance?.CameraEffects?.PlayJumpEffect();

                // ADD THIS - Play jump animation for buffered jump
                playerAnimator?.PlayJumpAnimation();
            }
        }

        public float GetLaneTiltAngle()
        {
            return laneHandler != null ? laneHandler.GetTiltAngle() : 0f;
        }

        private void OnJumpInput()
        {
            if (!inputEnabled) return;

            if (jumpHandler.TryJump(out float velocity))
            {
                motor.SetVerticalVelocity(velocity);
                visual?.PlayJumpSquash();
                Game.Instance?.CameraEffects?.PlayJumpEffect();

                // ADD THIS - Play jump animation
                playerAnimator?.PlayJumpAnimation();
            }
            else
            {
                jumpHandler.BufferJump();
            }
        }

        private void OnMoveLeftInput()
        {
            if (!inputEnabled) return;
            laneHandler.TryMoveLeft();
        }

        private void OnMoveRightInput()
        {
            if (!inputEnabled) return;
            laneHandler.TryMoveRight();
        }

        private void OnDashInput()
        {
            if (!inputEnabled) return;

            if (dashHandler.TryDash())
            {
                visual?.PlayDashStretch();
                Game.Instance?.CameraEffects?.PlayDashEffect();

                // ADD THIS - Play dash animation
                playerAnimator?.PlayDashAnimation();
            }
        }

        private void OnDashStarted()
        {
        }

        private void OnDashEnded()
        {
        }

        public void RestoreDashes()
        {
            dashHandler?.RestoreAllDashes();
        }

        public void ResetController()
        {
            currentSpeed = currentPreset != null ? currentPreset.BaseSpeed : 10f;
            runDistance = 0f;

            motor.ResetTo(startPosition);
            laneHandler.Reset();
            jumpHandler.Reset();
            dashHandler.Reset();
            visual?.Reset();
        }

        private void OnDestroy()
        {
            DisableInput();

            if (dashHandler != null)
            {
                dashHandler.OnDashCountChanged -= (current, max) => OnDashCountChanged?.Invoke(current, max);
                dashHandler.OnRegenProgressChanged -= (progress) => OnDashRegenProgress?.Invoke(progress);
                dashHandler.OnDashStarted -= OnDashStarted;
                dashHandler.OnDashEnded -= OnDashEnded;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (movementSettings == null) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < movementSettings.laneCount; i++)
            {
                int mid = movementSettings.laneCount / 2;
                float x = (i - mid) * movementSettings.laneDistance;
                Vector3 pos = new Vector3(x, transform.position.y, transform.position.z);
                Gizmos.DrawWireCube(pos, new Vector3(0.5f, 0.1f, 0.5f));
            }
        }

        private void OnGUI()
        {
            if (!showDebug) return;

            GUILayout.BeginArea(new Rect(10, 10, 500, 500));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b>PLAYER CONTROLLER</b>");
            GUILayout.Label($"Preset: {(currentPreset != null ? currentPreset.name : "null")}");

            GUILayout.Space(5);

            GUILayout.Label($"Speed: {currentSpeed:F1} / {currentPreset?.MaxSpeed}");
            GUILayout.Label($"Distance: {runDistance:F0}m");

            GUILayout.Space(5);

            GUILayout.Label($"Grounded: {IsGrounded}");
            GUILayout.Label($"Lane: {CurrentLane}");
            GUILayout.Label($"Jumps: {JumpsRemaining}/{MaxJumps}");

            GUILayout.Space(5);

            GUILayout.Label($"Dashing: {IsDashing}");
            GUILayout.Label($"Invincible: {IsInvincible}");
            GUILayout.Label($"Dashes: {DashesRemaining}/{MaxDashes}");
            GUILayout.Label($"Regen: {DashRegenProgressValue:P0}");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif
    }
}