using UnityEngine;

namespace Runner.Inventory.Abilities
{
    [CreateAssetMenu(fileName = "CoinStreakAbility", menuName = "Runner/Abilities/Coin Streak")]
    public class CoinStreakAbility : KatanaAbility
    {
        [Header("Settings")]
        [Tooltip("Maximum coin streak multiplier")]
        [SerializeField] private int maxStreakMultiplier = 5;

        [Tooltip("Time window to collect next coin before streak resets (seconds)")]
        [SerializeField] private float streakWindow = 2f;

        public int MaxStreakMultiplier => maxStreakMultiplier;
        public float StreakWindow => streakWindow;

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
