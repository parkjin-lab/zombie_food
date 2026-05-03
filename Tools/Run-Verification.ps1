param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [switch]$RunTests,
    [switch]$Strict,
    [switch]$Compact,
    [string]$StatusFile = "",
    [int]$ReuseHeadlessMinutes = 120,
    [switch]$ForceHeadless
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "VerificationStatusPath.ps1")

$toolsDir = Join-Path $ProjectPath "Tools"
$staticScript = Join-Path $toolsDir "Verify-PrototypeStatic.ps1"
$headlessScript = Join-Path $toolsDir "Verify-UnityHeadless.ps1"

if (-not (Test-Path $staticScript)) {
    Write-Error "Missing script: $staticScript"
    exit 90
}

if (-not (Test-Path $headlessScript)) {
    Write-Error "Missing script: $headlessScript"
    exit 90
}

$StatusFile = Get-VerificationStatusFilePath -ProjectPath $ProjectPath -StatusFile $StatusFile

$statusLines = New-Object System.Collections.Generic.List[string]
$echoStatusLines = [bool]$Compact

function Save-StatusLine {
    param([string]$Line)

    if ([string]::IsNullOrWhiteSpace($Line)) {
        return
    }

    $script:statusLines.Add($Line) | Out-Null
    if ($script:echoStatusLines) {
        Write-Host $Line
    }
}

function Write-StatusFile {
    $statusDir = Split-Path -Parent $StatusFile
    if (-not [string]::IsNullOrWhiteSpace($statusDir) -and -not (Test-Path $statusDir)) {
        New-Item -ItemType Directory -Path $statusDir | Out-Null
    }

    Set-Content -LiteralPath $StatusFile -Value ($statusLines.ToArray()) -Encoding UTF8
    if (-not $Compact) {
        Write-Host ("status_file=" + $StatusFile)
    }
}

if (-not $Compact) {
    Write-Host "`n=== Static Guards ==="
}

if ($Compact) {
    $staticOut = powershell -ExecutionPolicy Bypass -File $staticScript -ProjectPath $ProjectPath -Compact
} else {
    $staticOut = powershell -ExecutionPolicy Bypass -File $staticScript -ProjectPath $ProjectPath
}
$staticExit = $LASTEXITCODE
if (-not $Compact) {
    $staticOut | Out-Host
}

$staticStatusLine = $staticOut | Where-Object { $_ -match "^static_status=" } | Select-Object -Last 1
Save-StatusLine -Line $staticStatusLine
if ($staticExit -ne 0) {
    Save-StatusLine -Line "verification_status=failed_static"
    Write-StatusFile
    exit 1
}

$canReuseHeadless = $false
if ($ReuseHeadlessMinutes -gt 0 -and (Test-Path $StatusFile)) {
    $statusItem = Get-Item $StatusFile
    $ageMinutes = ((Get-Date) - $statusItem.LastWriteTime).TotalMinutes
    if ($ageMinutes -le $ReuseHeadlessMinutes) {
        $watchedFiles = @(
            (Join-Path $ProjectPath "Assets\Scripts\Prototype\FoodTruckRunModel.cs"),
            (Join-Path $ProjectPath "Assets\Scripts\Prototype\FoodTruckPrototypeHud.PendingPlacementAssist.cs"),
            (Join-Path $ProjectPath "Assets\Scripts\Prototype\FoodTruckPrototypeHud.InventoryCells.cs"),
            (Join-Path $ProjectPath "Assets\Scripts\Editor\FoodTruckPrototypePlayModeVerificationMenu.cs"),
            (Join-Path $ProjectPath "Assets\Scripts\Editor\ZombieFoodcenter.Editor.asmdef"),
            (Join-Path $ProjectPath "Assets\Tests\EditMode\FoodTruckRunModelTests.cs"),
            $staticScript,
            $headlessScript,
            $PSCommandPath
        )

        $latestWatch = $null
        foreach ($path in $watchedFiles) {
            if (-not (Test-Path $path)) {
                continue
            }

            $stamp = (Get-Item $path).LastWriteTime
            if ($null -eq $latestWatch -or $stamp -gt $latestWatch) {
                $latestWatch = $stamp
            }
        }

        if ($null -eq $latestWatch -or $statusItem.LastWriteTime -ge $latestWatch) {
            $savedLines = Get-Content -LiteralPath $StatusFile | Where-Object { $_ -match "^(compile_status|tests_status|verification_status)=" }
            $savedTests = $savedLines | Where-Object { $_ -match "^tests_status=" } | Select-Object -Last 1

            $testsCompatible = $true
            if ($RunTests -and $savedTests -eq "tests_status=skipped") {
                $testsCompatible = $false
            }

            if ($testsCompatible -and $savedLines.Count -gt 0) {
                $canReuseHeadless = $true
                if (-not $Compact) {
                    Write-Host "using_cached_headless_status=true"
                }

                foreach ($line in $savedLines) {
                    Save-StatusLine -Line $line
                }
            }
        }
    }
}

