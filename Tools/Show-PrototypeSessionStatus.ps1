param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"

function Invoke-JsonScript {
    param(
        [string]$ScriptPath,
        [string[]]$Arguments
    )

    if (-not (Test-Path -LiteralPath $ScriptPath)) {
        return [pscustomobject]@{
            ok = $false
            exit_code = 90
            data = $null
            error = "missing_script"
        }
    }

    $output = & powershell -ExecutionPolicy Bypass -File $ScriptPath @Arguments
    $exitCode = $LASTEXITCODE
    $jsonLine = $output | Select-Object -Last 1

    if ([string]::IsNullOrWhiteSpace([string]$jsonLine)) {
        return [pscustomobject]@{
            ok = $false
            exit_code = $exitCode
            data = $null
            error = "empty_output"
        }
    }

    try {
        return [pscustomobject]@{
            ok = ($exitCode -eq 0)
            exit_code = $exitCode
            data = ($jsonLine | ConvertFrom-Json)
            error = $null
        }
    }
    catch {
        return [pscustomobject]@{
            ok = $false
            exit_code = $exitCode
            data = $null
            error = "json_parse_failed: " + $_.Exception.Message
        }
    }
}

$gateScript = Join-Path $ProjectPath "Tools\Gate-Verification.ps1"
$assetScript = Join-Path $ProjectPath "Tools\Verify-PrototypeAssets.ps1"
$layoutScript = Join-Path $ProjectPath "Tools\Verify-PrototypeLayout.ps1"
$hudContractScript = Join-Path $ProjectPath "Tools\Verify-PrototypeHudStateContract.ps1"
$playModeRecordScript = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeRecord.ps1"
$playModeSuiteScript = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeSuite.ps1"
$playModeScreenshotsScript = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeScreenshots.ps1"
$playModeReviewPackScript = Join-Path $ProjectPath "Tools\Write-PrototypePlayModeReviewPack.ps1"
$playModeRetakePlanScript = Join-Path $ProjectPath "Tools\Write-PrototypePlayModeRetakePlan.ps1"
$playModeRetakePlanVerifierScript = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeRetakePlan.ps1"
$playModeEvidencePreflightScript = Join-Path $ProjectPath "Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1"
$playModeManualEvidenceScript = Join-Path $ProjectPath "Tools\Register-PrototypePlayModeManualEvidence.ps1"
$handoffPath = Join-Path $ProjectPath "Docs\Prototype_Session_Handoff.md"
$playModePath = Join-Path $ProjectPath "Docs\Prototype_PlayMode_Verification.md"
$playbookPath = Join-Path $ProjectPath "Docs\Prototype_NextStep_Playbook.md"

$gateResult = Invoke-JsonScript -ScriptPath $gateScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-RunTests",
    "-JsonOnly"
)

$assetResult = Invoke-JsonScript -ScriptPath $assetScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-Strict",
    "-JsonOnly"
)

$playModeRecordResult = Invoke-JsonScript -ScriptPath $playModeRecordScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-JsonOnly"
)

$hudContractResult = Invoke-JsonScript -ScriptPath $hudContractScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-JsonOnly"
)

$playModeSuiteResult = Invoke-JsonScript -ScriptPath $playModeSuiteScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-JsonOnly"
)

$playModeScreenshotsResult = Invoke-JsonScript -ScriptPath $playModeScreenshotsScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-JsonOnly"
)

$playModeReviewPackResult = Invoke-JsonScript -ScriptPath $playModeReviewPackScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-PreviewOnly",
    "-JsonOnly"
)

$playModeRetakePlanResult = Invoke-JsonScript -ScriptPath $playModeRetakePlanScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-PreviewOnly",
    "-JsonOnly"
)

$playModeRetakePlanVerifierResult = Invoke-JsonScript -ScriptPath $playModeRetakePlanVerifierScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-JsonOnly"
)

