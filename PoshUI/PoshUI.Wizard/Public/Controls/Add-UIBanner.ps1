function Add-UIBanner {
    <#
    .SYNOPSIS
    Adds a banner control to a wizard step.

    .DESCRIPTION
    Creates a visual banner at the top of a wizard step. Banners can display titles,
    descriptions, icons, images, buttons, progress indicators, and support many visual styles.

    .PARAMETER Step
    Name of the step to add this banner to. The step must already exist.

    .PARAMETER Title
    Main title text for the banner.

    .PARAMETER Subtitle
    Subtitle text displayed below the title.

    .PARAMETER Description
    Additional description text.

    .PARAMETER Icon
    Segoe MDL2 Assets icon glyph (e.g., '&#xE8BC;' for star). Use HTML entity format.

    .PARAMETER IconPath
    Path to a custom icon image file.

    .PARAMETER IconSize
    Size of the icon in pixels. Default: 64.

    .PARAMETER IconPosition
    Position of the icon relative to text content. Valid values: 'Left', 'Right', 'Top', 'Bottom'. Default: 'Right'.

    .PARAMETER BannerStyle
    Preset style for the banner: Default, Gradient, Image, Minimal, Hero, Accent.
    This simplifies banner creation for common use cases (80% of scenarios).
    - Default: Standard banner with theme colors
    - Gradient: Blue gradient background (135° angle)
    - Image: Larger banner optimized for background images
    - Minimal: Compact banner with no shadow
    - Hero: Large centered banner for landing pages
    - Accent: Green accent color with hover effect
    
    .PARAMETER BannerConfig
    Hashtable of advanced configuration options to override preset values.
    Allows fine-tuning of preset styles without specifying all parameters.
    Example: @{ Height = 250; TitleFontSize = 40; BackgroundColor = '#FF5722' }

    .PARAMETER BackgroundColor
    Hex color code for banner background (e.g., '#2D2D30').

    .PARAMETER BackgroundImagePath
    Path to background image file.

    .PARAMETER BackgroundImageOpacity
    Opacity of background image (0.0 to 1.0). Default: 0.3.

    .PARAMETER GradientStart
    Starting color for gradient background.

    .PARAMETER GradientEnd
    Ending color for gradient background.

    .PARAMETER Height
    Banner height in pixels. Default: 180.

    .PARAMETER ButtonText
    Text for an action button.

    .PARAMETER ButtonIcon
    Icon for the action button (Segoe MDL2 Assets glyph).

    .PARAMETER ButtonColor
    Button background color. Default: '#0078D4'.

    .PARAMETER ProgressValue
    Progress percentage (0-100). Set to -1 to hide progress bar. Default: -1.

    .PARAMETER ProgressLabel
    Label text for the progress bar.

    .PARAMETER CarouselItems
    Array of hashtables for carousel banners. Each item should have Title, Subtitle, Icon, etc.

    .PARAMETER AutoRotate
    Enable automatic rotation for carousel banners.

    .PARAMETER RotateInterval
    Rotation interval in milliseconds. Default: 3000.

    .EXAMPLE
    Add-UIBanner -Step "Welcome" -Title "Welcome to Setup" -Subtitle "Let's get started"

    Creates a simple banner with default styling.

    .EXAMPLE
    Add-UIBanner -Step "Welcome" -Title "Welcome" -Subtitle "Let's begin" -BannerStyle "Gradient"

    Creates a gradient banner using the preset style.

    .EXAMPLE
    Add-UIBanner -Step "Welcome" -Title "Hero Banner" -BannerStyle "Hero" `
        -BannerConfig @{ Height = 300; TitleFontSize = 48 }

    Creates a hero banner with preset style and custom overrides.

    .EXAMPLE
    Add-UIBanner -Step "Progress" -Title "Installing..." -ProgressValue 45 -ProgressLabel "Installing components"

    Creates a banner with a progress indicator.

    .NOTES
    Banners are display-only controls and do not create parameters in the generated script.
    #>
    [CmdletBinding(DefaultParameterSetName = 'Simple')]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string]$Step,

        [Parameter(Mandatory = $false)]
        [string]$Title,

        [Parameter(Mandatory = $false)]
        [string]$Subtitle,

        [Parameter(Mandatory = $false)]
        [string]$Description,

        # Simplified Banner Styles (80% use cases)
        [Parameter(ParameterSetName = 'Simple')]
        [ValidateSet('Default', 'Gradient', 'Image', 'Minimal', 'Hero', 'Accent')]
        [string]$BannerStyle = 'Default',
        
        [Parameter(ParameterSetName = 'Simple')]
        [hashtable]$BannerConfig = @{},

        [Parameter(Mandatory = $false)]
        [string]$Icon,

        [Parameter(Mandatory = $false)]
        [string]$IconPath,

        [Parameter()]
        [int]$IconSize = 64,

        [Parameter()]
        [ValidateSet('Left', 'Right', 'Top', 'Bottom', 'Background')]
        [string]$IconPosition = 'Right',

        [Parameter()]
        [string]$IconColor = '#40FFFFFF',

        [Parameter()]
        [ValidateSet('None', 'Pulse', 'Rotate', 'Bounce')]
        [string]$IconAnimation = 'None',


        [Parameter()]
        [string]$BackgroundColor,

        [Parameter()]
        [string]$BackgroundImagePath,

        [Parameter()]
        [ValidateRange(0.0, 1.0)]
        [double]$BackgroundImageOpacity = 0.3,

        [Parameter()]
        [ValidateSet('Fill', 'Uniform', 'UniformToFill', 'None')]
        [string]$BackgroundImageStretch = 'Uniform',

        [Parameter()]
        [string]$GradientStart,

        [Parameter()]
        [string]$GradientEnd,

        [Parameter()]
        [int]$Height = 180,

        [Parameter()]
        [string]$OverlayImagePath,

        [Parameter()]
        [ValidateRange(0.0, 1.0)]
        [double]$OverlayImageOpacity = 0.5,

        [Parameter()]
        [ValidateSet('Left', 'Right', 'Center')]
        [string]$OverlayPosition = 'Right',

        [Parameter()]
        [int]$OverlayImageSize = 120,

        [Parameter()]
        [string]$ButtonText,

        [Parameter()]
        [string]$ButtonIcon,

        [Parameter()]
        [string]$ButtonColor = '#0078D4',

        [Parameter()]
        [int]$ProgressValue = -1,

        [Parameter()]
        [string]$ProgressLabel,

        [Parameter()]
        [array]$CarouselItems,

        [Parameter()]
        [switch]$AutoRotate,

        [Parameter()]
        [int]$RotateInterval = 3000,

        # Enhanced styling properties
        [Parameter()]
        [string]$TitleFontSize,

        [Parameter()]
        [string]$SubtitleFontSize,

        [Parameter()]
        [ValidateSet('Normal', 'Medium', 'SemiBold', 'Bold', 'ExtraBold')]
        [string]$TitleFontWeight,

        [Parameter()]
        [string]$TitleColor,

        [Parameter()]
        [string]$SubtitleColor,

        [Parameter()]
        [string]$FontFamily,

        [Parameter()]
        [int]$CornerRadius,

        [Parameter()]
        [double]$GradientAngle,

        [Parameter()]
        [string]$LinkUrl,

        [Parameter()]
        [switch]$Clickable
    )

    begin {
        Write-Verbose "Adding Banner control to step: $Step"

        # Ensure wizard is initialized
        if (-not $script:CurrentWizard) {
            throw "No UI initialized. Call New-PoshUIWizard first."
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

            # Generate unique name for banner (banners don't create parameters)
            $bannerName = "Banner_$([Guid]::NewGuid().ToString('N').Substring(0, 8))"

            # Apply BannerStyle presets if using Simple parameter set
            if ($PSCmdlet.ParameterSetName -eq 'Simple' -and $BannerStyle -ne 'Default') {
                Write-Verbose "Applying BannerStyle preset: $BannerStyle"
                
                switch ($BannerStyle) {
                    'Gradient' {
                        if (-not $BannerConfig.ContainsKey('GradientStart')) { $GradientStart = '#0078D4' }
                        if (-not $BannerConfig.ContainsKey('GradientEnd')) { $GradientEnd = '#004578' }
                        if (-not $BannerConfig.ContainsKey('GradientAngle')) { $GradientAngle = 135 }
                        if (-not $BannerConfig.ContainsKey('Height')) { $Height = 200 }
                    }
                    'Image' {
                        if (-not $BannerConfig.ContainsKey('BackgroundImageOpacity')) { $BackgroundImageOpacity = 0.5 }
                        if (-not $BannerConfig.ContainsKey('Height')) { $Height = 220 }
                        if (-not $BannerConfig.ContainsKey('TitleFontSize')) { $TitleFontSize = '36' }
                    }
                    'Minimal' {
                        if (-not $BannerConfig.ContainsKey('Height')) { $Height = 120 }
                        if (-not $BannerConfig.ContainsKey('TitleFontSize')) { $TitleFontSize = '24' }
                        if (-not $BannerConfig.ContainsKey('SubtitleFontSize')) { $SubtitleFontSize = '14' }
                        if (-not $BannerConfig.ContainsKey('CornerRadius')) { $CornerRadius = 4 }
                    }
                    'Hero' {
                        if (-not $BannerConfig.ContainsKey('Height')) { $Height = 300 }
                        if (-not $BannerConfig.ContainsKey('TitleFontSize')) { $TitleFontSize = '48' }
                        if (-not $BannerConfig.ContainsKey('SubtitleFontSize')) { $SubtitleFontSize = '20' }
                    }
                    'Accent' {
                        if (-not $BannerConfig.ContainsKey('BackgroundColor')) { $BackgroundColor = '#107C10' }
                        if (-not $BannerConfig.ContainsKey('Height')) { $Height = 160 }
                    }
                }
                
                # Apply BannerConfig overrides
                foreach ($key in $BannerConfig.Keys) {
                    $value = $BannerConfig[$key]
                    Write-Verbose "  Applying BannerConfig override: $key = $value"
                    Set-Variable -Name $key -Value $value -Scope Local -ErrorAction SilentlyContinue
                }
            }

            # Create the control
            $control = [UIControl]::new($bannerName, $Title, 'Banner')

            # Set all banner properties
            if ($PSBoundParameters.ContainsKey('Title')) { $control.SetProperty('BannerTitle', $Title) }
            if ($PSBoundParameters.ContainsKey('Subtitle')) { $control.SetProperty('BannerSubtitle', $Subtitle) }
            if ($PSBoundParameters.ContainsKey('Description')) { $control.SetProperty('Description', $Description) }
            if ($PSBoundParameters.ContainsKey('Icon')) { $control.SetProperty('BannerIcon', $Icon) }
            if ($PSBoundParameters.ContainsKey('IconPath')) { $control.SetProperty('IconPath', $IconPath) }
            $control.SetProperty('IconSize', $IconSize)
            $control.SetProperty('IconPosition', $IconPosition)
            $control.SetProperty('IconColor', $IconColor)
            $control.SetProperty('IconAnimation', $IconAnimation)
            if ($PSBoundParameters.ContainsKey('BackgroundColor')) { $control.SetProperty('BackgroundColor', $BackgroundColor) }
            if ($PSBoundParameters.ContainsKey('BackgroundImagePath')) { $control.SetProperty('BackgroundImagePath', $BackgroundImagePath) }
            $control.SetProperty('BackgroundImageOpacity', $BackgroundImageOpacity)
            $control.SetProperty('BackgroundImageStretch', $BackgroundImageStretch)
            if ($PSBoundParameters.ContainsKey('GradientStart')) { $control.SetProperty('GradientStart', $GradientStart) }
            if ($PSBoundParameters.ContainsKey('GradientEnd')) { $control.SetProperty('GradientEnd', $GradientEnd) }
            $control.SetProperty('Height', $Height)
            # Overlay image properties
            if ($PSBoundParameters.ContainsKey('OverlayImagePath')) { $control.SetProperty('OverlayImagePath', $OverlayImagePath) }
            $control.SetProperty('OverlayImageOpacity', $OverlayImageOpacity)
            $control.SetProperty('OverlayPosition', $OverlayPosition)
            $control.SetProperty('OverlayImageSize', $OverlayImageSize)
            if ($PSBoundParameters.ContainsKey('ButtonText')) { $control.SetProperty('ButtonText', $ButtonText) }
            if ($PSBoundParameters.ContainsKey('ButtonIcon')) { $control.SetProperty('ButtonIcon', $ButtonIcon) }
            $control.SetProperty('ButtonColor', $ButtonColor)
            $control.SetProperty('ProgressValue', $ProgressValue)
            if ($PSBoundParameters.ContainsKey('ProgressLabel')) { $control.SetProperty('ProgressLabel', $ProgressLabel) }
            if ($PSBoundParameters.ContainsKey('CarouselItems')) { $control.SetProperty('CarouselItems', $CarouselItems) }
            $control.SetProperty('AutoRotate', $AutoRotate.IsPresent)
            $control.SetProperty('RotateInterval', $RotateInterval)
            
            # Enhanced styling properties
            if ($PSBoundParameters.ContainsKey('TitleFontSize')) { $control.SetProperty('TitleFontSize', $TitleFontSize) }
            if ($PSBoundParameters.ContainsKey('SubtitleFontSize')) { $control.SetProperty('SubtitleFontSize', $SubtitleFontSize) }
            if ($PSBoundParameters.ContainsKey('TitleFontWeight')) { $control.SetProperty('TitleFontWeight', $TitleFontWeight) }
            if ($PSBoundParameters.ContainsKey('TitleColor')) { $control.SetProperty('TitleColor', $TitleColor) }
            if ($PSBoundParameters.ContainsKey('SubtitleColor')) { $control.SetProperty('SubtitleColor', $SubtitleColor) }
            if ($PSBoundParameters.ContainsKey('FontFamily')) { $control.SetProperty('FontFamily', $FontFamily) }
            if ($PSBoundParameters.ContainsKey('CornerRadius')) { $control.SetProperty('CornerRadius', $CornerRadius) }
            if ($PSBoundParameters.ContainsKey('GradientAngle')) { $control.SetProperty('GradientAngle', $GradientAngle) }
            if ($PSBoundParameters.ContainsKey('LinkUrl')) { $control.SetProperty('LinkUrl', $LinkUrl) }
            $control.SetProperty('Clickable', $Clickable.IsPresent)

            # Add control to step
            $wizardStep.AddControl($control)

            Write-Verbose "Banner added successfully: $Title"

            # Return the control
            return $control
        }
        catch {
            throw "Failed to add banner to step '$Step': $_"
        }
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardBanner' -Value 'Add-UIBanner'
