# ConvertTo-UIScript.ps1 - Script generation engine for AST parsing
# Converts UIDefinition to a PowerShell script that the EXE can AST-parse

function ConvertTo-UIScript {
    <#
    .SYNOPSIS
    Converts a UIDefinition object to a traditional parameter-based PowerShell script.
    
    .DESCRIPTION
    Internal function that generates a PowerShell script with param() block and attributes
    that matches the AST parsing format. This allows the WPF executable to process
    wizards created with the module functions using ReflectionService.
    
    .PARAMETER Definition
    The UIDefinition object to convert to a script.
    
    .PARAMETER ScriptBody
    Optional script body to append after the parameter block.
    
    .OUTPUTS
    String containing the generated PowerShell script.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [UIDefinition]$Definition,
        
        [Parameter()]
        [scriptblock]$ScriptBody
    )
    
    Write-Verbose "Converting UI definition to script: $($Definition.Title)"
    
    try {
        $scriptLines = @()
        $scriptLines += "# Generated PoshUI script"
        $scriptLines += "# Created: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        $scriptLines += "# Wizard: $($Definition.Title)"
        $scriptLines += ""
        
        # Start param block
        $scriptLines += "param("
        
        $parameterLines = @()
        
        # Add branding parameter
        $needsBranding = (-not [string]::IsNullOrEmpty($Definition.Title)) -or
                        ($Definition.Branding.Count -gt 0) -or 
                        (-not [string]::IsNullOrEmpty($Definition.SidebarHeaderText)) -or
                        (-not [string]::IsNullOrEmpty($Definition.SidebarHeaderIcon))
        
        if ($needsBranding) {
            $brandingLines = @()
            $brandingLines += "    [Parameter(Mandatory=`$false)]"
            
            # Build branding attribute
            $brandingParts = @()
            
            # Check if WindowTitleText is explicitly set in branding
            $hasExplicitWindowTitle = $Definition.Branding.ContainsKey('WindowTitleText') -and 
                                     (-not [string]::IsNullOrEmpty($Definition.Branding['WindowTitleText']))
            
            # Add WindowTitleText from wizard Title if not explicitly set
            if (-not $hasExplicitWindowTitle -and (-not [string]::IsNullOrEmpty($Definition.Title))) {
                $brandingParts += "WindowTitleText = '$($Definition.Title)'"
            }
            
            # Track which keys we've already added
            $addedKeys = @{}
            
            # Add from Branding hashtable
            foreach ($key in $Definition.Branding.Keys) {
                $value = $Definition.Branding[$key]
                if ([string]::IsNullOrEmpty($value)) {
                    continue
                }
                $brandingParts += "$key = '$value'"
                $addedKeys[$key] = $true
            }
            
            # Add SidebarHeaderText if set
            if (-not $addedKeys.ContainsKey('SidebarHeaderText') -and 
                -not [string]::IsNullOrEmpty($Definition.SidebarHeaderText)) {
                $brandingParts += "SidebarHeaderText = '$($Definition.SidebarHeaderText)'"
                $addedKeys['SidebarHeaderText'] = $true
            }
            
            # Add SidebarHeaderIconPath if set
            if (-not $addedKeys.ContainsKey('SidebarHeaderIconPath') -and 
                -not [string]::IsNullOrEmpty($Definition.SidebarHeaderIcon)) {
                $brandingParts += "SidebarHeaderIconPath = '$($Definition.SidebarHeaderIcon)'"
                $addedKeys['SidebarHeaderIconPath'] = $true
            }
            
            # Add Theme
            if (-not [string]::IsNullOrEmpty($Definition.Theme)) {
                $brandingParts += "Theme = '$($Definition.Theme)'"
            }
            
            $brandingAttribute = "[WizardBranding($($brandingParts -join ', '))]"
            $brandingLines += "    $brandingAttribute"
            $brandingLines += "    [string]`$BrandingPlaceholder,"
            $brandingLines += ""
            
            $parameterLines += $brandingLines
        }
        
        # Process steps in order
        $sortedSteps = $Definition.Steps | Sort-Object Order
        
        foreach ($step in $sortedSteps) {
            Write-Verbose "Processing step: $($step.Name)"
            
            # Add step header comment
            $parameterLines += "    # --- Step: $($step.Title) ---"
            
            # Convert step controls
            $parameterLines += Convert-UIFormStep -Step $step
            $parameterLines += ""
        }
        
        # Remove trailing comma from last parameter
        if ($parameterLines.Count -gt 0) {
            for ($i = $parameterLines.Count - 1; $i -ge 0; $i--) {
                $line = $parameterLines[$i]
                if (-not [string]::IsNullOrWhiteSpace($line) -and $line.Trim().EndsWith(',')) {
                    $parameterLines[$i] = $line.TrimEnd(',')
                    break
                }
            }
        }
        
        # Add parameters to script
        $scriptLines += $parameterLines
        
        # Close param block
        $scriptLines += ")"
        $scriptLines += ""
        
        # Add script body if provided
        if ($ScriptBody) {
            $scriptLines += "# --- Script Body ---"
            $scriptLines += $ScriptBody.ToString()
        } else {
            $scriptLines += "# --- Default Script Body ---"
            $scriptLines += "Write-Host 'Wizard completed successfully!' -ForegroundColor Green"
        }
        
        $generatedScript = $scriptLines -join "`n"
        
        Write-Verbose "Successfully generated script ($($scriptLines.Count) lines)"
        
        return $generatedScript
    }
    catch {
        Write-Error "Failed to convert UI definition to script: $($_.Exception.Message)"
        throw
    }
}

