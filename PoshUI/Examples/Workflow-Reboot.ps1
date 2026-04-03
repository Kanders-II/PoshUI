#Requires -Version 5.1
<#
.SYNOPSIS
    Demonstrates Workflow reboot/resume functionality.

.DESCRIPTION
    This demo shows how Workflow can save state, handle a reboot request,
    and resume from where it left off. When a task calls RequestReboot(),
    the workflow state is saved and a "Reboot Now" button appears.

    AUTO-RESUME FEATURE:
    When "Reboot Now" is clicked, PoshUI automatically:
    1. Saves the workflow state to an encrypted file
    2. Registers a Windows scheduled task to re-run this script at logon
    3. Initiates the system reboot
    4. After reboot, the scheduled task runs the script automatically
    5. The script detects saved state and resumes from where it left off
    6. When the workflow completes, the scheduled task is removed

    This provides a seamless reboot/resume experience without manual intervention.

.EXAMPLE
    .\Workflow-Reboot.ps1

.NOTES
    The auto-resume scheduled task runs at user logon with highest privileges.
    The workflow state is encrypted with DPAPI (only readable by the same user).
    
    For testing without actual reboot:
    - Close the window when reboot is requested
    - Run the script again manually to see resume behavior
#>

# Import the workflow module
$modulePath = Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Workflow\PoshUI.Workflow.psd1'
Import-Module $modulePath -Force

# Get branding assets
$scriptIconPath = Join-Path $PSScriptRoot 'Logo Files\Icons\browser.png'
$sidebarIconPath = Join-Path $PSScriptRoot 'Logo Files\png\Color logo - no background.png'

# Define icon paths (using existing icons from Icon8 folder)
$iconBase = Join-Path $PSScriptRoot 'Icon8'
$iconConfig = Join-Path $iconBase 'icons8-settings-100.png'
$iconExecution = Join-Path $iconBase 'icons8-advance-100.png'
$iconPhase1 = Join-Path $iconBase 'icons8-gears-100.png'
$iconReboot = Join-Path $iconBase 'icons8-back-to-100.png'
$iconPhase2 = Join-Path $iconBase 'icons8-brand-new-100.png'
$iconFinalize = Join-Path $iconBase 'icons8-check-mark-100.png'
$iconServer = Join-Path $iconBase 'icons8-workstation-100.png'

# Check for saved state (resume scenario)
$isResume = Test-UIWorkflowState

if ($isResume) {
    Write-Host "=" * 60 -ForegroundColor Green
    Write-Host "  RESUMING WORKFLOW FROM SAVED STATE" -ForegroundColor Green
    Write-Host "=" * 60 -ForegroundColor Green
    $savedState = Get-UIWorkflowState
    Write-Host "Last saved: $($savedState.LastSaveTime)" -ForegroundColor DarkGray
    Write-Host "Tasks completed: $($savedState.CurrentTaskIndex)" -ForegroundColor DarkGray
    Write-Host ""
} else {
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host "  STARTING FRESH WORKFLOW" -ForegroundColor Cyan
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host ""
}

# Create the workflow UI
New-PoshUIWorkflow -Title "Reboot Demo" `
                   -Description "Demonstrates reboot/resume capability" `
                   -Theme Auto

# ============================================================================
# CUSTOM THEMES - Sleek modern look
# ============================================================================

# Custom Dark Theme - Deep Space with Cyan accents
$darkTheme = @{
    AccentColor = '#00BCD4'
    AccentDark = '#0097A7'
    Background = '#0D1117'
    ContentBackground = '#161B22'
    SidebarBackground = '#0D1117'
    TextPrimary = '#E6EDF3'
    TextSecondary = '#8B949E'
    BorderColor = '#30363D'
    SuccessColor = '#3FB950'
    WarningColor = '#D29922'
    ErrorColor = '#F85149'
}

# Custom Light Theme - Clean Arctic with Teal accents
$lightTheme = @{
    AccentColor = '#0097A7'
    AccentDark = '#00796B'
    Background = '#F0F4F8'
    ContentBackground = '#FFFFFF'
    SidebarBackground = '#E8EEF2'
    TextPrimary = '#1A202C'
    TextSecondary = '#4A5568'
    BorderColor = '#CBD5E0'
    SuccessColor = '#38A169'
    WarningColor = '#D69E2E'
    ErrorColor = '#E53E3E'
}

