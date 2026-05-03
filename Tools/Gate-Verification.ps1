param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [int]$StaleAfterMinutes = 180,
    [switch]$RunTests,
    [switch]$Strict,
    [switch]$ForceHeadless,
    [switch]$RequireFresh,
    [switch]$RequireCompileOk,
    [switch]$RequireTestsOk,
    [int]$MaxAgeMinutes = 180,
    [switch]$Compact,
    [switch]$Json,
    [switch]$JsonOnly,
    [switch]$SkipAssets,
    [switch]$SkipLayout
)

$ErrorActionPreference = "Stop"

if ($JsonOnly) {
    $Json = $true
    $Compact = $true
}

$ensureScript = Join-Path $ProjectPath "Tools\Ensure-VerificationFresh.ps1"
$assertScript = Join-Path $ProjectPath "Tools\Assert-VerificationStatus.ps1"
$showScript = Join-Path $ProjectPath "Tools\Show-VerificationStatus.ps1"
$assetScript = Join-Path $ProjectPath "Tools\Verify-PrototypeAssets.ps1"
$layoutScript = Join-Path $ProjectPath "Tools\Verify-PrototypeLayout.ps1"

if (-not (Test-Path $ensureScript)) {
    Write-Error "Missing script: $ensureScript"
    exit 90
}

if (-not (Test-Path $assertScript)) {
    Write-Error "Missing script: $assertScript"
    exit 90
}

if ($Json -and -not (Test-Path $showScript)) {
    Write-Error "Missing script: $showScript"
    exit 90
}

function Get-StatusObject {
    param(
        [string]$ShowScriptPath,
        [string]$RootPath,
        [int]$MaxAge
    )

    if (-not (Test-Path $ShowScriptPath)) {
        return $null
    }

    $statusJsonRaw = & powershell -ExecutionPolicy Bypass -File $ShowScriptPath -ProjectPath $RootPath -Json -StaleAfterMinutes $MaxAge
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    $jsonLine = $statusJsonRaw | Select-Object -Last 1
    if ([string]::IsNullOrWhiteSpace([string]$jsonLine)) {
        return $null
    }

    try {
        return ($jsonLine | ConvertFrom-Json)
    }
    catch {
        return $null
    }
}

function Invoke-AssetCheck {
    param(
        [string]$AssetScriptPath,
        [string]$RootPath
    )

    if (-not (Test-Path $AssetScriptPath)) {
        return [pscustomobject]@{
            exit_code = 90
            assets = [pscustomobject]@{
                asset_status = "missing_asset_verifier"
                runtime_required_missing = $null
                final_art_missing = $null
            }
        }
    }

    $assetJsonRaw = & powershell -ExecutionPolicy Bypass -File $AssetScriptPath -ProjectPath $RootPath -JsonOnly
    $exitCode = $LASTEXITCODE
    $jsonLine = $assetJsonRaw | Select-Object -Last 1
    $assetObj = $null

    if (-not [string]::IsNullOrWhiteSpace([string]$jsonLine)) {
        try {
            $assetObj = $jsonLine | ConvertFrom-Json
        }
        catch {
            $assetObj = $null
        }
    }

    if ($null -eq $assetObj) {
        $assetObj = [pscustomobject]@{
            asset_status = "failed_asset_parse"
            runtime_required_missing = $null
            final_art_missing = $null
        }
    }

    return [pscustomobject]@{
        exit_code = $exitCode
        assets = $assetObj
    }
}

function Invoke-LayoutCheck {
    param(
        [string]$LayoutScriptPath,
        [string]$RootPath
    )

    if (-not (Test-Path $LayoutScriptPath)) {
        return [pscustomobject]@{
            exit_code = 90
            layout = [pscustomobject]@{
                layout_status = "missing_layout_verifier"
                failed_checks = $null
            }
        }
    }

    $layoutJsonRaw = & powershell -ExecutionPolicy Bypass -File $LayoutScriptPath -ProjectPath $RootPath -JsonOnly
    $exitCode = $LASTEXITCODE
    $jsonLine = $layoutJsonRaw | Select-Object -Last 1
    $layoutObj = $null

    if (-not [string]::IsNullOrWhiteSpace([string]$jsonLine)) {
        try {
            $layoutObj = $jsonLine | ConvertFrom-Json
        }
        catch {
            $layoutObj = $null
        }
    }

    if ($null -eq $layoutObj) {
        $layoutObj = [pscustomobject]@{
            layout_status = "failed_layout_parse"
            failed_checks = $null
        }
    }

    return [pscustomobject]@{
        exit_code = $exitCode
        layout = $layoutObj
    }
}

$ensureArgs = @(
    "-ExecutionPolicy", "Bypass",
    "-File", $ensureScript,
    "-ProjectPath", $ProjectPath,
    "-StaleAfterMinutes", $StaleAfterMinutes
)

