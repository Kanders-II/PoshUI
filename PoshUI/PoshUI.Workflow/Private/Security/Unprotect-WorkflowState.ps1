function Unprotect-WorkflowState {
    <#
    .SYNOPSIS
        Decrypts workflow state data that was encrypted using Protect-WorkflowState.

    .DESCRIPTION
        Decrypts the workflow state using Windows Data Protection API (DPAPI).

        Security features:
        - DPAPI decryption (user-bound). DPAPI authenticates the ciphertext: any tampering
          causes a CryptographicException, which is surfaced as a load failure.
        - Format version checking
        - Backward compatibility with legacy POSHUI_STATE_V1 blobs

        The legacy V1 format prepended a 32-byte HMAC whose key was derived from guessable
        values (username + computer name). That HMAC provided no real protection, so it is no
        longer computed or trusted; for V1 blobs the legacy prefix is simply skipped and the
        DPAPI ciphertext is decrypted (DPAPI provides the actual integrity guarantee).

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

        $headerV2 = "POSHUI_STATE_V2:"
        $headerV1 = "POSHUI_STATE_V1:"

        if ($EncryptedData.StartsWith($headerV2)) {
            # Current format: header + base64(DPAPI ciphertext)
            $encryptedBytes = [System.Convert]::FromBase64String($EncryptedData.Substring($headerV2.Length))
        }
        elseif ($EncryptedData.StartsWith($headerV1)) {
            # Legacy format: header + base64([32-byte HMAC][DPAPI ciphertext]).
            # The HMAC used a guessable key and is ignored; DPAPI authenticates the ciphertext.
            $combined = [System.Convert]::FromBase64String($EncryptedData.Substring($headerV1.Length))
            $signatureLength = 32
            if ($combined.Length -le $signatureLength) {
                throw "Invalid state file: data too short."
            }
            $encryptedBytes = New-Object byte[] ($combined.Length - $signatureLength)
            [System.Array]::Copy($combined, $signatureLength, $encryptedBytes, 0, $encryptedBytes.Length)
        }
        else {
            throw "Invalid state file format. File may be corrupted or from an incompatible version."
        }

        # Decrypt using DPAPI (throws CryptographicException if tampered or created elsewhere)
        $entropy = [System.Text.Encoding]::UTF8.GetBytes("PoshUI_Workflow_State_v1")

        $decryptedBytes = [System.Security.Cryptography.ProtectedData]::Unprotect(
            $encryptedBytes,
            $entropy,
            [System.Security.Cryptography.DataProtectionScope]::CurrentUser
        )

        # Convert bytes back to JSON string
        $jsonData = [System.Text.Encoding]::UTF8.GetString($decryptedBytes)

        Write-Verbose "Successfully decrypted workflow state ($($encryptedBytes.Length) bytes -> $($decryptedBytes.Length) bytes)"

        return $jsonData
    }
    catch [System.Security.Cryptography.CryptographicException] {
        throw "State file integrity check failed or the file was created by a different user/machine. It may have been tampered with."
    }
    catch {
        Write-Error "Failed to decrypt workflow state: $_"
        throw
    }
}
