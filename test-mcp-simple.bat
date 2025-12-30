@echo off
echo ========================================
echo Testing Balatro MCP Server
echo ========================================
echo.

echo 1. Building MCP Server...
cd src\BalatroSeedOracle.MCP.CLI
dotnet build
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Build failed!
    pause
    exit /b 1
)
echo ✅ Build successful!
echo.

echo 2. Testing MCP Server...
echo Starting server and sending test requests...
echo.

echo Testing: Initialize MCP Server
echo {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}} | dotnet run
echo.

echo Testing: Get JAML Schema  
echo {"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"get_jaml_schema","arguments":{}}} | dotnet run
echo.

echo Testing: Generate JAML with Blueprint
echo {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"generate_jaml_with_context","arguments":{"userRequest":"Blueprint scaling build with face cards"}}} | dotnet run
echo.

echo ✅ All tests completed!
pause
