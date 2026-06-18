param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [Parameter(Mandatory = $true)]
    [ValidateSet("Draw Choice", "Pending Placement", "Invalid Placement", "Wave Combat")]
    [string]$State,
    [Parameter(Mandatory = $true)]
    [string]$ScreenshotPath,
    [string]$UnityVersion = "",
    [string]$AspectRatioResolution = "",
    [string]$PrepareResult = "",
    [string]$HudStateSummary = "",
    [switch]$WaveCombatActionShowcase,
    [switch]$PreviewOnly,
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"

$suitePath = Join-Path $ProjectPath "Docs\Prototype_PlayMode_Verification_Suite.txt"
$toolName = "Register-PrototypePlayModeManualEvidence.ps1"
$requiredStates = @("Draw Choice", "Pending Placement", "Invalid Placement", "Wave Combat")

function Normalize-FieldValue {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "NOT_RECORDED"
    }

    return $Value.Trim()
}

function Resolve-ProjectPath {
    param(
        [string]$CandidatePath,
        [string]$RootPath
    )

    if ([System.IO.Path]::IsPathRooted($CandidatePath)) {
        return [System.IO.Path]::GetFullPath($CandidatePath)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $RootPath $CandidatePath))
}

function Get-MarkdownEvidencePath {
    param(
        [string]$TargetPath,
        [string]$RootPath
    )

    try {
        $root = [System.IO.Path]::GetFullPath($RootPath).TrimEnd("\") + "\"
        $target = [System.IO.Path]::GetFullPath($TargetPath)
        $rootUri = New-Object System.Uri $root
        $targetUri = New-Object System.Uri $target
        $relative = $rootUri.MakeRelativeUri($targetUri).ToString()
        if (-not $relative.StartsWith("..")) {
            return [System.Uri]::UnescapeDataString($relative).Replace("/", "\")
        }
    }
    catch {
        return $TargetPath
    }

    return $TargetPath
}

function Read-PngHeader {
    param([string]$Path)

    $result = [ordered]@{
        valid_png = $false
        width = $null
        height = $null
        error = $null
    }

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        $result.error = "missing_file"
        return [pscustomobject]$result
    }

    $buffer = New-Object byte[] 24
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $read = $stream.Read($buffer, 0, $buffer.Length)
    }
    finally {
        $stream.Dispose()
    }

    if ($read -lt 24) {
        $result.error = "png_header_too_short"
        return [pscustomobject]$result
    }

    $signature = @(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)
    for ($i = 0; $i -lt $signature.Count; $i++) {
        if ($buffer[$i] -ne $signature[$i]) {
            $result.error = "not_png"
            return [pscustomobject]$result
        }
    }

    $width = ([int64]$buffer[16] -shl 24) -bor ([int64]$buffer[17] -shl 16) -bor ([int64]$buffer[18] -shl 8) -bor [int64]$buffer[19]
    $height = ([int64]$buffer[20] -shl 24) -bor ([int64]$buffer[21] -shl 16) -bor ([int64]$buffer[22] -shl 8) -bor [int64]$buffer[23]

    if ($width -le 0 -or $height -le 0) {
        $result.error = "invalid_dimensions"
        return [pscustomobject]$result
    }

    $result.valid_png = $true
    $result.width = $width
    $result.height = $height
    return [pscustomobject]$result
}