$gateData = $gateResult.data
$assetData = $assetResult.data
$verification = if ($null -ne $gateData) { $gateData.status } else { $null }
$gateAssets = if ($null -ne $gateData) { $gateData.assets } else { $null }
$gateLayout = if ($null -ne $gateData) { $gateData.layout } else { $null }
$gateHudContract = if ($null -ne $gateData) { $gateData.hud_contract } else { $null }
$gatePlayModeSuite = if ($null -ne $gateData) { $gateData.playmode_suite } else { $null }
$gatePlayModeScreenshots = if ($null -ne $gateData) { $gateData.playmode_screenshots } else { $null }
$playModeRecordData = $playModeRecordResult.data
$hudContractData = $hudContractResult.data
$playModeSuiteData = $playModeSuiteResult.data
$playModeScreenshotsData = $playModeScreenshotsResult.data
$playModeReviewPackData = $playModeReviewPackResult.data
$playModeRetakePlanData = $playModeRetakePlanResult.data
$playModeRetakePlanVerifierData = $playModeRetakePlanVerifierResult.data

$assetStatus = if ($null -ne $assetData) { $assetData.asset_status } elseif ($null -ne $gateAssets) { $gateAssets.asset_status } else { "unknown" }
$assetPlannedMissing = if ($null -ne $assetData -and $null -ne $assetData.PSObject.Properties["planned_missing"]) {
    $assetData.planned_missing
} elseif ($null -ne $gateAssets -and $null -ne $gateAssets.PSObject.Properties["planned_missing"]) {
    $gateAssets.planned_missing
} else {
    0
}
$layoutStatus = if ($null -ne $gateLayout) { $gateLayout.layout_status } else { "unknown" }
$hudContractStatus = if ($null -ne $hudContractData) { $hudContractData.hud_contract_status } elseif ($null -ne $gateHudContract) { $gateHudContract.hud_contract_status } else { "unknown" }
$hudContractFailedChecks = if ($null -ne $hudContractData) { $hudContractData.failed_checks } elseif ($null -ne $gateHudContract) { $gateHudContract.failed_checks } else { $null }
$playModeRecordStatus = if ($null -ne $playModeRecordData) { $playModeRecordData.playmode_record_status } else { "unknown" }
$playModeSuiteStatus = if ($null -ne $playModeSuiteData) { $playModeSuiteData.playmode_suite_status } elseif ($null -ne $gatePlayModeSuite) { $gatePlayModeSuite.playmode_suite_status } else { "unknown" }
$playModeSuiteCaptureSource = if ($null -ne $playModeSuiteData) { $playModeSuiteData.suite_capture_source } elseif ($null -ne $gatePlayModeSuite) { $gatePlayModeSuite.suite_capture_source } else { "unknown" }
$playModeSuiteCapturedCount = if ($null -ne $playModeSuiteData) { $playModeSuiteData.captured_count } elseif ($null -ne $gatePlayModeSuite) { $gatePlayModeSuite.captured_count } else { $null }
$playModeSuiteExpectedCount = if ($null -ne $playModeSuiteData) { $playModeSuiteData.expected_state_count } elseif ($null -ne $gatePlayModeSuite) { $gatePlayModeSuite.expected_state_count } else { $null }
$waveCombatActionShowcaseReady = if ($null -ne $playModeSuiteData) { $playModeSuiteData.wave_combat_action_showcase_ready } elseif ($null -ne $gatePlayModeSuite) { $gatePlayModeSuite.wave_combat_action_showcase_ready } else { $null }
$waveCombatActionShowcaseReason = if ($null -ne $playModeSuiteData) { $playModeSuiteData.wave_combat_action_showcase_reason } elseif ($null -ne $gatePlayModeSuite) { $gatePlayModeSuite.wave_combat_action_showcase_reason } else { "unknown" }
$playModeScreenshotStatus = if ($null -ne $playModeScreenshotsData) { $playModeScreenshotsData.playmode_screenshot_status } elseif ($null -ne $gatePlayModeScreenshots) { $gatePlayModeScreenshots.playmode_screenshot_status } else { "unknown" }
$playModeScreenshotCount = if ($null -ne $playModeScreenshotsData) { $playModeScreenshotsData.screenshot_count } elseif ($null -ne $gatePlayModeScreenshots) { $gatePlayModeScreenshots.screenshot_count } else { $null }
$playModeScreenshotInvalidCount = if ($null -ne $playModeScreenshotsData) { $playModeScreenshotsData.invalid_count } elseif ($null -ne $gatePlayModeScreenshots) { $gatePlayModeScreenshots.invalid_count } else { $null }
$playModeScreenshotMissingStates = if ($null -ne $playModeScreenshotsData) { $playModeScreenshotsData.missing_states } elseif ($null -ne $gatePlayModeScreenshots) { $gatePlayModeScreenshots.missing_states } else { @() }
$playModeManualRegistrationCandidateCount = if ($null -ne $playModeScreenshotsData -and $null -ne $playModeScreenshotsData.manual_registration_candidate_count) { $playModeScreenshotsData.manual_registration_candidate_count } else { 0 }
$playModeManualRegistrationCommands = if ($null -ne $playModeScreenshotsData -and $null -ne $playModeScreenshotsData.manual_registration_commands) { $playModeScreenshotsData.manual_registration_commands } else { @() }
$playModeTriagedNonStateCount = if ($null -ne $playModeScreenshotsData -and $null -ne $playModeScreenshotsData.triaged_non_state_count) { $playModeScreenshotsData.triaged_non_state_count } else { 0 }
$reviewPackStatus = if ($null -ne $playModeReviewPackData) { $playModeReviewPackData.review_pack_status } elseif (-not $playModeReviewPackResult.ok) { "failed" } else { "unknown" }
$reviewReadiness = if ($null -ne $playModeReviewPackData) { $playModeReviewPackData.review_readiness } else { "unknown" }
$reviewPackVisualReviewRequired = if ($null -ne $playModeReviewPackData) { $playModeReviewPackData.visual_review_required } else { $null }
$reviewPackNextAction = if ($null -ne $playModeReviewPackData) { $playModeReviewPackData.next_action } else { "Run Tools\Write-PrototypePlayModeReviewPack.ps1 -PreviewOnly -JsonOnly to inspect review readiness." }
$reviewPackError = if ($playModeReviewPackResult.ok) { $null } else { $playModeReviewPackResult.error }
$retakePlanStatus = if ($null -ne $playModeRetakePlanData) { $playModeRetakePlanData.retake_plan_status } elseif (-not $playModeRetakePlanResult.ok) { "failed" } else { "unknown" }
$retakePlanMissingStates = if ($null -ne $playModeRetakePlanData) { $playModeRetakePlanData.missing_states } else { @() }
$retakePlanFocusedRetakeCount = if ($null -ne $playModeRetakePlanData) { $playModeRetakePlanData.focused_retake_count } else { $null }
$retakePlanFocusedRetakeStates = if ($null -ne $playModeRetakePlanData) { $playModeRetakePlanData.focused_retake_states } else { @() }
$retakePlanNextAction = if ($null -ne $playModeRetakePlanData) { $playModeRetakePlanData.next_action } else { "Run Tools\Write-PrototypePlayModeRetakePlan.ps1 to generate a focused retake checklist before opening Unity." }
$retakePlanError = if ($playModeRetakePlanResult.ok) { $null } else { $playModeRetakePlanResult.error }
$retakePlanDocStatus = if ($null -ne $playModeRetakePlanVerifierData) { $playModeRetakePlanVerifierData.retake_plan_doc_status } elseif (-not $playModeRetakePlanVerifierResult.ok) { "failed" } else { "unknown" }
$retakePlanDocMissingNeedles = if ($null -ne $playModeRetakePlanVerifierData -and $null -ne $playModeRetakePlanVerifierData.missing_needles) { @($playModeRetakePlanVerifierData.missing_needles).Count } else { $null }
$retakePlanDocDocumentedCount = if ($null -ne $playModeRetakePlanVerifierData) { $playModeRetakePlanVerifierData.documented_focused_retake_count } else { $null }
$retakePlanDocExpectedCount = if ($null -ne $playModeRetakePlanVerifierData) { $playModeRetakePlanVerifierData.expected_focused_retake_count } else { $null }
$retakePlanDocNextAction = if ($null -ne $playModeRetakePlanVerifierData) { $playModeRetakePlanVerifierData.next_action } else { "Run Tools\Verify-PrototypePlayModeRetakePlan.ps1 to confirm the retake checklist is current." }
$retakePlanDocError = if ($playModeRetakePlanVerifierResult.ok) { $null } else { $playModeRetakePlanVerifierResult.error }
$staticStatus = if ($null -ne $verification) { $verification.static_status } else { "unknown" }
$compileStatus = if ($null -ne $verification) { $verification.compile_status } else { "unknown" }
$testsStatus = if ($null -ne $verification) { $verification.tests_status } else { "unknown" }
$manualRequired = if ($null -ne $verification) { [bool]$verification.manual_verification_required } else { $true }

