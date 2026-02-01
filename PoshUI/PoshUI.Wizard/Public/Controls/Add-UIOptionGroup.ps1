function Add-UIOptionGroup {
    <#
    .SYNOPSIS
    Adds an option group (radio button set) to a UI step.

    .DESCRIPTION
    Creates a compact group of mutually exclusive options rendered as radio buttons.
    Option groups are ideal when a small number of choices should be presented inline
    instead of a dropdown.

    .PARAMETER Step
    Name of the step to add this control to. The step must already exist.

    .PARAMETER Name
    Unique name for the control. This becomes the parameter name in the generated script.

    .PARAMETER Label
    Display label shown above the option group.

    .PARAMETER Options
    Array of string values that will be presented as individual radio buttons.

    .PARAMETER Default
    Default selected option. Must be one of the supplied options if specified.

    .PARAMETER Orientation
    Layout orientation for the radio buttons. Defaults to Vertical.

    .PARAMETER Mandatory
    Whether a selection is required. Users cannot proceed without selecting a value.

    .PARAMETER Width
    Preferred width of the control in pixels.

    .PARAMETER HelpText
    Help text or tooltip to display for this control.

    .EXAMPLE
    Add-UIOptionGroup -Step "Config" -Name "Environment" -Label "Target Environment" -Options @('Dev','Test','Prod') -Default 'Test'

    Adds a horizontal radio button group for environment selection.

    .OUTPUTS
    UIControl object representing the created option group.

    .NOTES
    This function requires that the specified step exists in the current UI.
    Option groups generate [string] parameters decorated with [WizardOptionGroup]
    and include a [ValidateSet] to enforce the allowed values.
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

        [Parameter(Mandatory = $true, Position = 3)]
        [ValidateNotNullOrEmpty()]
        [string[]]$Options,

        [Parameter()]
        [string]$Default,

        [Parameter()]
        [ValidateSet('Vertical','Horizontal')]
        [string]$Orientation = 'Vertical',

        [Parameter()]
        [switch]$Mandatory,

        [Parameter()]
        [int]$Width,

        [Parameter()]
        [string]$HelpText
    )

    begin {
        Write-Verbose "Adding OptionGroup control: $Name to step: $Step"

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

            if ($Options.Count -lt 2) {
                throw "OptionGroup requires at least two options."
            }

            if ($Default -and $Default -notin $Options) {
                throw "Default value '$Default' is not in the options list: $($Options -join ', ')"
            }

            $control = [UIControl]::new($Name, $Label, 'OptionGroup')
            $control.SetChoices($Options)
            if ($Default) {
                $control.Default = $Default
            }
            $control.Mandatory = $Mandatory.IsPresent
            $control.HelpText = $HelpText
            $control.Width = $Width
            $control.SetProperty('Orientation', $Orientation)

            $wizardStep.AddControl($control)

            Write-Verbose "Successfully added OptionGroup control: $($control.ToString())"
            Write-Verbose "Options: $($Options -join ', ')"
            return $control
        }
        catch {
            Write-Error "Failed to add OptionGroup control '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Add-UIOptionGroup completed for: $Name"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardOptionGroup' -Value 'Add-UIOptionGroup'
