# ==============================================================================
# Computer Maintenance Dashboard - System Health, Diagnostics & Tools
# A sleek, professional maintenance dashboard using Icon8 assets.
# Features real-time system metrics, storage analytics, network/security
# status, and interactive maintenance script cards.
# Compatible with PowerShell 5.1 and .NET Framework 4.8
# ==============================================================================

# Import the PoshUI.Dashboard module
$modulePath = Join-Path $PSScriptRoot '..\PoshUI.Dashboard\PoshUI.Dashboard.psd1'
Import-Module $modulePath -Force

Write-Host @'

+--------------------------------------------------------+
|  Computer Maintenance Dashboard                        |
|  System Health, Diagnostics & Maintenance Tools        |
+--------------------------------------------------------+
'@ -ForegroundColor Cyan

Write-Host "`nLaunching maintenance dashboard with live system data:" -ForegroundColor Yellow
Write-Host "  - System Health: CPU, Memory, Disk, Uptime gauges" -ForegroundColor White
Write-Host "  - Storage & Analytics: Disk charts, process data" -ForegroundColor White
Write-Host "  - Network & Security: Connections, firewall, updates" -ForegroundColor White
Write-Host "  - Maintenance Tools: Cleanup, diagnostics, services" -ForegroundColor White
Write-Host ""

# ==============================================================================
# ICON8 ASSET PATHS
# ==============================================================================

$iconBase = Join-Path $PSScriptRoot 'Icon8'

# System & Hardware (used by Add-UIMetricCard, Add-UICard, Add-UIBanner, Add-UIStep which support -IconPath)
$iconMonitor       = Join-Path $iconBase 'icons8-monitor-100.png'
$iconWorkstation   = Join-Path $iconBase 'icons8-workstation-100.png'
$iconMemory        = Join-Path $iconBase 'icons8-memory-slot-100.png'
$iconGears         = Join-Path $iconBase 'icons8-gears-100.png'
$iconSettings      = Join-Path $iconBase 'icons8-settings-100.png'
$iconStopwatch     = Join-Path $iconBase 'icons8-stopwatch-100.png'
$iconImac          = Join-Path $iconBase 'icons8-imac-100.png'

# Storage & Files
$iconFolder        = Join-Path $iconBase 'icons8-folder-100.png'
$iconGraph         = Join-Path $iconBase 'icons8-graph-100.png'
$iconStocksGrowth  = Join-Path $iconBase 'icons8-stocks-growth-100.png'

# Network & Security
$iconWifi          = Join-Path $iconBase 'icons8-wi-fi-logo-100.png'
$iconDns           = Join-Path $iconBase 'icons8-dns-100.png'
$iconNetworkCable  = Join-Path $iconBase 'icons8-network-cable-100.png'
$iconSecuritySSL   = Join-Path $iconBase 'icons8-security-ssl-100.png'
$iconWarningShield = Join-Path $iconBase 'icons8-warning-shield-100.png'
$iconEarth         = Join-Path $iconBase 'icons8-earth-planet-100.png'

# Tools & Actions
$iconWrench        = Join-Path $iconBase 'icons8-wrench-100.png'
$iconCommandLine   = Join-Path $iconBase 'icons8-command-line-100.png'
$iconPowershell    = Join-Path $iconBase 'icons8-powershell-100.png'
$iconCheckMark     = Join-Path $iconBase 'icons8-check-mark-100.png'
$iconSysReport     = Join-Path $iconBase 'icons8-system-report-100.png'
$iconFullBin       = Join-Path $iconBase 'icons8-full-recycle-bin-100.png'
$iconSearch        = Join-Path $iconBase 'icons8-search-100.png'
$iconWarning       = Join-Path $iconBase 'icons8-general-warning-sign-100.png'
$iconInspection    = Join-Path $iconBase 'icons8-inspection-100.png'

# Status & Indicators
$iconSparkling     = Join-Path $iconBase 'icons8-sparkling-100.png'
$iconWindows       = Join-Path $iconBase 'icons8-windows-10-100.png'

# ==============================================================================
# INITIALIZE DASHBOARD
# ==============================================================================

