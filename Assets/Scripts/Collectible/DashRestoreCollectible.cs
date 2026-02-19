using Runner.Core;

namespace Runner.Collectibles
{
    public class DashRestoreCollectible : Collectible
    {
        public override CollectibleType Type => CollectibleType.DashRestore;

        protected override void ApplyCollectEffect()
        {
            Game.Instance?.Player?.Controller?.RestoreDashes();
        }

        protected override void PlayCollectSound()
        {
            Game.Instance?.Sound?.PlayPowerupCollect();
        }
    }
}
