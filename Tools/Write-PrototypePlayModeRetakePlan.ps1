param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [string]$OutputPath = "",
    [switch]$PreviewOnly,
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"

$suiteVerifier = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeSuite.ps1"
$screenshotVerifier = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeScreenshots.ps1"
$recordVerifier = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeRecord.ps1"
$reviewPackWriter = Join-Path $ProjectPath "Tools\Write-PrototypePlayModeReviewPack.ps1"
$defaultOutputPath = Join-Path $ProjectPath "Docs\Prototype_PlayMode_RetakePlan.md"
$focusedStates = @("Draw Choice", "Pending Placement", "Invalid Placement")

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = $defaultOutputPath
}

function Invoke-JsonScript {
    param(
        [string]$ScriptPath,
        [string[]]$Arguments
    )

    if (-not (Test-Path -LiteralPath $ScriptPath -PathType Leaf)) {
        throw "Missing script: $ScriptPath"
    }

    $output = & powershell -ExecutionPolicy Bypass -File $ScriptPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Script failed with exit code $LASTEXITCODE`: $ScriptPath"
    }

    $jsonLine = $output | Select-Object -Last 1
    if ([string]::IsNullOrWhiteSpace([string]$jsonLine)) {
        throw "Script produced no JSON output: $ScriptPath"
    }

    return ($jsonLine | ConvertFrom-Json)
}

function Write-TextWithFallback {
    param(
        [string]$PreferredPath,
        [string]$Contents
    )

    try {
        $preferredDir = Split-Path -Parent $PreferredPath
        if (-not [string]::IsNullOrWhiteSpace($preferredDir)) {
            [System.IO.Directory]::CreateDirectory($preferredDir) | Out-Null
        }

        [System.IO.File]::WriteAllText($PreferredPath, $Contents, [System.Text.Encoding]::UTF8)
        return $PreferredPath
    }
    catch {
        $fallbackRoot = Join-Path ([System.IO.Path]::GetTempPath()) "zombieFoodcenter-verification"
        [System.IO.Directory]::CreateDirectory($fallbackRoot) | Out-Null
        $fallbackPath = Join-Path $fallbackRoot (Split-Path -Leaf $PreferredPath)
        [System.IO.File]::WriteAllText($fallbackPath, $Contents, [System.Text.Encoding]::UTF8)
        return $fallbackPath
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

function Get-StateCriteria {
    param([string]$StateName)

    switch ($StateName) {
        "Draw Choice" {
            return "Three comparable cards; shape, ingredient, value/risk, Fit, Heat, and Role readable; battlefield is still visible."
        }
        "Pending Placement" {
            return "Pending block, 3x3 board, recommendation reason, rotation, and next action readable; battlefield still occupies the main screen."
        }
        "Invalid Placement" {
            return "Blocked reason and Next recovery hint appear near the board or cue, without requiring the full log."
        }
        default {
            return "Required state is readable and can be judged from the screenshot."
        }
    }
}

function Get-StateMenuPath {
    param([string]$StateName)

    return "Tools > Food Truck Prototype > Prepare and Capture State > " + $StateName
}

function Build-RetakeTargetRows {
    param([string[]]$States)

    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($state in $States) {
        $rows.Add([pscustomobject]@{
            state = $state
            menu_path = Get-StateMenuPath -StateName $state
            must_see = Get-StateCriteria -StateName $state
        }) | Out-Null
    }

    return $rows.ToArray()
}

function Build-RetakePlanMarkdown {
    param(
        [object]$SuiteData,
        [object]$ScreenshotData,
        [object]$RecordData,
        [object]$ReviewPackData,
        [object[]]$FocusedRows
    )

    $missingStates = Get-ArrayValue (Get-ObjectProperty -ObjectValue $ScreenshotData -PropertyName "missing_states")
    $manualCandidateCount = Get-ObjectProperty -ObjectValue $ScreenshotData -PropertyName "manual_registration_candidate_count" -DefaultValue 0
    $triagedNonStateCount = Get-ObjectProperty -ObjectValue $ScreenshotData -PropertyName "triaged_non_state_count" -DefaultValue 0
    $reviewReadiness = Get-ObjectProperty -ObjectValue $ReviewPackData -PropertyName "review_readiness" -DefaultValue "unknown"
    $waveReady = Get-ObjectProperty -ObjectValue $SuiteData -PropertyName "wave_combat_action_showcase_ready" -DefaultValue $false
    $waveReason = Get-ObjectProperty -ObjectValue $SuiteData -PropertyName "wave_combat_action_showcase_reason" -DefaultValue "unknown"

    $builder = New-Object System.Text.StringBuilder
    [void]$builder.AppendLine("# Prototype Play Mode Retake Plan")
    [void]$builder.AppendLine()
    [void]$builder.AppendLine("Generated: " + (Get-Date).ToString("yyyy-MM-dd HH:mm") + " KST")
    [void]$builder.AppendLine('Retake plan status: `ok`')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine("## Machine Summary")
    [void]$builder.AppendLine('- Suite status: `' + $SuiteData.playmode_suite_status + '` (' + $SuiteData.captured_count + '/' + $SuiteData.expected_state_count + ')')
    [void]$builder.AppendLine('- Suite evidence source: `' + $SuiteData.suite_capture_source + '`')
    [void]$builder.AppendLine('- Screenshot status: `' + $ScreenshotData.playmode_screenshot_status + '` (' + $ScreenshotData.covered_state_count + '/' + $ScreenshotData.expected_state_count + ' covered)')
    [void]$builder.AppendLine("- Missing labeled states: " + ($(if ($missingStates.Count -gt 0) { $missingStates -join ", " } else { "none" })))
    [void]$builder.AppendLine('- Manual registration candidates: `' + $manualCandidateCount + '`')
    [void]$builder.AppendLine('- Triaged non-state screenshots: `' + $triagedNonStateCount + '`')
    [void]$builder.AppendLine('- Review readiness: `' + $reviewReadiness + '`')
    [void]$builder.AppendLine('- Manual record status: `' + $RecordData.playmode_record_status + '`')
    [void]$builder.AppendLine('- Wave Combat action showcase: `' + [string]$waveReady + '` (' + [string]$waveReason + ')')
    [void]$builder.AppendLine()

    if ($manualCandidateCount -eq 0 -and $FocusedRows.Count -gt 0) {
        [void]$builder.AppendLine("No standalone PNG candidates remain for the missing required states. Capture fresh evidence instead of trying to register old screenshots.")
        [void]$builder.AppendLine()
    }

    [void]$builder.AppendLine("## Focused Retake Targets")
    if ($FocusedRows.Count -eq 0) {
        [void]$builder.AppendLine("No Draw/Pending/Invalid focused retakes are currently missing. Continue with review pack judgment or the next recorded blocker.")
    }
    else {
        [void]$builder.AppendLine("| State | Unity menu path | Must see |")
        [void]$builder.AppendLine("| --- | --- | --- |")
        foreach ($row in $FocusedRows) {
            [void]$builder.AppendLine('| ' + $row.state + ' | `' + $row.menu_path + '` | ' + $row.must_see + ' |')
        }
    }
    [void]$builder.AppendLine()

    [void]$builder.AppendLine("## Wave Combat Note")
    if ($waveReady -eq $true) {
        [void]$builder.AppendLine("Wave Combat action showcase is already machine-reported as ready. Still judge overlap/readability visually before recording PASS.")
    }
    else {
        [void]$builder.AppendLine('Wave Combat has legacy or incomplete action-showcase evidence. If Play Mode is available, retake Wave Combat too and confirm `-12`, `KO`, `LEAK`, `TRUCK -7`, and lane flash are readable.')
    }
    [void]$builder.AppendLine()

    [void]$builder.AppendLine("## Follow-Up Commands")
    [void]$builder.AppendLine("Before opening Unity, verify this plan still matches the current evidence state:")
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('```powershell')
    [void]$builder.AppendLine('powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRetakePlan.ps1" -ProjectPath "' + $ProjectPath + '"')
    [void]$builder.AppendLine('powershell -ExecutionPolicy Bypass -File "Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1" -ProjectPath "' + $ProjectPath + '"')
    [void]$builder.AppendLine('```')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine("After retaking the missing screenshots, run these in order:")
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('```powershell')
    [void]$builder.AppendLine('powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeSuite.ps1" -ProjectPath "' + $ProjectPath + '"')
    [void]$builder.AppendLine('powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeScreenshots.ps1" -ProjectPath "' + $ProjectPath + '"')
    [void]$builder.AppendLine('powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "' + $ProjectPath + '" -PreviewOnly -JsonOnly')
    [void]$builder.AppendLine('powershell -ExecutionPolicy Bypass -File "Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1" -ProjectPath "' + $ProjectPath + '"')
    [void]$builder.AppendLine('```')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('Generate the full review pack only after `review_readiness` reaches `ready_for_visual_review` or when you intentionally want a partial evidence sheet for discussion.')

    return $builder.ToString()
}

