# Platform Architecture

PoshUI uses a hybrid architecture that combines the flexibility of PowerShell with the rendering power of the Windows Presentation Foundation (WPF).

## The Hybrid Approach

The framework is split into two distinct layers:

1.  **PowerShell Layer (The Definition)**: A set of native PowerShell modules (`PoshUI.Wizard`, `PoshUI.Dashboard`, `PoshUI.Workflow`) that provide cmdlets for building UI definitions. These modules manage the state and logic of your application.
2.  **C# Layer (The Engine)**: A standalone WPF executable (`PoshUI.exe`) built on .NET Framework 4.8. This engine is responsible for rendering the Windows 11-style UI, handling animations, and providing the execution console.

## Communication Bridge

PoshUI uses a "mixed communication" model to pass data between PowerShell and the WPF engine.

### Dashboard & Workflow (JSON-Based)
The Dashboard and Workflow modules use **JSON serialization**.
- PowerShell converts the `UIDefinition` object into a structured JSON file.
- The C# engine loads this JSON using `JsonDefinitionLoader.cs`.
- This method is faster and supports complex nested objects like chart data and workflow tasks.

### Wizard (AST-Based)
The Wizard module uses **Abstract Syntax Tree (AST) parsing** for maximum backward compatibility.
- PowerShell generates a temporary `.ps1` file containing a `param()` block with custom attributes (e.g., `[WizardTextBox()]`).
- The C# engine parses this script using `ReflectionService.cs`.
- This allows existing parameter-based scripts to be converted to PoshUI wizards with minimal changes.

## Execution Flow

1.  **Import**: You import one of the PoshUI modules.
2.  **Define**: You use cmdlets like `Add-UIStep` and `Add-UITextBox` to build your UI.
3.  **Serialize**: When you call `Show-PoshUI*`, the module serializes the definition to a secure temporary file.
4.  **Launch**: The module launches `PoshUI.exe`, passing the path to the temporary file.
5.  **Render**: The WPF engine parses the file and renders the professional interface.
6.  **Execute**: User interactions (like clicking "Finish") trigger PowerShell execution or return data.
7.  **Return**: The engine writes results to a `.result.json` file, which the module parses and returns to your script.

## Security & Isolation

- **Secure Temp Files**: All communication files are created with cryptographically random names and restrictive ACLs (accessible only by the current user).
- **Process Isolation**: The UI runs in a separate process from your main PowerShell session, ensuring that long-running tasks don't block your terminal.
- **Memory Safety**: Sensitive data like passwords (handled as `SecureString`) are cleared from memory immediately after use.

Next: [Module Structure](./module-structure.md)
