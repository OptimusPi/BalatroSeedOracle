# PowerShell test script for drop zone functionality
Write-Host "Testing Drop Zone Functionality" -ForegroundColor Green

# Build the project
Write-Host "`nBuilding project..." -ForegroundColor Yellow
dotnet build --configuration Debug "x:\BalatroSeedOracle\src\BalatroSeedOracle.csproj"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`nBuild successful!" -ForegroundColor Green
Write-Host "`nTo test the drop zones:" -ForegroundColor Cyan
Write-Host "1. Run the application: dotnet run --configuration Debug" -ForegroundColor White
Write-Host "2. Click 'Filters' button" -ForegroundColor White
Write-Host "3. Click 'Visual Builder' tab" -ForegroundColor White
Write-Host "4. Drag any joker from the shelf to MUST/SHOULD/BANNED zones" -ForegroundColor White
Write-Host "5. Check the debug output for AddToMust/AddToShould/AddToMustNot messages" -ForegroundColor White
Write-Host "`nExpected behavior:" -ForegroundColor Yellow
Write-Host "- Items should appear in drop zones after dragging" -ForegroundColor White
Write-Host "- Debug output should show item details and collection counts" -ForegroundColor White