<#
.SYNOPSIS
    Advanced demonstration of dynamic controls with cascading dependencies.

.DESCRIPTION
    Comprehensive showcase of PoshUI's dynamic control capabilities using PoshUI Cmdlets.
    Demonstrates real-world scenarios with cascading dropdowns, conditional logic,
    CSV data sources, and dependency chains.
    
.NOTES
    Company: Kanders-II
    Style: Clean PowerShell with hashtable splatting (no backticks)
    
.EXAMPLE
    .\Demo-DynamicControls.ps1
    
    Launches the advanced dynamic controls wizard demonstrating:
    - Cascading dropdowns (Environment -> Region -> Server)
    - CSV-based dynamic data
    - Multi-level dependencies
    - Real-time updates
#>

$modulePath = Join-Path $PSScriptRoot '..\PoshUI.Wizard\PoshUI.Wizard.psd1'
Import-Module $modulePath -Force

# PNG Icon Paths for Testing Colored PNG Support
$scriptIconPath = Join-Path $PSScriptRoot 'Logo Files\Icons\browser.png'
$sidebarIconPath = Join-Path $PSScriptRoot 'Logo Files\png\Color logo - no background.png'
$colorLogoPath = Join-Path $PSScriptRoot 'Logo Files\png\Color logo with background.png'
$whiteLogoPath = Join-Path $PSScriptRoot 'Logo Files\png\White logo - no background.png'
$iconPath = Join-Path $PSScriptRoot 'Logo Files\Icons\icon.png'

foreach ($assetPath in @($scriptIconPath, $sidebarIconPath, $colorLogoPath, $whiteLogoPath, $iconPath)) {
    if (-not (Test-Path $assetPath)) {
        throw "Branding asset not found: $assetPath"
    }
}

Write-Host "PNG Icons loaded for testing:" -ForegroundColor Cyan
Write-Host "  Browser Icon: $scriptIconPath" -ForegroundColor Gray
Write-Host "  Sidebar Logo: $sidebarIconPath" -ForegroundColor Gray
Write-Host "  Color Logo: $colorLogoPath" -ForegroundColor Gray
Write-Host "  White Logo: $whiteLogoPath" -ForegroundColor Gray
Write-Host "  Icon: $iconPath" -ForegroundColor Gray
Write-Host ""

Write-Host @'

+--------------------------------------+
|  PoshUI                        |
|  Advanced Dynamic Controls Demo |
+--------------------------------------+
'@ -ForegroundColor Cyan

Write-Host "`nDemonstrating advanced dynamic control patterns:" -ForegroundColor Yellow
Write-Host "  Cascading dropdowns with dependencies" -ForegroundColor White
Write-Host "  Script block-driven choices" -ForegroundColor White
Write-Host "  CSV data sources" -ForegroundColor White
Write-Host "  Multi-level dependency chains" -ForegroundColor White
Write-Host "  Conditional control behavior" -ForegroundColor White
Write-Host "  Real-time updates" -ForegroundColor White
Write-Host ""

# ========================================
# CREATE SAMPLE DATA FILES
# ========================================

# Create sample CSV for database selection
$databasesCsvPath = Join-Path $env:TEMP 'PoshUI_databases.csv'
$databasesCsvContent = @'
DatabaseName,Server,Environment,Size
ProductionDB01,SQL-PROD-01,Production,500GB
ProductionDB02,SQL-PROD-02,Production,750GB
StagingDB01,SQL-STAGE-01,Staging,100GB
StagingDB02,SQL-STAGE-02,Staging,150GB
DevDB01,SQL-DEV-01,Development,50GB
DevDB02,SQL-DEV-02,Development,75GB
'@
Set-Content -Path $databasesCsvPath -Value $databasesCsvContent -Force

# Create sample CSV for application selection
$applicationsCsvPath = Join-Path $env:TEMP 'PoshUI_applications.csv'
$applicationsCsvContent = @'
AppName,AppType,RequiresDatabase
WebPortal,Web Application,Yes
APIGateway,API Service,Yes
FileProcessor,Background Service,No
MonitoringAgent,Monitoring,No
ReportGenerator,Reporting,Yes
'@
Set-Content -Path $applicationsCsvPath -Value $applicationsCsvContent -Force

