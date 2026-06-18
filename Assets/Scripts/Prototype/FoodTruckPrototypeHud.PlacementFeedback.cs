using UnityEngine;
using UnityEngine.UI;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private void TriggerPendingTokenPulse(bool success, bool usePlacementPreset = false)
        {
            pendingTokenPulseSuccess = success;

            float duration = usePlacementPreset && pendingTokenPulseDurationRuntime > 0f
                ? pendingTokenPulseDurationRuntime
                : pendingTokenPulseDuration;

            if (!usePlacementPreset)
            {
                pendingTokenPulseAmplitude = success ? 0.11f : 0.08f;
            }

            pendingTokenPulseTimer = Mathf.Max(0.05f, duration);
        }

        private void UpdatePendingPlacementFeedback(float dt)
        {
            float delta = Mathf.Max(0f, dt);

            if (pendingPlacementFeedbackTimer > 0f)
            {
                pendingPlacementFeedbackTimer = Mathf.Max(0f, pendingPlacementFeedbackTimer - delta);
                PaintInventoryCells(pendingHoverAnchorCell, pendingHoverValid);

                float feedbackDuration = pendingPlacementFeedbackDurationRuntime > 0f
                    ? pendingPlacementFeedbackDurationRuntime
                    : pendingPlacementFeedbackDuration;
                float ratio = feedbackDuration > 0.001f
                    ? Mathf.Clamp01(pendingPlacementFeedbackTimer / feedbackDuration)
                    : 0f;
                float wave = Mathf.Sin((1f - ratio) * Mathf.PI);
                float amplitude = Mathf.Max(0f, pendingPlacementFeedbackAmplitude);
                float scale = 1f + amplitude * wave;

                for (int i = 0; i < pendingPlacementFeedbackCells.Length; i++)
                {
                    int cell = pendingPlacementFeedbackCells[i];
                    if (cell < 0 || cell >= inventoryCellRects.Count)
                    {
                        continue;
                    }

                    RectTransform rect = inventoryCellRects[cell];
                    if (rect != null)
                    {
                        Vector3 baseScale = cell < inventoryCellBaseScales.Length && inventoryCellBaseScales[cell] != Vector3.zero
                            ? inventoryCellBaseScales[cell]
                            : Vector3.one;
                        rect.localScale = baseScale * scale;
                    }
                }
            }
            else
            {
                for (int i = 0; i < inventoryCellRects.Count; i++)
                {
                    RectTransform rect = inventoryCellRects[i];
                    if (rect == null)
                    {
                        continue;
                    }

                    Vector3 baseScale = i < inventoryCellBaseScales.Length && inventoryCellBaseScales[i] != Vector3.zero
                        ? inventoryCellBaseScales[i]
                        : Vector3.one;
                    rect.localScale = baseScale;
                }
            }

            if (pendingTokenPulseTimer > 0f)
            {
                pendingTokenPulseTimer = Mathf.Max(0f, pendingTokenPulseTimer - delta);
                if (pendingTokenRect != null)
                {
                    float tokenDuration = pendingTokenPulseDurationRuntime > 0f
                        ? pendingTokenPulseDurationRuntime
                        : pendingTokenPulseDuration;
                    float ratio = tokenDuration > 0.001f
                        ? Mathf.Clamp01(pendingTokenPulseTimer / tokenDuration)
                        : 0f;
                    float wave = Mathf.Sin((1f - ratio) * Mathf.PI);
                    float amplitude = Mathf.Max(0f, pendingTokenPulseAmplitude);
                    float scale = 1f + amplitude * wave;
                    pendingTokenRect.localScale = pendingTokenBaseScale * scale;
                }
            }
            else if (pendingTokenRect != null)
            {
                pendingTokenRect.localScale = pendingTokenBaseScale;
            }
        }

        private void UpdateRecommendationPulse(float dt)
        {
            if (model == null || !model.HasPendingBlock)
            {
                recommendationPulseTimer = 0f;
                recommendationPulseAnchorCell = -1;
                lastRecommendedPrimaryAnchor = -1;
                recommendationAssistWasLocked = false;
                recommendationAssistLocked = false;
                recommendationAssistLockAnchorCell = -1;
                recommendationAssistFlashTimer = 0f;
                recommendationHapticCooldownTimer = 0f;
                if (pendingRecommendationAssistText != null)
                {
                    pendingRecommendationAssistText.rectTransform.localScale = Vector3.one;
                }

                return;
            }

            float delta = Mathf.Max(0f, dt);
            if (recommendationPulseTimer > 0f)
            {
                recommendationPulseTimer = Mathf.Max(0f, recommendationPulseTimer - delta);
            }

            if (recommendationHapticCooldownTimer > 0f)
            {
                recommendationHapticCooldownTimer = Mathf.Max(0f, recommendationHapticCooldownTimer - delta);
            }

            if (recommendationAssistFlashTimer > 0f)
            {
                recommendationAssistFlashTimer = Mathf.Max(0f, recommendationAssistFlashTimer - delta);
                if (pendingRecommendationAssistText != null)
                {
                    float ratio = recommendationAssistFlashDuration > 0.001f
                        ? Mathf.Clamp01(recommendationAssistFlashTimer / recommendationAssistFlashDuration)
                        : 0f;
                    float pulse = Mathf.Sin((1f - ratio) * Mathf.PI);
                    pendingRecommendationAssistText.rectTransform.localScale = Vector3.one * (1f + 0.12f * pulse);
                }
            }
            else if (pendingRecommendationAssistText != null)
            {
                pendingRecommendationAssistText.rectTransform.localScale = Vector3.one;
            }
        }

        private void UpdatePlacementImpact(float dt)
        {
            if (placementImpactImage == null)
            {
                return;
            }

            if (placementImpactTimer > 0f)
            {
                placementImpactTimer = Mathf.Max(0f, placementImpactTimer - Mathf.Max(0f, dt));
                float duration = placementImpactDurationRuntime > 0f
                    ? placementImpactDurationRuntime
                    : placementImpactDuration;
                float ratio = duration > 0.001f
                    ? Mathf.Clamp01(placementImpactTimer / duration)
                    : 0f;
                float pulseCycles = placementImpactSuccess ? 1f : 1.65f;
                float pulse = Mathf.Max(0f, Mathf.Sin((1f - ratio) * Mathf.PI * pulseCycles));
                float baseAlpha = (placementImpactSuccess ? placementImpactSuccessAlpha : placementImpactFailAlpha) * placementImpactAlphaScale;
                Color color = placementImpactSuccess ? placementImpactSuccessColor : placementImpactFailColor;
                color.a = baseAlpha * (0.35f + 0.65f * pulse);
                placementImpactImage.color = color;
                return;
            }

            Color idle = placementImpactImage.color;
            if (idle.a > 0f)
            {
                idle.a = 0f;
                placementImpactImage.color = idle;
            }
        }

        private void UpdatePlacementFailShake(float dt)
        {
            RectTransform shakeTarget = GetInventoryGridShakeTarget();
            if (shakeTarget == null)
            {
                return;
            }

            if (placementFailShakeTimer <= 0f)
            {
                // Avoid continuously overwriting base while idle; idle alignment logic uses this base to detect drift.
                if (!inventoryGridHolderBaseAnchoredPositionCached)
                {
                    inventoryGridHolderBaseAnchoredPosition = ResolveInventoryGridRestAnchoredPosition(shakeTarget);
                    inventoryGridHolderBaseAnchoredPositionCached = true;
                    if ((shakeTarget.anchoredPosition - inventoryGridHolderBaseAnchoredPosition).sqrMagnitude > GetInventoryLayoutPositionEpsilonSqr())
                    {
                        shakeTarget.anchoredPosition = inventoryGridHolderBaseAnchoredPosition;
                    }
                }

                return;
            }

            if (!inventoryGridHolderBaseAnchoredPositionCached)
            {
                inventoryGridHolderBaseAnchoredPosition = ResolveInventoryGridRestAnchoredPosition(shakeTarget);
                inventoryGridHolderBaseAnchoredPositionCached = true;
            }

            placementFailShakeTimer = Mathf.Max(0f, placementFailShakeTimer - Mathf.Max(0f, dt));
            float shakeDuration = placementFailShakeDurationRuntime > 0f
                ? placementFailShakeDurationRuntime
                : placementFailShakeDuration;
            float shakeDistance = placementFailShakeDistanceRuntime > 0f
                ? placementFailShakeDistanceRuntime
                : placementFailShakeDistance;

            float ratio = shakeDuration > 0.001f ? Mathf.Clamp01(placementFailShakeTimer / shakeDuration) : 0f;
            float decay = ratio * ratio;
            float oscillation = Mathf.Sin((1f - ratio) * Mathf.PI * Mathf.Max(1f, placementFailShakeFrequency));
            float xOffset = oscillation * placementFailShakeDirection * Mathf.Max(0f, shakeDistance) * decay;
            shakeTarget.anchoredPosition = inventoryGridHolderBaseAnchoredPosition + new Vector2(xOffset, 0f);

            if (placementFailShakeTimer <= 0f)
            {
                shakeTarget.anchoredPosition = inventoryGridHolderBaseAnchoredPosition;
            }
        }

        private PlacementFeedbackPreset ResolvePlacementFeedbackPreset()
        {
            if (!adaptivePlacementFeedbackPreset || model == null)
            {
                return placementFeedbackPreset;
            }

            if (model.Wave <= 3)
            {
                return PlacementFeedbackPreset.Punchy;
            }

            if (model.IsOverheated)
            {
                return PlacementFeedbackPreset.Soft;
            }

            return placementFeedbackPreset;
        }

        private void ConfigurePlacementFeedbackPreset(bool success)
        {
            PlacementFeedbackPreset preset = ResolvePlacementFeedbackPreset();

            switch (preset)
            {
                case PlacementFeedbackPreset.Soft:
                    pendingPlacementFeedbackAmplitude = success ? 0.09f : 0.06f;
                    pendingPlacementFeedbackDurationRuntime = pendingPlacementFeedbackDuration * (success ? 1.05f : 0.95f);
                    pendingTokenPulseAmplitude = success ? 0.08f : 0.06f;
                    pendingTokenPulseDurationRuntime = pendingTokenPulseDuration * 0.92f;
                    placementImpactDurationRuntime = placementImpactDuration * 0.90f;
                    placementImpactAlphaScale = success ? 0.80f : 0.92f;
                    placementFailShakeDurationRuntime = placementFailShakeDuration * 0.82f;
                    placementFailShakeDistanceRuntime = placementFailShakeDistance * 0.74f;
                    break;
                case PlacementFeedbackPreset.Punchy:
                    pendingPlacementFeedbackAmplitude = success ? 0.17f : 0.11f;
                    pendingPlacementFeedbackDurationRuntime = pendingPlacementFeedbackDuration * (success ? 1.12f : 1.00f);
                    pendingTokenPulseAmplitude = success ? 0.14f : 0.10f;
                    pendingTokenPulseDurationRuntime = pendingTokenPulseDuration * 1.10f;
                    placementImpactDurationRuntime = placementImpactDuration * 1.05f;
                    placementImpactAlphaScale = success ? 1.12f : 1.26f;
                    placementFailShakeDurationRuntime = placementFailShakeDuration * 1.04f;
                    placementFailShakeDistanceRuntime = placementFailShakeDistance * 1.24f;
                    break;
                default:
                    pendingPlacementFeedbackAmplitude = success ? 0.12f : 0.08f;
                    pendingPlacementFeedbackDurationRuntime = pendingPlacementFeedbackDuration;
                    pendingTokenPulseAmplitude = success ? 0.11f : 0.08f;
                    pendingTokenPulseDurationRuntime = pendingTokenPulseDuration;
                    placementImpactDurationRuntime = placementImpactDuration;
                    placementImpactAlphaScale = 1f;
                    placementFailShakeDurationRuntime = placementFailShakeDuration;
                    placementFailShakeDistanceRuntime = placementFailShakeDistance;
                    break;
            }
        }
    }
}
