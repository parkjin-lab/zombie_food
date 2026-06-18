using UnityEngine;
using UnityEngine.UI;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private void UpdateCueBanner(float dt)
        {
            if (cueBannerText == null || cueBannerBackground == null)
            {
                return;
            }

            if (cueBannerTimer > 0f)
            {
                cueBannerTimer = Mathf.Max(0f, cueBannerTimer - Mathf.Max(0f, dt));
            }

            float alpha = cueBannerDuration > 0.001f ? Mathf.Clamp01(cueBannerTimer / cueBannerDuration) : 0f;
            bool ultraFocus = IsGameplayFocusHudActive() && IsUltraFocusActive();
            bool minimalStrip = IsGameplayFocusHudActive() && minimalCombatStripActive;
            if (ultraFocus)
            {
                alpha *= 0.24f;
            }
            else if (minimalStrip)
            {
                alpha *= Mathf.Clamp01(cueBannerMinimalAlphaScale);
            }

            Color textColor = cueBannerText.color;
            textColor.a = alpha;
            cueBannerText.color = textColor;

            Color bgColor = cueBannerBackground.color;
            float bgScale = minimalStrip ? 0.72f : 0.92f;
            bgColor.a = alpha * bgScale;
            cueBannerBackground.color = bgColor;
        }

        private void ShowCueBanner(string message, Color color)
        {
            if (cueBannerText == null || cueBannerBackground == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            float duration = cueBannerDuration;
            if (IsGameplayFocusHudActive() && minimalCombatStripActive)
            {
                duration *= Mathf.Clamp(cueBannerMinimalDurationScale, 0.2f, 1f);
            }

            cueBannerTimer = Mathf.Max(0.2f, duration);
            cueBannerText.text = message;
            cueBannerText.color = new Color(color.r, color.g, color.b, 1f);
            cueBannerBackground.color = new Color(
                Mathf.Lerp(0.08f, color.r, 0.18f),
                Mathf.Lerp(0.10f, color.g, 0.14f),
                Mathf.Lerp(0.14f, color.b, 0.10f),
                0.92f);
        }

        private string BuildVentStatusText(bool compact)
        {
            if (model == null)
            {
                return compact ? "locked" : "Vent locked";
            }

            if (model.IsRunOver)
            {
                return compact ? "run over" : "Vent locked (run over)";
            }

            string keyboardSuffix = Application.isMobilePlatform ? string.Empty : " [C]";
            if (model.CanVentHeat)
            {
                return compact ? "ready" : "Vent ready" + keyboardSuffix;
            }

            if (model.VentCooldownRemaining > 0f)
            {
                return compact
                    ? "cd " + model.VentCooldownRemaining.ToString("0") + "s"
                    : "Vent CD " + model.VentCooldownRemaining.ToString("0") + "s" + keyboardSuffix;
            }

            if (model.EventPending || model.HasDrawChoice)
            {
                return compact ? "flow lock" : "Vent locked (resolve choice)";
            }

            if (model.Heat < model.VentMinHeatThresholdValue)
            {
                string threshold = model.VentMinHeatThresholdValue.ToString("0");
                return compact ? "heat<" + threshold : "Vent locked (Heat < " + threshold + ")";
            }

            if (model.Supplies < model.VentSupplyCostValue)
            {
                return compact
                    ? "sup<" + model.VentSupplyCostValue
                    : "Vent locked (Need " + model.VentSupplyCostValue + " supplies)";
            }

            return compact ? "locked" : "Vent locked" + keyboardSuffix;
        }

        private string BuildBurstStatusText(bool compact)
        {
            if (model == null)
            {
                return compact ? "locked" : "Burst locked";
            }

            if (model.IsRunOver)
            {
                return compact ? "run over" : "Burst locked (run over)";
            }

            string keyboardSuffix = Application.isMobilePlatform ? string.Empty : " [Space]";
            if (model.CanActivateComboBurst)
            {
                return compact ? "ready" : "Burst ready" + keyboardSuffix;
            }

            if (model.EventPending || model.HasDrawChoice)
            {
                return compact ? "flow lock" : "Burst locked (resolve choice)";
            }

            int required = Mathf.Max(1, model.ComboBurstRequiredStreakValue);
            int streak = Mathf.Max(0, model.ComboStreak);
            return compact
                ? ("x" + streak + "/" + required)
                : ("Burst locked (Combo x" + streak + "/" + required + ")");
        }

        private bool CanTriggerNextWaveNow()
        {
            return model != null && !model.EventPending && !model.HasDrawChoice && model.IsRestPhase;
        }

        private void TriggerVentReadyPulse(float durationScale)
        {
            if (model == null || !model.CanVentHeat || ventButton == null)
            {
                return;
            }

            float scale = Mathf.Max(0.55f, durationScale);
            float duration = Mathf.Max(0.08f, ventReadyPulseDuration * scale);
            ventReadyPulseTimer = Mathf.Max(ventReadyPulseTimer, duration);
        }

        private void UpdateHeatMeterPulse(float dt)
        {
            if (model == null || heatMeter.FillImage == null)
            {
                return;
            }

            if (overheatSpikePulseTimer > 0f)
            {
                overheatSpikePulseTimer = Mathf.Max(0f, overheatSpikePulseTimer - Mathf.Max(0f, dt));
            }

            if (heatStatePulseTimer > 0f)
            {
                heatStatePulseTimer = Mathf.Max(0f, heatStatePulseTimer - Mathf.Max(0f, dt));
            }

            Color baseColor = model.IsOverheated
                ? HeatDangerColor
                : (model.IsHeatWarning ? HeatWarningColor : HeatSafeColor);
            float pulse01 = 0f;
            if (overheatSpikePulseTimer > 0f)
            {
                float duration = Mathf.Max(0.08f, overheatSpikePulseDuration);
                float t = 1f - Mathf.Clamp01(overheatSpikePulseTimer / duration);
                float wave = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 4f);
                pulse01 = Mathf.Clamp01((0.35f + wave * 0.65f) * Mathf.Clamp01(overheatSpikePulseStrength));
            }

            float statePulse01 = 0f;
            if (heatStatePulseTimer > 0f)
            {
                float duration = Mathf.Max(0.08f, heatStatePulseDuration);
                float t = 1f - Mathf.Clamp01(heatStatePulseTimer / duration);
                statePulse01 = Mathf.Clamp01((0.45f + Mathf.Sin(t * Mathf.PI * 3f) * 0.55f) * Mathf.Clamp01(heatStatePulseStrength));
            }

            Color colorAfterOverheat = Color.Lerp(baseColor, new Color(1f, 0.80f, 0.30f, 1f), pulse01);
            heatMeter.FillImage.color = Color.Lerp(colorAfterOverheat, heatStatePulseColor, statePulse01);
            if (heatMeter.ValueText != null)
            {
                float textPulse = Mathf.Max(pulse01 * 0.85f, statePulse01 * 0.78f);
                Color textTarget = statePulse01 > pulse01
                    ? Color.Lerp(Color.white, heatStatePulseColor, 0.62f)
                    : new Color(1f, 0.93f, 0.66f, 1f);
                heatMeter.ValueText.color = Color.Lerp(Color.white, textTarget, textPulse);
            }
        }

        private void UpdateVentButtonPulse(float dt)
        {
            bool canVentNow = model != null && model.CanVentHeat;
            bool isHeatRiskZone = model != null && (model.IsHeatWarning || model.IsOverheated);
            if (canVentNow && !previousCanVentHeatState && isHeatRiskZone)
            {
                TriggerVentReadyPulse(0.80f);
            }

            previousCanVentHeatState = canVentNow;

            if (ventReadyPulseTimer > 0f)
            {
                ventReadyPulseTimer = Mathf.Max(0f, ventReadyPulseTimer - Mathf.Max(0f, dt));
            }

            if (ventButton == null)
            {
                return;
            }

            if (ventReadyPulseTimer <= 0f)
            {
                ventButton.transform.localScale = Vector3.one;
                return;
            }

            Image image = ventButton.GetComponent<Image>();
            float durationPulse = Mathf.Max(0.08f, ventReadyPulseDuration);
            float tPulse = 1f - Mathf.Clamp01(ventReadyPulseTimer / durationPulse);
            float wave = 0.5f + 0.5f * Mathf.Sin(tPulse * Mathf.PI * 4f);
            float pulse01 = Mathf.Clamp01((0.30f + wave * 0.70f) * Mathf.Clamp01(ventReadyPulseStrength));

            ventButton.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.08f, 1.08f, 1f), pulse01);
            if (image != null)
            {
                Color pulseColor = model != null && model.IsOverheated
                    ? new Color(1f, 0.52f, 0.20f, 1f)
                    : new Color(0.46f, 0.86f, 0.50f, 1f);
                image.color = Color.Lerp(image.color, pulseColor, pulse01 * 0.84f);
            }
        }

        private void UpdateEventPanelPulse(float dt)
        {
            if (eventResolvePanelPulseTimer > 0f)
            {
                eventResolvePanelPulseTimer = Mathf.Max(0f, eventResolvePanelPulseTimer - Mathf.Max(0f, dt));
            }

            if (eventPanel == null)
            {
                return;
            }

            Image panelImage = eventPanel.GetComponent<Image>();
            if (panelImage == null)
            {
                return;
            }

            Color baseColor = new Color(0.06f, 0.08f, 0.12f, 0.96f);
            float pulse01 = 0f;
            if (eventResolvePanelPulseTimer > 0f)
            {
                float duration = Mathf.Max(0.08f, eventResolvePanelPulseDuration);
                float t = 1f - Mathf.Clamp01(eventResolvePanelPulseTimer / duration);
                pulse01 = Mathf.Clamp01(0.45f + Mathf.Sin(t * Mathf.PI * 3f) * 0.55f);
            }

            panelImage.color = Color.Lerp(baseColor, new Color(0.94f, 0.70f, 0.30f, 0.96f), pulse01);
        }

        private void UpdateComboBurstPulse(float dt)
        {
            bool canBurstNow = model != null && model.CanActivateComboBurst;
            if (canBurstNow && !previousCanActivateBurstState)
            {
                comboBurstButtonPulseTimer = Mathf.Max(comboBurstButtonPulseTimer, Mathf.Max(0.08f, comboBurstButtonPulseDuration));
                ShowCueBanner(
                    Application.isMobilePlatform ? "Burst ready. Tap BURST now." : "Burst ready. Press [Space] now.",
                    new Color(0.98f, 0.64f, 0.26f, 1f));
            }

            previousCanActivateBurstState = canBurstNow;

            if (comboBurstButtonPulseTimer > 0f)
            {
                comboBurstButtonPulseTimer = Mathf.Max(0f, comboBurstButtonPulseTimer - Mathf.Max(0f, dt));
            }

            if (comboBurstButton == null)
            {
                return;
            }

            Image image = comboBurstButton.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            if (comboBurstButtonPulseTimer <= 0f)
            {
                comboBurstButton.transform.localScale = Vector3.one;
                return;
            }

            float durationPulse = Mathf.Max(0.08f, comboBurstButtonPulseDuration);
            float tPulse = 1f - Mathf.Clamp01(comboBurstButtonPulseTimer / durationPulse);
            float wave = 0.5f + 0.5f * Mathf.Sin(tPulse * Mathf.PI * 4f);
            float pulse01 = Mathf.Clamp01(0.35f + wave * 0.65f);

            comboBurstButton.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.10f, 1.10f, 1f), pulse01);
            image.color = Color.Lerp(image.color, new Color(0.99f, 0.72f, 0.28f, 1f), pulse01 * 0.90f);
        }

        private void UpdateNextWaveButtonPulse(float dt)
        {
            bool readyNow = CanTriggerNextWaveNow();
            if (readyNow && !previousNextWaveReadyState)
            {
                nextWaveButtonPulseTimer = Mathf.Max(nextWaveButtonPulseTimer, Mathf.Max(0.08f, nextWaveButtonPulseDuration));
                ShowCueBanner(
                    Application.isMobilePlatform ? "Next Wave ready. Tap NEXT WAVE." : "Next Wave ready. Press [N].",
                    new Color(0.36f, 0.74f, 0.96f, 1f));
            }

            previousNextWaveReadyState = readyNow;

            if (nextWaveButtonPulseTimer > 0f)
            {
                nextWaveButtonPulseTimer = Mathf.Max(0f, nextWaveButtonPulseTimer - Mathf.Max(0f, dt));
            }

            if (nextWaveButton == null)
            {
                return;
            }

            if (nextWaveButtonPulseTimer <= 0f)
            {
                nextWaveButton.transform.localScale = Vector3.one;
                return;
            }

            Image image = nextWaveButton.GetComponent<Image>();
            float durationPulse = Mathf.Max(0.08f, nextWaveButtonPulseDuration);
            float tPulse = 1f - Mathf.Clamp01(nextWaveButtonPulseTimer / durationPulse);
            float wave = 0.5f + 0.5f * Mathf.Sin(tPulse * Mathf.PI * 3.6f);
            float pulse01 = Mathf.Clamp01(0.30f + wave * 0.70f);

            nextWaveButton.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.07f, 1.07f, 1f), pulse01);
            if (image != null)
            {
                image.color = Color.Lerp(image.color, new Color(0.34f, 0.74f, 0.96f, 1f), pulse01 * 0.82f);
            }
        }

        private void UpdateProgressionUnlockPulse(float dt)
        {
            if (progressionUnlockPanelPulseTimer > 0f)
            {
                progressionUnlockPanelPulseTimer = Mathf.Max(0f, progressionUnlockPanelPulseTimer - Mathf.Max(0f, dt));
            }

            if (topPanelRect == null)
            {
                return;
            }

            Image panelImage = topPanelRect.GetComponent<Image>();
            if (panelImage == null)
            {
                return;
            }

            float pulse01 = 0f;
            if (progressionUnlockPanelPulseTimer > 0f)
            {
                float duration = Mathf.Max(0.10f, progressionUnlockPanelPulseDuration);
                float t = 1f - Mathf.Clamp01(progressionUnlockPanelPulseTimer / duration);
                pulse01 = Mathf.Clamp01(0.40f + Mathf.Sin(t * Mathf.PI * 3.2f) * 0.60f);
            }

            panelImage.color = Color.Lerp(PanelDark, new Color(0.16f, 0.34f, 0.44f, 0.98f), pulse01);
            if (titleText != null)
            {
                titleText.color = Color.Lerp(Color.white, new Color(0.78f, 0.95f, 1f, 1f), pulse01 * 0.85f);
            }
        }

        private void UpdateSynergyChipPulse(float dt)
        {
            if (synergyChipPulseTimer <= 0f)
            {
                return;
            }

            synergyChipPulseTimer = Mathf.Max(0f, synergyChipPulseTimer - Mathf.Max(0f, dt));
            if (synergyChipPulseTimer <= 0f)
            {
                lastActivatedRecipeName = string.Empty;
            }

            if (synergyContainer != null)
            {
                RefreshSynergyChips();
            }
        }
    }
}