Write-Host "Sample data files created in: $env:TEMP" -ForegroundColor Gray
Write-Host ""

# ========================================
# INITIALIZE WIZARD
# ========================================

$wizardParams = @{
    Title = 'Advanced Dynamic Controls Showcase'
    Description = 'Demonstrating cascading dependencies and dynamic data sources'
    Theme = 'Dark'
}
New-PoshUIWizard @wizardParams

# ========================================
# CUSTOM THEME DEFINITION
# ========================================

$customTheme = @{
    # Accent Colors - Purple theme
    AccentColor = '#7C3AED'
    AccentDark = '#6D28D9'
    AccentLight = '#A78BFA'
    
    # Backgrounds - Light gray palette
    Background = '#F5F5F5'
    ContentBackground = '#FFFFFF'
    CardBackground = '#FAFAFA'
    
    # Sidebar - Black
    SidebarBackground = '#000000'
    SidebarText = '#E0E0E0'
    SidebarHighlight = '#7C3AED'
    
    # Text Colors - Dark text on light backgrounds
    TextPrimary = '#1F1F1F'
    TextSecondary = '#6B6B6B'
    
    # Buttons
    ButtonBackground = '#7C3AED'
    ButtonForeground = '#FFFFFF'
    
    # Input Controls - Light gray
    InputBackground = '#F9F9F9'
    InputBorder = '#7C3AED'
    
    # Borders - Light gray
    BorderColor = '#E0E0E0'
    
    # Title Bar - Light gray
    TitleBarBackground = '#F0F0F0'
    TitleBarText = '#1F1F1F'
    
    # Status Colors
    SuccessColor = '#10B981'
    WarningColor = '#F59E0B'
    ErrorColor = '#EF4444'
}

Set-UITheme $customTheme

$brandingParams = @{
    WindowTitle                  = 'Advanced Dynamic Controls Showcase'
    WindowTitleIcon              = $scriptIconPath
    SidebarHeaderText            = 'Dynamic Controls'
    SidebarHeaderIcon            = $colorLogoPath
    SidebarHeaderIconOrientation = 'Top'
}
Set-UIBranding @brandingParams

Write-Host "Testing Colored PNG Icons:" -ForegroundColor Yellow
Write-Host "  Sidebar Icon: Colored PNG logo" -ForegroundColor White
Write-Host "  Banner Icons: Multiple colored PNGs" -ForegroundColor White
Write-Host ""

# ========================================
# STEP 1: Welcome and Overview
# ========================================

$step1Params = @{
    Name = 'hello'
    Title = 'Welcome'
    Order = 1
    IconPath = $colorLogoPath
    Description = 'Introduction to dynamic controls'
}
Add-UIStep @step1Params

$welcomeBannerParams = @{
    Step = 'hello'
    Title = 'Dynamic Controls Mastery'
    Subtitle = 'Cascading dependencies, script blocks, and real-time data sources'
    Height = 160
    TitleFontSize = 28
    SubtitleFontSize = 15
    BackgroundColor = '#7C3AED'
    GradientStart = '#7C3AED'
    GradientEnd = '#A78BFA'
    IconPath = $colorLogoPath
    IconPosition = 'Right'
    IconSize = 80
}
Add-UIBanner @welcomeBannerParams

$welcomeCardParams = @{
    Step = 'hello'
    Title = 'Advanced Dynamic Controls'
    Type = 'Info'
    IconPath = $iconPath
    Content = @'
This wizard demonstrates PoshUI\'s powerful dynamic control capabilities.

Key Concepts Demonstrated:

Cascading Dropdowns:
Choices that update based on other selections
Example: Environment -> Region -> Server

Script Block Logic:
Custom PowerShell code to generate options
Real-time data fetching and processing

CSV Data Sources:
Loading choices from external files
Integration with existing data sources

Dependency Chains:
Multi-level cascading (A -> B -> C -> D)
Complex real-world scenarios

Conditional Controls:
Controls that appear/behave differently based on context
Smart form adaptation

Real-World Scenario:
We\'ll walk through a deployment wizard where your selections dynamically
influence subsequent options, simulating real infrastructure choices.
'@
}
Add-UICard @welcomeCardParams

