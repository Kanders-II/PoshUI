<#
.SYNOPSIS
    Master test runner for all PoshUI test suites.

.DESCRIPTION
    Executes all PoshUI tests in sequence:
    1. PowerShell Pester tests (58 tests)
    2. C# xUnit tests (94 tests)
    3. Integration tests (optional)
    
    Provides comprehensive summary and exit code for CI/CD.

.PARAMETER SkipPowerShell
    Skip PowerShell Pester tests.

.PARAMETER SkipCSharp
    Skip C# xUnit tests.

.PARAMETER SkipIntegration
    Skip integration tests.

.PARAMETER OutputDir
    Directory to save test result files. Default: .\TestResults

.PARAMETER Verbosity
    Test output verbosity: Minimal, Normal, Detailed. Default: Normal

.EXAMPLE
    .\Run-AllTests-Complete.ps1

.EXAMPLE
    .\Run-AllTests-Complete.ps1 -SkipCSharp -Verbosity Detailed

.EXAMPLE
    .\Run-AllTests-Complete.ps1 -OutputDir C:\TestResults
#>

[CmdletBinding()]
param(
    [switch]$SkipPowerShell,
    [switch]$SkipCSharp,
    [switch]$SkipIntegration,
    [string]$OutputDir = (Join-Path $PSScriptRoot 'TestResults'),
    [ValidateSet('Minimal', 'Normal', 'Detailed')]
    [string]$Verbosity = 'Normal'
)

$ErrorActionPreference = 'Continue'
$testDir = $PSScriptRoot

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║           PoshUI Complete Test Suite                       ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Display configuration
$suites = @()
if (-not $SkipPowerShell) { $suites += "PowerShell (Pester)" }
if (-not $SkipCSharp) { $suites += "C# (xUnit)" }
if (-not $SkipIntegration) { $suites += "Integration" }

Write-Host "Test Suites: $($suites -join ', ')" -ForegroundColor Yellow
Write-Host "Verbosity: $Verbosity" -ForegroundColor Yellow
Write-Host "Output Directory: $OutputDir" -ForegroundColor Yellow
Write-Host ""

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$results = @{
    PowerShell = @{ Success = $false; TestCount = 0; Passed = 0; Failed = 0; Duration = 0 }
    CSharp = @{ Success = $false; TestCount = 0; Passed = 0; Failed = 0; Duration = 0 }
    Integration = @{ Success = $false; TestCount = 0; Passed = 0; Failed = 0; Duration = 0 }
}

$overallStartTime = Get-Date

# ═══════════════════════════════════════════════════════════════════════════════
# PART 1: PowerShell Pester Tests
# ═══════════════════════════════════════════════════════════════════════════════

if (-not $SkipPowerShell) {
    Write-Host "╔════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║  Part 1: PowerShell Pester Tests      ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Magenta
    
    $psTestScript = Join-Path $testDir 'Run-AllPowerShellTests.ps1'
    $psOutputFile = Join-Path $OutputDir 'powershell-test-results.xml'
    
    if (Test-Path $psTestScript) {
        try {
            $psStartTime = Get-Date
            $psArgs = @{
                OutputFormat = $Verbosity
                OutputPath = $psOutputFile
            }
            
            & $psTestScript @psArgs
            $psExitCode = $LASTEXITCODE
            $psDuration = ((Get-Date) - $psStartTime).TotalSeconds
            
            $results.PowerShell.Success = ($psExitCode -eq 0)
            $results.PowerShell.Duration = $psDuration
            
            # Parse results from XML if available
            if (Test-Path $psOutputFile) {
                [xml]$psXml = Get-Content $psOutputFile
                $results.PowerShell.TestCount = $psXml.testsuites.tests
                $results.PowerShell.Passed = $psXml.testsuites.tests - $psXml.testsuites.failures
                $results.PowerShell.Failed = $psXml.testsuites.failures
            }
        }
        catch {
            Write-Host "ERROR: PowerShell tests failed with exception: $_" -ForegroundColor Red
            $results.PowerShell.Success = $false
        }
    }
    else {
        Write-Host "WARNING: PowerShell test runner not found: $psTestScript" -ForegroundColor Yellow
    }
    Write-Host ""
}

# ═══════════════════════════════════════════════════════════════════════════════
# PART 2: C# xUnit Tests
# ═══════════════════════════════════════════════════════════════════════════════

if (-not $SkipCSharp) {
    Write-Host "╔════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║  Part 2: C# xUnit Tests                ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Magenta
    
    $csTestScript = Join-Path $testDir 'Run-AllCSharpTests.ps1'
    $csOutputFile = Join-Path $OutputDir 'csharp-test-results.xml'
    
    if (Test-Path $csTestScript) {
        try {
            $csStartTime = Get-Date
            $csArgs = @{
                Verbosity = $Verbosity.ToLower()
                OutputPath = $csOutputFile
            }
            
            & $csTestScript @csArgs
            $csExitCode = $LASTEXITCODE
            $csDuration = ((Get-Date) - $csStartTime).TotalSeconds
            
            $results.CSharp.Success = ($csExitCode -eq 0)
            $results.CSharp.Duration = $csDuration
            
            # Parse results from XML if available
            if (Test-Path $csOutputFile) {
                [xml]$csXml = Get-Content $csOutputFile
                $results.CSharp.TestCount = $csXml.testsuites.tests
                $results.CSharp.Passed = $csXml.testsuites.tests - $csXml.testsuites.failures
                $results.CSharp.Failed = $csXml.testsuites.failures
            }
        }
        catch {
            Write-Host "ERROR: C# tests failed with exception: $_" -ForegroundColor Red
            $results.CSharp.Success = $false
        }
    }
    else {
        Write-Host "WARNING: C# test runner not found: $csTestScript" -ForegroundColor Yellow
    }
    Write-Host ""
}

