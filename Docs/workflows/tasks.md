# Working with Tasks

Tasks are the individual units of work in a PoshUI Workflow. They execute sequentially and provide real-time feedback to the user.

## Quick Start: Choose Your Task Type

```
Is this a simple execution task?
├─ Yes: Use Normal Task (see below)
└─ No: Does it need user approval?
   ├─ Yes: Use Approval Gate (see below)
   └─ No: Still use Normal Task
```

## Normal Tasks

A normal task executes a PowerShell script or script block and reports its progress to the UI.

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'InstallApp' -Title 'Installing Application' `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Downloading installer...")
        # Logic here
        $PoshUIWorkflow.WriteOutput("Running setup...")
        # Logic here
    }
```

### Key Parameters

| Parameter | Description |
|-----------|-------------|
| `-Name` | Unique identifier for the task. |
| `-Title` | Display name shown in the execution list. |
| `-ScriptBlock` | PowerShell code to execute. |
| `-ScriptPath` | Path to a `.ps1` file to execute (alternative to ScriptBlock). |
| `-OnError` | How to handle failures: `Stop` (default) or `Continue`. |
| `-Arguments` | Hashtable of parameters passed to the task. |

### Pattern: Simple Task with Auto-Progress

Use this pattern for straightforward tasks where you just report status updates:

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Configure' -Title 'Configuring System' `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Step 1: Checking prerequisites...", "INFO")
        # Do work
        
        $PoshUIWorkflow.WriteOutput("Step 2: Installing components...", "INFO")
        # Do work
        
        $PoshUIWorkflow.WriteOutput("Step 3: Finalizing...", "INFO")
        # Do work
        # Progress bar automatically advances to 100%
    }
```

### Pattern: Complex Task with Manual Progress Control

Use this pattern when you need precise control over progress reporting:

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Deploy' -Title 'Deploying Application' `
    -ScriptBlock {
        $PoshUIWorkflow.UpdateProgress(10, "Validating configuration...")
        # Do validation
        
        $PoshUIWorkflow.UpdateProgress(40, "Downloading files...")
        # Do download
        
        $PoshUIWorkflow.UpdateProgress(70, "Installing...")
        # Do installation
        
        $PoshUIWorkflow.UpdateProgress(100, "Complete")
    }
```

::: warning Important
**Do not mix `WriteOutput()` and `UpdateProgress()` in the same task.** Choose one approach:
- Use `WriteOutput()` for simple sequential tasks (auto-progress)
- Use `UpdateProgress()` for tasks where you need explicit control
:::

### Pattern: Task with Arguments

Pass task-specific parameters using the `-Arguments` parameter:

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Backup' -Title 'Creating Backup' `
    -ScriptBlock {
        param($BackupPath, $RetentionDays)
        
        $PoshUIWorkflow.WriteOutput("Backing up to $BackupPath...", "INFO")
        # Use $BackupPath and $RetentionDays here
        
        $PoshUIWorkflow.WriteOutput("Backup complete", "INFO")
    } `
    -Arguments @{
        BackupPath = "C:\Backups"
        RetentionDays = 30
    }
```

## Approval Gates

An Approval Gate pauses the workflow and waits for a user to click "Approve" or "Reject" before proceeding.

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Audit' -Title 'Security Audit' `
    -TaskType ApprovalGate `
    -ApprovalMessage 'Please verify the system configuration before proceeding.' `
    -ApproveButtonText 'Proceed' `
    -RejectButtonText 'Abort'
```

### Key Parameters

| Parameter | Description |
|-----------|-------------|
| `-TaskType` | Must be set to `ApprovalGate`. |
| `-ApprovalMessage` | The message displayed to the user. |
| `-ApproveButtonText` | Label for the positive action button. |
| `-RejectButtonText` | Label for the negative action button. |
| `-RequireReason` | If true, user must provide text when rejecting. |

### Pattern: Approval Gate with Reason

Require users to provide a reason when rejecting:

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Review' -Title 'Configuration Review' `
    -TaskType ApprovalGate `
    -ApprovalMessage 'Review the configuration and approve to proceed.' `
    -RequireReason `
    -ApproveButtonText 'Approve' `
    -RejectButtonText 'Reject'
```

## Advanced Task Features

### Task Retry

Automatically retry failed tasks with configurable delay:

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Download' -Title 'Download Files' `
    -RetryCount 3 `
    -RetryDelaySeconds 10 `
    -ScriptBlock {
        # This will retry up to 3 times with 10 second delays
        Invoke-WebRequest -Uri $Url -OutFile $Path
    }
```

### Task Timeout

Set a maximum execution time for tasks:

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'LongTask' -Title 'Long Running Task' `
    -TimeoutSeconds 300 `
    -ScriptBlock {
        # Task will be cancelled if it runs longer than 5 minutes
        Start-LongOperation
    }
```

### Conditional Skip

Skip tasks based on conditions evaluated at runtime:

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'InstallSQL' -Title 'Install SQL Server' `
    -SkipCondition '$ServerType -ne "Database"' `
    -SkipReason 'Not a database server' `
    -ScriptBlock {
        # Only runs when ServerType equals "Database"
        Install-SQLServer
    }

# Skip based on data from previous task
Add-UIWorkflowTask -Step 'Execution' -Name 'Configure' -Title 'Configure App' `
    -SkipCondition '$WorkflowData["AlreadyInstalled"] -eq $true' `
    -SkipReason 'Application already installed' `
    -ScriptBlock {
        Install-Application
    }