New-PoshUIDashboard -Title 'Computer Maintenance Dashboard' `
    -Description 'System health monitoring, diagnostics, and maintenance tools' `
    -Theme 'Dark' `
    -Icon $iconMonitor

Set-UIBranding -WindowTitle "Computer Maintenance Dashboard" `
    -WindowTitleIcon $iconMonitor `
    -SidebarHeaderText "Maintenance" `
    -SidebarHeaderIcon $iconGears `
    -SidebarHeaderIconOrientation 'Top'

# ==============================================================================
# DUAL-MODE CUSTOM THEMES - Sleek Light & Dark
# ==============================================================================

# Light mode: Clean Arctic Blue / Teal
$lightTheme = @{
    AccentColor          = '#0097A7'
    AccentDark           = '#006978'
    AccentLight          = '#56C8D8'
    Background           = '#F0F4F8'
    ContentBackground    = '#FFFFFF'
    CardBackground       = '#FFFFFF'
    SidebarBackground    = '#0097A7'
    SidebarText          = '#FFFFFF'
    SidebarHighlight     = '#B2EBF2'
    TextPrimary          = '#1A2332'
    TextSecondary        = '#546E7A'
    ButtonBackground     = '#0097A7'
    ButtonForeground     = '#FFFFFF'
    InputBackground      = '#FFFFFF'
    InputBorder          = '#0097A7'
    BorderColor          = '#CFD8DC'
    TitleBarBackground   = '#0097A7'
    TitleBarText         = '#FFFFFF'
    SuccessColor         = '#2E7D32'
    WarningColor         = '#F57F17'
    ErrorColor           = '#C62828'
}

# Dark mode: Sleek Midnight Blue / Teal
$darkTheme = @{
    AccentColor          = '#00D1B2'
    AccentDark           = '#00A896'
    AccentLight          = '#5DFFD4'
    Background           = '#0B1622'
    ContentBackground    = '#111D2E'
    CardBackground       = '#172A3F'
    SidebarBackground    = '#0D1B2A'
    SidebarText          = '#A8D8EA'
    SidebarHighlight     = '#00D1B2'
    TextPrimary          = '#E8F1F8'
    TextSecondary        = '#7B9AB8'
    ButtonBackground     = '#00D1B2'
    ButtonForeground     = '#0B1622'
    InputBackground      = '#111D2E'
    InputBorder          = '#2A4060'
    BorderColor          = '#1E3450'
    TitleBarBackground   = '#0B1622'
    TitleBarText         = '#00D1B2'
    SuccessColor         = '#00E676'
    WarningColor         = '#FFAB40'
    ErrorColor           = '#FF5252'
}

Set-UITheme -Light $lightTheme -Dark $darkTheme

# ==============================================================================
# GATHER LIVE SYSTEM DATA
# ==============================================================================

$os = Get-CimInstance Win32_OperatingSystem
$cs = Get-CimInstance Win32_ComputerSystem
$cpu = Get-CimInstance Win32_Processor
$disk = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'"
$bios = Get-CimInstance Win32_BIOS

# Calculated values
$memoryPct = [math]::Round((($os.TotalVisibleMemorySize - $os.FreePhysicalMemory) / $os.TotalVisibleMemorySize) * 100, 1)
$memoryUsedGB = [math]::Round(($os.TotalVisibleMemorySize - $os.FreePhysicalMemory) / 1MB, 1)
$memoryTotalGB = [math]::Round($os.TotalVisibleMemorySize / 1MB, 1)
$diskPct = [math]::Round((($disk.Size - $disk.FreeSpace) / $disk.Size) * 100, 1)
$diskFreeGB = [math]::Round($disk.FreeSpace / 1GB, 1)
$diskTotalGB = [math]::Round($disk.Size / 1GB, 1)
$uptime = (Get-Date) - $os.LastBootUpTime
$uptimeDays = [math]::Round($uptime.TotalDays, 1)

# ==============================================================================
# PAGE 1: System Health Overview
# ==============================================================================

Add-UIStep -Name 'SystemHealth' -Title 'System Health' -Order 1 `
    -IconPath $iconMonitor `
    -Type "Dashboard" -Description 'Live CPU, memory, disk, and uptime metrics'

# Carousel banner
$healthCarousel = @(
    @{
        Title = 'System Health Monitor'
        Subtitle = "Real-time hardware metrics for $($cs.Name)"
        BackgroundColor = '#0D1B2A'
        GradientStart = '#0D1B2A'
        GradientEnd = '#00D1B2'
        IconPath = $iconMonitor
    },
    @{
        Title = 'Hardware Diagnostics'
        Subtitle = 'CPU, memory, storage, and thermal monitoring'
        BackgroundColor = '#00A896'
        GradientStart = '#00A896'
        GradientEnd = '#0D1B2A'
        IconPath = $iconWorkstation
    },
    @{
        Title = 'Performance Trends'
        Subtitle = 'Auto-refreshing sparklines track system behavior'
        BackgroundColor = '#1E3450'
        GradientStart = '#1E3450'
        GradientEnd = '#00D1B2'
        IconPath = $iconStocksGrowth
    }
)

Add-UIBanner -Step "SystemHealth" -Name "HealthBanner" `
    -Title "System Health Monitor" `
    -CarouselSlides $healthCarousel `
    -AutoRotate $true `
    -RotateInterval 5000 `
    -Height 170 `
    -TitleFontSize 28 `
    -TitleFontWeight "Bold" `
    -Style 'Info'

# CPU Usage - live gauge + sparkline
Add-UIMetricCard -Step "SystemHealth" -Name "CPUGauge" `
    -Title "CPU Usage" `
    -Value { (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average } `
    -Unit "%" `
    -Description "$($cpu.Name)" `
    -IconPath $iconGears `
    -ShowGauge `
    -ShowSparkline `
    -MinValue 0 `
    -MaxValue 100 `
    -Category "Performance"

# Memory Usage - gauge
Add-UIMetricCard -Step "SystemHealth" -Name "MemoryGauge" `
    -Title "Memory Usage" `
    -Value $memoryPct `
    -Unit "%" `
    -Description "$memoryUsedGB / $memoryTotalGB GB used" `
    -IconPath $iconMemory `
    -ShowGauge `
    -MinValue 0 `
    -MaxValue 100 `
    -Category "Performance"

# Disk C: Usage - gauge
Add-UIMetricCard -Step "SystemHealth" -Name "DiskGauge" `
    -Title "Disk C: Usage" `
    -Value $diskPct `
    -Unit "%" `
    -Description "$diskFreeGB GB free of $diskTotalGB GB" `
    -IconPath $iconFolder `
    -ShowGauge `
    -MinValue 0 `
    -MaxValue 100 `
    -Category "Storage"

# System Uptime
Add-UIMetricCard -Step "SystemHealth" -Name "UptimeMetric" `
    -Title "System Uptime" `
    -Value $uptimeDays `
    -Unit "days" `
    -Description "Last boot: $($os.LastBootUpTime.ToString('MMM dd, yyyy HH:mm'))" `
    -IconPath $iconStopwatch `
    -Category "System"

# Running Processes
Add-UIMetricCard -Step "SystemHealth" -Name "ProcessCount" `
    -Title "Processes" `
    -Value (Get-Process).Count `
    -Description "Active system processes" `
    -IconPath $iconCommandLine `
    -Category "System"

