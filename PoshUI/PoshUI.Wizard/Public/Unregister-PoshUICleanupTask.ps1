function Unregister-PoshUICleanupTask {
    <#
    .SYNOPSIS
    Removes the PoshUI automatic cleanup scheduled task.

    .DESCRIPTION
    Unregisters the scheduled task created by Register-PoshUICleanupTask.

    .PARAMETER Force
    Skip confirmation prompt.

    .EXAMPLE
    Unregister-PoshUICleanupTask

    Removes the PoshUI cleanup task.

    .NOTES
    Requires administrator privileges to remove scheduled tasks.
    Task name: "PoshUI-AutoCleanup"
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    param(
        [Parameter()]
        [switch]$Force
    )

    begin {
        Write-Verbose "Unregistering PoshUI cleanup task"
        $taskName = "PoshUI-AutoCleanup"
        $taskPath = "\PoshUI\"
    }

    process {
        try {
            # Check if running as administrator
            $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
            if (-not $isAdmin) {
                throw "This cmdlet requires administrator privileges to remove scheduled tasks"
            }

            # Check if task exists
            $existingTask = Get-ScheduledTask -TaskName $taskName -TaskPath $taskPath -ErrorAction SilentlyContinue
            if (-not $existingTask) {
                Write-Warning "Task '$taskPath$taskName' does not exist"
                return @{ Success = $false; Reason = "Task not found" }
            }

            if ($Force -or $PSCmdlet.ShouldProcess($taskName, "Unregister scheduled task")) {
                Unregister-ScheduledTask -TaskName $taskName -TaskPath $taskPath -Confirm:$false

                Write-Host "[OK] Scheduled task unregistered successfully" -ForegroundColor Green
                Write-Host "  Task name: $taskPath$taskName" -ForegroundColor Cyan

                return @{
                    Success = $true
                    TaskName = $taskName
                    TaskPath = $taskPath
                }
            }
        }
        catch {
            Write-Error "Failed to unregister cleanup task: $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Task unregistration completed"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Unregister-PoshWizardCleanupTask' -Value 'Unregister-PoshUICleanupTask'
