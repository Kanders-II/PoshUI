function Add-UICheckbox {
    <#
    .SYNOPSIS
    Adds a checkbox control to a UI step.

    .DESCRIPTION
    Creates a checkbox control that allows users to select true/false values.
    Checkboxes are ideal for boolean options and feature toggles.
    
    .PARAMETER Step
    Name of the step to add this control to. The step must already exist.
    
    .PARAMETER Name
    Unique name for the control. This becomes the parameter name in the generated script.
    
    .PARAMETER Label
    Display label shown next to the checkbox.
    
    .PARAMETER Default
    Default checked state for the checkbox. Default is $false.
    
    .PARAMETER Mandatory
    Whether this checkbox must be checked to proceed. Useful for acceptance checkboxes.
    
    .PARAMETER Width
    Preferred width of the control in pixels.
    
    .PARAMETER HelpText
    Help text or tooltip to display for this control.
    
    .EXAMPLE
    Add-UICheckbox -Step "Config" -Name "EnableLogging" -Label "Enable detailed logging"

    Adds a basic checkbox for enabling logging.

    .EXAMPLE
    Add-UICheckbox -Step "Config" -Name "AcceptTerms" -Label "I accept the terms and conditions" -Mandatory

    Adds a required checkbox that must be checked to proceed.

    .EXAMPLE
    Add-UICheckbox -Step "Config" -Name "CreateBackup" -Label "Create backup before installation" -Default $true

    Adds a checkbox that is checked by default.

    .OUTPUTS
    UIControl object representing the created checkbox.

    .NOTES
    This function requires that the specified step exists in the current UI.
    Checkboxes generate [bool] parameters in the resulting script.
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
        
        [Parameter()]
        [bool]$Default = $false,
        
        [Parameter()]
        [switch]$Mandatory,
        
        [Parameter()]
        [int]$Width,
        
        [Parameter()]
        [string]$HelpText
    )
    
    begin {
        Write-Verbose "Adding Checkbox control: $Name to step: $Step"

        # Ensure UI is initialized
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
            
            # Create the control
            $control = [UIControl]::new($Name, $Label, 'Checkbox')
            $control.Default = $Default
            $control.Mandatory = $Mandatory.IsPresent
            $control.HelpText = $HelpText
            $control.Width = $Width
            
            # Add to step
            $wizardStep.AddControl($control)
            
            Write-Verbose "Successfully added Checkbox control: $($control.ToString())"
            
            # Return the control object
            return $control
        }
        catch {
            Write-Error "Failed to add Checkbox control '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }
    
    end {
        Write-Verbose "Add-UICheckbox completed for: $Name"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardCheckbox' -Value 'Add-UICheckbox'

