<#
.SYNOPSIS
    Comprehensive demonstration of all PoshUI Workflow features.

.DESCRIPTION
    This demo showcases:
    - Custom dual theme (Set-UITheme with separate -Light and -Dark palettes)
    - Carousel banner with PNG icons on each slide
    - PNG icons on input controls
    - Inter-task data passing (SetData/GetData)
    - Task retry mechanism with configurable delay
    - Task timeout support
    - Conditional task execution (skip conditions)
    - Task grouping/phases
    - Rollback scripts
    - Approval gates
    - Progress reporting
    - Reboot/resume support

.NOTES
    Run this script to see all workflow features in action.
#>

# Import the workflow module
Import-Module "$PSScriptRoot\..\PoshUI.Workflow\PoshUI.Workflow.psd1" -Force

# Define icon paths for workflow tasks (using existing icons from Icon8 folder)
$iconBase = "$PSScriptRoot\Icon8"
$taskIconSystemCheck = Join-Path $iconBase 'icons8-system-report-100.png'
$taskIconNetwork = Join-Path $iconBase 'icons8-network-cable-100.png'
$taskIconTimeout = Join-Path $iconBase 'icons8-stopwatch-100.png'
$taskIconDatabase = Join-Path $iconBase 'icons8-dns-100.png'
$taskIconWebServer = Join-Path $iconBase 'icons8-website-100.png'
$taskIconFileServer = Join-Path $iconBase 'icons8-folder-100.png'
$taskIconComponents = Join-Path $iconBase 'icons8-gears-100.png'
$taskIconConfig = Join-Path $iconBase 'icons8-settings-100.png'
$taskIconApproval = Join-Path $iconBase 'icons8-checked-radio-button-100.png'
$taskIconVerify = Join-Path $iconBase 'icons8-check-mark-100.png'
$taskIconRollback = Join-Path $iconBase 'icons8-back-to-100.png'

# Icon paths for wizard controls
$wizardIconServerName = Join-Path $iconBase 'icons8-create-document-100.png'
$wizardIconServerType = Join-Path $iconBase 'icons8-bunch-of-keys-100.png'
$wizardIconEnvironment = Join-Path $iconBase 'icons8-america-100.png'
$wizardIconSimulateFailure = Join-Path $iconBase 'icons8-conflict-100.png'
$wizardIconSimulateTimeout = Join-Path $iconBase 'icons8-hourglass-100.png'

# Icon paths for steps and banner
$stepIconConfig = Join-Path $iconBase 'icons8-settings-100.png'
$stepIconExecution = Join-Path $iconBase 'icons8-forward-button-100.png'
$bannerIcon = Join-Path $iconBase 'icons8-workstation-100.png'

# Initialize the workflow
New-PoshUIWorkflow -Title "Workflow Features Demo" `
    -Description "Demonstrates all workflow capabilities" `
    -Theme Dark

# ============================================================================
# CUSTOM THEME: Separate light and dark color palettes
# ============================================================================

Set-UITheme -Dark @{
    AccentColor        = '#00D4AA'
    Background         = '#0B1120'
    ContentBackground  = '#111827'
    CardBackground     = '#1E293B'
    SidebarBackground  = '#0F172A'
    SidebarText        = '#94A3B8'
    TextPrimary        = '#F1F5F9'
    TextSecondary      = '#94A3B8'
    InputBackground    = '#1E293B'
    BorderColor        = '#334155'
    TitleBarBackground = '#0B1120'
    TitleBarText       = '#F1F5F9'
    SuccessColor       = '#10B981'
    ErrorColor         = '#EF4444'
    WarningColor       = '#F59E0B'
} -Light @{
    AccentColor        = '#0891B2'
    Background         = '#F0F9FF'
    ContentBackground  = '#FFFFFF'
    CardBackground     = '#F8FAFC'
    SidebarBackground  = '#E0F2FE'
    SidebarText        = '#334155'
    TextPrimary        = '#0F172A'
    TextSecondary      = '#64748B'
    InputBackground    = '#FFFFFF'
    BorderColor        = '#CBD5E1'
    TitleBarBackground = '#E0F2FE'
    TitleBarText       = '#0F172A'
    SuccessColor       = '#059669'
    ErrorColor         = '#DC2626'
    WarningColor       = '#D97706'
}

