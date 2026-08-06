function Add-UICanvasPage {
    <#
    .SYNOPSIS
    Adds a Canvas page (step). Multi-page canvases show a nav rail/tabs per -Navigation.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)][string]$Title,
        [string]$Description,
        [string]$Icon,
        # Page-layout root: omit/Canvas = absolute free-form (X/Y); Dock/Grid/VStack/HStack/Wrap = responsive reflow.
        [ValidateSet('Canvas', 'Dock', 'Grid', 'Stack', 'VStack', 'HStack', 'Wrap')][string]$Layout,
        [int]$Columns, [string]$ColumnWidths, [double]$Spacing, [string]$Padding
    )

    if (-not $Global:_PoshUICanvas) { throw "No canvas active. Call New-PoshUICanvas first." }
    $ctx = $Global:_PoshUICanvas

    $applyLayout = {
        param($s)
        if ($Layout)       { $s.Layout = $Layout }
        if ($Columns)      { $s.Columns = $Columns }
        if ($ColumnWidths) { $s.ColumnWidths = $ColumnWidths }
        if ($Spacing)      { $s.Spacing = $Spacing }
        if ($Padding)      { $s.Padding = $Padding }
    }

    # Reuse the auto-created first page for the author's first explicit Add-UICanvasPage.
    if ($ctx.AutoPage -and $ctx.CurrentStep) {
        $ctx.CurrentStep.Title = $Title
        $ctx.CurrentStep.Name  = $Title
        if ($Description) { $ctx.CurrentStep.Description = $Description }
        if ($Icon)        { $ctx.CurrentStep.Icon = $Icon }
        & $applyLayout $ctx.CurrentStep
        $ctx.AutoPage = $false
        return $ctx.CurrentStep
    }

    $step = @{
        Type     = 'Canvas'
        Title    = $Title
        Name     = $Title
        Controls = [System.Collections.Generic.List[hashtable]]::new()
    }
    if ($Description) { $step.Description = $Description }
    if ($Icon)        { $step.Icon = $Icon }
    & $applyLayout $step

    $ctx.Definition.Steps.Add($step)
    $ctx.CurrentStep = $step
    $ctx.AutoPage = $false
    $step
}
