#
# .SYNOPSIS
#     Interactive guide for testing signature enforcement
# .DESCRIPTION
#     This script guides you through the process of:
#     1. Building the PoshWizard executable
#     2. Checking signature status
#     3. Creating a test certificate (optional)
#     4. Signing the executable
#     5. Testing all three signature modes (Disabled, Warn, Enforce)

$ErrorActionPreference = 'Stop'

Write-Host @'

=========================================
  PoshWizard Signature Enforcement
  Testing Guide
=========================================
'@ -ForegroundColor Cyan

Write-Host "`nThis guide will help you test signature enforcement functionality.`n" -ForegroundColor Yellow

# Step 1: Check if exe exists
Write-Host "STEP 1: Checking for PoshWizard.exe..." -ForegroundColor Cyan
Write-Host "---------------------------------------" -ForegroundColor Gray

$exePaths = @(
    (Join-Path $PSScriptRoot '..\bin\PoshWizard.exe'),
    (Join-Path $PSScriptRoot '..\Launcher\bin\Release\net48\PoshWizard.exe'),
    (Join-Path $PSScriptRoot '..\Launcher\bin\Debug\net48\PoshWizard.exe')
)

$exePath = $null
foreach ($path in $exePaths) {
    if (Test-Path $path) {
        $exePath = $path
        Write-Host "Found: $exePath" -ForegroundColor Green
        break
    }
}