# Active Services
Add-UIMetricCard -Step "SystemHealth" -Name "ServiceCount" `
    -Title "Running Services" `
    -Value (Get-Service | Where-Object Status -eq 'Running').Count `
    -Description "Windows services active" `
    -IconPath $iconCheckMark `
    -Category "System"

# STATUS: Key Services Health
$criticalServices = @(
    @{ Label = 'Windows Update'; Status = (Get-Service wuauserv -ErrorAction SilentlyContinue).Status.ToString() -replace 'Running','Online' -replace 'Stopped','Offline' }
    @{ Label = 'Windows Defender'; Status = (Get-Service WinDefend -ErrorAction SilentlyContinue).Status.ToString() -replace 'Running','Online' -replace 'Stopped','Offline' }
    @{ Label = 'Windows Firewall'; Status = (Get-Service MpsSvc -ErrorAction SilentlyContinue).Status.ToString() -replace 'Running','Online' -replace 'Stopped','Offline' }
    @{ Label = 'DHCP Client'; Status = (Get-Service Dhcp -ErrorAction SilentlyContinue).Status.ToString() -replace 'Running','Online' -replace 'Stopped','Offline' }
    @{ Label = 'DNS Client'; Status = (Get-Service Dnscache -ErrorAction SilentlyContinue).Status.ToString() -replace 'Running','Online' -replace 'Stopped','Offline' }
    @{ Label = 'Print Spooler'; Status = (Get-Service Spooler -ErrorAction SilentlyContinue).Status.ToString() -replace 'Running','Online' -replace 'Stopped','Offline' }
)

Add-UIStatusCard -Step "SystemHealth" -Name "CriticalServices" `
    -Title "Critical Services" `
    -Data $criticalServices `
    -Icon "&#xE9F3;" `
    -Category "Health"

# System Info Card
Add-UICard -Step "SystemHealth" -Name "SystemInfoCard" `
    -Title "System Information" `
    -IconPath $iconImac `
    -Content @"
Computer: $($cs.Name)
OS: $($os.Caption)
Version: $($os.Version) Build $($os.BuildNumber)
Manufacturer: $($cs.Manufacturer)
Model: $($cs.Model)
BIOS: $($bios.SMBIOSBIOSVersion)
CPU: $($cpu.Name)
Cores: $($cpu.NumberOfCores) cores / $($cpu.NumberOfLogicalProcessors) threads
RAM: $memoryTotalGB GB
"@

# ==============================================================================
# PAGE 2: Storage & Process Analytics
# ==============================================================================

Add-UIStep -Name 'StorageAnalytics' -Title 'Storage & Analytics' -Order 2 `
    -IconPath $iconGraph `
    -Type "Dashboard" -Description 'Disk usage charts, top processes, and storage breakdown'

# Banner
Add-UIBanner -Step "StorageAnalytics" -Name "StorageBanner" `
    -Title "Storage & Process Analytics" `
    -Subtitle "Disk utilization, top resource consumers, and storage distribution" `
    -Height 160 `
    -TitleFontSize 26 `
    -TitleFontWeight "Bold" `
    -GradientStart '#0D1B2A' `
    -GradientEnd '#00897B' `
    -IconPath $iconGraph `
    -IconPosition 'Right' `
    -IconSize 70

# DONUT: Disk Space Used vs Free
$diskDonutData = @(
    @{ Label = 'Used'; Value = [math]::Round(($disk.Size - $disk.FreeSpace) / 1GB, 1) }
    @{ Label = 'Free'; Value = [math]::Round($disk.FreeSpace / 1GB, 1) }
)

Add-UIChartCard -Step "StorageAnalytics" -Name "DiskDonut" `
    -Title "Disk C: Space" `
    -ChartType "Donut" `
    -Data $diskDonutData `
    -Icon "&#xEDA2;" `
    -Description "Used vs Free (GB)" `
    -Category "Storage"

# PIE: All Drives Distribution
$allDiskData = Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" | ForEach-Object {
    @{ Label = "$($_.DeviceID) ($([math]::Round($_.Size/1GB))GB)"; Value = [math]::Round(($_.Size - $_.FreeSpace) / 1GB, 1) }
}

Add-UIChartCard -Step "StorageAnalytics" -Name "AllDiskPie" `
    -Title "Storage by Drive" `
    -ChartType "Pie" `
    -Data $allDiskData `
    -Icon "&#xE8B7;" `
    -Description "Used space per volume" `
    -Category "Storage"

# BAR: Top CPU Processes
$topCpuProcs = Get-Process | Where-Object { $_.CPU -gt 0 } | Sort-Object CPU -Descending | Select-Object -First 8
$cpuProcData = $topCpuProcs | ForEach-Object {
    @{ Label = $_.ProcessName.Substring(0, [Math]::Min(12, $_.ProcessName.Length)); Value = [math]::Round($_.CPU, 1) }
}

Add-UIChartCard -Step "StorageAnalytics" -Name "TopCpuBar" `
    -Title "Top CPU Consumers" `
    -ChartType "Bar" `
    -Data $cpuProcData `
    -Icon "&#xE950;" `
    -Description "CPU seconds by process" `
    -Category "Processes"

# BAR: Top Memory Processes
$topMemProcs = Get-Process | Sort-Object WorkingSet -Descending | Select-Object -First 8
$memProcData = $topMemProcs | ForEach-Object {
    @{ Label = $_.ProcessName.Substring(0, [Math]::Min(12, $_.ProcessName.Length)); Value = [math]::Round($_.WorkingSet / 1MB, 1) }
}

