using UnityEngine;
using System;
using System.Collections.Generic;

namespace Runner.Effects
{
    public enum VisualEffectType
    {
        Hit,
        LethalHit,
        Blood,
        Death,
        MuzzleFlash,
        Deflect,
        Block,
        Dash,
        Slash
    }

    [Serializable]
    public struct VisualEffectTypeEntry
    {
        public VisualEffectType type;
        public GameVisualEffect prefab;
        public int prewarm;
    }

    [Serializable]
    public struct VisualEffectPrefabPoolEntry
    {
        public GameVisualEffect prefab;
        public int prewarm;
    }

    public sealed class ParticleController : MonoBehaviour
    {
        public static ParticleController Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private int defaultPoolSize = 10;
        [SerializeField] private bool expandPoolIfNeeded = true;

        [Header("Default Typed Effects (optional)")]
        [SerializeField] private List<VisualEffectTypeEntry> typedEffects = new List<VisualEffectTypeEntry>();

        [Header("Prewarm Extra Prefabs (optional)")]
        [SerializeField] private List<VisualEffectPrefabPoolEntry> prefabPoolsToPrewarm = new List<VisualEffectPrefabPoolEntry>();

        private readonly Dictionary<GameVisualEffect, Queue<GameVisualEffect>> prefabPools = new();
        private readonly Dictionary<VisualEffectType, GameVisualEffect> typedPrefabs = new();
        private readonly List<GameVisualEffect> active = new();

        private Transform poolContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            poolContainer = new GameObject("VisualEffectPool").transform;
            poolContainer.SetParent(transform);

            BuildTypedMap();
            PrewarmTyped();
            PrewarmPrefabs();
        }

        private void BuildTypedMap()
        {
            typedPrefabs.Clear();
            for (int i = 0; i < typedEffects.Count; i++)
            {
                var e = typedEffects[i];
                if (e.prefab == null) continue;
                typedPrefabs[e.type] = e.prefab;
            }
        }

        private void PrewarmTyped()
        {
            for (int i = 0; i < typedEffects.Count; i++)
            {
                var e = typedEffects[i];
                if (e.prefab == null) continue;
                int size = e.prewarm > 0 ? e.prewarm : defaultPoolSize;
                CreatePool(e.prefab, size);
            }
        }

        private void PrewarmPrefabs()
        {
            for (int i = 0; i < prefabPoolsToPrewarm.Count; i++)
            {
                var e = prefabPoolsToPrewarm[i];
                if (e.prefab == null) continue;
                int size = e.prewarm > 0 ? e.prewarm : defaultPoolSize;
                CreatePool(e.prefab, size);
            }
        }

        private void CreatePool(GameVisualEffect prefab, int size)
        {
            if (prefab == null) return;
            if (!prefabPools.TryGetValue(prefab, out var q))
            {
                q = new Queue<GameVisualEffect>(size);
                prefabPools[prefab] = q;
            }

            for (int i = 0; i < size; i++)
                q.Enqueue(CreateInstance(prefab));
        }

        private GameVisualEffect CreateInstance(GameVisualEffect prefab)
        {
            var inst = Instantiate(prefab, poolContainer);
            inst.gameObject.SetActive(false);
            inst.Initialize(prefab);
            return inst;
        }

        private GameVisualEffect GetFromPool(GameVisualEffect prefab)
        {
            if (prefab == null) return null;

            if (!prefabPools.TryGetValue(prefab, out var q))
            {
                q = new Queue<GameVisualEffect>(defaultPoolSize);
                prefabPools[prefab] = q;
            }

            if (q.Count > 0) return q.Dequeue();
            if (!expandPoolIfNeeded) return null;

            return CreateInstance(prefab);
        }

        private void ReturnToPool(GameVisualEffect vfx)
        {
            if (vfx == null) return;

            var key = vfx.SourcePrefab != null ? vfx.SourcePrefab : null;
            vfx.Deactivate(poolContainer);

            if (key == null) return;

            if (!prefabPools.TryGetValue(key, out var q))
            {
                q = new Queue<GameVisualEffect>(defaultPoolSize);
                prefabPools[key] = q;
            }

            q.Enqueue(vfx);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var vfx = active[i];
                if (vfx == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                vfx.Tick(dt);

                if (!vfx.IsPlaying)
                {
                    active.RemoveAt(i);
                    ReturnToPool(vfx);
                }
            }
        }

