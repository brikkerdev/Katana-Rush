using UnityEngine;
using DG.Tweening;

namespace Runner.LevelGeneration
{
    public class SegmentReveal : MonoBehaviour
    {
        [Header("Reveal Settings")]
        [SerializeField] private float revealDuration = 0.6f;
        [SerializeField] private float dropDistance = 8f;
        [SerializeField] private Ease revealEase = Ease.OutBack;
        [SerializeField] private float overshoot = 1.2f;

        [Header("Visual Root")]
        [SerializeField] private Transform visualRoot;

        [Header("Stagger")]
        [SerializeField] private bool staggerChildren = false;
        [SerializeField] private float staggerDelay = 0.05f;
        [SerializeField] private int maxStaggeredChildren = 10;

        private Vector3 targetLocalPosition;
        private Tween activeTween;
        private Sequence activeSequence;

        public void PlayReveal(float delay = 0f)
        {
            Kill();

            if (visualRoot == null)
            {
                visualRoot = transform.Find("Visuals");

                if (visualRoot == null)
                {
                    Debug.LogWarning($"[SegmentReveal] No visual root on {gameObject.name}, skipping reveal");
                    return;
                }
            }

            targetLocalPosition = visualRoot.localPosition;

            if (staggerChildren && visualRoot.childCount > 1)
            {
                PlayStaggeredReveal(delay);
            }
            else
            {
                PlaySimpleReveal(delay);
            }
        }

        private void PlaySimpleReveal(float delay)
        {
            Vector3 startPos = targetLocalPosition;
            startPos.y -= dropDistance;
            visualRoot.localPosition = startPos;

            activeTween = visualRoot
                .DOLocalMoveY(targetLocalPosition.y, revealDuration)
                .SetDelay(delay)
                .SetEase(revealEase, overshoot)
                .SetAutoKill(true)
                .SetLink(gameObject);
        }

        private void PlayStaggeredReveal(float delay)
        {
            activeSequence = DOTween.Sequence().SetLink(gameObject);

            Vector3 startPos = targetLocalPosition;
            startPos.y -= dropDistance;
            visualRoot.localPosition = startPos;

            activeSequence.AppendInterval(delay);
            activeSequence.Append(
                visualRoot.DOLocalMoveY(targetLocalPosition.y, revealDuration)
                    .SetEase(revealEase, overshoot)
            );

            int count = Mathf.Min(visualRoot.childCount, maxStaggeredChildren);
            for (int i = 0; i < count; i++)
            {
                Transform child = visualRoot.GetChild(i);
                Vector3 childTarget = child.localPosition;

                Vector3 childStart = childTarget;
                childStart.y -= dropDistance * 0.5f;
                child.localPosition = childStart;

                activeSequence.Insert(delay + staggerDelay * i,
                    child.DOLocalMoveY(childTarget.y, revealDuration * 0.8f)
                        .SetEase(revealEase, overshoot)
                );
            }
        }

        public void Kill()
        {
            activeTween?.Kill();
            activeSequence?.Kill();
            activeTween = null;
            activeSequence = null;
        }

        public void ResetReveal()
        {
            Kill();

            if (visualRoot != null)
            {
                visualRoot.localPosition = targetLocalPosition;
            }
        }

        private void OnDisable() => Kill();
        private void OnDestroy() => Kill();
    }
}