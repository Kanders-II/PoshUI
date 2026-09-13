# MetricCard

::: warning Dashboard Module Only
MetricCards are only available in the **PoshUI.Dashboard** module. They cannot be used in Wizards or Workflows.
:::

The `MetricCard` is designed to display a single, high-impact value (KPI) with context like trends, icons, and progress bars.

## Basic Usage

```powershell
Add-UIMetricCard -Step 'Overview' -Name 'UserCount' `
    -Title 'Active Users' -Value 1250 -Icon '&#xE77B;'
```

## Advanced Features

### Trend Indicators
Show how a metric has changed over time using the `-Trend` and `-TrendValue` parameters.

```powershell
Add-UIMetricCard -Step 'Overview' -Name 'Sales' `
    -Title 'Monthly Sales' -Value 45000 -Unit '$' `
    -Trend 'up' -TrendValue 12.5
```

- **Trend Values**: `'up'`, `'down'`, `'stable'`.
- **TrendValue**: A number representing the percentage or absolute change.

### Target and Progress
Visualize a value relative to a target using a built-in progress bar.

```powershell
Add-UIMetricCard -Step 'Overview' -Name 'Quota' `
    -Title 'Storage Quota' -Value 85 -Unit '%' `
    -Target 100 -Icon '&#xEDA2;'
```

- **Target**: The 100% mark for the progress bar.
- **Value**: The current progress toward that target.

### Status Colors
The trend arrow is coloured by its direction (`up` green, `down` red, `stable` neutral). There is no semantic colour parameter on a metric card: to signal a threshold, return a different `-Trend` from your refresh script, or use [Add-UIStatusCard](../dashboards/visualization-cards.md) which is built for state rather than magnitude.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `-Title` | Main heading for the card. |
| `-Value` | The numeric or string value to display prominently. |
| `-Unit` | Suffix shown next to the value (e.g., `'%'`, `'GB'`). |
| `-Icon` | Segoe MDL2 glyph for the card. |
| `-Trend` | Directional indicator: `'up'`, `'down'`, or `'stable'`. |
| `-TrendValue` | Numeric change displayed next to the trend icon. |
| `-Target` | Optional target value for progress bar visualization. |
| `-Category` | Filtering category for the dashboard. |

## Live Refresh

Give the card a `-RefreshScript` and it can be re-read after launch, on demand - see [Card Refresh](../dashboards/refresh.md).

```powershell
Add-UIMetricCard -Step 'Overview' -Name 'CPU' `
    -Title 'System CPU' -Value 0 -Unit '%' `
    -RefreshScript { (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average }
```

Next: [GraphCard](./graph-cards.md)
