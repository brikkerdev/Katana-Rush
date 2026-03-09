using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "ExplosiveDashAbility", menuName = "Runner/Abilities/Explosive Dash")]
    public class ExplosiveDashAbility : KatanaAbility
    {
        [Header("Settings")]
        [Tooltip("Radius of the explosion at dash end")]
        [SerializeField] private float explosionRadius = 3f;

        [Tooltip("Damage dealt by the explosion")]
        [SerializeField] private float explosionDamage = 2f;

        public float ExplosionRadius => explosionRadius;
        public float ExplosionDamage => explosionDamage;

        public override void Activate()
        {
            AbilityManager.Instance?.RegisterAbility(this);
        }

        public override void Deactivate()
        {
            AbilityManager.Instance?.UnregisterAbility(this);
        }
    }
}
