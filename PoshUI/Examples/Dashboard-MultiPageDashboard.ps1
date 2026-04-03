# ==============================================================================
# Multi-Page Dashboard Demo - Complete Control Showcase
# Demonstrates ALL dashboard control types with custom navy/grey/red theme:
# - MetricCards with Gauges and Sparklines
# - GraphCards (Line, Bar, Donut, Pie)
# - StatusIndicatorCards
# - DataGridCards
# - InfoCards
# - ScriptCards
# - Carousel Banners on every page
# Compatible with PowerShell 5.1 and .NET Framework 4.8
# ==============================================================================

# Import the PoshUI.Dashboard module
$modulePath = Join-Path $PSScriptRoot '..\PoshUI.Dashboard\PoshUI.Dashboard.psd1'
Import-Module $modulePath -Force

Write-Host @'

+--------------------------------------------------------+
|  Multi-Page Dashboard - Complete Control Showcase     |
|  All Dashboard Controls with Navy/Grey/Red Theme       |
+--------------------------------------------------------+
'@ -ForegroundColor Cyan

Write-Host "`nShowcasing ALL dashboard controls with custom theme:" -ForegroundColor Yellow
Write-Host "  - Performance Metrics - Gauges, Sparklines, Status Cards" -ForegroundColor White
Write-Host "  - Analytics - All Chart Types (Bar, Line, Donut, Pie)" -ForegroundColor White
Write-Host "  - System Data - DataGrids and Tables" -ForegroundColor White
Write-Host "  - Automation - Interactive Script Cards" -ForegroundColor White
Write-Host "  - Custom Navy/Grey/Red Theme Applied" -ForegroundColor Magenta
Write-Host ""

# Get paths for branding assets - use Icon8 folder
$iconBase = Join-Path $PSScriptRoot 'Icon8'
$scriptIconPath = Join-Path $iconBase 'icons8-dashboard-100.png'
$sidebarIconPath = Join-Path $iconBase 'icons8-workstation-100.png'

# Verify branding assets exist
foreach ($assetPath in @($scriptIconPath, $sidebarIconPath)) {
    if (-not (Test-Path $assetPath)) {
        # Fallback to available icons if specific ones don't exist
        $scriptIconPath = Join-Path $iconBase 'icons8-monitor-100.png'
        $sidebarIconPath = Join-Path $iconBase 'icons8-imac-100.png'
    }
}