# Add a second card with different PNG icon to test colored PNG support
Add-UICard -Step 'hello' -Title 'PNG Icon Testing' -Type 'Tip' `
    -IconPath $colorLogoPath `
    -Content 'This card demonstrates colored PNG icon support. The icon to the left is a full-color PNG image, not a monochrome glyph.'

# ========================================
# STEP 2: Environment Selection (Base)
# ========================================

$step2Params = @{
    Name = 'Environment'
    Title = 'Environment'
    Order = 2
    IconPath = $iconPath
    Description = 'Select target environment'
}
Add-UIStep @step2Params

# Environment banner with context
$envBannerParams = @{
    Step = 'Environment'
    Title = 'Select Target Environment'
    Subtitle = 'Choose your deployment environment - This selection drives all subsequent dynamic choices'
    Height = 140
    TitleFontSize = 24
    SubtitleFontSize = 14
    BackgroundColor = '#2E7D32'
    GradientStart = '#2E7D32'
    GradientEnd = '#43A047'
    IconPath = $sidebarIconPath
    IconPosition = 'Right'
    IconSize = 60
}
Add-UIBanner @envBannerParams

$envInfoCardParams = @{
    Step = 'Environment'
    Title = 'Environment Selection'
    Type = 'Info'
    IconPath = $whiteLogoPath
    Content = @'
The environment you select will determine available regions and servers.

This demonstrates:
- Base selection that drives all subsequent choices
- Foundation of the dependency chain
- How static choices influence dynamic options

Impact on Next Steps:
Your Environment selection will affect:
- Available regions in Step 3
- Server options in Step 4
- Database choices in Step 6
- Feature availability in Step 7

Learning: Watch how your selection affects the next steps!
This is the foundation of our cascading dependency system.
'@
}
Add-UICard @envInfoCardParams

# Static environment dropdown (foundation of dependency chain)
$environmentParams = @{
    Step = 'Environment'
    Name = 'TargetEnvironment'
    Label = 'Target Environment'
    Choices = @('Development', 'Staging', 'Production')
    Default = 'Development'
    Mandatory = $true
}
Add-UIDropdown @environmentParams

# ========================================
# STEP 3: Region Selection (Dynamic - Level 1)
# ========================================

$step3Params = @{
    Name = 'Region'
    Title = 'Region'
    Order = 3
    IconPath = $scriptIconPath
    Description = 'Select deployment region (depends on environment)'
}
Add-UIStep @step3Params

# Region banner with context
$regionBannerParams = @{
    Step = 'Region'
    Title = 'Select Deployment Region'
    Subtitle = 'Regions are dynamically filtered based on your environment selection - Watch the dependency in action!'
    Height = 140
    TitleFontSize = 24
    SubtitleFontSize = 14
    BackgroundColor = '#1565C0'
    GradientStart = '#1565C0'
    GradientEnd = '#1976D2'
    IconPath = $sidebarIconPath
    IconPosition = 'Right'
    IconSize = 60
}
Add-UIBanner @regionBannerParams

$regionInfoCardParams = @{
    Step = 'Region'
    Title = 'Dynamic Region Selection'
    Type = 'Info'
    IconPath = $scriptIconPath
    Content = @'
Regions are filtered based on your environment selection.

Dependency Chain:
 TargetEnvironment -> Region

Environment-Based Logic:

Production: Access to all global regions
  - US-East-1, US-West-2, EU-Central-1, EU-West-1
  - AP-Southeast-1, AP-Northeast-1

Staging: Limited to staging-approved regions
  - US-East-1-Staging, EU-Central-1-Staging
  - AP-Southeast-1-Staging

Development: Local development regions only
  - Dev-Local, Dev-Cloud-US, Dev-Cloud-EU

Technical Details:
This is a ScriptBlock-driven dropdown with parameter dependency.
The script simulates data retrieval with a 300ms delay.

Try This: Go back and change the environment, then return here
to see the regions update automatically!
'@
}
Add-UICard @regionInfoCardParams

