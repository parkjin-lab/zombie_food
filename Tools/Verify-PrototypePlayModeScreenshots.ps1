param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [int]$MinWidth = 720,
    [int]$MinHeight = 1280,
    [int]$MinBytes = 10000,
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"

$screenshotDirectory = Join-Path $ProjectPath "Docs\PlayModeScreenshots"
$suiteScript = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeSuite.ps1"
$requiredStates = @("Draw Choice", "Pending Placement", "Invalid Placement", "Wave Combat")

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

function Get-StateFromFileName {
    param([string]$Path)

    $name = [System.IO.Path]::GetFileNameWithoutExtension($Path).ToLowerInvariant()
    if ($name.Contains("draw-choice")) { return "Draw Choice" }
    if ($name.Contains("pending-placement")) { return "Pending Placement" }
    if ($name.Contains("invalid-placement")) { return "Invalid Placement" }
    if ($name.Contains("wave-combat")) { return "Wave Combat" }
    if ($name.Contains("pass-confirmation")) { return "PASS Confirmation" }
    return "Unlabeled"
}

function Get-SuiteEvidenceMap {
    param(
        [string]$ScriptPath,
        [string]$RootPath
    )

    $map = @{}
    if (-not (Test-Path -LiteralPath $ScriptPath -PathType Leaf)) {
        return $map
    }

    $output = & powershell -ExecutionPolicy Bypass -File $ScriptPath -ProjectPath $RootPath -JsonOnly
    $jsonLine = $output | Select-Object -Last 1
    if ([string]::IsNullOrWhiteSpace([string]$jsonLine)) {
        return $map
    }

    try {
        $suiteData = $jsonLine | ConvertFrom-Json
    }
    catch {
        return $map
    }

    if ($null -eq $suiteData.state_results) {
        return $map
    }

    foreach ($property in $suiteData.state_results.PSObject.Properties) {
        $state = $property.Name
        $stateResult = $property.Value
        if ($null -eq $stateResult -or $stateResult.screenshot_exists -ne $true) {
            continue
        }

        $resolved = Resolve-EvidencePath -EvidencePath ([string]$stateResult.screenshot) -RootPath $RootPath
        if ($resolved -eq "NOT_RECORDED") {
            continue
        }

        $map[$resolved.ToLowerInvariant()] = $state
    }

    return $map
}

$suiteEvidenceMap = Get-SuiteEvidenceMap -ScriptPath $suiteScript -RootPath $ProjectPath
$candidatePaths = New-Object System.Collections.Generic.List[string]
$seenPaths = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)

foreach ($suitePath in $suiteEvidenceMap.Keys) {
    if ($seenPaths.Add($suitePath)) {
        $candidatePaths.Add($suitePath)
    }
}

if (Test-Path -LiteralPath $screenshotDirectory -PathType Container) {
    Get-ChildItem -LiteralPath $screenshotDirectory -Filter "*.png" -File | Sort-Object Name | ForEach-Object {
        $fullName = [System.IO.Path]::GetFullPath($_.FullName)
        if ($seenPaths.Add($fullName)) {
            $candidatePaths.Add($fullName)
        }
    }
}

$screenshots = New-Object System.Collections.Generic.List[object]
$invalidScreenshots = New-Object System.Collections.Generic.List[string]
$lowResolutionScreenshots = New-Object System.Collections.Generic.List[string]
$smallFileScreenshots = New-Object System.Collections.Generic.List[string]
$coveredStates = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
$unlabeledCount = 0

