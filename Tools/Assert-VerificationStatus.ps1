param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [string]$StatusFile = "",
    [switch]$RequireCompileOk,
    [switch]$RequireTestsOk,
    [switch]$RequireFresh,
    [int]$MaxAgeMinutes = 180
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "VerificationStatusPath.ps1")

$StatusFile = Get-VerificationStatusFilePath -ProjectPath $ProjectPath -StatusFile $StatusFile

if (-not (Test-Path -LiteralPath $StatusFile)) {
    Write-Host "assert_status=failed_missing_status_file"
    exit 2
}

$map = @{}
Get-Content -LiteralPath $StatusFile | ForEach-Object {
    if ([string]::IsNullOrWhiteSpace($_)) {
        return
    }

    $parts = $_.Split("=", 2)
    if ($parts.Length -eq 2) {
        $map[$parts[0]] = $parts[1]
    }
}

$item = Get-Item -LiteralPath $StatusFile
$age = [math]::Round(((Get-Date) - $item.LastWriteTime).TotalMinutes, 1)

$failed = $false
if ($RequireFresh -and $age -gt $MaxAgeMinutes) {
    Write-Host ("assert_fail=freshness age_minutes=" + $age + " max=" + $MaxAgeMinutes)
    $failed = $true
}

$compileStatus = $map["compile_status"]
$testsStatus = $map["tests_status"]
$blockedEnv = ($compileStatus -eq "blocked_env" -or $testsStatus -eq "blocked_env")

if (($RequireCompileOk -or $RequireTestsOk) -and $blockedEnv) {
    Write-Host "assert_fail=blocked_env manual_verification_required=true"
    Write-Host "assert_status=failed"
    exit 1
}

if ($RequireCompileOk -and $compileStatus -ne "ok") {
    Write-Host ("assert_fail=compile_status actual=" + $compileStatus + " expected=ok")
    $failed = $true
}

if ($RequireTestsOk -and $testsStatus -ne "ok") {
    Write-Host ("assert_fail=tests_status actual=" + $testsStatus + " expected=ok")
    $failed = $true
}

if ($failed) {
    Write-Host "assert_status=failed"
    exit 1
}

Write-Host "assert_status=ok"
Write-Host ("assert_age_minutes=" + $age)
Write-Host ("assert_compile_status=" + $compileStatus)
Write-Host ("assert_tests_status=" + $testsStatus)
Write-Host ("assert_blocked_env=" + $blockedEnv)
exit 0
