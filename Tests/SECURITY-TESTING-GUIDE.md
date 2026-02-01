# PoshUI Security Testing Guide

## Overview

This guide covers security testing procedures for PoshUI, focusing on encryption, integrity validation, access control, and secure data handling across all modules.

## Test Categories

### 1. Workflow State Encryption Tests

**Location**: `Tests\PoshUI\PoshUI.Workflow.Security.Tests.ps1`

**Coverage**:
- DPAPI encryption/decryption
- HMAC-SHA256 integrity validation
- File ACL restrictions
- Secure wipe functionality
- Tamper detection
- Cross-user isolation
- Backward compatibility

**Run Command**:
```powershell
.\Tests\Run-SecurityTests.ps1
```

### 2. Module Security Tests

**Location**: `Tests\PoshUI\PoshUI.Security.Tests.ps1`

**Coverage**:
- Temp file security
- Cryptographic random filename generation
- ACL validation across modules
- State file location security

### 3. Integration Security Tests

**Location**: `Tests\Integration\`

**Coverage**:
- Cross-module security consistency
- End-to-end encryption workflows
- Multi-user scenarios

## Quick Start

### Run All Security Tests

```powershell
cd Tests
.\Run-SecurityTests.ps1
```

### Run Specific Test Suite

```powershell
# Workflow security only
.\Run-SecurityTests.ps1 -TestPath ".\PoshUI\PoshUI.Workflow.Security.Tests.ps1"
```

### Generate Test Report

```powershell
.\Run-SecurityTests.ps1 -OutputFormat NUnitXml -OutputFile "SecurityTestResults.xml"
```

## Test Scenarios

### Encryption Validation

**Test**: State files are encrypted with DPAPI
```powershell
Describe "DPAPI Encryption" {
    It "Should encrypt state data" {
        # Creates workflow and saves state
        # Verifies raw file content is encrypted
        # Confirms no plain text leakage
    }
}
```

**Expected Result**: 
- File contains `POSHUI_STATE_V1:` header
- No plain text JSON visible
- Base64-encoded encrypted data

### Integrity Validation

**Test**: Tampered files are detected
```powershell
Describe "HMAC Integrity" {
    It "Should detect tampered files" {
        # Saves state file
        # Modifies file bytes
        # Attempts to load
    }
}
```

**Expected Result**:
- Load fails with integrity error
- Error message indicates tampering
- No data exposure in error

### ACL Validation

**Test**: File permissions are restrictive
```powershell
Describe "File Permissions" {
    It "Should set restrictive ACLs" {
        # Saves state file
        # Checks ACL configuration
    }
}
```

**Expected Result**:
- Inheritance disabled
- Only current user has access
- Full control for current user only

### Secure Wipe Validation

**Test**: Data is overwritten before deletion
```powershell
Describe "Secure Wipe" {
    It "Should overwrite file data" {
        # Saves state file
        # Performs secure wipe
        # Verifies deletion
    }
}
```

**Expected Result**:
- File overwritten with random data
- Second pass with zeros
- File deleted successfully

## Security Checklist

Before deploying PoshUI to production, verify:

- [ ] All security tests pass
- [ ] Workflow state files use `.dat` extension (encrypted)
- [ ] No plain text state files in production
- [ ] ACLs are set on state directories
- [ ] Secure wipe is used for sensitive workflows
- [ ] Error messages don't expose sensitive data
- [ ] Temp files are cleaned up
- [ ] Cross-user isolation is validated

## Manual Security Validation

### 1. Verify Encryption

```powershell
# Create a workflow and save state
New-PoshUIWorkflow -Title "Security Test" -Description "Manual validation"
Add-UIStep -Name "Test" -Title "Test" -Order 1
Save-UIWorkflowState

# Check the file
$statePath = Join-Path $env:LOCALAPPDATA 'PoshUI\PoshUI_Workflow_State.dat'
$content = Get-Content $statePath -Raw

# Should see:
# - POSHUI_STATE_V1: header
# - Base64 encoded data
# - No plain text
```

### 2. Verify ACLs

```powershell
# Check file permissions
$statePath = Join-Path $env:LOCALAPPDATA 'PoshUI\PoshUI_Workflow_State.dat'
$acl = Get-Acl $statePath

# Verify:
$acl.AreAccessRulesProtected  # Should be True
$acl.Access.Count              # Should be 1 (current user only)
```

### 3. Verify Tamper Detection

```powershell
# Save a state file
Save-UIWorkflowState

