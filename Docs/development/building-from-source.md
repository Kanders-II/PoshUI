# Building from Source

PoshUI is built using C# for the execution engine and PowerShell for the modules. This guide will walk you through the process of setting up your development environment and building the project from the source code.

## Prerequisites

To build PoshUI, you will need the following tools installed on your system:

- **Visual Studio 2019 or later**: The Community edition is sufficient. Ensure you select the **.NET desktop development** workload during installation.
- **.NET Framework 4.8 SDK**: Usually included with Visual Studio, but can be downloaded separately if needed.
- **PowerShell 5.1**: Pre-installed on Windows 10 and 11.

## Repository Structure

- `Launcher/`: Contains the C# source code for the WPF application (`PoshUI.exe`).
- `PoshUI/`: Contains the PowerShell modules and their class definitions.
- `WizardFramework.sln`: The primary Visual Studio solution file.

## Build Steps

### 1. Clone the Repository
Open your terminal and clone the source code:
```powershell
git clone https://github.com/Kanders-II/PoshUI.git
cd PoshUI
```

### 2. Open the Solution
Open `WizardFramework.sln` in Visual Studio.

### 3. Restore Dependencies
Visual Studio should automatically restore any internal project references. Since PoshUI has **zero third-party dependencies**, there are no NuGet packages to download.

### 4. Configure Build
Set the build configuration to **Release** and the platform to **Any CPU** (or **x64**) using the toolbars at the top of Visual Studio.

### 5. Build Solution
Go to **Build > Build Solution** (or press `Ctrl+Shift+B`).

## Output Location

Both configurations write straight to the folder the modules already look in:

`.\PoshUI\bin\PoshUI.exe`

Everything else the build produces (satellite assemblies, the `.pdb`, `.exe.config`) is moved into
`.\PoshUI\bin\bin\` by the `RearrangeOutput` target, so the module directory stays readable. There is nothing
to copy afterwards - import a module and it finds the engine you just built.

## Building Without Visual Studio

The project is SDK-style and has no third-party packages, so the .NET SDK alone is enough:

```powershell
dotnet build Launcher\Launcher.csproj -c Release
```

Next: [Debugging](./debugging.md)
