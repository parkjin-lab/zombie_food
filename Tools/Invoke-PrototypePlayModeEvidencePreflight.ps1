param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"

$scriptName = "Invoke-PrototypePlayModeEvidencePreflight.ps1"
$focusedCoreStates = @("Draw Choice", "Pending Placement", "Invalid Placement")

function Invoke-JsonScript {
    param(
        [string]$Name,
        [string]$ScriptPath,
        [string[]]$Arguments
    )

    if (-not (Test-Path -LiteralPath $ScriptPath -PathType Leaf)) {
        return [pscustomobject]@{
            name = $Name
            ok = $false
            parsed = $false
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
            name = $Name
            ok = $false
            parsed = $false
            exit_code = $exitCode
            data = $null
            error = "empty_output"
        }
    }

    try {
        return [pscustomobject]@{
            name = $Name
            ok = ($exitCode -eq 0)
            parsed = $true
            exit_code = $exitCode
            data = ($jsonLine | ConvertFrom-Json)
            error = $null
        }
    }
    catch {
        return [pscustomobject]@{
            name = $Name
            ok = $false
            parsed = $false
            exit_code = $exitCode
            data = $null
            error = "json_parse_failed: " + $_.Exception.Message
        }
    }
}

function Get-ObjectProperty {
    param(
        [object]$ObjectValue,
        [string]$PropertyName,
        [object]$DefaultValue = $null
    )

    if ($null -eq $ObjectValue) {
        return $DefaultValue
    }

    $property = $ObjectValue.PSObject.Properties[$PropertyName]
    if ($null -eq $property) {
        return $DefaultValue
    }

    return $property.Value
}

function Get-ArrayValue {
    param([object]$Value)

    if ($null -eq $Value) {
        return @()
    }

    return @($Value)
}

function Test-ContainsAll {
    param(
        [object]$Value,
        [string[]]$ExpectedValues
    )

    $items = Get-ArrayValue $Value
    foreach ($expected in $ExpectedValues) {
        if ($items -notcontains $expected) {
            return $false
        }
    }

    return $true
}

function Test-ValueIn {
    param(
        [string]$Value,
        [string[]]$AllowedValues
    )

    return $AllowedValues -contains $Value
}

function Join-StateList {
    param([object]$States)

    $items = Get-ArrayValue $States
    if ($items.Count -eq 0) {
        return "none"
    }

    return $items -join ", "
}

function New-CommandLine {
    param(
        [string]$ScriptRelativePath,
        [string[]]$ExtraArgs = @()
    )

    $parts = New-Object System.Collections.Generic.List[string]
    $parts.Add('powershell -ExecutionPolicy Bypass -File "' + $ScriptRelativePath + '"') | Out-Null
    $parts.Add('-ProjectPath "' + $ProjectPath + '"') | Out-Null
    foreach ($arg in $ExtraArgs) {
        $parts.Add($arg) | Out-Null
    }

    return ($parts.ToArray() -join " ")
}

$sessionStatusScript = Join-Path $ProjectPath "Tools\Show-PrototypeSessionStatus.ps1"
$retakePlanVerifierScript = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeRetakePlan.ps1"
$suiteVerifierScript = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeSuite.ps1"
$screenshotVerifierScript = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeScreenshots.ps1"
$reviewPackWriterScript = Join-Path $ProjectPath "Tools\Write-PrototypePlayModeReviewPack.ps1"

$sessionResult = Invoke-JsonScript -Name "session_status" -ScriptPath $sessionStatusScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-JsonOnly"
)

$retakePlanResult = Invoke-JsonScript -Name "retake_plan_doc" -ScriptPath $retakePlanVerifierScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-JsonOnly"
)

$suiteResult = Invoke-JsonScript -Name "playmode_suite" -ScriptPath $suiteVerifierScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-JsonOnly"
)

$screenshotResult = Invoke-JsonScript -Name "playmode_screenshots" -ScriptPath $screenshotVerifierScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-JsonOnly"
)

$reviewPackResult = Invoke-JsonScript -Name "review_pack_preview" -ScriptPath $reviewPackWriterScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-PreviewOnly",
    "-JsonOnly"
)

$scriptResults = @($sessionResult, $retakePlanResult, $suiteResult, $screenshotResult, $reviewPackResult)
$failedParses = @($scriptResults | Where-Object { -not $_.parsed })

$sessionData = $sessionResult.data
$retakePlanData = $retakePlanResult.data
$suiteData = $suiteResult.data
$screenshotData = $screenshotResult.data
$reviewPackData = $reviewPackResult.data

