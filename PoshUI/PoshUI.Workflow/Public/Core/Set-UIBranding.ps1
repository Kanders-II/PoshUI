function Set-UIBranding {
    <#
    .SYNOPSIS
    Configures branding and appearance settings for the UI.

    .DESCRIPTION
    Sets visual customization options including window title, sidebar header, icons, and theme.
    Must be called after New-PoshUI.
    
    .PARAMETER WindowTitle
    The title displayed in the window title bar.
    
    .PARAMETER WindowTitleIcon
    Path to an image file (PNG, ICO, etc.) to display in the window title bar.
    
    .PARAMETER SidebarHeaderText
    Text displayed in the sidebar header area.
    
    .PARAMETER SidebarHeaderIcon
    Segoe MDL2 icon glyph for the sidebar header (e.g., '&#xE8BC;').
    
    .PARAMETER SidebarHeaderIconOrientation
    Position of the sidebar icon relative to text: 'Left', 'Right', 'Top', or 'Bottom'.
    
    .PARAMETER ShowSidebarHeaderIcon
    Whether to display the sidebar header icon.
    
    .PARAMETER Theme
    Visual theme: 'Light', 'Dark', or 'Auto' (system default).
    
    .PARAMETER AllowCancel
    Whether users can cancel the UI (default: $true).

    .EXAMPLE
    Set-UIBranding -WindowTitle "Server Setup" -SidebarHeaderText "Company Name" -SidebarHeaderIcon "&#xE8BC;"

    Sets basic branding with custom title and sidebar.

    .EXAMPLE
    Set-UIBranding -WindowTitle "Deployment Wizard" -Theme "Dark" -AllowCancel $false

    Sets dark theme and prevents cancellation.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$WindowTitle,
        
        [Parameter()]
        [string]$WindowTitleIcon,
        
        [Parameter()]
        [string]$SidebarHeaderText,
        
        [Parameter()]
        [string]$SidebarHeaderIcon,
        
        [Parameter()]
        [ValidateSet('Left', 'Right', 'Top', 'Bottom')]
        [string]$SidebarHeaderIconOrientation = 'Left',
        
        [Parameter()]
        [bool]$ShowSidebarHeaderIcon = $true,
        
        [Parameter()]
        [ValidateSet('Light', 'Dark', 'Auto')]
        [string]$Theme = 'Auto',
        
        [Parameter()]
        [bool]$AllowCancel = $true
    )
    
    begin {
        Write-Verbose "Configuring UI branding"

        # Ensure UI is initialized
        if (-not $script:CurrentWorkflow) {
            throw "No UI initialized. Call New-PoshUI first."
        }
    }

    process {
        try {
            # Update branding properties
            if ($PSBoundParameters.ContainsKey('WindowTitle')) {
                $script:CurrentWorkflow.Branding['WindowTitleText'] = $WindowTitle
            }

            if ($PSBoundParameters.ContainsKey('WindowTitleIcon')) {
                $script:CurrentWorkflow.Branding['WindowTitleIcon'] = $WindowTitleIcon
            }

            if ($PSBoundParameters.ContainsKey('SidebarHeaderText')) {
                $script:CurrentWorkflow.Branding['SidebarHeaderText'] = $SidebarHeaderText
            }

            if ($PSBoundParameters.ContainsKey('SidebarHeaderIcon')) {
                $script:CurrentWorkflow.Branding['SidebarHeaderIconPath'] = $SidebarHeaderIcon
                $script:CurrentWorkflow.SidebarHeaderIcon = $SidebarHeaderIcon
            }

            if ($PSBoundParameters.ContainsKey('SidebarHeaderIconOrientation')) {
                $normalizedOrientation = switch (($SidebarHeaderIconOrientation -as [string]).ToLowerInvariant()) {
                    'right' { 'Right' }
                    'top'   { 'Top' }
                    'bottom' { 'Bottom' }
                    default { 'Left' }
                }
                $script:CurrentWorkflow.Branding['SidebarHeaderIconOrientation'] = $normalizedOrientation
                $script:CurrentWorkflow.SidebarHeaderIconOrientation = $normalizedOrientation
            }

            if ($PSBoundParameters.ContainsKey('ShowSidebarHeaderIcon')) {
                $script:CurrentWorkflow.Branding['ShowSidebarHeaderIcon'] = $ShowSidebarHeaderIcon
            }

            if ($PSBoundParameters.ContainsKey('Theme')) {
                $script:CurrentWorkflow.Theme = $Theme
            }

            if ($PSBoundParameters.ContainsKey('AllowCancel')) {
                $script:CurrentWorkflow.AllowCancel = $AllowCancel
            }

            Write-Verbose "Branding configured successfully"
        }
        catch {
            Write-Error "Failed to configure branding: $($_.Exception.Message)"
            throw
        }
    }
}

# Backward compatibility alias
Set-Alias -Name 'Set-WizardBranding' -Value 'Set-UIBranding'

