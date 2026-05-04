param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"

function Clamp01 {
    param([float]$Value)

    if ($Value -lt 0.0) { return 0.0 }
    if ($Value -gt 1.0) { return 1.0 }
    return $Value
}

function Lerp {
    param(
        [float]$A,
        [float]$B,
        [float]$T
    )

    return $A + (($B - $A) * $T)
}

function Get-GameplayFocusLayout {
    param(
        [float]$ViewportWidth,
        [float]$ViewportHeight,
        [bool]$HasPlacementContext,
        [bool]$DragFocus,
        [bool]$MinimalCombatStripRequested
    )

    $width = [Math]::Max(1.0, $ViewportWidth)
    $height = [Math]::Max(1.0, $ViewportHeight)
    $portrait01 = Clamp01 (($height / $width - 1.4) / 1.2)
    $combatOnlyFocus = (-not $DragFocus) -and (-not $HasPlacementContext)
    $minimalCombatStrip = $combatOnlyFocus -and $MinimalCombatStripRequested

    if ($DragFocus) {
        $topStartFocus = Lerp 0.91 0.94 $portrait01
    }
    elseif ($minimalCombatStrip) {
        $topStartFocus = Lerp 0.94 0.965 $portrait01
    }
    elseif ($combatOnlyFocus) {
        $topStartFocus = Lerp 0.38 0.42 $portrait01
    }
    else {
        $topStartFocus = Lerp 0.62 0.66 $portrait01
    }

    if ($DragFocus) {
        $bottomTopFocus = Lerp 0.16 0.20 $portrait01
    }
    elseif ($HasPlacementContext) {
        $bottomTopFocus = Lerp 0.54 0.58 $portrait01
    }
    else {
        if ($minimalCombatStrip) {
            $bottomTopFocus = Lerp 0.10 0.14 $portrait01
        }
        else {
            $bottomTopFocus = Lerp 0.13 0.16 $portrait01
        }
    }

    if ($DragFocus) {
        $focusGap = 0.24
    }
    elseif ($minimalCombatStrip) {
        $focusGap = 0.10
    }
    else {
        $focusGap = 0.04
    }

    if ($bottomTopFocus -gt ($topStartFocus - $focusGap)) {
        $bottomTopFocus = $topStartFocus - $focusGap
    }

    return [pscustomobject]@{
        top_start01 = [Math]::Round($topStartFocus, 4)
        bottom_top01 = [Math]::Round($bottomTopFocus, 4)
        center_gap01 = [Math]::Round(($topStartFocus - $bottomTopFocus), 4)
        top_panel01 = [Math]::Round((1.0 - $topStartFocus), 4)
        bottom_panel01 = [Math]::Round($bottomTopFocus, 4)
        minimal_combat_strip = [bool]$minimalCombatStrip
    }
}

function Test-Range {
    param(
        [object]$Value,
        [object]$Min,
        [object]$Max
    )

    if ($null -ne $Min -and $Value -lt $Min) { return $false }
    if ($null -ne $Max -and $Value -gt $Max) { return $false }
    return $true
}

function Add-SourceContainsCheck {
    param(
        [System.Collections.Generic.List[object]]$Target,
        [string]$Name,
        [string]$Source,
        [string]$Needle
    )

    $ok = -not [string]::IsNullOrEmpty($Source) -and $Source.Contains($Needle)
    $Target.Add([pscustomobject]@{
        name = $Name
        status = if ($ok) { "ok" } else { "fail" }
        expected = $Needle
    })
}

$hudPath = Join-Path $ProjectPath "Assets\Scripts\Prototype\FoodTruckPrototypeHud.cs"
$uiBootstrapPath = Join-Path $ProjectPath "Assets\Scripts\Prototype\FoodTruckPrototypeHud.UiBootstrap.cs"
$hudSource = if (Test-Path -LiteralPath $hudPath) { [System.IO.File]::ReadAllText($hudPath) } else { "" }
$uiBootstrapSource = if (Test-Path -LiteralPath $uiBootstrapPath) { [System.IO.File]::ReadAllText($uiBootstrapPath) } else { "" }

