# Creating Workflows

Creating a workflow in PoshUI involves defining the UI structure, collecting user input via a wizard, and then defining the tasks to be executed.

## Basic Structure

A typical workflow script follows this pattern:

```powershell
# 1. Import
Import-Module PoshUI.Workflow

# 2. Metadata
New-PoshUIWorkflow -Title 'System Setup' -Description 'Automated system configuration'

# 3. Wizard Phase (Optional but recommended)
Add-UIStep -Name 'Config' -Title 'Configuration' -Order 1
Add-UITextBox -Step 'Config' -Name 'ServerName' -Label 'Server Name' -Mandatory

# 4. Workflow Phase (Defining Tasks)
Add-UIStep -Name 'Execution' -Title 'Execution' -Order 2 -Type Workflow
Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Installing Components' -ScriptBlock {
    $PoshUIWorkflow.WriteOutput("Checking prerequisites...", "INFO")
    Start-Sleep -Seconds 2
    $PoshUIWorkflow.WriteOutput("Installing Windows Features...", "INFO")
    # Your logic here
}

# 5. Execute
Show-PoshUIWorkflow
```

## Initializing the Workflow

The `New-PoshUIWorkflow` cmdlet initializes the workflow context.

| Parameter | Description |
|-----------|-------------|
| `-Title` | The primary title shown in the window. |
| `-Description` | Optional text explaining the workflow's purpose. |
| `-Theme` | Sets the visual style (`Auto`, `Light`, or `Dark`). |
| `-LogPath` | Custom directory for execution logs. |

## The Workflow Context

When `New-PoshUIWorkflow` is called, it initializes a module-scoped variable that stores the definition. Like wizards and dashboards, subsequent `Add-UI*` calls automatically target this context.

## Workflow Execution Flow

1. **User Interaction**: The UI displays the wizard steps defined.
2. **Task Queueing**: After the user clicks "Start", the execution engine takes over.
3. **Serial Execution**: Tasks are executed one by one in the order they were added.
4. **State Persistence**: The workflow state is saved before each task starts, allowing for recovery if the process is interrupted.

## Two-Phase Workflow Pattern

Most workflows follow a two-phase pattern:

### Phase 1: Configuration (Wizard)
User provides input via forms. This data is available to all tasks.

```powershell
# Define what user inputs
Add-UIStep -Name 'Config' -Title 'Configuration' -Order 1
Add-UITextBox -Step 'Config' -Name 'ServerName' -Label 'Server Name' -Mandatory
Add-UIDropdown -Step 'Config' -Name 'Environment' -Label 'Environment' `
    -Choices @('Dev', 'Staging', 'Prod')
```

### Phase 2: Execution (Workflow)
Tasks run sequentially, accessing user inputs and reporting progress.

```powershell
# Define tasks that execute
Add-UIStep -Name 'Execution' -Title 'Execution' -Order 2 -Type Workflow
Add-UIWorkflowTask -Step 'Execution' -Name 'Deploy' -Title 'Deploying...' -ScriptBlock {
    # Can access $ServerName and $Environment from wizard
    $PoshUIWorkflow.WriteOutput("Deploying to $ServerName ($Environment)", "INFO")
}
```

## Key Concepts

### Wizard Inputs
Values collected from the user are automatically available as variables in all tasks. See [Data Passing Patterns](./data-passing.md) for details.

### Progress Reporting
Tasks report progress using either auto-progress (`WriteOutput()`) or manual progress (`UpdateProgress()`). See [Progress Reporting Guide](./progress-reporting.md) for decision tree and examples.

### Reboot & Resume
Workflows can request system reboots and automatically resume after. See [Reboot & Resume](./reboot-resume.md) for implementation details and checklist.

### Task Arguments
Pass task-specific parameters using the `-Arguments` parameter. See [Data Passing Patterns](./data-passing.md) for examples.

## Complete Minimal Example

```powershell
Import-Module PoshUI.Workflow

# Initialize
New-PoshUIWorkflow -Title 'Quick Setup'

# Wizard: Collect configuration
Add-UIStep -Name 'Config' -Title 'Configuration' -Order 1
Add-UITextBox -Step 'Config' -Name 'ComputerName' -Label 'Computer Name' -Mandatory

# Execution: Run tasks
Add-UIStep -Name 'Execution' -Title 'Execution' -Order 2 -Type Workflow

Add-UIWorkflowTask -Step 'Execution' -Name 'Rename' -Title 'Renaming Computer' `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Renaming to $ComputerName...", "INFO")
        Rename-Computer -NewName $ComputerName -Force
        $PoshUIWorkflow.WriteOutput("Rename complete", "INFO")
        $PoshUIWorkflow.RequestReboot("Computer rename requires reboot")
    }

# Show the UI
Show-PoshUIWorkflow
```

## Next Steps

Learn about specific workflow features:

- **[Working with Tasks](./tasks.md)** - Task types, patterns, and error handling
- **[Progress Reporting Guide](./progress-reporting.md)** - WriteOutput() vs UpdateProgress() decision tree
- **[Data Passing Patterns](./data-passing.md)** - Wizard inputs, task arguments, and $PoshUIWorkflow
- **[Reboot & Resume](./reboot-resume.md)** - Implementing multi-phase workflows with reboots
- **[Workflow Logging](./logging.md)** - Logging and CMTrace compatibility