# Apply both themes (dark and light modes)
Set-UITheme -Dark $darkTheme -Light $lightTheme

# Set branding with icons
Set-UIBranding -WindowTitle "Reboot Demo" `
               -WindowTitleIcon $scriptIconPath `
               -SidebarHeaderText "Reboot Demo" `
               -SidebarHeaderIcon $sidebarIconPath `
               -SidebarHeaderIconOrientation 'Top'

# Resume from saved state if available
if ($isResume) {
    Resume-UIWorkflow
}

# Step 1: Welcome/Configuration page
Add-UIStep -Name "Config" -Title "Configuration" -Order 1 -IconPath $iconConfig

# Carousel banner with clickable links
$carouselItems = @(
    @{
        Title = 'Reboot/Resume Demo'
        Subtitle = 'Demonstrates state persistence across reboots'
        BackgroundColor = '#0078D4'
        LinkUrl = 'https://kanders-ii.github.io/PoshUI/'
        Clickable = $true
    },
    @{
        Title = 'PowerShell Workflows'
        Subtitle = 'Build robust automation with state management'
        BackgroundColor = '#107C10'
        LinkUrl = 'https://kanders-ii.github.io/PoshUI/'
        Clickable = $true
    },
    @{
        Title = 'Modern UI Design'
        Subtitle = 'Beautiful Windows 11-style interfaces'
        BackgroundColor = '#8764B8'
        LinkUrl = 'https://learn.microsoft.com/windows/apps/design/'
        Clickable = $true
    }
)

Add-UIBanner -Step "Config" -CarouselItems $carouselItems -AutoRotate -RotateInterval 4000 -Height 150

if ($isResume) {
    Add-UICard -Step "Config" -Title "Resuming Workflow" -Type Info -Content @"
A saved workflow state was detected!

This workflow will resume from where it left off:
- Previously completed tasks will be skipped
- Execution continues from the next pending task

Click Next to continue the workflow.
"@
} else {
    Add-UICard -Step "Config" -Title "About This Demo" -Type Info -Content @"
This demo shows how workflows can survive reboots.

The workflow will:
1. Execute Phase 1 tasks
2. Request a simulated reboot
3. Save state to disk
4. Close the wizard

Run this script again to see it resume from Phase 2!
"@
}

Add-UITextBox -Step "Config" -Name "ServerName" -Label "Server Name" `
              -Default $env:COMPUTERNAME -Mandatory -IconPath $iconServer

# Step 2: Workflow execution
Add-UIStep -Name "Execution" -Title "Execution" -Type Workflow -Order 2 `
           -Description "Multi-phase deployment with reboot" -IconPath $iconExecution

# Task 1: Phase 1 - Pre-reboot tasks
Add-UIWorkflowTask -Step "Execution" -Name "Phase1" -Title "Phase 1: Pre-Reboot Setup" -Order 1 `
    -Description "Initial configuration before reboot" `
    -IconPath $iconPhase1 `
    -ScriptBlock {
        # Progress is auto-tracked based on WriteOutput calls - no need for UpdateProgress!
        $PoshUIWorkflow.WriteOutput("Starting Phase 1 configuration...", "INFO")
        Start-Sleep -Milliseconds 800
        
        $PoshUIWorkflow.WriteOutput("Checking system requirements...", "INFO")
        Start-Sleep -Milliseconds 800
        $PoshUIWorkflow.WriteOutput("System requirements verified", "INFO")
        
        $PoshUIWorkflow.WriteOutput("Installing prerequisites...", "INFO")
        Start-Sleep -Milliseconds 1000
        $PoshUIWorkflow.WriteOutput("Prerequisites installed", "INFO")
        
        $PoshUIWorkflow.WriteOutput("Configuring services...", "INFO")
        Start-Sleep -Milliseconds 800
        $PoshUIWorkflow.WriteOutput("Services configured", "INFO")
        
        $PoshUIWorkflow.WriteOutput("Applying initial settings...", "INFO")
        Start-Sleep -Milliseconds 600
        $PoshUIWorkflow.WriteOutput("Initial settings applied", "INFO")
        
        $PoshUIWorkflow.WriteOutput("Phase 1 completed successfully!", "INFO")
    }

# Task 2: Reboot Request
Add-UIWorkflowTask -Step "Execution" -Name "RebootRequest" -Title "System Reboot Required" -Order 2 `
    -Description "Reboot needed to apply Phase 1 changes" `
    -IconPath $iconReboot `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Phase 1 changes require a system reboot.", "WARN")
        $PoshUIWorkflow.WriteOutput("Preparing for reboot...", "INFO")
        Start-Sleep -Milliseconds 500
        
        # Request reboot - this will save state and show reboot options
        $PoshUIWorkflow.RequestReboot("Reboot required to apply Phase 1 configuration changes")
        
        # Code after RequestReboot executes after resume
        $PoshUIWorkflow.WriteOutput("System rebooted successfully", "INFO")
    }

