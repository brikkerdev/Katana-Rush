using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "EnemyKillRewardAbility", menuName = "Runner/Abilities/Enemy Kill Reward")]
    public class EnemyKillRewardAbility : KatanaAbility
    {
        [Header("Settings")]
        [SerializeField] private int coinsPerKill = 10;
        [SerializeField] private int scorePerKill = 50;

        public int CoinsPerKill => coinsPerKill;
        public int ScorePerKill => scorePerKill;

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