using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private void EnsureUndeadSpriteSetsLoaded()
        {
            if (undeadSpriteSetsLoaded)
            {
                return;
            }

            undeadSpriteSetsLoaded = true;
            undeadEnemySpriteSets.Clear();
            undeadSpecialSpriteSets.Clear();

            for (int i = 0; i < 5; i++)
            {
                TryAddUndeadSpriteSet(undeadEnemySpriteSets, "Enemy " + i);
            }

            for (int i = 0; i < 4; i++)
            {
                TryAddUndeadSpriteSet(undeadSpecialSpriteSets, "Farmer " + i);
            }
        }

        private void TryAddUndeadSpriteSet(List<UndeadSpriteSet> target, string sheetName)
        {
            Sprite[] sprites = LoadUndeadSheetSprites(sheetName);
            if (sprites == null || sprites.Length == 0)
            {
                return;
            }

            UndeadSpriteSet set = new UndeadSpriteSet();
            set.RunFrames = ExtractSpritesByPrefix(sprites, "Run");
            if (set.RunFrames == null || set.RunFrames.Length == 0)
            {
                set.RunFrames = sprites;
            }

            set.HitFrame = FindSpriteByPrefix(sprites, "Hit");
            set.DeadFrame = FindSpriteByPrefix(sprites, "Dead");
            target.Add(set);
        }

        private UndeadSpriteSet GetUndeadSpriteSet(LaneEnemyState enemy)
        {
            if (enemy == null)
            {
                return null;
            }

            return GetUndeadSpriteSet(enemy.Id, enemy.IsSpecial);
        }

        private UndeadSpriteSet GetUndeadSpriteSet(int enemyId, bool isSpecial)
        {
            EnsureUndeadSpriteSetsLoaded();

            List<UndeadSpriteSet> pool = isSpecial
                ? undeadSpecialSpriteSets
                : undeadEnemySpriteSets;

            if (pool.Count == 0)
            {
                pool = isSpecial
                    ? undeadEnemySpriteSets
                    : undeadSpecialSpriteSets;
            }

            if (pool.Count == 0)
            {
                return null;
            }

            int index = Mathf.Abs(enemyId) % pool.Count;
            return pool[index];
        }

        private static Sprite[] ExtractSpritesByPrefix(Sprite[] source, string prefix)
        {
            List<Sprite> matches = new List<Sprite>();
            for (int i = 0; i < source.Length; i++)
            {
                Sprite sprite = source[i];
                if (sprite == null || string.IsNullOrEmpty(sprite.name))
                {
                    continue;
                }

                if (sprite.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(sprite);
                }
            }

            if (matches.Count == 0)
            {
                return null;
            }

            Sprite[] result = matches.ToArray();
            Array.Sort(result, CompareSpriteNameByTrailingNumber);
            return result;
        }

        private static Sprite FindSpriteByPrefix(Sprite[] source, string prefix)
        {
            for (int i = 0; i < source.Length; i++)
            {
                Sprite sprite = source[i];
                if (sprite != null && !string.IsNullOrEmpty(sprite.name) &&
                    sprite.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return sprite;
                }
            }

            return null;
        }

        private static int CompareSpriteNameByTrailingNumber(Sprite left, Sprite right)
        {
            int leftNumber = ExtractTrailingNumber(left != null ? left.name : string.Empty);
            int rightNumber = ExtractTrailingNumber(right != null ? right.name : string.Empty);
            if (leftNumber != rightNumber)
            {
                return leftNumber.CompareTo(rightNumber);
            }

            string leftName = left != null ? left.name : string.Empty;
            string rightName = right != null ? right.name : string.Empty;
            return string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
        }

        private static int ExtractTrailingNumber(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return int.MaxValue;
            }

            int end = value.Length - 1;
            while (end >= 0 && !char.IsDigit(value[end]))
            {
                end--;
            }

            if (end < 0)
            {
                return int.MaxValue;
            }

            int start = end;
            while (start >= 0 && char.IsDigit(value[start]))
            {
                start--;
            }

            string number = value.Substring(start + 1, end - start);
            if (int.TryParse(number, out int parsed))
            {
                return parsed;
            }

            return int.MaxValue;
        }

        private Sprite[] LoadUndeadSheetSprites(string sheetName)
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                return null;
            }

            Sprite[] resourcesSprites = Resources.LoadAll<Sprite>("Undead Survivor/Sprites/" + sheetName);
            if (resourcesSprites != null && resourcesSprites.Length > 0)
            {
                return resourcesSprites;
            }

