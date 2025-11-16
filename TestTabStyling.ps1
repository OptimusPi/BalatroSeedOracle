# PowerShell script to test tab styling
# Start the app
$process = Start-Process -FilePath "dotnet" -ArgumentList "run --project X:\BalatroSeedOracle\src\BalatroSeedOracle.csproj" -PassThru

# Wait for the app to start
Start-Sleep -Seconds 3

# Take a screenshot after a moment
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Start-Sleep -Seconds 2

$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
$bitmap.Save("X:\BalatroSeedOracle\tab_styling_test.png", [System.Drawing.Imaging.ImageFormat]::Png)

Write-Host "Screenshot saved to X:\BalatroSeedOracle\tab_styling_test.png"

# Keep app running for manual inspection
Write-Host "App is running. Press Enter to close..."
Read-Host

# Clean up
Stop-Process -Id $process.Id -ErrorAction SilentlyContinue