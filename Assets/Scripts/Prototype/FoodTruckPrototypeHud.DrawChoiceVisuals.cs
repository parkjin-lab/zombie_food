using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private const string IngredientSpriteResourcesPath = "FoodTruckPrototype/Sprites/Ingredients/";

        private Button BuildDrawChoiceCardButton(Transform parent, int slot, UnityEngine.Events.UnityAction callback)
        {
            GameObject buttonObject = new GameObject("DrawChoiceCard" + (slot + 1),
                typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.24f, 0.32f, 0.98f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.26f, 0.35f, 0.45f, 1f);
            colors.pressedColor = new Color(0.16f, 0.22f, 0.28f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.14f, 0.14f, 0.14f, 0.72f);
            button.colors = colors;
            button.targetGraphic = image;
            button.onClick.AddListener(callback);

            RectTransform guideRing = new GameObject("GuideRing", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            guideRing.transform.SetParent(buttonObject.transform, false);
            Image guideRingImage = guideRing.GetComponent<Image>();
            guideRingImage.raycastTarget = false;
            guideRingImage.color = Color.clear;
            Stretch(guideRing, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            drawChoiceGuideRings.Add(guideRingImage);

            RectTransform guideBadge = new GameObject("GuideBadge", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            guideBadge.transform.SetParent(buttonObject.transform, false);
            Image guideBadgeImage = guideBadge.GetComponent<Image>();
            guideBadgeImage.raycastTarget = false;
            guideBadgeImage.color = new Color(0.14f, 0.40f, 0.22f, 0.92f);
            guideBadge.anchorMin = new Vector2(0f, 1f);
            guideBadge.anchorMax = new Vector2(0f, 1f);
            guideBadge.pivot = new Vector2(0f, 1f);
            guideBadge.sizeDelta = new Vector2(54f, 18f);
            guideBadge.anchoredPosition = new Vector2(5f, -5f);
            drawChoiceGuideBadgeRoots.Add(guideBadge);

            Text guideBadgeText = CreateText(guideBadge, "GuideBadgeText", 9, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.92f, 0.98f, 0.95f, 1f));
            guideBadgeText.text = "SAFE";
            guideBadgeText.raycastTarget = false;
            Stretch(guideBadgeText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            guideBadge.gameObject.SetActive(false);

            Text guidePointer = CreateText(buttonObject.GetComponent<RectTransform>(), "GuidePointer", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.74f, 0.94f, 0.80f, 1f));
            guidePointer.text = "v";
            guidePointer.raycastTarget = false;
            RectTransform guidePointerRect = guidePointer.rectTransform;
            guidePointerRect.anchorMin = new Vector2(0.5f, 1f);
            guidePointerRect.anchorMax = new Vector2(0.5f, 1f);
            guidePointerRect.pivot = new Vector2(0.5f, 1f);
            guidePointerRect.sizeDelta = new Vector2(18f, 16f);
            guidePointerRect.anchoredPosition = new Vector2(0f, -2f);
            guidePointer.gameObject.SetActive(false);
            drawChoiceGuidePointers.Add(guidePointer);

            RectTransform guideSweep = new GameObject("GuideSweep", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            guideSweep.transform.SetParent(buttonObject.transform, false);
            Image guideSweepImage = guideSweep.GetComponent<Image>();
            guideSweepImage.raycastTarget = false;
            guideSweepImage.color = new Color(0.80f, 0.98f, 0.88f, 0f);
            guideSweep.anchorMin = new Vector2(0.5f, 0.5f);
            guideSweep.anchorMax = new Vector2(0.5f, 0.5f);
            guideSweep.pivot = new Vector2(0.5f, 0.5f);
            guideSweep.sizeDelta = new Vector2(24f, 96f);
            guideSweep.anchoredPosition = new Vector2(-48f, 0f);
            guideSweep.localRotation = Quaternion.Euler(0f, 0f, 9f);
            drawChoiceGuideSweeps.Add(guideSweep);

            RectTransform content = new GameObject("CardContent", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            content.transform.SetParent(buttonObject.transform, false);
            Stretch(content, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));

            HorizontalLayoutGroup contentLayout = content.GetComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = 4f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = true;

            RectTransform previewFrame = new GameObject("PreviewFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            previewFrame.transform.SetParent(content, false);
            previewFrame.GetComponent<Image>().color = new Color(0.10f, 0.13f, 0.17f, 0.97f);
            LayoutElement previewLayout = previewFrame.GetComponent<LayoutElement>();
            previewLayout.preferredWidth = 46f;
            previewLayout.minWidth = 42f;
            previewLayout.flexibleHeight = 1f;

            RectTransform previewGrid = new GameObject("PreviewGrid", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            previewGrid.transform.SetParent(previewFrame, false);
            Stretch(previewGrid, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));

            GridLayoutGroup previewGridLayout = previewGrid.GetComponent<GridLayoutGroup>();
            previewGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            previewGridLayout.constraintCount = 3;
            previewGridLayout.cellSize = new Vector2(10f, 10f);
            previewGridLayout.spacing = new Vector2(2f, 2f);
            previewGridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            previewGridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            previewGridLayout.childAlignment = TextAnchor.MiddleCenter;

            Image[] previewCells = new Image[9];
            Image[] previewHighlights = new Image[9];
            Image[] previewOutlines = new Image[9];
            for (int i = 0; i < previewCells.Length; i++)
            {
                RectTransform previewCell = new GameObject("Cell" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                previewCell.transform.SetParent(previewGrid, false);
                Image cellImage = previewCell.GetComponent<Image>();
                cellImage.color = new Color(0.16f, 0.18f, 0.24f, 0.95f);
                previewCells[i] = cellImage;

                RectTransform outline = new GameObject("CellOutline" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                outline.transform.SetParent(previewCell, false);
                Stretch(outline, Vector2.zero, Vector2.one, new Vector2(-1f, -1f), new Vector2(1f, 1f));
                Image outlineImage = outline.GetComponent<Image>();
                outlineImage.color = new Color(0f, 0f, 0f, 0f);
                outlineImage.raycastTarget = false;
                previewOutlines[i] = outlineImage;

                RectTransform highlight = new GameObject("CellHighlight" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                highlight.transform.SetParent(previewCell, false);
                Stretch(highlight, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
                Image highlightImage = highlight.GetComponent<Image>();
                highlightImage.color = new Color(1f, 1f, 1f, 0f);
                highlightImage.raycastTarget = false;
                previewHighlights[i] = highlightImage;
            }

            drawChoicePreviewCells.Add(previewCells);
            drawChoicePreviewHighlights.Add(previewHighlights);
            drawChoicePreviewOutlines.Add(previewOutlines);

            RectTransform ingredientFrame = new GameObject("IngredientFrame", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            ingredientFrame.transform.SetParent(previewFrame, false);
            ingredientFrame.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.95f);
            ingredientFrame.anchorMin = new Vector2(1f, 1f);
            ingredientFrame.anchorMax = new Vector2(1f, 1f);
            ingredientFrame.pivot = new Vector2(1f, 1f);
            ingredientFrame.sizeDelta = new Vector2(22f, 22f);
            ingredientFrame.anchoredPosition = new Vector2(-3f, -3f);

            Image ingredientImage = new GameObject("IngredientImage", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            ingredientImage.transform.SetParent(ingredientFrame, false);
            ingredientImage.preserveAspect = true;
            Stretch(ingredientImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            drawChoiceIngredientImages.Add(ingredientImage);

            Text ingredientLabel = CreateText(ingredientFrame, "IngredientLabel", 10, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(ingredientLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            drawChoiceIngredientLabels.Add(ingredientLabel);

            RectTransform textWrap = new GameObject("TextWrap", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            textWrap.transform.SetParent(content, false);
            LayoutElement textWrapLayout = textWrap.GetComponent<LayoutElement>();
            textWrapLayout.flexibleWidth = 1f;
            textWrapLayout.minWidth = 44f;

            VerticalLayoutGroup textWrapGroup = textWrap.GetComponent<VerticalLayoutGroup>();
            textWrapGroup.spacing = 3f;
            textWrapGroup.childControlWidth = true;
            textWrapGroup.childControlHeight = true;
            textWrapGroup.childForceExpandWidth = true;
            textWrapGroup.childForceExpandHeight = false;

            Text label = CreateText(textWrap, "Label", 10, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.minHeight = 46f;
            labelLayout.flexibleHeight = 1f;
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            drawChoiceTexts.Add(label);

            RectTransform statBarsRoot = new GameObject("StatBarsRoot", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            statBarsRoot.transform.SetParent(textWrap, false);
            LayoutElement statBarsLayout = statBarsRoot.GetComponent<LayoutElement>();
            statBarsLayout.minHeight = 28f;
            statBarsLayout.preferredHeight = 30f;
            statBarsLayout.flexibleHeight = 0f;

            VerticalLayoutGroup statBarsGroup = statBarsRoot.GetComponent<VerticalLayoutGroup>();
            statBarsGroup.spacing = 3f;
            statBarsGroup.childControlWidth = true;
            statBarsGroup.childControlHeight = true;
            statBarsGroup.childForceExpandWidth = true;
            statBarsGroup.childForceExpandHeight = false;

            Image valueFill;
            Text valueValue;
            Image riskFill;
            Text riskValue;
            Image riskIcon;
            BuildDrawChoiceStatBar(statBarsRoot, "Value", new Color(0.28f, 0.74f, 0.98f, 1f), false, out valueFill, out valueValue, out _);
            BuildDrawChoiceStatBar(statBarsRoot, "Risk", new Color(0.96f, 0.58f, 0.24f, 1f), true, out riskFill, out riskValue, out riskIcon);
            drawChoiceValueBarFills.Add(valueFill);
            drawChoiceValueBarValues.Add(valueValue);
            drawChoiceRiskBarFills.Add(riskFill);
            drawChoiceRiskBarValues.Add(riskValue);
            drawChoiceRiskBarIcons.Add(riskIcon);

            SetDrawChoicePreviewPlaceholder(slot);
            SetDrawChoiceIngredientPlaceholder(slot);
            SetDrawChoiceStatBarPlaceholder(slot);
            return button;
        }

        private void RefreshDrawChoicePreview(int slot, PendingBlockState choice)
        {
            if (slot < 0 || slot >= drawChoicePreviewCells.Count)
            {
                return;
            }

            Image[] cells = drawChoicePreviewCells[slot];
            Image[] highlights = slot < drawChoicePreviewHighlights.Count ? drawChoicePreviewHighlights[slot] : null;
            Image[] outlines = slot < drawChoicePreviewOutlines.Count ? drawChoicePreviewOutlines[slot] : null;
            if (cells == null || cells.Length < 9)
            {
                return;
            }

            string riskTag = GetDrawChoiceRiskTag(choice);
            Color offColor = new Color(0.17f, 0.20f, 0.26f, 0.95f);
            Color onColor = Color.Lerp(ColorFromSeed(choice.ColorSeed), Color.white, 0.12f);
            onColor.a = 0.98f;
            Color riskOutline = GetDrawChoiceRiskOutlineColor(riskTag);
            Color highlightOn = Color.Lerp(onColor, riskOutline, 0.22f);
            highlightOn.a = 0.75f;

            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                {
                    ApplyKitchenModuleCellSprite(cells[i], false);
                    cells[i].color = offColor;
                }
                if (highlights != null && i < highlights.Length && highlights[i] != null)
                {
                    highlights[i].color = new Color(1f, 1f, 1f, 0f);
                }
                if (outlines != null && i < outlines.Length && outlines[i] != null)
                {
                    outlines[i].color = new Color(0f, 0f, 0f, 0f);
                }
            }

            Vector2Int[] offsets = GetPendingTokenPreviewOffsets(choice);
            for (int i = 0; i < offsets.Length; i++)
            {
                int x = Mathf.Clamp(offsets[i].x, 0, 2);
                int y = Mathf.Clamp(offsets[i].y, 0, 2);
                int cellIndex = (2 - y) * 3 + x;
                if (cellIndex >= 0 && cellIndex < cells.Length && cells[cellIndex] != null)
                {
                    ApplyKitchenModuleCellSprite(cells[cellIndex], true);
                    cells[cellIndex].color = onColor;
                    if (outlines != null && cellIndex < outlines.Length && outlines[cellIndex] != null)
                    {
                        outlines[cellIndex].color = riskOutline;
                    }
                    if (highlights != null && cellIndex < highlights.Length && highlights[cellIndex] != null)
                    {
                        highlights[cellIndex].color = highlightOn;
                    }
                }
            }
        }

        private void SetDrawChoicePreviewPlaceholder(int slot)
        {
            if (slot < 0 || slot >= drawChoicePreviewCells.Count)
            {
                return;
            }

            Image[] cells = drawChoicePreviewCells[slot];
            Image[] highlights = slot < drawChoicePreviewHighlights.Count ? drawChoicePreviewHighlights[slot] : null;
            Image[] outlines = slot < drawChoicePreviewOutlines.Count ? drawChoicePreviewOutlines[slot] : null;
            if (cells == null || cells.Length < 9)
            {
                return;
            }

            Color offColor = new Color(0.18f, 0.20f, 0.25f, 0.95f);
            Color centerColor = new Color(0.30f, 0.34f, 0.42f, 0.95f);
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                {
                    ApplyKitchenModuleCellSprite(cells[i], false);
                    cells[i].color = offColor;
                }
                if (highlights != null && i < highlights.Length && highlights[i] != null)
                {
                    highlights[i].color = new Color(1f, 1f, 1f, 0f);
                }
                if (outlines != null && i < outlines.Length && outlines[i] != null)
                {
                    outlines[i].color = new Color(0f, 0f, 0f, 0f);
                }
            }

            if (cells[4] != null)
            {
                ApplyKitchenModuleCellSprite(cells[4], false);
                cells[4].color = centerColor;
            }
        }

        private void RefreshDrawChoiceIngredientVisual(int slot, PendingBlockState choice)
        {
            if (slot < 0 || slot >= drawChoiceIngredientImages.Count || slot >= drawChoiceIngredientLabels.Count)
            {
                return;
            }

            Image icon = drawChoiceIngredientImages[slot];
            Text badge = drawChoiceIngredientLabels[slot];
            if (icon == null || badge == null)
            {
                return;
            }

            Sprite sprite = ResolveIngredientSprite(choice.IngredientName);
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;
                badge.text = string.Empty;
            }
            else
            {
                icon.sprite = null;
                icon.color = Color.Lerp(ColorFromSeed(choice.ColorSeed), new Color(0.16f, 0.18f, 0.24f, 1f), 0.40f);
                badge.text = BuildIngredientBadge(choice.IngredientName);
            }
        }

        private void SetDrawChoiceIngredientPlaceholder(int slot)
        {
            if (slot < 0 || slot >= drawChoiceIngredientImages.Count || slot >= drawChoiceIngredientLabels.Count)
            {
                return;
            }

            Image icon = drawChoiceIngredientImages[slot];
            Text badge = drawChoiceIngredientLabels[slot];
            if (icon != null)
            {
                icon.sprite = null;
                icon.color = new Color(0.24f, 0.28f, 0.35f, 0.96f);
            }

            if (badge != null)
            {
                badge.text = "?";
                badge.color = new Color(0.90f, 0.94f, 0.99f, 0.95f);
            }
        }

        private Sprite ResolveIngredientSprite(string ingredientName)
        {
            if (string.IsNullOrEmpty(ingredientName))
            {
                return null;
            }

            if (ingredientSpriteCache.TryGetValue(ingredientName, out Sprite cached))
            {
                return cached;
            }

            Sprite resolved = null;
            for (int i = 0; i < ingredientSpriteEntries.Length; i++)
            {
                IngredientSpriteEntry entry = ingredientSpriteEntries[i];
                if (!string.IsNullOrEmpty(entry.IngredientName) &&
                    string.Equals(entry.IngredientName, ingredientName, StringComparison.OrdinalIgnoreCase))
                {
                    resolved = entry.Sprite;
                    break;
                }
            }

            if (resolved == null)
            {
                resolved = LoadIngredientSpriteFromResources(ingredientName);
            }

            ingredientSpriteCache[ingredientName] = resolved;
            return resolved;
        }

        private static Sprite LoadIngredientSpriteFromResources(string ingredientName)
        {
            string trimmed = ingredientName.Trim();
            Sprite sprite = Resources.Load<Sprite>(IngredientSpriteResourcesPath + trimmed);
            if (sprite != null)
            {
                return sprite;
            }

            string compact = trimmed.Replace(" ", string.Empty);
            if (!string.Equals(compact, trimmed, StringComparison.Ordinal))
            {
                sprite = Resources.Load<Sprite>(IngredientSpriteResourcesPath + compact);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return Resources.Load<Sprite>(IngredientSpriteResourcesPath + compact.ToLowerInvariant());
        }

        private static string BuildIngredientBadge(string ingredientName)
        {
            if (string.IsNullOrEmpty(ingredientName))
            {
                return "?";
            }

            string trimmed = ingredientName.Trim();
            if (trimmed.Length <= 2)
            {
                return trimmed.ToUpperInvariant();
            }

            string[] words = trimmed.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
            {
                return char.ToUpperInvariant(words[0][0]).ToString() + char.ToUpperInvariant(words[1][0]);
            }

            return trimmed.Substring(0, 2).ToUpperInvariant();
        }
    }
}
