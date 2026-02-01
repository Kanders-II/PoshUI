# Module Structure

PoshUI follows a standardized directory and file structure across all its modules. This organization ensures maintainability and allows the shared executable to interact with each module consistently.

## Directory Layout

Each module (e.g., `PoshUI.Wizard`) is organized as follows:

```text
PoshUI.<ModuleName>/
├── PoshUI.<ModuleName>.psd1    # Module manifest
├── PoshUI.<ModuleName>.psm1    # Module script loader
├── Classes/                    # PowerShell class definitions
│   └── UIDefinition.ps1        # Core UI object for the module
├── Public/                     # Exported functions (Cmdlets)
│   ├── Core/                   # New-*, Show-* functions
│   ├── Steps/                  # Add-UIStep function
│   └── Controls/               # Add-UI* control functions
├── Private/                    # Internal helper functions
│   ├── Security/               # New-SecureTempFile, etc.
│   ├── StateManagement/        # Initialize-UIContext, etc.
│   └── Serialize-UIDefinition.ps1
└── Examples/                   # Module-specific demonstration scripts
```

## Component Roles

### 1. Module Manifest (.psd1)
Defines the module version, dependencies, and exports. All PoshUI modules target PowerShell 5.1 and .NET Framework 4.8.

### 2. Classes/
Contains the `UIDefinition` and `UIStep` class extensions specific to that module. For example, the Wizard module's `UIDefinition` class is hardcoded with `Template = 'Wizard'`.

### 3. Public/
This directory contains the user-facing cmdlets. They are organized into logical subfolders:
- **Core**: The primary functions used to start and end a UI session.
- **Steps**: Functions for adding navigation pages.
- **Controls**: The specialized input and display components.

### 4. Private/
Contains functions that are used internally by the module but not exposed to the user. This includes the serialization logic and security frameworks.

## State Management

Each module maintains its own isolated state using a script-scoped variable:
- **Wizard**: `$script:CurrentWizard`
- **Dashboard**: `$script:CurrentDashboard` (aliased to `$script:CurrentWizard` internally for engine compatibility)
- **Workflow**: `$script:CurrentWorkflow`

This isolation ensures that you can work with multiple module types in the same PowerShell session without data leakage or conflicts.

## Adding New Functions

If you are contributing to PoshUI, always place new cmdlets in the `Public/Controls` directory and ensure they follow the `Add-UI<Name>` naming convention.

Next: [Logging](./logging.md)
