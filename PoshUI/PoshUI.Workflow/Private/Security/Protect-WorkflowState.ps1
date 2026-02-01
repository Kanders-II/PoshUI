function Protect-WorkflowState {
    <#
    .SYNOPSIS
        Encrypts workflow state data using Windows Data Protection API (DPAPI).
    
    .DESCRIPTION
        Encrypts the workflow state JSON string using DPAPI with user-scope protection.
        The encrypted data can only be decrypted by the same user on the same machine.
        
        Security features:
        - DPAPI encryption (user-bound)
        - HMAC-SHA256 integrity validation
        - Base64 encoding for safe storage
        - Metadata header for format validation
    
    .PARAMETER JsonData
        The JSON string containing workflow state to encrypt.
    
    .OUTPUTS
        [string] Base64-encoded encrypted data with integrity signature.
    
    .EXAMPLE
        $encrypted = Protect-WorkflowState -JsonData $json
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$JsonData
    )
    
    try {
        # Add required assembly for DPAPI
        Add-Type -AssemblyName System.Security -ErrorAction Stop
        
        # Convert JSON to bytes
        $jsonBytes = [System.Text.Encoding]::UTF8.GetBytes($JsonData)
        
        # Generate entropy (additional protection)
        $entropy = [System.Text.Encoding]::UTF8.GetBytes("PoshUI_Workflow_State_v1")
        
        # Encrypt using DPAPI (CurrentUser scope - only this user can decrypt)
        $encryptedBytes = [System.Security.Cryptography.ProtectedData]::Protect(
            $jsonBytes,
            $entropy,
            [System.Security.Cryptography.DataProtectionScope]::CurrentUser
        )
        
        # Create HMAC for integrity validation
        $hmac = New-Object System.Security.Cryptography.HMACSHA256
        $hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($env:USERNAME + $env:COMPUTERNAME + "PoshUI")
        $signature = $hmac.ComputeHash($encryptedBytes)
        
        # Combine signature + encrypted data
        $combined = New-Object byte[] ($signature.Length + $encryptedBytes.Length)
        [System.Array]::Copy($signature, 0, $combined, 0, $signature.Length)
        [System.Array]::Copy($encryptedBytes, 0, $combined, $signature.Length, $encryptedBytes.Length)
        
        # Create header with format version
        $header = "POSHUI_STATE_V1:"
        $base64Data = [System.Convert]::ToBase64String($combined)
        
        Write-Verbose "Successfully encrypted workflow state (${jsonBytes.Length} bytes -> ${combined.Length} bytes)"
        
        return $header + $base64Data
    }
    catch {
        Write-Error "Failed to encrypt workflow state: $_"
        throw
    }
}
