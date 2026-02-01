function Add-UICard {
    <#
    .SYNOPSIS
    Adds an informational card control to a UI step.

    .DESCRIPTION
    Creates a card control that displays formatted text, instructions, or information.
    Cards are rendered as visually distinct panels and are perfect for providing
    context, guidelines, warnings, or helpful tips within a UI step.
    
    .PARAMETER Step
    Name of the step to add this card to. The step must already exist.
    
    .PARAMETER Name
    Unique name for the card. This is used internally to reference the card.
    
    .PARAMETER Title
    Title displayed at the top of the card.
    
    .PARAMETER Content
    The main content text to display in the card. Supports multi-line text.
    Best practice: Use here-strings (@"..."@) for multi-line content instead of backtick-n.
    You can use bullet points (•), numbers, and formatting for better readability.
    
    .PARAMETER Icon
    Optional icon to display in the card header. Can be:
    - Segoe MDL2 icon glyph in format '&#xE1D3;' (e.g., '&#xE946;' for Info)
    - Emoji characters (e.g., '📋', '💡', '⚠️')
    
    .PARAMETER IconPath
    Path to an image file to display as an icon next to the title (32x32px).
    
    .PARAMETER ImagePath
    Path to a background image for the card.
    
    .PARAMETER ImageOpacity
    Opacity of the background image (0.0 to 1.0). Default is 1.0.
    
    .PARAMETER LinkUrl
    URL to open when the link is clicked.
    
    .PARAMETER LinkText
    Text to display for the clickable link. Default is 'Learn more...'.
    
    .PARAMETER BackgroundColor
    Background color for the card (e.g., '#107C10').
    
    .PARAMETER TitleColor
    Color for the title text (e.g., '#FFFFFF').
    
    .PARAMETER ContentColor
    Color for the content text (e.g., '#B0B0B0').
    
    .PARAMETER CornerRadius
    Corner radius for the card border (0-50). Default is 8.
    
    .PARAMETER GradientStart
    Starting color for gradient background (e.g., '#0078D4').
    
    .PARAMETER GradientEnd
    Ending color for gradient background (e.g., '#004578').
    
    .EXAMPLE
    Add-UICard -Step "Config" -Name "InfoCard" -Title "Important Information" -Content @"
Please read the following guidelines before proceeding:

• Requirement 1
• Requirement 2
• Requirement 3
"@

    Adds a simple informational card with bullet points using here-string.

    .EXAMPLE
    Add-UICard -Step "Setup" -Name "TipsCard" -Title "💡 Pro Tips" -Content @"
Here are some tips for optimal configuration:

1. Use strong passwords
2. Enable backup options
3. Test before deploying
"@

    Adds a tips card with emoji icon and numbered list using here-string.

    .EXAMPLE
    Add-UICard -Step "Network" -Name "NetworkInfo" -Title "Network Requirements" -Icon "&#xE968;" -Content @"
Ensure the following network requirements are met:

• Port 443 must be open
• DNS resolution configured
• Proxy settings (if applicable)
"@

    Adds a card with a Segoe MDL2 network icon using here-string.

    .OUTPUTS
    UIControl object representing the created card.

    .NOTES
    This function requires that the specified step exists in the current UI.
    Cards do not collect user input - they are for display purposes only.
    Cards are rendered at the beginning of the step, before input controls.
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
        
        [Parameter(Position = 2)]
        [string]$Title = '',
        
        [Parameter(Position = 3)]
        [string]$Content = '',
        
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
        [string]$LinkText,
        
        [Parameter()]
        [string]$BackgroundColor,
        
        [Parameter()]
        [string]$TitleColor,
        
        [Parameter()]
        [string]$ContentColor,
        
        [Parameter()]
        [ValidateRange(0, 50)]
        [int]$CornerRadius = 8,
        
        [Parameter()]
        [string]$GradientStart,
        
        [Parameter()]
        [string]$GradientEnd,
        
        [Parameter()]
        [string]$Category
    )
    
    begin {
        Write-Verbose "Adding Card control: $Name to step: $Step"

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
            $control = [UIControl]::new($Name, $Title, 'infocard')
            $control.SetProperty('Type', 'infocard')
            $control.SetProperty('CardTitle', $Title)
            $control.SetProperty('CardContent', $Content)
            $control.SetProperty('Category', $Category)

            if ($Icon) {
                $control.SetProperty('Icon', $Icon)
            }
            
            # Wizard template card features
            if ($IconPath) {
                $control.SetProperty('IconPath', $IconPath)
            }
            
            if ($ImagePath) {
                $control.SetProperty('ImagePath', $ImagePath)
                $control.SetProperty('ImageOpacity', $ImageOpacity)
            }
            
            if ($LinkUrl) {
                $control.SetProperty('LinkUrl', $LinkUrl)
                if ($LinkText) {
                    $control.SetProperty('LinkText', $LinkText)
                }
            }
            
            # Styling properties
            if ($BackgroundColor) {
                $control.SetProperty('BackgroundColor', $BackgroundColor)
            }
            
            if ($TitleColor) {
                $control.SetProperty('TitleColor', $TitleColor)
            }
            
            if ($ContentColor) {
                $control.SetProperty('ContentColor', $ContentColor)
            }
            
            if ($CornerRadius -ne 8) {
                $control.SetProperty('CornerRadius', $CornerRadius)
            }
            
            if ($GradientStart -and $GradientEnd) {
                $control.SetProperty('GradientStart', $GradientStart)
                $control.SetProperty('GradientEnd', $GradientEnd)
            }

            # Cards don't have a value, they're display-only
            $control.Mandatory = $false

            # Add to step
            $wizardStep.AddControl($control)

            Write-Verbose "Successfully added Card control: $($control.ToString())"
            Write-Verbose "Title: $Title"
            Write-Verbose "Content length: $($Content.Length) characters"

            # Return the control object
            return $control
        }
        catch {
            Write-Error "Failed to add Card control '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Add-UICard completed for: $Name"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardCard' -Value 'Add-UICard'

