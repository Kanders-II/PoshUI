# Demo-ScriptCard.ps1 - Demonstrates the new Dashboard view mode with script cards
# This example shows how to create a dashboard-style interface with executable script cards

# Get module path relative to this script
$modulePath = Join-Path $PSScriptRoot '..\PoshUI.Dashboard\PoshUI.Dashboard.psd1'
Import-Module $modulePath -Force

# Get paths for branding assets
$scriptIconPath = Join-Path $PSScriptRoot 'Logo Files\Icons\browser.png'
$sidebarIconPath = Join-Path $PSScriptRoot 'Logo Files\png\Color logo - no background.png'

# Verify branding assets exist
foreach ($assetPath in @($scriptIconPath, $sidebarIconPath)) {
    if (-not (Test-Path $assetPath)) {
        throw "Branding asset not found: $assetPath"
    }
}

Write-Host @'

+----------------------------------------+
|  PoshUI - DashBoard Demo               |
|  Script Cards with Auto-Discovery      |
+----------------------------------------+
'@ -ForegroundColor Cyan

Write-Host "`nDemonstrating CardGrid view mode with executable script cards:" -ForegroundColor Yellow
Write-Host "  [CARD] Script cards that execute PowerShell scripts" -ForegroundColor White
Write-Host "  [PARAM] Auto-discovery of parameters from script param blocks" -ForegroundColor White
Write-Host "  [FILE] Support for external .ps1 files or inline scriptblocks" -ForegroundColor White
Write-Host "  [CAT] Category filtering and organization" -ForegroundColor White
Write-Host ""

# ==============================================================================
# INITIALIZE CARDGRID DASHBOARD
# ==============================================================================

New-PoshUIDashboard -Title "Admin Tools Dashboard" `
    -Description "Quick access to common administrative tasks" `
    -GridColumns 3 `
    -Theme "Dark" `
    -Icon $scriptIconPath

Set-UIBranding -WindowTitle "Admin Dashboard" `
    -WindowTitleIcon $scriptIconPath `
    -SidebarHeaderText "Quick Tools" `
    -SidebarHeaderIcon $sidebarIconPath `
    -SidebarHeaderIconOrientation 'Top'

# ==============================================================================
# SINGLE STEP
# ==============================================================================

Add-UIStep -Name "Tools" -Title "Administrative Tools" -Type "Dashboard" -Icon "&#xE770;"

# Banner for Tools step
Add-UIBanner -Step "Tools" -Name "ToolsBanner" `
    -Title "Administrative Tools" `
    -Subtitle "Quick access to common system monitoring and maintenance tasks" `
    -BackgroundColor "#0078D4"

# ==============================================================================
# SCRIPT CARDS
# ==============================================================================

# Card 1: Inline script with auto-discovered parameters
Add-UIScriptCard -Step "Tools" -Name "DiskCheck" `
    -Title "Check Disk Space" `
    -Description "View disk usage for a drive" `
    -Icon "" `
    -Category "Monitoring" `
    -ScriptBlock {
        param(
            [Parameter(HelpMessage = "Drive letter to check")]
            [ValidateSet('C', 'D', 'E', 'F')]
            [string]$DriveLetter = "C",
            
            [Parameter(HelpMessage = "Show detailed breakdown")]
            [switch]$Detailed
        )
        
        Write-Host "Checking disk space for drive $DriveLetter`:" -ForegroundColor Cyan
        
        $drive = Get-PSDrive -Name $DriveLetter -ErrorAction SilentlyContinue
        if (-not $drive) {
            Write-Warning "Drive $DriveLetter not found!"
            return
        }
        
        $usedGB = [math]::Round($drive.Used / 1GB, 2)
        $freeGB = [math]::Round($drive.Free / 1GB, 2)
        $totalGB = $usedGB + $freeGB
        $pctFree = [math]::Round(($freeGB / $totalGB) * 100, 1)
        
        Write-Host ""
        Write-Host "Drive $DriveLetter`: Status Report" -ForegroundColor White
        Write-Host "-------------------------" -ForegroundColor DarkGray
        Write-Host "Total:  $totalGB GB"
        Write-Host "Used:   $usedGB GB"
        Write-Host "Free:   $freeGB GB ($pctFree%)"
        
        if ($pctFree -lt 10) {
            Write-Warning "[WARNING] Low disk space!"
        } else {
            Write-Host "[OK] Disk space OK" -ForegroundColor Green
        }
        
        if ($Detailed) {
            Write-Host ""
            Write-Host "Detailed Usage:" -ForegroundColor Cyan
            Get-ChildItem "$DriveLetter`:\" -Directory -ErrorAction SilentlyContinue | 
                Select-Object -First 10 |
                ForEach-Object {
                    $size = (Get-ChildItem $_.FullName -Recurse -File -ErrorAction SilentlyContinue | 
                             Measure-Object -Property Length -Sum).Sum / 1GB
                    Write-Host "  $($_.Name): $([math]::Round($size, 2)) GB"
                }
        }
    }

