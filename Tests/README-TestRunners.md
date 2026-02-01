# PoshUI Test Runners

This directory contains comprehensive test runner scripts for all PoshUI test suites.

## Quick Start

### Run All Tests
```powershell
.\Run-AllTests-Complete.ps1
```

### Run Specific Test Suite
```powershell
# PowerShell tests only
.\Run-AllPowerShellTests.ps1

# C# tests only
.\Run-AllCSharpTests.ps1

# Integration tests only
.\Run-AllTests-Complete.ps1 -SkipPowerShell -SkipCSharp
```

## Test Runner Scripts

### 1. Run-AllTests-Complete.ps1
**Master test runner** - Executes all test suites in sequence.

**Features:**
- Runs PowerShell Pester tests (58 tests)
- Runs C# xUnit tests (94 tests)
- Runs Integration tests
- Generates comprehensive summary
- Saves test results to XML files
- Returns proper exit codes for CI/CD

**Usage:**
```powershell
# Run everything
.\Run-AllTests-Complete.ps1

# Skip C# tests
.\Run-AllTests-Complete.ps1 -SkipCSharp

# Detailed output with custom results directory
.\Run-AllTests-Complete.ps1 -Verbosity Detailed -OutputDir C:\TestResults

# CI/CD mode (skip integration tests)
.\Run-AllTests-Complete.ps1 -SkipIntegration
```

**Parameters:**
- `-SkipPowerShell` - Skip PowerShell Pester tests
- `-SkipCSharp` - Skip C# xUnit tests
- `-SkipIntegration` - Skip integration tests
- `-OutputDir` - Directory for test result files (default: `.\TestResults`)
- `-Verbosity` - Output level: Minimal, Normal, Detailed (default: Normal)

---

### 2. Run-AllPowerShellTests.ps1
**PowerShell Pester test runner** - Executes all Pester v5 tests.

**Features:**
- Runs all tests in `Tests/PoshUI/` folder
- Filter by module (Dashboard, Wizard, Workflow, etc.)
- Generates JUnit XML for CI/CD
- Detailed test summary

**Usage:**
```powershell
# Run all PowerShell tests
.\Run-AllPowerShellTests.ps1

# Run only Dashboard tests
.\Run-AllPowerShellTests.ps1 -ModuleFilter Dashboard

# Detailed output with JUnit XML
.\Run-AllPowerShellTests.ps1 -OutputFormat Detailed -OutputPath .\results.xml
```

**Parameters:**
- `-OutputFormat` - Detailed, Normal, or Minimal (default: Normal)
- `-OutputPath` - Path to save JUnit XML results
- `-ModuleFilter` - Filter: All, Dashboard, Wizard, Workflow, Security, Serialization, ControlLogic

**Test Coverage:**
- Module loading and cmdlet exports
- Core functionality (New-PoshUIDashboard, Add-UIStep, etc.)
- Type-specific card cmdlets (Add-UIMetricCard, Add-UIChartCard, Add-UITableCard)
- Get-PoshUIDashboard cmdlet
- BannerStyle presets
- ScriptBlock error handling
- Security validation

---

### 3. Run-AllCSharpTests.ps1
**C# xUnit test runner** - Executes all C# unit tests.

**Features:**
- Builds and runs Launcher.Tests project
- Generates JUnit XML for CI/CD
- Configurable build configuration (Debug/Release)

**Usage:**
```powershell
# Run all C# tests
.\Run-AllCSharpTests.ps1

# Release configuration with detailed output
.\Run-AllCSharpTests.ps1 -Configuration Release -Verbosity detailed

# Save results to XML
.\Run-AllCSharpTests.ps1 -OutputPath .\csharp-results.xml
```

**Parameters:**
- `-Configuration` - Debug or Release (default: Debug)
- `-OutputPath` - Path to save JUnit XML results
- `-Verbosity` - quiet, minimal, normal, detailed, diagnostic (default: normal)

**Test Coverage:**
- Module loading for all three PoshUI modules
- Module manifest validation
- Cmdlet export verification
- Parameter validation (regex, ranges, types)
- Theme detection

---

## Test Suites Overview

### PowerShell Tests (Pester v5)
**Location:** `Tests/PoshUI/`  
**Test Files:**
- `PoshUI.Dashboard.Tests.ps1` (58 tests) ✅
- `PoshUI.Wizard.Tests.ps1`
- `PoshUI.Workflow.Tests.ps1`
- `PoshUI.Security.Tests.ps1`
- `PoshUI.Serialization.Tests.ps1`
- `PoshUI.ControlLogic.Tests.ps1`

**Requirements:**
- Pester v5.0 or higher
- PowerShell 5.1 or PowerShell 7+

**Install Pester:**
```powershell
Install-Module -Name Pester -Force -SkipPublisherCheck -Scope CurrentUser
```

### C# Tests (xUnit)
**Location:** `Launcher.Tests/`  
**Test Files:**
- `ModuleLoadingTests.cs` (18 tests) ✅
- `ParameterValidationTests.cs` (77 tests) ✅
- `ThemeTests.cs` (3 tests) ✅

**Requirements:**
- .NET Framework 4.8 SDK
- xUnit 2.4.2

**Excluded Files:**
- `DynamicParameterManagerTests.cs` (outdated)
- `ScriptPreprocessingTests.cs` (outdated)

### Integration Tests
**Location:** `Tests/Integration/`  
**Test Files:**
- `Test-ModuleLoading.ps1`
- `Test-DemoScripts.ps1`
- `Test-CmdletParameters.ps1`

---

## CI/CD Integration

### GitHub Actions
```yaml
- name: Run All Tests
  shell: pwsh
  run: |
    cd Tests
    .\Run-AllTests-Complete.ps1 -OutputDir ${{ github.workspace }}\TestResults
    
- name: Upload Test Results
  uses: actions/upload-artifact@v3
  with:
    name: test-results
    path: TestResults/*.xml
```

### Azure DevOps
```yaml
- task: PowerShell@2
  inputs:
    targetType: 'filePath'
    filePath: 'Tests\Run-AllTests-Complete.ps1'
    arguments: '-OutputDir $(Build.ArtifactStagingDirectory)\TestResults'
    
- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'JUnit'
    testResultsFiles: '**/*.xml'
    searchFolder: '$(Build.ArtifactStagingDirectory)\TestResults'
```

---

## Exit Codes

All test runners return proper exit codes:
- **0** - All tests passed
- **1** - One or more tests failed
- **>1** - Error during test execution

---

## Test Results

Test results are saved in JUnit XML format for CI/CD integration:
- `powershell-test-results.xml` - PowerShell Pester results
- `csharp-test-results.xml` - C# xUnit results

---

## Current Test Status

| Suite | Tests | Status |
|-------|-------|--------|
| PowerShell (Dashboard) | 58 | ✅ All Passing |
| C# (Launcher) | 94 | ✅ All Passing |
| Integration | Variable | ⚠️ Optional |

**Total: 152 automated tests**

---

## Troubleshooting

### Pester Not Found
```powershell
Install-Module -Name Pester -Force -SkipPublisherCheck -Scope CurrentUser
Import-Module Pester -MinimumVersion 5.0
```

### .NET SDK Not Found
Download from: https://dotnet.microsoft.com/download

### Tests Fail to Run
1. Ensure you're in the `Tests` directory
2. Check PowerShell execution policy: `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`
3. Verify module paths are correct
4. Check for file locks on test assemblies

---

**Last Updated:** January 2026
