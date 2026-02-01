#
# .SYNOPSIS
#     Tests enforced signature verification for PoshWizard.exe
# .DESCRIPTION
#     This test script validates that PoshWizard properly enforces code signature
#     verification when POSHWIZARD_SIGNATURE_MODE is set to "Enforce".
#     
#     Test scenarios:
#     1. Disabled mode - No signature check (should always work)
#     2. Warn mode - Signature checked but execution continues even if invalid
#     3. Enforce mode - Signature must be valid or execution is blocked

$ErrorActionPreference = 'Stop'

Write-Host @'

========================================
 PoshWizard Signature Enforcement Test
========================================
'@ -ForegroundColor Cyan

# Locate module and exe
$modulePath = Join-Path $PSScriptRoot '..\PoshWizard\PoshWizard.psd1'
if (-not (Test-Path $modulePath)) {
    throw "Module not found at: $modulePath"
}

Import-Module $modulePath -Force

# Find the exe
$exePaths = @(
    (Join-Path $PSScriptRoot '..\PoshWizard\bin\Poshwizard.exe'),
    (Join-Path $PSScriptRoot '..\Launcher\bin\Release\Poshwizard.exe'),
    (Join-Path $PSScriptRoot '..\Launcher\bin\Debug\Poshwizard.exe'),
    (Join-Path $PSScriptRoot '..\Launcher.Tests\bin\Debug\net48\Poshwizard.exe')
)

$exePath = $null
foreach ($path in $exePaths) {
    if (Test-Path $path) {
        $exePath = $path
        break
    }
}

if (-not $exePath) {
    throw "PoshWizard.exe not found in any expected location"
}

Write-Host "Found PoshWizard.exe: $exePath" -ForegroundColor Green

# Check current signature status
Write-Host "`nChecking signature status..." -ForegroundColor Yellow
$signature = Get-AuthenticodeSignature -FilePath $exePath
Write-Host "  Status: $($signature.Status)" -ForegroundColor $(if ($signature.Status -eq 'Valid') { 'Green' } else { 'Red' })
if ($signature.SignerCertificate) {
    Write-Host "  Signer: $($signature.SignerCertificate.Subject)" -ForegroundColor Gray
    Write-Host "  Thumbprint: $($signature.SignerCertificate.Thumbprint)" -ForegroundColor Gray
    Write-Host "  Valid Until: $($signature.SignerCertificate.NotAfter)" -ForegroundColor Gray
} else {
    Write-Host "  No signature found" -ForegroundColor Red
}

# Create a simple test wizard script
$testScriptPath = [System.IO.Path]::GetTempFileName()
$testScriptPath = $testScriptPath -replace '\.tmp$', '.ps1'
@'
New-PoshWizard -Title "Signature Test" -Description "Testing signature enforcement"
Set-WizardBranding -WindowTitle "Test Wizard"
Add-WizardStep -Name "TestStep" -Title "Test" -Order 1
Add-WizardTextBox -Step "TestStep" -Name "TestInput" -Label "Test Field"
Show-PoshWizard
'@ | Out-File -FilePath $testScriptPath -Encoding UTF8

Write-Host "`nCreated test wizard script: $testScriptPath" -ForegroundColor Gray

# Save original mode
$originalMode = $env:POSHWIZARD_SIGNATURE_MODE
Write-Host "`nOriginal signature mode: $originalMode" -ForegroundColor Gray

