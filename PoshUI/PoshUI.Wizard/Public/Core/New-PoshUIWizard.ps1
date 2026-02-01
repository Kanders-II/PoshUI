function New-PoshUIWizard {
    <#
    .SYNOPSIS
    Initializes a new PoshUI Wizard definition.

    .DESCRIPTION
    Creates a new Wizard UI context that can be populated with steps and input controls.
    This function must be called before adding any steps or controls to the UI.
    
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

    .PARAMETER LogPath
    Optional path to a custom log file for wizard execution logging.
    
    .EXAMPLE
    New-PoshUIWizard -Title "Server Configuration Wizard"

    Creates a new Wizard UI with the specified title.

    .EXAMPLE
    New-PoshUIWizard -Title "Database Setup" -Description "Configure database connection settings" -Theme Dark

    Creates a new Wizard UI with title, description, and dark theme.
    
    .OUTPUTS
    UIDefinition object representing the initialized Wizard UI.

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
        [string]$LogPath
    )
    
    begin {
        Write-Verbose "Creating new PoshUI Wizard: $Title"
    }
    
    process {
        try {
            # Create new UI definition (hardcoded to Wizard template)
            $wizard = [UIDefinition]::new($Title)
            $wizard.Description = $Description
            $wizard.Icon = $Icon
            $wizard.SidebarHeaderText = $SidebarHeaderText
            $wizard.WindowTitleIcon = $WindowTitleIcon
            $wizard.SidebarHeaderIcon = $SidebarHeaderIcon
            $wizard.SidebarHeaderIconOrientation = $SidebarHeaderIconOrientation
            $wizard.Theme = $Theme
            $wizard.AllowCancel = $AllowCancel
            $wizard.ViewMode = 'Wizard'
            $wizard.Template = 'Wizard'
            $wizard.GridColumns = 3

            # Store custom log path in Variables if specified
            if ($LogPath) {
                $wizard.Variables['_LogPath'] = $LogPath
            }

            # Store as current wizard for other functions to use
            $script:CurrentWizard = $wizard

            Write-Verbose "Successfully created Wizard UI: $($wizard.ToString())"

            # Return the UI object to support method chaining
            return $wizard
        }
        catch {
            Write-Error "Failed to create Wizard UI: $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "New-PoshUIWizard completed"
    }
}
