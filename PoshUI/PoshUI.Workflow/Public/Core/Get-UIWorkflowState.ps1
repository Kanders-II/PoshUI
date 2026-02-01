function Get-UIWorkflowState {
    <#
    .SYNOPSIS
    Loads a saved workflow state from an encrypted file.

    .DESCRIPTION
    Deserializes a workflow state from an encrypted file that was saved by Save-UIWorkflowState.
    Returns the state as a hashtable that can be used to restore workflow execution.

    Security features:
    - DPAPI decryption (CurrentUser scope)
    - HMAC-SHA256 integrity validation (detects tampering)
    - Supports both encrypted (.dat) and legacy plain (.json) files

    .PARAMETER Path
    Optional custom path for the state file. If not specified, searches default locations:
    - $env:LOCALAPPDATA\PoshUI\PoshUI_Workflow_State.dat (encrypted, preferred)
    - $env:LOCALAPPDATA\PoshUI\PoshUI_Workflow_State.json (legacy plain)
    - $env:PROGRAMDATA\PoshUI\PoshUI_Workflow_State.dat (encrypted)
    - $env:PROGRAMDATA\PoshUI\PoshUI_Workflow_State.json (legacy plain)

    .EXAMPLE
    $state = Get-UIWorkflowState

    Loads workflow state from the default location.

    .EXAMPLE
    $state = Get-UIWorkflowState -Path "C:\Temp\MyWorkflowState.dat"

    Loads workflow state from a custom location.

    .OUTPUTS
    System.Collections.Hashtable - The saved workflow state.

    .NOTES
    Returns $null if no state file is found.
    The state file can only be decrypted by the same user on the same machine.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Path
    )

    begin {
        Write-Verbose "Loading workflow state..."
    }

    process {
        try {
            # Determine state file path
            $statePath = $null

            if ($Path) {
                if (Test-Path $Path) {
                    $statePath = $Path
                }
            }
            else {
                # Search default locations (encrypted .dat first, then legacy .json)
                $defaultLocations = @(
                    (Join-Path $env:LOCALAPPDATA 'PoshUI\PoshUI_Workflow_State.dat'),
                    (Join-Path $env:LOCALAPPDATA 'PoshUI\PoshUI_Workflow_State.json'),
                    (Join-Path $env:PROGRAMDATA 'PoshUI\PoshUI_Workflow_State.dat'),
                    (Join-Path $env:PROGRAMDATA 'PoshUI\PoshUI_Workflow_State.json')
                )

                foreach ($loc in $defaultLocations) {
                    if (Test-Path $loc) {
                        $statePath = $loc
                        break
                    }
                }
            }

            if (-not $statePath) {
                Write-Verbose "No saved workflow state found."
                return $null
            }

            Write-Verbose "Loading state from: $statePath"

            # Read file content
            $content = Get-Content -Path $statePath -Raw -Encoding UTF8

            # Check if content is encrypted (has POSHUI_STATE_V1: header)
            if ($content.StartsWith("POSHUI_STATE_V1:")) {
                Write-Verbose "Decrypting encrypted state file..."
                $json = Unprotect-WorkflowState -EncryptedData $content
            }
            else {
                # Legacy plain JSON or NoEncryption mode
                Write-Verbose "Loading plain text state file (legacy or debug mode)"
                $json = $content
            }

            # Parse JSON
            $state = $json | ConvertFrom-Json

            # Convert PSCustomObject to hashtable for easier use
            $stateHash = @{}
            foreach ($prop in $state.PSObject.Properties) {
                $stateHash[$prop.Name] = $prop.Value
            }

            # Add source path
            $stateHash['_StateFilePath'] = $statePath

            Write-Verbose "Workflow state loaded successfully. Tasks: $($stateHash.Tasks.Count)"

            return $stateHash
        }
        catch {
            Write-Error "Failed to load workflow state: $_"
            return $null
        }
    }
}
