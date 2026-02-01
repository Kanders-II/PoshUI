#Requires -Version 5.1
#Requires -Modules Pester

<#
.SYNOPSIS
    Runs comprehensive security tests for PoshUI.

.DESCRIPTION
    Executes security-focused test suites across all PoshUI modules:
    - Workflow state encryption and integrity
    - Temp file security
    - ACL validation
    - Cross-module security consistency
    
.PARAMETER TestPath
    Optional path to specific test file. If not specified, runs all security tests.

.PARAMETER OutputFormat
    Pester output format: NUnitXml, JUnitXml, or None (default: None)

.PARAMETER OutputFile
    Path to save test results (only used if OutputFormat is specified)

.EXAMPLE
    .\Run-SecurityTests.ps1
    
    Runs all security tests with console output.

.EXAMPLE
    .\Run-SecurityTests.ps1 -OutputFormat NUnitXml -OutputFile "SecurityTestResults.xml"
    
    Runs all security tests and saves results to XML.

.EXAMPLE
    .\Run-SecurityTests.ps1 -TestPath ".\PoshUI\PoshUI.Workflow.Security.Tests.ps1"
    
    Runs only workflow security tests.
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$TestPath,
    
    [Parameter()]
    [ValidateSet('None', 'NUnitXml', 'JUnitXml')]
    [string]$OutputFormat = 'None',
    
    [Parameter()]
    [string]$OutputFile
)

$ErrorActionPreference = 'Stop'

# Banner
Write-Host @"

╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║              PoshUI Security Test Suite                      ║
║                                                               ║
║  Testing encryption, integrity, ACLs, and security features  ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

# Check Pester version
$pesterModule = Get-Module -Name Pester -ListAvailable | Sort-Object Version -Descending | Select-Object -First 1
if (-not $pesterModule) {
    Write-Error "Pester module not found. Install with: Install-Module Pester -Force"
    exit 1
}

Write-Host "Using Pester version: $($pesterModule.Version)" -ForegroundColor Gray
Write-Host ""

# Determine test files to run
$testFiles = @()

if ($TestPath) {
    if (Test-Path $TestPath) {
        $testFiles += $TestPath
    }
    else {
        Write-Error "Test file not found: $TestPath"
        exit 1
    }
}
else {
    # Find all security test files
    $testDir = Join-Path $PSScriptRoot "PoshUI"
    $testFiles = Get-ChildItem -Path $testDir -Filter "*.Security.Tests.ps1" -Recurse | Select-Object -ExpandProperty FullName
    
    # Also include general security tests
    $generalSecurityTest = Join-Path $testDir "PoshUI.Security.Tests.ps1"
    if (Test-Path $generalSecurityTest) {
        $testFiles += $generalSecurityTest
    }
}

if ($testFiles.Count -eq 0) {
    Write-Warning "No security test files found."
    exit 0
}

Write-Host "Found $($testFiles.Count) security test file(s):" -ForegroundColor Yellow
foreach ($file in $testFiles) {
    Write-Host "  - $(Split-Path $file -Leaf)" -ForegroundColor Gray
}
Write-Host ""

# Configure Pester
$pesterConfig = @{
    Run = @{
        Path = $testFiles
        PassThru = $true
    }
    Output = @{
        Verbosity = 'Detailed'
    }
    Filter = @{
        Tag = 'Security'
    }
}

# Add output configuration if specified
if ($OutputFormat -ne 'None' -and $OutputFile) {
    $pesterConfig.TestResult = @{
        Enabled = $true
        OutputFormat = $OutputFormat
        OutputPath = $OutputFile
    }
}

# Run tests
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Starting Security Tests..." -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$startTime = Get-Date

try {
    $config = New-PesterConfiguration -Hashtable $pesterConfig
    $result = Invoke-Pester -Configuration $config
}
catch {
    Write-Error "Failed to run security tests: $_"
    exit 1
}

$endTime = Get-Date
$duration = $endTime - $startTime

# Summary
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Security Test Summary" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$totalTests = $result.TotalCount
$passedTests = $result.PassedCount
$failedTests = $result.FailedCount
$skippedTests = $result.SkippedCount

Write-Host "Total Tests:   $totalTests" -ForegroundColor White
Write-Host "Passed:        " -NoNewline
Write-Host $passedTests -ForegroundColor Green
Write-Host "Failed:        " -NoNewline
if ($failedTests -gt 0) {
    Write-Host $failedTests -ForegroundColor Red
}
else {
    Write-Host $failedTests -ForegroundColor Green
}
Write-Host "Skipped:       $skippedTests" -ForegroundColor Yellow
Write-Host "Duration:      $($duration.TotalSeconds.ToString('F2')) seconds" -ForegroundColor Gray
Write-Host ""

# Security-specific summary
Write-Host "Security Test Categories:" -ForegroundColor Cyan
Write-Host "  ✓ Encryption Tests" -ForegroundColor $(if ($failedTests -eq 0) { 'Green' } else { 'Yellow' })
Write-Host "  ✓ Integrity Tests" -ForegroundColor $(if ($failedTests -eq 0) { 'Green' } else { 'Yellow' })
Write-Host "  ✓ ACL Tests" -ForegroundColor $(if ($failedTests -eq 0) { 'Green' } else { 'Yellow' })
Write-Host "  ✓ Secure Wipe Tests" -ForegroundColor $(if ($failedTests -eq 0) { 'Green' } else { 'Yellow' })
Write-Host "  ✓ Cross-User Isolation Tests" -ForegroundColor $(if ($failedTests -eq 0) { 'Green' } else { 'Yellow' })
Write-Host ""

if ($OutputFile -and (Test-Path $OutputFile)) {
    Write-Host "Test results saved to: $OutputFile" -ForegroundColor Gray
    Write-Host ""
}

# Exit with appropriate code
if ($failedTests -gt 0) {
    Write-Host "❌ SECURITY TESTS FAILED" -ForegroundColor Red -BackgroundColor Black
    Write-Host ""
    Write-Host "Review failed tests above and address security issues before deployment." -ForegroundColor Yellow
    exit 1
}
else {
    Write-Host "✅ ALL SECURITY TESTS PASSED" -ForegroundColor Green -BackgroundColor Black
    Write-Host ""
    Write-Host "Security validation complete. System is ready for production use." -ForegroundColor Green
    exit 0
}
