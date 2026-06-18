using System;
using UnityEngine;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        public enum PlayModeVerificationState
        {
            DrawChoice,
            PendingPlacement,
            InvalidPlacement,
            WaveCombat
        }

        public bool TryPreparePlayModeVerificationState(PlayModeVerificationState state, out string message)
        {
            message = string.Empty;
            if (model == null)
            {
                message = "FoodTruckRunModel is not initialized.";
                return false;
            }

            bool prepared;
            switch (state)
            {
                case PlayModeVerificationState.DrawChoice:
                    prepared = PrepareDrawChoiceVerificationState(out message);
                    break;
                case PlayModeVerificationState.PendingPlacement:
                    prepared = PreparePendingPlacementVerificationState(out message);
                    break;
                case PlayModeVerificationState.InvalidPlacement:
                    prepared = PrepareInvalidPlacementVerificationState(out message);
                    break;
                case PlayModeVerificationState.WaveCombat:
                    prepared = PrepareWaveCombatVerificationState(out message);
                    break;
                default:
                    message = "Unknown verification state.";
                    return false;
            }

            if (prepared)
            {
                RefreshAfterPlayModeVerificationSetup();
                if (state == PlayModeVerificationState.WaveCombat)
                {
                    SpawnWaveCombatVerificationShowcase();
                    Canvas.ForceUpdateCanvases();
                }
            }

            return prepared;
        }

        public string BuildPlayModeVerificationStateSummary()
        {
            if (model == null)
            {
                return "model=missing";
            }

            string pending = model.HasPendingBlock && model.PendingBlock != null
                ? model.PendingBlock.Label
                : "none";
            string failText = string.IsNullOrEmpty(model.LastPlacementFailReasonText)
                ? "none"
                : model.LastPlacementFailReasonText;

            return "wave=" + model.Wave +
                ", draw_choice=" + model.HasDrawChoice +
                ", wave_choice_used=" + model.WaveBlockChoiceUsed +
                ", theme=" + model.CurrentWavePressureThemeLabel +
                ", pending=" + pending +
                ", fail_reason=" + model.LastPlacementFailReason +
                " (" + failText + ")" +
                ", enemies=" + model.LaneEnemies.Count +
                ", filled_cells=" + model.InventoryCellsFilled +
                ", hp=" + model.TruckHp.ToString("0") + "/" + model.MaxTruckHp.ToString("0") +
                ", heat=" + model.Heat.ToString("0.0");
        }

        private bool PrepareDrawChoiceVerificationState(out string message)
        {
            ResetRunForPlayModeVerification();
            if (!model.DrawIngredient())
            {
                message = "Could not draw options.";
                return false;
            }

            message = "Prepared Draw Choice: 3 cards should be visible.";
            ShowCueBanner("Verification state: Draw Choice.", AccentBlue);
            return true;
        }

        private bool PreparePendingPlacementVerificationState(out string message)
        {
            ResetRunForPlayModeVerification();
            if (!model.DrawIngredient() || !model.ChooseDrawOption(0))
            {
                message = "Could not create pending block.";
                return false;
            }

            message = "Prepared Pending Placement: selected block and 3x3 grid should be visible.";
            ShowCueBanner("Verification state: Pending Placement.", AccentGreen);
            return true;
        }

        private bool PrepareInvalidPlacementVerificationState(out string message)
        {
            if (!PreparePendingPlacementVerificationState(out message))
            {
                return false;
            }

            bool blocked = !model.TryPlacePendingAtCell(-1);
            if (!blocked)
            {
                message = "Unexpectedly placed block while preparing invalid placement.";
                return false;
            }

            string blockedReason = BuildPlacementBlockedHint(model.LastPlacementFailReasonText, model.LastPlacementFailReason);
            TriggerPendingPlacementFeedback(false, Array.Empty<int>());
            ShowPlacementBlockedCue(blockedReason);
            RefreshPendingHintText();

            message = "Prepared Invalid Placement: " + (string.IsNullOrEmpty(blockedReason) ? "placement blocked." : blockedReason);
            return true;
        }

        private bool PrepareWaveCombatVerificationState(out string message)
        {
            ResetRunForPlayModeVerification();
            if (model.DrawIngredient() && model.ChooseDrawOption(0))
            {
                int anchor = FindFirstValidPendingAnchor();
                if (anchor >= 0)
                {
                    model.TryPlacePendingAtCell(anchor);
                }
            }

            model.SetBattleSpeed(2f);
            for (int i = 0; i < 12; i++)
            {
                model.Tick(1f);
            }

            message = "Prepared Wave Combat: lanes, truck, enemies, HP, Heat, attack trails, attack labels, and Wave status should be readable.";
            ShowCueBanner("Verification state: Wave Combat.", AccentOrange);
            return true;
        }

        private void ResetRunForPlayModeVerification()
        {
            model.ResetRun();
            ClearTransientCombatVisuals();
            lastPlacementBlockedHint = string.Empty;
            lastPlacementBlockedFailReason = PlacementFailReason.None;
            ResetPendingPlacementAssist(true);
            SetPendingDragGhostVisible(false);
            SetPendingRecommendationAssistVisible(false);
            ResetTelemetryRunTracking(true);
            CaptureTelemetryWaveBaseline();
        }

        private int FindFirstValidPendingAnchor()
        {
            if (model == null || !model.HasPendingBlock)
            {
                return -1;
            }

            for (int anchor = 0; anchor < FoodTruckRunModel.InventoryCellCount; anchor++)
            {
                if (model.TryGetPendingFootprintCellsPreview(anchor, true, out _, out _, out _))
                {
                    return anchor;
                }
            }

            return -1;
        }

        private void SpawnWaveCombatVerificationShowcase()
        {
            if (laneTrackRoots == null || laneTrackRoots.Length < 3)
            {
                return;
            }

            TriggerLaneHitFlash(1);
            TriggerLaneHitFlash(2);

            SpawnVerificationCombatImpact(0, 0.64f, -0.05f, false);
            SpawnVerificationCombatImpact(1, 0.58f, 0.02f, true);
            SpawnVerificationAttackTrail(0, 0.64f, -0.05f, false);
            SpawnVerificationAttackTrail(1, 0.58f, 0.02f, true);

            SpawnVerificationCombatLabel(
                0,
                0.70f,
                0.24f,
                "HIT>Z -12",
                new Color(1f, 0.64f, 0.28f, 1f),
                4.60f);
            SpawnVerificationCombatLabel(
                1,
                0.62f,
                0.25f,
                "HIT>Z KO",
                new Color(1f, 0.94f, 0.42f, 1f),
                4.85f);
            SpawnVerificationCombatLabel(
                2,
                0.34f,
                0.18f,
                "LEAK",
                new Color(1f, 0.32f, 0.22f, 1f),
                4.50f);
            SpawnVerificationCombatLabel(
                1,
                0.18f,
                0.26f,
                BuildTruckDamageFloaterLabel("BITE", 7f),
                new Color(1f, 0.40f, 0.26f, 1f),
                4.70f);
        }

        private void SpawnVerificationCombatImpact(int laneIndex, float x01, float yOffset01, bool knockout)
        {
            if (laneIndex < 0 || laneIndex >= laneTrackRoots.Length)
            {
                return;
            }

            RectTransform laneRoot = laneTrackRoots[laneIndex];
            if (laneRoot == null)
            {
                return;
            }

            float laneHeight = Mathf.Max(1f, laneRoot.rect.height);
            Vector2 position = BuildVerificationCombatLabelPosition(laneRoot, x01, yOffset01);
            RectTransform anchor = new GameObject("VerificationCombatImpactAnchor", typeof(RectTransform)).GetComponent<RectTransform>();
            anchor.transform.SetParent(laneRoot, false);
            anchor.anchorMin = new Vector2(0f, 0.5f);
            anchor.anchorMax = new Vector2(0f, 0.5f);
            anchor.pivot = new Vector2(0.5f, 0.5f);
            anchor.anchoredPosition = position;
            anchor.sizeDelta = new Vector2(Mathf.Clamp(laneHeight * 0.48f, 26f, 72f), Mathf.Clamp(laneHeight * 0.48f, 26f, 72f));

            EnemyVisualWidget widget = new EnemyVisualWidget();
            widget.Rect = anchor;
            widget.LaneIndex = laneIndex;
            widget.EnemyId = 900 + laneIndex;
            widget.IsSpecial = knockout;
            widget.LastHp = knockout ? 1f : 12f;
            widget.LastDistance01 = Mathf.Clamp01(x01);
            widget.KnockoutFloaterSpawned = false;

            SpawnEnemyAttackTrail(widget, knockout);
            SpawnEnemyHitEffect(widget, knockout);
            Destroy(anchor.gameObject);
        }

        private void SpawnVerificationAttackTrail(int laneIndex, float x01, float yOffset01, bool knockout)
        {
            if (laneIndex < 0 || laneIndex >= laneTrackRoots.Length)
            {
                return;
            }

            RectTransform laneRoot = laneTrackRoots[laneIndex];
            if (laneRoot == null)
            {
                return;
            }

            float laneHeight = Mathf.Max(1f, laneRoot.rect.height);
            Vector2 target = BuildVerificationCombatLabelPosition(laneRoot, x01, yOffset01);
            float truckEndX = foodTruckSprite != null
                ? Mathf.Clamp(laneHeight * 1.66f, 118f, 252f)
                : Mathf.Clamp(laneHeight * 0.82f, 64f, 136f);
            float startX = Mathf.Min(truckEndX, target.x - laneHeight * 0.58f);
            float width = Mathf.Max(laneHeight * 0.92f, target.x - startX);
            float punch = knockout ? 1.24f : 1f;

            SpawnEnemyAttackTrailSegment(laneRoot, startX, target.y, width, laneHeight, 0f, 0.30f * punch, new Color(0.20f, 0.05f, 0.01f, 0.86f), 4.40f);
            SpawnEnemyAttackTrailSegment(laneRoot, startX, target.y, width, laneHeight, 0f, 0.18f * punch, new Color(1f, 0.62f, 0.08f, 1f), 4.40f);
            SpawnEnemyAttackTrailSegment(laneRoot, startX + width * 0.12f, target.y, width * 0.72f, laneHeight, 0f, 0.09f * punch, new Color(1f, 1f, 0.74f, 1f), 4.10f);
            SpawnEnemyAttackArrowhead(laneRoot, target, laneHeight, knockout);
        }

        private void SpawnVerificationCombatLabel(
            int laneIndex,
            float x01,
            float yOffset01,
            string label,
            Color color,
            float durationScale)
        {
            if (laneIndex < 0 || laneIndex >= laneTrackRoots.Length)
            {
                return;
            }

            RectTransform laneRoot = laneTrackRoots[laneIndex];
            if (laneRoot == null)
            {
                return;
            }

            float laneHeight = Mathf.Max(1f, laneRoot.rect.height);
            SpawnCombatFloatingText(
                laneRoot,
                BuildVerificationCombatLabelPosition(laneRoot, x01, yOffset01),
                label,
                color,
                Mathf.Clamp(Mathf.RoundToInt(laneHeight * 0.26f), 18, 36),
                durationScale);
        }

        private static Vector2 BuildVerificationCombatLabelPosition(RectTransform laneRoot, float x01, float yOffset01)
        {
            float laneWidth = laneRoot != null ? Mathf.Max(80f, laneRoot.rect.width) : 80f;
            float laneHeight = laneRoot != null ? Mathf.Max(1f, laneRoot.rect.height) : 1f;
            float safeInset = Mathf.Clamp(laneHeight * 0.64f, 42f, 86f);
            float x = Mathf.Clamp(laneWidth * Mathf.Clamp01(x01), safeInset, Mathf.Max(safeInset, laneWidth - safeInset));
            float y = laneHeight * Mathf.Clamp(yOffset01, -0.35f, 0.35f);
            return new Vector2(x, y);
        }

        private void RefreshAfterPlayModeVerificationSetup()
        {
            RefreshAll();
            RefreshPendingHintText();
            UpdateFlowChecklist();
            RefreshGameplayHudContextImmediate();
            ApplyPanelLayout();
            UpdateInventoryGridSizing();
            StabilizeInventoryLayout();
            Canvas.ForceUpdateCanvases();
        }
    }
}
