<#
.SYNOPSIS
    Runs all C# xUnit tests for PoshUI Launcher.

.DESCRIPTION
    Executes all xUnit tests in the Launcher.Tests project and generates a summary report.
    Requires .NET Framework 4.8 SDK.

.PARAMETER Configuration
    Build configuration: Debug or Release. Default: Debug

.PARAMETER OutputPath
    Optional path to save test results as JUnit XML for CI/CD integration.

.PARAMETER Verbosity
    Test output verbosity: quiet, minimal, normal, detailed, diagnostic. Default: normal

.EXAMPLE
    .\Run-AllCSharpTests.ps1

.EXAMPLE
    .\Run-AllCSharpTests.ps1 -Configuration Release

.EXAMPLE
    .\Run-AllCSharpTests.ps1 -Verbosity detailed

.EXAMPLE
    .\Run-AllCSharpTests.ps1 -OutputPath .\csharp-test-results.xml
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    
    [Parameter()]
    [string]$OutputPath,
    
    [Parameter()]
    [ValidateSet('quiet', 'minimal', 'normal', 'detailed', 'diagnostic')]
    [string]$Verbosity = 'normal'
)

$ErrorActionPreference = 'Stop'

Write-Host "=== PoshUI C# Test Suite ===" -ForegroundColor Cyan
Write-Host ""

# Check for dotnet CLI
Write-Host "Checking for .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "  Found .NET SDK v$dotnetVersion" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: .NET SDK not found" -ForegroundColor Red
    Write-Host "Install from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# Locate test project
$testProjectPath = Join-Path $PSScriptRoot '..\Launcher.Tests\Launcher.Tests.csproj'
if (-not (Test-Path $testProjectPath)) {
    Write-Host "ERROR: Test project not found at: $testProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "Test Project: $testProjectPath" -ForegroundColor Gray
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host ""

# Build test arguments
$testArgs = @(
    'test',
    $testProjectPath,
    '--configuration', $Configuration,
    '--verbosity', $Verbosity,
    '--no-build'
)

# Add logger for JUnit output if requested
if ($OutputPath) {
    $absoluteOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
    $testArgs += '--logger', "junit;LogFilePath=$absoluteOutputPath"
    Write-Host "Test results will be saved to: $absoluteOutputPath" -ForegroundColor Cyan
    Write-Host ""
}

# Build the project first
Write-Host "Building test project..." -ForegroundColor Yellow
$buildArgs = @(
    'build',
    $testProjectPath,
    '--configuration', $Configuration
)

& dotnet @buildArgs | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful" -ForegroundColor Green
Write-Host ""

# Run tests
Write-Host "Running C# tests..." -ForegroundColor Yellow
Write-Host ""

$startTime = Get-Date
& dotnet @testArgs
$exitCode = $LASTEXITCODE
$duration = (Get-Date) - $startTime

Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Duration: $($duration.TotalSeconds.ToString('F2'))s" -ForegroundColor White
Write-Host ""

if ($exitCode -eq 0) {
    Write-Host "SUCCESS: All C# tests passed!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "FAILED: Some C# tests failed (exit code: $exitCode)" -ForegroundColor Red
    exit $exitCode
}
