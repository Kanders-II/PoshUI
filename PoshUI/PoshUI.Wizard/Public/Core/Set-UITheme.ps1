function Set-UITheme {
    <#
    .SYNOPSIS
    Applies a custom color theme to the UI using a simple PowerShell hashtable.

    .DESCRIPTION
    Configures the UI color scheme by accepting a hashtable of named color slots.
    No XAML knowledge required - just provide hex color values for the slots you want to override.
    Any slots not specified will use the base theme defaults (Light or Dark).

    Supports three usage patterns:
    1. Single theme: Set-UITheme @{ AccentColor = '#FF6B35' }
       Applied to both light and dark modes. Theme toggle re-applies these overrides.
    2. Dual themes: Set-UITheme -Light @{...} -Dark @{...}
       Separate overrides for each mode. Theme toggle switches between them.
    3. Mixed: Set-UITheme @{ AccentColor = '#FF6B35' } -Light @{ Background = '#FFF' } -Dark @{ Background = '#111' }
       Shared base + mode-specific overrides (mode-specific keys win on conflict).

    Must be called after New-PoshUIWizard (or New-PoshUIDashboard, New-PoshUIWorkflow, New-PoshUIFreeform).
    Works with all PoshUI modules: Wizard, Dashboard, Workflow, and Freeform.

    .PARAMETER Theme
    A hashtable of color slot overrides applied to BOTH light and dark modes.
    When used alone, the theme toggle will re-apply these overrides after switching the base theme.
    
    Available color slots (all optional):
    
    ACCENT COLORS:
      AccentColor       - Your brand/accent color (e.g., '#FF6B35')
      AccentDark        - Hover state (auto-derived if omitted)
      AccentDarker      - Pressed state (auto-derived if omitted)
      AccentLight       - Light variant (auto-derived if omitted)
    
    BACKGROUNDS:
      Background        - Window/app background
      ContentBackground - Main content area background
      CardBackground    - Card and panel surfaces
    
    SIDEBAR:
      SidebarBackground - Navigation sidebar background
      SidebarText       - Sidebar text color
      SidebarHighlight  - Active sidebar item color (defaults to AccentColor)
    
    TEXT:
      TextPrimary       - Headings and body text
      TextSecondary     - Muted/secondary text
    
    BUTTONS:
      ButtonBackground  - Primary button background (defaults to AccentColor)
      ButtonForeground  - Primary button text color
    
    INPUTS:
      InputBackground   - Text input field background
      InputBorder       - Input focus border color (defaults to AccentColor)
    
    BORDERS:
      BorderColor       - General border color
    
    TITLE BAR:
      TitleBarBackground - Window title bar background
      TitleBarText       - Window title bar text
    
    SEMANTIC:
      SuccessColor      - Green status indicator
      WarningColor      - Amber warning indicator
      ErrorColor        - Red error indicator
    
    TYPOGRAPHY:
      FontFamily        - Global font family name (e.g., 'Segoe UI')
      CornerRadius      - Control corner radius in pixels (e.g., 8)

    .PARAMETER Light
    A hashtable of color slot overrides applied ONLY in light mode.
    These are merged on top of the base -Theme hashtable (if provided).

    .PARAMETER Dark
    A hashtable of color slot overrides applied ONLY in dark mode.
    These are merged on top of the base -Theme hashtable (if provided).

    .EXAMPLE
    Set-UITheme @{ AccentColor = '#FF6B35' }

    Changes only the accent color to orange. Applied to both light and dark modes.
    The theme toggle button will re-apply these overrides after switching.

    .EXAMPLE
    Set-UITheme -Light @{
        AccentColor       = '#0078D4'
        Background        = '#F5F5F5'
        TextPrimary       = '#1A1A2E'
    } -Dark @{
        AccentColor       = '#4DA3E8'
        Background        = '#1A1A2E'
        TextPrimary       = '#E0E0E0'
    }

    Separate light and dark themes. The toggle switches between them.

    .EXAMPLE
    Set-UITheme @{ AccentColor = '#FF6B35'; FontFamily = 'Cascadia Code' } -Dark @{
        Background        = '#1A1210'
        ContentBackground = '#231C18'
        TextPrimary       = '#F5EDE8'
    }

    Shared accent color and font for both modes, with dark-specific backgrounds.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [hashtable]$Theme,

        [Parameter()]
        [hashtable]$Light,

        [Parameter()]
        [hashtable]$Dark
    )

    begin {
        Write-Verbose "Applying UI theme overrides"

        # Ensure UI is initialized
        if (-not $script:CurrentWizard) {
            throw "No UI initialized. Call New-PoshUIWizard first."
        }
    }

    process {
        try {
            if (-not $Theme -and -not $Light -and -not $Dark) {
                Write-Warning "No theme parameters provided. Use -Theme, -Light, -Dark, or a combination."
                return
            }

            # Validate color values
            $validSlots = @(
                'AccentColor', 'AccentDark', 'AccentDarker', 'AccentLight',
                'Background', 'ContentBackground', 'CardBackground',
                'SidebarBackground', 'SidebarText', 'SidebarHighlight',
                'TextPrimary', 'TextSecondary',
                'ButtonBackground', 'ButtonForeground',
                'InputBackground', 'InputBorder',
                'BorderColor',
                'TitleBarBackground', 'TitleBarText',
                'SuccessColor', 'WarningColor', 'ErrorColor',
                'FontFamily', 'CornerRadius'
            )

            $nonColorSlots = @('FontFamily', 'CornerRadius')
            $hexPattern = '^#?([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$'

            # Helper: validate and normalize a theme hashtable
            $validateTheme = {
                param([hashtable]$Source, [string]$Label)
                $result = @{}
                foreach ($key in $Source.Keys) {
                    $value = $Source[$key]
                    if ($key -notin $validSlots) {
                        Write-Warning "[$Label] Unknown theme slot '$key'. Valid slots: $($validSlots -join ', ')"
                        continue
                    }
                    if ($key -notin $nonColorSlots) {
                        $strValue = [string]$value
                        if ($strValue -notmatch $hexPattern) {
                            Write-Error "[$Label] '$key' value '$strValue' is not a valid hex color. Use format '#RRGGBB' or '#AARRGGBB'."
                            continue
                        }
                        if (-not $strValue.StartsWith('#')) {
                            $strValue = '#' + $strValue
                        }
                        $result[$key] = $strValue
                    }
                    else {
                        $result[$key] = [string]$value
                    }
                    Write-Verbose "  [$Label] $key = $($result[$key])"
                }
                return $result
            }

            # Validate each provided hashtable
            $baseOverrides = @{}
            $lightOverrides = @{}
            $darkOverrides = @{}

            if ($Theme) {
                $baseOverrides = & $validateTheme $Theme 'Theme'
            }
            if ($Light) {
                $lightOverrides = & $validateTheme $Light 'Light'
            }
            if ($Dark) {
                $darkOverrides = & $validateTheme $Dark 'Dark'
            }

            $totalSlots = $baseOverrides.Count + $lightOverrides.Count + $darkOverrides.Count
            if ($totalSlots -eq 0) {
                Write-Warning "No valid theme overrides provided."
                return
            }

            $hasDualMode = ($lightOverrides.Count -gt 0) -or ($darkOverrides.Count -gt 0)

            if ($hasDualMode) {
                # Dual-mode: merge base into light and dark, then store all three
                # Base overrides are shared - mode-specific keys take priority
                $mergedLight = @{}
                $mergedDark = @{}

                # Start with base
                foreach ($key in $baseOverrides.Keys) {
                    $mergedLight[$key] = $baseOverrides[$key]
                    $mergedDark[$key] = $baseOverrides[$key]
                }

                # Layer mode-specific on top (overrides base on conflict)
                foreach ($key in $lightOverrides.Keys) {
                    $mergedLight[$key] = $lightOverrides[$key]
                }
                foreach ($key in $darkOverrides.Keys) {
                    $mergedDark[$key] = $darkOverrides[$key]
                }

                # Store in branding for C# launcher
                # ThemeOverrides = base (for backward compat / initial apply)
                # ThemeOverridesLight = merged light overrides
                # ThemeOverridesDark = merged dark overrides
                $script:CurrentWizard.Branding['ThemeOverrides'] = $baseOverrides
                $script:CurrentWizard.Branding['ThemeOverridesLight'] = $mergedLight
                $script:CurrentWizard.Branding['ThemeOverridesDark'] = $mergedDark

                Write-Verbose "Dual-mode theme configured: Base=$($baseOverrides.Count), Light=$($mergedLight.Count), Dark=$($mergedDark.Count) slots"
            }
            else {
                # Single-mode: store base overrides only (backward compatible)
                $script:CurrentWizard.Branding['ThemeOverrides'] = $baseOverrides
                # Clear any previous dual-mode entries
                $script:CurrentWizard.Branding.Remove('ThemeOverridesLight')
                $script:CurrentWizard.Branding.Remove('ThemeOverridesDark')

                Write-Verbose "Theme configured with $($baseOverrides.Count) override(s)"
            }
        }
        catch {
            Write-Error "Failed to configure theme: $($_.Exception.Message)"
            throw
        }
    }
}
