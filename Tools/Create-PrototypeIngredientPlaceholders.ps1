param(
    [string]$ProjectPath = "D:\uni\zombieFoodcenter",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$outputDir = Join-Path $ProjectPath "Assets\Resources\FoodTruckPrototype\Sprites\Ingredients"
if (-not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
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

function Save-Icon {
    param(
        [string]$Name,
        [scriptblock]$Draw
    )

    $path = Join-Path $outputDir ($Name + ".png")
    if ((Test-Path -LiteralPath $path) -and -not $Force) {
        Write-Host ("[SKIP] " + $Name + " already exists")
        return
    }

    $bitmap = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $shadow = New-Brush 70 0 0 0
    $graphics.FillEllipse($shadow, 42, 190, 172, 24)
    $shadow.Dispose()

    & $Draw $graphics

    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
    Write-Host ("[OK] " + $path)
}

Save-Icon "Onion" {
    param($g)

    $body = New-Brush 255 171 92 210
    $shade = New-Brush 190 119 55 170
    $leaf = New-Brush 255 95 205 124
    $line = New-Pen 220 244 205 255 8
    $outline = New-Pen 210 79 45 120 8
    $g.FillEllipse($body, 54, 64, 148, 150)
    $g.FillEllipse($shade, 65, 76, 120, 132)
    $g.DrawEllipse($outline, 54, 64, 148, 150)
    $g.DrawArc($line, 82, 78, 40, 116, 78, 190)
    $g.DrawArc($line, 125, 78, 42, 116, -88, -190)
    $g.FillEllipse($leaf, 112, 32, 30, 50)
    $body.Dispose(); $shade.Dispose(); $leaf.Dispose(); $line.Dispose(); $outline.Dispose()
}

Save-Icon "Beef" {
    param($g)

    $meat = New-Brush 255 202 66 69
    $fat = New-Brush 255 255 211 174
    $sear = New-Pen 220 115 36 44 10
    $outline = New-Pen 230 82 31 38 8
    $g.FillEllipse($meat, 44, 70, 168, 124)
    $g.DrawEllipse($outline, 44, 70, 168, 124)
    $g.FillEllipse($fat, 94, 96, 58, 46)
    $g.DrawLine($sear, 78, 92, 180, 158)
    $g.DrawLine($sear, 74, 154, 170, 92)
    $meat.Dispose(); $fat.Dispose(); $sear.Dispose(); $outline.Dispose()
}

Save-Icon "Shrimp" {
    param($g)

    $shell = New-Brush 255 255 132 128
    $belly = New-Brush 255 255 196 178
    $line = New-Pen 220 202 69 74 8
    $eye = New-Brush 255 48 24 32
    $g.FillPie($shell, 44, 50, 164, 164, 25, 285)
    $g.FillPie((New-Brush 255 27 31 42), 78, 80, 116, 116, 25, 285)
    $g.FillEllipse($belly, 70, 74, 96, 90)
    for ($i = 0; $i -lt 5; $i++) {
        $x = 76 + ($i * 22)
        $g.DrawLine($line, $x, 82, $x + 20, 154)
    }
    $g.FillEllipse($eye, 166, 86, 12, 12)
    $shell.Dispose(); $belly.Dispose(); $line.Dispose(); $eye.Dispose()
}

Save-Icon "Chili" {
    param($g)

    $chili = New-Brush 255 224 44 48
    $highlight = New-Pen 190 255 149 130 9
    $stem = New-Pen 255 72 166 84 16
    $outline = New-Pen 220 133 26 38 8
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddBezier(54, 122, 82, 42, 166, 46, 204, 78)
    $path.AddBezier(205, 80, 176, 116, 148, 154, 82, 188)
    $path.AddBezier(82, 188, 70, 180, 48, 154, 54, 122)
    $g.FillPath($chili, $path)
    $g.DrawPath($outline, $path)
    $g.DrawCurve($highlight, @(
        [System.Drawing.PointF]::new(82, 112),
        [System.Drawing.PointF]::new(116, 70),
        [System.Drawing.PointF]::new(164, 72)
    ))
    $g.DrawLine($stem, 184, 70, 212, 42)
    $path.Dispose(); $chili.Dispose(); $highlight.Dispose(); $stem.Dispose(); $outline.Dispose()
}

Save-Icon "Rice" {
    param($g)

    $bowl = New-Brush 255 82 137 210
    $rim = New-Brush 255 124 175 236
    $rice = New-Brush 255 250 246 220
    $grain = New-Pen 210 224 217 184 5
    $outline = New-Pen 210 43 80 140 7
    $g.FillEllipse($rice, 58, 58, 140, 94)
    $g.FillRectangle($bowl, 54, 122, 148, 48)
    $g.FillPie($bowl, 54, 114, 148, 92, 0, 180)
    $g.FillEllipse($rim, 50, 112, 156, 36)
    $g.DrawEllipse($outline, 50, 112, 156, 36)
    $g.DrawArc($outline, 54, 114, 148, 92, 0, 180)
    $g.DrawLine($grain, 94, 82, 116, 96)
    $g.DrawLine($grain, 130, 76, 154, 94)
    $bowl.Dispose(); $rim.Dispose(); $rice.Dispose(); $grain.Dispose(); $outline.Dispose()
}

Save-Icon "Seaweed" {
    param($g)

    $sheet = New-Brush 255 34 128 92
    $dark = New-Brush 255 22 78 67
    $shine = New-Pen 170 112 222 165 7
    $outline = New-Pen 230 17 55 47 8
    $g.FillRectangle($sheet, 66, 46, 124, 164)
    $g.FillRectangle($dark, 82, 62, 92, 132)
    $g.DrawRectangle($outline, 66, 46, 124, 164)
    $g.DrawLine($shine, 94, 74, 154, 74)
    $g.DrawLine($shine, 94, 110, 154, 110)
    $g.DrawLine($shine, 94, 146, 154, 146)
    $sheet.Dispose(); $dark.Dispose(); $shine.Dispose(); $outline.Dispose()
}

Save-Icon "Garlic" {
    param($g)

    $bulb = New-Brush 255 244 235 198
    $shade = New-Brush 255 222 204 164
    $sprout = New-Pen 255 101 177 86 12
    $line = New-Pen 210 180 155 119 6
    $outline = New-Pen 210 134 112 84 7
    $g.FillEllipse($bulb, 64, 80, 128, 126)
    $g.FillEllipse($shade, 96, 88, 64, 118)
    $g.DrawEllipse($outline, 64, 80, 128, 126)
    $g.DrawLine($line, 112, 94, 106, 190)
    $g.DrawLine($line, 142, 94, 150, 190)
    $g.DrawLine($sprout, 126, 86, 102, 44)
    $g.DrawLine($sprout, 130, 84, 160, 48)
    $bulb.Dispose(); $shade.Dispose(); $sprout.Dispose(); $line.Dispose(); $outline.Dispose()
}

Save-Icon "Pork" {
    param($g)

    $ham = New-Brush 255 238 114 138
    $fat = New-Brush 255 255 204 185
    $bone = New-Brush 255 238 221 185
    $outline = New-Pen 225 145 54 82 8
    $g.FillEllipse($ham, 50, 78, 154, 116)
    $g.DrawEllipse($outline, 50, 78, 154, 116)
    $g.FillEllipse($fat, 84, 102, 62, 48)
    $g.FillRectangle($bone, 156, 126, 52, 24)
    $g.FillEllipse($bone, 196, 114, 34, 34)
    $g.FillEllipse($bone, 196, 142, 34, 34)
    $ham.Dispose(); $fat.Dispose(); $bone.Dispose(); $outline.Dispose()
}

Write-Host "ingredient_placeholder_status=ok"