# Task 3: Phase 2 - Post-reboot tasks
Add-UIWorkflowTask -Step "Execution" -Name "Phase2" -Title "Phase 2: Post-Reboot Configuration" -Order 3 `
    -Description "Configuration after system reboot" `
    -IconPath $iconPhase2 `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Starting Phase 2 configuration...", "INFO")
        Start-Sleep -Milliseconds 600
        
        $PoshUIWorkflow.WriteOutput("Verifying reboot completed...", "INFO")
        Start-Sleep -Milliseconds 600
        $PoshUIWorkflow.WriteOutput("Reboot verification passed", "INFO")
        
        $PoshUIWorkflow.WriteOutput("Applying post-reboot settings...", "INFO")
        Start-Sleep -Milliseconds 800
        $PoshUIWorkflow.WriteOutput("Post-reboot settings applied", "INFO")
        
        $PoshUIWorkflow.WriteOutput("Starting services...", "INFO")
        Start-Sleep -Milliseconds 600
        $PoshUIWorkflow.WriteOutput("Services started", "INFO")
        
        $PoshUIWorkflow.WriteOutput("Phase 2 completed successfully!", "INFO")
    }

# Task 4: Finalize
Add-UIWorkflowTask -Step "Execution" -Name "Finalize" -Title "Finalize Deployment" -Order 4 `
    -Description "Final cleanup and verification" `
    -IconPath $iconFinalize `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Finalizing deployment...", "INFO")
        Start-Sleep -Milliseconds 500
        
        $PoshUIWorkflow.WriteOutput("Running final checks...", "INFO")
        Start-Sleep -Milliseconds 500
        
        $PoshUIWorkflow.WriteOutput("Cleaning up temporary files...", "INFO")
        Start-Sleep -Milliseconds 500
        
        $PoshUIWorkflow.WriteOutput("All phases completed successfully!", "INFO")
    }

# Show the workflow
$result = Show-PoshUIWorkflow

# Handle result
if ($result) {
    # Check if workflow completed or was paused for reboot
    $stateFile = Join-Path $env:LOCALAPPDATA 'PoshUI\PoshUI_Workflow_State.json'
    
    if (Test-Path $stateFile) {
        Write-Host ""
        Write-Host "=" * 60 -ForegroundColor Yellow
        Write-Host "  WORKFLOW PAUSED - REBOOT PENDING" -ForegroundColor Yellow
        Write-Host "=" * 60 -ForegroundColor Yellow
        Write-Host ""
        Write-Host "State has been saved to:" -ForegroundColor Cyan
        Write-Host "  $stateFile" -ForegroundColor DarkGray
        Write-Host ""
        Write-Host "To test resume functionality:" -ForegroundColor Cyan
        Write-Host "  1. Run this script again" -ForegroundColor White
        Write-Host "  2. The workflow will resume from where it left off" -ForegroundColor White
        Write-Host "  3. Remaining tasks (Phase 2, Finalize) will execute" -ForegroundColor White
        Write-Host ""
    } else {
        # Workflow completed - clear any saved state
        Clear-UIWorkflowState -ErrorAction SilentlyContinue
        
        Write-Host ""
        Write-Host "=" * 60 -ForegroundColor Green
        Write-Host "  WORKFLOW COMPLETED SUCCESSFULLY!" -ForegroundColor Green
        Write-Host "=" * 60 -ForegroundColor Green
        Write-Host ""
        Write-Host "All deployment phases completed." -ForegroundColor Green
        Write-Host "Server: $($result.ServerName)" -ForegroundColor White
    }
} else {
    Write-Host ""
    Write-Host "Workflow was cancelled." -ForegroundColor Yellow
}

