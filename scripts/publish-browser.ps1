param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot\.."
$project = Join-Path $repoRoot "src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj"
$publishDir = Join-Path $repoRoot "src/BalatroSeedOracle.Browser/bin/$Configuration/net10.0-browser/publish/wwwroot"
$destination = Join-Path $repoRoot "external/Motely/wwwroot/BSO"

Write-Host "Publishing $project ($Configuration)..."
dotnet publish $project -c $Configuration

if (-not (Test-Path $publishDir)) {
    throw "Publish directory not found: $publishDir"
}

Write-Host "Syncing output to $destination"
if (Test-Path $destination) {
    Remove-Item "$destination/*" -Recurse -Force -ErrorAction SilentlyContinue
} else {
    New-Item -ItemType Directory -Path $destination | Out-Null
}

Copy-Item "$publishDir/*" -Destination $destination -Recurse -Force

Write-Host "Browser artifacts published to $destination"
