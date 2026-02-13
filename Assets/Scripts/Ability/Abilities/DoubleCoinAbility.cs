using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "DoubleCoinAbility", menuName = "Runner/Abilities/Double Coins")]
    public class DoubleCoinAbility : KatanaAbility
    {
        [Header("Settings")]
        [SerializeField] private int coinMultiplier = 2;

        public int CoinMultiplier => coinMultiplier;

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