# Dynamic region dropdown (depends on TargetEnvironment)
$regionParams = @{
    Step = 'Region'
    Name = 'DeploymentRegion'
    Label = 'Deployment Region'
    ScriptBlock = {
        param($TargetEnvironment)
        
        Start-Sleep -Milliseconds 300  # Simulate data retrieval
        
        switch ($TargetEnvironment) {
            'Production' {
                @('US-East-1', 'US-West-2', 'EU-Central-1', 'EU-West-1', 'AP-Southeast-1', 'AP-Northeast-1')
            }
            'Staging' {
                @('US-East-1-Staging', 'EU-Central-1-Staging', 'AP-Southeast-1-Staging')
            }
            'Development' {
                @('Dev-Local', 'Dev-Cloud-US', 'Dev-Cloud-EU')
            }
            default {
                @('Unknown')
            }
        }
    }
    DependsOn = @('TargetEnvironment')
    Mandatory = $true
}
Add-UIDropdown @regionParams

# ========================================
# STEP 4: Server Selection (Dynamic - Level 2)
# ========================================

$step4Params = @{
    Name = 'Server'
    Title = 'Server'
    Order = 4
    IconPath = $whiteLogoPath
    Description = 'Select target server (depends on environment and region)'
}
Add-UIStep @step4Params

# Server banner with context
$serverBannerParams = @{
    Step = 'Server'
    Title = 'Select Target Server'
    Subtitle = 'Server list dynamically generated based on environment AND region - Multi-parameter dependency in action!'
    Height = 140
    TitleFontSize = 24
    SubtitleFontSize = 14
    BackgroundColor = '#6A1B9A'
    GradientStart = '#6A1B9A'
    GradientEnd = '#7B1FA2'
    IconPath = $sidebarIconPath
    IconPosition = 'Right'
    IconSize = 60
}
Add-UIBanner @serverBannerParams

$serverInfoCardParams = @{
    Step = 'Server'
    Title = 'Cascading Server Selection'
    Type = 'Info'
    Content = @'
Server list is dynamically generated based on BOTH environment and region.

Dependency Chain:
TargetEnvironment -> Region -> Server

Dynamic Logic:

Server Naming:
- Environment prefix (PROD/STG/DEV)
- Region code extraction
- Sequential numbering (01, 02, etc.)

Regional Filtering:
- Server availability filtered by selected region
- Realistic server naming conventions
- Multiple server types per region

Technical Implementation:
- Multi-parameter ScriptBlock dependency
- 500ms simulated discovery delay
- Real infrastructure discovery simulation

Advanced Feature:
This demonstrates multi-parameter dependencies in ScriptBlocks.
Both TargetEnvironment AND DeploymentRegion parameters are required!

Try This: Change either environment or region to see
how server names update automatically!
'@
}
Add-UICard @serverInfoCardParams

# Dynamic server dropdown (depends on TargetEnvironment AND DeploymentRegion)
$serverParams = @{
    Step = 'Server'
    Name = 'TargetServer'
    Label = 'Target Server'
    ScriptBlock = {
        param($TargetEnvironment, $DeploymentRegion)
        
        Start-Sleep -Milliseconds 500  # Simulate server discovery
        
        # Generate server names based on environment and region
        $envPrefix = switch ($TargetEnvironment) {
            'Production' { 'PROD' }
            'Staging' { 'STG' }
            'Development' { 'DEV' }
        }
        
        # Extract region code (e.g., "US-East-1" -> "USE1")
        $regionCode = if ($DeploymentRegion -match '^([A-Z]{2,3})-([A-Za-z]+)-?(\d*)') {
            "$($matches[1])$($matches[2].Substring(0,1).ToUpper())$($matches[3])"
        }
        else {
            'LOCAL'
        }
        
        # Generate realistic server list
        $servers = @(
            "$envPrefix-WEB-$regionCode-01"
            "$envPrefix-WEB-$regionCode-02"
            "$envPrefix-APP-$regionCode-01"
            "$envPrefix-DB-$regionCode-01"
        )
        
        $servers
    }
    DependsOn = @('TargetEnvironment', 'DeploymentRegion')
    Mandatory = $true
}
Add-UIDropdown @serverParams

# ========================================
# STEP 5: Application Selection (CSV-Driven)
# ========================================

$step5Params = @{
    Name = 'Application'
    Title = 'Application'
    Order = 5
    IconPath = $colorLogoPath
    Description = 'Select application to deploy from CSV data'
}
Add-UIStep @step5Params

