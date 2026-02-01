# Installation

PoshUI is a portable module suite. It does not require a traditional installer and can be run from any directory.

## Download

You can download the latest version of PoshUI from the [GitHub Releases](https://github.com/asolutionit/PoshUI/releases) page.

1. Download the `PoshUI.zip` file.
2. Right-click the ZIP file and select **Properties**.
3. Check the **Unblock** box and click **OK**.
4. Extract the contents to your desired location (e.g., `C:\Program Files\WindowsPowerShell\Modules\PoshUI`).

## Build from Source

If you prefer to build PoshUI yourself, you can do so using Visual Studio or the MSBuild command line.

### Prerequisites

- [Visual Studio 2019 or later](https://visualstudio.microsoft.com/)
- [.NET Framework 4.8 Target Pack](https://dotnet.microsoft.com/download/dotnet-framework/net48)

### Build Steps

1. Clone the repository:
   ```powershell
   git clone https://github.com/asolutionit/PoshUI.git
   ```
2. Open `UIFramework.sln` in Visual Studio.
3. Set the build configuration to **Release**.
4. Build the solution (**Build > Build Solution**).

The compiled files will be located in the `bin/Release` folder.

## Verification

To verify the installation, try importing one of the modules:

```powershell
# Import the Wizard module
Import-Module .\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1 -Force

# Check for cmdlets
Get-Command -Module PoshUI.Wizard
```

## Module Paths

For the best experience, you can copy the `PoshUI` folder (containing the sub-modules) to one of your `$env:PSModulePath` locations:

- `C:\Users\<User>\Documents\WindowsPowerShell\Modules`
- `C:\Program Files\WindowsPowerShell\Modules`

This allows you to import the modules by name:

```powershell
Import-Module PoshUI.Wizard
Import-Module PoshUI.Dashboard
```
