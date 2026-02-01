# Dashboard Card Cmdlets Reference

Complete reference for PoshUI.Dashboard card cmdlets with accurate parameter information.

## Add-UIMetricCard

Displays a single numeric value with optional trend indicator and progress bar. Ideal for KPIs and system metrics.

### Syntax

```powershell
Add-UIMetricCard
    -Step <String>
    -Name <String>
    -Title <String>
    [-Description <String>]
    -Value <Object>
    [-Unit <String>]
    [-Format <String>]
    [-Trend <String>]
    [-TrendValue <Double>]
    [-Target <Double>]
    [-MinValue <Double>]
    [-MaxValue <Double>]
    [-Icon <String>]
    [-Category <String>]
    [-RefreshScript <ScriptBlock>]
```

### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `-Step` | String | Yes | - | Name of the step to add this card to |
| `-Name` | String | Yes | - | Unique identifier for the card |
| `-Title` | String | Yes | - | Display title for the card |
| `-Description` | String | No | - | Short description shown below the title |
| `-Value` | Object | Yes | - | Numeric value or ScriptBlock that returns a number |
| `-Unit` | String | No | - | Unit suffix (e.g., %, GB, items) |
| `-Format` | String | No | "N0" | Number format string (e.g., 'N0', 'N2', 'P0') |
| `-Trend` | String | No | - | Trend indicator: 'up', 'down', or 'stable' |
| `-TrendValue` | Double | No | - | Numeric trend value to display with indicator |
| `-Target` | Double | No | - | Target value for progress bar |
| `-MinValue` | Double | No | 0 | Minimum value for progress bar |
| `-MaxValue` | Double | No | 100 | Maximum value for progress bar |
| `-Icon` | String | No | - | Segoe MDL2 icon glyph (e.g., '&#xE7C4;') |
| `-Category` | String | No | "General" | Category for filtering cards |
| `-RefreshScript` | ScriptBlock | No | - | Script to refresh the value |

### Examples

**Static Value:**
```powershell
Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU Usage' `
    -Value 75.5 -Unit '%' -Target 80
```

**Dynamic Value with ScriptBlock:**
```powershell
Add-UIMetricCard -Step 'Dashboard' -Name 'Memory' -Title 'Memory Usage' `
    -Value { (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1MB } `
    -Unit 'GB' -Format 'N2'
```

**With Trend Indicator:**
```powershell
Add-UIMetricCard -Step 'Dashboard' -Name 'Disk' -Title 'Disk Usage' `
    -Value 450 -Unit 'GB' -Trend 'up' -TrendValue 12.5
```

**With Progress Bar:**
```powershell
Add-UIMetricCard -Step 'Dashboard' -Name 'Tasks' -Title 'Completed Tasks' `
    -Value 75 -Target 100 -MinValue 0 -MaxValue 100 -Unit 'tasks'
```

### Notes

- When `-Value` is a ScriptBlock, it executes once for initial display
- If `-RefreshScript` is not specified and `-Value` is a ScriptBlock, the Value ScriptBlock is automatically used for refresh
- Use `-Verbose` to see ScriptBlock execution details and errors
- Trend values: 'up' (↑), 'down' (↓), 'stable' (→)

---

## Add-UIChartCard

Displays data visualization charts (Line, Bar, Area, Pie). Ideal for trends and comparisons.

### Syntax

```powershell
Add-UIChartCard
    -Step <String>
    -Name <String>
    -Title <String>
    [-Description <String>]
    -ChartType <String>
    -Data <Object>
    [-Icon <String>]
    [-Category <String>]
    [-RefreshScript <ScriptBlock>]
