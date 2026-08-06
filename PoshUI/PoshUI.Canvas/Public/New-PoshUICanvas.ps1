function New-PoshUICanvas {
    <#
    .SYNOPSIS
    Initializes a free-form Canvas UI for the v1 (.NET 4.8 / WPF) engine.
    .DESCRIPTION
    Creates a Canvas context and its first page. Place controls by absolute X/Y with
    Add-UICanvas* cmdlets, then call Show-PoshUICanvas to launch.
    .EXAMPLE
    New-PoshUICanvas -Title "Deploy" -Navigation Sidebar
    Add-UICanvasButton "Go" -X 40 -Y 40 -Style Accent -Action { Submit-UICanvas }
    Show-PoshUICanvas
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)][ValidateNotNullOrEmpty()][string]$Title,
        [string]$Description,
        [ValidateSet('Light', 'Dark', 'Auto')][string]$Theme = 'Auto',
        [string]$SidebarHeaderText,
        [string]$SidebarHeaderIcon,
        [string]$WindowTitleIcon,
        [Alias('WindowTitle')][string]$WindowTitleText,
        [bool]$AllowCancel = $true,
        [ValidateSet('None', 'Sidebar', 'Compact', 'Top')][string]$Navigation = 'None',
        # Chromeless window: hide the native title bar so a Canvas toolbar is the top chrome. The window stays
        # drag/resizable (add a close control, e.g. a toolbar action calling Submit-UICanvas).
        [switch]$HideTitleBar,
        [double]$Width, [double]$Height, [double]$MinWidth, [double]$MinHeight
    )

    $resolvedWindowTitle = if ($WindowTitleText) { $WindowTitleText } else { $Title }

    $ui = @{
        Title       = $Title
        Template    = 'Canvas'
        AllowCancel = $AllowCancel
        Branding    = @{
            WindowTitleText   = $resolvedWindowTitle
            SidebarHeaderText = if ($SidebarHeaderText) { $SidebarHeaderText } else { $Title }
            SidebarHeaderIcon = $SidebarHeaderIcon
            WindowTitleIcon   = $WindowTitleIcon
            Theme             = $Theme
            Navigation        = $Navigation
            HideTitleBar      = [bool]$HideTitleBar
        }
        Steps       = [System.Collections.Generic.List[hashtable]]::new()
    }
    if ($Width)     { $ui.Branding.WindowWidth = $Width }
    if ($Height)    { $ui.Branding.WindowHeight = $Height }
    if ($MinWidth)  { $ui.Branding.WindowMinWidth = $MinWidth }
    if ($MinHeight) { $ui.Branding.WindowMinHeight = $MinHeight }
    if ($Description) { $ui.Description = $Description }

    $Global:_PoshUICanvas = @{
        Definition  = $ui
        CurrentStep = $null
        TargetStack = [System.Collections.Generic.Stack[object]]::new()
        AutoPage    = $false
    }

    # Auto-create the first page; the first explicit Add-UICanvasPage reuses it.
    [void](Add-UICanvasPage -Title $Title -Description $Description)
    $Global:_PoshUICanvas.AutoPage = $true

    Write-Verbose "New-PoshUICanvas: $Title"
    $ui
}