# Application banner with context
$appBannerParams = @{
    Step = 'Application'
    Title = 'Select Application to Deploy'
    Subtitle = 'Application choices loaded from CSV data - External data source integration in action!'
    Height = 140
    TitleFontSize = 24
    SubtitleFontSize = 14
    BackgroundColor = '#D84315'
    GradientStart = '#D84315'
    GradientEnd = '#E64A19'
    IconPath = $sidebarIconPath
    IconPosition = 'Right'
    IconSize = 60
}
Add-UIBanner @appBannerParams

$appInfoCardParams = @{
    Step = 'Application'
    Title = 'CSV-Based Application Selection'
    Type = 'Info'
    Content = @'
Application choices are loaded from a CSV file.

Data Source:
PoshUI_applications.csv (temporarily created in %TEMP%)

CSV Structure:
- AppName - Application display name
- AppType - Application category
- RequiresDatabase - Database dependency flag

This Demonstrates:

External Data Source Integration:
- Loading choices from external files
- Real-time data import with Import-Csv
- Dynamic choice population

CSV Column Mapping:
- Direct column-to-choice mapping
- Flexible data structure
- Easy maintenance and updates

Real-World Data-Driven Wizards:
- Production application catalogs
- Environment-specific app lists
- Dynamic inventory management

Next Step:
The CSV contains application metadata that will be used
to determine database requirements in the next step!

Technical Note:
CSV is created temporarily and cleaned up after wizard completion.
'@
}
Add-UICard @appInfoCardParams

# CSV-based application dropdown (using Import-Csv + Add-UIDropdown)
$applicationChoices = (Import-Csv -Path $applicationsCsvPath).AppName

$applicationParams = @{
    Step = 'Application'
    Name = 'ApplicationName'
    Label = 'Application to Deploy'
    Choices = $applicationChoices
    Mandatory = $true
}
Add-UIDropdown @applicationParams

# ========================================
# STEP 6: Database Selection (Environment-Filtered)
# ========================================

$step6Params = @{
    Name = 'Database'
    Title = 'Database'
    Order = 6
    IconPath = $iconPath
    Description = 'Select target database'
}
Add-UIStep @step6Params

# Database banner with context
$dbBannerParams = @{
    Step = 'Database'
    Title = 'Select Target Database'
    Subtitle = 'Databases dynamically filtered based on environment selection - Environment-specific data filtering!'
    Height = 140
    TitleFontSize = 24
    SubtitleFontSize = 14
    BackgroundColor = '#00695C'
    GradientStart = '#00695C'
    GradientEnd = '#00796B'
    IconPath = $sidebarIconPath
    IconPosition = 'Right'
    IconSize = 60
}
Add-UIBanner @dbBannerParams

$dbInfoCardParams = @{
    Step = 'Database'
    Title = 'Environment-Specific Database Selection'
    Type = 'Info'
    Content = @'
Databases are dynamically filtered based on your environment selection.

Environment-Specific Database Options:

Development Environment:
- DevDB01 (50GB) - Small development database
- DevDB02 (75GB) - Medium development database
- Perfect for testing and development workflows

Staging Environment:
- StagingDB01 (100GB) - Pre-production testing
- StagingDB02 (150GB) - Full staging environment
- Mirrors production structure with smaller scale

Production Environment:
- ProductionDB01 (500GB) - Primary production database
- ProductionDB02 (750GB) - High-capacity production
- Enterprise-grade storage and performance

Dynamic Filtering:
The database list updates automatically when you change the environment.

Try This:
Go back and change the environment, then return here to see
the database options update instantly!

Related Examples:
- Demo-DynamicParameters-Cascading.ps1
- Demo-DynamicParameters-Dependencies.ps1

Learning: This demonstrates how environment-specific data
can be filtered dynamically based on user selections.

Dynamic Examples in This Demo:
Steps 3, 4, 6, and 7 (Region, Server, Database, Features)
'@
}
Add-UICard @dbInfoCardParams

