function Add-UIChartCard {
    <#
    .SYNOPSIS
    Adds a chart card (graph visualization) to a UI step.
    
    .DESCRIPTION
    Creates a chart card that displays data as a line, bar, area, or pie chart.
    Chart cards are ideal for visualizing trends, comparisons, and distributions.
    
    .PARAMETER Step
    Name of the step to add this card to. The step must already exist.
    
    .PARAMETER Name
    Unique name for the card.
    
    .PARAMETER Title
    Display title for the card.
    
    .PARAMETER Description
    Short description shown below the card title.
    
    .PARAMETER ChartType
    Type of chart: 'Line', 'Bar', 'Area', or 'Pie'. Default is 'Line'.
    
    .PARAMETER Data
    Data to display. Can be an array of objects, a hashtable, or a ScriptBlock that returns data.
    When a ScriptBlock is provided, it is executed once for initial display and automatically used as RefreshScript.
    
    .PARAMETER ShowLegend
    Whether to display the chart legend. Default is $true.
    
    .PARAMETER ShowTooltip
    Whether to display tooltips on hover. Default is $true.
    
    .PARAMETER Icon
    Optional icon glyph (e.g., '&#xE7C4;').
    
    .PARAMETER Category
    Category for grouping/filtering cards.
    
    .PARAMETER RefreshScript
    PowerShell script block to re-fetch the data. If not specified and Data is a ScriptBlock, Data is used as RefreshScript.
    
    .EXAMPLE
    $data = @(
        @{Month='Jan'; Sales=100; Profit=20}
        @{Month='Feb'; Sales=150; Profit=35}
        @{Month='Mar'; Sales=120; Profit=25}
    )
    Add-UIChartCard -Step "Dashboard" -Name "Sales" -Title "Sales Trend" -ChartType "Line" -Data $data
    
    Adds a line chart showing sales data.
    
    .EXAMPLE
    Add-UIChartCard -Step "Dashboard" -Name "ProcessCPU" -Title "Process CPU Usage" -ChartType "Bar" `
        -Data { Get-Process | Select-Object -First 10 Name, CPU }
    
    Adds a bar chart with dynamic data from a script block.
    
    .OUTPUTS
    UIControl object representing the created chart card.
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
        
        [Parameter()]
        [ValidateSet('Line', 'Bar', 'Area', 'Pie')]
        [string]$ChartType = "Line",
        
        [Parameter(Mandatory = $true)]
        [object]$Data,
        
        [Parameter()]
        [bool]$ShowLegend = $true,
        
        [Parameter()]
        [bool]$ShowTooltip = $true,
        
        [Parameter()]
        [string]$Icon,
        
        [Parameter()]
        [string]$Category = "General",
        
        [Parameter()]
        [scriptblock]$RefreshScript
    )
    
    begin {
        Write-Verbose "Adding ChartCard - $Name to step $Step"
        
        if (-not $script:CurrentWizard) {
            $errorMsg = Get-UIValidationErrorMessage `
                -Message "No UI initialized" `
                -Suggestions @(
                    "Call New-PoshUIDashboard to initialize the dashboard",
                    "Ensure you're using the correct module (PoshUI.Dashboard)"
                ) `
                -Context "Add-UIChartCard"
            throw $errorMsg
        }
        
        Test-UIStepExists -StepName $Step -UIDefinition $script:CurrentWizard -Context "Add-UIChartCard"
    }
    
    process {
        try {
            $wizardStep = $script:CurrentWizard.GetStep($Step)
            
            Test-UIControlNameUnique -ControlName $Name -UIStep $wizardStep -StepName $Step -Context "Add-UIChartCard"
            
            $control = [UIControl]::new($Name, $Title, 'graphcard')
            
            $control.SetProperty('Type', 'graphcard')
            $control.SetProperty('CardTitle', $Title)
            $control.SetProperty('CardDescription', $Description)
            $control.SetProperty('Category', $Category)
            
            if ($Icon) {
                $control.SetProperty('Icon', $Icon)
            }
            
            $control.SetProperty('ChartType', $ChartType)
            $control.SetProperty('ShowLegend', $ShowLegend)
            $control.SetProperty('ShowTooltip', $ShowTooltip)
            
            $actualData = $Data
            $refreshScriptToUse = $RefreshScript
            
            if ($Data -is [scriptblock]) {
                Write-Verbose "Executing Data ScriptBlock for ChartCard '$Name'..."
                try {
                    $actualData = & $Data
                    Write-Verbose "ScriptBlock returned $(@($actualData).Count) data items"
                    if (-not $refreshScriptToUse) {
                        $refreshScriptToUse = $Data
                        Write-Verbose "Auto-configured RefreshScript from Data ScriptBlock"
                    }
                }
                catch {
                    $errorMsg = Get-UIValidationErrorMessage `
                        -Message "Failed to execute Data ScriptBlock for ChartCard '$Name'" `
                        -Suggestions @(
                            "Check that the ScriptBlock syntax is valid",
                            "Ensure the ScriptBlock returns data in format: @{Label='X'; Value=Y}",
                            "Verify all cmdlets/functions used in the ScriptBlock are available",
                            "Test the ScriptBlock independently: & { $($Data.ToString()) }",
                            "Error details: $($_.Exception.Message)"
                        ) `
                        -Context "Add-UIChartCard"
                    Write-Error $errorMsg
                    $actualData = @()
                }
            }
            
            if ($actualData) {
                if ($actualData -is [string] -and ($actualData.Trim().StartsWith('[') -or $actualData.Trim().StartsWith('{'))) {
                    $control.SetProperty('Data', $actualData.Trim())
                } else {
                    $dataJson = $actualData | ConvertTo-Json -Depth 5 -Compress
                    $control.SetProperty('Data', $dataJson)
                }
            }
            
            if ($refreshScriptToUse) {
                $scriptBlockContent = $refreshScriptToUse.ToString().Trim()
                if ($scriptBlockContent.StartsWith('{') -and $scriptBlockContent.EndsWith('}')) {
                    $scriptBlockContent = $scriptBlockContent.Substring(1, $scriptBlockContent.Length - 2).Trim()
                }
                $control.SetProperty('RefreshScript', $scriptBlockContent)
            }
            
            $control.Mandatory = $false
            $wizardStep.AddControl($control)
            
            Write-Verbose "Successfully added ChartCard control: $Name"
            return $control
        }
        catch {
            Write-Error "Failed to add ChartCard '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }
}

Set-Alias -Name 'Add-UIGraphCard' -Value 'Add-UIChartCard' -Force
