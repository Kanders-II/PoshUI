# Dashboard Demo

The `Dashboard-MultiPageDashboard.ps1` example demonstrates how to build a complex, professional monitoring interface with multiple pages, various visualization cards, and live data refreshing.

![Dashboard Multi-Page](../images/visualization/Screenshot-2026-01-30-145120.png)

## Overview

This demonstration illustrates the power of the PoshUI.Dashboard module for IT operations. It features:
- **Multiple Pages**: Navigation between different system views (Overview, Performance, Storage).
- **MetricCards**: High-level KPIs for CPU, Memory, and Network.
- **GraphCards**: Real-time charts showing system trends.
- **DataGridCards**: Interactive tables for processes and event logs.
- **Live Refresh**: Automatic updates for all cards without manual interaction.

## Running the Example

```powershell
# Navigate to the Dashboard examples directory
cd .\PoshUI\PoshUI.Dashboard\Examples

# Run the script
.\Dashboard-MultiPageDashboard.ps1
```

## Key Configuration Steps

### 1. Multi-Page Navigation
The example uses `Add-UIStep` with `-Type Dashboard` to create separate views accessible from the sidebar.

```powershell
Add-UIStep -Name 'Overview' -Title 'Overview' -Type Dashboard -Icon '&#xE8BC;'
Add-UIStep -Name 'Perf' -Title 'Performance' -Type Dashboard -Icon '&#xE9D2;'
```

### 2. High-Density Metrics
The "Overview" step uses a multi-column grid to show several `MetricCards` at once, providing an immediate health status of the system.

### 3. Interactive Data
The "Storage" step demonstrates how to use `DataGridCards` to list drives and their utilization, with built-in sorting and filtering.

## Customization

The example also showcases how to use `-Category` to group cards and `-Description` to provide context for each visualization.

::: tip
Explore the `Dashboard-CardFeatures.ps1` example for a deeper dive into the specific styling options available for each card type.
:::
