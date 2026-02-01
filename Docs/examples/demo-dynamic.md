# Dynamic Controls Demo

The `Wizard-DynamicControls.ps1` example showcases PoshUI's powerful cascading dependency system, which allows control choices to refresh automatically based on user selections.

## Overview

This demonstration illustrates how to build intelligent forms that guide users through hierarchical choices. It focuses on the use of `ScriptBlocks` and the `-DependsOn` parameter.

## Key Scenarios Covered

- **Cascading Dropdowns**: Environment -> Region -> Server selection.
- **Conditional Logic**: Showing different options based on previous selections.
- **CSV Integration**: Loading dynamic data from an external `sample-servers.csv` file.
- **Real-time Feedback**: Instant UI updates as selections change.

## Running the Example

```powershell
# Navigate to the Wizard examples directory
cd .\PoshUI\PoshUI.Wizard\Examples

# Run the script
.\Wizard-DynamicControls.ps1
```

## How It Works

The example uses a chain of dependent dropdowns. When the user selects an "Environment", the "Region" dropdown executes a script block to filter its options.

### Code Snippet: Cascading Logic

```powershell
# Parent Control
Add-UIDropdown -Step 'Config' -Name 'Environment' -Label 'Environment' `
               -Choices @('Development', 'Staging', 'Production')

# Dependent Control
Add-UIDropdown -Step 'Config' -Name 'Region' -Label 'Region' `
               -DependsOn 'Environment' `
               -ScriptBlock {
                   param($Environment)
                   if ($Environment -eq 'Production') {
                       return @('North US', 'West Europe', 'Asia Pacific')
                   } else {
                       return @('Internal Lab', 'Test DMZ')
                   }
               }
```

## Advanced Features

### CSV Data Sourcing
The example demonstrates how to use `Import-Csv` within a `ScriptBlock` to populate choices from an external file, which is a common requirement for enterprise automation.

### Sequential Refresh
You will notice a brief loading state when a parent value changes. PoshUI manages these updates sequentially to prevent data race conditions in complex dependency trees.

::: tip
Check the `sample-servers.csv` file in the examples folder to see the data structure used for the CSV-driven dropdowns.
:::
