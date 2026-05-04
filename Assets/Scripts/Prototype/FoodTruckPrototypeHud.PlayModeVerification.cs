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

            message = "Prepared Wave Combat: lanes, truck, enemies, HP, Heat, and Wave status should be readable.";
            ShowCueBanner("Verification state: Wave Combat.", AccentOrange);
            return true;
        }

        private void ResetRunForPlayModeVerification()
        {
            model.ResetRun();
            lastPlacementBlockedHint = string.Empty;
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
