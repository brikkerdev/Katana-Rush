using UnityEngine;
using System.Collections.Generic;

namespace Runner.Enemy
{
    public class EnemyRagdoll : MonoBehaviour
    {
        [Header("Ragdoll Parts")]
        [SerializeField] private Transform ragdollRoot;
        [SerializeField] private Transform headBone;
        [SerializeField] private Rigidbody headRigidbody;
        [SerializeField] private Rigidbody hipsRigidbody;

        [Header("Head Separation")]
        [SerializeField] private bool separateHeadOnDeath = true;
        [SerializeField] private float headForce = 15f;
        [SerializeField] private float headTorque = 10f;
        [SerializeField] private float headUpwardForce = 8f;

        [Header("Body Physics")]
        [SerializeField] private float bodyForce = 5f;
        [SerializeField] private float bodyUpwardForce = 3f;

        [Header("Performance")]
        [SerializeField] private float ragdollDuration = 3f;
        [SerializeField] private float simplifyDelay = 1f;
        [SerializeField] private bool disableAfterSettle = true;

        private Rigidbody[] allRigidbodies;
        private Collider[] allColliders;
        private CharacterJoint[] allJoints;

        private bool isRagdollActive;
        private float ragdollTimer;
        private float settleTimer;
        private bool isSettled;
        private Vector3 lastVelocity;

        private Transform originalHeadParent;
        private Vector3 originalHeadLocalPos;
        private Quaternion originalHeadLocalRot;
        private bool headWasSeparated;

        private Dictionary<Transform, TransformData> originalTransforms;

        private struct TransformData
        {
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public Transform parent;
        }

        public bool IsRagdollActive => isRagdollActive;

        private void Awake()
        {
            CacheComponents();
            StoreOriginalState();
            DisableRagdoll();
        }

        private void CacheComponents()
        {
            if (ragdollRoot == null)
            {
                ragdollRoot = transform;
            }

            allRigidbodies = ragdollRoot.GetComponentsInChildren<Rigidbody>(true);
            allColliders = ragdollRoot.GetComponentsInChildren<Collider>(true);
            allJoints = ragdollRoot.GetComponentsInChildren<CharacterJoint>(true);
        }

        private void StoreOriginalState()
        {
            originalTransforms = new Dictionary<Transform, TransformData>();

            Transform[] allTransforms = ragdollRoot.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allTransforms)
            {
                TransformData data = new TransformData
                {
                    localPosition = t.localPosition,
                    localRotation = t.localRotation,
                    localScale = t.localScale,
                    parent = t.parent
                };

                originalTransforms[t] = data;
            }

            if (headBone != null)
            {
                originalHeadParent = headBone.parent;
                originalHeadLocalPos = headBone.localPosition;
                originalHeadLocalRot = headBone.localRotation;
            }
        }

        private void Update()
        {
            if (!isRagdollActive) return;

            ragdollTimer -= Time.deltaTime;

            if (!isSettled)
            {
                CheckIfSettled();
            }

            if (ragdollTimer <= 0f && disableAfterSettle)
            {
                isRagdollActive = false;
            }
        }

        private void CheckIfSettled()
        {
            if (hipsRigidbody == null) return;

            float currentSpeed = hipsRigidbody.linearVelocity.magnitude;

            if (currentSpeed < 0.1f)
            {
                settleTimer += Time.deltaTime;

                if (settleTimer >= simplifyDelay)
                {
                    isSettled = true;
                    SimplifyRagdoll();
                }
            }
            else
            {
                settleTimer = 0f;
            }

            lastVelocity = hipsRigidbody.linearVelocity;
        }

        private void SimplifyRagdoll()
        {
            for (int i = 0; i < allRigidbodies.Length; i++)
            {
                if (allRigidbodies[i] == null) continue;
                if (allRigidbodies[i] == headRigidbody && headWasSeparated) continue;

                allRigidbodies[i].isKinematic = true;
                allRigidbodies[i].linearVelocity = Vector3.zero;
                allRigidbodies[i].angularVelocity = Vector3.zero;
            }
        }

        public void ActivateRagdoll(Vector3 hitDirection, float force = 1f)
        {
            if (isRagdollActive) return;

            isRagdollActive = true;
            ragdollTimer = ragdollDuration;
            settleTimer = 0f;
            isSettled = false;

            EnableRagdoll();

            if (separateHeadOnDeath)
            {
                SeparateHead(hitDirection, force);
            }

            ApplyBodyForce(hitDirection, force);
        }

        private void EnableRagdoll()
        {
            for (int i = 0; i < allRigidbodies.Length; i++)
            {
                if (allRigidbodies[i] == null) continue;

                allRigidbodies[i].isKinematic = false;
                allRigidbodies[i].linearVelocity = Vector3.zero;
                allRigidbodies[i].angularVelocity = Vector3.zero;
            }

            for (int i = 0; i < allColliders.Length; i++)
            {
                if (allColliders[i] == null) continue;
                allColliders[i].enabled = true;
            }
        }

        public void DisableRagdoll()
        {
            for (int i = 0; i < allRigidbodies.Length; i++)
            {
                if (allRigidbodies[i] == null) continue;

                allRigidbodies[i].isKinematic = true;
                allRigidbodies[i].linearVelocity = Vector3.zero;
                allRigidbodies[i].angularVelocity = Vector3.zero;
            }

            for (int i = 0; i < allColliders.Length; i++)
            {
                if (allColliders[i] == null) continue;
                allColliders[i].enabled = false;
            }

            isRagdollActive = false;
        }

