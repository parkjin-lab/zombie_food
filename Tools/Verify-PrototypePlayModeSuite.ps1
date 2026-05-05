param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"

$suitePath = Join-Path $ProjectPath "Docs\Prototype_PlayMode_Verification_Suite.txt"
$requiredStates = @("Draw Choice", "Pending Placement", "Invalid Placement", "Wave Combat")

function Normalize-FieldValue {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "NOT_RECORDED"
    }

    return $Value.Trim()
}

function Resolve-EvidencePath {
    param(
        [string]$EvidencePath,
        [string]$RootPath
    )

    if ([string]::IsNullOrWhiteSpace($EvidencePath) -or $EvidencePath -eq "not captured" -or $EvidencePath -eq "NOT_RECORDED") {
        return "NOT_RECORDED"
    }

    if ([System.IO.Path]::IsPathRooted($EvidencePath)) {
        return [System.IO.Path]::GetFullPath($EvidencePath)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $RootPath $EvidencePath))
}

function Get-TopLevelFieldMap {
    param([string]$Text)

    $map = @{}
    $lines = $Text -split "\r?\n"
    foreach ($line in $lines) {
        if ($line -match "^\s*([^:]+):\s*(.*)\s*$") {
            $key = $matches[1].Trim()
            $value = Normalize-FieldValue $matches[2]
            if (-not $map.ContainsKey($key)) {
                $map[$key] = $value
            }
        }
    }

    return $map
}

function Get-StateDetails {
    param(
        [string]$Text,
        [string[]]$States
    )

    $stateSet = @{}
    foreach ($state in $States) {
        $stateSet[$state] = $true
    }

    $details = @{}
    $currentState = $null
    $lines = $Text -split "\r?\n"

    foreach ($line in $lines) {
        if ($line -match "^\s*-\s+(.+?)\s*$") {
            $candidate = $matches[1].Trim()
            if ($stateSet.ContainsKey($candidate)) {
                $currentState = $candidate
                if (-not $details.ContainsKey($currentState)) {
                    $details[$currentState] = @{}
                }
                continue
            }
        }

        if ($null -ne $currentState -and $line -match "^\s+([^:]+):\s*(.*)\s*$") {
            $key = $matches[1].Trim()
            $value = Normalize-FieldValue $matches[2]
            $details[$currentState][$key] = $value
        }
    }

    return $details
}

function New-NotRecordedResult {
    param([string]$Path)

    return [ordered]@{
        playmode_suite_status = "not_recorded"
        suite_path = $Path
        suite_status_field = "NOT_RECORDED"
        missing_states = $requiredStates
        unprepared_states = @()
        missing_screenshots = @()
        invalid_fields = @()
        captured_count = 0
        expected_state_count = $requiredStates.Count
        state_results = [ordered]@{}
        date = "NOT_RECORDED"
        completed = "NOT_RECORDED"
        unity_version = "NOT_RECORDED"
        aspect_ratio_resolution = "NOT_RECORDED"
        suite_capture_source = "not_recorded"
        wave_combat_action_showcase_ready = $false
        wave_combat_action_showcase_reason = "suite_not_recorded"
        next_action = "Run Tools > Food Truck Prototype > Capture Verification Suite in Unity Play Mode, or register standalone PNGs with Tools\Register-PrototypePlayModeManualEvidence.ps1."
    }
}

function Get-WaveCombatActionShowcaseStatus {
    param([object]$StateResults)

    if ($null -eq $StateResults -or -not $StateResults.Contains("Wave Combat")) {
        return [ordered]@{
            ready = $false
            reason = "wave_combat_state_missing"
        }
    }

    $waveCombat = $StateResults["Wave Combat"]
    $prepared = if ($waveCombat.Contains("prepared")) { [string]$waveCombat["prepared"] } else { "NOT_RECORDED" }
    $prepareResult = if ($waveCombat.Contains("prepare_result")) { [string]$waveCombat["prepare_result"] } else { "NOT_RECORDED" }
    $screenshotExists = if ($waveCombat.Contains("screenshot_exists")) { [bool]$waveCombat["screenshot_exists"] } else { $false }

    if ($prepared.ToLowerInvariant() -eq "yes" -and $prepareResult.Contains("attack labels")) {
        return [ordered]@{
            ready = $true
            reason = "prepare_result_mentions_attack_labels"
        }
    }

    if ($screenshotExists) {
        return [ordered]@{
            ready = $false
            reason = "legacy_wave_combat_capture_without_attack_labels"
        }
    }

    return [ordered]@{
        ready = $false
        reason = "wave_combat_not_captured"
    }
}