$viewports = @(
    [pscustomobject]@{ name = "reference_portrait"; width = 1080; height = 1920 },
    [pscustomobject]@{ name = "narrow_phone"; width = 390; height = 844 },
    [pscustomobject]@{ name = "small_phone"; width = 360; height = 800 },
    [pscustomobject]@{ name = "landscape_tablet"; width = 1024; height = 768 }
)

$scenarios = @(
    [pscustomobject]@{
        name = "draw_or_pending_placement"
        has_placement = $true
        drag_focus = $false
        minimal_requested = $false
        min_top_panel01 = 0.30
        max_top_panel01 = $null
        min_bottom_panel01 = 0.50
        max_bottom_panel01 = 0.62
        min_center_gap01 = 0.04
        max_center_gap01 = $null
    },
    [pscustomobject]@{
        name = "drag_focus_placement"
        has_placement = $true
        drag_focus = $true
        minimal_requested = $false
        min_top_panel01 = $null
        max_top_panel01 = 0.10
        min_bottom_panel01 = 0.14
        max_bottom_panel01 = 0.22
        min_center_gap01 = 0.70
        max_center_gap01 = $null
    },
    [pscustomobject]@{
        name = "combat_overview"
        has_placement = $false
        drag_focus = $false
        minimal_requested = $false
        min_top_panel01 = 0.58
        max_top_panel01 = $null
        min_bottom_panel01 = 0.12
        max_bottom_panel01 = 0.18
        min_center_gap01 = 0.20
        max_center_gap01 = $null
    },
    [pscustomobject]@{
        name = "minimal_combat_strip"
        has_placement = $false
        drag_focus = $false
        minimal_requested = $true
        min_top_panel01 = $null
        max_top_panel01 = 0.08
        min_bottom_panel01 = $null
        max_bottom_panel01 = 0.16
        min_center_gap01 = 0.75
        max_center_gap01 = $null
    }
)

$checks = New-Object System.Collections.Generic.List[object]