# Database dropdown with dynamic filtering based on environment
$databaseParams = @{
    Step = 'Database'
    Name = 'DatabaseName'
    Label = 'Target Database'
    ScriptBlock = {
        param($TargetEnvironment)
        
        if ($TargetEnvironment -eq 'Production') {
            @('ProductionDB01 (500GB)', 'ProductionDB02 (750GB)')
        }
        elseif ($TargetEnvironment -eq 'Staging') {
            @('StagingDB01 (100GB)', 'StagingDB02 (150GB)')
        }
        else {
            @('DevDB01 (50GB)', 'DevDB02 (75GB)')
        }
    }
    DependsOn = @('TargetEnvironment')
    Mandatory = $true
}
Add-UIDropdown @databaseParams

# ========================================
# STEP 7: Deployment Options (Dynamic ListBox)
# ========================================

$step7Params = @{
    Name = 'Options'
    Title = 'Options'
    Order = 7
    IconPath = $scriptIconPath
    Description = 'Select deployment features (multi-select, environment-dependent)'
}
Add-UIStep @step7Params

# Options banner with context
$optionsBannerParams = @{
    Step = 'Options'
    Title = 'Select Deployment Features'
    Subtitle = 'Multi-select features dynamically filtered by environment - ScriptBlock-driven ListBox in action!'
    Height = 140
    TitleFontSize = 24
    SubtitleFontSize = 14
    BackgroundColor = '#4527A0'
    GradientStart = '#4527A0'
    GradientEnd = '#512DA8'
    IconPath = $sidebarIconPath
    IconPosition = 'Right'
    IconSize = 60
}
Add-UIBanner @optionsBannerParams

$optionsInfoCardParams = @{
    Step = 'Options'
    Title = 'Dynamic Multi-Select Options'
    Type = 'Info'
    Content = @'
Available deployment features change based on environment.

Dependency Chain:
TargetEnvironment -> FeatureList

Environment-Specific Logic:

Production Environment:
Security-focused features enabled
- High Availability - Redundancy and failover
- Auto-Scaling - Dynamic resource management
- Disaster Recovery - Backup and restore capabilities
- Security Hardening - Enhanced security measures

Staging Environment:
Testing and validation features
- Integration Tests - Cross-system testing
- Performance Testing - Load and stress testing
- Load Testing - Capacity validation

Development Environment:
Debug and development tools
- Debug Mode - Enhanced logging and debugging
- Hot Reload - Dynamic code updates
- Detailed Logging - Comprehensive debug output
- Developer Tools - Advanced development utilities

Technical Implementation:
This demonstrates ScriptBlock-driven multi-select ListBox controls.
Features are dynamically generated based on environment selection.

Try This:
Change the environment and watch the feature list update!
Notice how different environments prioritize different capabilities.

Learning: Multi-select controls can also be dynamic,
not just single-select dropdowns!
'@
}
Add-UICard @optionsInfoCardParams

# Dynamic multi-select ListBox (depends on TargetEnvironment)
$featuresParams = @{
    Step = 'Options'
    Name = 'DeploymentFeatures'
    Label = 'Deployment Features (Multi-Select)'
    ScriptBlock = {
        param($TargetEnvironment)
        
        Start-Sleep -Milliseconds 200
        
        $baseFeatures = @('Logging', 'Monitoring', 'Health Checks')
        
        $environmentFeatures = switch ($TargetEnvironment) {
            'Production' {
                @('High Availability', 'Auto-Scaling', 'Disaster Recovery', 'Security Hardening')
            }
            'Staging' {
                @('Integration Tests', 'Performance Testing', 'Load Testing')
            }
            'Development' {
                @('Debug Mode', 'Hot Reload', 'Detailed Logging', 'Developer Tools')
            }
        }
        
        $baseFeatures + $environmentFeatures | Sort-Object
    }
    DependsOn = @('TargetEnvironment')
    MultiSelect = $true
    Height = 180
}
Add-UIListBox @featuresParams

# ========================================
# STEP 8: Review and Summary
# ========================================

$step8Params = @{
    Name = 'Summary'
    Title = 'Summary'
    Order = 8
    IconPath = $whiteLogoPath
    Description = 'Review your dynamic selections'
}
Add-UIStep @step8Params