# Card 2: Another inline script with different parameters
Add-UIScriptCard -Step "Tools" -Name "SystemInfo" `
    -Title "System Information" `
    -Description "Display system details" `
    -Icon "&#xE7F4;" `
    -Category "Monitoring" `
    -ScriptBlock {
        param(
            [Parameter(HelpMessage = "Include network information")]
            [switch]$IncludeNetwork,
            
            [Parameter(HelpMessage = "Include process count")]
            [switch]$IncludeProcesses
        )
        
        Write-Host "System Information" -ForegroundColor Cyan
        Write-Host "-------------------------" -ForegroundColor DarkGray
        
        $os = Get-CimInstance Win32_OperatingSystem
        $cs = Get-CimInstance Win32_ComputerSystem
        
        Write-Host "Computer: $($cs.Name)"
        Write-Host "OS: $($os.Caption)"
        Write-Host "Version: $($os.Version)"
        Write-Host "Architecture: $($os.OSArchitecture)"
        Write-Host "RAM: $([math]::Round($cs.TotalPhysicalMemory / 1GB, 2)) GB"
        Write-Host "Uptime: $((Get-Date) - $os.LastBootUpTime)"
        
        if ($IncludeProcesses) {
            Write-Host ""
            Write-Host "Processes: $((Get-Process).Count) running"
        }
        
        if ($IncludeNetwork) {
            Write-Host ""
            Write-Host "Network Adapters:" -ForegroundColor Cyan
            Get-NetAdapter | Where-Object Status -eq 'Up' | ForEach-Object {
                Write-Host "  $($_.Name): $($_.LinkSpeed)"
            }
        }
        
        Write-Host ""
        Write-Host "[OK] System info retrieved" -ForegroundColor Green
    }

# Card 3: Simple action card (no parameters)
Add-UIScriptCard -Step "Tools" -Name "ClearTemp" `
    -Title "Clear Temp Files" `
    -Description "Remove temporary files" `
    -Icon "&#xE74D;" `
    -Category "Maintenance" `
    -ScriptBlock {
        Write-Host "Clearing temporary files..." -ForegroundColor Cyan
        
        $tempPath = $env:TEMP
        $beforeCount = (Get-ChildItem $tempPath -Recurse -File -ErrorAction SilentlyContinue).Count
        
        Write-Host "Temp folder: $tempPath"
        Write-Host "Files before: $beforeCount"
        
        # Simulate cleanup (don't actually delete in demo)
        Write-Host ""
        Write-Host "[Demo mode - files not actually deleted]" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "[OK] Cleanup simulation complete" -ForegroundColor Green
    }