```

::: tip Important: Use Quotes, Not Braces
**SkipCondition expects a STRING (in quotes), not a scriptblock (in braces).**

- ✅ **Correct**: `-SkipCondition '$ServerType -ne "Database"'`
- ❌ **Incorrect**: `-SkipCondition { $ServerType -ne "Database" }`

**Why quotes?** The condition is stored as a string and evaluated at runtime during workflow execution, not when the workflow is defined. This allows the condition to access wizard variables and workflow data that may not exist when the script is first parsed.
:::

### Task Groups

Organize tasks into logical phases:

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'CheckPrereqs' -Title 'Check Prerequisites' `
    -Group 'Pre-flight Checks' -ScriptBlock { ... }

Add-UIWorkflowTask -Step 'Execution' -Name 'Validate' -Title 'Validate Config' `
    -Group 'Pre-flight Checks' -ScriptBlock { ... }

Add-UIWorkflowTask -Step 'Execution' -Name 'Install' -Title 'Install Software' `
    -Group 'Installation' -ScriptBlock { ... }

Add-UIWorkflowTask -Step 'Execution' -Name 'Configure' -Title 'Configure Software' `
    -Group 'Configuration' -ScriptBlock { ... }
```

### Rollback Scripts

Define cleanup scripts that run when a task fails:

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Install' -Title 'Install Application' `
    -RollbackScriptBlock {
        # This runs if the main script fails
        Write-Host "Rolling back installation..."
        Uninstall-Application
    } `
    -ScriptBlock {
        Install-Application
    }
```

## Accessing Data in Tasks

### From Wizard Inputs

If your workflow has a wizard phase, wizard inputs are automatically available as variables:

```powershell
# Wizard step defines this:
Add-UITextBox -Step 'Config' -Name 'ServerName' -Label 'Server Name'

# Task can access it directly:
Add-UIWorkflowTask -Step 'Execution' -Name 'Deploy' -ScriptBlock {
    Write-Host "Deploying to: $ServerName"  # $ServerName is available
}
```

### From Task Arguments

Task-specific parameters are passed via the `-Arguments` parameter:

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Task' -ScriptBlock {
    param($Param1, $Param2)
    # $Param1 and $Param2 are available
} -Arguments @{
    Param1 = "Value1"
    Param2 = "Value2"
}
```

### Sharing Data Between Tasks

Use the `$PoshUIWorkflow` context to pass data between tasks:

```powershell
# Task 1: Store data for later
Add-UIWorkflowTask -Step 'Execution' -Name 'Install' -Title 'Install Application' `
    -ScriptBlock {
        $installPath = "C:\Program Files\MyApp"
        Install-Application -Path $installPath

        # Store for subsequent tasks
        $PoshUIWorkflow.SetData('InstallPath', $installPath)
        $PoshUIWorkflow.SetData('InstalledVersion', '2.0.0')
    }

# Task 2: Retrieve data from previous task
Add-UIWorkflowTask -Step 'Execution' -Name 'Configure' -Title 'Configure Application' `
    -ScriptBlock {
        $installPath = $PoshUIWorkflow.GetData('InstallPath')
        $version = $PoshUIWorkflow.GetData('InstalledVersion')

        Configure-Application -Path $installPath -Version $version
    }

# Check if data exists
Add-UIWorkflowTask -Step 'Execution' -Name 'Verify' -ScriptBlock {
    if ($PoshUIWorkflow.HasData('InstallPath')) {
        $path = $PoshUIWorkflow.GetData('InstallPath')
        Test-Path $path
    }
}
```

### From $PoshUIWorkflow Context

The `$PoshUIWorkflow` object is always available:

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Task' -ScriptBlock {
    # Progress and status
    $PoshUIWorkflow.WriteOutput("Status message", "INFO")
    $PoshUIWorkflow.UpdateProgress(50, "Working...")

    # Reboot support
    $PoshUIWorkflow.RequestReboot("Reboot needed")

    # Skip current task
    $PoshUIWorkflow.SkipTask("Condition not met")

    # Data sharing
    $PoshUIWorkflow.SetData('key', 'value')
    $value = $PoshUIWorkflow.GetData('key')
    $exists = $PoshUIWorkflow.HasData('key')
    $allKeys = $PoshUIWorkflow.GetDataKeys()

    # Task info
    $currentIndex = $PoshUIWorkflow.CurrentTaskIndex
    $totalTasks = $PoshUIWorkflow.TotalTaskCount
}

## Progress Reporting Decision Tree

```
Do you know exactly what percentage complete each step is?
├─ Yes: Use UpdateProgress() with explicit percentages
│   └─ Call it multiple times as work progresses
│
└─ No: Use WriteOutput() for status messages
    └─ Progress auto-advances with each call
    └─ Simpler, but less precise
```

## Error Handling

Control how tasks respond to failures:

```powershell
# Stop on error (default) - workflow stops if this task fails
Add-UIWorkflowTask -Step 'Execution' -Name 'Critical' -ScriptBlock {
    # If this fails, workflow stops
} -OnError Stop

# Continue on error - workflow continues even if this task fails
Add-UIWorkflowTask -Step 'Execution' -Name 'Optional' -ScriptBlock {
    # If this fails, workflow continues to next task
} -OnError Continue
```

Next: [Reboot & Resume](./reboot-resume.md) | [Progress Reporting Guide](./progress-reporting.md) | [Data Passing Patterns](./data-passing.md)
