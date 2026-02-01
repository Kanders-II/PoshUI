# Handling Results

When a PoshUI Wizard completes, it returns the data collected from the user back to your PowerShell script. Understanding how to handle these results is key to building successful automation.

## Return Value (Simple Mode)

In Simple Mode (without `-ScriptBody`), `Show-PoshUIWizard` returns a **hashtable** containing the values from all input controls.

### Keys and Values
The keys in the hashtable correspond to the `-Name` parameter you provided when adding each control.

```powershell
$result = Show-PoshUIWizard

if ($result) {
    # Access by key name
    $server = $result.ServerName
    $env    = $result.Environment
    
    Write-Host "Proceeding with deployment to $server in $env..."
}
```

## Handling Cancellation

If the user clicks the **Cancel** button or closes the window using the **X**, `Show-PoshUIWizard` returns `$null`. 

::: danger
Always check for `$null` before attempting to access properties on the result object.
:::

```powershell
$result = Show-PoshUIWizard

if ($null -eq $result) {
    Write-Warning "User cancelled. Aborting operation."
    exit
}
```

## Data Types

PoshUI preserves the appropriate PowerShell data types for your results:

| Control Type | PowerShell Return Type |
|--------------|------------------------|
| TextBox, MultiLine | `[string]` |
| Password | `[SecureString]` |
| Checkbox, Toggle | `[bool]` / `[switch]` |
| Numeric | `[double]` |
| Date | `[DateTime]` |
| Dropdown (Single) | `[string]` |
| ListBox (Multi) | `[string[]]` |
| FilePath, FolderPath | `[string]` (Full Path) |

## Results in Live Execution Mode

In Live Execution Mode (using `-ScriptBody`), the behavior is different:

1. **Variables are Injected**: All results are available *during* execution as variables (e.g., `$ServerName`).
2. **Return Value is Status**: The value returned by `Show-PoshUIWizard` indicates if the window was closed normally or cancelled, but since the script has already run, this is usually less important.

::: tip
If you need to pass data from your `ScriptBody` back to the calling script, consider writing it to a file or using global variables (though file-based state is recommended for reliability).
:::

Next: [About Dashboards](../dashboards/about.md)
