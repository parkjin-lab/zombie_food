using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private void RefreshEventPanel()
        {
            bool show = model.EventPending || eventResolvePanelPulseTimer > 0f;
            eventPanel.SetActive(show);
            if (!show)
            {
                return;
            }

            if (!model.EventPending)
            {
                eventTitleText.text = "Event Resolved";
                eventDescText.text = "Choice applied. Return to your build flow.";
                for (int i = 0; i < eventOptionButtons.Count; i++)
                {
                    eventOptionButtons[i].gameObject.SetActive(false);
                }

                return;
            }

            RunEvent currentEvent = model.PendingEvent;
            eventTitleText.text = currentEvent.Title;
            eventDescText.text = currentEvent.Description;

            for (int i = 0; i < eventOptionButtons.Count; i++)
            {
                bool valid = i < currentEvent.Options.Length;
                eventOptionButtons[i].gameObject.SetActive(valid);
                if (!valid)
                {
                    continue;
                }

                RunEventOption option = currentEvent.Options[i];
                eventOptionTexts[i].text = option.Label + "\n" + option.EffectSummary;
            }
        }

        private void RefreshSpeedButtons()
        {
            RefreshSpeedCycleButton();
            RefreshHudDensityButton();
            RefreshTelemetryControls();
            RefreshActionButtons();
            HighlightBurstButton(comboBurstButton, model.CanActivateComboBurst);
            HighlightVentButton(ventButton, model.CanVentHeat, model.IsOverheated, model.VentCooldownRemaining);
        }

        private void RefreshSpeedCycleButton()
        {
            if (speedCycleButton == null)
            {
                return;
            }

            if (speedCycleButtonText != null)
            {
                speedCycleButtonText.text = "Speed x" + model.BattleSpeed.ToString("0.#") + "\n[T]";
            }

            HighlightSpeedCycleButton(speedCycleButton, model.BattleSpeed);
        }

        private void RefreshActionButtons()
        {
            if (model == null)
            {
                return;
            }

            bool flowLocked = model.EventPending || model.HasDrawChoice;
            bool hasPending = model.HasPendingBlock;
            bool hasPlacedBlock = HasPlacedBlockInGrid();
            bool nextWaveReady = CanTriggerNextWaveNow();

            if (drawButton != null)
            {
                drawButton.interactable = model.CanDrawIngredient;
            }
            if (drawButtonText != null)
            {
                drawButtonText.text = BuildDrawButtonLabel(flowLocked, hasPending);
                drawButtonText.fontSize = Application.isMobilePlatform ? 10 : 11;
            }

            if (sellButton != null)
            {
                sellButton.interactable = !flowLocked && (hasPending || hasPlacedBlock);
            }
            if (sellButtonText != null)
            {
                sellButtonText.text = BuildSellButtonLabel(flowLocked, hasPending, hasPlacedBlock);
                sellButtonText.fontSize = Application.isMobilePlatform ? 10 : 11;
            }

            if (rotateLeftButton != null)
            {
                rotateLeftButton.interactable = !flowLocked && hasPending;
            }

            if (rotateRightButton != null)
            {
                rotateRightButton.interactable = !flowLocked && hasPending;
            }

            if (recipeButton != null)
            {
                recipeButton.interactable = !flowLocked;
            }
            if (recipeButtonText != null)
            {
                recipeButtonText.text = BuildRecipeButtonLabel(flowLocked);
                recipeButtonText.fontSize = Application.isMobilePlatform ? 10 : 11;
            }

            if (nextWaveButton != null)
            {
                nextWaveButton.interactable = nextWaveReady;
            }
            if (nextWaveButtonText != null)
            {
                nextWaveButtonText.text = BuildNextWaveButtonLabel(nextWaveReady, flowLocked);
                nextWaveButtonText.fontSize = Application.isMobilePlatform ? 10 : 11;
            }

            if (ventButton != null)
            {
                ventButton.interactable = !flowLocked && model.CanVentHeat;
            }
            if (ventButtonText != null)
            {
                ventButtonText.text = BuildVentButtonLabel();
                ventButtonText.fontSize = Application.isMobilePlatform ? 11 : 12;
            }

            if (comboBurstButton != null)
            {
                comboBurstButton.interactable = !flowLocked && model.CanActivateComboBurst;
            }
            if (comboBurstButtonText != null)
            {
                comboBurstButtonText.text = BuildBurstButtonLabel();
                comboBurstButtonText.fontSize = Application.isMobilePlatform ? 10 : 11;
            }
        }

        private bool HasPlacedBlockInGrid()
        {
            for (int i = 0; i < FoodTruckRunModel.InventoryCellCount; i++)
            {
                if (model.GetCellBlockId(i) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void HighlightSpeedCycleButton(Button button, float speed)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (Mathf.Abs(speed - 1f) < 0.05f)
            {
                image.color = new Color(0.18f, 0.55f, 0.85f, 1f);
            }
            else if (Mathf.Abs(speed - 1.5f) < 0.05f)
            {
                image.color = new Color(0.20f, 0.62f, 0.72f, 1f);
            }
            else
            {
                image.color = new Color(0.22f, 0.70f, 0.54f, 1f);
            }
        }

        private static void HighlightBurstButton(Button button, bool ready)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (!button.interactable)
            {
                image.color = new Color(0.16f, 0.19f, 0.24f, 0.82f);
                return;
            }

            image.color = ready
                ? new Color(0.88f, 0.40f, 0.18f, 1f)
                : new Color(0.22f, 0.28f, 0.36f, 0.97f);
        }

        private void HandleKeyboardShortcuts()
        {
            if (model == null)
            {
                return;
            }

            if (KeyPressedThisFrame(KeyCode.Y))
            {
                ToggleTelemetryPanel();
            }

            if (KeyPressedThisFrame(KeyCode.U))
            {
                ToggleTelemetryScope();
            }

            if (KeyPressedThisFrame(KeyCode.K))
            {
                ExportTelemetryReportFromHud("manual_hotkey", true);
            }

            if (model.EventPending)
            {
                if (KeyPressedThisFrame(KeyCode.Alpha1)) model.ChooseEventOption(0);
                if (KeyPressedThisFrame(KeyCode.Alpha2)) model.ChooseEventOption(1);
                if (KeyPressedThisFrame(KeyCode.Alpha3)) model.ChooseEventOption(2);
                if (KeyPressedThisFrame(KeyCode.T)) CycleBattleSpeed();
                if (KeyPressedThisFrame(KeyCode.H)) ToggleHudDensity();
                if (KeyPressedThisFrame(KeyCode.R)) RequestRunResetFromHud();
                return;
            }

            if (model.HasDrawChoice)
            {
                if (KeyPressedThisFrame(KeyCode.Alpha1)) AttemptChooseDrawOptionFromHud(0);
                if (KeyPressedThisFrame(KeyCode.Alpha2)) AttemptChooseDrawOptionFromHud(1);
                if (KeyPressedThisFrame(KeyCode.Alpha3)) AttemptChooseDrawOptionFromHud(2);
                if (KeyPressedThisFrame(KeyCode.T)) CycleBattleSpeed();
                if (KeyPressedThisFrame(KeyCode.H)) ToggleHudDensity();
                if (KeyPressedThisFrame(KeyCode.R)) RequestRunResetFromHud();
                return;
            }

            if (KeyPressedThisFrame(KeyCode.D)) model.DrawIngredient();
            if (KeyPressedThisFrame(KeyCode.S)) model.SellIngredient();
            if (KeyPressedThisFrame(KeyCode.Q)) model.RotatePendingCounterClockwise();
            if (KeyPressedThisFrame(KeyCode.E)) model.RotatePendingClockwise();
            if (KeyPressedThisFrame(KeyCode.F)) model.TriggerRandomRecipe();
            if (KeyPressedThisFrame(KeyCode.T)) CycleBattleSpeed();
            if (KeyPressedThisFrame(KeyCode.H)) ToggleHudDensity();
            if (KeyPressedThisFrame(KeyCode.C)) model.VentHeat();
            if (KeyPressedThisFrame(KeyCode.Space)) model.ActivateComboBurst();
            if (KeyPressedThisFrame(KeyCode.M)) selectedMergeCell = -1;
            if (KeyPressedThisFrame(KeyCode.N)) RequestNextWaveFromHud();
            if (KeyPressedThisFrame(KeyCode.R)) RequestRunResetFromHud();
        }

        private void HandleGridMergeClick()
        {
            if (model.HasPendingBlock || model.HasDrawChoice || isDraggingPending || model.EventPending)
            {
                selectedMergeCell = -1;
                return;
            }

            if (!PrimaryPointerDownThisFrame())
            {
                return;
            }

            if (!TryGetGridCellAtScreen(GetPrimaryPointerPosition(), out int cellIndex))
            {
                selectedMergeCell = -1;
                return;
            }

            if (model.GetCellBlockId(cellIndex) < 0)
            {
                selectedMergeCell = -1;
                return;
            }

            if (selectedMergeCell < 0)
            {
                selectedMergeCell = cellIndex;
                return;
            }

            if (selectedMergeCell == cellIndex)
            {
                selectedMergeCell = -1;
                return;
            }

            bool merged = model.TryMergeBlocksFromCells(selectedMergeCell, cellIndex);
            if (merged)
            {
                RegisterTelemetryManualMergeSuccess();
            }
            selectedMergeCell = -1;
        }

        private static bool KeyPressedThisFrame(KeyCode keyCode)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            switch (keyCode)
            {
                case KeyCode.Alpha1:
                case KeyCode.Keypad1:
                    return keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame;
                case KeyCode.Alpha2:
                case KeyCode.Keypad2:
                    return keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame;
                case KeyCode.Alpha3:
                case KeyCode.Keypad3:
                    return keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame;
                case KeyCode.D:
                    return keyboard.dKey.wasPressedThisFrame;
                case KeyCode.S:
                    return keyboard.sKey.wasPressedThisFrame;
                case KeyCode.F:
                    return keyboard.fKey.wasPressedThisFrame;
                case KeyCode.C:
                    return keyboard.cKey.wasPressedThisFrame;
                case KeyCode.Space:
                    return keyboard.spaceKey.wasPressedThisFrame;
                case KeyCode.Q:
                    return keyboard.qKey.wasPressedThisFrame;
                case KeyCode.E:
                    return keyboard.eKey.wasPressedThisFrame;
                case KeyCode.T:
                    return keyboard.tKey.wasPressedThisFrame;
                case KeyCode.H:
                    return keyboard.hKey.wasPressedThisFrame;
                case KeyCode.Y:
                    return keyboard.yKey.wasPressedThisFrame;
                case KeyCode.U:
                    return keyboard.uKey.wasPressedThisFrame;
                case KeyCode.M:
                    return keyboard.mKey.wasPressedThisFrame;
                case KeyCode.N:
                    return keyboard.nKey.wasPressedThisFrame;
                case KeyCode.K:
                    return keyboard.kKey.wasPressedThisFrame;
                case KeyCode.R:
                    return keyboard.rKey.wasPressedThisFrame;
                default:
                    return false;
            }
        }

        private bool TryGetGridCellAtScreen(Vector2 screenPoint, out int cellIndex)
        {
            for (int i = 0; i < inventoryCellRects.Count; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(inventoryCellRects[i], screenPoint, null))
                {
                    cellIndex = i;
                    return true;
                }
            }

            cellIndex = -1;
            return false;
        }

        private void CycleBattleSpeed()
        {
            if (model == null)
            {
                return;
            }

            float current = model.BattleSpeed;
            if (current < 1.25f)
            {
                model.SetBattleSpeed(1.5f);
            }
            else if (current < 1.75f)
            {
                model.SetBattleSpeed(2f);
            }
            else
            {
                model.SetBattleSpeed(1f);
            }
        }

        private void RequestNextWaveFromHud()
        {
            if (model == null)
            {
                return;
            }

            if (model.EventPending || model.HasDrawChoice)
            {
                ShowCueBanner("Resolve current choice first.", new Color(0.94f, 0.67f, 0.18f, 1f));
                PlaySfx(sfxPlaceBlockedClip, 0.78f);
                return;
            }

            if (!model.IsRestPhase)
            {
                ShowCueBanner("Next Wave is available only during rest phase.", new Color(0.90f, 0.36f, 0.30f, 1f));
                PlaySfx(sfxPlaceBlockedClip, 0.78f);
                return;
            }

            model.ForceNextWave();
            PlaySfx(sfxSelectClip, 0.95f);
        }

        private void RequestRunResetFromHud()
        {
            if (model == null)
            {
                return;
            }

            if (autoExportTelemetryOnRunReset)
            {
                ExportTelemetryReportFromHud("run_reset", false);
            }

            model.ResetRun();
            AdvanceTelemetryRunSession();
            ResetTelemetryRunTracking(true);
            flowChecklistLogPrimed = false;
            lastFlowChecklistLogMessage = string.Empty;
            flowChecklistLogCooldownTimer = 0f;
            CaptureTelemetryWaveBaseline();
            previousCanVentHeatState = model.CanVentHeat;
            previousCanActivateBurstState = model.CanActivateComboBurst;
            previousNextWaveReadyState = CanTriggerNextWaveNow();
            nextWaveButtonPulseTimer = 0f;
            ResetInventoryGridShakeState();
            PlaySfx(sfxSelectClip, 0.85f);
        }

        private void ToggleHudDensity()
        {
            compactHudMode = !compactHudMode;
            ApplyPanelLayout();
            RefreshHudDensityButton();
            UpdateInventoryGridSizing();
            ResetInventoryGridShakeState();
            RefreshGameplayHudContextImmediate();
        }

        private static void HighlightVentButton(Button button, bool ready, bool overheated, float cooldownRemaining)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (!button.interactable)
            {
                image.color = new Color(0.16f, 0.19f, 0.24f, 0.82f);
                return;
            }

            if (ready)
            {
                image.color = overheated
                    ? new Color(0.96f, 0.45f, 0.18f, 1f)
                    : new Color(0.28f, 0.68f, 0.42f, 1f);
            }
            else if (cooldownRemaining > 0f)
            {
                image.color = new Color(0.24f, 0.47f, 0.74f, 0.97f);
            }
            else
            {
                image.color = overheated
                    ? new Color(0.42f, 0.16f, 0.12f, 0.97f)
                    : new Color(0.22f, 0.28f, 0.36f, 0.97f);
            }
        }
    }
}
