function Set-UIState {
    <#
    .SYNOPSIS
    Sets or updates the UI state.

    .DESCRIPTION
    Internal function to persist UI state information to the registry,
    including form data, navigation state, and runtime information.

    .PARAMETER SessionId
    Session ID for the UI instance.

    .PARAMETER Title
    Title of the UI.

    .PARAMETER Template
    Template type (Wizard, Dashboard, etc.).

    .PARAMETER Status
    Current status (Active, Completed, Cancelled, Error).

    .PARAMETER FormData
    Hashtable of form data to persist.

    .EXAMPLE
    Set-UIState -SessionId $sessionId -Status 'Active' -FormData $data

    Updates the state for a specific session.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$SessionId,

        [Parameter()]
        [string]$Title,

        [Parameter()]
        [string]$Template,

        [Parameter()]
        [ValidateSet('Active', 'Completed', 'Cancelled', 'Error')]
        [string]$Status,

        [Parameter()]
        [hashtable]$FormData
    )

    process {
        try {
            $registryPath = "HKCU:\Software\PoshUI\Sessions"
            $sessionPath = Join-Path $registryPath $SessionId

            # Create session path if it doesn't exist
            if (-not (Test-Path $sessionPath)) {
                New-Item -Path $sessionPath -Force | Out-Null
                Set-ItemProperty -Path $sessionPath -Name 'CreatedAt' -Value (Get-Date).ToString('o')
            }

            # Update last accessed time
            Set-ItemProperty -Path $sessionPath -Name 'LastAccessed' -Value (Get-Date).ToString('o')

            # Update properties if provided
            if ($PSBoundParameters.ContainsKey('Title')) {
                Set-ItemProperty -Path $sessionPath -Name 'Title' -Value $Title
            }

            if ($PSBoundParameters.ContainsKey('Template')) {
                Set-ItemProperty -Path $sessionPath -Name 'Template' -Value $Template
            }

            if ($PSBoundParameters.ContainsKey('Status')) {
                Set-ItemProperty -Path $sessionPath -Name 'Status' -Value $Status
            }

            if ($PSBoundParameters.ContainsKey('FormData')) {
                $formDataJson = $FormData | ConvertTo-Json -Depth 10 -Compress
                Set-ItemProperty -Path $sessionPath -Name 'FormData' -Value $formDataJson
            }

            Write-Verbose "UI state updated for session: $SessionId"
        }
        catch {
            Write-Error "Failed to set UI state: $($_.Exception.Message)"
            throw
        }
    }
}
