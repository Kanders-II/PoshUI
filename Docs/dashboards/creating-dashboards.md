# Creating Dashboards

Dashboards in PoshUI provide a grid-based interface for visualizing system metrics and running interactive tasks.

## Basic Structure

A minimal dashboard script follows this structure:

```powershell
# 1. Import
Import-Module PoshUI.Dashboard

# 2. Metadata
New-PoshUIDashboard -Title 'System Overview' -GridColumns 3

# 3. Pages (Steps)
Add-UIStep -Name 'Main' -Title 'Overview' -Type Dashboard

# 4. Components
Add-UIVisualizationCard -Step 'Main' -Name 'CPU' -Type MetricCard `
    -Title 'CPU Usage' -Value 45 -Unit '%'

# 5. Show
Show-PoshUIDashboard
```

## Initializing the Dashboard

The `New-PoshUIDashboard` cmdlet creates the context for your dashboard.

| Parameter | Description |
|-----------|-------------|
| `-Title` | The primary title shown in the window and sidebar. |
| `-Description` | Optional text explaining the purpose of the dashboard. |
| `-GridColumns` | Number of columns in the card grid (1-6, default is 3). |
| `-Theme` | Sets the visual style (`Auto`, `Light`, or `Dark`). |

## Dashboard Steps

Dashboards use the same `Add-UIStep` cmdlet as wizards, but they must be set to `-Type Dashboard`.

```powershell
Add-UIStep -Name 'Network' -Title 'Network Stats' -Type Dashboard -Icon '&#xE968;'
```

Each step in a dashboard represents a separate "page" or "view" that users can switch between using the sidebar.

## Grid Layout

PoshUI uses a responsive grid system. You can control the number of columns using the `-GridColumns` parameter on `New-PoshUIDashboard`.

- **1-2 Columns**: Best for detailed charts and large data grids.
- **3-4 Columns**: Ideal for balanced KPI dashboards.
- **5-6 Columns**: Best for high-density "NOC" style displays with many small metric cards.

::: tip
Cards automatically wrap to the next row when the grid columns are filled.
:::

Next: [Visualization Cards](./visualization-cards.md)
