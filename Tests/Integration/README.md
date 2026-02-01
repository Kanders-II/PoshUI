# PoshWizard Integration Tests

This folder contains PowerShell integration tests for PoshWizard.

## Test Files

- **Test-ModuleLoading.ps1** - Verifies module loads and exports cmdlets
- **Test-DemoScripts.ps1** - Validates demo scripts run without errors

## Running Tests

### Run All Tests
```powershell
cd Tests
.\Run-AllTests.ps1
```

### Run All Tests (CI/CD Mode - Non-Interactive)
```powershell
.\Run-AllTests.ps1 -SkipInteractive
```

### Run Individual Test
```powershell
.\Integration\Test-ModuleLoading.ps1
.\Integration\Test-DemoScripts.ps1 -SkipInteractive
```

## Test Output

Tests use color-coded output:
- **Green** = PASS
- **Red** = FAIL
- **Yellow** = Test in progress
- **Cyan** = Headers/sections
- **Gray** = Details

## Creating New Tests

1. Create a new file: `Test-FeatureName.ps1`
2. Include proper help documentation
3. Use consistent output formatting
4. Exit with code 1 on failure, 0 on success
5. Support `-SkipInteractive` parameter for CI/CD

## CI/CD Integration

Tests are designed to run in CI/CD pipelines:

```yaml
# Example GitHub Actions
- name: Run Integration Tests
  run: |
    cd Tests
    .\Run-AllTests.ps1 -SkipInteractive
```

## Best Practices

- Keep tests focused and independent
- Use descriptive test names
- Include error handling
- Provide clear pass/fail messages
- Support both interactive and non-interactive modes
- Exit with appropriate codes for CI/CD
