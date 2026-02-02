#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Runner.Enemy
{
    public class RagdollSetupHelper : EditorWindow
    {
        private GameObject targetObject;
        private float totalMass = 20f;
        private float colliderRadius = 0.1f;
        private bool addJoints = true;
        private PhysicsMaterial physicMaterial;

        private static readonly string[] BONE_NAMES = new string[]
        {
            "hips", "pelvis", "spine", "chest", "neck", "head",
            "shoulder", "arm", "elbow", "forearm", "hand", "wrist",
            "thigh", "leg", "calf", "shin", "foot", "toe"
        };

        [MenuItem("Tools/Ragdoll Setup Helper")]
        public static void ShowWindow()
        {
            GetWindow<RagdollSetupHelper>("Ragdoll Setup");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Ragdoll Setup Helper", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);
            totalMass = EditorGUILayout.FloatField("Total Mass", totalMass);
            colliderRadius = EditorGUILayout.FloatField("Collider Radius", colliderRadius);
            addJoints = EditorGUILayout.Toggle("Add Character Joints", addJoints);
            physicMaterial = (PhysicsMaterial)EditorGUILayout.ObjectField("Physics Material", physicMaterial, typeof(PhysicsMaterial), false);

            EditorGUILayout.Space();

            if (targetObject == null)
            {
                EditorGUILayout.HelpBox("Assign a character with bones to setup ragdoll", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Auto Setup Ragdoll"))
            {
                SetupRagdoll();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Add Rigidbodies Only"))
            {
                AddRigidbodies();
            }

            if (GUILayout.Button("Add Colliders Only"))
            {
                AddColliders();
            }

            if (GUILayout.Button("Remove All Ragdoll Components"))
            {
                RemoveRagdollComponents();
            }
        }

        private void SetupRagdoll()
        {
            if (targetObject == null) return;

            Undo.RegisterCompleteObjectUndo(targetObject, "Setup Ragdoll");

            List<Transform> bones = FindBones();
            float massPerBone = totalMass / Mathf.Max(1, bones.Count);

            foreach (Transform bone in bones)
            {
                SetupBone(bone, massPerBone);
            }

            if (addJoints)
            {
                SetupJoints(bones);
            }

            Debug.Log($"Ragdoll setup complete! Added components to {bones.Count} bones.");
        }

        private List<Transform> FindBones()
        {
            List<Transform> bones = new List<Transform>();
            Transform[] allTransforms = targetObject.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allTransforms)
            {
                string name = t.name.ToLower();

                foreach (string boneName in BONE_NAMES)
                {
                    if (name.Contains(boneName))
                    {
                        bones.Add(t);
                        break;
                    }
                }
            }

            return bones;
        }

        private void SetupBone(Transform bone, float mass)
        {
            Rigidbody rb = bone.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = bone.gameObject.AddComponent<Rigidbody>();
            }

            rb.mass = mass;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            rb.isKinematic = true;

            CapsuleCollider col = bone.GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = bone.gameObject.AddComponent<CapsuleCollider>();
            }

            col.radius = colliderRadius;
            col.height = colliderRadius * 4f;
            col.enabled = false;

            if (physicMaterial != null)
            {
                col.material = physicMaterial;
            }
        }

        private void SetupJoints(List<Transform> bones)
        {
            foreach (Transform bone in bones)
            {
                if (bone.parent == null) continue;

                Rigidbody parentRb = bone.parent.GetComponent<Rigidbody>();
                if (parentRb == null) continue;

                CharacterJoint joint = bone.GetComponent<CharacterJoint>();
                if (joint == null)
                {
                    joint = bone.gameObject.AddComponent<CharacterJoint>();
                }

                joint.connectedBody = parentRb;
                joint.enableProjection = true;

                SoftJointLimit lowTwist = joint.lowTwistLimit;
                lowTwist.limit = -30f;
                joint.lowTwistLimit = lowTwist;

                SoftJointLimit highTwist = joint.highTwistLimit;
                highTwist.limit = 30f;
                joint.highTwistLimit = highTwist;

                SoftJointLimit swing1 = joint.swing1Limit;
                swing1.limit = 30f;
                joint.swing1Limit = swing1;

                SoftJointLimit swing2 = joint.swing2Limit;
                swing2.limit = 30f;
                joint.swing2Limit = swing2;
            }
        }

        private void AddRigidbodies()
        {
            if (targetObject == null) return;

            Undo.RegisterCompleteObjectUndo(targetObject, "Add Rigidbodies");

            List<Transform> bones = FindBones();
            float massPerBone = totalMass / Mathf.Max(1, bones.Count);

            foreach (Transform bone in bones)
            {
                Rigidbody rb = bone.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = bone.gameObject.AddComponent<Rigidbody>();
                }

                rb.mass = massPerBone;
                rb.isKinematic = true;
            }
        }

        private void AddColliders()
        {
            if (targetObject == null) return;

            Undo.RegisterCompleteObjectUndo(targetObject, "Add Colliders");

            List<Transform> bones = FindBones();

            foreach (Transform bone in bones)
            {
                CapsuleCollider col = bone.GetComponent<CapsuleCollider>();
                if (col == null)
                {
                    col = bone.gameObject.AddComponent<CapsuleCollider>();
                }

                col.radius = colliderRadius;
                col.height = colliderRadius * 4f;
                col.enabled = false;
            }
        }

        private void RemoveRagdollComponents()
        {
            if (targetObject == null) return;

            Undo.RegisterCompleteObjectUndo(targetObject, "Remove Ragdoll");

            CharacterJoint[] joints = targetObject.GetComponentsInChildren<CharacterJoint>(true);
            foreach (var j in joints)
            {
                DestroyImmediate(j);
            }

            Rigidbody[] rigidbodies = targetObject.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rigidbodies)
            {
                DestroyImmediate(rb);
            }

            Collider[] colliders = targetObject.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if (col is CapsuleCollider || col is SphereCollider || col is BoxCollider)
                {
                    DestroyImmediate(col);
                }
            }

            Debug.Log("Ragdoll components removed!");
        }
    }
}
#endif