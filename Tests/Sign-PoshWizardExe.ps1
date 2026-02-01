#
# .SYNOPSIS
#     Signs PoshWizard.exe with a test certificate
# .DESCRIPTION
#     Creates a self-signed code signing certificate and signs the PoshWizard executable

$ErrorActionPreference = 'Stop'

Write-Host "Creating and using self-signed certificate for testing..." -ForegroundColor Cyan

# Check if test cert already exists
$existingCert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert | 
    Where-Object { $_.Subject -like "*PoshWizard*" } | 
    Select-Object -First 1

if ($existingCert) {
    Write-Host "Found existing PoshWizard certificate: $($existingCert.Subject)" -ForegroundColor Green
    $cert = $existingCert
} else {
    Write-Host "Creating new self-signed certificate..." -ForegroundColor Yellow
    
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject "CN=PoshWizard Test Certificate" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -NotAfter (Get-Date).AddYears(5)
    
    Write-Host "Certificate created: $($cert.Thumbprint)" -ForegroundColor Green
    
    # Trust the certificate
    Write-Host "Adding certificate to Trusted Root..." -ForegroundColor Yellow
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root","CurrentUser")
    $store.Open("ReadWrite")
    $store.Add($cert)
    $store.Close()
    Write-Host "Certificate trusted." -ForegroundColor Green
}

# Find the exe
$exePath = Join-Path $PSScriptRoot '..\PoshWizard\bin\Poshwizard.exe'
if (-not (Test-Path $exePath)) {
    throw "PoshWizard.exe not found at: $exePath"
}

Write-Host "`nSigning: $exePath" -ForegroundColor Cyan

# Sign the executable
$sigResult = Set-AuthenticodeSignature -FilePath $exePath `
                                       -Certificate $cert `
                                       -TimestampServer "http://timestamp.digicert.com"

if ($sigResult.Status -eq 'Valid') {
    Write-Host "Successfully signed!" -ForegroundColor Green
    Write-Host "  Signer: $($sigResult.SignerCertificate.Subject)" -ForegroundColor Gray
    Write-Host "  Thumbprint: $($sigResult.SignerCertificate.Thumbprint)" -ForegroundColor Gray
} else {
    Write-Host "Signing failed: $($sigResult.Status)" -ForegroundColor Red
    Write-Host "  StatusMessage: $($sigResult.StatusMessage)" -ForegroundColor Red
    exit 1
}

Write-Host "`nYou can now run Test-SignedExecution.ps1 to verify enforce mode works with signed exe." -ForegroundColor Cyan
