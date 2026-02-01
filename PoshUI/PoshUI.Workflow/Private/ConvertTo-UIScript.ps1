# ConvertTo-UIScript.ps1 - Script generation engine for Workflow AST parsing
# Converts UIDefinition with workflow tasks to a PowerShell script that the EXE can AST-parse

function ConvertTo-UIScript {
    <#
    .SYNOPSIS
    Converts a UIDefinition object to a traditional parameter-based PowerShell script.
    
    .DESCRIPTION
    Internal function that generates a PowerShell script with param() block and attributes
    that matches the AST parsing format. This version supports Workflow steps with tasks.
    
    .PARAMETER Definition
    The UIDefinition object to convert to a script.
    
    .OUTPUTS
    String containing the generated PowerShell script.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [object]$Definition
    )
    
    Write-Verbose "Converting UI definition to script: $($Definition.Title)"
    
    try {
        $scriptLines = @()
        $scriptLines += "# Generated PoshUI Workflow script"
        $scriptLines += "# Created: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        $scriptLines += "# Workflow: $($Definition.Title)"
        $scriptLines += ""
        
        # Start param block
        $scriptLines += "param("
        
        $parameterLines = @()
        
        # Add branding parameter
        $needsBranding = (-not [string]::IsNullOrEmpty($Definition.Title)) -or
                        ($Definition.Branding.Count -gt 0) -or 
                        (-not [string]::IsNullOrEmpty($Definition.SidebarHeaderText)) -or
                        (-not [string]::IsNullOrEmpty($Definition.SidebarHeaderIcon))
        
        if ($needsBranding) {
            $brandingLines = @()
            $brandingLines += "    [Parameter(Mandatory=`$false)]"
            
            $brandingParts = @()
            
            $hasExplicitWindowTitle = $Definition.Branding.ContainsKey('WindowTitleText') -and 
                                     (-not [string]::IsNullOrEmpty($Definition.Branding['WindowTitleText']))
            
            if (-not $hasExplicitWindowTitle -and (-not [string]::IsNullOrEmpty($Definition.Title))) {
                $brandingParts += "WindowTitleText = '$($Definition.Title)'"
            }
            
            $addedKeys = @{}
            
            foreach ($key in $Definition.Branding.Keys) {
                $value = $Definition.Branding[$key]
                if ([string]::IsNullOrEmpty($value)) {
                    continue
                }
                $brandingParts += "$key = '$value'"
                $addedKeys[$key] = $true
            }
            
            if (-not $addedKeys.ContainsKey('SidebarHeaderText') -and 
                -not [string]::IsNullOrEmpty($Definition.SidebarHeaderText)) {
                $brandingParts += "SidebarHeaderText = '$($Definition.SidebarHeaderText)'"
            }
            
            if (-not $addedKeys.ContainsKey('SidebarHeaderIconPath') -and 
                -not [string]::IsNullOrEmpty($Definition.SidebarHeaderIcon)) {
                $brandingParts += "SidebarHeaderIconPath = '$($Definition.SidebarHeaderIcon)'"
            }
            
            if (-not [string]::IsNullOrEmpty($Definition.Theme)) {
                $brandingParts += "Theme = '$($Definition.Theme)'"
            }
            
            # Check if this is a resume scenario - skip to workflow step
            if ($Definition.Variables -and $Definition.Variables['_ResumeState'] -and $Definition.Variables['_ResumeState'].IsResume) {
                $brandingParts += "SkipToWorkflow = 'true'"
                Write-Verbose "Resume detected - adding SkipToWorkflow flag"
            }
            
            # Pass custom log path if specified
            if ($Definition.Variables -and $Definition.Variables['_LogPath']) {
                $logPath = $Definition.Variables['_LogPath'] -replace "'", "''"
                $brandingParts += "LogPath = '$logPath'"
                Write-Verbose "Custom log path: $logPath"
            }
            
            # Pass previous log file path for restoring log content on resume
            if ($Definition.Variables -and $Definition.Variables['_PreviousLogFilePath']) {
                $prevLogPath = $Definition.Variables['_PreviousLogFilePath'] -replace "'", "''"
                $brandingParts += "PreviousLogFilePath = '$prevLogPath'"
                Write-Verbose "Previous log file path for resume: $prevLogPath"
            }
            
            $brandingAttribute = "[WizardBranding($($brandingParts -join ', '))]"
            $brandingLines += "    $brandingAttribute"
            $brandingLines += "    [string]`$BrandingPlaceholder,"
            $brandingLines += ""
            
            $parameterLines += $brandingLines
        }
        
        # Get completed tasks from Variables (set by Resume-UIWorkflow)
        $completedTasks = @{}
        if ($Definition.Variables -and $Definition.Variables['_CompletedTasks']) {
            $completedTasks = $Definition.Variables['_CompletedTasks']
            Write-Verbose "Found $($completedTasks.Count) completed tasks from resume state"
        }
        
        # Process steps in order
        $sortedSteps = $Definition.Steps | Sort-Object Order
        
        foreach ($step in $sortedSteps) {
            Write-Verbose "Processing step: $($step.Name) (Type: $($step.Type))"
            
            $parameterLines += "    # --- Step: $($step.Title) ---"
            
            if ($step.Type -eq 'Workflow') {
                # Generate workflow tasks JSON with completed tasks info
                $parameterLines += Convert-UIWorkflowStep -Step $step -CompletedTasks $completedTasks
            } else {
                # Standard wizard step with controls
                $parameterLines += Convert-UIFormStep -Step $step
            }
            $parameterLines += ""
        }
        
        # Remove trailing comma from last parameter
        if ($parameterLines.Count -gt 0) {
            for ($i = $parameterLines.Count - 1; $i -ge 0; $i--) {
                $line = $parameterLines[$i]
                if (-not [string]::IsNullOrWhiteSpace($line) -and $line.Trim().EndsWith(',')) {
                    $parameterLines[$i] = $line.TrimEnd(',')
                    break
                }
            }
        }
        
        $scriptLines += $parameterLines
        $scriptLines += ")"
        $scriptLines += ""
        
        $scriptLines += "# --- Default Script Body ---"
        $scriptLines += "Write-Host 'Workflow completed successfully!' -ForegroundColor Green"
        
        $generatedScript = $scriptLines -join "`n"
        
        Write-Verbose "Successfully generated script ($($scriptLines.Count) lines)"
        
        return $generatedScript
    }
    catch {
        Write-Error "Failed to convert UI definition to script: $($_.Exception.Message)"
        throw
    }
}