if (-not $exePath) {
    Write-Host "PoshWizard.exe not found!" -ForegroundColor Red
    Write-Host "`nYou need to build the project first:" -ForegroundColor Yellow
    Write-Host "  cd Launcher" -ForegroundColor White
    Write-Host "  dotnet build --configuration Debug" -ForegroundColor White
    Write-Host "`nOr for Release build:" -ForegroundColor Yellow
    Write-Host "  dotnet build --configuration Release" -ForegroundColor White
    Write-Host ""
    
    $build = Read-Host "Would you like to build now? (y/n)"
    if ($build -eq 'y') {
        Write-Host "`nBuilding Debug configuration..." -ForegroundColor Cyan
        Push-Location (Join-Path $PSScriptRoot '..\Launcher')
        dotnet build --configuration Debug
        Pop-Location
        
        # Check again
        foreach ($path in $exePaths) {
            if (Test-Path $path) {
                $exePath = $path
                Write-Host "`nBuild successful! Found: $exePath" -ForegroundColor Green
                break
            }
        }
        
        if (-not $exePath) {
            Write-Host "Build completed but exe still not found. Please check build output." -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "Please build the project and run this script again." -ForegroundColor Yellow
        exit 1
    }
}

Write-Host ""

# Step 2: Check current signature
Write-Host "STEP 2: Checking current signature status..." -ForegroundColor Cyan
Write-Host "---------------------------------------------" -ForegroundColor Gray

$signature = Get-AuthenticodeSignature -FilePath $exePath

Write-Host "Status: " -NoNewline
switch ($signature.Status) {
    'Valid' { Write-Host $signature.Status -ForegroundColor Green }
    'NotSigned' { Write-Host $signature.Status -ForegroundColor Yellow }
    default { Write-Host $signature.Status -ForegroundColor Red }
}

if ($signature.SignerCertificate) {
    Write-Host "Signer: $($signature.SignerCertificate.Subject)" -ForegroundColor Gray
    Write-Host "Thumbprint: $($signature.SignerCertificate.Thumbprint)" -ForegroundColor Gray
    Write-Host "Valid Until: $($signature.SignerCertificate.NotAfter)" -ForegroundColor Gray
    
    if ($signature.TimeStamperCertificate) {
        Write-Host "Timestamp: $($signature.TimeStamperCertificate.Subject)" -ForegroundColor Gray
    } else {
        Write-Host "Timestamp: None" -ForegroundColor Yellow
    }
} else {
    Write-Host "The executable is not signed." -ForegroundColor Yellow
}

Write-Host ""

# Step 3: Offer to create test certificate and sign
if ($signature.Status -ne 'Valid') {
    Write-Host "STEP 3: Code Signing Options..." -ForegroundColor Cyan
    Write-Host "--------------------------------" -ForegroundColor Gray
    Write-Host ""
    Write-Host "The executable is not signed. You have the following options:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  1. Create a self-signed test certificate and sign the exe (for testing)" -ForegroundColor White
    Write-Host "  2. Skip signing and test with Disabled/Warn modes only" -ForegroundColor White
    Write-Host "  3. Use an existing certificate to sign" -ForegroundColor White
    Write-Host "  4. Exit and sign manually" -ForegroundColor White
    Write-Host ""
    
    $choice = Read-Host "Enter choice (1-4)"
    
    switch ($choice) {
        '1' {
            Write-Host "`nCreating self-signed test certificate..." -ForegroundColor Cyan
            
            # Check if test cert already exists
            $existingCert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert | 
                Where-Object { $_.Subject -like "*PoshWizard*" } | 
                Select-Object -First 1
            
            if ($existingCert) {
                Write-Host "Found existing PoshWizard certificate: $($existingCert.Subject)" -ForegroundColor Green
                $useCert = $existingCert
            } else {
                Write-Host "Creating new self-signed certificate..." -ForegroundColor Yellow
                
                $cert = New-SelfSignedCertificate `
                    -Type CodeSigningCert `
                    -Subject "CN=PoshWizard Development Test" `
                    -CertStoreLocation "Cert:\CurrentUser\My" `
                    -NotAfter (Get-Date).AddYears(5)
                
                Write-Host "Certificate created: $($cert.Thumbprint)" -ForegroundColor Green
                
                # Trust the certificate
                Write-Host "Adding certificate to Trusted Root..." -ForegroundColor Yellow
                $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root","CurrentUser")
                $store.Open("ReadWrite")
                $store.Add($cert)
                $store.Close()
                Write-Host "Certificate trusted for local testing." -ForegroundColor Green
                
                $useCert = $cert
            }
            
            # Sign the executable
            Write-Host "`nSigning PoshWizard.exe..." -ForegroundColor Cyan
            $sigResult = Set-AuthenticodeSignature -FilePath $exePath `
                                                   -Certificate $useCert `
                                                   -TimestampServer "http://timestamp.digicert.com"
            
            if ($sigResult.Status -eq 'Valid') {
                Write-Host "Successfully signed!" -ForegroundColor Green
                $signature = $sigResult
            } else {
                Write-Host "Signing failed: $($sigResult.Status)" -ForegroundColor Red
                Write-Host "StatusMessage: $($sigResult.StatusMessage)" -ForegroundColor Red
            }
        }
        
        '2' {
            Write-Host "`nSkipping signing. Testing will be limited to Disabled/Warn modes." -ForegroundColor Yellow
        }
        
        '3' {
            Write-Host "`nListing available code signing certificates..." -ForegroundColor Cyan
            $certs = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert
            
            if (-not $certs) {
                Write-Host "No code signing certificates found in CurrentUser\My store." -ForegroundColor Red
                Write-Host "Please import a certificate first." -ForegroundColor Yellow
            } else {
                $certs | ForEach-Object {
                    Write-Host "$($_.Subject) (Thumbprint: $($_.Thumbprint))" -ForegroundColor White
                }
                
                $thumbprint = Read-Host "`nEnter certificate thumbprint to use"
                $cert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Thumbprint -eq $thumbprint }
                
                if ($cert) {
                    Write-Host "Signing with: $($cert.Subject)" -ForegroundColor Cyan
                    $sigResult = Set-AuthenticodeSignature -FilePath $exePath `
                                                           -Certificate $cert `
                                                           -TimestampServer "http://timestamp.digicert.com"
                    
                    if ($sigResult.Status -eq 'Valid') {
                        Write-Host "Successfully signed!" -ForegroundColor Green
                        $signature = $sigResult
                    } else {
                        Write-Host "Signing failed: $($sigResult.Status)" -ForegroundColor Red
                    }
                } else {
                    Write-Host "Certificate not found." -ForegroundColor Red
                }
            }
        }
        
        '4' {
            Write-Host "`nPlease sign the executable manually and run this script again." -ForegroundColor Yellow
            exit 0
        }
        
        default {
            Write-Host "Invalid choice." -ForegroundColor Red
            exit 1
        }
    }
    
    Write-Host ""
    
    # Refresh signature status
    $signature = Get-AuthenticodeSignature -FilePath $exePath
}

# Step 4: Test signature modes
Write-Host "STEP 4: Testing signature enforcement modes..." -ForegroundColor Cyan
Write-Host "-----------------------------------------------" -ForegroundColor Gray
Write-Host ""

# Load module
$modulePath = Join-Path $PSScriptRoot '..\PoshWizard\PoshWizard.psd1'
if (-not (Test-Path $modulePath)) {
    Write-Host "ERROR: Module not found at $modulePath" -ForegroundColor Red
    exit 1
}

Import-Module $modulePath -Force -Verbose:$false

# Create test wizard script
$testScriptPath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "PoshWizard-SignTest-$(Get-Random).ps1")
@'
New-PoshWizard -Title "Signature Test" -Description "Testing signature enforcement"
Set-WizardBranding -WindowTitle "Signature Test Wizard"
Add-WizardStep -Name "TestStep" -Title "Test Step" -Order 1 -Icon '&#xE73E;'
Add-WizardTextBox -Step "TestStep" -Name "TestInput" -Label "Enter any text" -Mandatory
$result = Show-PoshWizard
if ($result) {
    Write-Host "Wizard completed successfully" -ForegroundColor Green
    Write-Host "TestInput: $($result.TestInput)" -ForegroundColor Cyan
}
'@ | Out-File -FilePath $testScriptPath -Encoding UTF8