# Initialize the dashboard
New-PoshUIDashboard -Title 'Multi-Page Dashboard - Complete Showcase' `
    -Description 'Comprehensive dashboard demonstrating all control types' `
    -Theme 'Dark' `
    -Icon $scriptIconPath

Set-UIBranding -WindowTitle "Multi-Page Dashboard - Complete Showcase" `
    -WindowTitleIcon $scriptIconPath `
    -SidebarHeaderIcon $sidebarIconPath

# Apply Custom Navy/Grey/Red Theme
Set-UITheme @{
    AccentColor = '#DC2626'              # Crimson Red accent
    Background = '#0F172A'               # Dark navy background
    SidebarBackground = '#1B3A57'        # Navy blue sidebar
    ContentBackground = '#1E293B'        # Slate content area
    CardBackground = '#334155'           # Slate grey cards
    TitleBarBackground = '#1B3A57'       # Navy title bar
    TextPrimary = '#F1F5F9'              # Light text
    TextSecondary = '#CBD5E1'            # Grey text
    SidebarText = '#E2E8F0'              # Light sidebar text
    BorderColor = '#475569'              # Slate borders
    InputBackground = '#1E293B'          # Dark inputs
    TitleBarText = '#F1F5F9'             # Light title text
}

# ==============================================================================
# PAGE 1: Performance Metrics - Gauges, Sparklines, Status Cards
# ==============================================================================

Add-UIStep -Name 'PerformanceMetrics' -Title 'Performance Metrics' -Order 1 `
    -IconPath (Join-Path $iconBase 'icons8-graph-100.png') `
    -Type "Dashboard" -Description 'Real-time metrics with gauges, sparklines, and status indicators'

# Carousel banner with navy/red theme
$carouselItems = @(
    @{
        Title = 'Performance Dashboard'
        Subtitle = 'Real-time system monitoring with visual indicators'
        BackgroundColor = '#1B3A57'  # Navy
        LinkUrl = 'https://kanders-ii.github.io/PoshUI/'
        Clickable = $true
        IconPath = (Join-Path $iconBase 'icons8-graph-100.png')
    },
    @{
        Title = 'Gauge Controls'
        Subtitle = 'Radial gauges show metrics at a glance'
        BackgroundColor = '#DC2626'  # Crimson Red
        IconPath = (Join-Path $iconBase 'icons8-stopwatch-100.png')
    },
    @{
        Title = 'Sparkline Charts'
        Subtitle = 'Compact trend visualization in cards'
        BackgroundColor = '#4A5568'  # Steel Grey
        IconPath = (Join-Path $iconBase 'icons8-stocks-growth-100.png')
    }
)

Add-UIBanner -Step "PerformanceMetrics" -Name "PerfBanner" `
    -Title "Performance Metrics" `
    -CarouselSlides $carouselItems `
    -AutoRotate $true `
    -RotateInterval 4000 `
    -Height 180 `
    -Style 'Info'

# Context card
Add-UICard -Step "PerformanceMetrics" -Name "MetricsInfo" `
    -Title "About This Page" `
    -Content "This page showcases **MetricCards with Gauges and Sparklines**, plus **StatusIndicatorCards**. Gauges display radial progress rings, sparklines show compact trend data, and status cards provide quick health indicators with colored dots." `
    -Category "Info" `
    -IconPath (Join-Path $iconBase 'icons8-ask-question-100.png')

# CPU Usage with AUTO-SPARKLINE
Add-UIMetricCard -Step "PerformanceMetrics" -Name "CPUMetric" `
    -Title "CPU Usage" `
    -Value { (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average } `
    -Unit "%" `
    -Description "Processor utilization with trend" `
    -IconPath (Join-Path $iconBase 'icons8-powershell-100.png') `
    -ShowSparkline `
    -Category "Performance"

# Memory Usage with GAUGE
$os = Get-CimInstance Win32_OperatingSystem
$memoryPct = [math]::Round((($os.TotalVisibleMemorySize - $os.FreePhysicalMemory) / $os.TotalVisibleMemorySize) * 100, 1)
Add-UIMetricCard -Step "PerformanceMetrics" -Name "MemoryMetric" `
    -Title "Memory Usage" `
    -Value $memoryPct `
    -Unit "%" `
    -Description "RAM utilization" `
    -IconPath (Join-Path $iconBase 'icons8-memory-slot-100.png') `
    -ShowGauge `
    -MinValue 0 `
    -MaxValue 100 `
    -Category "Performance"

# Disk Usage with GAUGE
$disk = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'"
$diskPct = [math]::Round((($disk.Size - $disk.FreeSpace) / $disk.Size) * 100, 1)
Add-UIMetricCard -Step "PerformanceMetrics" -Name "DiskMetric" `
    -Title "Disk C: Usage" `
    -Value $diskPct `
    -Unit "%" `
    -Description "Storage utilization" `
    -IconPath (Join-Path $iconBase 'icons8-memory-slot-100.png') `
    -ShowGauge `
    -MinValue 0 `
    -MaxValue 100 `
    -Category "Performance"

# Network with static value (no auto-sparkline for static values)
Add-UIMetricCard -Step "PerformanceMetrics" -Name "NetworkMetric" `
    -Title "Network Activity" `
    -Value 245 `
    -Unit "Mbps" `
    -Description "Current throughput" `
    -IconPath (Join-Path $iconBase 'icons8-network-cable-100.png') `
    -Category "Performance"

# Battery with GAUGE and AUTO-SPARKLINE (simulated with random data)
Add-UIMetricCard -Step "PerformanceMetrics" -Name "BatteryMetric" `
    -Title "Battery Level" `
    -Value { Get-Random -Minimum 85 -Maximum 95 } `
    -Unit "%" `
    -Description "Power remaining (simulated)" `
    -IconPath (Join-Path $iconBase 'icons8-shutdown-100.png') `
    -ShowGauge `
    -ShowSparkline `
    -MinValue 0 `
    -MaxValue 100 `
    -Category "Performance"

