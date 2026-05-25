param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [switch]$Strict,
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$checks = New-Object System.Collections.Generic.List[object]

function Join-ProjectPath {
    param([string]$RelativePath)

    return Join-Path $ProjectPath $RelativePath
}

function Test-AssetFile {
    param([string]$RelativePath)

    return Test-Path -LiteralPath (Join-ProjectPath $RelativePath) -PathType Leaf
}

function Find-FirstAsset {
    param(
        [string]$RelativeDirectory,
        [string[]]$FileNames
    )

    foreach ($fileName in $FileNames) {
        $relativePath = Join-Path $RelativeDirectory $fileName
        if (Test-AssetFile $relativePath) {
            return $relativePath
        }
    }

    return $null
}

function Add-Check {
    param(
        [string]$Category,
        [string]$Label,
        [string]$Kind,
        [bool]$Present,
        [string]$Path,
        [string]$Fallback,
        [string]$Recommendation,
        [string[]]$ExpectedSizes = @()
    )

    $diagnostics = Get-PngDiagnostics -RelativePath $Path -ExpectedSizes $ExpectedSizes
    $status = if ($Present) {
        "ok"
    } elseif ($Kind -eq "runtime") {
        "missing_runtime"
    } elseif ($Kind -eq "art") {
        "missing_art"
    } else {
        "missing_planned"
    }
    $checks.Add([pscustomobject]@{
        category = $Category
        label = $Label
        kind = $Kind
        status = $status
        path = $Path
        has_meta = $diagnostics.has_meta
        meta_status = $diagnostics.meta_status
        fallback = $Fallback
        recommendation = $Recommendation
        actual_size = $diagnostics.actual_size
        expected_sizes = $diagnostics.expected_sizes
        dimension_status = $diagnostics.dimension_status
        has_alpha = $diagnostics.has_alpha
        alpha_status = $diagnostics.alpha_status
        diagnostic = $diagnostics.diagnostic
    }) | Out-Null
}

function Get-PngDiagnostics {
    param(
        [string]$RelativePath,
        [string[]]$ExpectedSizes = @()
    )

    $fullPath = Join-ProjectPath $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        return [pscustomobject]@{
            actual_size = $null
            expected_sizes = $ExpectedSizes
            has_meta = $false
            meta_status = "not_checked"
            dimension_status = "not_checked"
            has_alpha = $null
            alpha_status = "not_checked"
            diagnostic = "missing"
        }
    }

    if (-not $RelativePath.EndsWith(".png", [System.StringComparison]::OrdinalIgnoreCase)) {
        return [pscustomobject]@{
            actual_size = $null
            expected_sizes = $ExpectedSizes
            has_meta = (Test-Path -LiteralPath ($fullPath + ".meta") -PathType Leaf)
            meta_status = if (Test-Path -LiteralPath ($fullPath + ".meta") -PathType Leaf) { "ok" } else { "warning" }
            dimension_status = "not_checked"
            has_alpha = $null
            alpha_status = "not_checked"
            diagnostic = "not_png"
        }
    }

    $image = $null
    try {
        $image = [System.Drawing.Image]::FromFile($fullPath)
        $actualSize = [string]$image.Width + "x" + [string]$image.Height
        $hasMeta = Test-Path -LiteralPath ($fullPath + ".meta") -PathType Leaf
        $dimensionStatus = if ($ExpectedSizes.Count -eq 0) {
            "not_checked"
        } elseif ($ExpectedSizes -contains $actualSize) {
            "ok"
        } else {
            "warning"
        }

        $hasAlpha = (($image.Flags -band [int][System.Drawing.Imaging.ImageFlags]::HasAlpha) -ne 0)
        return [pscustomobject]@{
            actual_size = $actualSize
            expected_sizes = $ExpectedSizes
            has_meta = $hasMeta
            meta_status = if ($hasMeta) { "ok" } else { "warning" }
            dimension_status = $dimensionStatus
            has_alpha = $hasAlpha
            alpha_status = if ($hasAlpha) { "ok" } else { "warning" }
            diagnostic = "ok"
        }
    }
    catch {
        return [pscustomobject]@{
            actual_size = $null
            expected_sizes = $ExpectedSizes
            has_meta = (Test-Path -LiteralPath ($fullPath + ".meta") -PathType Leaf)
            meta_status = if (Test-Path -LiteralPath ($fullPath + ".meta") -PathType Leaf) { "ok" } else { "warning" }
            dimension_status = "warning"
            has_alpha = $null
            alpha_status = "warning"
            diagnostic = "image_read_failed: " + $_.Exception.Message
        }
    }
    finally {
        if ($null -ne $image) {
            $image.Dispose()
        }
    }
}

