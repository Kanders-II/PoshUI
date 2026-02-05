# Security

PoshUI is designed with security as a core principle. This page outlines the security features and best practices when using PoshUI in your environment.

## Code Signing

All PoshUI executables are digitally signed with an Extended Validation (EV) certificate to ensure authenticity and integrity.

### Verification

You can verify the digital signature of PoshUI executables:

```powershell
# Verify the signature
Get-AuthenticodeSignature -FilePath ".\PoshUI.exe"

# Check signature status
$sig = Get-AuthenticodeSignature -FilePath ".\PoshUI.exe"
if ($sig.Status -eq 'Valid') {
    Write-Host "Signature is valid" -ForegroundColor Green
    Write-Host "Signer: $($sig.SignerCertificate.Subject)"
}
```

### Why Code Signing Matters

- **Authenticity**: Confirms the executable comes from A Solution IT LLC
- **Integrity**: Ensures the file hasn't been tampered with since signing
- **Trust**: Windows SmartScreen and antivirus software recognize signed executables
- **Compliance**: Meets enterprise security policies requiring signed code

## Execution Model

### PowerShell Execution

PoshUI executes PowerShell scripts in isolated runspaces with the following security characteristics:

- **Isolated Sessions**: Each script execution runs in its own PowerShell runspace
- **No Persistent State**: Scripts cannot access variables or state from other executions
- **User Context**: Scripts run with the permissions of the user who launched PoshUI
- **No Elevation**: PoshUI does not automatically elevate privileges

### Script Validation

When you provide scripts to PoshUI:

1. **AST Parsing**: Scripts are parsed using PowerShell's Abstract Syntax Tree parser
2. **Parameter Discovery**: Only parameter definitions are extracted for UI generation
3. **No Pre-Execution**: Scripts are not executed until the user explicitly triggers them
4. **User Confirmation**: ScriptCards require user interaction before execution

## Data Handling

### Input Validation

- **Type Safety**: Parameters are validated against their declared types
- **Attribute Validation**: PowerShell validation attributes (`[ValidateSet]`, `[ValidateRange]`, etc.) are enforced
- **Mandatory Fields**: Required parameters are enforced before script execution
- **Path Validation**: File and folder paths are validated for existence when appropriate

### Data Storage

- **No Persistent Storage**: PoshUI does not store user input or script results
- **Temporary Files**: Generated scripts are written to `$env:TEMP` and cleaned up after execution
- **Memory Only**: UI state and data exist only in memory during runtime
- **No Telemetry**: PoshUI does not collect or transmit usage data

## Network Security

### No External Dependencies

- **Offline Operation**: PoshUI works completely offline with no internet requirement
- **No Phone Home**: No telemetry, analytics, or update checks
- **No External Libraries**: Zero third-party dependencies that could introduce supply chain risks
- **Air-Gapped Compatible**: Fully functional in isolated networks

### Script Network Access

Scripts executed by PoshUI have the same network access as the user:

- PoshUI does not restrict or enable network access
- Scripts inherit the user's network permissions and proxy settings
- Firewall rules and network policies apply normally

## Execution Policy

PoshUI respects PowerShell's execution policy:

```powershell
# Check current execution policy
Get-ExecutionPolicy

# PoshUI requires one of these policies:
# - RemoteSigned (recommended)
# - Unrestricted
# - Bypass
```

### Recommended Policy

```powershell
# Set execution policy for current user (no admin required)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

## Best Practices

### For Script Authors

1. **Validate Input**: Use PowerShell validation attributes on parameters
2. **Least Privilege**: Don't require admin rights unless absolutely necessary
3. **Error Handling**: Use try/catch blocks to handle errors gracefully
4. **Logging**: Log actions for audit trails when appropriate
5. **Sensitive Data**: Avoid hardcoding credentials or secrets in scripts

### For IT Administrators

1. **Code Review**: Review scripts before deploying them in ScriptCards
2. **Access Control**: Use NTFS permissions to control who can run PoshUI scripts
3. **Audit Logging**: Enable PowerShell script block logging for audit trails
4. **Network Isolation**: Deploy PoshUI tools on appropriate network segments
5. **Regular Updates**: Keep PoshUI updated to receive security improvements

### For End Users

1. **Verify Signature**: Check that PoshUI.exe is properly signed before running
2. **Trusted Sources**: Only run PoshUI scripts from trusted IT sources
3. **Review Output**: Monitor script output for unexpected behavior
4. **Report Issues**: Report suspicious behavior to your IT security team

## Security Considerations

### What PoshUI Does NOT Do

- ❌ Does not bypass Windows security features
- ❌ Does not elevate privileges automatically
- ❌ Does not disable antivirus or security software
- ❌ Does not access the internet or send data externally
- ❌ Does not store credentials or sensitive data
- ❌ Does not modify system files or registry without explicit script commands

### What PoshUI DOES Do

- ✅ Executes PowerShell scripts with user's current permissions
- ✅ Validates input before passing to scripts
- ✅ Isolates script executions in separate runspaces
- ✅ Provides a signed, trusted executable
- ✅ Respects Windows security policies and execution policies
- ✅ Operates transparently with no hidden functionality

## Reporting Security Issues

If you discover a security vulnerability in PoshUI, please report it responsibly:

1. **Do not** create a public GitHub issue
2. Email security details to: [security contact - to be added]
3. Include steps to reproduce the issue
4. Allow time for a fix before public disclosure

We take security seriously and will respond promptly to legitimate security concerns.

## Compliance

PoshUI is designed to support enterprise security requirements:

- **Code Signing**: All executables are EV signed
- **Audit Trail**: Compatible with PowerShell script block logging
- **No Data Leakage**: No external communication or data storage
- **Transparent Operation**: Open source code available for review
- **Standard Permissions**: Works with standard user permissions (scripts may require elevation)

---

Next: [Validation](./validation.md)
