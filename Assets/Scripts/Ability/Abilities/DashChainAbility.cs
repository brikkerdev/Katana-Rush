using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "DashChainAbility", menuName = "Runner/Abilities/Dash Chain")]
    public class DashChainAbility : KatanaAbility
    {
        [Header("Settings")]
        [Tooltip("Window in seconds to chain consecutive dashes")]
        [SerializeField] private float chainWindow = 0.8f;

        [Tooltip("Speed multiplier bonus per chained dash (stacks additively)")]
        [SerializeField] private float chainSpeedBonus = 0.5f;

        [Tooltip("Maximum chain stacks")]
        [SerializeField] private int maxChainStacks = 3;

        public float ChainWindow => chainWindow;
        public float ChainSpeedBonus => chainSpeedBonus;
        public int MaxChainStacks => maxChainStacks;

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
