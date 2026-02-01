# Data Passing Patterns

Workflow tasks need access to data from multiple sources: user inputs, task-specific parameters, and the workflow context. This guide explains the three data-passing mechanisms and when to use each.

## Quick Decision Tree

```
Where does the data come from?
│
├─ User filled it in the wizard form?
│   └─ Use Wizard Inputs (automatic)
│
├─ Task-specific parameter?
│   └─ Use Task Arguments (-Arguments parameter)
│
└─ Progress/reboot/context data?
    └─ Use $PoshUIWorkflow object (always available)
```

## Mechanism 1: Wizard Inputs

Data collected from the user in the wizard phase. Automatically available to all tasks.

### How It Works

1. Define input controls in the wizard step
2. User fills them in
3. Values automatically become variables in all tasks

### Example

```powershell
# WIZARD PHASE: Define what user inputs
Add-UIStep -Name 'Config' -Title 'Configuration' -Order 1
Add-UITextBox -Step 'Config' -Name 'ServerName' -Label 'Server Name' -Mandatory
Add-UITextBox -Step 'Config' -Name 'AdminUser' -Label 'Admin Username' -Mandatory
Add-UIDropdown -Step 'Config' -Name 'Environment' -Label 'Environment' `
    -Choices @('Development', 'Staging', 'Production')

# WORKFLOW PHASE: Access the inputs
Add-UIStep -Name 'Execution' -Title 'Execution' -Order 2 -Type Workflow

# Task 1: Can access $ServerName, $AdminUser, $Environment
Add-UIWorkflowTask -Step 'Execution' -Name 'Validate' -Title 'Validating Configuration' `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Server: $ServerName", "INFO")
        $PoshUIWorkflow.WriteOutput("Admin: $AdminUser", "INFO")
        $PoshUIWorkflow.WriteOutput("Environment: $Environment", "INFO")
    }

# Task 2: Also can access $ServerName, $AdminUser, $Environment
Add-UIWorkflowTask -Step 'Execution' -Name 'Deploy' -Title 'Deploying' `
    -ScriptBlock {
        if ($Environment -eq 'Production') {
            $PoshUIWorkflow.WriteOutput("Deploying to PRODUCTION", "WARN")
        } else {
            $PoshUIWorkflow.WriteOutput("Deploying to $Environment", "INFO")
        }
    }
```

### Scope

- **Available to**: All tasks
- **Scope**: Read-only (don't modify)
- **Persistence**: Saved across reboots

### When to Use

- ✅ Configuration data from user
- ✅ Data needed by multiple tasks
- ✅ Values that should persist across reboots
- ✅ User-provided parameters

### Supported Control Types

All input controls create wizard inputs:
- `Add-UITextBox` → `$VariableName`
- `Add-UIPassword` → `$VariableName`
- `Add-UICheckbox` → `$VariableName` (boolean)
- `Add-UIDropdown` → `$VariableName`
- `Add-UIMultiSelect` → `$VariableName` (array)
- `Add-UIDatePicker` → `$VariableName`
- `Add-UIFilePicker` → `$VariableName`

## Mechanism 2: Task Arguments

Task-specific parameters passed only to that task via the `-Arguments` parameter.

### How It Works

1. Define parameters in the task's script block
2. Pass values via `-Arguments` hashtable
3. Parameters available only in that task

### Example

```powershell
# Task 1: With specific arguments
Add-UIWorkflowTask -Step 'Execution' -Name 'Backup' -Title 'Creating Backup' `
    -ScriptBlock {
        param($BackupPath, $RetentionDays, $Compress)
        
        $PoshUIWorkflow.WriteOutput("Backup path: $BackupPath", "INFO")
        $PoshUIWorkflow.WriteOutput("Retention: $RetentionDays days", "INFO")
        $PoshUIWorkflow.WriteOutput("Compress: $Compress", "INFO")
    } `
    -Arguments @{
        BackupPath = "C:\Backups"
        RetentionDays = 30
        Compress = $true
    }