function Convert-UIFormStep {
    param([UIStep]$Step)
    
    Write-Verbose "Converting step: $($Step.Title) with $($Step.Controls.Count) controls"
    
    $lines = @()
    $isFirstNonCardControl = $true
    
    foreach ($control in $Step.Controls) {
        Write-Verbose "  Processing control: Name=$($control.Name), Type=$($control.Type)"
        
        # Check if this is a Card control
        $isCard = $control.Type -eq 'Card'
        
        # First NON-CARD control gets the UIStep attribute
        if ($isFirstNonCardControl -and -not $isCard) {
            $lines += "    [Parameter(Mandatory=`$$($control.Mandatory.ToString().ToLower()))]" 
            $stepAttribute = "[WizardStep('$($Step.Title)', $($Step.Order)"
            if ($Step.Description) {
                $stepAttribute += ", Description='$($Step.Description)'"
            }
            if ($Step.Icon) {
                $stepAttribute += ", IconPath='$($Step.Icon)'"
            }
            $stepAttribute += ")]"
            $lines += "    $stepAttribute"
            $isFirstNonCardControl = $false
        } elseif (-not $isCard) {
            # Subsequent non-card controls
            $lines += "    [Parameter(Mandatory=`$$($control.Mandatory.ToString().ToLower()))]"
            $lines += "    [WizardStep('$($Step.Title)', $($Step.Order))]"
        } else {
            # Card controls
            if ($isFirstNonCardControl -and ($Step.Controls | Where-Object { $_.Type -ne 'Card' }).Count -eq 0) {
                # Step has ONLY cards
                $lines += "    [Parameter(Mandatory=`$false)]"
                $stepAttribute = "[WizardStep('$($Step.Title)', $($Step.Order)"
                if ($Step.Description) {
                    $stepAttribute += ", Description='$($Step.Description)'"
                }
                if ($Step.Icon) {
                    $stepAttribute += ", IconPath='$($Step.Icon)'"
                }
                $stepAttribute += ")]"
                $lines += "    $stepAttribute"
                $isFirstNonCardControl = $false
            } else {
                $lines += "    [Parameter(Mandatory=`$false)]"
                $lines += "    [WizardStep('$($Step.Title)', $($Step.Order))]"
            }
        }
        
        $lines += Convert-UIControl -Control $control
    }
    
    return $lines
}

