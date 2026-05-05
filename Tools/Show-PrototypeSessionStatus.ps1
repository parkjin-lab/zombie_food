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

$assetStatus = if ($null -ne $assetData) { $assetData.asset_status } elseif ($null -ne $gateAssets) { $gateAssets.asset_status } else { "unknown" }
$layoutStatus = if ($null -ne $gateLayout) { $gateLayout.layout_status } else { "unknown" }
$hudContractStatus = if ($null -ne $hudContractData) { $hudContractData.hud_contract_status } elseif ($null -ne $gateHudContract) { $gateHudContract.hud_contract_status } else { "unknown" }
$hudContractFailedChecks = if ($null -ne $hudContractData) { $hudContractData.failed_checks } elseif ($null -ne $gateHudContract) { $gateHudContract.failed_checks } else { $null }
$playModeRecordStatus = if ($null -ne $playModeRecordData) { $playModeRecordData.playmode_record_status } else { "unknown" }
$playModeSuiteStatus = if ($null -ne $playModeSuiteData) { $playModeSuiteData.playmode_suite_status } elseif ($null -ne $gatePlayModeSuite) { $gatePlayModeSuite.playmode_suite_status } else { "unknown" }
$playModeSuiteCapturedCount = if ($null -ne $playModeSuiteData) { $playModeSuiteData.captured_count } elseif ($null -ne $gatePlayModeSuite) { $gatePlayModeSuite.captured_count } else { $null }
$playModeSuiteExpectedCount = if ($null -ne $playModeSuiteData) { $playModeSuiteData.expected_state_count } elseif ($null -ne $gatePlayModeSuite) { $gatePlayModeSuite.expected_state_count } else { $null }
$waveCombatActionShowcaseReady = if ($null -ne $playModeSuiteData) { $playModeSuiteData.wave_combat_action_showcase_ready } elseif ($null -ne $gatePlayModeSuite) { $gatePlayModeSuite.wave_combat_action_showcase_ready } else { $null }
$waveCombatActionShowcaseReason = if ($null -ne $playModeSuiteData) { $playModeSuiteData.wave_combat_action_showcase_reason } elseif ($null -ne $gatePlayModeSuite) { $gatePlayModeSuite.wave_combat_action_showcase_reason } else { "unknown" }
$playModeScreenshotStatus = if ($null -ne $playModeScreenshotsData) { $playModeScreenshotsData.playmode_screenshot_status } elseif ($null -ne $gatePlayModeScreenshots) { $gatePlayModeScreenshots.playmode_screenshot_status } else { "unknown" }
$playModeScreenshotCount = if ($null -ne $playModeScreenshotsData) { $playModeScreenshotsData.screenshot_count } elseif ($null -ne $gatePlayModeScreenshots) { $gatePlayModeScreenshots.screenshot_count } else { $null }
$playModeScreenshotInvalidCount = if ($null -ne $playModeScreenshotsData) { $playModeScreenshotsData.invalid_count } elseif ($null -ne $gatePlayModeScreenshots) { $gatePlayModeScreenshots.invalid_count } else { $null }
$playModeScreenshotMissingStates = if ($null -ne $playModeScreenshotsData) { $playModeScreenshotsData.missing_states } elseif ($null -ne $gatePlayModeScreenshots) { $gatePlayModeScreenshots.missing_states } else { @() }
$reviewPackStatus = if ($null -ne $playModeReviewPackData) { $playModeReviewPackData.review_pack_status } elseif (-not $playModeReviewPackResult.ok) { "failed" } else { "unknown" }
$reviewReadiness = if ($null -ne $playModeReviewPackData) { $playModeReviewPackData.review_readiness } else { "unknown" }
$reviewPackVisualReviewRequired = if ($null -ne $playModeReviewPackData) { $playModeReviewPackData.visual_review_required } else { $null }
$reviewPackNextAction = if ($null -ne $playModeReviewPackData) { $playModeReviewPackData.next_action } else { "Run Tools\Write-PrototypePlayModeReviewPack.ps1 -PreviewOnly -JsonOnly to inspect review readiness." }
$reviewPackError = if ($playModeReviewPackResult.ok) { $null } else { $playModeReviewPackResult.error }
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
elseif ($playModeRecordStatus -eq "needs_fix" -or $playModeRecordStatus -eq "blocked") {
    $readiness = "needs_playmode_fix"
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
$unresolvedIssues.Add("Manual Unity Play Mode verification record status: " + $playModeRecordStatus + ".") | Out-Null
$unresolvedIssues.Add("Draw Choice, Pending Placement, and Invalid Placement still need visual confirmation when Play Mode input is reliable again.") | Out-Null

$summary = [ordered]@{
    project_path = $ProjectPath
    generated_at = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    readiness = $readiness
    gate_status = if ($null -ne $gateData) { $gateData.gate_status } else { "unknown" }
    asset_status = $assetStatus
    layout_status = $layoutStatus
    hud_contract_status = $hudContractStatus
    hud_contract_failed_checks = $hudContractFailedChecks
    playmode_suite_status = $playModeSuiteStatus
    playmode_suite_captured_count = $playModeSuiteCapturedCount
    playmode_suite_expected_count = $playModeSuiteExpectedCount
    wave_combat_action_showcase_ready = $waveCombatActionShowcaseReady
    wave_combat_action_showcase_reason = $waveCombatActionShowcaseReason
    playmode_screenshot_status = $playModeScreenshotStatus
    playmode_screenshot_count = $playModeScreenshotCount
    playmode_screenshot_invalid_count = $playModeScreenshotInvalidCount
    playmode_screenshot_missing_states = $playModeScreenshotMissingStates
    review_pack_status = $reviewPackStatus
    review_readiness = $reviewReadiness
    review_pack_visual_review_required = $reviewPackVisualReviewRequired
    review_pack_error = $reviewPackError
    review_pack_next_action = $reviewPackNextAction
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
        playmode_record_verifier = (Test-Path -LiteralPath $playModeRecordScript)
    }
    unresolved_issues = $unresolvedIssues.ToArray()
    recommended_next_actions = @(
        "Continue code-level next work if this PC cannot reliably interact with Play Mode.",
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
Write-Host ("gate_status=" + $summary.gate_status)
Write-Host ("asset_status=" + $summary.asset_status)
Write-Host ("layout_status=" + $summary.layout_status)
Write-Host ("hud_contract_status=" + $summary.hud_contract_status)
Write-Host ("hud_contract_failed_checks=" + $summary.hud_contract_failed_checks)
Write-Host ("playmode_suite_status=" + $summary.playmode_suite_status)
Write-Host ("playmode_suite_captured_count=" + $summary.playmode_suite_captured_count + "/" + $summary.playmode_suite_expected_count)
Write-Host ("wave_combat_action_showcase_ready=" + [string]$summary.wave_combat_action_showcase_ready)
Write-Host ("wave_combat_action_showcase_reason=" + $summary.wave_combat_action_showcase_reason)
Write-Host ("playmode_screenshot_status=" + $summary.playmode_screenshot_status)
Write-Host ("playmode_screenshot_count=" + $summary.playmode_screenshot_count)
Write-Host ("review_pack_status=" + $summary.review_pack_status)
Write-Host ("review_readiness=" + $summary.review_readiness)
Write-Host ("review_pack_visual_review_required=" + [string]$summary.review_pack_visual_review_required)
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