$suiteData = Invoke-JsonScript -ScriptPath $suiteVerifier -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-JsonOnly"
)

$screenshotData = Invoke-JsonScript -ScriptPath $screenshotVerifier -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-JsonOnly"
)

$recordData = Invoke-JsonScript -ScriptPath $recordVerifier -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-JsonOnly"
)

$reviewPackData = Invoke-JsonScript -ScriptPath $reviewPackWriter -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-PreviewOnly",
    "-JsonOnly"
)

$missingStates = Get-ArrayValue (Get-ObjectProperty -ObjectValue $screenshotData -PropertyName "missing_states")
$focusedRetakeStates = @($focusedStates | Where-Object { $missingStates -contains $_ })
$focusedRows = Build-RetakeTargetRows -States $focusedRetakeStates
$markdown = Build-RetakePlanMarkdown -SuiteData $suiteData -ScreenshotData $screenshotData -RecordData $recordData -ReviewPackData $reviewPackData -FocusedRows $focusedRows

$actualOutputPath = "PREVIEW_ONLY"
if (-not $PreviewOnly) {
    $actualOutputPath = Write-TextWithFallback -PreferredPath $OutputPath -Contents $markdown
}

$manualCandidateCount = Get-ObjectProperty -ObjectValue $screenshotData -PropertyName "manual_registration_candidate_count" -DefaultValue 0
$nextAction = if ($focusedRetakeStates.Count -gt 0 -and $manualCandidateCount -eq 0) {
    "Run focused retakes for " + ($focusedRetakeStates -join ", ") + " with Tools > Food Truck Prototype > Prepare and Capture State, then rerun suite/screenshot/review pack checks."
}
elseif ($focusedRetakeStates.Count -gt 0) {
    "Inspect manual registration candidates first; retake any missing state without a visually matching PNG."
}
elseif ($reviewPackData.review_readiness -eq "ready_for_visual_review") {
    "Open the review pack and record PASS/FIX/BLOCKED decisions."
}
else {
    "Rerun session status and review pack preview to choose the next evidence action."
}

