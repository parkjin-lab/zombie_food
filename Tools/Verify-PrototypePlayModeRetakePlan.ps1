param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [string]$PlanPath = "",
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"

$scriptName = "Verify-PrototypePlayModeRetakePlan.ps1"
$retakePlanWriter = Join-Path $ProjectPath "Tools\Write-PrototypePlayModeRetakePlan.ps1"
$defaultPlanPath = Join-Path $ProjectPath "Docs\Prototype_PlayMode_RetakePlan.md"

if ([string]::IsNullOrWhiteSpace($PlanPath)) {
    $PlanPath = $defaultPlanPath
}

function Invoke-RetakePlanPreview {
    param(
        [string]$ScriptPath,
        [string]$RootPath
    )

    if (-not (Test-Path -LiteralPath $ScriptPath -PathType Leaf)) {
        throw "Missing retake plan writer: $ScriptPath"
    }

    $output = & powershell -ExecutionPolicy Bypass -File $ScriptPath -ProjectPath $RootPath -PreviewOnly -JsonOnly
    if ($LASTEXITCODE -ne 0) {
        throw "Retake plan writer failed with exit code $LASTEXITCODE`: $ScriptPath"
    }

    $jsonLine = $output | Select-Object -Last 1
    if ([string]::IsNullOrWhiteSpace([string]$jsonLine)) {
        throw "Retake plan writer produced no JSON output: $ScriptPath"
    }

    return ($jsonLine | ConvertFrom-Json)
}

function Get-ArrayValue {
    param([object]$Value)

    if ($null -eq $Value) {
        return @()
    }

    return @($Value)
}

function Get-ListText {
    param([object]$Value)

    $items = Get-ArrayValue $Value
    if ($items.Count -eq 0) {
        return "none"
    }

    return $items -join ", "
}

function Add-MissingNeedle {
    param(
        [System.Collections.Generic.List[string]]$Target,
        [string]$Source,
        [string]$Needle
    )

    if ([string]::IsNullOrEmpty($Source) -or -not $Source.Contains($Needle)) {
        $Target.Add($Needle) | Out-Null
    }
}

try {
    $previewData = Invoke-RetakePlanPreview -ScriptPath $retakePlanWriter -RootPath $ProjectPath
}
catch {
    $result = [ordered]@{
        retake_plan_doc_status = "preview_failed"
        verifier = $scriptName
        plan_path = $PlanPath
        missing_needles = @()
        expected_focused_retake_count = $null
        documented_focused_retake_count = $null
        error = $_.Exception.Message
        next_action = "Fix Tools\Write-PrototypePlayModeRetakePlan.ps1 before checking the retake plan document."
    }

    $result | ConvertTo-Json -Depth 6 -Compress | Write-Host
    exit 12
}

$planExists = Test-Path -LiteralPath $PlanPath -PathType Leaf
$content = if ($planExists) { [System.IO.File]::ReadAllText($PlanPath) } else { "" }
$missingNeedles = New-Object System.Collections.Generic.List[string]
$focusedStates = Get-ArrayValue $previewData.focused_retake_states
$missingStatesText = Get-ListText $previewData.missing_states
$expectedFocusedRetakeCount = [int]$previewData.focused_retake_count
$documentedFocusedRetakeCount = 0

if ($planExists) {
    foreach ($state in $focusedStates) {
        $menuPath = "Tools > Food Truck Prototype > Prepare and Capture State > " + [string]$state
        if ($content.Contains($menuPath)) {
            $documentedFocusedRetakeCount++
        }
    }

    Add-MissingNeedle $missingNeedles $content "# Prototype Play Mode Retake Plan"
    Add-MissingNeedle $missingNeedles $content 'Retake plan status: `ok`'
    Add-MissingNeedle $missingNeedles $content ('Suite status: `' + $previewData.suite_status + '`')
    Add-MissingNeedle $missingNeedles $content ('Screenshot status: `' + $previewData.screenshot_status + '`')
    Add-MissingNeedle $missingNeedles $content ("Missing labeled states: " + $missingStatesText)
    Add-MissingNeedle $missingNeedles $content ('Manual registration candidates: `' + $previewData.manual_registration_candidate_count + '`')
    Add-MissingNeedle $missingNeedles $content ('Triaged non-state screenshots: `' + $previewData.triaged_non_state_count + '`')
    Add-MissingNeedle $missingNeedles $content ('Review readiness: `' + $previewData.review_readiness + '`')
    Add-MissingNeedle $missingNeedles $content ('Manual record status: `' + $previewData.manual_record_status + '`')
    Add-MissingNeedle $missingNeedles $content ('Wave Combat action showcase: `' + [string]$previewData.wave_combat_action_showcase_ready + '` (' + [string]$previewData.wave_combat_action_showcase_reason + ')')
    Add-MissingNeedle $missingNeedles $content 'Tools\Verify-PrototypePlayModeRetakePlan.ps1'
    Add-MissingNeedle $missingNeedles $content 'Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1'
    Add-MissingNeedle $missingNeedles $content 'Tools\Verify-PrototypePlayModeSuite.ps1'
    Add-MissingNeedle $missingNeedles $content 'Tools\Verify-PrototypePlayModeScreenshots.ps1'
    Add-MissingNeedle $missingNeedles $content 'Tools\Write-PrototypePlayModeReviewPack.ps1'

    foreach ($state in $focusedStates) {
        Add-MissingNeedle $missingNeedles $content ("Tools > Food Truck Prototype > Prepare and Capture State > " + [string]$state)
    }
}

$status = "ok"
$nextAction = "Retake plan document is current with the latest suite/screenshot/review preview state."

if (-not $planExists) {
    $status = "missing_doc"
    $nextAction = "Run Tools\Write-PrototypePlayModeRetakePlan.ps1 to create Docs\Prototype_PlayMode_RetakePlan.md."
}
elseif ($missingNeedles.Count -gt 0 -or $documentedFocusedRetakeCount -ne $expectedFocusedRetakeCount) {
    $status = "stale_doc"
    $nextAction = "Regenerate Docs\Prototype_PlayMode_RetakePlan.md with Tools\Write-PrototypePlayModeRetakePlan.ps1."
}

$result = [ordered]@{
    retake_plan_doc_status = $status
    verifier = $scriptName
    plan_path = $PlanPath
    plan_exists = $planExists
    missing_needles = $missingNeedles.ToArray()
    expected_missing_states = $previewData.missing_states
    expected_focused_retake_states = $focusedStates
    expected_focused_retake_count = $expectedFocusedRetakeCount
    documented_focused_retake_count = $documentedFocusedRetakeCount
    manual_registration_candidate_count = $previewData.manual_registration_candidate_count
    triaged_non_state_count = $previewData.triaged_non_state_count
    suite_status = $previewData.suite_status
    screenshot_status = $previewData.screenshot_status
    review_readiness = $previewData.review_readiness
    wave_combat_action_showcase_ready = $previewData.wave_combat_action_showcase_ready
    wave_combat_action_showcase_reason = $previewData.wave_combat_action_showcase_reason
    next_action = $nextAction
}

if ($JsonOnly) {
    $result | ConvertTo-Json -Depth 6 -Compress | Write-Host
}
else {
    Write-Host ("retake_plan_doc_status=" + $status)
    Write-Host ("expected_focused_retake_count=" + $expectedFocusedRetakeCount)
    Write-Host ("documented_focused_retake_count=" + $documentedFocusedRetakeCount)
    Write-Host ("missing_needles=" + $missingNeedles.Count)
    Write-Host ("next_action=" + $nextAction)
}

if ($status -ne "ok") {
    exit 13
}

exit 0
