using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private void RefreshDrawChoiceRow()
        {
            if (drawChoiceRow == null)
            {
                return;
            }

            if (model == null)
            {
                if (drawChoiceRow.gameObject.activeSelf)
                {
                    drawChoiceRow.gameObject.SetActive(false);
                }

                return;
            }

            bool hasDrawChoice = model.HasDrawChoice && model.DrawChoices != null && model.DrawChoices.Count > 0;
            if (drawChoiceRow.gameObject.activeSelf != hasDrawChoice)
            {
                drawChoiceRow.gameObject.SetActive(hasDrawChoice);
            }

            if (drawChoiceRowLayoutElement != null)
            {
                bool tallLayout = IsTallPortraitLayout() || Application.isMobilePlatform;
                bool compactChoiceLayout = IsGameplayFocusHudActive();
                float preferred = compactChoiceLayout ? (tallLayout ? 132f : 126f) : (tallLayout ? 214f : 196f);
                drawChoiceRowLayoutElement.preferredHeight = hasDrawChoice ? preferred : 0f;
                drawChoiceRowLayoutElement.minHeight = hasDrawChoice ? preferred - (compactChoiceLayout ? 14f : 18f) : 0f;
            }

            if (!hasDrawChoice)
            {
                safeChoicePulseTimer = 0f;
                safeChoiceSweepTimer = 0f;

                for (int i = 0; i < drawChoiceButtons.Count; i++)
                {
                    if (drawChoiceButtons[i] != null)
                    {
                        drawChoiceButtons[i].interactable = false;
                        drawChoiceButtons[i].transform.localScale = Vector3.one;
                    }

                    if (i < drawChoiceTexts.Count && drawChoiceTexts[i] != null)
                    {
                        drawChoiceTexts[i].text = "Draw options appear here";
                        drawChoiceTexts[i].color = new Color(0.72f, 0.78f, 0.86f, 0.9f);
                    }

                    SetDrawChoicePreviewPlaceholder(i);
                    SetDrawChoiceIngredientPlaceholder(i);
                    SetDrawChoiceStatBarPlaceholder(i);
                    SetDrawChoiceGuideState(i, false, false);
                }

                return;
            }

            IReadOnlyList<PendingBlockState> choices = model.DrawChoices;
            int safeIndex = FindSafeDrawChoiceIndex();
            bool showTutorialCue = model.Wave <= 3;

            for (int i = 0; i < drawChoiceButtons.Count; i++)
            {
                bool validChoice = i < choices.Count;
                Button cardButton = drawChoiceButtons[i];
                if (cardButton == null)
                {
                    continue;
                }

                cardButton.gameObject.SetActive(validChoice);
                if (!validChoice)
                {
                    cardButton.transform.localScale = Vector3.one;
                    SetDrawChoicePreviewPlaceholder(i);
                    SetDrawChoiceIngredientPlaceholder(i);
                    SetDrawChoiceStatBarPlaceholder(i);
                    SetDrawChoiceGuideState(i, false, false);
                    continue;
                }

                PendingBlockState choice = choices[i];
                string assistTag = model.GetDrawChoiceAssistTag(i);
                bool safeSlot = i == safeIndex;
                cardButton.interactable = true;

                Image cardImage = cardButton.GetComponent<Image>();
                if (cardImage != null)
                {
                    Color cardColor = GetDrawChoiceCardColor(choice, assistTag);
                    if (safeSlot && showTutorialCue)
                    {
                        float pulse = 0.5f + 0.5f * Mathf.Sin(safeChoicePulseTimer * 6.2f);
                        cardColor = Color.Lerp(cardColor, new Color(0.76f, 0.95f, 0.82f, 1f), 0.10f + pulse * 0.10f);
                    }

                    cardImage.color = cardColor;
                }

                if (i < drawChoiceTexts.Count && drawChoiceTexts[i] != null)
                {
                    drawChoiceTexts[i].text = BuildDrawChoiceCardText(choice, assistTag);
                    drawChoiceTexts[i].fontSize = IsGameplayFocusHudActive() ? 9 : 12;
                    drawChoiceTexts[i].color = Color.Lerp(
                        new Color(0.90f, 0.95f, 1f, 1f),
                        GetDrawChoiceRiskThemeColor(GetDrawChoiceRiskTag(choice)),
                        0.14f);
                }

                LayoutElement cardLayout = cardButton.GetComponent<LayoutElement>();
                if (cardLayout != null)
                {
                    bool compactChoiceLayout = IsGameplayFocusHudActive();
                    cardLayout.minHeight = compactChoiceLayout ? 104f : 174f;
                    cardLayout.preferredHeight = compactChoiceLayout ? 118f : 0f;
                    cardLayout.flexibleHeight = 1f;
                }

                RefreshDrawChoicePreview(i, choice);
                RefreshDrawChoiceIngredientVisual(i, choice);
                RefreshDrawChoiceStatBars(i, choice, assistTag);
                SetDrawChoiceGuideState(i, safeSlot, showTutorialCue);
            }
        }

        private void UpdateDrawChoiceTutorialPulse(float dt)
        {
            if (model == null || !model.HasDrawChoice)
            {
                safeChoicePulseTimer = 0f;
                safeChoiceSweepTimer = 0f;
                for (int i = 0; i < drawChoiceButtons.Count; i++)
                {
                    SetDrawChoiceGuideState(i, false, false);
                }

                return;
            }

            int safeIndex = FindSafeDrawChoiceIndex();
            if (safeIndex < 0)
            {
                for (int i = 0; i < drawChoiceButtons.Count; i++)
                {
                    SetDrawChoiceGuideState(i, false, false);
                }

                return;
            }

            safeChoicePulseTimer += Mathf.Max(0f, dt);
            safeChoiceSweepTimer += Mathf.Max(0f, dt);

            bool emphasize = model.Wave <= 3;
            IReadOnlyList<PendingBlockState> choices = model.DrawChoices;
            for (int i = 0; i < drawChoiceButtons.Count; i++)
            {
                bool isSafeSlot = i == safeIndex;
                SetDrawChoiceGuideState(i, isSafeSlot, emphasize);

                if (i >= choices.Count || i >= drawChoiceButtons.Count || drawChoiceButtons[i] == null)
                {
                    continue;
                }

                Image cardImage = drawChoiceButtons[i].GetComponent<Image>();
                if (cardImage == null)
                {
                    continue;
                }

                string assistTag = model.GetDrawChoiceAssistTag(i);
                Color cardColor = GetDrawChoiceCardColor(choices[i], assistTag);
                if (isSafeSlot && emphasize)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(safeChoicePulseTimer * 6.2f);
                    cardColor = Color.Lerp(cardColor, new Color(0.76f, 0.95f, 0.82f, 1f), 0.10f + pulse * 0.10f);
                }

                cardImage.color = cardColor;
            }
        }

        private void UpdateDrawChoicePickImpact(float dt)
        {
            float step = Mathf.Max(0f, dt);
            float duration = Mathf.Max(0.08f, drawChoicePickImpactDuration);

            for (int i = 0; i < drawChoicePickImpactTimers.Length && i < drawChoiceButtons.Count; i++)
            {
                Button cardButton = drawChoiceButtons[i];
                if (cardButton == null)
                {
                    continue;
                }

                float timer = drawChoicePickImpactTimers[i];
                if (timer <= 0f)
                {
                    cardButton.transform.localScale = Vector3.one;
                    continue;
                }

                timer = Mathf.Max(0f, timer - step);
                drawChoicePickImpactTimers[i] = timer;

                float progress = 1f - timer / duration;
                float pulse = Mathf.Sin(progress * Mathf.PI);
                float boost = drawChoicePickImpactSafeFlags[i] ? 0.09f : 0.07f;
                cardButton.transform.localScale = Vector3.one * (1f + boost * pulse);

                if (timer <= 0f)
                {
                    drawChoicePickImpactSafeFlags[i] = false;
                    cardButton.transform.localScale = Vector3.one;
                }
            }
        }

        private int FindSafeDrawChoiceIndex()
        {
            if (model == null || !model.HasDrawChoice)
            {
                return -1;
            }

            IReadOnlyList<PendingBlockState> choices = model.DrawChoices;
            for (int i = 0; i < choices.Count; i++)
            {
                if (string.Equals(model.GetDrawChoiceAssistTag(i), "SAFE", StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private void SetDrawChoiceGuideState(int slot, bool active, bool emphasize)
        {
            if (slot < 0)
            {
                return;
            }

            Image guideRing = slot < drawChoiceGuideRings.Count ? drawChoiceGuideRings[slot] : null;
            RectTransform guideBadgeRoot = slot < drawChoiceGuideBadgeRoots.Count ? drawChoiceGuideBadgeRoots[slot] : null;
            Text guidePointer = slot < drawChoiceGuidePointers.Count ? drawChoiceGuidePointers[slot] : null;
            RectTransform guideSweep = slot < drawChoiceGuideSweeps.Count ? drawChoiceGuideSweeps[slot] : null;
            float pulse = 0.5f + 0.5f * Mathf.Sin(safeChoicePulseTimer * 6.8f);

            if (guideRing != null)
            {
                if (!active)
                {
                    guideRing.color = Color.clear;
                }
                else
                {
                    float alpha = emphasize ? Mathf.Lerp(0.42f, 0.82f, pulse) : 0.58f;
                    guideRing.color = new Color(0.42f, 0.90f, 0.60f, alpha);
                }
            }

            if (guideBadgeRoot != null)
            {
                bool showBadge = active && emphasize;
                if (guideBadgeRoot.gameObject.activeSelf != showBadge)
                {
                    guideBadgeRoot.gameObject.SetActive(showBadge);
                }

                if (showBadge)
                {
                    Image badgeImage = guideBadgeRoot.GetComponent<Image>();
                    if (badgeImage != null)
                    {
                        badgeImage.color = Color.Lerp(
                            new Color(0.14f, 0.40f, 0.22f, 0.90f),
                            new Color(0.20f, 0.58f, 0.32f, 0.96f),
                            pulse);
                    }
                }
            }

            if (guidePointer != null)
            {
                bool showPointer = active;
                if (guidePointer.gameObject.activeSelf != showPointer)
                {
                    guidePointer.gameObject.SetActive(showPointer);
                }

                if (showPointer)
                {
                    guidePointer.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-2f, -5f, pulse));
                    guidePointer.color = Color.Lerp(
                        new Color(0.70f, 0.90f, 0.76f, 0.86f),
                        new Color(0.86f, 1f, 0.90f, 1f),
                        emphasize ? pulse : 0.5f);
                }
            }

            if (guideSweep != null)
            {
                bool showSweep = active && emphasize;
                if (guideSweep.gameObject.activeSelf != showSweep)
                {
                    guideSweep.gameObject.SetActive(showSweep);
                }

                if (showSweep)
                {
                    float cycle = Mathf.Repeat(safeChoiceSweepTimer * 0.65f, 1f);
                    guideSweep.anchoredPosition = new Vector2(Mathf.Lerp(-92f, 92f, cycle), 0f);
                    Image sweepImage = guideSweep.GetComponent<Image>();
                    if (sweepImage != null)
                    {
                        sweepImage.color = new Color(0.82f, 0.98f, 0.89f, Mathf.Lerp(0.08f, 0.24f, pulse));
                    }
                }
            }
        }

        private string BuildDrawChoiceCardText(PendingBlockState choice, string assistTag)
        {
            if (choice == null)
            {
                return string.Empty;
            }

            float dps = choice.Damage / Mathf.Max(0.45f, choice.CooldownSeconds);
            string valueBucket = GetDrawChoiceValueBucket(choice);
            string riskTag = GetDrawChoiceRiskTag(choice);
            string targetLabel = GetTargetTypeLabel(choice.TargetType);
            string shapeLabel = GetShapeLabel(choice.ShapeKey);
            string resolvedAssistTag = string.IsNullOrEmpty(assistTag) ? "BAL" : assistTag;
            string tacticalChips = BuildDrawChoiceTacticalChipLine(choice, resolvedAssistTag, valueBucket, riskTag);
            if (IsGameplayFocusHudActive())
            {
                return choice.IngredientName + " G" + choice.Grade + " " + resolvedAssistTag + "\n"
                    + shapeLabel + " / " + targetLabel + "\n"
                    + tacticalChips;
            }

            return choice.IngredientName + "  G" + choice.Grade + "  " + resolvedAssistTag + "\n"
                + shapeLabel + " / " + targetLabel + "\n"
                + "ATK " + choice.Damage.ToString("0.0")
                + "  CD " + choice.CooldownSeconds.ToString("0.0") + "s"
                + "  DPS " + dps.ToString("0.0") + "\n"
                + tacticalChips + "\n"
                + "Value " + valueBucket + "  Risk " + riskTag;
        }

        private string BuildDrawChoiceTacticalChipLine(
            PendingBlockState choice,
            string assistTag,
            string valueBucket,
            string riskTag)
        {
            int fitSlots = EstimateDrawChoiceFitSlots(choice);
            int heatCost = Mathf.CeilToInt(3f + choice.CellCount * 1.15f);
            string roleLabel = GetDrawChoiceRoleLabel(choice, assistTag, valueBucket, riskTag, fitSlots);
            return "Fit " + fitSlots + "  Heat +" + heatCost + "  Role " + roleLabel;
        }

        private int EstimateDrawChoiceFitSlots(PendingBlockState choice)
        {
            if (model == null || choice == null)
            {
                return 0;
            }

            HashSet<string> uniqueFootprints = new HashSet<string>();
            for (int rotation = 0; rotation < 4; rotation++)
            {
                Vector2Int[] offsets = GetDrawChoiceRotatedOffsets(choice, rotation);
                for (int anchor = 0; anchor < FoodTruckRunModel.InventoryCellCount; anchor++)
                {
                    if (!TryCollectDrawChoiceCells(offsets, anchor, out int[] cells))
                    {
                        continue;
                    }

                    Array.Sort(cells);
                    uniqueFootprints.Add(string.Join(",", cells));
                }
            }

            return uniqueFootprints.Count;
        }

        private bool TryCollectDrawChoiceCells(Vector2Int[] offsets, int anchorCellIndex, out int[] cells)
        {
            cells = null;
            if (offsets == null || offsets.Length == 0)
            {
                return false;
            }

            int anchorX = anchorCellIndex % FoodTruckRunModel.InventoryWidth;
            int anchorY = anchorCellIndex / FoodTruckRunModel.InventoryWidth;
            int[] resolved = new int[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2Int offset = offsets[i];
                int x = anchorX + offset.x;
                int y = anchorY + offset.y;
                if (x < 0 || x >= FoodTruckRunModel.InventoryWidth || y < 0 || y >= FoodTruckRunModel.InventoryHeight)
                {
                    return false;
                }

                int index = y * FoodTruckRunModel.InventoryWidth + x;
                if (model.GetCellBlockId(index) >= 0)
                {
                    return false;
                }

                resolved[i] = index;
            }

            cells = resolved;
            return true;
        }

        private static Vector2Int[] GetDrawChoiceRotatedOffsets(PendingBlockState choice, int quarterTurns)
        {
            if (choice == null || choice.CellOffsets == null || choice.CellOffsets.Length == 0)
            {
                return Array.Empty<Vector2Int>();
            }

            Vector2Int[] source = choice.CellOffsets;
            Vector2Int[] rotated = new Vector2Int[source.Length];
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int turns = ((quarterTurns % 4) + 4) % 4;

            for (int i = 0; i < source.Length; i++)
            {
                int x = source[i].x;
                int y = source[i].y;
                for (int t = 0; t < turns; t++)
                {
                    int nextX = y;
                    int nextY = -x;
                    x = nextX;
                    y = nextY;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                rotated[i] = new Vector2Int(x, y);
            }

            for (int i = 0; i < rotated.Length; i++)
            {
                rotated[i] = new Vector2Int(rotated[i].x - minX, rotated[i].y - minY);
            }

            return rotated;
        }

        private static string GetDrawChoiceRoleLabel(
            PendingBlockState choice,
            string assistTag,
            string valueBucket,
            string riskTag,
            int fitSlots)
        {
            if (choice == null)
            {
                return "Unknown";
            }

            if (fitSlots <= 0)
            {
                return "No fit";
            }

            if (string.Equals(assistTag, "SAFE", StringComparison.Ordinal) ||
                (choice.CellCount <= 1 && string.Equals(riskTag, "LOW", StringComparison.Ordinal)))
            {
                return "Safe fill";
            }

            switch (choice.TargetType)
            {
                case BlockTargetType.HighestHp:
                    return "Boss hit";
                case BlockTargetType.Farthest:
                    return "Backline";
                case BlockTargetType.RandomLane:
                    return "Swing";
            }

            if (choice.CellCount >= 4)
            {
                return "Board lock";
            }

            if (string.Equals(valueBucket, "HIGH", StringComparison.Ordinal) &&
                !string.Equals(riskTag, "HIGH", StringComparison.Ordinal))
            {
                return "DPS";
            }

            if (string.Equals(riskTag, "HIGH", StringComparison.Ordinal))
            {
                return "Greedy";
            }

            return choice.CellCount <= 2 ? "Lane patch" : "Lane cover";
        }

        private float EstimateDrawChoiceValue(PendingBlockState choice)
        {
            if (choice == null)
            {
                return 0f;
            }

            float dps = choice.Damage / Mathf.Max(0.45f, choice.CooldownSeconds);
            float statValue = choice.Grade * 0.60f + choice.CellCount * 0.35f;
            float targetBonus;

            switch (choice.TargetType)
            {
                case BlockTargetType.Farthest:
                    targetBonus = 0.28f;
                    break;
                case BlockTargetType.HighestHp:
                    targetBonus = 0.34f;
                    break;
                case BlockTargetType.RandomLane:
                    targetBonus = 0.06f;
                    break;
                default:
                    targetBonus = 0.16f;
                    break;
            }

            return dps + statValue + targetBonus;
        }

        private string GetDrawChoiceValueBucket(PendingBlockState choice)
        {
            float value = EstimateDrawChoiceValue(choice);
            if (value >= 5.8f)
            {
                return "HIGH";
            }

            if (value >= 4.2f)
            {
                return "MID";
            }

            return "LOW";
        }

        private string GetDrawChoiceRiskTag(PendingBlockState choice)
        {
            float riskScore = EstimateDrawChoiceRiskScore(choice);
            if (riskScore >= 6.4f)
            {
                return "HIGH";
            }

            if (riskScore >= 4.1f)
            {
                return "MID";
            }

            return "LOW";
        }

        private Color GetDrawChoiceRiskThemeColor(string riskTag)
        {
            switch (riskTag)
            {
                case "HIGH":
                    return new Color(0.96f, 0.41f, 0.30f, 1f);
                case "MID":
                    return new Color(0.96f, 0.64f, 0.26f, 1f);
                default:
                    return new Color(0.34f, 0.84f, 0.48f, 1f);
            }
        }

        private Color GetDrawChoiceRiskOutlineColor(string riskTag)
        {
            switch (riskTag)
            {
                case "HIGH":
                    return new Color(0.98f, 0.38f, 0.30f, 0.96f);
                case "MID":
                    return new Color(0.96f, 0.66f, 0.24f, 0.94f);
                default:
                    return new Color(0.24f, 0.82f, 0.46f, 0.92f);
            }
        }

        private float EstimateDrawChoiceRiskScore(PendingBlockState choice)
        {
            if (choice == null)
            {
                return 0f;
            }

            float score = 0f;
            score += Mathf.Clamp(choice.Damage - 4.4f, 0f, 9f) * 0.82f;
            score += Mathf.Clamp(choice.CooldownSeconds - 2.5f, 0f, 4f) * 1.04f;
            score += Mathf.Clamp(choice.CellCount - 2f, 0f, 3f) * 0.78f;
            score += Mathf.Max(0, choice.Grade - 1) * 0.60f;

            switch (choice.TargetType)
            {
                case BlockTargetType.Farthest:
                    score += 0.90f;
                    break;
                case BlockTargetType.HighestHp:
                    score += 1.15f;
                    break;
                case BlockTargetType.RandomLane:
                    score += 1.80f;
                    break;
                default:
                    score += 0.40f;
                    break;
            }

            return score;
        }

        private static string GetTargetTypeLabel(BlockTargetType targetType)
        {
            switch (targetType)
            {
                case BlockTargetType.Farthest:
                    return "Farthest";
                case BlockTargetType.HighestHp:
                    return "Highest HP";
                case BlockTargetType.RandomLane:
                    return "Random Lane";
                default:
                    return "Nearest";
            }
        }

        private static string GetShapeLabel(string shapeKey)
        {
            switch (shapeKey)
            {
                case "Dot":
                    return "Dot";
                case "LineH2":
                case "LineV2":
                    return "Line-2";
                case "L3":
                    return "L-3";
                case "T4":
                    return "T-4";
                case "Square4":
                    return "Square-4";
                default:
                    return string.IsNullOrEmpty(shapeKey) ? "Custom" : shapeKey;
            }
        }

        private Color GetDrawChoiceCardColor(PendingBlockState choice, string assistTag)
        {
            Color baseColor = new Color(0.18f, 0.24f, 0.32f, 0.98f);
            if (choice != null)
            {
                baseColor = Color.Lerp(ColorFromSeed(choice.ColorSeed), new Color(0.13f, 0.17f, 0.24f, 1f), 0.58f);
                baseColor = Color.Lerp(baseColor, GetDrawChoiceRiskThemeColor(GetDrawChoiceRiskTag(choice)), 0.16f);
            }

            return ApplyDrawAssistTint(baseColor, assistTag);
        }

        private static Color ApplyDrawAssistTint(Color baseColor, string assistTag)
        {
            Color tint;
            if (string.Equals(assistTag, "SAFE", StringComparison.Ordinal))
            {
                tint = new Color(0.30f, 0.82f, 0.52f, 1f);
                return Color.Lerp(baseColor, tint, 0.24f);
            }

            if (string.Equals(assistTag, "POWER", StringComparison.Ordinal))
            {
                tint = new Color(0.95f, 0.52f, 0.24f, 1f);
                return Color.Lerp(baseColor, tint, 0.20f);
            }

            tint = new Color(0.38f, 0.70f, 0.95f, 1f);
            return Color.Lerp(baseColor, tint, 0.15f);
        }
    }
}