if ($RunTests) { $ensureArgs += "-RunTests" }
if ($Strict) { $ensureArgs += "-Strict" }
if ($ForceHeadless) { $ensureArgs += "-ForceHeadless" }
if ($Compact) { $ensureArgs += "-Compact" }

$ensureOut = & powershell @ensureArgs
if (-not $Compact) {
    $ensureOut | Out-Host
}

if ($LASTEXITCODE -ne 0) {
    if ($JsonOnly) {
        $statusObj = Get-StatusObject -ShowScriptPath $showScript -RootPath $ProjectPath -MaxAge $MaxAgeMinutes
        [ordered]@{
            gate_status = "failed_refresh"
            status = $statusObj
        } | ConvertTo-Json -Depth 8 -Compress | Write-Host
        exit 2
    }

    Write-Host "gate_status=failed_refresh"
    exit 2
}

$assertArgs = @(
    "-ExecutionPolicy", "Bypass",
    "-File", $assertScript,
    "-ProjectPath", $ProjectPath,
    "-MaxAgeMinutes", $MaxAgeMinutes
)

if ($RequireFresh) { $assertArgs += "-RequireFresh" }
if ($RequireCompileOk) { $assertArgs += "-RequireCompileOk" }
if ($RequireTestsOk) { $assertArgs += "-RequireTestsOk" }

$assertOut = & powershell @assertArgs
if (-not $Compact) {
    $assertOut | Out-Host
}

if ($LASTEXITCODE -ne 0) {
    if ($JsonOnly) {
        $statusObj = Get-StatusObject -ShowScriptPath $showScript -RootPath $ProjectPath -MaxAge $MaxAgeMinutes
        $gateStatus = "failed_assert"
        if ($null -ne $statusObj -and $statusObj.verification_blocked_env -eq $true) {
            $gateStatus = "failed_blocked_env"
        }

        [ordered]@{
            gate_status = $gateStatus
            status = $statusObj
        } | ConvertTo-Json -Depth 8 -Compress | Write-Host
        exit 1
    }

    Write-Host "gate_status=failed_assert"
    exit 1
}

$assetObj = $null
if (-not $SkipAssets) {
    $assetResult = Invoke-AssetCheck -AssetScriptPath $assetScript -RootPath $ProjectPath
    $assetObj = $assetResult.assets

    if (-not $Compact -and $null -ne $assetObj) {
        Write-Host ("asset_status=" + $assetObj.asset_status)
        Write-Host ("runtime_required_missing=" + $assetObj.runtime_required_missing)
        Write-Host ("final_art_missing=" + $assetObj.final_art_missing)
    }

    if ($assetResult.exit_code -ne 0) {
        if ($JsonOnly) {
            $statusObj = Get-StatusObject -ShowScriptPath $showScript -RootPath $ProjectPath -MaxAge $MaxAgeMinutes
            [ordered]@{
                gate_status = "failed_assets"
                status = $statusObj
                assets = $assetObj
            } | ConvertTo-Json -Depth 8 -Compress | Write-Host
            exit 4
        }

        Write-Host "gate_status=failed_assets"
        exit 4
    }
}

$layoutObj = $null
if (-not $SkipLayout) {
    $layoutResult = Invoke-LayoutCheck -LayoutScriptPath $layoutScript -RootPath $ProjectPath
    $layoutObj = $layoutResult.layout

    if (-not $Compact -and $null -ne $layoutObj) {
        Write-Host ("layout_status=" + $layoutObj.layout_status)
        Write-Host ("layout_failed_checks=" + $layoutObj.failed_checks)
    }

    if ($layoutResult.exit_code -ne 0) {
        if ($JsonOnly) {
            $statusObj = Get-StatusObject -ShowScriptPath $showScript -RootPath $ProjectPath -MaxAge $MaxAgeMinutes
            [ordered]@{
                gate_status = "failed_layout"
                status = $statusObj
                assets = $assetObj
                layout = $layoutObj
            } | ConvertTo-Json -Depth 8 -Compress | Write-Host
            exit 5
        }

        Write-Host "gate_status=failed_layout"
        exit 5
    }
}

$statusObj = $null
if ($Json) {
    $statusObj = Get-StatusObject -ShowScriptPath $showScript -RootPath $ProjectPath -MaxAge $MaxAgeMinutes
    if ($null -eq $statusObj) {
        if ($JsonOnly) {
        @{ gate_status = "failed_show" } | ConvertTo-Json -Depth 8 -Compress | Write-Host
            exit 3
        }

        Write-Host "gate_status=failed_show"
        exit 3
    }

    if (-not $JsonOnly) {
        $statusObj | ConvertTo-Json -Depth 8 -Compress | Write-Host
    }
}

if ($JsonOnly) {
    [ordered]@{
        gate_status = "ok"
        status = $statusObj
        assets = $assetObj
        layout = $layoutObj
    } | ConvertTo-Json -Depth 8 -Compress | Write-Host
    exit 0
}

Write-Host "gate_status=ok"
exit 0
