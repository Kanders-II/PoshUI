function Reset-UIState {
    <#
    .SYNOPSIS
    Resets or clears UI state.

    .DESCRIPTION
    Internal function to clear UI state for a specific session or all sessions.
    Useful for cleanup and troubleshooting.

    .PARAMETER SessionId
    Optional session ID to reset. If not specified, resets all sessions.

    .PARAMETER Force
    Force reset without confirmation.

    .EXAMPLE
    Reset-UIState -SessionId $sessionId

    Resets state for a specific session.

    .EXAMPLE
    Reset-UIState -Force

    Resets all session states without confirmation.
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter()]
        [string]$SessionId,

        [Parameter()]
        [switch]$Force
    )

    process {
        try {
            $registryPath = "HKCU:\Software\PoshUI\Sessions"

            if (-not (Test-Path $registryPath)) {
                Write-Verbose "No sessions to reset"
                return
            }

            if ($SessionId) {
                # Reset specific session
                $sessionPath = Join-Path $registryPath $SessionId
                
                if (Test-Path $sessionPath) {
                    if ($Force -or $PSCmdlet.ShouldProcess("Session $SessionId", "Reset state")) {
                        Remove-Item -Path $sessionPath -Recurse -Force
                        Write-Verbose "Reset state for session: $SessionId"
                    }
                }
                else {
                    Write-Warning "Session not found: $SessionId"
                }
            }
            else {
                # Reset all sessions
                if ($Force -or $PSCmdlet.ShouldProcess("All sessions", "Reset state")) {
                    Remove-Item -Path $registryPath -Recurse -Force
                    Write-Verbose "Reset state for all sessions"
                }
            }
        }
        catch {
            Write-Error "Failed to reset UI state: $($_.Exception.Message)"
            throw
        }
    }
}