# Card 4: Script with numeric parameters
Add-UIScriptCard -Step "Tools" -Name "ProcessMonitor" `
    -Title "Top Processes" `
    -Description "Show resource-heavy processes" `
    -Icon "" `
    -Category "Monitoring" `
    -ScriptBlock {
        param(
            [Parameter(HelpMessage = "Number of processes to show")]
            [ValidateRange(1, 20)]
            [int]$TopCount = 5,
            
            [Parameter(HelpMessage = "Sort by CPU or Memory")]
            [ValidateSet('CPU', 'Memory')]
            [string]$SortBy = 'CPU'
        )
        
        Write-Host "Top $TopCount Processes by $SortBy" -ForegroundColor Cyan
        Write-Host "---------------------------------" -ForegroundColor DarkGray
        
        $sortProperty = if ($SortBy -eq 'CPU') { 'CPU' } else { 'WorkingSet64' }
        
        Get-Process | 
            Sort-Object $sortProperty -Descending | 
            Select-Object -First $TopCount |
            ForEach-Object {
                $cpu = [math]::Round($_.CPU, 1)
                $memMB = [math]::Round($_.WorkingSet64 / 1MB, 0)
                Write-Host "  $($_.ProcessName.PadRight(25)) CPU: $($cpu.ToString().PadLeft(8))  Mem: $($memMB.ToString().PadLeft(6)) MB"
            }
        
        Write-Host ""
        Write-Host "[OK] Process list retrieved" -ForegroundColor Green
    }

# Card 5: Service management
Add-UIScriptCard -Step "Tools" -Name "ServiceStatus" `
    -Title "Service Status" `
    -Description "Check service status" `
    -Icon "&#xE912;" `
    -Category "Services" `
    -ScriptBlock {
        param(
            [Parameter(Mandatory, HelpMessage = "Service name to check")]
            [string]$ServiceName = "Spooler",
            
            [Parameter(HelpMessage = "Show detailed info")]
            [switch]$Detailed
        )
        
        Write-Host "Checking service: $ServiceName" -ForegroundColor Cyan
        Write-Host ""
        
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        
        if (-not $service) {
            Write-Warning "Service '$ServiceName' not found!"
            return
        }
        
        $statusColor = if ($service.Status -eq 'Running') { 'Green' } else { 'Yellow' }
        
        Write-Host "Name:        $($service.Name)"
        Write-Host "Display:     $($service.DisplayName)"
        Write-Host "Status:      $($service.Status)" -ForegroundColor $statusColor
        Write-Host "Start Type:  $($service.StartType)"
        
        if ($Detailed) {
            Write-Host ""
            Write-Host "Dependencies:" -ForegroundColor Cyan
            $service.DependentServices | ForEach-Object {
                Write-Host "  -> $($_.Name) ($($_.Status))"
            }
        }
        
        Write-Host ""
        Write-Host "[OK] Service check complete" -ForegroundColor Green
    }

# Card 6: Event log viewer
Add-UIScriptCard -Step "Tools" -Name "EventLogs" `
    -Title "Recent Events" `
    -Description "View recent event log entries" `
    -Icon "" `
    -Category "Monitoring" `
    -ScriptBlock {
        param(
            [Parameter(HelpMessage = "Event log to query")]
            [ValidateSet('System', 'Application', 'Security')]
            [string]$LogName = "System",
            
            [Parameter(HelpMessage = "Number of events to show")]
            [ValidateRange(1, 50)]
            [int]$MaxEvents = 10,
            
            [Parameter(HelpMessage = "Filter by level")]
            [ValidateSet('All', 'Error', 'Warning', 'Information')]
            [string]$Level = "All"
        )
        
        Write-Host "Recent $LogName Events (showing $MaxEvents)" -ForegroundColor Cyan
        Write-Host "-----------------------------------------" -ForegroundColor DarkGray
        
        $levelFilter = switch ($Level) {
            'Error' { 1, 2 }
            'Warning' { 3 }
            'Information' { 4 }
            default { 1, 2, 3, 4 }
        }
        
        try {
            Get-WinEvent -LogName $LogName -MaxEvents $MaxEvents -ErrorAction SilentlyContinue |
                Where-Object { $_.Level -in $levelFilter } |
                ForEach-Object {
                    $levelIcon = switch ($_.Level) {
                        1 { "[ERR]" }
                        2 { "[ERR]" }
                        3 { "[WARN]" }
                        default { "[INFO]" }
                    }
                    Write-Host "$levelIcon $($_.TimeCreated.ToString('MM/dd HH:mm')) - $($_.Message.Substring(0, [Math]::Min(60, $_.Message.Length)))..."
                }
        }
        catch {
            Write-Warning "Could not read event log. Run as Administrator for full access."
        }
        
        Write-Host ""
        Write-Host "[OK] Event log query complete" -ForegroundColor Green
    }

# ===============================================================================
# SECOND CARDGRID STEP - Network & Security Tools
# ===============================================================================

Add-UIStep -Name "Network" -Title "Network & Security" -Type "Dashboard" -Icon "&#xE968;"

# Banner for Network step
Add-UIBanner -Step "Network" -Name "NetworkBanner" `
    -Title "Network & Security" `
    -Subtitle "Network diagnostics, connectivity tests, and security status checks" `
    -BackgroundColor "#107C10"

