function Get-UIState {
    <#
    .SYNOPSIS
    Retrieves the current UI state.

    .DESCRIPTION
    Internal function to get the current state of a UI instance,
    including form data, navigation state, and runtime information.

    .PARAMETER SessionId
    Optional session ID to retrieve state for a specific session.
    If not specified, returns the current session state.

    .EXAMPLE
    $state = Get-UIState

    Gets the current UI state.

    .OUTPUTS
    Hashtable containing UI state information.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$SessionId
    )

    process {
        try {
            $registryPath = "HKCU:\Software\PoshUI\Sessions"
            
            if (-not (Test-Path $registryPath)) {
                return $null
            }

            # If SessionId specified, get that session
            if ($SessionId) {
                $sessionPath = Join-Path $registryPath $SessionId
                if (-not (Test-Path $sessionPath)) {
                    Write-Warning "Session not found: $SessionId"
                    return $null
                }

                $sessionData = Get-ItemProperty -Path $sessionPath -ErrorAction SilentlyContinue
                if ($sessionData) {
                    return @{
                        SessionId = $SessionId
                        Title = $sessionData.Title
                        Template = $sessionData.Template
                        CreatedAt = $sessionData.CreatedAt
                        LastAccessed = $sessionData.LastAccessed
                        Status = $sessionData.Status
                        FormData = if ($sessionData.FormData) { 
                            $sessionData.FormData | ConvertFrom-Json 
                        } else { 
                            @{} 
                        }
                    }
                }
            }
            else {
                # Return all active sessions
                $sessions = @()
                $sessionKeys = Get-ChildItem -Path $registryPath -ErrorAction SilentlyContinue
                
                foreach ($key in $sessionKeys) {
                    $sessionData = Get-ItemProperty -Path $key.PSPath -ErrorAction SilentlyContinue
                    if ($sessionData) {
                        $sessions += @{
                            SessionId = $key.PSChildName
                            Title = $sessionData.Title
                            Template = $sessionData.Template
                            CreatedAt = $sessionData.CreatedAt
                            LastAccessed = $sessionData.LastAccessed
                            Status = $sessionData.Status
                        }
                    }
                }
                
                return $sessions
            }
        }
        catch {
            Write-Error "Failed to get UI state: $($_.Exception.Message)"
            return $null
        }
    }
}
