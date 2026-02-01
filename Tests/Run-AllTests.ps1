<#
.SYNOPSIS
Runs all PoshUI tests (PowerShell, C#, and Integration).

.DESCRIPTION
Master test runner that executes all test suites:
- PowerShell Pester tests (58 tests)
- C# xUnit tests (94 tests)
- Integration tests
Used in CI/CD pipelines and local development.

.PARAMETER SkipPowerShell
Skip PowerShell Pester tests.

.PARAMETER SkipCSharp
Skip C# xUnit tests.

.PARAMETER SkipIntegration
Skip integration tests.

.PARAMETER OutputPath
Base path for test result files (will create separate files for each suite).

.PARAMETER Verbosity
Test output verbosity: Minimal, Normal, Detailed. Default: Normal

.EXAMPLE
.\Run-AllTests.ps1

.EXAMPLE
.\Run-AllTests.ps1 -SkipCSharp

.EXAMPLE
.\Run-AllTests.ps1 -OutputPath .\TestResults -Verbosity Detailed
#>

param(
    [switch]$SkipPowerShell,
    [switch]$SkipCSharp,
    [switch]$SkipIntegration,
    [string]$OutputPath,
    [ValidateSet('Minimal', 'Normal', 'Detailed')]
    [string]$Verbosity = 'Normal'
)

$ErrorActionPreference = 'Stop'
$testDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║           PoshUI Complete Test Suite                       ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Display what will run
$suites = @()
if (-not $SkipPowerShell) { $suites += "PowerShell (Pester)" }
if (-not $SkipCSharp) { $suites += "C# (xUnit)" }
if (-not $SkipIntegration) { $suites += "Integration" }

Write-Host "Test Suites: $($suites -join ', ')" -ForegroundColor Yellow
Write-Host "Verbosity: $Verbosity" -ForegroundColor Yellow
if ($OutputPath) {
    Write-Host "Output Path: $OutputPath" -ForegroundColor Yellow
}
Write-Host ""

$results = @{
    PowerShell = $null
    CSharp = $null
    Integration = @{ Passed = 0; Failed = 0 }
}
$startTime = Get-Date

# ═══════════════════════════════════════════════════════════════════════════════
# PART 1: Integration Tests
# ═══════════════════════════════════════════════════════════════════════════════

if (-not $PesterOnly) {
    Write-Host "╔════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║  Part 1: Integration Tests             ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Magenta

    $testFiles = @()

    if (Test-Path $integrationDir) {
        $testFiles = Get-ChildItem -Path $integrationDir -Filter 'Test-*.ps1' -File
    } else {
        Write-Host "WARNING: Integration tests directory not found: $integrationDir" -ForegroundColor Yellow
    }

    if ($testFiles.Count -gt 0) {
        Write-Host "Found $($testFiles.Count) integration test file(s):`n" -ForegroundColor Cyan

        foreach ($testFile in $testFiles) {
            Write-Host "Running: $($testFile.Name)" -ForegroundColor Cyan
            Write-Host "─" * 40 -ForegroundColor Gray
            
            try {
                if ($SkipInteractive) {
                    & $testFile.FullName -SkipInteractive -ErrorAction Stop
                } else {
                    & $testFile.FullName -ErrorAction Stop
                }
                $totalPassed++
            } catch {
                Write-Host "ERROR: Test failed with exception: $_" -ForegroundColor Red
                $totalFailed++
            }
            
            Write-Host ""
        }
    } else {
        Write-Host "No integration test files found.`n" -ForegroundColor Yellow
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# PART 2: Pester Unit Tests
# ═══════════════════════════════════════════════════════════════════════════════

if (-not $SkipPester) {
    Write-Host "╔════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║  Part 2: Pester Unit Tests             ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Magenta

    $pesterRunner = Join-Path $pesterDir 'Run-PesterTests.ps1'
    
    if (Test-Path $pesterRunner) {
        Write-Host "Running Pester test suite..." -ForegroundColor Cyan
        Write-Host "─" * 40 -ForegroundColor Gray
        
        try {
            $pesterArgs = @{ PassThru = $true }
            if ($OutputPath) {
                $pesterArgs['OutputPath'] = $OutputPath
            }
            
            $pesterResults = & $pesterRunner @pesterArgs
            
            if ($pesterResults.FailedCount -eq 0) {
                $totalPassed++
            } else {
                $totalFailed++
            }
        } catch {
            Write-Host "ERROR: Pester tests failed with exception: $_" -ForegroundColor Red
            $totalFailed++
        }
    } else {
        Write-Host "WARNING: Pester test runner not found: $pesterRunner" -ForegroundColor Yellow
    }
    Write-Host ""
}

# ═══════════════════════════════════════════════════════════════════════════════
# Final Summary
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                 Final Test Summary                         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

if (-not $PesterOnly -and $testFiles) {
    Write-Host "  Integration Tests: $($testFiles.Count) file(s)" -ForegroundColor White
}

if ($pesterResults) {
    Write-Host "  Pester Tests:" -ForegroundColor White
    Write-Host "    Total:   $($pesterResults.TotalCount)" -ForegroundColor Gray
    Write-Host "    Passed:  $($pesterResults.PassedCount)" -ForegroundColor Green
    Write-Host "    Failed:  $($pesterResults.FailedCount)" -ForegroundColor $(if ($pesterResults.FailedCount -gt 0) { 'Red' } else { 'Green' })
    Write-Host "    Skipped: $($pesterResults.SkippedCount)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  Overall Result:" -ForegroundColor White
Write-Host "    Suites Passed: $totalPassed" -ForegroundColor Green
Write-Host "    Suites Failed: $totalFailed" -ForegroundColor $(if ($totalFailed -gt 0) { 'Red' } else { 'Green' })
Write-Host "════════════════════════════════════════════════════════════`n" -ForegroundColor Cyan

if ($totalFailed -gt 0 -or ($pesterResults -and $pesterResults.FailedCount -gt 0)) {
    Write-Host "RESULT: FAILED" -ForegroundColor Red
    exit 1
} else {
    Write-Host "RESULT: PASSED" -ForegroundColor Green
    exit 0
}
