function Test-UIWorkflowState {
    <#
    .SYNOPSIS
    Tests if a saved workflow state exists.

    .DESCRIPTION
    Checks if a workflow state file exists at the specified or default location.
    Useful for determining if a workflow should resume from a previous execution.

    .PARAMETER Path
    Optional custom path for the state file. If not specified, searches default locations:
    - $env:LOCALAPPDATA\PoshUI\PoshUI_Workflow_State.dat (encrypted, preferred)
    - $env:LOCALAPPDATA\PoshUI\PoshUI_Workflow_State.json (legacy plain)
    - $env:PROGRAMDATA\PoshUI\PoshUI_Workflow_State.dat (encrypted)
    - $env:PROGRAMDATA\PoshUI\PoshUI_Workflow_State.json (legacy plain)

    .EXAMPLE
    if (Test-UIWorkflowState) {
        Write-Host "Resuming from saved state..."
    }

    Checks if a saved state exists and acts accordingly.

    .EXAMPLE
    Test-UIWorkflowState -Path "C:\Temp\MyWorkflowState.dat"

    Checks if a state file exists at the specified path.

    .OUTPUTS
    System.Boolean - $true if state file exists, $false otherwise.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Path
    )

    process {
        if ($Path) {
            return (Test-Path $Path)
        }

        # Check default locations (encrypted .dat first, then legacy .json)
        $defaultLocations = @(
            (Join-Path $env:LOCALAPPDATA 'PoshUI\PoshUI_Workflow_State.dat'),
            (Join-Path $env:LOCALAPPDATA 'PoshUI\PoshUI_Workflow_State.json'),
            (Join-Path $env:PROGRAMDATA 'PoshUI\PoshUI_Workflow_State.dat'),
            (Join-Path $env:PROGRAMDATA 'PoshUI\PoshUI_Workflow_State.json')
        )

        foreach ($loc in $defaultLocations) {
            if (Test-Path $loc) {
                Write-Verbose "Found saved state at: $loc"
                return $true
            }
        }

        return $false
    }
}
