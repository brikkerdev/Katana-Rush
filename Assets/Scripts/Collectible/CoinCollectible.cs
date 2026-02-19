using UnityEngine;
using DG.Tweening;
using Runner.Core;
using Runner.Inventory;
using Runner.Save;

namespace Runner.Collectibles
{
    public class CoinCollectible : Collectible
    {
        public override CollectibleType Type => CollectibleType.Coin;

        private Tweener magnetTween;
        private bool isBeingAttracted;

        protected override void ApplyCollectEffect()
        {
            int coinAmount = Value;
            if (AbilityManager.Instance != null)
            {
                coinAmount *= AbilityManager.Instance.GetCoinMultiplier();
            }
            SaveManager.AddCoins(coinAmount);
            UI.UIManager.Instance?.NotifyCoinsCollected(coinAmount);
        }

        protected override void PlayCollectSound()
        {
            Game.Instance?.Sound?.PlayCoinCollect();
        }

        /// <summary>
        /// Attract this coin towards the player using DoTween (without auto-collecting)
        /// </summary>
        public void AttractToPlayer(Transform playerTransform, float attractionSpeed)
        {
            if (IsCollected || isBeingAttracted) return;

            // Kill any existing tween
            magnetTween?.Kill();

            isBeingAttracted = true;

            // Calculate distance to player
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            // Calculate duration based on distance and speed
            float duration = distance / attractionSpeed;
            duration = Mathf.Max(0.1f, duration);

            Vector3 endPos = playerTransform.position;
            
            // Create a slight upward arc
            float arcHeight = 1f;
            
            magnetTween = transform
                .DOMove(endPos, duration)
                .SetEase(Ease.Linear)
                .SetLink(gameObject)
                .OnUpdate(() =>
                {
                    // Add slight arc to the movement
                    float progress = 1f - (Vector3.Distance(transform.position, endPos) / distance);
                    float yOffset = Mathf.Sin(progress * Mathf.PI) * arcHeight;
                    Vector3 pos = transform.position;
                    pos.y = Mathf.Max(pos.y, endPos.y + yOffset);
                    transform.position = pos;
                })
                .OnComplete(() =>
                {
                    // Coin reached the target position - stop being attracted
                    // The coin will be collected via trigger when it touches the player
                    isBeingAttracted = false;
                });
        }

        /// <summary>
        /// Stop any ongoing magnet attraction
        /// </summary>
        public void StopAttraction()
        {
            magnetTween?.Kill();
            magnetTween = null;
            isBeingAttracted = false;
        }

        /// <summary>
        /// Check if this coin is currently being attracted by magnet
        /// </summary>
        public bool IsBeingAttracted => isBeingAttracted;

        public override void Collect()
        {
            StopAttraction();
            base.Collect();
        }

        private void OnDestroy()
        {
            magnetTween?.Kill();
        }
    }
}
