# System Requirements

PoshUI is designed to run in diverse IT environments, from modern Windows 11 workstations to legacy Windows Server instances.

## Operating System

PoshUI requires a 64-bit (x64) version of Windows:

- **Client**: Windows 10, Windows 11
- **Server**: Windows Server 2016, 2019, 2022
- **WinPE**: Supported (requires .NET Framework 4.8 component)

## Software Requirements

### .NET Framework

- **Requirement**: .NET Framework 4.8 or later
- **Note**: This is pre-installed on Windows 10 (version 1903+) and Windows 11. For older versions of Windows Server, it may need to be installed.

### PowerShell

- **Requirement**: Windows PowerShell 5.1
- **Edition**: Desktop
- **Note**: PoshUI targets the native PowerShell version included with Windows. 

::: info
For PowerShell 7.4+ and .NET 8 environments, use the modern version of PoshUI available at [github.com/asolutionit/PoshUI](https://github.com/asolutionit/PoshUI).
:::

## Hardware Requirements

- **Processor**: 1.6 GHz or faster (x64)
- **Memory**: 4 GB RAM minimum
- **Display**: 1024 x 768 minimum resolution

## Environment Constraints

- **Execution Policy**: Must be set to `RemoteSigned`, `Unrestricted`, or `Bypass` to allow the module to load.
- **Permissions**: Standard user permissions are sufficient for the UI, but administrative privileges may be required depending on the tasks your scripts perform.
