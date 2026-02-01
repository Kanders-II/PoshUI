# Migration Guide

This guide assists users in migrating their existing scripts from PoshWizard 1.x to PoshUI 2.0.

## Overview of Changes

PoshWizard has been rebranded to **PoshUI**. While the core engine remains focused on providing professional interfaces for PowerShell automation, the architecture has evolved to a more modular and extensible structure.

### Key Changes:
1.  **Rebranding**: All "Wizard" prefixes in cmdlets have been updated to "UI" (e.g., `Add-WizardTextBox` is now `Add-UITextBox`).
2.  **Module Separation**: The single module has been split into three specialized modules: `PoshUI.Wizard`, `PoshUI.Dashboard`, and `PoshUI.Workflow`.
3.  **Namespace Update**: The repository and default branding now reflect "PoshUI".

---

## Backward Compatibility

We understand that you have existing automation. PoshUI 2.0 includes a comprehensive set of **aliases** that ensure your 1.x scripts continue to work without modification.

| 1.x Cmdlet | 2.0 Equivalent | Alias Provided |
|------------|----------------|----------------|
| `New-PoshWizard` | `New-PoshUIWizard` | Yes |
| `Show-PoshWizard`| `Show-PoshUIWizard`| Yes |
| `Add-WizardStep` | `Add-UIStep` | Yes |
| `Add-WizardTextBox`| `Add-UITextBox` | Yes |
| `Add-WizardDropdown`| `Add-UIDropdown`| Yes |
| ... and all others | ... | Yes |

---

## Migration Steps

### 1. Update Module Imports
If your scripts explicitly point to the `.psd1` file, update the path to the new module structure.

**Old (1.x):**
```powershell
Import-Module "C:\Path\To\PoshWizard\PoshWizard.psd1"
```

**New (2.0):**
```powershell
# For standard wizards
Import-Module "C:\Path\To\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1"
```

### 2. Update to "UI" Cmdlets (Optional)
While aliases work, we recommend updating your scripts to the new naming convention for better long-term support and clarity.

```powershell
# Before
Add-WizardTextBox -Name "Server" -Label "Server"

# After
Add-UITextBox -Name "Server" -Label "Server"
```

### 3. Handle ScriptBody Results
In PoshUI 2.0, we have clarified the behavior of `-ScriptBody`. In Live Execution mode, the script runs *before* the wizard returns. You no longer need to check the `$result` variable for data if your script body has already performed the work.

---

## Removed Features

- **`Add-WizardDropdownFromCsv`**: This cmdlet has been removed. Use the standard `Import-Csv` cmdlet and pass the results to the `-Choices` parameter of `Add-UIDropdown` instead.

```powershell
$choices = (Import-Csv "servers.csv").ServerName
Add-UIDropdown -Choices $choices ...
```

## Need Help?

If you encounter issues during your migration, please reach out to us:
- **GitHub Issues**: [Report a bug](https://github.com/asolutionit/PoshUI/issues)
- **Discussions**: [Ask a question](https://github.com/asolutionit/PoshUI/discussions)