# ============================================================================
# WIZARD PHASE: Collect configuration from user
# ============================================================================

Add-UIStep -Name 'Config' -Title 'Configuration' -Order 1 -IconPath $stepIconConfig `
    -Description 'Configure server deployment parameters and workflow options'

# Banner icon paths for carousel slides
$slideIconWorkstation = Join-Path $iconBase 'icons8-workstation-100.png'
$slideIconGears = Join-Path $iconBase 'icons8-gears-100.png'
$slideIconShield = Join-Path $iconBase 'icons8-warning-shield-100.png'
$slideIconCloud = Join-Path $iconBase 'icons8-upload-to-cloud-100.png'

$carouselSlides = @(
    @{
        Title = 'Workflow Features Demo'
        Subtitle = 'Inter-task data passing, retry, timeout, conditional execution, approval gates'
        BackgroundColor = '#0F4C75'
        IconPath = $slideIconWorkstation
        IconSize = 56
    },
    @{
        Title = 'Custom Themes & PNG Icons'
        Subtitle = 'Dual light/dark themes with Set-UITheme and PNG icons on every control'
        BackgroundColor = '#1B5E20'
        IconPath = $slideIconGears
        IconSize = 56
    },
    @{
        Title = 'Rollback & Error Handling'
        Subtitle = 'Automatic retry, task timeout, rollback scripts, and error recovery'
        BackgroundColor = '#B71C1C'
        IconPath = $slideIconShield
        IconSize = 56
    },
    @{
        Title = 'Approval Gates & Grouping'
        Subtitle = 'Production approval workflows with task grouping and conditional skip'
        BackgroundColor = '#4A148C'
        IconPath = $slideIconCloud
        IconSize = 56
    }
)

Add-UIBanner -Step 'Config' `
    -CarouselItems $carouselSlides `
    -Height 150 `
    -TitleFontSize 26 `
    -SubtitleFontSize 14 `
    -AutoRotate `
    -RotateInterval 4000

Add-UITextBox -Step 'Config' -Name 'ServerName' -Label 'Server Name' `
    -Default 'DEMO-SERVER' -Mandatory -IconPath $wizardIconServerName

Add-UIDropdown -Step 'Config' -Name 'ServerType' -Label 'Server Type' `
    -Choices @('WebServer', 'Database', 'FileServer', 'Application') `
    -Default 'WebServer' -IconPath $wizardIconServerType

Add-UIDropdown -Step 'Config' -Name 'Environment' -Label 'Environment' `
    -Choices @('Development', 'Staging', 'Production') `
    -Default 'Development' -IconPath $wizardIconEnvironment

Add-UICheckbox -Step 'Config' -Name 'SimulateFailure' -Label 'Simulate task failure (to demo retry)' `
    -Default $false -IconPath $wizardIconSimulateFailure

Add-UICheckbox -Step 'Config' -Name 'SimulateTimeout' -Label 'Simulate task timeout' `
    -Default $false -IconPath $wizardIconSimulateTimeout

# ============================================================================
# WORKFLOW PHASE: Execute tasks with all features
# ============================================================================

Add-UIStep -Name 'Execution' -Title 'Deployment' -Order 2 -Type Workflow -IconPath $stepIconExecution

# ----------------------------------------------------------------------------
# GROUP 1: Pre-flight Checks
# ----------------------------------------------------------------------------

