param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [ValidateSet("PASS", "FIX_LAYOUT", "FIX_ASSET", "FIX_FEEDBACK", "BLOCKED", "NOT_RECORDED")]
    [string]$DrawChoice = "NOT_RECORDED",
    [ValidateSet("PASS", "FIX_LAYOUT", "FIX_ASSET", "FIX_FEEDBACK", "BLOCKED", "NOT_RECORDED")]
    [string]$PendingPlacement = "NOT_RECORDED",
    [ValidateSet("PASS", "FIX_LAYOUT", "FIX_ASSET", "FIX_FEEDBACK", "BLOCKED", "NOT_RECORDED")]
    [string]$InvalidPlacement = "NOT_RECORDED",
    [ValidateSet("PASS", "FIX_LAYOUT", "FIX_ASSET", "FIX_FEEDBACK", "BLOCKED", "NOT_RECORDED")]
    [string]$WaveCombat = "NOT_RECORDED",
    [string]$TopIssue = "Manual review pending from suite result draft.",
    [string]$NextCodeTarget = "",
    [switch]$Apply,
    [switch]$RequireSuiteCaptured,
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"

$docPath = Join-Path $ProjectPath "Docs\Prototype_PlayMode_Verification.md"
$draftPath = Join-Path $ProjectPath "Docs\Prototype_PlayMode_Verification_ResultDraft.txt"
$suiteVerifier = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeSuite.ps1"
$stateFields = @("Draw Choice", "Pending Placement", "Invalid Placement", "Wave Combat")
$stateValues = [ordered]@{
    "Draw Choice" = $DrawChoice
    "Pending Placement" = $PendingPlacement
    "Invalid Placement" = $InvalidPlacement
    "Wave Combat" = $WaveCombat
}

function Invoke-SuiteVerifier {
    param(
        [string]$ScriptPath,
        [string]$RootPath
    )

    if (-not (Test-Path -LiteralPath $ScriptPath -PathType Leaf)) {
        throw "Missing suite verifier: $ScriptPath"
    }

    $output = & powershell -ExecutionPolicy Bypass -File $ScriptPath -ProjectPath $RootPath -JsonOnly
    if ($LASTEXITCODE -ne 0) {
        throw "Suite verifier failed with exit code $LASTEXITCODE."
    }

    $jsonLine = $output | Select-Object -Last 1
    if ([string]::IsNullOrWhiteSpace([string]$jsonLine)) {
        throw "Suite verifier produced no JSON output."
    }

    return ($jsonLine | ConvertFrom-Json)
}

