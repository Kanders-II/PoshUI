function Set-UITheme {
    <#
    .SYNOPSIS
    Applies a colour/typography theme to the current canvas (slot -> hex). Supports single, or
    per-mode -Light/-Dark overrides. Call after New-PoshUICanvas.
    .EXAMPLE
    Set-UITheme @{ AccentColor = '#6366F1'; Background = '#0A0E1A' }
    #>
    [CmdletBinding(DefaultParameterSetName = 'Overrides')]
    param(
        [Parameter(Position = 0, ParameterSetName = 'Overrides')][hashtable]$Theme,
        [Parameter(ParameterSetName = 'Overrides')][hashtable]$Light,
        [Parameter(ParameterSetName = 'Overrides')][hashtable]$Dark,
        # One-call palette presets; -Accent recolors just the accent. Explicit -Theme slots still win.
        [Parameter(ParameterSetName = 'Overrides')][ValidateSet('Dark', 'Light', 'Midnight', 'Slate')][string]$Preset,
        [Parameter(ParameterSetName = 'Overrides')][ValidateSet('Indigo', 'Emerald', 'Sky', 'Amber', 'Rose', 'Violet', 'Cyan', 'Orange')][string]$Accent,
        [Parameter(ParameterSetName = 'Overrides')][Parameter(ParameterSetName = 'ModeOnly')]
        [ValidateSet('Light', 'Dark', 'Auto')][string]$Mode
    )

    if (-not $Global:_PoshUICanvas) { throw "No canvas active. Call New-PoshUICanvas first." }
    $branding = $Global:_PoshUICanvas.Definition.Branding

    if ($Mode) { $branding.Theme = $Mode }

    # Curated full palettes (slot -> hex). Each preset is a complete look; -Theme overrides win on top.
    $presets = @{
        Dark     = @{ AccentColor = '#6366F1'; Background = '#0A0E1A'; ContentBackground = '#0A0E1A'; CardBackground = '#141A2E'; TitleBarBackground = '#0A0E1A'; TitleBarText = '#E5E9F5'; BorderColor = '#243049'; TextPrimary = '#E5E9F5'; TextSecondary = '#94A3B8'; InputBackground = '#0E1424'; InputBorder = '#6366F1'; SidebarBackground = '#0E1424'; SidebarText = '#C7D2FE' }
        Midnight = @{ AccentColor = '#818CF8'; Background = '#070A14'; ContentBackground = '#070A14'; CardBackground = '#0F1525'; TitleBarBackground = '#070A14'; TitleBarText = '#E5E9F5'; BorderColor = '#1B2741'; TextPrimary = '#E5E9F5'; TextSecondary = '#8593AE'; InputBackground = '#0B1020'; InputBorder = '#818CF8'; SidebarBackground = '#0B1020'; SidebarText = '#C7D2FE' }
        Slate    = @{ AccentColor = '#38BDF8'; Background = '#0F172A'; ContentBackground = '#0F172A'; CardBackground = '#1E293B'; TitleBarBackground = '#0F172A'; TitleBarText = '#E2E8F0'; BorderColor = '#334155'; TextPrimary = '#E2E8F0'; TextSecondary = '#94A3B8'; InputBackground = '#16223A'; InputBorder = '#38BDF8'; SidebarBackground = '#16223A'; SidebarText = '#CBD5E1' }
        Light    = @{ AccentColor = '#6366F1'; Background = '#F4F6FB'; ContentBackground = '#F4F6FB'; CardBackground = '#FFFFFF'; TitleBarBackground = '#FFFFFF'; TitleBarText = '#1E2433'; BorderColor = '#E2E8F0'; TextPrimary = '#1E2433'; TextSecondary = '#64748B'; InputBackground = '#FFFFFF'; InputBorder = '#6366F1'; SidebarBackground = '#FFFFFF'; SidebarText = '#334155' }
    }
    $accents = @{
        Indigo  = @{ AccentColor = '#6366F1'; AccentLight = '#818CF8'; AccentDark = '#4F46E5' }
        Emerald = @{ AccentColor = '#34D399'; AccentLight = '#6EE7B7'; AccentDark = '#059669' }
        Sky     = @{ AccentColor = '#38BDF8'; AccentLight = '#7DD3FC'; AccentDark = '#0284C7' }
        Amber   = @{ AccentColor = '#FBBF24'; AccentLight = '#FCD34D'; AccentDark = '#D97706' }
        Rose    = @{ AccentColor = '#F472B6'; AccentLight = '#F9A8D4'; AccentDark = '#DB2777' }
        Violet  = @{ AccentColor = '#A78BFA'; AccentLight = '#C4B5FD'; AccentDark = '#7C3AED' }
        Cyan    = @{ AccentColor = '#22D3EE'; AccentLight = '#67E8F9'; AccentDark = '#0891B2' }
        Orange  = @{ AccentColor = '#FB923C'; AccentLight = '#FDBA74'; AccentDark = '#EA580C' }
    }

    # Compose preset -> accent -> explicit Theme (later wins). Presets also set the base Mode.
    $composed = @{}
    if ($Preset) {
        foreach ($k in $presets[$Preset].Keys) { $composed[$k] = $presets[$Preset][$k] }
        if (-not $Mode) { $branding.Theme = ($(if ($Preset -eq 'Light') { 'Light' } else { 'Dark' })) }
    }
    if ($Accent) { foreach ($k in $accents[$Accent].Keys) { $composed[$k] = $accents[$Accent][$k] } }
    if ($Theme) { foreach ($k in $Theme.Keys) { $composed[$k] = $Theme[$k] } }
    if ($composed.Count) { $Theme = $composed }

    $hex = '^#?([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$'
    $nonColor = @('FontFamily', 'CornerRadius')
    $clean = {
        param([hashtable]$src)
        $r = @{}
        if (-not $src) { return $r }
        foreach ($k in $src.Keys) {
            $v = [string]$src[$k]
            if ($k -notin $nonColor) {
                if ($v -notmatch $hex) { Write-Warning "Theme slot '$k' value '$v' is not a hex colour."; continue }
                if (-not $v.StartsWith('#')) { $v = '#' + $v }
            }
            $r[$k] = $v
        }
        $r
    }

    $base = & $clean $Theme
    $lt   = & $clean $Light
    $dk   = & $clean $Dark

    if ($base.Count) { $branding.ThemeOverrides = $base }
    if ($lt.Count -or $dk.Count) {
        $mergedLight = @{}; $mergedDark = @{}
        foreach ($k in $base.Keys) { $mergedLight[$k] = $base[$k]; $mergedDark[$k] = $base[$k] }
        foreach ($k in $lt.Keys)   { $mergedLight[$k] = $lt[$k] }
        foreach ($k in $dk.Keys)   { $mergedDark[$k]  = $dk[$k] }
        if ($lt.Count) { $branding.ThemeOverridesLight = $mergedLight }
        if ($dk.Count) { $branding.ThemeOverridesDark  = $mergedDark }
    }
}