Add-UIChartCard -Step "StorageAnalytics" -Name "TopMemBar" `
    -Title "Top Memory Consumers" `
    -ChartType "Bar" `
    -Data $memProcData `
    -Icon "&#xE964;" `
    -Description "Working set (MB)" `
    -Category "Processes"

# LINE: CPU Trend (simulated recent history)
$cpuTrendData = @()
for ($i = 10; $i -ge 0; $i--) {
    $cpuTrendData += @{ Label = "-$i min"; Value = [math]::Round((Get-Random -Minimum 20 -Maximum 70), 1) }
}

Add-UIChartCard -Step "StorageAnalytics" -Name "CpuTrend" `
    -Title "CPU Trend (Simulated)" `
    -ChartType "Line" `
    -Data $cpuTrendData `
    -Icon "&#xE9D2;" `
    -Description "Processor load over time" `
    -Category "Trends"

# DATAGRID: Process Details
$processGrid = Get-Process | Select-Object -First 20 Name, Id, CPU, WorkingSet, Threads, HandleCount |
    Select-Object Name, Id,
        @{N='CPU';E={[math]::Round($_.CPU, 2)}},
        @{N='MemoryMB';E={[math]::Round($_.WorkingSet/1MB, 2)}},
        @{N='Threads';E={$_.Threads.Count}},
        @{N='Handles';E={$_.HandleCount}}

Add-UITableCard -Step "StorageAnalytics" -Name "ProcessGrid" `
    -Title "Process Details" `
    -Data $processGrid `
    -Icon "&#xE756;" `
    -Category "Processes"

# ==============================================================================
# PAGE 3: Network & Security
# ==============================================================================

Add-UIStep -Name 'NetworkSecurity' -Title 'Network & Security' -Order 3 `
    -IconPath $iconWifi `
    -Type "Dashboard" -Description 'Network connections, firewall status, and security posture'

# Banner
$netCarousel = @(
    @{
        Title = 'Network Overview'
        Subtitle = 'Active connections and adapter status'
        BackgroundColor = '#0D1B2A'
        GradientStart = '#0D1B2A'
        GradientEnd = '#1565C0'
        IconPath = $iconNetworkCable
    },
    @{
        Title = 'Security Posture'
        Subtitle = 'Firewall, defender, and encryption status'
        BackgroundColor = '#1B5E20'
        GradientStart = '#1B5E20'
        GradientEnd = '#00D1B2'
        IconPath = $iconSecuritySSL
    }
)

Add-UIBanner -Step "NetworkSecurity" -Name "NetBanner" `
    -Title "Network & Security" `
    -CarouselSlides $netCarousel `
    -AutoRotate $true `
    -RotateInterval 5000 `
    -Height 170 `
    -TitleFontSize 26 `
    -Style 'Info'

# Established Connections Count
$tcpConnections = Get-NetTCPConnection -State Established -ErrorAction SilentlyContinue
$tcpCount = if ($tcpConnections) { $tcpConnections.Count } else { 0 }

Add-UIMetricCard -Step "NetworkSecurity" -Name "TCPConnections" `
    -Title "TCP Connections" `
    -Value $tcpCount `
    -Description "Established connections" `
    -IconPath $iconNetworkCable `
    -Category "Network"

# Listening Ports
$listenPorts = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue
$listenCount = if ($listenPorts) { $listenPorts.Count } else { 0 }

Add-UIMetricCard -Step "NetworkSecurity" -Name "ListeningPorts" `
    -Title "Listening Ports" `
    -Value $listenCount `
    -Description "Open TCP listeners" `
    -IconPath $iconDns `
    -Category "Network"

# Firewall Status
$fwProfiles = Get-NetFirewallProfile -ErrorAction SilentlyContinue
$fwEnabled = if ($fwProfiles) { ($fwProfiles | Where-Object Enabled -eq $true).Count } else { 0 }

Add-UIMetricCard -Step "NetworkSecurity" -Name "FirewallProfiles" `
    -Title "Firewall Profiles" `
    -Value "$fwEnabled / 3" `
    -Description "Active firewall profiles" `
    -IconPath $iconWarningShield `
    -Category "Security"

# Network Adapter Info
$activeAdapter = Get-NetAdapter -ErrorAction SilentlyContinue | Where-Object Status -eq 'Up' | Select-Object -First 1
$adapterIP = if ($activeAdapter) {
    (Get-NetIPAddress -InterfaceIndex $activeAdapter.InterfaceIndex -AddressFamily IPv4 -ErrorAction SilentlyContinue | Select-Object -First 1).IPAddress
} else { 'N/A' }

