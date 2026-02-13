using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "TargetedDeflectAbility", menuName = "Runner/Abilities/Targeted Deflect")]
    public class TargetedDeflectAbility : KatanaAbility
    {
        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float targetChance = 0.5f;
        [SerializeField] private float deflectSpeed = 30f;
        [SerializeField] private float searchRadius = 30f;
        [SerializeField] private bool canDamageEnemies = true;

        public float TargetChance => targetChance;
        public float DeflectSpeed => deflectSpeed;
        public float SearchRadius => searchRadius;
        public bool CanDamageEnemies => canDamageEnemies;

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