function Convert-UIWorkflowStep {
    param(
        [object]$Step,
        [hashtable]$CompletedTasks = @{}
    )
    
    Write-Verbose "Converting workflow step: $($Step.Title)"
    
    $lines = @()
    
    # Get workflow from step properties
    $workflow = $Step.Properties['Workflow']
    if (-not $workflow -or $workflow.Tasks.Count -eq 0) {
        Write-Warning "Workflow step '$($Step.Name)' has no tasks"
        $lines += "    [Parameter(Mandatory=`$false)]"
        $stepAttribute = "[WizardStep('$($Step.Title)', $($Step.Order), PageType='Workflow'"
        if ($Step.Description) {
            $stepAttribute += ", Description='$($Step.Description)'"
        }
        if ($Step.Icon) {
            $stepAttribute += ", IconPath='$($Step.Icon)'"
        }
        $stepAttribute += ")]"
        $lines += "    $stepAttribute"
        $lines += "    [string]`$WorkflowPlaceholder_$($Step.Name),"
        return $lines
    }
    
    # Build tasks JSON for WizardWorkflowTasks attribute
    $tasksArray = @()
    foreach ($task in $workflow.Tasks) {
        $taskData = @{
            Name = $task.Name
            Title = $task.Title
            Order = $task.Order
            TaskType = $task.TaskType.ToString()
            ErrorAction = $task.ErrorAction
        }
        
        if ($task.Description) { $taskData.Description = $task.Description }
        if ($task.Icon) { $taskData.Icon = $task.Icon }
        if ($task.ScriptPath) { $taskData.ScriptPath = $task.ScriptPath }
        if ($task.ScriptBlock) {
            # Store script block as string
            $taskData.ScriptBlock = $task.ScriptBlock.ToString()
        }
        if ($task.Arguments) { $taskData.Arguments = $task.Arguments }
        if ($task.ApprovalMessage) { $taskData.ApprovalMessage = $task.ApprovalMessage }
        if ($task.ApproveButtonText -ne 'Approve') { $taskData.ApproveButtonText = $task.ApproveButtonText }
        if ($task.RejectButtonText -ne 'Reject') { $taskData.RejectButtonText = $task.RejectButtonText }
        if ($task.RequireReason) { $taskData.RequireReason = $task.RequireReason }
        if ($task.TimeoutMinutes -gt 0) { $taskData.TimeoutMinutes = $task.TimeoutMinutes }
        if ($task.DefaultTimeoutAction -ne 'None') { $taskData.DefaultTimeoutAction = $task.DefaultTimeoutAction }

        # Advanced task properties
        if ($task.RetryCount -gt 0) { $taskData.RetryCount = $task.RetryCount }
        if ($task.RetryDelaySeconds -ne 5) { $taskData.RetryDelaySeconds = $task.RetryDelaySeconds }
        if ($task.TimeoutSeconds -gt 0) { $taskData.TimeoutSeconds = $task.TimeoutSeconds }
        if ($task.SkipCondition) { $taskData.SkipCondition = $task.SkipCondition }
        if ($task.SkipReason) { $taskData.SkipReason = $task.SkipReason }
        if ($task.Group) { $taskData.Group = $task.Group }
        if ($task.RollbackScriptPath) { $taskData.RollbackScriptPath = $task.RollbackScriptPath }
        if ($task.RollbackScriptBlock) { $taskData.RollbackScriptBlock = $task.RollbackScriptBlock.ToString() }
        
        # Check if this task was completed in a previous run (resume scenario)
        if ($CompletedTasks.ContainsKey($task.Name)) {
            $taskData.PreCompleted = $true
            $taskData.PreCompletedStatus = $CompletedTasks[$task.Name].Status
            $taskData.PreCompletedProgress = $CompletedTasks[$task.Name].ProgressPercent
            $taskData.PreCompletedMessage = $CompletedTasks[$task.Name].ProgressMessage
            # Restore OutputLines from saved state
            if ($CompletedTasks[$task.Name].OutputLines) {
                $taskData.PreCompletedOutputLines = @($CompletedTasks[$task.Name].OutputLines)
            }
            Write-Verbose "  Task '$($task.Name)' marked as pre-completed from resume state"
        }
        
        $tasksArray += $taskData
    }
    
    # Serialize tasks to JSON
    $tasksJson = $tasksArray | ConvertTo-Json -Compress -Depth 10
    # Escape single quotes for PowerShell attribute
    $escapedJson = $tasksJson -replace "'", "''"
    
    $lines += "    [Parameter(Mandatory=`$false)]"
    
    # Add step attribute with Workflow type
    $stepAttribute = "[WizardStep('$($Step.Title)', $($Step.Order), PageType='Workflow'"
    if ($Step.Description) {
        $stepAttribute += ", Description='$($Step.Description)'"
    }
    if ($Step.Icon) {
        $stepAttribute += ", IconPath='$($Step.Icon)'"
    }
    $stepAttribute += ")]"
    $lines += "    $stepAttribute"
    
    # Add workflow tasks attribute with JSON
    $lines += "    [WizardWorkflowTasks('$escapedJson')]"
    $lines += "    [string]`$WorkflowTasks_$($Step.Name),"
    
    return $lines
}