if (-not (Test-Path -LiteralPath $suitePath -PathType Leaf)) {
    $result = New-NotRecordedResult -Path $suitePath
    if ($JsonOnly) {
        $result | ConvertTo-Json -Depth 8 -Compress | Write-Host
    }
    else {
        Write-Host "playmode_suite_status=not_recorded"
        Write-Host ("suite_path=" + $suitePath)
        Write-Host "next_action=Run Tools > Food Truck Prototype > Capture Verification Suite in Unity Play Mode, or register standalone PNGs with Tools\Register-PrototypePlayModeManualEvidence.ps1."
    }
    exit 0
}

$content = [System.IO.File]::ReadAllText($suitePath)
$topFields = Get-TopLevelFieldMap $content
$stateDetails = Get-StateDetails -Text $content -States $requiredStates

$invalidFields = New-Object System.Collections.Generic.List[string]
$missingStates = New-Object System.Collections.Generic.List[string]
$unpreparedStates = New-Object System.Collections.Generic.List[string]
$missingScreenshots = New-Object System.Collections.Generic.List[string]
$stateResults = [ordered]@{}
$capturedCount = 0

foreach ($field in @("Date", "Completed", "Unity version", "Aspect ratio / resolution", "Suite status")) {
    if (-not $topFields.ContainsKey($field) -or $topFields[$field] -eq "NOT_RECORDED") {
        $invalidFields.Add($field)
    }
}

$suiteStatusField = if ($topFields.ContainsKey("Suite status")) { $topFields["Suite status"] } else { "NOT_RECORDED" }
$suiteStatusNormalized = $suiteStatusField.ToLowerInvariant()
$manualSuiteStatuses = @("manual_partial", "manual_completed")
$isManualSuite = $manualSuiteStatuses -contains $suiteStatusNormalized
if ($suiteStatusNormalized -ne "completed" -and $suiteStatusNormalized -ne "aborted" -and $suiteStatusNormalized -ne "not_recorded" -and -not $isManualSuite) {
    $invalidFields.Add("Suite status=" + $suiteStatusField)
}

foreach ($state in $requiredStates) {
    if (-not $stateDetails.ContainsKey($state)) {
        $missingStates.Add($state)
        $stateResults[$state] = [ordered]@{
            prepared = "NOT_RECORDED"
            screenshot = "NOT_RECORDED"
            screenshot_exists = $false
            prepare_result = "NOT_RECORDED"
            hud_state_summary = "NOT_RECORDED"
        }
        continue
    }

    $details = $stateDetails[$state]
    $prepared = if ($details.ContainsKey("Prepared")) { $details["Prepared"] } else { "NOT_RECORDED" }
    $prepareResult = if ($details.ContainsKey("Prepare result")) { $details["Prepare result"] } else { "NOT_RECORDED" }
    $hudSummary = if ($details.ContainsKey("HUD state summary")) { $details["HUD state summary"] } else { "NOT_RECORDED" }
    $screenshotRaw = if ($details.ContainsKey("Screenshot")) { $details["Screenshot"] } else { "NOT_RECORDED" }
    $resolvedScreenshot = Resolve-EvidencePath -EvidencePath $screenshotRaw -RootPath $ProjectPath
    $screenshotExists = $false

    if ($prepared.ToLowerInvariant() -ne "yes") {
        $unpreparedStates.Add($state)
    }

    if ($resolvedScreenshot -eq "NOT_RECORDED") {
        $missingScreenshots.Add($state)
    }
    elseif (Test-Path -LiteralPath $resolvedScreenshot -PathType Leaf) {
        $screenshotExists = $true
        $capturedCount++
    }
    else {
        $missingScreenshots.Add($state + "=" + $resolvedScreenshot)
    }

    $stateResults[$state] = [ordered]@{
        prepared = $prepared
        screenshot = $resolvedScreenshot
        screenshot_exists = $screenshotExists
        prepare_result = $prepareResult
        hud_state_summary = $hudSummary
    }
}

$suiteCaptureSource = if ($topFields.ContainsKey("Evidence source")) {
    $topFields["Evidence source"]
}
elseif ($isManualSuite) {
    "manual screenshot registration"
}
elseif ($suiteStatusNormalized -eq "completed") {
    "unity capture suite"
}
else {
    "not_recorded"
}