# Task 1: System Check - Demonstrates data passing (using WriteOutput for auto-progress)
Add-UIWorkflowTask -Step 'Execution' -Name 'SystemCheck' -Title 'System Requirements Check' `
    -Group 'Pre-flight Checks' `
    -Description 'Verifies system meets requirements and stores results for later tasks' `
    -IconPath $taskIconSystemCheck `
    -ScriptBlock {
        # Using WriteOutput pattern - progress auto-advances with each call
        $PoshUIWorkflow.WriteOutput("Checking system requirements...", "INFO")

        # Simulate checking various requirements
        Start-Sleep -Milliseconds 500
        $PoshUIWorkflow.WriteOutput("Checking CPU...", "INFO")
        $cpuOk = $true

        Start-Sleep -Milliseconds 500
        $PoshUIWorkflow.WriteOutput("Checking Memory...", "INFO")
        $memoryOk = $true

        Start-Sleep -Milliseconds 500
        $PoshUIWorkflow.WriteOutput("Checking Disk Space...", "INFO")
        $diskOk = $true

        # FEATURE: Inter-task data passing - store results for later tasks
        $PoshUIWorkflow.SetData('SystemCheckPassed', ($cpuOk -and $memoryOk -and $diskOk))
        $PoshUIWorkflow.SetData('CheckTimestamp', (Get-Date).ToString())
        $PoshUIWorkflow.SetData('ServerName', $ServerName)

        $PoshUIWorkflow.WriteOutput("System check passed. Results stored for subsequent tasks.", "INFO")
    }

# Task 2: Network Check - Demonstrates retry mechanism (using WriteOutput for auto-progress)
Add-UIWorkflowTask -Step 'Execution' -Name 'NetworkCheck' -Title 'Network Connectivity Check' `
    -Group 'Pre-flight Checks' `
    -Description 'Checks network with automatic retry on failure' `
    -IconPath $taskIconNetwork `
    -RetryCount 3 `
    -RetryDelaySeconds 2 `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Testing network connectivity...", "INFO")

        # FEATURE: Retry mechanism demo
        # If SimulateFailure is checked, this will fail and retry 3 times
        if ($SimulateFailure) {
            # Check if we've retried enough (use workflow data to track)
            $retryAttempt = $PoshUIWorkflow.GetData('NetworkRetryAttempt')
            if ($null -eq $retryAttempt) { $retryAttempt = 0 }
            $retryAttempt++
            $PoshUIWorkflow.SetData('NetworkRetryAttempt', $retryAttempt)

            if ($retryAttempt -lt 3) {
                $PoshUIWorkflow.WriteOutput("Network check failed (attempt $retryAttempt)", "ERR")
                throw "Simulated network failure - will retry"
            }
            $PoshUIWorkflow.WriteOutput("Network check succeeded on attempt $retryAttempt", "INFO")
        }

        $PoshUIWorkflow.WriteOutput("Network connectivity verified", "INFO")
        $PoshUIWorkflow.SetData('NetworkOk', $true)
    }

# Task 3: Timeout Demo - Demonstrates task timeout (using WriteOutput for auto-progress)
Add-UIWorkflowTask -Step 'Execution' -Name 'TimeoutDemo' -Title 'Timeout Demonstration' `
    -Group 'Pre-flight Checks' `
    -Description 'Shows task timeout feature (5 second limit)' `
    -IconPath $taskIconTimeout `
    -TimeoutSeconds 5 `
    -OnError Continue `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Starting timeout demo...", "INFO")

        # FEATURE: Task timeout - if SimulateTimeout is checked, this will exceed the 5 second limit
        if ($SimulateTimeout) {
            $PoshUIWorkflow.WriteOutput("Simulating long-running task (will timeout)...", "WARN")
            Start-Sleep -Seconds 10  # This exceeds the 5 second timeout
        } else {
            $PoshUIWorkflow.WriteOutput("Running quick task (within timeout)...", "INFO")
            Start-Sleep -Seconds 1
        }

        $PoshUIWorkflow.WriteOutput("Timeout demo completed successfully", "INFO")
    }

# ----------------------------------------------------------------------------
# GROUP 2: Installation
# ----------------------------------------------------------------------------

