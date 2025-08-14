<#!
 .SYNOPSIS
  Publishes the BalatroSeedOracle app and creates Velopack packages locally.

 .DESCRIPTION
  Wrapper for dotnet publish followed by vpk pack. Automatically pulls version
  from the project file unless overridden. Installs the Velopack CLI (vpk)
  globally if missing (unless -SkipInstallVpk). Produces output in ./publish and
  ./Releases by default.

 .PARAMETER Rid
  .NET Runtime Identifier (RID) to publish for (default: win-x64).

 .PARAMETER Configuration
  Build configuration (default: Release).

 .PARAMETER Version
  Explicit version to use for packaging (overrides csproj <Version> value).

 .PARAMETER Clean
  Remove existing publish / Releases folders before building.

 .PARAMETER SkipInstallVpk
  Do not attempt to install/update vpk tool (assume already available in PATH).

 .PARAMETER FrameworkDependent
  Produce a framework-dependent build instead of self-contained (NOT typical for Velopack).

 .EXAMPLE
  ./Publish-Velopack.ps1 -Rid win-x64 -Clean

 .EXAMPLE
  ./Publish-Velopack.ps1 -Version 1.2.0-beta.1

 .NOTES
  Requires: dotnet SDK, PowerShell 7+, internet (first install of vpk), write perms.
#!>

param(
    [string]$Rid = "win-x64",
    [string]$Configuration = "Release",
    [string]$Version,
    [switch]$Clean,
    [switch]$SkipInstallVpk,
    [switch]$FrameworkDependent
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $RepoRoot

$ProjectFile = Join-Path $RepoRoot 'src/Oracle.csproj'
if (-not (Test-Path $ProjectFile)) { throw "Project file not found at $ProjectFile" }

function Get-VersionFromCsproj {
    param([string]$Path)
    $match = Select-String -Path $Path -Pattern '<Version>([^<]+)</Version>' -SimpleMatch | Select-Object -First 1
    if (-not $match) { throw "Could not locate <Version> element in $Path" }
    $v = ($match.Matches[0].Groups[1].Value).Trim()
    if (-not $v) { throw "Empty <Version> element in $Path" }
    return $v
}

if (-not $Version) { $Version = Get-VersionFromCsproj -Path $ProjectFile }

# Basic SemVer (allow optional pre-release & build metadata)
if ($Version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+([\-+][0-9A-Za-z\.-]+)?$') {
    throw "Version '$Version' is not a valid semantic version (expected e.g. 1.2.3 or 1.2.3-beta.1)"
}

Write-Host "[INFO] Using version: $Version" -ForegroundColor Cyan
Write-Host "[INFO] RID: $Rid" -ForegroundColor Cyan

if ($Clean) {
    Write-Host "[INFO] Cleaning previous publish/Releases folders" -ForegroundColor Yellow
    Remove-Item -Recurse -Force "$RepoRoot/publish" -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force "$RepoRoot/Releases" -ErrorAction SilentlyContinue
}

if (-not $SkipInstallVpk) {
    $toolList = (& dotnet tool list -g) 2>$null
    if ($LASTEXITCODE -ne 0) { throw "Failed to list dotnet tools. Is the SDK installed?" }
    if ($toolList -notmatch 'vpk') {
        Write-Host "[INFO] Installing Velopack CLI (vpk)" -ForegroundColor Yellow
        dotnet tool install -g vpk
    } else {
        Write-Host "[INFO] Updating Velopack CLI (vpk)" -ForegroundColor Yellow
        dotnet tool update -g vpk | Out-Null
    }
    $toolsPath = Join-Path ([Environment]::GetFolderPath('UserProfile')) '.dotnet/tools'
    if (-not ($env:PATH.Split([IO.Path]::PathSeparator) -contains $toolsPath)) {
        Write-Host "[INFO] Adding tools path to current session PATH" -ForegroundColor Yellow
        $env:PATH += [IO.Path]::PathSeparator + $toolsPath
    }
}

$PublishDir = Join-Path $RepoRoot 'publish'
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

$publishArgs = @('publish', $ProjectFile, '-c', $Configuration, '-r', $Rid, '-o', $PublishDir)
if (-not $FrameworkDependent) {
    $publishArgs += '--self-contained' ; $publishArgs += 'true'
}
$publishArgs += '-p:PublishSingleFile=true'

Write-Host "[STEP] dotnet $($publishArgs -join ' ')" -ForegroundColor Green
dotnet @publishArgs

# Determine main exe name (expected Oracle.exe)
$ExpectedExe = 'Oracle.exe'
$ExePath = Join-Path $PublishDir $ExpectedExe
if (-not (Test-Path $ExePath)) {
    $candidate = Get-ChildItem $PublishDir -Filter '*Oracle*.exe' | Select-Object -First 1
    if ($candidate) { $ExePath = $candidate.FullName; $ExpectedExe = $candidate.Name }
}
if (-not (Test-Path $ExePath)) { throw "Could not find main executable (expected $ExpectedExe) in $PublishDir" }

Write-Host "[INFO] Found main executable: $ExpectedExe" -ForegroundColor Cyan

$ReleasesDir = Join-Path $RepoRoot 'Releases'
New-Item -ItemType Directory -Force -Path $ReleasesDir | Out-Null

$vpkArgs = @('pack', '--packId','BalatroSeedOracle', '--packVersion', $Version,
             '--packDir', $PublishDir, '--mainExe', $ExpectedExe,
             '--packTitle','BalatroSeedOracle', '--packAuthors','OptimusPi',
             '--outputDir', $ReleasesDir, '--runtime', $Rid)

Write-Host "[STEP] vpk $($vpkArgs -join ' ')" -ForegroundColor Green
vpk @vpkArgs

if ($LASTEXITCODE -ne 0) { throw "vpk pack failed with exit code $LASTEXITCODE" }

Write-Host "[DONE] Packages produced in $ReleasesDir" -ForegroundColor Green
Get-ChildItem $ReleasesDir | Format-Table -AutoSize