# Card 7: Network connectivity test
Add-UIScriptCard -Step "Network" -Name "PingTest" `
    -Title "Ping Test" `
    -Description "Test network connectivity" `
    -Icon "&#xE968;" `
    -Category "Network" `
    -ScriptBlock {
        param(
            [Parameter(Mandatory, HelpMessage = "Host to ping")]
            [string]$HostName = "google.com",
            
            [Parameter(HelpMessage = "Number of pings")]
            [ValidateRange(1, 10)]
            [int]$Count = 4
        )
        
        Write-Host "Pinging $HostName ($Count times)..." -ForegroundColor Cyan
        Write-Host ""
        
        $results = Test-Connection -ComputerName $HostName -Count $Count -ErrorAction SilentlyContinue
        
        if ($results) {
            $avgTime = ($results | Measure-Object -Property ResponseTime -Average).Average
            Write-Host "Results:" -ForegroundColor White
            Write-Host "  Packets: $Count sent, $($results.Count) received"
            Write-Host "  Avg Response: $([math]::Round($avgTime, 2)) ms"
            Write-Host ""
            Write-Host "[OK] Host is reachable" -ForegroundColor Green
        } else {
            Write-Warning "Host $HostName is not reachable!"
        }
    }

# Card 8: IP Configuration
Add-UIScriptCard -Step "Network" -Name "IPConfig" `
    -Title "IP Configuration" `
    -Description "Show network adapter details" `
    -Icon "" `
    -Category "Network" `
    -ScriptBlock {
        param(
            [Parameter(HelpMessage = "Show only active adapters")]
            [switch]$ActiveOnly
        )
        
        Write-Host "Network Configuration" -ForegroundColor Cyan
        Write-Host "-------------------------" -ForegroundColor DarkGray
        
        $adapters = Get-NetAdapter
        if ($ActiveOnly) {
            $adapters = $adapters | Where-Object Status -eq 'Up'
        }
        
        foreach ($adapter in $adapters) {
            $ip = Get-NetIPAddress -InterfaceIndex $adapter.ifIndex -AddressFamily IPv4 -ErrorAction SilentlyContinue
            Write-Host ""
            Write-Host "Adapter: $($adapter.Name)" -ForegroundColor White
            Write-Host "  Status: $($adapter.Status)"
            Write-Host "  Speed:  $($adapter.LinkSpeed)"
            if ($ip) {
                Write-Host "  IP:     $($ip.IPAddress)"
            }
        }
        
        Write-Host ""
        Write-Host "[OK] Network config retrieved" -ForegroundColor Green
    }

