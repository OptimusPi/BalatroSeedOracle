#!/usr/bin/env pwsh
# Clean script to remove all bin/ and obj/ directories recursively

Write-Host "🧹 Cleaning all bin/ and obj/ directories..." -ForegroundColor Cyan

# Get all bin and obj directories recursively
$binDirs = Get-ChildItem -Path . -Recurse -Directory -Name "bin" -Force
$objDirs = Get-ChildItem -Path . -Recurse -Directory -Name "obj" -Force

$totalDirs = $binDirs.Count + $objDirs.Count

if ($totalDirs -eq 0) {
    Write-Host "✅ No bin/ or obj/ directories found. Already clean!" -ForegroundColor Green
    exit 0
}

Write-Host "Found $($binDirs.Count) bin/ and $($objDirs.Count) obj/ directories to remove..." -ForegroundColor Yellow

# Remove bin directories
foreach ($dir in $binDirs) {
    $fullPath = Get-ChildItem -Path . -Recurse -Directory -Filter "bin" | Where-Object { $_.Name -eq "bin" } | Select-Object -First 1 FullName
    try {
        Remove-Item -Path $dir -Recurse -Force -ErrorAction Stop
        Write-Host "🗑️  Removed: $dir" -ForegroundColor Red
    }
    catch {
        Write-Host "⚠️  Failed to remove: $dir - $_" -ForegroundColor Yellow
    }
}

# Remove obj directories  
foreach ($dir in $objDirs) {
    try {
        Remove-Item -Path $dir -Recurse -Force -ErrorAction Stop
        Write-Host "🗑️  Removed: $dir" -ForegroundColor Red
    }
    catch {
        Write-Host "⚠️  Failed to remove: $dir - $_" -ForegroundColor Yellow
    }
}

Write-Host "🎉 Clean completed!" -ForegroundColor Green