function Convert-UIFormStep {
    param([object]$Step)
    
    Write-Verbose "Converting form step: $($Step.Title) with $($Step.Controls.Count) controls"
    
    $lines = @()
    $isFirstControl = $true
    
    # Separate display controls (Banner, InfoCard) from input controls
    $displayControls = @($Step.Controls | Where-Object { $_.Type -in @('Banner', 'InfoCard') })
    $inputControls = @($Step.Controls | Where-Object { $_.Type -notin @('Banner', 'InfoCard') })
    
    Write-Verbose "  Found $($displayControls.Count) display controls, $($inputControls.Count) input controls"
    
    if ($inputControls.Count -eq 0) {
        # Step has no input controls - create placeholder with display controls
        $lines += "    [Parameter(Mandatory=`$false)]"
        $stepAttribute = "[WizardStep('$($Step.Title)', $($Step.Order)"
        if ($Step.Description) {
            $stepAttribute += ", Description='$($Step.Description)'"
        }
        if ($Step.Icon) {
            $stepAttribute += ", IconPath='$($Step.Icon)'"
        }
        $stepAttribute += ")]"
        $lines += "    $stepAttribute"
        
        # Add Banner attributes for display controls
        foreach ($dc in $displayControls) {
            $lines += Convert-UIDisplayControl -Control $dc
        }
        
        $lines += "    [string]`$StepPlaceholder_$($Step.Name),"
        return $lines
    }
    
    # First, add a placeholder parameter for Banner/Card display controls
    if ($displayControls.Count -gt 0) {
        $lines += "    [Parameter(Mandatory=`$false)]"
        $stepAttribute = "[WizardStep('$($Step.Title)', $($Step.Order)"
        if ($Step.Description) {
            $stepAttribute += ", Description='$($Step.Description)'"
        }
        if ($Step.Icon) {
            $stepAttribute += ", IconPath='$($Step.Icon)'"
        }
        $stepAttribute += ")]"
        $lines += "    $stepAttribute"
        
        # Add Banner/Card attributes to placeholder
        foreach ($dc in $displayControls) {
            $lines += Convert-UIDisplayControl -Control $dc
        }
        
        $lines += "    [string]`$DisplayPlaceholder_$($Step.Name),"
        $lines += ""
    }
    
    # Then add input controls
    foreach ($control in $inputControls) {
        Write-Verbose "  Processing control: Name=$($control.Name), Type=$($control.Type)"
        
        if ($isFirstControl -and $displayControls.Count -eq 0) {
            # Only add step attribute to first control if no display controls
            $lines += "    [Parameter(Mandatory=`$$($control.Mandatory.ToString().ToLower()))]" 
            $stepAttribute = "[WizardStep('$($Step.Title)', $($Step.Order)"
            if ($Step.Description) {
                $stepAttribute += ", Description='$($Step.Description)'"
            }
            if ($Step.Icon) {
                $stepAttribute += ", IconPath='$($Step.Icon)'"
            }
            $stepAttribute += ")]"
            $lines += "    $stepAttribute"
            $isFirstControl = $false
        } else {
            $lines += "    [Parameter(Mandatory=`$$($control.Mandatory.ToString().ToLower()))]"
            $lines += "    [WizardStep('$($Step.Title)', $($Step.Order))]"
        }
        
        $lines += Convert-UIControl -Control $control
    }
    
    return $lines
}