# Response Time with AUTO-SPARKLINE (simulated with random data)
Add-UIMetricCard -Step "PerformanceMetrics" -Name "ResponseMetric" `
    -Title "Avg Response" `
    -Value { Get-Random -Minimum 120 -Maximum 180 } `
    -Unit "ms" `
    -Description "API latency (simulated)" `
    -IconPath (Join-Path $iconBase 'icons8-stopwatch-100.png') `
    -ShowSparkline `
    -Category "Performance"

# STATUS INDICATOR CARD - Service Health
$serviceStatus = @(
    @{ Label = 'Web Server'; Status = 'Online' }
    @{ Label = 'Database'; Status = 'Online' }
    @{ Label = 'API Gateway'; Status = 'Warning' }
    @{ Label = 'Cache'; Status = 'Online' }
    @{ Label = 'Queue'; Status = 'Offline' }
)

Add-UIStatusCard -Step "PerformanceMetrics" -Name "ServiceStatus" `
    -Title "Service Health" `
    -Data $serviceStatus `
    -IconPath (Join-Path $iconBase 'icons8-gears-100.png') `
    -Category "Status"

# STATUS INDICATOR CARD - Environment Status
$envStatus = @(
    @{ Label = 'Production'; Status = 'Online' }
    @{ Label = 'Staging'; Status = 'Online' }
    @{ Label = 'Development'; Status = 'Warning' }
)

Add-UIStatusCard -Step "PerformanceMetrics" -Name "EnvStatus" `
    -Title "Environment Status" `
    -Data $envStatus `
    -IconPath (Join-Path $iconBase 'icons8-earth-planet-100.png') `
    -Category "Status"


# ==============================================================================
# PAGE 2: Analytics - All Chart Types (Bar, Line, Donut, Pie)
# ==============================================================================

Add-UIStep -Name 'Analytics' -Title 'Analytics' -Order 2 `
    -IconPath (Join-Path $iconBase 'icons8-graph-100.png') `
    -Type "Dashboard" -Description 'Comprehensive chart visualization with all supported types'

# Carousel banner for Analytics page
$analyticsCarousel = @(
    @{
        Title = 'Data Visualization'
        Subtitle = 'Bar, Line, Donut, and Pie charts'
        BackgroundColor = '#DC2626'  # Crimson Red
        IconPath = (Join-Path $iconBase 'icons8-graph-100.png')
    },
    @{
        Title = 'Interactive Charts'
        Subtitle = 'Click and explore your data'
        BackgroundColor = '#1B3A57'  # Navy
        IconPath = (Join-Path $iconBase 'icons8-stocks-growth-100.png')
    },
    @{
        Title = 'Real-Time Updates'
        Subtitle = 'Refresh data on demand'
        BackgroundColor = '#4A5568'  # Steel Grey
        IconPath = (Join-Path $iconBase 'icons8-stopwatch-100.png')
    }
)

Add-UIBanner -Step "Analytics" -Name "AnalyticsBanner" `
    -Title "Analytics Dashboard" `
    -CarouselSlides $analyticsCarousel `
    -AutoRotate $true `
    -RotateInterval 4000 `
    -Height 180 `
    -Style 'Warning'

# Context card
Add-UICard -Step "Analytics" -Name "ChartsInfo" `
    -Title "Chart Types" `
    -Content "This page demonstrates **all chart types**: Bar charts for comparisons, Line charts for trends, Donut charts for proportions with center totals, and Pie charts for distribution. All charts support dynamic data and refresh capabilities." `
    -Category "Info" `
    -IconPath (Join-Path $iconBase 'icons8-ask-question-100.png')

# LINE CHART - CPU Trend
$cpuTrendData = @()
for ($i = 10; $i -ge 0; $i--) {
    $cpuTrendData += @{ Label = "-$i min"; Value = [math]::Round((Get-Random -Minimum 30 -Maximum 75), 1) }
}

Add-UIChartCard -Step "Analytics" -Name "CPULineChart" `
    -Title "CPU Usage Trend" `
    -ChartType "Line" `
    -Data $cpuTrendData `
    -IconPath (Join-Path $iconBase 'icons8-graph-100.png') `
    -Description "Processor utilization over time" `
    -Category "Line Charts"

# BAR CHART - Top Processes
$procs = Get-Process | Where-Object { $_.CPU -gt 0 } | Sort-Object CPU -Descending | Select-Object -First 8
$procData = $procs | ForEach-Object {
    @{ Label = $_.ProcessName.Substring(0, [Math]::Min(10, $_.ProcessName.Length)); Value = [math]::Round($_.CPU, 1) }
}

Add-UIChartCard -Step "Analytics" -Name "ProcessBarChart" `
    -Title "Top CPU Processes" `
    -ChartType "Bar" `
    -Data $procData `
    -IconPath (Join-Path $iconBase 'icons8-stocks-growth-100.png') `
    -Description "Processes by CPU usage" `
    -Category "Bar Charts"

# DONUT CHART - License Allocation
$licenseData = @(
    @{ Label = 'Office 365'; Value = 350 }
    @{ Label = 'Windows'; Value = 280 }
    @{ Label = 'Azure'; Value = 150 }
    @{ Label = 'Unused'; Value = 120 }
)

Add-UIChartCard -Step "Analytics" -Name "LicenseDonut" `
    -Title "License Allocation" `
    -ChartType "Donut" `
    -Data $licenseData `
    -IconPath (Join-Path $iconBase 'icons8-certificate-100.png') `
    -Description "By product" `
    -Category "Donut Charts"

# PIE CHART - Disk Distribution
$diskData = Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" | ForEach-Object {
    @{ Label = "$($_.DeviceID)"; Value = [math]::Round(($_.Size - $_.FreeSpace) / 1GB, 1) }
}

Add-UIChartCard -Step "Analytics" -Name "DiskPieChart" `
    -Title "Disk Usage Distribution" `
    -ChartType "Pie" `
    -Data $diskData `
    -IconPath (Join-Path $iconBase 'icons8-memory-slot-100.png') `
    -Description "Storage by drive" `
    -Category "Pie Charts"

# BAR CHART - Memory by Process
$memProcs = Get-Process | Sort-Object WorkingSet -Descending | Select-Object -First 8
$memData = $memProcs | ForEach-Object {
    @{ Label = $_.ProcessName.Substring(0, [Math]::Min(10, $_.ProcessName.Length)); Value = [math]::Round($_.WorkingSet / 1MB, 1) }
}

Add-UIChartCard -Step "Analytics" -Name "MemoryBarChart" `
    -Title "Memory Usage by Process" `
    -ChartType "Bar" `
    -Data $memData `
    -IconPath (Join-Path $iconBase 'icons8-stocks-growth-100.png') `
    -Description "Top processes by RAM" `
    -Category "Bar Charts"

# LINE CHART - Network Throughput
$networkTrendData = @()
for ($i = 12; $i -ge 0; $i--) {
    $networkTrendData += @{ Label = "-$i min"; Value = [math]::Round((Get-Random -Minimum 150 -Maximum 300), 1) }
}

Add-UIChartCard -Step "Analytics" -Name "NetworkLineChart" `
    -Title "Network Throughput" `
    -ChartType "Line" `
    -Data $networkTrendData `
    -IconPath (Join-Path $iconBase 'icons8-network-cable-100.png') `
    -Description "Bandwidth over time (Mbps)" `
    -Category "Line Charts"

# DONUT CHART - Service Distribution
$serviceDistData = @(
    @{ Label = 'Running'; Value = 145 }
    @{ Label = 'Stopped'; Value = 78 }
    @{ Label = 'Disabled'; Value = 32 }
)

Add-UIChartCard -Step "Analytics" -Name "ServiceDonut" `
    -Title "Windows Services" `
    -ChartType "Donut" `
    -Data $serviceDistData `
    -IconPath (Join-Path $iconBase 'icons8-gears-100.png') `
    -Description "By status" `
    -Category "Donut Charts"

# ==============================================================================
# PAGE 3: System Data - DataGrids and Tables
# ==============================================================================

Add-UIStep -Name 'SystemData' -Title 'System Data' -Order 3 `
    -IconPath (Join-Path $iconBase 'icons8-system-report-100.png') `
    -Type "Dashboard" -Description 'Tabular data views with DataGrid cards'

# Carousel banner for System Data page
$dataCarousel = @(
    @{
        Title = 'Tabular Data'
        Subtitle = 'DataGrid cards for structured information'
        BackgroundColor = '#4A5568'  # Steel Grey
        IconPath = (Join-Path $iconBase 'icons8-system-report-100.png')
    },
    @{
        Title = 'Sortable Columns'
        Subtitle = 'Click headers to sort data'
        BackgroundColor = '#1B3A57'  # Navy
        IconPath = (Join-Path $iconBase 'icons8-create-document-100.png')
    },
    @{
        Title = 'Live Updates'
        Subtitle = 'Refresh to see current system state'
        BackgroundColor = '#DC2626'  # Crimson Red
        IconPath = (Join-Path $iconBase 'icons8-advance-100.png')
    }
)

Add-UIBanner -Step "SystemData" -Name "DataBanner" `
    -Title "System Data" `
    -CarouselSlides $dataCarousel `
    -AutoRotate $true `
    -RotateInterval 4000 `
    -Height 180

