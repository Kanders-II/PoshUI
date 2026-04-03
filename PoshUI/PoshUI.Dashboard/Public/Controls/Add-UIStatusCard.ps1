function Add-UIStatusCard {
    <#
    .SYNOPSIS
    Adds a status indicator card to a UI step.
    
    .DESCRIPTION
    Creates a status indicator card that displays a list of items with colored status dots.
    Status colors are automatically assigned based on common status strings:
    - Green: Online, Running, Healthy, OK, Active, Up, Connected, Success
    - Amber: Warning, Degraded, Slow, Pending, Starting
    - Red: Offline, Stopped, Error, Critical, Down, Failed, Disconnected
    - Gray: Maintenance, Disabled, Unknown
    
    .PARAMETER Step
    Name of the step to add this card to. The step must already exist.
    
    .PARAMETER Name
    Unique name for the card.
    
    .PARAMETER Title
    Display title for the card.
    
    .PARAMETER Description
    Short description shown below the card title.
    
    .PARAMETER Data
    Array of hashtables with Label and Status keys.
    Example: @( @{Label='DNS Server'; Status='Online'}, @{Label='DHCP'; Status='Warning'} )
    
    .PARAMETER Icon
    Optional icon glyph (e.g., '&#xE770;').
    
    .PARAMETER Category
    Category for grouping/filtering cards.
    
    .PARAMETER RefreshScript
    PowerShell script block to re-fetch the status data.
    
    .EXAMPLE
    Add-UIStatusCard -Step "Dashboard" -Name "Services" -Title "Core Services" `
        -Icon '&#xE770;' -Data @(
            @{Label='Active Directory'; Status='Online'}
            @{Label='DNS Server'; Status='Online'}
            @{Label='DHCP Server'; Status='Warning'}
        )
    
    Adds a status card showing service health with colored dots.
    
    .OUTPUTS
    UIControl object representing the created status card.
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
        [object[]]$Data,
        
        [Parameter()]
        [string]$Icon,
        
        [Parameter()]
        [string]$Category = "General",
        
        [Parameter()]
        [scriptblock]$RefreshScript
    )
    
    begin {
        Write-Verbose "Adding StatusCard - $Name to step $Step"
        
        if (-not $script:CurrentWizard) {
            $errorMsg = Get-UIValidationErrorMessage `
                -Message "No UI initialized" `
                -Suggestions @(
                    "Call New-PoshUIDashboard to initialize the dashboard",
                    "Ensure you're using the correct module (PoshUI.Dashboard)"
                ) `
                -Context "Add-UIStatusCard"
            throw $errorMsg
        }
        
        Test-UIStepExists -StepName $Step -UIDefinition $script:CurrentWizard -Context "Add-UIStatusCard"
    }
    
    process {
        try {
            $wizardStep = $script:CurrentWizard.GetStep($Step)
            
            Test-UIControlNameUnique -ControlName $Name -UIStep $wizardStep -StepName $Step -Context "Add-UIStatusCard"
            
            $control = [UIControl]::new($Name, $Title, 'statusindicatorcard')
            
            $control.SetProperty('Type', 'statusindicatorcard')
            $control.SetProperty('CardTitle', $Title)
            $control.SetProperty('CardDescription', $Description)
            $control.SetProperty('Category', $Category)
            
            if ($Icon) {
                $control.SetProperty('Icon', $Icon)
            }
            
            # Convert Data array to JSON for the C# side
            $actualData = $Data
            $refreshScriptToUse = $RefreshScript
            
            if ($Data -is [scriptblock]) {
                Write-Verbose "Executing Data ScriptBlock for StatusCard '$Name'..."
                try {
                    $actualData = & $Data
                    Write-Verbose "ScriptBlock returned $(@($actualData).Count) status items"
                    if (-not $refreshScriptToUse) {
                        $refreshScriptToUse = $Data
                        Write-Verbose "Auto-configured RefreshScript from Data ScriptBlock"
                    }
                }
                catch {
                    Write-Error "Failed to execute Data ScriptBlock for StatusCard '$Name': $($_.Exception.Message)"
                    $actualData = @()
                }
            }
            
            if ($actualData) {
                $dataJson = $actualData | ConvertTo-Json -Depth 5 -Compress
                $control.SetProperty('Data', $dataJson)
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
            
            Write-Verbose "Successfully added StatusCard control: $Name"
            return $control
        }
        catch {
            Write-Error "Failed to add StatusCard '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }
}

Set-Alias -Name 'Add-UIStatusIndicatorCard' -Value 'Add-UIStatusCard' -Force
