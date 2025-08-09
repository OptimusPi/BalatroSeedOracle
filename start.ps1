#!/usr/bin/env pwsh

Write-Host "BalatroSeedOracle Startup Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Check for Git
Write-Host "`nChecking for Git..." -ForegroundColor Yellow
try {
    $gitVersion = git --version
    Write-Host "✓ Git found: $gitVersion" -ForegroundColor Green
    
    # Initialize submodules
    Write-Host "Initializing Git submodules..." -ForegroundColor Yellow
    git submodule update --init --recursive
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Submodules initialized" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to initialize submodules" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Git not found!" -ForegroundColor Red
    Write-Host "Please install Git from: https://git-scm.com/downloads" -ForegroundColor Yellow
    exit 1
}

# Check for .NET SDK
Write-Host "`nChecking for .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK found: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK not found!" -ForegroundColor Red
    Write-Host "Please install .NET 9 SDK from: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
    exit 1
}

# Clean
Write-Host "`nCleaning solution..." -ForegroundColor Yellow
dotnet clean
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Clean failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Clean complete" -ForegroundColor Green

# Restore
Write-Host "`nRestoring packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Restore failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Restore complete" -ForegroundColor Green

# Build
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build complete" -ForegroundColor Green

# Run
Write-Host "`nStarting BalatroSeedOracle..." -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
dotnet run --configuration Release --project src/Oracle.csproj