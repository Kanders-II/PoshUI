<#
.SYNOPSIS
    Complete Workflow Demo - All Features
.DESCRIPTION
    Demonstrates all workflow features including:
    - Wizard inputs passed to workflow tasks as variables
    - Normal tasks with ScriptBlock
    - Tasks with ScriptPath (dot-sourced scripts)
    - Tasks with Arguments
    - Approval gates with RequireReason and Timeout
    - OnError Continue mode
    - Progress tracking and output logging
    - File logging (View Logs button)
#>

# Import the workflow module
$modulePath = Join-Path $PSScriptRoot '..\PoshUI.Workflow\PoshUI.Workflow.psd1'
Import-Module $modulePath -Force

# Get branding assets
$scriptIconPath = Join-Path $PSScriptRoot 'Logo Files\Icons\browser.png'
$sidebarIconPath = Join-Path $PSScriptRoot 'Logo Files\png\Color logo - no background.png'

# Create the workflow UI
New-PoshUIWorkflow -Title "Complete Workflow Demo" `
                   -Description "Demonstrates all workflow features" `
                   -Theme Auto

# Set branding
Set-UIBranding -WindowTitle "Complete Workflow Demo" `
               -SidebarHeaderIcon $sidebarIconPath `
             

# ============================================================================
# STEP 1: CONFIGURATION (Wizard inputs that will be passed to workflow tasks)
# ============================================================================
Add-UIStep -Name "Config" -Title "Configuration" -Order 1


Add-UICard -Step "Config" -Title "About This Demo" -Type Info `
    -BackgroundColor "#1e293b" `
    -TitleColor "#00D4AA" `
    -ContentColor "#e2e8f0" `
    -CornerRadius 12 `
    -LinkUrl "https://kanders-ii.github.io/PoshUI/" `
    -LinkText "View Documentation" `
    -Content @"
This demo shows all workflow features:
- Wizard inputs become workflow variables
- OnError Continue mode
- ScriptPath for external scripts
- Task Arguments
- Approval gates with reason requirement
- File logging with View Logs button
"@

Add-UITextBox -Step "Config" -Name "ProjectName" -Label "Project Name" `
              -Default "MyProject" -Mandatory -HelpText "Name of the project"

Add-UITextBox -Step "Config" -Name "ServerName" -Label "Target Server" `
              -Default "SERVER01" -Mandatory -HelpText "Server to deploy to"

Add-UIDropdown -Step "Config" -Name "Environment" -Label "Environment" `
               -Choices @("Development", "Staging", "Production") -Default "Development"

Add-UICheckbox -Step "Config" -Name "EnableLogging" -Label "Enable Detailed Logging" `
               -Default $true

# ============================================================================
# STEP 2: WORKFLOW EXECUTION
# ============================================================================
Add-UIStep -Name "Deploy" -Title "Deployment" -Type Workflow -Order 2 `
           -Description "Execute deployment tasks"

# Task 1: Initialize - Uses wizard inputs as variables
Add-UIWorkflowTask -Step "Deploy" -Name "Initialize" -Title "Initialize Deployment" -Order 1 `
    -Description "Validates configuration using wizard inputs" `
    -ScriptBlock {
        # Wizard inputs are available as variables!
        $PoshUIWorkflow.WriteOutput("=== Deployment Initialization ===", "INFO")
        $PoshUIWorkflow.WriteOutput("Project: $ProjectName", "INFO")
        $PoshUIWorkflow.WriteOutput("Server: $ServerName", "INFO")
        $PoshUIWorkflow.WriteOutput("Environment: $Environment", "INFO")
        
        $PoshUIWorkflow.UpdateProgress(25, "Checking prerequisites...")
        Start-Sleep -Milliseconds 500
        
        $PoshUIWorkflow.UpdateProgress(50, "Validating configuration...")
        Start-Sleep -Milliseconds 500
        
        if ($EnableLogging) {
            $PoshUIWorkflow.WriteOutput("Detailed logging is ENABLED", "INFO")
        }
        
        $PoshUIWorkflow.UpdateProgress(75, "Creating workspace...")
        Start-Sleep -Milliseconds 500
        
        $PoshUIWorkflow.UpdateProgress(100, "Initialization complete!")
        $PoshUIWorkflow.WriteOutput("Ready to deploy $ProjectName to $ServerName", "INFO")
    }