$readiness = "needs_manual_playmode"
if ($gateResult.ok -and $assetStatus -eq "ok" -and $layoutStatus -eq "ok" -and $hudContractStatus -eq "ok" -and $staticStatus -eq "ok" -and $playModeRecordStatus -eq "passed" -and -not $manualRequired) {
    $readiness = "ready"
}
elseif (-not $gateResult.ok -or $assetStatus -ne "ok" -or $layoutStatus -ne "ok" -or $hudContractStatus -ne "ok" -or $staticStatus -ne "ok" -or $playModeRecordStatus -eq "invalid_record") {
    $readiness = "needs_fix_before_playmode"
}
elseif ($playModeSuiteStatus -eq "invalid_suite") {
    $readiness = "needs_fix_before_playmode"
}
elseif ($playModeScreenshotStatus -eq "invalid_screenshots") {
    $readiness = "needs_fix_before_playmode"
}
elseif ($reviewPackStatus -ne "ok" -and $reviewPackStatus -ne "unknown") {
    $readiness = "needs_fix_before_playmode"
}
elseif ($retakePlanStatus -ne "ok" -and $retakePlanStatus -ne "unknown") {
    $readiness = "needs_fix_before_playmode"
}
elseif ($retakePlanDocStatus -ne "ok" -and $retakePlanDocStatus -ne "unknown") {
    $readiness = "needs_fix_before_playmode"
}
elseif ($playModeRecordStatus -eq "needs_fix" -or $playModeRecordStatus -eq "blocked") {
    $readiness = "needs_playmode_fix"
}