function Get-ResolutionLabel {
    param([object]$Header)

    if ($null -eq $Header -or $Header.valid_png -ne $true) {
        return "manual evidence; resolution unavailable"
    }

    $a = [int]$Header.width
    $b = [int]$Header.height
    while ($b -ne 0) {
        $remainder = $a % $b
        $a = $b
        $b = $remainder
    }

    $divisor = [Math]::Max(1, $a)
    return [string]$Header.width + "x" + [string]$Header.height + " (" + ([int]$Header.width / $divisor) + ":" + ([int]$Header.height / $divisor) + ")"
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
    foreach ($stateName in $States) {
        $stateSet[$stateName] = $true
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

function Get-StateValue {
    param(
        [hashtable]$Details,
        [string]$Key,
        [string]$Fallback
    )

    if ($null -ne $Details -and $Details.ContainsKey($Key)) {
        return Normalize-FieldValue $Details[$Key]
    }

    return $Fallback
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
        return [pscustomobject]@{
            path = $PreferredPath
            write_status = "ok"
            error = $null
        }
    }
    catch {
        $fallbackRoot = Join-Path ([System.IO.Path]::GetTempPath()) "zombieFoodcenter-verification"
        [System.IO.Directory]::CreateDirectory($fallbackRoot) | Out-Null
        $fallbackPath = Join-Path $fallbackRoot (Split-Path -Leaf $PreferredPath)
        [System.IO.File]::WriteAllText($fallbackPath, $Contents, [System.Text.Encoding]::UTF8)
        return [pscustomobject]@{
            path = $fallbackPath
            write_status = "fallback"
            error = $_.Exception.Message
        }
    }
}

function Test-StateHasScreenshot {
    param(
        [hashtable]$Details,
        [string]$RootPath
    )

    if ($null -eq $Details) {
        return $false
    }

    $prepared = Get-StateValue -Details $Details -Key "Prepared" -Fallback "NOT_RECORDED"
    $screenshot = Get-StateValue -Details $Details -Key "Screenshot" -Fallback "not captured"
    if ($prepared.ToLowerInvariant() -ne "yes" -or $screenshot -eq "not captured" -or $screenshot -eq "NOT_RECORDED") {
        return $false
    }

    $resolved = Resolve-ProjectPath -CandidatePath $screenshot -RootPath $RootPath
    return (Test-Path -LiteralPath $resolved -PathType Leaf)
}

function Build-ManualSuiteText {
    param(
        [hashtable]$TopFields,
        [hashtable]$StateDetails,
        [string[]]$States,
        [string]$RootPath,
        [object]$Header
    )

    $allCaptured = $true
    foreach ($stateName in $States) {
        if (-not $StateDetails.ContainsKey($stateName) -or -not (Test-StateHasScreenshot -Details $StateDetails[$stateName] -RootPath $RootPath)) {
            $allCaptured = $false
        }
    }

    $now = (Get-Date).ToString("yyyy-MM-dd HH:mm") + " KST"
    $dateValue = if ($TopFields.ContainsKey("Date") -and $TopFields["Date"] -ne "NOT_RECORDED") { $TopFields["Date"] } else { $now }
    $unityValue = if (-not [string]::IsNullOrWhiteSpace($UnityVersion)) {
        $UnityVersion
    }
    elseif ($TopFields.ContainsKey("Unity version") -and $TopFields["Unity version"] -ne "NOT_RECORDED") {
        $TopFields["Unity version"]
    }
    else {
        "manual evidence; Unity version unavailable"
    }

    $resolutionValue = if (-not [string]::IsNullOrWhiteSpace($AspectRatioResolution)) {
        $AspectRatioResolution
    }
    elseif ($TopFields.ContainsKey("Aspect ratio / resolution") -and $TopFields["Aspect ratio / resolution"] -ne "NOT_RECORDED") {
        $TopFields["Aspect ratio / resolution"]
    }
    else {
        Get-ResolutionLabel -Header $Header
    }

    $suiteStatus = if ($allCaptured) { "manual_completed" } else { "manual_partial" }
    $builder = New-Object System.Text.StringBuilder
    [void]$builder.AppendLine("FoodTruck Prototype Play Mode Verification Suite")
    [void]$builder.AppendLine("Date: " + $dateValue)
    [void]$builder.AppendLine("Completed: " + $now)
    [void]$builder.AppendLine("Unity version: " + $unityValue)
    [void]$builder.AppendLine("Aspect ratio / resolution: " + $resolutionValue)
    [void]$builder.AppendLine("Suite status: " + $suiteStatus)
    [void]$builder.AppendLine("Evidence source: manual screenshot registration")
    [void]$builder.AppendLine()
    [void]$builder.AppendLine("Captured states:")

    $index = 1
    foreach ($stateName in $States) {
        $details = if ($StateDetails.ContainsKey($stateName)) { $StateDetails[$stateName] } else { @{} }
        $prepared = Get-StateValue -Details $details -Key "Prepared" -Fallback "NOT_RECORDED"
        $screenshot = Get-StateValue -Details $details -Key "Screenshot" -Fallback "not captured"
        $stateStatus = if ((Test-StateHasScreenshot -Details $details -RootPath $RootPath)) { "manual_registered" } else { "not_recorded" }
        [void]$builder.AppendLine([string]$index + ". " + $stateName + ": " + $stateStatus + " | " + $screenshot)
        $index++
    }

    [void]$builder.AppendLine()
    [void]$builder.AppendLine("State details:")
    foreach ($stateName in $States) {
        $details = if ($StateDetails.ContainsKey($stateName)) { $StateDetails[$stateName] } else { @{} }
        [void]$builder.AppendLine("- " + $stateName)
        [void]$builder.AppendLine("  Prepared: " + (Get-StateValue -Details $details -Key "Prepared" -Fallback "NOT_RECORDED"))
        [void]$builder.AppendLine("  Prepare result: " + (Get-StateValue -Details $details -Key "Prepare result" -Fallback "NOT_RECORDED"))
        [void]$builder.AppendLine("  HUD state summary: " + (Get-StateValue -Details $details -Key "HUD state summary" -Fallback "NOT_RECORDED"))
        [void]$builder.AppendLine("  Evidence source: " + (Get-StateValue -Details $details -Key "Evidence source" -Fallback "NOT_RECORDED"))
        [void]$builder.AppendLine("  Screenshot: " + (Get-StateValue -Details $details -Key "Screenshot" -Fallback "not captured"))
    }

    [void]$builder.AppendLine()
    [void]$builder.AppendLine("Manual result template:")
    [void]$builder.AppendLine("Draw Choice: PASS / FIX_LAYOUT / FIX_ASSET / BLOCKED")
    [void]$builder.AppendLine("Pending Placement: PASS / FIX_LAYOUT / FIX_ASSET / BLOCKED")
    [void]$builder.AppendLine("Invalid Placement: PASS / FIX_FEEDBACK / FIX_LAYOUT / BLOCKED")
    [void]$builder.AppendLine("Wave Combat: PASS / FIX_LAYOUT / FIX_ASSET / BLOCKED")
    [void]$builder.AppendLine()
    [void]$builder.AppendLine("After recording, run:")
    [void]$builder.AppendLine("powershell -ExecutionPolicy Bypass -File ""Tools\Verify-PrototypePlayModeRecord.ps1"" -ProjectPath ""D:\uni\zombieFoodcenter"" -JsonOnly")
    return [pscustomobject]@{
        text = $builder.ToString()
        suite_status = $suiteStatus
    }
}

$resolvedScreenshot = Resolve-ProjectPath -CandidatePath $ScreenshotPath -RootPath $ProjectPath
if (-not (Test-Path -LiteralPath $resolvedScreenshot -PathType Leaf)) {
    [ordered]@{
        manual_evidence_status = "missing_screenshot"
        state = $State
        screenshot = $resolvedScreenshot
        suite_path = $suitePath
    } | ConvertTo-Json -Depth 4 -Compress | Write-Host
    exit 12
}

$pngHeader = Read-PngHeader -Path $resolvedScreenshot
if ($pngHeader.valid_png -ne $true) {
    [ordered]@{
        manual_evidence_status = "invalid_screenshot"
        state = $State
        screenshot = $resolvedScreenshot
        error = $pngHeader.error
        suite_path = $suitePath
    } | ConvertTo-Json -Depth 4 -Compress | Write-Host
    exit 13
}

$content = if (Test-Path -LiteralPath $suitePath -PathType Leaf) {
    [System.IO.File]::ReadAllText($suitePath)
}
else {
    ""
}

$topFields = Get-TopLevelFieldMap $content
$stateDetails = Get-StateDetails -Text $content -States $requiredStates
foreach ($stateName in $requiredStates) {
    if (-not $stateDetails.ContainsKey($stateName)) {
        $stateDetails[$stateName] = @{}
    }
}

$evidencePath = Get-MarkdownEvidencePath -TargetPath $resolvedScreenshot -RootPath $ProjectPath
$resolvedPrepareResult = if (-not [string]::IsNullOrWhiteSpace($PrepareResult)) {
    $PrepareResult
}
elseif ($State -eq "Wave Combat" -and $WaveCombatActionShowcase) {
    "Manual screenshot registered; reviewer marked attack trails and attack labels visible."
}
else {
    "Manual screenshot registered from existing Play Mode evidence."
}

$resolvedHudSummary = if (-not [string]::IsNullOrWhiteSpace($HudStateSummary)) {
    $HudStateSummary
}
else {
    "Manual evidence registered; visual review required."
}

$stateDetails[$State]["Prepared"] = "yes"
$stateDetails[$State]["Prepare result"] = $resolvedPrepareResult
$stateDetails[$State]["HUD state summary"] = $resolvedHudSummary
$stateDetails[$State]["Evidence source"] = "manual_registration"
$stateDetails[$State]["Screenshot"] = $evidencePath

$suite = Build-ManualSuiteText -TopFields $topFields -StateDetails $stateDetails -States $requiredStates -RootPath $ProjectPath -Header $pngHeader
$actualOutputPath = "PREVIEW_ONLY"
$writeStatus = "preview"
$writeError = $null
if (-not $PreviewOnly) {
    $writeResult = Write-TextWithFallback -PreferredPath $suitePath -Contents $suite.text
    $actualOutputPath = $writeResult.path
    $writeStatus = $writeResult.write_status
    $writeError = $writeResult.error
}

$registeredCount = 0
foreach ($stateName in $requiredStates) {
    if (Test-StateHasScreenshot -Details $stateDetails[$stateName] -RootPath $ProjectPath) {
        $registeredCount++
    }
}

$result = [ordered]@{
    manual_evidence_status = "ok"
    source_tool = $toolName
    state = $State
    screenshot = $resolvedScreenshot
    registered_path = $evidencePath
    suite_path = $actualOutputPath
    write_status = $writeStatus
    write_error = $writeError
    suite_status = $suite.suite_status
    registered_count = $registeredCount
    expected_state_count = $requiredStates.Count
    valid_png = $pngHeader.valid_png
    width = $pngHeader.width
    height = $pngHeader.height
    wave_combat_action_showcase_marked = [bool]$WaveCombatActionShowcase
    next_action = if ($registeredCount -eq $requiredStates.Count) { "Run Tools\Verify-PrototypePlayModeSuite.ps1, Tools\Verify-PrototypePlayModeScreenshots.ps1, then generate the review pack." } else { "Register the remaining states or run Capture Verification Suite before recording a PASS/FIX/BLOCKED result." }
}

if ($JsonOnly) {
    $result | ConvertTo-Json -Depth 5 -Compress | Write-Host
}
else {
    Write-Host ("manual_evidence_status=" + $result.manual_evidence_status)
    Write-Host ("state=" + $result.state)
    Write-Host ("suite_status=" + $result.suite_status)
    Write-Host ("registered_count=" + $result.registered_count + "/" + $result.expected_state_count)
    Write-Host ("suite_path=" + $result.suite_path)
    Write-Host ("next_action=" + $result.next_action)
}

exit 0