# ═══════════════════════════════════════════════════════════════════════════════
# PART 3: Integration Tests
# ═══════════════════════════════════════════════════════════════════════════════

if (-not $SkipIntegration) {
    Write-Host "╔════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║  Part 3: Integration Tests             ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Magenta
    
    $integrationDir = Join-Path $testDir 'Integration'
    
    if (Test-Path $integrationDir) {
        $testFiles = Get-ChildItem -Path $integrationDir -Filter 'Test-*.ps1' -File
        
        if ($testFiles.Count -gt 0) {
            $intStartTime = Get-Date
            $intPassed = 0
            $intFailed = 0
            
            foreach ($testFile in $testFiles) {
                Write-Host "Running: $($testFile.Name)" -ForegroundColor Cyan
                try {
                    & $testFile.FullName -ErrorAction Stop
                    $intPassed++
                    Write-Host "  PASSED" -ForegroundColor Green
                }
                catch {
                    Write-Host "  FAILED: $_" -ForegroundColor Red
                    $intFailed++
                }
            }
            
            $intDuration = ((Get-Date) - $intStartTime).TotalSeconds
            $results.Integration.TestCount = $testFiles.Count
            $results.Integration.Passed = $intPassed
            $results.Integration.Failed = $intFailed
            $results.Integration.Duration = $intDuration
            $results.Integration.Success = ($intFailed -eq 0)
        }
        else {
            Write-Host "No integration test files found" -ForegroundColor Yellow
            $results.Integration.Success = $true
        }
    }
    else {
        Write-Host "Integration tests directory not found: $integrationDir" -ForegroundColor Yellow
        $results.Integration.Success = $true
    }
    Write-Host ""
}

# ═══════════════════════════════════════════════════════════════════════════════
# Final Summary
# ═══════════════════════════════════════════════════════════════════════════════

$overallDuration = ((Get-Date) - $overallStartTime).TotalSeconds

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                 Final Test Summary                         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

if (-not $SkipPowerShell) {
    $psStatus = if ($results.PowerShell.Success) { "PASSED" } else { "FAILED" }
    $psColor = if ($results.PowerShell.Success) { "Green" } else { "Red" }
    Write-Host "PowerShell Tests: $psStatus" -ForegroundColor $psColor
    if ($results.PowerShell.TestCount -gt 0) {
        Write-Host "  Total: $($results.PowerShell.TestCount), Passed: $($results.PowerShell.Passed), Failed: $($results.PowerShell.Failed)" -ForegroundColor Gray
    }
    Write-Host "  Duration: $($results.PowerShell.Duration.ToString('F2'))s" -ForegroundColor Gray
    Write-Host ""
}

if (-not $SkipCSharp) {
    $csStatus = if ($results.CSharp.Success) { "PASSED" } else { "FAILED" }
    $csColor = if ($results.CSharp.Success) { "Green" } else { "Red" }
    Write-Host "C# Tests: $csStatus" -ForegroundColor $csColor
    if ($results.CSharp.TestCount -gt 0) {
        Write-Host "  Total: $($results.CSharp.TestCount), Passed: $($results.CSharp.Passed), Failed: $($results.CSharp.Failed)" -ForegroundColor Gray
    }
    Write-Host "  Duration: $($results.CSharp.Duration.ToString('F2'))s" -ForegroundColor Gray
    Write-Host ""
}

if (-not $SkipIntegration) {
    $intStatus = if ($results.Integration.Success) { "PASSED" } else { "FAILED" }
    $intColor = if ($results.Integration.Success) { "Green" } else { "Red" }
    Write-Host "Integration Tests: $intStatus" -ForegroundColor $intColor
    if ($results.Integration.TestCount -gt 0) {
        Write-Host "  Total: $($results.Integration.TestCount), Passed: $($results.Integration.Passed), Failed: $($results.Integration.Failed)" -ForegroundColor Gray
    }
    Write-Host "  Duration: $($results.Integration.Duration.ToString('F2'))s" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "Overall Duration: $($overallDuration.ToString('F2'))s" -ForegroundColor White
Write-Host "════════════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Determine overall success
$allSuccess = $true
if (-not $SkipPowerShell -and -not $results.PowerShell.Success) { $allSuccess = $false }
if (-not $SkipCSharp -and -not $results.CSharp.Success) { $allSuccess = $false }
if (-not $SkipIntegration -and -not $results.Integration.Success) { $allSuccess = $false }

if ($allSuccess) {
    Write-Host "OVERALL RESULT: PASSED ✓" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "OVERALL RESULT: FAILED ✗" -ForegroundColor Red
    exit 1
}
