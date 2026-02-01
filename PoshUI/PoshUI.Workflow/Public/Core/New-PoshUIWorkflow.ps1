function New-PoshUIWorkflow {
    <#
    .SYNOPSIS
    Initializes a new PoshUI Workflow definition.

    .DESCRIPTION
    Creates a new Workflow UI context that can be populated with steps and workflow tasks.
    This function must be called before adding any steps or tasks to the UI.
    
    .PARAMETER Title
    The title of the UI that will be displayed in the window title bar.
    
    .PARAMETER Description
    Optional description of the UI's purpose.
    
    .PARAMETER Icon
    Optional path to an icon file (PNG, ICO) to display in the UI.
    Can also be a Segoe MDL2 icon glyph in the format '&#xE1D3;'.
    
    .PARAMETER SidebarHeaderText
    Optional text to display in the sidebar header for branding.
    
    .PARAMETER SidebarHeaderIcon
    Optional icon for the sidebar header. Can be a file path or Segoe MDL2 glyph.

    .PARAMETER SidebarHeaderIconOrientation
    Optional orientation for the sidebar icon relative to the text. Supported values: Left (default), Right, Top, Bottom.
    
    .PARAMETER Theme
    The theme to use for the UI. Valid values are 'Light', 'Dark', or 'Auto'.
    Default is 'Auto' which follows the system theme.

    .PARAMETER AllowCancel
    Whether to allow users to cancel the UI. Default is $true.
    
    .EXAMPLE
    New-PoshUIWorkflow -Title "Server Deployment Workflow"

    Creates a new Workflow UI with the specified title.

    .EXAMPLE
    New-PoshUIWorkflow -Title "System Setup" -Description "Automated system configuration" -Theme Dark

    Creates a new Workflow UI with title, description, and dark theme.
    
    .OUTPUTS
    UIDefinition object representing the initialized Workflow UI.

    .NOTES
    This function initializes the module-level $script:CurrentWorkflow variable that is used by other PoshUI functions.
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
        Write-Verbose "Creating new PoshUI Workflow: $Title"
    }
    
    process {
        try {
            # Create new UI definition (hardcoded to Workflow template)
            $workflow = [UIDefinition]::new($Title)
            $workflow.Description = $Description
            $workflow.Icon = $Icon
            $workflow.SidebarHeaderText = $SidebarHeaderText
            $workflow.WindowTitleIcon = $WindowTitleIcon
            $workflow.SidebarHeaderIcon = $SidebarHeaderIcon
            $workflow.SidebarHeaderIconOrientation = $SidebarHeaderIconOrientation
            $workflow.Theme = $Theme
            $workflow.AllowCancel = $AllowCancel
            $workflow.ViewMode = 'Workflow'
            $workflow.Template = 'Workflow'
            $workflow.GridColumns = 3

            # Store custom log path in Variables if specified
            if ($LogPath) {
                $workflow.Variables['_LogPath'] = $LogPath
            }

            # Store as current workflow for other functions to use
            $script:CurrentWorkflow = $workflow

            Write-Verbose "Successfully created Workflow UI: $($workflow.ToString())"

            # Return the UI object to support method chaining
            return $workflow
        }
        catch {
            Write-Error "Failed to create Workflow UI: $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "New-PoshUIWorkflow completed"
    }
}

# Backward compatibility alias
Set-Alias -Name 'New-PoshWorkflow' -Value 'New-PoshUIWorkflow'
