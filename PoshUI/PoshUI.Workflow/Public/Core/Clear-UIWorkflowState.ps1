function Clear-UIWorkflowState {
    <#
    .SYNOPSIS
    Securely removes saved workflow state files.

    .DESCRIPTION
    Deletes workflow state files from the specified or default locations.
    Use this after a workflow completes successfully or to reset a failed workflow.

    Security features:
    - Secure wipe option (overwrites with random data before deletion)
    - Searches both encrypted (.dat) and legacy (.json) files

    .PARAMETER Path
    Optional custom path for the state file to remove. If not specified, 
    removes state files from all default locations.

    .PARAMETER All
    If specified, removes state files from all default locations.

    .PARAMETER SecureWipe
    If specified, overwrites the file with random data before deletion.
    This prevents potential data recovery of sensitive workflow state.

    .EXAMPLE
    Clear-UIWorkflowState

    Removes the state file from the default location.

    .EXAMPLE
    Clear-UIWorkflowState -Path "C:\Temp\MyWorkflowState.dat"

    Removes a specific state file.

    .EXAMPLE
    Clear-UIWorkflowState -All -SecureWipe

    Securely removes state files from all default locations.

    .OUTPUTS
    None
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter()]
        [string]$Path,

        [Parameter()]
        [switch]$All,

        [Parameter()]
        [switch]$SecureWipe
    )

    process {
        $filesToRemove = @()

        if ($Path) {
            if (Test-Path $Path) {
                $filesToRemove += $Path
            }
        }
        else {
            # Default locations (encrypted .dat and legacy .json)
            $defaultLocations = @(
                (Join-Path $env:LOCALAPPDATA 'PoshUI\PoshUI_Workflow_State.dat'),
                (Join-Path $env:LOCALAPPDATA 'PoshUI\PoshUI_Workflow_State.json'),
                (Join-Path $env:PROGRAMDATA 'PoshUI\PoshUI_Workflow_State.dat'),
                (Join-Path $env:PROGRAMDATA 'PoshUI\PoshUI_Workflow_State.json')
            )

            if ($All) {
                # Remove from all locations
                foreach ($loc in $defaultLocations) {
                    if (Test-Path $loc) {
                        $filesToRemove += $loc
                    }
                }
            }
            else {
                # Remove from first found location
                foreach ($loc in $defaultLocations) {
                    if (Test-Path $loc) {
                        $filesToRemove += $loc
                        break
                    }
                }
            }
        }

        foreach ($file in $filesToRemove) {
            if ($PSCmdlet.ShouldProcess($file, "Remove workflow state file")) {
                try {
                    if ($SecureWipe) {
                        # Overwrite with random data before deletion
                        Write-Verbose "Performing secure wipe of: $file"
                        $fileInfo = Get-Item $file
                        $fileSize = $fileInfo.Length
                        
                        if ($fileSize -gt 0) {
                            $randomData = New-Object byte[] $fileSize
                            $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
                            $rng.GetBytes($randomData)
                            [System.IO.File]::WriteAllBytes($file, $randomData)
                            
                            # Second pass with zeros
                            $zeros = New-Object byte[] $fileSize
                            [System.IO.File]::WriteAllBytes($file, $zeros)
                        }
                    }
                    
                    Remove-Item -Path $file -Force
                    Write-Verbose "Removed workflow state file: $file"
                }
                catch {
                    Write-Warning "Failed to remove state file '$file': $_"
                }
            }
        }

        if ($filesToRemove.Count -eq 0) {
            Write-Verbose "No workflow state files found to remove."
        }
    }
}