# Task 2: Different arguments
Add-UIWorkflowTask -Step 'Execution' -Name 'Cleanup' -Title 'Cleaning Up' `
    -ScriptBlock {
        param($LogPath, $MaxLogSize)
        
        $PoshUIWorkflow.WriteOutput("Log path: $LogPath", "INFO")
        $PoshUIWorkflow.WriteOutput("Max size: $MaxLogSize MB", "INFO")
    } `
    -Arguments @{
        LogPath = "C:\Logs"
        MaxLogSize = 100
    }
```

### Scope

- **Available to**: Only the task that defines them
- **Scope**: Read-only
- **Persistence**: Not saved (task-specific only)

### When to Use

- ✅ Task-specific configuration
- ✅ Constants that don't change between tasks
- ✅ Parameters that shouldn't be exposed to user
- ✅ Different values for similar tasks

### Parameter Types

Arguments can be any PowerShell type:

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Task' -ScriptBlock {
    param($StringParam, $IntParam, $BoolParam, $ArrayParam, $HashParam)
    
    Write-Host "String: $StringParam"
    Write-Host "Int: $IntParam"
    Write-Host "Bool: $BoolParam"
    Write-Host "Array: $($ArrayParam -join ', ')"
    Write-Host "Hash keys: $($HashParam.Keys -join ', ')"
} `
-Arguments @{
    StringParam = "Hello"
    IntParam = 42
    BoolParam = $true
    ArrayParam = @("Item1", "Item2", "Item3")
    HashParam = @{
        Key1 = "Value1"
        Key2 = "Value2"
    }
}
```

## Mechanism 3: Shared Workflow Data Store

Pass data between tasks using the workflow data store. Perfect for sharing computed values or results from earlier tasks.

### How It Works

Use `$PoshUIWorkflow.SetData()` to store values and `$PoshUIWorkflow.GetData()` to retrieve them.

### Example

```powershell
# Task 1: Store data for later tasks
Add-UIWorkflowTask -Step 'Execution' -Name 'Install' -Title 'Installing Application' `
    -ScriptBlock {
        $installPath = "C:\Program Files\MyApp"
        Install-Application -Path $installPath

        # Store data for subsequent tasks
        $PoshUIWorkflow.SetData('InstallPath', $installPath)
        $PoshUIWorkflow.SetData('InstalledVersion', '2.0.0')
        $PoshUIWorkflow.SetData('InstallTime', (Get-Date))
    }

# Task 2: Retrieve data from previous task
Add-UIWorkflowTask -Step 'Execution' -Name 'Configure' -Title 'Configuring Application' `
    -ScriptBlock {
        $installPath = $PoshUIWorkflow.GetData('InstallPath')
        $version = $PoshUIWorkflow.GetData('InstalledVersion')

        $PoshUIWorkflow.WriteOutput("Configuring v$version at $installPath", "INFO")
        Configure-Application -Path $installPath
    }

# Task 3: Check if data exists
Add-UIWorkflowTask -Step 'Execution' -Name 'Verify' -Title 'Verifying Installation' `
    -ScriptBlock {
        if ($PoshUIWorkflow.HasData('InstallPath')) {
            $path = $PoshUIWorkflow.GetData('InstallPath')
            if (Test-Path $path) {
                $PoshUIWorkflow.WriteOutput("Verified: $path exists", "INFO")
            }
        }
    }
```

### Scope

- **Available to**: All subsequent tasks
- **Scope**: Read/write
- **Persistence**: Saved across reboots with workflow state

### When to Use

- ✅ Pass computed values between tasks
- ✅ Share installation paths, version info, or results
- ✅ Store status flags for conditional task execution
- ✅ Build up configuration data across tasks

### Available Methods

| Method | Description |
|--------|-------------|
| `SetData(key, value)` | Store a value under the given key |
| `GetData(key)` | Retrieve a value by key (returns null if not found) |
| `HasData(key)` | Check if a key exists in the data store |
| `GetDataKeys()` | Get all stored keys |

