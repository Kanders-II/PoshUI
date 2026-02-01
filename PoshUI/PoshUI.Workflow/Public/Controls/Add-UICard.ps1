function Add-UICard {
    <#
    .SYNOPSIS
    Adds an informational card control to a wizard step.

    .DESCRIPTION
    Creates a visual card that can display information, icons, images, and links.
    Cards are useful for displaying contextual help, tips, warnings, or related information.

    .PARAMETER Step
    Name of the step to add this card to. The step must already exist.

    .PARAMETER Title
    Card title text.

    .PARAMETER Content
    Main content text for the card.

    .PARAMETER Type
    Card type affecting default styling. Valid values: 'Info', 'Success', 'Warning', 'Error', 'Tip'. Default: 'Info'.

    .PARAMETER Icon
    Segoe MDL2 Assets icon glyph (e.g., '&#xE946;' for info). Use HTML entity format.

    .PARAMETER IconPath
    Path to a custom icon image file.

    .PARAMETER ImagePath
    Path to an image file to display in the card.

    .PARAMETER ImageOpacity
    Opacity of the image (0.0 to 1.0). Default: 1.0.

    .PARAMETER LinkUrl
    URL for a clickable link.

    .PARAMETER LinkText
    Display text for the link. Default: 'Learn more'.

    .PARAMETER BackgroundColor
    Hex color code for card background.

    .PARAMETER TitleColor
    Hex color code for title text.

    .PARAMETER ContentColor
    Hex color code for content text.

    .PARAMETER CornerRadius
    Corner radius in pixels for rounded corners. Default: 8.

    .PARAMETER GradientStart
    Starting color for gradient background.

    .PARAMETER GradientEnd
    Ending color for gradient background.

    .PARAMETER Width
    Card width in pixels or 'Auto' for automatic sizing.

    .PARAMETER Height
    Card height in pixels or 'Auto' for automatic sizing.

    .EXAMPLE
    Add-UICard -Step "Config" -Title "Important" -Content "Make sure to back up your data before proceeding." -Type "Warning" -Icon "&#xE7BA;"

    Creates a warning card with an icon.

    .EXAMPLE
    Add-UICard -Step "Welcome" -Title "Need Help?" -Content "Visit our documentation for detailed guides." -LinkUrl "https://docs.example.com" -LinkText "View Docs"

    Creates an info card with a clickable link.

    .NOTES
    Cards are display-only controls and do not create parameters in the generated script.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string]$Step,

        [Parameter(Mandatory = $false, Position = 1)]
        [string]$Title,

        [Parameter(Mandatory = $false, Position = 2)]
        [string]$Content,

        [Parameter()]
        [ValidateSet('Info', 'Success', 'Warning', 'Error', 'Tip')]
        [string]$Type = 'Info',

        [Parameter()]
        [string]$Icon,

        [Parameter()]
        [string]$IconPath,

        [Parameter()]
        [string]$ImagePath,

        [Parameter()]
        [ValidateRange(0.0, 1.0)]
        [double]$ImageOpacity = 1.0,

        [Parameter()]
        [string]$LinkUrl,

        [Parameter()]
        [string]$LinkText = 'Learn more',

        [Parameter()]
        [string]$BackgroundColor,

        [Parameter()]
        [string]$TitleColor,

        [Parameter()]
        [string]$ContentColor,

        [Parameter()]
        [int]$CornerRadius = 8,

        [Parameter()]
        [string]$GradientStart,

        [Parameter()]
        [string]$GradientEnd,

        [Parameter()]
        [string]$Width,

        [Parameter()]
        [string]$Height
    )

    begin {
        Write-Verbose "Adding Card control to step: $Step"

        # Ensure wizard is initialized
        if (-not $script:CurrentWorkflow) {
            throw "No UI initialized. Call New-PoshUIWorkflow first."
        }

        # Ensure step exists
        if (-not $script:CurrentWorkflow.HasStep($Step)) {
            throw "Step '$Step' does not exist. Add the step first using Add-UIStep."
        }
    }

    process {
        try {
            # Get the step
            $wizardStep = $script:CurrentWorkflow.GetStep($Step)

            # Generate unique name for card (cards don't create parameters)
            $cardName = "Card_$([Guid]::NewGuid().ToString('N').Substring(0, 8))"

            # Create the control as InfoCard type
            $control = [UIControl]::new($cardName, $Title, 'InfoCard')

            # Set card properties
            if ($PSBoundParameters.ContainsKey('Title')) { $control.SetProperty('CardTitle', $Title) }
            if ($PSBoundParameters.ContainsKey('Content')) { $control.SetProperty('CardContent', $Content) }
            $control.SetProperty('CardType', $Type)
            if ($PSBoundParameters.ContainsKey('Icon')) { $control.SetProperty('CardIcon', $Icon) }
            if ($PSBoundParameters.ContainsKey('IconPath')) { $control.SetProperty('CardIconPath', $IconPath) }
            if ($PSBoundParameters.ContainsKey('ImagePath')) { $control.SetProperty('CardImagePath', $ImagePath) }
            $control.SetProperty('CardImageOpacity', $ImageOpacity)
            if ($PSBoundParameters.ContainsKey('LinkUrl')) { $control.SetProperty('CardLinkUrl', $LinkUrl) }
            $control.SetProperty('CardLinkText', $LinkText)
            if ($PSBoundParameters.ContainsKey('BackgroundColor')) { $control.SetProperty('CardBackgroundColor', $BackgroundColor) }
            if ($PSBoundParameters.ContainsKey('TitleColor')) { $control.SetProperty('CardTitleColor', $TitleColor) }
            if ($PSBoundParameters.ContainsKey('ContentColor')) { $control.SetProperty('CardContentColor', $ContentColor) }
            $control.SetProperty('CardCornerRadius', $CornerRadius)
            if ($PSBoundParameters.ContainsKey('GradientStart')) { $control.SetProperty('CardGradientStart', $GradientStart) }
            if ($PSBoundParameters.ContainsKey('GradientEnd')) { $control.SetProperty('CardGradientEnd', $GradientEnd) }
            if ($PSBoundParameters.ContainsKey('Width')) { $control.SetProperty('CardWidth', $Width) }
            if ($PSBoundParameters.ContainsKey('Height')) { $control.SetProperty('CardHeight', $Height) }

            # Add control to step
            $wizardStep.AddControl($control)

            Write-Verbose "Card added successfully: $Title"

            # Return the control
            return $control
        }
        catch {
            throw "Failed to add card to step '$Step': $_"
        }
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardCard' -Value 'Add-UICard'
