using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "SpeedSurgeAbility", menuName = "Runner/Abilities/Speed Surge")]
    public class SpeedSurgeAbility : KatanaAbility
    {
        [Header("Settings")]
        [Tooltip("Temporary speed boost after killing an enemy")]
        [SerializeField] private float speedBoost = 5f;

        [Tooltip("Duration of the speed surge in seconds")]
        [SerializeField] private float surgeDuration = 3f;

        public float SpeedBoost => speedBoost;
        public float SurgeDuration => surgeDuration;

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
