param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"

$docPath = Join-Path $ProjectPath "Docs\Prototype_PlayMode_Verification.md"
$validStateValues = @("PASS", "FIX_LAYOUT", "FIX_ASSET", "FIX_FEEDBACK", "BLOCKED", "NOT_RECORDED")
$stateFields = @("Draw Choice", "Pending Placement", "Invalid Placement", "Wave Combat")
$requiredInfoFields = @("Date", "Unity version", "Aspect ratio / resolution", "Verification command result")

function Normalize-FieldValue {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "NOT_RECORDED"
    }

    return $Value.Trim()
}

function Get-FieldMap {
    param([string]$SectionText)

    $map = @{}
    $lines = $SectionText -split "\r?\n"
    foreach ($line in $lines) {
        if ($line -match "^\s*([^:]+):\s*(.*)\s*$") {
            $key = $matches[1].Trim()
            $value = Normalize-FieldValue $matches[2]
            $map[$key] = $value
        }
    }

    return $map
}

if (-not (Test-Path -LiteralPath $docPath)) {
    $result = [ordered]@{
        playmode_record_status = "missing_doc"
        doc_path = $docPath
        missing_fields = @()
        invalid_fields = @()
        state_results = [ordered]@{}
        next_action = "Create Docs\Prototype_PlayMode_Verification.md."
    }

    $result | ConvertTo-Json -Depth 6 -Compress | Write-Host
    exit 7
}

$content = [System.IO.File]::ReadAllText($docPath)
$sectionMatch = [regex]::Match(
    $content,
    "(?ms)^## Latest Manual Result\s*(?<section>.*?)(?=^## |\z)"
)

if (-not $sectionMatch.Success) {
    $result = [ordered]@{
        playmode_record_status = "missing_latest_result"
        doc_path = $docPath
        missing_fields = @("Latest Manual Result")
        invalid_fields = @()
        state_results = [ordered]@{}
        next_action = "Add a '## Latest Manual Result' section and record Play Mode results."
    }

    $result | ConvertTo-Json -Depth 6 -Compress | Write-Host
    exit 8
}

$fields = Get-FieldMap $sectionMatch.Groups["section"].Value
$missingFields = New-Object System.Collections.Generic.List[string]
$invalidFields = New-Object System.Collections.Generic.List[string]
$stateResults = [ordered]@{}

foreach ($field in $requiredInfoFields) {
    if (-not $fields.ContainsKey($field) -or $fields[$field] -eq "NOT_RECORDED") {
        $missingFields.Add($field)
    }
}

foreach ($field in $stateFields) {
    if (-not $fields.ContainsKey($field)) {
        $missingFields.Add($field)
        $stateResults[$field] = "NOT_RECORDED"
        continue
    }

    $value = $fields[$field].ToUpperInvariant()
    $stateResults[$field] = $value
    if ($validStateValues -notcontains $value) {
        $invalidFields.Add($field + "=" + $fields[$field])
    }
    elseif ($value -eq "NOT_RECORDED") {
        $missingFields.Add($field)
    }
}

$stateValues = @($stateFields | ForEach-Object { $stateResults[$_] })
$hasBlocked = $stateValues -contains "BLOCKED"
$hasFix = @($stateValues | Where-Object { $_ -like "FIX_*" }).Count -gt 0
$allPass = @($stateValues | Where-Object { $_ -eq "PASS" }).Count -eq $stateFields.Count

$recordStatus = "not_recorded"
$nextAction = "Run Unity Play Mode and fill the Latest Manual Result section."

if ($invalidFields.Count -gt 0) {
    $recordStatus = "invalid_record"
    $nextAction = "Fix invalid status values in Docs\Prototype_PlayMode_Verification.md."
}
elseif ($missingFields.Count -gt 0) {
    $recordStatus = "not_recorded"
}
elseif ($hasBlocked) {
    $recordStatus = "blocked"
    $nextAction = "Resolve the Play Mode blocker before UX tuning."
}
elseif ($hasFix) {
    $recordStatus = "needs_fix"
    $nextAction = "Use the recorded FIX_* state to target the next HUD/layout or asset fix."
}
elseif ($allPass) {
    $recordStatus = "passed"
    $nextAction = "Keep the result with the session notes, then continue feature work."
}

$result = [ordered]@{
    playmode_record_status = $recordStatus
    doc_path = $docPath
    missing_fields = $missingFields.ToArray()
    invalid_fields = $invalidFields.ToArray()
    state_results = $stateResults
    date = if ($fields.ContainsKey("Date")) { $fields["Date"] } else { "NOT_RECORDED" }
    unity_version = if ($fields.ContainsKey("Unity version")) { $fields["Unity version"] } else { "NOT_RECORDED" }
    aspect_ratio_resolution = if ($fields.ContainsKey("Aspect ratio / resolution")) { $fields["Aspect ratio / resolution"] } else { "NOT_RECORDED" }
    top_issue = if ($fields.ContainsKey("Top issue")) { $fields["Top issue"] } else { "NOT_RECORDED" }
    next_code_target = if ($fields.ContainsKey("Next code target")) { $fields["Next code target"] } else { "NOT_RECORDED" }
    next_action = $nextAction
}

if ($JsonOnly) {
    $result | ConvertTo-Json -Depth 6 -Compress | Write-Host
}
else {
    Write-Host ("playmode_record_status=" + $recordStatus)
    Write-Host ("missing_fields=" + $missingFields.Count)
    Write-Host ("invalid_fields=" + $invalidFields.Count)
    foreach ($field in $stateFields) {
        Write-Host ("- " + $field + ": " + $stateResults[$field])
    }
    Write-Host ("next_action=" + $nextAction)
}

if ($recordStatus -eq "invalid_record") {
    exit 9
}

exit 0
