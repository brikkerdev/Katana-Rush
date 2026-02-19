using Runner.Core;

namespace Runner.Collectibles
{
    public class MagnetCollectible : Collectible
    {
        public override CollectibleType Type => CollectibleType.Magnet;

        protected override void ApplyCollectEffect()
        {
            Game.Instance?.ActivateMagnet(EffectDuration);
        }

        protected override void PlayCollectSound()
        {
            Game.Instance?.Sound?.PlayPowerupCollect();
        }
    }
}
