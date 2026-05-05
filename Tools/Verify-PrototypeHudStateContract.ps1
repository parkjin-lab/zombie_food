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
    playModeVerification = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.PlayModeVerification.cs"
    pendingAssist = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.PendingPlacementAssist.cs"
    placementFeedback = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.PlacementFeedback.cs"
    presentation = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.PresentationActions.cs"
    enemyVisuals = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.EnemyVisuals.cs"
    prototypeVfx = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.PrototypeVfx.cs"
    telemetry = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.Telemetry.cs"
    editorMenu = "Assets\Scripts\Editor\FoodTruckPrototypePlayModeVerificationMenu.cs"
    model = "Assets\Scripts\Prototype\FoodTruckRunModel.cs"
    tests = "Assets\Tests\EditMode\FoodTruckRunModelTests.cs"
    playModeSuiteVerifier = "Tools\Verify-PrototypePlayModeSuite.ps1"
    playModeScreenshotVerifier = "Tools\Verify-PrototypePlayModeScreenshots.ps1"
    playModeReviewPackWriter = "Tools\Write-PrototypePlayModeReviewPack.ps1"
    playModeResultWriter = "Tools\Write-PrototypePlayModeResultFromSuite.ps1"
    gate = "Tools\Gate-Verification.ps1"
    sessionStatus = "Tools\Show-PrototypeSessionStatus.ps1"
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
    'BuildDrawChoiceTacticalChipLine(choice, resolvedAssistTag, valueBucket, riskTag)',
    'EstimateDrawChoiceFitSlots(choice)',
    '"Fit " + fitSlots + "  Heat +" + heatCost + "  Role " + roleLabel',
    'GetDrawChoiceRoleLabel(choice, assistTag, valueBucket, riskTag, fitSlots)',
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
    'BuildPlacementBlockedActionHint(blockedReason, lastPlacementBlockedFailReason)',
    '" | Next: " + recoveryHint',
    '"Recommend R1 slot " + (recommendedAnchorCells[0] + 1)',
    '"Recommend R1/R2 slots " + (recommendedAnchorCells[0] + 1) + "/" + (recommendedAnchorCells[1] + 1)',
    'string pressureTag = GetPlacementPressureTag(hottestPressure);',
    '"cover L" + (hottestLane + 1) + " " + pressureTag',
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
    'private static PlacementFailReason ResolvePlacementFailReasonFromText(string reason)',
    'private string BuildPlacementBlockedActionHint(string reason, PlacementFailReason failReason)',
    'private string BuildRecommendedRetryHint(string prefix)',
    'case PlacementFailReason.OutOfBounds:',
    'case PlacementFailReason.Occupied:',
    'case PlacementFailReason.InvalidAnchor:',
    'case PlacementFailReason.NoPendingBlock:',
    'ShowPlacementBlockedCue("Drop onto a valid inventory slot.");',
    'TriggerPendingPlacementFeedback(false, Array.Empty<int>())'
) "Failure feedback must be readable near the grid, not only in logs."

Add-ContractCheck $checks "invalid_placement" "blocked_cues_include_next_action" ($sources.pendingAssist + $sources.presentation) @(
    'string recoveryHint = BuildPlacementBlockedActionHint(blockedReason, lastPlacementBlockedFailReason);',
    '"Placement blocked: " + blockedReason +',
    'string.IsNullOrEmpty(recoveryHint) ? string.Empty : " " + recoveryHint',
    '" | Next: " + recoveryHint'
) "Blocked placement cues should include a short corrective next action."

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

Add-ContractCheck $checks "wave_outcome" "model_tracks_wave_payoff_summary" $sources.model @(
    'public string LastWaveOutcomeSummary => lastWaveOutcomeSummary;',
    'public string LastWaveOutcomeCue => lastWaveOutcomeCue;',
    'CaptureWaveOutcomeBaseline();',
    'CaptureWaveOutcomeSummary();',
    'waveDamageDealt',
    'waveEnemiesDefeated',
    'waveTruckHits',
    'FormatSignedRounded'
) "Wave transitions must preserve a concise payoff summary that links combat results to the next decision."

