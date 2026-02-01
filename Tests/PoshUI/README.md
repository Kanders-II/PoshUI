# PoshUI Test Suite

Comprehensive automated tests for PoshUI modules using **Pester** (PowerShell testing framework).

## Test Categories

| Test File | Coverage |
|-----------|----------|
| `PoshUI.Wizard.Tests.ps1` | Module loading, core cmdlets, all control types |
| `PoshUI.Dashboard.Tests.ps1` | Dashboard creation, card types, banner features |
| `PoshUI.Workflow.Tests.ps1` | Workflow execution, task management, approval gates |
| `PoshUI.Security.Tests.ps1` | Injection prevention, path traversal, XSS protection |
| `PoshUI.Serialization.Tests.ps1` | JSON handling, carousel slides, special characters |
| `PoshUI.ControlLogic.Tests.ps1` | Validation rules, defaults, ranges, dependencies |

## Prerequisites

### Pester Module (v5+)
```powershell
# Install Pester if not present
Install-Module -Name Pester -Force -SkipPublisherCheck -Scope CurrentUser

# Verify installation
Get-Module -Name Pester -ListAvailable
```

## Running Tests

### Run All Tests
```powershell
cd Tests\PoshUI
.\Run-PesterTests.ps1
```

### Run Specific Module Tests
```powershell
.\Run-PesterTests.ps1 -ModuleFilter Wizard
.\Run-PesterTests.ps1 -ModuleFilter Dashboard
.\Run-PesterTests.ps1 -ModuleFilter Workflow
.\Run-PesterTests.ps1 -ModuleFilter Security
.\Run-PesterTests.ps1 -ModuleFilter Serialization
```

### Generate JUnit XML Report (for CI/CD)
```powershell
.\Run-PesterTests.ps1 -OutputPath .\test-results.xml
```

### Get Test Results Object
```powershell
$results = .\Run-PesterTests.ps1 -PassThru
$results | Select-Object TotalCount, PassedCount, FailedCount
```

### Run Individual Test Files
```powershell
Invoke-Pester -Path .\PoshUI.Security.Tests.ps1 -Output Detailed
```

## Test Coverage

### Module Loading Tests
- Module file existence
- Successful import
- Cmdlet export verification
- Core cmdlet presence

### Control Tests
- **TextBox**: Required params, defaults, validation patterns, placeholders
- **Password**: Min length, complexity requirements
- **Dropdown**: Static choices, dynamic ScriptBlock, cascading dependencies
- **Numeric**: Min/Max ranges, step increments
- **Checkbox/Toggle**: Default states, mandatory behavior
- **Banner**: Gradients, images, carousel slides
- **Cards**: Metric, Graph, DataGrid, Script cards

### Security Tests
- **Injection Prevention**: Special characters, blacklisted terms
- **Path Traversal**: Directory traversal attempts
- **Script Injection**: Command injection in defaults
- **XSS Prevention**: HTML/JavaScript in labels
- **Resource Exhaustion**: Length limits, collection sizes

### Serialization Tests
- Basic types (string, number, boolean, null)
- Arrays and nested objects
- Carousel slide structures
- Unicode and escape sequences

## CI/CD Integration

### GitHub Actions
```yaml
- name: Run PoshUI Tests
  shell: pwsh
  run: |
    cd Tests\PoshUI
    .\Run-PesterTests.ps1 -OutputPath test-results.xml
    
- name: Upload Test Results
  uses: actions/upload-artifact@v3
  with:
    name: test-results
    path: Tests\PoshUI\test-results.xml
```

### Azure DevOps
```yaml
- task: PowerShell@2
  inputs:
    targetType: 'filePath'
    filePath: 'Tests\PoshUI\Run-PesterTests.ps1'
    arguments: '-OutputPath $(Build.ArtifactStagingDirectory)\test-results.xml'
    
- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'JUnit'
    testResultsFiles: '**/test-results.xml'
```

## Writing New Tests

### Test Structure
```powershell
#Requires -Modules Pester

BeforeAll {
    $modulePath = Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1'
    Import-Module $modulePath -Force
}

Describe 'Feature Name' {
    Context 'Specific Scenario' {
        BeforeEach {
            # Setup for each test
            New-PoshUIWizard -Title 'Test'
            Add-UIStep -Name 'Step1' -Title 'Step' -Order 1
        }

        It 'Should behave correctly' {
            { Add-UITextBox -Step 'Step1' -Name 'Field' -Label 'Test' } | Should -Not -Throw
        }

        It 'Should reject invalid input' {
            { Add-UITextBox -Step 'InvalidStep' -Name 'Field' -Label 'Test' } | Should -Throw
        }
    }
}

AfterAll {
    Remove-Module PoshUI.Wizard -Force -ErrorAction SilentlyContinue
}
```

### Assertion Examples
```powershell
# Should not throw
{ Some-Command } | Should -Not -Throw

# Should throw
{ Invalid-Command } | Should -Throw

# Value comparison
$result | Should -Be 'Expected'
$result | Should -Not -BeNullOrEmpty
$result | Should -BeGreaterThan 0
$result | Should -Contain 'Item'
$result | Should -Match 'Pattern'
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All tests passed |
| N | N tests failed |

## Troubleshooting

### Module Not Found
Ensure you're running from the correct directory:
```powershell
cd D:\Sandbox\PoshUI_DotNet4.8\Tests\PoshUI
```

### Pester Version Issues
Update to Pester v5+:
```powershell
Install-Module -Name Pester -Force -SkipPublisherCheck
Import-Module Pester -MinimumVersion 5.0.0
```

### Test Isolation
Each test should be independent. Use `BeforeEach` to reset state:
```powershell
BeforeEach {
    Remove-Variable -Name CurrentWizard -Scope Script -ErrorAction SilentlyContinue
    New-PoshUIWizard -Title 'Fresh Wizard'
}
```

---

**Last Updated:** January 2026
