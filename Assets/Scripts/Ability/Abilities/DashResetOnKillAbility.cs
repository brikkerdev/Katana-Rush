using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "DashResetOnKillAbility", menuName = "Runner/Abilities/Dash Reset On Kill")]
    public class DashResetOnKillAbility : KatanaAbility
    {
        [Header("Settings")]
        [Tooltip("Number of dash charges restored per kill")]
        [SerializeField] private int dashesRestoredPerKill = 1;

        public int DashesRestoredPerKill => dashesRestoredPerKill;

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
