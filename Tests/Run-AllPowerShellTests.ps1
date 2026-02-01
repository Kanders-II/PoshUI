<#
.SYNOPSIS
    Runs all PowerShell Pester tests for PoshUI modules.

.DESCRIPTION
    Executes all Pester v5 tests in the Tests/PoshUI folder and generates a summary report.
    Requires Pester v5.0 or higher.

.PARAMETER OutputFormat
    Output format: Detailed, Normal, or Minimal. Default: Normal

.PARAMETER OutputPath
    Optional path to save test results as JUnit XML for CI/CD integration.

.PARAMETER ModuleFilter
    Filter tests to specific module: Dashboard, Wizard, Workflow, Security, Serialization, ControlLogic, or All (default).

.EXAMPLE
    .\Run-AllPowerShellTests.ps1

.EXAMPLE
    .\Run-AllPowerShellTests.ps1 -OutputFormat Detailed

.EXAMPLE
    .\Run-AllPowerShellTests.ps1 -ModuleFilter Dashboard

.EXAMPLE
    .\Run-AllPowerShellTests.ps1 -OutputPath .\test-results.xml
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Detailed', 'Normal', 'Minimal')]
    [string]$OutputFormat = 'Normal',
    
    [Parameter()]
    [string]$OutputPath,
    
    [Parameter()]
    [ValidateSet('All', 'Dashboard', 'Wizard', 'Workflow', 'Security', 'Serialization', 'ControlLogic')]
    [string]$ModuleFilter = 'All'
)

$ErrorActionPreference = 'Stop'

Write-Host "=== PoshUI PowerShell Test Suite ===" -ForegroundColor Cyan
Write-Host ""

# Check for Pester v5
Write-Host "Checking Pester version..." -ForegroundColor Yellow
$pesterModule = Get-Module -ListAvailable Pester | Sort-Object Version -Descending | Select-Object -First 1

if (-not $pesterModule -or $pesterModule.Version.Major -lt 5) {
    Write-Host "ERROR: Pester v5.0 or higher is required" -ForegroundColor Red
    Write-Host "Install with: Install-Module -Name Pester -Force -SkipPublisherCheck -Scope CurrentUser" -ForegroundColor Yellow
    exit 1
}

Write-Host "  Found Pester v$($pesterModule.Version)" -ForegroundColor Green
Write-Host ""

# Import Pester
Import-Module Pester -MinimumVersion 5.0 -ErrorAction Stop

# Determine test files to run
$testPath = Join-Path $PSScriptRoot 'PoshUI'
$testFiles = @()

switch ($ModuleFilter) {
    'Dashboard' { $testFiles = @(Join-Path $testPath 'PoshUI.Dashboard.Tests.ps1') }
    'Wizard' { $testFiles = @(Join-Path $testPath 'PoshUI.Wizard.Tests.ps1') }
    'Workflow' { $testFiles = @(Join-Path $testPath 'PoshUI.Workflow.Tests.ps1') }
    'Security' { $testFiles = @(Join-Path $testPath 'PoshUI.Security.Tests.ps1') }
    'Serialization' { $testFiles = @(Join-Path $testPath 'PoshUI.Serialization.Tests.ps1') }
    'ControlLogic' { $testFiles = @(Join-Path $testPath 'PoshUI.ControlLogic.Tests.ps1') }
    'All' {
        $testFiles = Get-ChildItem -Path $testPath -Filter '*.Tests.ps1' | Select-Object -ExpandProperty FullName
    }
}

# Verify test files exist
$missingFiles = $testFiles | Where-Object { -not (Test-Path $_) }
if ($missingFiles) {
    Write-Host "WARNING: The following test files were not found:" -ForegroundColor Yellow
    $missingFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor DarkYellow }
    $testFiles = $testFiles | Where-Object { Test-Path $_ }
}

if ($testFiles.Count -eq 0) {
    Write-Host "ERROR: No test files found to run" -ForegroundColor Red
    exit 1
}

Write-Host "Running tests from $($testFiles.Count) file(s):" -ForegroundColor Yellow
$testFiles | ForEach-Object { Write-Host "  - $(Split-Path $_ -Leaf)" -ForegroundColor Gray }
Write-Host ""

# Configure Pester
$pesterConfig = New-PesterConfiguration
$pesterConfig.Run.Path = $testFiles
$pesterConfig.Output.Verbosity = $OutputFormat
$pesterConfig.TestResult.Enabled = $false

# Add JUnit output if requested
if ($OutputPath) {
    $pesterConfig.TestResult.Enabled = $true
    $pesterConfig.TestResult.OutputPath = $OutputPath
    $pesterConfig.TestResult.OutputFormat = 'JUnitXml'
    Write-Host "Test results will be saved to: $OutputPath" -ForegroundColor Cyan
    Write-Host ""
}

# Run tests
$startTime = Get-Date
$results = Invoke-Pester -Configuration $pesterConfig

# Display summary
$duration = (Get-Date) - $startTime
Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Total Tests:    $($results.TotalCount)" -ForegroundColor White
Write-Host "Passed:         $($results.PassedCount)" -ForegroundColor Green
Write-Host "Failed:         $($results.FailedCount)" -ForegroundColor $(if ($results.FailedCount -gt 0) { 'Red' } else { 'Green' })
Write-Host "Skipped:        $($results.SkippedCount)" -ForegroundColor Yellow
Write-Host "Duration:       $($duration.TotalSeconds.ToString('F2'))s" -ForegroundColor White
Write-Host ""

if ($results.FailedCount -gt 0) {
    Write-Host "FAILED: $($results.FailedCount) test(s) failed" -ForegroundColor Red
    exit 1
}
else {
    Write-Host "SUCCESS: All tests passed!" -ForegroundColor Green
    exit 0
}
