function New-PoshUIDashboard {
    <#
    .SYNOPSIS
    Initializes a new PoshUI Dashboard definition.

    .DESCRIPTION
    Creates a new Dashboard UI context that can be populated with steps and visualization cards.
    This function must be called before adding any steps or cards to the UI.
    
    .PARAMETER Title
    The title of the UI that will be displayed in the window title bar.
    
    .PARAMETER Description
    Optional description of the UI's purpose.
    
    .PARAMETER Icon
    Optional path to an icon file (PNG, ICO) to display in the UI.
    Can also be a Segoe MDL2 icon glyph in the format '&#xE1D3;' (e.g., Database icon).
    
    .PARAMETER SidebarHeaderText
    Optional text to display in the sidebar header for branding.
    
    .PARAMETER SidebarHeaderIcon
    Optional icon for the sidebar header. Can be a file path or Segoe MDL2 glyph (e.g., '&#xE1D3;').

    .PARAMETER SidebarHeaderIconOrientation
    Optional orientation for the sidebar icon relative to the text. Supported values: Left (default), Right, Top, Bottom.
    
    .PARAMETER Theme
    The theme to use for the UI. Valid values are 'Light', 'Dark', or 'Auto'.
    Default is 'Auto' which follows the system theme.

    .PARAMETER AllowCancel
    Whether to allow users to cancel the UI. Default is $true.
    
    .PARAMETER GridColumns
    Number of columns for Dashboard grid layout. Default is 3. Valid range: 1-6.

    .PARAMETER LogPath
    Optional path to a custom log file for dashboard execution logging.
    
    .EXAMPLE
    New-PoshUIDashboard -Title "System Dashboard"

    Creates a new Dashboard UI with the specified title.

    .EXAMPLE
    New-PoshUIDashboard -Title "Sales Dashboard" -Description "Real-time sales metrics" -GridColumns 4

    Creates a new Dashboard UI with title, description, and 4-column grid layout.
    
    .OUTPUTS
    UIDefinition object representing the initialized Dashboard UI.

    .NOTES
    This function initializes the module-level $script:CurrentWizard variable that is used by other PoshUI functions.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string]$Title,
        
        [Parameter()]
        [string]$Description,
        
        [Parameter()]
        [string]$Icon,
        
        [Parameter()]
        [string]$SidebarHeaderText,
        
        [Parameter()]
        [string]$WindowTitleIcon,
        
        [Parameter()]
        [string]$SidebarHeaderIcon,
        
        [Parameter()]
        [ValidateSet('Left', 'Right', 'Top', 'Bottom')]
        [string]$SidebarHeaderIconOrientation = 'Left',
        
        [Parameter()]
        [ValidateSet('Light', 'Dark', 'Auto')]
        [string]$Theme = 'Auto',
        
        [Parameter()]
        [bool]$AllowCancel = $true,
        
        [Parameter()]
        [ValidateRange(1, 6)]
        [int]$GridColumns = 3,
        
        [Parameter()]
        [string]$LogPath
    )
    
    begin {
        Write-Verbose "Creating new PoshUI Dashboard: $Title"
    }
    
    process {
        try {
            # Create new UI definition (hardcoded to Dashboard template)
            $wizard = [UIDefinition]::new($Title)
            $wizard.Description = $Description
            $wizard.Icon = $Icon
            $wizard.SidebarHeaderText = $SidebarHeaderText
            $wizard.WindowTitleIcon = $WindowTitleIcon
            $wizard.SidebarHeaderIcon = $SidebarHeaderIcon
            $wizard.SidebarHeaderIconOrientation = $SidebarHeaderIconOrientation
            $wizard.Theme = $Theme
            $wizard.AllowCancel = $AllowCancel
            $wizard.ViewMode = 'Dashboard'
            $wizard.Template = 'Dashboard'
            $wizard.GridColumns = $GridColumns

            # Store custom log path in Variables if specified
            if ($LogPath) {
                $wizard.Variables['_LogPath'] = $LogPath
            }

            # Store as current wizard for other functions to use
            $script:CurrentWizard = $wizard

            Write-Verbose "Successfully created Dashboard UI: $($wizard.ToString())"

            # Return the UI object to support method chaining
            return $wizard
        }
        catch {
            Write-Error "Failed to create Dashboard UI: $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "New-PoshUIDashboard completed"
    }
}