Add-ContractCheck $checks "wave_outcome" "hud_surfaces_wave_payoff_cue" $sources.hud @(
    'model.LastWaveOutcomeCue',
    'CreateChip("Wave: " + BuildWaveOutcomeChipText(lastWaveCue)',
    'public static string BuildWaveOutcomeChipText(string cue)',
    '"Wave " + model.Wave + " started. Keep your lanes stable."'
) "Wave change feedback should prioritize the last wave payoff when one is available."

Add-ContractCheck $checks "combat_feedback" "floating_damage_text_explains_hits_and_leaks" ($sources.hud + $sources.enemyVisuals) @(
    'private sealed class CombatFloatingTextWidget',
    'private readonly List<CombatFloatingTextWidget> combatFloatingTexts',
    'UpdateCombatFloatingTexts(dt);',
    'SpawnTruckDamageFloater(truckDamageThisFrame);',
    'SpawnEnemyDamageFloater(widget, damageTaken, knockout);',
    'SpawnEnemyLeakFloater(widget);',
    'SpawnCombatFloatingText(',
    '"TRUCK " + BuildCombatDamageLabel(damageAmount)',
    '"LEAK"',
    '"KO"'
) "Combat feedback should label enemy damage, knockouts, leaks, and truck HP loss directly in the battlefield."

Add-ContractCheck $checks "recipe_feedback" "recipe_activation_payload_explains_cause" $sources.model @(
    'public string LastRecipeActivationName => lastRecipeActivationName;',
    'public string LastRecipeActivationSummary => lastRecipeActivationSummary;',
    'public string LastRecipeActivationCue => lastRecipeActivationCue;',
    'private bool TriggerRandomRecipe(string cause)',
    'TriggerRandomRecipe("Placement bonus roll")',
    'TriggerRandomRecipe("Auto-merge bonus roll")',
    'TriggerRandomRecipe("Merge bonus roll")',
    'TriggerRandomRecipe("Recipe Rush event")',
    'BuildRecipeActivationCause(prefix, pair.Key, pair.Value, maxGrade)',
    'lastRecipeActivationCue = BuildRecipeActivationCue(template, cause);',
    'private static string BuildRecipeActivationPayload(RecipeTemplate template, string cause)',
    'EmitPresentationTrigger(PresentationTriggerType.RecipeActivated, payload)'
) "Recipe activation feedback should explain whether the recipe came from placement, merge, event, or bingo conditions."

Add-ContractCheck $checks "recipe_feedback" "hud_keeps_recent_recipe_cause_visible" ($sources.hud + $sources.presentation) @(
    'model.LastRecipeActivationCue',
    'CreateChip("Last: " + lastRecipeCue',
    'lastActivatedRecipeName = model != null && !string.IsNullOrEmpty(model.LastRecipeActivationName)',
    'ExtractRecipeNameFromActivationPayload(payload)',
    'payload.IndexOf(" from ", StringComparison.Ordinal)'
) "The recipe source should remain visible after the activation banner fades, and chip pulse should still target the active recipe name."

Add-ContractCheck $checks "recipe_feedback" "active_recipe_chips_describe_effect_role" $sources.hud @(
    'BuildRecipeEffectChipText(recipe)',
    'public static string BuildRecipeEffectChipText(RecipeState recipe)',
    'BuildRecipeLiveProgressChipText(recipe)',
    'public static string BuildRecipeLiveProgressChipText(RecipeState recipe)',
    '"Regen/Cool " + power',
    '"Lane Hit " + power',
    '"Warming up"',
    'CreateChip(chipText, color, 320f, 380f, 13)'
) "Active recipe chips should show what the recipe is doing, not only name and remaining time."

Add-ContractCheck $checks "recipe_feedback" "recipe_expiry_reports_accumulated_payoff" ($sources.model + $sources.hud + $sources.presentation) @(
    'RecipeExpired',
    'public string LastRecipeResultSummary => lastRecipeResultSummary;',
    'public string LastRecipeResultCue => lastRecipeResultCue;',
    'recipe.DamageDealt += effectiveDealt;',
    'recipe.EnemiesDefeated += 1;',
    'recipe.HpRestored += Mathf.Max(0f, TruckHp - hpBefore);',
    'recipe.HeatRelieved += Mathf.Max(0f, heatBefore - Heat);',
    'public static string BuildRecipeImpactSummary(RecipeState recipe)',
    'EmitPresentationTrigger(PresentationTriggerType.RecipeExpired, lastRecipeResultSummary)',
    'CreateChip("Result: " + lastRecipeResultCue'
) "Recipe expiry feedback should summarize accumulated damage, KOs, recovery, and Heat relief."