foreach ($viewport in $viewports) {
    foreach ($scenario in $scenarios) {
        $metrics = Get-GameplayFocusLayout `
            -ViewportWidth $viewport.width `
            -ViewportHeight $viewport.height `
            -HasPlacementContext $scenario.has_placement `
            -DragFocus $scenario.drag_focus `
            -MinimalCombatStripRequested $scenario.minimal_requested

        $failures = New-Object System.Collections.Generic.List[string]

        if (-not (Test-Range $metrics.top_panel01 $scenario.min_top_panel01 $scenario.max_top_panel01)) {
            $failures.Add("top_panel01_out_of_range")
        }

        if (-not (Test-Range $metrics.bottom_panel01 $scenario.min_bottom_panel01 $scenario.max_bottom_panel01)) {
            $failures.Add("bottom_panel01_out_of_range")
        }

        if (-not (Test-Range $metrics.center_gap01 $scenario.min_center_gap01 $scenario.max_center_gap01)) {
            $failures.Add("center_gap01_out_of_range")
        }

        $checks.Add([pscustomobject]@{
            viewport = $viewport.name
            width = $viewport.width
            height = $viewport.height
            scenario = $scenario.name
            status = if ($failures.Count -eq 0) { "ok" } else { "fail" }
            failures = $failures.ToArray()
            metrics = $metrics
        })
    }
}

$missingFiles = @()
if (-not (Test-Path -LiteralPath $hudPath)) { $missingFiles += "FoodTruckPrototypeHud.cs" }
if (-not (Test-Path -LiteralPath $uiBootstrapPath)) { $missingFiles += "FoodTruckPrototypeHud.UiBootstrap.cs" }

$sourceSyncChecks = New-Object System.Collections.Generic.List[object]
Add-SourceContainsCheck $sourceSyncChecks "calculate_method_exists" $hudSource "public static GameplayFocusLayoutMetrics CalculateGameplayFocusLayout("
Add-SourceContainsCheck $sourceSyncChecks "portrait_normalization_constants" $hudSource "float portrait01 = Mathf.Clamp01((height / width - 1.4f) / 1.2f);"
Add-SourceContainsCheck $sourceSyncChecks "drag_top_lerp_constants" $hudSource "topStartFocus = Mathf.Lerp(0.91f, 0.94f, portrait01);"
Add-SourceContainsCheck $sourceSyncChecks "combat_top_lerp_constants" $hudSource "topStartFocus = Mathf.Lerp(0.38f, 0.42f, portrait01);"
Add-SourceContainsCheck $sourceSyncChecks "placement_top_lerp_constants" $hudSource "topStartFocus = Mathf.Lerp(0.62f, 0.66f, portrait01);"
Add-SourceContainsCheck $sourceSyncChecks "placement_bottom_lerp_constants" $hudSource "bottomTopFocus = Mathf.Lerp(0.54f, 0.58f, portrait01);"
Add-SourceContainsCheck $sourceSyncChecks "combat_bottom_lerp_constants" $hudSource "Mathf.Lerp(0.13f, 0.16f, portrait01);"
Add-SourceContainsCheck $sourceSyncChecks "focus_gap_constants" $hudSource "float focusGap = dragFocus ? 0.24f : (minimalCombatStrip ? 0.10f : 0.04f);"
Add-SourceContainsCheck $sourceSyncChecks "combat_inventory_grid_hidden_without_placement" $hudSource "bool showInventoryGrid = !gameplayFocusHud || hasPlacementContext;"
Add-SourceContainsCheck $sourceSyncChecks "layout_context_changes_reapply_panel_layout" $hudSource "bool layoutContextChanged = !gameplayHudLayoutContextInitialized"
Add-SourceContainsCheck $sourceSyncChecks "apply_layout_uses_calculator" $uiBootstrapSource "GameplayFocusLayoutMetrics focusLayout = CalculateGameplayFocusLayout("
Add-SourceContainsCheck $sourceSyncChecks "bottom_panel_uses_bottom_top_focus" $uiBootstrapSource "Stretch(bottomPanelRect, new Vector2(0f, 0f), new Vector2(1f, bottomTopFocus), bottomOffsetMin, bottomOffsetMax);"

$failedChecks = @($checks | Where-Object { $_.status -ne "ok" })
$failedSourceSyncChecks = @($sourceSyncChecks | Where-Object { $_.status -ne "ok" })
$sourceSyncStatus = if ($failedSourceSyncChecks.Count -eq 0) { "ok" } else { "failed" }
$layoutStatus = if ($missingFiles.Count -gt 0) {
    "missing_source"
}
elseif ($sourceSyncStatus -ne "ok") {
    "source_drift"
}
elseif ($failedChecks.Count -gt 0) {
    "failed"
}
else {
    "ok"
}

$result = [ordered]@{
    layout_status = $layoutStatus
    source_files_present = ($missingFiles.Count -eq 0)
    missing_files = $missingFiles
    viewport_count = $viewports.Count
    scenario_count = $scenarios.Count
    failed_checks = $failedChecks.Count
    source_sync_status = $sourceSyncStatus
    source_sync_failed_checks = $failedSourceSyncChecks.Count
    source_sync_checks = $sourceSyncChecks.ToArray()
    checks = $checks.ToArray()
    notes = @(
        "This is a numeric guard for FoodTruckPrototypeHud.CalculateGameplayFocusLayout.",
        "The source sync checks intentionally fail if core C# layout constants change without updating this verifier.",
        "It does not replace Unity Play Mode visual verification."
    )
}

if ($JsonOnly) {
    $result | ConvertTo-Json -Depth 8 -Compress | Write-Host
}
else {
    Write-Host ("layout_status=" + $layoutStatus)
    Write-Host ("viewport_count=" + $viewports.Count)
    Write-Host ("scenario_count=" + $scenarios.Count)
    Write-Host ("failed_checks=" + $failedChecks.Count)
    Write-Host ("source_sync_status=" + $sourceSyncStatus)
    Write-Host ("source_sync_failed_checks=" + $failedSourceSyncChecks.Count)
    foreach ($check in $checks) {
        Write-Host ("- " + $check.viewport + "/" + $check.scenario + ": " + $check.status + " center_gap01=" + $check.metrics.center_gap01 + " top_panel01=" + $check.metrics.top_panel01 + " bottom_panel01=" + $check.metrics.bottom_panel01)
    }
}

if ($layoutStatus -ne "ok") {
    exit 6
}

exit 0