```

### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `-Step` | String | Yes | - | Name of the step to add this card to |
| `-Name` | String | Yes | - | Unique identifier for the card |
| `-Title` | String | Yes | - | Display title for the card |
| `-Description` | String | No | - | Short description shown below the title |
| `-ChartType` | String | Yes | - | Chart type: 'Line', 'Bar', 'Area', or 'Pie' |
| `-Data` | Object | Yes | - | Array of data objects or ScriptBlock |
| `-Icon` | String | No | - | Segoe MDL2 icon glyph |
| `-Category` | String | No | "General" | Category for filtering cards |
| `-RefreshScript` | ScriptBlock | No | - | Script to refresh the data |

### Data Format

Data must be an array of hashtables with `Label` and `Value` properties:

```powershell
@(
    @{ Label = 'January'; Value = 100 }
    @{ Label = 'February'; Value = 150 }
    @{ Label = 'March'; Value = 120 }
)
```

### Examples

**Static Data:**
```powershell
$data = @(
    @{ Label = 'CPU'; Value = 75 }
    @{ Label = 'Memory'; Value = 60 }
    @{ Label = 'Disk'; Value = 45 }
)

Add-UIChartCard -Step 'Dashboard' -Name 'Resources' -Title 'Resource Usage' `
    -ChartType 'Bar' -Data $data
```

**Dynamic Data with ScriptBlock:**
```powershell
Add-UIChartCard -Step 'Dashboard' -Name 'TopProcesses' -Title 'Top 5 Processes' `
    -ChartType 'Bar' `
    -Data {
        Get-Process | Sort-Object CPU -Descending | Select-Object -First 5 | ForEach-Object {
            @{ Label = $_.Name; Value = [math]::Round($_.CPU, 2) }
        }
    }
```

**Pie Chart:**
```powershell
Add-UIChartCard -Step 'Dashboard' -Name 'DiskSpace' -Title 'Disk Space Distribution' `
    -ChartType 'Pie' `
    -Data {
        Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" | ForEach-Object {
            @{ 
                Label = $_.DeviceID
                Value = [math]::Round(($_.Size - $_.FreeSpace) / 1GB, 1)
            }
        }
    }
```

### Notes

- Use `-Verbose` to see data retrieval and chart rendering details
- ScriptBlock errors are caught and displayed with helpful suggestions
- Chart colors are automatically assigned from theme palette

---

## Add-UITableCard

Displays tabular data with columns and rows. Ideal for lists and detailed information.

### Syntax

```powershell
Add-UITableCard
    -Step <String>
    -Name <String>
    -Title <String>
    [-Description <String>]
    -Data <Object>
    [-Icon <String>]
    [-Category <String>]
    [-RefreshScript <ScriptBlock>]
```

### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `-Step` | String | Yes | - | Name of the step to add this card to |
| `-Name` | String | Yes | - | Unique identifier for the card |
| `-Title` | String | Yes | - | Display title for the card |
| `-Description` | String | No | - | Short description shown below the title |
| `-Data` | Object | Yes | - | Array of objects or ScriptBlock |
| `-Icon` | String | No | - | Segoe MDL2 icon glyph |
| `-Category` | String | No | "General" | Category for filtering cards |
| `-RefreshScript` | ScriptBlock | No | - | Script to refresh the data |

### Data Format

Data must be an array of objects. Properties become columns:

```powershell
@(
    [PSCustomObject]@{ Name = 'Server1'; Status = 'Running'; CPU = 45 }
    [PSCustomObject]@{ Name = 'Server2'; Status = 'Stopped'; CPU = 0 }
)
```

### Examples

**Static Data:**
```powershell
$servers = @(
    [PSCustomObject]@{ Name = 'WEB01'; Status = 'Running'; Uptime = '15 days' }
    [PSCustomObject]@{ Name = 'WEB02'; Status = 'Running'; Uptime = '8 days' }
    [PSCustomObject]@{ Name = 'DB01'; Status = 'Stopped'; Uptime = '0 days' }
)

Add-UITableCard -Step 'Dashboard' -Name 'Servers' -Title 'Server Status' `
    -Data $servers
