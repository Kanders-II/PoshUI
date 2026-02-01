# PoshWizard CI/CD Testing Guide

## Overview

PoshWizard uses a comprehensive three-tier testing strategy designed for continuous integration and deployment:

1. **PowerShell Integration Tests** (Primary - 80%)
2. **C# Unit Tests** (Secondary - 15%)
3. **Manual Testing** (Tertiary - 5%)

## CI/CD Pipeline Architecture

### GitHub Actions Workflow

The CI/CD pipeline is defined in `.github/workflows/ci-tests.yml` and consists of 6 jobs:

#### 1. PowerShell Integration Tests
- **Purpose**: Validate PowerShell module functionality
- **Tests**:
  - Module loading and cmdlet export
  - Demo script validation
  - All integration tests
- **Platform**: Windows (PowerShell 5.1+)

#### 2. C# Unit Tests
- **Purpose**: Test internal Launcher logic
- **Framework**: xUnit
- **Tests**:
  - Module loading
  - Parameter validation
  - Script preprocessing
  - Dynamic parameter management
  - Theme functionality
- **Platform**: Windows (.NET Framework 4.8)

#### 3. Build Validation
- **Purpose**: Ensure solution builds correctly
- **Validates**:
  - Debug configuration builds
  - Release configuration builds
  - PoshWizard.exe exists after build
- **Artifacts**: Build output uploaded

#### 4. Module Validation
- **Purpose**: Validate PowerShell module structure
- **Checks**:
  - Module manifest validity
  - PowerShell script syntax
  - No parse errors in .ps1 files

#### 5. Security Scan
- **Purpose**: Detect security issues
- **Scans for**:
  - Hardcoded credentials
  - API keys or secrets
  - Hardcoded paths
  - Potential security vulnerabilities

#### 6. Test Summary
- **Purpose**: Aggregate results
- **Reports**: Final pass/fail status of all jobs

## Test Categories

### PowerShell Integration Tests

Location: `Tests/Integration/`

**Test-ModuleLoading.ps1**
- Module file exists
- Module imports successfully
- Cmdlets are exported
- Core cmdlets present

**Test-DemoScripts.ps1**
- Demo scripts exist
- Script syntax is valid
- Scripts run without errors (interactive mode)

**Test-CmdletParameters.ps1**
- Parameter definitions are correct
- Mandatory flags are set
- Parameter types are valid
- Validation attributes work

**Test-ErrorHandling.ps1**
- Missing parameters throw errors
- Invalid inputs are rejected
- Meaningful error messages provided
- Edge cases handled

### C# Unit Tests

Location: `Launcher.Tests/`

**ModuleLoadingTests.cs**
- Module manifest validation
- Module import functionality
- Cmdlet discovery
- Version validation

**ThemeTests.cs**
- System theme detection
- Theme value validation
- Default theme behavior

**ParameterValidationTests.cs**
- Parameter type validation
- Mandatory parameter enforcement
- ValidateSet attribute handling

**ScriptPreprocessingTests.cs**
- Script parsing
- Parameter extraction
- Security validation

**DynamicParameterManagerTests.cs**
- Dynamic parameter creation
- Parameter dependency handling
- Runtime parameter updates

## Running Tests Locally

### All Tests (Recommended)
```powershell
cd Tests
.\Run-AllTests.ps1
```

### PowerShell Integration Tests Only
```powershell
cd Tests
.\Run-AllTests.ps1 -SkipInteractive
```

### C# Unit Tests Only
```powershell
dotnet test WizardFramework.sln --configuration Debug
```

### Individual Integration Test
```powershell
.\Tests\Integration\Test-ModuleLoading.ps1
.\Tests\Integration\Test-CmdletParameters.ps1
.\Tests\Integration\Test-ErrorHandling.ps1
```

### Build and Test
```powershell
# Build
dotnet build WizardFramework.sln --configuration Debug

# Run all tests
cd Tests
.\Run-AllTests.ps1
cd ..
dotnet test WizardFramework.sln --no-build
```

## CI/CD Integration

### GitHub Actions

