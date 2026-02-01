function Add-UIMultiLine {
    <#
    .SYNOPSIS
    Adds a multi-line text area to a UI step.

    .DESCRIPTION
    Creates a text input that expands vertically and supports optional row count
    metadata. Unlike Add-WizardTextBox -Multiline, this helper surfaces explicit
    multi-line metadata that the new UI templates and parser understand.

    .PARAMETER Step
    Name of the step to add this control to. The step must already exist.

    .PARAMETER Name
    Unique name for the control. This becomes the parameter name in the generated script.

    .PARAMETER Label
    Display label shown above the text area.

    .PARAMETER Default
    Default text to populate the control with.

    .PARAMETER Rows
    Suggested number of text rows to display. Default is 4.

    .PARAMETER Mandatory
    Whether text input is required. Users cannot proceed without entering a value.

    .PARAMETER ValidationPattern
    Regular expression pattern to validate the input against.

    .PARAMETER ValidationMessage
    Custom error message to display when validation fails.

    .PARAMETER Width
    Preferred width of the control in pixels.

    .PARAMETER HelpText
    Help text or tooltip to display for this control.

    .EXAMPLE
    Add-UIMultiLine -Step "Config" -Name "Notes" -Label "Deployment Notes" -Rows 6

    Adds a multi-line text area showing six rows by default.

    .OUTPUTS
    UIControl object representing the created multi-line text area.

    .NOTES
    This function requires that the specified step exists in the current UI.
    Multi-line controls generate [string] parameters decorated with [WizardMultiLine].
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
        [string]$Label,

        [Parameter()]
        [string]$Default,

        [Parameter()]
        [ValidateRange(1, 50)]
        [int]$Rows = 4,

        [Parameter()]
        [switch]$Mandatory,

        [Parameter()]
        [string]$ValidationPattern,

        [Parameter()]
        [string]$ValidationMessage,

        [Parameter()]
        [int]$Width,

        [Parameter()]
        [string]$HelpText
    )

    begin {
        Write-Verbose "Adding MultiLine control: $Name to step: $Step"

        if (-not $script:CurrentWizard) {
            throw "No UI initialized. Call New-PoshUI first."
        }

        if (-not $script:CurrentWizard.HasStep($Step)) {
            throw "Step '$Step' does not exist. Add the step first using Add-UIStep."
        }
    }

    process {
        try {
            $wizardStep = $script:CurrentWizard.GetStep($Step)

            if ($wizardStep.HasControl($Name)) {
                throw "Control with name '$Name' already exists in step '$Step'"
            }

            $control = [UIControl]::new($Name, $Label, 'TextBox')
            $control.Default = $Default
            $control.Mandatory = $Mandatory.IsPresent
            $control.ValidationPattern = $ValidationPattern
            $control.ValidationMessage = $ValidationMessage
            $control.HelpText = $HelpText
            $control.Width = $Width

            $control.SetProperty('Multiline', $true)
            $control.SetProperty('Rows', $Rows)

            $wizardStep.AddControl($control)

            Write-Verbose "Successfully added MultiLine control: $($control.ToString())"
            return $control
        }
        catch {
            Write-Error "Failed to add MultiLine control '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Add-UIMultiLine completed for: $Name"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardMultiLine' -Value 'Add-UIMultiLine'
