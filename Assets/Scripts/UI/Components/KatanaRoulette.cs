using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Runner.Inventory;

namespace Runner.UI
{
    public class KatanaRoulette : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform slotsContainer;
        [SerializeField] private RectTransform selectionFrame;

        [Header("Settings")]
        [SerializeField] private float slotWidth = 120f;
        [SerializeField] private float rollDuration = 5.5f;
        [SerializeField] private int visibleSlots = 5;
        [SerializeField] private int totalRollSlots = 30;

        [Header("Animation")]
        [SerializeField] private Ease rollEase = Ease.OutQuint;
        [SerializeField] private float bounceStrength = 0.1f;

        private List<KatanaSlot> rouletteSlots = new List<KatanaSlot>();
        private bool isRolling;
        private Katana resultKatana;

        public bool IsRolling => isRolling;

        public event Action<Katana> OnRouletteComplete;

        public void StartRoulette(List<Katana> availableKatanas, Katana guaranteedResult, KatanaSlot slotPrefab)
        {
            if (isRolling) return;
            if (availableKatanas == null || availableKatanas.Count == 0) return;

            resultKatana = guaranteedResult;
            isRolling = true;

            ClearSlots();
            CreateRouletteSlots(availableKatanas, guaranteedResult, slotPrefab);
            StartCoroutine(RollRoutine());
        }

        private void ClearSlots()
        {
            foreach (var slot in rouletteSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            rouletteSlots.Clear();
        }

        private void CreateRouletteSlots(List<Katana> katanas, Katana result, KatanaSlot slotPrefab)
        {
            int resultIndex = totalRollSlots - (visibleSlots / 2) - 1;

            for (int i = 0; i < totalRollSlots; i++)
            {
                KatanaSlot slot = Instantiate(slotPrefab, slotsContainer);

                Katana katanaToShow;

                if (i == resultIndex)
                {
                    katanaToShow = result;
                }
                else
                {
                    katanaToShow = katanas[UnityEngine.Random.Range(0, katanas.Count)];
                }

                slot.Setup(katanaToShow, true, false);
                slot.SetSelected(false);

                RectTransform rt = slot.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(i * slotWidth, 0);

                rouletteSlots.Add(slot);
            }

            float startOffset = (visibleSlots / 2) * slotWidth;
            slotsContainer.anchoredPosition = new Vector2(startOffset, 0);
        }

        private IEnumerator RollRoutine()
        {
            float targetX = -((totalRollSlots - visibleSlots / 2 - 1) * slotWidth) + (visibleSlots / 2) * slotWidth;

            slotsContainer.DOAnchorPosX(targetX, rollDuration)
                .SetEase(rollEase)
                .SetUpdate(true);

            yield return new WaitForSecondsRealtime(rollDuration);

            slotsContainer.DOPunchAnchorPos(new Vector2(slotWidth * bounceStrength, 0), 0.3f, 2, 0.5f)
                .SetUpdate(true);

            yield return new WaitForSecondsRealtime(0.3f);

            int centerIndex = totalRollSlots - (visibleSlots / 2) - 1;
            if (centerIndex >= 0 && centerIndex < rouletteSlots.Count)
            {
                rouletteSlots[centerIndex].SetSelected(true);
            }

            yield return new WaitForSecondsRealtime(0.5f);

            isRolling = false;
            OnRouletteComplete?.Invoke(resultKatana);
        }

        public void Reset()
        {
            slotsContainer.DOKill();
            ClearSlots();
            slotsContainer.anchoredPosition = Vector2.zero;
            isRolling = false;
        }
    }
}