        private void SeparateHead(Vector3 hitDirection, float force)
        {
            if (headBone == null || headRigidbody == null) return;

            CharacterJoint headJoint = headBone.GetComponent<CharacterJoint>();
            if (headJoint != null)
            {
                headJoint.connectedBody = null;
                Destroy(headJoint);
            }

            headBone.SetParent(null);
            headWasSeparated = true;

            Vector3 forceDirection = hitDirection + Vector3.up * headUpwardForce;
            forceDirection.Normalize();

            headRigidbody.isKinematic = false;
            headRigidbody.AddForce(forceDirection * headForce * force, ForceMode.Impulse);

            Vector3 randomTorque = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ) * headTorque;

            headRigidbody.AddTorque(randomTorque, ForceMode.Impulse);
        }

        private void ApplyBodyForce(Vector3 hitDirection, float force)
        {
            if (hipsRigidbody == null) return;

            Vector3 forceDirection = hitDirection + Vector3.up * bodyUpwardForce;
            forceDirection.Normalize();

            hipsRigidbody.AddForce(forceDirection * bodyForce * force, ForceMode.Impulse);

            for (int i = 0; i < allRigidbodies.Length; i++)
            {
                if (allRigidbodies[i] == null) continue;
                if (allRigidbodies[i] == hipsRigidbody) continue;
                if (allRigidbodies[i] == headRigidbody) continue;

                float randomForce = Random.Range(0.5f, 1.5f);
                allRigidbodies[i].AddForce(forceDirection * bodyForce * force * randomForce * 0.3f, ForceMode.Impulse);
            }
        }

        public void ResetRagdoll()
        {
            DisableRagdoll();

            if (headBone != null && headWasSeparated)
            {
                ReattachHead();
            }

            RestoreAllTransforms();

            ragdollTimer = ragdollDuration;
            settleTimer = 0f;
            isSettled = false;
            headWasSeparated = false;
            isRagdollActive = false;
        }

        private void ReattachHead()
        {
            if (headBone == null) return;

            headBone.SetParent(originalHeadParent);
            headBone.localPosition = originalHeadLocalPos;
            headBone.localRotation = originalHeadLocalRot;

            if (headRigidbody != null)
            {
                headRigidbody.isKinematic = true;
                headRigidbody.linearVelocity = Vector3.zero;
                headRigidbody.angularVelocity = Vector3.zero;
            }

            CharacterJoint existingJoint = headBone.GetComponent<CharacterJoint>();
            if (existingJoint == null && originalHeadParent != null)
            {
                Rigidbody parentRb = originalHeadParent.GetComponent<Rigidbody>();
                if (parentRb != null)
                {
                    CharacterJoint newJoint = headBone.gameObject.AddComponent<CharacterJoint>();
                    newJoint.connectedBody = parentRb;
                    newJoint.enableProjection = true;

                    SoftJointLimit lowTwist = newJoint.lowTwistLimit;
                    lowTwist.limit = -30f;
                    newJoint.lowTwistLimit = lowTwist;

                    SoftJointLimit highTwist = newJoint.highTwistLimit;
                    highTwist.limit = 30f;
                    newJoint.highTwistLimit = highTwist;

                    SoftJointLimit swing1 = newJoint.swing1Limit;
                    swing1.limit = 30f;
                    newJoint.swing1Limit = swing1;

                    SoftJointLimit swing2 = newJoint.swing2Limit;
                    swing2.limit = 30f;
                    newJoint.swing2Limit = swing2;
                }
            }
        }

        private void RestoreAllTransforms()
        {
            if (originalTransforms == null) return;

            foreach (var kvp in originalTransforms)
            {
                Transform t = kvp.Key;
                TransformData data = kvp.Value;

                if (t == null) continue;

                if (t.parent != data.parent && data.parent != null)
                {
                    t.SetParent(data.parent);
                }

                t.localPosition = data.localPosition;
                t.localRotation = data.localRotation;
                t.localScale = data.localScale;
            }
        }

        public void ForceReset()
        {
            StopAllCoroutines();
            ResetRagdoll();
        }

#if UNITY_EDITOR
        [ContextMenu("Store Current State")]
        private void EditorStoreState()
        {
            CacheComponents();
            StoreOriginalState();
            Debug.Log("Original state stored!");
        }

        [ContextMenu("Reset To Original")]
        private void EditorReset()
        {
            ResetRagdoll();
            Debug.Log("Reset complete!");
        }

        [ContextMenu("Test Ragdoll")]
        private void EditorTestRagdoll()
        {
            ActivateRagdoll(Vector3.forward, 1f);
        }

        [ContextMenu("Auto Find Bones")]
        private void EditorFindBones()
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allTransforms)
            {
                string name = t.name.ToLower();

                if (headBone == null && name.Contains("head"))
                {
                    headBone = t;
                    headRigidbody = t.GetComponent<Rigidbody>();
                    Debug.Log($"Found head: {t.name}");
                }

                if (hipsRigidbody == null && (name.Contains("hip") || name.Contains("pelvis")))
                {
                    hipsRigidbody = t.GetComponent<Rigidbody>();
                    Debug.Log($"Found hips: {t.name}");
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}