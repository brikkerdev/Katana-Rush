using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "MagnetAuraAbility", menuName = "Runner/Abilities/Magnet Aura")]
    public class MagnetAuraAbility : KatanaAbility
    {
        [Header("Settings")]
        [SerializeField] private float magnetRadius = 5f;

        public float MagnetRadius => magnetRadius;

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