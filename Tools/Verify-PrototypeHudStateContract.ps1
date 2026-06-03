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
    inventoryCells = "Assets\Scripts\Prototype\FoodTruckPrototypeHud.InventoryCells.cs"
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
    playModeRetakePlanWriter = "Tools\Write-PrototypePlayModeRetakePlan.ps1"
    playModeRetakePlanVerifier = "Tools\Verify-PrototypePlayModeRetakePlan.ps1"
    playModeEvidencePreflight = "Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1"
    playModeManualEvidenceRegister = "Tools\Register-PrototypePlayModeManualEvidence.ps1"
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
    'ResolveDrawChoiceIntentLabel(choice, assistTag, valueBucket, riskTag, fitSlots, currentHeat, warningHeat, overheatHeat)',
    '"  Intent " + intentLabel',
    '"Value " + valueBucket + "  Risk " + riskTag'
) "Draw cards must remain explainable without opening extra panels and expose a live-context intent label."

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
    'public static string BuildBoardLocalPlacementFailLabel(',
    'private string BuildPlacementBlockedActionHint(string reason, PlacementFailReason failReason)',
    'private string BuildRecommendedRetryHint(string prefix)',
    'case PlacementFailReason.OutOfBounds:',
    'case PlacementFailReason.Occupied:',
    'case PlacementFailReason.InvalidAnchor:',
    'case PlacementFailReason.NoPendingBlock:',
    'ShowPlacementBlockedCue("Drop onto a valid inventory slot.");',
    'TriggerPendingPlacementFeedback(false, Array.Empty<int>())'
) "Failure feedback must be readable near the grid, not only in logs."

Add-ContractCheck $checks "invalid_placement" "board_local_fail_labels_render_on_cells" ($sources.hud + $sources.uiBootstrap + $sources.inventoryCells + $sources.pendingAssist + $sources.tests) @(
    'private readonly List<Text> inventoryCellBlockedLabels',
    'CreateText(cellRect, "CellBlockedLabel"',
    'inventoryCellBlockedLabels.Add(blockedLabel);',
    'bool isBlockedAnchorCell = hasPending && hoverAnchorCell == i && !hoverValid',
    'bool isFailedFeedbackCell = pendingPlacementFeedbackTimer > 0f',
    'BuildBoardLocalPlacementFailLabel(',
    'blockedLabel.gameObject.SetActive(true)',
    'BuildBoardLocalPlacementFailLabel_UsesShortCellLocalCopy'
) "Invalid placement should show compact reason copy directly on the blocked board cell."

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
    'public string LastWaveOutcomeBestContributor => lastWaveOutcomeBestContributor;',
    'CaptureWaveOutcomeBaseline();',
    'CaptureWaveOutcomeSummary();',
    'waveDamageDealt',
    'waveEnemiesDefeated',
    'waveTruckHits',
    'lastWaveOutcomeBestContributor = BuildWaveOutcomeBestContributor(',
    'FormatSignedRounded'
) "Wave transitions must preserve a concise payoff summary that links combat results to the next decision."

Add-ContractCheck $checks "wave_outcome" "model_tracks_wave_best_contributor" ($sources.model + $sources.tests) @(
    'public static string BuildWaveOutcomeBestContributor(',
    'return "Best KO chain";',
    'return "Best damage";',
    'return "Best combo x" + bestComboStreak;',
    'return "Best cooling";',
    'return "Best recovery";',
    'return "Best stock";',
    'BuildWaveOutcomeBestContributor_PrioritizesVisiblePayoffCause',
    'Assert.AreEqual("Best combo x3", model.LastWaveOutcomeBestContributor);'
) "Wave payoff should name the most visible contributor so the player can learn from the result."

Add-ContractCheck $checks "wave_outcome" "hud_surfaces_wave_payoff_cue" $sources.hud @(
    'model.LastWaveOutcomeCue',
    'CreateChip("Wave: " + BuildWaveOutcomeChipText(lastWaveCue)',
    'public static string BuildWaveOutcomeChipText(string cue)',
    '"Wave " + model.Wave + " started. Keep your lanes stable."'
) "Wave change feedback should prioritize the last wave payoff when one is available."

