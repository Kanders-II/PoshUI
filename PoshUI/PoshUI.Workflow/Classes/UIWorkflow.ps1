# UIWorkflow.ps1 - Workflow container class for PoshUI

<#
.SYNOPSIS
Represents a complete workflow with multiple tasks.

.DESCRIPTION
UIWorkflow class manages a collection of tasks to be executed sequentially,
with support for progress tracking, reboot persistence, and approval gates.
#>

class UIWorkflow {
    [string]$Id
    [string]$Name
    [string]$Description
    [System.Collections.Generic.List[UIWorkflowTask]]$Tasks
    [int]$CurrentTaskIndex = -1
    [UIWorkflowTaskStatus]$OverallStatus = [UIWorkflowTaskStatus]::NotStarted
    [datetime]$StartTime
    [datetime]$EndTime
    [timespan]$TotalDuration
    [string]$ErrorAction = 'Stop'

    # State persistence
    [string]$StateFilePath
    [hashtable]$WizardResults
    [int]$RebootCount = 0
    [System.Collections.Generic.List[hashtable]]$RebootHistory

    # Constructor
    UIWorkflow() {
        $this.Id = [guid]::NewGuid().ToString('N').Substring(0, 8)
        $this.Tasks = [System.Collections.Generic.List[UIWorkflowTask]]::new()
        $this.RebootHistory = [System.Collections.Generic.List[hashtable]]::new()
    }

    UIWorkflow([string]$Name) {
        $this.Id = [guid]::NewGuid().ToString('N').Substring(0, 8)
        $this.Name = $Name
        $this.Tasks = [System.Collections.Generic.List[UIWorkflowTask]]::new()
        $this.RebootHistory = [System.Collections.Generic.List[hashtable]]::new()
    }

    # Methods
    [void]AddTask([UIWorkflowTask]$Task) {
        if ($this.Tasks | Where-Object Name -eq $Task.Name) {
            throw "Task with name '$($Task.Name)' already exists in workflow"
        }
        $this.Tasks.Add($Task)
        # Sort by order
        $sorted = $this.Tasks | Sort-Object Order
        $this.Tasks.Clear()
        foreach ($t in $sorted) {
            $this.Tasks.Add($t)
        }
    }

    [UIWorkflowTask]GetTask([string]$Name) {
        $task = $this.Tasks | Where-Object Name -eq $Name
        if (-not $task) {
            throw "Task with name '$Name' not found in workflow"
        }
        return $task
    }

    [UIWorkflowTask]GetCurrentTask() {
        if ($this.CurrentTaskIndex -ge 0 -and $this.CurrentTaskIndex -lt $this.Tasks.Count) {
            return $this.Tasks[$this.CurrentTaskIndex]
        }
        return $null
    }

    [UIWorkflowTask]GetNextTask() {
        $nextIndex = $this.CurrentTaskIndex + 1
        if ($nextIndex -lt $this.Tasks.Count) {
            return $this.Tasks[$nextIndex]
        }
        return $null
    }

    [bool]HasMoreTasks() {
        return ($this.CurrentTaskIndex + 1) -lt $this.Tasks.Count
    }

    [int]GetCompletedCount() {
        return ($this.Tasks | Where-Object { $_.Status -eq [UIWorkflowTaskStatus]::Completed }).Count
    }

    [int]GetTotalCount() {
        return $this.Tasks.Count
    }

    [double]GetOverallProgress() {
        if ($this.Tasks.Count -eq 0) { return 0 }
        $completed = $this.GetCompletedCount()
        $currentProgress = 0
        $currentTask = $this.GetCurrentTask()
        if ($currentTask -and $currentTask.Status -eq [UIWorkflowTaskStatus]::Running) {
            $currentProgress = $currentTask.ProgressPercent / 100
        }
        return (($completed + $currentProgress) / $this.Tasks.Count) * 100
    }

    [hashtable]ToState() {
        return @{
            Id = $this.Id
            Name = $this.Name
            CurrentTaskIndex = $this.CurrentTaskIndex
            OverallStatus = $this.OverallStatus.ToString()
            StartTime = $this.StartTime
            WizardResults = $this.WizardResults
            RebootCount = $this.RebootCount
            RebootHistory = $this.RebootHistory
            Tasks = @($this.Tasks | ForEach-Object { $_.ToHashtable() })
            SchemaVersion = 1
            LastSaveTime = [datetime]::Now
        }
    }

    [string]ToString() {
        $completed = $this.GetCompletedCount()
        return "UIWorkflow: '$($this.Name)' ($completed/$($this.Tasks.Count) tasks, Status: $($this.OverallStatus))"
    }
}