        public GameVisualEffect Spawn(GameVisualEffect prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            var vfx = GetFromPool(prefab);
            if (vfx == null) return null;

            vfx.Spawn(position, rotation, parent);
            active.Add(vfx);
            return vfx;
        }

        public GameVisualEffect Spawn(GameVisualEffect prefab, Vector3 position, Vector3 direction, Transform parent = null)
        {
            Quaternion rot = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            return Spawn(prefab, position, rot, parent);
        }

        public GameVisualEffect Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;
            var vfxPrefab = prefab.GetComponent<GameVisualEffect>();
            if (vfxPrefab == null) return null;
            return Spawn(vfxPrefab, position, rotation, parent);
        }

        public GameVisualEffect Spawn(GameObject prefab, Vector3 position, Vector3 direction, Transform parent = null)
        {
            Quaternion rot = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            return Spawn(prefab, position, rot, parent);
        }

        public void StopEffect(GameVisualEffect vfx)
        {
            if (vfx == null) return;

            int idx = active.IndexOf(vfx);
            if (idx >= 0) active.RemoveAt(idx);

            ReturnToPool(vfx);
        }

        public GameVisualEffect Spawn(VisualEffectType type, Vector3 position, Vector3 direction, Transform parent = null)
        {
            if (!typedPrefabs.TryGetValue(type, out var prefab) || prefab == null) return null;
            return Spawn(prefab, position, direction, parent);
        }

        public GameVisualEffect SpawnHitEffect(Vector3 position, Vector3 hitDirection)
        {
            Vector3 dir = hitDirection.sqrMagnitude > 0.001f ? -hitDirection.normalized : Vector3.forward;
            return Spawn(VisualEffectType.Hit, position, dir);
        }

        public GameVisualEffect SpawnLethalHitEffect(Vector3 position, Vector3 hitDirection)
        {
            Vector3 dir = hitDirection.sqrMagnitude > 0.001f ? -hitDirection.normalized : Vector3.forward;
            return Spawn(VisualEffectType.LethalHit, position, dir);
        }

        public GameVisualEffect SpawnBloodEffect(Vector3 position, Vector3 hitDirection)
        {
            Vector3 dir = hitDirection.sqrMagnitude > 0.001f ? -hitDirection.normalized : Vector3.forward;
            return Spawn(VisualEffectType.Blood, position, dir);
        }

        public GameVisualEffect SpawnDeathEffect(Vector3 position, Vector3 hitDirection)
        {
            Vector3 dir = hitDirection.sqrMagnitude > 0.001f ? -hitDirection.normalized : Vector3.forward;
            return Spawn(VisualEffectType.Death, position, dir);
        }

        public GameVisualEffect SpawnDeflectEffect(Vector3 position, Vector3 deflectedDirection)
        {
            return Spawn(VisualEffectType.Deflect, position, deflectedDirection);
        }

        public GameVisualEffect SpawnMuzzleFlash(Transform firePoint, Vector3 fireDirection)
        {
            if (firePoint == null) return null;
            Quaternion rot = fireDirection.sqrMagnitude > 0.001f ? Quaternion.LookRotation(fireDirection) : firePoint.rotation;
            return Spawn(VisualEffectType.MuzzleFlash, firePoint.position, fireDirection, firePoint);
        }

        public GameVisualEffect SpawnBlockEffect(Vector3 position, Vector3 forward)
        {
            return Spawn(VisualEffectType.Block, position, forward);
        }

        public GameVisualEffect SpawnDashEffect(Vector3 position, Vector3 forward, Transform parent = null)
        {
            return Spawn(VisualEffectType.Dash, position, forward, parent);
        }

        public GameVisualEffect SpawnSlashEffect(Vector3 position, Vector3 forward)
        {
            return Spawn(VisualEffectType.Slash, position, forward);
        }

        public void ClearAllEffects()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var vfx = active[i];
                if (vfx == null) continue;
                ReturnToPool(vfx);
            }

            active.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}