#if UNITY_EDITOR
            string assetPath = "Assets/Resources/Undead Survivor/Sprites/" + sheetName + ".png";
            UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets != null && assets.Length > 0)
            {
                List<Sprite> sprites = new List<Sprite>();
                for (int i = 0; i < assets.Length; i++)
                {
                    Sprite sprite = assets[i] as Sprite;
                    if (sprite != null)
                    {
                        sprites.Add(sprite);
                    }
                }

                if (sprites.Count > 0)
                {
                    Sprite[] result = sprites.ToArray();
                    Array.Sort(result, CompareSpriteNameByTrailingNumber);
                    return result;
                }
            }
#endif

            return null;
        }

        private void HandleEnemyVisualRemoval(EnemyVisualWidget widget, bool truckDamagedThisFrame)
        {
            if (widget == null || widget.Rect == null)
            {
                return;
            }

            bool likelyReachedTruck = truckDamagedThisFrame && widget.LastDistance01 <= 0.18f;
            if (likelyReachedTruck)
            {
                TriggerLaneHitFlash(widget.LaneIndex);
                SpawnEnemyLeakFloater(widget);
                return;
            }

            UndeadSpriteSet spriteSet = GetUndeadSpriteSet(widget.EnemyId, widget.IsSpecial);
            Sprite deadSprite = spriteSet != null && spriteSet.DeadFrame != null
                ? spriteSet.DeadFrame
                : widget.Icon.sprite;

            if (!widget.KnockoutFloaterSpawned)
            {
                SpawnEnemyAttackTrail(widget, true);
                SpawnEnemyHitEffect(widget, true);
                SpawnEnemyDamageFloater(widget, Mathf.Max(1f, widget.LastHp), true);
                widget.KnockoutFloaterSpawned = true;
            }
            SpawnEnemyAfterVisual(widget, deadSprite);
        }

        private void SpawnEnemyAfterVisual(EnemyVisualWidget source, Sprite sprite)
        {
            if (source == null || source.Rect == null)
            {
                return;
            }

            RectTransform laneRoot = laneTrackRoots[source.LaneIndex];
            if (laneRoot == null)
            {
                return;
            }

            RectTransform rect = new GameObject("EnemyAfter", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.transform.SetParent(laneRoot, false);
            rect.anchorMin = source.Rect.anchorMin;
            rect.anchorMax = source.Rect.anchorMax;
            rect.pivot = source.Rect.pivot;
            rect.anchoredPosition = source.Rect.anchoredPosition;
            rect.sizeDelta = source.Rect.sizeDelta;

            Image icon = rect.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.sprite = sprite;
            if (sprite != null)
            {
                icon.color = Color.white;
            }
            else if (source.Icon != null)
            {
                icon.color = source.Icon.color;
            }
            else
            {
                icon.color = new Color(0.72f, 0.72f, 0.72f, 0.95f);
            }

            EnemyAfterVisualWidget after = new EnemyAfterVisualWidget();
            after.Rect = rect;
            after.Icon = icon;
            after.FadeDuration = Mathf.Max(0.08f, enemyDeathFadeSeconds);
            after.Remaining = Mathf.Max(0.02f, enemyDeathHoldSeconds) + after.FadeDuration;
            enemyAfterVisuals.Add(after);
        }

        private void TriggerLaneHitFlash(int laneIndex)
        {
            if (laneIndex < 0 || laneIndex >= laneHitFlashTimers.Length)
            {
                return;
            }

            laneHitFlashTimers[laneIndex] = Mathf.Max(laneHitFlashTimers[laneIndex], laneHitFlashDuration);
        }

        private void ClearTransientCombatVisuals()
        {
            for (int i = enemyAfterVisuals.Count - 1; i >= 0; i--)
            {
                EnemyAfterVisualWidget after = enemyAfterVisuals[i];
                if (after != null && after.Rect != null)
                {
                    Destroy(after.Rect.gameObject);
                }
            }

            enemyAfterVisuals.Clear();

            for (int i = enemyHitEffects.Count - 1; i >= 0; i--)
            {
                EnemyHitEffectWidget effect = enemyHitEffects[i];
                if (effect != null && effect.Rect != null)
                {
                    Destroy(effect.Rect.gameObject);
                }
            }

            enemyHitEffects.Clear();

            for (int i = combatFloatingTexts.Count - 1; i >= 0; i--)
            {
                CombatFloatingTextWidget floating = combatFloatingTexts[i];
                if (floating != null && floating.Rect != null)
                {
                    Destroy(floating.Rect.gameObject);
                }
            }

            combatFloatingTexts.Clear();

            for (int i = 0; i < laneHitFlashTimers.Length; i++)
            {
                laneHitFlashTimers[i] = 0f;
            }
        }

        private void UpdateLaneHitFlashVisuals(float dt)
        {
            for (int i = 0; i < laneHitFlashTimers.Length; i++)
            {
                if (laneHitFlashTimers[i] > 0f)
                {
                    laneHitFlashTimers[i] = Mathf.Max(0f, laneHitFlashTimers[i] - Mathf.Max(0f, dt));
                }

                Image laneImage = laneTrackImages[i];
                if (laneImage == null)
                {
                    continue;
                }

                float t = laneHitFlashDuration > 0.001f
                    ? Mathf.Clamp01(laneHitFlashTimers[i] / laneHitFlashDuration)
                    : 0f;
                laneImage.color = Color.Lerp(laneBaseColor, laneHitFlashColor, t);
            }

            RefreshFoodTruckMarkers();
        }

        private void RefreshFoodTruckMarkers()
        {
            float hp01 = model != null && model.MaxTruckHp > 0f
                ? Mathf.Clamp01(model.TruckHp / model.MaxTruckHp)
                : 1f;

            for (int i = 0; i < laneTruckImages.Length; i++)
            {
                Image truckImage = laneTruckImages[i];
                RectTransform lane = laneTrackRoots[i];
                Text truckText = laneTruckTexts[i];
                if (truckImage == null || lane == null)
                {
                    continue;
                }

                bool mainTruckLane = i == 1;
                truckImage.gameObject.SetActive(mainTruckLane);
                if (truckText != null)
                {
                    truckText.gameObject.SetActive(false);
                }

                if (!mainTruckLane)
                {
                    continue;
                }

                RectTransform truckRect = truckImage.rectTransform;
                float laneHeight = Mathf.Max(1f, lane.rect.height);
                bool hasTruckSprite = foodTruckSprite != null;
                float width = hasTruckSprite
                    ? Mathf.Clamp(laneHeight * 2.10f, 128f, 280f)
                    : Mathf.Clamp(laneHeight * 1.35f, 96f, 220f);
                truckRect.anchorMin = new Vector2(0f, 0.05f);
                truckRect.anchorMax = new Vector2(0f, 0.95f);
                truckRect.pivot = new Vector2(0f, 0.5f);
                truckRect.anchoredPosition = new Vector2(12f, 0f);
                truckRect.sizeDelta = new Vector2(width, 0f);

                Color healthy = new Color(0.95f, 0.72f, 0.34f, 0.97f);
                Color damaged = new Color(0.94f, 0.34f, 0.22f, 0.98f);
                truckImage.sprite = foodTruckSprite;
                truckImage.preserveAspect = hasTruckSprite;
                truckImage.color = hasTruckSprite
                    ? Color.Lerp(new Color(1f, 0.44f, 0.36f, 1f), Color.white, hp01)
                    : Color.Lerp(damaged, healthy, hp01);

                if (truckText != null)
                {
                    truckText.gameObject.SetActive(!hasTruckSprite);
                    truckText.fontSize = Mathf.Clamp(Mathf.RoundToInt(width * 0.24f), 9, 15);
                    truckText.text = "FOOD\nTRUCK";
                }
            }
        }

        private void UpdateEnemyAfterVisuals(float dt)
        {
            float delta = Mathf.Max(0f, dt);
            for (int i = enemyAfterVisuals.Count - 1; i >= 0; i--)
            {
                EnemyAfterVisualWidget after = enemyAfterVisuals[i];
                if (after == null || after.Rect == null)
                {
                    enemyAfterVisuals.RemoveAt(i);
                    continue;
                }

                after.Remaining -= delta;
                if (after.Remaining <= 0f)
                {
                    Destroy(after.Rect.gameObject);
                    enemyAfterVisuals.RemoveAt(i);
                    continue;
                }

                if (after.Icon == null)
                {
                    continue;
                }

                float alpha = 1f;
                if (after.Remaining < after.FadeDuration)
                {
                    alpha = Mathf.Clamp01(after.Remaining / Mathf.Max(0.001f, after.FadeDuration));
                }

                Color c = after.Icon.color;
                c.a = alpha;
                after.Icon.color = c;
            }
        }

        private void SpawnEnemyHitEffect(EnemyVisualWidget source)
        {
            SpawnEnemyHitEffect(source, false);
        }

        private void SpawnEnemyHitEffect(EnemyVisualWidget source, bool knockout)
        {
            if (source == null || source.Rect == null || source.LaneIndex < 0 || source.LaneIndex >= laneTrackRoots.Length)
            {
                return;
            }

            RectTransform laneRoot = laneTrackRoots[source.LaneIndex];
            if (laneRoot == null)
            {
                return;
            }

            float punch = knockout ? 1.22f : 1f;
            SpawnEnemyHitSlash(laneRoot, source.Rect.anchoredPosition, 0f, 1.08f * punch, new Color(1f, 0.96f, 0.18f, 1f));
            SpawnEnemyHitSlash(laneRoot, source.Rect.anchoredPosition, 52f, 0.80f * punch, new Color(1f, 0.55f, 0.16f, 0.96f));
            SpawnEnemyHitSlash(laneRoot, source.Rect.anchoredPosition, -48f, 0.66f * punch, new Color(1f, 1f, 1f, 0.88f));
            SpawnEnemyHitImpactCore(laneRoot, source.Rect.anchoredPosition, punch);
        }

        private void SpawnEnemyHitSlash(RectTransform laneRoot, Vector2 center, float angleOffset, float scale, Color color)
        {
            if (laneRoot == null)
            {
                return;
            }

            RectTransform rect = new GameObject("EnemyHitFx", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.transform.SetParent(laneRoot, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            float laneHeight = Mathf.Max(1f, laneRoot.rect.height);
            float width = Mathf.Max(enemyHitEffectSize, laneHeight * 0.88f * Mathf.Clamp(scale, 0.35f, 1.2f));
            rect.sizeDelta = new Vector2(width, Mathf.Clamp(width * 0.24f, 22f, 70f));
            rect.localRotation = Quaternion.Euler(0f, 0f, angleOffset + UnityEngine.Random.Range(-10f, 10f));
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();

            Image image = rect.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = color;

            Vector2 direction = new Vector2(UnityEngine.Random.Range(-0.18f, 0.18f), UnityEngine.Random.Range(0.12f, 0.36f)).normalized;
            EnemyHitEffectWidget effect = new EnemyHitEffectWidget();
            effect.Rect = rect;
            effect.Image = image;
            effect.Duration = Mathf.Max(0.12f, enemyHitEffectDuration * 1.12f);
            effect.Remaining = effect.Duration;
            effect.Velocity = direction * Mathf.Max(4f, enemyHitEffectTravelSpeed * 0.26f);
            enemyHitEffects.Add(effect);
        }

        private void SpawnEnemyAttackTrail(EnemyVisualWidget source)
        {
            SpawnEnemyAttackTrail(source, false);
        }

        private void SpawnEnemyAttackTrail(EnemyVisualWidget source, bool knockout)
        {
            if (source == null || source.Rect == null || source.LaneIndex < 0 || source.LaneIndex >= laneTrackRoots.Length)
            {
                return;
            }

            RectTransform laneRoot = laneTrackRoots[source.LaneIndex];
            if (laneRoot == null)
            {
                return;
            }

            float laneHeight = Mathf.Max(1f, laneRoot.rect.height);
            float truckEndX = foodTruckSprite != null
                ? Mathf.Clamp(laneHeight * 1.66f, 118f, 252f)
                : Mathf.Clamp(laneHeight * 0.82f, 64f, 136f);
            Vector2 target = source.Rect.anchoredPosition;
            float width = Mathf.Max(laneHeight * 0.58f, target.x - truckEndX);
            float y = target.y + UnityEngine.Random.Range(-3f, 3f);
            float startX = Mathf.Min(truckEndX, target.x - width);
            float angle = UnityEngine.Random.Range(-4f, 4f);
            float punch = knockout ? 1.22f : 1f;

            SpawnEnemyAttackTrailSegment(laneRoot, startX, y, width, laneHeight, angle, 0.23f * punch, new Color(0.22f, 0.08f, 0.02f, 0.68f), 1.08f);
            SpawnEnemyAttackTrailSegment(laneRoot, startX, y, width, laneHeight, angle, 0.14f * punch, new Color(1f, 0.72f, 0.10f, 0.96f), 1.00f);
            SpawnEnemyAttackTrailSegment(laneRoot, startX + width * 0.18f, y, width * 0.62f, laneHeight, angle, 0.07f * punch, new Color(1f, 1f, 0.72f, 0.96f), 0.86f);
            SpawnEnemyAttackArrowhead(laneRoot, target, laneHeight, knockout);
        }

        private void SpawnEnemyAttackTrailSegment(
            RectTransform laneRoot,
            float startX,
            float y,
            float width,
            float laneHeight,
            float angle,
            float heightRatio,
            Color color,
            float durationScale)
        {
            RectTransform rect = new GameObject("EnemyAttackTrail", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.transform.SetParent(laneRoot, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(startX, y);
            rect.sizeDelta = new Vector2(width, Mathf.Clamp(laneHeight * heightRatio, 8f, 54f));
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            rect.localScale = Vector3.one;

            Image image = rect.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = color;
            rect.SetAsLastSibling();

            EnemyHitEffectWidget effect = new EnemyHitEffectWidget();
            effect.Rect = rect;
            effect.Image = image;
            effect.Duration = Mathf.Max(0.08f, enemyHitEffectDuration * durationScale);
            effect.Remaining = effect.Duration;
            effect.Velocity = new Vector2(Mathf.Max(12f, enemyHitEffectTravelSpeed * 0.18f), 0f);
            enemyHitEffects.Add(effect);
        }

        private void SpawnEnemyAttackArrowhead(RectTransform laneRoot, Vector2 target, float laneHeight, bool knockout)
        {
            RectTransform rect = new GameObject("EnemyAttackArrowhead", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.transform.SetParent(laneRoot, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = target + new Vector2(-Mathf.Clamp(laneHeight * 0.10f, 5f, 14f), 0f);
            float size = Mathf.Clamp(laneHeight * (knockout ? 0.28f : 0.22f), 18f, 52f);
            rect.sizeDelta = new Vector2(size, size);
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            rect.localScale = Vector3.one;

            Image image = rect.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = knockout
                ? new Color(1f, 0.96f, 0.20f, 0.98f)
                : new Color(1f, 0.70f, 0.12f, 0.92f);
            rect.SetAsLastSibling();

            EnemyHitEffectWidget effect = new EnemyHitEffectWidget();
            effect.Rect = rect;
            effect.Image = image;
            effect.Duration = Mathf.Max(0.08f, enemyHitEffectDuration * (knockout ? 1.14f : 0.96f));
            effect.Remaining = effect.Duration;
            effect.Velocity = new Vector2(Mathf.Max(4f, enemyHitEffectTravelSpeed * 0.08f), Mathf.Max(3f, enemyHitEffectTravelSpeed * 0.05f));
            enemyHitEffects.Add(effect);
        }

        private void SpawnEnemyHitImpactCore(RectTransform laneRoot, Vector2 center, float scale)
        {
            RectTransform rect = new GameObject("EnemyHitImpactCore", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.transform.SetParent(laneRoot, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            float laneHeight = Mathf.Max(1f, laneRoot.rect.height);
            float size = Mathf.Clamp(laneHeight * 0.34f * Mathf.Clamp(scale, 0.6f, 1.5f), 22f, 68f);
            rect.sizeDelta = new Vector2(size, size);
            rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            rect.localScale = Vector3.one;

            Image image = rect.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = new Color(1f, 0.94f, 0.26f, 0.94f);
            rect.SetAsLastSibling();

            EnemyHitEffectWidget effect = new EnemyHitEffectWidget();
            effect.Rect = rect;
            effect.Image = image;
            effect.Duration = Mathf.Max(0.10f, enemyHitEffectDuration * 0.76f);
            effect.Remaining = effect.Duration;
            effect.Velocity = Vector2.zero;
            enemyHitEffects.Add(effect);
        }

        private void SpawnEnemyDamageFloater(EnemyVisualWidget source, float damageAmount, bool knockout)
        {
            if (source == null || source.Rect == null || source.LaneIndex < 0 || source.LaneIndex >= laneTrackRoots.Length)
            {
                return;
            }

            RectTransform laneRoot = laneTrackRoots[source.LaneIndex];
            if (laneRoot == null)
            {
                return;
            }

            float laneHeight = Mathf.Max(1f, laneRoot.rect.height);
            string label = BuildEnemyDamageFloaterLabel(damageAmount, knockout);
            Color color = knockout
                ? new Color(1f, 0.94f, 0.42f, 1f)
                : new Color(1f, 0.64f, 0.28f, 1f);
            int fontSize = knockout
                ? Mathf.Clamp(Mathf.RoundToInt(laneHeight * 0.29f), 19, 38)
                : Mathf.Clamp(Mathf.RoundToInt(laneHeight * 0.25f), 17, 34);
            Vector2 position = source.Rect.anchoredPosition + new Vector2(0f, Mathf.Clamp(laneHeight * 0.30f, 20f, 62f));
            SpawnCombatFloatingText(laneRoot, position, label, color, fontSize, 1.12f);
        }

        private void SpawnEnemyLeakFloater(EnemyVisualWidget source)
        {
            if (source == null || source.Rect == null || source.LaneIndex < 0 || source.LaneIndex >= laneTrackRoots.Length)
            {
                return;
            }

            RectTransform laneRoot = laneTrackRoots[source.LaneIndex];
            if (laneRoot == null)
            {
                return;
            }

            float laneHeight = Mathf.Max(1f, laneRoot.rect.height);
            Vector2 position = source.Rect.anchoredPosition + new Vector2(0f, Mathf.Clamp(laneHeight * 0.24f, 18f, 54f));
            SpawnCombatFloatingText(
                laneRoot,
                position,
                "LEAK",
                new Color(1f, 0.32f, 0.22f, 1f),
                Mathf.Clamp(Mathf.RoundToInt(laneHeight * 0.27f), 18, 36),
                1.18f);
        }

        private void SpawnTruckDamageFloater(float damageAmount, string causeLabel)
        {
            if (laneTrackRoots.Length < 2)
            {
                return;
            }

            RectTransform laneRoot = laneTrackRoots[1];
            if (laneRoot == null)
            {
                return;
            }

            float laneHeight = Mathf.Max(1f, laneRoot.rect.height);
            float truckX = foodTruckSprite != null
                ? Mathf.Clamp(laneHeight * 1.16f, 86f, 190f)
                : Mathf.Clamp(laneHeight * 0.58f, 52f, 112f);
            SpawnCombatFloatingText(
                laneRoot,
                new Vector2(truckX, Mathf.Clamp(laneHeight * 0.22f, 18f, 52f)),
                BuildTruckDamageFloaterLabel(causeLabel, damageAmount),
                new Color(1f, 0.40f, 0.26f, 1f),
                Mathf.Clamp(Mathf.RoundToInt(laneHeight * 0.25f), 18, 34),
                1.22f);
        }

        private void SpawnCombatFloatingText(
            RectTransform laneRoot,
            Vector2 anchoredPosition,
            string label,
            Color color,
            int fontSize,
            float durationScale)
        {
            if (laneRoot == null || string.IsNullOrEmpty(label))
            {
                return;
            }

            float laneHeight = Mathf.Max(1f, laneRoot.rect.height);
            Text text = CreateText(laneRoot, "CombatFloatText", fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, color);
            text.raycastTarget = false;
            text.text = label;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(Mathf.Clamp(laneHeight * 1.90f, 128f, 300f), Mathf.Clamp(laneHeight * 0.48f, 38f, 92f));
            rect.SetAsLastSibling();

            CombatFloatingTextWidget floating = new CombatFloatingTextWidget();
            floating.Rect = rect;
            floating.Text = text;
            floating.Duration = Mathf.Max(0.12f, combatFloatingTextDuration * Mathf.Max(0.2f, durationScale));
            floating.Remaining = floating.Duration;
            floating.Velocity = new Vector2(
                UnityEngine.Random.Range(-combatFloatingTextSideDrift, combatFloatingTextSideDrift),
                Mathf.Max(6f, combatFloatingTextRiseSpeed));
            combatFloatingTexts.Add(floating);
        }

        private void UpdateCombatFloatingTexts(float dt)
        {
            float delta = Mathf.Max(0f, dt);
            for (int i = combatFloatingTexts.Count - 1; i >= 0; i--)
            {
                CombatFloatingTextWidget floating = combatFloatingTexts[i];
                if (floating == null || floating.Rect == null)
                {
                    combatFloatingTexts.RemoveAt(i);
                    continue;
                }

                floating.Remaining -= delta;
                if (floating.Remaining <= 0f)
                {
                    Destroy(floating.Rect.gameObject);
                    combatFloatingTexts.RemoveAt(i);
                    continue;
                }

                float life01 = floating.Duration > 0.001f
                    ? 1f - Mathf.Clamp01(floating.Remaining / floating.Duration)
                    : 1f;
                floating.Rect.anchoredPosition += floating.Velocity * delta;
                floating.Rect.localScale = Vector3.one * Mathf.Lerp(1.14f, 0.88f, life01);

                if (floating.Text != null)
                {
                    Color c = floating.Text.color;
                    c.a = Mathf.Lerp(1f, 0f, life01);
                    floating.Text.color = c;
                }
            }
        }

        private static string BuildCombatDamageLabel(float amount)
        {
            return "-" + Mathf.CeilToInt(Mathf.Max(1f, amount));
        }

        public static string BuildTruckDamageFloaterLabel(string causeLabel, float amount)
        {
            string resolvedCause = string.IsNullOrEmpty(causeLabel) ? "TRUCK" : causeLabel.Trim();
            return BuildCombatHitFlowLabel(resolvedCause, "TRK", BuildCombatDamageLabel(amount));
        }

        public static string BuildEnemyDamageFloaterLabel(float amount, bool knockout)
        {
            return BuildCombatHitFlowLabel("HIT", "Z", knockout ? "KO" : BuildCombatDamageLabel(amount));
        }

        public static string BuildCombatHitFlowLabel(string sourceLabel, string targetLabel, string resultLabel)
        {
            string source = string.IsNullOrEmpty(sourceLabel) ? "HIT" : sourceLabel.Trim();
            string target = string.IsNullOrEmpty(targetLabel) ? "Z" : targetLabel.Trim();
            string result = string.IsNullOrEmpty(resultLabel) ? "--" : resultLabel.Trim();
            return source + ">" + target + " " + result;
        }

        private void UpdateEnemyHitEffects(float dt)
        {
            float delta = Mathf.Max(0f, dt);
            for (int i = enemyHitEffects.Count - 1; i >= 0; i--)
            {
                EnemyHitEffectWidget effect = enemyHitEffects[i];
                if (effect == null || effect.Rect == null)
                {
                    enemyHitEffects.RemoveAt(i);
                    continue;
                }

                effect.Remaining -= delta;
                if (effect.Remaining <= 0f)
                {
                    Destroy(effect.Rect.gameObject);
                    enemyHitEffects.RemoveAt(i);
                    continue;
                }

                float life01 = effect.Duration > 0.001f
                    ? 1f - Mathf.Clamp01(effect.Remaining / effect.Duration)
                    : 1f;
                effect.Rect.anchoredPosition += effect.Velocity * delta;
                effect.Rect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.35f, life01);

                if (effect.Image != null)
                {
                    Color c = effect.Image.color;
                    c.a = Mathf.Lerp(0.90f, 0f, life01);
                    effect.Image.color = c;
                }
            }
        }

        private void RefreshEnemyVisuals(bool truckDamagedThisFrame)
        {
            HashSet<int> alive = new HashSet<int>();
            IReadOnlyList<LaneEnemyState> enemies = model.LaneEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                LaneEnemyState enemy = enemies[i];
                alive.Add(enemy.Id);

                if (!enemyVisuals.TryGetValue(enemy.Id, out EnemyVisualWidget widget))
                {
                    widget = CreateEnemyVisual(enemy);
                    enemyVisuals.Add(enemy.Id, widget);
                }

                UpdateEnemyVisual(widget, enemy);
            }

            List<int> removeIds = new List<int>();
            foreach (KeyValuePair<int, EnemyVisualWidget> pair in enemyVisuals)
            {
                if (!alive.Contains(pair.Key))
                {
                    HandleEnemyVisualRemoval(pair.Value, truckDamagedThisFrame);

                    if (pair.Value.Rect != null)
                    {
                        Destroy(pair.Value.Rect.gameObject);
                    }

                    removeIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < removeIds.Count; i++)
            {
                enemyVisuals.Remove(removeIds[i]);
            }
        }

        private EnemyVisualWidget CreateEnemyVisual(LaneEnemyState enemy)
        {
            EnsureUndeadSpriteSetsLoaded();

            EnemyVisualWidget widget = new EnemyVisualWidget();
            widget.Rect = new GameObject("Enemy_" + enemy.Id, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            widget.Rect.transform.SetParent(laneTrackRoots[enemy.LaneIndex], false);
            widget.Rect.anchorMin = new Vector2(0f, 0.5f);
            widget.Rect.anchorMax = new Vector2(0f, 0.5f);
            widget.Rect.pivot = new Vector2(0.5f, 0.5f);
            float initialSize = GetEnemyVisualSize(laneTrackRoots[enemy.LaneIndex], enemy.IsSpecial, 0f);
            widget.Rect.sizeDelta = new Vector2(initialSize, initialSize);

            widget.Icon = widget.Rect.GetComponent<Image>();
            widget.Icon.color = Color.white;
            widget.Icon.preserveAspect = true;

            widget.HpText = CreateText(widget.Rect, "Hp", GetEnemyHpFontSize(initialSize), FontStyle.Bold, TextAnchor.LowerCenter,
                new Color(0.06f, 0.08f, 0.12f, 0.95f));
            Stretch(widget.HpText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, -9f), new Vector2(0f, 2f));

            widget.EnemyId = enemy.Id;
            widget.IsSpecial = enemy.IsSpecial;
            widget.LaneIndex = enemy.LaneIndex;
            widget.LastHp = enemy.Hp;
            widget.LastDistance01 = enemy.DistanceToTruck01;
            widget.HitTimer = 0f;
            widget.AnimationOffset = (enemy.Id % 17) * 0.071f;
            widget.KnockoutFloaterSpawned = false;
            return widget;
        }

        private float GetEnemyVisualSize(RectTransform lane, bool isSpecial, float hitWave)
        {
            float laneHeight = lane != null ? Mathf.Max(1f, lane.rect.height) : enemyIconSize;
            float baseSize = Mathf.Max(enemyIconSize, laneHeight * 0.62f);
            if (isSpecial)
            {
                baseSize *= 1.14f;
            }

            float maxSize = Mathf.Max(28f, laneHeight * 0.84f);
            float hitScale = 1f + hitWave * Mathf.Max(0f, enemyHitScalePulse);
            return Mathf.Clamp(baseSize * hitScale, 24f, maxSize);
        }

        private static int GetEnemyHpFontSize(float enemySize)
        {
            return Mathf.Clamp(Mathf.RoundToInt(enemySize * 0.23f), 9, 16);
        }

        private void UpdateEnemyVisual(EnemyVisualWidget widget, LaneEnemyState enemy)
        {
            if (widget.LaneIndex != enemy.LaneIndex)
            {
                widget.Rect.transform.SetParent(laneTrackRoots[enemy.LaneIndex], false);
                widget.LaneIndex = enemy.LaneIndex;
            }

            widget.EnemyId = enemy.Id;
            widget.IsSpecial = enemy.IsSpecial;

            RectTransform lane = laneTrackRoots[enemy.LaneIndex];
            float width = Mathf.Max(80f, lane.rect.width);
            float laneHeight = Mathf.Max(1f, lane.rect.height);
            float sideMargin = foodTruckSprite != null
                ? Mathf.Clamp(laneHeight * 1.72f, 118f, 260f)
                : Mathf.Clamp(laneHeight * 0.46f, 48f, 92f);
            float x = Mathf.Lerp(sideMargin, width - sideMargin, enemy.DistanceToTruck01);
            float y = ((enemy.Id % 3) - 1) * Mathf.Clamp(laneHeight * 0.09f, 7f, 16f);
            float runBob = Mathf.Sin((Time.unscaledTime + widget.AnimationOffset) * 6.2f) * Mathf.Clamp(laneHeight * 0.025f, 1.7f, 4.2f);
            widget.HpText.text = Mathf.CeilToInt(enemy.Hp).ToString();

            bool tookHit = enemy.Hp + 0.01f < widget.LastHp;
            if (tookHit)
            {
                float damageTaken = Mathf.Max(0f, widget.LastHp - enemy.Hp);
                widget.HitTimer = Mathf.Max(widget.HitTimer, enemyHitPoseDuration);
                SpawnEnemyAttackTrail(widget);
                SpawnEnemyHitEffect(widget);
                SpawnEnemyHitEffect(widget);
                bool knockout = enemy.Hp <= 0.01f;
                SpawnEnemyDamageFloater(widget, damageTaken, knockout);
                if (knockout)
                {
                    widget.KnockoutFloaterSpawned = true;
                }
            }

            widget.LastHp = enemy.Hp;
            widget.LastDistance01 = enemy.DistanceToTruck01;
            widget.HitTimer = Mathf.Max(0f, widget.HitTimer - Time.unscaledDeltaTime);
            float hit01 = enemyHitPoseDuration > 0.001f
                ? Mathf.Clamp01(widget.HitTimer / enemyHitPoseDuration)
                : 0f;
            float hitWave = Mathf.Sin((1f - hit01) * Mathf.PI);
            float hitKick = hit01 * Mathf.Max(0f, enemyHitShakeDistance);
            widget.Rect.anchoredPosition = new Vector2(x - hitKick, y + runBob);
            if (widget.HpText != null)
            {
                widget.HpText.rectTransform.localScale = Vector3.one * (1f + hitWave * 0.18f);
            }

            UndeadSpriteSet spriteSet = GetUndeadSpriteSet(enemy.Id, enemy.IsSpecial);
            Sprite sprite = null;
            if (spriteSet != null)
            {
                if (widget.HitTimer > 0.001f && spriteSet.HitFrame != null)
                {
                    sprite = spriteSet.HitFrame;
                }
                else if (spriteSet.RunFrames != null && spriteSet.RunFrames.Length > 0)
                {
                    int frameIndex = Mathf.FloorToInt((Time.unscaledTime + widget.AnimationOffset) * Mathf.Max(1f, enemyRunAnimationFps));
                    frameIndex %= spriteSet.RunFrames.Length;
                    if (frameIndex < 0)
                    {
                        frameIndex += spriteSet.RunFrames.Length;
                    }

                    sprite = spriteSet.RunFrames[frameIndex];
                }
            }

            if (sprite != null)
            {
                widget.Icon.sprite = sprite;
                widget.Icon.color = Color.Lerp(Color.white, new Color(1f, 0.72f, 0.54f, 1f), hit01 * 0.72f);
                float size = GetEnemyVisualSize(lane, enemy.IsSpecial, hitWave);
                widget.Rect.sizeDelta = new Vector2(size, size);
                if (widget.HpText != null)
                {
                    widget.HpText.fontSize = GetEnemyHpFontSize(size);
                }

                widget.HpText.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
                return;
            }

            widget.Icon.sprite = null;
            Color fallbackBase = enemy.IsSpecial
                ? new Color(0.94f, 0.46f, 0.22f, 1f)
                : new Color(0.74f, 0.85f, 0.30f, 1f);
            widget.Icon.color = Color.Lerp(fallbackBase, new Color(1f, 0.76f, 0.56f, 1f), hit01 * 0.75f);
            float fallbackSize = GetEnemyVisualSize(lane, enemy.IsSpecial, hitWave * 0.75f);
            widget.Rect.sizeDelta = new Vector2(fallbackSize, fallbackSize);
            if (widget.HpText != null)
            {
                widget.HpText.fontSize = GetEnemyHpFontSize(fallbackSize);
            }

            widget.HpText.color = new Color(0.05f, 0.06f, 0.08f, 1f);
        }
    }
}
