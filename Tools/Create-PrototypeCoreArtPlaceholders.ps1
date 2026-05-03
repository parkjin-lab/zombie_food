param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

function Ensure-Directory {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function New-Brush {
    param([int]$A, [int]$R, [int]$G, [int]$B)

    return New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb($A, $R, $G, $B))
}

function New-Pen {
    param([int]$A, [int]$R, [int]$G, [int]$B, [float]$Width)

    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb($A, $R, $G, $B), $Width)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    return $pen
}

function Save-TransparentPng {
    param(
        [string]$Path,
        [int]$Width,
        [int]$Height,
        [scriptblock]$Draw
    )

    if ((Test-Path -LiteralPath $Path) -and -not $Force) {
        Write-Host ("[SKIP] " + $Path)
        return
    }

    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)

    & $Draw $graphics

    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
    Write-Host ("[OK] " + $Path)
}

$truckDir = Join-Path $ProjectPath "Assets\Resources\FoodTruckPrototype\Sprites\Truck"
$kitchenDir = Join-Path $ProjectPath "Assets\Resources\FoodTruckPrototype\Sprites\KitchenModules"
Ensure-Directory $truckDir
Ensure-Directory $kitchenDir

$truckPath = Join-Path $truckDir "FoodTruck.png"
Save-TransparentPng -Path $truckPath -Width 512 -Height 256 -Draw {
    param($g)

    $shadow = New-Brush 85 0 0 0
    $body = New-Brush 255 235 75 64
    $bodyDark = New-Brush 255 178 48 54
    $cab = New-Brush 255 246 170 75
    $window = New-Brush 230 102 198 238
    $panel = New-Brush 255 251 229 168
    $wheel = New-Brush 255 30 35 45
    $rim = New-Brush 255 218 226 235
    $outline = New-Pen 230 33 38 52 8
    $stripe = New-Pen 220 255 238 180 10

    $g.FillEllipse($shadow, 72, 198, 360, 28)
    $g.FillRectangle($bodyDark, 104, 112, 256, 72)
    $g.FillRectangle($body, 82, 88, 274, 88)
    $g.FillRectangle($cab, 342, 104, 76, 72)
    $g.FillPolygon($cab, @(
        [System.Drawing.Point]::new(342, 104),
        [System.Drawing.Point]::new(374, 74),
        [System.Drawing.Point]::new(418, 104)
    ))
    $g.FillRectangle($window, 366, 94, 38, 28)
    $g.FillRectangle($panel, 124, 106, 108, 44)
    $g.DrawLine($stripe, 96, 164, 352, 164)
    $g.DrawRectangle($outline, 82, 88, 274, 88)
    $g.DrawRectangle($outline, 342, 104, 76, 72)
    $g.DrawPolygon($outline, @(
        [System.Drawing.Point]::new(342, 104),
        [System.Drawing.Point]::new(374, 74),
        [System.Drawing.Point]::new(418, 104)
    ))
    $g.FillEllipse($wheel, 128, 160, 56, 56)
    $g.FillEllipse($wheel, 340, 160, 56, 56)
    $g.FillEllipse($rim, 146, 178, 20, 20)
    $g.FillEllipse($rim, 358, 178, 20, 20)

    $shadow.Dispose(); $body.Dispose(); $bodyDark.Dispose(); $cab.Dispose(); $window.Dispose()
    $panel.Dispose(); $wheel.Dispose(); $rim.Dispose(); $outline.Dispose(); $stripe.Dispose()
}

$kitchenPath = Join-Path $kitchenDir "KitchenModule.png"
Save-TransparentPng -Path $kitchenPath -Width 256 -Height 256 -Draw {
    param($g)

    $shadow = New-Brush 75 0 0 0
    $case = New-Brush 255 72 90 118
    $front = New-Brush 255 92 118 154
    $stove = New-Brush 255 36 42 58
    $flame = New-Brush 255 255 127 66
    $flameCore = New-Brush 255 255 221 112
    $metal = New-Brush 255 202 214 221
    $accent = New-Pen 230 255 217 116 8
    $outline = New-Pen 230 24 29 42 8

    $g.FillEllipse($shadow, 48, 196, 160, 24)
    $g.FillRectangle($case, 54, 60, 148, 136)
    $g.FillRectangle($front, 68, 86, 120, 96)
    $g.DrawRectangle($outline, 54, 60, 148, 136)
    $g.FillRectangle($stove, 76, 72, 104, 34)
    $g.FillEllipse($metal, 88, 82, 22, 12)
    $g.FillEllipse($metal, 146, 82, 22, 12)
    $g.DrawLine($accent, 78, 132, 178, 132)
    $g.FillPie($flame, 104, 110, 48, 66, 210, 120)
    $g.FillPie($flameCore, 116, 126, 24, 38, 210, 120)
    $g.FillRectangle($metal, 78, 154, 100, 16)
    $g.DrawRectangle($outline, 68, 86, 120, 96)

    $shadow.Dispose(); $case.Dispose(); $front.Dispose(); $stove.Dispose(); $flame.Dispose()
    $flameCore.Dispose(); $metal.Dispose(); $accent.Dispose(); $outline.Dispose()
}

Write-Host "core_art_placeholder_status=ok"
