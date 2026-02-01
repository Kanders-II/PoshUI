# Category Filtering

PoshUI Dashboards support built-in category filtering, allowing you to organize large numbers of cards and let users focus on specific system areas.

## Using Categories

Every dashboard card (`MetricCard`, `GraphCard`, `DataGridCard`, `ScriptCard`) supports a `-Category` parameter.

```powershell
# Add cards with categories
Add-UIVisualizationCard -Step 'Main' -Name 'CPU' -Type MetricCard -Title 'CPU' -Category 'Performance'
Add-UIVisualizationCard -Step 'Main' -Name 'RAM' -Type MetricCard -Title 'RAM' -Category 'Performance'
Add-UIVisualizationCard -Step 'Main' -Name 'SQL' -Type MetricCard -Title 'SQL Status' -Category 'Database'
```

## How Filtering Works

When multiple categories are present on a dashboard step:

1. **Title Bar Filter**: A searchable dropdown appears in the dashboard title bar.
2. **Category Selection**: Selecting a category immediately hides all cards that do not match.
3. **"All" View**: The default view shows all cards from all categories.
4. **Search**: The filter dropdown supports "type-to-search" for quickly finding a category in dense dashboards.

## Best Practices

- **Consistent Naming**: Use consistent category names across different cards (e.g., don't mix "Performance" and "Perf").
- **Logical Grouping**: Group related cards together (e.g., "Network", "Storage", "Application").
- **Avoid Over-Categorization**: Too many categories can make the filter dropdown difficult to use. Aim for 3-7 categories per dashboard step.

## Example: Multi-Category Dashboard

```powershell
Import-Module PoshUI.Dashboard

New-PoshUIDashboard -Title 'Enterprise Monitor' -GridColumns 4

Add-UIStep -Name 'Dashboard' -Title 'Overview' -Type Dashboard

# Performance Category
Add-UIVisualizationCard -Step 'Dashboard' -Name 'CPU' -Type MetricCard `
    -Title 'CPU Usage' -Value 45 -Unit '%' -Category 'Performance'

# Security Category
Add-UIVisualizationCard -Step 'Dashboard' -Name 'Logins' -Type MetricCard `
    -Title 'Failed Logins' -Value 2 -Category 'Security'

# Services Category
Add-UIVisualizationCard -Step 'Dashboard' -Name 'IIS' -Type MetricCard `
    -Title 'IIS Status' -Value 'Running' -Category 'Services'

Show-PoshUIDashboard
```

Next: [About Workflows](../workflows/about.md)
