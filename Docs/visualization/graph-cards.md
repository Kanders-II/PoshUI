# GraphCard

::: warning Dashboard Module Only
GraphCards are only available in the **PoshUI.Dashboard** module. They cannot be used in Wizards or Workflows.
:::

The `GraphCard` provides interactive charts for visualizing trends and distributions directly on your dashboard.

![Dashboard with Charts](../images/visualization/Dashboard_Charts_Light_2.png)

## Basic Usage

```powershell
$chartData = @(
    @{ Label = 'Jan'; Value = 100 }
    @{ Label = 'Feb'; Value = 150 }
    @{ Label = 'Mar'; Value = 120 }
)

Add-UIVisualizationCard -Step 'Overview' -Name 'SalesChart' -Type 'GraphCard' `
    -Title 'Monthly Sales' -ChartType 'Bar' -Data $chartData
```

## Supported Chart Types

You can choose from four primary chart types using the `-ChartType` parameter:

- **Bar**: Best for comparing distinct categories.
- **Line**: Ideal for showing trends over time.
- **Area**: Similar to line charts but highlights the volume under the line.
- **Pie**: Best for showing proportional distributions (percentage of a whole).

## Data Format

The `-Data` parameter accepts an array of hashtables. Each hashtable should contain a `Label` (string) and a `Value` (numeric).

```powershell
$data = @(
    @{ Label = 'Available'; Value = 45 }
    @{ Label = 'Used'; Value = 55 }
)
Add-UIVisualizationCard -Step 'Overview' -Name 'Storage' -Type 'GraphCard' `
    -Title 'Storage Distribution' -ChartType 'Pie' -Data $data
```

## Key Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-ChartType`| String | The type of chart: `'Bar'`, `'Line'`, `'Area'`, or `'Pie'`. |
| `-Data` | Array | An array of hashtables containing `Label` and `Value`. |
| `-ShowLegend`| Boolean| Whether to display the chart legend (default: `$true`). |
| `-ShowTooltip`| Boolean| Whether to show values when hovering over data points (default: `$true`). |

## Live Refresh

Like all visualization cards, GraphCards support automatic updates.

```powershell
Add-UIVisualizationCard -Step 'Overview' -Name 'LiveNet' -Type 'GraphCard' `
    -Title 'Network Traffic' -ChartType 'Area' `
    -RefreshScript {
        # Fetch latest traffic stats and return as chart data
        Get-NetworkStats | Select-Object @{N='Label';E={$_.Time}}, @{N='Value';E={$_.Bytes}}
    } `
    -RefreshInterval 5
```

Next: [DataGridCard](./datagrid-cards.md)