Add-ContractCheck $checks "wave_cadence" "model_composes_wave_schedule_without_random_rolls" $sources.model @(
    'public sealed class WaveCadencePlan',
    'public WaveCadencePlan LastWaveCadencePlan => lastWaveCadencePlan;',
    'public string LastWaveCadenceSummary => lastWaveCadencePlan != null ? lastWaveCadencePlan.Summary : string.Empty;',
    'public bool LastWaveCadencePlannedSpike => lastWaveCadencePlan != null && lastWaveCadencePlan.PlannedSpike;',
    'private static WaveCadencePlan BuildWaveCadencePlan(int wave)',
    'bool progressionUnlock = wave == 4 || wave == 7;',
    'bool weatherRotation = wave % 4 == 0;',
    'bool bossPressureSpike = wave % 5 == 0;',
    'bool runEvent = wave % 3 == 0;',
    'bool plannedSpike = bossPressureSpike || scheduledBeatCount >= 2;'
) "Wave rhythm scheduling must be inspectable without consuming weather or event random rolls."

Add-ContractCheck $checks "wave_cadence" "editmode_covers_core_cadence_beats" $sources.tests @(
    'Tick_WhenWaveThreeStarts_ReportsEventCadence',
    'Tick_WhenWaveFourStarts_ReportsUnlockWeatherPlannedSpike',
    'Tick_WhenWaveFiveStarts_ReportsBossRestPlannedSpike',
    'Tick_WhenWaveSevenStarts_ReportsUnlockCadenceWithoutSpike',
    'AdvanceToWave(FoodTruckRunModel model, int targetWave)'
) "EditMode coverage should lock the first event, unlock/weather overlap, boss/rest spike, and non-spike unlock beat."

Add-ContractCheck $checks "payoff_to_read" "model_builds_next_decision_hint_from_wave_payoff" $sources.model @(
    'private string lastWaveOutcomeNextHint = string.Empty;',
    'public string LastWaveOutcomeNextHint => lastWaveOutcomeNextHint;',
    'lastWaveOutcomeNextHint = BuildWaveOutcomeNextHint(',
    'public static string BuildWaveOutcomeNextHint(',
    'return "Stabilize lanes";',
    'return "Pick COOL/SAFE";',
    'return "Push damage";',
    'return "Keep combo window";',
    'return "Keep balanced draw";'
) "Wave payoff should become a compact next-decision hint, not only a result label."

Add-ContractCheck $checks "payoff_to_read" "hud_keeps_next_hint_visible_near_wave_chip" $sources.hud @(
    'model.LastWaveOutcomeNextHint',
    'CreateChip("Next: " + BuildPayoffToReadHintChipText(nextDecisionHint)',
    'public static string BuildPayoffToReadHintChipText(string hint)',
    'const int maxLength = 28;'
) "The next-decision hint should persist in the same compact read area as wave payoff, with bounded copy length."

Add-ContractCheck $checks "payoff_to_read" "editmode_covers_next_hint_cases" $sources.tests @(
    'BuildWaveOutcomeNextHint_WhenTruckLeaks_PrioritizesLaneStability',
    'BuildWaveOutcomeNextHint_WhenHeatSpikes_RecommendsSafeCoolingDraw',
    'BuildWaveOutcomeNextHint_WhenDamagePaysOff_RecommendsPushingDamage',
    'BuildPayoffToReadHintChipText_TrimsLongHints'
) "EditMode coverage should lock leak, Heat, damage payoff, and copy-trimming hint behavior."

Add-ContractCheck $checks "rhythm_beat" "model_resolves_named_loop_beats" $sources.model @(
    'public enum RhythmBeatType',
    'Read,',
    'Commit,',
    'Pressure,',
    'Payoff,',
    'Release',
    'public RhythmBeatType CurrentRhythmBeat => ResolveRhythmBeat(',
    'public string CurrentRhythmBeatLabel => BuildRhythmBeatLabel(CurrentRhythmBeat);',
    'public static RhythmBeatType ResolveRhythmBeat(',
    'public static string BuildRhythmBeatLabel(RhythmBeatType beat)'
) "The run model should expose the current rhythm beat as a stable design-language enum and label."

