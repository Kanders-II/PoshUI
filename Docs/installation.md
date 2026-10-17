# Installation

PoshUI is a portable module suite. It does not require a traditional installer and can be run from any directory.

## Download

You can download the latest version of PoshUI from the [GitHub Releases](https://github.com/Kanders-II/PoshUI/releases) page.

1. Download `PoshUI.v1.5.1.zip` (or the latest `PoshUI.v<version>.zip`). `bin\PoshUI.exe` inside it is Authenticode-signed.
2. Right-click the ZIP file and select **Properties**.
3. Check the **Unblock** box and click **OK**.
4. Extract it anywhere you like, for example `C:\Tools`. You get one folder, `PoshUI v1.5.1\`, holding `bin\` and the four module folders side by side. Keep them together: the modules find the engine in `bin\`.

## Build from Source

All you need is the [.NET SDK](https://dotnet.microsoft.com/download) (it builds the .NET Framework 4.8 engine).

```powershell
git clone https://github.com/Kanders-II/PoshUI.git
cd PoshUI
dotnet build Launcher\Launcher.csproj -c Release
```

Prefer Visual Studio? Open `WizardFramework.sln`, choose **Release**, and build the solution.

The build writes `PoshUI\bin\PoshUI.exe`, which is where the modules look for it - no copying needed.

## Verification

To verify the installation, try importing one of the modules:

```powershell
# Import the Wizard module (from the extracted folder; in a clone of the repo it's .\PoshUI\PoshUI.Wizard\...)
Import-Module ".\PoshUI v1.5.1\PoshUI.Wizard\PoshUI.Wizard.psd1" -Force

# Check for cmdlets
Get-Command -Module PoshUI.Wizard
```

## Module Paths

To import the modules by name instead of by path, add the extracted folder to `$env:PSModulePath`. Don't copy
the module folders out on their own: they need `bin\PoshUI.exe` beside them.

```powershell
# for this session; add the line to your PowerShell profile to keep it
$env:PSModulePath = "C:\Tools\PoshUI v1.5.1;$env:PSModulePath"

Import-Module PoshUI.Wizard
Import-Module PoshUI.Canvas
```
