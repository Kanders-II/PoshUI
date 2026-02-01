# Hyper-V VM Creation Demo

The `Wizard-HyperV-CreateVM.ps1` example demonstrates a real-world automation scenario: collecting complex configuration data to provision a new virtual machine.

## Overview

This wizard showcases how to build a production-ready interface for infrastructure tasks. It includes:
- **Prerequisite Checks**: Ensuring the script is running with Administrator privileges.
- **Validation**: Enforcing naming conventions and resource limits.
- **Dynamic Logic**: While this specific example is relatively static, it sets the stage for dynamic resource selection.

## Running the Example

```powershell
# Navigate to the Wizard examples directory
cd .\PoshUI\PoshUI.Wizard\Examples

# Run the script
.\Wizard-HyperV-CreateVM.ps1
```

## Key Configuration Steps

### 1. General Settings
Collects the VM Name and Notes. The name is validated to ensure it contains only valid Windows character sets.

### 2. Resource Allocation
Uses `Numeric` controls for CPU core count and Memory (RAM). Bounds are set to ensure users don't request more resources than the host can provide.

### 3. Storage & Networking
Uses `Path` controls to select the VHDX location and `Dropdowns` for virtual switch selection.

## Automation Logic

The script uses the **Simple Mode** pattern. Once the user clicks "Finish", the collected hashtable is returned to the main script where the actual Hyper-V cmdlets (`New-VM`, `New-VHD`, etc.) would be called.

```powershell
$result = Show-PoshUIWizard

if ($result) {
    Write-Host "Ready to create VM: $($result.VMName)" -ForegroundColor Green
    Write-Host "CPU Cores: $($result.CPU)"
    Write-Host "Memory: $($result.RAM) GB"
    # Logic to call New-VM would go here
}
```

::: info
This demo requires the **Hyper-V PowerShell Module** to be installed on your system if you intend to extend it with actual provisioning logic.
:::
