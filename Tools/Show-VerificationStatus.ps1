param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [string]$StatusFile = "",
    [switch]$Json,
    [int]$StaleAfterMinutes = 180
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "VerificationStatusPath.ps1")

$StatusFile = Get-VerificationStatusFilePath -ProjectPath $ProjectPath -StatusFile $StatusFile

if (-not (Test-Path -LiteralPath $StatusFile)) {
    Write-Host "status_file=missing"
    exit 1
}

$item = Get-Item -LiteralPath $StatusFile
$rawLines = Get-Content -LiteralPath $StatusFile
$ageMinutes = [math]::Round(((Get-Date) - $item.LastWriteTime).TotalMinutes, 1)
$isStale = $ageMinutes -gt $StaleAfterMinutes

$statusMap = @{}
foreach ($line in $rawLines) {
    if ([string]::IsNullOrWhiteSpace($line)) {
        continue
    }

    $parts = $line.Split("=", 2)
    if ($parts.Length -ne 2) {
        continue
    }

    $statusMap[$parts[0]] = $parts[1]
}

$staticStatus = $statusMap["static_status"]
$compileStatus = $statusMap["compile_status"]
$testsStatus = $statusMap["tests_status"]
$verificationStatus = $statusMap["verification_status"]
$blockedEnv = ($compileStatus -eq "blocked_env" -or $testsStatus -eq "blocked_env")
$manualVerificationRequired = $blockedEnv -or $compileStatus -eq "inconclusive" -or $testsStatus -eq "inconclusive"
$verificationReadiness = if ($manualVerificationRequired) { "needs_manual_check" } else { "ready" }

$confidence = "low"
if ($staticStatus -eq "ok" -and $compileStatus -eq "ok" -and $testsStatus -eq "ok") {
    $confidence = "high"
}
elseif ($staticStatus -eq "ok" -and $compileStatus -eq "ok") {
    $confidence = "medium"
}
elseif ($staticStatus -eq "ok" -and $compileStatus -eq "inconclusive" -and ($testsStatus -eq "inconclusive" -or $testsStatus -eq "skipped")) {
    $confidence = "low"
}

$recommendedAction = "inspect_status_file"
$recommendedCommand = "powershell -ExecutionPolicy Bypass -File `"$ProjectPath\Tools\Show-VerificationStatus.ps1`" -ProjectPath `"$ProjectPath`""
if ($isStale) {
    $recommendedAction = "refresh_verification"
    $recommendedCommand = "powershell -ExecutionPolicy Bypass -File `"$ProjectPath\Tools\Ensure-VerificationFresh.ps1`" -ProjectPath `"$ProjectPath`" -StaleAfterMinutes $StaleAfterMinutes"
}
elseif ($staticStatus -ne "ok") {
    $recommendedAction = "fix_static_guards"
    $recommendedCommand = "powershell -ExecutionPolicy Bypass -File `"$ProjectPath\Tools\Verify-PrototypeStatic.ps1`" -ProjectPath `"$ProjectPath`""
}
elseif ($verificationStatus -eq "failed_headless") {
    $recommendedAction = "investigate_headless_failure"
    $recommendedCommand = "powershell -ExecutionPolicy Bypass -File `"$ProjectPath\Tools\Verify-UnityHeadless.ps1`" -ProjectPath `"$ProjectPath`" -Compact"
}
elseif ($blockedEnv) {
    $recommendedAction = "run_local_unity_editor_verification"
    $recommendedCommand = "Unity Editor local run (sandbox blocked)"
}
elseif ($compileStatus -eq "ok" -and $testsStatus -eq "skipped") {
    $recommendedAction = "run_tests_for_confidence"
    $recommendedCommand = "powershell -ExecutionPolicy Bypass -File `"$ProjectPath\Tools\Gate-Verification.ps1`" -ProjectPath `"$ProjectPath`" -RunTests -RequireFresh -RequireCompileOk -RequireTestsOk"
}
elseif (($compileStatus -eq "inconclusive" -or $testsStatus -eq "inconclusive") -and -not $blockedEnv) {
    $recommendedAction = "force_headless_for_higher_confidence"
    $recommendedCommand = "powershell -ExecutionPolicy Bypass -File `"$ProjectPath\Tools\Gate-Verification.ps1`" -ProjectPath `"$ProjectPath`" -RunTests -ForceHeadless -RequireFresh -RequireCompileOk -RequireTestsOk"
}
elseif ($compileStatus -eq "ok" -and $testsStatus -eq "ok") {
    $recommendedAction = "ready_for_next_feature_task"
    $recommendedCommand = "Proceed to implementation; keep verification gate in CI."
}

if ($Json) {
    $obj = [ordered]@{
        status_file = $StatusFile
        status_updated = $item.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
        status_age_minutes = $ageMinutes
        status_stale = $isStale
        static_status = $staticStatus
        compile_status = $compileStatus
        tests_status = $testsStatus
        verification_status = $verificationStatus
        verification_confidence = $confidence
        verification_blocked_env = $blockedEnv
        manual_verification_required = $manualVerificationRequired
        verification_readiness = $verificationReadiness
        recommended_next_action = $recommendedAction
        recommended_command = $recommendedCommand
    }

    $obj | ConvertTo-Json -Compress | Write-Host
    exit 0
}

Write-Host ("status_file=" + $StatusFile)
Write-Host ("status_updated=" + $item.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"))
Write-Host ("status_age_minutes=" + $ageMinutes)
Write-Host ("status_stale=" + $isStale)
$rawLines | ForEach-Object { Write-Host $_ }
Write-Host ("verification_confidence=" + $confidence)
Write-Host ("verification_blocked_env=" + $blockedEnv)
Write-Host ("manual_verification_required=" + $manualVerificationRequired)
Write-Host ("verification_readiness=" + $verificationReadiness)
Write-Host ("recommended_next_action=" + $recommendedAction)
Write-Host ("recommended_command=" + $recommendedCommand)
exit 0
