using UnityEngine;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private void HandlePresentationTrigger(PresentationTriggerType triggerType, string payload)
        {
            string message;
            Color color;
            AudioClip clip = null;
            float clipVolume = 1f;
            Sprite vfxSprite = null;
            float vfxSizeScale = 1f;
            float vfxDurationScale = 1f;

            switch (triggerType)
            {
                case PresentationTriggerType.EventSelected:
                    message = "Event selected: " + (string.IsNullOrEmpty(payload) ? "Option applied." : payload);
                    color = new Color(0.94f, 0.72f, 0.30f, 1f);
                    clip = sfxSelectClip;
                    clipVolume = 0.92f;
                    suppressNextEventResolvedCue = true;
                    eventResolvePanelPulseTimer = Mathf.Max(eventResolvePanelPulseTimer, Mathf.Max(0.08f, eventResolvePanelPulseDuration));
                    break;
                case PresentationTriggerType.PlacementSuccess:
                    message = "Placed: " + (string.IsNullOrEmpty(payload) ? "Block deployed." : payload);
                    color = new Color(0.34f, 0.78f, 0.46f, 1f);
                    clip = sfxPlaceSuccessClip;
                    vfxSprite = vfxPlaceSuccessSprite;
                    vfxSizeScale = 0.82f;
                    vfxDurationScale = 0.90f;
                    suppressNextPlacementResolvedCue = true;
                    break;
                case PresentationTriggerType.AutoMergeSuccess:
                    message = "Merged: " + (string.IsNullOrEmpty(payload) ? "Auto merge completed." : payload);
                    color = new Color(0.30f, 0.82f, 0.60f, 1f);
                    clip = sfxPlaceSuccessClip;
                    vfxSprite = vfxPlaceSuccessSprite;
                    vfxSizeScale = 1.04f;
                    vfxDurationScale = 1.05f;
                    suppressNextPlacementResolvedCue = true;
                    break;
                case PresentationTriggerType.PlacementBlocked:
                    string blockedReason = BuildPlacementBlockedHint(payload);
                    string recoveryHint = BuildPlacementBlockedActionHint(blockedReason, lastPlacementBlockedFailReason);
                    message = string.IsNullOrEmpty(blockedReason)
                        ? "Placement blocked."
                        : "Placement blocked: " + blockedReason +
                            (string.IsNullOrEmpty(recoveryHint) ? string.Empty : " " + recoveryHint);
                    color = new Color(0.92f, 0.38f, 0.28f, 1f);
                    clip = sfxPlaceBlockedClip;
                    clipVolume = 0.80f;
                    vfxSprite = ResolvePlacementFailVfxSprite(payload);
                    vfxSizeScale = 0.92f;
                    break;
                case PresentationTriggerType.RecipeActivated:
                    lastActivatedRecipeName = string.IsNullOrEmpty(payload) ? string.Empty : payload;
                    synergyChipPulseTimer = Mathf.Max(synergyChipPulseTimer, Mathf.Max(0.08f, synergyChipPulseDuration));
                    progressionUnlockPanelPulseTimer = Mathf.Max(
                        progressionUnlockPanelPulseTimer,
                        Mathf.Max(0.10f, progressionUnlockPanelPulseDuration * 0.72f));
                    message = string.IsNullOrEmpty(payload)
                        ? "Recipe online."
                        : "Recipe online: " + payload;
                    color = new Color(0.96f, 0.72f, 0.26f, 1f);
                    clip = sfxProgressionUnlockClip != null ? sfxProgressionUnlockClip : sfxComboBurstClip;
                    clipVolume = 0.86f;
                    vfxSprite = vfxComboBurstSprite;
                    vfxSizeScale = 0.86f;
                    break;
                case PresentationTriggerType.ComboBurst:
                    message = string.IsNullOrEmpty(payload)
                        ? "Combo Burst activated."
                        : "Combo Burst! " + payload;
                    color = new Color(0.98f, 0.58f, 0.24f, 1f);
                    clip = sfxComboBurstClip;
                    vfxSprite = vfxComboBurstSprite;
                    vfxSizeScale = 1.22f;
                    vfxDurationScale = 1.16f;
                    comboBurstButtonPulseTimer = Mathf.Max(comboBurstButtonPulseTimer, Mathf.Max(0.08f, comboBurstButtonPulseDuration));
                    TriggerLaneHitFlash(0);
                    TriggerLaneHitFlash(1);
                    TriggerLaneHitFlash(2);
                    break;
                case PresentationTriggerType.OverheatSpike:
                    message = string.IsNullOrEmpty(payload)
                        ? "OVERHEAT spike detected. Stabilize heat now."
                        : "OVERHEAT spike! " + payload;
                    message += BuildVentActionCueSuffix();
                    color = new Color(0.98f, 0.44f, 0.24f, 1f);
                    clip = sfxOverheatClip;
                    clipVolume = 0.95f;
                    vfxSprite = vfxOverheatSpikeSprite;
                    vfxSizeScale = 1.18f;
                    vfxDurationScale = 1.18f;
                    overheatSpikePulseTimer = Mathf.Max(overheatSpikePulseTimer, Mathf.Max(0.08f, overheatSpikePulseDuration));
                    TriggerVentReadyPulse(1f);
                    break;
                case PresentationTriggerType.ProgressionUnlock:
                    message = string.IsNullOrEmpty(payload)
                        ? "Progression unlock reached."
                        : "Progression unlock! " + payload;
                    color = new Color(0.34f, 0.78f, 0.96f, 1f);
                    clip = sfxProgressionUnlockClip;
                    vfxSprite = vfxComboBurstSprite;
                    vfxSizeScale = 0.94f;
                    progressionUnlockPanelPulseTimer = Mathf.Max(
                        progressionUnlockPanelPulseTimer,
                        Mathf.Max(0.10f, progressionUnlockPanelPulseDuration));
                    break;
                case PresentationTriggerType.HeatWarningEnter:
                    message = string.IsNullOrEmpty(payload)
                        ? "Heat warning entered. Rewards and risk increased."
                        : "Heat warning: " + payload;
                    message += BuildVentActionCueSuffix();
                    color = new Color(0.96f, 0.70f, 0.26f, 1f);
                    clip = sfxHeatWarningClip;
                    clipVolume = 0.88f;
                    vfxSprite = vfxHeatWarningSprite;
                    vfxSizeScale = 0.98f;
                    heatStatePulseColor = new Color(1f, 0.82f, 0.32f, 1f);
                    heatStatePulseTimer = Mathf.Max(heatStatePulseTimer, Mathf.Max(0.08f, heatStatePulseDuration));
                    TriggerVentReadyPulse(0.84f);
                    break;
                case PresentationTriggerType.HeatStabilized:
                    message = string.IsNullOrEmpty(payload)
                        ? "Heat stabilized. Risk normalized."
                        : "Heat stabilized: " + payload;
                    color = new Color(0.40f, 0.86f, 0.56f, 1f);
                    clip = sfxHeatStabilizedClip;
                    clipVolume = 0.84f;
                    vfxSprite = vfxPlaceSuccessSprite;
                    vfxSizeScale = 0.72f;
                    vfxDurationScale = 0.78f;
                    heatStatePulseColor = new Color(0.62f, 0.96f, 0.74f, 1f);
                    heatStatePulseTimer = Mathf.Max(heatStatePulseTimer, Mathf.Max(0.08f, heatStatePulseDuration));
                    break;
                default:
                    return;
            }

            TriggerPrototypeVfx(vfxSprite, color, vfxSizeScale, vfxDurationScale);
            ShowCueBanner(message, color);
            PlaySfx(clip, clipVolume);
        }

        private string BuildVentActionCueSuffix()
        {
            if (model == null)
            {
                return string.Empty;
            }

            if (model.IsRunOver)
            {
                return " Run ended.";
            }

            if (model.CanVentHeat)
            {
                return Application.isMobilePlatform ? " Tap VENT now." : " Press [C] to vent now.";
            }

            if (model.VentCooldownRemaining > 0f)
            {
                return " Vent CD " + model.VentCooldownRemaining.ToString("0") + "s.";
            }

            if (model.EventPending || model.HasDrawChoice)
            {
                return " Resolve the current choice first.";
            }

            if (model.Heat < model.VentMinHeatThresholdValue)
            {
                return " Vent unlocks at Heat " + model.VentMinHeatThresholdValue.ToString("0") + "+.";
            }

            if (model.Supplies < model.VentSupplyCostValue)
            {
                return " Need " + model.VentSupplyCostValue + "+ supplies to vent.";
            }

            return " Vent temporarily unavailable.";
        }

        private string BuildVentButtonLabel()
        {
            if (model == null)
            {
                return Application.isMobilePlatform ? "Vent\nTap" : "Vent\n[C]";
            }

            if (model.IsRunOver)
            {
                return "Run Over\nVent";
            }

            string inputHint = Application.isMobilePlatform ? "Tap" : "[C]";
            if (model.CanVentHeat)
            {
                return "Vent Ready\n" + inputHint;
            }

            if (model.VentCooldownRemaining > 0f)
            {
                return "Vent CD " + model.VentCooldownRemaining.ToString("0") + "s\n" + inputHint;
            }

            if (model.EventPending || model.HasDrawChoice)
            {
                return "Vent Locked\nFlow";
            }

            if (model.Heat < model.VentMinHeatThresholdValue)
            {
                return "Heat <" + model.VentMinHeatThresholdValue.ToString("0") + "\nVent";
            }

            if (model.Supplies < model.VentSupplyCostValue)
            {
                return "Need " + model.VentSupplyCostValue + " Sup\nVent";
            }

            return "Vent Locked\n" + inputHint;
        }

        private string BuildBurstButtonLabel()
        {
            if (model == null)
            {
                return Application.isMobilePlatform ? "Burst\nTap" : "Burst\n[Space]";
            }

            if (model.IsRunOver)
            {
                return "Burst\nRun Over";
            }

            string inputHint = Application.isMobilePlatform ? "Tap" : "[Space]";
            if (model.CanActivateComboBurst)
            {
                return "Burst Ready\n" + inputHint;
            }

            if (model.EventPending || model.HasDrawChoice)
            {
                return "Burst Locked\nFlow";
            }

            int required = Mathf.Max(1, model.ComboBurstRequiredStreakValue);
            int streak = Mathf.Max(0, model.ComboStreak);
            return "Burst x" + streak + "/" + required + "\n" + inputHint;
        }

        private string BuildDrawButtonLabel(bool flowLocked, bool hasPending)
        {
            if (model == null)
            {
                return Application.isMobilePlatform ? "Draw\nTap" : "Draw\n[D]";
            }

            if (model.IsRunOver)
            {
                return "Draw Locked\nRun Over";
            }

            string inputHint = Application.isMobilePlatform ? "Tap" : "[D]";
            int cost = Mathf.Max(0, model.GetDrawCost());
            if (flowLocked)
            {
                return "Draw Locked\nFlow";
            }

            if (hasPending)
            {
                return "Draw Locked\nPending";
            }

            if (model.Supplies < cost)
            {
                return "Need " + cost + " Sup\nDraw";
            }

            return "Draw " + cost + " Sup\n" + inputHint;
        }

        private string BuildNextWaveButtonLabel(bool readyNow, bool flowLocked)
        {
            if (model == null)
            {
                return Application.isMobilePlatform ? "Next Wave\nTap" : "Next Wave\n[N]";
            }

            if (model.IsRunOver)
            {
                return "Next Locked\nRun Over";
            }

            string inputHint = Application.isMobilePlatform ? "Tap" : "[N]";
            if (readyNow)
            {
                return "Next Wave\n" + inputHint;
            }

            if (flowLocked)
            {
                return "Next Locked\nFlow";
            }

            if (!model.IsRestPhase)
            {
                return "Next Locked\nCombat";
            }

            return "Next Locked\n" + inputHint;
        }

        private string BuildSellButtonLabel(bool flowLocked, bool hasPending, bool hasPlacedBlock)
        {
            if (model == null)
            {
                return Application.isMobilePlatform ? "Sell\nTap" : "Sell\n[S]";
            }

            if (model.IsRunOver)
            {
                return "Sell Locked\nRun Over";
            }

            string inputHint = Application.isMobilePlatform ? "Tap" : "[S]";
            if (flowLocked)
            {
                return "Sell Locked\nFlow";
            }

            if (hasPending)
            {
                return "Sell Pending\n" + inputHint;
            }

            if (hasPlacedBlock)
            {
                return "Sell Board\n" + inputHint;
            }

            return "Sell Locked\nNo Block";
        }

        private string BuildRecipeButtonLabel(bool flowLocked)
        {
            if (model == null)
            {
                return Application.isMobilePlatform ? "Recipe\nTap" : "Recipe\n[F]";
            }

            if (model.IsRunOver)
            {
                return "Recipe Locked\nRun Over";
            }

            if (flowLocked)
            {
                return "Recipe Locked\nFlow";
            }

            string inputHint = Application.isMobilePlatform ? "Tap" : "[F]";
            int activeCount = model.ActiveRecipes != null ? model.ActiveRecipes.Count : 0;
            if (activeCount <= 0)
            {
                return "Recipe Roll\n" + inputHint;
            }

            return "Recipe +" + activeCount + "\n" + inputHint;
        }
    }
}
