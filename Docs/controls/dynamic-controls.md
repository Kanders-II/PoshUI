# Dynamic Controls

Dynamic controls in PoshUI allow you to create interactive interfaces where the available choices in one control depend on the values selected in others. This is commonly used for cascading dropdowns (e.g., selecting a Region, then a Data Center, then a Server).

## How it Works

Dynamic controls use a **ScriptBlock** to fetch or generate data at runtime. When a "parent" control's value changes, any "dependent" controls are automatically refreshed by re-executing their script blocks with the updated values.

## Basic Usage: Cascading Dropdowns

To create a dynamic dependency, use the `-ScriptBlock` parameter and define your dependencies using the `-DependsOn` parameter.

```powershell
# 1. The Parent Control
Add-UIDropdown -Step 'Config' -Name 'Environment' -Label 'Environment' `
               -Choices @('Dev', 'Prod')

# 2. The Dependent Control
Add-UIDropdown -Step 'Config' -Name 'Server' -Label 'Target Server' `
               -DependsOn 'Environment' `
               -ScriptBlock {
                   # The value of $Environment is automatically available
                   if ($Environment -eq 'Prod') {
                       return @('PROD-SQL-01', 'PROD-WEB-01')
                   } else {
                       return @('DEV-SQL-01', 'DEV-WEB-01')
                   }
               }
```

## Advanced Usage: Fetching Live Data

You can use standard PowerShell cmdlets inside the script block to fetch real-world data from your environment.

```powershell
Add-UIDropdown -Step 'VM' -Name 'VMName' -Label 'Select Virtual Machine' `
               -ScriptBlock {
                   Get-VM | Select-Object -ExpandProperty Name
               }
```

## Key Parameters

| Parameter | Description |
|-----------|-------------|
| `-ScriptBlock` | The PowerShell code that returns an array of strings for the control choices. |
| `-DependsOn` | One or more names of other controls that, when changed, should trigger this control to refresh. |

## Important Considerations

1. **Sequential Refresh**: If you have a chain of dependencies (A -> B -> C), PoshUI refreshes them sequentially to ensure data consistency.
2. **Variable Injection**: Any control name listed in `-DependsOn` becomes a variable inside your `-ScriptBlock` containing its current value.
3. **Performance**: Script blocks execute in the background, but very slow operations (like complex AD queries) may show a brief loading indicator.
4. **Return Type**: The script block **must** return an array of strings (or objects that can be converted to strings).

::: tip
Always use `-DependsOn` when your script block references another control's value to ensure the UI updates correctly when the user changes their selection.
:::

Next: [Visualization Cards](../dashboards/visualization-cards.md)