Add-ContractCheck $checks "rhythm_beat" "hud_and_telemetry_surface_current_beat" ($sources.hud + $sources.telemetry) @(
    'string rhythmBeat = model.CurrentRhythmBeatLabel;',
    '"Beat " + rhythmBeat',
    'BuildRhythmBeatTelemetryLine(',
    'BuildRhythmBeatMiniText(model.CurrentRhythmBeatLabel, telemetryCurrentRhythmBeatSeconds)',
    'rhythm_beat_duration_s',
    'rhythm_beat_transition_count',
    'CsvEscape(rhythmBeat)',
    'telemetryCurrentRhythmBeatSeconds.ToString("0.###", CultureInfo.InvariantCulture)',
    'telemetryWaveBaseRhythmBeatTransitionCount = telemetryRhythmBeatTransitionCount;',
    'ApplyTelemetryScope(telemetryRhythmBeatTransitionCount, telemetryWaveBaseRhythmBeatTransitionCount)',
    'scopedRhythmBeatTransitions.ToString(CultureInfo.InvariantCulture)',
    'CsvEscape(waveCadence)',
    'model.LastWaveCadenceSummary'
) "HUD and UX telemetry should reuse the same current beat label without adding a large new panel."

Add-ContractCheck $checks "rhythm_beat" "editmode_covers_rhythm_beat_mapping" $sources.tests @(
    'ResolveRhythmBeat_ReadStates_PrioritizeChoices',
    'ResolveRhythmBeat_CommitAndReleaseStates_AreExplicit',
    'ResolveRhythmBeat_PayoffOnlyCoversEarlyWaveWindow',
    'BuildRhythmBeatLabel_ReturnsPlayerFacingNames',
    'BuildRhythmBeatTelemetryLine_IncludesDurationTransitionsAndCadence',
    'BuildRhythmBeatMiniText_ClampsNegativeDuration'
) "EditMode coverage should lock Read, Commit, Pressure, Payoff, and Release mapping semantics."

Add-ContractCheck $checks "pressure_ramp" "model_exposes_pressure_ramp_profile" $sources.model @(
    'public enum PressureRampPhase',
    'Build,',
    'Climb,',
    'Peak',
    'public PressureRampPhase CurrentPressureRampPhase => ResolvePressureRampPhase(',
    'public string CurrentPressureRampLabel => BuildPressureRampLabel(CurrentPressureRampPhase);',
    'public float CurrentPressureRampIntensity01 => BuildPressureRampIntensity01(',
    'public float CurrentPressureRampSpawnMultiplier => BuildPressureRampSpawnMultiplier(',
    'public static PressureRampPhase ResolvePressureRampPhase(',
    'public static float BuildPressureRampIntensity01(',
    'public static float BuildPressureRampSpawnMultiplier(',
    'spawnIntensity *= CurrentPressureRampSpawnMultiplier;'
) "The model should expose and apply a bounded pressure ramp tuning value."

Add-ContractCheck $checks "pressure_ramp" "hud_and_telemetry_surface_pressure_ramp" ($sources.hud + $sources.telemetry) @(
    'string pressureRamp = model.CurrentPressureRampLabel;',
    'string pressureRampTuning = pressureRamp + " x" + model.CurrentPressureRampSpawnMultiplier.ToString("0.00");',
    '"  |  Ramp " + pressureRampTuning',
    'pressure_ramp_phase,pressure_ramp_intensity,pressure_ramp_spawn_mult',
    'CsvEscape(pressureRamp)',
    'CsvEscape(pressureRampIntensity)',
    'CsvEscape(pressureRampSpawnMultiplier)',
    '"Pressure Ramp: " + model.CurrentPressureRampLabel'
) "HUD and UX telemetry should show the current pressure ramp phase and spawn multiplier."

Add-ContractCheck $checks "pressure_ramp" "editmode_covers_pressure_ramp_profile" $sources.tests @(
    'ResolvePressureRampPhase_MapsWaveProgressToBuildClimbPeak',
    'ResolvePressureRampPhase_WhenFlowLocked_ReturnsBuild',
    'BuildPressureRampIntensity_UsesSmoothProgressAndFlowLocks',
    'BuildPressureRampSpawnMultiplier_EasesFromLowToPeakPressure'
) "EditMode coverage should lock pressure ramp phase thresholds and flow-lock behavior."

