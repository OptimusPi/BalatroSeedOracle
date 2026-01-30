<#
.SYNOPSIS
  Build Balatro Seed Oracle browser (WASM) and deploy to API wwwroot so the API can host it at /BSO.

.DESCRIPTION
  Publishes src/BalatroSeedOracle.Browser to Release, then copies publish/wwwroot
  to external/Motely/wwwroot/BSO. Motely.API serves static files from wwwroot,
  so after running this script the API serves the browser app at /BSO (with COOP/COEP headers).

.EXAMPLE
  .\scripts\publish-browser.ps1
  # Then run Motely.API and open https://localhost:PORT/BSO
#>
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot\.."
$project = Join-Path $repoRoot "src\BalatroSeedOracle.Browser\BalatroSeedOracle.Browser.csproj"
$publishDir = Join-Path $repoRoot "src\BalatroSeedOracle.Browser\bin\$Configuration\net10.0-browser\publish\wwwroot"
$wwwRoot = Join-Path $repoRoot "external\Motely\wwwroot"
$destination = Join-Path $wwwRoot "BSO"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BSO Browser -> API wwwroot (route /BSO)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/2] Publishing browser (WASM)..." -ForegroundColor Yellow
dotnet publish $project -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

if (-not (Test-Path $publishDir)) {
    throw "Publish output not found: $publishDir"
}

Write-Host ""
Write-Host "[2/2] Deploying to API wwwroot..." -ForegroundColor Yellow
if (-not (Test-Path $wwwRoot)) {
    New-Item -ItemType Directory -Path $wwwRoot -Force | Out-Null
}
if (Test-Path $destination) {
    Get-ChildItem $destination -Recurse | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item $destination -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item "$publishDir\*" -Destination $destination -Recurse -Force

Write-Host ""
Write-Host "Done. API wwwroot/BSO is ready." -ForegroundColor Green
Write-Host "  Run Motely.API and open: /BSO" -ForegroundColor Gray
Write-Host "  Path: $destination" -ForegroundColor Gray
Write-Host ""