function Add-AnyOfCheck {
    param(
        [string]$Category,
        [string]$Label,
        [string]$Kind,
        [string]$RelativeDirectory,
        [string[]]$FileNames,
        [string]$Fallback,
        [string]$Recommendation,
        [string[]]$ExpectedSizes = @()
    )

    $found = Find-FirstAsset -RelativeDirectory $RelativeDirectory -FileNames $FileNames
    $expected = Join-Path $RelativeDirectory ($FileNames[0])
    Add-Check -Category $Category -Label $Label -Kind $Kind -Present ($null -ne $found) -Path ($(if ($found) { $found } else { $expected })) -Fallback $Fallback -Recommendation $Recommendation -ExpectedSizes $ExpectedSizes
}

function Add-RequiredFileCheck {
    param(
        [string]$Category,
        [string]$Label,
        [string]$Kind,
        [string]$RelativePath,
        [string]$Fallback,
        [string]$Recommendation,
        [string[]]$ExpectedSizes = @()
    )

    Add-Check -Category $Category -Label $Label -Kind $Kind -Present (Test-AssetFile $RelativePath) -Path $RelativePath -Fallback $Fallback -Recommendation $Recommendation -ExpectedSizes $ExpectedSizes
}

$truckDir = "Assets\Resources\FoodTruckPrototype\Sprites\Truck"
$kitchenDir = "Assets\Resources\FoodTruckPrototype\Sprites\KitchenModules"
$ingredientDir = "Assets\Resources\FoodTruckPrototype\Sprites\Ingredients"
$errorDir = "Assets\Resources\FoodTruckPrototype\Sprites\PlacementErrors"
$vfxDir = "Assets\Resources\FoodTruckPrototype\VFX"
$audioDir = "Assets\Resources\FoodTruckPrototype\Audio"

Add-AnyOfCheck -Category "final_art" -Label "Food truck lane sprite" -Kind "art" -RelativeDirectory $truckDir -FileNames @("FoodTruck.png", "food_truck.png", "Truck.png", "truck.png") -Fallback "Lane marker uses placeholder TRK text." -Recommendation "512x256 or 384x192 transparent side-view PNG facing right." -ExpectedSizes @("512x256", "384x192")
Add-AnyOfCheck -Category "final_art" -Label "Kitchen module block sprite" -Kind "art" -RelativeDirectory $kitchenDir -FileNames @("KitchenModule.png", "kitchen_module.png", "Kitchen.png", "kitchen.png") -Fallback "Block cells use colored rectangles and labels." -Recommendation "256x256 transparent PNG, readable inside a 3x3 block cell." -ExpectedSizes @("256x256")

$ingredientNames = @("Onion", "Beef", "Shrimp", "Chili", "Rice", "Seaweed", "Garlic", "Pork")
foreach ($ingredient in $ingredientNames) {
    Add-RequiredFileCheck -Category "final_art" -Label ("Ingredient icon: " + $ingredient) -Kind "art" -RelativePath (Join-Path $ingredientDir ($ingredient + ".png")) -Fallback "Draw cards and cells use ingredient text/color fallback." -Recommendation "256x256 transparent PNG, edible silhouette inside central 80% safe area." -ExpectedSizes @("256x256")
}

$placementErrors = @(
    "placement_fail_no_pending.png",
    "placement_fail_invalid_anchor.png",
    "placement_fail_out_of_bounds.png",
    "placement_fail_occupied.png"
)
foreach ($fileName in $placementErrors) {
    Add-RequiredFileCheck -Category "runtime_feedback" -Label ("Placement fail icon: " + $fileName) -Kind "runtime" -RelativePath (Join-Path $errorDir $fileName) -Fallback "Placement failure still has text feedback, but reason icon is absent." -Recommendation "Keep 128x128 transparent PNG, no embedded text." -ExpectedSizes @("128x128", "256x256")
}

$vfxFiles = @(
    "place_success_pop.png",
    "place_fail_flash.png",
    "combo_burst.png",
    "overheat_spike.png",
    "heat_warning_ring.png"
)
foreach ($fileName in $vfxFiles) {
    Add-RequiredFileCheck -Category "runtime_feedback" -Label ("Prototype VFX: " + $fileName) -Kind "runtime" -RelativePath (Join-Path $vfxDir $fileName) -Fallback "HUD falls back to simple color flash where possible." -Recommendation "Keep 256x256 transparent PNG until final VFX replaces it." -ExpectedSizes @("256x256")
}

