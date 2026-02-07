using UnityEngine;

namespace Runner.Effects
{
    public sealed class GameVisualEffect : MonoBehaviour
    {
        private ParticleSystem[] particleSystems;
        private AudioSource[] audioSources;
        private Transform cachedTransform;

        private float autoDeactivateTime;
        private float timer;
        private bool useAutoDeactivate;

        public GameVisualEffect SourcePrefab { get; private set; }
        public bool IsPlaying { get; private set; }

        public void Initialize(GameVisualEffect sourcePrefab)
        {
            SourcePrefab = sourcePrefab;
            cachedTransform = transform;
            CacheComponents();
            ConfigureForPooling();
            RecalculateAutoDeactivate();
        }

        private void CacheComponents()
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            audioSources = GetComponentsInChildren<AudioSource>(true);
        }

        private void ConfigureForPooling()
        {
            if (particleSystems == null) return;

            for (int i = 0; i < particleSystems.Length; i++)
            {
                var ps = particleSystems[i];
                if (!ps) continue;
                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.None;
            }
        }

        private void RecalculateAutoDeactivate()
        {
            float maxDuration = 0f;
            bool anyLooping = false;

            if (particleSystems != null)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    var ps = particleSystems[i];
                    if (!ps) continue;

                    var main = ps.main;
                    anyLooping |= main.loop;

                    float d = main.duration + GetMax(main.startDelay) + GetMax(main.startLifetime);
                    if (d > maxDuration) maxDuration = d;
                }
            }

            autoDeactivateTime = maxDuration + 0.35f;
            useAutoDeactivate = !anyLooping && maxDuration > 0.01f;
        }

        private bool NeedsRecache()
        {
            if (particleSystems == null) return true;
            for (int i = 0; i < particleSystems.Length; i++)
                if (!particleSystems[i]) return true;
            return false;
        }

        public void Spawn(Vector3 position, Quaternion rotation, Transform parent)
        {
            if (NeedsRecache())
            {
                CacheComponents();
                ConfigureForPooling();
                RecalculateAutoDeactivate();
            }

            cachedTransform.localScale = Vector3.one;

            if (parent != null)
            {
                cachedTransform.SetParent(parent, true);
                cachedTransform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                cachedTransform.SetParent(null, true);
                cachedTransform.SetPositionAndRotation(position, rotation);
            }

            gameObject.SetActive(true);
            IsPlaying = true;
            timer = 0f;

            if (particleSystems != null)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    var ps = particleSystems[i];
                    if (!ps) continue;
                    ps.Clear(true);
                    ps.Play(true);
                }
            }

            if (audioSources != null)
            {
                for (int i = 0; i < audioSources.Length; i++)
                {
                    var a = audioSources[i];
                    if (!a) continue;
                    if (a.clip != null) a.Play();
                }
            }
        }

        public void Tick(float dt)
        {
            if (!IsPlaying) return;

            if (useAutoDeactivate)
            {
                timer += dt;
                if (timer >= autoDeactivateTime)
                {
                    IsPlaying = false;
                    return;
                }
            }

            if (particleSystems == null || particleSystems.Length == 0)
            {
                IsPlaying = false;
                return;
            }

            for (int i = 0; i < particleSystems.Length; i++)
            {
                var ps = particleSystems[i];
                if (!ps) continue;
                if (ps.isPlaying || ps.particleCount > 0) return;
            }

            IsPlaying = false;
        }

        public void Stop()
        {
            if (particleSystems != null)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    var ps = particleSystems[i];
                    if (!ps) continue;
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            if (audioSources != null)
            {
                for (int i = 0; i < audioSources.Length; i++)
                {
                    var a = audioSources[i];
                    if (!a) continue;
                    a.Stop();
                }
            }

            IsPlaying = false;
        }

        public void Deactivate(Transform poolParent)
        {
            Stop();
            cachedTransform.SetParent(poolParent, false);
            cachedTransform.localPosition = Vector3.zero;
            cachedTransform.localRotation = Quaternion.identity;
            cachedTransform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        public void SetDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
                cachedTransform.rotation = Quaternion.LookRotation(direction);
        }

        private static float GetMax(ParticleSystem.MinMaxCurve c)
        {
            switch (c.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return c.constant;
                case ParticleSystemCurveMode.TwoConstants:
                    return c.constantMax;
                case ParticleSystemCurveMode.Curve:
                    return c.curveMultiplier * SampleMax(c.curve);
                case ParticleSystemCurveMode.TwoCurves:
                    return c.curveMultiplier * Mathf.Max(SampleMax(c.curveMin), SampleMax(c.curveMax));
                default:
                    return c.constantMax;
            }
        }

        private static float SampleMax(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0) return 0f;

            float max = float.MinValue;
            const int steps = 24;

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float v = curve.Evaluate(t);
                if (v > max) max = v;
            }

            return Mathf.Max(0f, max);
        }
    }
}