Add-ContractCheck $checks "first_block_combat_lock" "model_locks_combat_until_first_block_is_placed" $sources.model @(
    'public bool CombatFlowLocked => IsCombatFlowLocked();',
    'public string CombatFlowLockReason => GetCombatFlowLockReason();',
    'return EventPending || HasDrawChoice || HasPendingBlock || placedBlocks.Count == 0;',
    '"Draw/place 1 block to start combat"',
    'if (!combatFlowLocked)',
    'if (!IsOverheated || IsRestPhase || CombatFlowLocked)'
) "The run model should pause combat pressure, wave time, and overheat chip until the first block is actually placed."

Add-ContractCheck $checks "first_block_combat_lock" "hud_surfaces_first_block_ready_state" $sources.hud @(
    'if (model.CombatFlowLocked)',
    '"READY  |  " + model.CombatFlowLockReason',
    '"  |  Wave paused"'
) "The HUD should explain why the first wave is paused instead of feeling frozen."

Add-ContractCheck $checks "first_block_combat_lock" "editmode_covers_first_block_start_gate" $sources.tests @(
    'Tick_BeforeFirstBlock_DoesNotStartCombatOrWaveTimer',
    'Tick_WithPendingBlock_DoesNotStartCombatUntilPlaced',
    'Assert.AreEqual("Draw/place 1 block to start combat", model.CombatFlowLockReason);',
    'Assert.AreEqual("Place pending block to start combat", model.CombatFlowLockReason);',
    'SeedStarterBlock(model)'
) "EditMode coverage should lock the initial preparation gate and prove existing wave-advance tests opt into combat by seeding a starter block."

Add-ContractCheck $checks "rest_reward" "model_applies_named_rest_reward" $sources.model @(
    'public enum RestRewardProfile',
    'public RestRewardProfile LastRestRewardProfile => lastRestRewardProfile;',
    'public string LastRestRewardLabel => BuildRestRewardLabel(lastRestRewardProfile);',
    'public string LastRestRewardSummary => lastRestRewardSummary;',
    'ApplyRestPhaseReward();',
    'public static RestRewardProfile ResolveRestRewardProfile(',
    'public static string BuildRestRewardSummary(RestRewardProfile profile)'
) "Rest should become a visible release reward beat instead of only a timer."

Add-ContractCheck $checks "rest_reward" "hud_and_telemetry_surface_rest_reward" ($sources.hud + $sources.telemetry) @(
    'string restReward = model.IsRestPhase && !string.IsNullOrEmpty(model.LastRestRewardSummary)',
    '"  |  Rest " + model.LastRestRewardLabel',
    'BuildRestRewardChipText(model.LastRestRewardLabel, model.LastRestRewardSummary)',
    'public static string BuildRestRewardChipText(string label, string summary)',
    '"Release " + resolvedLabel + " | " + detail',
    'rest_reward,rest_reward_summary',
    'CsvEscape(restReward)',
    'CsvEscape(restRewardSummary)',
    '"Rest Reward: " + model.LastRestRewardLabel'
) "HUD and UX telemetry should expose which release reward the rest phase granted."

Add-ContractCheck $checks "rest_reward" "editmode_covers_rest_reward_profile" $sources.tests @(
    'ResolveRestRewardProfile_PrioritizesRepairCoolingThenStock',
    'BuildRestRewardSummary_ReturnsReadablePayoff',
    'BuildRestRewardChipText_CondensesReleaseReward'
) "EditMode coverage should lock rest reward priority and copy."

