using UnityEngine;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private const string PrototypeVfxResourcesPath = "FoodTruckPrototype/VFX/";
        private const string PlacementErrorSpriteResourcesPath = "FoodTruckPrototype/Sprites/PlacementErrors/";

        private void TryAutoLoadPrototypeVfx()
        {
            if (!autoLoadPrototypeVfx)
            {
                return;
            }

            vfxPlaceSuccessSprite = vfxPlaceSuccessSprite != null
                ? vfxPlaceSuccessSprite
                : Resources.Load<Sprite>(PrototypeVfxResourcesPath + "place_success_pop");
            vfxPlaceFailSprite = vfxPlaceFailSprite != null
                ? vfxPlaceFailSprite
                : Resources.Load<Sprite>(PrototypeVfxResourcesPath + "place_fail_flash");
            vfxComboBurstSprite = vfxComboBurstSprite != null
                ? vfxComboBurstSprite
                : Resources.Load<Sprite>(PrototypeVfxResourcesPath + "combo_burst");
            vfxOverheatSpikeSprite = vfxOverheatSpikeSprite != null
                ? vfxOverheatSpikeSprite
                : Resources.Load<Sprite>(PrototypeVfxResourcesPath + "overheat_spike");
            vfxHeatWarningSprite = vfxHeatWarningSprite != null
                ? vfxHeatWarningSprite
                : Resources.Load<Sprite>(PrototypeVfxResourcesPath + "heat_warning_ring");
            placementFailNoPendingSprite = placementFailNoPendingSprite != null
                ? placementFailNoPendingSprite
                : Resources.Load<Sprite>(PlacementErrorSpriteResourcesPath + "placement_fail_no_pending");
            placementFailInvalidAnchorSprite = placementFailInvalidAnchorSprite != null
                ? placementFailInvalidAnchorSprite
                : Resources.Load<Sprite>(PlacementErrorSpriteResourcesPath + "placement_fail_invalid_anchor");
            placementFailOutOfBoundsSprite = placementFailOutOfBoundsSprite != null
                ? placementFailOutOfBoundsSprite
                : Resources.Load<Sprite>(PlacementErrorSpriteResourcesPath + "placement_fail_out_of_bounds");
            placementFailOccupiedSprite = placementFailOccupiedSprite != null
                ? placementFailOccupiedSprite
                : Resources.Load<Sprite>(PlacementErrorSpriteResourcesPath + "placement_fail_occupied");
        }

        private Sprite ResolvePlacementFailVfxSprite(string fallbackReason)
        {
            PlacementFailReason reason = model != null
                ? model.LastPlacementFailReason
                : PlacementFailReason.None;
            if (reason == PlacementFailReason.None)
            {
                reason = InferPlacementFailReason(fallbackReason);
            }

            switch (reason)
            {
                case PlacementFailReason.NoPendingBlock:
                    return placementFailNoPendingSprite != null ? placementFailNoPendingSprite : vfxPlaceFailSprite;
                case PlacementFailReason.InvalidAnchor:
                    return placementFailInvalidAnchorSprite != null ? placementFailInvalidAnchorSprite : vfxPlaceFailSprite;
                case PlacementFailReason.OutOfBounds:
                    return placementFailOutOfBoundsSprite != null ? placementFailOutOfBoundsSprite : vfxPlaceFailSprite;
                case PlacementFailReason.Occupied:
                    return placementFailOccupiedSprite != null ? placementFailOccupiedSprite : vfxPlaceFailSprite;
                default:
                    return vfxPlaceFailSprite;
            }
        }

        private static PlacementFailReason InferPlacementFailReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return PlacementFailReason.None;
            }

            string lower = reason.ToLowerInvariant();
            if (lower.Contains("no pending"))
            {
                return PlacementFailReason.NoPendingBlock;
            }

            if (lower.Contains("invalid anchor") || lower.Contains("valid inventory slot"))
            {
                return PlacementFailReason.InvalidAnchor;
            }

            if (lower.Contains("bounds") || lower.Contains("exceeds grid"))
            {
                return PlacementFailReason.OutOfBounds;
            }

            if (lower.Contains("occupied") || lower.Contains("slot is already"))
            {
                return PlacementFailReason.Occupied;
            }

            return PlacementFailReason.None;
        }

        private void TriggerPrototypeVfx(Sprite sprite, Color tint, float sizeScale = 1f, float durationScale = 1f)
        {
            if (prototypeVfxImage == null || prototypeVfxRect == null || sprite == null)
            {
                return;
            }

            float baseSize = Mathf.Max(32f, prototypeVfxSize);
            prototypeVfxImage.sprite = sprite;
            prototypeVfxImage.enabled = true;
            prototypeVfxTint = new Color(tint.r, tint.g, tint.b, 1f);
            prototypeVfxSizeRuntime = baseSize * Mathf.Clamp(sizeScale, 0.55f, 1.8f);
            prototypeVfxDurationRuntime = Mathf.Max(0.08f, prototypeVfxDuration * Mathf.Clamp(durationScale, 0.35f, 2.4f));
            prototypeVfxTimer = prototypeVfxDurationRuntime;
            prototypeVfxRect.sizeDelta = new Vector2(prototypeVfxSizeRuntime, prototypeVfxSizeRuntime);
            prototypeVfxRect.localScale = Vector3.one * 0.72f;
            prototypeVfxImage.color = new Color(prototypeVfxTint.r, prototypeVfxTint.g, prototypeVfxTint.b, 0.92f);
        }

        private void UpdatePrototypeVfx(float dt)
        {
            if (prototypeVfxImage == null || prototypeVfxRect == null)
            {
                return;
            }

            if (prototypeVfxTimer <= 0f)
            {
                Color idle = prototypeVfxImage.color;
                if (idle.a > 0f)
                {
                    idle.a = 0f;
                    prototypeVfxImage.color = idle;
                }

                return;
            }

            prototypeVfxTimer = Mathf.Max(0f, prototypeVfxTimer - Mathf.Max(0f, dt));
            float duration = prototypeVfxDurationRuntime > 0.001f
                ? prototypeVfxDurationRuntime
                : Mathf.Max(0.08f, prototypeVfxDuration);
            float progress = 1f - Mathf.Clamp01(prototypeVfxTimer / duration);
            float alpha = Mathf.Sin(progress * Mathf.PI);
            float overshoot = Mathf.Sin(progress * Mathf.PI * 1.15f);
            float scale = Mathf.Lerp(0.72f, 1.34f, progress) + 0.10f * Mathf.Max(0f, overshoot);

            prototypeVfxRect.localScale = Vector3.one * scale;
            prototypeVfxImage.color = new Color(
                prototypeVfxTint.r,
                prototypeVfxTint.g,
                prototypeVfxTint.b,
                Mathf.Clamp01(alpha) * 0.94f);
        }
    }
}
