# Properly create multi-size ICO from PNG using magick (ImageMagick)
$pngPath = "X:\BalatroSeedOracle\src\BalatroSeedOracle-App-Icon.png"
$icoPath = "X:\BalatroSeedOracle\src\BalatroSeedOracle.ico"

# Check if ImageMagick is installed
try {
    $magickVersion = magick --version 2>&1
    Write-Host "ImageMagick found!"

    # Create proper ICO with multiple sizes: 16, 32, 48, 256
    Write-Host "Creating multi-size ICO from $pngPath..."
    magick convert "$pngPath" -define icon:auto-resize=256,48,32,16 "$icoPath"

    Write-Host "Icon created successfully!"
    Write-Host "File size: $((Get-Item $icoPath).Length) bytes"
} catch {
    Write-Host "ERROR: ImageMagick not found. Trying alternative method..."

    # Alternative: Use .NET to create proper multi-frame ICO
    Add-Type -AssemblyName System.Drawing

    $srcImage = [System.Drawing.Image]::FromFile($pngPath)
    $sizes = @(256, 48, 32, 16)

    # Create MemoryStream to build ICO
    $memStream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($memStream)

    # ICO header
    $writer.Write([uint16]0)      # Reserved
    $writer.Write([uint16]1)      # Type: ICO
    $writer.Write([uint16]$sizes.Count)  # Number of images

    # Create bitmaps for each size
    $imageData = @()
    $offset = 6 + ($sizes.Count * 16)  # Header + directory entries

    foreach ($size in $sizes) {
        $bitmap = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawImage($srcImage, 0, 0, $size, $size)
        $graphics.Dispose()

        # Save to PNG in memory
        $pngStream = New-Object System.IO.MemoryStream
        $bitmap.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngBytes = $pngStream.ToArray()
        $pngStream.Dispose()
        $bitmap.Dispose()

        # Directory entry
        $writer.Write([byte]$size)           # Width
        $writer.Write([byte]$size)           # Height
        $writer.Write([byte]0)               # Color palette
        $writer.Write([byte]0)               # Reserved
        $writer.Write([uint16]1)             # Color planes
        $writer.Write([uint16]32)            # Bits per pixel
        $writer.Write([uint32]$pngBytes.Length)  # Size of image data
        $writer.Write([uint32]$offset)       # Offset to image data

        $imageData += $pngBytes
        $offset += $pngBytes.Length
    }

    # Write all image data
    foreach ($data in $imageData) {
        $writer.Write($data)
    }

    $srcImage.Dispose()
    $writer.Flush()

    # Write to file
    [System.IO.File]::WriteAllBytes($icoPath, $memStream.ToArray())

    $writer.Dispose()
    $memStream.Dispose()

    Write-Host "Icon created with .NET fallback!"
    Write-Host "File size: $((Get-Item $icoPath).Length) bytes"
}
