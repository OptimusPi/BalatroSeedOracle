# Builds the static seed-finder site by pulling the latest motely-wasm tarball
# from npm (no file:// to the local emit) and extracting it next to index.html.
# Run from repo root: ./docs/seed-finder/build.ps1

$ErrorActionPreference = "Stop"

$siteDir = $PSScriptRoot
$workDir = Join-Path $siteDir ".pack-tmp"
$outDir = Join-Path $siteDir "motely-wasm"

if (Test-Path $workDir) { Remove-Item $workDir -Recurse -Force }
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Path $workDir | Out-Null

Push-Location $workDir
try {
    # Pull the tarball straight from the registry — what real consumers get.
    npm pack motely-wasm@latest --quiet | Out-Null
    $tarball = Get-ChildItem -Filter "motely-wasm-*.tgz" | Select-Object -First 1
    if (-not $tarball) { throw "npm pack produced no tarball" }
    tar -xzf $tarball.Name
    # npm pack tarballs extract to ./package/
    Move-Item ./package $outDir
}
finally {
    Pop-Location
    Remove-Item $workDir -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Site assets refreshed at $outDir"
Write-Host "Version: $(Get-Content (Join-Path $outDir 'package.json') | ConvertFrom-Json | Select-Object -ExpandProperty version)"
