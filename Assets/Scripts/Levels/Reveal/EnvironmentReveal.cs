using UnityEngine;
using DG.Tweening;

namespace Runner.LevelGeneration
{
    public class EnvironmentReveal : MonoBehaviour
    {
        [Header("Reveal")]
        [SerializeField] private float revealDuration = 1.2f;
        [SerializeField] private float dropDistance = 15f;
        [SerializeField] private Ease revealEase = Ease.OutCubic;

        [Header("Target")]
        [SerializeField] private Transform animateTarget;

        [Header("Groups")]
        [SerializeField] private RevealGroup[] revealGroups;

        private Sequence sequence;
        private Vector3 targetPosition;

        [System.Serializable]
        public class RevealGroup
        {
            public Transform groupRoot;
            public float delay = 0f;
            public float duration = 0.8f;
            public float dropDistance = 10f;
            public Ease ease = Ease.OutCubic;
        }

        public void PlayReveal(float delay = 0f)
        {
            Kill();

            Transform target = animateTarget != null ? animateTarget : transform;
            targetPosition = target.position;

            if (revealGroups != null && revealGroups.Length > 0)
            {
                PlayGroupedReveal(target, delay);
            }
            else
            {
                PlayWholeReveal(target, delay);
            }
        }

        private void PlayWholeReveal(Transform target, float delay)
        {
            Vector3 startPos = targetPosition;
            startPos.y -= dropDistance;
            target.position = startPos;

            sequence = DOTween.Sequence().SetLink(gameObject);
            sequence.AppendInterval(delay);
            sequence.Append(
                target.DOMoveY(targetPosition.y, revealDuration)
                    .SetEase(revealEase)
            );
        }

        private void PlayGroupedReveal(Transform target, float delay)
        {
            sequence = DOTween.Sequence().SetLink(gameObject);

            Vector3 startPos = targetPosition;
            startPos.y -= dropDistance;
            target.position = startPos;

            sequence.AppendInterval(delay);
            sequence.Append(
                target.DOMoveY(targetPosition.y, revealDuration)
                    .SetEase(revealEase)
            );

            foreach (var group in revealGroups)
            {
                if (group.groupRoot == null) continue;

                Vector3 groupTarget = group.groupRoot.localPosition;
                Vector3 groupStart = groupTarget;
                groupStart.y -= group.dropDistance;
                group.groupRoot.localPosition = groupStart;

                sequence.Insert(delay + group.delay,
                    group.groupRoot.DOLocalMoveY(groupTarget.y, group.duration)
                        .SetEase(group.ease)
                );
            }
        }

        public void Kill()
        {
            sequence?.Kill();
            sequence = null;
        }

        private void OnDisable() => Kill();
        private void OnDestroy() => Kill();
    }
}