# Context card
Add-UICard -Step "SystemData" -Name "DataGridInfo" `
    -Title "DataGrid Cards" `
    -Content "**DataGrid cards** display structured tabular data with sortable columns. Click column headers to sort, and use the scroll bar for large datasets. Perfect for process lists, service status, and system information." `
    -Category "Info" `
    -IconPath (Join-Path $iconBase 'icons8-ask-question-100.png')

# Summary Metrics
Add-UIMetricCard -Step "SystemData" -Name "ProcessCountMetric" `
    -Title "Running Processes" `
    -Value (Get-Process).Count `
    -Description "Total active processes" `
    -IconPath (Join-Path $iconBase 'icons8-stocks-growth-100.png') `
    -Category "Metrics"

Add-UIMetricCard -Step "SystemData" -Name "ServiceCountMetric" `
    -Title "Running Services" `
    -Value (Get-Service | Where-Object Status -eq 'Running').Count `
    -Description "Active Windows services" `
    -IconPath (Join-Path $iconBase 'icons8-gears-100.png') `
    -Category "Metrics"

Add-UIMetricCard -Step "SystemData" -Name "ConnectionsMetric" `
    -Title "Network Connections" `
    -Value (Get-NetTCPConnection -State Established -ErrorAction SilentlyContinue).Count `
    -Description "Established TCP connections" `
    -IconPath (Join-Path $iconBase 'icons8-network-cable-100.png') `
    -Category "Metrics"

# DATAGRID 1: Process Details
$processData = Get-Process | Select-Object -First 15 Name, Id, CPU, WorkingSet, Threads, HandleCount |
    Select-Object Name, Id, 
        @{N='CPU';E={[math]::Round($_.CPU, 2)}},
        @{N='MemoryMB';E={[math]::Round($_.WorkingSet/1MB, 2)}},
        @{N='Threads';E={$_.Threads.Count}},
        @{N='Handles';E={$_.HandleCount}}

Add-UITableCard -Step "SystemData" -Name "ProcessDataGrid" `
    -Title "Process Details" `
    -Data $processData `
    -IconPath (Join-Path $iconBase 'icons8-stocks-growth-100.png') `
    -Category "Process Data"

# DATAGRID 2: Services
$serviceData = Get-Service | Select-Object -First 20 DisplayName, Status, StartType, Name |
    Sort-Object Status -Descending

Add-UITableCard -Step "SystemData" -Name "ServicesDataGrid" `
    -Title "Windows Services" `
    -Data $serviceData `
    -IconPath (Join-Path $iconBase 'icons8-gears-100.png') `
    -Category "Service Data"

# DATAGRID 3: Network Connections
$networkData = Get-NetTCPConnection -ErrorAction SilentlyContinue | 
    Where-Object { $_.State -eq 'Established' } |
    Select-Object -First 15 LocalAddress, LocalPort, RemoteAddress, RemotePort, State, OwningProcess

Add-UITableCard -Step "SystemData" -Name "NetworkDataGrid" `
    -Title "Network Connections" `
    -Data $networkData `
    -IconPath (Join-Path $iconBase 'icons8-network-cable-100.png') `
    -Category "Network Data"

# ==============================================================================
# PAGE 4: Automation - Interactive Script Cards
# ==============================================================================

Add-UIStep -Name 'Automation' -Title 'Automation' -Order 4 `
    -IconPath (Join-Path $iconBase 'icons8-powershell-100.png') `
    -Type "Dashboard" -Description 'Interactive script execution with ScriptCards'

# Carousel banner for Automation page
$automationCarousel = @(
    @{
        Title = 'Script Automation'
        Subtitle = 'Execute PowerShell scripts from the dashboard'
        BackgroundColor = '#DC2626'  # Crimson Red
        IconPath = (Join-Path $iconBase 'icons8-powershell-100.png')
    },
    @{
        Title = 'Parameter Input'
        Subtitle = 'Dynamic forms for script parameters'
        BackgroundColor = '#4A5568'  # Steel Grey
        IconPath = (Join-Path $iconBase 'icons8-command-line-100.png')
    },
    @{
        Title = 'Real-Time Output'
        Subtitle = 'See script results instantly'
        BackgroundColor = '#1B3A57'  # Navy
        IconPath = (Join-Path $iconBase 'icons8-code-file-100.png')
    }
)

Add-UIBanner -Step "Automation" -Name "AutomationBanner" `
    -Title "Automation Dashboard" `
    -CarouselSlides $automationCarousel `
    -AutoRotate $true `
    -RotateInterval 4000 `
    -Height 180 `
    -Style 'Success'

# Context card
Add-UICard -Step "Automation" -Name "AutomationInfo" `
    -Title "Script Cards" `
    -Content "**ScriptCards** enable interactive PowerShell script execution directly from the dashboard. Parameters are auto-discovered and presented as input fields. Output appears in a console panel within the card. Perfect for automation tasks, diagnostics, and system management." `
    -Category "Info" `
    -IconPath (Join-Path $iconBase 'icons8-ask-question-100.png')



# SCRIPT CARD 1: System Information
Add-UIScriptCard -Step "Automation" -Name "SystemInfoScript" `
    -Title "Get System Info" `
    -Description "Display detailed system information" `
    -ScriptBlock {
        Write-Host "=== System Information ===" -ForegroundColor Cyan
        $os = Get-CimInstance Win32_OperatingSystem
        $cs = Get-CimInstance Win32_ComputerSystem
        Write-Host "`nComputer: $($cs.Name)" -ForegroundColor Yellow
        Write-Host "OS: $($os.Caption)" -ForegroundColor Yellow
        Write-Host "Version: $($os.Version)" -ForegroundColor Yellow
        Write-Host "Memory: $([math]::Round($cs.TotalPhysicalMemory/1GB, 2)) GB" -ForegroundColor Yellow
        Write-Host "Manufacturer: $($cs.Manufacturer)" -ForegroundColor Yellow
    } `
    -IconPath (Join-Path $iconBase 'icons8-system-report-100.png') `
    -Category "Diagnostics"


