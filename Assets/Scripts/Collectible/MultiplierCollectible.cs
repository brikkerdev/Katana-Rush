using Runner.Core;

namespace Runner.Collectibles
{
    public class MultiplierCollectible : Collectible
    {
        public override CollectibleType Type => CollectibleType.Multiplier;

        protected override void ApplyCollectEffect()
        {
            Game.Instance?.ActivateMultiplier(Value, EffectDuration);
        }

        protected override void PlayCollectSound()
        {
            Game.Instance?.Sound?.PlayPowerupCollect();
        }
    }
}