Add-ContractCheck $checks "editor_helpers" "hud_can_prepare_manual_capture_states" $sources.playModeVerification @(
    'public enum PlayModeVerificationState',
    'DrawChoice',
    'PendingPlacement',
    'InvalidPlacement',
    'WaveCombat',
    'public bool TryPreparePlayModeVerificationState(PlayModeVerificationState state, out string message)',
    'BuildPlayModeVerificationStateSummary()'
) "PC-limited sessions need one-click setup for each remaining manual capture state."

Add-ContractCheck $checks "editor_helpers" "wave_combat_capture_has_action_showcase" ($sources.playModeVerification + $sources.enemyVisuals) @(
    'SpawnWaveCombatVerificationShowcase();',
    'ClearTransientCombatVisuals();',
    'Prepared Wave Combat: lanes, truck, enemies, HP, Heat, attack labels, and Wave status should be readable.',
    'SpawnVerificationCombatLabel(',
    'SpawnTruckDamageFloater(7f);',
    '"-12"',
    '"KO"',
    '"LEAK"',
    'TriggerLaneHitFlash(1);',
    'TriggerLaneHitFlash(2);'
) "Wave Combat captures should include a readable action moment, not only a static lane overview."

Add-ContractCheck $checks "editor_helpers" "menu_exposes_prepare_and_capture_actions" $sources.editorMenu @(
    'Prepare State/Draw Choice',
    'Prepare State/Pending Placement',
    'Prepare State/Invalid Placement',
    'Prepare State/Wave Combat',
    'Prepare and Capture State/Draw Choice',
    'Prepare and Capture State/Pending Placement',
    'Prepare and Capture State/Invalid Placement',
    'Prepare and Capture State/Wave Combat'
) "The Editor menu must expose low-interaction state setup and capture paths."

Add-ContractCheck $checks "editor_helpers" "snapshot_draft_includes_prepared_state_summary" $sources.editorMenu @(
    'Prepared state: ',
    'Prepare result: ',
    'HUD state summary: ',
    'hud.BuildPlayModeVerificationStateSummary()'
) "Captured drafts should explain which verification state was generated."

Add-ContractCheck $checks "editor_helpers" "suite_capture_batches_manual_evidence" $sources.editorMenu @(
    'Capture Verification Suite',
    'SuiteDraftRelativePath',
    'VerificationSuiteCaptureState',
    'ContinueVerificationSuiteCapture',
    'BuildVerificationSuiteDraft',
    'lastVerificationSuiteEntries'
) "The Editor helper should batch all required states into one low-interaction evidence pass."

Add-ContractCheck $checks "editor_helpers" "suite_evidence_is_machine_checkable" $sources.playModeSuiteVerifier @(
    'Prototype_PlayMode_Verification_Suite.txt',
    'playmode_suite_status',
    'missing_states',
    'missing_screenshots',
    'captured_count',
    'expected_state_count',
    'wave_combat_action_showcase_ready',
    'wave_combat_action_showcase_reason',
    'Get-WaveCombatActionShowcaseStatus'
) "The low-interaction suite evidence must be parsable before manual PASS/FIX recording."

Add-ContractCheck $checks "editor_helpers" "screenshot_evidence_quality_is_machine_checkable" $sources.playModeScreenshotVerifier @(
    'Read-PngHeader',
    'playmode_screenshot_status',
    'low_resolution_count',
    'missing_states',
    'unlabeled_count',
    'visual_review_required'
) "Captured Play Mode screenshots should have a local quality and coverage check before manual review."

