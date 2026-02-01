# Visualization Cards

Visualization cards are the primary components of PoshUI dashboards. They provide specialized displays for different types of data.

![Dashboard with Charts](../images/visualization/Dashboard_DataGird.png)

## Adding Cards

Use the `Add-UIVisualizationCard` cmdlet to add cards to a dashboard step.

```powershell
Add-UIVisualizationCard -Step 'Overview' -Name 'Metric1' -Type MetricCard ...
```

## Supported Card Types

### MetricCard
Designed for Key Performance Indicators (KPIs). It shows a large value, an optional unit, a trend indicator, and an icon.

- **Use case**: CPU usage, memory availability, active user count.
- **Key Parameters**: `-Value`, `-Unit`, `-Trend`, `-TrendValue`, `-Icon`, `-Target`.

### GraphCard
Displays data in various chart formats.

- **Use case**: Historical performance, distribution of resources.
- **Chart Types**: `Bar`, `Line`, `Area`, `Pie`.
- **Key Parameters**: `-ChartType`, `-Data`.

### DataGridCard
Displays tabular data in a sortable, filterable grid.

- **Use case**: Process lists, event logs, service status tables.
- **Key Parameters**: `-Data`, `-AllowSort`, `-AllowExport`.

### InfoCard
A display-only card for providing context or instructions.

- **Use case**: Dashboard descriptions, help information.
- **Key Parameters**: `-Title`, `-Description`, `-Content`.

## Card Customization

All cards support the following common parameters:

| Parameter | Description |
|-----------|-------------|
| `-Title` | The bold header text at the top of the card. |
| `-Description` | Smaller text shown below the title. |
| `-Icon` | Segoe MDL2 icon glyph. |
| `-Category` | String used for grouping and filtering cards. |

## Live Refresh

Cards can be configured to update automatically without reloading the entire dashboard.

```powershell
Add-UIVisualizationCard -Step 'Main' -Name 'CPU' -Type MetricCard `
    -Title 'Live CPU' `
    -RefreshScript { (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average } `
    -RefreshInterval 5
```

::: tip
See [Live Refresh](./refresh.md) for more details on scheduling updates.
:::

Next: [Script Cards](./script-cards.md)
