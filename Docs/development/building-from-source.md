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
- `Examples/`: contains sample scripts for testing and demonstration.
- `UIFramework.sln`: The primary Visual Studio solution file.

## Build Steps

### 1. Clone the Repository
Open your terminal and clone the source code:
```powershell
git clone https://github.com/asolutionit/PoshUI.git
cd PoshUI
```

### 2. Open the Solution
Open `UIFramework.sln` in Visual Studio.

### 3. Restore Dependencies
Visual Studio should automatically restore any internal project references. Since PoshUI has **zero third-party dependencies**, there are no NuGet packages to download.

### 4. Configure Build
Set the build configuration to **Release** and the platform to **Any CPU** (or **x64**) using the toolbars at the top of Visual Studio.

### 5. Build Solution
Go to **Build > Build Solution** (or press `Ctrl+Shift+B`).

## Output Location

Once the build is complete, the compiled executable and associated files will be located in:
`.\Launcher\bin\Release\PoshUI.exe`

## Post-Build Setup

To test your build, you need to ensure the PowerShell modules can find the new executable. The modules are configured to look for the EXE in a `bin` folder relative to the module root.

1. Create a `bin` folder inside the `PoshUI` module directory if it doesn't exist.
2. Copy the compiled `PoshUI.exe` into that `bin` folder.

```powershell
New-Item -ItemType Directory -Path ".\PoshUI\bin" -Force
Copy-Item ".\Launcher\bin\Release\PoshUI.exe" ".\PoshUI\bin\"
```

Next: [Debugging](./debugging.md)