# Card 9: Firewall Status
Add-UIScriptCard -Step "Network" -Name "FirewallStatus" `
    -Title "Firewall Status" `
    -Description "Check Windows Firewall status" `
    -Icon "&#xE83D;" `
    -Category "Security" `
    -ScriptBlock {
        Write-Host "Windows Firewall Status" -ForegroundColor Cyan
        Write-Host "-------------------------" -ForegroundColor DarkGray
        
        $profiles = Get-NetFirewallProfile
        
        foreach ($profile in $profiles) {
            $statusColor = if ($profile.Enabled) { 'Green' } else { 'Red' }
            $statusText = if ($profile.Enabled) { 'Enabled' } else { 'Disabled' }
            Write-Host ""
            Write-Host "$($profile.Name) Profile:" -ForegroundColor White
            Write-Host "  Status: $statusText" -ForegroundColor $statusColor
            Write-Host "  Default Inbound: $($profile.DefaultInboundAction)"
            Write-Host "  Default Outbound: $($profile.DefaultOutboundAction)"
        }
        
        Write-Host ""
        Write-Host "[OK] Firewall status retrieved" -ForegroundColor Green
    }
# Card 10: Open Ports
Add-UIScriptCard -Step "Network" -Name "OpenPorts" `
    -Title "Open Ports" `
    -Description "Show listening ports" `
    -Icon "" `
    -Category "Security" `
    -ScriptBlock {
        param(
            [Parameter(HelpMessage = "Maximum ports to show")]
            [ValidateRange(5, 50)]
            [int]$MaxPorts = 15
        )
        
        Write-Host "Listening Ports (Top $MaxPorts)" -ForegroundColor Cyan
        Write-Host "-----------------------------" -ForegroundColor DarkGray
        
        Get-NetTCPConnection -State Listen | 
            Select-Object LocalPort, OwningProcess -Unique |
            Sort-Object LocalPort |
            Select-Object -First $MaxPorts |
            ForEach-Object {
                $process = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
                $procName = if ($process) { $process.ProcessName } else { "Unknown" }
                Write-Host "  Port $($_.LocalPort.ToString().PadRight(6)) -> $procName"
            }
        
        Write-Host ""
        Write-Host "[OK] Port scan complete" -ForegroundColor Green
    }

# ===============================================================================
# THIRD CARDGRID STEP - User & Storage Tools  
# ===============================================================================

Add-UIStep -Name "Storage" -Title "Users & Storage" -Type "Dashboard" -Icon "&#xE77B;"

# Banner for Storage step
Add-UIBanner -Step "Storage" -Name "StorageBanner" `
    -Title "Users & Storage" `
    -Subtitle "User session information and storage management utilities" `
    -BackgroundColor "#5C2D91"

# Card 11: User Sessions
Add-UIScriptCard -Step "Storage" -Name "UserSessions" `
    -Title "User Sessions" `
    -Description "Show logged in users" `
    -Icon "" `
    -Category "Users" `
    -ScriptBlock {
        Write-Host "Current User Sessions" -ForegroundColor Cyan
        Write-Host "-------------------------" -ForegroundColor DarkGray
        
        Write-Host ""
        Write-Host "Current User: $env:USERNAME"
        Write-Host "Computer:     $env:COMPUTERNAME"
        Write-Host "Domain:       $env:USERDOMAIN"
        Write-Host ""
        
        $logonTime = (Get-CimInstance Win32_LogonSession | Where-Object LogonType -eq 2 | Select-Object -First 1).StartTime
        if ($logonTime) {
            Write-Host "Logon Time:   $logonTime"
        }
        
        Write-Host ""
        Write-Host "[OK] Session info retrieved" -ForegroundColor Green
    }

