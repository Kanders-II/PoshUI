function Remove-SecureTempScript {
    <#
    .SYNOPSIS
        Securely removes a temporary script file.
    
    .DESCRIPTION
        Removes a temporary script file with optional secure deletion
        (overwriting with random data before removal).
        
        This prevents potential data recovery of the script content.
    
    .PARAMETER Path
        Full path to the temporary script file to remove
    
    .PARAMETER SecureWipe
        If specified, overwrites the file with random data before deletion
    
    .EXAMPLE
        Remove-SecureTempScript -Path "C:\Temp\PoshWizard\ABC123.ps1"
    
    .EXAMPLE
        Remove-SecureTempScript -Path $tempPath -SecureWipe
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [ValidateNotNullOrEmpty()]
        [string]$Path,
        
        [Parameter()]
        [switch]$SecureWipe
    )
    
    process {
        if (-not (Test-Path $Path)) {
            Write-Verbose "Temp script file not found (may already be deleted): $Path"
            return
        }
        
        try {
            if ($SecureWipe) {
                Write-Verbose "Performing secure wipe of temp script: $Path"
                
                # Get file size
                $fileInfo = Get-Item $Path
                $fileSize = $fileInfo.Length
                
                if ($fileSize -gt 0) {
                    # Generate random data
                    $randomBytes = New-Object byte[] $fileSize
                    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
                    $rng.GetBytes($randomBytes)
                    $rng.Dispose()
                    
                    # Overwrite file
                    if ($PSCmdlet.ShouldProcess($Path, "Overwrite with random data")) {
                        [System.IO.File]::WriteAllBytes($Path, $randomBytes)
                        Write-Verbose "Overwrote file with random data"
                    }
                }
            }
            
            # Remove file
            if ($PSCmdlet.ShouldProcess($Path, "Remove file")) {
                Remove-Item -Path $Path -Force -ErrorAction Stop
                Write-Verbose "Successfully removed temp script: $Path"
            }
            
        } catch {
            Write-Warning "Failed to remove temp script '$Path': $_"
            # Don't throw - this is cleanup, best effort
        }
    }
}

