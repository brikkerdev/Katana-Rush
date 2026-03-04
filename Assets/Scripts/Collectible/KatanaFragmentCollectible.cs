using Runner.Core;
using UnityEngine;
using Runner.Save;

namespace Runner.Collectibles
{
    public class KatanaFragmentCollectible : Collectible
    {
        public override CollectibleType Type => CollectibleType.Fragment;

        protected override void ApplyCollectEffect()
        {
            string uuid = SpawnPointUuid;
            if (!string.IsNullOrEmpty(uuid))
            {
                SaveManager.CollectFragment(uuid);
            }
        }

        protected override void PlayCollectSound()
        {
            Game.Instance?.Sound?.PlayPowerupCollect();
        }
    }
}
