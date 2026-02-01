using UnityEngine;

namespace Runner.Enemy
{
    public class EnemyAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        // Parameters
        private static readonly int IsIdle = Animator.StringToHash("IsIdle");
        private static readonly int IsPatrolling = Animator.StringToHash("IsPatrolling");
        private static readonly int IsChasing = Animator.StringToHash("IsChasing");

        // Triggers
        private static readonly int FireTrigger = Animator.StringToHash("Fire");
        private static readonly int DieTrigger = Animator.StringToHash("Die");
        private static readonly int HitTrigger = Animator.StringToHash("Hit");

        private Enemy enemy;

        public void Initialize(Enemy enemyRef)
        {
            enemy = enemyRef;

            if (enemy != null)
            {
                enemy.OnDeath += OnEnemyDeath;
                enemy.OnHit += OnEnemyHit;
            }
        }

        private void Start()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }
        }

        public void SetIdle(bool idle)
        {
            animator?.SetBool(IsIdle, idle);
        }

        public void SetPatrolling(bool patrolling)
        {
            animator?.SetBool(IsPatrolling, patrolling);
        }

        public void SetChasing(bool chasing)
        {
            animator?.SetBool(IsChasing, chasing);
        }

        public void PlayFireAnimation()
        {
            animator?.SetTrigger(FireTrigger);
        }

        public void PlayDieAnimation()
        {
            animator?.SetTrigger(DieTrigger);
        }

        public void PlayHitAnimation()
        {
            animator?.SetTrigger(HitTrigger);
        }

        private void OnEnemyDeath(Enemy e)
        {
            PlayDieAnimation();
        }

        private void OnEnemyHit(Enemy e)
        {
            PlayHitAnimation();
        }

        public void ResetAnimator()
        {
            if (animator == null) return;

            animator.ResetTrigger(FireTrigger);
            animator.ResetTrigger(DieTrigger);
            animator.ResetTrigger(HitTrigger);

            animator.SetBool(IsIdle, true);
            animator.SetBool(IsPatrolling, false);
            animator.SetBool(IsChasing, false);
        }

        private void OnDestroy()
        {
            if (enemy != null)
            {
                enemy.OnDeath -= OnEnemyDeath;
                enemy.OnHit -= OnEnemyHit;
            }
        }

        private void OnValidate()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }
    }
}