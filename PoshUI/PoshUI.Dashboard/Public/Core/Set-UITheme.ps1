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
    2. Dual themes: Set-UITheme -Light @{...} -Dark @{...}
    3. Mixed: Set-UITheme @{ AccentColor = '#FF6B35' } -Light @{...} -Dark @{...}

    Must be called after New-PoshUIDashboard.
    See Set-UITheme in PoshUI.Wizard for full slot documentation.

    .PARAMETER Theme
    A hashtable of color slot overrides applied to BOTH light and dark modes.

    .PARAMETER Light
    A hashtable of color slot overrides applied ONLY in light mode.

    .PARAMETER Dark
    A hashtable of color slot overrides applied ONLY in dark mode.

    .EXAMPLE
    Set-UITheme @{ AccentColor = '#FF6B35' }

    .EXAMPLE
    Set-UITheme -Light @{ Background = '#F5F5F5' } -Dark @{ Background = '#1A1A2E' }
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

        if (-not $script:CurrentWizard) {
            throw "No UI initialized. Call New-PoshUIDashboard first."
        }
    }

    process {
        try {
            if (-not $Theme -and -not $Light -and -not $Dark) {
                Write-Warning "No theme parameters provided. Use -Theme, -Light, -Dark, or a combination."
                return
            }

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

            $validateTheme = {
                param([hashtable]$Source, [string]$Label)
                $result = @{}
                foreach ($key in $Source.Keys) {
                    $value = $Source[$key]
                    if ($key -notin $validSlots) {
                        Write-Warning "[$Label] Unknown theme slot '$key'."
                        continue
                    }
                    if ($key -notin $nonColorSlots) {
                        $strValue = [string]$value
                        if ($strValue -notmatch $hexPattern) {
                            Write-Error "[$Label] '$key' value '$strValue' is not a valid hex color."
                            continue
                        }
                        if (-not $strValue.StartsWith('#')) { $strValue = '#' + $strValue }
                        $result[$key] = $strValue
                    }
                    else { $result[$key] = [string]$value }
                    Write-Verbose "  [$Label] $key = $($result[$key])"
                }
                return $result
            }

            $baseOverrides = @{}
            $lightOverrides = @{}
            $darkOverrides = @{}

            if ($Theme) { $baseOverrides = & $validateTheme $Theme 'Theme' }
            if ($Light) { $lightOverrides = & $validateTheme $Light 'Light' }
            if ($Dark) { $darkOverrides = & $validateTheme $Dark 'Dark' }

            $totalSlots = $baseOverrides.Count + $lightOverrides.Count + $darkOverrides.Count
            if ($totalSlots -eq 0) {
                Write-Warning "No valid theme overrides provided."
                return
            }

            $hasDualMode = ($lightOverrides.Count -gt 0) -or ($darkOverrides.Count -gt 0)

            if ($hasDualMode) {
                $mergedLight = @{}
                $mergedDark = @{}
                foreach ($key in $baseOverrides.Keys) {
                    $mergedLight[$key] = $baseOverrides[$key]
                    $mergedDark[$key] = $baseOverrides[$key]
                }
                foreach ($key in $lightOverrides.Keys) { $mergedLight[$key] = $lightOverrides[$key] }
                foreach ($key in $darkOverrides.Keys) { $mergedDark[$key] = $darkOverrides[$key] }

                $script:CurrentWizard.Branding['ThemeOverrides'] = $baseOverrides
                $script:CurrentWizard.Branding['ThemeOverridesLight'] = $mergedLight
                $script:CurrentWizard.Branding['ThemeOverridesDark'] = $mergedDark
                Write-Verbose "Dual-mode theme configured: Base=$($baseOverrides.Count), Light=$($mergedLight.Count), Dark=$($mergedDark.Count)"
            }
            else {
                $script:CurrentWizard.Branding['ThemeOverrides'] = $baseOverrides
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