# Task 4: Conditional Skip Demo - Skip based on server type (using UpdateProgress for precise control)
Add-UIWorkflowTask -Step 'Execution' -Name 'InstallSQL' -Title 'Install SQL Server' `
    -Group 'Installation' `
    -Description 'Only runs for Database server type' `
    -IconPath $taskIconDatabase `
    -SkipCondition '$ServerType -ne "Database"' `
    -SkipReason 'Not a database server - SQL installation skipped' `
    -ScriptBlock {
        # Using UpdateProgress pattern - explicit percentage control
        $PoshUIWorkflow.UpdateProgress(10, "Downloading SQL Server installer...")
        Start-Sleep -Milliseconds 800

        $PoshUIWorkflow.UpdateProgress(50, "Installing SQL Server components...")
        Start-Sleep -Milliseconds 800

        # Store installation path for later tasks
        $PoshUIWorkflow.SetData('SQLInstallPath', 'C:\Program Files\Microsoft SQL Server')
        $PoshUIWorkflow.SetData('SQLInstalled', $true)

        $PoshUIWorkflow.UpdateProgress(100, "SQL Server installed")
    }

# Task 5: Install IIS - Skip based on server type (using UpdateProgress for precise control)
Add-UIWorkflowTask -Step 'Execution' -Name 'InstallIIS' -Title 'Install IIS Web Server' `
    -Group 'Installation' `
    -Description 'Only runs for WebServer type' `
    -IconPath $taskIconWebServer `
    -SkipCondition '$ServerType -ne "WebServer"' `
    -SkipReason 'Not a web server - IIS installation skipped' `
    -ScriptBlock {
        # Using UpdateProgress pattern - explicit percentage control
        $PoshUIWorkflow.UpdateProgress(10, "Enabling IIS Windows feature...")
        Start-Sleep -Milliseconds 600

        $PoshUIWorkflow.UpdateProgress(40, "Installing management tools...")
        Start-Sleep -Milliseconds 600

        $PoshUIWorkflow.UpdateProgress(70, "Setting up default website...")
        Start-Sleep -Milliseconds 400

        # Store for later
        $PoshUIWorkflow.SetData('IISInstallPath', 'C:\inetpub\wwwroot')
        $PoshUIWorkflow.SetData('IISInstalled', $true)

        $PoshUIWorkflow.UpdateProgress(100, "IIS installed")
    }

# Task 6: Install File Services - Skip based on server type (using WriteOutput for auto-progress)
Add-UIWorkflowTask -Step 'Execution' -Name 'InstallFileServices' -Title 'Install File Services' `
    -Group 'Installation' `
    -Description 'Only runs for FileServer type' `
    -IconPath $taskIconFileServer `
    -SkipCondition '$ServerType -ne "FileServer"' `
    -SkipReason 'Not a file server - File Services installation skipped' `
    -ScriptBlock {
        # Using WriteOutput pattern - progress auto-advances
        $PoshUIWorkflow.WriteOutput("Enabling File Server role...", "INFO")
        Start-Sleep -Milliseconds 500

        $PoshUIWorkflow.WriteOutput("Configuring share permissions...", "INFO")
        Start-Sleep -Milliseconds 500

        $PoshUIWorkflow.SetData('FileServicesInstalled', $true)

        $PoshUIWorkflow.WriteOutput("File Services installation complete", "INFO")
    }

# Task 7: Common Installation - Uses data from previous tasks (using UpdateProgress for precise control)
Add-UIWorkflowTask -Step 'Execution' -Name 'InstallCommon' -Title 'Install Common Components' `
    -Group 'Installation' `
    -Description 'Installs components needed by all server types' `
    -IconPath $taskIconComponents `
    -ScriptBlock {
        # Using UpdateProgress pattern - explicit percentage control
        # FEATURE: Read data from previous tasks
        $serverName = $PoshUIWorkflow.GetData('ServerName')
        $checkTime = $PoshUIWorkflow.GetData('CheckTimestamp')

        $PoshUIWorkflow.UpdateProgress(10, "Installing on server: $serverName")
        Start-Sleep -Milliseconds 300

        $PoshUIWorkflow.UpdateProgress(30, "Installing monitoring agent...")
        Start-Sleep -Milliseconds 500

        $PoshUIWorkflow.UpdateProgress(60, "Applying security updates...")
        Start-Sleep -Milliseconds 500

        $PoshUIWorkflow.UpdateProgress(90, "Registering server...")
        Start-Sleep -Milliseconds 300

        $PoshUIWorkflow.SetData('CommonInstalled', $true)
        $PoshUIWorkflow.SetData('InstallCompletedAt', (Get-Date).ToString())

        $PoshUIWorkflow.UpdateProgress(100, "Common components installed")
    }

