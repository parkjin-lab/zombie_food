param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"

function Invoke-JsonScript {
    param(
        [string]$ScriptPath,
        [string[]]$Arguments
    )

    if (-not (Test-Path -LiteralPath $ScriptPath)) {
        return [pscustomobject]@{
            ok = $false
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
            ok = $false
            exit_code = $exitCode
            data = $null
            error = "empty_output"
        }
    }

    try {
        return [pscustomobject]@{
            ok = ($exitCode -eq 0)
            exit_code = $exitCode
            data = ($jsonLine | ConvertFrom-Json)
            error = $null
        }
    }
    catch {
        return [pscustomobject]@{
            ok = $false
            exit_code = $exitCode
            data = $null
            error = "json_parse_failed: " + $_.Exception.Message
        }
    }
}

$gateScript = Join-Path $ProjectPath "Tools\Gate-Verification.ps1"
$assetScript = Join-Path $ProjectPath "Tools\Verify-PrototypeAssets.ps1"
$layoutScript = Join-Path $ProjectPath "Tools\Verify-PrototypeLayout.ps1"
$playModeRecordScript = Join-Path $ProjectPath "Tools\Verify-PrototypePlayModeRecord.ps1"
$handoffPath = Join-Path $ProjectPath "Docs\Prototype_Session_Handoff.md"
$playModePath = Join-Path $ProjectPath "Docs\Prototype_PlayMode_Verification.md"
$playbookPath = Join-Path $ProjectPath "Docs\Prototype_NextStep_Playbook.md"

$gateResult = Invoke-JsonScript -ScriptPath $gateScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-RunTests",
    "-JsonOnly"
)

$assetResult = Invoke-JsonScript -ScriptPath $assetScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-Strict",
    "-JsonOnly"
)

$playModeRecordResult = Invoke-JsonScript -ScriptPath $playModeRecordScript -Arguments @(
    "-ProjectPath", $ProjectPath,
    "-JsonOnly"
)

$gateData = $gateResult.data
$assetData = $assetResult.data
$verification = if ($null -ne $gateData) { $gateData.status } else { $null }
$gateAssets = if ($null -ne $gateData) { $gateData.assets } else { $null }
$gateLayout = if ($null -ne $gateData) { $gateData.layout } else { $null }
$playModeRecordData = $playModeRecordResult.data

$assetStatus = if ($null -ne $assetData) { $assetData.asset_status } elseif ($null -ne $gateAssets) { $gateAssets.asset_status } else { "unknown" }
$layoutStatus = if ($null -ne $gateLayout) { $gateLayout.layout_status } else { "unknown" }
$playModeRecordStatus = if ($null -ne $playModeRecordData) { $playModeRecordData.playmode_record_status } else { "unknown" }
$staticStatus = if ($null -ne $verification) { $verification.static_status } else { "unknown" }
$compileStatus = if ($null -ne $verification) { $verification.compile_status } else { "unknown" }
$testsStatus = if ($null -ne $verification) { $verification.tests_status } else { "unknown" }
$manualRequired = if ($null -ne $verification) { [bool]$verification.manual_verification_required } else { $true }

$readiness = "needs_manual_playmode"
if ($gateResult.ok -and $assetStatus -eq "ok" -and $layoutStatus -eq "ok" -and $staticStatus -eq "ok" -and $playModeRecordStatus -eq "passed" -and -not $manualRequired) {
    $readiness = "ready"
}
elseif (-not $gateResult.ok -or $assetStatus -ne "ok" -or $layoutStatus -ne "ok" -or $staticStatus -ne "ok" -or $playModeRecordStatus -eq "invalid_record") {
    $readiness = "needs_fix_before_playmode"
}
elseif ($playModeRecordStatus -eq "needs_fix" -or $playModeRecordStatus -eq "blocked") {
    $readiness = "needs_playmode_fix"
}

$summary = [ordered]@{
    project_path = $ProjectPath
    generated_at = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    readiness = $readiness
    gate_status = if ($null -ne $gateData) { $gateData.gate_status } else { "unknown" }
    asset_status = $assetStatus
    layout_status = $layoutStatus
    playmode_record_status = $playModeRecordStatus
    static_status = $staticStatus
    compile_status = $compileStatus
    tests_status = $testsStatus
    manual_verification_required = $manualRequired
    handoff_doc = $handoffPath
    playmode_verification_doc = $playModePath
    playbook_doc = $playbookPath
    docs_exist = [ordered]@{
        handoff = (Test-Path -LiteralPath $handoffPath)
        playmode_verification = (Test-Path -LiteralPath $playModePath)
        playbook = (Test-Path -LiteralPath $playbookPath)
        layout_verifier = (Test-Path -LiteralPath $layoutScript)
        playmode_record_verifier = (Test-Path -LiteralPath $playModeRecordScript)
    }
    unresolved_issues = @(
        "Unity headless compile/tests remain inconclusive in this environment.",
        ("Manual Unity Play Mode verification record status: " + $playModeRecordStatus + "."),
        "Portrait UI readability for game view + placement board + block choice still needs visual confirmation even when numeric layout guard passes."
    )
    recommended_next_actions = @(
        "Open Unity Editor locally and run Play Mode, or use the already-open Editor session if one is running.",
        "In Play Mode, use Tools > Food Truck Prototype > Capture Play Mode Snapshot to collect screenshot evidence.",
        "If all checklist states pass visually, use Tools > Food Truck Prototype > Record PASS Manual Result.",
        "If any checklist state fails, record the FIX_* result manually in Docs\Prototype_PlayMode_Verification.md.",
        "Close Unity Editor before using forced headless verification.",
        "Run Tools\Verify-PrototypeLayout.ps1 if layout code changes before Play Mode.",
        "Run Tools\Verify-PrototypePlayModeRecord.ps1 after recording Play Mode results.",
        "If layout fails, adjust FoodTruckPrototypeHud.CalculateGameplayFocusLayout / ApplyPanelLayout before adding features.",
        "After manual visual confirmation, rerun Tools\Gate-Verification.ps1 -RunTests -JsonOnly."
    )
}

if ($JsonOnly) {
    $summary | ConvertTo-Json -Depth 6 -Compress | Write-Host
    exit 0
}

Write-Host ("prototype_session_readiness=" + $summary.readiness)
Write-Host ("gate_status=" + $summary.gate_status)
Write-Host ("asset_status=" + $summary.asset_status)
Write-Host ("layout_status=" + $summary.layout_status)
Write-Host ("playmode_record_status=" + $summary.playmode_record_status)
Write-Host ("static_status=" + $summary.static_status)
Write-Host ("compile_status=" + $summary.compile_status)
Write-Host ("tests_status=" + $summary.tests_status)
Write-Host ("manual_verification_required=" + $summary.manual_verification_required)
Write-Host ("handoff_doc=" + $summary.handoff_doc)
Write-Host ("playmode_verification_doc=" + $summary.playmode_verification_doc)
Write-Host "recommended_next_actions:"
foreach ($action in $summary.recommended_next_actions) {
    Write-Host ("- " + $action)
}

exit 0