$playModeSuiteStatus = if ($isManualSuite) { "captured_manual" } else { "captured" }
$nextAction = if ($isManualSuite) { "Review the manually registered screenshots visually, then record PASS or FIX_* in Docs\Prototype_PlayMode_Verification.md." } else { "Review the suite screenshots visually, then record PASS or FIX_* in Docs\Prototype_PlayMode_Verification.md." }

if ($invalidFields.Count -gt 0) {
    $playModeSuiteStatus = "invalid_suite"
    $nextAction = "Fix Docs\Prototype_PlayMode_Verification_Suite.txt or rerun Capture Verification Suite."
}
elseif ($suiteStatusNormalized -eq "not_recorded") {
    $playModeSuiteStatus = "not_recorded"
    $nextAction = "Run Tools > Food Truck Prototype > Capture Verification Suite in Unity Play Mode, or register standalone PNGs with Tools\Register-PrototypePlayModeManualEvidence.ps1."
}
elseif ($suiteStatusNormalized -eq "aborted") {
    $playModeSuiteStatus = "aborted"
    $nextAction = "Rerun Capture Verification Suite in Unity Play Mode."
}
elseif ($missingStates.Count -gt 0 -or $unpreparedStates.Count -gt 0 -or $missingScreenshots.Count -gt 0) {
    if ($isManualSuite) {
        $playModeSuiteStatus = "manual_partial"
        $nextAction = "Register missing state screenshots with Tools\Register-PrototypePlayModeManualEvidence.ps1, or rerun Capture Verification Suite when Play Mode input is reliable."
    }
    else {
        $playModeSuiteStatus = "incomplete"
        $nextAction = "Rerun Capture Verification Suite or retake missing states with Prepare and Capture State."
    }
}

$waveCombatActionShowcase = Get-WaveCombatActionShowcaseStatus -StateResults $stateResults

$result = [ordered]@{
    playmode_suite_status = $playModeSuiteStatus
    suite_path = $suitePath
    suite_status_field = $suiteStatusField
    missing_states = $missingStates.ToArray()
    unprepared_states = $unpreparedStates.ToArray()
    missing_screenshots = $missingScreenshots.ToArray()
    invalid_fields = $invalidFields.ToArray()
    captured_count = $capturedCount
    expected_state_count = $requiredStates.Count
    state_results = $stateResults
    date = if ($topFields.ContainsKey("Date")) { $topFields["Date"] } else { "NOT_RECORDED" }
    completed = if ($topFields.ContainsKey("Completed")) { $topFields["Completed"] } else { "NOT_RECORDED" }
    unity_version = if ($topFields.ContainsKey("Unity version")) { $topFields["Unity version"] } else { "NOT_RECORDED" }
    aspect_ratio_resolution = if ($topFields.ContainsKey("Aspect ratio / resolution")) { $topFields["Aspect ratio / resolution"] } else { "NOT_RECORDED" }
    suite_capture_source = $suiteCaptureSource
    wave_combat_action_showcase_ready = [bool]$waveCombatActionShowcase.ready
    wave_combat_action_showcase_reason = [string]$waveCombatActionShowcase.reason
    next_action = $nextAction
}

if ($JsonOnly) {
    $result | ConvertTo-Json -Depth 8 -Compress | Write-Host
}
else {
    Write-Host ("playmode_suite_status=" + $playModeSuiteStatus)
    Write-Host ("suite_status_field=" + $suiteStatusField)
    Write-Host ("suite_capture_source=" + $suiteCaptureSource)
    Write-Host ("captured_count=" + $capturedCount + "/" + $requiredStates.Count)
    Write-Host ("wave_combat_action_showcase_ready=" + [string]$waveCombatActionShowcase.ready)
    Write-Host ("wave_combat_action_showcase_reason=" + [string]$waveCombatActionShowcase.reason)
    foreach ($state in $requiredStates) {
        $stateResult = $stateResults[$state]
        Write-Host ("- " + $state + ": prepared=" + $stateResult["prepared"] + ", screenshot_exists=" + $stateResult["screenshot_exists"])
    }
    Write-Host ("next_action=" + $nextAction)
}

if ($playModeSuiteStatus -eq "invalid_suite") {
    exit 10
}

exit 0
