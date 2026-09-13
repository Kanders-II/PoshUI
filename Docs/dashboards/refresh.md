# Card Refresh

A dashboard card can re-read its own data after the window is open, so a dashboard stays useful for monitoring
instead of showing a snapshot from launch time.

## How Refresh Works

Refresh is **on demand, not on a timer.** Give a card a `-RefreshScript` and it becomes refreshable:

1. **Initial load**: the card renders the `-Value` (or `-Data`) supplied when it was declared.
2. **Trigger**: the operator refreshes the card, or uses the dashboard's *Refresh all*, which fires every
   refreshable card on the visible page.
3. **Execution**: the `RefreshScript` runs in its own runspace, off the UI thread, so a slow query does not
   freeze the window.
4. **UI update**: whatever the script returns replaces the card's value or rows.

::: warning No interval parameter
There is no `-RefreshInterval` on a dashboard card, and nothing polls in the background. If you need a value
that updates itself on a clock, use the Canvas module, where a control takes `-Refresh <seconds>` and the
engine ticks it — see [About Canvas](../canvas/about.md).
:::

## Configuring Refresh

### Simple Metric Refresh

```powershell
Add-UIMetricCard -Step 'Main' -Name 'CPU' `
    -Title 'CPU Usage' `
    -Value 0 `
    -Unit '%' `
    -RefreshScript { (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average }
```

### DataGrid Refresh

```powershell
Add-UITableCard -Step 'Main' -Name 'ProcGrid' `
    -Title 'Top Processes' `
    -Data @() `
    -RefreshScript { Get-Process | Sort-Object CPU -Descending | Select-Object -First 10 Name, Id, CPU }
```

## Refresh Properties

| Parameter | Description |
|-----------|-------------|
| `-RefreshScript` | A ScriptBlock returning the new value (MetricCard) or rows (TableCard / ChartCard). Supported on `Add-UIMetricCard`, `Add-UIChartCard`, `Add-UITableCard` and `Add-UIStatusCard`. |

## Performance Considerations

- **Background execution**: refresh scripts run in separate runspaces, so they do not block the UI.
- **Cost per click**: *Refresh all* fires every refreshable card at once, so keep the scripts cheap — a remote
  WMI query on a dozen cards is a dozen remote queries.
- **Error handling**: a script that throws leaves the card showing its last known value.

::: tip
Use `Get-CimInstance` rather than `Get-WmiObject` in refresh scripts: it is faster and still supported.
:::

Next: [Category Filtering](./categories.md)
