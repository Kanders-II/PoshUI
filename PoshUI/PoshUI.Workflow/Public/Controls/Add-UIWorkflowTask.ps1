function Add-UIWorkflowTask {
    <#
    .SYNOPSIS
    Adds a task to a Workflow step for sequential execution.

    .DESCRIPTION
    Creates a new workflow task that will be executed as part of a workflow sequence.
    Tasks can be normal execution tasks or approval gates that pause for user input.
    Tasks execute sequentially and support progress tracking and real-time output streaming.

    .PARAMETER Step
    Name of the Workflow step to add this task to.

    .PARAMETER Name
    Unique name for the task. Used internally for identification.

    .PARAMETER Title
    Display title for the task shown in the workflow UI.

    .PARAMETER Description
    Optional description displayed below the title.

    .PARAMETER Order
    Numeric order for the task. Tasks execute in ascending order.
    If not specified, tasks are ordered by the sequence they are added.

    .PARAMETER Icon
    Optional icon glyph for the task. Must be a Segoe MDL2 icon glyph.

    .PARAMETER ScriptBlock
    PowerShell script block to execute for this task.
    The script block has access to $PoshUIWorkflow for progress updates.

    .PARAMETER ScriptPath
    Path to a PowerShell script file to execute for this task.
    Alternative to ScriptBlock for larger scripts.

    .PARAMETER Arguments
    Hashtable of arguments to pass to the script.

    .PARAMETER TaskType
    Type of task: Normal (default) or ApprovalGate.
    ApprovalGate tasks pause execution and wait for user approval.

    .PARAMETER OnError
    How to handle errors: Stop (default) halts pipeline, Continue proceeds to next task.

    .PARAMETER ApprovalMessage
    Message to display when task is an ApprovalGate.

    .PARAMETER ApproveButtonText
    Custom text for the approve button (default: 'Approve').

    .PARAMETER RejectButtonText
    Custom text for the reject button (default: 'Reject').

    .PARAMETER RequireReason
    If true, user must provide a reason when rejecting.

    .PARAMETER TimeoutMinutes
    Optional timeout for approval gates. 0 means no timeout.

    .PARAMETER DefaultTimeoutAction
    Action to take on timeout: None, Approve, or Reject.

    .PARAMETER RetryCount
    Number of times to retry the task if it fails. Default is 0 (no retry).

    .PARAMETER RetryDelaySeconds
    Delay in seconds between retry attempts. Default is 5 seconds.

    .PARAMETER TimeoutSeconds
    Maximum time in seconds the task can run before timing out. 0 means no timeout.

    .PARAMETER SkipCondition
    PowerShell expression as a STRING that, if true, causes the task to be skipped.
    The condition is evaluated at runtime during workflow execution, not when the workflow is defined.
    
    IMPORTANT: This parameter expects a STRING (in quotes), not a scriptblock (in braces).
    Use quotes: -SkipCondition '$ServerType -ne "Database"'
    NOT braces: -SkipCondition { $ServerType -ne "Database" }
    
    Can reference wizard results ($ParameterName) and workflow data ($WorkflowData['key']).
    The string approach allows the condition to be evaluated in the proper runtime context
    with access to user input values collected during wizard execution.

    .PARAMETER SkipReason
    Message shown when task is skipped by condition.

    .PARAMETER Group
    Group/phase name for organizing tasks visually.

    .PARAMETER RollbackScriptBlock
    PowerShell script to execute if rollback is requested after this task fails.

    .PARAMETER RollbackScriptPath
    Path to a PowerShell script to execute if rollback is requested after this task fails.

    .EXAMPLE
    Add-UIWorkflowTask -Step "Execution" -Name "Install" -Title "Install Software" -Order 1 -ScriptBlock {
        $PoshUIWorkflow.UpdateProgress(10, "Starting installation...")
        Start-Sleep -Seconds 2
        $PoshUIWorkflow.UpdateProgress(100, "Installation complete")
    }

    Creates a task that installs software with progress updates.

    .EXAMPLE
    Add-UIWorkflowTask -Step "Execution" -Name "Confirm" -Title "Confirm Changes" -Order 1 `
        -TaskType ApprovalGate -ApprovalMessage "Apply all changes?" `
        -ApproveButtonText "Yes, Apply" -RejectButtonText "Cancel"

    Creates an approval gate that requires user confirmation before proceeding.

    .EXAMPLE
    Add-UIWorkflowTask -Step "Execution" -Name "Download" -Title "Download Files" -Order 1 `
        -RetryCount 3 -RetryDelaySeconds 10 -TimeoutSeconds 300 -ScriptBlock {
        # Download with retry support
        Invoke-WebRequest -Uri $DownloadUrl -OutFile $DestPath
    }

    Creates a task with retry (3 attempts, 10 second delay) and 5 minute timeout.

    .EXAMPLE
    Add-UIWorkflowTask -Step "Execution" -Name "InstallSQL" -Title "Install SQL Server" -Order 2 `
        -SkipCondition '$ServerType -ne "Database"' -SkipReason "Not a database server" -ScriptBlock {
        # SQL installation logic
    }

    Creates a task that is skipped unless ServerType is "Database".
    Note: SkipCondition uses QUOTES (string) not braces (scriptblock) because it's evaluated at runtime.

    .EXAMPLE
    Add-UIWorkflowTask -Step "Execution" -Name "ConfigureDB" -Title "Configure Database" -Order 3 `
        -Group "Database Setup" -ScriptBlock {
        $installPath = $PoshUIWorkflow.GetData('SQLInstallPath')
        # Use data from previous task
    }

    Creates a task in the "Database Setup" group that uses data from a previous task.

    .OUTPUTS
    UIWorkflowTask object representing the created task.

    .NOTES
    This function requires that a Workflow step has been created with Add-UIStep first.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Step,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Title,

        [Parameter()]
        [string]$Description,

        [Parameter()]
        [int]$Order,

        [Parameter()]
        [string]$Icon,

        [Parameter()]
        [scriptblock]$ScriptBlock,

        [Parameter()]
        [string]$ScriptPath,

        [Parameter()]
        [hashtable]$Arguments,

        [Parameter()]
        [ValidateSet('Normal', 'ApprovalGate')]
        [string]$TaskType = 'Normal',

        [Parameter()]
        [ValidateSet('Stop', 'Continue')]
        [string]$OnError = 'Stop',

        [Parameter()]
        [string]$ApprovalMessage,

        [Parameter()]
        [string]$ApproveButtonText = 'Approve',

        [Parameter()]
        [string]$RejectButtonText = 'Reject',

        [Parameter()]
        [switch]$RequireReason,

        [Parameter()]
        [int]$TimeoutMinutes = 0,

        [Parameter()]
        [ValidateSet('None', 'Approve', 'Reject')]
        [string]$DefaultTimeoutAction = 'None',

        # Advanced task properties
        [Parameter()]
        [ValidateRange(0, 100)]
        [int]$RetryCount = 0,

        [Parameter()]
        [ValidateRange(0, 3600)]
        [int]$RetryDelaySeconds = 5,

        [Parameter()]
        [ValidateRange(0, 86400)]
        [int]$TimeoutSeconds = 0,

        [Parameter()]
        [string]$SkipCondition,

        [Parameter()]
        [string]$SkipReason,

        [Parameter()]
        [string]$Group,

        [Parameter()]
        [scriptblock]$RollbackScriptBlock,

        [Parameter()]
        [string]$RollbackScriptPath
    )

    begin {
        Write-Verbose "Adding workflow task: $Name ($Title) to step $Step"

        # Ensure UI is initialized
        if (-not $script:CurrentWorkflow) {
            throw "No UI initialized. Call New-PoshUIWorkflow first."
        }
    }

    process {
        try {
            # Get the target step
            if (-not $script:CurrentWorkflow.HasStep($Step)) {
                throw "Step '$Step' does not exist. Create it first with Add-UIStep."
            }

            $targetStep = $script:CurrentWorkflow.GetStep($Step)

            # Verify it's a Workflow step
            if ($targetStep.Type -ne 'Workflow') {
                throw "Step '$Step' is not a Workflow step. Workflow tasks can only be added to Workflow steps."
            }

            # Initialize workflow on step if not present
            if (-not $targetStep.Properties.ContainsKey('Workflow')) {
                $workflow = [UIWorkflow]::new($targetStep.Title)
                $targetStep.Properties['Workflow'] = $workflow
            }

            $workflow = $targetStep.Properties['Workflow']

            # Auto-assign order if not specified
            if (-not $PSBoundParameters.ContainsKey('Order')) {
                $Order = $workflow.Tasks.Count + 1
            }

            # Check for duplicate task name
            $existingTask = $workflow.Tasks | Where-Object Name -eq $Name
            if ($existingTask) {
                throw "Task with name '$Name' already exists in workflow"
            }

            # Validate TaskType requirements
            if ($TaskType -eq 'ApprovalGate') {
                if ([string]::IsNullOrWhiteSpace($ApprovalMessage)) {
                    throw "ApprovalMessage is required for ApprovalGate tasks"
                }
            }
            else {
                # Normal tasks need either a ScriptBlock or ScriptPath
                if (-not $ScriptBlock -and [string]::IsNullOrWhiteSpace($ScriptPath)) {
                    throw "Either ScriptBlock or ScriptPath is required for Normal tasks"
                }

                # Validate ScriptPath exists if provided
                if (-not [string]::IsNullOrWhiteSpace($ScriptPath)) {
                    if (-not (Test-Path $ScriptPath -PathType Leaf)) {
                        throw "ScriptPath '$ScriptPath' does not exist"
                    }
                }
            }

            # Create the task
            $task = [UIWorkflowTask]::new($Name, $Title, $Order)
            $task.Description = $Description
            $task.Icon = $Icon
            $task.ScriptBlock = $ScriptBlock
            $task.ScriptPath = $ScriptPath
            $task.Arguments = $Arguments
            $task.TaskType = [UIWorkflowTaskType]::$TaskType
            $task.ErrorAction = $OnError
            $task.ApprovalMessage = $ApprovalMessage
            $task.ApproveButtonText = $ApproveButtonText
            $task.RejectButtonText = $RejectButtonText
            $task.RequireReason = $RequireReason.IsPresent
            $task.TimeoutMinutes = $TimeoutMinutes
            $task.DefaultTimeoutAction = $DefaultTimeoutAction

            # Advanced task properties
            $task.RetryCount = $RetryCount
            $task.RetryDelaySeconds = $RetryDelaySeconds
            $task.TimeoutSeconds = $TimeoutSeconds
            $task.SkipCondition = $SkipCondition
            $task.SkipReason = $SkipReason
            $task.Group = $Group
            $task.RollbackScriptBlock = $RollbackScriptBlock
            $task.RollbackScriptPath = $RollbackScriptPath

            # Add to workflow
            $workflow.AddTask($task)

            Write-Verbose "Successfully added workflow task: $($task.ToString())"

            return $task
        }
        catch {
            Write-Error "Failed to add workflow task '$Name': $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Add-UIWorkflowTask completed for: $Name"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WorkflowTask' -Value 'Add-UIWorkflowTask'
