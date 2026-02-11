using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Runner.Input
{
    public class InputReader : MonoBehaviour
    {
        public static InputReader Instance { get; private set; }

        [Header("Swipe Settings (Percentage of Screen)")]
        [SerializeField, Range(0.02f, 0.15f)]
        private float minSwipePercent = 0.05f;

        [SerializeField, Range(0.005f, 0.05f)]
        private float maxTapPercent = 0.015f;

        [SerializeField]
        private float maxSwipeTime = 0.4f;

        [SerializeField]
        private float maxTapTime = 0.2f;

        [Header("Direction Detection")]
        [SerializeField, Range(0.3f, 0.8f)]
        private float directionalThreshold = 0.6f;

        [Header("Debug")]
        [SerializeField] private bool showDebugUI = false;

        private Vector2 touchStartPosition;
        private float touchStartTime;
        private int activeTouchId = -1;

        private float screenDiagonal;
        private float minSwipeDistance;
        private float maxTapDistance;

        private string lastGesture = "None";

        public event Action OnJump;
        public event Action OnMoveLeft;
        public event Action OnMoveRight;
        public event Action OnDash;
        public event Action OnSlide;
        public event Action OnPause;

        private bool inputEnabled = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            CalculateScreenThresholds();
        }

        private void CalculateScreenThresholds()
        {
            screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
            minSwipeDistance = screenDiagonal * minSwipePercent;
            maxTapDistance = screenDiagonal * maxTapPercent;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerUp += OnFingerUp;
        }

        private void OnDisable()
        {
            Touch.onFingerDown -= OnFingerDown;
            Touch.onFingerUp -= OnFingerUp;
            EnhancedTouchSupport.Disable();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void EnableGameplayInput()
        {
            inputEnabled = true;
        }

        public void DisableGameplayInput()
        {
            inputEnabled = false;
        }

        private void OnFingerDown(Finger finger)
        {
            if (!inputEnabled) return;
            if (activeTouchId >= 0) return;

            activeTouchId = finger.index;
            touchStartPosition = finger.screenPosition;
            touchStartTime = Time.unscaledTime;
        }

        private void OnFingerUp(Finger finger)
        {
            if (!inputEnabled) return;
            if (finger.index != activeTouchId) return;

            Vector2 touchEndPosition = finger.screenPosition;
            float duration = Time.unscaledTime - touchStartTime;

            activeTouchId = -1;

            ProcessGesture(touchStartPosition, touchEndPosition, duration);
        }

        private void ProcessGesture(Vector2 startPos, Vector2 endPos, float duration)
        {
            Vector2 delta = endPos - startPos;
            float distance = delta.magnitude;

            if (distance <= maxTapDistance && duration <= maxTapTime)
            {
                lastGesture = "TAP";
                OnDash?.Invoke();
                return;
            }

            if (distance >= minSwipeDistance && duration <= maxSwipeTime)
            {
                float absX = Mathf.Abs(delta.x);
                float absY = Mathf.Abs(delta.y);
                float total = absX + absY;

                if (total < 0.001f) return;

                float horizontalRatio = absX / total;
                float verticalRatio = absY / total;

                if (verticalRatio > directionalThreshold)
                {
                    if (delta.y > 0)
                    {
                        lastGesture = "SWIPE UP";
                        OnJump?.Invoke();
                    }
                    else
                    {
                        lastGesture = "SWIPE DOWN";
                        OnSlide?.Invoke();
                    }
                }
                else if (horizontalRatio > directionalThreshold)
                {
                    if (delta.x > 0)
                    {
                        lastGesture = "SWIPE RIGHT";
                        OnMoveRight?.Invoke();
                    }
                    else
                    {
                        lastGesture = "SWIPE LEFT";
                        OnMoveLeft?.Invoke();
                    }
                }
            }
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleKeyboardInput();
#endif
            HandlePauseInput();
        }

        private void HandleKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (!inputEnabled) return;

            if (keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.wKey.wasPressedThisFrame ||
                keyboard.upArrowKey.wasPressedThisFrame)
            {
                lastGesture = "KEY: Jump";
                OnJump?.Invoke();
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                lastGesture = "KEY: Left";
                OnMoveLeft?.Invoke();
            }

            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                lastGesture = "KEY: Right";
                OnMoveRight?.Invoke();
            }

            if (keyboard.leftShiftKey.wasPressedThisFrame)
            {
                lastGesture = "KEY: Dash";
                OnDash?.Invoke();
            }

            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                lastGesture = "KEY: Slide";
                OnSlide?.Invoke();
            }
        }

        private void HandlePauseInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                OnPause?.Invoke();
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!showDebugUI) return;

            int boxWidth = 300;
            int boxHeight = 120;

            GUILayout.BeginArea(new Rect(10, Screen.height - boxHeight - 10, boxWidth, boxHeight));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b>INPUT</b>");
            GUILayout.Label($"Tap: {maxTapDistance:F0}px | Swipe: {minSwipeDistance:F0}px");
            GUILayout.Label($"Active Touch: {activeTouchId}");
            GUILayout.Label($"Last: {lastGesture}");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif
    }
}