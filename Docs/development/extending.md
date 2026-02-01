# Extending PoshUI

PoshUI is designed to be extensible, allowing developers to add new controls and templates to meet their specific needs. This guide covers how to extend both the PowerShell and C# layers of the framework.

## Adding a New PowerShell Control

To add a new control to a module, follow these steps:

1.  **Create the Cmdlet**: Add a new `.ps1` file in the `Public/Controls` directory of the relevant module (e.g., `PoshUI.Wizard/Public/Controls/Add-UINewControl.ps1`).
2.  **Define Parameters**: Use standard PowerShell parameters for common properties (Step, Name, Label, Mandatory).
3.  **Implement Logic**: Inside the cmdlet, create a new `[UIControl]` object and set its properties.
4.  **Register the Control**: Add the new cmdlet name to the `FunctionsToExport` array in the module's `.psd1` manifest.

```powershell
function Add-UINewControl {
    [CmdletBinding()]
    param($Step, $Name, $Label, $CustomProperty)
    
    $wizardStep = $script:CurrentWizard.GetStep($Step)
    $control = [UIControl]::new($Name, $Label, 'NewControlType')
    $control.SetProperty('CustomProperty', $CustomProperty)
    
    $wizardStep.AddControl($control)
    return $control
}
```

## Extending the C# UI Engine

After adding the PowerShell cmdlet, you must update the C# `Launcher` project to render the new control.

1.  **Create a ViewModel**: Add a new class in `Launcher/ViewModels/Controls/` that inherits from `ParameterViewModel`.
2.  **Create a View**: Add a new XAML `UserControl` in `Launcher/Views/Controls/` to define the visual appearance.
3.  **Register the Type**: Update the `ReflectionService.cs` (for Wizards) or `JsonDefinitionLoader.cs` (for Dashboards) to map the PowerShell control type string to your new ViewModel.
4.  **Data Binding**: Ensure your XAML view correctly binds to the properties in your ViewModel.

## Adding Custom Templates

PoshUI currently supports `Wizard`, `Dashboard`, and `Workflow` templates. To add a new template:

1.  **Hardcode Template Type**: Create a new module where the `UIDefinition` class constructor sets a unique `Template` property.
2.  **Update MainWindow**: In the C# project, modify `MainWindow.xaml` and `MainWindowViewModel.cs` to handle the new template type, likely by adding a new `View` and `ViewModel` for the primary content area.

## Best Practices for Extensions

- **No External Dependencies**: Ensure your new controls do not require any third-party DLLs or NuGet packages.
- **Maintain Consistency**: Follow the existing naming conventions and UI styling (Windows 11 look).
- **Documentation**: Always update the documentation in the `Docs/` folder when adding new capabilities.

Next: [Examples - All Controls](../examples/demo-all-controls.md)
