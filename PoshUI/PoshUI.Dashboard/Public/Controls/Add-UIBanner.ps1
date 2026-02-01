function Add-UIBanner {
    <#
    .SYNOPSIS
    Adds a banner component to a UI step for displaying hero content.
    
    .DESCRIPTION
    Creates a highly customizable banner with title, subtitle, optional icon, background image,
    gradients, interactive elements, and responsive design. Banners are ideal for welcome screens,
    section headers, dashboards, and promotional content.
    
    .PARAMETER Step
    Name of the step to add this banner to.
    
    .PARAMETER Name
    Unique name for the banner.
    
    .PARAMETER Title
    Main title text displayed prominently.
    
    .PARAMETER Subtitle
    Secondary text displayed below the title.
    
    .PARAMETER Description
    Additional descriptive text below the subtitle.
    
    .PARAMETER Icon
    Optional icon glyph (e.g., '&#xE950;').
    
    .PARAMETER IconPath
    Path to an image file to use as icon instead of glyph.
    
    .PARAMETER IconSize
    Size of the icon in pixels (default: 64).
    
    .PARAMETER IconPosition
    Position of the icon: Left, Right, Top, Bottom, Background (default: Right).
    
    .PARAMETER IconColor
    Color of the icon glyph (default: #40FFFFFF).
    
    .PARAMETER IconAnimation
    Animation for the icon: None, Pulse, Rotate, Bounce (default: None).
    
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
    
    .PARAMETER BackgroundImagePath
    Optional path to a background image.
    
    .PARAMETER BackgroundImageOpacity
    Opacity of the background image (default: 0.3).
    
    .PARAMETER BackgroundColor
    Background color (default: #2D2D30).
    
    .PARAMETER GradientStart
    Start color for gradient background.
    
    .PARAMETER GradientEnd
    End color for gradient background.
    
    .PARAMETER GradientAngle
    Angle of the gradient in degrees (default: 90).
    
    .PARAMETER TitleColor
    Title text color (default: #FFFFFF).
    
    .PARAMETER SubtitleColor
    Subtitle text color (default: #B0B0B0).
    
    .PARAMETER DescriptionColor
    Description text color (default: #909090).
    
    .PARAMETER TitleFontSize
    Font size for the title (default: 32).
    
    .PARAMETER SubtitleFontSize
    Font size for the subtitle (default: 16).
    
    .PARAMETER TitleFontWeight
    Font weight for title: Normal, Medium, SemiBold, Bold, ExtraBold (default: Bold).
    
    .PARAMETER TitleAllCaps
    Display title in all uppercase letters.
    
    .PARAMETER FontFamily
    Font family for all text (default: Segoe UI).
    
    .PARAMETER Height
    Height of the banner in pixels (default: 180).
    
    .PARAMETER Width
    Width of the banner in pixels (default: 700).
    
    .PARAMETER FullWidth
    Stretch banner to full available width.
    
    .PARAMETER Layout
    Content layout: Left, Center, Right (default: Left).
    
    .PARAMETER ContentAlignment
    Text alignment within content area: Left, Center, Right (default: Left).
    
    .PARAMETER CornerRadius
    Border corner radius in pixels (default: 12).
    
    .PARAMETER ShadowIntensity
    Shadow effect intensity: None, Light, Medium, Heavy (default: Medium).
    
    .PARAMETER Clickable
    Make the entire banner clickable.
    
    .PARAMETER ClickAction
    ScriptBlock to execute when banner is clicked.
    
    .PARAMETER LinkUrl
    URL to open when banner is clicked.
    
    .PARAMETER HoverEffect
    Effect on hover: None, Lift, Glow, Zoom, Darken (default: None).
    
    .PARAMETER ButtonText
    Text for an action button on the banner.
    
    .PARAMETER ButtonIcon
    Icon glyph for the action button.
    
    .PARAMETER ButtonColor
    Background color of the action button (default: #0078D4).
    
    .PARAMETER BadgeText
    Text for a small badge/label on the banner.
    
    .PARAMETER BadgeColor
    Background color of the badge (default: #FF5722).
    
    .PARAMETER BadgePosition
    Position of the badge: TopLeft, TopRight, BottomLeft, BottomRight (default: TopRight).
    
    .PARAMETER OverlayImagePath
    Path to an overlay image displayed on the banner.
    
    .PARAMETER OverlayImageOpacity
    Opacity of the overlay image (default: 0.5).
    
    .PARAMETER OverlayPosition
    Position of overlay image: Left, Right, Center (default: Right).
    
    .PARAMETER OverlayImageSize
    Size of the overlay image in pixels (default: 120).
    
    .PARAMETER EntranceAnimation
    Entrance animation: None, FadeIn, SlideIn, ZoomIn (default: None).
    
    .PARAMETER AnimationDuration
    Duration of entrance animation in milliseconds (default: 300).
    
    .PARAMETER Category
    Category for grouping/filtering banners (default: General).
    
    .EXAMPLE
    Add-UIBanner -Step "Welcome" -Title "Welcome to Setup" -Subtitle "Let's get started"

    Creates a simple banner with default styling.
    
    .EXAMPLE
    Add-UIBanner -Step "Dashboard" -Title "System Dashboard" -Subtitle "Monitor your infrastructure" `
        -BannerStyle "Gradient"

    Creates a gradient banner using the preset style (blue gradient, 200px height).
    
    .EXAMPLE
    Add-UIBanner -Step "Dashboard" -Title "Hero Banner" -Subtitle "Welcome" `
        -BannerStyle "Hero" `
        -BannerConfig @{ Height = 350; TitleFontSize = 52 }

    Creates a hero banner with preset style and custom overrides.
    
    .EXAMPLE
    Add-UIBanner -Step "Dashboard" -Name "CustomBanner" `
        -Title "System Dashboard" `
        -Subtitle "Monitor your infrastructure" `
        -GradientStart "#0078D4" `
        -GradientEnd "#004578" `
        -GradientAngle 135 `
        -Height 220 `
        -TitleFontSize 36 `
        -HoverEffect "Lift"
    
    Creates a fully customized banner with individual parameters (advanced usage).
    
    .EXAMPLE
    Add-UIBanner -Step "Promo" -Name "PromoBanner" `
        -Title "NEW FEATURE" `
        -TitleAllCaps `
        -Subtitle "Check out our latest update" `
        -ButtonText "Learn More" `
        -ButtonIcon "&#xE8A7;" `
        -BadgeText "NEW" `
        -BadgePosition "TopRight" `
        -Clickable `
        -LinkUrl "https://example.com"
    
    #>
    [CmdletBinding(DefaultParameterSetName = 'Simple')]
    param(
        # ═══════════════════════════════════════════════════════════════════════════════
        # Core Parameters
        # ═══════════════════════════════════════════════════════════════════════════════
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string]$Step,
        
        [Parameter(Mandatory = $false, Position = 1)]
        [string]$Name,
        
        [Parameter(Mandatory = $true, Position = 2)]
        [ValidateNotNullOrEmpty()]
        [string]$Title,
        
        [Parameter()]
        [string]$Subtitle,
        
        [Parameter()]
        [string]$Description,
        
        [Parameter()]
        [string]$Category = "General",

        # ═══════════════════════════════════════════════════════════════════════════════
        # Simplified Banner Styles (80% use cases)
        # ═══════════════════════════════════════════════════════════════════════════════
        [Parameter(ParameterSetName = 'Simple')]
        [ValidateSet('Default', 'Gradient', 'Image', 'Minimal', 'Hero', 'Accent')]
        [string]$BannerStyle = 'Default',
        
        [Parameter(ParameterSetName = 'Simple')]
        [hashtable]$BannerConfig = @{},
        
        # ═══════════════════════════════════════════════════════════════════════════════
        # Layout & Sizing Options
        # ═══════════════════════════════════════════════════════════════════════════════
        [Parameter()]
        [ValidateRange(80, 600)]
        [int]$Height = 180,
        
        [Parameter()]
        [ValidateRange(200, 2000)]
        [int]$Width = 700,
        
        [Parameter()]
        [switch]$FullWidth,
        
        [Parameter()]
        [ValidateSet('Left', 'Center', 'Right')]
        [string]$Layout = "Left",
        
        [Parameter()]
        [ValidateSet('Left', 'Center', 'Right')]
        [string]$ContentAlignment = "Left",
        
        [Parameter()]
        [ValidateSet('Top', 'Center', 'Bottom')]
        [string]$VerticalAlignment = "Center",
        
        [Parameter()]
        [string]$Padding = "32,24",
        
        [Parameter()]
        [ValidateRange(0, 50)]
        [int]$CornerRadius = 12,

        # ═══════════════════════════════════════════════════════════════════════════════
        # Typography Enhancements
        # ═══════════════════════════════════════════════════════════════════════════════
        [Parameter()]
        [ValidateRange(12, 72)]
        [int]$TitleFontSize = 32,
        
        [Parameter()]
        [ValidateRange(10, 36)]
        [int]$SubtitleFontSize = 16,
        
        [Parameter()]
        [ValidateRange(10, 24)]
        [int]$DescriptionFontSize = 14,
        
        [Parameter()]
        [ValidateSet('Normal', 'Medium', 'SemiBold', 'Bold', 'ExtraBold')]
        [string]$TitleFontWeight = "Bold",
        
        [Parameter()]
        [ValidateSet('Normal', 'Medium', 'SemiBold', 'Bold')]
        [string]$SubtitleFontWeight = "Normal",
        
        [Parameter()]
        [string]$FontFamily = "Segoe UI",
        
        [Parameter()]
        [string]$TitleColor = "#FFFFFF",
        
        [Parameter()]
        [string]$SubtitleColor = "#B0B0B0",
        
        [Parameter()]
        [string]$DescriptionColor = "#909090",
        
        [Parameter()]
        [switch]$TitleAllCaps,
        
        [Parameter()]
        [ValidateRange(0, 10)]
        [double]$TitleLetterSpacing = 0,

        # ═══════════════════════════════════════════════════════════════════════════════
        # Background & Visual Effects
        # ═══════════════════════════════════════════════════════════════════════════════
        [Parameter()]
        [string]$BackgroundColor = "#2D2D30",
        
        [Parameter()]
        [string]$BackgroundImagePath,
        
        [Parameter()]
        [ValidateRange(0.0, 1.0)]
        [double]$BackgroundImageOpacity = 0.3,
        
        [Parameter()]
        [ValidateSet('Fill', 'Uniform', 'UniformToFill', 'None')]
        [string]$BackgroundImageStretch = "Uniform",
        
        [Parameter()]
        [string]$GradientStart,
        
        [Parameter()]
        [string]$GradientEnd,
        
        [Parameter()]
        [ValidateRange(0, 360)]
        [int]$GradientAngle = 90,
        
        [Parameter()]
        [string]$BorderColor = "Transparent",
        
        [Parameter()]
        [ValidateRange(0, 10)]
        [int]$BorderThickness = 0,
        
        [Parameter()]
        [ValidateSet('None', 'Light', 'Medium', 'Heavy')]
        [string]$ShadowIntensity = "Medium",

        [Parameter()]
        [ValidateRange(0.0, 1.0)]
        [double]$Opacity = 1.0,

        # ═══════════════════════════════════════════════════════════════════════════════
        # Icon & Image Options
        # ═══════════════════════════════════════════════════════════════════════════════
        [Parameter()]
        [Alias('BannerIcon')]
        [string]$Icon,
        
        [Parameter()]
        [string]$IconPath,
        
        [Parameter()]
        [ValidateRange(16, 200)]
        [int]$IconSize = 64,
        
        [Parameter()]
        [ValidateSet('Left', 'Right', 'Top', 'Bottom', 'Background')]
        [string]$IconPosition = "Right",
        
        [Parameter()]
        [string]$IconColor = "#40FFFFFF",
        
        [Parameter()]
        [ValidateSet('None', 'Pulse', 'Rotate', 'Bounce')]
        [string]$IconAnimation = "None",
        
        [Parameter()]
        [string]$OverlayImagePath,
        
        [Parameter()]
        [ValidateRange(0.0, 1.0)]
        [double]$OverlayImageOpacity = 0.5,
        
        [Parameter()]
        [ValidateSet('Left', 'Right', 'Center')]
        [string]$OverlayPosition = "Right",
        
        [Parameter()]
        [ValidateRange(40, 400)]
        [int]$OverlayImageSize = 120,

        # ═══════════════════════════════════════════════════════════════════════════════
        # Interactive Elements
        # ═══════════════════════════════════════════════════════════════════════════════
        [Parameter()]
        [switch]$Clickable,
        
        [Parameter()]
        [scriptblock]$ClickAction,
        
        [Parameter()]
        [string]$LinkUrl,
        
        [Parameter()]
        [string]$LinkText,
        
        [Parameter()]
        [ValidateSet('None', 'Lift', 'Glow', 'Zoom', 'Darken')]
        [string]$HoverEffect = "None",
        
        [Parameter()]
        [string]$ButtonText,
        
        [Parameter()]
        [string]$ButtonIcon,
        
        [Parameter()]
        [string]$ButtonColor = "#0078D4",
        
        [Parameter()]
        [string]$ButtonTextColor = "#FFFFFF",
        
        [Parameter()]
        [switch]$ShowCloseButton,

        # ═══════════════════════════════════════════════════════════════════════════════
        # Badge/Label
        # ═══════════════════════════════════════════════════════════════════════════════
        [Parameter()]
        [string]$BadgeText,
        
        [Parameter()]
        [string]$BadgeColor = "#FF5722",
        
        [Parameter()]
        [string]$BadgeTextColor = "#FFFFFF",
        
        [Parameter()]
        [ValidateSet('TopLeft', 'TopRight', 'BottomLeft', 'BottomRight')]
        [string]$BadgePosition = "TopRight",

        # ═══════════════════════════════════════════════════════════════════════════════
        # Progress Indicator
        # ═══════════════════════════════════════════════════════════════════════════════
        [Parameter()]
        [ValidateRange(-1, 100)]
        [int]$ProgressValue = -1,

        [Parameter()]
        [string]$ProgressLabel,

        [Parameter()]
        [string]$ProgressColor = "#0078D4",

        [Parameter()]
        [string]$ProgressBackgroundColor = "#40FFFFFF",

        # ═══════════════════════════════════════════════════════════════════════════════
        # Responsive Design
        # ═══════════════════════════════════════════════════════════════════════════════
        [Parameter()]
        [switch]$Responsive,

        [Parameter()]
        [ValidateRange(12, 48)]
        [int]$SmallTitleFontSize = 24,

        [Parameter()]
        [ValidateRange(10, 24)]
        [int]$SmallSubtitleFontSize = 14,

        [Parameter()]
        [ValidateRange(80, 300)]
        [int]$SmallHeight = 140,

        [Parameter()]
        [ValidateRange(16, 100)]
        [int]$SmallIconSize = 48,

        [Parameter()]
        [ValidateRange(300, 800)]
        [int]$ResponsiveBreakpoint = 500,

        # ═══════════════════════════════════════════════════════════════════════════════
        # Animation
        # ═══════════════════════════════════════════════════════════════════════════════
        [Parameter()]
        [ValidateSet('None', 'FadeIn', 'SlideIn', 'ZoomIn')]
        [string]$EntranceAnimation = "None",

        [Parameter()]
        [ValidateRange(100, 2000)]
        [int]$AnimationDuration = 300,

        # ═══════════════════════════════════════════════════════════════════════════════
        # Carousel
        # ═══════════════════════════════════════════════════════════════════════════════
        [Parameter()]
        [hashtable[]]$CarouselSlides,

        [Parameter()]
        [bool]$AutoRotate = $false,

        [Parameter()]
        [ValidateRange(1000, 10000)]
        [int]$RotateInterval = 3000,

        [Parameter()]
        [ValidateSet('Dots', 'Arrows', 'None')]
        [string]$NavigationStyle = "Dots"
    )

    # Validate wizard context
    if (-not $script:CurrentWizard) {
        throw "No UI initialized. Call New-PoshUI first."
    }
    
    # Find the target step
    Write-Verbose "Looking for step '$Step' in $($script:CurrentWizard.Steps.Count) steps"
    $targetStep = $script:CurrentWizard.Steps | Where-Object { $_.Name -eq $Step }
    if (-not $targetStep) {
        Write-Verbose "Available steps: $($script:CurrentWizard.Steps | ForEach-Object { $_.Name } | Join-String -Separator ', ')"
        throw "Step '$Step' not found. Add the step first with Add-UIStep."
    }
    Write-Verbose "Found step '$Step' with $($targetStep.Controls.Count) existing controls"

    # Generate unique name if not provided
    if ([string]::IsNullOrEmpty($Name)) {
        $Name = "Banner_$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
    }

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
                if (-not $BannerConfig.ContainsKey('TitleFontSize')) { $TitleFontSize = 36 }
            }
            'Minimal' {
                if (-not $BannerConfig.ContainsKey('Height')) { $Height = 120 }
                if (-not $BannerConfig.ContainsKey('TitleFontSize')) { $TitleFontSize = 24 }
                if (-not $BannerConfig.ContainsKey('SubtitleFontSize')) { $SubtitleFontSize = 14 }
                if (-not $BannerConfig.ContainsKey('ShadowIntensity')) { $ShadowIntensity = 'None' }
            }
            'Hero' {
                if (-not $BannerConfig.ContainsKey('Height')) { $Height = 300 }
                if (-not $BannerConfig.ContainsKey('TitleFontSize')) { $TitleFontSize = 48 }
                if (-not $BannerConfig.ContainsKey('SubtitleFontSize')) { $SubtitleFontSize = 20 }
                if (-not $BannerConfig.ContainsKey('Layout')) { $Layout = 'Center' }
                if (-not $BannerConfig.ContainsKey('ContentAlignment')) { $ContentAlignment = 'Center' }
            }
            'Accent' {
                if (-not $BannerConfig.ContainsKey('BackgroundColor')) { $BackgroundColor = '#107C10' }
                if (-not $BannerConfig.ContainsKey('Height')) { $Height = 160 }
                if (-not $BannerConfig.ContainsKey('HoverEffect')) { $HoverEffect = 'Lift' }
            }
        }
        
        # Apply BannerConfig overrides
        foreach ($key in $BannerConfig.Keys) {
            $value = $BannerConfig[$key]
            Write-Verbose "  Applying BannerConfig override: $key = $value"
            Set-Variable -Name $key -Value $value -Scope Local -ErrorAction SilentlyContinue
        }
    }

    # Create banner control
    $banner = [UIControl]::new()
    $banner.Name = $Name
    $banner.Label = $Title
    $banner.Type = "Banner"
    
    # Pre-compute values that require conditionals (PowerShell 5.1 compatibility)
    $bannerIconValue = $null
    if ($Icon) {
        if ($Icon -match '&#x([0-9A-Fa-f]+);') {
            $hexValue = $matches[1]
            $bannerIconValue = [char]::ConvertFromUtf32([int]"0x$hexValue")
        } else {
            $bannerIconValue = $Icon
        }
    }
    
    $buttonIconValue = $null
    if ($ButtonIcon) {
        if ($ButtonIcon -match '&#x([0-9A-Fa-f]+);') {
            $hexValue = $matches[1]
            $buttonIconValue = [char]::ConvertFromUtf32([int]"0x$hexValue")
        } else {
            $buttonIconValue = $ButtonIcon
        }
    }
    
    $clickActionValue = $null
    if ($ClickAction) { $clickActionValue = $ClickAction.ToString() }
    
    $carouselSlidesJsonValue = $null
    if ($CarouselSlides) { $carouselSlidesJsonValue = $CarouselSlides | ConvertTo-Json -Depth 5 -Compress }
    
    # Build properties hashtable with all enhanced options
    $banner.Properties = @{
        # Core
        BannerTitle = $Title
        BannerSubtitle = $Subtitle
        Description = $Description
        Category = $Category
        BannerType = "default"
        
        # Layout & Sizing
        Height = $Height
        Width = $Width
        FullWidth = $FullWidth.IsPresent
        Layout = $Layout
        ContentAlignment = $ContentAlignment
        VerticalAlignment = $VerticalAlignment
        Padding = $Padding
        CornerRadius = $CornerRadius
        
        # Typography
        TitleFontSize = $TitleFontSize.ToString()
        SubtitleFontSize = $SubtitleFontSize.ToString()
        DescriptionFontSize = $DescriptionFontSize.ToString()
        TitleFontWeight = $TitleFontWeight
        SubtitleFontWeight = $SubtitleFontWeight
        FontFamily = $FontFamily
        TitleColor = $TitleColor
        SubtitleColor = $SubtitleColor
        DescriptionColor = $DescriptionColor
        TitleAllCaps = $TitleAllCaps.IsPresent
        TitleLetterSpacing = $TitleLetterSpacing
        
        # Background & Visual Effects
        BackgroundColor = $BackgroundColor
        BackgroundImagePath = $BackgroundImagePath
        BackgroundImageOpacity = $BackgroundImageOpacity
        BackgroundImageStretch = $BackgroundImageStretch
        GradientStart = $GradientStart
        GradientEnd = $GradientEnd
        GradientAngle = $GradientAngle
        BorderColor = $BorderColor
        BorderThickness = $BorderThickness
        ShadowIntensity = $ShadowIntensity
        Opacity = $Opacity

        # Icon & Image
        BannerIcon = $bannerIconValue
        IconPath = $IconPath
        IconSize = $IconSize
        IconPosition = $IconPosition
        IconColor = $IconColor
        IconAnimation = $IconAnimation
        OverlayImagePath = $OverlayImagePath
        OverlayImageOpacity = $OverlayImageOpacity
        OverlayPosition = $OverlayPosition
        OverlayImageSize = $OverlayImageSize
        
        # Interactive
        Clickable = $Clickable.IsPresent
        ClickAction = $clickActionValue
        LinkUrl = $LinkUrl
        LinkText = $LinkText
        HoverEffect = $HoverEffect
        ButtonText = $ButtonText
        ButtonIcon = $buttonIconValue
        ButtonColor = $ButtonColor
        ButtonTextColor = $ButtonTextColor
        ShowCloseButton = $ShowCloseButton.IsPresent
        
        # Badge
        BadgeText = $BadgeText
        BadgeColor = $BadgeColor
        BadgeTextColor = $BadgeTextColor
        BadgePosition = $BadgePosition

        # Progress
        ProgressValue = $ProgressValue
        ProgressLabel = $ProgressLabel
        ProgressColor = $ProgressColor
        ProgressBackgroundColor = $ProgressBackgroundColor

        # Responsive
        Responsive = $Responsive.IsPresent
        SmallTitleFontSize = $SmallTitleFontSize.ToString()
        SmallSubtitleFontSize = $SmallSubtitleFontSize.ToString()
        SmallHeight = $SmallHeight
        SmallIconSize = $SmallIconSize
        ResponsiveBreakpoint = $ResponsiveBreakpoint

        # Animation
        EntranceAnimation = $EntranceAnimation
        AnimationDuration = $AnimationDuration

        # Carousel - serialize slides as JSON string to avoid complex object issues
        CarouselSlidesJson = $carouselSlidesJsonValue
        AutoRotate = $AutoRotate
        RotateInterval = $RotateInterval
        NavigationStyle = $NavigationStyle
    }
    
    # Add to step controls using the AddControl method
    $targetStep.AddControl($banner)

    Write-Verbose "Added enhanced banner '$Name' to step '$Step'. Step now has $($targetStep.Controls.Count) controls"
    
    return $banner
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardBanner' -Value 'Add-UIBanner'
