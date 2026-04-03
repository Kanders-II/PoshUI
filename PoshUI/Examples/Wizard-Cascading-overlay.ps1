#
# .SYNOPSIS
#     Demonstrates cascading dropdowns using PoshUI Cmdlets with progress overlays.
# .DESCRIPTION
#     Builds a multi-step wizard that highlights when the built-in progress overlay appears
#     for dependent dropdowns. Cards provide guidance for each scenario so authors can see
#     what triggers the overlay and how to communicate it to users.

$modulePath = Join-Path $PSScriptRoot '..\PoshUI.Wizard\PoshUI.Wizard.psd1'
Import-Module $modulePath -Force

$scriptIconPath = Join-Path $PSScriptRoot 'Logo Files\Icons\browser.png'
$sidebarIconPath = Join-Path $PSScriptRoot 'Logo Files\png\Color logo - no background.png'

foreach ($assetPath in @($scriptIconPath, $sidebarIconPath)) {
    if (-not (Test-Path $assetPath)) {
        throw "Branding asset not found: $assetPath"
    }
}

Write-Host @'

+----------------------------------------+
|  Cascading Dropdowns                  |
|  Progress Overlay Demo                  |
+----------------------------------------+
'@ -ForegroundColor Cyan

Write-Host 'Launching wizard via PoshUI Cmdlets so you can experiment with overlay triggers...' -ForegroundColor Yellow

# -------------------------------------------------------------
# Wizard definition
# -------------------------------------------------------------

$wizardParams = @{
    Title              = 'Overlay Showcase'
    Description        = 'Understand how dependent dropdowns display the progress overlay.'
    Theme              = 'Auto'
    Icon               = $scriptIconPath
}
New-PoshUIWizard @wizardParams

$brandingParams = @{
    WindowTitle                  = 'Overlay Showcase'
    WindowTitleIcon              = $scriptIconPath
    SidebarHeaderText            = 'Overlay Scenarios'
    SidebarHeaderIcon            = $sidebarIconPath
    SidebarHeaderIconOrientation = 'Top'
}
Set-UIBranding @brandingParams

# -------------------------------------------------------------
# STEP 1: Overview
# -------------------------------------------------------------

$overviewStep = @{
    Name        = 'Overview'
    Title       = 'Welcome'
    Order       = 1
    Icon        = '&#xE8D6;'
    Description = 'How the progress overlay behaves in cascading flows.'
}
Add-UIStep @overviewStep

$overviewBannerParams = @{
    Step = 'Overview'
    Title = 'Progress Overlay Demo'
    Subtitle = 'Watch how PoshUI provides visual feedback during dynamic data loading'
    Height = 160
    TitleFontSize = 28
    SubtitleFontSize = 15
    BackgroundColor = '#0891B2'
    GradientStart = '#0891B2'
    GradientEnd = '#06B6D4'
    IconPath = $sidebarIconPath
    IconPosition = 'Right'
    IconSize = 80
}
Add-UIBanner @overviewBannerParams

Add-UICard -Step 'Overview' -Type 'Info' -Title 'What to Watch For' -Content @'
This demo illustrates two key overlay interactions:

Dependent dropdowns trigger the overlay while data loads.
Nested dependencies can cause multiple overlays as chains resolve.

Timing Matters:
Overlays appear when operations take ~500ms or longer.

Learning Experience:
Each subsequent step includes a card explaining the scenario so you can reproduce it in your own scripts.

Pro Tip: Use Start-Sleep in your scripts to see overlays in action!
'@

# -------------------------------------------------------------
# STEP 2: Environment selection (base)
# -------------------------------------------------------------

$envStep = @{
    Name        = 'Environment'
    Title       = 'Environment'
    Order       = 2
    Icon        = '&#xE77C;'
    Description = 'Choose the base environment (loads instantly).'
}
Add-UIStep @envStep

