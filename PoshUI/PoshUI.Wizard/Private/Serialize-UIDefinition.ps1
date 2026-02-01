# Serialize-UIDefinition.ps1 - Convert UIDefinition to JSON

function Serialize-UIDefinition {
    <#
    .SYNOPSIS
    Converts a UIDefinition object to JSON format for the exe.
    
    .DESCRIPTION
    Serializes a UIDefinition object (with steps, controls, cards, banners) to JSON.
    Handles ScriptBlocks by converting them to strings. Separates cards from regular controls.
    
    .PARAMETER Definition
    The UIDefinition object to serialize.
    
    .OUTPUTS
    String containing the JSON representation of the UI definition.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [object]$Definition
    )
    
    try {
        Write-Verbose "Serializing UI definition to JSON: $($Definition.Title)"
        
        # Build branding object
        $branding = @{}
        if ($Definition.Branding -and $Definition.Branding.Count -gt 0) {
            foreach ($key in $Definition.Branding.Keys) {
                $branding[$key] = $Definition.Branding[$key]
            }
        }
        
        # Add explicit branding properties if set
        if (-not [string]::IsNullOrEmpty($Definition.SidebarHeaderText)) {
            $branding['SidebarHeaderText'] = $Definition.SidebarHeaderText
        }
        if (-not [string]::IsNullOrEmpty($Definition.SidebarHeaderIcon)) {
            $branding['SidebarHeaderIconPath'] = $Definition.SidebarHeaderIcon
        }
        if (-not [string]::IsNullOrEmpty($Definition.SidebarHeaderIconOrientation)) {
            $branding['SidebarHeaderIconOrientation'] = $Definition.SidebarHeaderIconOrientation
        }
        if (-not [string]::IsNullOrEmpty($Definition.WindowTitleIcon)) {
            $branding['WindowTitleIcon'] = $Definition.WindowTitleIcon
        }
        
        # Build steps array
        $stepsJson = @()
        foreach ($step in $Definition.Steps | Sort-Object Order) {
            $stepJson = @{
                Name = $step.Name
                Title = $step.Title
                Description = $step.Description
                Order = $step.Order
                Type = $step.Type
                Icon = $step.Icon
                Skippable = $step.Skippable
                Controls = @()
                Cards = @()
                Properties = @{}
            }
            
            # Separate controls and cards
            $regularControls = @()
            $cards = @()
            $banner = $null
            
            Write-Verbose "  Processing step '$($step.Name)' with $($step.Controls.Count) controls"
            foreach ($control in $step.Controls) {
                Write-Verbose "    Control: Name='$($control.Name)', Type='$($control.Type)'"
                # Check if this is a card (card types)
                $cardTypes = @('MetricCard', 'GraphCard', 'DataGridCard', 'ScriptCard', 'InfoCard')
                if ($control.Type -in $cardTypes) {
                    Write-Verbose "      -> Identified as CARD type"
                    # This is a card - map properties explicitly like banner
                    $cardJson = @{
                        CardType = $control.Type
                        Title = $control.GetPropertyOrDefault('CardTitle', $control.Label)
                        Content = $control.GetPropertyOrDefault('CardContent', $null)
                        Type = $control.GetPropertyOrDefault('CardType', 'Info')
                        Icon = $control.GetPropertyOrDefault('CardIcon', $null)
                        IconPath = $control.GetPropertyOrDefault('CardIconPath', $null)
                        ImagePath = $control.GetPropertyOrDefault('CardImagePath', $null)
                        ImageOpacity = $control.GetPropertyOrDefault('CardImageOpacity', 1.0)
                        LinkUrl = $control.GetPropertyOrDefault('CardLinkUrl', $null)
                        LinkText = $control.GetPropertyOrDefault('CardLinkText', 'Learn more')
                        BackgroundColor = $control.GetPropertyOrDefault('CardBackgroundColor', $null)
                        TitleColor = $control.GetPropertyOrDefault('CardTitleColor', $null)
                        ContentColor = $control.GetPropertyOrDefault('CardContentColor', $null)
                        CornerRadius = $control.GetPropertyOrDefault('CardCornerRadius', 8)
                        GradientStart = $control.GetPropertyOrDefault('CardGradientStart', $null)
                        GradientEnd = $control.GetPropertyOrDefault('CardGradientEnd', $null)
                        Width = $control.GetPropertyOrDefault('CardWidth', $null)
                        Height = $control.GetPropertyOrDefault('CardHeight', $null)
                    }

                    # Also copy any additional properties that might be in the control
                    foreach ($key in $control.Properties.Keys) {
                        if (-not $cardJson.ContainsKey($key)) {
                            $value = $control.Properties[$key]

                            # Convert ScriptBlocks to strings
                            if ($value -is [scriptblock]) {
                                $scriptContent = $value.ToString().Trim()
                                # Remove outer braces if present
                                if ($scriptContent.StartsWith('{') -and $scriptContent.EndsWith('}')) {
                                    $scriptContent = $scriptContent.Substring(1, $scriptContent.Length - 2).Trim()
                                }
                                $cardJson[$key] = $scriptContent
                            }
                            else {
                                $cardJson[$key] = $value
                            }
                        }
                    }

                    $cards += $cardJson
                }
                elseif ($control.Type -eq 'Banner') {
                    # This is a banner - copy ALL properties for the C# side
                    $banner = @{
                        # Core Properties
                        Title = $control.GetPropertyOrDefault('BannerTitle', $control.Label)
                        Subtitle = $control.GetPropertyOrDefault('BannerSubtitle', $null)
                        Description = $control.GetPropertyOrDefault('BannerSubtitle', $null)
                        DescriptionText = $control.GetPropertyOrDefault('Description', $null)
                        Icon = $control.GetPropertyOrDefault('BannerIcon', $null)
                        Type = $control.GetPropertyOrDefault('BannerType', 'info')
                        Category = $control.GetPropertyOrDefault('Category', 'General')
                        
                        # Layout & Sizing
                        Height = $control.GetPropertyOrDefault('Height', 180)
                        Width = $control.GetPropertyOrDefault('Width', 700)
                        MinHeight = $control.GetPropertyOrDefault('MinHeight', 120)
                        MaxHeight = $control.GetPropertyOrDefault('MaxHeight', 400)
                        Layout = $control.GetPropertyOrDefault('Layout', 'Left')
                        ContentAlignment = $control.GetPropertyOrDefault('ContentAlignment', 'Left')
                        VerticalAlignment = $control.GetPropertyOrDefault('VerticalAlignment', 'Center')
                        Padding = $control.GetPropertyOrDefault('Padding', '32,24')
                        CornerRadius = $control.GetPropertyOrDefault('CornerRadius', 12)
                        FullWidth = $control.GetPropertyOrDefault('FullWidth', $false)
                        
                        # Typography
                        TitleFontSize = $control.GetPropertyOrDefault('TitleFontSize', '32')
                        SubtitleFontSize = $control.GetPropertyOrDefault('SubtitleFontSize', '16')
                        DescriptionFontSize = $control.GetPropertyOrDefault('DescriptionFontSize', '14')
                        TitleFontWeight = $control.GetPropertyOrDefault('TitleFontWeight', 'Bold')
                        SubtitleFontWeight = $control.GetPropertyOrDefault('SubtitleFontWeight', 'Normal')
                        DescriptionFontWeight = $control.GetPropertyOrDefault('DescriptionFontWeight', 'Normal')
                        FontFamily = $control.GetPropertyOrDefault('FontFamily', 'Segoe UI')
                        TitleColor = $control.GetPropertyOrDefault('TitleColor', '#FFFFFF')
                        SubtitleColor = $control.GetPropertyOrDefault('SubtitleColor', '#B0B0B0')
                        DescriptionColor = $control.GetPropertyOrDefault('DescriptionColor', '#909090')
                        TitleAllCaps = $control.GetPropertyOrDefault('TitleAllCaps', $false)
                        TitleLetterSpacing = $control.GetPropertyOrDefault('TitleLetterSpacing', 0)
                        LineHeight = $control.GetPropertyOrDefault('LineHeight', 1.4)
                        
                        # Background & Visual Effects
                        BackgroundColor = $control.GetPropertyOrDefault('BackgroundColor', '#2D2D30')
                        BackgroundImagePath = $control.GetPropertyOrDefault('BackgroundImagePath', $null)
                        BackgroundImageOpacity = $control.GetPropertyOrDefault('BackgroundImageOpacity', 0.3)
                        BackgroundImageStretch = $control.GetPropertyOrDefault('BackgroundImageStretch', 'Uniform')
                        GradientStart = $control.GetPropertyOrDefault('GradientStart', $null)
                        GradientEnd = $control.GetPropertyOrDefault('GradientEnd', $null)
                        GradientAngle = $control.GetPropertyOrDefault('GradientAngle', 90)
                        BorderColor = $control.GetPropertyOrDefault('BorderColor', 'Transparent')
                        BorderThickness = $control.GetPropertyOrDefault('BorderThickness', 0)
                        ShadowIntensity = $control.GetPropertyOrDefault('ShadowIntensity', 'Medium')
                        Opacity = $control.GetPropertyOrDefault('Opacity', 1.0)
                        
                        # Icon & Image Options
                        IconPath = $control.GetPropertyOrDefault('IconPath', $null)
                        IconSize = $control.GetPropertyOrDefault('IconSize', 64)
                        IconPosition = $control.GetPropertyOrDefault('IconPosition', 'Right')
                        IconColor = $control.GetPropertyOrDefault('IconColor', '#40FFFFFF')
                        IconAnimation = $control.GetPropertyOrDefault('IconAnimation', 'None')
                        OverlayImagePath = $control.GetPropertyOrDefault('OverlayImagePath', $null)
                        OverlayImageOpacity = $control.GetPropertyOrDefault('OverlayImageOpacity', 0.5)
                        OverlayPosition = $control.GetPropertyOrDefault('OverlayPosition', 'Right')
                        OverlayImageSize = $control.GetPropertyOrDefault('OverlayImageSize', 120)
                        
                        # Interactive Elements
                        Clickable = $control.GetPropertyOrDefault('Clickable', $false)
                        ClickAction = $control.GetPropertyOrDefault('ClickAction', $null)
                        LinkUrl = $control.GetPropertyOrDefault('LinkUrl', $null)
                        LinkText = $control.GetPropertyOrDefault('LinkText', $null)
                        HoverEffect = $control.GetPropertyOrDefault('HoverEffect', 'None')
                        ButtonText = $control.GetPropertyOrDefault('ButtonText', $null)
                        ButtonIcon = $control.GetPropertyOrDefault('ButtonIcon', $null)
                        ButtonColor = $control.GetPropertyOrDefault('ButtonColor', '#0078D4')
                        ButtonTextColor = $control.GetPropertyOrDefault('ButtonTextColor', '#FFFFFF')
                        ShowCloseButton = $control.GetPropertyOrDefault('ShowCloseButton', $false)
                        
                        # Badge/Label
                        BadgeText = $control.GetPropertyOrDefault('BadgeText', $null)
                        BadgeColor = $control.GetPropertyOrDefault('BadgeColor', '#FF5722')
                        BadgeTextColor = $control.GetPropertyOrDefault('BadgeTextColor', '#FFFFFF')
                        BadgePosition = $control.GetPropertyOrDefault('BadgePosition', 'TopRight')
                        
                        # Progress Indicator
                        ProgressValue = $control.GetPropertyOrDefault('ProgressValue', -1)
                        ProgressLabel = $control.GetPropertyOrDefault('ProgressLabel', $null)
                        ProgressColor = $control.GetPropertyOrDefault('ProgressColor', '#0078D4')
                        ProgressBackgroundColor = $control.GetPropertyOrDefault('ProgressBackgroundColor', '#40FFFFFF')
                        
                        # Responsive Design
                        Responsive = $control.GetPropertyOrDefault('Responsive', $true)
                        SmallTitleFontSize = $control.GetPropertyOrDefault('SmallTitleFontSize', '24')
                        SmallSubtitleFontSize = $control.GetPropertyOrDefault('SmallSubtitleFontSize', '14')
                        SmallHeight = $control.GetPropertyOrDefault('SmallHeight', 140)
                        SmallIconSize = $control.GetPropertyOrDefault('SmallIconSize', 48)
                        ResponsiveBreakpoint = $control.GetPropertyOrDefault('ResponsiveBreakpoint', 500)
                        
                        # Animation
                        EntranceAnimation = $control.GetPropertyOrDefault('EntranceAnimation', 'None')
                        AnimationDuration = $control.GetPropertyOrDefault('AnimationDuration', 300)
                        
                        # Carousel - use CarouselSlidesJson (pre-serialized JSON string)
                        AutoRotate = $control.GetPropertyOrDefault('AutoRotate', $false)
                        RotateInterval = $control.GetPropertyOrDefault('RotateInterval', 3000)
                        NavigationStyle = $control.GetPropertyOrDefault('NavigationStyle', 'Dots')
                        CarouselSlidesJson = $control.GetPropertyOrDefault('CarouselSlidesJson', $null)
                    }
                }
                else {
                    # Regular control
                    $controlJson = @{
                        Name = $control.Name
                        Type = $control.Type
                        Label = $control.Label
                        Default = $control.Default
                        Mandatory = $control.Mandatory
                        HelpText = $control.HelpText
                        Width = $control.Width
                        ValidationPattern = $control.ValidationPattern
                        ValidationScript = $control.GetPropertyOrDefault('ValidationScript', $null)
                        ValidationMessage = $control.ValidationMessage
                        Choices = $control.Choices
                        Properties = @{}
                    }
                    
                    # Copy properties that need to be at top level for C# deserialization
                    # Dynamic control properties
                    if ($control.GetPropertyOrDefault('IsDynamic', $false)) {
                        $controlJson.IsDynamic = $true
                    }
                    $dsScriptBlock = $control.GetPropertyOrDefault('DataSourceScriptBlock', $null)
                    if ($dsScriptBlock) {
                        # Convert ScriptBlock to string
                        if ($dsScriptBlock -is [scriptblock]) {
                            $scriptContent = $dsScriptBlock.ToString().Trim()
                            if ($scriptContent.StartsWith('{') -and $scriptContent.EndsWith('}')) {
                                $scriptContent = $scriptContent.Substring(1, $scriptContent.Length - 2).Trim()
                            }
                            $controlJson.DataSourceScriptBlock = $scriptContent
                        } else {
                            $controlJson.DataSourceScriptBlock = $dsScriptBlock.ToString()
                        }
                    }
                    $dsDependsOn = $control.GetPropertyOrDefault('DataSourceDependsOn', $null)
                    if ($dsDependsOn) {
                        $controlJson.DataSourceDependsOn = $dsDependsOn
                    }
                    
                    # Numeric control properties
                    $min = $control.GetPropertyOrDefault('Minimum', $null)
                    if ($null -ne $min) { $controlJson.Minimum = $min }
                    $max = $control.GetPropertyOrDefault('Maximum', $null)
                    if ($null -ne $max) { $controlJson.Maximum = $max }
                    $step = $control.GetPropertyOrDefault('Step', $null)
                    if ($null -ne $step) { $controlJson.Step = $step }
                    
                    # Other control properties
                    if ($control.GetPropertyOrDefault('Multiline', $false)) {
                        $controlJson.Multiline = $true
                    }
                    $maxLen = $control.GetPropertyOrDefault('MaxLength', $null)
                    if ($maxLen) { $controlJson.MaxLength = $maxLen }
                    $placeholder = $control.GetPropertyOrDefault('Placeholder', $null)
                    if ($placeholder) { $controlJson.Placeholder = $placeholder }
                    $filter = $control.GetPropertyOrDefault('Filter', $null)
                    if ($filter) { $controlJson.Filter = $filter }
                    if ($control.GetPropertyOrDefault('IsMultiSelect', $false)) {
                        $controlJson.IsMultiSelect = $true
                    }
                    if ($control.GetPropertyOrDefault('ShowRevealButton', $false)) {
                        $controlJson.ShowRevealButton = $true
                    }
                    
                    # Copy remaining properties to Properties dictionary
                    $topLevelKeys = @('IsDynamic', 'DataSourceScriptBlock', 'DataSourceDependsOn', 
                                      'Minimum', 'Maximum', 'Step', 'Multiline', 'MaxLength', 
                                      'Placeholder', 'Filter', 'IsMultiSelect', 'ShowRevealButton')
                    foreach ($key in $control.Properties.Keys) {
                        if ($key -in $topLevelKeys) { continue }
                        
                        $value = $control.Properties[$key]
                        
                        if ($value -is [scriptblock]) {
                            $scriptContent = $value.ToString().Trim()
                            # Remove outer braces if present
                            if ($scriptContent.StartsWith('{') -and $scriptContent.EndsWith('}')) {
                                $scriptContent = $scriptContent.Substring(1, $scriptContent.Length - 2).Trim()
                            }
                            $controlJson.Properties[$key] = $scriptContent
                        }
                        else {
                            $controlJson.Properties[$key] = $value
                        }
                    }
                    
                    $regularControls += $controlJson
                }
            }
            
            $stepJson.Controls = @($regularControls)  # Force array even if single element
            $stepJson.Cards = @($cards)  # Force array even if single element
            Write-Verbose "  Step '$($step.Name)' serialized: $($regularControls.Count) controls, $($cards.Count) cards, banner=$($null -ne $banner)"
            if ($banner) {
                $stepJson.Banner = $banner
            }
            
            # Copy step properties
            if ($step.Properties -and $step.Properties.Count -gt 0) {
                foreach ($key in $step.Properties.Keys) {
                    $stepJson.Properties[$key] = $step.Properties[$key]
                }
            }
            
            $stepsJson += $stepJson
        }
        
        # Build root object
        $jsonObject = @{
            Title = $Definition.Title
            Description = $Definition.Description
            Template = "Wizard"  # HARDCODED - Wizard module always uses Wizard template
            Theme = $Definition.Theme
            AllowCancel = $Definition.AllowCancel
            GridColumns = $Definition.GridColumns
            Branding = $branding
            Steps = $stepsJson
            ScriptBody = if ($Definition.ScriptBody) {
                $scriptContent = $Definition.ScriptBody.ToString().Trim()
                # Remove outer braces if present (ScriptBlock.ToString() includes them)
                if ($scriptContent.StartsWith('{') -and $scriptContent.EndsWith('}')) {
                    $scriptContent = $scriptContent.Substring(1, $scriptContent.Length - 2).Trim()
                }
                $scriptContent
            } else {
                $null
            }
            Variables = $Definition.Variables
        }
        
        # Convert to JSON with proper depth
        $json = $jsonObject | ConvertTo-Json -Depth 20 -Compress:$false
        
        Write-Verbose "Serialization complete. JSON length: $($json.Length) characters"
        return $json
    }
    catch {
        Write-Error "Failed to serialize UI definition: $_"
        throw
    }
}
