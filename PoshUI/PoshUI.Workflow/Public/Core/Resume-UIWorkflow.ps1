function Resume-UIWorkflow {
    <#
    .SYNOPSIS
    Resumes a workflow from saved state.

    .DESCRIPTION
    Restores a workflow from a saved state file and prepares it for continued execution.
    The workflow will skip already completed tasks and resume from the last pending task.

    .PARAMETER Path
    Optional custom path for the state file. If not specified, searches default locations.

    .PARAMETER State
    Optional pre-loaded state hashtable from Get-UIWorkflowState.

    .EXAMPLE
    if (Test-UIWorkflowState) {
        Resume-UIWorkflow
        Show-PoshUIWorkflow
    }

    Resumes workflow from saved state if one exists.

    .EXAMPLE
    $state = Get-UIWorkflowState -Path "C:\Temp\state.json"
    Resume-UIWorkflow -State $state

    Resumes workflow from a pre-loaded state.

    .OUTPUTS
    UIWorkflow - The restored workflow object.

    .NOTES
    This function modifies the current workflow context ($script:CurrentWorkflow).
    Call this after New-PoshUIWorkflow but before Show-PoshUIWorkflow.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Path,

        [Parameter()]
        [hashtable]$State
    )

    begin {
        Write-Verbose "Resuming workflow from saved state..."
    }

    process {
        try {
            # Get state if not provided
            if (-not $State) {
                $State = Get-UIWorkflowState -Path $Path
                if (-not $State) {
                    throw "No saved workflow state found."
                }
            }

            # Ensure we have a current workflow
            if (-not $script:CurrentWorkflow) {
                throw "No workflow initialized. Call New-PoshUIWorkflow first."
            }

            # Store resume state in Variables for the C# launcher to use
            # The UIDefinition class uses Variables hashtable for passing state
            $script:CurrentWorkflow.Variables['_ResumeState'] = @{
                CurrentTaskIndex = $State.CurrentTaskIndex
                Tasks = $State.Tasks
                WizardResults = $State.WizardResults
                RebootCount = if ($State.RebootCount) { $State.RebootCount } else { 0 }
                StartTime = $State.StartTime
                IsResume = $true
            }

            # Convert WizardResults PSCustomObject to hashtable if needed
            if ($State.WizardResults) {
                if ($State.WizardResults -is [PSCustomObject]) {
                    $wizardResults = @{}
                    foreach ($prop in $State.WizardResults.PSObject.Properties) {
                        $wizardResults[$prop.Name] = $prop.Value
                    }
                    $script:CurrentWorkflow.Variables['_ResumeState'].WizardResults = $wizardResults
                }
            }

            # Store task completion info in Variables for marking tasks as done
            $completedTasks = @{}
            if ($State.Tasks) {
                foreach ($savedTask in $State.Tasks) {
                    $taskName = if ($savedTask -is [PSCustomObject]) { $savedTask.Name } else { $savedTask['Name'] }
                    $statusStr = if ($savedTask -is [PSCustomObject]) { $savedTask.Status } else { $savedTask['Status'] }
                    $progress = if ($savedTask -is [PSCustomObject]) { $savedTask.ProgressPercent } else { $savedTask['ProgressPercent'] }
                    
                    if ($statusStr -eq 'Completed') {
                        $progressMsg = if ($savedTask -is [PSCustomObject]) { $savedTask.ProgressMessage } else { $savedTask['ProgressMessage'] }
                        $outputLines = if ($savedTask -is [PSCustomObject]) { $savedTask.OutputLines } else { $savedTask['OutputLines'] }
                        $completedTasks[$taskName] = @{
                            Status = $statusStr
                            ProgressPercent = $progress
                            ProgressMessage = $progressMsg
                            OutputLines = $outputLines
                        }
                        Write-Verbose "Task '$taskName' marked as completed from saved state (OutputLines: $($outputLines.Count))"
                    }
                }
            }
            $script:CurrentWorkflow.Variables['_CompletedTasks'] = $completedTasks

            # Store state file path
            if ($State._StateFilePath) {
                $script:CurrentWorkflow.Variables['_StateFilePath'] = $State._StateFilePath
            }

            # Store log file path from previous run for restoring log content
            if ($State.LogFilePath) {
                $script:CurrentWorkflow.Variables['_PreviousLogFilePath'] = $State.LogFilePath
                Write-Verbose "Previous log file path: $($State.LogFilePath)"
            }

            Write-Verbose "Workflow resume state stored. Completed tasks: $($completedTasks.Count)"

            return $script:CurrentWorkflow
        }
        catch {
            Write-Error "Failed to resume workflow: $_"
            throw
        }
    }
}