# Manually corrupt it
$statePath = Join-Path $env:LOCALAPPDATA 'PoshUI\PoshUI_Workflow_State.dat'
$bytes = [System.IO.File]::ReadAllBytes($statePath)
$bytes[50] = $bytes[50] -bxor 0xFF
[System.IO.File]::WriteAllBytes($statePath, $bytes)

# Try to load - should fail
Get-UIWorkflowState  # Should throw integrity error
```

### 4. Verify Secure Wipe

```powershell
# Create and wipe a state file
Save-UIWorkflowState
Clear-UIWorkflowState -SecureWipe -Confirm:$false

# File should be gone
Test-Path (Join-Path $env:LOCALAPPDATA 'PoshUI\PoshUI_Workflow_State.dat')  # Should be False
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Security Tests

on: [push, pull_request]

jobs:
  security-tests:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Install Pester
        shell: pwsh
        run: Install-Module Pester -Force -SkipPublisherCheck
      
      - name: Run Security Tests
        shell: pwsh
        run: |
          cd Tests
          .\Run-SecurityTests.ps1 -OutputFormat NUnitXml -OutputFile SecurityResults.xml
      
      - name: Upload Test Results
        uses: actions/upload-artifact@v3
        if: always()
        with:
          name: security-test-results
          path: Tests/SecurityResults.xml
```

### Azure DevOps Example

```yaml
steps:
- task: PowerShell@2
  displayName: 'Run Security Tests'
  inputs:
    targetType: 'filePath'
    filePath: 'Tests/Run-SecurityTests.ps1'
    arguments: '-OutputFormat NUnitXml -OutputFile $(Build.ArtifactStagingDirectory)/SecurityResults.xml'
    
- task: PublishTestResults@2
  displayName: 'Publish Security Test Results'
  condition: always()
  inputs:
    testResultsFormat: 'NUnit'
    testResultsFiles: '$(Build.ArtifactStagingDirectory)/SecurityResults.xml'
```

## Security Test Metrics

### Coverage Goals

- **Encryption**: 100% of state save/load operations
- **Integrity**: 100% of tamper scenarios
- **ACL**: 100% of file/directory creations
- **Secure Wipe**: 100% of deletion operations

### Performance Benchmarks

- Encryption: < 100ms for typical state file
- Decryption: < 50ms for typical state file
- ACL setting: < 10ms per file
- Secure wipe: < 500ms for typical state file

## Troubleshooting

### Test Failures

**Encryption tests fail**:
- Verify DPAPI is available (Windows only)
- Check user permissions
- Ensure .NET Framework 4.8 is installed

**ACL tests fail**:
- Run as regular user (not admin)
- Check NTFS file system
- Verify ACL cmdlets are available

**Integrity tests fail**:
- Verify HMAC implementation
- Check file corruption logic
- Ensure consistent entropy

### Common Issues

**Issue**: "DPAPI encryption failed"
**Solution**: Ensure running on Windows with user profile loaded

**Issue**: "ACL setting failed"
**Solution**: Verify NTFS file system and proper permissions

**Issue**: "Integrity check failed on valid file"
**Solution**: Check for consistent USERNAME and COMPUTERNAME in HMAC key

## Security Best Practices

1. **Always use encryption in production**
   - Never use `-NoEncryption` in production
   - Only for debugging/development

2. **Enable secure wipe for sensitive data**
   - Use `-SecureWipe` when clearing state with sensitive information
   - Consider automatic secure wipe on workflow completion

3. **Validate ACLs regularly**
   - Run security tests in CI/CD
   - Monitor file permissions in production

4. **Monitor for tampering**
   - Log integrity check failures
   - Alert on repeated failures

5. **Keep state files local**
   - Don't sync to cloud storage
   - Don't store on network shares
   - Use local user profile directory

## References

- [Windows Data Protection API (DPAPI)](https://docs.microsoft.com/windows/win32/seccng/cng-dpapi)
- [HMAC-SHA256](https://docs.microsoft.com/dotnet/api/system.security.cryptography.hmacsha256)
- [File System ACLs](https://docs.microsoft.com/windows/win32/secauthz/access-control-lists)
- [Pester Testing Framework](https://pester.dev/)

## Support

For security-related issues or questions:
- GitHub Issues: https://github.com/asolutionit/PoshUI/issues
- Tag with `security` label
- Include test results and environment details
