using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "ShieldAbility", menuName = "Runner/Abilities/Shield")]
    public class ShieldAbility : KatanaAbility
    {
        [Header("Settings")]
        [Tooltip("Number of hits absorbed before shield breaks")]
        [SerializeField] private int shieldHits = 1;

        [Tooltip("Cooldown in seconds before shield regenerates after breaking")]
        [SerializeField] private float shieldCooldown = 30f;

        public int ShieldHits => shieldHits;
        public float ShieldCooldown => shieldCooldown;

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
