# Kill all Cursor processes
Write-Host "Killing all Cursor processes..."
Get-Process -Name "Cursor" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# Verify they're gone
$remaining = Get-Process -Name "Cursor" -ErrorAction SilentlyContinue
if ($remaining) {
    Write-Host "WARNING: Some Cursor processes still running:" -ForegroundColor Yellow
    $remaining | Format-Table
} else {
    Write-Host "All Cursor processes killed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Now try opening the folder again:"
    Write-Host "  cursor .\BalatroSeedOracle\"
}


