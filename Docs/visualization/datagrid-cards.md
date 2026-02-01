# DataGridCard

::: warning Dashboard Module Only
DataGridCards are only available in the **PoshUI.Dashboard** module. They cannot be used in Wizards or Workflows.
:::

The `DataGridCard` is used to display tabular data in a sortable and filterable grid. It is ideal for lists of processes, event logs, service statuses, or any PowerShell object collection.

![DataGrid Cards](../images/visualization/Screenshot-2026-01-23-200009.png)

## Basic Usage

```powershell
$processData = Get-Process | Select-Object -First 10 Name, Id, CPU, WorkingSet

Add-UIVisualizationCard -Step 'Overview' -Name 'ProcessGrid' -Type 'DataGridCard' `
    -Title 'Running Processes' -Data $processData
```

## Features

- **Automatic Columns**: PoshUI automatically generates columns based on the properties of the objects in your `-Data` collection.
- **Sorting**: Users can click column headers to sort data ascending or descending.
- **Filtering**: A built-in search box allows users to filter the grid in real-time.
- **Export**: Users can export the current grid view to CSV or TXT files using the export button on the card.

## Data Format

The `-Data` parameter accepts:
- An array of PowerShell objects (e.g., from `Get-Service` or `Get-Process`).
- An array of hashtables.
- A ScriptBlock that returns one of the above (for Live Refresh).

```powershell
Add-UIVisualizationCard -Step 'Overview' -Name 'Services' -Type 'DataGridCard' `
    -Title 'Critical Services' `
    -Data { Get-Service | Where-Object Status -eq 'Running' | Select-Object Name, DisplayName, StartType } `
    -RefreshInterval 30
```

## Key Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-Data` | Object/Array | The collection of objects or hashtables to display. |
| `-RefreshScript` | ScriptBlock | PowerShell code to re-fetch the grid data. |
| `-RefreshInterval` | Int | How often to refresh the data in seconds. |

::: info
In the current version, Sorting, Filtering, and Export capabilities are enabled by default for all DataGridCards to provide the best user experience.
:::

Next: [ScriptCard](./script-cards.md)
