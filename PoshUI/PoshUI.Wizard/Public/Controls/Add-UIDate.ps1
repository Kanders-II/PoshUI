function Add-UIDate {
    <#
    .SYNOPSIS
    Adds a date picker control to a UI step.

    .DESCRIPTION
    Creates a calendar-based input that supports optional minimum/maximum bounds and a display format.

    .PARAMETER Step
    Name of the step to add this control to. The step must already exist.

    .PARAMETER Name
    Unique name for the control. This becomes the parameter name in the generated script.

    .PARAMETER Label
    Display label shown next to the date picker.

    .PARAMETER Default
    Default selected date. Must be within the specified range if provided.

    .PARAMETER Minimum
    Earliest date allowed. Leave unset to allow any past date.

    .PARAMETER Maximum
    Latest date allowed. Leave unset to allow any future date.

    .PARAMETER Format
    Optional custom display/export format (e.g. "yyyy-MM-dd"). When omitted the wizard defaults to ISO date.

    .PARAMETER Mandatory
    Whether selecting a date is required before proceeding.

    .PARAMETER Width
    Preferred width of the control in pixels.

    .PARAMETER HelpText
    Help text or tooltip to display for this control.

    .EXAMPLE
    Add-UIDate -Step "Config" -Name "InstallDate" -Label "Installation Date" -Minimum (Get-Date) -Maximum (Get-Date).AddDays(30)

    Adds a date picker restricted to the next 30 days.

    .OUTPUTS
    UIControl object representing the created date control.

    .NOTES
    This function requires that the specified step exists in the current UI.
    Date controls generate [DateTime] parameters decorated with [WizardDate].
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
        [Nullable[DateTime]]$Default,

        [Parameter()]
        [Nullable[DateTime]]$Minimum,

        [Parameter()]
        [Nullable[DateTime]]$Maximum,

        [Parameter()]
        [string]$Format,

        [Parameter()]
        [switch]$Mandatory,

        [Parameter()]
        [int]$Width,

        [Parameter()]
        [string]$HelpText
    )

    begin {
        Write-Verbose "Adding Date control: $Name to step: $Step"

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

            if ($Minimum.HasValue -and $Maximum.HasValue -and $Minimum.Value -gt $Maximum.Value) {
                throw "Minimum date $Minimum cannot be later than maximum date $Maximum."
            }

            if ($Default.HasValue) {
                if ($Minimum.HasValue -and $Default.Value -lt $Minimum.Value) {
                    throw "Default date $($Default.Value) precedes the minimum $($Minimum.Value)."
                }
                if ($Maximum.HasValue -and $Default.Value -gt $Maximum.Value) {
                    throw "Default date $($Default.Value) exceeds the maximum $($Maximum.Value)."
                }
            }

            $control = [UIControl]::new($Name, $Label, 'Date')
            if ($Default.HasValue) {
                $control.Default = $Default.Value
            }
            $control.Mandatory = $Mandatory.IsPresent
            $control.HelpText = $HelpText
            $control.Width = $Width

            if ($Minimum.HasValue) { $control.SetProperty('Minimum', $Minimum.Value) }
            if ($Maximum.HasValue) { $control.SetProperty('Maximum', $Maximum.Value) }
            if ($Format) { $control.SetProperty('Format', $Format) }

            $wizardStep.AddControl($control)

            Write-Verbose "Successfully added Date control: $($control.ToString())"
            return $control
        }
        catch {
            Write-Error "Failed to add Date control '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Add-UIDate completed for: $Name"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardDate' -Value 'Add-UIDate'
