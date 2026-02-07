using Runner.Effects;
using UnityEngine;

public static class ParticleExtensions
{
    public static GameVisualEffect SpawnHitEffect(this Transform t, Vector3 hitDirection)
    {
        if (ParticleController.Instance == null || t == null) return null;
        return ParticleController.Instance.SpawnHitEffect(t.position, hitDirection);
    }

    public static GameVisualEffect SpawnDeathEffect(this Transform t, Vector3 hitDirection)
    {
        if (ParticleController.Instance == null || t == null) return null;
        return ParticleController.Instance.SpawnDeathEffect(t.position, hitDirection);
    }

    public static GameVisualEffect SpawnMuzzleFlash(this Transform firePoint, Vector3 fireDirection)
    {
        if (ParticleController.Instance == null) return null;
        return ParticleController.Instance.SpawnMuzzleFlash(firePoint, fireDirection);
    }
}