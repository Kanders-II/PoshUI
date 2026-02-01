# UIWorkflowTaskStatus.ps1 - Workflow task status enumeration for PoshUI

<#
.SYNOPSIS
Defines the status states for workflow tasks.

.DESCRIPTION
UIWorkflowTaskStatus enum specifies the execution state of a workflow task.
#>

enum UIWorkflowTaskStatus {
    NotStarted = 0
    Running = 1
    Completed = 2
    Failed = 3
    PendingReboot = 4
    AwaitingApproval = 5
    Skipped = 6
}
