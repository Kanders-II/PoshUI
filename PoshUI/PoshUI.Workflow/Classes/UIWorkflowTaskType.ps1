# UIWorkflowTaskType.ps1 - Workflow task type enumeration for PoshUI

<#
.SYNOPSIS
Defines the types of workflow tasks.

.DESCRIPTION
UIWorkflowTaskType enum specifies whether a task is a normal execution task
or an approval gate requiring user interaction.
#>

enum UIWorkflowTaskType {
    Normal = 0
    ApprovalGate = 1
}
