[CmdletBinding()]
param([string]$OutputPath)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'assets\ST_Holmes.ico'
}
Add-Type -AssemblyName System.Drawing

$frames = [System.Collections.Generic.List[byte[]]]::new()
foreach ($size in @(16, 24, 32, 48, 64, 128, 256)) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $scale = $size / 64.0

        $background = [System.Drawing.SolidBrush]::new(
            [System.Drawing.Color]::FromArgb(255, 15, 23, 42))
        $lensFill = [System.Drawing.SolidBrush]::new(
            [System.Drawing.Color]::FromArgb(255, 30, 41, 59))
        $cyan = [System.Drawing.Pen]::new(
            [System.Drawing.Color]::FromArgb(255, 56, 189, 248), 5 * $scale)
        $green = [System.Drawing.Pen]::new(
            [System.Drawing.Color]::FromArgb(255, 34, 197, 94), 4 * $scale)
        try {
            $cyan.StartCap = $cyan.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $green.StartCap = $green.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $graphics.FillEllipse($background, 1 * $scale, 1 * $scale, 62 * $scale, 62 * $scale)
            $graphics.FillEllipse($lensFill, 11 * $scale, 9 * $scale, 34 * $scale, 34 * $scale)
            $graphics.DrawEllipse($cyan, 11 * $scale, 9 * $scale, 34 * $scale, 34 * $scale)
            $graphics.DrawLine($cyan, 40 * $scale, 39 * $scale, 53 * $scale, 52 * $scale)
            $points = [System.Drawing.PointF[]]@(
                [System.Drawing.PointF]::new(17 * $scale, 30 * $scale),
                [System.Drawing.PointF]::new(23 * $scale, 30 * $scale),
                [System.Drawing.PointF]::new(27 * $scale, 21 * $scale),
                [System.Drawing.PointF]::new(33 * $scale, 35 * $scale),
                [System.Drawing.PointF]::new(40 * $scale, 25 * $scale)
            )
            $graphics.DrawLines($green, $points)
        }
        finally {
            $background.Dispose()
            $lensFill.Dispose()
            $cyan.Dispose()
            $green.Dispose()
        }

        $stream = [System.IO.MemoryStream]::new()
        try {
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            $frames.Add($stream.ToArray())
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$directory = Split-Path -Parent $OutputPath
[System.IO.Directory]::CreateDirectory($directory) | Out-Null
$file = [System.IO.File]::Create($OutputPath)
$writer = [System.IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$frames.Count)
    $offset = 6 + (16 * $frames.Count)
    for ($index = 0; $index -lt $frames.Count; $index++) {
        $size = @(16, 24, 32, 48, 64, 128, 256)[$index]
        $writer.Write([byte]($(if ($size -eq 256) { 0 } else { $size })))
        $writer.Write([byte]($(if ($size -eq 256) { 0 } else { $size })))
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$index].Length)
        $writer.Write([uint32]$offset)
        $offset += $frames[$index].Length
    }
    foreach ($frame in $frames) {
        $writer.Write($frame)
    }
}
finally {
    $writer.Dispose()
    $file.Dispose()
}

Write-Host "Icono creado en: $OutputPath"
