# PoshUI Testing Suite

Comprehensive testing infrastructure for PoshUI with CI/CD integration and security validation.

## Quick Start

### Run All Tests
```powershell
.\Run-AllTests.ps1
```

### Run Security Tests
```powershell
.\Run-SecurityTests.ps1
```

### Run All Tests (CI/CD Mode)
```powershell
.\Run-AllTests.ps1 -SkipInteractive
```

## Test Structure

```
Tests/
├── Integration/              # PowerShell integration tests
│   ├── Test-ModuleLoading.ps1
│   ├── Test-DemoScripts.ps1
│   ├── Test-CmdletParameters.ps1
│   ├── Test-ErrorHandling.ps1
│   └── README.md
├── Run-AllTests.ps1          # Master test runner
└── CI-CD-TESTING-GUIDE.md    # Comprehensive CI/CD documentation

Launcher.Tests/               # C# unit tests (xUnit)
├── ModuleLoadingTests.cs
├── ThemeTests.cs
├── ParameterValidationTests.cs
├── ScriptPreprocessingTests.cs
└── DynamicParameterManagerTests.cs

.github/
└── workflows/
    └── ci-tests.yml          # GitHub Actions CI/CD pipeline
```

## Test Categories

### 1. Security Tests (Critical)

**What**: Encryption, integrity, and access control validation
**Coverage**: DPAPI encryption, HMAC integrity, ACLs, secure wipe, tamper detection
**Run**: `.\Run-SecurityTests.ps1`

Tests:
- ✅ Workflow state encryption (DPAPI)
- ✅ HMAC-SHA256 integrity validation
- ✅ File ACL restrictions
- ✅ Secure wipe functionality
- ✅ Tamper detection
- ✅ Cross-user isolation
- ✅ Backward compatibility

**Documentation**: [SECURITY-TESTING-GUIDE.md](SECURITY-TESTING-GUIDE.md)

### 2. PowerShell Integration Tests (Primary)

**What**: End-to-end testing of PowerShell module functionality
**Coverage**: Module loading, cmdlets, demo scripts, error handling
**Run**: `.\Run-AllTests.ps1`

Tests:
- ✅ Module imports correctly
- ✅ All cmdlets exported
- ✅ Parameters validated
- ✅ Demo scripts run without errors
- ✅ Error handling works
- ✅ Edge cases covered

### 3. C# Unit Tests (Secondary)

**What**: Internal Launcher component testing
**Coverage**: Script parsing, parameter management, theme logic
**Run**: `dotnet test WizardFramework.sln`

Tests:
- ✅ Module manifest validation
- ✅ Theme detection
- ✅ Parameter validation
- ✅ Script preprocessing
- ✅ Dynamic parameters

### 4. CI/CD Pipeline (Automated)

**What**: GitHub Actions workflow
**Coverage**: Full build, test, and validation pipeline
**Trigger**: Push to main/master/develop or PR

Jobs:
- ✅ Security tests
- ✅ PowerShell integration tests
- ✅ C# unit tests
- ✅ Build validation (Debug + Release)
- ✅ Module validation
- ✅ Security scanning
- ✅ Test summary

## Test Results

All tests provide:
- **Color-coded output** (Green=Pass, Red=Fail, Yellow=In Progress)
- **Pass/Fail counts**
- **Detailed error messages**
- **Proper exit codes** (0=success, 1=failure)

## CI/CD Integration

### GitHub Actions

Automatically runs on:
- Push to `main`, `master`, or `develop`
- Pull requests
- Manual dispatch

View results:
- Actions tab in GitHub
- Test artifacts uploaded for 90 days
- Build artifacts available for download

### Local CI/CD Testing

Simulate CI/CD environment locally:
```powershell
# Run all tests in non-interactive mode
.\Run-AllTests.ps1 -SkipInteractive

# Build and test
dotnet build WizardFramework.sln --configuration Release
dotnet test WizardFramework.sln --configuration Release
```

## Adding New Tests

### PowerShell Test
1. Create `Test-FeatureName.ps1` in `Integration/`
2. Follow template in CI-CD-TESTING-GUIDE.md
3. Support `-SkipInteractive` parameter
4. Exit with proper code (0 or 1)

### C# Test
1. Add test class to `Launcher.Tests/`
2. Use xUnit `[Fact]` or `[Theory]`
3. Follow AAA pattern (Arrange, Act, Assert)
4. Tests run automatically in CI/CD

## Documentation

- **[SECURITY-TESTING-GUIDE.md](SECURITY-TESTING-GUIDE.md)** - Security testing procedures
- **[Integration/README.md](Integration/README.md)** - Integration tests guide
- **[CI-CD-TESTING-GUIDE.md](CI-CD-TESTING-GUIDE.md)** - Complete CI/CD documentation
- **[../Docs/CONTRIBUTING.md](../Docs/CONTRIBUTING.md)** - Contributor testing guidelines

## Test Coverage

| Area | Coverage | Status |
|------|----------|--------|
| Security | Encryption, integrity, ACLs | ✅ Complete |
| PowerShell Module | 100% cmdlets | ✅ Complete |
| Demo Scripts | 100% demos | ✅ Complete |
| C# Launcher | >70% critical paths | ✅ Complete |
| Error Handling | All failure scenarios | ✅ Complete |
| Workflow State | Save/load/resume | ✅ Complete |

## Troubleshooting

**Tests fail locally but pass in CI/CD:**
- Check for hardcoded paths
- Verify module imports work from test location
- Ensure no interactive prompts

**Module import fails:**
- Run `Test-ModuleManifest PoshWizard\PoshWizard.psd1`
- Check for syntax errors in .ps1 files
- Verify all files are present

**Build fails:**
- Ensure .NET Framework 4.8 SDK installed
- Run `dotnet restore WizardFramework.sln`
- Check build logs for specific errors

## Support

- **Issues**: [GitHub Issues](https://github.com/asolutionit/PoshWizard/issues)
- **Email**: support@asolutionit.com
- **Docs**: See CONTRIBUTING.md for testing guidelines

---

**Last Updated**: November 2025
**Maintained by**: A Solution IT LLC