$gateStatus = Get-ObjectProperty $sessionData "gate_status" "unknown"
$assetStatus = Get-ObjectProperty $sessionData "asset_status" "unknown"
$layoutStatus = Get-ObjectProperty $sessionData "layout_status" "unknown"
$hudContractStatus = Get-ObjectProperty $sessionData "hud_contract_status" "unknown"
$staticStatus = Get-ObjectProperty $sessionData "static_status" "unknown"
$compileStatus = Get-ObjectProperty $sessionData "compile_status" "unknown"
$testsStatus = Get-ObjectProperty $sessionData "tests_status" "unknown"
$suiteStatus = Get-ObjectProperty $suiteData "playmode_suite_status" (Get-ObjectProperty $sessionData "playmode_suite_status" "unknown")
$suiteCapturedCount = Get-ObjectProperty $suiteData "captured_count" (Get-ObjectProperty $sessionData "playmode_suite_captured_count" $null)
$suiteExpectedCount = Get-ObjectProperty $suiteData "expected_state_count" (Get-ObjectProperty $sessionData "playmode_suite_expected_count" $null)
$suiteCaptureSource = Get-ObjectProperty $suiteData "suite_capture_source" (Get-ObjectProperty $sessionData "playmode_suite_capture_source" "unknown")
$screenshotStatus = Get-ObjectProperty $screenshotData "playmode_screenshot_status" (Get-ObjectProperty $sessionData "playmode_screenshot_status" "unknown")
$screenshotCount = Get-ObjectProperty $screenshotData "screenshot_count" (Get-ObjectProperty $sessionData "playmode_screenshot_count" 0)
$invalidScreenshotCount = Get-ObjectProperty $screenshotData "invalid_count" (Get-ObjectProperty $sessionData "playmode_screenshot_invalid_count" 0)
$missingStates = Get-ArrayValue (Get-ObjectProperty $screenshotData "missing_states" (Get-ObjectProperty $retakePlanData "expected_missing_states" @()))
$focusedRetakeStates = Get-ArrayValue (Get-ObjectProperty $retakePlanData "expected_focused_retake_states" $null)
if ($focusedRetakeStates.Count -eq 0) {
    $focusedRetakeStates = @($focusedCoreStates | Where-Object { $missingStates -contains $_ })
}

$manualCandidateCount = Get-ObjectProperty $screenshotData "manual_registration_candidate_count" (Get-ObjectProperty $retakePlanData "manual_registration_candidate_count" 0)
$triagedNonStateCount = Get-ObjectProperty $screenshotData "triaged_non_state_count" (Get-ObjectProperty $retakePlanData "triaged_non_state_count" 0)
$retakePlanDocStatus = Get-ObjectProperty $retakePlanData "retake_plan_doc_status" "unknown"
$retakePlanDocMissingNeedles = Get-ArrayValue (Get-ObjectProperty $retakePlanData "missing_needles" @())
$reviewPackStatus = Get-ObjectProperty $reviewPackData "review_pack_status" (Get-ObjectProperty $sessionData "review_pack_status" "unknown")
$reviewReadiness = Get-ObjectProperty $reviewPackData "review_readiness" (Get-ObjectProperty $sessionData "review_readiness" "unknown")
$reviewNextAction = Get-ObjectProperty $reviewPackData "next_action" (Get-ObjectProperty $sessionData "review_pack_next_action" "")
$waveCombatActionShowcaseReady = Get-ObjectProperty $suiteData "wave_combat_action_showcase_ready" (Get-ObjectProperty $sessionData "wave_combat_action_showcase_ready" $null)
$waveCombatActionShowcaseReason = Get-ObjectProperty $suiteData "wave_combat_action_showcase_reason" (Get-ObjectProperty $sessionData "wave_combat_action_showcase_reason" "unknown")

$coreChecksOk = (
    $gateStatus -eq "ok" -and
    $assetStatus -eq "ok" -and
    $layoutStatus -eq "ok" -and
    $hudContractStatus -eq "ok" -and
    $staticStatus -eq "ok"
)

$suiteReady = Test-ValueIn $suiteStatus @("captured", "captured_manual")
$canRetakeFromCurrentEvidence = (
    $focusedRetakeStates.Count -gt 0 -and
    (Test-ValueIn $suiteStatus @("manual_partial", "not_recorded")) -and
    (Test-ValueIn $screenshotStatus @("partial", "not_recorded")) -and
    $manualCandidateCount -eq 0
)
$allFocusedStatesMissing = Test-ContainsAll $focusedRetakeStates $focusedCoreStates

$preflightStatus = "ready_for_focused_retake"
$nextAction = "Open Unity Play Mode, run Prepare and Capture State for " + (Join-StateList $focusedRetakeStates) + ", then rerun this preflight."

