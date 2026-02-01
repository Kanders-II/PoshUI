<#
.SYNOPSIS
    Runs all PoshUI Pester tests.

.DESCRIPTION
    Master test runner for PoshUI modules using Pester framework.
    Supports CI/CD integration with JUnit output format.

.PARAMETER OutputPath
    Path for test results output file (JUnit XML format).

.PARAMETER ModuleFilter
    Filter to run tests for specific module: Wizard, Dashboard, Workflow, Security, Serialization, or All.

.PARAMETER PassThru
    Returns the Pester result object for further processing.

.EXAMPLE
    .\Run-PesterTests.ps1
    Runs all tests with default settings.

.EXAMPLE
    .\Run-PesterTests.ps1 -ModuleFilter Security -OutputPath .\results.xml
    Runs only security tests and outputs JUnit XML.

.EXAMPLE
    .\Run-PesterTests.ps1 -PassThru | Select-Object TotalCount, PassedCount, FailedCount
    Returns test summary for CI/CD processing.
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$OutputPath,
    
    [Parameter()]
    [ValidateSet('Wizard', 'Dashboard', 'Workflow', 'Security', 'Serialization', 'All')]
    [string]$ModuleFilter = 'All',
    
    [Parameter()]
    [switch]$PassThru
)

$ErrorActionPreference = 'Stop'

Write-Host "`n" -NoNewline
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║           PoshUI Pester Test Suite                         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Check for Pester module
$pesterModule = Get-Module -Name Pester -ListAvailable | Sort-Object Version -Descending | Select-Object -First 1

if (-not $pesterModule) {
    Write-Host "Pester module not found. Installing..." -ForegroundColor Yellow
    Install-Module -Name Pester -Force -SkipPublisherCheck -Scope CurrentUser
    $pesterModule = Get-Module -Name Pester -ListAvailable | Sort-Object Version -Descending | Select-Object -First 1
}

if ($pesterModule.Version -lt [Version]'5.0.0') {
    Write-Host "Pester version $($pesterModule.Version) found. Updating to v5+..." -ForegroundColor Yellow
    Install-Module -Name Pester -Force -SkipPublisherCheck -Scope CurrentUser
}

Import-Module Pester -MinimumVersion 5.0.0 -Force

Write-Host "Pester Version: $((Get-Module Pester).Version)" -ForegroundColor Gray
Write-Host "PowerShell Version: $($PSVersionTable.PSVersion)" -ForegroundColor Gray
Write-Host "Test Filter: $ModuleFilter" -ForegroundColor Gray
Write-Host ""

# Build test file paths
$testDir = $PSScriptRoot
$testFiles = @()

switch ($ModuleFilter) {
    'Wizard' { $testFiles = @(Join-Path $testDir 'PoshUI.Wizard.Tests.ps1') }
    'Dashboard' { $testFiles = @(Join-Path $testDir 'PoshUI.Dashboard.Tests.ps1') }
    'Workflow' { $testFiles = @(Join-Path $testDir 'PoshUI.Workflow.Tests.ps1') }
    'Security' { $testFiles = @(Join-Path $testDir 'PoshUI.Security.Tests.ps1') }
    'Serialization' { $testFiles = @(Join-Path $testDir 'PoshUI.Serialization.Tests.ps1') }
    'All' { $testFiles = Get-ChildItem -Path $testDir -Filter '*.Tests.ps1' | Select-Object -ExpandProperty FullName }
}

# Verify test files exist
$missingFiles = $testFiles | Where-Object { -not (Test-Path $_) }
if ($missingFiles) {
    Write-Host "ERROR: Test files not found:" -ForegroundColor Red
    $missingFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "Test Files to Run:" -ForegroundColor Cyan
$testFiles | ForEach-Object { Write-Host "  - $(Split-Path $_ -Leaf)" -ForegroundColor Gray }
Write-Host ""

# Configure Pester
$pesterConfig = New-PesterConfiguration
$pesterConfig.Run.Path = $testFiles
$pesterConfig.Output.Verbosity = 'Detailed'
$pesterConfig.Run.PassThru = $true

# Configure JUnit output if path specified
if ($OutputPath) {
    $pesterConfig.TestResult.Enabled = $true
    $pesterConfig.TestResult.OutputPath = $OutputPath
    $pesterConfig.TestResult.OutputFormat = 'JUnitXml'
    Write-Host "Results will be saved to: $OutputPath" -ForegroundColor Gray
    Write-Host ""
}

# Run tests
Write-Host "Running tests..." -ForegroundColor Yellow
Write-Host "─" * 60 -ForegroundColor Gray
Write-Host ""

$result = Invoke-Pester -Configuration $pesterConfig

# Display summary
Write-Host ""
Write-Host "─" * 60 -ForegroundColor Gray
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    Test Summary                            ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Total Tests:    $($result.TotalCount)" -ForegroundColor White
Write-Host "  Passed:         $($result.PassedCount)" -ForegroundColor Green
Write-Host "  Failed:         $($result.FailedCount)" -ForegroundColor $(if ($result.FailedCount -gt 0) { 'Red' } else { 'Green' })
Write-Host "  Skipped:        $($result.SkippedCount)" -ForegroundColor Yellow
Write-Host "  Not Run:        $($result.NotRunCount)" -ForegroundColor Gray
Write-Host "  Duration:       $([math]::Round($result.Duration.TotalSeconds, 2))s" -ForegroundColor Gray
Write-Host ""

if ($result.FailedCount -gt 0) {
    Write-Host "FAILED TESTS:" -ForegroundColor Red
    $result.Failed | ForEach-Object {
        Write-Host "  - $($_.ExpandedName)" -ForegroundColor Red
        Write-Host "    Error: $($_.ErrorRecord.Exception.Message)" -ForegroundColor DarkRed
    }
    Write-Host ""
}

# Final result
if ($result.FailedCount -eq 0) {
    Write-Host "═" * 60 -ForegroundColor Green
    Write-Host "  RESULT: ALL TESTS PASSED  " -ForegroundColor Green
    Write-Host "═" * 60 -ForegroundColor Green
} else {
    Write-Host "═" * 60 -ForegroundColor Red
    Write-Host "  RESULT: $($result.FailedCount) TEST(S) FAILED  " -ForegroundColor Red
    Write-Host "═" * 60 -ForegroundColor Red
}

Write-Host ""

# Return result or exit code
if ($PassThru) {
    return $result
} else {
    exit $result.FailedCount
}