$missingStateText = if ($null -ne $playModeScreenshotMissingStates -and $playModeScreenshotMissingStates.Count -gt 0) {
    $playModeScreenshotMissingStates -join ", "
}
else {
    "none"
}

$topIssue = "Manual Play Mode evidence is still open."
$nextEvidenceAction = "Use Tools > Food Truck Prototype > Capture Verification Suite, then rerun the suite, screenshot, and review pack verifiers."
$nextCodeTarget = "No new gameplay code target until fresh suite evidence is available; continue PC-limited evidence tooling only if Play Mode input remains unreliable."
$suiteEvidenceReady = $playModeSuiteStatus -eq "captured" -or $playModeSuiteStatus -eq "captured_manual"

if ($reviewPackStatus -ne "ok" -and $reviewPackStatus -ne "unknown") {
    $topIssue = "Review pack preview failed: " + $reviewPackStatus + "."
    $nextEvidenceAction = "Fix review pack preview before generating or recording PASS/FIX/BLOCKED evidence."
    $nextCodeTarget = "Fix Tools\Write-PrototypePlayModeReviewPack.ps1 or the verifier input that blocks preview mode."
}
elseif ($retakePlanDocStatus -ne "ok" -and $retakePlanDocStatus -ne "unknown") {
    $topIssue = "Retake plan document is not current: " + $retakePlanDocStatus + "."
    $nextEvidenceAction = $retakePlanDocNextAction
    $nextCodeTarget = "Regenerate or fix Docs\Prototype_PlayMode_RetakePlan.md before opening Unity."
}
elseif (-not $suiteEvidenceReady) {
    if ($playModeSuiteStatus -eq "manual_partial") {
        $topIssue = "Manual Play Mode evidence is partial: " + $playModeSuiteCapturedCount + "/" + $playModeSuiteExpectedCount + " states registered."
        if ($playModeManualRegistrationCandidateCount -gt 0) {
            $nextEvidenceAction = "Review " + $playModeManualRegistrationCandidateCount + " unlabeled PNG candidate(s), then register any matching missing state with Tools\Register-PrototypePlayModeManualEvidence.ps1."
        }
        else {
            $nextEvidenceAction = $retakePlanNextAction
        }
    }
    else {
        $topIssue = "Play Mode verification suite is not captured: " + $playModeSuiteStatus + "."
        $nextEvidenceAction = "Run Tools > Food Truck Prototype > Capture Verification Suite in Unity Play Mode, or register standalone PNGs with Tools\Register-PrototypePlayModeManualEvidence.ps1."
    }
}
elseif ($playModeScreenshotStatus -ne "suite_ready") {
    $topIssue = "Play Mode screenshot evidence is not suite-ready: " + $playModeScreenshotStatus + " (missing: " + $missingStateText + ")."
    $nextEvidenceAction = "Capture or retake missing states with Capture Verification Suite or focused Prepare and Capture State."
}
elseif ($waveCombatActionShowcaseReady -ne $true) {
    $topIssue = "Wave Combat action showcase is not ready: " + $waveCombatActionShowcaseReason + "."
    $nextEvidenceAction = "Retake Wave Combat evidence and confirm action labels before recording PASS."
    $nextCodeTarget = "If retake still misses labels, fix the Wave Combat verification showcase setup."
}
elseif ($reviewReadiness -ne "ready_for_visual_review") {
    $topIssue = "Review pack is not ready for visual judgment: " + $reviewReadiness + "."
    $nextEvidenceAction = $reviewPackNextAction
}
elseif ($playModeRecordStatus -ne "passed") {
    $topIssue = "Manual Play Mode result is not recorded as passed: " + $playModeRecordStatus + "."
    $nextEvidenceAction = "Generate the review pack, make PASS/FIX/BLOCKED judgments, then apply the result writer."
    $nextCodeTarget = "Use recorded FIX_LAYOUT/FIX_ASSET/FIX_FEEDBACK/BLOCKED result to choose the next code target."
}
else {
    $topIssue = "No current evidence blocker."
    $nextEvidenceAction = "Keep the current evidence pack with the session notes."
    $nextCodeTarget = "Proceed to the next planned core loop improvement."
}

