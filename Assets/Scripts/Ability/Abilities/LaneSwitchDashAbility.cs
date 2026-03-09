using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "LaneSwitchDashAbility", menuName = "Runner/Abilities/Lane Switch Dash")]
    public class LaneSwitchDashAbility : KatanaAbility
    {
        [Header("Settings")]
        [Tooltip("Brief invincibility window when switching lanes (seconds)")]
        [SerializeField] private float invincibilityDuration = 0.2f;

        [Tooltip("Damage dealt to enemies passed through during lane switch")]
        [SerializeField] private float laneSwitchDamage = 0.5f;

        public float InvincibilityDuration => invincibilityDuration;
        public float LaneSwitchDamage => laneSwitchDamage;

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
