function Register-PoshUICleanupTask {
    <#
    .SYNOPSIS
    Registers a scheduled task to automatically clean up PoshUI state.

    .DESCRIPTION
    Creates a Windows scheduled task that periodically runs Clear-PoshUIState
    to prevent accumulation of orphaned resources from crashed UI sessions.

    .PARAMETER Frequency
    How often to run the cleanup. Valid values: Daily, Weekly, Monthly
    Default is Weekly.

    .PARAMETER Time
    Time of day to run cleanup in 24-hour format (e.g., "02:00")
    Default is 2:00 AM.

    .PARAMETER IncludeLogs
    Configure the task to also clean log files.

    .PARAMETER LogRetentionDays
    Number of days to retain log files. Default is 30.

    .PARAMETER Force
    Overwrite existing task if it exists.

    .EXAMPLE
    Register-PoshUICleanupTask

    Registers a weekly cleanup task at 2:00 AM.

    .EXAMPLE
    Register-PoshUICleanupTask -Frequency Daily -Time "03:00" -IncludeLogs

    Registers a daily cleanup at 3:00 AM including log cleanup.

    .NOTES
    Requires administrator privileges to create scheduled tasks.
    Task name: "PoshUI-AutoCleanup"
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter()]
        [ValidateSet('Daily', 'Weekly', 'Monthly')]
        [string]$Frequency = 'Weekly',

        [Parameter()]
        [ValidatePattern('^\d{2}:\d{2}$')]
        [string]$Time = '02:00',

        [Parameter()]
        [switch]$IncludeLogs,

        [Parameter()]
        [ValidateRange(1, 365)]
        [int]$LogRetentionDays = 30,

        [Parameter()]
        [switch]$Force
    )

    begin {
        Write-Verbose "Registering PoshUI cleanup task"
        $taskName = "PoshUI-AutoCleanup"
        $taskPath = "\PoshUI\"
    }

    process {
        try {
            # Check if running as administrator
            $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
            if (-not $isAdmin) {
                throw "This cmdlet requires administrator privileges to create scheduled tasks"
            }

            # Check if task already exists
            $existingTask = Get-ScheduledTask -TaskName $taskName -TaskPath $taskPath -ErrorAction SilentlyContinue
            if ($existingTask -and -not $Force) {
                throw "Task '$taskName' already exists. Use -Force to overwrite."
            }

            if ($Force -or $PSCmdlet.ShouldProcess($taskName, "Register scheduled cleanup task")) {
                # Build the PowerShell command
                $cleanupArgs = "-IncludeLogs:`$$IncludeLogs -LogRetentionDays $LogRetentionDays -Force"
                $psCommand = "Import-Module PoshUI; Clear-PoshUIState $cleanupArgs"

                # Create action
                $action = New-ScheduledTaskAction -Execute "powershell.exe" `
                    -Argument "-NoProfile -WindowStyle Hidden -Command `"$psCommand`""

                # Create trigger based on frequency
                switch ($Frequency) {
                    'Daily' {
                        $trigger = New-ScheduledTaskTrigger -Daily -At $Time
                    }
                    'Weekly' {
                        $trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At $Time
                    }
                    'Monthly' {
                        $trigger = New-ScheduledTaskTrigger -Weekly -WeeksInterval 4 -DaysOfWeek Sunday -At $Time
                    }
                }

                # Create settings
                $settings = New-ScheduledTaskSettingsSet `
                    -AllowStartIfOnBatteries `
                    -DontStopIfGoingOnBatteries `
                    -StartWhenAvailable `
                    -RunOnlyIfNetworkAvailable:$false `
                    -ExecutionTimeLimit (New-TimeSpan -Hours 1)

                # Create principal (run as current user)
                $principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType S4U

                # Register the task
                $task = New-ScheduledTask -Action $action -Trigger $trigger -Settings $settings -Principal $principal
                Register-ScheduledTask -TaskName $taskName -TaskPath $taskPath -InputObject $task -Force:$Force | Out-Null

                Write-Host "[OK] Scheduled task registered successfully" -ForegroundColor Green
                Write-Host "  Task name: $taskPath$taskName" -ForegroundColor Cyan
                Write-Host "  Frequency: $Frequency at $Time" -ForegroundColor Cyan
                Write-Host "  Include logs: $IncludeLogs" -ForegroundColor Cyan

                return @{
                    Success = $true
                    TaskName = $taskName
                    TaskPath = $taskPath
                    Frequency = $Frequency
                    Time = $Time
                }
            }
        }
        catch {
            Write-Error "Failed to register cleanup task: $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Task registration completed"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Register-PoshWizardCleanupTask' -Value 'Register-PoshUICleanupTask'
