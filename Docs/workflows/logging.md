# Workflow Logging

PoshUI Workflows feature a comprehensive logging system designed for enterprise troubleshooting. By default, every workflow execution generates a detailed log file in a CMTrace-compatible format.

## Log File Location

By default, logs are stored in a `Logs` folder located in the same directory as your PowerShell script.

- **Default Path**: `.\Logs\Workflow_Script_Name_yyyy-MM-dd_HHmmss.log`

You can customize the log directory using the `-LogPath` parameter when calling `New-PoshUIWorkflow`.

```powershell
New-PoshUIWorkflow -Title 'System Setup' -LogPath 'C:\IT\Logs'
```

## Logging Features

### CMTrace Compatibility
The logs use the standard CMTrace format, allowing you to use Microsoft's log viewer to easily filter by component, level, and timestamp.

### Automatic Capture
The following events are automatically logged without any extra code:
- **Task Transitions**: Start and end times for every task.
- **Wizard Data**: A summary of values collected during the wizard phase.
- **Errors**: Full exception details if a task fails.
- **Console Output**: Anything written to the UI console is also captured in the log.

## Writing Custom Log Entries

You can write your own messages to the log file from within a task using the `$PoshUIWorkflow` context object.

```powershell
Add-UIWorkflowTask -Name 'Verify' -Title 'Verifying State' -ScriptBlock {
    # This writes to both the UI console and the log file
    $PoshUIWorkflow.WriteOutput("Checking service status...", "Information")
    
    # You can specify levels: Information, Warning, Error
    $PoshUIWorkflow.WriteOutput("Low disk space detected!", "Warning")
}
```

## Log Continuity during Resumes

When a workflow resumes after a reboot, PoshUI automatically detects the previous log file and continues writing to it. This provides a single, contiguous record of the entire process from start to finish across multiple sessions.

## Log Levels

When writing to the log, use appropriate levels to help with troubleshooting:

```powershell
# Informational - normal operation
$PoshUIWorkflow.WriteOutput("Starting deployment...", "INFO")

# Warning - something unexpected but recoverable
$PoshUIWorkflow.WriteOutput("Service already running, skipping start", "WARN")

# Error - something failed
$PoshUIWorkflow.WriteOutput("Failed to connect to server", "ERROR")
```

## Viewing Logs

Logs are stored in CMTrace format, which can be viewed with:
- **CMTrace.exe** - Microsoft's log viewer (included with SCCM/MECM)
- **Trace32.exe** - Alternative log viewer
- **Any text editor** - Logs are plain text

The CMTrace format makes it easy to filter by:
- Component
- Level (Info, Warning, Error)
- Timestamp
- Thread ID

## Best Practices for Logging

1. **Log important state changes** - When tasks start/complete
2. **Log errors with context** - Include what was being done when error occurred
3. **Use appropriate levels** - INFO for normal, WARN for issues, ERROR for failures
4. **Be concise** - Keep messages short but descriptive
5. **Include values** - Log relevant variable values for debugging

### Example: Good Logging

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Deploy' -Title 'Deploying Application' `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Starting deployment to $ServerName", "INFO")
        
        try {
            $result = Invoke-Deployment -Server $ServerName
            $PoshUIWorkflow.WriteOutput("Deployment succeeded: $($result.Status)", "INFO")
        }
        catch {
            $PoshUIWorkflow.WriteOutput("Deployment failed: $($_.Exception.Message)", "ERROR")
            throw
        }
    }
```

Next: [Working with Tasks](./tasks.md) | [Reboot & Resume](./reboot-resume.md)