```

**Dynamic Data with ScriptBlock:**
```powershell
Add-UITableCard -Step 'Dashboard' -Name 'Services' -Title 'Critical Services' `
    -Data {
        Get-Service | Where-Object { $_.DisplayName -like '*SQL*' } | 
            Select-Object DisplayName, Status, StartType | 
            Select-Object -First 10
    }
```

**Formatted Data:**
```powershell
Add-UITableCard -Step 'Dashboard' -Name 'Disks' -Title 'Disk Space' `
    -Data {
        Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" | ForEach-Object {
            [PSCustomObject]@{
                Drive = $_.DeviceID
                'Total (GB)' = [math]::Round($_.Size / 1GB, 2)
                'Free (GB)' = [math]::Round($_.FreeSpace / 1GB, 2)
                'Used %' = [math]::Round((($_.Size - $_.FreeSpace) / $_.Size) * 100, 1)
            }
        }
    }
```

### Notes

- Column headers are automatically generated from property names
- Data is sortable and filterable in the UI
- Use `-Verbose` to see data retrieval details

---

## Add-UICard (InfoCard)

Displays informational text content. Ideal for instructions, help text, and contextual information.

### Syntax

```powershell
Add-UICard
    -Step <String>
    -Name <String>
    -Title <String>
    -Content <String>
    [-Icon <String>]
    [-Category <String>]
```

### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `-Step` | String | Yes | - | Name of the step to add this card to |
| `-Name` | String | Yes | - | Unique identifier for the card |
| `-Title` | String | Yes | - | Display title for the card |
| `-Content` | String | Yes | - | Text content to display |
| `-Icon` | String | No | - | Segoe MDL2 icon glyph |
| `-Category` | String | No | "General" | Category for filtering cards |

### Examples

**Simple Info Card:**
```powershell
Add-UICard -Step 'Dashboard' -Name 'Welcome' -Title 'Welcome' `
    -Content 'This dashboard shows real-time system metrics. Cards refresh automatically every 5 seconds.'
```

**With Icon:**
```powershell
Add-UICard -Step 'Dashboard' -Name 'Help' -Title 'Quick Help' `
    -Content 'Click any metric card to see detailed information. Use the category filter to show specific card types.' `
    -Icon '&#xE897;'
```

**Multi-line Content:**
```powershell
$helpText = @"
Dashboard Features:
- Real-time metric updates
- Interactive charts
- Sortable data tables
- Category filtering
"@

Add-UICard -Step 'Dashboard' -Name 'Features' -Title 'Features' `
    -Content $helpText
```

### Notes

- Content supports multi-line text
- Cards are sized dynamically based on content length
- Use for instructions, warnings, or contextual help

---

## Get-PoshUIDashboard

Retrieves the current dashboard definition for inspection and debugging.

### Syntax

```powershell
Get-PoshUIDashboard
    [-IncludeProperties]
    [-StepName <String>]
    [-AsJson]
```

### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `-IncludeProperties` | Switch | No | - | Include detailed property information |
| `-StepName` | String | No | - | Filter to a specific step |
| `-AsJson` | Switch | No | - | Return as JSON string |

### Examples

**Basic Summary:**
```powershell
Get-PoshUIDashboard
```

**Detailed View:**
```powershell
Get-PoshUIDashboard -IncludeProperties
```

**Specific Step:**
```powershell
Get-PoshUIDashboard -StepName 'Dashboard'
```

**Export as JSON:**
```powershell
Get-PoshUIDashboard -AsJson | Out-File dashboard.json
```

### Use Cases

- **Debugging:** See what controls are actually added to steps
- **Verification:** Confirm card properties are set correctly
- **Troubleshooting:** Identify why cards aren't showing
- **Documentation:** Export dashboard structure

---

## Common Patterns

### Auto-Refresh with ScriptBlocks

When using ScriptBlocks for dynamic data, the ScriptBlock automatically becomes the refresh source:

```powershell
# This card refreshes every 5 seconds automatically
Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU Usage' `
    -Value { (Get-CimInstance Win32_Processor).LoadPercentage } `
    -Unit '%'
```

