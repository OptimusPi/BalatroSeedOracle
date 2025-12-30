Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Testing Balatro MCP Server" -ForegroundColor Cyan  
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. Building MCP Server..." -ForegroundColor Yellow
Set-Location "src\BalatroSeedOracle.MCP.CLI"
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "✅ Build successful!" -ForegroundColor Green
Write-Host ""

Write-Host "2. Testing MCP Server..." -ForegroundColor Yellow
Write-Host "Starting server and sending test requests..." -ForegroundColor Gray
Write-Host ""

Write-Host "Testing: Initialize MCP Server" -ForegroundColor Cyan
'{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}' | dotnet run
Write-Host ""

Write-Host "Testing: Get JAML Schema" -ForegroundColor Cyan  
'{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"get_jaml_schema","arguments":{}}}' | dotnet run
Write-Host ""

Write-Host "Testing: Generate JAML with Blueprint" -ForegroundColor Cyan
'{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"generate_jaml_with_context","arguments":{"userRequest":"Blueprint scaling build with face cards"}}}' | dotnet run
Write-Host ""

Write-Host "✅ All tests completed!" -ForegroundColor Green
Read-Host "Press Enter to exit"