Add-ContractCheck $checks "combat_feedback" "floating_damage_text_explains_hits_and_leaks" ($sources.hud + $sources.enemyVisuals) @(
    'private sealed class CombatFloatingTextWidget',
    'private readonly List<CombatFloatingTextWidget> combatFloatingTexts',
    'private float combatFloatingTextDuration = 1.18f;',
    'private float combatFloatingTextRiseSpeed = 26f;',
    'private float laneHitFlashDuration = 0.52f;',
    'UpdateCombatFloatingTexts(dt);',
    'SpawnTruckDamageFloater(truckDamageThisFrame, model.LastTruckDamageCauseLabel);',
    'SpawnEnemyDamageFloater(widget, damageTaken, knockout);',
    'SpawnEnemyLeakFloater(widget);',
    'SpawnCombatFloatingText(',
    'BuildEnemyDamageFloaterLabel(damageAmount, knockout)',
    'BuildTruckDamageFloaterLabel(causeLabel, damageAmount)',
    'BuildCombatHitFlowLabel("HIT", "Z"',
    'SpawnEnemyHitSlash(laneRoot, source.Rect.anchoredPosition, 0f, 1.00f',
    'SpawnEnemyHitSlash(laneRoot, source.Rect.anchoredPosition, 52f, 0.72f',
    'SpawnEnemyHitSlash(laneRoot, source.Rect.anchoredPosition, -48f, 0.58f',
    'rect.SetAsLastSibling();',
    'Mathf.Clamp(Mathf.RoundToInt(laneHeight * 0.29f), 19, 38)',
    'Mathf.Clamp(laneHeight * 1.90f, 128f, 300f)',
    '"LEAK"',
    '"KO"'
) "Combat feedback should label enemy damage, knockouts, leaks, and truck HP loss directly in the battlefield with compact source-to-target result flow."

Add-ContractCheck $checks "combat_feedback" "truck_damage_cause_labels_explain_hp_loss" ($sources.model + $sources.enemyVisuals + $sources.playModeVerification + $sources.tests) @(
    'public enum TruckDamageCause',
    'public TruckDamageCause LastTruckDamageCause => lastTruckDamageCause;',
    'public string LastTruckDamageCauseLabel => BuildTruckDamageCauseLabel(lastTruckDamageCause);',
    'ApplyTruckDamage(hitDamage, TruckDamageCause.Bite);',
    'ApplyTruckDamage(chip, TruckDamageCause.Pressure);',
    'ApplyTruckDamage(chip, TruckDamageCause.Overheat);',
    'public static string BuildTruckDamageCauseLabel(TruckDamageCause cause)',
    'public static int GetTruckDamageCausePriority(TruckDamageCause cause)',
    'SpawnTruckDamageFloater(7f, "BITE");',
    'BuildTruckDamageCauseLabel_ReturnsCauseForCombatFloaters',
    'BuildTruckDamageFloaterLabel_IncludesCauseAndAmount',
    'BuildEnemyDamageFloaterLabel_UsesSourceTargetResultFlow'
) "Truck HP loss should show whether the cause was a bite, pressure chip, or overheat."

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

Add-ContractCheck $checks "recipe_feedback" "draw_cards_preview_recipe_bingo_progress" ($sources.model + $sources.drawFlow + $sources.tests) @(
    'public string BuildDrawChoiceRecipeProgressLabel(PendingBlockState choice)',
    'CountPlacedBlocksByRecipeKey(choice.IngredientName, true) + 1',
    'CountPlacedBlocksByRecipeKey(choice.ShapeKey, false) + 1',
    '"Recipe Bingo x2"',
    '"Recipe Near " + choice.IngredientName + " " + ingredientProgress + "/3"',
    'model.BuildDrawChoiceRecipeProgressLabel(choice)',
    'BuildDrawChoiceRecipeProgressLabel_PreviewsNearAndBingoStates'
) "Draw cards should preview whether a card seeds, nears, or completes recipe bingo progress."

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
    'SpawnTruckDamageFloater(7f, "BITE");',
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
    'manual_partial',
    'captured_manual',
    'suite_capture_source',
    'wave_combat_action_showcase_ready',
    'wave_combat_action_showcase_reason',
    'Get-WaveCombatActionShowcaseStatus'
) "The low-interaction suite evidence must be parsable before manual PASS/FIX recording."

