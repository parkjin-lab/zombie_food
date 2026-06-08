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
$assetVerifier = Join-Path $ProjectPath "Tools\Verify-PrototypeAssets.ps1"
$defaultOutputPath = Join-Path $ProjectPath "Docs\Prototype_PlayMode_ReviewPack.md"
$requiredStates = @("Draw Choice", "Pending Placement", "Invalid Placement", "Wave Combat")

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = $defaultOutputPath
}

function Invoke-JsonVerifier {
    param(
        [string]$ScriptPath,
        [string]$RootPath
    )

    if (-not (Test-Path -LiteralPath $ScriptPath -PathType Leaf)) {
        throw "Missing verifier: $ScriptPath"
    }

    $output = & powershell -ExecutionPolicy Bypass -File $ScriptPath -ProjectPath $RootPath -JsonOnly
    if ($LASTEXITCODE -ne 0) {
        throw "Verifier failed with exit code $LASTEXITCODE`: $ScriptPath"
    }

    $jsonLine = $output | Select-Object -Last 1
    if ([string]::IsNullOrWhiteSpace([string]$jsonLine)) {
        throw "Verifier produced no JSON output: $ScriptPath"
    }

    return ($jsonLine | ConvertFrom-Json)
}

function Write-TextWithFallback {
    param(
        [string]$PreferredPath,
        [string]$Contents
    )

    $normalizedContents = $Contents -replace "`r`n", "`n" -replace "`r", "`n"

    try {
        $preferredDir = Split-Path -Parent $PreferredPath
        if (-not [string]::IsNullOrWhiteSpace($preferredDir)) {
            [System.IO.Directory]::CreateDirectory($preferredDir) | Out-Null
        }

        [System.IO.File]::WriteAllText($PreferredPath, $normalizedContents, [System.Text.Encoding]::UTF8)
        return $PreferredPath
    }
    catch {
        $fallbackRoot = Join-Path ([System.IO.Path]::GetTempPath()) "zombieFoodcenter-verification"
        [System.IO.Directory]::CreateDirectory($fallbackRoot) | Out-Null
        $fallbackPath = Join-Path $fallbackRoot (Split-Path -Leaf $PreferredPath)
        [System.IO.File]::WriteAllText($fallbackPath, $normalizedContents, [System.Text.Encoding]::UTF8)
        return $fallbackPath
    }
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

function Get-MarkdownPath {
    param(
        [string]$TargetPath,
        [string]$FromFilePath
    )

    if ([string]::IsNullOrWhiteSpace($TargetPath) -or $TargetPath -eq "NOT_RECORDED") {
        return "NOT_RECORDED"
    }

    try {
        $fromDirectory = Split-Path -Parent ([System.IO.Path]::GetFullPath($FromFilePath))
        $fromUri = New-Object System.Uri (($fromDirectory.TrimEnd("\") + "\"))
        $toUri = New-Object System.Uri ([System.IO.Path]::GetFullPath($TargetPath))
        return $fromUri.MakeRelativeUri($toUri).ToString()
    }
    catch {
        return $TargetPath
    }
}

function Get-ObjectProperty {
    param(
        [object]$ObjectValue,
        [string]$PropertyName
    )

    if ($null -eq $ObjectValue) {
        return $null
    }

    $property = $ObjectValue.PSObject.Properties[$PropertyName]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Get-SuiteStateResult {
    param(
        [object]$SuiteData,
        [string]$StateName
    )

    $stateResults = Get-ObjectProperty -ObjectValue $SuiteData -PropertyName "state_results"
    return Get-ObjectProperty -ObjectValue $stateResults -PropertyName $StateName
}

function Get-RecordStateValue {
    param(
        [object]$RecordData,
        [string]$StateName
    )

    $stateResults = Get-ObjectProperty -ObjectValue $RecordData -PropertyName "state_results"
    $value = Get-ObjectProperty -ObjectValue $stateResults -PropertyName $StateName
    if ($null -eq $value) {
        return "NOT_RECORDED"
    }

    return [string]$value
}

function Get-ScreenshotRows {
    param([object]$ScreenshotData)

    if ($null -eq $ScreenshotData -or $null -eq $ScreenshotData.screenshots) {
        return @()
    }

    return @($ScreenshotData.screenshots)
}

function Get-ManualRegistrationCommands {
    param([object]$ScreenshotData)

    if ($null -eq $ScreenshotData -or $null -eq $ScreenshotData.manual_registration_commands) {
        return @()
    }

    return @($ScreenshotData.manual_registration_commands)
}

function Get-TriagedNonStateScreenshots {
    param([object]$ScreenshotData)

    if ($null -eq $ScreenshotData -or $null -eq $ScreenshotData.triaged_non_state_screenshots) {
        return @()
    }

    return @($ScreenshotData.triaged_non_state_screenshots)
}

function Get-PlannedResourceRows {
    param([object]$AssetData)

    if ($null -eq $AssetData -or $null -eq $AssetData.checks) {
        return @()
    }

    return @($AssetData.checks | Where-Object { $_.status -eq "missing_planned" })
}

function Get-ReviewReadiness {
    param(
        [object]$SuiteData,
        [object]$ScreenshotData
    )

    if ($ScreenshotData.playmode_screenshot_status -eq "invalid_screenshots") {
        return "needs_screenshot_fix"
    }

    $suiteReady = $SuiteData.playmode_suite_status -eq "captured" -or $SuiteData.playmode_suite_status -eq "captured_manual"
    if ($suiteReady -and $ScreenshotData.playmode_screenshot_status -eq "suite_ready") {
        return "ready_for_visual_review"
    }

    if ($ScreenshotData.screenshot_count -gt 0) {
        return "partial_evidence"
    }

    return "needs_suite_capture"
}

function Get-WaveCombatActionShowcaseLine {
    param([object]$SuiteData)

    $ready = Get-ObjectProperty -ObjectValue $SuiteData -PropertyName "wave_combat_action_showcase_ready"
    $reason = Get-ObjectProperty -ObjectValue $SuiteData -PropertyName "wave_combat_action_showcase_reason"
    if ($null -eq $ready) {
        return '`unknown` (suite verifier did not report action showcase status)'
    }

    return '`' + [string]$ready + '` (' + [string]$reason + ')'
}

function Build-StateEvidenceLine {
    param(
        [object]$SuiteData,
        [object[]]$ScreenshotRows,
        [string]$StateName,
        [string]$OutputFilePath
    )

    $suiteState = Get-SuiteStateResult -SuiteData $SuiteData -StateName $StateName
    if ($null -ne $suiteState -and $suiteState.screenshot_exists -eq $true) {
        $path = Resolve-EvidencePath -EvidencePath ([string]$suiteState.screenshot) -RootPath $ProjectPath
        return "suite: " + (Get-MarkdownPath -TargetPath $path -FromFilePath $OutputFilePath)
    }

    $matched = @($ScreenshotRows | Where-Object { $_.state -eq $StateName })
    if ($matched.Count -gt 0) {
        return "filename: " + (Get-MarkdownPath -TargetPath ([string]$matched[0].path) -FromFilePath $OutputFilePath)
    }

    return "missing"
}

function Build-ReviewPackMarkdown {
    param(
        [object]$SuiteData,
        [object]$ScreenshotData,
        [object]$AssetData,
        [object]$RecordData,
        [string]$OutputFilePath
    )

    $readiness = Get-ReviewReadiness -SuiteData $SuiteData -ScreenshotData $ScreenshotData
    $screenshotRows = Get-ScreenshotRows -ScreenshotData $ScreenshotData
    $manualRegistrationCommands = Get-ManualRegistrationCommands -ScreenshotData $ScreenshotData
    $triagedNonStateScreenshots = Get-TriagedNonStateScreenshots -ScreenshotData $ScreenshotData
    $plannedResourceRows = Get-PlannedResourceRows -AssetData $AssetData
    $builder = New-Object System.Text.StringBuilder

    [void]$builder.AppendLine("# Prototype Play Mode Review Pack")
    [void]$builder.AppendLine()
    [void]$builder.AppendLine("Generated: " + (Get-Date).ToString("yyyy-MM-dd HH:mm") + " KST")
    [void]$builder.AppendLine('Review readiness: `' + $readiness + '`')
    [void]$builder.AppendLine('Visual review required: `' + [string]$ScreenshotData.visual_review_required + '`')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine("## Machine Summary")
    [void]$builder.AppendLine('- Suite status: `' + $SuiteData.playmode_suite_status + '` (' + $SuiteData.captured_count + '/' + $SuiteData.expected_state_count + ')')
    if ($null -ne $SuiteData.suite_capture_source) {
        [void]$builder.AppendLine('- Suite evidence source: `' + $SuiteData.suite_capture_source + '`')
    }
    [void]$builder.AppendLine('- Screenshot status: `' + $ScreenshotData.playmode_screenshot_status + '` (' + $ScreenshotData.machine_quality_pass_count + '/' + $ScreenshotData.screenshot_count + ' quality pass)')
    [void]$builder.AppendLine('- Unlabeled screenshots: `' + $ScreenshotData.unlabeled_count + '`')
    if ($null -ne $ScreenshotData.manual_registration_candidate_count) {
        [void]$builder.AppendLine('- Manual registration candidates: `' + $ScreenshotData.manual_registration_candidate_count + '`')
    }
    if ($null -ne $ScreenshotData.triaged_non_state_count) {
        [void]$builder.AppendLine('- Triaged non-state screenshots: `' + $ScreenshotData.triaged_non_state_count + '`')
    }
    [void]$builder.AppendLine('- Manual record status: `' + $RecordData.playmode_record_status + '`')
    if ($null -ne $AssetData) {
        [void]$builder.AppendLine('- Asset status: `' + $AssetData.asset_status + '`, planned missing: `' + $AssetData.planned_missing + '`')
    }
    [void]$builder.AppendLine('- Wave Combat action showcase: ' + (Get-WaveCombatActionShowcaseLine -SuiteData $SuiteData))
    if ($ScreenshotData.missing_states.Count -gt 0) {
        [void]$builder.AppendLine("- Missing labeled states: " + ($ScreenshotData.missing_states -join ", "))
    }
    [void]$builder.AppendLine()

    [void]$builder.AppendLine("## Planned Resource Backlog")
    [void]$builder.AppendLine("These missing planned resources are not runtime blockers. Use them to separate feedback polish from Play Mode readability failures.")
    if ($plannedResourceRows.Count -eq 0) {
        [void]$builder.AppendLine("No planned VFX/SFX resources are missing.")
    }
    else {
        [void]$builder.AppendLine("| Category | Resource | Intended role |")
        [void]$builder.AppendLine("| --- | --- | --- |")
        foreach ($resource in $plannedResourceRows) {
            [void]$builder.AppendLine("| " + $resource.category + " | ``" + $resource.path + "`` | " + $resource.recommendation + " |")
        }
    }
    [void]$builder.AppendLine()

    [void]$builder.AppendLine("## State Review Table")
    [void]$builder.AppendLine("| State | Evidence | Current record | Visual decision |")
    [void]$builder.AppendLine("| --- | --- | --- | --- |")
    foreach ($state in $requiredStates) {
        $evidence = Build-StateEvidenceLine -SuiteData $SuiteData -ScreenshotRows $screenshotRows -StateName $state -OutputFilePath $OutputFilePath
        $recordValue = Get-RecordStateValue -RecordData $RecordData -StateName $state
        [void]$builder.AppendLine('| ' + $state + ' | ' + $evidence + ' | `' + $recordValue + '` | PASS / FIX_LAYOUT / FIX_ASSET / FIX_FEEDBACK / BLOCKED |')
    }
    [void]$builder.AppendLine()

    [void]$builder.AppendLine("## Screenshot Contact Sheet")
    if ($screenshotRows.Count -eq 0) {
        [void]$builder.AppendLine("No Play Mode screenshots found.")
    }
    else {
        foreach ($row in $screenshotRows) {
            $markdownPath = Get-MarkdownPath -TargetPath ([string]$row.path) -FromFilePath $OutputFilePath
            [void]$builder.AppendLine("### " + $row.state)
            [void]$builder.AppendLine('- Quality: `' + $row.machine_quality_pass + '`, size: `' + $row.width + 'x' + $row.height + '`, bytes: `' + $row.size_bytes + '`')
            [void]$builder.AppendLine("![" + $row.state + "](" + $markdownPath + ")")
            [void]$builder.AppendLine()
        }
    }

    [void]$builder.AppendLine("## Visual Acceptance Checklist")
    [void]$builder.AppendLine("| State | Must see in screenshot | Record as FIX when missing |")
    [void]$builder.AppendLine("| --- | --- | --- |")
    [void]$builder.AppendLine("| Draw Choice | Three comparable cards with shape, ingredient, value/risk, ``Fit``, ``Heat``, and ``Role`` visible. | ``FIX_LAYOUT`` if clipped or overlapping; ``FIX_FEEDBACK`` if the choice tradeoff is unclear. |")
    [void]$builder.AppendLine("| Pending Placement | Pending block, 3x3 board, recommendation reason, rotation, and next action are readable. | ``FIX_LAYOUT`` if board/controls crowd the battlefield; ``FIX_FEEDBACK`` if the next action is unclear. |")
    [void]$builder.AppendLine("| Invalid Placement | Blocked reason and ``Next`` recovery hint appear near the board/cue. | ``FIX_FEEDBACK`` if the reason or recovery hint is missing. |")
    [void]$builder.AppendLine("| Wave Combat | Battlefield takes over half the screen, one long truck is visible, enemies/lane pressure are readable, and attack trails plus action labels ``-12``, ``KO``, ``LEAK``, ``TRUCK -7`` and lane flash are visible. | ``FIX_LAYOUT`` if the battlefield is crowded; ``FIX_FEEDBACK`` if attack trails, action labels, or payoff cues are missing. |")
    [void]$builder.AppendLine()

    [void]$builder.AppendLine("## Manual Evidence Registration Hints")
    if ($manualRegistrationCommands.Count -eq 0) {
        [void]$builder.AppendLine("No unlabeled machine-quality PNG candidates are available for manual registration.")
    }
    else {
        [void]$builder.AppendLine("Use these only after visually confirming the PNG actually matches the target state.")
        foreach ($commandTemplate in $manualRegistrationCommands) {
            [void]$builder.AppendLine()
            [void]$builder.AppendLine("### " + $commandTemplate.state)
            [void]$builder.AppendLine('- Candidate: `' + $commandTemplate.relative_path + '`')
            [void]$builder.AppendLine('```powershell')
            [void]$builder.AppendLine([string]$commandTemplate.command)
            [void]$builder.AppendLine('```')
        }
    }
    [void]$builder.AppendLine()

    [void]$builder.AppendLine("## Triaged Non-State Screenshots")
    if ($triagedNonStateScreenshots.Count -eq 0) {
        [void]$builder.AppendLine("No screenshots have been triaged as non-required states.")
    }
    else {
        foreach ($triaged in $triagedNonStateScreenshots) {
            [void]$builder.AppendLine('- `' + $triaged.relative_path + '`: ' + $triaged.reason)
        }
    }
    [void]$builder.AppendLine()

    [void]$builder.AppendLine("## Recommended Result Commands")
    [void]$builder.AppendLine("All PASS after visual review:")
    [void]$builder.AppendLine('```powershell')
    [void]$builder.AppendLine('powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeResultFromSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -DrawChoice PASS -PendingPlacement PASS -InvalidPlacement PASS -WaveCombat PASS -Apply')
    [void]$builder.AppendLine('```')
    [void]$builder.AppendLine("Example FIX result:")
    [void]$builder.AppendLine('```powershell')
    [void]$builder.AppendLine('powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeResultFromSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -DrawChoice PASS -PendingPlacement FIX_LAYOUT -InvalidPlacement FIX_FEEDBACK -WaveCombat PASS -Apply')
    [void]$builder.AppendLine('```')
    [void]$builder.AppendLine("After applying a result:")
    [void]$builder.AppendLine('```powershell')
    [void]$builder.AppendLine('powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRecord.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly')
    [void]$builder.AppendLine('```')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine("Note: this review pack organizes evidence only. It does not replace visual PASS/FIX/BLOCKED judgment.")

    return $builder.ToString()
}

$suiteData = Invoke-JsonVerifier -ScriptPath $suiteVerifier -RootPath $ProjectPath
$screenshotData = Invoke-JsonVerifier -ScriptPath $screenshotVerifier -RootPath $ProjectPath
$recordData = Invoke-JsonVerifier -ScriptPath $recordVerifier -RootPath $ProjectPath
$assetData = Invoke-JsonVerifier -ScriptPath $assetVerifier -RootPath $ProjectPath
$readiness = Get-ReviewReadiness -SuiteData $suiteData -ScreenshotData $screenshotData
$markdown = Build-ReviewPackMarkdown -SuiteData $suiteData -ScreenshotData $screenshotData -AssetData $assetData -RecordData $recordData -OutputFilePath $OutputPath

$actualOutputPath = "PREVIEW_ONLY"
if (-not $PreviewOnly) {
    $actualOutputPath = Write-TextWithFallback -PreferredPath $OutputPath -Contents $markdown
}

$nextAction = if ($readiness -eq "ready_for_visual_review") {
    "Open the review pack, make visual PASS/FIX/BLOCKED decisions, then apply the result writer command."
}
elseif ($screenshotData.manual_registration_candidate_count -gt 0) {
    "Visually inspect unlabeled PNG candidates, then register any matching missing state with Tools\Register-PrototypePlayModeManualEvidence.ps1."
}
else {
    "Use Capture Verification Suite or focused retakes for missing states, then regenerate this review pack."
}

$result = [ordered]@{
    review_pack_status = "ok"
    review_readiness = $readiness
    output_path = $actualOutputPath
    suite_status = $suiteData.playmode_suite_status
    suite_capture_source = $suiteData.suite_capture_source
    screenshot_status = $screenshotData.playmode_screenshot_status
    screenshot_count = $screenshotData.screenshot_count
    machine_quality_pass_count = $screenshotData.machine_quality_pass_count
    missing_states = $screenshotData.missing_states
    manual_registration_candidate_count = $screenshotData.manual_registration_candidate_count
    manual_registration_commands = $screenshotData.manual_registration_commands
    triaged_non_state_count = $screenshotData.triaged_non_state_count
    triaged_non_state_screenshots = $screenshotData.triaged_non_state_screenshots
    manual_record_status = $recordData.playmode_record_status
    asset_status = $assetData.asset_status
    asset_planned_missing = $assetData.planned_missing
    wave_combat_action_showcase_ready = $suiteData.wave_combat_action_showcase_ready
    wave_combat_action_showcase_reason = $suiteData.wave_combat_action_showcase_reason
    visual_review_required = $screenshotData.visual_review_required
    next_action = $nextAction
}

if ($JsonOnly) {
    $result | ConvertTo-Json -Depth 6 -Compress | Write-Host
}
else {
    Write-Host "review_pack_status=ok"
    Write-Host ("review_readiness=" + $readiness)
    Write-Host ("output_path=" + $actualOutputPath)
    Write-Host ("suite_status=" + $suiteData.playmode_suite_status)
    Write-Host ("screenshot_status=" + $screenshotData.playmode_screenshot_status)
    Write-Host ("screenshot_count=" + $screenshotData.screenshot_count)
    Write-Host ("manual_record_status=" + $recordData.playmode_record_status)
    Write-Host ("asset_status=" + $assetData.asset_status)
    Write-Host ("asset_planned_missing=" + $assetData.planned_missing)
    Write-Host ("next_action=" + $result.next_action)
}

exit 0
