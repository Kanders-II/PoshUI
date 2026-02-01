function Add-UITextBox {
    <#
    .SYNOPSIS
    Adds a text input control to a UI step.

    .DESCRIPTION
    Creates a text input field that allows users to enter string values.
    Supports validation, default values, and various formatting options.
    
    .PARAMETER Step
    Name of the step to add this control to. The step must already exist.
    
    .PARAMETER Name
    Unique name for the control. This becomes the parameter name in the generated script.
    
    .PARAMETER Label
    Display label shown next to the text input field.
    
    .PARAMETER Default
    Default value for the text input.
    
    .PARAMETER Placeholder
    Placeholder text shown when the field is empty.
    
    .PARAMETER Mandatory
    Whether this field is required. Users cannot proceed without entering a value.
    
    .PARAMETER Multiline
    Whether to create a multi-line text area instead of a single-line text box.
    
    .PARAMETER ValidationPattern
    Regular expression pattern to validate the input against.
    
    .PARAMETER ValidationMessage
    Custom error message to show when validation fails.
    
    .PARAMETER MaxLength
    Maximum number of characters allowed.
    
    .PARAMETER Width
    Preferred width of the control in pixels.
    
    .PARAMETER HelpText
    Help text or tooltip to display for this control.
    
    .EXAMPLE
    Add-UITextBox -Step "Config" -Name "ServerName" -Label "Server Name:" -Mandatory

    Adds a required text input for server name.

    .EXAMPLE
    Add-UITextBox -Step "Config" -Name "Description" -Label "Description:" -Multiline -MaxLength 500

    Adds a multi-line text area with character limit.

    .EXAMPLE
    Add-UITextBox -Step "Config" -Name "Email" -Label "Email Address:" -ValidationPattern "^[^@]+@[^@]+\.[^@]+$" -ValidationMessage "Please enter a valid email address"

    Adds a text input with email validation.

    .OUTPUTS
    UIControl object representing the created text input.

    .NOTES
    This function requires that the specified step exists in the current UI.
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
        [string]$Default,
        
        [Parameter()]
        [string]$Placeholder,
        
        [Parameter()]
        [switch]$Mandatory,
        
        [Parameter()]
        [switch]$Multiline,
        
        [Parameter()]
        [string]$ValidationPattern,
        
        [Parameter()]
        [string]$ValidationMessage,
        
        [Parameter()]
        [int]$MaxLength,
        
        [Parameter()]
        [int]$Width,
        
        [Parameter()]
        [string]$HelpText
    )
    
    begin {
        Write-Verbose "Adding TextBox control: $Name to step: $Step"

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
            $control = [UIControl]::new($Name, $Label, 'TextBox')
            $control.Default = $Default
            $control.Mandatory = $Mandatory.IsPresent
            $control.ValidationPattern = $ValidationPattern
            $control.ValidationMessage = $ValidationMessage
            $control.HelpText = $HelpText
            $control.Width = $Width

            # Set control-specific properties
            if ($PSBoundParameters.ContainsKey('Placeholder')) {
                $control.SetProperty('Placeholder', $Placeholder)
            }
            if ($Multiline.IsPresent) {
                $control.SetProperty('Multiline', $true)
            }
            if ($PSBoundParameters.ContainsKey('MaxLength')) {
                $control.SetProperty('MaxLength', $MaxLength)
            }

            # Add to step
            $wizardStep.AddControl($control)

            Write-Verbose "Successfully added TextBox control: $($control.ToString())"

            # Return the control object
            return $control
        }
        catch {
            Write-Error "Failed to add TextBox control '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Add-UITextBox completed for: $Name"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardTextBox' -Value 'Add-UITextBox'

