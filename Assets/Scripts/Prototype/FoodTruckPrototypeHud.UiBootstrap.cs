using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private static Font TryLoadBuiltinFont()
        {
            Font font = null;
            try
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
            }

            if (font != null)
            {
                return font;
            }

            try
            {
                font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Segoe UI", "Malgun Gothic", "Arial", "Noto Sans CJK KR", "Helvetica Neue" }, 16);
            }
            catch
            {
            }

            return font;
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject(
                "FTA_DummyHud",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;

            canvasRect = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            topPanelRect = CreatePanel(canvasObject.transform, "TopBattlePanel", PanelDark,
                new Vector2(0f, 0.66f), new Vector2(1f, 1f), new Vector2(12f, -10f), new Vector2(-12f, -6f));
            midPanelRect = CreatePanel(canvasObject.transform, "SynergyPanel", PanelMid,
                new Vector2(0f, 0.60f), new Vector2(1f, 0.66f), new Vector2(12f, 2f), new Vector2(-12f, -2f));
            bottomPanelRect = CreatePanel(canvasObject.transform, "BottomControlPanel", PanelDark,
                new Vector2(0f, 0f), new Vector2(1f, 0.60f), new Vector2(12f, 6f), new Vector2(-12f, -12f));

            BuildTopPanel(topPanelRect);
            BuildMidPanel(midPanelRect);
            BuildBottomPanel(bottomPanelRect);
            BuildEventPanel(canvasObject.transform);

            RectTransform placementImpactRect = new GameObject("PlacementImpactFlash", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            placementImpactRect.transform.SetParent(canvasObject.transform, false);
            Stretch(placementImpactRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            placementImpactImage = placementImpactRect.GetComponent<Image>();
            placementImpactImage.color = new Color(0f, 0f, 0f, 0f);
            placementImpactImage.raycastTarget = false;

            prototypeVfxRect = new GameObject("PrototypeVfxBurst", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            prototypeVfxRect.transform.SetParent(canvasObject.transform, false);
            prototypeVfxRect.anchorMin = new Vector2(0.5f, 0.56f);
            prototypeVfxRect.anchorMax = new Vector2(0.5f, 0.56f);
            prototypeVfxRect.pivot = new Vector2(0.5f, 0.5f);
            prototypeVfxRect.sizeDelta = new Vector2(prototypeVfxSize, prototypeVfxSize);
            prototypeVfxRect.anchoredPosition = Vector2.zero;
            prototypeVfxImage = prototypeVfxRect.GetComponent<Image>();
            prototypeVfxImage.color = new Color(1f, 1f, 1f, 0f);
            prototypeVfxImage.preserveAspect = true;
            prototypeVfxImage.raycastTarget = false;

            cueBannerRect = new GameObject("CueBanner", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            cueBannerRect.transform.SetParent(canvasObject.transform, false);
            cueBannerRect.anchorMin = new Vector2(0.18f, 0.90f);
            cueBannerRect.anchorMax = new Vector2(0.82f, 0.97f);
            cueBannerRect.offsetMin = Vector2.zero;
            cueBannerRect.offsetMax = Vector2.zero;
            cueBannerBackground = cueBannerRect.GetComponent<Image>();
            cueBannerBackground.color = new Color(0.10f, 0.14f, 0.22f, 0f);
            cueBannerBackground.raycastTarget = false;
            cueBannerText = CreateText(cueBannerRect, "CueBannerText", 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.97f, 0.98f, 1f, 0f));
            Stretch(cueBannerText.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 6f), new Vector2(-14f, -6f));

            ApplyPanelLayout();
            UpdateInventoryGridSizing();
        }

        private void BuildTopPanel(RectTransform topPanel)
        {
            VerticalLayoutGroup layout = topPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            titleText = CreateText(topPanel, "Title", 28, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            titleText.text = "FOOD TRUCK APOCALYPSE - PROTOTYPE";
            titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

            topLineText = CreateText(topPanel, "TopLine", 17, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
            topLineText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            secondLineText = CreateText(topPanel, "SecondLine", 15, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
            secondLineText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            RectTransform metersContainer = CreateContainer(topPanel, "Meters", 110f);
            meterContainerRect = metersContainer;
            meterContainerLayoutElement = metersContainer.GetComponent<LayoutElement>();
            VerticalLayoutGroup meterLayout = metersContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            meterLayout.spacing = 3f;
            meterLayout.childControlHeight = false;
            meterLayout.childControlWidth = true;
            meterLayout.childForceExpandWidth = true;
            meterLayout.childForceExpandHeight = false;

            BuildMeter(metersContainer, "Truck HP", AccentRed, hpMeter);
            BuildMeter(metersContainer, "Wave", AccentBlue, waveMeter);
            BuildMeter(metersContainer, "Heat", AccentOrange, heatMeter);
            BuildMeter(metersContainer, "Momentum", AccentGreen, momentumMeter);

            combatLineText = CreateText(topPanel, "CombatLine", 14, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(0.85f, 0.94f, 1f, 1f));
            combatLineText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            mapControlRowRect = CreateContainer(topPanel, "MapControlRow", 34f);
            HorizontalLayoutGroup mapControlLayout = mapControlRowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            mapControlLayout.childAlignment = TextAnchor.MiddleRight;
            mapControlLayout.spacing = 6f;
            mapControlLayout.childControlWidth = false;
            mapControlLayout.childControlHeight = true;
            mapControlLayout.childForceExpandWidth = false;
            mapControlLayout.childForceExpandHeight = true;

            speedCycleButton = BuildActionButton(mapControlRowRect, "Speed x1\n[T]", CycleBattleSpeed);
            speedCycleButtonText = speedCycleButton.GetComponentInChildren<Text>();
            if (speedCycleButtonText != null)
            {
                speedCycleButtonText.fontSize = 13;
            }

            LayoutElement speedLayout = speedCycleButton.gameObject.AddComponent<LayoutElement>();
            speedLayout.preferredWidth = 146f;
            speedLayout.preferredHeight = 32f;

            hudDensityButton = BuildActionButton(mapControlRowRect, "HUD Compact\n[H]", ToggleHudDensity);
            hudDensityButtonText = hudDensityButton.GetComponentInChildren<Text>();
            if (hudDensityButtonText != null)
            {
                hudDensityButtonText.fontSize = 12;
            }

            LayoutElement hudLayout = hudDensityButton.gameObject.AddComponent<LayoutElement>();
            hudLayout.preferredWidth = 166f;
            hudLayout.preferredHeight = 32f;

            laneContainerRect = CreateContainer(topPanel, "LaneContainer", 196f);
            laneContainerLayoutElement = laneContainerRect.GetComponent<LayoutElement>();
            VerticalLayoutGroup laneLayout = laneContainerRect.gameObject.AddComponent<VerticalLayoutGroup>();
            laneLayout.spacing = 5f;
            laneLayout.childControlWidth = true;
            laneLayout.childControlHeight = true;
            laneLayout.childForceExpandWidth = true;
            laneLayout.childForceExpandHeight = true;

            for (int i = 0; i < laneTexts.Length; i++)
            {
                RectTransform laneRow = CreatePanel(laneContainerRect, "Lane" + (i + 1),
                    new Color(0.16f, 0.20f, 0.25f, 0.95f),
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                LayoutElement laneRowLayout = laneRow.gameObject.AddComponent<LayoutElement>();
                laneRowLayout.preferredHeight = 60f;
                laneRowLayout.minHeight = 56f;
                laneRowLayoutElements[i] = laneRowLayout;

                RectTransform laneTrack = new GameObject("Track", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                laneTrack.transform.SetParent(laneRow, false);
                Image laneTrackImage = laneTrack.GetComponent<Image>();
                laneTrackImage.color = laneBaseColor;
                Stretch(laneTrack, Vector2.zero, Vector2.one, new Vector2(6f, 5f), new Vector2(-6f, -5f));

                RectTransform truckMarker = new GameObject("FoodTruckMarker", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                truckMarker.transform.SetParent(laneTrack, false);
                truckMarker.anchorMin = new Vector2(0f, 0.12f);
                truckMarker.anchorMax = new Vector2(0f, 0.88f);
                truckMarker.pivot = new Vector2(0f, 0.5f);
                truckMarker.anchoredPosition = new Vector2(8f, 0f);
                truckMarker.sizeDelta = new Vector2(44f, 0f);
                Image truckImage = truckMarker.GetComponent<Image>();
                truckImage.color = new Color(0.95f, 0.72f, 0.34f, 0.96f);
                truckImage.raycastTarget = false;

                Text truckLabel = CreateText(truckMarker, "TruckLabel", 9, FontStyle.Bold, TextAnchor.MiddleCenter,
                    new Color(0.08f, 0.08f, 0.10f, 0.96f));
                truckLabel.text = "TRK";
                Stretch(truckLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 0f), new Vector2(-2f, 0f));

                laneTexts[i] = CreateText(laneTrack, "LaneText", 14, FontStyle.Bold, TextAnchor.UpperLeft,
                    new Color(0.95f, 0.96f, 0.98f, 1f));
                Stretch(laneTexts[i].rectTransform, new Vector2(0f, 0.58f), new Vector2(1f, 1f),
                    new Vector2(8f, -2f), new Vector2(-8f, -2f));

                laneTrackRoots[i] = laneTrack;
                laneTrackImages[i] = laneTrackImage;
                laneTruckImages[i] = truckImage;
                laneTruckTexts[i] = truckLabel;
                laneHitFlashTimers[i] = 0f;
            }

            logText = CreateText(topPanel, "LogText", 13, FontStyle.Italic, TextAnchor.UpperLeft,
                new Color(0.96f, 0.92f, 0.70f, 1f));
            logText.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
        }

        private void BuildMidPanel(RectTransform midPanel)
        {
            VerticalLayoutGroup layout = midPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text synergyTitle = CreateText(midPanel, "SynergyTitle", 22, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(0.84f, 0.95f, 0.85f, 1f));
            synergyTitle.text = "Synergy Bar (Dummy)";
            synergyTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

            GameObject chipScroll = new GameObject("ChipScroll", typeof(RectTransform), typeof(Image), typeof(Mask));
            chipScroll.transform.SetParent(midPanel, false);
            chipScroll.GetComponent<Image>().color = new Color(0.08f, 0.11f, 0.15f, 0.9f);
            chipScroll.GetComponent<Mask>().showMaskGraphic = true;
            chipScroll.AddComponent<LayoutElement>().preferredHeight = 60f;

            synergyContainer = new GameObject("SynergyContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            synergyContainer.transform.SetParent(chipScroll.transform, false);
            Stretch(synergyContainer, Vector2.zero, Vector2.one, new Vector2(8f, 6f), new Vector2(-8f, -6f));

            HorizontalLayoutGroup chipLayout = synergyContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            chipLayout.spacing = 8f;
            chipLayout.childControlHeight = true;
            chipLayout.childControlWidth = false;
            chipLayout.childForceExpandWidth = false;
            chipLayout.childForceExpandHeight = true;
        }

        private void BuildMeter(RectTransform parent, string label, Color fillColor, MeterWidget widget)
        {
            RectTransform row = new GameObject(label.Replace(" ", string.Empty) + "Meter", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            row.transform.SetParent(parent, false);
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = 24f;

            RectTransform bg = new GameObject("Bg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            bg.transform.SetParent(row, false);
            Stretch(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.GetComponent<Image>().color = new Color(0.14f, 0.16f, 0.22f, 0.96f);

            RectTransform fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            fill.transform.SetParent(bg, false);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(1f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = fillColor;

            Text valueText = CreateText(bg, "Value", 12, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            valueText.text = label;
            Stretch(valueText.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-6f, 0f));

            widget.RowRect = row;
            widget.RowLayout = rowLayout;
            widget.FillRect = fill;
            widget.FillImage = fillImage;
            widget.ValueText = valueText;
        }

        private void BuildBottomPanel(RectTransform bottomPanel)
        {
            VerticalLayoutGroup layout = bottomPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            inventoryTitleText = CreateText(bottomPanel, "InventoryTitle", 20, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(0.93f, 0.93f, 0.98f, 1f));
            inventoryTitleText.text = "Inventory (3x3) + Draw Choice (Pick 1 of 3)";
            inventoryTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            inventoryCells.Clear();
            inventoryGhostCells.Clear();
            inventoryRecommendationRings.Clear();
            inventoryCellLabels.Clear();
            inventoryCellRects.Clear();
            Array.Clear(inventoryCellBaseScales, 0, inventoryCellBaseScales.Length);

            inventoryGridShakeRoot = CreateContainer(bottomPanel, "GridHolder", 470f);
            inventoryGridShakeRoot.pivot = new Vector2(0f, 1f);
            inventoryGridLayoutElement = inventoryGridShakeRoot.GetComponent<LayoutElement>();
            inventoryGridLayoutElement.minHeight = 350f;
            inventoryGridLayoutElement.preferredHeight = 470f;
            inventoryGridLayoutElement.flexibleHeight = 1f;

            inventoryGridShakeLayer = new GameObject("GridShakeLayer", typeof(RectTransform)).GetComponent<RectTransform>();
            inventoryGridShakeLayer.transform.SetParent(inventoryGridShakeRoot, false);
            inventoryGridShakeLayer.anchorMin = new Vector2(0f, 1f);
            inventoryGridShakeLayer.anchorMax = new Vector2(0f, 1f);
            inventoryGridShakeLayer.pivot = new Vector2(0f, 1f);
            inventoryGridShakeLayer.anchoredPosition = Vector2.zero;
            inventoryGridShakeLayer.localScale = Vector3.one;
            inventoryGridShakeLayer.localRotation = Quaternion.identity;

            inventoryGridHolder = new GameObject("GridContent", typeof(RectTransform)).GetComponent<RectTransform>();
            inventoryGridHolder.transform.SetParent(inventoryGridShakeLayer, false);
            inventoryGridHolder.anchorMin = new Vector2(0f, 1f);
            inventoryGridHolder.anchorMax = new Vector2(0f, 1f);
            inventoryGridHolder.pivot = new Vector2(0f, 1f);
            inventoryGridHolder.anchoredPosition = Vector2.zero;
            inventoryGridHolder.localScale = Vector3.one;
            inventoryGridHolder.localRotation = Quaternion.identity;

            inventoryGridLayout = inventoryGridHolder.gameObject.AddComponent<GridLayoutGroup>();
            inventoryGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            inventoryGridLayout.constraintCount = 3;
            inventoryGridLayout.cellSize = new Vector2(132f, 132f);
            inventoryGridLayout.spacing = new Vector2(8f, 8f);
            inventoryGridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            inventoryGridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            inventoryGridLayout.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < FoodTruckRunModel.InventoryCellCount; i++)
            {
                RectTransform cellRect = new GameObject("Cell" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                cellRect.transform.SetParent(inventoryGridHolder, false);
                Image image = cellRect.GetComponent<Image>();
                image.color = new Color(0.18f, 0.20f, 0.24f, 1f);

                RectTransform ghostRect = new GameObject("CellGhost" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                ghostRect.transform.SetParent(cellRect, false);
                Image ghostImage = ghostRect.GetComponent<Image>();
                ghostImage.color = Color.clear;
                ghostImage.raycastTarget = false;
                Stretch(ghostRect, Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -16f));

                RectTransform recommendationRingRect = new GameObject("CellRecommendationRing" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                recommendationRingRect.transform.SetParent(cellRect, false);
                Image recommendationRing = recommendationRingRect.GetComponent<Image>();
                recommendationRing.color = Color.clear;
                recommendationRing.raycastTarget = false;
                Stretch(recommendationRingRect, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));

                Text label = CreateText(cellRect, "CellLabel", 16, FontStyle.Bold,
                    TextAnchor.MiddleCenter, new Color(0.80f, 0.82f, 0.88f, 1f));
                label.text = (i + 1).ToString();
                Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                inventoryCells.Add(image);
                inventoryGhostCells.Add(ghostImage);
                inventoryRecommendationRings.Add(recommendationRing);
                inventoryCellLabels.Add(label);
                inventoryCellRects.Add(cellRect);
                inventoryCellBaseScales[i] = Vector3.one;
                cellRect.localScale = inventoryCellBaseScales[i];
            }

            drawChoiceButtons.Clear();
            drawChoiceTexts.Clear();
            drawChoiceGuideRings.Clear();
            drawChoiceGuideBadgeRoots.Clear();
            drawChoiceGuidePointers.Clear();
            drawChoiceGuideSweeps.Clear();
            drawChoicePreviewCells.Clear();
            drawChoicePreviewHighlights.Clear();
            drawChoicePreviewOutlines.Clear();
            drawChoiceIngredientImages.Clear();
            drawChoiceIngredientLabels.Clear();
            drawChoiceValueBarFills.Clear();
            drawChoiceValueBarValues.Clear();
            drawChoiceRiskBarFills.Clear();
            drawChoiceRiskBarValues.Clear();
            drawChoiceRiskBarIcons.Clear();
            ingredientSpriteCache.Clear();

            drawChoiceRow = CreateContainer(bottomPanel, "DrawChoiceRow", 132f);
            drawChoiceRowLayoutElement = drawChoiceRow.GetComponent<LayoutElement>();
            HorizontalLayoutGroup drawChoiceLayout = drawChoiceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            drawChoiceLayout.spacing = 8f;
            drawChoiceLayout.childControlWidth = true;
            drawChoiceLayout.childControlHeight = true;
            drawChoiceLayout.childForceExpandWidth = true;
            drawChoiceLayout.childForceExpandHeight = true;

            for (int i = 0; i < 3; i++)
            {
                int capture = i;
                Button choiceButton = BuildDrawChoiceCardButton(drawChoiceRow, i, () => AttemptChooseDrawOptionFromHud(capture));
                LayoutElement choiceLayout = choiceButton.gameObject.AddComponent<LayoutElement>();
                choiceLayout.minHeight = 104f;
                choiceLayout.preferredHeight = 118f;
                choiceLayout.flexibleHeight = 1f;
                drawChoiceButtons.Add(choiceButton);
            }

            pendingRowRect = CreateContainer(bottomPanel, "PendingRow", 58f);
            pendingRowLayoutElement = pendingRowRect.GetComponent<LayoutElement>();
            HorizontalLayoutGroup pendingLayout = pendingRowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            pendingLayout.spacing = 8f;
            pendingLayout.childControlWidth = true;
            pendingLayout.childControlHeight = true;
            pendingLayout.childForceExpandWidth = true;
            pendingLayout.childForceExpandHeight = true;

            pendingSlotRect = new GameObject("PendingSlot", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            pendingSlotRect.transform.SetParent(pendingRowRect, false);
            pendingSlotRect.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.29f, 1f);
            pendingSlotRect.GetComponent<LayoutElement>().preferredWidth = 430f;

            pendingHintText = CreateText(pendingSlotRect, "PendingHint", 17, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.91f, 0.95f, 1f, 1f));
            pendingHintText.text = "Pending: none";
            Stretch(pendingHintText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));

            pendingHelpRect = new GameObject("DragHelp", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            pendingHelpRect.transform.SetParent(pendingRowRect, false);
            pendingHelpRect.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.20f, 1f);
            pendingHelpRect.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            pendingHelpText = CreateText(pendingHelpRect, "HelpText", 14, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Color(0.86f, 0.90f, 0.95f, 1f));
            pendingHelpText.text = Application.isMobilePlatform
                ? "Pick choice -> Rotate buttons -> Drag or tap to place"
                : "Pick choice -> Rotate(Q/E) -> Drag or tap to place";
            Stretch(pendingHelpText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));

            flowChecklistPanelRect = CreateContainer(bottomPanel, "FlowChecklistPanel", 124f);
            flowChecklistPanelRect.GetComponent<LayoutElement>().minHeight = 108f;
            flowChecklistPanelRect.GetComponent<LayoutElement>().preferredHeight = 124f;
            flowChecklistPanelRect.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.14f, 0.18f, 0.96f);

            VerticalLayoutGroup checklistLayout = flowChecklistPanelRect.gameObject.AddComponent<VerticalLayoutGroup>();
            checklistLayout.padding = new RectOffset(12, 12, 8, 8);
            checklistLayout.spacing = 3f;
            checklistLayout.childControlWidth = true;
            checklistLayout.childControlHeight = false;
            checklistLayout.childForceExpandWidth = true;
            checklistLayout.childForceExpandHeight = false;

            Text checklistTitle = CreateText(flowChecklistPanelRect, "FlowChecklistTitle", 17, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.92f, 0.96f, 1f, 1f));
            checklistTitle.text = "Flow Checklist (Dummy)";
            checklistTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            for (int i = 0; i < flowChecklistTexts.Length; i++)
            {
                flowChecklistTexts[i] = CreateText(flowChecklistPanelRect, "FlowStep" + i, 13, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.80f, 0.85f, 0.92f, 1f));
                flowChecklistTexts[i].gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
            }

            telemetryControlRowRect = CreateContainer(bottomPanel, "TelemetryControlRow", 42f);
            HorizontalLayoutGroup telemetryControlLayout = telemetryControlRowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            telemetryControlLayout.spacing = 8f;
            telemetryControlLayout.childControlWidth = false;
            telemetryControlLayout.childControlHeight = true;
            telemetryControlLayout.childForceExpandWidth = false;
            telemetryControlLayout.childForceExpandHeight = true;
            telemetryControlLayout.childAlignment = TextAnchor.MiddleLeft;

            telemetryToggleButton = BuildActionButton(telemetryControlRowRect, "UX Panel Off\n[Y]", ToggleTelemetryPanel);
            telemetryToggleButtonText = telemetryToggleButton.GetComponentInChildren<Text>();
            if (telemetryToggleButtonText != null)
            {
                telemetryToggleButtonText.fontSize = 12;
            }

            LayoutElement telemetryToggleLayout = telemetryToggleButton.gameObject.AddComponent<LayoutElement>();
            telemetryToggleLayout.preferredWidth = 148f;
            telemetryToggleLayout.minWidth = 132f;

            telemetryScopeButton = BuildActionButton(telemetryControlRowRect, "Scope Run\n[U]", ToggleTelemetryScope);
            telemetryScopeButtonText = telemetryScopeButton.GetComponentInChildren<Text>();
            if (telemetryScopeButtonText != null)
            {
                telemetryScopeButtonText.fontSize = 12;
            }

            LayoutElement telemetryScopeLayout = telemetryScopeButton.gameObject.AddComponent<LayoutElement>();
            telemetryScopeLayout.preferredWidth = 146f;
            telemetryScopeLayout.minWidth = 132f;

            telemetryPanelRect = CreateContainer(bottomPanel, "TelemetryPanel", 104f);
            telemetryPanelLayoutElement = telemetryPanelRect.GetComponent<LayoutElement>();
            telemetryPanelLayoutElement.minHeight = 88f;
            telemetryPanelLayoutElement.preferredHeight = 104f;
            telemetryPanelRect.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.13f, 0.18f, 0.95f);

            telemetryPanelText = CreateText(telemetryPanelRect, "TelemetryPanelText", 13, FontStyle.Normal, TextAnchor.UpperLeft,
                new Color(0.88f, 0.93f, 0.99f, 1f));
            telemetryPanelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            telemetryPanelText.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(telemetryPanelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -8f));

            pendingTokenRect = new GameObject("PendingToken", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            pendingTokenRect.transform.SetParent(canvasRect, false);
            pendingTokenBackgroundImage = pendingTokenRect.GetComponent<Image>();
            pendingTokenBackgroundImage.color = PendingTokenNeutralColor;
            pendingTokenRect.sizeDelta = new Vector2(296f, 118f);
            pendingTokenRect.pivot = new Vector2(0.5f, 0.5f);
            pendingTokenRect.anchorMin = new Vector2(0.5f, 0.5f);
            pendingTokenRect.anchorMax = new Vector2(0.5f, 0.5f);
            pendingTokenRect.localScale = Vector3.one;
            pendingTokenBaseScale = Vector3.one;
            pendingTokenRect.gameObject.SetActive(false);

            pendingRecommendationAssistText = CreateText(pendingTokenRect, "PendingRecommendationAssist", 12, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.80f, 0.98f, 0.86f, 0.96f));
            pendingRecommendationAssistText.horizontalOverflow = HorizontalWrapMode.Overflow;
            pendingRecommendationAssistText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform pendingRecommendationAssistRect = pendingRecommendationAssistText.rectTransform;
            pendingRecommendationAssistRect.anchorMin = new Vector2(1f, 0f);
            pendingRecommendationAssistRect.anchorMax = new Vector2(1f, 0f);
            pendingRecommendationAssistRect.pivot = new Vector2(1f, 0f);
            pendingRecommendationAssistRect.sizeDelta = new Vector2(124f, 24f);
            pendingRecommendationAssistRect.anchoredPosition = new Vector2(-8f, 6f);
            pendingRecommendationAssistText.gameObject.SetActive(false);

            pendingDragGhostRect = new GameObject("PendingDragGhost", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            pendingDragGhostRect.transform.SetParent(canvasRect, false);
            pendingDragGhostRect.anchorMin = new Vector2(0.5f, 0.5f);
            pendingDragGhostRect.anchorMax = new Vector2(0.5f, 0.5f);
            pendingDragGhostRect.pivot = new Vector2(0.5f, 0.5f);
            pendingDragGhostRect.sizeDelta = new Vector2(156f, 156f);
            Image pendingDragGhostBackground = pendingDragGhostRect.GetComponent<Image>();
            pendingDragGhostBackground.color = new Color(0.07f, 0.10f, 0.15f, 0.78f);
            pendingDragGhostBackground.raycastTarget = false;

            RectTransform pendingDragGhostGrid = new GameObject("PendingDragGhostGrid", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            pendingDragGhostGrid.transform.SetParent(pendingDragGhostRect, false);
            Stretch(pendingDragGhostGrid, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));

            GridLayoutGroup pendingDragGhostLayout = pendingDragGhostGrid.GetComponent<GridLayoutGroup>();
            pendingDragGhostLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            pendingDragGhostLayout.constraintCount = 3;
            pendingDragGhostLayout.cellSize = new Vector2(36f, 36f);
            pendingDragGhostLayout.spacing = new Vector2(6f, 6f);
            pendingDragGhostLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            pendingDragGhostLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            pendingDragGhostLayout.childAlignment = TextAnchor.MiddleCenter;

            pendingDragGhostCells = new Image[9];
            pendingDragGhostHighlights = new Image[9];
            pendingDragGhostOutlines = new Image[9];
            for (int i = 0; i < pendingDragGhostCells.Length; i++)
            {
                RectTransform ghostCell = new GameObject("PendingDragGhostCell" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                ghostCell.transform.SetParent(pendingDragGhostGrid, false);
                Image cellImage = ghostCell.GetComponent<Image>();
                cellImage.color = PendingDragGhostOffColor;
                cellImage.raycastTarget = false;
                pendingDragGhostCells[i] = cellImage;

                RectTransform outline = new GameObject("PendingDragGhostOutline" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                outline.transform.SetParent(ghostCell, false);
                Stretch(outline, Vector2.zero, Vector2.one, new Vector2(-1f, -1f), new Vector2(1f, 1f));
                Image outlineImage = outline.GetComponent<Image>();
                outlineImage.color = new Color(0f, 0f, 0f, 0f);
                outlineImage.raycastTarget = false;
                pendingDragGhostOutlines[i] = outlineImage;

                RectTransform highlight = new GameObject("PendingDragGhostHighlight" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                highlight.transform.SetParent(ghostCell, false);
                Stretch(highlight, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
                Image highlightImage = highlight.GetComponent<Image>();
                highlightImage.color = new Color(1f, 1f, 1f, 0f);
                highlightImage.raycastTarget = false;
                pendingDragGhostHighlights[i] = highlightImage;
            }
            pendingDragGhostRect.gameObject.SetActive(false);

            RectTransform pendingTokenContent = new GameObject("PendingTokenContent", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            pendingTokenContent.transform.SetParent(pendingTokenRect, false);
            Stretch(pendingTokenContent, Vector2.zero, Vector2.one, new Vector2(10f, 8f), new Vector2(-10f, -8f));

            HorizontalLayoutGroup pendingTokenLayout = pendingTokenContent.GetComponent<HorizontalLayoutGroup>();
            pendingTokenLayout.spacing = 10f;
            pendingTokenLayout.childControlWidth = true;
            pendingTokenLayout.childControlHeight = true;
            pendingTokenLayout.childForceExpandWidth = true;
            pendingTokenLayout.childForceExpandHeight = true;

            RectTransform pendingPreviewFrame = new GameObject("PendingPreviewFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            pendingPreviewFrame.transform.SetParent(pendingTokenContent, false);
            pendingPreviewFrame.GetComponent<Image>().color = new Color(0.08f, 0.11f, 0.16f, 0.97f);
            LayoutElement pendingPreviewLayout = pendingPreviewFrame.GetComponent<LayoutElement>();
            pendingPreviewLayout.preferredWidth = 112f;
            pendingPreviewLayout.minWidth = 104f;
            pendingPreviewLayout.flexibleHeight = 1f;

            RectTransform pendingPreviewGrid = new GameObject("PendingPreviewGrid", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            pendingPreviewGrid.transform.SetParent(pendingPreviewFrame, false);
            Stretch(pendingPreviewGrid, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));

            GridLayoutGroup pendingPreviewGridLayout = pendingPreviewGrid.GetComponent<GridLayoutGroup>();
            pendingPreviewGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            pendingPreviewGridLayout.constraintCount = 3;
            pendingPreviewGridLayout.cellSize = new Vector2(20f, 20f);
            pendingPreviewGridLayout.spacing = new Vector2(4f, 4f);
            pendingPreviewGridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            pendingPreviewGridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            pendingPreviewGridLayout.childAlignment = TextAnchor.MiddleCenter;

            pendingTokenPreviewCells = new Image[9];
            for (int i = 0; i < pendingTokenPreviewCells.Length; i++)
            {
                RectTransform previewCell = new GameObject("PendingPreviewCell" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                previewCell.transform.SetParent(pendingPreviewGrid, false);
                pendingTokenPreviewCells[i] = previewCell.GetComponent<Image>();
            }

            RectTransform pendingIconFrame = new GameObject("PendingIngredientIconFrame", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            pendingIconFrame.transform.SetParent(pendingPreviewFrame, false);
            pendingIconFrame.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 0.96f);
            pendingIconFrame.anchorMin = new Vector2(1f, 1f);
            pendingIconFrame.anchorMax = new Vector2(1f, 1f);
            pendingIconFrame.pivot = new Vector2(1f, 1f);
            pendingIconFrame.sizeDelta = new Vector2(44f, 44f);
            pendingIconFrame.anchoredPosition = new Vector2(-6f, -6f);

            pendingTokenIngredientImage = new GameObject("PendingIngredientIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            pendingTokenIngredientImage.transform.SetParent(pendingIconFrame, false);
            pendingTokenIngredientImage.preserveAspect = true;
            Stretch(pendingTokenIngredientImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));

            pendingTokenIngredientBadge = CreateText(pendingIconFrame, "PendingIngredientBadge", 11, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.95f, 0.97f, 1f, 1f));
            Stretch(pendingTokenIngredientBadge.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform pendingInfoRoot = new GameObject("PendingInfoRoot", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            pendingInfoRoot.transform.SetParent(pendingTokenContent, false);
            LayoutElement pendingInfoLayout = pendingInfoRoot.GetComponent<LayoutElement>();
            pendingInfoLayout.flexibleWidth = 1f;
            pendingInfoLayout.minWidth = 154f;

            pendingTokenText = CreateText(pendingInfoRoot, "PendingTokenText", 15, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            pendingTokenText.horizontalOverflow = HorizontalWrapMode.Wrap;
            pendingTokenText.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(pendingTokenText.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 12f), new Vector2(-2f, -2f));

            RectTransform rotateStrip = new GameObject("PendingRotateStrip", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            rotateStrip.transform.SetParent(pendingInfoRoot, false);
            rotateStrip.anchorMin = new Vector2(0.5f, 0f);
            rotateStrip.anchorMax = new Vector2(0.5f, 0f);
            rotateStrip.pivot = new Vector2(0.5f, 0f);
            rotateStrip.sizeDelta = new Vector2(132f, 34f);
            rotateStrip.anchoredPosition = new Vector2(0f, 2f);

            HorizontalLayoutGroup rotateLayout = rotateStrip.GetComponent<HorizontalLayoutGroup>();
            rotateLayout.spacing = 6f;
            rotateLayout.childAlignment = TextAnchor.MiddleCenter;
            rotateLayout.childControlWidth = true;
            rotateLayout.childControlHeight = true;
            rotateLayout.childForceExpandWidth = false;
            rotateLayout.childForceExpandHeight = false;

            pendingRotateLeftHotspot = BuildPendingRotateHotspot(rotateStrip, false);
            pendingRotateRightHotspot = BuildPendingRotateHotspot(rotateStrip, true);

            SetPendingTokenPreviewPlaceholder();

            actionsRow1Rect = CreateContainer(bottomPanel, "ActionsRow1", 48f);
            actionsRow1LayoutElement = actionsRow1Rect.GetComponent<LayoutElement>();
            HorizontalLayoutGroup row1 = actionsRow1Rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            row1.spacing = 8f;
            row1.childControlWidth = true;
            row1.childControlHeight = true;
            row1.childForceExpandWidth = true;
            row1.childForceExpandHeight = true;

            drawButton = BuildActionButton(actionsRow1Rect, "Draw\n[D]", () => model.DrawIngredient());
            drawButtonText = drawButton.GetComponentInChildren<Text>();
            sellButton = BuildActionButton(actionsRow1Rect, "Sell\n[S]", () => model.SellIngredient());
            sellButtonText = sellButton.GetComponentInChildren<Text>();
            rotateLeftButton = BuildActionButton(actionsRow1Rect, "Rotate L\n[Q]", () => model.RotatePendingCounterClockwise());
            rotateRightButton = BuildActionButton(actionsRow1Rect, "Rotate R\n[E]", () => model.RotatePendingClockwise());
            recipeButton = BuildActionButton(actionsRow1Rect, "Recipe\n[F]", () => model.TriggerRandomRecipe());
            recipeButtonText = recipeButton.GetComponentInChildren<Text>();
            nextWaveButton = BuildActionButton(actionsRow1Rect, "Next Wave\n[N]", RequestNextWaveFromHud);
            nextWaveButtonText = nextWaveButton.GetComponentInChildren<Text>();

            actionsRow2Rect = CreateContainer(bottomPanel, "ActionsRow2", 48f);
            actionsRow2LayoutElement = actionsRow2Rect.GetComponent<LayoutElement>();
            HorizontalLayoutGroup row2 = actionsRow2Rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            row2.spacing = 8f;
            row2.childControlWidth = true;
            row2.childControlHeight = true;
            row2.childForceExpandWidth = true;
            row2.childForceExpandHeight = true;

            ventButton = BuildActionButton(actionsRow2Rect, "Vent\n[C]", () => model.VentHeat());
            ventButtonText = ventButton.GetComponentInChildren<Text>();
            comboBurstButton = BuildActionButton(actionsRow2Rect, "Burst\n[Space]", () => model.ActivateComboBurst());
            comboBurstButtonText = comboBurstButton.GetComponentInChildren<Text>();
            exportTelemetryButton = BuildActionButton(actionsRow2Rect, "Export UX\n[K]", () => ExportTelemetryReportFromHud("manual_button", true));
            resetRunButton = BuildActionButton(actionsRow2Rect, "Reset\n[R]", RequestRunResetFromHud);

            RefreshTelemetryControls();
            RefreshTelemetryPanel();
        }

        private void BuildEventPanel(Transform canvasRoot)
        {
            RectTransform panel = new GameObject("EventPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.transform.SetParent(canvasRoot, false);
            Stretch(panel, new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.82f), Vector2.zero, Vector2.zero);
            panel.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
            panel.gameObject.SetActive(false);
            eventPanel = panel.gameObject;

            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            eventTitleText = CreateText(panel, "EventTitle", 24, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            eventTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            eventDescText = CreateText(panel, "EventDesc", 16, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.90f, 0.93f, 0.98f, 1f));
            eventDescText.horizontalOverflow = HorizontalWrapMode.Wrap;
            eventDescText.verticalOverflow = VerticalWrapMode.Overflow;
            eventDescText.gameObject.AddComponent<LayoutElement>().preferredHeight = 120f;

            eventOptionButtons.Clear();
            eventOptionTexts.Clear();
            for (int i = 0; i < 3; i++)
            {
                int capture = i;
                Button button = BuildActionButton(panel, "Option " + (i + 1), () => model.ChooseEventOption(capture));
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
                eventOptionButtons.Add(button);
                eventOptionTexts.Add(button.GetComponentInChildren<Text>());
            }
        }

        private Button BuildActionButton(Transform parent, string label, UnityEngine.Events.UnityAction callback)
        {
            GameObject buttonObject = new GameObject(label.Replace("\n", string.Empty) + "Button",
                typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.22f, 0.28f, 0.36f, 0.97f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.30f, 0.40f, 0.50f, 1f);
            colors.pressedColor = new Color(0.18f, 0.24f, 0.30f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.16f, 0.16f, 0.16f, 0.75f);
            button.colors = colors;
            button.targetGraphic = image;
            if (callback != null)
            {
                button.onClick.AddListener(callback);
            }

            Text text = CreateText(buttonObject.GetComponent<RectTransform>(), "ButtonText", 18, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            text.text = label;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));

            return button;
        }

        private RectTransform BuildPendingRotateHotspot(Transform parent, bool clockwise)
        {
            string nodeName = clockwise ? "PendingRotateRight" : "PendingRotateLeft";
            RectTransform rect = new GameObject(nodeName, typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            rect.transform.SetParent(parent, false);

            Image image = rect.GetComponent<Image>();
            image.color = new Color(0.20f, 0.25f, 0.33f, 0.96f);

            LayoutElement layout = rect.GetComponent<LayoutElement>();
            layout.preferredWidth = 62f;
            layout.preferredHeight = 30f;
            layout.minWidth = 56f;
            layout.minHeight = 28f;

            Text label = CreateText(rect, "Label", 13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            label.text = clockwise ? "ROT R" : "ROT L";
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return rect;
        }

        private RectTransform CreateContainer(Transform parent, string name, float preferredHeight)
        {
            RectTransform container = new GameObject(name, typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            container.transform.SetParent(parent, false);
            container.GetComponent<LayoutElement>().preferredHeight = preferredHeight;
            return container;
        }

        private RectTransform CreatePanel(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.transform.SetParent(parent, false);
            Stretch(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.GetComponent<Image>();
            image.color = color;
            // Decorative background panels should not consume pointer input.
            image.raycastTarget = false;
            return rect;
        }

        private Text CreateText(Transform parent, string name, int size, FontStyle style, TextAnchor align, Color color)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            rect.transform.SetParent(parent, false);
            Text text = rect.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = align;
            text.color = color;
            text.supportRichText = false;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void ApplyPanelLayout()
        {
            if (topPanelRect == null || midPanelRect == null || bottomPanelRect == null)
            {
                return;
            }

            float width = Mathf.Max(1f, canvasRect != null ? canvasRect.rect.width : Screen.width);
            float height = Mathf.Max(1f, canvasRect != null ? canvasRect.rect.height : Screen.height);
            float portrait01 = Mathf.Clamp01((height / width - 1.4f) / 1.2f);
            bool gameplayFocusHud = IsGameplayFocusHudActive();

            if (gameplayFocusHud)
            {
                bool hasPlacementContext = model != null && (model.HasPendingBlock || model.HasDrawChoice || model.IsRestPhase);
                bool dragFocus = model != null && model.HasPendingBlock && IsUltraFocusActive();
                GameplayFocusLayoutMetrics focusLayout = CalculateGameplayFocusLayout(
                    width,
                    height,
                    hasPlacementContext,
                    dragFocus,
                    minimalCombatStripActive);
                bool combatOnlyFocus = !dragFocus && !hasPlacementContext;
                bool minimalCombatStrip = focusLayout.MinimalCombatStrip;
                float topStartFocus = focusLayout.TopStart01;
                float bottomTopFocus = focusLayout.BottomTop01;

                float sideMargin = dragFocus ? 4f : (hasPlacementContext ? 12f : (minimalCombatStrip ? 6f : 8f));
                Vector2 topOffsetMin = dragFocus
                    ? new Vector2(sideMargin, -8f)
                    : (minimalCombatStrip ? new Vector2(sideMargin, -5f) : new Vector2(sideMargin, -10f));
                Vector2 topOffsetMax = dragFocus
                    ? new Vector2(-sideMargin, -4f)
                    : (minimalCombatStrip ? new Vector2(-sideMargin, -2f) : new Vector2(-sideMargin, -6f));
                Vector2 bottomOffsetMin = new Vector2(sideMargin, 6f);
                Vector2 bottomOffsetMax = dragFocus
                    ? new Vector2(-sideMargin, -8f)
                    : (hasPlacementContext ? new Vector2(-sideMargin, -12f) : (minimalCombatStrip ? new Vector2(-sideMargin, -4f) : new Vector2(-sideMargin, -6f)));

                if (minimalCombatStrip)
                {
                    // Keep a narrow info strip on the left to free the center and avoid cue overlap on the right.
                    Stretch(topPanelRect, new Vector2(0.02f, topStartFocus), new Vector2(0.66f, 1f), new Vector2(2f, -4f), new Vector2(-2f, -2f));
                }
                else
                {
                    Stretch(topPanelRect, new Vector2(0f, topStartFocus), new Vector2(1f, 1f), topOffsetMin, topOffsetMax);
                }
                Stretch(midPanelRect, new Vector2(0f, bottomTopFocus), new Vector2(1f, bottomTopFocus), Vector2.zero, Vector2.zero);
                Stretch(bottomPanelRect, new Vector2(0f, 0f), new Vector2(1f, bottomTopFocus), bottomOffsetMin, bottomOffsetMax);
                UpdateDrawChoiceRiskUiScale();
                return;
            }

            float topStartExpanded = Mathf.Lerp(0.60f, 0.64f, portrait01);
            float topStartCompact = Mathf.Lerp(0.66f, 0.72f, portrait01);
            float topStart = compactHudMode ? topStartCompact : topStartExpanded;

            float midHeightExpanded = Mathf.Lerp(0.08f, 0.07f, portrait01);
            float midHeightCompact = Mathf.Lerp(0.06f, 0.05f, portrait01);
            float midHeight = compactHudMode ? midHeightCompact : midHeightExpanded;
            float midStart = Mathf.Clamp01(topStart - midHeight);

            Stretch(topPanelRect, new Vector2(0f, topStart), new Vector2(1f, 1f), new Vector2(12f, -10f), new Vector2(-12f, -6f));
            Stretch(midPanelRect, new Vector2(0f, midStart), new Vector2(1f, topStart), new Vector2(12f, 2f), new Vector2(-12f, -2f));
            Stretch(bottomPanelRect, new Vector2(0f, 0f), new Vector2(1f, midStart), new Vector2(12f, 6f), new Vector2(-12f, -12f));
            UpdateDrawChoiceRiskUiScale();
        }

        private bool IsTallPortraitLayout()
        {
            float width = Mathf.Max(1f, canvasRect != null ? canvasRect.rect.width : Screen.width);
            float height = Mathf.Max(1f, canvasRect != null ? canvasRect.rect.height : Screen.height);
            return height / width >= 1.85f;
        }

        private void RefreshHudDensityButton()
        {
            if (hudDensityButton == null)
            {
                return;
            }

            if (hudDensityButtonText != null)
            {
                hudDensityButtonText.text = compactHudMode ? "HUD Compact\n[H]" : "HUD Expanded\n[H]";
            }

            Image image = hudDensityButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = compactHudMode
                    ? new Color(0.20f, 0.50f, 0.78f, 0.98f)
                    : new Color(0.26f, 0.33f, 0.46f, 0.97f);
            }
        }

        private void UpdateInventoryGridSizing()
        {
            RectTransform sizingTarget = inventoryGridShakeRoot != null
                ? inventoryGridShakeRoot
                : (inventoryGridShakeLayer != null ? inventoryGridShakeLayer : inventoryGridHolder);
            if (sizingTarget == null || inventoryGridLayout == null)
            {
                return;
            }

            if (inventoryGridShakeRoot != null)
            {
                inventoryGridShakeRoot.anchorMin = new Vector2(0f, 1f);
                inventoryGridShakeRoot.anchorMax = new Vector2(0f, 1f);
                inventoryGridShakeRoot.pivot = new Vector2(0f, 1f);
                inventoryGridShakeRoot.localScale = Vector3.one;
                inventoryGridShakeRoot.localRotation = Quaternion.identity;
            }

            float width = sizingTarget.rect.width;
            float height = sizingTarget.rect.height;
            if (width <= 1f || height <= 1f)
            {
                return;
            }

            float boardSize = Mathf.Max(1f, Mathf.Min(width, height));

            if (inventoryGridShakeLayer != null)
            {
                inventoryGridShakeLayer.anchorMin = new Vector2(0f, 1f);
                inventoryGridShakeLayer.anchorMax = new Vector2(0f, 1f);
                inventoryGridShakeLayer.pivot = new Vector2(0f, 1f);
                inventoryGridShakeLayer.localScale = Vector3.one;
                inventoryGridShakeLayer.localRotation = Quaternion.identity;
                inventoryGridShakeLayer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, boardSize);
                inventoryGridShakeLayer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, boardSize);
                inventoryGridShakeLayer.anchoredPosition = ResolveInventoryGridRestAnchoredPosition(inventoryGridShakeLayer);
            }

            float spacingX = inventoryGridLayout.spacing.x;
            float spacingY = inventoryGridLayout.spacing.y;

            if (inventoryGridHolder != null)
            {
                inventoryGridHolder.anchorMin = new Vector2(0f, 1f);
                inventoryGridHolder.anchorMax = new Vector2(0f, 1f);
                inventoryGridHolder.pivot = new Vector2(0f, 1f);
                inventoryGridHolder.localScale = Vector3.one;
                inventoryGridHolder.localRotation = Quaternion.identity;
                inventoryGridHolder.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, boardSize);
                inventoryGridHolder.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, boardSize);
                inventoryGridHolder.anchoredPosition = ResolveInventoryGridRestAnchoredPosition(inventoryGridHolder);
            }

            float usableWidth = Mathf.Max(1f, boardSize - spacingX * (FoodTruckRunModel.InventoryWidth - 1f));
            float usableHeight = Mathf.Max(1f, boardSize - spacingY * (FoodTruckRunModel.InventoryHeight - 1f));

            float cellByWidth = usableWidth / FoodTruckRunModel.InventoryWidth;
            float cellByHeight = usableHeight / FoodTruckRunModel.InventoryHeight;
            float naturalCellSize = Mathf.Max(1f, Mathf.Min(cellByWidth, cellByHeight));
            float minCellSize = Mathf.Clamp(inventoryGridCellSizeMinRuntime, 32f, 220f);
            float maxCellSize = Mathf.Clamp(inventoryGridCellSizeMaxRuntime, minCellSize, 240f);
            float cellSize = Mathf.Min(naturalCellSize, maxCellSize);

            inventoryGridLayout.cellSize = new Vector2(cellSize, cellSize);
            inventoryGridLayout.childAlignment = TextAnchor.MiddleCenter;
            CacheInventoryGridBaseAnchoredPosition();
        }

        private void StabilizeInventoryLayout()
        {
            if (bottomPanelRect == null || inventoryGridHolder == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(bottomPanelRect);
            ApplyPanelLayout();
            UpdateInventoryGridSizing();
            Canvas.ForceUpdateCanvases();

            ResetInventoryGridShakeState();
            inventoryLayoutInitialized = true;
        }
    }
}
