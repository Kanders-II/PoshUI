# Script Cards

Script Cards turn your PowerShell scripts into clickable tools that anyone can use—no command line knowledge required.

![ScriptCard Dark Theme](../images/visualization/ScriptCard_Dark_2.png)

## Why Script Cards?

Script Cards bridge the gap between IT professionals who write PowerShell and end users or colleagues who need to run those scripts. Instead of teaching someone to open PowerShell, navigate to a folder, and type commands with the right parameters, you give them a button.

**Common use cases:**
- **IT Help Desk** - Give support staff tools to reset passwords, check system status, or restart services without elevated PowerShell access
- **Team Tooling** - Share your automation scripts with colleagues who prefer a GUI
- **Self-Service Portals** - Let users run approved operations (disk cleanup, printer setup, VPN troubleshooting) without submitting tickets
- **Training Environments** - Provide guided tools for new team members learning your infrastructure

When a user clicks a Script Card:
1. A dialog opens with input fields for each parameter (automatically generated from your script)
2. The user fills in values using familiar controls (text boxes, dropdowns, file pickers)
3. They click "Run" and see real-time output in an embedded console
4. No PowerShell window, no syntax to remember, no mistakes

## Adding Script Cards

Use the `Add-UIScriptCard` cmdlet to add an executable card. You can either point to an existing `.ps1` file or provide an inline script block.

```powershell
Add-UIScriptCard -Step 'Tools' -Name 'DiskCleanup' `
    -Title 'Run Disk Cleanup' `
    -Description 'Clears temporary files from the system drive' `
    -Icon '&#xE74D;' `
    -ScriptBlock {
        param([switch]$IncludeRecycleBin)
        Write-Host "Starting cleanup..."
        # Logic here
        Write-Host "Cleanup complete." -ForegroundColor Green
    }
```

## Automatic Parameter Discovery

One of the most powerful features of Script Cards is **automatic parameter discovery**. PoshUI uses AST parsing to examine your script's `param()` block and dynamically generate UI controls for each parameter.

Supported parameter types include:
- `[string]`: Renders a text box.
- `[bool]` / `[switch]`: Renders a toggle switch.
- `[int]` / `[double]`: Renders a numeric spinner.
- `[DateTime]`: Renders a date picker.
- `[ValidateSet()]`: Renders a dropdown menu.
- `[WizardFilePath()]`: Renders a text box with a file browse button.
- `[WizardFolderPath()]`: Renders a text box with a folder browse button.

## File and Folder Pickers

Use the `[WizardFilePath()]` and `[WizardFolderPath()]` attributes to add browse buttons:

```powershell
Add-UIScriptCard -Step 'Tools' -Name 'BackupConfig' `
    -Title 'Backup Configuration' `
    -ScriptBlock {
        param(
            [Parameter(Mandatory)]
            [WizardFilePath()]
            [string]$ConfigFile,

            [Parameter(Mandatory)]
            [WizardFolderPath()]
            [string]$BackupFolder
        )
        Copy-Item $ConfigFile -Destination $BackupFolder
        Write-Host "Backed up $ConfigFile to $BackupFolder" -ForegroundColor Green
    }
```

## Isolated Execution

Each Script Card runs in its own isolated PowerShell runspace. This ensures that:
- Long-running scripts don't freeze the dashboard UI.
- Variables and state from one script don't leak into others.
- The user can see the output in a dedicated real-time execution console.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `-Step` | Name of the dashboard step. |
| `-Name` | Unique identifier for the card. |
| `-Title` | Header text for the card. |
| `-ScriptPath` | Path to a `.ps1` file to execute. |
| `-ScriptBlock` | Inline PowerShell code to execute. |
| `-DefaultParameters` | Hashtable of default values for script parameters. |
| `-Category` | Grouping name for the dashboard filter. |

Next: [Live Refresh](./refresh.md)