# Task 2: Pre-flight with OnError Continue
Add-UIWorkflowTask -Step "Deploy" -Name "PreFlight" -Title "Pre-flight Checks" -Order 2 `
    -Description "System checks (continues on error)" `
    -OnError Continue `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("=== Pre-flight Checks ===", "INFO")
        
        $checks = @("Disk Space", "Memory", "Network", "Permissions")
        $progress = 0
        
        foreach ($check in $checks) {
            $progress += 25
            $PoshUIWorkflow.UpdateProgress($progress, "Checking $check...")
            $PoshUIWorkflow.WriteOutput("$check check: OK", "INFO")
            Start-Sleep -Milliseconds 400
        }
        
        $PoshUIWorkflow.WriteOutput("All checks passed!", "INFO")
    }

# Task 3: Approval Gate with RequireReason
Add-UIWorkflowTask -Step "Deploy" -Name "Approval" -Title "Deployment Approval" -Order 3 `
    -TaskType ApprovalGate `
    -ApprovalMessage "Ready to deploy. Please review the configuration and approve to continue." `
    -ApproveButtonText "Approve Deployment" `
    -RejectButtonText "Cancel" `
    -RequireReason `
    -TimeoutMinutes 30

# Task 4: Deploy with Arguments
Add-UIWorkflowTask -Step "Deploy" -Name "DeployApp" -Title "Deploy Application" -Order 4 `
    -Description "Deploys application files" `
    -Arguments @{
        MaxRetries = 3
        BackupFirst = $true
    } `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("=== Application Deployment ===", "INFO")
        $PoshUIWorkflow.WriteOutput("Max Retries: $MaxRetries", "INFO")
        $PoshUIWorkflow.WriteOutput("Backup First: $BackupFirst", "INFO")
        
        $steps = @(
            "Stopping services",
            "Backing up current version",
            "Copying application files",
            "Updating configuration",
            "Starting services"
        )
        
        $progress = 0
        $increment = 100 / $steps.Count
        
        foreach ($step in $steps) {
            $progress += $increment
            $PoshUIWorkflow.UpdateProgress([int]$progress, $step)
            $PoshUIWorkflow.WriteOutput("$step...", "INFO")
            Start-Sleep -Milliseconds 600
        }
        
        $PoshUIWorkflow.WriteOutput("Deployment complete!", "INFO")
    }

# Task 5: Verification
Add-UIWorkflowTask -Step "Deploy" -Name "Verify" -Title "Verify Deployment" -Order 5 `
    -Description "Validates deployment success" `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("=== Deployment Verification ===", "INFO")
        
        $PoshUIWorkflow.UpdateProgress(33, "Checking application health...")
        Start-Sleep -Milliseconds 500
        $PoshUIWorkflow.WriteOutput("Application health: OK", "INFO")
        
        $PoshUIWorkflow.UpdateProgress(66, "Testing connectivity...")
        Start-Sleep -Milliseconds 500
        $PoshUIWorkflow.WriteOutput("Connectivity test: PASSED", "INFO")
        
        $PoshUIWorkflow.UpdateProgress(100, "All verifications passed!")
        $PoshUIWorkflow.WriteOutput("Deployment verified successfully!", "INFO")
        $PoshUIWorkflow.WriteOutput("$ProjectName is now running on $ServerName ($Environment)", "INFO")
    }

# Show the workflow
$result = Show-PoshUIWorkflow

# Display results
if ($result) {
    Write-Host "`nWorkflow completed!" -ForegroundColor Green
    Write-Host "Check the log file at: $env:LOCALAPPDATA\PoshUI\Logs\" -ForegroundColor Cyan
}