# ----------------------------------------------------------------------------
# GROUP 3: Configuration
# ----------------------------------------------------------------------------

# Task 8: Skip based on workflow data from earlier task (using WriteOutput for auto-progress)
Add-UIWorkflowTask -Step 'Execution' -Name 'ConfigureSQL' -Title 'Configure SQL Server' `
    -Group 'Configuration' `
    -Description 'Configures SQL if it was installed' `
    -IconPath $taskIconConfig `
    -SkipCondition '$WorkflowData["SQLInstalled"] -ne $true' `
    -SkipReason 'SQL Server was not installed - skipping configuration' `
    -ScriptBlock {
        # Using WriteOutput pattern - progress auto-advances
        $sqlPath = $PoshUIWorkflow.GetData('SQLInstallPath')
        $PoshUIWorkflow.WriteOutput("SQL installed at: $sqlPath", "INFO")

        $PoshUIWorkflow.WriteOutput("Configuring memory settings...", "INFO")
        Start-Sleep -Milliseconds 400

        $PoshUIWorkflow.WriteOutput("Setting up default database...", "INFO")
        Start-Sleep -Milliseconds 400

        $PoshUIWorkflow.WriteOutput("SQL Server configured", "INFO")
    }

# Task 9: Configure IIS - Skip based on workflow data (using WriteOutput for auto-progress)
Add-UIWorkflowTask -Step 'Execution' -Name 'ConfigureIIS' -Title 'Configure IIS' `
    -Group 'Configuration' `
    -Description 'Configures IIS if it was installed' `
    -IconPath $taskIconConfig `
    -SkipCondition '$WorkflowData["IISInstalled"] -ne $true' `
    -SkipReason 'IIS was not installed - skipping configuration' `
    -ScriptBlock {
        # Using WriteOutput pattern - progress auto-advances
        $iisPath = $PoshUIWorkflow.GetData('IISInstallPath')
        $PoshUIWorkflow.WriteOutput("IIS root: $iisPath", "INFO")

        $PoshUIWorkflow.WriteOutput("Configuring application pool...", "INFO")
        Start-Sleep -Milliseconds 400

        $PoshUIWorkflow.WriteOutput("Setting up bindings...", "INFO")
        Start-Sleep -Milliseconds 400

        $PoshUIWorkflow.WriteOutput("IIS configured", "INFO")
    }

# Task 10: Rollback Demo - Shows rollback script capability (using WriteOutput for auto-progress)
Add-UIWorkflowTask -Step 'Execution' -Name 'RollbackDemo' -Title 'Rollback Script Demo' `
    -Group 'Configuration' `
    -Description 'Demonstrates rollback script feature' `
    -IconPath $taskIconRollback `
    -OnError Continue `
    -RollbackScriptBlock {
        # This would run if the main task fails
        Write-Host "ROLLBACK: Cleaning up failed configuration..."
        Write-Host "ROLLBACK: Reverting changes..."
        Write-Host "ROLLBACK: Cleanup complete"
    } `
    -ScriptBlock {
        # Using WriteOutput pattern - progress auto-advances
        $PoshUIWorkflow.WriteOutput("This task has a rollback script defined", "INFO")
        $PoshUIWorkflow.WriteOutput("If this task failed, the rollback script would execute", "INFO")

        # Normally succeeds - uncomment throw to test rollback
        # throw "Simulated failure to trigger rollback"

        $PoshUIWorkflow.WriteOutput("Task completed successfully (no rollback needed)", "INFO")
    }

# ----------------------------------------------------------------------------
# GROUP 4: Approval & Verification
# ----------------------------------------------------------------------------