$unresolvedIssues = New-Object System.Collections.Generic.List[string]
$unresolvedIssues.Add("Unity headless compile/tests remain inconclusive in this environment.") | Out-Null
if ($hudContractStatus -ne "ok") {
    $unresolvedIssues.Add("HUD state contract status: " + $hudContractStatus + ".") | Out-Null
}
$unresolvedIssues.Add("Play Mode verification suite status: " + $playModeSuiteStatus + ".") | Out-Null
$unresolvedIssues.Add("Wave Combat action showcase ready: " + [string]$waveCombatActionShowcaseReady + " (" + $waveCombatActionShowcaseReason + ").") | Out-Null
$unresolvedIssues.Add("Play Mode screenshot evidence status: " + $playModeScreenshotStatus + ".") | Out-Null
$unresolvedIssues.Add("Play Mode review pack readiness: " + $reviewReadiness + ".") | Out-Null
$unresolvedIssues.Add("Play Mode retake plan status: " + $retakePlanStatus + ".") | Out-Null
$unresolvedIssues.Add("Play Mode retake plan document status: " + $retakePlanDocStatus + ".") | Out-Null
$unresolvedIssues.Add("Manual Unity Play Mode verification record status: " + $playModeRecordStatus + ".") | Out-Null
if ($assetPlannedMissing -gt 0) {
    $unresolvedIssues.Add("Planned feedback resources still missing: " + $assetPlannedMissing + " non-blocking VFX/SFX items.") | Out-Null
}
$unresolvedIssues.Add("Top issue: " + $topIssue) | Out-Null
$unresolvedIssues.Add("Draw Choice, Pending Placement, and Invalid Placement still need visual confirmation when Play Mode input is reliable again.") | Out-Null