function Get-StateResult {
    param(
        [object]$SuiteData,
        [string]$StateName
    )

    if ($null -eq $SuiteData -or $null -eq $SuiteData.state_results) {
        return $null
    }

    $property = $SuiteData.state_results.PSObject.Properties[$StateName]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Get-SuiteScreenshotPaths {
    param([object]$SuiteData)

    $paths = New-Object System.Collections.Generic.List[string]
    foreach ($state in $stateFields) {
        $stateResult = Get-StateResult -SuiteData $SuiteData -StateName $state
        if ($null -eq $stateResult) {
            continue
        }

        if ($stateResult.screenshot_exists -eq $true -and -not [string]::IsNullOrWhiteSpace([string]$stateResult.screenshot)) {
            $paths.Add([string]$stateResult.screenshot)
        }
    }

    return $paths.ToArray()
}

function Get-RecordStatusPreview {
    param([object]$Values)

    $allValues = @($stateFields | ForEach-Object { $Values[$_] })
    if (($allValues | Where-Object { $_ -eq "NOT_RECORDED" }).Count -gt 0) {
        return "not_recorded"
    }
    if ($allValues -contains "BLOCKED") {
        return "blocked"
    }
    if (($allValues | Where-Object { $_ -like "FIX_*" }).Count -gt 0) {
        return "needs_fix"
    }
    if (($allValues | Where-Object { $_ -eq "PASS" }).Count -eq $stateFields.Count) {
        return "passed"
    }

    return "not_recorded"
}

function Get-DefaultNextCodeTarget {
    param([string]$RecordStatus)

    switch ($RecordStatus) {
        "passed" { return "Continue feature work after Play Mode verification." }
        "needs_fix" { return "Use the recorded FIX_* state to target the next HUD/layout, asset, or feedback fix." }
        "blocked" { return "Resolve the recorded Play Mode blocker before UX tuning." }
        default { return "Complete manual visual review, then record PASS or FIX_*." }
    }
}

function Build-LatestResultSection {
    param(
        [object]$SuiteData,
        [string[]]$Screenshots,
        [object]$Values,
        [string]$Issue,
        [string]$CodeTarget,
        [string]$RecordStatus
    )

    $date = if ($SuiteData.date -and $SuiteData.date -ne "NOT_RECORDED") { $SuiteData.date } else { (Get-Date).ToString("yyyy-MM-dd HH:mm") + " KST" }
    $unityVersion = if ($SuiteData.unity_version -and $SuiteData.unity_version -ne "NOT_RECORDED") { $SuiteData.unity_version } else { "NOT_RECORDED" }
    $resolution = if ($SuiteData.aspect_ratio_resolution -and $SuiteData.aspect_ratio_resolution -ne "NOT_RECORDED") { $SuiteData.aspect_ratio_resolution } else { "NOT_RECORDED" }
    $target = if ([string]::IsNullOrWhiteSpace($CodeTarget)) { Get-DefaultNextCodeTarget -RecordStatus $RecordStatus } else { $CodeTarget }

    $builder = New-Object System.Text.StringBuilder
    [void]$builder.AppendLine("## Latest Manual Result")
    [void]$builder.AppendLine("Date: " + $date)
    [void]$builder.AppendLine("Unity version: " + $unityVersion)
    [void]$builder.AppendLine("Aspect ratio / resolution: " + $resolution)
    [void]$builder.AppendLine()
    foreach ($state in $stateFields) {
        [void]$builder.AppendLine($state + ": " + $Values[$state])
    }
    [void]$builder.AppendLine()
    [void]$builder.AppendLine("Screenshots captured: " + $Screenshots.Count)
    for ($i = 0; $i -lt $Screenshots.Count; $i++) {
        [void]$builder.AppendLine(($i + 1).ToString() + ". " + $Screenshots[$i])
    }
    [void]$builder.AppendLine("Top issue: " + $Issue)
    [void]$builder.AppendLine("Next code target: " + $target)
    [void]$builder.AppendLine("Verification command result: Recorded with Tools\Write-PrototypePlayModeResultFromSuite.ps1 from suite status " + $SuiteData.playmode_suite_status + "; run Tools\Verify-PrototypePlayModeRecord.ps1 -JsonOnly.")
    [void]$builder.AppendLine()
    return $builder.ToString()
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

if (-not (Test-Path -LiteralPath $docPath -PathType Leaf)) {
    throw "Missing verification doc: $docPath"
}

$suiteData = Invoke-SuiteVerifier -ScriptPath $suiteVerifier -RootPath $ProjectPath
if ($RequireSuiteCaptured -and $suiteData.playmode_suite_status -ne "captured") {
    throw "Suite evidence is not captured. Current status: $($suiteData.playmode_suite_status)"
}

$screenshots = @(Get-SuiteScreenshotPaths -SuiteData $suiteData)
$recordStatus = Get-RecordStatusPreview -Values $stateValues
$latestSection = Build-LatestResultSection `
    -SuiteData $suiteData `
    -Screenshots $screenshots `
    -Values $stateValues `
    -Issue $TopIssue `
    -CodeTarget $NextCodeTarget `
    -RecordStatus $recordStatus

$actualDraftPath = Write-TextWithFallback -PreferredPath $draftPath -Contents $latestSection

$applied = $false
if ($Apply) {
    $content = [System.IO.File]::ReadAllText($docPath)
    $updated = [regex]::Replace(
        $content,
        "(?ms)^## Latest Manual Result\s*.*?(?=^## |\z)",
        $latestSection)

    if ($updated -eq $content) {
        throw "Could not find the Latest Manual Result section in $docPath"
    }

    [System.IO.File]::WriteAllText($docPath, $updated, [System.Text.Encoding]::UTF8)
    $applied = $true
}

$result = [ordered]@{
    result_writer_status = "ok"
    applied = $applied
    draft_path = $actualDraftPath
    doc_path = $docPath
    suite_status = $suiteData.playmode_suite_status
    screenshot_count = $screenshots.Count
    record_status_preview = $recordStatus
    state_results = $stateValues
    next_action = if ($applied) { "Run Tools\Verify-PrototypePlayModeRecord.ps1 -JsonOnly." } else { "Review the draft, then rerun with -Apply when ready." }
}

if ($JsonOnly) {
    $result | ConvertTo-Json -Depth 6 -Compress | Write-Host
}
else {
    Write-Host "result_writer_status=ok"
    Write-Host ("applied=" + $applied)
    Write-Host ("draft_path=" + $actualDraftPath)
    Write-Host ("suite_status=" + $suiteData.playmode_suite_status)
    Write-Host ("screenshot_count=" + $screenshots.Count)
    Write-Host ("record_status_preview=" + $recordStatus)
    Write-Host ("next_action=" + $result.next_action)
}

exit 0
