using UnityEngine;
using System;

namespace Runner.Enemy
{
    public enum EnemyType
    {
        Static,
        Patrol,
        Chase
    }

    public class Enemy : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private EnemyType enemyType = EnemyType.Static;
        [SerializeField] private float maxHealth = 1f;
        [SerializeField] private float collisionRadius = 0.5f;

        [Header("Visual")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Animator animator;

        [Header("Effects")]
        [SerializeField] private GameObject deathEffectPrefab;
        [SerializeField] private GameObject hitEffectPrefab;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private bool isDead;
        private float currentHealth;
        private Vector3 spawnPosition;

        public EnemyType Type => enemyType;
        public bool IsDead => isDead;
        public float CollisionRadius => collisionRadius;
        public Vector3 SpawnPosition => spawnPosition;

        public event Action<Enemy> OnDeath;
        public event Action<Enemy> OnHit;

        private void Awake()
        {
            currentHealth = maxHealth;

            if (visualRoot == null)
            {
                visualRoot = transform.childCount > 0 ? transform.GetChild(0).gameObject : gameObject;
            }
        }

        public void Setup(Vector3 position, Quaternion rotation)
        {
            spawnPosition = position;
            transform.position = position;
            transform.rotation = rotation;

            isDead = false;
            currentHealth = maxHealth;

            if (visualRoot != null)
            {
                visualRoot.SetActive(true);
            }

            gameObject.SetActive(true);

            if (showDebug)
            {
                Debug.Log($"[Enemy] Setup at {position}");
            }
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;

            if (showDebug)
            {
                Debug.Log($"[Enemy] Took {damage} damage, health: {currentHealth}/{maxHealth}");
            }

            OnHit?.Invoke(this);

            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Die()
        {
            if (isDead) return;

            isDead = true;

            if (showDebug)
            {
                Debug.Log($"[Enemy] Died at {transform.position}");
            }

            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            if (animator != null)
            {
                animator.SetTrigger("Die");
            }

            OnDeath?.Invoke(this);

            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }

            Invoke(nameof(Deactivate), 0.1f);
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }

        public void ResetEnemy()
        {
            CancelInvoke();

            isDead = false;
            currentHealth = maxHealth;

            if (visualRoot != null)
            {
                visualRoot.SetActive(true);
            }

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isDead ? Color.gray : Color.red;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);

            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1.5f,
                $"{enemyType}\nHP: {currentHealth}/{maxHealth}\n{(isDead ? "DEAD" : "ALIVE")}"
            );
        }
#endif
    }
}