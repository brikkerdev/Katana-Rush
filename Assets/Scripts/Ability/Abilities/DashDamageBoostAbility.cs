using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "DashDamageBoostAbility", menuName = "Runner/Abilities/Dash Damage Boost")]
    public class DashDamageBoostAbility : KatanaAbility
    {
        [Header("Settings")]
        [SerializeField] private float damageMultiplier = 2f;

        public float DamageMultiplier => damageMultiplier;

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
