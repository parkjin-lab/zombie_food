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
    [switch]$SkipLayout,
    [switch]$SkipHudContract,
    [switch]$SkipPlayModeSuite,
    [switch]$SkipPlayModeScreenshots,
    [switch]$SkipPlayModeRetakePlan
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
$hudContractScript = Join-Path $ProjectPath "Tools\Verify-PrototypeHudStateContract.ps1"
$playModeSuiteScript = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeSuite.ps1"
$playModeScreenshotsScript = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeScreenshots.ps1"
$playModeRetakePlanScript = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeRetakePlan.ps1"

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

function Invoke-HudContractCheck {
    param(
        [string]$HudContractScriptPath,
        [string]$RootPath
    )

    if (-not (Test-Path $HudContractScriptPath)) {
        return [pscustomobject]@{
            exit_code = 90
            hud_contract = [pscustomobject]@{
                hud_contract_status = "missing_hud_contract_verifier"
                failed_checks = $null
            }
        }
    }

    $hudContractJsonRaw = & powershell -ExecutionPolicy Bypass -File $HudContractScriptPath -ProjectPath $RootPath -JsonOnly
    $exitCode = $LASTEXITCODE
    $jsonLine = $hudContractJsonRaw | Select-Object -Last 1
    $hudContractObj = $null

    if (-not [string]::IsNullOrWhiteSpace([string]$jsonLine)) {
        try {
            $hudContractObj = $jsonLine | ConvertFrom-Json
        }
        catch {
            $hudContractObj = $null
        }
    }

    if ($null -eq $hudContractObj) {
        $hudContractObj = [pscustomobject]@{
            hud_contract_status = "failed_hud_contract_parse"
            failed_checks = $null
        }
    }

    return [pscustomobject]@{
        exit_code = $exitCode
        hud_contract = $hudContractObj
    }
}

function Invoke-PlayModeSuiteCheck {
    param(
        [string]$PlayModeSuiteScriptPath,
        [string]$RootPath
    )

    if (-not (Test-Path $PlayModeSuiteScriptPath)) {
        return [pscustomobject]@{
            exit_code = 90
            playmode_suite = [pscustomobject]@{
                playmode_suite_status = "missing_playmode_suite_verifier"
                captured_count = $null
                expected_state_count = $null
            }
        }
    }

    $suiteJsonRaw = & powershell -ExecutionPolicy Bypass -File $PlayModeSuiteScriptPath -ProjectPath $RootPath -JsonOnly
    $exitCode = $LASTEXITCODE
    $jsonLine = $suiteJsonRaw | Select-Object -Last 1
    $suiteObj = $null

    if (-not [string]::IsNullOrWhiteSpace([string]$jsonLine)) {
        try {
            $suiteObj = $jsonLine | ConvertFrom-Json
        }
        catch {
            $suiteObj = $null
        }
    }

    if ($null -eq $suiteObj) {
        $suiteObj = [pscustomobject]@{
            playmode_suite_status = "failed_playmode_suite_parse"
            captured_count = $null
            expected_state_count = $null
        }
    }

    return [pscustomobject]@{
        exit_code = $exitCode
        playmode_suite = $suiteObj
    }
}

function Invoke-PlayModeScreenshotCheck {
    param(
        [string]$PlayModeScreenshotsScriptPath,
        [string]$RootPath
    )

    if (-not (Test-Path $PlayModeScreenshotsScriptPath)) {
        return [pscustomobject]@{
            exit_code = 90
            playmode_screenshots = [pscustomobject]@{
                playmode_screenshot_status = "missing_playmode_screenshot_verifier"
                screenshot_count = $null
                invalid_count = $null
            }
        }
    }

    $screenshotJsonRaw = & powershell -ExecutionPolicy Bypass -File $PlayModeScreenshotsScriptPath -ProjectPath $RootPath -JsonOnly
    $exitCode = $LASTEXITCODE
    $jsonLine = $screenshotJsonRaw | Select-Object -Last 1
    $screenshotObj = $null

    if (-not [string]::IsNullOrWhiteSpace([string]$jsonLine)) {
        try {
            $screenshotObj = $jsonLine | ConvertFrom-Json
        }
        catch {
            $screenshotObj = $null
        }
    }

    if ($null -eq $screenshotObj) {
        $screenshotObj = [pscustomobject]@{
            playmode_screenshot_status = "failed_playmode_screenshot_parse"
            screenshot_count = $null
            invalid_count = $null
        }
    }

    return [pscustomobject]@{
        exit_code = $exitCode
        playmode_screenshots = $screenshotObj
    }
}

