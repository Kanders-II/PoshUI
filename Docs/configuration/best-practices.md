# Best Practices

Following these best practices will help you create PoshUI interfaces that are professional, reliable, and user-friendly.

## General Principles

### 1. Module-Specific Focus
Load only the module you need for your task.
- Use **Wizard** for guided data collection.
- Use **Dashboard** for monitoring and visualization.
- Use **Workflow** for sequential automation tasks.

### 2. Hashtable Splatting
For cmdlets with many parameters (like `New-PoshUIWizard` or `Add-UITextBox`), use hashtable splatting for cleaner, more readable code.

```powershell
$textBoxParams = @{
    Step = 'Config'
    Name = 'ServerName'
    Label = 'Target Server'
    Placeholder = 'SRV-01'
    Mandatory = $true
}
Add-UITextBox @textBoxParams
```

---

## Wizard Best Practices

### 1. Handle Cancellation
Always check if the result of `Show-PoshUIWizard` is `$null` before proceeding with your logic.

```powershell
$result = Show-PoshUIWizard
if ($null -eq $result) { return }
```

### 2. Group Logically
Organize related fields into steps. A wizard with 3 steps of 4 fields each is better than 1 step with 12 fields.

### 3. Use Descriptions
Provide helpful subtitle text for steps and `HelpText` for complex controls to guide the user.

---

## Dashboard Best Practices

### 1. Optimize Refresh Intervals
Don't set refresh intervals shorter than necessary. Querying system metrics every 1 second can consume significant resources. A 5-10 second interval is usually sufficient for most KPIs.

### 2. Use Categories
Group your cards using the `-Category` parameter to enable the built-in filtering feature, especially in dense dashboards.

### 3. Asynchronous Refresh
Ensure your refresh scripts are efficient. While they run in background runspaces, very slow scripts can lead to delayed UI updates.

---

## Workflow Best Practices

### 1. Small, Atomic Tasks
Break your workflow into several small tasks rather than one giant task. This provides better progress feedback and allows for more granular resumption after reboots.

### 2. Leverage Auto-Progress
Use `$PoshUIWorkflow.WriteOutput()` liberally. It not only updates the console and log but also automatically increments the task's progress bar.

### 3. Robust Error Handling
Set `-OnError 'Stop'` for critical tasks that must succeed before the next one starts. Use `'Continue'` only for optional maintenance steps.

---

## Security Best Practices

### 1. SecureStrings for Passwords
Always use `Add-UIPassword` for sensitive input. It returns a `SecureString` which is encrypted in memory.

### 2. Restricted Temp Files
PoshUI handles temp file security automatically, but you should ensure your scripts are running from directories with appropriate NTFS permissions.

### 3. Least Privilege
Run your PoshUI scripts with the minimum permissions required for the tasks being performed.

Next: [Building from Source](../development/building-from-source.md)