# SCRIPT CARD 2: Process Management
Add-UIScriptCard -Step "Automation" -Name "ProcessMgmtScript" `
    -Title "Process Manager" `
    -Description "List or manage processes" `
    -ScriptBlock {
        param(
            [Parameter(Mandatory=$true)]
            [string]$ProcessName = "notepad",
            
            [Parameter(Mandatory=$true)]
            [ValidateSet("List", "Start", "Stop")]
            [string]$Action = "List"
        )

        switch ($Action) {
            "List" {
                $processes = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
                if ($processes) {
                    Write-Host "Running processes named '$ProcessName':" -ForegroundColor Cyan
                    $processes | Select-Object Id, CPU, WorkingSet | Format-Table -AutoSize
                } else {
                    Write-Host "No processes found with name '$ProcessName'" -ForegroundColor Yellow
                }
            }
            "Start" {
                try {
                    Start-Process $ProcessName
                    Write-Host "Started process: $ProcessName" -ForegroundColor Green
                } catch {
                    Write-Host "Failed to start: $($_.Exception.Message)" -ForegroundColor Red
                }
            }
            "Stop" {
                try {
                    Stop-Process -Name $ProcessName -Force -ErrorAction Stop
                    Write-Host "Stopped process: $ProcessName" -ForegroundColor Yellow
                } catch {
                    Write-Host "Failed to stop: $($_.Exception.Message)" -ForegroundColor Red
                }
            }
        }
    } `
    -DefaultParameters @{ ProcessName = "notepad"; Action = "List" } `
    -IconPath (Join-Path $iconBase 'icons8-stocks-growth-100.png') `
    -Category "Management"