# Card 12: Folder Size Calculator
Add-UIScriptCard -Step "Storage" -Name "FolderSize" `
    -Title "Folder Size" `
    -Description "Calculate folder size" `
    -Icon "&#xE8B7;" `
    -Category "Storage" `
    -ScriptBlock {
        param(
            [Parameter(Mandatory, HelpMessage = "Folder path to analyze")]
            [string]$FolderPath = "$env:USERPROFILE\Documents",
            
            [Parameter(HelpMessage = "Include subfolders")]
            [switch]$IncludeSubfolders
        )
        
        Write-Host "Calculating folder size..." -ForegroundColor Cyan
        Write-Host "Path: $FolderPath"
        Write-Host ""
        
        if (-not (Test-Path $FolderPath)) {
            Write-Warning "Folder not found: $FolderPath"
            return
        }
        
        $files = if ($IncludeSubfolders) {
            Get-ChildItem $FolderPath -Recurse -File -ErrorAction SilentlyContinue
        } else {
            Get-ChildItem $FolderPath -File -ErrorAction SilentlyContinue
        }
        
        $totalSize = ($files | Measure-Object -Property Length -Sum).Sum
        $fileCount = $files.Count
        
        $sizeGB = [math]::Round($totalSize / 1GB, 2)
        $sizeMB = [math]::Round($totalSize / 1MB, 2)
        
        Write-Host "Results:" -ForegroundColor White
        Write-Host "  Files: $fileCount"
        Write-Host "  Size:  $sizeMB MB ($sizeGB GB)"
        
        Write-Host ""
        Write-Host "[OK] Size calculation complete" -ForegroundColor Green
    }

# Card 13: Recycle Bin Info
Add-UIScriptCard -Step "Storage" -Name "RecycleBin" `
    -Title "Recycle Bin" `
    -Description "View recycle bin contents" `
    -Icon "&#xE74D;" `
    -Category "Storage" `
    -ScriptBlock {
        Write-Host "Recycle Bin Status" -ForegroundColor Cyan
        Write-Host "-------------------------" -ForegroundColor DarkGray
        
        $shell = New-Object -ComObject Shell.Application
        $recycleBin = $shell.NameSpace(0x0a)
        $items = $recycleBin.Items()
        
        $totalSize = 0
        $items | ForEach-Object { $totalSize += $_.Size }
        
        Write-Host ""
        Write-Host "Items: $($items.Count)"
        Write-Host "Size:  $([math]::Round($totalSize / 1MB, 2)) MB"
        
        if ($items.Count -gt 0) {
            Write-Host ""
            Write-Host "Recent items:" -ForegroundColor White
            $items | Select-Object -First 5 | ForEach-Object {
                Write-Host "  $($_.Name)"
            }
        }
        
        Write-Host ""
        Write-Host "[OK] Recycle bin info retrieved" -ForegroundColor Green
    }

# ===============================================================================
# CARD 14: External Script with Dynamic File Generation
# ===============================================================================
# This demonstrates how ScriptCard can use an external .ps1 file that is 
# dynamically generated at runtime. PoshUI automatically discovers parameters
# from the script's param() block, including ValidateScript attributes for
# file/folder path selectors.

$dynamicScriptPath = Join-Path $env:TEMP "PoshUI_DynamicTool.ps1"

# Generate script content dynamically (simulates runtime tool creation)
$scriptContent = @'
param(
    [Parameter(Mandatory=$true)]
    [ValidateScript({ Test-Path $_ -PathType Leaf })]
    [string]$InputFile,

    [Parameter(Mandatory=$true)]
    [ValidateScript({ Test-Path $_ -PathType Container })]
    [string]$OutputFolder,

    [Parameter(Mandatory=$false)]
    [string]$Message = "Processing files..."
)

Write-Host "=== Dynamic File Processor ===" -ForegroundColor Cyan
Write-Host ""
Write-Host $Message -ForegroundColor Yellow
Write-Host ""
Write-Host "Input File:    $InputFile" -ForegroundColor White
Write-Host "Output Folder: $OutputFolder" -ForegroundColor White
Write-Host ""

$fileInfo = Get-Item $InputFile
Write-Host "File Details:" -ForegroundColor Green
Write-Host "  Name: $($fileInfo.Name)"
Write-Host "  Size: $([math]::Round($fileInfo.Length / 1KB, 2)) KB"
Write-Host "  Modified: $($fileInfo.LastWriteTime)"
Write-Host ""
Write-Host "[OK] Processing complete!" -ForegroundColor Green
'@

# Write the dynamic script to temp location
$scriptContent | Out-File -FilePath $dynamicScriptPath -Encoding UTF8 -Force

Add-UIScriptCard -Step "Tools" -Name "DynamicFileTool" `
    -Title "Dynamic File Processor" `
    -Description "Demonstrates external script with auto-discovered path selectors" `
    -Icon "&#xE8B7;" `
    -Category "Advanced" `
    -ScriptPath $dynamicScriptPath

# ===============================================================================
# LAUNCH DASHBOARD
# ===============================================================================

Write-Host "`nLaunching CardGrid Dashboard..." -ForegroundColor Green
Write-Host "   Each card can be clicked to open a dialog with parameters and execute the script." -ForegroundColor Gray
Write-Host "   Navigate between pages using the sidebar." -ForegroundColor Gray
Write-Host ""

Show-PoshUIDashboard

