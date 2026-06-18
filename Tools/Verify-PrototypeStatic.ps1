param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [switch]$Compact
)

$ErrorActionPreference = "Stop"
$failed = $false

function Require-Pattern {
    param(
        [string]$FilePath,
        [string]$Pattern,
        [string]$Label
    )

    if (-not (Test-Path $FilePath)) {
        Write-Host ("[FAIL] " + $Label + " (missing file: " + $FilePath + ")")
        $script:failed = $true
        return
    }

    $hit = Select-String -Path $FilePath -Pattern $Pattern -SimpleMatch
    if ($null -eq $hit) {
        Write-Host ("[FAIL] " + $Label)
        $script:failed = $true
        return
    }

    if (-not $Compact) {
        Write-Host ("[OK] " + $Label)
    }
}

function Forbid-Pattern {
    param(
        [string]$FilePath,
        [string]$Pattern,
        [string]$Label
    )

    if (-not (Test-Path $FilePath)) {
        if (-not $Compact) {
            Write-Host ("[OK] " + $Label + " (file missing)")
        }
        return
    }

    $hit = Select-String -Path $FilePath -Pattern $Pattern -SimpleMatch
    if ($null -ne $hit) {
        Write-Host ("[FAIL] " + $Label)
        $script:failed = $true
        return
    }

    if (-not $Compact) {
        Write-Host ("[OK] " + $Label)
    }
}

$modelFile = Join-Path $ProjectPath "Assets\Scripts\Prototype\FoodTruckRunModel.cs"
$assistFile = Join-Path $ProjectPath "Assets\Scripts\Prototype\FoodTruckPrototypeHud.PendingPlacementAssist.cs"
$cellsFile = Join-Path $ProjectPath "Assets\Scripts\Prototype\FoodTruckPrototypeHud.InventoryCells.cs"
$testFile = Join-Path $ProjectPath "Assets\Tests\EditMode\FoodTruckRunModelTests.cs"

if (-not $Compact) {
    Write-Host "=== Static Guards ==="
}
Require-Pattern -FilePath $modelFile -Pattern "public bool CanAutoMergePendingAtCell(int anchorCellIndex)" -Label "Model exposes auto-merge capability API"
Require-Pattern -FilePath $modelFile -Pattern "private bool TryResolveAutoMergeTarget(" -Label "Model has shared auto-merge resolver"
Require-Pattern -FilePath $assistFile -Pattern "AUTO-MERGE READY" -Label "HUD hint includes auto-merge readiness text"
Require-Pattern -FilePath $assistFile -Pattern "model.CanAutoMergePendingAtCell(anchorCell)" -Label "HUD hover validation checks auto-merge possibility"
Require-Pattern -FilePath $assistFile -Pattern "private int[] ResolvePendingPlacementFeedbackCells(int anchorCell)" -Label "HUD computes feedback cells for merge path"
Require-Pattern -FilePath $cellsFile -Pattern "model.TryGetPendingFootprintCellsPreview(hoverAnchorCell, false, out hoverCells);" -Label "Inventory preview supports occupied overlap footprint"
Require-Pattern -FilePath $testFile -Pattern "CanAutoMergePendingAtCell_WhenSingleMatchingOverlap_ReturnsTrue" -Label "EditMode test includes positive auto-merge probe case"
Require-Pattern -FilePath $testFile -Pattern "CanAutoMergePendingAtCell_WhenOverlapWithTwoBlocks_ReturnsFalse" -Label "EditMode test includes negative multi-overlap case"
Forbid-Pattern -FilePath $assistFile -Pattern "pendingHoverAutoMerge" -Label "No stale removed hover field references"

if ($failed) {
    Write-Host "static_status=failed"
    exit 1
}

Write-Host "static_status=ok"
exit 0