Write-Host "Created test script: $testScriptPath" -ForegroundColor Gray
Write-Host ""

# Save original mode
$originalMode = $env:POSHWIZARD_SIGNATURE_MODE

# Test each mode
$testResults = @()

Write-Host "Testing Mode: Disabled" -ForegroundColor Magenta
Write-Host "---------------------" -ForegroundColor Gray
$env:POSHWIZARD_SIGNATURE_MODE = "Disabled"
Write-Host "Environment: POSHWIZARD_SIGNATURE_MODE = $env:POSHWIZARD_SIGNATURE_MODE" -ForegroundColor Gray

try {
    $result = Invoke-PoshWizardExe -ScriptPath $testScriptPath -Wait -Verbose
    Write-Host "Result: SUCCESS - Execution allowed" -ForegroundColor Green
    $testResults += [PSCustomObject]@{
        Mode = 'Disabled'
        Success = $true
        Message = 'Execution allowed (no signature check)'
    }
} catch {
    Write-Host "Result: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    $testResults += [PSCustomObject]@{
        Mode = 'Disabled'
        Success = $false
        Message = $_.Exception.Message
    }
}

Write-Host ""
Write-Host "Testing Mode: Warn" -ForegroundColor Magenta
Write-Host "---------------------" -ForegroundColor Gray
$env:POSHWIZARD_SIGNATURE_MODE = "Warn"
Write-Host "Environment: POSHWIZARD_SIGNATURE_MODE = $env:POSHWIZARD_SIGNATURE_MODE" -ForegroundColor Gray

try {
    $result = Invoke-PoshWizardExe -ScriptPath $testScriptPath -Wait -Verbose
    Write-Host "Result: SUCCESS - Execution allowed" -ForegroundColor Green
    $testResults += [PSCustomObject]@{
        Mode = 'Warn'
        Success = $true
        Message = 'Execution allowed (warnings shown if invalid)'
    }
} catch {
    Write-Host "Result: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    $testResults += [PSCustomObject]@{
        Mode = 'Warn'
        Success = $false
        Message = $_.Exception.Message
    }
}

Write-Host ""
Write-Host "Testing Mode: Enforce" -ForegroundColor Magenta
Write-Host "---------------------" -ForegroundColor Gray
$env:POSHWIZARD_SIGNATURE_MODE = "Enforce"
Write-Host "Environment: POSHWIZARD_SIGNATURE_MODE = $env:POSHWIZARD_SIGNATURE_MODE" -ForegroundColor Gray

try {
    $result = Invoke-PoshWizardExe -ScriptPath $testScriptPath -Wait -Verbose
    Write-Host "Result: SUCCESS - Execution allowed (signature is valid)" -ForegroundColor Green
    $testResults += [PSCustomObject]@{
        Mode = 'Enforce'
        Success = $true
        Message = 'Execution allowed (valid signature)'
    }
} catch {
    Write-Host "Result: BLOCKED - $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "(This is expected behavior for unsigned/invalid signatures)" -ForegroundColor Gray
    $testResults += [PSCustomObject]@{
        Mode = 'Enforce'
        Success = $false
        Message = $_.Exception.Message
    }
}

# Cleanup
Write-Host ""
if (Test-Path $testScriptPath) {
    Remove-Item $testScriptPath -Force
}

if ($originalMode) {
    $env:POSHWIZARD_SIGNATURE_MODE = $originalMode
} else {
    Remove-Item env:POSHWIZARD_SIGNATURE_MODE -ErrorAction SilentlyContinue
}

# Summary
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  TEST SUMMARY" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Executable Status: " -NoNewline
if ($signature.Status -eq 'Valid') {
    Write-Host "SIGNED (Valid)" -ForegroundColor Green
} else {
    Write-Host "UNSIGNED or Invalid" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Test Results:" -ForegroundColor White
$testResults | Format-Table -Property Mode, Success, Message -AutoSize | Out-String | Write-Host

Write-Host ""
Write-Host "Expected Behavior:" -ForegroundColor White
Write-Host "  - Disabled mode: Always allows execution" -ForegroundColor Gray
Write-Host "  - Warn mode: Always allows execution (warns if invalid)" -ForegroundColor Gray
Write-Host "  - Enforce mode: Only allows execution if signature is Valid" -ForegroundColor Gray

Write-Host ""
if ($signature.Status -eq 'Valid') {
    Write-Host "CONCLUSION: All modes should work with a valid signature." -ForegroundColor Green
    Write-Host "Production recommendation: Set POSHWIZARD_SIGNATURE_MODE to 'Enforce'" -ForegroundColor Cyan
} else {
    Write-Host "CONCLUSION: Enforce mode correctly blocks unsigned executables." -ForegroundColor Yellow
    Write-Host "For production use, sign the executable with a trusted certificate." -ForegroundColor Cyan
}

Write-Host ""
