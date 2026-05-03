param(
    [string]$UnityPath = "D:\Unity\6000.3.8f1\Editor\Unity.exe",
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [switch]$SkipTests,
    [switch]$Strict,
    [switch]$Compact,
    [int]$BatchTimeoutSeconds = 240
)

$ErrorActionPreference = "Stop"

function Invoke-UnityBatch {
    param(
        [string]$ExePath,
        [string[]]$Arguments,
        [int]$TimeoutSeconds,
        [ref]$OutputLines
    )

    $job = Start-Job -ScriptBlock {
        param($jobExe, $jobArgs)
        $out = & $jobExe @jobArgs 2>&1
        [pscustomobject]@{
            ExitCode = $LASTEXITCODE
            Lines = @($out | ForEach-Object { $_.ToString() })
        }
    } -ArgumentList $ExePath, $Arguments

    try {
        $completed = Wait-Job -Job $job -Timeout $TimeoutSeconds
        if ($null -eq $completed) {
            Stop-Job -Job $job -ErrorAction SilentlyContinue | Out-Null
            if ($null -ne $OutputLines) {
                $OutputLines.Value = @("ProcessTimeout")
            }

            return 124
        }

        $result = Receive-Job -Job $job
        $exitCode = 0
        $lines = @()
        if ($null -ne $result) {
            $first = @($result | Select-Object -First 1)[0]
            if ($null -ne $first -and $null -ne $first.PSObject.Properties["ExitCode"]) {
                $exitCode = [int]$first.ExitCode
            }

            if ($null -ne $first -and $null -ne $first.PSObject.Properties["Lines"]) {
                $lines = @($first.Lines | ForEach-Object { $_.ToString() })
            }
        }

        if ($null -ne $OutputLines) {
            $OutputLines.Value = $lines
        }

        return $exitCode
    }
    finally {
        Remove-Job -Job $job -Force -ErrorAction SilentlyContinue | Out-Null
    }
}

function Log-Section {
    param([string]$Message)
    if (-not $Compact) {
        Write-Host ("`n=== " + $Message + " ===")
    }
}

function Is-EnvironmentBlocked {
    param(
        [string[]]$Lines
    )

    if ($null -eq $Lines -or $Lines.Count -eq 0) {
        return $false
    }

    foreach ($line in $Lines) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        if ($line.IndexOf("CodexSandboxOffline", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $true
        }

        if ($line.IndexOf("Access is denied", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $true
        }

    }

    return $false
}

if (-not (Test-Path $UnityPath)) {
    Write-Error "Unity executable not found: $UnityPath"
    exit 99
}

if (-not (Test-Path $ProjectPath)) {
    Write-Error "Project path not found: $ProjectPath"
    exit 99
}

$existingUnity = Get-Process -Name "Unity" -ErrorAction SilentlyContinue | Where-Object { $_.Id -ne $PID }
if ($null -ne $existingUnity -and $existingUnity.Count -gt 0) {
    if (-not $Compact) {
        Write-Warning "Detected running Unity Editor process. Treating as blocked environment for headless verification."
    }

    Write-Host "compile_status=blocked_env"
    if ($SkipTests) {
        Write-Host "tests_status=skipped"
    } else {
        Write-Host "tests_status=blocked_env"
    }
    exit 0
}

$tempDir = Join-Path $ProjectPath "Temp"
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir | Out-Null
}

$compileLog = Join-Path $tempDir "codex_compile_check.log"
$testLog = Join-Path $tempDir "editmode-test.log"
$testResult = Join-Path $tempDir "editmode-results.xml"

if (Test-Path $compileLog) { Remove-Item -LiteralPath $compileLog -Force }
if (Test-Path $testLog) { Remove-Item -LiteralPath $testLog -Force }
if (Test-Path $testResult) { Remove-Item -LiteralPath $testResult -Force }

Log-Section "Compile Check"
$compileArgs = @(
    "-batchmode",
    "-quit",
    "-projectPath", $ProjectPath,
    "-logFile", $compileLog
)

$compileOutput = @()
$compileExit = Invoke-UnityBatch -ExePath $UnityPath -Arguments $compileArgs -TimeoutSeconds $BatchTimeoutSeconds -OutputLines ([ref]$compileOutput)
if (-not $Compact) {
    Write-Host ("compile_exit_code=" + $compileExit)
}