# Test function
function Test-SignatureMode {
    param(
        [string]$Mode,
        [string]$ExpectedResult
    )
    
    Write-Host "`n--- Testing Mode: $Mode ---" -ForegroundColor Cyan
    $env:POSHWIZARD_SIGNATURE_MODE = $Mode
    
    try {
        $result = Invoke-PoshWizardExe -ScriptPath $testScriptPath -Wait -ErrorAction Stop
        $success = $true
        $message = "Execution succeeded (ExitCode: $($result.ExitCode))"
    } catch {
        $success = $false
        $message = $_.Exception.Message
    }
    
    Write-Host "  Result: $($success)" -ForegroundColor $(if ($success) { 'Green' } else { 'Red' })
    Write-Host "  Message: $message" -ForegroundColor Gray
    Write-Host "  Expected: $ExpectedResult" -ForegroundColor Gray
    
    # Determine if test passed
    $testPassed = $false
    switch ($ExpectedResult) {
        'Success' { $testPassed = $success }
        'Failure' { $testPassed = -not $success }
        'Either' { $testPassed = $true }
    }
    
    if ($testPassed) {
        Write-Host "  TEST PASSED" -ForegroundColor Green
    } else {
        Write-Host "  TEST FAILED" -ForegroundColor Red
    }
    
    return $testPassed
}

# Run tests
$results = @()

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " Running Signature Mode Tests" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Test 1: Disabled mode (should always work)
$results += Test-SignatureMode -Mode "Disabled" -ExpectedResult "Success"

# Test 2: Warn mode (should work regardless of signature, but may show warning)
$results += Test-SignatureMode -Mode "Warn" -ExpectedResult "Success"

# Test 3: Enforce mode (should fail if unsigned, succeed if signed)
if ($signature.Status -eq 'Valid') {
    $results += Test-SignatureMode -Mode "Enforce" -ExpectedResult "Success"
} else {
    $results += Test-SignatureMode -Mode "Enforce" -ExpectedResult "Failure"
}

# Test 4: Empty/null mode (should behave like default - Warn)
$results += Test-SignatureMode -Mode "" -ExpectedResult "Success"

# Cleanup
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " Cleanup" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

if (Test-Path $testScriptPath) {
    Remove-Item $testScriptPath -Force
    Write-Host "Removed test script: $testScriptPath" -ForegroundColor Gray
}

# Restore original mode
if ($originalMode) {
    $env:POSHWIZARD_SIGNATURE_MODE = $originalMode
    Write-Host "Restored signature mode to: $originalMode" -ForegroundColor Gray
} else {
    Remove-Item env:POSHWIZARD_SIGNATURE_MODE -ErrorAction SilentlyContinue
    Write-Host "Cleared signature mode (restored to default)" -ForegroundColor Gray
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " Test Summary" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$passCount = ($results | Where-Object { $_ -eq $true }).Count
$totalCount = $results.Count

Write-Host "Tests Passed: $passCount / $totalCount" -ForegroundColor $(if ($passCount -eq $totalCount) { 'Green' } else { 'Yellow' })

if ($passCount -eq $totalCount) {
    Write-Host "`nALL TESTS PASSED!" -ForegroundColor Green
} else {
    Write-Host "`nSOME TESTS FAILED" -ForegroundColor Red
}

# Recommendations based on signature status
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " Recommendations" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

if ($signature.Status -ne 'Valid') {
    Write-Host "UNSIGNED EXECUTABLE DETECTED" -ForegroundColor Red
    Write-Host ""
    Write-Host "The PoshWizard.exe is not signed with a valid code signing certificate." -ForegroundColor Yellow
    Write-Host "For production use, you should sign the executable with a code signing certificate." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "For development/testing, you can:" -ForegroundColor Yellow
    Write-Host "  - Use 'Disabled' mode: " -NoNewline -ForegroundColor White
    Write-Host '$env:POSHWIZARD_SIGNATURE_MODE = "Disabled"' -ForegroundColor Cyan
} else {
    Write-Host "SIGNED EXECUTABLE DETECTED" -ForegroundColor Green
    Write-Host ""
    Write-Host "The executable is properly signed and can be used in Enforce mode." -ForegroundColor Green
    Write-Host ""
    Write-Host "To enable enforce mode for production:" -ForegroundColor Yellow
    Write-Host '  $env:POSHWIZARD_SIGNATURE_MODE = "Enforce"' -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Or set it system-wide:" -ForegroundColor Yellow
    Write-Host '  [System.Environment]::SetEnvironmentVariable("POSHWIZARD_SIGNATURE_MODE", "Enforce", "Machine")' -ForegroundColor Cyan
}

Write-Host ""