foreach ($path in $candidatePaths) {
    $fullPath = [System.IO.Path]::GetFullPath($path)
    $pathKey = $fullPath.ToLowerInvariant()
    $state = if ($suiteEvidenceMap.ContainsKey($pathKey)) { $suiteEvidenceMap[$pathKey] } else { Get-StateFromFileName -Path $fullPath }
    $fileInfo = if (Test-Path -LiteralPath $fullPath -PathType Leaf) { Get-Item -LiteralPath $fullPath } else { $null }
    $header = Read-PngHeader -Path $fullPath
    $sizeBytes = if ($null -ne $fileInfo) { [int64]$fileInfo.Length } else { 0 }
    $isPortrait = $header.valid_png -eq $true -and $header.height -ge $header.width
    $meetsSize = $sizeBytes -ge $MinBytes
    $meetsResolution = $header.valid_png -eq $true -and $header.width -ge $MinWidth -and $header.height -ge $MinHeight
    $machineQuality = $header.valid_png -eq $true -and $meetsSize -and $meetsResolution

    if ($state -eq "Unlabeled") {
        $unlabeledCount++
    }
    elseif ($requiredStates -contains $state) {
        [void]$coveredStates.Add($state)
    }

    if ($header.valid_png -ne $true) {
        $invalidScreenshots.Add($fullPath + "=" + $header.error)
    }
    elseif (-not $meetsResolution) {
        $lowResolutionScreenshots.Add($fullPath + "=" + $header.width + "x" + $header.height)
    }

    if (-not $meetsSize) {
        $smallFileScreenshots.Add($fullPath + "=" + $sizeBytes + " bytes")
    }

    $screenshots.Add([ordered]@{
        path = $fullPath
        state = $state
        from_suite_manifest = $suiteEvidenceMap.ContainsKey($pathKey)
        valid_png = $header.valid_png
        width = $header.width
        height = $header.height
        portrait_or_square = $isPortrait
        size_bytes = $sizeBytes
        meets_min_resolution = $meetsResolution
        meets_min_bytes = $meetsSize
        machine_quality_pass = $machineQuality
        error = $header.error
    }) | Out-Null
}

$missingStates = @($requiredStates | Where-Object { -not $coveredStates.Contains($_) })
$status = "not_recorded"
$nextAction = "Capture Play Mode evidence with Tools > Food Truck Prototype > Capture Verification Suite."

if ($screenshots.Count -gt 0) {
    if ($invalidScreenshots.Count -gt 0 -or $lowResolutionScreenshots.Count -gt 0 -or $smallFileScreenshots.Count -gt 0) {
        $status = "invalid_screenshots"
        $nextAction = "Retake or remove invalid/low-resolution Play Mode screenshots before recording the manual result."
    }
    elseif ($missingStates.Count -eq 0) {
        $status = "suite_ready"
        $nextAction = "Review the screenshots visually, then record PASS or FIX/BLOCKED."
    }
    else {
        $status = "partial"
        $nextAction = "Use Capture Verification Suite or focused retakes so each required state has a labeled screenshot."
    }
}

$result = [ordered]@{
    playmode_screenshot_status = $status
    screenshot_directory = $screenshotDirectory
    screenshot_count = $screenshots.Count
    valid_png_count = @($screenshots | Where-Object { $_.valid_png -eq $true }).Count
    machine_quality_pass_count = @($screenshots | Where-Object { $_.machine_quality_pass -eq $true }).Count
    invalid_count = $invalidScreenshots.Count
    low_resolution_count = $lowResolutionScreenshots.Count
    small_file_count = $smallFileScreenshots.Count
    unlabeled_count = $unlabeledCount
    covered_state_count = $coveredStates.Count
    expected_state_count = $requiredStates.Count
    missing_states = $missingStates
    invalid_screenshots = $invalidScreenshots.ToArray()
    low_resolution_screenshots = $lowResolutionScreenshots.ToArray()
    small_file_screenshots = $smallFileScreenshots.ToArray()
    screenshots = $screenshots.ToArray()
    visual_review_required = $true
    next_action = $nextAction
}

if ($JsonOnly) {
    $result | ConvertTo-Json -Depth 8 -Compress | Write-Host
}
else {
    Write-Host ("playmode_screenshot_status=" + $status)
    Write-Host ("screenshot_count=" + $screenshots.Count)
    Write-Host ("machine_quality_pass_count=" + $result.machine_quality_pass_count + "/" + $screenshots.Count)
    Write-Host ("covered_state_count=" + $coveredStates.Count + "/" + $requiredStates.Count)
    Write-Host ("unlabeled_count=" + $unlabeledCount)
    if ($missingStates.Count -gt 0) {
        Write-Host ("missing_states=" + ($missingStates -join ", "))
    }
    Write-Host "visual_review_required=true"
    Write-Host ("next_action=" + $nextAction)
}

if ($status -eq "invalid_screenshots") {
    exit 11
}

exit 0
