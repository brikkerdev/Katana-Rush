using UnityEngine;

namespace Runner.Enemy
{
    [RequireComponent(typeof(Rigidbody))]
    public class RagdollHead : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] private float drag = 0.5f;
        [SerializeField] private float angularDrag = 0.5f;

        [Header("Trail")]
        [SerializeField] private TrailRenderer bloodTrail;

        [Header("Effects")]
        [SerializeField] private ParticleSystem bloodSpray;
        [SerializeField] private AudioSource impactSound;

        private Rigidbody rb;
        private Collider col;
        private RagdollPartPool pool;
        private float lifetime;
        private float timer;
        private bool isActive;
        private bool hasHitGround;
        private int bounceCount;
        private const int MAX_BOUNCES = 3;

        private Vector3 originalScale;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            originalScale = transform.localScale;

            if (rb != null)
            {
                rb.linearDamping = drag;
                rb.angularDamping = angularDrag;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }

        public void Initialize(RagdollPartPool poolRef, float life)
        {
            pool = poolRef;
            lifetime = life;
        }

        public void Spawn(Vector3 position, Quaternion rotation, Vector3 force, Vector3 torque)
        {
            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = originalScale;

            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.AddForce(force, ForceMode.Impulse);
            rb.AddTorque(torque, ForceMode.Impulse);

            timer = lifetime;
            isActive = true;
            hasHitGround = false;
            bounceCount = 0;

            if (bloodTrail != null)
            {
                bloodTrail.Clear();
                bloodTrail.emitting = true;
            }

            if (bloodSpray != null)
            {
                bloodSpray.Play();
            }

            if (col != null)
            {
                col.enabled = true;
            }

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!isActive) return;

            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                Deactivate();
            }

            if (hasHitGround && rb.linearVelocity.magnitude < 0.1f)
            {
                rb.isKinematic = true;

                if (bloodTrail != null)
                {
                    bloodTrail.emitting = false;
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isActive) return;

            if (!hasHitGround)
            {
                hasHitGround = true;

                if (impactSound != null)
                {
                    impactSound.pitch = Random.Range(0.9f, 1.1f);
                    impactSound.Play();
                }
            }

            bounceCount++;

            if (bounceCount >= MAX_BOUNCES)
            {
                rb.linearVelocity *= 0.3f;
                rb.angularVelocity *= 0.3f;
            }
        }

        private void Deactivate()
        {
            isActive = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (bloodTrail != null)
            {
                bloodTrail.emitting = false;
                bloodTrail.Clear();
            }

            if (bloodSpray != null)
            {
                bloodSpray.Stop();
                bloodSpray.Clear();
            }

            if (col != null)
            {
                col.enabled = false;
            }

            if (pool != null)
            {
                pool.ReturnHead(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void ForceDeactivate()
        {
            Deactivate();
        }

        public void ResetHead()
        {
            isActive = false;
            hasHitGround = false;
            bounceCount = 0;
            timer = 0f;

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (bloodTrail != null)
            {
                bloodTrail.emitting = false;
                bloodTrail.Clear();
            }

            if (bloodSpray != null)
            {
                bloodSpray.Stop();
                bloodSpray.Clear();
            }

            transform.localScale = originalScale;
        }
    }
}