Add-ContractCheck $checks "editor_helpers" "manual_screenshot_evidence_can_update_suite_manifest" $sources.playModeManualEvidenceRegister @(
    'Register-PrototypePlayModeManualEvidence.ps1',
    '[ValidateSet("Draw Choice", "Pending Placement", "Invalid Placement", "Wave Combat")]',
    'manual screenshot registration',
    'manual_partial',
    'manual_completed',
    'WaveCombatActionShowcase',
    'Prototype_PlayMode_Verification_Suite.txt',
    'Read-PngHeader',
    'Write-TextWithFallback',
    'manual_evidence_status',
    'registered_count'
) "PC-limited sessions should be able to register standalone PNG evidence into the suite manifest without pretending it was captured by direct play."

Add-ContractCheck $checks "editor_helpers" "screenshot_evidence_quality_is_machine_checkable" $sources.playModeScreenshotVerifier @(
    'Read-PngHeader',
    'playmode_screenshot_status',
    'low_resolution_count',
    'missing_states',
    'unlabeled_count',
    'manual_registration_candidate_count',
    'manual_registration_commands',
    'New-ManualRegistrationCommand',
    'Prototype_PlayMode_Screenshot_Triage.txt',
    'triaged_non_state_count',
    'Triaged Non-State',
    'visual_review_required'
) "Captured Play Mode screenshots should have a local quality and coverage check before manual review."

Add-ContractCheck $checks "editor_helpers" "suite_status_is_in_gate_and_session_status" ($sources.gate + $sources.sessionStatus) @(
    'Verify-PrototypePlayModeSuite.ps1',
    'playmode_suite',
    'playmode_suite_status',
    'playmode_suite_captured_count',
    'wave_combat_action_showcase_ready',
    'wave_combat_action_showcase_reason',
    'Confirm wave_combat_action_showcase_ready=true before recording Wave Combat as PASS.'
) "The suite evidence status should be visible in gate and session readiness output."

Add-ContractCheck $checks "editor_helpers" "screenshot_status_is_in_gate_and_session_status" ($sources.gate + $sources.sessionStatus) @(
    'Verify-PrototypePlayModeScreenshots.ps1',
    'playmode_screenshots',
    'playmode_screenshot_status',
    'playmode_screenshot_count'
) "Screenshot evidence quality should be visible in gate and session readiness output."

Add-ContractCheck $checks "editor_helpers" "review_pack_status_is_in_session_status" $sources.sessionStatus @(
    'Write-PrototypePlayModeReviewPack.ps1',
    '"-PreviewOnly"',
    'review_pack_status',
    'review_readiness',
    'review_pack_visual_review_required',
    'Check review_readiness before generating or recording manual PASS/FIX/BLOCKED evidence.'
) "The first session status command should expose review pack readiness before writing review output."

Add-ContractCheck $checks "editor_helpers" "retake_plan_status_is_in_session_status" $sources.sessionStatus @(
    'Write-PrototypePlayModeRetakePlan.ps1',
    'Verify-PrototypePlayModeRetakePlan.ps1',
    'Invoke-PrototypePlayModeEvidencePreflight.ps1',
    '"-PreviewOnly"',
    'retake_plan_status',
    'retake_plan_focused_retake_count',
    'retake_plan_doc_status',
    'retake_plan_next_action',
    'Run Tools\Write-PrototypePlayModeRetakePlan.ps1 to generate a focused retake checklist before opening Unity.'
) "The first session status command should expose focused retake plan readiness before opening Unity."

Add-ContractCheck $checks "editor_helpers" "playmode_evidence_preflight_summarizes_capture_readiness" $sources.playModeEvidencePreflight @(
    'Invoke-PrototypePlayModeEvidencePreflight.ps1',
    'Show-PrototypeSessionStatus.ps1',
    'Verify-PrototypePlayModeRetakePlan.ps1',
    'Verify-PrototypePlayModeSuite.ps1',
    'Verify-PrototypePlayModeScreenshots.ps1',
    'Write-PrototypePlayModeReviewPack.ps1',
    'asset_planned_missing',
    'playmode_evidence_preflight_status',
    'ready_for_focused_retake',
    'ready_for_visual_review',
    'needs_retake_plan_refresh',
    'needs_screenshot_fix',
    'commands_after_capture',
    'focused_retake_states'
) "A single preflight should summarize whether the PC-limited session is ready for focused retakes or visual review."

