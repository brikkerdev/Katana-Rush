using UnityEngine;
using System.Collections.Generic;

namespace Runner.Enemy
{
    public class RagdollPartPool : MonoBehaviour
    {
        [Header("Head Prefab")]
        [SerializeField] private GameObject headPrefab;
        [SerializeField] private int poolSize = 20;

        [Header("Settings")]
        [SerializeField] private float headLifetime = 3f;

        private Queue<RagdollHead> headPool;
        private List<RagdollHead> activeHeads;
        private Transform poolContainer;

        private static RagdollPartPool instance;
        public static RagdollPartPool Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Initialize();
        }

        private void Initialize()
        {
            headPool = new Queue<RagdollHead>();
            activeHeads = new List<RagdollHead>();

            poolContainer = new GameObject("RagdollPartPool").transform;
            poolContainer.SetParent(transform);

            if (headPrefab != null)
            {
                for (int i = 0; i < poolSize; i++)
                {
                    GameObject headObj = Instantiate(headPrefab, poolContainer);
                    RagdollHead head = headObj.GetComponent<RagdollHead>();

                    if (head == null)
                    {
                        head = headObj.AddComponent<RagdollHead>();
                    }

                    head.Initialize(this, headLifetime);
                    headObj.SetActive(false);
                    headPool.Enqueue(head);
                }
            }
        }

        public RagdollHead SpawnHead(Vector3 position, Quaternion rotation, Vector3 force, Vector3 torque)
        {
            RagdollHead head = null;

            if (headPool.Count > 0)
            {
                head = headPool.Dequeue();
            }
            else if (activeHeads.Count > 0)
            {
                head = activeHeads[0];
                activeHeads.RemoveAt(0);
                head.ForceDeactivate();
            }

            if (head == null) return null;

            head.Spawn(position, rotation, force, torque);
            activeHeads.Add(head);

            return head;
        }

        public void ReturnHead(RagdollHead head)
        {
            if (head == null) return;

            activeHeads.Remove(head);
            head.ResetHead();
            head.gameObject.SetActive(false);
            headPool.Enqueue(head);
        }

        public void ReturnAllHeads()
        {
            for (int i = activeHeads.Count - 1; i >= 0; i--)
            {
                if (activeHeads[i] != null)
                {
                    activeHeads[i].ForceDeactivate();
                }
            }

            activeHeads.Clear();
        }

        public void ResetPool()
        {
            ReturnAllHeads();

            foreach (var head in headPool)
            {
                if (head != null)
                {
                    head.ResetHead();
                }
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}