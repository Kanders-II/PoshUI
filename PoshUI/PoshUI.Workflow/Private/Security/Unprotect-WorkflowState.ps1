function Unprotect-WorkflowState {
    <#
    .SYNOPSIS
        Decrypts workflow state data that was encrypted using Protect-WorkflowState.
    
    .DESCRIPTION
        Decrypts the workflow state using Windows Data Protection API (DPAPI).
        Validates the HMAC signature to ensure data integrity.
        
        Security features:
        - DPAPI decryption (user-bound)
        - HMAC-SHA256 integrity validation
        - Format version checking
        - Tamper detection
    
    .PARAMETER EncryptedData
        The Base64-encoded encrypted data string from Protect-WorkflowState.
    
    .OUTPUTS
        [string] The decrypted JSON string.
    
    .EXAMPLE
        $json = Unprotect-WorkflowState -EncryptedData $encrypted
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$EncryptedData
    )
    
    try {
        # Add required assembly for DPAPI
        Add-Type -AssemblyName System.Security -ErrorAction Stop
        
        # Check for format header
        $header = "POSHUI_STATE_V1:"
        if (-not $EncryptedData.StartsWith($header)) {
            throw "Invalid state file format. File may be corrupted or from an incompatible version."
        }
        
        # Remove header and decode Base64
        $base64Data = $EncryptedData.Substring($header.Length)
        $combined = [System.Convert]::FromBase64String($base64Data)
        
        # HMAC-SHA256 signature is 32 bytes
        $signatureLength = 32
        if ($combined.Length -le $signatureLength) {
            throw "Invalid state file: data too short."
        }
        
        # Extract signature and encrypted data
        $storedSignature = New-Object byte[] $signatureLength
        $encryptedBytes = New-Object byte[] ($combined.Length - $signatureLength)
        [System.Array]::Copy($combined, 0, $storedSignature, 0, $signatureLength)
        [System.Array]::Copy($combined, $signatureLength, $encryptedBytes, 0, $encryptedBytes.Length)
        
        # Verify HMAC signature
        $hmac = New-Object System.Security.Cryptography.HMACSHA256
        $hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($env:USERNAME + $env:COMPUTERNAME + "PoshUI")
        $computedSignature = $hmac.ComputeHash($encryptedBytes)
        
        # Compare signatures (constant-time comparison to prevent timing attacks)
        $signatureValid = $true
        for ($i = 0; $i -lt $signatureLength; $i++) {
            if ($storedSignature[$i] -ne $computedSignature[$i]) {
                $signatureValid = $false
            }
        }
        
        if (-not $signatureValid) {
            throw "State file integrity check failed. File may have been tampered with or was created by a different user."
        }
        
        # Decrypt using DPAPI
        $entropy = [System.Text.Encoding]::UTF8.GetBytes("PoshUI_Workflow_State_v1")
        
        $decryptedBytes = [System.Security.Cryptography.ProtectedData]::Unprotect(
            $encryptedBytes,
            $entropy,
            [System.Security.Cryptography.DataProtectionScope]::CurrentUser
        )
        
        # Convert bytes back to JSON string
        $jsonData = [System.Text.Encoding]::UTF8.GetString($decryptedBytes)
        
        Write-Verbose "Successfully decrypted workflow state (${encryptedBytes.Length} bytes -> ${decryptedBytes.Length} bytes)"
        
        return $jsonData
    }
    catch [System.Security.Cryptography.CryptographicException] {
        throw "Failed to decrypt state file. This may occur if the file was created by a different user or on a different machine."
    }
    catch {
        Write-Error "Failed to decrypt workflow state: $_"
        throw
    }
}
