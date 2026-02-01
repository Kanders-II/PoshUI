# Live Refresh

Dashboards in PoshUI support real-time updates for visualization cards. This allows you to monitor dynamic data like system performance, active processes, or service status without manual interaction.

## How Refresh Works

Each card in a dashboard can have its own `-RefreshScript` and `-RefreshInterval`. 

1. **Initial Load**: When the dashboard starts, the `Value` or `Data` script is executed once.
2. **Scheduling**: The card enters a refresh loop based on the `RefreshInterval` (in seconds).
3. **Execution**: The `RefreshScript` is executed in a background runspace.
4. **UI Update**: The result of the script is pushed to the UI, and the card updates its display.

## Configuring Refresh

### Simple Metric Refresh

```powershell
Add-UIVisualizationCard -Step 'Main' -Name 'CPU' -Type MetricCard `
    -Title 'CPU Usage' `
    -Value 0 `
    -Unit '%' `
    -RefreshScript { (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average } `
    -RefreshInterval 5
```

### DataGrid Refresh

```powershell
Add-UIVisualizationCard -Step 'Main' -Name 'ProcGrid' -Type DataGridCard `
    -Title 'Top Processes' `
    -Data @() `
    -RefreshScript { Get-Process | Sort-Object CPU -Descending | Select-Object -First 10 Name, Id, CPU } `
    -RefreshInterval 10
```

## Refresh Properties

| Parameter | Description |
|-----------|-------------|
| `-RefreshScript` | A ScriptBlock that returns the new value/data for the card. |
| `-RefreshInterval` | Time in seconds between updates. Minimum is 1 second. |

## Performance Considerations

- **Background Execution**: Refresh scripts run in separate runspaces, so they don't block the UI.
- **Resource Usage**: Frequent refreshes (e.g., every 1 second) on complex scripts (like querying remote WMI) can increase CPU/Memory usage.
- **Error Handling**: If a refresh script fails, the card will display the last known good value or an error indicator if configured.

::: tip
Use `Get-CimInstance` instead of `Get-WmiObject` for better performance and modern compatibility in your refresh scripts.
:::

Next: [Category Filtering](./categories.md)
