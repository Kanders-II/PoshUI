function Add-UITableCard {
    <#
    .SYNOPSIS
    Adds a data grid card (table visualization) to a UI step.
    
    .DESCRIPTION
    Creates a data grid card that displays tabular data with sorting, filtering, and export capabilities.
    Data grid cards are ideal for displaying lists, logs, and structured data.
    
    .PARAMETER Step
    Name of the step to add this card to. The step must already exist.
    
    .PARAMETER Name
    Unique name for the card.
    
    .PARAMETER Title
    Display title for the card.
    
    .PARAMETER Description
    Short description shown below the card title.
    
    .PARAMETER Data
    Data to display as a table. Can be an array of objects or a ScriptBlock that returns data.
    When a ScriptBlock is provided, it is executed once for initial display and automatically used as RefreshScript.
    
    .PARAMETER Icon
    Optional icon glyph (e.g., '&#xE7C4;').
    
    .PARAMETER Category
    Category for grouping/filtering cards.
    
    .PARAMETER RefreshScript
    PowerShell script block to re-fetch the data. If not specified and Data is a ScriptBlock, Data is used as RefreshScript.
    
    .EXAMPLE
    $processes = Get-Process | Select-Object -First 20 Name, Id, CPU, Memory
    Add-UITableCard -Step "Dashboard" -Name "Processes" -Title "Running Processes" -Data $processes
    
    Adds a data grid showing process information.
    
    .EXAMPLE
    Add-UITableCard -Step "Dashboard" -Name "Services" -Title "Windows Services" `
        -Data { Get-Service | Select-Object Name, Status, StartType }
    
    Adds a data grid with dynamic data from a script block.
    
    .OUTPUTS
    UIControl object representing the created data grid card.
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
        [object]$Data,
        
        [Parameter()]
        [string]$Icon,
        
        [Parameter()]
        [string]$Category = "General",
        
        [Parameter()]
        [scriptblock]$RefreshScript
    )
    
    begin {
        Write-Verbose "Adding TableCard - $Name to step $Step"
        
        if (-not $script:CurrentWizard) {
            $errorMsg = Get-UIValidationErrorMessage `
                -Message "No UI initialized" `
                -Suggestions @(
                    "Call New-PoshUIDashboard to initialize the dashboard",
                    "Ensure you're using the correct module (PoshUI.Dashboard)"
                ) `
                -Context "Add-UITableCard"
            throw $errorMsg
        }
        
        Test-UIStepExists -StepName $Step -UIDefinition $script:CurrentWizard -Context "Add-UITableCard"
    }
    
    process {
        try {
            $wizardStep = $script:CurrentWizard.GetStep($Step)
            
            Test-UIControlNameUnique -ControlName $Name -UIStep $wizardStep -StepName $Step -Context "Add-UITableCard"
            
            $control = [UIControl]::new($Name, $Title, 'datagridcard')
            
            $control.SetProperty('Type', 'datagridcard')
            $control.SetProperty('CardTitle', $Title)
            $control.SetProperty('CardDescription', $Description)
            $control.SetProperty('Category', $Category)
            
            if ($Icon) {
                $control.SetProperty('Icon', $Icon)
            }
            
            $control.SetProperty('AllowSort', $true)
            $control.SetProperty('AllowFilter', $true)
            $control.SetProperty('AllowExport', $true)
            
            $actualData = $Data
            $refreshScriptToUse = $RefreshScript
            
            if ($Data -is [scriptblock]) {
                Write-Verbose "Executing Data ScriptBlock for TableCard '$Name'..."
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
                        -Message "Failed to execute Data ScriptBlock for TableCard '$Name'" `
                        -Suggestions @(
                            "Check that the ScriptBlock syntax is valid",
                            "Ensure the ScriptBlock returns an array of objects",
                            "Verify all cmdlets/functions used in the ScriptBlock are available",
                            "Test the ScriptBlock independently: & { $($Data.ToString()) }",
                            "Error details: $($_.Exception.Message)"
                        ) `
                        -Context "Add-UITableCard"
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
            
            Write-Verbose "Successfully added TableCard control: $Name"
            return $control
        }
        catch {
            Write-Error "Failed to add TableCard '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }
}

Set-Alias -Name 'Add-UIDataGridCard' -Value 'Add-UITableCard' -Force