$result = [ordered]@{
    retake_plan_status = "ok"
    output_path = $actualOutputPath
    missing_states = $missingStates
    focused_retake_states = $focusedRetakeStates
    focused_retake_count = $focusedRetakeStates.Count
    manual_registration_candidate_count = $manualCandidateCount
    triaged_non_state_count = Get-ObjectProperty -ObjectValue $screenshotData -PropertyName "triaged_non_state_count" -DefaultValue 0
    suite_status = $suiteData.playmode_suite_status
    screenshot_status = $screenshotData.playmode_screenshot_status
    review_readiness = $reviewPackData.review_readiness
    manual_record_status = $recordData.playmode_record_status
    wave_combat_action_showcase_ready = $suiteData.wave_combat_action_showcase_ready
    wave_combat_action_showcase_reason = $suiteData.wave_combat_action_showcase_reason
    next_action = $nextAction
}

if ($JsonOnly) {
    $result | ConvertTo-Json -Depth 6 -Compress | Write-Host
}
else {
    Write-Host "retake_plan_status=ok"
    Write-Host ("focused_retake_count=" + $focusedRetakeStates.Count)
    Write-Host ("missing_states=" + ($(if ($missingStates.Count -gt 0) { $missingStates -join ", " } else { "none" })))
    Write-Host ("output_path=" + $actualOutputPath)
    Write-Host ("next_action=" + $nextAction)
}

exit 0