# SCRIPT CARD 3: Network Diagnostics
Add-UIScriptCard -Step "Automation" -Name "NetworkDiagScript" `
    -Title "Network Diagnostics" `
    -Description "Test network connectivity" `
    -ScriptBlock {
        param(
            [Parameter(Mandatory=$true)]
            [string]$Target = "8.8.8.8",
            
            [Parameter(Mandatory=$true)]
            [ValidateSet("Ping", "DNS", "Both")]
            [string]$TestType = "Both"
        )

        Write-Host "=== Network Diagnostics for $Target ===" -ForegroundColor Cyan

        if ($TestType -in @("Ping", "Both")) {
            Write-Host "`n--- Ping Test ---" -ForegroundColor Yellow
            Test-Connection -ComputerName $Target -Count 3 -ErrorAction SilentlyContinue
        }

        if ($TestType -in @("DNS", "Both")) {
            Write-Host "`n--- DNS Resolution ---" -ForegroundColor Yellow
            try {
                $dnsResult = Resolve-DnsName $Target -ErrorAction Stop
                $dnsResult | Select-Object Name, Type, IPAddress | Format-Table -AutoSize
            } catch {
                Write-Host "DNS resolution failed: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    } `
    -DefaultParameters @{ Target = "8.8.8.8"; TestType = "Both" } `
    -IconPath (Join-Path $iconBase 'icons8-network-cable-100.png') `
    -Category "Diagnostics"

# SCRIPT CARD 4: Service Management
Add-UIScriptCard -Step "Automation" -Name "ServiceMgmtScript" `
    -Title "Service Manager" `
    -Description "Check and manage Windows services" `
    -ScriptBlock {
        param(
            [Parameter(Mandatory=$true)]
            [string]$ServiceName = "Spooler",
            
            [Parameter(Mandatory=$true)]
            [ValidateSet("Status", "Start", "Stop", "Restart")]
            [string]$Action = "Status"
        )

        Write-Host "=== Service Management: $ServiceName ===" -ForegroundColor Cyan

        switch ($Action) {
            "Status" {
                $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
                if ($service) {
                    Write-Host "`nService: $($service.DisplayName)" -ForegroundColor Yellow
                    Write-Host "Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq 'Running') { 'Green' } else { 'Red' })
                    Write-Host "Start Type: $($service.StartType)" -ForegroundColor Yellow
                } else {
                    Write-Host "Service '$ServiceName' not found." -ForegroundColor Red
                }
            }
            "Start" {
                try {
                    Start-Service -Name $ServiceName -ErrorAction Stop
                    Write-Host "Service '$ServiceName' started successfully." -ForegroundColor Green
                } catch {
                    Write-Host "Failed to start service: $($_.Exception.Message)" -ForegroundColor Red
                }
            }
            "Stop" {
                try {
                    Stop-Service -Name $ServiceName -Force -ErrorAction Stop
                    Write-Host "Service '$ServiceName' stopped successfully." -ForegroundColor Yellow
                } catch {
                    Write-Host "Failed to stop service: $($_.Exception.Message)" -ForegroundColor Red
                }
            }
            "Restart" {
                try {
                    Restart-Service -Name $ServiceName -Force -ErrorAction Stop
                    Write-Host "Service '$ServiceName' restarted successfully." -ForegroundColor Green
                } catch {
                    Write-Host "Failed to restart service: $($_.Exception.Message)" -ForegroundColor Red
                }
            }
        }
    } `
    -DefaultParameters @{ ServiceName = "Spooler"; Action = "Status" } `
    -IconPath (Join-Path $iconBase 'icons8-gears-100.png') `
    -Category "Management"

# ===============================================================================
# Show the multi-page dashboard
# ===============================================================================

Write-Host "Launching Multi-Page Dashboard - Complete Showcase..." -ForegroundColor Green
Write-Host "  - Page 1: Performance Metrics (Gauges, Sparklines, Status Cards)" -ForegroundColor White
Write-Host "  - Page 2: Analytics (Bar, Line, Donut, Pie Charts)" -ForegroundColor White
Write-Host "  - Page 3: System Data (DataGrids and Tables)" -ForegroundColor White
Write-Host "  - Page 4: Automation (Interactive Script Cards)" -ForegroundColor White
Write-Host "  - Custom Navy/Grey/Red Theme Applied" -ForegroundColor Magenta
Write-Host ""

Show-PoshUIDashboard

