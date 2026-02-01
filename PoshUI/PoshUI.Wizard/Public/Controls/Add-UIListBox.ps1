function Add-UIListBox {
    <#
    .SYNOPSIS
    Adds a ListBox selection control to a UI step.
    
    .DESCRIPTION
    Creates a ListBox control that displays a scrollable list of options.
    Supports both single-select and multi-select modes.
    This control generates ValidateSet attributes for single-select or string arrays for multi-select in the resulting script.
    Can also create dynamic listboxes using scriptblocks that generate choices at runtime.
    
    .PARAMETER Step
    Name of the step to add this control to. The step must already exist.
    
    .PARAMETER Name
    Unique name for the control. This becomes the parameter name in the generated script.
    
    .PARAMETER Label
    Display label shown above the ListBox.
    
    .PARAMETER Choices
    Array of string values that will be available in the ListBox.
    Not required if ScriptBlock is provided.
    
    .PARAMETER ScriptBlock
    PowerShell script block that returns an array of choices dynamically.
    Dependencies are detected from param() declarations in the script block.
    
    .PARAMETER DependsOn
    Optional explicit array of parameter names this control depends on.
    If not specified, dependencies are auto-detected from the script block.
    
    .PARAMETER Default
    Default selected value(s). Must be one or more of the choices if specified.
    For multi-select, provide an array of values.
    
    .PARAMETER Mandatory
    Whether a selection is required. Users cannot proceed without selecting a value.
    
    .PARAMETER MultiSelect
    Whether users can select multiple values from the list.
    
    .PARAMETER Height
    Preferred height of the ListBox in pixels. Default is 150.
    
    .PARAMETER Width
    Preferred width of the control in pixels.
    
    .PARAMETER HelpText
    Help text or tooltip to display for this control.
    
    .EXAMPLE
    Add-UIListBox -Step "Config" -Name "Features" -Label "Select Features:" -Choices @('Web Server', 'Database', 'Cache', 'Monitoring') -MultiSelect
    
    Adds a multi-select ListBox for feature selection.
    
    .EXAMPLE
    Add-UIListBox -Step "Config" -Name "Priority" -Label "Priority Level:" -Choices @('Low', 'Medium', 'High', 'Critical') -Default 'Medium' -Mandatory
    
    Adds a required single-select ListBox with a default selection.
    
    .EXAMPLE
    Add-UIListBox -Step "Config" -Name "Regions" -Label "Deployment Regions:" -Choices @('US-East', 'US-West', 'EU-Central', 'APAC') -MultiSelect -Default @('US-East', 'US-West') -Height 200
    
    Adds a multi-select ListBox with multiple defaults and custom height.
    
    .EXAMPLE
    Add-UIListBox -Step "Config" -Name "Features" -Label "Select Features:" -ScriptBlock {
        param($Environment)
        if ($Environment -eq 'Production') {
            @('Logging', 'Monitoring', 'Caching', 'LoadBalancing')
        } else {
            @('Logging', 'DebugMode', 'VerboseErrors')
        }
    } -MultiSelect
    
    Creates a dynamic multi-select listbox that updates based on Environment selection.
    
    .OUTPUTS
    UIControl object representing the created ListBox.
    
    .NOTES
    This function requires that the specified step exists in the current UI.
    ListBox controls generate [string] parameters for single-select or [string[]] for multi-select with [ValidateSet] attributes.
    For dynamic listboxes, use the ScriptBlock parameter with param() declarations for dependencies.
    #>
    [CmdletBinding()]
    # [OutputType([UIControl])] # Commented out to avoid type loading issues
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string]$Step,
        
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullOrEmpty()]
        [string]$Name,
        
        [Parameter(Mandatory = $true, Position = 2)]
        [ValidateNotNullOrEmpty()]
        [string]$Label,
        
        [Parameter(Mandatory = $false, Position = 3)]
        [ValidateNotNullOrEmpty()]
        [string[]]$Choices,
        
        [Parameter()]
        [ValidateNotNull()]
        [scriptblock]$ScriptBlock,
        
        [Parameter()]
        [string[]]$DependsOn,
        
        [Parameter()]
        $Default,
        
        [Parameter()]
        [switch]$Mandatory,
        
        [Parameter()]
        [switch]$MultiSelect,
        
        [Parameter()]
        [int]$Height = 150,
        
        [Parameter()]
        [int]$Width,
        
        [Parameter()]
        [string]$HelpText
    )
    
    begin {
        Write-Verbose "Adding ListBox control: $Name to step: $Step"
        
        # Ensure wizard is initialized
        if (-not $script:CurrentWizard) {
            throw "No UI initialized. Call New-PoshUI first."
        }
        
        # Ensure step exists
        if (-not $script:CurrentWizard.HasStep($Step)) {
            throw "Step '$Step' does not exist. Add the step first using Add-UIStep."
        }
    }
    
    process {
        try {
            # Get the step
            $wizardStep = $script:CurrentWizard.GetStep($Step)
            
            # Check for duplicate control name within the step
            if ($wizardStep.HasControl($Name)) {
                throw "Control with name '$Name' already exists in step '$Step'"
            }
            
            # Validate parameters - either Choices or ScriptBlock must be provided
            if (-not $Choices -and -not $ScriptBlock) {
                throw "Either -Choices or -ScriptBlock parameter must be provided"
            }
            
            if ($Choices -and $ScriptBlock) {
                throw "Cannot specify both -Choices and -ScriptBlock parameters. Use one or the other."
            }
            
            # Validate default value(s) if provided with static choices
            if ($Default -and $Choices) {
                $defaultArray = if ($Default -is [array]) { $Default } else { @($Default) }
                foreach ($defaultValue in $defaultArray) {
                    if ($defaultValue -notin $Choices) {
                        throw "Default value '$defaultValue' is not in the choices list: $($Choices -join ', ')"
                    }
                }
                
                # For single-select, ensure only one default is provided
                if (-not $MultiSelect.IsPresent -and $defaultArray.Count -gt 1) {
                    throw "Single-select ListBox cannot have multiple default values. Use -MultiSelect for multiple selections."
                }
            }
            
            # Create the control
            $control = [UIControl]::new($Name, $Label, 'ListBox')
            
            # Convert default to proper format
            if ($Default) {
                if ($MultiSelect.IsPresent) {
                    $control.Default = if ($Default -is [array]) { $Default } else { @($Default) }
                } else {
                    $control.Default = if ($Default -is [array]) { $Default[0] } else { $Default }
                }
            }
            
            $control.Mandatory = $Mandatory.IsPresent
            $control.HelpText = $HelpText
            $control.Width = $Width
            
            # Handle static choices
            if ($Choices) {
                $control.SetChoices($Choices)
                Write-Verbose "Choices: $($Choices -join ', ')"
            }
            
            # Handle dynamic scriptblock
            if ($ScriptBlock) {
                $control.SetProperty('IsDynamic', $true)
                # Strip outer braces from ScriptBlock.ToString() to avoid double-braces when generating script
                $scriptBlockContent = $ScriptBlock.ToString().Trim()
                if ($scriptBlockContent.StartsWith('{') -and $scriptBlockContent.EndsWith('}')) {
                    $scriptBlockContent = $scriptBlockContent.Substring(1, $scriptBlockContent.Length - 2).Trim()
                }
                $control.SetProperty('DataSourceScriptBlock', $scriptBlockContent)
                
                # Set dependencies (auto-detected or explicit)
                if ($PSBoundParameters.ContainsKey('DependsOn') -and $DependsOn) {
                    $control.SetProperty('DataSourceDependsOn', $DependsOn)
                }
                else {
                    # Dependencies will be auto-detected from script block by ReflectionService
                    $control.SetProperty('DataSourceDependsOn', @())
                }
                
                # Mark as synchronous execution
                $control.SetProperty('DataSourceAsync', $false)
                
                Write-Verbose "Dynamic listbox with ScriptBlock configured"
            }
            
            # Set ListBox-specific properties
            $control.SetProperty('IsListBox', $true)
            if ($PSBoundParameters.ContainsKey('Height')) {
                $control.SetProperty('Height', $Height)
            }
            if ($MultiSelect.IsPresent) {
                $control.SetProperty('IsMultiSelect', $true)
            }
            
            # Add to step
            $wizardStep.AddControl($control)
            
            Write-Verbose "Successfully added ListBox control: $($control.ToString())"
            Write-Verbose "MultiSelect: $($MultiSelect.IsPresent)"
            
            # Return the control object
            return $control
        }
        catch {
            Write-Error "Failed to add ListBox control '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }
    
    end {
        Write-Verbose "Add-UIListBox completed for: $Name"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardListBox' -Value 'Add-UIListBox'