$summary = [ordered]@{
    project_path = $ProjectPath
    generated_at = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    readiness = $readiness
    top_issue = $topIssue
    next_evidence_action = $nextEvidenceAction
    next_code_target = $nextCodeTarget
    gate_status = if ($null -ne $gateData) { $gateData.gate_status } else { "unknown" }
    asset_status = $assetStatus
    asset_planned_missing = $assetPlannedMissing
    layout_status = $layoutStatus
    hud_contract_status = $hudContractStatus
    hud_contract_failed_checks = $hudContractFailedChecks
    playmode_suite_status = $playModeSuiteStatus
    playmode_suite_capture_source = $playModeSuiteCaptureSource
    playmode_suite_captured_count = $playModeSuiteCapturedCount
    playmode_suite_expected_count = $playModeSuiteExpectedCount
    wave_combat_action_showcase_ready = $waveCombatActionShowcaseReady
    wave_combat_action_showcase_reason = $waveCombatActionShowcaseReason
    playmode_screenshot_status = $playModeScreenshotStatus
    playmode_screenshot_count = $playModeScreenshotCount
    playmode_screenshot_invalid_count = $playModeScreenshotInvalidCount
    playmode_screenshot_missing_states = $playModeScreenshotMissingStates
    playmode_manual_registration_candidate_count = $playModeManualRegistrationCandidateCount
    playmode_manual_registration_commands = $playModeManualRegistrationCommands
    playmode_triaged_non_state_count = $playModeTriagedNonStateCount
    review_pack_status = $reviewPackStatus
    review_readiness = $reviewReadiness
    review_pack_visual_review_required = $reviewPackVisualReviewRequired
    review_pack_error = $reviewPackError
    review_pack_next_action = $reviewPackNextAction
    retake_plan_status = $retakePlanStatus
    retake_plan_missing_states = $retakePlanMissingStates
    retake_plan_focused_retake_count = $retakePlanFocusedRetakeCount
    retake_plan_focused_retake_states = $retakePlanFocusedRetakeStates
    retake_plan_error = $retakePlanError
    retake_plan_next_action = $retakePlanNextAction
    retake_plan_doc_status = $retakePlanDocStatus
    retake_plan_doc_missing_needles = $retakePlanDocMissingNeedles
    retake_plan_doc_documented_focused_retake_count = $retakePlanDocDocumentedCount
    retake_plan_doc_expected_focused_retake_count = $retakePlanDocExpectedCount
    retake_plan_doc_error = $retakePlanDocError
    retake_plan_doc_next_action = $retakePlanDocNextAction
    playmode_record_status = $playModeRecordStatus
    static_status = $staticStatus
    compile_status = $compileStatus
    tests_status = $testsStatus
    manual_verification_required = $manualRequired
    handoff_doc = $handoffPath
    playmode_verification_doc = $playModePath
    playbook_doc = $playbookPath
    docs_exist = [ordered]@{
        handoff = (Test-Path -LiteralPath $handoffPath)
        playmode_verification = (Test-Path -LiteralPath $playModePath)
        playbook = (Test-Path -LiteralPath $playbookPath)
        layout_verifier = (Test-Path -LiteralPath $layoutScript)
        hud_state_contract_verifier = (Test-Path -LiteralPath $hudContractScript)
        playmode_suite_verifier = (Test-Path -LiteralPath $playModeSuiteScript)
        playmode_screenshot_verifier = (Test-Path -LiteralPath $playModeScreenshotsScript)
        playmode_review_pack_writer = (Test-Path -LiteralPath $playModeReviewPackScript)
        playmode_retake_plan_writer = (Test-Path -LiteralPath $playModeRetakePlanScript)
        playmode_retake_plan_verifier = (Test-Path -LiteralPath $playModeRetakePlanVerifierScript)
        playmode_evidence_preflight = (Test-Path -LiteralPath $playModeEvidencePreflightScript)
        playmode_manual_evidence_register = (Test-Path -LiteralPath $playModeManualEvidenceScript)
        playmode_record_verifier = (Test-Path -LiteralPath $playModeRecordScript)
    }
    unresolved_issues = $unresolvedIssues.ToArray()
    recommended_next_actions = @(
        ($nextEvidenceAction),
        ("Next code target: " + $nextCodeTarget),
        ("Track planned VFX/SFX backlog via asset_planned_missing=" + $assetPlannedMissing + "; it is non-blocking until assets are intentionally produced/imported."),
        "Continue code-level next work if this PC cannot reliably interact with Play Mode.",
        "Run Tools\Write-PrototypePlayModeRetakePlan.ps1 to generate a focused retake checklist before opening Unity.",
        "Run Tools\Verify-PrototypePlayModeRetakePlan.ps1 to confirm the focused retake checklist is not stale.",
        "Run Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1 before opening Unity to summarize focused retake readiness.",
        "Open the review pack or screenshot verifier output to copy manual registration command templates for unlabeled PNGs.",
        "If only standalone PNGs are available, register them with Tools\Register-PrototypePlayModeManualEvidence.ps1 before generating the review pack.",
        "In Play Mode, use Tools > Food Truck Prototype > Capture Verification Suite for one-pass evidence across all required states.",
        "After suite capture, run Tools\Verify-PrototypePlayModeSuite.ps1 to confirm all screenshots exist.",
        "Confirm wave_combat_action_showcase_ready=true before recording Wave Combat as PASS.",
        "Run Tools\Verify-PrototypePlayModeScreenshots.ps1 to check screenshot PNG quality and state coverage.",
        "Check review_readiness before generating or recording manual PASS/FIX/BLOCKED evidence.",
        "Run Tools\Write-PrototypePlayModeReviewPack.ps1 to generate a single visual review sheet.",
        "Use Tools\Write-PrototypePlayModeResultFromSuite.ps1 to draft or apply PASS/FIX/BLOCKED results without hand-editing markdown.",
        "In Play Mode, use Tools > Food Truck Prototype > Prepare and Capture State for low-interaction Draw/Pending/Invalid evidence.",
        "When Play Mode input is reliable again, capture Draw Choice, Pending Placement, and Invalid Placement evidence.",
        "In Play Mode, use Tools > Food Truck Prototype > Capture Play Mode Snapshot to collect screenshot evidence.",
        "If all checklist states pass visually, use Tools > Food Truck Prototype > Record PASS Manual Result.",
        "If any checklist state fails, record the FIX_* result manually in Docs\Prototype_PlayMode_Verification.md.",
        "Close Unity Editor before using forced headless verification.",
        "Run Tools\Verify-PrototypeHudStateContract.ps1 after Draw/Pending/Invalid Placement HUD code changes.",
        "Run Tools\Verify-PrototypeLayout.ps1 if layout code changes before Play Mode.",
        "Run Tools\Verify-PrototypePlayModeRecord.ps1 after recording Play Mode results.",
        "If layout fails, adjust FoodTruckPrototypeHud.CalculateGameplayFocusLayout / ApplyGameplayHudContext before adding features.",
        "After manual visual confirmation, rerun Tools\Gate-Verification.ps1 -RunTests -JsonOnly."
    )
}