### Pattern: Using Data with Skip Conditions

```powershell
# Task 1: Detect existing installation
Add-UIWorkflowTask -Step 'Execution' -Name 'Check' -Title 'Check Existing Installation' `
    -ScriptBlock {
        $existingPath = "C:\Program Files\MyApp"
        if (Test-Path $existingPath) {
            $PoshUIWorkflow.SetData('AlreadyInstalled', $true)
            $PoshUIWorkflow.SetData('ExistingPath', $existingPath)
            $PoshUIWorkflow.WriteOutput("Existing installation found at $existingPath", "INFO")
        } else {
            $PoshUIWorkflow.SetData('AlreadyInstalled', $false)
        }
    }

# Task 2: Skip if already installed (uses SkipCondition)
Add-UIWorkflowTask -Step 'Execution' -Name 'Install' -Title 'Install Application' `
    -SkipCondition '$WorkflowData["AlreadyInstalled"] -eq $true' `
    -SkipReason 'Application already installed' `
    -ScriptBlock {
        Install-Application
    }
```

---

## Mechanism 4: $PoshUIWorkflow Context

The workflow context object. Always available, provides methods for progress, output, data sharing, and reboot control.

### How It Works

The `$PoshUIWorkflow` object is automatically injected into every task's scope.

### Available Methods

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Task' -ScriptBlock {
    # Progress and output
    $PoshUIWorkflow.WriteOutput("Message", "Level")
    $PoshUIWorkflow.UpdateProgress(Percent, "Message")
    $PoshUIWorkflow.SetStatus("Message")

    # Data sharing between tasks
    $PoshUIWorkflow.SetData('key', 'value')
    $value = $PoshUIWorkflow.GetData('key')
    $exists = $PoshUIWorkflow.HasData('key')
    $allKeys = $PoshUIWorkflow.GetDataKeys()

    # Task control
    $PoshUIWorkflow.SkipTask("Reason to skip")
    $PoshUIWorkflow.RequestReboot("Reason for reboot")

    # Task info
    $currentIndex = $PoshUIWorkflow.CurrentTaskIndex
    $totalTasks = $PoshUIWorkflow.TotalTaskCount

    # Access wizard data
    $value = $PoshUIWorkflow.GetWizardValue("ParameterName")
}
```

### Scope

- **Available to**: All tasks (always)
- **Scope**: Context methods are framework-managed
- **Persistence**: Data store is saved across reboots

### When to Use

- ✅ Report progress
- ✅ Write output/logging
- ✅ Share data between tasks
- ✅ Request system reboot
- ✅ Skip current task programmatically
- ✅ Access wizard values programmatically

## Comparison Table

| Aspect | Wizard Inputs | Task Arguments | Shared Data Store | $PoshUIWorkflow |
|--------|---------------|----------------|-------------------|-----------------|
| **Scope** | All tasks | Single task | All subsequent tasks | All tasks |
| **Source** | User input | Code definition | Previous tasks | Framework |
| **Persistence** | Across reboots | Task-specific | Across reboots | Framework managed |
| **Mutability** | Read-only | Read-only | Read/write | Methods only |
| **Use case** | Configuration | Task parameters | Task results | Progress/control |
| **Example** | ServerName | BackupPath | InstallPath | WriteOutput() |

## Real-World Example: Complete Workflow

```powershell
Import-Module PoshUI.Workflow

New-PoshUIWorkflow -Title 'Server Deployment'

