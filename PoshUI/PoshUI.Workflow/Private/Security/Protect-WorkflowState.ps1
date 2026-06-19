function Protect-WorkflowState {
    <#
    .SYNOPSIS
        Encrypts workflow state data using Windows Data Protection API (DPAPI).

    .DESCRIPTION
        Encrypts the workflow state JSON string using DPAPI with user-scope protection.
        The encrypted data can only be decrypted by the same user on the same machine.

        Security features:
        - DPAPI authenticated encryption (user-bound). DPAPI provides BOTH confidentiality
          and integrity: any tampering with the ciphertext causes decryption to fail with a
          CryptographicException, so a separate message authentication code is unnecessary.
        - Application-specific secondary entropy
        - Base64 encoding for safe storage
        - Versioned header for format validation

        NOTE: Earlier versions (POSHUI_STATE_V1) prepended an HMAC-SHA256 whose key was
        derived from the (guessable) username + computer name. That key provided no real
        integrity guarantee and has been removed; DPAPI's own authentication is relied upon
        instead. Unprotect-WorkflowState still reads legacy V1 blobs for backward compatibility.

    .PARAMETER JsonData
        The JSON string containing workflow state to encrypt.

    .OUTPUTS
        [string] Versioned, Base64-encoded encrypted data.

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

        # Application-specific secondary entropy (not secret; binds ciphertext to this app)
        $entropy = [System.Text.Encoding]::UTF8.GetBytes("PoshUI_Workflow_State_v1")

        # Encrypt using DPAPI (CurrentUser scope - only this user can decrypt).
        # DPAPI ciphertext is integrity-protected; tampering breaks decryption.
        $encryptedBytes = [System.Security.Cryptography.ProtectedData]::Protect(
            $jsonBytes,
            $entropy,
            [System.Security.Cryptography.DataProtectionScope]::CurrentUser
        )

        # Create header with format version
        $header = "POSHUI_STATE_V2:"
        $base64Data = [System.Convert]::ToBase64String($encryptedBytes)

        Write-Verbose "Successfully encrypted workflow state ($($jsonBytes.Length) bytes -> $($encryptedBytes.Length) bytes)"

        return $header + $base64Data
    }
    catch {
        Write-Error "Failed to encrypt workflow state: $_"
        throw
    }
}
