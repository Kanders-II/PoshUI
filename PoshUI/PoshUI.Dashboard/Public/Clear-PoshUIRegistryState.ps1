function Clear-PoshUIRegistryState {
    <#
    .SYNOPSIS
    Clears PoshUI session state from the Windows Registry.

    .DESCRIPTION
    Removes all registry keys under HKCU:\Software\PoshUI\Sessions that contain
    session state data. Detects and removes stale sessions from crashed UIs.

    .PARAMETER SessionId
    Optional specific session ID to remove. If not specified, all sessions are removed.

    .PARAMETER OlderThan
    Only remove sessions older than this many hours. Default is to remove all.

    .PARAMETER Force
    Skip confirmation prompts.

    .EXAMPLE
    Clear-PoshUIRegistryState

    Removes all PoshUI registry sessions.

    .EXAMPLE
    Clear-PoshUIRegistryState -OlderThan 24

    Removes sessions older than 24 hours.

    .EXAMPLE
    Clear-PoshUIRegistryState -SessionId "12345-67890"

    Removes a specific session.

    .NOTES
    Registry location: HKCU:\Software\PoshUI\Sessions
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter()]
        [string]$SessionId,

        [Parameter()]
        [int]$OlderThan,

        [Parameter()]
        [switch]$Force
    )

    begin {
        Write-Verbose "Starting registry cleanup"
        $registryPath = "HKCU:\Software\PoshUI\Sessions"
        $keysRemoved = 0
    }

    process {
        try {
            # Check if registry path exists
            if (-not (Test-Path $registryPath)) {
                Write-Verbose "No PoshUI registry state found at $registryPath"
                return @{ KeysRemoved = 0 }
            }

            # Get all session keys
            $sessionKeys = Get-ChildItem -Path $registryPath -ErrorAction SilentlyContinue

            if ($sessionKeys.Count -eq 0) {
                Write-Verbose "No session keys found"
                return @{ KeysRemoved = 0 }
            }

            foreach ($key in $sessionKeys) {
                $shouldRemove = $false

                # Check if specific session ID requested
                if ($SessionId) {
                    if ($key.PSChildName -eq $SessionId) {
                        $shouldRemove = $true
                    }
                }
                # Check age filter
                elseif ($OlderThan) {
                    try {
                        $lastWriteTime = $key.LastWriteTime
                        if ($lastWriteTime -lt (Get-Date).AddHours(-$OlderThan)) {
                            $shouldRemove = $true
                        }
                    }
                    catch {
                        Write-Warning "Could not determine age of session $($key.PSChildName): $_"
                        $shouldRemove = $true  # Remove if we can't determine age
                    }
                }
                else {
                    # No filters, remove all
                    $shouldRemove = $true
                }

                if ($shouldRemove) {
                    if ($Force -or $PSCmdlet.ShouldProcess($key.PSChildName, "Remove registry session")) {
                        try {
                            Remove-Item -Path $key.PSPath -Recurse -Force -ErrorAction Stop
                            $keysRemoved++
                            Write-Verbose "Removed session: $($key.PSChildName)"
                        }
                        catch {
                            Write-Warning "Failed to remove session $($key.PSChildName): $_"
                        }
                    }
                }
            }

            # Remove parent key if empty
            $remainingKeys = Get-ChildItem -Path $registryPath -ErrorAction SilentlyContinue
            if ($remainingKeys.Count -eq 0) {
                try {
                    Remove-Item -Path $registryPath -Force -ErrorAction Stop
                    Write-Verbose "Removed empty Sessions registry key"
                }
                catch {
                    Write-Warning "Failed to remove parent registry key: $_"
                }
            }

            Write-Verbose "Removed $keysRemoved registry session(s)"
            return @{ KeysRemoved = $keysRemoved }
        }
        catch {
            Write-Error "Failed to clean registry state: $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Registry cleanup completed"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Clear-PoshWizardRegistryState' -Value 'Clear-PoshUIRegistryState'
