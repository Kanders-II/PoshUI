function Register-UIWorkflowResumeTask {
    <#
    .SYNOPSIS
    Registers a scheduled task to auto-resume the workflow after system reboot.

    .DESCRIPTION
    Creates a Windows scheduled task that runs the workflow script at system startup
    to automatically resume execution after a reboot. The task is configured to:
    - Run at system startup (before user logon if running as SYSTEM)
    - Run with highest privileges if needed
    - Self-delete after the workflow completes

    .PARAMETER ScriptPath
    Path to the workflow script to run after reboot. If not specified, uses the
    current script path from the workflow state.

    .PARAMETER TaskName
    Name for the scheduled task. Defaults to "PoshUI_WorkflowResume_<WorkflowId>".

    .PARAMETER RunAsSystem
    If specified, the task runs as SYSTEM account (requires admin privileges).
    Otherwise, runs as the current user at logon.

    .PARAMETER PowerShellPath
    Path to PowerShell executable. Defaults to pwsh.exe for PowerShell 7+,
    or powershell.exe for Windows PowerShell.

    .EXAMPLE
    Register-UIWorkflowResumeTask

    Registers a task to resume the current workflow script after reboot.

    .EXAMPLE
    Register-UIWorkflowResumeTask -RunAsSystem

    Registers a task that runs as SYSTEM at startup (requires admin).

    .OUTPUTS
    Microsoft.Management.Infrastructure.CimInstance - The created scheduled task.

    .NOTES
    Requires the ScheduledTasks module (built into Windows 8+/Server 2012+).
    Running as SYSTEM requires administrator privileges.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$ScriptPath,

        [Parameter()]
        [string]$TaskName,

        [Parameter()]
        [switch]$RunAsSystem,

        [Parameter()]
        [string]$PowerShellPath
    )

    begin {
        Write-Verbose "Registering workflow resume scheduled task..."

        # Check if ScheduledTasks module is available
        if (-not (Get-Module -ListAvailable -Name ScheduledTasks)) {
            throw "ScheduledTasks module is not available. This feature requires Windows 8/Server 2012 or later."
        }
    }

    process {
        try {
            # Determine script path
            if (-not $ScriptPath) {
                # Try to get from workflow state
                $state = Get-UIWorkflowState -ErrorAction SilentlyContinue
                if ($state -and $state.ScriptPath) {
                    $ScriptPath = $state.ScriptPath
                }
                else {
                    # Try current script
                    $ScriptPath = $MyInvocation.PSCommandPath
                    if (-not $ScriptPath) {
                        throw "Could not determine script path. Please specify -ScriptPath parameter."
                    }
                }
            }

            # Validate script exists
            if (-not (Test-Path $ScriptPath -PathType Leaf)) {
                throw "Script not found: $ScriptPath"
            }

            # Determine PowerShell executable
            if (-not $PowerShellPath) {
                if ($PSVersionTable.PSEdition -eq 'Core') {
                    # PowerShell 7+
                    $PowerShellPath = (Get-Process -Id $PID).Path
                    if (-not $PowerShellPath) {
                        $PowerShellPath = 'pwsh.exe'
                    }
                }
                else {
                    # Windows PowerShell
                    $PowerShellPath = 'powershell.exe'
                }
            }

            # Generate task name if not specified
            if (-not $TaskName) {
                $workflowId = if ($script:CurrentWorkflow) { $script:CurrentWorkflow.Id } else { [guid]::NewGuid().ToString('N').Substring(0, 8) }
                $TaskName = "PoshUI_WorkflowResume_$workflowId"
            }

            # Store task name in module scope for cleanup later
            $script:ResumeTaskName = $TaskName

            # Build the action - run PowerShell with the script
            $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$ScriptPath`""
            $action = New-ScheduledTaskAction -Execute $PowerShellPath -Argument $arguments

            # Build the trigger - at startup or at logon
            if ($RunAsSystem) {
                # Run at system startup (before logon)
                $trigger = New-ScheduledTaskTrigger -AtStartup
                $principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
            }
            else {
                # Run at user logon
                $trigger = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
                $principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Highest
            }

            # Task settings
            $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
                -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 4)

            # Remove existing task if present
            $existingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
            if ($existingTask) {
                Write-Verbose "Removing existing task: $TaskName"
                Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
            }

            # Register the task
            $task = Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger `
                -Principal $principal -Settings $settings -Description "PoshUI Workflow auto-resume after reboot"

            Write-Verbose "Scheduled task registered: $TaskName"
            Write-Verbose "  Script: $ScriptPath"
            Write-Verbose "  PowerShell: $PowerShellPath"
            Write-Verbose "  Run as: $(if ($RunAsSystem) { 'SYSTEM' } else { $env:USERNAME })"

            return $task
        }
        catch {
            Write-Error "Failed to register workflow resume task: $_"
            throw
        }
    }
}
