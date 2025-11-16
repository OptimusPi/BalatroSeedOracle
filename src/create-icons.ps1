# Create proper multi-size .ICO file from PNG
Add-Type -AssemblyName System.Drawing

$pngPath = "X:\BalatroSeedOracle\src\BalatroSeedOracle-App-Icon.png"
$icoPath = "X:\BalatroSeedOracle\src\BalatroSeedOracle.ico"

Write-Host "Loading PNG: $pngPath"
$srcImage = [System.Drawing.Image]::FromFile($pngPath)
Write-Host "Source image: $($srcImage.Width)x$($srcImage.Height)"

# Create ICO with standard sizes: 16, 32, 48, 256
$sizes = @(16, 32, 48, 256)
$iconSizes = @()

foreach ($size in $sizes) {
    Write-Host "Creating ${size}x${size} icon..."
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.DrawImage($srcImage, 0, 0, $size, $size)
    $graphics.Dispose()
    $iconSizes += $bitmap
}

$srcImage.Dispose()

# Save as ICO
Write-Host "Saving multi-size ICO to: $icoPath"
$iconSizes[0].Save($icoPath, [System.Drawing.Imaging.ImageFormat]::Icon)

foreach ($icon in $iconSizes) {
    $icon.Dispose()
}

Write-Host "Icon created successfully with sizes: $($sizes -join ', ')"
