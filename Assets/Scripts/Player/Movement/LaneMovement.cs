using System;
using UnityEngine;
using Runner.Player.Data;

namespace Runner.Player.Movement
{
    public class LaneHandler
    {
        private MovementSettings settings;
        private float laneSwitchSpeed;

        private int currentLane;
        private int targetLane;
        private float switchProgress;
        private float startX;
        private float targetX;
        private float currentX;
        private bool isSwitching;

        public int CurrentLane => currentLane;
        public int TargetLane => targetLane;
        public float CurrentX => currentX;
        public bool IsSwitching => isSwitching;
        public float SwitchProgress => switchProgress;

        public event Action OnLaneSwitched;

        public void Initialize(MovementSettings movementSettings, float presetLaneSwitchSpeed)
        {
            settings = movementSettings;
            laneSwitchSpeed = presetLaneSwitchSpeed;

            currentLane = settings.laneCount / 2;
            targetLane = currentLane;
            currentX = GetLaneX(currentLane);
            targetX = currentX;
            startX = currentX;
            switchProgress = 0f;
            isSwitching = false;
        }

        public void UpdateLaneSwitchSpeed(float speed)
        {
            laneSwitchSpeed = speed;
        }

        public bool TryMoveLeft()
        {
            if (targetLane <= 0) return false;

            StartSwitch(targetLane - 1);
            OnLaneSwitched?.Invoke();
            return true;
        }

        public bool TryMoveRight()
        {
            if (targetLane >= settings.laneCount - 1) return false;

            StartSwitch(targetLane + 1);
            OnLaneSwitched?.Invoke();
            return true;
        }

        private void StartSwitch(int newLane)
        {
            if (isSwitching)
            {
                currentLane = targetLane;
                startX = currentX;
            }
            else
            {
                startX = currentX;
            }

            targetLane = newLane;
            targetX = GetLaneX(newLane);
            switchProgress = 0f;
            isSwitching = true;
        }

        public void Update(float deltaTime)
        {
            if (!isSwitching)
            {
                currentX = Mathf.MoveTowards(currentX, targetX, laneSwitchSpeed * deltaTime);
                return;
            }

            switchProgress += deltaTime / settings.laneSwitchDuration;

            if (switchProgress >= 1f)
            {
                switchProgress = 1f;
                currentLane = targetLane;
                isSwitching = false;
            }

            float curveValue = settings.laneSwitchCurve.Evaluate(switchProgress);
            currentX = Mathf.LerpUnclamped(startX, targetX, curveValue);
        }

        public float GetTiltAngle()
        {
            if (!isSwitching) return 0f;

            float direction = targetX > startX ? -1f : 1f;

            float midPoint = 0.5f;
            float tiltStrength;

            if (switchProgress < midPoint)
            {
                tiltStrength = settings.tiltCurve.Evaluate(switchProgress / midPoint);
            }
            else
            {
                tiltStrength = settings.tiltCurve.Evaluate(1f - (switchProgress - midPoint) / midPoint);
            }

            return direction * settings.maxTiltAngle * tiltStrength;
        }

        private float GetLaneX(int lane)
        {
            int middleLane = settings.laneCount / 2;
            return (lane - middleLane) * settings.laneDistance;
        }

        public void Reset()
        {
            currentLane = settings.laneCount / 2;
            targetLane = currentLane;
            currentX = GetLaneX(currentLane);
            targetX = currentX;
            startX = currentX;
            switchProgress = 0f;
            isSwitching = false;
        }
    }
}