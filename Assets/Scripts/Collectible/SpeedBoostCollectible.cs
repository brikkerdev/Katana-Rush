using Runner.Core;

namespace Runner.Collectibles
{
    public class SpeedBoostCollectible : Collectible
    {
        public override CollectibleType Type => CollectibleType.SpeedBoost;

        protected override void ApplyCollectEffect()
        {
            Game.Instance?.ActivateSpeedBoost(EffectDuration);
        }

        protected override void PlayCollectSound()
        {
            Game.Instance?.Sound?.PlayPowerupCollect();
        }
    }
}