$envBannerParams = @{
    Step = 'Environment'
    Title = 'Base Environment Selection'
    Subtitle = 'Static selection - no overlay appears for instant loading'
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

Add-UICard -Step 'Environment' -Type 'Info' -Title 'Base Selection' -Content @'
The first dropdown is static, so no overlay appears here.

Purpose:
Use this selection to drive dependencies in later steps.

How it works:
- Static choices load instantly
- No processing delay = no overlay
- Serves as foundation for cascading dependencies

Next Steps:
Your Environment selection will influence:
- Available regions in Step 3
- Server options in Step 4
- Application choices in Step 5

Learning: Static controls do not trigger overlays - only dynamic ones!
'@

Add-UIDropdown -Step 'Environment' -Name 'TargetEnvironment' -Label 'Target Environment' -Choices @('Development', 'Staging', 'Production') -Default 'Development' -Mandatory

# -------------------------------------------------------------
# STEP 3: Region (single dependency)
# -------------------------------------------------------------

$regionStep = @{
    Name        = 'Region'
    Title       = 'Region'
    Order       = 3
    Icon        = '&#xE909;'
    Description = 'Demonstrates overlay for a single dependency.'
}
Add-UIStep @regionStep

$regionBannerParams = @{
    Step = 'Region'
    Title = 'Single Dependency Demo'
    Subtitle = 'Watch the overlay appear when environment changes trigger data reload'
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

Add-UICard -Step 'Region' -Type 'Info' -Title 'Single Dependency Overlay' -Content @'
Changing the Environment parameter causes this dropdown to re-run its data source.
We intentionally pause for 2 seconds (Start-Sleep) so the launcher detects the delay
and displays the overlay. Any dependency change that takes longer than ~500 ms
automatically triggers the overlay.
'@

$regionDropdownParams = @{
    Step         = 'Region'
    Name         = 'TargetRegion'
    Label        = 'Target Region'
    ScriptBlock  = {
        param($TargetEnvironment)

        Start-Sleep -Seconds 2

        switch ($TargetEnvironment) {
            'Development' { @('Dev-US-East', 'Dev-US-West', 'Dev-EU-Central') }
            'Staging'     { @('Stage-US-East', 'Stage-EU-West') }
            'Production'  { @('Prod-US-East-1', 'Prod-US-West-1', 'Prod-EU-Central-1', 'Prod-APAC-1') }
            default       { @('Unknown') }
        }
    }
    DependsOn    = @('TargetEnvironment')
    Mandatory    = $true
}
Add-UIDropdown @regionDropdownParams

# -------------------------------------------------------------
# STEP 4: Server (multi dependency)
# -------------------------------------------------------------

$serverStep = @{
    Name        = 'Server'
    Title       = 'Server'
    Order       = 4
    Icon        = '&#xE9CE;'
    Description = 'Demonstrates overlays for multi-parameter dependencies.'
}
Add-UIStep @serverStep

$serverBannerParams = @{
    Step = 'Server'
    Title = 'Multi-Parameter Dependencies'
    Subtitle = 'Observe overlays when both environment and region affect server choices'
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

Add-UICard -Step 'Server' -Type 'Info' -Title 'Cascading Dependencies' -Content @'
This dropdown depends on both Environment and Region. When either parent changes,
the script block runs again, waits 1.5 seconds, and the overlay appears while
choices are recalculated. Chained dependencies often show multiple overlays.
'@

$serverDropdownParams = @{
    Step         = 'Server'
    Name         = 'TargetServer'
    Label        = 'Target Server'
    ScriptBlock  = {
        param($TargetEnvironment, $TargetRegion)

        Start-Sleep -Milliseconds 1500

        $prefix = switch ($TargetEnvironment) {
            'Production' { 'PROD' }
            'Staging'    { 'STG' }
            default      { 'DEV' }
        }

        $regionCode = ($TargetRegion -split '-')[-1]

        1..5 | ForEach-Object { "$prefix-$regionCode-Server-$_" }
    }
    DependsOn    = @('TargetEnvironment', 'TargetRegion')
    Mandatory    = $true
}
Add-UIDropdown @serverDropdownParams

# -------------------------------------------------------------
# STEP 5: Additional example (listbox dependency)
# -------------------------------------------------------------

$appsStep = @{
    Name        = 'Applications'
    Title       = 'Applications'
    Order       = 5
    Icon        = '&#xE8FD;'
    Description = 'Shows overlays with a listbox fed by a script block.'
}
Add-UIStep @appsStep

$appsBannerParams = @{
    Step = 'Applications'
    Title = 'ListBox with Dependencies'
    Subtitle = 'Multi-select listbox also shows overlays when dependent on server selection'
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
Add-UIBanner @appsBannerParams

Add-UICard -Step 'Applications' -Type 'Info' -Title 'Bonus Scenario' -Content @'
This listbox depends on the Target Server. After you pick a server we wait 1.2 seconds
to simulate gathering app metadata, so the overlay flashes again. Any time a dependent
parameter change causes slow work, the overlay provides feedback to the user.
'@

$appListParams = @{
    Step         = 'Applications'
    Name         = 'Applications'
    Label        = 'Applications to Deploy'
    ScriptBlock  = {
        param($TargetServer)

        Start-Sleep -Milliseconds 1200

        if ([string]::IsNullOrWhiteSpace($TargetServer)) {
            return @()
        }

        @(
            'Config Service'
            'Telemetry Gateway'
            'Metrics Collector'
            "$TargetServer Backup Agent"
        )
    }
    DependsOn    = @('TargetServer')
    Mandatory    = $false
}
Add-UIListBox @appListParams

# -------------------------------------------------------------
# STEP 6: Summary and notes
# -------------------------------------------------------------

$summaryStep = @{
    Name        = 'Summary'
    Title       = 'Summary'
    Order       = 6
    Icon        = '&#xE9F1;'
    Description = 'Capture final notes after exploring overlays.'
}
Add-UIStep @summaryStep

$summaryBannerParams = @{
    Step = 'Summary'
    Title = 'Overlay Observations'
    Subtitle = 'Capture your findings about overlay behavior and timing'
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

Add-UICard -Step 'Summary' -Type 'Info' -Title 'Observations' -Content @'
Consider documenting what caused overlays and how long each dependency took.
Use this space to collect qualitative feedback from testers after they explore the scenarios.
'@

Add-UIMultiLine -Step 'Summary' -Name 'OverlayNotes' -Label 'Overlay Observations' -Rows 5 -HelpText 'Record your findings here.'

# -------------------------------------------------------------
# Render wizard
# -------------------------------------------------------------

Show-PoshUIWizard -ScriptBody {
    Write-Host ''
    Write-Host ('=' * 40) -ForegroundColor Green
    Write-Host 'Cascading Overlay Results' -ForegroundColor Green
    Write-Host ('=' * 40) -ForegroundColor Green
    Write-Host "Environment: $TargetEnvironment" -ForegroundColor Cyan
    Write-Host "Region:      $TargetRegion" -ForegroundColor Cyan
    Write-Host "Server:      $TargetServer" -ForegroundColor Cyan

    if ($Applications) {
        Write-Host 'Applications:' -ForegroundColor Cyan
        $Applications | ForEach-Object { Write-Host "  - $_" -ForegroundColor Cyan }
    } else {
        Write-Host 'Applications: (none selected)' -ForegroundColor Yellow
    }

    if ($OverlayNotes) {
        Write-Host ''
        Write-Host 'Overlay Notes:' -ForegroundColor Cyan
        Write-Host $OverlayNotes -ForegroundColor Cyan
    }

    Write-Host ''
    Write-Host 'Remember: overlays appear whenever a dependent script block runs long enough to trigger progress feedback.' -ForegroundColor Green
}


