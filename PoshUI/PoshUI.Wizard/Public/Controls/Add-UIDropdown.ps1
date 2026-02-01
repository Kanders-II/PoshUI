function Add-UIDropdown {
    <#
    .SYNOPSIS
    Adds a dropdown selection control to a UI step.
    
    .DESCRIPTION
    Creates a dropdown (ComboBox) control that allows users to select from a predefined list of options.
    This control generates ValidateSet attributes in the resulting script for parameter validation.
    Can also create dynamic dropdowns using scriptblocks that generate choices at runtime.
    
    .PARAMETER Step
    Name of the step to add this control to. The step must already exist.
    
    .PARAMETER Name
    Unique name for the control. This becomes the parameter name in the generated script.
    
    .PARAMETER Label
    Display label shown next to the dropdown.
    
    .PARAMETER Choices
    Array of string values that will be available in the dropdown.
    Not required if ScriptBlock is provided.
    
    .PARAMETER ScriptBlock
    PowerShell script block that returns an array of choices dynamically.
    Dependencies are detected from param() declarations in the script block.
    
    .PARAMETER DependsOn
    Optional explicit array of parameter names this control depends on.
    If not specified, dependencies are auto-detected from the script block.
    
    .PARAMETER Default
    Default selected value. Must be one of the choices if specified.
    
    .PARAMETER Mandatory
    Whether a selection is required. Users cannot proceed without selecting a value.
    
    .PARAMETER Editable
    Whether users can type custom values in addition to selecting from the list.
    
    .PARAMETER Width
    Preferred width of the control in pixels.
    
    .PARAMETER HelpText
    Help text or tooltip to display for this control.
    
    .EXAMPLE
    Add-UIDropdown -Step "Config" -Name "Environment" -Label "Target Environment:" -Choices @('Development', 'Testing', 'Production') -Mandatory
    
    Adds a required dropdown for environment selection.
    
    .EXAMPLE
    Add-UIDropdown -Step "Config" -Name "LogLevel" -Label "Log Level:" -Choices @('Error', 'Warning', 'Information', 'Debug') -Default 'Information'
    
    Adds a dropdown with a default selection.
    
    .EXAMPLE
    Add-UIDropdown -Step "Config" -Name "CustomOption" -Label "Custom Option:" -Choices @('Option1', 'Option2', 'Option3') -Editable
    
    Adds an editable dropdown that allows custom values.
    
    .EXAMPLE
    Add-UIDropdown -Step "Config" -Name "Region" -Label "Region:" -ScriptBlock {
        param($Environment)
        if ($Environment -eq 'Production') {
            @('US-East-1', 'US-West-2', 'EU-Central-1')
        } else {
            @('Dev-Region-1', 'Dev-Region-2')
        }
    } -Mandatory
    
    Creates a dynamic dropdown that updates based on Environment selection.
    
    .OUTPUTS
    UIControl object representing the created dropdown.
    
    .NOTES
    This function requires that the specified step exists in the current UI.
    Dropdowns generate [string] parameters with [ValidateSet] attributes in the resulting script.
    For dynamic dropdowns, use the ScriptBlock parameter with param() declarations for dependencies.
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
        [string]$Default,
        
        [Parameter()]
        [switch]$Mandatory,
        
        [Parameter()]
        [switch]$Editable,
        
        [Parameter()]
        [int]$Width,
        
        [Parameter()]
        [string]$HelpText
    )
    
    begin {
        Write-Verbose "Adding Dropdown control: $Name to step: $Step"
        
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
            
            # Validate default value if provided with static choices
            if ($Default -and $Choices -and $Default -notin $Choices) {
                throw "Default value '$Default' is not in the choices list: $($Choices -join ', ')"
            }
            
            # Create the control
            $control = [UIControl]::new($Name, $Label, 'Dropdown')
            $control.Default = $Default
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
                
                Write-Verbose "Dynamic dropdown with ScriptBlock configured"
            }
            
            # Set control-specific properties
            if ($Editable.IsPresent) {
                $control.SetProperty('Editable', $true)
            }
            
            # Add to step
            $wizardStep.AddControl($control)
            
            Write-Verbose "Successfully added Dropdown control: $($control.ToString())"
            
            # Return the control object
            return $control
        }
        catch {
            Write-Error "Failed to add Dropdown control '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }
    
    end {
        Write-Verbose "Add-UIDropdown completed for: $Name"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardDropdown' -Value 'Add-UIDropdown'