if ($JsonOnly) {
    $summary | ConvertTo-Json -Depth 6 -Compress | Write-Host
    exit 0
}

Write-Host ("prototype_session_readiness=" + $summary.readiness)
Write-Host ("top_issue=" + $summary.top_issue)
Write-Host ("next_evidence_action=" + $summary.next_evidence_action)
Write-Host ("next_code_target=" + $summary.next_code_target)
Write-Host ("gate_status=" + $summary.gate_status)
Write-Host ("asset_status=" + $summary.asset_status)
Write-Host ("asset_planned_missing=" + $summary.asset_planned_missing)
Write-Host ("layout_status=" + $summary.layout_status)
Write-Host ("hud_contract_status=" + $summary.hud_contract_status)
Write-Host ("hud_contract_failed_checks=" + $summary.hud_contract_failed_checks)
Write-Host ("playmode_suite_status=" + $summary.playmode_suite_status)
Write-Host ("playmode_suite_capture_source=" + $summary.playmode_suite_capture_source)
Write-Host ("playmode_suite_captured_count=" + $summary.playmode_suite_captured_count + "/" + $summary.playmode_suite_expected_count)
Write-Host ("wave_combat_action_showcase_ready=" + [string]$summary.wave_combat_action_showcase_ready)
Write-Host ("wave_combat_action_showcase_reason=" + $summary.wave_combat_action_showcase_reason)
Write-Host ("playmode_screenshot_status=" + $summary.playmode_screenshot_status)
Write-Host ("playmode_screenshot_count=" + $summary.playmode_screenshot_count)
Write-Host ("playmode_manual_registration_candidate_count=" + $summary.playmode_manual_registration_candidate_count)
Write-Host ("playmode_triaged_non_state_count=" + $summary.playmode_triaged_non_state_count)
Write-Host ("review_pack_status=" + $summary.review_pack_status)
Write-Host ("review_readiness=" + $summary.review_readiness)
Write-Host ("review_pack_visual_review_required=" + [string]$summary.review_pack_visual_review_required)
Write-Host ("retake_plan_status=" + $summary.retake_plan_status)
Write-Host ("retake_plan_focused_retake_count=" + $summary.retake_plan_focused_retake_count)
Write-Host ("retake_plan_next_action=" + $summary.retake_plan_next_action)
Write-Host ("retake_plan_doc_status=" + $summary.retake_plan_doc_status)
Write-Host ("retake_plan_doc_focused_retake_count=" + $summary.retake_plan_doc_documented_focused_retake_count + "/" + $summary.retake_plan_doc_expected_focused_retake_count)
Write-Host ("playmode_record_status=" + $summary.playmode_record_status)
Write-Host ("static_status=" + $summary.static_status)
Write-Host ("compile_status=" + $summary.compile_status)
Write-Host ("tests_status=" + $summary.tests_status)
Write-Host ("manual_verification_required=" + $summary.manual_verification_required)
Write-Host ("handoff_doc=" + $summary.handoff_doc)
Write-Host ("playmode_verification_doc=" + $summary.playmode_verification_doc)
Write-Host "recommended_next_actions:"
foreach ($action in $summary.recommended_next_actions) {
    Write-Host ("- " + $action)
}

exit 0
