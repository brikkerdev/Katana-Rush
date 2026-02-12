using UnityEngine;
using Runner.Core;
using Runner.Player.Core;

namespace Runner.Player
{
    public class PlayerFootsteps : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float baseStepInterval = 0.35f;
        [SerializeField] private float minStepInterval = 0.15f;
        [SerializeField] private float speedReference = 20f;

        [Header("Volume")]
        [SerializeField] private float baseVolume = 0.3f;
        [SerializeField] private float maxVolume = 0.5f;
        [SerializeField] private float slideVolume = 0.15f;

        [Header("Pitch")]
        [SerializeField] private float basePitch = 0.9f;
        [SerializeField] private float maxPitch = 1.2f;
        [SerializeField] private float pitchVariation = 0.08f;

        [Header("Clips")]
        [SerializeField] private AudioClip[] footstepClips;

        private Player player;
        private PlayerController controller;
        private float stepTimer;
        private int lastClipIndex = -1;
        private bool initialized;

        public void Initialize(Player playerRef)
        {
            player = playerRef;
            controller = player.Controller;
            initialized = true;
            stepTimer = 0f;
        }

        private void Update()
        {
            if (!initialized) return;
            if (player == null) return;
            if (!player.IsRunning) return;
            if (controller == null) return;

            if (!controller.IsGrounded) return;
            if (controller.IsDashing) return;

            float speed = controller.CurrentSpeed;
            if (speed < 0.1f) return;

            float speedRatio = Mathf.Clamp01(speed / speedReference);
            float interval = Mathf.Lerp(baseStepInterval, minStepInterval, speedRatio);

            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                stepTimer = interval;
                PlayFootstep(speedRatio);
            }
        }

        private void PlayFootstep(float speedRatio)
        {
            if (Game.Instance?.Sound == null) return;

            AudioClip clip = GetNextClip();
            if (clip == null) return;

            float volume;
            float pitch;

            volume = Mathf.Lerp(baseVolume, maxVolume, speedRatio);
            pitch = Mathf.Lerp(basePitch, maxPitch, speedRatio) + Random.Range(-pitchVariation, pitchVariation);

            Game.Instance.Sound.Play(clip, volume, pitch);
        }

        private AudioClip GetNextClip()
        {
            AudioClip[] clips;

            if (footstepClips != null && footstepClips.Length > 0)
            {
                clips = footstepClips;
            }
            else
            {
                return null;
            }

            if (clips.Length == 1) return clips[0];

            int index = Random.Range(0, clips.Length);

            while (index == lastClipIndex && clips.Length > 1)
            {
                index = Random.Range(0, clips.Length);
            }

            lastClipIndex = index;
            return clips[index];
        }

        public void Reset()
        {
            stepTimer = 0f;
            lastClipIndex = -1;
        }
    }
}