Add-ContractCheck $checks "editor_helpers" "preflight_is_visible_in_session_status" $sources.sessionStatus @(
    'Invoke-PrototypePlayModeEvidencePreflight.ps1',
    'playmode_evidence_preflight',
    'Run Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1 before opening Unity to summarize focused retake readiness.'
) "Session status should point the user to the one-command preflight without invoking it recursively."

Add-ContractCheck $checks "editor_helpers" "retake_plan_status_is_in_gate" $sources.gate @(
    'Verify-PrototypePlayModeRetakePlan.ps1',
    'SkipPlayModeRetakePlan',
    'playmode_retake_plan',
    'retake_plan_doc_status',
    'failed_playmode_retake_plan'
) "The integrated gate should detect stale or missing focused retake plan docs."

Add-ContractCheck $checks "editor_helpers" "session_status_reports_next_work_focus" $sources.sessionStatus @(
    'top_issue',
    'next_evidence_action',
    'next_code_target',
    'asset_planned_missing',
    'Planned feedback resources still missing',
    'Track planned VFX/SFX backlog',
    'playmode_manual_registration_candidate_count',
    'playmode_triaged_non_state_count',
    'unlabeled PNG candidate',
    'Register-PrototypePlayModeManualEvidence.ps1',
    'captured_manual',
    'Manual Play Mode evidence is partial',
    'Play Mode verification suite is not captured',
    'Review pack preview failed',
    'Next code target: '
) "The session status should translate readiness into the next evidence action and code target."

Add-ContractCheck $checks "editor_helpers" "review_pack_collects_evidence_for_visual_decision" $sources.playModeReviewPackWriter @(
    'Prototype_PlayMode_ReviewPack.md',
    'Verify-PrototypePlayModeSuite.ps1',
    'Verify-PrototypePlayModeScreenshots.ps1',
    'Verify-PrototypePlayModeRecord.ps1',
    'captured_manual',
    'suite_capture_source',
    'Verify-PrototypeAssets.ps1',
    'Planned Resource Backlog',
    'asset_planned_missing',
    'missing_planned',
    'Register-PrototypePlayModeManualEvidence.ps1',
    'Manual Evidence Registration Hints',
    'manual_registration_commands',
    'Triaged Non-State Screenshots',
    'triaged_non_state_count',
    'Screenshot Contact Sheet',
    'Visual Acceptance Checklist',
    'Wave Combat action showcase',
    '``-12``, ``KO``, ``LEAK``, ``TRUCK -7``',
    'Recommended Result Commands',
    'Write-TextWithFallback',
    'visual_review_required'
) "Manual visual review should have a single evidence pack before PASS/FIX/BLOCKED recording."

Add-ContractCheck $checks "editor_helpers" "retake_plan_collects_focused_capture_work" $sources.playModeRetakePlanWriter @(
    'Prototype_PlayMode_RetakePlan.md',
    'Verify-PrototypePlayModeSuite.ps1',
    'Verify-PrototypePlayModeScreenshots.ps1',
    'Write-PrototypePlayModeReviewPack.ps1',
    'Prepare and Capture State',
    'Draw Choice',
    'Pending Placement',
    'Invalid Placement',
    'retake_plan_status',
    'focused_retake_count'
) "Focused retake planning should translate partial evidence into state-specific capture work."

Add-ContractCheck $checks "editor_helpers" "retake_plan_doc_is_machine_checkable" $sources.playModeRetakePlanVerifier @(
    'Verify-PrototypePlayModeRetakePlan.ps1',
    'Write-PrototypePlayModeRetakePlan.ps1',
    'Prototype_PlayMode_RetakePlan.md',
    'retake_plan_doc_status',
    'stale_doc',
    'missing_needles',
    'expected_focused_retake_count',
    'documented_focused_retake_count',
    'Prepare and Capture State'
) "Focused retake plan docs should fail fast when current evidence and the written checklist drift apart."

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
    groups = @("draw_choice", "pending_placement", "invalid_placement", "telemetry", "wave_outcome", "wave_cadence", "payoff_to_read", "rhythm_beat", "pressure_ramp", "rest_reward", "combat_feedback", "recipe_feedback", "editor_helpers", "regression_tests")
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
