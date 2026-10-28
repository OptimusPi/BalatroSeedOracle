<#
.SYNOPSIS
    Automated E2E Test Runner for Balatro Seed Oracle Ecosystem across all 6 components.

.DESCRIPTION
    Runs independent opaque-box E2E test suites across Tiers 1-4:
      - Tier 1: Feature Coverage (Features 1-23)
      - Tier 2: Boundary & Corner Cases (Features 1-23)
      - Tier 3: Cross-Feature Combinations (Pairwise Component Interactions)
      - Tier 4: Real-World Application Scenarios (End-to-End User Journeys)

.PARAMETER Tier
    Specify Tier to execute: 1, 2, 3, or 4. Omit to run all tiers.

.PARAMETER Feature
    Specify Feature index (1-23) to run feature-specific tests.

.PARAMETER Component
    Filter by component name (dungmot, motely, jaml-ui, seedfinder, erraticdeck, desktop).

.PARAMETER Verbose
    Enable verbose test output.

.EXAMPLE
    pwsh -File run_e2e_tests.ps1
    pwsh -File run_e2e_tests.ps1 -Tier 1
    pwsh -File run_e2e_tests.ps1 -Feature 1
    pwsh -File run_e2e_tests.ps1 -Component jaml-ui
#>

param(
    [ValidateSet('1', '2', '3', '4')]
    [string]$Tier,

    [ValidateRange(1, 23)]
    [int]$Feature,

    [string]$Component,

    [switch]$Verbose,

    [switch]$Help
)

$ErrorActionPreference = 'Stop'

if ($Help) {
    Get-Help $MyInvocation.MyCommand.Path -Detailed
    exit 0
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$runnerScript = [System.IO.Path]::Combine($scriptDir, "tests", "e2e", "runner.mjs")

if (-not (Test-Path $runnerScript)) {
    Write-Error "Test runner script not found at $runnerScript"
    exit 1
}

$nodeArgs = @($runnerScript)

if ($Tier) {
    $nodeArgs += @("-t", $Tier)
}
if ($PSBoundParameters.ContainsKey('Feature')) {
    $nodeArgs += @("-f", $Feature.ToString())
}
if ($Component) {
    $nodeArgs += @("-c", $Component)
}
if ($Verbose) {
    $nodeArgs += @("-v")
}

Write-Host "Running Balatro Seed Oracle E2E Tests via Node.js..." -ForegroundColor Cyan
& node @nodeArgs
exit $LASTEXITCODE