Add-UIMetricCard -Step "NetworkSecurity" -Name "IPAddress" `
    -Title "IP Address" `
    -Value $adapterIP `
    -Description $(if ($activeAdapter) { "$($activeAdapter.Name) - $($activeAdapter.LinkSpeed)" } else { "No active adapter" }) `
    -IconPath $iconEarth `
    -Category "Network"

# STATUS: Security Services
$securityStatus = @(
    @{ Label = 'Windows Defender'; Status = (Get-Service WinDefend -ErrorAction SilentlyContinue).Status.ToString() -replace 'Running','Online' -replace 'Stopped','Offline' }
    @{ Label = 'Firewall Service'; Status = (Get-Service MpsSvc -ErrorAction SilentlyContinue).Status.ToString() -replace 'Running','Online' -replace 'Stopped','Offline' }
    @{ Label = 'Windows Update'; Status = (Get-Service wuauserv -ErrorAction SilentlyContinue).Status.ToString() -replace 'Running','Online' -replace 'Stopped','Offline' }
    @{ Label = 'Cryptographic Svc'; Status = (Get-Service CryptSvc -ErrorAction SilentlyContinue).Status.ToString() -replace 'Running','Online' -replace 'Stopped','Offline' }
    @{ Label = 'Security Center'; Status = (Get-Service wscsvc -ErrorAction SilentlyContinue).Status.ToString() -replace 'Running','Online' -replace 'Stopped','Offline' }
)

Add-UIStatusCard -Step "NetworkSecurity" -Name "SecurityServices" `
    -Title "Security Services" `
    -Data $securityStatus `
    -Icon "&#xE72E;" `
    -Category "Security"

# STATUS: Firewall Profiles Detail
$fwStatusData = @()
if ($fwProfiles) {
    foreach ($fw in $fwProfiles) {
        $fwStatusData += @{ Label = $fw.Name; Status = if ($fw.Enabled) { 'Online' } else { 'Offline' } }
    }
}

if ($fwStatusData.Count -gt 0) {
    Add-UIStatusCard -Step "NetworkSecurity" -Name "FirewallStatus" `
        -Title "Firewall Profiles" `
        -Data $fwStatusData `
        -Icon "&#xED63;" `
        -Category "Security"
}

# DONUT: Connection States
$connStates = Get-NetTCPConnection -ErrorAction SilentlyContinue | Group-Object State | ForEach-Object {
    @{ Label = $_.Name; Value = $_.Count }
} | Sort-Object { $_.Value } -Descending | Select-Object -First 6

if ($connStates) {
    Add-UIChartCard -Step "NetworkSecurity" -Name "ConnStatesDonut" `
        -Title "Connection States" `
        -ChartType "Donut" `
        -Data $connStates `
        -Icon "&#xE968;" `
        -Description "TCP connection breakdown" `
        -Category "Network"
}

# DATAGRID: Active Connections
$netGrid = Get-NetTCPConnection -ErrorAction SilentlyContinue |
    Where-Object { $_.State -eq 'Established' } |
    Select-Object -First 15 LocalAddress, LocalPort, RemoteAddress, RemotePort, State, OwningProcess

if ($netGrid) {
    Add-UITableCard -Step "NetworkSecurity" -Name "NetConnGrid" `
        -Title "Active Connections" `
        -Data $netGrid `
        -Icon "&#xE968;" `
        -Category "Network"
}

# Network Info Card
Add-UICard -Step "NetworkSecurity" -Name "NetInfoCard" `
    -Title "Network Configuration" `
    -IconPath $iconDns `
    -Content @"
Adapter: $(if ($activeAdapter) { $activeAdapter.Name } else { 'N/A' })
IP Address: $adapterIP
Link Speed: $(if ($activeAdapter) { $activeAdapter.LinkSpeed } else { 'N/A' })
MAC Address: $(if ($activeAdapter) { $activeAdapter.MacAddress } else { 'N/A' })
DNS Servers: $((Get-DnsClientServerAddress -InterfaceIndex $(if ($activeAdapter) { $activeAdapter.InterfaceIndex } else { 0 }) -AddressFamily IPv4 -ErrorAction SilentlyContinue | Select-Object -First 1).ServerAddresses -join ', ')
"@

# ==============================================================================
# PAGE 4: Maintenance Tools
# ==============================================================================

Add-UIStep -Name 'MaintenanceTools' -Title 'Maintenance Tools' -Order 4 `
    -IconPath $iconWrench `
    -Type "Dashboard" -Description 'Interactive cleanup, diagnostics, and service management'

# Banner
$toolsCarousel = @(
    @{
        Title = 'Maintenance Toolkit'
        Subtitle = 'One-click system cleanup, diagnostics, and repairs'
        BackgroundColor = '#0D1B2A'
        GradientStart = '#0D1B2A'
        GradientEnd = '#E65100'
        IconPath = $iconWrench
    },
    @{
        Title = 'Automated Diagnostics'
        Subtitle = 'Run health checks and generate reports instantly'
        BackgroundColor = '#1E3450'
        GradientStart = '#1E3450'
        GradientEnd = '#00D1B2'
        IconPath = $iconSysReport
    },
    @{
        Title = 'Service Management'
        Subtitle = 'Start, stop, and restart Windows services'
        BackgroundColor = '#004D40'
        GradientStart = '#004D40'
        GradientEnd = '#00D1B2'
        IconPath = $iconSettings
    }
)

