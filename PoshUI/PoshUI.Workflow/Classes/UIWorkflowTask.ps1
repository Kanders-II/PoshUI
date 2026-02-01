# UIWorkflowTask.ps1 - Workflow task definition class for PoshUI

<#
.SYNOPSIS
Represents a single task in a workflow execution sequence.

.DESCRIPTION
UIWorkflowTask class defines a task that can be executed as part of a workflow.
Tasks can be normal execution tasks or approval gates that pause for user input.
#>

class UIWorkflowTask {
    [string]$Name
    [string]$Title
    [string]$Description
    [int]$Order
    [string]$Icon
    [scriptblock]$ScriptBlock
    [string]$ScriptPath
    [hashtable]$Arguments
    [UIWorkflowTaskType]$TaskType = [UIWorkflowTaskType]::Normal
    [UIWorkflowTaskStatus]$Status = [UIWorkflowTaskStatus]::NotStarted

    # Execution tracking
    [datetime]$StartTime
    [datetime]$EndTime
    [timespan]$Duration
    [int]$ProgressPercent = 0
    [string]$ProgressMessage = ''
    [string]$ErrorMessage = ''
    [System.Collections.Generic.List[string]]$OutputLines

    # Approval gate properties
    [string]$ApprovalMessage
    [string]$ApproveButtonText = 'Approve'
    [string]$RejectButtonText = 'Reject'
    [bool]$RequireReason = $false
    [int]$TimeoutMinutes = 0
    [string]$DefaultTimeoutAction = 'None'
    [string]$ApprovalReason = ''
    [string]$ApprovalAction = ''
    [datetime]$ApprovalTime

    # Error handling
    [string]$ErrorAction = 'Stop'

    # Retry properties
    [int]$RetryCount = 0
    [int]$RetryDelaySeconds = 5

    # Timeout properties (task execution timeout, distinct from approval timeout)
    [int]$TimeoutSeconds = 0

    # Skip/conditional execution
    [string]$SkipCondition
    [string]$SkipReason

    # Task grouping/phases
    [string]$Group

    # Rollback support
    [scriptblock]$RollbackScriptBlock
    [string]$RollbackScriptPath

    # Constructor
    UIWorkflowTask() {
        $this.OutputLines = [System.Collections.Generic.List[string]]::new()
    }

    UIWorkflowTask([string]$Name, [string]$Title, [int]$Order) {
        $this.Name = $Name
        $this.Title = $Title
        $this.Order = $Order
        $this.OutputLines = [System.Collections.Generic.List[string]]::new()
    }

    # Methods
    [void]SetStatus([UIWorkflowTaskStatus]$NewStatus) {
        $this.Status = $NewStatus

        if ($NewStatus -eq [UIWorkflowTaskStatus]::Running -and $null -eq $this.StartTime) {
            $this.StartTime = [datetime]::Now
        }
        elseif ($NewStatus -in @([UIWorkflowTaskStatus]::Completed, [UIWorkflowTaskStatus]::Failed, [UIWorkflowTaskStatus]::Skipped)) {
            $this.EndTime = [datetime]::Now
            if ($null -ne $this.StartTime) {
                $this.Duration = $this.EndTime - $this.StartTime
            }
        }
    }

    [void]UpdateProgress([int]$Percent, [string]$Message) {
        $this.ProgressPercent = [Math]::Max(0, [Math]::Min(100, $Percent))
        $this.ProgressMessage = $Message
    }

    [void]AddOutput([string]$Line) {
        $this.OutputLines.Add($Line)
    }

    [void]SetError([string]$Message) {
        $this.ErrorMessage = $Message
        $this.SetStatus([UIWorkflowTaskStatus]::Failed)
    }

    [void]RequestReboot([string]$Reason) {
        $this.ProgressMessage = $Reason
        $this.SetStatus([UIWorkflowTaskStatus]::PendingReboot)
    }

    [void]SetApproval([string]$Action, [string]$Reason) {
        $this.ApprovalAction = $Action
        $this.ApprovalReason = $Reason
        $this.ApprovalTime = [datetime]::Now

        if ($Action -eq 'Approved') {
            $this.SetStatus([UIWorkflowTaskStatus]::Completed)
        }
        elseif ($Action -eq 'Rejected') {
            $this.SetStatus([UIWorkflowTaskStatus]::Failed)
            $this.ErrorMessage = "Rejected: $Reason"
        }
    }

    [hashtable]ToHashtable() {
        return @{
            Name = $this.Name
            Title = $this.Title
            Description = $this.Description
            Order = $this.Order
            Icon = $this.Icon
            ScriptPath = $this.ScriptPath
            Arguments = $this.Arguments
            TaskType = $this.TaskType.ToString()
            Status = $this.Status.ToString()
            StartTime = $this.StartTime
            EndTime = $this.EndTime
            Duration = $this.Duration
            ProgressPercent = $this.ProgressPercent
            ProgressMessage = $this.ProgressMessage
            ErrorMessage = $this.ErrorMessage
            ApprovalMessage = $this.ApprovalMessage
            ApproveButtonText = $this.ApproveButtonText
            RejectButtonText = $this.RejectButtonText
            RequireReason = $this.RequireReason
            TimeoutMinutes = $this.TimeoutMinutes
            ApprovalAction = $this.ApprovalAction
            ApprovalReason = $this.ApprovalReason
            # Advanced task properties
            RetryCount = $this.RetryCount
            RetryDelaySeconds = $this.RetryDelaySeconds
            TimeoutSeconds = $this.TimeoutSeconds
            SkipCondition = $this.SkipCondition
            SkipReason = $this.SkipReason
            Group = $this.Group
            RollbackScriptPath = $this.RollbackScriptPath
        }
    }

    [string]ToString() {
        return "UIWorkflowTask: '$($this.Title)' (Order: $($this.Order), Status: $($this.Status))"
    }
}