function Convert-UIDisplayControl {
    param([object]$Control)
    
    $lines = @()
    
    if ($Control.Type -eq 'Banner') {
        try {
        Write-Verbose "Processing Banner control: $($Control.Name)"
        # Build Banner JSON from control properties (matching Wizard module)
        $bannerData = @{
            Title = $Control.GetPropertyOrDefault('BannerTitle', $Control.Label)
            Subtitle = $Control.GetPropertyOrDefault('BannerSubtitle', $null)
            Description = $Control.GetPropertyOrDefault('Description', $null)
            IconPath = $Control.GetPropertyOrDefault('IconPath', $null)
            IconSize = $Control.GetPropertyOrDefault('IconSize', 64)
            IconPosition = $Control.GetPropertyOrDefault('IconPosition', 'Right')
            Type = $Control.GetPropertyOrDefault('BannerType', 'info')
            BackgroundColor = $Control.GetPropertyOrDefault('BackgroundColor', $null)
            BackgroundImagePath = $Control.GetPropertyOrDefault('BackgroundImagePath', $null)
            BackgroundImageOpacity = $Control.GetPropertyOrDefault('BackgroundImageOpacity', 0.3)
            GradientStart = $Control.GetPropertyOrDefault('GradientStart', $null)
            GradientEnd = $Control.GetPropertyOrDefault('GradientEnd', $null)
            Height = $Control.GetPropertyOrDefault('Height', 180)
            ButtonText = $Control.GetPropertyOrDefault('ButtonText', $null)
            ButtonIcon = $Control.GetPropertyOrDefault('ButtonIcon', $null)
            ButtonColor = $Control.GetPropertyOrDefault('ButtonColor', '#0078D4')
            TitleColor = $Control.GetPropertyOrDefault('TitleColor', $null)
            SubtitleColor = $Control.GetPropertyOrDefault('SubtitleColor', $null)
            TitleFontSize = $Control.GetPropertyOrDefault('TitleFontSize', $null)
            SubtitleFontSize = $Control.GetPropertyOrDefault('SubtitleFontSize', $null)
            AutoRotate = $Control.GetPropertyOrDefault('AutoRotate', $false)
            RotateInterval = $Control.GetPropertyOrDefault('RotateInterval', 3000)
        }
        
        # Handle CarouselItems
        $carouselItems = $Control.GetPropertyOrDefault('CarouselItems', $null)
        if ($carouselItems -and $carouselItems.Count -gt 0) {
            $bannerData.CarouselItems = $carouselItems
        }
        
        # Handle icon - convert HTML entity to Unicode if needed
        $iconValue = $Control.GetPropertyOrDefault('BannerIcon', $null)
        if ($iconValue -match '&#x([0-9A-Fa-f]+);') {
            $hexValue = $matches[1]
            $charCode = [Convert]::ToInt32($hexValue, 16)
            $bannerData.Icon = [char]$charCode
        } elseif ($iconValue) {
            $bannerData.Icon = $iconValue
        }
        
        # Convert to JSON, then Base64 encode for safe attribute passing
        $bannerJson = $bannerData | ConvertTo-Json -Compress -Depth 10
        $bannerBytes = [System.Text.Encoding]::UTF8.GetBytes($bannerJson)
        $bannerBase64 = [Convert]::ToBase64String($bannerBytes)
        
        $lines += "    [UIBanner('BASE64:$bannerBase64')]"
        Write-Verbose "Banner serialized successfully: $($bannerBase64.Substring(0, [Math]::Min(50, $bannerBase64.Length)))..."
        }
        catch {
            Write-Warning "Failed to serialize Banner control: $($_.Exception.Message)"
        }
    }
    elseif ($Control.Type -eq 'InfoCard') {
        # Build Card attribute from control properties using GetPropertyOrDefault
        $cardAttr = "[WizardCard("
        $cardParts = @()
        
        $title = $Control.GetPropertyOrDefault('CardTitle', $Control.Label)
        if ($title) { $cardParts += "Title='$title'" }
        
        $content = $Control.GetPropertyOrDefault('CardContent', $null)
        if ($content) { 
            $escapedContent = $content -replace "'", "''" -replace "`r`n", " " -replace "`n", " "
            $cardParts += "Content='$escapedContent'" 
        }
        
        $cardType = $Control.GetPropertyOrDefault('CardType', $null)
        if ($cardType) { $cardParts += "Type='$cardType'" }
        
        $icon = $Control.GetPropertyOrDefault('CardIcon', $null)
        if ($icon) {
            # Convert HTML entity to Unicode character if needed
            if ($icon -match '&#x([0-9A-Fa-f]+);') {
                $hexValue = $matches[1]
                $charCode = [Convert]::ToInt32($hexValue, 16)
                $icon = [char]$charCode
            }
            $cardParts += "Icon='$icon'"
        }
        
        $iconPath = $Control.GetPropertyOrDefault('CardIconPath', $null)
        if ($iconPath) { $cardParts += "IconPath='$iconPath'" }
        
        $imagePath = $Control.GetPropertyOrDefault('CardImagePath', $null)
        if ($imagePath) { $cardParts += "ImagePath='$imagePath'" }
        
        $linkUrl = $Control.GetPropertyOrDefault('CardLinkUrl', $null)
        if ($linkUrl) { $cardParts += "LinkUrl='$linkUrl'" }
        
        $linkText = $Control.GetPropertyOrDefault('CardLinkText', $null)
        if ($linkText) { $cardParts += "LinkText='$linkText'" }
        
        $bgColor = $Control.GetPropertyOrDefault('CardBackgroundColor', $null)
        if ($bgColor) { $cardParts += "BackgroundColor='$bgColor'" }
        
        $titleColor = $Control.GetPropertyOrDefault('CardTitleColor', $null)
        if ($titleColor) { $cardParts += "TitleColor='$titleColor'" }
        
        $contentColor = $Control.GetPropertyOrDefault('CardContentColor', $null)
        if ($contentColor) { $cardParts += "ContentColor='$contentColor'" }
        
        $cornerRadius = $Control.GetPropertyOrDefault('CardCornerRadius', $null)
        if ($cornerRadius) { $cardParts += "CornerRadius=$cornerRadius" }
        
        $gradientStart = $Control.GetPropertyOrDefault('CardGradientStart', $null)
        if ($gradientStart) { $cardParts += "GradientStart='$gradientStart'" }
        
        $gradientEnd = $Control.GetPropertyOrDefault('CardGradientEnd', $null)
        if ($gradientEnd) { $cardParts += "GradientEnd='$gradientEnd'" }
        
        $cardAttr += ($cardParts -join ', ') + ")]"
        $lines += "    $cardAttr"
    }
    
    return $lines
}

