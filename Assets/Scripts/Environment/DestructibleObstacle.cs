using UnityEngine;
using System;
using Runner.Core;

namespace Runner.Environment
{
    /// <summary>
    /// Represents a destructible obstacle that can be destroyed by player dash.
    /// Features optional physics-based destruction for realistic crash effects.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DestructibleObstacle : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int scoreReward = 50;
        [SerializeField] private bool destroyOnDash = true;
        
        [Header("Destruction Effects")]
        [SerializeField] private GameObject destructionVFX;
        [SerializeField] private AudioClip customDestructionSound;
        
        [Header("Physics Destruction")]
        [Tooltip("Enable this and add Rigidbody for physics-based destruction")]
        [SerializeField] private bool usePhysicsDestruction = false;
        [SerializeField] private float explosionForce = 500f;
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private float upwardForce = 100f;
        
        private bool isDestroyed;
        private Rigidbody rb;
        
        public event Action<int> OnObstacleDestroyed;
        
        public bool IsDestroyed => isDestroyed;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
        
        /// <summary>
        /// Called when player dashes into this obstacle.
        /// </summary>
        public void OnDashHit()
        {
            if (isDestroyed) return;
            
            if (destroyOnDash)
            {
                DestroyObstacle();
            }
        }
        
        /// <summary>
        /// Called when player dashes into this obstacle with velocity info.
        /// Enables physics-based destruction effects.
        /// </summary>
        /// <param name="playerVelocity">The velocity of the player at impact</param>
        public void OnDashHit(Vector3 playerVelocity)
        {
            if (isDestroyed) return;
            
            // Apply physics effects before destroying
            if (usePhysicsDestruction)
            {
                ApplyExplosionForce(playerVelocity);
            }
            
            if (destroyOnDash)
            {
                DestroyObstacle();
            }
        }
        
        private void DestroyObstacle()
        {
            if (isDestroyed) return;
            isDestroyed = true;
            
            // Play destruction effects
            PlayDestructionEffects();
            
            // Add score
            if (Game.Instance != null)
            {
                Game.Instance.AddScore(scoreReward);
            }
            
            // Notify listeners
            OnObstacleDestroyed?.Invoke(scoreReward);
            
            // Destroy the game object
            Destroy(gameObject);
        }
        
        private void PlayDestructionEffects()
        {
            // Spawn VFX if assigned
            if (destructionVFX != null)
            {
                Instantiate(destructionVFX, transform.position, Quaternion.identity);
            }
            
            // Play custom sound if assigned, otherwise use global obstacle destroy sound
            if (customDestructionSound != null)
            {
                if (Game.Instance?.Sound != null)
                {
                    Game.Instance.Sound.Play(customDestructionSound);
                }
            }
            else
            {
                Game.Instance?.Sound?.PlayObstacleDestroy(transform.position);
            }
        }
        
        /// <summary>
        /// Applies physics-based explosion force to create crash effects.
        /// This creates realistic debris flying apart when player dashes through obstacle.
        /// 
        /// PHYSICS CRASH IMPLEMENTATION GUIDE:
        /// ====================================
        /// 
        /// To enable physics-based destruction:
        /// 
        /// 1. Add a Rigidbody component to this obstacle in the Unity Inspector
        /// 2. Set Rigidbody properties:
        ///    - Mass: 1-5 (heavier objects need more force)
        ///    - Drag: 0.5 (air resistance)
        ///    - Angular Drag: 0.5 (rotation resistance)
        ///    - Use Gravity: false (initially, will enable on impact)
        ///    - Is Kinematic: true (initially, will disable on impact)
        /// 
        /// 3. For child objects with separate colliders:
        ///    - Add Rigidbody to each child piece
        ///    - Consider using Joint components (FixedJoint, HingeJoint)
        ///    - Break force can be set to make them separate on impact
        /// 
        /// 4. For best crash effects:
        ///    - Use multiple smaller colliders instead of one large box
        ///    - Add different materials with varying mass
        ///    - Consider using particle systems for debris
        /// 
        /// 5. Alternative physics approaches:
        ///    a) Pre-broken pieces: Have multiple smaller objects hidden, 
        ///       activate them on impact and hide the main object
        ///    b) Skinned mesh: Use mesh deformation with BlendShapes
        ///    c) Vertex displacement: Shader-based destruction effect
        /// 
        /// 6. Performance considerations:
        ///    - Limit physics objects to ~20 active at once
        ///    - Use physics layers to avoid unnecessary collisions
        ///    - Destroy physics objects after 3-5 seconds
        /// </summary>
        /// <param name="impactVelocity">Player velocity at moment of impact</param>
        public void ApplyExplosionForce(Vector3 impactVelocity)
        {
            if (rb == null || !usePhysicsDestruction) return;
            
            // Enable physics
            rb.isKinematic = false;
            rb.useGravity = true;
            
            // Calculate explosion direction (away from player)
            Vector3 directionFromPlayer = (transform.position - impactVelocity.normalized).normalized;
            
            // Apply explosion force from center
            rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardForce);
            
            // Also apply direct velocity from player impact for more dramatic effect
            rb.linearVelocity = impactVelocity * 1.5f;
            
            // Add rotation for more dynamic crash
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 10f;
            
            // Auto-destroy after 5 seconds to clean up
            Destroy(rb.gameObject, 5f);
        }
        
        /// <summary>
        /// Creates a simple physics push effect without full explosion.
        /// Use this for lighter obstacles or when you want a simpler effect.
        /// </summary>
        /// <param name="pushDirection">Direction to push the obstacle</param>
        /// <param name="force">Force magnitude</param>
        public void ApplyPushForce(Vector3 pushDirection, float force)
        {
            if (rb == null) return;
            
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(pushDirection.normalized * force, ForceMode.Impulse);
            
            Destroy(rb.gameObject, 3f);
        }
        
        /// <summary>
        /// Alternative method for batch-destroyed obstacles (like boxes).
        /// Spawns multiple smaller physics objects at the obstacle's position.
        /// </summary>
        /// <param name="pieces">Array of piece prefabs to spawn</param>
        /// <param name="playerVelocity">Player velocity for direction</param>
        public void SpawnDebrisPieces(GameObject[] pieces, Vector3 playerVelocity)
        {
            if (pieces == null || pieces.Length == 0) return;
            
            foreach (GameObject piece in pieces)
            {
                if (piece == null) continue;
                
                // Spawn piece at random offset from original position
                Vector3 offset = UnityEngine.Random.insideUnitSphere * 0.5f;
                offset.y = Mathf.Abs(offset.y); // Keep above ground
                
                GameObject debris = Instantiate(piece, transform.position + offset, Quaternion.identity);
                
                // Apply velocity
                Rigidbody pieceRb = debris.GetComponent<Rigidbody>();
                if (pieceRb != null)
                {
                    pieceRb.linearVelocity = playerVelocity * 0.5f;
                    pieceRb.angularVelocity = UnityEngine.Random.insideUnitSphere * 5f;
                    
                    // Auto-cleanup
                    Destroy(debris, 5f);
                }
            }
        }
    }
}
