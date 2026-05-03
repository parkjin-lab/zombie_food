function Test-VerificationStatusDirectoryWritable {
    param([string]$DirectoryPath)

    if ([string]::IsNullOrWhiteSpace($DirectoryPath)) {
        return $false
    }

    try {
        if (-not (Test-Path -LiteralPath $DirectoryPath)) {
            New-Item -ItemType Directory -Path $DirectoryPath -Force -ErrorAction Stop | Out-Null
        }

        $probePath = Join-Path $DirectoryPath (".verification-write-probe-" + [Guid]::NewGuid().ToString("N") + ".tmp")
        Set-Content -LiteralPath $probePath -Value "ok" -Encoding UTF8 -ErrorAction Stop
        Remove-Item -LiteralPath $probePath -Force -ErrorAction SilentlyContinue
        return $true
    }
    catch {
        return $false
    }
}

function Get-VerificationStatusFilePath {
    param(
        [string]$ProjectPath,
        [string]$StatusFile = ""
    )

    if (-not [string]::IsNullOrWhiteSpace($StatusFile)) {
        return $StatusFile
    }

    $projectStatusFile = Join-Path $ProjectPath "Temp\verification-status.txt"
    $projectStatusDir = Split-Path -Parent $projectStatusFile
    if (Test-VerificationStatusDirectoryWritable -DirectoryPath $projectStatusDir) {
        return $projectStatusFile
    }

    $tempRoot = $env:TEMP
    if ([string]::IsNullOrWhiteSpace($tempRoot)) {
        $tempRoot = [System.IO.Path]::GetTempPath()
    }

    $projectName = Split-Path -Leaf $ProjectPath
    if ([string]::IsNullOrWhiteSpace($projectName)) {
        $projectName = "project"
    }

    $safeProjectName = $projectName -replace '[^A-Za-z0-9._-]', '_'
    $fallbackDir = Join-Path $tempRoot ($safeProjectName + "-verification")

    if (-not (Test-Path -LiteralPath $fallbackDir)) {
        New-Item -ItemType Directory -Path $fallbackDir -Force | Out-Null
    }

    return (Join-Path $fallbackDir "verification-status.txt")
}
