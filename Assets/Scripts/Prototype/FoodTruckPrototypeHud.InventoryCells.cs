using UnityEngine;
using UnityEngine.UI;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private void PaintInventoryCells(int hoverAnchorCell, bool hoverValid)
        {
            int[] hoverCells = null;
            if (hoverAnchorCell >= 0 && model.HasPendingBlock)
            {
                model.TryGetPendingFootprintCellsPreview(hoverAnchorCell, false, out hoverCells);
            }

            int selectedBlockId = selectedMergeCell >= 0 ? model.GetCellBlockId(selectedMergeCell) : -1;
            bool hasPending = model.HasPendingBlock;
            int pendingSeed = 0;
            if (hasPending)
            {
                pendingSeed = !string.IsNullOrEmpty(model.PendingBlock.IngredientName)
                    ? model.PendingBlock.IngredientName.GetHashCode()
                    : model.PendingBlock.Label.GetHashCode();
            }

            Color pendingGhostBase = hasPending ? ColorFromSeed(pendingSeed) : Color.clear;

            if (hasPending)
            {
                UpdatePlacementRecommendations();
            }
            else
            {
                recommendedAnchorCount = 0;
                recommendedAnchorCells[0] = -1;
                recommendedAnchorCells[1] = -1;
                recommendationPulseTimer = 0f;
                recommendationPulseAnchorCell = -1;
                lastRecommendedPrimaryAnchor = -1;
            }

            for (int i = 0; i < inventoryCells.Count; i++)
            {
                int blockId = model.GetCellBlockId(i);
                bool isHoverCell = hoverCells != null && Contains(hoverCells, i);
                bool isRecommendedPrimary = false;
                bool isRecommendedAnchor = hasPending && IsRecommendedAnchorCell(i, out isRecommendedPrimary);
                bool isLockedRecommendation = isRecommendedPrimary
                    && recommendationAssistLocked
                    && recommendationAssistLockAnchorCell == i;

                Color color = blockId >= 0 ? ColorFromSeed(blockId) : new Color(0.18f, 0.20f, 0.24f, 1f);
                string label = blockId >= 0 ? model.GetBlockShortLabel(blockId) : (i + 1).ToString();

                if (selectedBlockId >= 0 && blockId == selectedBlockId)
                {
                    color = Color.Lerp(color, new Color(0.27f, 0.60f, 0.94f, 1f), 0.55f);
                }

                if (!isHoverCell && isRecommendedAnchor)
                {
                    Color recColor = isRecommendedPrimary
                        ? new Color(0.28f, 0.82f, 0.52f, 1f)
                        : new Color(0.30f, 0.66f, 0.92f, 1f);
                    color = Color.Lerp(color, recColor, blockId >= 0 ? 0.22f : 0.44f);

                    if (isLockedRecommendation)
                    {
                        color = Color.Lerp(color, new Color(0.74f, 0.98f, 0.78f, 1f), blockId >= 0 ? 0.26f : 0.54f);
                    }
                }

                if (isHoverCell)
                {
                    color = hoverValid
                        ? Color.Lerp(color, new Color(0.22f, 0.85f, 0.38f, 1f), 0.6f)
                        : Color.Lerp(color, new Color(0.92f, 0.24f, 0.24f, 1f), 0.6f);
                }

                if (pendingPlacementFeedbackTimer > 0f && pendingPlacementFeedbackCells != null && Contains(pendingPlacementFeedbackCells, i))
                {
                    float t = pendingPlacementFeedbackDuration > 0.001f
                        ? Mathf.Clamp01(pendingPlacementFeedbackTimer / pendingPlacementFeedbackDuration)
                        : 0f;
                    Color flash = pendingPlacementFeedbackSuccess
                        ? new Color(0.20f, 0.88f, 0.43f, 1f)
                        : new Color(0.95f, 0.28f, 0.24f, 1f);
                    color = Color.Lerp(color, flash, 0.35f + (0.45f * t));
                }

                ApplyKitchenModuleCellSprite(inventoryCells[i], blockId >= 0);
                inventoryCells[i].color = color;
                if (isHoverCell && blockId < 0 && !isRecommendedAnchor)
                {
                    inventoryCellLabels[i].text = string.Empty;
                }
                else if (blockId < 0 && isRecommendedAnchor)
                {
                    inventoryCellLabels[i].text = isLockedRecommendation
                        ? "LOCK"
                        : (isRecommendedPrimary ? "R1" : "R2");
                }
                else
                {
                    inventoryCellLabels[i].text = label;
                }

                if (i < inventoryRecommendationRings.Count && inventoryRecommendationRings[i] != null)
                {
                    Image recommendationRing = inventoryRecommendationRings[i];
                    RectTransform ringRect = recommendationRing.rectTransform;
                    ringRect.localScale = Vector3.one;

                    if (hasPending && isRecommendedAnchor)
                    {
                        bool isPulseCell = isRecommendedPrimary && recommendationPulseAnchorCell == i && recommendationPulseTimer > 0f;
                        float pulse01 = isPulseCell && recommendationPulseDuration > 0.001f
                            ? 1f - Mathf.Clamp01(recommendationPulseTimer / recommendationPulseDuration)
                            : 1f;
                        float wave = isPulseCell ? Mathf.Sin(pulse01 * Mathf.PI) : 0f;

                        float ringScale = isPulseCell
                            ? Mathf.Lerp(1.24f, 1.02f, pulse01)
                            : (isRecommendedPrimary ? 1.02f : 1f);

                        if (isLockedRecommendation)
                        {
                            float lockWave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 16f);
                            ringScale = Mathf.Max(ringScale, 1.10f + 0.08f * lockWave);
                        }

                        ringRect.localScale = Vector3.one * ringScale;

                        Color ringColor = isRecommendedPrimary
                            ? new Color(0.42f, 0.96f, 0.62f, isPulseCell ? Mathf.Lerp(0.78f, 0.24f, pulse01) : 0.48f)
                            : new Color(0.42f, 0.76f, 1f, 0.30f);
                        if (isPulseCell)
                        {
                            ringColor.a = Mathf.Clamp01(ringColor.a + wave * 0.08f);
                        }

                        if (isLockedRecommendation)
                        {
                            float lockWave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 16f);
                            ringColor = Color.Lerp(ringColor, new Color(0.86f, 1.0f, 0.88f, 0.96f), 0.64f);
                            ringColor.a = Mathf.Clamp01(Mathf.Max(ringColor.a, 0.76f + 0.18f * lockWave));
                        }

                        recommendationRing.color = ringColor;
                    }
                    else
                    {
                        recommendationRing.color = Color.clear;
                    }
                }

                if (i >= inventoryGhostCells.Count || inventoryGhostCells[i] == null)
                {
                    continue;
                }

                Image ghost = inventoryGhostCells[i];
                if (isHoverCell && hasPending)
                {
                    Color accent = hoverValid ? PendingGhostValidColor : PendingGhostInvalidColor;
                    Color ghostColor = Color.Lerp(pendingGhostBase, accent, hoverValid ? 0.26f : 0.58f);
                    ghostColor.a = hoverValid ? 0.84f : 0.78f;
                    ghost.color = ghostColor;
                    continue;
                }

                if (hasPending && isRecommendedAnchor)
                {
                    Color recGhost = isRecommendedPrimary
                        ? new Color(0.36f, 0.92f, 0.58f, 0.34f)
                        : new Color(0.36f, 0.72f, 0.98f, 0.26f);

                    if (isLockedRecommendation)
                    {
                        float lockWave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 16f);
                        recGhost = Color.Lerp(recGhost, new Color(0.82f, 1f, 0.86f, 0.50f), 0.66f);
                        recGhost.a = Mathf.Clamp01(0.44f + 0.24f * lockWave);
                    }

                    ghost.color = recGhost;
                    continue;
                }

                if (pendingPlacementFeedbackTimer > 0f && pendingPlacementFeedbackCells != null && Contains(pendingPlacementFeedbackCells, i))
                {
                    float t = pendingPlacementFeedbackDuration > 0.001f
                        ? Mathf.Clamp01(pendingPlacementFeedbackTimer / pendingPlacementFeedbackDuration)
                        : 0f;
                    Color flash = pendingPlacementFeedbackSuccess ? PendingGhostValidColor : PendingGhostInvalidColor;
                    flash.a = 0.18f + (0.36f * t);
                    ghost.color = flash;
                    continue;
                }

                ghost.color = Color.clear;
            }
        }

        private static bool Contains(int[] values, int target)
        {
            if (values == null)
            {
                return false;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static Color ColorFromSeed(int seed)
        {
            int positive = seed & int.MaxValue;
            float hue = (positive % 360) / 360f;
            Color color = Color.HSVToRGB(hue, 0.56f, 0.90f);
            color.a = 1f;
            return color;
        }
    }
}
