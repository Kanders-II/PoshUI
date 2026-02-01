# Creating Wizards

Building a wizard in PoshUI involves defining its metadata, adding navigation steps, and populating those steps with controls.

## Basic Structure

A minimal wizard script follows this structure:

```powershell
# 1. Import
Import-Module PoshUI.Wizard

# 2. Metadata
New-PoshUIWizard -Title 'Deployment Wizard' -Description 'Set up your environment'

# 3. Steps
Add-UIStep -Name 'Config' -Title 'Configuration' -Order 1

# 4. Controls
Add-UITextBox -Step 'Config' -Name 'ServerName' -Label 'Server Name'

# 5. Show
$result = Show-PoshUIWizard
```

## Initializing the Wizard

The `New-PoshUIWizard` cmdlet creates the context for your wizard. It should be called only once at the beginning of your script.

| Parameter | Description |
|-----------|-------------|
| `-Title` | The primary title shown in the window and sidebar. |
| `-Description` | Optional text explaining the purpose of the wizard. |
| `-Theme` | Sets the visual style (`Auto`, `Light`, or `Dark`). |
| `-Icon` | Path to an icon file or a Segoe MDL2 glyph. |

## Working with the Current Wizard

When you call `New-PoshUIWizard`, it stores the definition in a module-scoped variable `$script:CurrentWizard`. All subsequent `Add-UI*` calls automatically target this instance.

::: tip
If you need to manage multiple wizard definitions in the same session, you can capture the object returned by `New-PoshUIWizard` and pass it to other cmdlets, although this is rarely needed for standard scripts.
:::

## Best Practices

1. **Keep it Simple**: Don't overload a single step with too many controls. Use multiple steps for better flow.
2. **Use Descriptive Names**: Use clear, alphanumeric names for steps and controls.
3. **Handle Results**: Always check if `$result` is null (meaning the user cancelled) before proceeding with your automation logic.

Next: [Working with Steps](./steps.md)