The workflow automatically runs on:
- Push to `main`, `master`, or `develop` branches
- Pull requests to `main`, `master`, or `develop` branches
- Manual workflow dispatch

### Test Results

Test results are uploaded as artifacts:
- **powershell-test-results**: PowerShell test logs
- **csharp-test-results**: xUnit test results (.trx files)
- **build-artifacts**: Build output (PoshWizard.exe, modules)

### Exit Codes

- **0**: All tests passed
- **1**: One or more tests failed

### Artifacts Retention

Artifacts are retained for 90 days by default.

## Adding New Tests

### PowerShell Integration Test

1. Create file in `Tests/Integration/Test-FeatureName.ps1`
2. Follow template:

```powershell
<#
.SYNOPSIS
Brief description of test

.DESCRIPTION
Detailed description

.EXAMPLE
& .\Test-FeatureName.ps1
#>

param(
    [string]$ModulePath = (Join-Path $PSScriptRoot '..\..\PoshWizard\PoshWizard.psd1'),
    [switch]$SkipInteractive
)

$testsPassed = 0
$testsFailed = 0

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Test Name" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Import module
Import-Module $ModulePath -Force -ErrorAction Stop

# Test 1: Description
Write-Host "Test 1: Description..." -ForegroundColor Yellow
try {
    # Test logic
    Write-Host "  PASS: Description" -ForegroundColor Green
    $testsPassed++
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $testsFailed++
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Passed: $testsPassed" -ForegroundColor Green
Write-Host "  Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -gt 0) { 'Red' } else { 'Green' })
Write-Host "========================================`n" -ForegroundColor Cyan

if ($testsFailed -gt 0) {
    exit 1
}
```

3. Test is automatically picked up by `Run-AllTests.ps1`

### C# Unit Test

1. Add to `Launcher.Tests/` project
2. Follow xUnit patterns:

```csharp
using Xunit;

namespace Launcher.Tests
{
    public class FeatureTests
    {
        [Fact]
        public void Feature_ShouldBehavior()
        {
            // Arrange
            var input = "test";
            
            // Act
            var result = ProcessInput(input);
            
            // Assert
            Assert.NotNull(result);
        }
        
        [Theory]
        [InlineData("input1", "expected1")]
        [InlineData("input2", "expected2")]
        public void Feature_ShouldHandleMultipleInputs(string input, string expected)
        {
            // Arrange & Act
            var result = ProcessInput(input);
            
            // Assert
            Assert.Equal(expected, result);
        }
    }
}
```

3. Tests automatically run in CI/CD

## Test Coverage Goals

- **PowerShell Module**: 100% of exported cmdlets tested
- **C# Launcher**: > 70% code coverage for critical paths
- **Demo Scripts**: All demos validated in CI/CD
- **Error Handling**: All failure scenarios tested

## Troubleshooting

### Tests Pass Locally but Fail in CI/CD

1. Check for environment-specific dependencies
2. Verify paths are relative, not absolute
3. Ensure no interactive prompts
4. Check for timing-dependent tests

### Module Import Failures

1. Verify module manifest is valid: `Test-ModuleManifest`
2. Check for syntax errors in .ps1 files
3. Ensure all dependencies are present
4. Verify file paths are correct

### Build Failures

1. Check .NET version (4.8 required)
2. Verify all NuGet packages restore
3. Check for missing files
4. Review build logs for specific errors

## Best Practices

1. **Keep tests independent** - No shared state between tests
2. **Use descriptive names** - Clear test and assertion names
3. **Test one thing** - Single responsibility per test
4. **Support CI/CD** - Use `-SkipInteractive` parameter
5. **Clean up** - Remove test artifacts and variables
6. **Exit correctly** - Return proper exit codes
7. **Log clearly** - Color-coded output with clear messages
8. **Version control** - Commit test files with code changes

## Continuous Improvement

- Review test results regularly
- Add tests for bug fixes
- Update tests when features change
- Monitor test execution time
- Keep tests maintainable and readable

---

**For questions or issues, contact:** support@asolutionit.com