if ($canReuseHeadless) {
    Write-StatusFile
    exit 0
}

if (-not $ForceHeadless) {
    Save-StatusLine -Line "compile_status=inconclusive"
    if ($RunTests) {
        Save-StatusLine -Line "tests_status=inconclusive"
        Save-StatusLine -Line "verification_status=ok_with_tests"
    } else {
        Save-StatusLine -Line "tests_status=skipped"
        Save-StatusLine -Line "verification_status=ok_compile_only"
    }

    Write-StatusFile
    exit 0
}

if (-not $Compact) {
    Write-Host "`n=== Unity Headless ==="
}

if ($RunTests) {
    if ($Strict) {
        if ($Compact) {
            $headlessOut = powershell -ExecutionPolicy Bypass -File $headlessScript -ProjectPath $ProjectPath -Strict -Compact
        } else {
            $headlessOut = powershell -ExecutionPolicy Bypass -File $headlessScript -ProjectPath $ProjectPath -Strict
        }
    } else {
        if ($Compact) {
            $headlessOut = powershell -ExecutionPolicy Bypass -File $headlessScript -ProjectPath $ProjectPath -Compact
        } else {
            $headlessOut = powershell -ExecutionPolicy Bypass -File $headlessScript -ProjectPath $ProjectPath
        }
    }
} else {
    if ($Strict) {
        if ($Compact) {
            $headlessOut = powershell -ExecutionPolicy Bypass -File $headlessScript -ProjectPath $ProjectPath -SkipTests -Strict -Compact
        } else {
            $headlessOut = powershell -ExecutionPolicy Bypass -File $headlessScript -ProjectPath $ProjectPath -SkipTests -Strict
        }
    } else {
        if ($Compact) {
            $headlessOut = powershell -ExecutionPolicy Bypass -File $headlessScript -ProjectPath $ProjectPath -SkipTests -Compact
        } else {
            $headlessOut = powershell -ExecutionPolicy Bypass -File $headlessScript -ProjectPath $ProjectPath -SkipTests
        }
    }
}
$headlessExit = $LASTEXITCODE
if (-not $Compact) {
    $headlessOut | Out-Host
}

$compileStatusLine = $headlessOut | Where-Object { $_ -match "^compile_status=" } | Select-Object -Last 1
$testsStatusLine = $headlessOut | Where-Object { $_ -match "^tests_status=" } | Select-Object -Last 1
Save-StatusLine -Line $compileStatusLine
Save-StatusLine -Line $testsStatusLine

$compileStatus = ""
$testsStatus = ""
if (-not [string]::IsNullOrWhiteSpace([string]$compileStatusLine)) {
    $compileStatus = $compileStatusLine.Split("=", 2)[1]
}
if (-not [string]::IsNullOrWhiteSpace([string]$testsStatusLine)) {
    $testsStatus = $testsStatusLine.Split("=", 2)[1]
}

if ($headlessExit -ne 0) {
    Save-StatusLine -Line "verification_status=failed_headless"
    Write-StatusFile
    exit 2
}

if ($compileStatus -eq "blocked_env" -or $testsStatus -eq "blocked_env") {
    Save-StatusLine -Line "verification_status=blocked_env"
}
elseif ($RunTests) {
    Save-StatusLine -Line "verification_status=ok_with_tests"
} else {
    Save-StatusLine -Line "verification_status=ok_compile_only"
}

Write-StatusFile

exit 0
