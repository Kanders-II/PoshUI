# UIFactory.ps1 - Factory class for creating UI elements

<#
.SYNOPSIS
Factory class for creating UI elements in PoshUI.Workflow.

.DESCRIPTION
UIFactory provides static methods for creating pre-configured UI elements.
#>

class UIFactory {
    # Create a new UIDefinition with default settings
    static [UIDefinition]CreateWorkflow([string]$Title) {
        $definition = [UIDefinition]::new($Title)
        $definition.Template = 'Workflow'
        $definition.ViewMode = 'Workflow'
        return $definition
    }

    # Create a new workflow step
    static [UIStep]CreateStep([string]$Name, [string]$Title, [int]$Order) {
        $step = [UIStep]::new($Name, $Title, $Order)
        $step.Type = 'Workflow'
        return $step
    }

    # Create a new workflow task
    static [UIWorkflowTask]CreateTask([string]$Name, [string]$Title, [int]$Order) {
        return [UIWorkflowTask]::new($Name, $Title, $Order)
    }

    # Create a workflow from a hashtable definition
    static [UIWorkflow]CreateWorkflowFromHashtable([hashtable]$Definition) {
        $workflow = [UIWorkflow]::new($Definition.Name)
        
        if ($Definition.Description) {
            $workflow.Description = $Definition.Description
        }
        
        if ($Definition.ErrorAction) {
            $workflow.ErrorAction = $Definition.ErrorAction
        }
        
        if ($Definition.Tasks) {
            foreach ($taskDef in $Definition.Tasks) {
                $task = [UIWorkflowTask]::new($taskDef.Name, $taskDef.Title, $taskDef.Order)
                if ($taskDef.Description) { $task.Description = $taskDef.Description }
                if ($taskDef.Icon) { $task.Icon = $taskDef.Icon }
                if ($taskDef.ScriptBlock) { $task.ScriptBlock = $taskDef.ScriptBlock }
                if ($taskDef.ScriptPath) { $task.ScriptPath = $taskDef.ScriptPath }
                if ($taskDef.Arguments) { $task.Arguments = $taskDef.Arguments }
                if ($taskDef.TaskType) { $task.TaskType = [UIWorkflowTaskType]::($taskDef.TaskType) }
                if ($taskDef.ErrorAction) { $task.ErrorAction = $taskDef.ErrorAction }
                if ($taskDef.ApprovalMessage) { $task.ApprovalMessage = $taskDef.ApprovalMessage }
                if ($taskDef.ApproveButtonText) { $task.ApproveButtonText = $taskDef.ApproveButtonText }
                if ($taskDef.RejectButtonText) { $task.RejectButtonText = $taskDef.RejectButtonText }
                if ($taskDef.RequireReason) { $task.RequireReason = $taskDef.RequireReason }
                if ($taskDef.TimeoutMinutes) { $task.TimeoutMinutes = $taskDef.TimeoutMinutes }
                
                $workflow.AddTask($task)
            }
        }
        
        return $workflow
    }
}