# WIZARD PHASE: Collect configuration
Add-UIStep -Name 'Config' -Title 'Configuration' -Order 1
Add-UITextBox -Step 'Config' -Name 'ServerName' -Label 'Server Name' -Mandatory
Add-UITextBox -Step 'Config' -Name 'IPAddress' -Label 'IP Address' -Mandatory
Add-UIDropdown -Step 'Config' -Name 'Role' -Label 'Server Role' `
    -Choices @('WebServer', 'Database', 'FileServer')

# WORKFLOW PHASE: Execute tasks
Add-UIStep -Name 'Execution' -Title 'Deployment' -Order 2 -Type Workflow

# Task 1: Validate (uses wizard inputs)
Add-UIWorkflowTask -Step 'Execution' -Name 'Validate' -Title 'Validating Configuration' `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Validating server: $ServerName", "INFO")
        $PoshUIWorkflow.WriteOutput("IP Address: $IPAddress", "INFO")
        $PoshUIWorkflow.WriteOutput("Role: $Role", "INFO")
        
        if (-not (Test-Connection -ComputerName $ServerName -Count 1 -Quiet)) {
            throw "Cannot reach server $ServerName"
        }
        
        $PoshUIWorkflow.WriteOutput("Server is reachable", "INFO")
    }

# Task 2: Configure (uses wizard inputs + task arguments)
Add-UIWorkflowTask -Step 'Execution' -Name 'Configure' -Title 'Configuring Server' `
    -ScriptBlock {
        param($ConfigPath, $TimeoutSeconds)
        
        $PoshUIWorkflow.UpdateProgress(10, "Starting configuration...")
        
        # Use wizard input
        $PoshUIWorkflow.UpdateProgress(30, "Configuring $Role role...")
        
        # Use task argument
        $config = Get-Content -Path $ConfigPath
        
        $PoshUIWorkflow.UpdateProgress(70, "Applying settings...")
        Start-Sleep -Seconds 2
        
        $PoshUIWorkflow.UpdateProgress(100, "Configuration complete")
    } `
    -Arguments @{
        ConfigPath = "C:\Config\server-config.json"
        TimeoutSeconds = 300
    }

# Task 3: Deploy (uses wizard inputs + $PoshUIWorkflow)
Add-UIWorkflowTask -Step 'Execution' -Name 'Deploy' -Title 'Deploying Application' `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Deploying to $ServerName ($Role)...", "INFO")
        
        # Simulate deployment
        $PoshUIWorkflow.WriteOutput("Copying files...", "INFO")
        Start-Sleep -Seconds 2
        
        $PoshUIWorkflow.WriteOutput("Installing services...", "INFO")
        Start-Sleep -Seconds 2
        
        $PoshUIWorkflow.WriteOutput("Deployment complete", "INFO")
    }

# Task 4: Verify (uses wizard inputs + reboot)
Add-UIWorkflowTask -Step 'Execution' -Name 'Verify' -Title 'Verifying Deployment' `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Verifying $ServerName...", "INFO")
        
        # Check if reboot is needed
        if ($Role -eq 'WebServer') {
            $PoshUIWorkflow.WriteOutput("Web server role requires reboot", "WARN")
            $PoshUIWorkflow.RequestReboot("Web server configuration requires system restart")
        } else {
            $PoshUIWorkflow.WriteOutput("Verification complete", "INFO")
        }
    }

Show-PoshUIWorkflow
```

## Best Practices

1. **Use wizard inputs for user configuration** - Let users provide values, don't hardcode
2. **Use task arguments for constants** - Task-specific values that don't change
3. **Use $PoshUIWorkflow for control** - Progress, output, reboot requests
4. **Keep arguments simple** - Use hashtables with clear key names
5. **Document parameter expectations** - Add comments explaining what each parameter does
6. **Validate inputs** - Check wizard inputs early in first task
7. **Don't modify variables** - Treat all inputs as read-only

## Troubleshooting

### Variable not found in task
- Check that wizard input name matches exactly (case-sensitive)
- Verify task arguments are defined in `-Arguments` parameter
- Ensure you're using `param()` block for task arguments

### Value is null or empty
- Wizard input: Check that user actually filled in the field
- Task argument: Verify the value was passed in `-Arguments`
- $PoshUIWorkflow: It's always available, check method names

### Can't access wizard value in second task
- Wizard inputs are available to all tasks
- Check the variable name matches the control name
- Verify the control was added to the wizard step

Next: [Progress Reporting Guide](./progress-reporting.md) | [Working with Tasks](./tasks.md)
