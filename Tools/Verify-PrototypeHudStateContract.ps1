param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"

function Read-Source {
    param([string]$RelativePath)

    $path = Join-Path $ProjectPath $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        return ""
    }

    return [System.IO.File]::ReadAllText($path)
}

function Add-ContractCheck {
    param(
        [System.Collections.Generic.List[object]]$Target,
        [string]$Group,
        [string]$Name,
        [string]$Source,
        [string[]]$Needles,
        [string]$Reason
    )

    $missing = New-Object System.Collections.Generic.List[string]
    foreach ($needle in $Needles) {
        if ([string]::IsNullOrEmpty($Source) -or -not $Source.Contains($needle)) {
            $missing.Add($needle)
        }
    }

    $ok = $missing.Count -eq 0
    $Target.Add([pscustomobject]@{
        group = $Group
        name = $Name
        status = if ($ok) { "ok" } else { "fail" }
        reason = $Reason
        missing = $missing.ToArray()
    }) | Out-Null
}

$relativeFiles = [ordered]@{
    hud = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.cs"
    uiBootstrap = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.UiBootstrap.cs"
    drawFlow = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.DrawChoiceFlow.cs"
    drawVisuals = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.DrawChoiceVisuals.cs"
    drawRisk = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.DrawChoiceRiskUI.cs"
    pendingAssist = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.PendingPlacementAssist.cs"
    placementFeedback = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.PlacementFeedback.cs"
    presentation = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.PresentationActions.cs"
    prototypeVfx = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.PrototypeVfx.cs"
    telemetry = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.Telemetry.cs"
    model = "Assets\Scripts\Prototype\FoodTruckRunModel.cs"
    tests = "Assets\Tests\EditMode\FoodTruckRunModelTests.cs"
}

$sources = @{}
$missingFiles = New-Object System.Collections.Generic.List[string]
foreach ($entry in $relativeFiles.GetEnumerator()) {
    $source = Read-Source $entry.Value
    $sources[$entry.Key] = $source
    if ([string]::IsNullOrEmpty($source)) {
        $missingFiles.Add($entry.Value)
    }
}

$checks = New-Object System.Collections.Generic.List[object]

Add-ContractCheck $checks "draw_choice" "three_card_row_bootstraps" $sources.uiBootstrap @(
    'drawChoiceRow = CreateContainer(bottomPanel, "DrawChoiceRow", 132f);',
    'for (int i = 0; i < 3; i++)',
    'Button choiceButton = BuildDrawChoiceCardButton(drawChoiceRow, i, () => AttemptChooseDrawOptionFromHud(capture));'
) "Draw phase must expose exactly three comparable card slots."

Add-ContractCheck $checks "draw_choice" "card_visuals_have_preview_icon_value_risk" $sources.drawVisuals @(
    'BuildDrawChoiceStatBar(statBarsRoot, "Value"',
    'BuildDrawChoiceStatBar(statBarsRoot, "Risk"',
    'RefreshDrawChoicePreview(int slot, PendingBlockState choice)',
    'RefreshDrawChoiceIngredientVisual(int slot, PendingBlockState choice)'
) "Each draw card must carry shape preview, ingredient identity, value, and risk."

Add-ContractCheck $checks "draw_choice" "card_text_has_tactical_labels" $sources.drawFlow @(
    'private string BuildDrawChoiceCardText(PendingBlockState choice, string assistTag)',
    'string targetLabel = GetTargetTypeLabel(choice.TargetType);',
    'string shapeLabel = GetShapeLabel(choice.ShapeKey);',
    '"  DPS " + dps.ToString("0.0")',
    '"Value " + valueBucket + "  Risk " + riskTag'
) "Draw cards must remain explainable without opening extra panels."

Add-ContractCheck $checks "draw_choice" "risk_scale_and_icons_are_bound" $sources.drawRisk @(
    'float value = EstimateDrawChoiceValue(choice);',
    'float riskScore = EstimateDrawChoiceRiskScore(choice);',
    'riskText.text = riskTag;',
    'riskIcon.sprite = GetDrawChoiceRiskSprite(riskTag);',
    'case "HIGH":',
    'case "MID":'
) "Risk information must be a visible label plus icon/color tier."

