using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "GhostStepAbility", menuName = "Runner/Abilities/Ghost Step")]
    public class GhostStepAbility : KatanaAbility
    {
        [Header("Settings")]
        [Tooltip("Invincibility duration after landing from a jump (seconds)")]
        [SerializeField] private float invincibilityOnLand = 0.3f;

        public float InvincibilityOnLand => invincibilityOnLand;

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