if ($failedParses.Count -gt 0 -or -not $coreChecksOk) {
    $preflightStatus = "needs_preflight_fix"
    $nextAction = "Fix the failing local verifier or source-level gate before opening Unity."
}
elseif ($screenshotStatus -eq "invalid_screenshots" -or $invalidScreenshotCount -gt 0) {
    $preflightStatus = "needs_screenshot_fix"
    $nextAction = "Fix or remove invalid screenshot evidence, then rerun the screenshot verifier and this preflight."
}
elseif ($retakePlanDocStatus -ne "ok") {
    $preflightStatus = "needs_retake_plan_refresh"
    $nextAction = "Regenerate Docs\Prototype_PlayMode_RetakePlan.md, verify it, then rerun this preflight."
}
elseif ($reviewReadiness -eq "ready_for_visual_review" -or ($suiteReady -and $screenshotStatus -eq "suite_ready")) {
    $preflightStatus = "ready_for_visual_review"
    $nextAction = "Open the review pack, judge the screenshots visually, then record PASS/FIX/BLOCKED."
}
elseif ($focusedRetakeStates.Count -gt 0 -and $manualCandidateCount -gt 0) {
    $preflightStatus = "needs_manual_registration_review"
    $nextAction = "Inspect manual registration candidates first; retake any missing state without a visually matching PNG."
}
elseif ($canRetakeFromCurrentEvidence) {
    $preflightStatus = "ready_for_focused_retake"
    $nextAction = "Open Unity Play Mode, run Prepare and Capture State for " + (Join-StateList $focusedRetakeStates) + ", then rerun this preflight."
}
else {
    $preflightStatus = "needs_suite_capture"
    $nextAction = $reviewNextAction
    if ([string]::IsNullOrWhiteSpace($nextAction)) {
        $nextAction = "Run Capture Verification Suite or focused retakes, then rerun this preflight."
    }
}

$commandsBeforeUnity = @(
    (New-CommandLine "Tools\Show-PrototypeSessionStatus.ps1"),
    (New-CommandLine "Tools\Verify-PrototypePlayModeRetakePlan.ps1"),
    (New-CommandLine "Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1")
)

$commandsAfterCapture = @(
    (New-CommandLine "Tools\Verify-PrototypePlayModeSuite.ps1"),
    (New-CommandLine "Tools\Verify-PrototypePlayModeScreenshots.ps1"),
    (New-CommandLine "Tools\Write-PrototypePlayModeReviewPack.ps1" @("-PreviewOnly", "-JsonOnly")),
    (New-CommandLine "Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1")
)

$result = [ordered]@{
    playmode_evidence_preflight_status = $preflightStatus
    verifier = $scriptName
    generated_at = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    project_path = $ProjectPath
    gate_status = $gateStatus
    asset_status = $assetStatus
    layout_status = $layoutStatus
    hud_contract_status = $hudContractStatus
    static_status = $staticStatus
    compile_status = $compileStatus
    tests_status = $testsStatus
    playmode_suite_status = $suiteStatus
    playmode_suite_capture_source = $suiteCaptureSource
    playmode_suite_captured_count = $suiteCapturedCount
    playmode_suite_expected_count = $suiteExpectedCount
    playmode_screenshot_status = $screenshotStatus
    playmode_screenshot_count = $screenshotCount
    playmode_screenshot_invalid_count = $invalidScreenshotCount
    missing_states = $missingStates
    focused_retake_states = $focusedRetakeStates
    focused_retake_count = $focusedRetakeStates.Count
    focused_retake_core_states_all_missing = $allFocusedStatesMissing
    manual_registration_candidate_count = $manualCandidateCount
    triaged_non_state_count = $triagedNonStateCount
    retake_plan_doc_status = $retakePlanDocStatus
    retake_plan_doc_missing_needles = $retakePlanDocMissingNeedles.Count
    review_pack_status = $reviewPackStatus
    review_readiness = $reviewReadiness
    wave_combat_action_showcase_ready = $waveCombatActionShowcaseReady
    wave_combat_action_showcase_reason = $waveCombatActionShowcaseReason
    required_script_results = @($scriptResults | ForEach-Object {
        [pscustomobject]@{
            name = $_.name
            ok = $_.ok
            parsed = $_.parsed
            exit_code = $_.exit_code
            error = $_.error
        }
    })
    commands_before_unity = $commandsBeforeUnity
    commands_after_capture = $commandsAfterCapture
    next_action = $nextAction
}

if ($JsonOnly) {
    $result | ConvertTo-Json -Depth 8 -Compress | Write-Host
}
else {
    Write-Host ("playmode_evidence_preflight_status=" + $preflightStatus)
    Write-Host ("focused_retake_count=" + $focusedRetakeStates.Count)
    Write-Host ("focused_retake_states=" + (Join-StateList $focusedRetakeStates))
    Write-Host ("retake_plan_doc_status=" + $retakePlanDocStatus)
    Write-Host ("review_readiness=" + $reviewReadiness)
    Write-Host ("playmode_suite_status=" + $suiteStatus)
    Write-Host ("playmode_screenshot_status=" + $screenshotStatus)
    Write-Host ("manual_registration_candidate_count=" + $manualCandidateCount)
    Write-Host ("triaged_non_state_count=" + $triagedNonStateCount)
    Write-Host ("wave_combat_action_showcase_ready=" + [string]$waveCombatActionShowcaseReady)
    Write-Host ("wave_combat_action_showcase_reason=" + $waveCombatActionShowcaseReason)
    Write-Host ("next_action=" + $nextAction)
    Write-Host "commands_after_capture:"
    foreach ($command in $commandsAfterCapture) {
        Write-Host ("- " + $command)
    }
}

if (Test-ValueIn $preflightStatus @("needs_preflight_fix", "needs_retake_plan_refresh", "needs_screenshot_fix")) {
    exit 14
}

exit 0