Add-ContractCheck $checks "draw_choice" "choice_pick_updates_model_and_telemetry" $sources.hud @(
    'private bool AttemptChooseDrawOptionFromHud(int slotIndex)',
    'string valueBucket = GetDrawChoiceValueBucket(selectedChoice);',
    'string riskTag = GetDrawChoiceRiskTag(selectedChoice);',
    'bool chosen = model.ChooseDrawOption(slotIndex);',
    'RegisterTelemetryDrawPick(valueBucket, riskTag);',
    'TriggerDrawChoicePickImpact(slotIndex, safePick);'
) "Choosing a card must move into pending placement and preserve UX telemetry."

Add-ContractCheck $checks "pending_placement" "pending_row_and_help_text_exist" $sources.uiBootstrap @(
    'pendingRowRect = CreateContainer(bottomPanel, "PendingRow", 58f);',
    'pendingHintText = CreateText(pendingSlotRect, "PendingHint"',
    'pendingHelpText = CreateText(pendingHelpRect, "HelpText"',
    '"Pick choice -> Rotate(Q/E) -> Drag or tap to place"'
) "Pending placement must tell the player the next action."

Add-ContractCheck $checks "pending_placement" "pending_hint_tracks_anchor_rotation_and_blocked_reason" $sources.pendingAssist @(
    '" | Slot " + (anchorCell + 1) + " " + readiness',
    '" | Slot " + (anchorCell + 1) + " BLOCKED"',
    'BuildPlacementBlockedHint()',
    '" | Rotate [Q/E] or ROT L/R | Drag/drop or tap another cell"',
    '" | Rot " + model.PendingRotationDegrees + "deg"'
) "Pending copy must update with hover state, rotation, and blocked context."

Add-ContractCheck $checks "pending_placement" "drag_tap_rotate_paths_are_gated" $sources.pendingAssist @(
    'if (model.EventPending)',
    'if (model.HasDrawChoice)',
    'if (!model.HasPendingBlock)',
    'TryHandlePendingRotateTouch(pointerDownPosition)',
    'RectTransformUtility.RectangleContainsScreenPoint(pendingTokenRect, pointerDownPosition, null)',
    'TryResolvePlacementAnchor(pointerDownPosition, true, out int tapCell)',
    'model.TryPlacePendingAtCell(tapCell)'
) "Pending placement must not leak input across event/draw/no-pending states."

Add-ContractCheck $checks "pending_placement" "token_preview_and_drag_ghost_are_visible" $sources.pendingAssist @(
    'RefreshPendingTokenVisual(model.PendingBlock);',
    'SetPendingDragGhostVisible(true);',
    'UpdatePendingDragGhostPosition(dragPosition);',
    'RefreshPendingDragGhostVisual(model.PendingBlock, pendingHoverValid);',
    'GetPendingTokenPreviewOffsets(pending)'
) "The selected block must be visible before and during drag."

Add-ContractCheck $checks "invalid_placement" "model_tracks_all_fail_reasons" $sources.model @(
    'NoPendingBlock',
    'InvalidAnchor',
    'OutOfBounds',
    'Occupied',
    'public string LastPlacementFailReasonText => GetPlacementFailReasonText(lastPlacementFailReason);',
    'placementBlockedOutOfBoundsCount',
    'placementBlockedOccupiedCount',
    'placementBlockedInvalidAnchorCount',
    'placementBlockedNoPendingCount'
) "Invalid placement needs stable reason codes and counters."

Add-ContractCheck $checks "invalid_placement" "fail_reason_text_is_actionable" $sources.model @(
    'return "No pending block.";',
    'return "Invalid anchor cell.";',
    'return "Block exceeds grid bounds.";',
    'return "That slot is already occupied.";'
) "The model reason text is the HUD source of truth."

Add-ContractCheck $checks "invalid_placement" "hud_maps_fail_reasons_to_near_target_feedback" $sources.pendingAssist @(
    'private static string BuildPlacementFailReasonHint(PlacementFailReason failReason)',
    'case PlacementFailReason.OutOfBounds:',
    'case PlacementFailReason.Occupied:',
    'case PlacementFailReason.InvalidAnchor:',
    'case PlacementFailReason.NoPendingBlock:',
    'ShowPlacementBlockedCue("Drop onto a valid inventory slot.");',
    'TriggerPendingPlacementFeedback(false, Array.Empty<int>())'
) "Failure feedback must be readable near the grid, not only in logs."