### Error Handling

Use `-Verbose` to see detailed error messages:

```powershell
Add-UIMetricCard -Step 'Dashboard' -Name 'Test' -Title 'Test' `
    -Value { Get-NonExistentCommand } `
    -Verbose
```

Output includes:
- Exact error message
- Suggestions for fixing
- ScriptBlock content for debugging

### Combining Card Types

```powershell
# Overview page with multiple card types
Add-UIStep -Name 'Overview' -Title 'System Overview' -Order 1

# Info card for context
Add-UICard -Step 'Overview' -Name 'Info' -Title 'Dashboard Info' `
    -Content 'Real-time system monitoring dashboard'

# Metric cards for KPIs
Add-UIMetricCard -Step 'Overview' -Name 'CPU' -Title 'CPU' `
    -Value { (Get-CimInstance Win32_Processor).LoadPercentage } -Unit '%'

Add-UIMetricCard -Step 'Overview' -Name 'Memory' -Title 'Memory' `
    -Value { (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1MB } -Unit 'GB'

# Chart for trends
Add-UIChartCard -Step 'Overview' -Name 'Processes' -Title 'Top Processes' `
    -ChartType 'Bar' `
    -Data { Get-Process | Sort-Object CPU -Descending | Select-Object -First 5 | 
            ForEach-Object { @{ Label = $_.Name; Value = $_.CPU } } }

# Table for details
Add-UITableCard -Step 'Overview' -Name 'Services' -Title 'Services' `
    -Data { Get-Service | Where-Object Status -eq 'Running' | Select-Object -First 10 Name, Status }
```

---

## Add-UIScriptCard

Adds an executable script card to a dashboard that runs PowerShell scripts with auto-discovered parameters.

### Syntax

```powershell
Add-UIScriptCard -Step <string> -Name <string> -Title <string>
                 [-Description <string>] [-Icon <string>]
                 -ScriptPath <string> [-DefaultParameters <hashtable>]
                 [-Category <string>] [-Tags <string[]>]

Add-UIScriptCard -Step <string> -Name <string> -Title <string>
                 [-Description <string>] [-Icon <string>]
                 -ScriptBlock <scriptblock> [-DefaultParameters <hashtable>]
                 [-Category <string>] [-Tags <string[]>]
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| **Step** | string | Name of the dashboard step (required) |
| **Name** | string | Unique identifier for the card (required) |
| **Title** | string | Display title shown on the card (required) |
| **Description** | string | Short description below the title |
| **Icon** | string | Segoe MDL2 glyph (`'&#xE77B;'`) or emoji |
| **ScriptPath** | string | Path to a `.ps1` file to execute |
| **ScriptBlock** | scriptblock | Inline PowerShell code to execute |
| **DefaultParameters** | hashtable | Default values to pre-populate parameters |
| **Category** | string | Category for grouping cards |
| **Tags** | string[] | Tags for filtering capabilities |

### Description

Script cards provide an interactive way to execute PowerShell scripts directly from the dashboard. When clicked, the card opens a dialog showing:

1. **Auto-discovered parameters** from the script's `param()` block
2. **Execution console** with real-time output
3. **Parameter inputs** with appropriate UI controls

Parameters are automatically discovered using PowerShell AST parsing, and each script executes in an isolated runspace for safety.

### Examples

#### Example 1: External Script with Auto-Discovery

```powershell
Add-UIScriptCard -Step "Tools" -Name "CreateUser" -Title "Create User" `
    -Description "Create a new local user account" `
    -Icon "&#xE77B;" `
    -ScriptPath ".\Scripts\New-LocalUser.ps1"
```

The script's parameters are automatically discovered and rendered as input controls in the dialog.

#### Example 2: Inline ScriptBlock with No Parameters

```powershell
Add-UIScriptCard -Step "Actions" -Name "RestartIIS" -Title "Restart IIS" `
    -Description "Restart the IIS web server" `
    -Icon "&#xE753;" `
    -ScriptBlock {
        Restart-Service W3SVC -Force
        "IIS Restarted at $(Get-Date)"
    }
```

Simple action card that executes immediately when clicked.

#### Example 3: Parameterized Script with Defaults

```powershell
Add-UIScriptCard -Step "Diagnostics" -Name "DiskCheck" -Title "Check Disk Space" `
    -Description "View disk space usage" `
    -Icon "&#xE74E;" `
    -ScriptBlock {
        param(
            [string]$Drive = "C",
            [int]$WarningThresholdPercent = 20
        )

        $disk = Get-PSDrive $Drive
        $percentFree = [math]::Round($disk.Free / ($disk.Used + $disk.Free) * 100, 1)

        [PSCustomObject]@{
            Drive = $Drive
            UsedGB = [math]::Round($disk.Used / 1GB, 2)
            FreeGB = [math]::Round($disk.Free / 1GB, 2)
            PercentFree = $percentFree
            Status = if ($percentFree -lt $WarningThresholdPercent) { "WARNING" } else { "OK" }
        }
    } `
    -DefaultParameters @{
        Drive = "C"
        WarningThresholdPercent = 15
    }
```

The dialog will show two input fields (Drive and WarningThresholdPercent) pre-populated with the default values.

#### Example 4: Real-World IT Administration

```powershell
# Service Management Card
Add-UIScriptCard -Step "Services" -Name "RestartService" -Title "Restart Service" `
    -Description "Safely restart a Windows service" `
    -ScriptPath ".\Scripts\Restart-ServiceSafe.ps1" `
    -Category "Management"

# Where Restart-ServiceSafe.ps1 contains:
<#
param(
    [Parameter(Mandatory=$true)]
    [string]$ServiceName,

    [int]$TimeoutSeconds = 30,

    [switch]$Force
)

$service = Get-Service $ServiceName -ErrorAction Stop
Write-Output "Stopping $ServiceName..."
Stop-Service $ServiceName -Force:$Force

Start-Sleep -Seconds 2

Write-Output "Starting $ServiceName..."
Start-Service $ServiceName

$service.WaitForStatus('Running', [TimeSpan]::FromSeconds($TimeoutSeconds))
Write-Output "$ServiceName restarted successfully"
#>
```

Parameters are auto-discovered: `ServiceName` (TextBox), `TimeoutSeconds` (Numeric), `Force` (Checkbox).

### How Parameter Auto-Discovery Works

Script cards analyze your script using PowerShell's Abstract Syntax Tree (AST) to discover:

- **Parameter names** and types
- **Mandatory** vs optional parameters
- **Default values**
- **Validation attributes** (e.g., `[ValidateSet]`, `[ValidateRange]`)

The appropriate UI control is automatically chosen:
- `[string]` → TextBox
- `[int]`, `[double]` → Numeric input
- `[switch]` → Checkbox
- `[ValidateSet]` → Dropdown
- `[securestring]` → Password field

### Security Considerations

- Scripts execute in **isolated runspaces**
- **No access** to the parent session's variables
- **File path validation** enforced for `ScriptPath`
- **Parameter sanitization** prevents injection attacks

### Tips and Best Practices

1. **Use clear parameter names** - They become UI labels
2. **Provide help comments** - Shown as tooltips in the dialog
3. **Set sensible defaults** - Makes the card easier to use
4. **Keep scripts focused** - One task per card
5. **Output status messages** - Use `Write-Output` for user feedback
6. **Handle errors gracefully** - Use try/catch blocks

---

## See Also

- [Dashboard Overview](./dashboards/about.md)
- [Branding Configuration](./configuration/branding.md)
- [Refresh Patterns](./dashboards/refresh.md)
- [Category Filtering](./dashboards/categories.md)