function Convert-UIControl {
    param([object]$Control)
    
    $lines = @()
    
    # Add parameter details attribute
    $detailsAttribute = "[WizardParameterDetails(Label='$($Control.Label)'"
    if ($Control.Width -gt 0) {
        $detailsAttribute += ", ControlWidth=$($Control.Width)"
    }
    $detailsAttribute += ")]"
    $lines += "    $detailsAttribute"
    
    # Determine parameter type based on control type
    $paramType = '[string]'
    
    switch ($Control.Type) {
        'TextBox' { $paramType = '[string]' }
        'Password' { $paramType = '[SecureString]' }
        'Checkbox' { $paramType = '[bool]' }
        'Toggle' { 
            $paramType = '[switch]'
            $lines += "    [WizardSwitch]"
        }
        'Dropdown' {
            if ($Control.Choices -and $Control.Choices.Count -gt 0) {
                $choicesString = ($Control.Choices | ForEach-Object { "'$_'" }) -join ', '
                $lines += "    [ValidateSet($choicesString)]"
            }
        }
        'Numeric' {
            $paramType = '[double]'
            $lines += "    [WizardNumeric]"
        }
        'Date' {
            $lines += "    [WizardDate]"
        }
        default { $paramType = '[string]' }
    }
    
    # Add parameter declaration
    $paramDeclaration = "    $paramType`$$($Control.Name)"
    if ($null -ne $Control.Default -and $Control.Default -ne '') {
        if ($Control.Type -in @('Checkbox', 'Toggle')) {
            $paramDeclaration += " = `$$($Control.Default)"
        }
        elseif ($Control.Type -eq 'Numeric') {
            $paramDeclaration += " = $($Control.Default)"
        }
        elseif ($Control.Type -ne 'Password') {
            $paramDeclaration += " = '$($Control.Default)'"
        }
    }
    $paramDeclaration += ","
    
    $lines += $paramDeclaration
    
    return $lines
}