Add-ContractCheck $checks "invalid_placement" "presentation_uses_reason_specific_vfx" $sources.prototypeVfx @(
    'private Sprite ResolvePlacementFailVfxSprite(string fallbackReason)',
    'return placementFailNoPendingSprite != null ? placementFailNoPendingSprite : vfxPlaceFailSprite;',
    'return placementFailInvalidAnchorSprite != null ? placementFailInvalidAnchorSprite : vfxPlaceFailSprite;',
    'return placementFailOutOfBoundsSprite != null ? placementFailOutOfBoundsSprite : vfxPlaceFailSprite;',
    'return placementFailOccupiedSprite != null ? placementFailOccupiedSprite : vfxPlaceFailSprite;'
) "The four blocked reasons must keep their icon/VFX mapping."

Add-ContractCheck $checks "invalid_placement" "fail_micro_feedback_stays_enabled" $sources.placementFeedback @(
    'private void UpdatePlacementImpact(float dt)',
    'private void UpdatePlacementFailShake(float dt)',
    'private void ConfigurePlacementFeedbackPreset(bool success)',
    'case PlacementFeedbackPreset.Punchy:',
    'placementFailShakeDistanceRuntime = placementFailShakeDistance * 1.24f;'
) "Invalid placement should shake/flash with the configured micro-feedback preset."

Add-ContractCheck $checks "telemetry" "hud_reports_core_ux_metrics" $sources.telemetry @(
    'RegisterTelemetryDrawPick(string valueBucket, string riskTag)',
    'blockedOutOfBounds',
    'blockedOccupied',
    'blockedInvalidAnchor',
    'blockedNoPending',
    '"Draw Picks: [1] " + pick1',
    '"Pick Value: LOW " + valueLow + " | MID " + valueMid + " | HIGH " + valueHigh',
    '"Pick Risk: LOW " + riskLow + " | MID " + riskMid + " | HIGH " + riskHigh'
) "The HUD must preserve the next sprint's UX instrumentation path."

Add-ContractCheck $checks "regression_tests" "editmode_covers_fail_reasons_and_layout_visibility" $sources.tests @(
    'TryPlacePendingAtCell_WithoutPendingBlock_RecordsNoPendingFailure',
    'TryPlacePendingAtCell_InvalidAnchor_RecordsInvalidAnchorFailure',
    'TryPlacePendingAtCell_OverlapWithTwoBlocks_DoesNotAutoMerge',
    'HudActionVisibility_CombatOnlyIdle_ShowsBuildEntryRow',
    'HudActionVisibility_CombatOnlyUrgent_ShowsOnlyReactionRow',
    'HudActionVisibility_PlacementContext_KeepsInventoryGridVisible'
) "Existing EditMode coverage should catch the riskiest regressions before Play Mode."

$failedChecks = @($checks | Where-Object { $_.status -ne "ok" })
$contractStatus = if ($missingFiles.Count -gt 0) {
    "missing_source"
}
elseif ($failedChecks.Count -gt 0) {
    "failed"
}
else {
    "ok"
}

$result = [ordered]@{
    hud_contract_status = $contractStatus
    source_files_present = ($missingFiles.Count -eq 0)
    missing_files = $missingFiles.ToArray()
    check_count = $checks.Count
    failed_checks = $failedChecks.Count
    groups = @("draw_choice", "pending_placement", "invalid_placement", "telemetry", "regression_tests")
    checks = $checks.ToArray()
    notes = @(
        "This is a source-level contract for Draw Choice, Pending Placement, and Invalid Placement HUD states.",
        "It does not replace manual Unity Play Mode visual verification.",
        "Use it when this PC cannot reliably capture the remaining Play Mode states."
    )
}

if ($JsonOnly) {
    $result | ConvertTo-Json -Depth 8 -Compress | Write-Host
}
else {
    Write-Host ("hud_contract_status=" + $contractStatus)
    Write-Host ("source_files_present=" + ($missingFiles.Count -eq 0))
    Write-Host ("check_count=" + $checks.Count)
    Write-Host ("failed_checks=" + $failedChecks.Count)

    if ($missingFiles.Count -gt 0) {
        Write-Host "missing_files:"
        foreach ($file in $missingFiles) {
            Write-Host ("- " + $file)
        }
    }

    if ($failedChecks.Count -gt 0) {
        Write-Host "failed_contract_checks:"
        foreach ($check in $failedChecks) {
            Write-Host ("- " + $check.group + "/" + $check.name + ": " + $check.reason)
        }
    }
}

if ($contractStatus -ne "ok") {
    exit 1
}

exit 0