Add-UIBanner -Step "MaintenanceTools" -Name "ToolsBanner" `
    -Title "Maintenance Toolkit" `
    -CarouselSlides $toolsCarousel `
    -AutoRotate $true `
    -RotateInterval 5000 `
    -Height 170 `
    -TitleFontSize 26 `
    -Style 'Success'

# SCRIPT CARD 1: Disk Cleanup Analysis
Add-UIScriptCard -Step "MaintenanceTools" -Name "DiskCleanup" `
    -Title "Disk Cleanup Analysis" `
    -Description "Scan for temporary files, caches, and reclaimable space" `
    -Icon "&#xE74D;" `
    -IconPath $iconFullBin `
    -Category "Cleanup" `
    -ScriptBlock {
        param(
            [Parameter(Mandatory=$true)]
            [ValidateSet('C', 'D', 'E')]
            [string]$DriveLetter = "C"
        )

        Write-Host "=== Disk Cleanup Analysis for $DriveLetter`:\ ===" -ForegroundColor Cyan
        Write-Host ""

        $drive = Get-PSDrive -Name $DriveLetter -ErrorAction SilentlyContinue
        if (-not $drive) {
            Write-Host "Drive $DriveLetter not found!" -ForegroundColor Red
            return
        }

        $freeGB = [math]::Round($drive.Free / 1GB, 2)
        $usedGB = [math]::Round($drive.Used / 1GB, 2)
        $totalGB = $usedGB + $freeGB
        Write-Host "Drive $DriveLetter`: $usedGB GB used / $totalGB GB total ($([math]::Round($freeGB/$totalGB*100,1))% free)" -ForegroundColor White
        Write-Host ""

        Write-Host "Scanning reclaimable space..." -ForegroundColor Yellow
        Write-Host ""

        $tempUser = "$env:TEMP"
        $tempWin = "$env:SystemRoot\Temp"
        $prefetch = "$env:SystemRoot\Prefetch"

        $tempUserSize = 0
        $tempWinSize = 0
        $prefetchSize = 0

        if (Test-Path $tempUser) {
            $tempUserSize = (Get-ChildItem $tempUser -Recurse -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
        }
        if (Test-Path $tempWin) {
            $tempWinSize = (Get-ChildItem $tempWin -Recurse -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
        }
        if (Test-Path $prefetch) {
            $prefetchSize = (Get-ChildItem $prefetch -Recurse -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
        }

        Write-Host "  User Temp:     $([math]::Round($tempUserSize/1MB, 2)) MB" -ForegroundColor White
        Write-Host "  Windows Temp:  $([math]::Round($tempWinSize/1MB, 2)) MB" -ForegroundColor White
        Write-Host "  Prefetch:      $([math]::Round($prefetchSize/1MB, 2)) MB" -ForegroundColor White

        $totalReclaimable = [math]::Round(($tempUserSize + $tempWinSize + $prefetchSize) / 1MB, 2)
        Write-Host ""
        Write-Host "  Total Reclaimable: $totalReclaimable MB" -ForegroundColor Green
        Write-Host ""
        Write-Host "Run Disk Cleanup (cleanmgr.exe) to safely remove these files." -ForegroundColor DarkGray
    } `
    -DefaultParameters @{ DriveLetter = "C" }

# SCRIPT CARD 2: System Health Check
Add-UIScriptCard -Step "MaintenanceTools" -Name "HealthCheck" `
    -Title "System Health Check" `
    -Description "Run a comprehensive system health diagnostic" `
    -Icon "&#xE9D9;" `
    -IconPath $iconInspection `
    -Category "Diagnostics" `
    -ScriptBlock {
        Write-Host "=== System Health Check ===" -ForegroundColor Cyan
        Write-Host ""

        # OS Info
        $os = Get-CimInstance Win32_OperatingSystem
        $cs = Get-CimInstance Win32_ComputerSystem
        Write-Host "[OS] $($os.Caption) (Build $($os.BuildNumber))" -ForegroundColor Yellow

        # Uptime
        $uptime = (Get-Date) - $os.LastBootUpTime
        $uptimeStr = "$([math]::Floor($uptime.TotalDays))d $($uptime.Hours)h $($uptime.Minutes)m"
        if ($uptime.TotalDays -gt 30) {
            Write-Host "[UPTIME] $uptimeStr - Consider rebooting!" -ForegroundColor Red
        } else {
            Write-Host "[UPTIME] $uptimeStr" -ForegroundColor Green
        }

        # Memory
        $memPct = [math]::Round((($os.TotalVisibleMemorySize - $os.FreePhysicalMemory) / $os.TotalVisibleMemorySize) * 100, 1)
        if ($memPct -gt 90) {
            Write-Host "[MEMORY] $memPct% used - HIGH PRESSURE!" -ForegroundColor Red
        } elseif ($memPct -gt 75) {
            Write-Host "[MEMORY] $memPct% used - Moderate" -ForegroundColor Yellow
        } else {
            Write-Host "[MEMORY] $memPct% used - Healthy" -ForegroundColor Green
        }

        # Disk
        $disk = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'"
        $diskPct = [math]::Round((($disk.Size - $disk.FreeSpace) / $disk.Size) * 100, 1)
        $freeGB = [math]::Round($disk.FreeSpace / 1GB, 1)
        if ($diskPct -gt 90) {
            Write-Host "[DISK C:] $diskPct% used ($freeGB GB free) - CRITICAL!" -ForegroundColor Red
        } elseif ($diskPct -gt 80) {
            Write-Host "[DISK C:] $diskPct% used ($freeGB GB free) - Warning" -ForegroundColor Yellow
        } else {
            Write-Host "[DISK C:] $diskPct% used ($freeGB GB free) - Healthy" -ForegroundColor Green
        }

        # Critical Services
        Write-Host ""
        Write-Host "--- Critical Services ---" -ForegroundColor Cyan
        $services = @('wuauserv','WinDefend','MpsSvc','Dhcp','Dnscache','EventLog')
        foreach ($svc in $services) {
            $s = Get-Service -Name $svc -ErrorAction SilentlyContinue
            if ($s) {
                $color = if ($s.Status -eq 'Running') { 'Green' } else { 'Red' }
                Write-Host "  $($s.DisplayName): $($s.Status)" -ForegroundColor $color
            }
        }

        Write-Host ""
        Write-Host "Health check complete." -ForegroundColor Cyan
    }

# SCRIPT CARD 3: Network Diagnostics
Add-UIScriptCard -Step "MaintenanceTools" -Name "NetDiag" `
    -Title "Network Diagnostics" `
    -Description "Test connectivity, DNS, and gateway reachability" `
    -Icon "&#xE774;" `
    -IconPath $iconEarth `
    -Category "Diagnostics" `
    -ScriptBlock {
        param(
            [Parameter(Mandatory=$true)]
            [string]$Target = "8.8.8.8",

            [Parameter(Mandatory=$true)]
            [ValidateSet("Ping", "DNS", "Both")]
            [string]$TestType = "Both"
        )

        Write-Host "=== Network Diagnostics: $Target ===" -ForegroundColor Cyan
        Write-Host ""

        if ($TestType -in @("Ping", "Both")) {
            Write-Host "--- Ping Test ---" -ForegroundColor Yellow
            $ping = Test-Connection -ComputerName $Target -Count 4 -ErrorAction SilentlyContinue
            if ($ping) {
                $avg = [math]::Round(($ping | Measure-Object ResponseTime -Average).Average, 1)
                Write-Host "  Reply from $Target" -ForegroundColor Green
                Write-Host "  Avg latency: $avg ms" -ForegroundColor Green
                Write-Host "  Packets: 4 sent, $($ping.Count) received" -ForegroundColor White
            } else {
                Write-Host "  No response from $Target" -ForegroundColor Red
            }
            Write-Host ""
        }

        if ($TestType -in @("DNS", "Both")) {
            Write-Host "--- DNS Resolution ---" -ForegroundColor Yellow
            try {
                $dns = Resolve-DnsName $Target -ErrorAction Stop
                $dns | Select-Object Name, Type, IPAddress | Format-Table -AutoSize
            } catch {
                Write-Host "  DNS resolution failed: $($_.Exception.Message)" -ForegroundColor Red
            }
        }

        # Gateway check
        Write-Host "--- Default Gateway ---" -ForegroundColor Yellow
        $gw = (Get-NetRoute -DestinationPrefix '0.0.0.0/0' -ErrorAction SilentlyContinue | Select-Object -First 1).NextHop
        if ($gw) {
            $gwPing = Test-Connection -ComputerName $gw -Count 2 -ErrorAction SilentlyContinue
            if ($gwPing) {
                Write-Host "  Gateway $gw is reachable" -ForegroundColor Green
            } else {
                Write-Host "  Gateway $gw is NOT responding" -ForegroundColor Red
            }
        } else {
            Write-Host "  No default gateway found" -ForegroundColor Yellow
        }
    } `
    -DefaultParameters @{ Target = "8.8.8.8"; TestType = "Both" }

# SCRIPT CARD 4: Service Manager
Add-UIScriptCard -Step "MaintenanceTools" -Name "ServiceMgr" `
    -Title "Service Manager" `
    -Description "Check status or restart Windows services" `
    -Icon "&#xE713;" `
    -IconPath $iconSettings `
    -Category "Management" `
    -ScriptBlock {
        param(
            [Parameter(Mandatory=$true)]
            [string]$ServiceName = "Spooler",

            [Parameter(Mandatory=$true)]
            [ValidateSet("Status", "Start", "Stop", "Restart")]
            [string]$Action = "Status"
        )

        Write-Host "=== Service Manager: $ServiceName ===" -ForegroundColor Cyan
        Write-Host ""

        $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if (-not $svc) {
            Write-Host "Service '$ServiceName' not found." -ForegroundColor Red
            Write-Host ""
            Write-Host "Did you mean one of these?" -ForegroundColor Yellow
            Get-Service | Where-Object { $_.Name -like "*$ServiceName*" -or $_.DisplayName -like "*$ServiceName*" } |
                Select-Object -First 5 Name, DisplayName, Status | Format-Table -AutoSize
            return
        }

        switch ($Action) {
            "Status" {
                $color = if ($svc.Status -eq 'Running') { 'Green' } else { 'Red' }
                Write-Host "  Name:       $($svc.Name)" -ForegroundColor White
                Write-Host "  Display:    $($svc.DisplayName)" -ForegroundColor White
                Write-Host "  Status:     $($svc.Status)" -ForegroundColor $color
                Write-Host "  Start Type: $($svc.StartType)" -ForegroundColor White
            }
            "Start" {
                try {
                    Start-Service -Name $ServiceName -ErrorAction Stop
                    Write-Host "Service '$ServiceName' started successfully." -ForegroundColor Green
                } catch {
                    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
                }
            }
            "Stop" {
                try {
                    Stop-Service -Name $ServiceName -Force -ErrorAction Stop
                    Write-Host "Service '$ServiceName' stopped." -ForegroundColor Yellow
                } catch {
                    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
                }
            }
            "Restart" {
                try {
                    Restart-Service -Name $ServiceName -Force -ErrorAction Stop
                    Write-Host "Service '$ServiceName' restarted successfully." -ForegroundColor Green
                } catch {
                    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
                }
            }
        }
    } `
    -DefaultParameters @{ ServiceName = "Spooler"; Action = "Status" }

# SCRIPT CARD 5: Installed Software Audit
Add-UIScriptCard -Step "MaintenanceTools" -Name "SoftwareAudit" `
    -Title "Installed Software Audit" `
    -Description "List installed applications with version info" `
    -Icon "&#xE721;" `
    -IconPath $iconSearch `
    -Category "Inventory" `
    -ScriptBlock {
        param(
            [Parameter(Mandatory=$true)]
            [string]$Filter = "*"
        )

        Write-Host "=== Installed Software Audit ===" -ForegroundColor Cyan
        Write-Host "Filter: $Filter" -ForegroundColor DarkGray
        Write-Host ""

        $apps = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
                                 "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*" -ErrorAction SilentlyContinue |
            Where-Object { $_.DisplayName -and $_.DisplayName -like $Filter } |
            Sort-Object DisplayName |
            Select-Object DisplayName, DisplayVersion, Publisher, InstallDate

        if ($apps) {
            Write-Host "Found $($apps.Count) application(s):" -ForegroundColor Green
            Write-Host ""
            $apps | Format-Table -AutoSize -Wrap
        } else {
            Write-Host "No applications found matching '$Filter'" -ForegroundColor Yellow
        }
    } `
    -DefaultParameters @{ Filter = "*" }

# SCRIPT CARD 6: Event Log Scanner
Add-UIScriptCard -Step "MaintenanceTools" -Name "EventScanner" `
    -Title "Event Log Scanner" `
    -Description "Scan recent error and warning events" `
    -Icon "&#xE7BA;" `
    -IconPath $iconWarning `
    -Category "Diagnostics" `
    -ScriptBlock {
        param(
            [Parameter(Mandatory=$true)]
            [ValidateSet("System", "Application", "Security")]
            [string]$LogName = "System",

            [Parameter(Mandatory=$true)]
            [ValidateSet("Error", "Warning", "Both")]
            [string]$Level = "Both",

            [Parameter(Mandatory=$true)]
            [int]$Hours = 24
        )

        Write-Host "=== Event Log Scanner ===" -ForegroundColor Cyan
        Write-Host "Log: $LogName | Level: $Level | Last $Hours hours" -ForegroundColor DarkGray
        Write-Host ""

        $after = (Get-Date).AddHours(-$Hours)
        $levels = switch ($Level) {
            "Error"   { @(1,2) }
            "Warning" { @(3) }
            "Both"    { @(1,2,3) }
        }

        $events = Get-WinEvent -FilterHashtable @{
            LogName = $LogName
            Level = $levels
            StartTime = $after
        } -MaxEvents 20 -ErrorAction SilentlyContinue

        if ($events) {
            Write-Host "Found $($events.Count) events:" -ForegroundColor Yellow
            Write-Host ""
            foreach ($evt in $events) {
                $color = if ($evt.Level -le 2) { 'Red' } else { 'Yellow' }
                $levelStr = if ($evt.Level -le 2) { 'ERROR' } else { 'WARN' }
                Write-Host "[$levelStr] $($evt.TimeCreated.ToString('MM/dd HH:mm')) - $($evt.ProviderName)" -ForegroundColor $color
                Write-Host "  $($evt.Message.Substring(0, [Math]::Min(120, $evt.Message.Length)))..." -ForegroundColor Gray
                Write-Host ""
            }
        } else {
            Write-Host "No $Level events found in the last $Hours hours." -ForegroundColor Green
            Write-Host "System looks clean!" -ForegroundColor Green
        }
    } `
    -DefaultParameters @{ LogName = "System"; Level = "Both"; Hours = 24 }

# ==============================================================================
# PAGE 5: Quick Reference
# ==============================================================================

Add-UIStep -Name 'QuickRef' -Title 'Quick Reference' -Order 5 `
    -IconPath $iconSparkling `
    -Type "Dashboard" -Description 'Keyboard shortcuts, tips, and about info'

# Banner
Add-UIBanner -Step "QuickRef" -Name "RefBanner" `
    -Title "Quick Reference & Tips" `
    -Subtitle "Maintenance best practices and keyboard shortcuts" `
    -Height 150 `
    -TitleFontSize 26 `
    -TitleFontWeight "Bold" `
    -GradientStart '#0D1B2A' `
    -GradientEnd '#6A1B9A' `
    -IconPath $iconSparkling `
    -IconPosition 'Right' `
    -IconSize 60

# Maintenance Tips Card
Add-UICard -Step "QuickRef" -Name "MaintenanceTips" `
    -Title "Maintenance Best Practices" `
    -IconPath $iconCheckMark `
    -Content @"
Weekly Tasks:
  - Check disk space and clean temp files
  - Review Windows Update status
  - Verify backup completion
  - Check critical service health

Monthly Tasks:
  - Review event logs for recurring errors
  - Audit installed software
  - Check driver updates
  - Run disk defragmentation (HDD only)
  - Review startup programs

Quarterly Tasks:
  - Full system health assessment
  - Review security policies
  - Test disaster recovery procedures
  - Update documentation
"@

# Keyboard Shortcuts Card
Add-UICard -Step "QuickRef" -Name "Shortcuts" `
    -Title "Useful PowerShell Commands" `
    -IconPath $iconPowershell `
    -Content @"
System Information:
  systeminfo | findstr /B /C:"OS"
  Get-CimInstance Win32_OperatingSystem

Disk Cleanup:
  cleanmgr /d C
  Clear-RecycleBin -Force

Network Reset:
  ipconfig /flushdns
  netsh winsock reset

Service Management:
  Get-Service | Where Status -eq Stopped
  Restart-Service -Name Spooler

Performance:
  Get-Process | Sort CPU -Desc | Select -First 10
  Get-Counter '\Processor(_Total)\% Processor Time'
"@

# About Card
Add-UICard -Step "QuickRef" -Name "AboutCard" `
    -Title "About This Dashboard" `
    -IconPath $iconWindows `
    -Content @"
Computer Maintenance Dashboard v1.0
Built with PoshUI (.NET Framework 4.8)

This dashboard provides real-time system monitoring,
storage analytics, network diagnostics, and interactive
maintenance tools - all from a single sleek interface.

Icons: Icons8 (https://icons8.com)
Framework: PoshUI by Kanders-II
"@

# ==============================================================================
# SHOW DASHBOARD
# ==============================================================================

Write-Host "Launching Computer Maintenance Dashboard..." -ForegroundColor Green
Write-Host "  - Page 1: System Health (gauges, sparklines, status)" -ForegroundColor White
Write-Host "  - Page 2: Storage & Analytics (charts, grids)" -ForegroundColor White
Write-Host "  - Page 3: Network & Security (connections, firewall)" -ForegroundColor White
Write-Host "  - Page 4: Maintenance Tools (script cards)" -ForegroundColor White
Write-Host "  - Page 5: Quick Reference (tips, commands)" -ForegroundColor White
Write-Host ""

Show-PoshUIDashboard
