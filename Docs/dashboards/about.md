# About Dashboards

Dashboards in PoshUI are designed for real-time monitoring, KPI visualization, and IT operations management. They provide a high-level overview of system health and metrics using a card-based grid layout.

![Dashboard Dark Theme](../images/visualization/Dashboard_Dark_.png)

## Key Features

- **Card-Based Layout**: Organized grid of information cards that can be categorized and filtered.
- **Rich Visualizations**: Built-in support for metric KPIs, charts (Bar, Line, Area, Pie), and data grids.
- **Live Refresh**: Cards can execute PowerShell scripts on a schedule to update their values automatically.
- **Category Filtering**: Built-in search and filtering to group related cards together.
- **Interactive Tools**: ScriptCards allow users to run management tasks directly from the dashboard.
- **Export Capabilities**: Export data from DataGridCards to CSV or TXT files.

## When to Use Dashboards

Dashboards are ideal for:
- **System Monitoring**: Tracking CPU, memory, disk space, and service status across servers.
- **Deployment Tracking**: Monitoring the progress of automated deployments.
- **Incident Management**: Displaying active tickets or alerts from monitoring systems.
- **Executive KPI Displays**: High-level summaries of infrastructure health.

## Workflow

A PoshUI Dashboard follows this pattern:

1. **Initialize**: Use `New-PoshUIDashboard` to start a new definition.
2. **Define Pages**: Add dashboard pages using `Add-UIStep -Type Dashboard`.
3. **Add Components**: Populate pages with `Add-UIVisualizationCard`, `Add-UIScriptCard`, or `Add-UIBanner`.
4. **Show**: Display the interface using `Show-PoshUIDashboard`.

Next: [Creating Dashboards](./creating-dashboards.md)
