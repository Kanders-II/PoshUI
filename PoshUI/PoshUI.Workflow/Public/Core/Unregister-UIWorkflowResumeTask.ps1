function Unregister-UIWorkflowResumeTask {
    <#
    .SYNOPSIS
    Removes the scheduled task created for workflow auto-resume.

    .DESCRIPTION
    Unregisters the Windows scheduled task that was created to auto-resume
    the workflow after a system reboot. This should be called when the
    workflow completes successfully or is cancelled.

    .PARAMETER TaskName
    Name of the scheduled task to remove. If not specified, uses the task
    name stored during Register-UIWorkflowResumeTask or searches for tasks
    matching the PoshUI_WorkflowResume_* pattern.

    .PARAMETER RemoveAll
    If specified, removes all PoshUI workflow resume tasks (useful for cleanup).

    .EXAMPLE
    Unregister-UIWorkflowResumeTask

    Removes the current workflow's resume task.

    .EXAMPLE
    Unregister-UIWorkflowResumeTask -RemoveAll

    Removes all PoshUI workflow resume tasks.

    .OUTPUTS
    None

    .NOTES
    This function is automatically called when a workflow completes.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$TaskName,

        [Parameter()]
        [switch]$RemoveAll
    )

    begin {
        Write-Verbose "Unregistering workflow resume scheduled task..."
    }

    process {
        try {
            if ($RemoveAll) {
                # Remove all PoshUI workflow resume tasks
                $tasks = Get-ScheduledTask -TaskName "PoshUI_WorkflowResume_*" -ErrorAction SilentlyContinue
                if ($tasks) {
                    foreach ($task in $tasks) {
                        Write-Verbose "Removing task: $($task.TaskName)"
                        Unregister-ScheduledTask -TaskName $task.TaskName -Confirm:$false
                    }
                    Write-Verbose "Removed $($tasks.Count) workflow resume task(s)"
                }
                else {
                    Write-Verbose "No PoshUI workflow resume tasks found"
                }
            }
            else {
                # Determine task name
                if (-not $TaskName) {
                    # Try module scope variable
                    if ($script:ResumeTaskName) {
                        $TaskName = $script:ResumeTaskName
                    }
                    else {
                        # Try to find by workflow ID
                        if ($script:CurrentWorkflow) {
                            $TaskName = "PoshUI_WorkflowResume_$($script:CurrentWorkflow.Id)"
                        }
                    }
                }

                if ($TaskName) {
                    $existingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
                    if ($existingTask) {
                        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
                        Write-Verbose "Removed scheduled task: $TaskName"
                        
                        # Clear module scope variable
                        $script:ResumeTaskName = $null
                    }
                    else {
                        Write-Verbose "Scheduled task not found: $TaskName"
                    }
                }
                else {
                    Write-Verbose "No task name specified and no current workflow task found"
                }
            }
        }
        catch {
            Write-Warning "Failed to unregister workflow resume task: $_"
        }
    }
}