function Convert-UIControl {
    param([UIControl]$Control)
    
    $lines = @()
    
    # Add parameter details attribute
    $detailsAttribute = "[WizardParameterDetails(Label='$($Control.Label)'"
    if ($Control.Width -gt 0) {
        $detailsAttribute += ", ControlWidth=$($Control.Width)"
    }
    $detailsAttribute += ")]"
    $lines += "    $detailsAttribute"
    
    # Add validation attributes (not for Password - SecureString does not support ValidatePattern)
    if ($Control.ValidationPattern -and $Control.Type -ne 'Password') {
        $lines += "    [ValidatePattern('$($Control.ValidationPattern)')]"
    }
    
    # Add type-specific attributes and parameter declaration
    switch ($Control.Type) {
        'TextBox' {
            $paramType = '[string]'
            
            if ($Control.GetPropertyOrDefault('Multiline', $false)) {
                $rows = $Control.GetPropertyOrDefault('Rows', $null)
                $multiLineArgs = @()
                if ($rows) {
                    $multiLineArgs += "Rows=$rows"
                }
                if ($multiLineArgs.Count -gt 0) {
                    $lines += "    [WizardMultiLine($($multiLineArgs -join ', '))]"
                } else {
                    $lines += "    [WizardMultiLine]"
                }
            } else {
                $maxLength = $Control.GetPropertyOrDefault('MaxLength', $null)
                $placeholder = $Control.GetPropertyOrDefault('Placeholder', $null)
                
                if ($maxLength -or $placeholder) {
                    $textBoxArgs = @()
                    if ($maxLength) {
                        $textBoxArgs += "MaxLength=$maxLength"
                    }
                    if ($placeholder) {
                        $escapedPlaceholder = $placeholder -replace "'", "''"
                        $textBoxArgs += "Placeholder='$escapedPlaceholder'"
                    }
                    $lines += "    [WizardTextBox($($textBoxArgs -join ', '))]"
                }
            }
        }
        'Password' {
            $paramType = '[SecureString]'
            
            $minLength = $Control.GetPropertyOrDefault('MinLength', $null)
            $showReveal = $Control.GetPropertyOrDefault('ShowRevealButton', $null)
            $validationPattern = $Control.ValidationPattern
            $validationScript = $Control.GetPropertyOrDefault('ValidationScript', $null)
            
            if ($minLength -or ($null -ne $showReveal) -or $validationPattern -or $validationScript) {
                $passwordArgs = @()
                if ($minLength) {
                    $passwordArgs += "MinLength=$minLength"
                }
                if ($null -ne $showReveal) {
                    $passwordArgs += "ShowRevealButton=`$$($showReveal.ToString().ToLower())"
                }
                if ($validationPattern) {
                    $escapedPattern = $validationPattern -replace "'", "''"
                    $passwordArgs += "ValidationPattern='$escapedPattern'"
                }
                if ($validationScript) {
                    # Base64 encode to avoid parsing issues with special characters
                    $bytes = [System.Text.Encoding]::UTF8.GetBytes($validationScript)
                    $base64Script = [Convert]::ToBase64String($bytes)
                    $passwordArgs += "ValidationScript='$base64Script'"
                }
                $lines += "    [WizardPassword($($passwordArgs -join ', '))]"
            }
        }
        'Checkbox' {
            $paramType = '[bool]'
            
            $checkedLabel = $Control.GetPropertyOrDefault('CheckedLabel', $null)
            $uncheckedLabel = $Control.GetPropertyOrDefault('UncheckedLabel', $null)
            
            if ($checkedLabel -or $uncheckedLabel) {
                $checkBoxArgs = @()
                if ($checkedLabel) {
                    $escapedChecked = $checkedLabel -replace "'", "''"
                    $checkBoxArgs += "CheckedLabel='$escapedChecked'"
                }
                if ($uncheckedLabel) {
                    $escapedUnchecked = $uncheckedLabel -replace "'", "''"
                    $checkBoxArgs += "UncheckedLabel='$escapedUnchecked'"
                }
                $lines += "    [WizardCheckBox($($checkBoxArgs -join ', '))]"
            }
        }
        'Toggle' {
            $lines += "    [WizardSwitch]"
            $paramType = '[switch]'
        }
        'Dropdown' {
            # Check for dynamic scriptblock first - THIS IS THE KEY FOR AST PARSING
            $isDynamic = $Control.GetPropertyOrDefault('IsDynamic', $false)
            if ($isDynamic) {
                $scriptBlockContent = $Control.GetPropertyOrDefault('DataSourceScriptBlock', $null)
                if ($scriptBlockContent) {
                    # Generate UIDataSource attribute with script block wrapped in braces
                    # This is critical - the braces allow AST parsing to recognize it as a ScriptBlock
                    $dataSourceAttribute = "[UIDataSource({$scriptBlockContent})]"
                    $lines += "    $dataSourceAttribute"
                }
            }
            # Fall back to static choices if no scriptblock
            elseif ($Control.Choices -and $Control.Choices.Count -gt 0) {
                $choicesString = ($Control.Choices | ForEach-Object { "'$_'" }) -join ', '
                $lines += "    [ValidateSet($choicesString)]"
            }
            $paramType = '[string]'
        }
        'ListBox' {
            # Check for dynamic scriptblock first
            $isDynamic = $Control.GetPropertyOrDefault('IsDynamic', $false)
            if ($isDynamic) {
                $scriptBlockContent = $Control.GetPropertyOrDefault('DataSourceScriptBlock', $null)
                if ($scriptBlockContent) {
                    # Generate UIDataSource attribute with script block wrapped in braces
                    $dataSourceAttribute = "[UIDataSource({$scriptBlockContent})]"
                    $lines += "    $dataSourceAttribute"
                }
            }
            # Fall back to static choices if no scriptblock
            elseif ($Control.Choices -and $Control.Choices.Count -gt 0) {
                $choicesString = ($Control.Choices | ForEach-Object { "'$_'" }) -join ', '
                $lines += "    [ValidateSet($choicesString)]"
            }
            $isMultiSelect = $Control.GetPropertyOrDefault('IsMultiSelect', $false)
            if ($isMultiSelect) {
                $paramType = '[string[]]'
                $lines += "    [WizardListBox(MultiSelect=`$true)]"
            } else {
                $paramType = '[string]'
                $lines += "    [WizardListBox()]"
            }
        }
        'Numeric' {
            $paramType = '[double]'
            
            $numericArgs = @()
            $min = $Control.GetPropertyOrDefault('Minimum', $null)
            $max = $Control.GetPropertyOrDefault('Maximum', $null)
            $step = $Control.GetPropertyOrDefault('Step', $null)
            $allowDecimal = [bool]$Control.GetPropertyOrDefault('AllowDecimal', $false)
            
            if ($null -ne $min) {
                $numericArgs += "Minimum=$($min.ToString([System.Globalization.CultureInfo]::InvariantCulture))"
            }
            if ($null -ne $max) {
                $numericArgs += "Maximum=$($max.ToString([System.Globalization.CultureInfo]::InvariantCulture))"
            }
            if ($null -ne $step) {
                $numericArgs += "Step=$($step.ToString([System.Globalization.CultureInfo]::InvariantCulture))"
            }
            if ($allowDecimal) {
                $numericArgs += "AllowDecimal=`$true"
            }
            
            $numericAttribute = if ($numericArgs.Count -gt 0) {
                "[WizardNumeric($($numericArgs -join ', '))]"
            } else {
                "[WizardNumeric]"
            }
            $lines += "    $numericAttribute"
        }
        'Date' {
            $paramType = '[string]'
            
            $dateArgs = @()
            $minDate = $Control.GetPropertyOrDefault('Minimum', $null)
            $maxDate = $Control.GetPropertyOrDefault('Maximum', $null)
            $format = $Control.GetPropertyOrDefault('Format', $null)
            
            if ($minDate) {
                $dateArgs += "Minimum='$(($minDate).ToString('o'))'"
            }
            if ($maxDate) {
                $dateArgs += "Maximum='$(($maxDate).ToString('o'))'"
            }
            if ($format) {
                $escapedFormat = $format -replace "'", "''"
                $dateArgs += "Format='$escapedFormat'"
            }
            
            $dateAttribute = if ($dateArgs.Count -gt 0) {
                "[WizardDate($($dateArgs -join ', '))]"
            } else {
                "[WizardDate]"
            }
            $lines += "    $dateAttribute"
        }
        'OptionGroup' {
            if ($Control.Choices -and $Control.Choices.Count -gt 0) {
                $choicesString = ($Control.Choices | ForEach-Object { "'$_'" }) -join ', '
                $lines += "    [ValidateSet($choicesString)]"
            }
            
            $paramType = '[string]'
            
            $orientation = $Control.GetPropertyOrDefault('Orientation', 'Vertical')
            $arguments = @()
            if ($Control.Choices) {
                $arguments += ($Control.Choices | ForEach-Object { "'$_'" })
            }
            
            $orientationArg = if ($orientation -and $orientation -ne 'Vertical') {
                "Orientation='$orientation'"
            } else {
                $null
            }
            
            $optionAttribute = if ($orientationArg) {
                if ($arguments.Count -gt 0) {
                    "[WizardOptionGroup($($arguments -join ', '), $orientationArg)]"
                } else {
                    "[WizardOptionGroup($orientationArg)]"
                }
            } else {
                if ($arguments.Count -gt 0) {
                    "[WizardOptionGroup($($arguments -join ', '))]"
                } else {
                    "[WizardOptionGroup]"
                }
            }
            
        } else {
                $lines += "    $optionAttribute"
            }
            'Card' {
                $title = $Control.GetPropertyOrDefault('CardTitle', $null)
                $content = $Control.GetPropertyOrDefault('CardContent', $null)
                
                if ($title -and $content) {
                    $escapedTitle = $title -replace "'", "''"
                    $escapedContent = $content -replace "'", "''" -replace "`r`n", '``n' -replace "`n", '``n'
                    $lines += "    [WizardCard('$escapedTitle', '$escapedContent')]"
                }
                $paramType = '[string]'
            }
            'InfoCard' {
                # InfoCard is the type created by Add-UICard
                $title = $Control.GetPropertyOrDefault('CardTitle', $null)
                $content = $Control.GetPropertyOrDefault('CardContent', $null)
                
                $escapedTitle = if ($title) { $title -replace "'", "''" } else { '' }
                $escapedContent = if ($content) { $content -replace "'", "''" -replace "`r`n", '``n' -replace "`n", '``n' } else { '' }
                
                # Build WizardCard with additional properties
                $namedArgs = @()
                $icon = $Control.GetPropertyOrDefault('CardIcon', $null)
                $iconPath = $Control.GetPropertyOrDefault('CardIconPath', $null)
                $imagePath = $Control.GetPropertyOrDefault('CardImagePath', $null)
                $linkUrl = $Control.GetPropertyOrDefault('CardLinkUrl', $null)
                $linkText = $Control.GetPropertyOrDefault('CardLinkText', $null)
                $bgColor = $Control.GetPropertyOrDefault('CardBackgroundColor', $null)
                
                if ($icon) { 
                    # Convert HTML entity &#xE713; to actual Unicode character
                    if ($icon -match '&#x([0-9A-Fa-f]+);') {
                        $hexValue = $matches[1]
                        $unicodeChar = [char]::ConvertFromUtf32([int]"0x$hexValue")
                        $namedArgs += "Icon='$unicodeChar'"
                    } else {
                        $namedArgs += "Icon='$icon'"
                    }
                }
                if ($iconPath) { $namedArgs += "IconPath='$iconPath'" }
                if ($imagePath) { $namedArgs += "ImagePath='$imagePath'" }
                if ($linkUrl) { $namedArgs += "LinkUrl='$linkUrl'" }
                if ($linkText -and $linkText -ne 'Learn more') { $namedArgs += "LinkText='$linkText'" }
                if ($bgColor) { $namedArgs += "BackgroundColor='$bgColor'" }
                
                # Add gradient properties
                $gradientStart = $Control.GetPropertyOrDefault('CardGradientStart', $null)
                $gradientEnd = $Control.GetPropertyOrDefault('CardGradientEnd', $null)
                if ($gradientStart) { $namedArgs += "GradientStart='$gradientStart'" }
                if ($gradientEnd) { $namedArgs += "GradientEnd='$gradientEnd'" }
                
                # Add other styling properties
                $titleColor = $Control.GetPropertyOrDefault('CardTitleColor', $null)
                $contentColor = $Control.GetPropertyOrDefault('CardContentColor', $null)
                $cornerRadius = $Control.GetPropertyOrDefault('CardCornerRadius', $null)
                $imageOpacity = $Control.GetPropertyOrDefault('CardImageOpacity', $null)
                
                if ($titleColor) { $namedArgs += "TitleColor='$titleColor'" }
                if ($contentColor) { $namedArgs += "ContentColor='$contentColor'" }
                if ($cornerRadius) { $namedArgs += "CornerRadius=$cornerRadius" }
                if ($imageOpacity) { $namedArgs += "ImageOpacity=$imageOpacity" }
                
                if ($namedArgs.Count -gt 0) {
                    $lines += "    [WizardCard('$escapedTitle', '$escapedContent', $($namedArgs -join ', '))]"
                } else {
                    $lines += "    [WizardCard('$escapedTitle', '$escapedContent')]"
                }
                $paramType = '[string]'
            }
            'Banner' {
                # Banner controls are serialized as JSON and passed via UIBanner attribute
                $bannerData = @{
                    Title = $Control.GetPropertyOrDefault('BannerTitle', $Control.Label)
                    Subtitle = $Control.GetPropertyOrDefault('BannerSubtitle', $null)
                    Description = $Control.GetPropertyOrDefault('Description', $null)
                    Icon = if ($Control.GetPropertyOrDefault('BannerIcon', $null)) { 
                    $iconValue = $Control.GetPropertyOrDefault('BannerIcon', $null)
                    # Convert HTML entity &#xE713; to actual Unicode character
                    if ($iconValue -match '&#x([0-9A-Fa-f]+);') {
                        $hexValue = $matches[1]
                        $unicodeChar = [char]::ConvertFromUtf32([int]"0x$hexValue")
                        $unicodeChar
                    } else {
                        $iconValue
                    }
                } else { $null }
                    IconPath = $Control.GetPropertyOrDefault('IconPath', $null)
                    IconSize = $Control.GetPropertyOrDefault('IconSize', 64)
                    IconPosition = $Control.GetPropertyOrDefault('IconPosition', 'Right')
                    IconColor = $Control.GetPropertyOrDefault('IconColor', '#40FFFFFF')
                    IconAnimation = $Control.GetPropertyOrDefault('IconAnimation', 'None')
                    Type = $Control.GetPropertyOrDefault('BannerType', 'info')
                    BackgroundColor = $Control.GetPropertyOrDefault('BackgroundColor', $null)
                    BackgroundImagePath = $Control.GetPropertyOrDefault('BackgroundImagePath', $null)
                    BackgroundImageOpacity = $Control.GetPropertyOrDefault('BackgroundImageOpacity', 0.3)
                    BackgroundImageStretch = $Control.GetPropertyOrDefault('BackgroundImageStretch', 'Uniform')
                    GradientStart = $Control.GetPropertyOrDefault('GradientStart', $null)
                    GradientEnd = $Control.GetPropertyOrDefault('GradientEnd', $null)
                    # Overlay Image
                    OverlayImagePath = $Control.GetPropertyOrDefault('OverlayImagePath', $null)
                    OverlayImageOpacity = $Control.GetPropertyOrDefault('OverlayImageOpacity', 0.5)
                    OverlayPosition = $Control.GetPropertyOrDefault('OverlayPosition', 'Right')
                    OverlayImageSize = $Control.GetPropertyOrDefault('OverlayImageSize', 120)
                    Height = $Control.GetPropertyOrDefault('Height', 180)
                    ButtonText = $Control.GetPropertyOrDefault('ButtonText', $null)
                    ButtonIcon = $Control.GetPropertyOrDefault('ButtonIcon', $null)
                    ButtonColor = $Control.GetPropertyOrDefault('ButtonColor', '#0078D4')
                    ProgressValue = $Control.GetPropertyOrDefault('ProgressValue', -1)
                    ProgressLabel = $Control.GetPropertyOrDefault('ProgressLabel', $null)
                    # Enhanced styling properties
                    TitleFontSize = $Control.GetPropertyOrDefault('TitleFontSize', $null)
                    SubtitleFontSize = $Control.GetPropertyOrDefault('SubtitleFontSize', $null)
                    TitleFontWeight = $Control.GetPropertyOrDefault('TitleFontWeight', $null)
                    TitleColor = $Control.GetPropertyOrDefault('TitleColor', $null)
                    SubtitleColor = $Control.GetPropertyOrDefault('SubtitleColor', $null)
                    FontFamily = $Control.GetPropertyOrDefault('FontFamily', $null)
                    CornerRadius = $Control.GetPropertyOrDefault('CornerRadius', $null)
                    GradientAngle = $Control.GetPropertyOrDefault('GradientAngle', $null)
                    LinkUrl = $Control.GetPropertyOrDefault('LinkUrl', $null)
                    Clickable = $Control.GetPropertyOrDefault('Clickable', $false)
                    # Carousel properties
                    CarouselItems = $Control.GetPropertyOrDefault('CarouselItems', $null)
                    AutoRotate = $Control.GetPropertyOrDefault('AutoRotate', $false)
                    RotateInterval = $Control.GetPropertyOrDefault('RotateInterval', 3000)
                    NavigationStyle = $Control.GetPropertyOrDefault('NavigationStyle', 'Dots')
                }
                
                # Convert to JSON, then Base64 encode for safe attribute passing
                $bannerJson = $bannerData | ConvertTo-Json -Compress -Depth 10
                $bannerBytes = [System.Text.Encoding]::UTF8.GetBytes($bannerJson)
                $bannerBase64 = [Convert]::ToBase64String($bannerBytes)
                
                $lines += "    [UIBanner('BASE64:$bannerBase64')]"
                $paramType = '[string]'
            }
            'FilePath' {
                $pathProps = @()
                $filter = $Control.GetPropertyOrDefault('Filter', $null)
                $dialogTitle = $Control.GetPropertyOrDefault('DialogTitle', $null)
                
                if ($filter) {
                    $pathProps += "Filter='$filter'"
                }
                if ($dialogTitle) {
                    $escapedTitle = $dialogTitle -replace "'", "''"
                    $pathProps += "DialogTitle='$escapedTitle'"
                }
                
                if ($pathProps.Count -gt 0) {
                    $lines += "    [WizardFilePath($($pathProps -join ', '))]"
                } else {
                    $lines += "    [WizardPathSelector('File')]"
                }
                $paramType = '[string]'
            }
            'FolderPath' {
                $lines += "    [WizardPathSelector('Folder')]"
                $paramType = '[string]'
            }
            default {
                $paramType = '[string]'
            }
        }
        
        # Add parameter declaration with proper default value formatting
        $paramDeclaration = "    $paramType`$$($Control.Name)"
        if ($null -ne $Control.Default -and $Control.Default -ne '') {
            # Format default value based on type
            if ($Control.Type -in @('Checkbox', 'Toggle')) {
                $paramDeclaration += " = `$$($Control.Default)"
            }
            elseif ($Control.Type -eq 'Password') {
                # SecureString types do not have defaults
            }
            elseif ($Control.Type -eq 'Numeric') {
                $paramDeclaration += " = $($Control.Default.ToString([System.Globalization.CultureInfo]::InvariantCulture))"
            }
            elseif ($Control.Type -eq 'Date') {
                if ($Control.Default -is [DateTime]) {
                    $dateLiteral = $Control.Default.ToString('yyyy-MM-dd')
                    $paramDeclaration += " = '$dateLiteral'"
                $paramDeclaration += " = '$($Control.Default)'"
            }
        }
        elseif ($Control.Type -eq 'ListBox' -and $paramType -eq '[string[]]') {
            if ($Control.Default -is [array]) {
                $defaultArray = ($Control.Default | ForEach-Object { "'$_'" }) -join ', '
                $paramDeclaration += " = @($defaultArray)"
            } else {
                $paramDeclaration += " = @('$($Control.Default)')"
            }
        }
        elseif ($Control.Type -eq 'Card') {
            # Cards do not have default values
        }
        else {
            $paramDeclaration += " = '$($Control.Default)'"
        }
    }
    $paramDeclaration += ","
    
    $lines += $paramDeclaration
    
    return $lines
}
