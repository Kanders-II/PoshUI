function Save-UIWorkflowState {
    <#
    .SYNOPSIS
    Saves the current workflow state to an encrypted file for later resume.

    .DESCRIPTION
    Serializes the current workflow state including task progress, wizard results,
    and execution state to an encrypted file. This enables resuming workflow execution
    after a reboot or script restart.

    Security features:
    - DPAPI encryption (CurrentUser scope - only this user can decrypt)
    - HMAC-SHA256 integrity validation (detects tampering)
    - Restrictive ACLs (current user only)
    - Secure file extension (.dat instead of .json)

    .PARAMETER Path
    Optional custom path for the state file. If not specified, uses the default
    location in $env:LOCALAPPDATA\PoshUI\PoshUI_Workflow_State.dat

    .PARAMETER Workflow
    Optional UIWorkflow object to save. If not specified, saves the current workflow.

    .PARAMETER NoEncryption
    If specified, saves state as plain JSON without encryption. NOT recommended for
    production use. Useful for debugging only.

    .EXAMPLE
    Save-UIWorkflowState

    Saves the current workflow state (encrypted) to the default location.

    .EXAMPLE
    Save-UIWorkflowState -Path "C:\Temp\MyWorkflowState.dat"

    Saves the current workflow state (encrypted) to a custom location.

    .EXAMPLE
    Save-UIWorkflowState -NoEncryption

    Saves state as plain JSON (for debugging only).

    .OUTPUTS
    System.String - Path to the saved state file.

    .NOTES
    The state file can only be decrypted by the same user on the same machine.
    This provides protection against unauthorized access to workflow state data.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Path,

        [Parameter()]
        $Workflow,

        [Parameter()]
        [switch]$NoEncryption
    )

    begin {
        Write-Verbose "Saving workflow state..."
    }

    process {
        try {
            # Use current workflow if not specified
            if (-not $Workflow) {
                if (-not $script:CurrentWorkflow) {
                    throw "No workflow initialized. Call New-PoshUIWorkflow first or provide a Workflow parameter."
                }
                $Workflow = $script:CurrentWorkflow
            }

            # Determine state file path (use .dat for encrypted, .json for plain)
            $extension = if ($NoEncryption) { '.json' } else { '.dat' }
            if (-not $Path) {
                $stateDir = Join-Path $env:LOCALAPPDATA 'PoshUI'
                if (-not (Test-Path $stateDir)) {
                    $dir = New-Item -Path $stateDir -ItemType Directory -Force
                    # Set restrictive ACL on directory
                    Set-SecureFileACL -Path $dir.FullName -IsDirectory
                }
                $Path = Join-Path $stateDir "PoshUI_Workflow_State$extension"
            }
            else {
                # Ensure parent directory exists
                $parentDir = Split-Path $Path -Parent
                if ($parentDir -and -not (Test-Path $parentDir)) {
                    New-Item -Path $parentDir -ItemType Directory -Force | Out-Null
                }
            }

            # Get state from workflow
            $state = $Workflow.ToState()

            # Add metadata
            $state['ScriptPath'] = $MyInvocation.PSCommandPath
            $state['SavedBy'] = $env:USERNAME
            $state['ComputerName'] = $env:COMPUTERNAME
            $state['IsEncrypted'] = (-not $NoEncryption)

            # Serialize to JSON
            $json = $state | ConvertTo-Json -Depth 10 -Compress:$false

            # Encrypt if not disabled
            if ($NoEncryption) {
                Write-Warning "Saving workflow state WITHOUT encryption. This is not recommended for production use."
                $content = $json
            }
            else {
                Write-Verbose "Encrypting workflow state with DPAPI..."
                $content = Protect-WorkflowState -JsonData $json
            }

            # Save to file
            $content | Out-File -FilePath $Path -Encoding UTF8 -Force

            # Set restrictive ACL on file (current user only)
            Set-SecureFileACL -Path $Path

            Write-Verbose "Workflow state saved securely to: $Path"

            # Store path in workflow for reference (if property exists)
            if ($Workflow.PSObject.Properties['StateFilePath']) {
                $Workflow.StateFilePath = $Path
            }

            return $Path
        }
        catch {
            Write-Error "Failed to save workflow state: $_"
            throw
        }
    }
}
