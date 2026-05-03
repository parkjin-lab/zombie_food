using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private void RefreshPendingHintText(int anchorCell = -1, bool? canPlace = null, bool autoMergeReady = false)
        {
            if (pendingHintText == null || model == null)
            {
                return;
            }

            if (model.EventPending)
            {
                pendingHintText.text = "Flow: Event active. Choose Option 1/2/3 first.";
                return;
            }

            if (model.HasDrawChoice)
            {
                pendingHintText.text = "Flow: Choose 1 of 3 blocks first [1/2/3]. Then drag, rotate, and place.";
                return;
            }

            if (!model.HasPendingBlock)
            {
                pendingHintText.text = "Flow: Press Draw [D] to roll 3 options, then choose one to place.";
                return;
            }

            UpdatePlacementRecommendations();
            string recHint = BuildPlacementRecommendationHint();

            if (anchorCell >= 0 && canPlace.HasValue)
            {
                bool isRecPrimary;
                bool isRecommended = IsRecommendedAnchorCell(anchorCell, out isRecPrimary);
                string recTag = isRecommended ? (isRecPrimary ? " [BEST]" : " [ALT]") : string.Empty;

                if (canPlace.Value)
                {
                    string readiness = autoMergeReady ? "AUTO-MERGE READY" : "READY";
                    pendingHintText.text = model.PendingBlock.Label +
                        " | Slot " + (anchorCell + 1) + " " + readiness + recTag +
                        " | " + recHint +
                        " | Rotate [Q/E] or ROT L/R | Drag/drop or tap another cell";
                }
                else
                {
                    string blockedReason = BuildPlacementBlockedHint();
                    pendingHintText.text = model.PendingBlock.Label +
                        " | Slot " + (anchorCell + 1) + " BLOCKED" + recTag +
                        (string.IsNullOrEmpty(blockedReason) ? string.Empty : " (" + blockedReason + ")") +
                        " | " + recHint +
                        " | Rotate [Q/E] or ROT L/R | Drag/drop or tap another cell";
                }

                return;
            }

            pendingHintText.text = model.PendingBlock.Label +
                " | Rot " + model.PendingRotationDegrees + "deg" +
                " | " + recHint +
                " | Rotate [Q/E] or ROT L/R | Drag or tap grid to place";
        }

        private string BuildPlacementBlockedHint()
        {
            return BuildPlacementBlockedHint(null, PlacementFailReason.None);
        }

        private string BuildPlacementBlockedHint(string fallbackReason)
        {
            return BuildPlacementBlockedHint(fallbackReason, PlacementFailReason.None);
        }

        private string BuildPlacementBlockedHint(string fallbackReason, PlacementFailReason fallbackFailReason)
        {
            string reason = NormalizePlacementBlockedHint(fallbackReason);
            if (string.IsNullOrEmpty(reason))
            {
                reason = BuildPlacementFailReasonHint(fallbackFailReason);
            }

            if (!string.IsNullOrEmpty(reason))
            {
                lastPlacementBlockedHint = reason;
                return reason;
            }

            if (model == null)
            {
                return lastPlacementBlockedHint;
            }

            reason = NormalizePlacementBlockedHint(model.LastPlacementFailReasonText);
            if (!string.IsNullOrEmpty(reason))
            {
                lastPlacementBlockedHint = reason;
                return reason;
            }

            reason = BuildPlacementFailReasonHint(model.LastPlacementFailReason);
            if (!string.IsNullOrEmpty(reason))
            {
                lastPlacementBlockedHint = reason;
                return reason;
            }

            return lastPlacementBlockedHint;
        }

        private static string NormalizePlacementBlockedHint(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        private static string BuildPlacementFailReasonHint(PlacementFailReason failReason)
        {
            switch (failReason)
            {
                case PlacementFailReason.OutOfBounds:
                    return "Block exceeds grid bounds.";
                case PlacementFailReason.Occupied:
                    return "That slot is already occupied.";
                case PlacementFailReason.InvalidAnchor:
                    return "Invalid anchor cell.";
                case PlacementFailReason.NoPendingBlock:
                    return "No pending block.";
                default:
                    return string.Empty;
            }
        }

        private void ShowPlacementBlockedCue(string reason)
        {
            string blockedReason = BuildPlacementBlockedHint(reason);
            string message = string.IsNullOrEmpty(blockedReason)
                ? "Placement blocked."
                : "Placement blocked: " + blockedReason;
            Color color = new Color(0.92f, 0.38f, 0.28f, 1f);
            TriggerPrototypeVfx(ResolvePlacementFailVfxSprite(blockedReason), color, 0.92f);
            ShowCueBanner(message, color);
            PlaySfx(sfxPlaceBlockedClip, 0.80f);
        }

        private void UpdatePlacementRecommendations()
        {
            recommendedAnchorCount = 0;
            recommendedAnchorCells[0] = -1;
            recommendedAnchorCells[1] = -1;
            recommendedAnchorScores[0] = float.MinValue;
            recommendedAnchorScores[1] = float.MinValue;

            if (model == null || !model.HasPendingBlock)
            {
                return;
            }

            for (int anchorCell = 0; anchorCell < FoodTruckRunModel.InventoryCellCount; anchorCell++)
            {
                if (!model.TryGetPendingFootprintCellsPreview(anchorCell, true, out int[] cells))
                {
                    continue;
                }

                float score = ScorePlacementAnchor(anchorCell, cells);
                TryPushPlacementRecommendation(anchorCell, score);
            }

            int primaryAnchor = recommendedAnchorCount > 0 ? recommendedAnchorCells[0] : -1;
            if (primaryAnchor != lastRecommendedPrimaryAnchor)
            {
                lastRecommendedPrimaryAnchor = primaryAnchor;
                if (primaryAnchor >= 0)
                {
                    recommendationPulseAnchorCell = primaryAnchor;
                    recommendationPulseTimer = Mathf.Max(0.06f, recommendationPulseDuration);
                }
                else
                {
                    recommendationPulseAnchorCell = -1;
                    recommendationPulseTimer = 0f;
                }
            }
        }

        private float ScorePlacementAnchor(int anchorCell, int[] cells)
        {
            if (model == null)
            {
                return float.MinValue;
            }

            bool[] laneSeen = new bool[3];
            float laneSum = 0f;
            float laneMax = 0f;
            int laneCount = 0;

            if (cells != null)
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    int cellIndex = cells[i];
                    int lane = Mathf.Clamp(cellIndex % FoodTruckRunModel.InventoryWidth, 0, 2);
                    if (laneSeen[lane])
                    {
                        continue;
                    }

                    laneSeen[lane] = true;
                    float pressure = model.GetLanePressure(lane);
                    laneSum += pressure;
                    laneMax = Mathf.Max(laneMax, pressure);
                    laneCount++;
                }
            }

            float laneAvg = laneCount > 0 ? laneSum / laneCount : 0f;
            int x = anchorCell % FoodTruckRunModel.InventoryWidth;
            int y = anchorCell / FoodTruckRunModel.InventoryWidth;
            int centerDistance = Mathf.Abs(x - 1) + Mathf.Abs(y - 1);
            float centerBonus = recommendationCenterBaseBonus - centerDistance * recommendationCenterDistancePenalty;
            float coverageBonus = laneCount * recommendationScoreWeightCoverage;

            return laneAvg * recommendationScoreWeightLaneAverage
                + laneMax * recommendationScoreWeightLaneMax
                + coverageBonus
                + centerBonus;
        }

        private void TryPushPlacementRecommendation(int anchorCell, float score)
        {
            if (score > recommendedAnchorScores[0])
            {
                recommendedAnchorScores[1] = recommendedAnchorScores[0];
                recommendedAnchorCells[1] = recommendedAnchorCells[0];
                recommendedAnchorScores[0] = score;
                recommendedAnchorCells[0] = anchorCell;
                recommendedAnchorCount = Mathf.Min(2, recommendedAnchorCount + 1);
                return;
            }

            if (score > recommendedAnchorScores[1])
            {
                recommendedAnchorScores[1] = score;
                recommendedAnchorCells[1] = anchorCell;
                if (recommendedAnchorCount < 2)
                {
                    recommendedAnchorCount = 2;
                }
            }
        }

        private string BuildPlacementRecommendationHint()
        {
            if (recommendedAnchorCount <= 0 || recommendedAnchorCells[0] < 0)
            {
                return "Recommend: no valid slot";
            }

            string primaryScore = recommendedAnchorScores[0] > float.MinValue * 0.5f
                ? recommendedAnchorScores[0].ToString("0.0")
                : "--";
            string primaryDetail = BuildPlacementRecommendationDetail(recommendedAnchorCells[0]);
            if (recommendedAnchorCount == 1 || recommendedAnchorCells[1] < 0)
            {
                return "Recommend R1: slot " + (recommendedAnchorCells[0] + 1) +
                    " s" + primaryScore +
                    (string.IsNullOrEmpty(primaryDetail) ? string.Empty : " (" + primaryDetail + ")");
            }

            string secondaryScore = recommendedAnchorScores[1] > float.MinValue * 0.5f
                ? recommendedAnchorScores[1].ToString("0.0")
                : "--";
            return "Recommend R1/R2: " + (recommendedAnchorCells[0] + 1) + "/" + (recommendedAnchorCells[1] + 1) +
                " s" + primaryScore + "/" + secondaryScore +
                (string.IsNullOrEmpty(primaryDetail) ? string.Empty : " | " + primaryDetail);
        }

        private string BuildPlacementRecommendationDetail(int anchorCell)
        {
            if (model == null || anchorCell < 0)
            {
                return string.Empty;
            }

            if (!model.TryGetPendingFootprintCellsPreview(anchorCell, true, out int[] cells) || cells == null || cells.Length == 0)
            {
                return string.Empty;
            }

            bool[] laneSeen = new bool[3];
            int laneCount = 0;
            int hottestLane = -1;
            float hottestPressure = -1f;

            for (int i = 0; i < cells.Length; i++)
            {
                int lane = Mathf.Clamp(cells[i] % FoodTruckRunModel.InventoryWidth, 0, 2);
                if (laneSeen[lane])
                {
                    continue;
                }

                laneSeen[lane] = true;
                float pressure = model.GetLanePressure(lane);
                if (pressure > hottestPressure)
                {
                    hottestPressure = pressure;
                    hottestLane = lane;
                }

                laneCount++;
            }

            if (hottestLane < 0)
            {
                return string.Empty;
            }

            string laneText = "lane " + (hottestLane + 1) + " p" + hottestPressure.ToString("0.0");
            if (laneCount > 1)
            {
                laneText += ", cover " + laneCount + " lanes";
            }

            return laneText;
        }

        private bool IsRecommendedAnchorCell(int cellIndex, out bool isPrimary)
        {
            isPrimary = false;
            if (recommendedAnchorCount <= 0)
            {
                return false;
            }

            if (recommendedAnchorCells[0] == cellIndex)
            {
                isPrimary = true;
                return true;
            }

            if (recommendedAnchorCount > 1 && recommendedAnchorCells[1] == cellIndex)
            {
                return true;
            }

            return false;
        }

        private string BuildRecommendationMiniLine()
        {
            if (model == null || !model.HasPendingBlock)
            {
                return string.Empty;
            }

            UpdatePlacementRecommendations();
            if (recommendedAnchorCount <= 0 || recommendedAnchorCells[0] < 0)
            {
                return "   Rec[none]";
            }

            string line = "   Rec[R1 " + (recommendedAnchorCells[0] + 1) + "@" + recommendedAnchorScores[0].ToString("0.0");
            if (recommendedAnchorCount > 1 && recommendedAnchorCells[1] >= 0)
            {
                line += " R2 " + (recommendedAnchorCells[1] + 1) + "@" + recommendedAnchorScores[1].ToString("0.0");
            }

            line += "]";
            return line;
        }

        private void TriggerPendingPlacementFeedback(bool success, int[] affectedCells)
        {
            ConfigurePlacementFeedbackPreset(success);
            pendingPlacementFeedbackSuccess = success;
            pendingPlacementFeedbackCells = affectedCells ?? Array.Empty<int>();
            float feedbackDuration = pendingPlacementFeedbackDurationRuntime > 0f
                ? pendingPlacementFeedbackDurationRuntime
                : pendingPlacementFeedbackDuration;
            pendingPlacementFeedbackTimer = Mathf.Max(0.05f, feedbackDuration);
            placementImpactSuccess = success;
            float impactDuration = placementImpactDurationRuntime > 0f
                ? placementImpactDurationRuntime
                : placementImpactDuration;
            placementImpactTimer = Mathf.Max(0.05f, impactDuration);

            TriggerPendingTokenPulse(success, true);

            if (pendingTokenBackgroundImage != null)
            {
                pendingTokenBackgroundImage.color = success
                    ? PendingTokenValidColor
                    : PendingTokenInvalidColor;
            }

            if (!success)
            {
                RectTransform shakeTarget = GetInventoryGridShakeTarget();
                if (shakeTarget != null)
                {
                    if (inventoryGridHolderBaseAnchoredPositionCached)
                    {
                        // Keep shake centered on the known base even on consecutive failed placements.
                        shakeTarget.anchoredPosition = inventoryGridHolderBaseAnchoredPosition;
                    }
                    else
                    {
                        inventoryGridHolderBaseAnchoredPosition = ResolveInventoryGridRestAnchoredPosition(shakeTarget);
                        inventoryGridHolderBaseAnchoredPositionCached = true;
                        shakeTarget.anchoredPosition = inventoryGridHolderBaseAnchoredPosition;
                    }
                }

                float failShakeDuration = placementFailShakeDurationRuntime > 0f
                    ? placementFailShakeDurationRuntime
                    : placementFailShakeDuration;
                placementFailShakeTimer = Mathf.Max(0.05f, failShakeDuration);
                placementFailShakeDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            }
            else
            {
                placementFailShakeTimer = 0f;
            }
        }

        private void ResetPendingPlacementAssist(bool clearHighlights)
        {
            pendingHoverAnchorCell = -1;
            pendingHoverValid = false;

            if (pendingTokenBackgroundImage != null && pendingPlacementFeedbackTimer <= 0f)
            {
                pendingTokenBackgroundImage.color = PendingTokenNeutralColor;
            }

            if (clearHighlights)
            {
                PaintInventoryCells(-1, false);
            }
        }

        private void UpdateGridHoverFromMouse(Vector2 mouse, bool snapTokenToCell = false, bool allowRecommendationSnap = false)
        {
            if (model == null)
            {
                return;
            }

            if (TryResolvePlacementAnchor(mouse, allowRecommendationSnap, out int anchorCell))
            {
                bool canPlaceDirect = model.TryGetPendingFootprintCellsPreview(
                    anchorCell,
                    true,
                    out _,
                    out PlacementFailReason previewFailReason,
                    out string previewFailText);
                bool canAutoMerge = !canPlaceDirect && model.CanAutoMergePendingAtCell(anchorCell);
                bool valid = canPlaceDirect || canAutoMerge;
                pendingHoverAnchorCell = anchorCell;
                pendingHoverValid = valid;
                if (valid)
                {
                    lastPlacementBlockedHint = string.Empty;
                }
                else
                {
                    BuildPlacementBlockedHint(previewFailText, previewFailReason);
                }

                PaintInventoryCells(anchorCell, valid);

                RefreshPendingHintText(anchorCell, valid, canAutoMerge);

                if (pendingTokenBackgroundImage != null)
                {
                    pendingTokenBackgroundImage.color = valid
                        ? PendingTokenValidColor
                        : PendingTokenInvalidColor;
                }

                if (snapTokenToCell && TryGetGridCellCenterInCanvas(anchorCell, out Vector2 cellCenter))
                {
                    float snapLerp = 0.55f;
                    if (allowRecommendationSnap && recommendedAnchorCount > 0 && anchorCell == recommendedAnchorCells[0])
                    {
                        snapLerp = 0.68f;
                    }

                    pendingTokenRect.anchoredPosition = Vector2.Lerp(pendingTokenRect.anchoredPosition, cellCenter, snapLerp);
                }

                return;
            }

            pendingHoverAnchorCell = -1;
            pendingHoverValid = false;

            if (pendingTokenBackgroundImage != null && pendingPlacementFeedbackTimer <= 0f)
            {
                pendingTokenBackgroundImage.color = PendingTokenNeutralColor;
            }

            RefreshPendingHintText();

            PaintInventoryCells(-1, false);
        }

        private bool TryResolvePlacementAnchor(Vector2 screenPoint, bool allowRecommendationSnap, out int anchorCell)
        {
            if (TryGetGridCellAtScreen(screenPoint, out anchorCell))
            {
                return true;
            }

            anchorCell = -1;
            if (!allowRecommendationSnap || model == null || !model.HasPendingBlock)
            {
                return false;
            }

            UpdatePlacementRecommendations();
            if (recommendedAnchorCount <= 0 || recommendedAnchorCells[0] < 0)
            {
                return false;
            }

            int primaryAnchor = recommendedAnchorCells[0];
            if (!TryGetGridCellCenterOnScreen(primaryAnchor, out Vector2 primaryScreen))
            {
                return false;
            }

            float snapRadius = Mathf.Max(28f, recommendationSnapRadiusPixels);
            if ((primaryScreen - screenPoint).sqrMagnitude <= snapRadius * snapRadius)
            {
                anchorCell = primaryAnchor;
                return true;
            }

            return false;
        }

        private bool TryGetGridCellCenterOnScreen(int cellIndex, out Vector2 screenPoint)
        {
            screenPoint = Vector2.zero;
            if (cellIndex < 0 || cellIndex >= inventoryCellRects.Count)
            {
                return false;
            }

            RectTransform cellRect = inventoryCellRects[cellIndex];
            if (cellRect == null)
            {
                return false;
            }

            Vector3 worldCenter = cellRect.TransformPoint(cellRect.rect.center);
            screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldCenter);
            return true;
        }

        private void SetPendingRecommendationAssistVisible(bool visible)
        {
            if (pendingRecommendationAssistText == null)
            {
                return;
            }

            if (pendingRecommendationAssistText.gameObject.activeSelf != visible)
            {
                pendingRecommendationAssistText.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                recommendationAssistWasLocked = false;
                recommendationAssistLocked = false;
                recommendationAssistLockAnchorCell = -1;
                recommendationAssistFlashTimer = 0f;
                pendingRecommendationAssistText.rectTransform.localScale = Vector3.one;
            }
        }

        private void UpdatePendingRecommendationAssist(Vector2 pointerScreen)
        {
            if (pendingRecommendationAssistText == null || model == null || !model.HasPendingBlock)
            {
                SetPendingRecommendationAssistVisible(false);
                return;
            }

            UpdatePlacementRecommendations();
            if (recommendedAnchorCount <= 0 || recommendedAnchorCells[0] < 0)
            {
                SetPendingRecommendationAssistVisible(false);
                return;
            }

            if (!TryGetGridCellCenterOnScreen(recommendedAnchorCells[0], out Vector2 targetScreen))
            {
                SetPendingRecommendationAssistVisible(false);
                return;
            }

            Vector2 delta = targetScreen - pointerScreen;
            float distance = delta.magnitude;
            float lockDistance = Mathf.Max(16f, recommendationAssistLockDistancePixels);
            bool isLocked = distance <= lockDistance;

            if (isLocked && !recommendationAssistWasLocked)
            {
                recommendationAssistFlashTimer = Mathf.Max(0.05f, recommendationAssistFlashDuration);
                if (recommendationLockHapticOnMobile
                    && Application.isMobilePlatform
                    && recommendationHapticCooldownTimer <= 0f)
                {
                    Handheld.Vibrate();
                    recommendationHapticCooldownTimer = Mathf.Max(0.05f, recommendationHapticCooldownSeconds);
                }
            }

            recommendationAssistWasLocked = isLocked;
            recommendationAssistLocked = isLocked;
            recommendationAssistLockAnchorCell = isLocked ? recommendedAnchorCells[0] : -1;
            pendingRecommendationAssistText.text = isLocked
                ? "R1 LOCK"
                : "R1 " + GetRecommendationDirectionToken(delta);
            pendingRecommendationAssistText.color = isLocked
                ? new Color(0.86f, 1.0f, 0.90f, 0.98f)
                : new Color(0.74f, 0.95f, 0.82f, 0.95f);
            SetPendingRecommendationAssistVisible(true);
        }

        private static string GetRecommendationDirectionToken(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                return delta.x >= 0f ? "->" : "<-";
            }

            return delta.y >= 0f ? "^" : "v";
        }

        private bool TryGetGridCellCenterInCanvas(int cellIndex, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;
            if (canvasRect == null)
            {
                return false;
            }

            if (!TryGetGridCellCenterOnScreen(cellIndex, out Vector2 screenPoint))
            {
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint);
        }

        private void SetPendingDragGhostVisible(bool visible)
        {
            if (pendingDragGhostRect == null)
            {
                return;
            }

            if (pendingDragGhostRect.gameObject.activeSelf != visible)
            {
                pendingDragGhostRect.gameObject.SetActive(visible);
            }
        }

        private void UpdatePendingDragGhostPosition(Vector2 screenPosition)
        {
            if (pendingDragGhostRect == null || canvasRect == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 localPoint))
            {
                Vector2 desired = localPoint + new Vector2(0f, 112f);
                Rect canvasBounds = canvasRect.rect;
                float halfW = pendingDragGhostRect.rect.width * 0.5f;
                float halfH = pendingDragGhostRect.rect.height * 0.5f;
                desired.x = Mathf.Clamp(desired.x, canvasBounds.xMin + halfW, canvasBounds.xMax - halfW);
                desired.y = Mathf.Clamp(desired.y, canvasBounds.yMin + halfH, canvasBounds.yMax - halfH);
                pendingDragGhostRect.anchoredPosition = desired;
            }
        }

        private void RefreshPendingDragGhostVisual(PendingBlockState pending, bool validPlacement)
        {
            if (pendingDragGhostCells == null || pendingDragGhostCells.Length < 9)
            {
                return;
            }

            Color offColor = PendingDragGhostOffColor;
            for (int i = 0; i < pendingDragGhostCells.Length; i++)
            {
                if (pendingDragGhostCells[i] != null)
                {
                    pendingDragGhostCells[i].color = offColor;
                }
                if (pendingDragGhostHighlights != null && i < pendingDragGhostHighlights.Length && pendingDragGhostHighlights[i] != null)
                {
                    pendingDragGhostHighlights[i].color = new Color(1f, 1f, 1f, 0f);
                }
                if (pendingDragGhostOutlines != null && i < pendingDragGhostOutlines.Length && pendingDragGhostOutlines[i] != null)
                {
                    pendingDragGhostOutlines[i].color = new Color(0f, 0f, 0f, 0f);
                }
            }

            if (pending == null)
            {
                return;
            }

            Color onColor = ColorFromSeed(pending.ColorSeed);
            Color tint = validPlacement ? PendingGhostValidColor : PendingGhostInvalidColor;
            onColor = Color.Lerp(onColor, tint, validPlacement ? 0.20f : 0.40f);
            onColor.a = validPlacement ? 0.94f : 0.88f;
            Color highlightOn = Color.Lerp(onColor, Color.white, 0.45f);
            highlightOn.a = validPlacement ? 0.70f : 0.60f;

            Vector2Int[] offsets = GetPendingTokenPreviewOffsets(pending);
            for (int i = 0; i < offsets.Length; i++)
            {
                int x = Mathf.Clamp(offsets[i].x, 0, 2);
                int y = Mathf.Clamp(offsets[i].y, 0, 2);
                int cellIndex = (2 - y) * 3 + x;
                if (cellIndex >= 0 && cellIndex < pendingDragGhostCells.Length && pendingDragGhostCells[cellIndex] != null)
                {
                    pendingDragGhostCells[cellIndex].color = onColor;
                    if (pendingDragGhostOutlines != null && cellIndex < pendingDragGhostOutlines.Length && pendingDragGhostOutlines[cellIndex] != null)
                    {
                        Color outlineColor = validPlacement ? new Color(0.10f, 0.72f, 0.36f, 0.85f) : new Color(0.86f, 0.24f, 0.20f, 0.85f);
                        pendingDragGhostOutlines[cellIndex].color = outlineColor;
                    }
                    if (pendingDragGhostHighlights != null && cellIndex < pendingDragGhostHighlights.Length && pendingDragGhostHighlights[cellIndex] != null)
                    {
                        pendingDragGhostHighlights[cellIndex].color = highlightOn;
                    }
                }
            }
        }

        private void HandlePendingDrag()
        {
            if (pendingTokenRect == null || pendingHintText == null)
            {
                return;
            }

            if (model.EventPending)
            {
                isDraggingPending = false;
                ResetDragPointerCapture();
                pendingTokenRect.gameObject.SetActive(false);
                SetPendingDragGhostVisible(false);
                SetPendingRecommendationAssistVisible(false);
                SetPendingTokenPreviewPlaceholder();
                RefreshPendingHintText();
                ResetPendingPlacementAssist(true);
                return;
            }

            if (model.HasDrawChoice)
            {
                isDraggingPending = false;
                ResetDragPointerCapture();
                pendingTokenRect.gameObject.SetActive(false);
                SetPendingDragGhostVisible(false);
                SetPendingRecommendationAssistVisible(false);
                SetPendingTokenPreviewPlaceholder();
                RefreshPendingHintText();
                ResetPendingPlacementAssist(true);
                return;
            }

            if (!model.HasPendingBlock)
            {
                isDraggingPending = false;
                ResetDragPointerCapture();
                pendingTokenRect.gameObject.SetActive(false);
                SetPendingDragGhostVisible(false);
                SetPendingRecommendationAssistVisible(false);
                SetPendingTokenPreviewPlaceholder();
                RefreshPendingHintText();
                ResetPendingPlacementAssist(true);
                return;
            }

            pendingTokenRect.gameObject.SetActive(true);
            string pendingInputHint = Application.isMobilePlatform
                ? "Rotate buttons, Drag or tap grid"
                : "Rotate Q/E, Drag or tap grid";
            pendingTokenText.text = model.PendingBlock.Label + "\n" + pendingInputHint;
            SetPendingRecommendationAssistVisible(false);
            RefreshPendingHintText();
            RefreshPendingTokenVisual(model.PendingBlock);
            selectedMergeCell = -1;
            RefreshPendingDragGhostVisual(model.PendingBlock, false);

            if (!isDraggingPending)
            {
                SnapPendingTokenToHome();
                SetPendingDragGhostVisible(false);

                if (TryGetPointerDownThisFrame(out Vector2 pointerDownPosition, out bool isTouchPointer, out int touchId))
                {
                    if (TryHandlePendingRotateTouch(pointerDownPosition))
                    {
                        return;
                    }

                    bool pressedToken = RectTransformUtility.RectangleContainsScreenPoint(pendingTokenRect, pointerDownPosition, null);
                    bool pressedHomeSlot = pendingSlotRect != null && RectTransformUtility.RectangleContainsScreenPoint(pendingSlotRect, pointerDownPosition, null);
                    if (pressedToken || pressedHomeSlot)
                    {
                        isDraggingPending = true;
                        dragPointerIsTouch = isTouchPointer;
                        dragTouchId = isTouchPointer ? touchId : -1;
                        lastDragPointerScreenPos = pointerDownPosition;

                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pointerDownPosition, null, out Vector2 localPoint))
                        {
                            pendingDragOffset = pendingTokenRect.anchoredPosition - localPoint;
                        }

                        SetPendingDragGhostVisible(true);
                        UpdatePendingDragGhostPosition(pointerDownPosition);
                        RefreshPendingDragGhostVisual(model.PendingBlock, false);
                    }
                    else if (TryResolvePlacementAnchor(pointerDownPosition, true, out int tapCell))
                    {
                        int[] affectedCells = Array.Empty<int>();
                        affectedCells = ResolvePendingPlacementFeedbackCells(tapCell);

                        bool placed = model.TryPlacePendingAtCell(tapCell);
                        TriggerPendingPlacementFeedback(placed, affectedCells);
                        if (!placed)
                        {
                            BuildPlacementBlockedHint();
                            UpdateGridHoverFromMouse(pointerDownPosition, false, true);
                        }
                        else
                        {
                            lastPlacementBlockedHint = string.Empty;
                            ResetPendingPlacementAssist(true);
                        }

                        SnapPendingTokenToHome();
                        return;
                    }
                }

                if (PrimaryPointerIsPressed())
                {
                    Vector2 pointerPosition = GetPrimaryPointerPosition();
                    UpdateGridHoverFromMouse(pointerPosition, false, true);
                    UpdatePendingRecommendationAssist(pointerPosition);
                }
                else if (TryGetPrimaryPointerPosition(out Vector2 hoverPosition))
                {
                    UpdateGridHoverFromMouse(hoverPosition, false, true);
                    UpdatePendingRecommendationAssist(hoverPosition);
                }
                else
                {
                    SetPendingRecommendationAssistVisible(false);
                    ResetPendingPlacementAssist(true);
                }
            }

            if (isDraggingPending)
            {
                if (TryGetCapturedPointerPosition(out Vector2 dragPosition))
                {
                    lastDragPointerScreenPos = dragPosition;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, dragPosition, null, out Vector2 localPoint))
                    {
                        pendingTokenRect.anchoredPosition = localPoint + pendingDragOffset;
                    }

                    UpdateGridHoverFromMouse(dragPosition, true, true);
                    UpdatePendingDragGhostPosition(dragPosition);
                    RefreshPendingDragGhostVisual(model.PendingBlock, pendingHoverValid);
                    UpdatePendingRecommendationAssist(dragPosition);
                    SetPendingDragGhostVisible(true);
                }

                if (TryGetCapturedPointerReleaseThisFrame(out Vector2 releasePosition))
                {
                    isDraggingPending = false;
                    ResetDragPointerCapture();
                    Vector2 dropPosition = releasePosition;

                    int dropCell = pendingHoverAnchorCell;
                    if (dropCell < 0 && TryResolvePlacementAnchor(dropPosition, true, out int anchorCell))
                    {
                        dropCell = anchorCell;
                    }

                    if (dropCell >= 0)
                    {
                        int[] affectedCells = ResolvePendingPlacementFeedbackCells(dropCell);

                        bool placed = model.TryPlacePendingAtCell(dropCell);
                        TriggerPendingPlacementFeedback(placed, affectedCells);
                        if (!placed)
                        {
                            BuildPlacementBlockedHint();
                        }
                        else
                        {
                            lastPlacementBlockedHint = string.Empty;
                        }
                    }
                    else
                    {
                        ShowPlacementBlockedCue("Drop onto a valid inventory slot.");
                        TriggerPendingPlacementFeedback(false, Array.Empty<int>());
                    }

                    ResetPendingPlacementAssist(true);
                    SnapPendingTokenToHome();
                    SetPendingDragGhostVisible(false);
                    SetPendingRecommendationAssistVisible(false);
                }
            }
        }

        private void ResetDragPointerCapture()
        {
            dragPointerIsTouch = false;
            dragTouchId = -1;
            lastDragPointerScreenPos = Vector2.zero;
        }

        private int[] ResolvePendingPlacementFeedbackCells(int anchorCell)
        {
            if (model == null)
            {
                return Array.Empty<int>();
            }

            if (model.TryGetPendingFootprintCells(anchorCell, out int[] directCells))
            {
                return directCells ?? Array.Empty<int>();
            }

            if (model.TryGetPendingFootprintCellsPreview(anchorCell, false, out int[] anyCells))
            {
                return anyCells ?? Array.Empty<int>();
            }

            return Array.Empty<int>();
        }

        private static bool TryGetPointerDownThisFrame(out Vector2 pointerDownPosition, out bool isTouchPointer, out int touchId)
        {
            pointerDownPosition = Vector2.zero;
            isTouchPointer = false;
            touchId = -1;

            if (TryGetTouchPointerDownThisFrame(out pointerDownPosition, out touchId))
            {
                isTouchPointer = true;
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pointerDownPosition = Mouse.current.position.ReadValue();
                return true;
            }

            return false;
        }

        private bool TryGetCapturedPointerPosition(out Vector2 position)
        {
            if (dragPointerIsTouch)
            {
                if (TryGetTouchStateById(dragTouchId, out position, out _, out _))
                {
                    return true;
                }

                return TryGetTouchPointerPosition(out position);
            }

            if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }

            position = Vector2.zero;
            return false;
        }

        private bool TryGetCapturedPointerReleaseThisFrame(out Vector2 releasePosition)
        {
            releasePosition = Vector2.zero;
            if (dragPointerIsTouch)
            {
                if (TryGetTouchStateById(dragTouchId, out Vector2 exactPosition, out _, out bool wasReleasedThisFrame) && wasReleasedThisFrame)
                {
                    releasePosition = exactPosition;
                    return true;
                }

                if (TryGetTouchPointerReleaseThisFrame(out Vector2 anyReleasePosition, out int releasedTouchId) &&
                    (dragTouchId < 0 || releasedTouchId == dragTouchId))
                {
                    releasePosition = anyReleasePosition;
                    return true;
                }

                return false;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                releasePosition = Mouse.current.position.ReadValue();
                return true;
            }

            return false;
        }

        private static bool TryGetTouchPointerDownThisFrame(out Vector2 pointerDownPosition, out int touchId)
        {
            pointerDownPosition = Vector2.zero;
            touchId = -1;

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            var primaryTouch = touchscreen.primaryTouch;
            if (primaryTouch != null && primaryTouch.press.wasPressedThisFrame)
            {
                pointerDownPosition = primaryTouch.position.ReadValue();
                touchId = primaryTouch.touchId.ReadValue();
                return true;
            }

            var touches = touchscreen.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch == null || !touch.press.wasPressedThisFrame)
                {
                    continue;
                }

                pointerDownPosition = touch.position.ReadValue();
                touchId = touch.touchId.ReadValue();
                return true;
            }

            return false;
        }

        private static bool TryGetTouchPointerReleaseThisFrame(out Vector2 releasePosition, out int touchId)
        {
            releasePosition = Vector2.zero;
            touchId = -1;

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            var primaryTouch = touchscreen.primaryTouch;
            if (primaryTouch != null && primaryTouch.press.wasReleasedThisFrame)
            {
                releasePosition = primaryTouch.position.ReadValue();
                touchId = primaryTouch.touchId.ReadValue();
                return true;
            }

            var touches = touchscreen.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch == null || !touch.press.wasReleasedThisFrame)
                {
                    continue;
                }

                releasePosition = touch.position.ReadValue();
                touchId = touch.touchId.ReadValue();
                return true;
            }

            return false;
        }

        private static bool TryGetTouchStateById(int touchId, out Vector2 position, out bool isPressed, out bool wasReleasedThisFrame)
        {
            position = Vector2.zero;
            isPressed = false;
            wasReleasedThisFrame = false;

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            var touches = touchscreen.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch == null)
                {
                    continue;
                }

                if (touch.touchId.ReadValue() != touchId)
                {
                    continue;
                }

                position = touch.position.ReadValue();
                isPressed = touch.press.isPressed;
                wasReleasedThisFrame = touch.press.wasReleasedThisFrame;
                return true;
            }

            return false;
        }

        private static Vector2 GetPrimaryPointerPosition()
        {
            if (TryGetTouchPointerPosition(out Vector2 touchPosition))
            {
                return touchPosition;
            }

            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }

            return Vector2.zero;
        }

        private static bool TryGetPrimaryPointerPosition(out Vector2 position)
        {
            if (TryGetTouchPointerPosition(out position))
            {
                return true;
            }

            if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }

            position = Vector2.zero;
            return false;
        }

        private static bool PrimaryPointerDownThisFrame()
        {
            if (TouchPointerDownThisFrame())
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        private static bool PrimaryPointerIsPressed()
        {
            if (TouchPointerIsPressed())
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                return true;
            }

            return false;
        }

        private static bool PrimaryPointerUpThisFrame()
        {
            if (TouchPointerUpThisFrame())
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                return true;
            }

            return false;
        }

        private static bool TryGetTouchPointerPosition(out Vector2 position)
        {
            position = Vector2.zero;
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            var primaryTouch = touchscreen.primaryTouch;
            if (primaryTouch != null && (primaryTouch.press.isPressed || primaryTouch.press.wasPressedThisFrame || primaryTouch.press.wasReleasedThisFrame))
            {
                position = primaryTouch.position.ReadValue();
                return true;
            }

            var touches = touchscreen.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch == null)
                {
                    continue;
                }

                if (touch.press.isPressed || touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame)
                {
                    position = touch.position.ReadValue();
                    return true;
                }
            }

            return false;
        }

        private static bool TouchPointerDownThisFrame()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            var primaryTouch = touchscreen.primaryTouch;
            if (primaryTouch != null && primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            var touches = touchscreen.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch != null && touch.press.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TouchPointerIsPressed()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            var primaryTouch = touchscreen.primaryTouch;
            if (primaryTouch != null && primaryTouch.press.isPressed)
            {
                return true;
            }

            var touches = touchscreen.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch != null && touch.press.isPressed)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TouchPointerUpThisFrame()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            var primaryTouch = touchscreen.primaryTouch;
            if (primaryTouch != null && primaryTouch.press.wasReleasedThisFrame)
            {
                return true;
            }

            var touches = touchscreen.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch != null && touch.press.wasReleasedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetPendingTokenPreviewPlaceholder()
        {
            if (pendingTokenPreviewCells == null || pendingTokenPreviewCells.Length < 9)
            {
                return;
            }

            Color offColor = new Color(0.18f, 0.20f, 0.25f, 0.95f);
            Color centerColor = new Color(0.30f, 0.34f, 0.42f, 0.95f);
            for (int i = 0; i < pendingTokenPreviewCells.Length; i++)
            {
                if (pendingTokenPreviewCells[i] != null)
                {
                    ApplyKitchenModuleCellSprite(pendingTokenPreviewCells[i], false);
                    pendingTokenPreviewCells[i].color = offColor;
                }
            }

            if (pendingTokenPreviewCells[4] != null)
            {
                ApplyKitchenModuleCellSprite(pendingTokenPreviewCells[4], false);
                pendingTokenPreviewCells[4].color = centerColor;
            }

            if (pendingTokenIngredientImage != null)
            {
                pendingTokenIngredientImage.sprite = null;
                pendingTokenIngredientImage.color = new Color(0.24f, 0.28f, 0.35f, 0.96f);
            }

            if (pendingTokenIngredientBadge != null)
            {
                pendingTokenIngredientBadge.text = "?";
                pendingTokenIngredientBadge.color = new Color(0.90f, 0.94f, 0.99f, 0.95f);
            }
        }

        private void RefreshPendingTokenVisual(PendingBlockState pending)
        {
            if (pending == null)
            {
                SetPendingTokenPreviewPlaceholder();
                return;
            }

            if (pendingTokenPreviewCells != null && pendingTokenPreviewCells.Length >= 9)
            {
                Color offColor = new Color(0.18f, 0.20f, 0.25f, 0.95f);
                Color onColor = Color.Lerp(ColorFromSeed(pending.ColorSeed), Color.white, 0.12f);
                onColor.a = 0.98f;

                for (int i = 0; i < pendingTokenPreviewCells.Length; i++)
                {
                    if (pendingTokenPreviewCells[i] != null)
                    {
                        ApplyKitchenModuleCellSprite(pendingTokenPreviewCells[i], false);
                        pendingTokenPreviewCells[i].color = offColor;
                    }
                }

                Vector2Int[] offsets = GetPendingTokenPreviewOffsets(pending);
                for (int i = 0; i < offsets.Length; i++)
                {
                    int x = Mathf.Clamp(offsets[i].x, 0, 2);
                    int y = Mathf.Clamp(offsets[i].y, 0, 2);
                    int cellIndex = (2 - y) * 3 + x;
                    if (cellIndex >= 0 && cellIndex < pendingTokenPreviewCells.Length && pendingTokenPreviewCells[cellIndex] != null)
                    {
                        ApplyKitchenModuleCellSprite(pendingTokenPreviewCells[cellIndex], true);
                        pendingTokenPreviewCells[cellIndex].color = onColor;
                    }
                }
            }

            if (pendingTokenIngredientImage == null || pendingTokenIngredientBadge == null)
            {
                return;
            }

            Sprite sprite = ResolveIngredientSprite(pending.IngredientName);
            if (sprite != null)
            {
                pendingTokenIngredientImage.sprite = sprite;
                pendingTokenIngredientImage.color = Color.white;
                pendingTokenIngredientBadge.text = string.Empty;
            }
            else
            {
                pendingTokenIngredientImage.sprite = null;
                pendingTokenIngredientImage.color = Color.Lerp(ColorFromSeed(pending.ColorSeed), new Color(0.16f, 0.18f, 0.24f, 1f), 0.40f);
                pendingTokenIngredientImage.color = new Color(pendingTokenIngredientImage.color.r, pendingTokenIngredientImage.color.g, pendingTokenIngredientImage.color.b, 0.98f);
                pendingTokenIngredientBadge.text = BuildIngredientBadge(pending.IngredientName);
                pendingTokenIngredientBadge.color = Color.white;
            }
        }

        private Vector2Int[] GetPendingTokenPreviewOffsets(PendingBlockState pending)
        {
            if (pending == null || pending.CellOffsets == null || pending.CellOffsets.Length == 0)
            {
                return Array.Empty<Vector2Int>();
            }

            int quarterTurns = 0;
            if (model != null)
            {
                quarterTurns = ((model.PendingRotationDegrees / 90) % 4 + 4) % 4;
            }

            Vector2Int[] source = pending.CellOffsets;
            Vector2Int[] rotated = new Vector2Int[source.Length];
            int minX = int.MaxValue;
            int minY = int.MaxValue;

            for (int i = 0; i < source.Length; i++)
            {
                int x = source[i].x;
                int y = source[i].y;

                for (int t = 0; t < quarterTurns; t++)
                {
                    int nextX = y;
                    int nextY = -x;
                    x = nextX;
                    y = nextY;
                }

                if (x < minX)
                {
                    minX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                rotated[i] = new Vector2Int(x, y);
            }

            for (int i = 0; i < rotated.Length; i++)
            {
                rotated[i] = new Vector2Int(rotated[i].x - minX, rotated[i].y - minY);
            }

            return rotated;
        }

        private void SnapPendingTokenToHome()
        {
            if (pendingSlotRect == null || canvasRect == null)
            {
                return;
            }

            Vector3 worldCenter = pendingSlotRect.TransformPoint(pendingSlotRect.rect.center);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldCenter);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint))
            {
                pendingTokenRect.anchoredPosition = localPoint;
            }
        }

        private bool TryHandlePendingRotateTouch(Vector2 pointerDownPosition)
        {
            if (model == null || !model.HasPendingBlock)
            {
                return false;
            }

            if (pendingRotateLeftHotspot != null && RectTransformUtility.RectangleContainsScreenPoint(pendingRotateLeftHotspot, pointerDownPosition, null))
            {
                bool rotated = model.RotatePendingCounterClockwise();
                TriggerPendingTokenPulse(rotated);
                return true;
            }

            if (pendingRotateRightHotspot != null && RectTransformUtility.RectangleContainsScreenPoint(pendingRotateRightHotspot, pointerDownPosition, null))
            {
                bool rotated = model.RotatePendingClockwise();
                TriggerPendingTokenPulse(rotated);
                return true;
            }

            return false;
        }
    }
}
