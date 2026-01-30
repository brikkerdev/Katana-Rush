using UnityEngine;
using Unity.Cinemachine;

namespace Runner.CameraSystem
{
    public enum CameraShakePreset
    {
        Light,
        Medium,
        Heavy,
        Death,
        Dash,
        Land
    }

    public class CameraShake : MonoBehaviour
    {
        [Header("Impulse Source")]
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [Header("Presets")]
        [SerializeField] private ShakeSettings lightShake = new(0.2f, 0.15f);
        [SerializeField] private ShakeSettings mediumShake = new(0.5f, 0.2f);
        [SerializeField] private ShakeSettings heavyShake = new(1f, 0.3f);
        [SerializeField] private ShakeSettings deathShake = new(1.5f, 0.5f);
        [SerializeField] private ShakeSettings dashShake = new(0.3f, 0.1f);
        [SerializeField] private ShakeSettings landShake = new(0.4f, 0.15f);

        [Header("Noise Settings")]
        [SerializeField] private NoiseSettings noiseProfile;

        private void Awake()
        {
            if (impulseSource == null)
            {
                impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            }

            ConfigureImpulseSource();
        }

        private void ConfigureImpulseSource()
        {
            if (noiseProfile != null)
            {
                impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Rumble;
            }
        }

        public void Shake(float intensity, float duration)
        {
            if (impulseSource == null) return;

            impulseSource.ImpulseDefinition.ImpulseDuration = duration;
            impulseSource.GenerateImpulse(Vector3.one * intensity);
        }

        public void Shake(CameraShakePreset preset)
        {
            ShakeSettings settings = GetSettingsForPreset(preset);
            Shake(settings.intensity, settings.duration);
        }

        public void ShakeWithDirection(Vector3 direction, float intensity, float duration)
        {
            if (impulseSource == null) return;

            impulseSource.ImpulseDefinition.ImpulseDuration = duration;
            impulseSource.GenerateImpulse(direction.normalized * intensity);
        }

        private ShakeSettings GetSettingsForPreset(CameraShakePreset preset)
        {
            return preset switch
            {
                CameraShakePreset.Light => lightShake,
                CameraShakePreset.Medium => mediumShake,
                CameraShakePreset.Heavy => heavyShake,
                CameraShakePreset.Death => deathShake,
                CameraShakePreset.Dash => dashShake,
                CameraShakePreset.Land => landShake,
                _ => lightShake
            };
        }

        private void OnValidate()
        {
            if (impulseSource == null)
                impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        [System.Serializable]
        public struct ShakeSettings
        {
            public float intensity;
            public float duration;

            public ShakeSettings(float intensity, float duration)
            {
                this.intensity = intensity;
                this.duration = duration;
            }
        }
    }
}