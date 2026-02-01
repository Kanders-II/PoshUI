function Add-UIMetricCard {
    <#
    .SYNOPSIS
    Adds a metric card (KPI display) to a UI step.
    
    .DESCRIPTION
    Creates a metric card that displays a single numeric value with optional unit, trend indicator, and target progress bar.
    Metric cards are ideal for displaying KPIs, system metrics, and performance indicators.
    
    .PARAMETER Step
    Name of the step to add this card to. The step must already exist.
    
    .PARAMETER Name
    Unique name for the card.
    
    .PARAMETER Title
    Display title for the card.
    
    .PARAMETER Description
    Short description shown below the card title.
    
    .PARAMETER Value
    The numeric value to display. Can be a number or a ScriptBlock that returns a number.
    When a ScriptBlock is provided, it is executed once for initial display and automatically used as RefreshScript.
    
    .PARAMETER Unit
    Unit suffix (e.g., %, GB, items).
    
    .PARAMETER Format
    Number format string (e.g., 'N0', 'N2', 'P0'). Default is 'N0'.
    
    .PARAMETER Trend
    Trend indicator: 'up', 'down', or 'stable'.
    
    .PARAMETER TrendValue
    Numeric trend value to display with the trend indicator.
    
    .PARAMETER Target
    Target value for progress bar. If specified, a progress bar is displayed.
    
    .PARAMETER MinValue
    Minimum value for the progress bar. Default is 0.
    
    .PARAMETER MaxValue
    Maximum value for the progress bar. Default is 100.
    
    .PARAMETER Icon
    Optional icon glyph (e.g., '&#xE7C4;').
    
    .PARAMETER Category
    Category for grouping/filtering cards.
    
    .PARAMETER RefreshScript
    PowerShell script block to re-fetch the value. If not specified and Value is a ScriptBlock, Value is used as RefreshScript.
    
    .EXAMPLE
    Add-UIMetricCard -Step "Dashboard" -Name "CPU" -Title "CPU Usage" -Value 75.5 -Unit "%" -Trend "up" -Target 80
    
    Adds a metric card showing CPU usage with trend and target.
    
    .EXAMPLE
    Add-UIMetricCard -Step "Dashboard" -Name "Memory" -Title "Memory Usage" `
        -Value { (Get-CimInstance Win32_OperatingSystem).TotalVisibleMemorySize / 1MB } `
        -Unit "GB" -Target 16
    
    Adds a metric card with dynamic value from a script block.
    
    .OUTPUTS
    UIControl object representing the created metric card.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string]$Step,
        
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullOrEmpty()]
        [string]$Name,
        
        [Parameter(Mandatory = $true, Position = 2)]
        [ValidateNotNullOrEmpty()]
        [string]$Title,
        
        [Parameter()]
        [string]$Description,
        
        [Parameter(Mandatory = $true)]
        [object]$Value,
        
        [Parameter()]
        [string]$Unit,
        
        [Parameter()]
        [string]$Format = "N0",
        
        [Parameter()]
        [ValidateSet("up", "down", "stable", "")]
        [string]$Trend,
        
        [Parameter()]
        [double]$TrendValue,
        
        [Parameter()]
        [double]$Target,
        
        [Parameter()]
        [double]$MinValue = 0,
        
        [Parameter()]
        [double]$MaxValue = 100,
        
        [Parameter()]
        [string]$Icon,
        
        [Parameter()]
        [string]$Category = "General",
        
        [Parameter()]
        [scriptblock]$RefreshScript
    )
    
    begin {
        Write-Verbose "Adding MetricCard - $Name to step $Step"
        
        if (-not $script:CurrentWizard) {
            $errorMsg = Get-UIValidationErrorMessage `
                -Message "No UI initialized" `
                -Suggestions @(
                    "Call New-PoshUIDashboard to initialize the dashboard",
                    "Ensure you're using the correct module (PoshUI.Dashboard)"
                ) `
                -Context "Add-UIMetricCard"
            throw $errorMsg
        }
        
        Test-UIStepExists -StepName $Step -UIDefinition $script:CurrentWizard -Context "Add-UIMetricCard"
    }
    
    process {
        try {
            $wizardStep = $script:CurrentWizard.GetStep($Step)
            
            Test-UIControlNameUnique -ControlName $Name -UIStep $wizardStep -StepName $Step -Context "Add-UIMetricCard"
            
            $control = [UIControl]::new($Name, $Title, 'metriccard')
            
            $control.SetProperty('Type', 'metriccard')
            $control.SetProperty('CardTitle', $Title)
            $control.SetProperty('CardDescription', $Description)
            $control.SetProperty('Category', $Category)
            
            if ($Icon) {
                $control.SetProperty('Icon', $Icon)
            }
            
            $actualValue = $Value
            $refreshScriptToUse = $RefreshScript
            
            if ($Value -is [scriptblock]) {
                Write-Verbose "Executing Value ScriptBlock for MetricCard '$Name'..."
                try {
                    $actualValue = & $Value
                    Write-Verbose "ScriptBlock returned value: $actualValue"
                    if (-not $refreshScriptToUse) {
                        $refreshScriptToUse = $Value
                        Write-Verbose "Auto-configured RefreshScript from Value ScriptBlock"
                    }
                }
                catch {
                    $errorMsg = Get-UIValidationErrorMessage `
                        -Message "Failed to execute Value ScriptBlock for MetricCard '$Name'" `
                        -Suggestions @(
                            "Check that the ScriptBlock syntax is valid",
                            "Ensure all cmdlets/functions used in the ScriptBlock are available",
                            "Test the ScriptBlock independently: & { $($Value.ToString()) }",
                            "Error details: $($_.Exception.Message)"
                        ) `
                        -Context "Add-UIMetricCard"
                    Write-Error $errorMsg
                    $actualValue = 0
                }
            }
            
            $control.SetProperty('Value', $actualValue)
            $control.SetProperty('Unit', $Unit)
            $control.SetProperty('Format', $Format)
            $control.SetProperty('Trend', $Trend)
            $control.SetProperty('TrendValue', $TrendValue)
            $control.SetProperty('Target', $Target)
            $control.SetProperty('MinValue', $MinValue)
            $control.SetProperty('MaxValue', $MaxValue)
            $control.SetProperty('ShowProgressBar', ($Target -gt 0))
            $control.SetProperty('ShowTrend', (-not [string]::IsNullOrEmpty($Trend)))
            $control.SetProperty('ShowTarget', ($Target -gt 0))
            
            if ($refreshScriptToUse) {
                $scriptBlockContent = $refreshScriptToUse.ToString().Trim()
                if ($scriptBlockContent.StartsWith('{') -and $scriptBlockContent.EndsWith('}')) {
                    $scriptBlockContent = $scriptBlockContent.Substring(1, $scriptBlockContent.Length - 2).Trim()
                }
                $control.SetProperty('RefreshScript', $scriptBlockContent)
            }
            
            $control.Mandatory = $false
            $wizardStep.AddControl($control)
            
            Write-Verbose "Successfully added MetricCard control: $Name"
            return $control
        }
        catch {
            Write-Error "Failed to add MetricCard '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }
}

Set-Alias -Name 'Add-UIKPICard' -Value 'Add-UIMetricCard' -Force
