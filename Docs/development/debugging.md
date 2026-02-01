# Debugging PoshUI

Debugging a hybrid application like PoshUI requires looking at both the PowerShell modules and the C# execution engine. This guide provides strategies for identifying and resolving issues in both layers.

## Debugging PowerShell Modules

### Verbose Output
Most PoshUI cmdlets support the `-Verbose` switch. This is the first step in troubleshooting module-level logic, such as serialization or state management.

```powershell
Show-PoshUIWizard -Verbose
```

### Script-Scoped State
You can inspect the current state of the UI definition by looking at the script-scoped variables in the module:
- **Wizard**: `$script:CurrentWizard`
- **Workflow**: `$script:CurrentWorkflow`

Since these are script-scoped, you may need to use `Get-Variable` with the module's scope if you are debugging from an external terminal.

---

## Debugging the C# Engine (PoshUI.exe)

### AppDebug Mode
When calling `Show-PoshUIWizard` or `Show-PoshUIWorkflow`, you can use the `-AppDebug` switch. This tells the C# engine to enable internal debugging features, such as more detailed error dialogs and logging.

```powershell
Show-PoshUIWizard -AppDebug
```

### Visual Studio Debugging
If you have the source code, you can debug the UI engine directly:
1. Open `UIFramework.sln` in Visual Studio.
2. Set the `Launcher` project as the StartUp project.
3. In the project properties, go to the **Debug** tab.
4. Set **Application arguments** to point to a valid PoshUI temporary file (e.g., `C:\Users\...\AppData\Local\Temp\PoshUI\debug.json`).
5. Press **F5** to start debugging.

---

## Analyzing Logs

PoshUI's CMTrace-compatible logs are the most valuable resource for troubleshooting in production.

1.  Navigate to the `Logs` folder relative to your script.
2.  Open the most recent `.log` file using **CMTrace.exe**.
3.  Look for entries highlighted in **Red (Error)** or **Yellow (Warning)**.

Common log indicators:
- **Serialization Error**: Indicates a problem converting your PowerShell definition to JSON.
- **AST Parsing Error**: Indicates a syntax issue in your `param()` block or custom attributes.
- **WPF Binding Error**: Usually indicates a mismatch between a control's `Name` and its internal ViewModel property.

---

## Common Issues & Solutions

### UI Fails to Launch
- **Check**: Is `PoshUI.exe` blocked by Windows? Right-click the file and select "Unblock".
- **Check**: Is the `.NET Framework 4.8` installed?
- **Solution**: Run your script with `-Verbose` to see exactly which path the module is using to launch the EXE.

### Dynamic Dropdown is Empty
- **Check**: Does the `-ScriptBlock` return an array of strings?
- **Check**: Did you include the `-DependsOn` parameter?
- **Solution**: Test your script block independently in a standard PowerShell terminal to ensure it returns the expected data.

### Password Validation Fails
- **Check**: Are you using `-ValidationPattern`?
- **Check**: Does the regex work against the plaintext version of your test password?
- **Solution**: Remember that password validation happens inside the C# engine for security.

Next: [Contributing](./contributing.md)