Add-ContractCheck $checks "editor_helpers" "suite_status_is_in_gate_and_session_status" ($sources.gate + $sources.sessionStatus) @(
    'Verify-PrototypePlayModeSuite.ps1',
    'playmode_suite',
    'playmode_suite_status',
    'playmode_suite_captured_count'
) "The suite evidence status should be visible in gate and session readiness output."

Add-ContractCheck $checks "editor_helpers" "screenshot_status_is_in_gate_and_session_status" ($sources.gate + $sources.sessionStatus) @(
    'Verify-PrototypePlayModeScreenshots.ps1',
    'playmode_screenshots',
    'playmode_screenshot_status',
    'playmode_screenshot_count'
) "Screenshot evidence quality should be visible in gate and session readiness output."

Add-ContractCheck $checks "editor_helpers" "review_pack_collects_evidence_for_visual_decision" $sources.playModeReviewPackWriter @(
    'Prototype_PlayMode_ReviewPack.md',
    'Verify-PrototypePlayModeSuite.ps1',
    'Verify-PrototypePlayModeScreenshots.ps1',
    'Verify-PrototypePlayModeRecord.ps1',
    'Screenshot Contact Sheet',
    'Visual Acceptance Checklist',
    'Wave Combat action showcase',
    '`-12`, `KO`, `LEAK`, `TRUCK -7`',
    'Recommended Result Commands',
    'Write-TextWithFallback',
    'visual_review_required'
) "Manual visual review should have a single evidence pack before PASS/FIX/BLOCKED recording."

Add-ContractCheck $checks "editor_helpers" "pass_record_reuses_suite_manifest_evidence" $sources.editorMenu @(
    'CollectManualResultScreenshotEvidence',
    'AddSuiteManifestEvidence',
    'Regex.Matches',
    'SuiteDraftRelativePath',
    'AddEvidencePath(paths, seen, match.Groups["path"].Value, true)',
    'Screenshots captured: '
) "The PASS record helper should preserve suite screenshot evidence even after Editor state reloads."

Add-ContractCheck $checks "editor_helpers" "suite_result_writer_handles_pass_fix_and_blocked" $sources.playModeResultWriter @(
    'Write-PrototypePlayModeResultFromSuite.ps1',
    '[ValidateSet("PASS", "FIX_LAYOUT", "FIX_ASSET", "FIX_FEEDBACK", "BLOCKED", "NOT_RECORDED")]',
    'Prototype_PlayMode_Verification_ResultDraft.txt',
    'Verify-PrototypePlayModeSuite.ps1',
    'Get-RecordStatusPreview',
    'Write-TextWithFallback',
    'Latest Manual Result',
    '-Apply'
) "Manual suite review should be recordable without hand-editing markdown for FIX or BLOCKED outcomes."

Add-ContractCheck $checks "regression_tests" "editmode_covers_fail_reasons_and_layout_visibility" $sources.tests @(
    'TryPlacePendingAtCell_WithoutPendingBlock_RecordsNoPendingFailure',
    'TryPlacePendingAtCell_InvalidAnchor_RecordsInvalidAnchorFailure',
    'TryPlacePendingAtCell_OverlapWithTwoBlocks_DoesNotAutoMerge',
    'TryPlacePendingAtCell_ThirdMatchingBlock_ActivatesRecipeBingo',
    'TriggerRandomRecipe_LogsActivationCause',
    'BuildRecipeEffectChipText_DescribesPassiveAndActiveRoles',
    'BuildRecipeLiveProgressChipText_UsesPayoffOrWarmup',
    'BuildRecipeImpactSummary_ReportsAccumulatedPayoff',
    'BuildRecipeImpactSummary_WhenNoPayoff_ReportsNoPayoff',
    'BuildWaveOutcomeChipText_TrimsTrailingPeriod',
    'model.LastRecipeActivationCue',
    'Tick_WhenWaveAdvances_RecordsOutcomeSummary',
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
    groups = @("draw_choice", "pending_placement", "invalid_placement", "telemetry", "wave_outcome", "combat_feedback", "recipe_feedback", "editor_helpers", "regression_tests")
    checks = $checks.ToArray()
    notes = @(
        "This is a source-level contract for Draw Choice, Pending Placement, Invalid Placement, and Recipe Feedback HUD states.",
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
