using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "ExtendedDashAbility", menuName = "Runner/Abilities/Extended Dash")]
    public class ExtendedDashAbility : KatanaAbility
    {
        [Header("Settings")]
        [SerializeField] private float invincibilityExtension = 0.15f;

        public float InvincibilityExtension => invincibilityExtension;

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