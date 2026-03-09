using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "SlowFallAbility", menuName = "Runner/Abilities/Slow Fall")]
    public class SlowFallAbility : KatanaAbility
    {
        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float gravityReduction = 0.5f;

        public float GravityReduction => gravityReduction;

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
