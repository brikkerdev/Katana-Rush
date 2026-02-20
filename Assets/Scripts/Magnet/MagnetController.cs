using System;
using System.Collections;
using System.Collections.Generic;
using Runner.Collectibles;
using Runner.Player;
using UnityEngine;

namespace Magnet
{
    public class MagnetController: MonoBehaviour
    {
        [Header("Magnet Settings")]
        [SerializeField] private float magnetRadius;
        [SerializeField] private float magnetSpeed = 15f;
        [SerializeField] private float magnetAcceleration = 2f;

        private Player player;
        private CollectibleSpawner collectibleSpawner;

        private bool isMagnetActive = false;
        private bool isInitialized = false;

        private readonly HashSet<Collectible> animatingCollectibles = new HashSet<Collectible>();

        public void Initialize(Player player, CollectibleSpawner  collectibleSpawner)
        {
            this.player = player;
            this.collectibleSpawner = collectibleSpawner;
            
            isInitialized = true;
        }

        private void FixedUpdate()
        {
            if (!isInitialized) return;
            if (!isMagnetActive) return;
            
            foreach (Collectible collectible in collectibleSpawner.ActiveCollectibles)
            {
                if (collectible.Type != CollectibleType.Coin)
                {
                    continue;
                }
                
                float distance = Vector3.Distance(player.transform.position, collectible.transform.position);

                if (distance > magnetRadius)
                {
                    continue;
                }

                // Skip if already being animated or collected
                if (animatingCollectibles.Contains(collectible) || collectible.IsCollected)
                {
                    continue;
                }

                // Start magnet animation coroutine
                animatingCollectibles.Add(collectible);
                StartCoroutine(AnimateCoinPickup(collectible));
            }
        }

        public void ActivateMagnet()
        {
            isMagnetActive = true;
        }

        public void DeactivateMagnet()
        {
            isMagnetActive = false;
        }

        private IEnumerator AnimateCoinPickup(Collectible collectible)
        {
            float currentSpeed = magnetSpeed;

            Vector3 offset = new Vector3(0f, 0f, 5f);
            
            while (!collectible.IsCollected)
            {
                if (player == null)
                {
                    animatingCollectibles.Remove(collectible);
                    yield break;
                }
                Vector3 targetPosition = player.transform.position + offset;

                Vector3 direction = targetPosition - collectible.transform.position;
                float distanceToPlayer = direction.magnitude;

                // Check if close enough to collect
                if (distanceToPlayer < 0.5f)
                {
                    break;
                }

                // Accelerate towards player (like a real magnet)
                currentSpeed += magnetAcceleration * Time.deltaTime;

                // Move towards player with increasing speed
                collectible.transform.position += direction.normalized * (currentSpeed * Time.deltaTime);

                // Add some rotation for visual effect
                collectible.transform.Rotate(Vector3.up, 720f * Time.deltaTime);

                yield return null;
            }

            // Collect the coin
            if (!collectible.IsCollected)
            {
                collectible.Collect();
            }

            animatingCollectibles.Remove(collectible);
        }
    }
}