using UnityEngine;
using System;

namespace Runner.Player
{
    public enum BufferedInput
    {
        None,
        Jump,
        MoveLeft,
        MoveRight,
        Dash
    }

    public class PlayerInputBuffer : MonoBehaviour
    {
        [SerializeField] private float bufferDuration = 0.15f;

        private BufferedInput currentBuffer;
        private float bufferTimer;

        public event Action OnJumpBuffered;
        public event Action OnMoveLeftBuffered;
        public event Action OnMoveRightBuffered;
        public event Action OnDashBuffered;

        public bool HasBufferedInput => currentBuffer != BufferedInput.None && bufferTimer > 0f;
        public BufferedInput CurrentBuffer => currentBuffer;

        public void BufferInput(BufferedInput input)
        {
            currentBuffer = input;
            bufferTimer = bufferDuration;
        }

        public void ConsumeBuffer()
        {
            currentBuffer = BufferedInput.None;
            bufferTimer = 0f;
        }

        public bool TryConsumeBuffer(BufferedInput expectedInput)
        {
            if (currentBuffer == expectedInput && bufferTimer > 0f)
            {
                ConsumeBuffer();
                return true;
            }
            return false;
        }

        private void Update()
        {
            if (bufferTimer > 0f)
            {
                bufferTimer -= Time.deltaTime;

                if (bufferTimer <= 0f)
                {
                    currentBuffer = BufferedInput.None;
                }
            }
        }

        public void Clear()
        {
            currentBuffer = BufferedInput.None;
            bufferTimer = 0f;
        }
    }
}