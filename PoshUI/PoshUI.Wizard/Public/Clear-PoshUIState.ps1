function Clear-PoshUIState {
    <#
    .SYNOPSIS
    Clears all PoshUI state including registry entries, temporary files, and orphaned processes.

    .DESCRIPTION
    Master cleanup function that removes all PoshUI-related resources:
    - Registry keys in HKCU:\Software\PoshUI
    - Temporary script files in $env:TEMP\PoshUI
    - Log files older than specified retention period
    - Stale session data from crashed UIs

    .PARAMETER IncludeLogs
    Also remove log files. By default, logs are retained.

    .PARAMETER LogRetentionDays
    Number of days to retain log files. Default is 30 days.

    .PARAMETER Force
    Skip confirmation prompts.

    .EXAMPLE
    Clear-PoshUIState

    Clears all PoshUI state except logs.

    .EXAMPLE
    Clear-PoshUIState -IncludeLogs -LogRetentionDays 7 -Force

    Clears all state including logs older than 7 days without confirmation.

    .NOTES
    This function is useful for troubleshooting and cleaning up after crashed UI sessions.
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    param(
        [Parameter()]
        [switch]$IncludeLogs,

        [Parameter()]
        [ValidateRange(1, 365)]
        [int]$LogRetentionDays = 30,

        [Parameter()]
        [switch]$Force
    )

    begin {
        Write-Verbose "Starting PoshUI state cleanup"
        $cleanupReport = @{
            RegistryKeysRemoved = 0
            FilesRemoved = 0
            LogFilesRemoved = 0
            Errors = @()
        }
    }

    process {
        try {
            # Confirm if not forced
            if (-not $Force -and -not $PSCmdlet.ShouldProcess("PoshUI state data", "Clear all state")) {
                Write-Warning "Cleanup cancelled by user"
                return
            }

            # Clean registry state
            Write-Verbose "Cleaning registry state..."
            try {
                $result = Clear-PoshUIRegistryState -Force
                $cleanupReport.RegistryKeysRemoved = $result.KeysRemoved
            }
            catch {
                $cleanupReport.Errors += "Registry cleanup failed: $($_.Exception.Message)"
                Write-Error $_
            }

            # Clean file state
            Write-Verbose "Cleaning file state..."
            try {
                $result = Clear-PoshUIFileState -IncludeLogs:$IncludeLogs -LogRetentionDays $LogRetentionDays -Force
                $cleanupReport.FilesRemoved = $result.FilesRemoved
                $cleanupReport.LogFilesRemoved = $result.LogFilesRemoved
            }
            catch {
                $cleanupReport.Errors += "File cleanup failed: $($_.Exception.Message)"
                Write-Error $_
            }

            # Clear module-level state
            Write-Verbose "Clearing module-level state..."
            $script:CurrentWizard = $null

            Write-Host "[OK] PoshUI state cleanup completed" -ForegroundColor Green
            Write-Host "  Registry keys removed: $($cleanupReport.RegistryKeysRemoved)" -ForegroundColor Cyan
            Write-Host "  Files removed: $($cleanupReport.FilesRemoved)" -ForegroundColor Cyan
            if ($IncludeLogs) {
                Write-Host "  Log files removed: $($cleanupReport.LogFilesRemoved)" -ForegroundColor Cyan
            }

            if ($cleanupReport.Errors.Count -gt 0) {
                Write-Warning "Cleanup completed with $($cleanupReport.Errors.Count) error(s)"
            }

            return $cleanupReport
        }
        catch {
            Write-Error "Failed to clean PoshUI state: $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Clear-PoshUIState completed"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Clear-PoshWizardState' -Value 'Clear-PoshUIState'