# Summary banner with context
$summaryBannerParams = @{
    Step = 'Summary'
    Title = 'Review Dynamic Configuration'
    Subtitle = 'All cascading dependencies resolved - Ready to see your complete dynamic deployment configuration!'
    Height = 140
    TitleFontSize = 24
    SubtitleFontSize = 14
    BackgroundColor = '#2E7D32'
    GradientStart = '#2E7D32'
    GradientEnd = '#43A047'
    IconPath = $sidebarIconPath
    IconPosition = 'Right'
    IconSize = 60
}
Add-UIBanner @summaryBannerParams

$summaryCardParams = @{
    Step = 'Summary'
    Title = 'Deployment Configuration Ready'
    Type = 'Info'
    Content = @'
Dynamic Dependencies Resolved!

Your advanced configuration is complete with:

Cascading Chain Results:
- Environment -> Region -> Server selections linked
- All dependencies properly resolved
- Real-time data loading successful

Data Integration:
- CSV-based application selection
- Script-driven feature filtering
- Multi-select options captured

Ready for Production:
- Click Finish to see the complete configuration
- Review how cascading data flows through your scripts
- All dynamic parameters available for deployment

Learning Achieved:
Mastered dynamic controls, dependencies, and real-time data!
'@
}
Add-UICard @summaryCardParams

# ========================================
# EXECUTION SCRIPT
# ========================================

$scriptBody = {
    Write-Host "`n" -NoNewline
    Write-Host ('=' * 80) -ForegroundColor Cyan
    Write-Host "  Advanced Dynamic Controls - Configuration Summary" -ForegroundColor Cyan
    Write-Host ('=' * 80) -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "DEPENDENCY CHAIN RESULTS:" -ForegroundColor Yellow
    Write-Host "  Environment         : $TargetEnvironment" -ForegroundColor White
    Write-Host "  ->" -ForegroundColor DarkGray
    Write-Host "  Region              : $DeploymentRegion" -ForegroundColor White
    Write-Host "  ->" -ForegroundColor DarkGray
    Write-Host "  Server              : $TargetServer" -ForegroundColor White
    Write-Host ""
    
    Write-Host "CSV-DRIVEN SELECTIONS:" -ForegroundColor Yellow
    Write-Host "  Application         : $ApplicationName" -ForegroundColor White
    Write-Host "  Database            : $DatabaseName" -ForegroundColor White
    Write-Host ""
    
    Write-Host "DYNAMIC FEATURES (Multi-Select):" -ForegroundColor Yellow
    if ($DeploymentFeatures -and $DeploymentFeatures.Count -gt 0) {
        foreach ($feature in $DeploymentFeatures) {
            Write-Host "  + $feature" -ForegroundColor Green
        }
    }
    else {
        Write-Host "  (none selected)" -ForegroundColor Gray
    }
    Write-Host ""
    
    Write-Host ('=' * 80) -ForegroundColor Cyan
    Write-Host "  All dynamic dependencies resolved successfully!" -ForegroundColor Green
    Write-Host ('=' * 80) -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "KEY LEARNINGS:" -ForegroundColor Yellow
    Write-Host "  Used -ScriptBlock with param() for dynamic choices" -ForegroundColor White
    Write-Host "  Specified -DependsOn to create cascading updates" -ForegroundColor White
    Write-Host "  Loaded data from CSV files with Import-Csv + Add-UIDropdown" -ForegroundColor White
    Write-Host "  Combined CSV + ScriptBlock for filtered data" -ForegroundColor White
    Write-Host "  Applied dependencies to multi-select ListBox controls" -ForegroundColor White
    Write-Host ""
}

# ========================================
# LAUNCH WIZARD
# ========================================

Write-Host "Launching advanced dynamic controls wizard..." -ForegroundColor Cyan
Write-Host ""

Show-PoshUIWizard -ScriptBody $scriptBody

# ========================================
# CLEANUP
# ========================================

Write-Host "`n[CLEANUP] Cleaning up temporary files..." -ForegroundColor Gray
if (Test-Path $databasesCsvPath) {
    Remove-Item $databasesCsvPath -Force
}
if (Test-Path $applicationsCsvPath) {
    Remove-Item $applicationsCsvPath -Force
}
Write-Host "Cleanup complete." -ForegroundColor Gray