if ($compileExit -eq 124) {
    if (-not $Compact) {
        Write-Warning "Compile batch run timed out. Treating as blocked environment."
    }

    Write-Host "compile_status=blocked_env"
    if ($SkipTests) {
        Write-Host "tests_status=skipped"
    } else {
        Write-Host "tests_status=blocked_env"
    }
    exit 0
}

if (-not [string]::IsNullOrWhiteSpace([string]$compileExit) -and $compileExit -ne 0) {
    Write-Error "Compile batch run failed with exit code $compileExit"
    exit 2
}

if (-not (Test-Path $compileLog)) {
    if ($Strict) {
        Write-Error "Compile log was not created: $compileLog"
        exit 2
    }

    if (Is-EnvironmentBlocked -Lines $compileOutput) {
        Write-Host "compile_status=blocked_env"
        if ($SkipTests) {
            Write-Host "tests_status=skipped"
            exit 0
        }
    }
    else {
        if (-not $Compact) {
            Write-Warning "Compile log was not created. Treating compile status as inconclusive."
        }
        Write-Host "compile_status=inconclusive"
    }

}
else {
    $compileErrors = Select-String -Path $compileLog -Pattern "error CS\d+|Compilation failed|All compiler errors" -SimpleMatch:$false
    if ($compileErrors) {
        Write-Error "Compile errors detected in $compileLog"
        $compileErrors | ForEach-Object { Write-Host $_.Line }
        exit 2
    }

    Write-Host "compile_status=ok"
}

if ($SkipTests) {
    Write-Host "tests_status=skipped"
    exit 0
}

Log-Section "EditMode Tests"
$testArgs = @(
    "-batchmode",
    "-projectPath", $ProjectPath,
    "-runTests",
    "-testPlatform", "EditMode",
    "-testResults", $testResult,
    "-logFile", $testLog
)

$testOutput = @()
$testExit = Invoke-UnityBatch -ExePath $UnityPath -Arguments $testArgs -TimeoutSeconds $BatchTimeoutSeconds -OutputLines ([ref]$testOutput)
if (-not $Compact) {
    Write-Host ("test_process_exit_code=" + $testExit)
}

if ($testExit -eq 124) {
    if (-not $Compact) {
        Write-Warning "EditMode test run timed out. Treating as blocked environment."
    }

    Write-Host "tests_status=blocked_env"
    exit 0
}

if (-not (Test-Path $testLog)) {
    if ($Strict) {
        Write-Error "Test log was not created: $testLog"
        exit 4
    }

    if (Is-EnvironmentBlocked -Lines $testOutput) {
        Write-Host "tests_status=blocked_env"
        exit 0
    }

    if (-not $Compact) {
        Write-Warning "Test log was not created. Treating tests status as inconclusive."
    }
    Write-Host "tests_status=inconclusive"
    exit 0
}

if (-not (Test-Path $testResult)) {
    $runnerLines = Select-String -Path $testLog -Pattern "TestRunner|Running tests|No tests|test results|test-run" -SimpleMatch:$false
    if ($runnerLines) {
        if (-not $Compact) {
            Write-Warning "Test result XML missing but runner-like lines exist."
            $runnerLines | ForEach-Object { Write-Host $_.Line }
        }
    } else {
        if (-not $Compact) {
            Write-Warning "Test result XML missing and no runner markers found in log."
        }
    }

    if ($Strict) {
        exit 4
    }

    Write-Host "tests_status=inconclusive"
    exit 0
}

[xml]$xml = Get-Content -Path $testResult
$testRun = $xml.SelectSingleNode("//test-run")
if ($null -eq $testRun) {
    if (-not $Compact) {
        Write-Warning "test-run node not found in XML. Treating as inconclusive."
    }
    if ($Strict) {
        exit 4
    }

    Write-Host "tests_status=inconclusive"
    exit 0
}

$failed = [int]$testRun.GetAttribute("failed")
$total = [int]$testRun.GetAttribute("total")
$passed = [int]$testRun.GetAttribute("passed")
if (-not $Compact) {
    Write-Host ("tests_total=" + $total + " passed=" + $passed + " failed=" + $failed)
}

if ($failed -gt 0) {
    Write-Error "EditMode tests failed."
    exit 3
}

Write-Host "tests_status=ok"
exit 0