function Invoke-PlayModeRetakePlanCheck {
    param(
        [string]$PlayModeRetakePlanScriptPath,
        [string]$RootPath
    )

    if (-not (Test-Path $PlayModeRetakePlanScriptPath)) {
        return [pscustomobject]@{
            exit_code = 90
            playmode_retake_plan = [pscustomobject]@{
                retake_plan_doc_status = "missing_playmode_retake_plan_verifier"
                expected_focused_retake_count = $null
                documented_focused_retake_count = $null
            }
        }
    }

    $retakePlanJsonRaw = & powershell -ExecutionPolicy Bypass -File $PlayModeRetakePlanScriptPath -ProjectPath $RootPath -JsonOnly
    $exitCode = $LASTEXITCODE
    $jsonLine = $retakePlanJsonRaw | Select-Object -Last 1
    $retakePlanObj = $null

    if (-not [string]::IsNullOrWhiteSpace([string]$jsonLine)) {
        try {
            $retakePlanObj = $jsonLine | ConvertFrom-Json
        }
        catch {
            $retakePlanObj = $null
        }
    }

    if ($null -eq $retakePlanObj) {
        $retakePlanObj = [pscustomobject]@{
            retake_plan_doc_status = "failed_playmode_retake_plan_parse"
            expected_focused_retake_count = $null
            documented_focused_retake_count = $null
        }
    }

    return [pscustomobject]@{
        exit_code = $exitCode
        playmode_retake_plan = $retakePlanObj
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

$hudContractObj = $null
if (-not $SkipHudContract) {
    $hudContractResult = Invoke-HudContractCheck -HudContractScriptPath $hudContractScript -RootPath $ProjectPath
    $hudContractObj = $hudContractResult.hud_contract

    if (-not $Compact -and $null -ne $hudContractObj) {
        Write-Host ("hud_contract_status=" + $hudContractObj.hud_contract_status)
        Write-Host ("hud_contract_failed_checks=" + $hudContractObj.failed_checks)
    }

    if ($hudContractResult.exit_code -ne 0) {
        if ($JsonOnly) {
            $statusObj = Get-StatusObject -ShowScriptPath $showScript -RootPath $ProjectPath -MaxAge $MaxAgeMinutes
            [ordered]@{
                gate_status = "failed_hud_contract"
                status = $statusObj
                assets = $assetObj
                layout = $layoutObj
                hud_contract = $hudContractObj
            } | ConvertTo-Json -Depth 8 -Compress | Write-Host
            exit 6
        }

        Write-Host "gate_status=failed_hud_contract"
        exit 6
    }
}

$playModeSuiteObj = $null
if (-not $SkipPlayModeSuite) {
    $playModeSuiteResult = Invoke-PlayModeSuiteCheck -PlayModeSuiteScriptPath $playModeSuiteScript -RootPath $ProjectPath
    $playModeSuiteObj = $playModeSuiteResult.playmode_suite

    if (-not $Compact -and $null -ne $playModeSuiteObj) {
        Write-Host ("playmode_suite_status=" + $playModeSuiteObj.playmode_suite_status)
        Write-Host ("playmode_suite_captured_count=" + $playModeSuiteObj.captured_count + "/" + $playModeSuiteObj.expected_state_count)
        Write-Host ("wave_combat_action_showcase_ready=" + [string]$playModeSuiteObj.wave_combat_action_showcase_ready)
        Write-Host ("wave_combat_action_showcase_reason=" + $playModeSuiteObj.wave_combat_action_showcase_reason)
    }

    if ($playModeSuiteResult.exit_code -ne 0) {
        if ($JsonOnly) {
            $statusObj = Get-StatusObject -ShowScriptPath $showScript -RootPath $ProjectPath -MaxAge $MaxAgeMinutes
            [ordered]@{
                gate_status = "failed_playmode_suite"
                status = $statusObj
                assets = $assetObj
                layout = $layoutObj
                hud_contract = $hudContractObj
                playmode_suite = $playModeSuiteObj
            } | ConvertTo-Json -Depth 8 -Compress | Write-Host
            exit 7
        }

        Write-Host "gate_status=failed_playmode_suite"
        exit 7
    }
}

$playModeScreenshotsObj = $null
if (-not $SkipPlayModeScreenshots) {
    $playModeScreenshotsResult = Invoke-PlayModeScreenshotCheck -PlayModeScreenshotsScriptPath $playModeScreenshotsScript -RootPath $ProjectPath
    $playModeScreenshotsObj = $playModeScreenshotsResult.playmode_screenshots

    if (-not $Compact -and $null -ne $playModeScreenshotsObj) {
        Write-Host ("playmode_screenshot_status=" + $playModeScreenshotsObj.playmode_screenshot_status)
        Write-Host ("playmode_screenshot_count=" + $playModeScreenshotsObj.screenshot_count)
    }

    if ($playModeScreenshotsResult.exit_code -ne 0) {
        if ($JsonOnly) {
            $statusObj = Get-StatusObject -ShowScriptPath $showScript -RootPath $ProjectPath -MaxAge $MaxAgeMinutes
            [ordered]@{
                gate_status = "failed_playmode_screenshots"
                status = $statusObj
                assets = $assetObj
                layout = $layoutObj
                hud_contract = $hudContractObj
                playmode_suite = $playModeSuiteObj
                playmode_screenshots = $playModeScreenshotsObj
            } | ConvertTo-Json -Depth 8 -Compress | Write-Host
            exit 8
        }

        Write-Host "gate_status=failed_playmode_screenshots"
        exit 8
    }
}

$playModeRetakePlanObj = $null
if (-not $SkipPlayModeRetakePlan) {
    $playModeRetakePlanResult = Invoke-PlayModeRetakePlanCheck -PlayModeRetakePlanScriptPath $playModeRetakePlanScript -RootPath $ProjectPath
    $playModeRetakePlanObj = $playModeRetakePlanResult.playmode_retake_plan

    if (-not $Compact -and $null -ne $playModeRetakePlanObj) {
        Write-Host ("retake_plan_doc_status=" + $playModeRetakePlanObj.retake_plan_doc_status)
        Write-Host ("retake_plan_doc_focused_retake_count=" + $playModeRetakePlanObj.documented_focused_retake_count + "/" + $playModeRetakePlanObj.expected_focused_retake_count)
    }

    if ($playModeRetakePlanResult.exit_code -ne 0) {
        if ($JsonOnly) {
            $statusObj = Get-StatusObject -ShowScriptPath $showScript -RootPath $ProjectPath -MaxAge $MaxAgeMinutes
            [ordered]@{
                gate_status = "failed_playmode_retake_plan"
                status = $statusObj
                assets = $assetObj
                layout = $layoutObj
                hud_contract = $hudContractObj
                playmode_suite = $playModeSuiteObj
                playmode_screenshots = $playModeScreenshotsObj
                playmode_retake_plan = $playModeRetakePlanObj
            } | ConvertTo-Json -Depth 8 -Compress | Write-Host
            exit 9
        }

        Write-Host "gate_status=failed_playmode_retake_plan"
        exit 9
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
        hud_contract = $hudContractObj
        playmode_suite = $playModeSuiteObj
        playmode_screenshots = $playModeScreenshotsObj
        playmode_retake_plan = $playModeRetakePlanObj
    } | ConvertTo-Json -Depth 8 -Compress | Write-Host
    exit 0
}

Write-Host "gate_status=ok"
exit 0
