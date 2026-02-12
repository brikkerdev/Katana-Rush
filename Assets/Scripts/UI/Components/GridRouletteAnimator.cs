using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Runner.Core;
using Runner.Inventory;

namespace Runner.UI
{
    public class GridRouletteAnimator : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float totalDuration = 5.5f;
        [SerializeField] private float initialInterval = 0.05f;
        [SerializeField] private float finalInterval = 0.3f;
        [SerializeField] private int minCycles = 3;
        [SerializeField] private float slowdownThreshold = 0.7f;

        private List<KatanaSlot> slots = new List<KatanaSlot>();
        private Katana targetKatana;
        private bool isRolling;
        private Coroutine rollCoroutine;
        private bool hasPlayedSlow;

        public bool IsRolling => isRolling;

        public event Action<Katana> OnRouletteComplete;

        public void StartRoulette(List<KatanaSlot> gridSlots, Katana result)
        {
            if (isRolling) return;
            if (gridSlots == null || gridSlots.Count == 0) return;

            slots = new List<KatanaSlot>();
            foreach (var slot in gridSlots)
            {
                if (slot != null && !slot.IsEmpty)
                {
                    slots.Add(slot);
                }
            }

            if (slots.Count == 0) return;

            targetKatana = result;
            isRolling = true;
            hasPlayedSlow = false;

            foreach (var slot in slots)
            {
                slot.SetInteractable(false);
                slot.ClearHighlight();
            }

            rollCoroutine = StartCoroutine(RollRoutine());
        }

        private IEnumerator RollRoutine()
        {
            int targetIndex = -1;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Katana == targetKatana)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex == -1)
            {
                targetIndex = 0;
            }

            int totalSlots = slots.Count;
            int stepsToTarget = minCycles * totalSlots + targetIndex;

            int currentStep = 0;
            int currentIndex = 0;
            int previousIndex = -1;

            while (currentStep < stepsToTarget)
            {
                float progress = (float)currentStep / stepsToTarget;
                float easedProgress = EaseOutQuart(progress);
                float currentInterval = Mathf.Lerp(initialInterval, finalInterval, easedProgress);

                if (previousIndex >= 0 && previousIndex < slots.Count)
                {
                    slots[previousIndex].SetHighlighted(false);
                }

                previousIndex = currentIndex;
                currentIndex = currentStep % totalSlots;
                currentStep++;

                slots[currentIndex].SetHighlighted(true);

                float tickPitch = Mathf.Lerp(1.2f, 0.8f, easedProgress);
                Game.Instance?.Sound?.PlayRouletteTickAtPitch(tickPitch);

                if (progress > slowdownThreshold && !hasPlayedSlow)
                {
                    hasPlayedSlow = true;
                    Game.Instance?.Sound?.PlayRouletteSlow();
                }

                yield return new WaitForSecondsRealtime(currentInterval);
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (i != targetIndex)
                {
                    slots[i].SetHighlighted(false);
                }
            }

            slots[targetIndex].ShowWinEffect();

            if (targetKatana != null)
            {
                Game.Instance?.Sound?.PlayRouletteWinByRarity(targetKatana.Rarity);
            }

            yield return new WaitForSecondsRealtime(0.5f);

            isRolling = false;

            foreach (var slot in slots)
            {
                slot.SetInteractable(true);
            }

            OnRouletteComplete?.Invoke(targetKatana);
        }

        private float EaseOutQuart(float t)
        {
            return 1f - Mathf.Pow(1f - t, 4f);
        }

        public void Cancel()
        {
            if (rollCoroutine != null)
            {
                StopCoroutine(rollCoroutine);
                rollCoroutine = null;
            }

            isRolling = false;

            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    slot.ClearHighlight();
                    slot.SetInteractable(true);
                }
            }

            slots.Clear();
        }

        private void OnDestroy()
        {
            Cancel();
        }
    }
}