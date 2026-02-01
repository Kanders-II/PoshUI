# Reboot & Resume

One of the most powerful features of PoshUI Workflows is the ability to handle system reboots and automatically resume execution.

## Key Concept

`RequestReboot()` is **not** a system reboot command. It's a **workflow control signal** that:
1. Saves the current workflow state to disk
2. Stops the current task execution
3. Shows the user a reboot dialog
4. Allows the workflow to resume from the next task after reboot

## Requesting a Reboot

From within any workflow task, call `$PoshUIWorkflow.RequestReboot()`:

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'UpdateKernel' -Title 'Updating Kernel' -ScriptBlock {
    $PoshUIWorkflow.WriteOutput("Kernel update applied. System needs to restart.", "INFO")
    
    # Request a reboot with a reason
    $PoshUIWorkflow.RequestReboot("Kernel Update Applied")
    
    # Code after RequestReboot() will execute after resume
    $PoshUIWorkflow.WriteOutput("System rebooted successfully", "INFO")
}
```

### What Happens

1. **State Saved**: Workflow state is automatically saved to `%LOCALAPPDATA%\PoshUI\PoshUI_Workflow_State.json`
2. **Task Marked Complete**: Current task is marked as completed
3. **Dialog Shown**: User sees reboot options ("Reboot Now", "Reboot Later", "Simulate")
4. **Execution Stops**: Remaining tasks pause until after reboot
5. **Script Closes**: The PoshUI window closes

## Resuming After Reboot

When the system restarts and the user runs the script again, it automatically detects the saved state and resumes.

### Basic Resume Pattern

```powershell
Import-Module PoshUI.Workflow

# 1. Initialize workflow (must match the original)
New-PoshUIWorkflow -Title 'System Setup'

# 2. Check for saved state
if (Test-UIWorkflowState) {
    # 3. Load and resume from saved state
    Resume-UIWorkflow
    
    # 4. Redefine the SAME tasks (Resume-UIWorkflow marks completed ones to skip)
    Add-UIWorkflowTask -Step 'Execution' -Name 'Phase1' -Title 'Phase 1' -ScriptBlock { ... }
    Add-UIWorkflowTask -Step 'Execution' -Name 'Phase2' -Title 'Phase 2' -ScriptBlock { ... }
    
    # 5. Show the UI (skips wizard, continues from next pending task)
    Show-PoshUIWorkflow
} else {
    # First run: normal workflow setup
    Add-UIStep -Name 'Config' -Title 'Configuration' -Order 1
    Add-UITextBox -Step 'Config' -Name 'ServerName' -Label 'Server Name'
    
    Add-UIStep -Name 'Execution' -Title 'Execution' -Order 2 -Type Workflow
    Add-UIWorkflowTask -Step 'Execution' -Name 'Phase1' -Title 'Phase 1' -ScriptBlock { ... }
    Add-UIWorkflowTask -Step 'Execution' -Name 'Phase2' -Title 'Phase 2' -ScriptBlock { ... }
    
    Show-PoshUIWorkflow
}
```

## Reboot & Resume Checklist

Use this checklist when implementing reboot/resume workflows:

- [ ] **Define workflow structure** - Create all steps and tasks before first run
- [ ] **Call `Test-UIWorkflowState`** - Check for saved state at script start
- [ ] **Call `Resume-UIWorkflow`** - Load saved state if it exists
- [ ] **Redefine all tasks** - Must define the same tasks in the same order
- [ ] **Use `RequestReboot()`** - Call from task when reboot is needed
- [ ] **Test first run** - Verify wizard and first tasks work
- [ ] **Test resume** - Simulate reboot and verify resume works
- [ ] **Verify cleanup** - Confirm state file is deleted after completion
- [ ] **Test cancellation** - Verify state file cleanup if user cancels

## State File Management

### Location
`%LOCALAPPDATA%\PoshUI\PoshUI_Workflow_State.json`

### Contents
- Current task index (which task to resume from)
- All wizard input values
- Completed task output and progress
- Log file path (for continuity)
- Timestamp and computer info

### Automatic Cleanup
The state file is **automatically deleted** when:
- Workflow completes successfully
- User cancels the workflow

### Manual Cleanup
If you need to manually clear the state (for testing or recovery):

```powershell
Clear-UIWorkflowState
```

## State Persistence Details

### What is Saved
- ✅ Wizard input values
- ✅ Completed task status and output
- ✅ Log file path
- ✅ Current task index

### What is NOT Saved
- ❌ Variables created during task execution
- ❌ External file changes
- ❌ Registry modifications
- ❌ Installed software

**Important**: Your tasks should be **idempotent** (safe to run multiple times). For example:
- Check if software is already installed before installing
- Check if a service is already running before starting it
- Use `-Force` flags cautiously on resume

### Example: Idempotent Task

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'InstallFeature' -Title 'Install Windows Feature' `
    -ScriptBlock {
        $feature = Get-WindowsFeature -Name 'Web-Server'
        
        if ($feature.Installed) {
            $PoshUIWorkflow.WriteOutput("Feature already installed", "INFO")
        } else {
            $PoshUIWorkflow.WriteOutput("Installing feature...", "INFO")
            Install-WindowsFeature -Name 'Web-Server'
            $PoshUIWorkflow.WriteOutput("Feature installed", "INFO")
        }
    }
```

## Auto-Launch After Reboot (Optional)

For the best user experience, add your script to the Windows RunOnce registry key so it launches automatically after reboot:

```powershell
$scriptPath = $PSCommandPath
$regPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\RunOnce'

# Add script to RunOnce (runs once at next logon)
New-ItemProperty -Path $regPath -Name 'PoshUIWorkflow' `
    -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`"" `
    -Force | Out-Null

# Then request reboot
$PoshUIWorkflow.RequestReboot("System reboot required")
```

## Troubleshooting

### State file not found after reboot
- Verify the script is running as the same user
- Check that `%LOCALAPPDATA%` path is accessible
- Confirm `Test-UIWorkflowState` is called before `Resume-UIWorkflow`

### Tasks running again after resume
- Ensure you call `Resume-UIWorkflow` before defining tasks
- Verify task names match exactly (case-sensitive)
- Check that task order hasn't changed

### State file not deleted after completion
- Verify workflow completed without errors
- Check that all tasks have status "Completed"
- Manually clear with `Clear-UIWorkflowState` if needed

Next: [Workflow Logging](./logging.md) | [Working with Tasks](./tasks.md)
