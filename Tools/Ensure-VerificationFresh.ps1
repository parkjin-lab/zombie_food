param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [string]$StatusFile = "",
    [int]$StaleAfterMinutes = 180,
    [switch]$RunTests,
    [switch]$Strict,
    [switch]$ForceHeadless
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "VerificationStatusPath.ps1")

$runScript = Join-Path $ProjectPath "Tools\Run-Verification.ps1"
$showScript = Join-Path $ProjectPath "Tools\Show-VerificationStatus.ps1"
$statusFile = Get-VerificationStatusFilePath -ProjectPath $ProjectPath -StatusFile $StatusFile

if (-not (Test-Path $runScript)) {
    Write-Error "Missing script: $runScript"
    exit 90
}

if (-not (Test-Path $showScript)) {
    Write-Error "Missing script: $showScript"
    exit 90
}

$needsRefresh = $true
if (Test-Path $statusFile) {
    $item = Get-Item $statusFile
    $age = ((Get-Date) - $item.LastWriteTime).TotalMinutes
    if ($age -le $StaleAfterMinutes) {
        $needsRefresh = $false
    }

    if (-not $needsRefresh) {
        if ($ForceHeadless) {
            # Explicit override: caller requested a real headless rerun even when status is fresh.
            $needsRefresh = $true
        }

        $statusMap = @{}
        Get-Content -LiteralPath $statusFile | ForEach-Object {
            if ([string]::IsNullOrWhiteSpace($_)) {
                return
            }

            $parts = $_.Split("=", 2)
            if ($parts.Length -eq 2) {
                $statusMap[$parts[0]] = $parts[1]
            }
        }

        if ($RunTests -and $statusMap.ContainsKey("tests_status") -and $statusMap["tests_status"] -eq "skipped") {
            $needsRefresh = $true
        }
    }
}

if ($needsRefresh) {
    $runArgs = @(
        "-ExecutionPolicy", "Bypass",
        "-File", $runScript,
        "-ProjectPath", $ProjectPath,
        "-StatusFile", $statusFile,
        "-Compact"
    )

    if ($RunTests) {
        $runArgs += "-RunTests"
    }

    if ($Strict) {
        $runArgs += "-Strict"
    }

    if ($ForceHeadless) {
        $runArgs += "-ForceHeadless"
    }

    & powershell @runArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host "verification_refresh=failed"
        exit 2
    }

    Write-Host "verification_refresh=ran"
} else {
    Write-Host "verification_refresh=skipped_fresh"
}

powershell -ExecutionPolicy Bypass -File $showScript -ProjectPath $ProjectPath -StatusFile $statusFile -Json -StaleAfterMinutes $StaleAfterMinutes
exit $LASTEXITCODE
