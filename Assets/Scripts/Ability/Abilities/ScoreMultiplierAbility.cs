using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "ScoreMultiplierAbility", menuName = "Runner/Abilities/Score Multiplier")]
    public class ScoreMultiplierAbility : KatanaAbility
    {
        [Header("Settings")]
        [SerializeField] private float scoreMultiplier = 1.5f;

        public float ScoreMultiplier => scoreMultiplier;

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