$plannedVfxFiles = @(
    "attack_source_trail.png",
    "hit_impact_pop.png",
    "lane_leak_warning.png",
    "release_reward_pulse.png",
    "wave_payoff_pulse.png"
)
foreach ($fileName in $plannedVfxFiles) {
    Add-RequiredFileCheck -Category "planned_feedback" -Label ("Planned VFX: " + $fileName) -Kind "planned" -RelativePath (Join-Path $vfxDir $fileName) -Fallback "Current prototype uses text floaters, lane flash, and generic prototype VFX." -Recommendation "Future 256x256 transparent PNG or sprite-sheet frame set. Keep no embedded text so labels can stay localized/readable." -ExpectedSizes @("256x256", "512x512")
}

$plannedAudioFiles = @(
    "placement_success.wav",
    "placement_fail.wav",
    "wave_start.wav",
    "heat_warning.wav",
    "overheat_spike.wav",
    "combo_ready.wav",
    "recipe_activate.wav",
    "release_reward.wav",
    "wave_payoff.wav"
)
foreach ($fileName in $plannedAudioFiles) {
    Add-RequiredFileCheck -Category "planned_audio" -Label ("Planned SFX: " + $fileName) -Kind "planned" -RelativePath (Join-Path $audioDir $fileName) -Fallback "Prototype can run silently or with existing imported fallback audio if available." -Recommendation "Short WAV, normalized for UI/gameplay clarity; map each cue to a named rhythm beat." -ExpectedSizes @()
}

$missingRuntime = @($checks | Where-Object { $_.status -eq "missing_runtime" })
$missingArt = @($checks | Where-Object { $_.status -eq "missing_art" })
$missingPlanned = @($checks | Where-Object { $_.status -eq "missing_planned" })
$missingMeta = @($checks | Where-Object { $_.status -eq "ok" -and $_.meta_status -eq "warning" })
$diagnosticWarnings = @($checks | Where-Object { $_.dimension_status -eq "warning" -or $_.alpha_status -eq "warning" -or $_.meta_status -eq "warning" })
$assetStatus = if ($missingRuntime.Count -gt 0) {
    "failed_runtime_assets"
} elseif ($missingArt.Count -gt 0) {
    "needs_art"
} elseif ($missingMeta.Count -gt 0) {
    "needs_meta"
} else {
    "ok"
}

if ($JsonOnly) {
    [pscustomobject]@{
        asset_status = $assetStatus
        strict = [bool]$Strict
        runtime_required_missing = $missingRuntime.Count
        final_art_missing = $missingArt.Count
        planned_missing = $missingPlanned.Count
        missing_meta = $missingMeta.Count
        diagnostic_warnings = $diagnosticWarnings.Count
        checks = $checks
    } | ConvertTo-Json -Depth 6 -Compress
} else {
    Write-Host ("asset_status=" + $assetStatus)
    Write-Host ("runtime_required_missing=" + $missingRuntime.Count)
    Write-Host ("final_art_missing=" + $missingArt.Count)
    Write-Host ("planned_missing=" + $missingPlanned.Count)
    Write-Host ("missing_meta=" + $missingMeta.Count)
    Write-Host ("diagnostic_warnings=" + $diagnosticWarnings.Count)

    foreach ($check in $checks) {
        if ($check.status -eq "ok") {
            Write-Host ("[OK] " + $check.label + " -> " + $check.path)
            if ($check.dimension_status -eq "warning" -or $check.alpha_status -eq "warning" -or $check.meta_status -eq "warning") {
                Write-Host ("  [WARN] size=" + $check.actual_size + " expected=" + ($check.expected_sizes -join ",") + " alpha=" + $check.alpha_status + " meta=" + $check.meta_status)
            }
        } else {
            Write-Host ("[MISSING] " + $check.label)
            Write-Host ("  path=" + $check.path)
            Write-Host ("  fallback=" + $check.fallback)
            Write-Host ("  recommendation=" + $check.recommendation)
        }
    }
}

if ($missingRuntime.Count -gt 0) {
    exit 1
}

if ($Strict -and ($missingArt.Count -gt 0 -or $missingMeta.Count -gt 0)) {
    exit 1
}

exit 0
