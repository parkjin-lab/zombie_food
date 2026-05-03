using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private void UpdateDrawChoiceRiskUiScale()
        {
            bool tallLayout = IsTallPortraitLayout() || Application.isMobilePlatform;
            float iconSize = tallLayout ? drawChoiceRiskIconSizeTallPortrait : drawChoiceRiskIconSize;
            float valueWidth = tallLayout ? drawChoiceRiskValueWidthTallPortrait : drawChoiceRiskValueWidth;
            int valueFontSize = tallLayout ? drawChoiceRiskValueFontSizeTallPortrait : drawChoiceRiskValueFontSize;

            for (int i = 0; i < drawChoiceRiskBarIcons.Count; i++)
            {
                Image icon = drawChoiceRiskBarIcons[i];
                if (icon == null)
                {
                    continue;
                }

                LayoutElement iconLayout = icon.GetComponent<LayoutElement>();
                if (iconLayout != null)
                {
                    iconLayout.minWidth = iconSize;
                    iconLayout.preferredWidth = iconSize;
                    iconLayout.minHeight = iconSize;
                    iconLayout.preferredHeight = iconSize;
                }
            }

            for (int i = 0; i < drawChoiceRiskBarValues.Count; i++)
            {
                Text valueText = drawChoiceRiskBarValues[i];
                if (valueText == null)
                {
                    continue;
                }

                LayoutElement valueLayout = valueText.GetComponent<LayoutElement>();
                if (valueLayout != null)
                {
                    valueLayout.minWidth = valueWidth;
                    valueLayout.preferredWidth = valueWidth;
                }

                valueText.fontSize = valueFontSize;
            }
        }

        private void BuildDrawChoiceStatBar(
            Transform parent,
            string title,
            Color barColor,
            bool withRiskIcon,
            out Image fillImage,
            out Text valueText,
            out Image iconImage)
        {
            RectTransform row = new GameObject(title + "BarRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>();
            row.transform.SetParent(parent, false);
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = 17f;
            rowLayout.preferredHeight = 18f;

            HorizontalLayoutGroup rowGroup = row.GetComponent<HorizontalLayoutGroup>();
            rowGroup.spacing = 5f;
            rowGroup.childAlignment = TextAnchor.MiddleLeft;
            rowGroup.childControlWidth = true;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childForceExpandHeight = false;

            Text titleText = CreateText(row, title + "Label", 10, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.85f, 0.91f, 0.97f, 1f));
            titleText.text = title;
            LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredWidth = 34f;
            titleLayout.minWidth = 34f;
            iconImage = null;

            RectTransform barRoot = new GameObject(title + "BarRoot", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            barRoot.transform.SetParent(row, false);
            barRoot.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 0.96f);
            LayoutElement barLayout = barRoot.GetComponent<LayoutElement>();
            barLayout.flexibleWidth = 1f;
            barLayout.minWidth = 56f;

            fillImage = new GameObject(title + "Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fillImage.transform.SetParent(barRoot, false);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;
            fillImage.color = barColor;
            Stretch(fillImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(-1f, -1f));

            if (withRiskIcon)
            {
                iconImage = new GameObject(title + "Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
                iconImage.transform.SetParent(row, false);
                iconImage.raycastTarget = false;
                iconImage.preserveAspect = true;
                LayoutElement iconLayout = iconImage.GetComponent<LayoutElement>();
                iconLayout.minWidth = 12f;
                iconLayout.preferredWidth = 12f;
                iconLayout.minHeight = 12f;
                iconLayout.preferredHeight = 12f;
                iconImage.color = new Color(0.78f, 0.84f, 0.90f, 0.25f);
            }

            valueText = CreateText(row, title + "Value", 10, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.90f, 0.95f, 1f, 1f));
            valueText.text = "--";
            LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = withRiskIcon ? 34f : 40f;
            valueLayout.minWidth = withRiskIcon ? 34f : 40f;
        }

        private void RefreshDrawChoiceStatBars(int slot, PendingBlockState choice, string assistTag)
        {
            if (slot < 0 || slot >= drawChoiceValueBarFills.Count || slot >= drawChoiceRiskBarFills.Count)
            {
                return;
            }

            float value = EstimateDrawChoiceValue(choice);
            float riskScore = EstimateDrawChoiceRiskScore(choice);
            string riskTag = GetDrawChoiceRiskTag(choice);
            float value01 = Mathf.Clamp01(value / 16f);
            float risk01 = Mathf.Clamp01(riskScore / 11.5f);
            bool safeAssist = string.Equals(assistTag, "SAFE", StringComparison.Ordinal);
            Color riskThemeColor = GetDrawChoiceRiskThemeColor(riskTag);

            Image valueFill = drawChoiceValueBarFills[slot];
            if (valueFill != null)
            {
                valueFill.fillAmount = Mathf.Lerp(0.08f, 1f, value01);
                Color valueColor = safeAssist
                    ? Color.Lerp(new Color(0.30f, 0.82f, 0.48f, 0.98f), new Color(0.52f, 0.95f, 0.72f, 0.98f), value01)
                    : Color.Lerp(new Color(0.27f, 0.64f, 0.94f, 0.98f), new Color(0.45f, 0.84f, 1f, 0.98f), value01);
                valueFill.color = valueColor;
            }

            Text valueText = slot < drawChoiceValueBarValues.Count ? drawChoiceValueBarValues[slot] : null;
            if (valueText != null)
            {
                valueText.text = value.ToString("0.0");
                valueText.color = new Color(0.90f, 0.95f, 1f, 1f);
            }

            Image riskFill = drawChoiceRiskBarFills[slot];
            if (riskFill != null)
            {
                riskFill.fillAmount = Mathf.Lerp(0.08f, 1f, risk01);
                Color baseRisk = Color.Lerp(new Color(0.20f, 0.28f, 0.22f, 0.98f), riskThemeColor, 0.55f);
                riskFill.color = Color.Lerp(baseRisk, riskThemeColor, Mathf.Lerp(0.30f, 1f, risk01));
            }

            Text riskText = slot < drawChoiceRiskBarValues.Count ? drawChoiceRiskBarValues[slot] : null;
            if (riskText != null)
            {
                bool tallLayout = IsTallPortraitLayout() || Application.isMobilePlatform;
                riskText.text = riskTag;
                riskText.color = Color.Lerp(new Color(0.88f, 0.92f, 0.98f, 1f), riskThemeColor, tallLayout ? 0.74f : 0.62f);
            }

            Image riskIcon = slot < drawChoiceRiskBarIcons.Count ? drawChoiceRiskBarIcons[slot] : null;
            if (riskIcon != null)
            {
                bool tallLayout = IsTallPortraitLayout() || Application.isMobilePlatform;
                riskIcon.sprite = GetDrawChoiceRiskSprite(riskTag);
                riskIcon.color = Color.Lerp(new Color(0.86f, 0.92f, 0.98f, 1f), riskThemeColor, tallLayout ? 0.90f : 0.82f);
                riskIcon.enabled = riskIcon.sprite != null;
            }
        }

        private void SetDrawChoiceStatBarPlaceholder(int slot)
        {
            if (slot < 0)
            {
                return;
            }

            if (slot < drawChoiceValueBarFills.Count && drawChoiceValueBarFills[slot] != null)
            {
                drawChoiceValueBarFills[slot].fillAmount = 0f;
                drawChoiceValueBarFills[slot].color = new Color(0.27f, 0.64f, 0.94f, 0f);
            }

            if (slot < drawChoiceRiskBarFills.Count && drawChoiceRiskBarFills[slot] != null)
            {
                drawChoiceRiskBarFills[slot].fillAmount = 0f;
                drawChoiceRiskBarFills[slot].color = new Color(0.96f, 0.48f, 0.26f, 0f);
            }

            if (slot < drawChoiceValueBarValues.Count && drawChoiceValueBarValues[slot] != null)
            {
                drawChoiceValueBarValues[slot].text = "--";
                drawChoiceValueBarValues[slot].color = new Color(0.70f, 0.76f, 0.84f, 0.9f);
            }

            if (slot < drawChoiceRiskBarValues.Count && drawChoiceRiskBarValues[slot] != null)
            {
                drawChoiceRiskBarValues[slot].text = "--";
                drawChoiceRiskBarValues[slot].color = new Color(0.70f, 0.76f, 0.84f, 0.9f);
            }

            if (slot < drawChoiceRiskBarIcons.Count && drawChoiceRiskBarIcons[slot] != null)
            {
                drawChoiceRiskBarIcons[slot].sprite = null;
                drawChoiceRiskBarIcons[slot].enabled = false;
                drawChoiceRiskBarIcons[slot].color = new Color(0.72f, 0.78f, 0.84f, 0.25f);
            }
        }

        private Sprite GetDrawChoiceRiskSprite(string riskTag)
        {
            EnsureRiskIconSprites();
            switch (riskTag)
            {
                case "HIGH":
                    return riskIconHighSprite;
                case "MID":
                    return riskIconMidSprite;
                default:
                    return riskIconLowSprite;
            }
        }
    }
}
