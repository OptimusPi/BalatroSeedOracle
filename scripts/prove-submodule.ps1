# Fail loud if Motely submodule is empty (Windows / PowerShell).
# Usage: pwsh -File scripts/prove-submodule.ps1
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $root "src/MotelyJAML/Motely/Motely.csproj"
if (-not (Test-Path $csproj)) {
    Write-Host "SUBMODULE_MISSING — run: git submodule update --init --recursive"
    exit 1
}
Write-Host "SUBMODULE_OK  $csproj"
exit 0