# Task 11: Approval Gate - Production only
Add-UIWorkflowTask -Step 'Execution' -Name 'ProdApproval' -Title 'Production Approval' `
    -Group 'Approval & Verification' `
    -Description 'Requires approval for production deployments' `
    -IconPath $taskIconApproval `
    -SkipCondition '$Environment -ne "Production"' `
    -SkipReason 'Not a production deployment - approval not required' `
    -TaskType ApprovalGate `
    -ApprovalMessage "You are about to deploy to PRODUCTION environment.`n`nServer: $ServerName`nType: $ServerType`n`nPlease review and approve to continue." `
    -ApproveButtonText 'Approve Deployment' `
    -RejectButtonText 'Cancel Deployment' `
    -RequireReason

# Task 12: Final Verification - Uses all stored data (using WriteOutput for auto-progress)
Add-UIWorkflowTask -Step 'Execution' -Name 'Verify' -Title 'Final Verification' `
    -Group 'Approval & Verification' `
    -Description 'Verifies deployment using data from all previous tasks' `
    -IconPath $taskIconVerify `
    -ScriptBlock {
        # Using WriteOutput pattern - progress auto-advances with each message
        $PoshUIWorkflow.WriteOutput("=== Deployment Summary ===", "INFO")

        # FEATURE: Access all data stored by previous tasks
        $serverName = $PoshUIWorkflow.GetData('ServerName')
        $checkTime = $PoshUIWorkflow.GetData('CheckTimestamp')
        $networkOk = $PoshUIWorkflow.GetData('NetworkOk')
        $installTime = $PoshUIWorkflow.GetData('InstallCompletedAt')

        $PoshUIWorkflow.WriteOutput("Server: $serverName", "INFO")
        $PoshUIWorkflow.WriteOutput("Environment: $Environment", "INFO")
        $PoshUIWorkflow.WriteOutput("Server Type: $ServerType", "INFO")
        $PoshUIWorkflow.WriteOutput("System Check: $checkTime", "INFO")
        $PoshUIWorkflow.WriteOutput("Network OK: $networkOk", "INFO")
        $PoshUIWorkflow.WriteOutput("Install Completed: $installTime", "INFO")

        # Check what was installed
        $PoshUIWorkflow.WriteOutput("", "INFO")
        $PoshUIWorkflow.WriteOutput("=== Installed Components ===", "INFO")

        if ($PoshUIWorkflow.HasData('SQLInstalled')) {
            $sqlPath = $PoshUIWorkflow.GetData('SQLInstallPath')
            $PoshUIWorkflow.WriteOutput("SQL Server: $sqlPath", "INFO")
        }

        if ($PoshUIWorkflow.HasData('IISInstalled')) {
            $iisPath = $PoshUIWorkflow.GetData('IISInstallPath')
            $PoshUIWorkflow.WriteOutput("IIS: $iisPath", "INFO")
        }

        if ($PoshUIWorkflow.HasData('FileServicesInstalled')) {
            $PoshUIWorkflow.WriteOutput("File Services: Installed", "INFO")
        }

        if ($PoshUIWorkflow.HasData('CommonInstalled')) {
            $PoshUIWorkflow.WriteOutput("Common Components: Installed", "INFO")
        }

        # List all stored data keys
        $PoshUIWorkflow.WriteOutput("", "INFO")
        $PoshUIWorkflow.WriteOutput("=== All Workflow Data Keys ===", "INFO")
        $allKeys = $PoshUIWorkflow.GetDataKeys()
        foreach ($key in $allKeys) {
            $PoshUIWorkflow.WriteOutput("  - $key", "INFO")
        }

        # Task info
        $currentIndex = $PoshUIWorkflow.CurrentTaskIndex
        $totalTasks = $PoshUIWorkflow.TotalTaskCount
        $PoshUIWorkflow.WriteOutput("", "INFO")
        $PoshUIWorkflow.WriteOutput("Task $($currentIndex + 1) of $totalTasks", "INFO")

        $PoshUIWorkflow.WriteOutput("", "INFO")
        $PoshUIWorkflow.WriteOutput("=== Deployment Complete ===", "INFO")
    }

# ============================================================================
# LAUNCH THE WORKFLOW
# ============================================================================

Show-PoshUIWorkflow
