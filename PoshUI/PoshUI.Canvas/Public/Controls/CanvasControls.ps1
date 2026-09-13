# Typed canvas control cmdlets (core set). Each is a thin wrapper over Add-UICanvasControlInternal,
# forwarding the parameters the caller supplied. Label/Value/Choices accept scriptblocks (evaluated
# here at authoring time); Value/Label + -Refresh becomes a live ValueScript re-run in-app;
# Action/OnChange stay scriptblocks (run in-app via the injected runtime cmdlets).

function script:_ResolveValue($v) {
    if ($v -is [scriptblock]) { & $v } else { $v }
}

function script:_FwdCanvas {
    param([string]$Type, [hashtable]$Bound, [hashtable]$Props)
    $fwd = @{ Type = $Type }

    $sb = $null
    if ($Bound.ContainsKey('Value') -and $Bound['Value'] -is [scriptblock]) { $sb = $Bound['Value'] }
    elseif ($Bound.ContainsKey('Label') -and $Bound['Label'] -is [scriptblock]) { $sb = $Bound['Label'] }

    foreach ($p in 'Name', 'Label', 'Value', 'X', 'Y', 'Width', 'Height', 'ZIndex', 'Choices', 'Action', 'OnChange', 'Tooltip', 'Visible', 'Enabled') {
        if (-not $Bound.ContainsKey($p)) { continue }
        $val = $Bound[$p]
        if ($p -in 'Label', 'Value', 'Choices') { $val = _ResolveValue $val }
        $fwd[$p] = $val
    }

    if ($sb -and $Bound.ContainsKey('Refresh') -and [int]$Bound['Refresh'] -gt 0) {
        $Props['ValueScript']     = $sb.ToString()
        $Props['RefreshInterval'] = [int]$Bound['Refresh']
    }

    if ($Bound.ContainsKey('Bind')) { $Props['Bind'] = $Bound['Bind'] }   # reactive state binding
    if ($Bound.ContainsKey('Properties')) {
        foreach ($k in $Bound['Properties'].Keys) { $Props[$k] = _ResolveValue $Bound['Properties'][$k] }
    }
    if ($Props.Count -gt 0) { $fwd['Properties'] = $Props }
    Add-UICanvasControlInternal @fwd
}

# Cascading/dynamic options: emits a hidden 1s watcher that recomputes a control's items via -OptionsScript
# whenever any -DependsOn field changes (change-detected against reactive state, so it never clobbers the
# user's current selection). Used by Add-UICanvasDropdown / -ListBox when -OptionsScript is supplied. No engine
# change: rides the existing -Refresh (polled ValueScript) + Set-UICanvasProperty ItemsSource path.
function script:_WireCascade {
    param([Parameter(Mandatory)][string]$Name, [string[]]$DependsOn, [Parameter(Mandatory)][scriptblock]$OptionsScript, [int]$PollSeconds = 1)
    if (-not $DependsOn -or $DependsOn.Count -eq 0) {
        throw "-OptionsScript requires -DependsOn (the field name(s) whose change recomputes the options)."
    }
    $key = '__dep_' + ($Name -replace '[^A-Za-z0-9]', '')
    $depLiteral = '@(' + (($DependsOn | ForEach-Object { "'" + ($_ -replace "'", "''") + "'" }) -join ',') + ')'
    $optBody = $OptionsScript.ToString()
    # Runs in the bridge runspace every $PollSeconds: only recompute when the watched fields actually changed.
    $watch = @"
`$__names = $depLiteral
`$__cur = (`$__names | ForEach-Object { [string](Get-UICanvasValue -Name `$_) }) -join [char]31
`$__last = [string](Get-UICanvasState '$key')
if (`$__cur -ne `$__last) {
  Set-UICanvasState '$key' `$__cur
  Set-UICanvasProperty -Name '$Name' -Property ItemsSource -Value (@(& { $optBody })) -Quiet
}
''
"@
    Add-UICanvasLabel -Label ([scriptblock]::Create($watch)) -Name "${Name}_dep" -Refresh $PollSeconds -Visible $false | Out-Null
}

# ── Generic ──────────────────────────────────────────────────────────────────
function Add-UICanvasControl {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)][string]$Type,
        [string]$Name, [object]$Label, [object]$Value, [object]$Choices,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$Action, [scriptblock]$OnChange,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties
    )
    $bound = @{}
    foreach ($k in $PSBoundParameters.Keys) { if ($k -ne 'Type') { $bound[$k] = $PSBoundParameters[$k] } }
    _FwdCanvas -Type $Type -Bound $bound -Props @{}
}

# ── Text ─────────────────────────────────────────────────────────────────────
function Add-UICanvasLabel {
    [CmdletBinding()]
    param([Parameter(Position = 0)][object]$Label, [string]$Name, [string]$Bind,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [double]$FontSize, [string]$FontWeight, [string]$Foreground,
        # Live elapsed clock (engine-driven, UI-thread): -Clock is the name of a control holding run-start UTC
        # ticks; the label self-updates as mm:ss without the runspace, so it ticks even during a busy step.
        # -ClockStop names a control holding end ticks; once set the clock freezes at the final elapsed.
        [string]$Clock, [string]$ClockStop,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('FontSize'))   { $props.FontSize = $FontSize }
    if ($PSBoundParameters.ContainsKey('FontWeight')) { $props.FontWeight = $FontWeight }
    if ($PSBoundParameters.ContainsKey('Foreground')) { $props.Foreground = $Foreground }
    if ($PSBoundParameters.ContainsKey('Clock'))      { $props.Clock = $Clock }
    if ($PSBoundParameters.ContainsKey('ClockStop'))  { $props.ClockStop = $ClockStop }
    _FwdCanvas -Type 'Label' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasHyperlink {
    [CmdletBinding()]
    param([Parameter(Position = 0)][object]$Label, [string]$Name, [string]$NavigateUri,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('NavigateUri')) { $props.NavigateUri = $NavigateUri }
    _FwdCanvas -Type 'Hyperlink' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasBanner {
    [CmdletBinding()]
    param([Parameter(Position = 0)][object]$Label, [string]$Name, [object]$Value,
        [ValidateSet('Informational', 'Success', 'Warning', 'Error')][string]$Severity = 'Informational',
        [string]$Icon, [string]$Image,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{ Severity = $Severity }
    if ($PSBoundParameters.ContainsKey('Icon'))  { $props.Icon = $Icon }    # leading status icon (glyph or PNG)
    if ($PSBoundParameters.ContainsKey('Image')) { $props.Image = $Image }  # hero illustration on the right
    _FwdCanvas -Type 'Banner' -Bound $PSBoundParameters -Props $props
}

# Application header/toolbar: brand + status pill (left), nav links (centre), action icons (right).
#   -Links   @('Dashboard','Workflows',...)          each navigates to the page of the same title
#   -Actions @( @{ Icon='bell'; Tooltip='Alerts'; Action={ ... } }, @{ Image='avatar.png'; Action={ ... } } )
function Add-UICanvasToolbar {
    [CmdletBinding()]
    param([string]$Name, [string]$Brand, [string]$BrandIcon, [string]$Status,
        [string[]]$Links, [string]$Active, [object[]]$Actions, [hashtable]$Properties)
    $props = @{}
    if ($Brand)     { $props.Brand = $Brand }
    if ($BrandIcon) { $props.BrandIcon = $BrandIcon }
    if ($Status)    { $props.Status = $Status }
    if ($Active)    { $props.Active = $Active }

    $linkMeta = @()
    foreach ($l in $Links) {
        $evt = '__tbl_' + [guid]::NewGuid().ToString('N').Substring(0, 8)
        Add-UICanvasControlInternal -Type 'Event' -Name $evt -Action ([scriptblock]::Create("Show-UICanvasPage '$($l -replace "'", "''")'")) | Out-Null
        $linkMeta += ([ordered]@{ text = [string]$l; evt = $evt } | ConvertTo-Json -Compress)
    }
    $props.LinksJson = '[' + ($linkMeta -join ',') + ']'

    $actMeta = @()
    foreach ($a in $Actions) {
        $evt = if ($a.Name) { [string]$a.Name } else { '__tba_' + [guid]::NewGuid().ToString('N').Substring(0, 8) }
        if ($a.Action)   { Add-UICanvasControlInternal -Type 'Event' -Name $evt -Action ([scriptblock]$a.Action) | Out-Null }
        elseif ($a.Page) { Add-UICanvasControlInternal -Type 'Event' -Name $evt -Action ([scriptblock]::Create("Show-UICanvasPage '$($a.Page -replace "'", "''")'")) | Out-Null }
        $actMeta += ([ordered]@{ icon = [string]$a.Icon; image = [string]$a.Image; tooltip = [string]$a.Tooltip; evt = $evt } | ConvertTo-Json -Compress)
    }
    $props.ActionsJson = '[' + ($actMeta -join ',') + ']'
    _FwdCanvas -Type 'Toolbar' -Bound $PSBoundParameters -Props $props
}

# Defines a secondary window template (captured now, shown later). Content is a -Content { Add-UICanvas* } block.
# Show it from any action with:  Show-UICanvasWindow -Name <name>.  -Modal opens it as a blocking dialog.
#   New-UICanvasWindow -Name run -Title 'Run script' -Width 640 -Height 480 -Content {
#       Add-UICanvasTextBox -Name arg -Placeholder 'argument'
#       Add-UICanvasConsole -Name runLog -Height 240
#       Add-UICanvasButton 'Run' -Style Accent -Action { Start-UICanvasAsync { Set-UICanvasProperty runLog AppendLine 'running…' } }
#   }
function New-UICanvasWindow {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)][string]$Name,
        [string]$Title = 'PoshUI', [double]$Width, [double]$Height, [switch]$Modal,
        # Window chrome / behaviour customization:
        [bool]$Resizable = $true, [switch]$Topmost, [double]$MinWidth, [double]$MinHeight,
        [string]$Icon, [ValidateSet('CenterOwner', 'CenterScreen', 'Manual')][string]$Position = 'CenterOwner',
        [double]$X, [double]$Y, [switch]$NoScroll, [switch]$HideTitleBar,
        [string]$TitleBarColor, [string]$TitleBarText,   # tint the OS caption (Win11) to match the theme
        # Content layout:
        [ValidateSet('Canvas', 'Stack', 'VStack', 'HStack', 'Grid', 'Wrap')][string]$Layout = 'VStack',
        [int]$Columns, [double]$Spacing, [double]$Padding,
        [Parameter(Mandatory)][scriptblock]$Content)

    $childList = [System.Collections.Generic.List[hashtable]]::new()
    $Global:_PoshUICanvas.TargetStack.Push($childList)
    try { & $Content } finally { [void]$Global:_PoshUICanvas.TargetStack.Pop() }

    $cprops = @{ Layout = $Layout; Padding = $(if ($PSBoundParameters.ContainsKey('Padding')) { $Padding } else { 20 }) }
    if ($PSBoundParameters.ContainsKey('Columns')) { $cprops.Columns = $Columns }
    if ($PSBoundParameters.ContainsKey('Spacing')) { $cprops.Spacing = $Spacing }
    $panel = @{ Type = 'Panel'; Properties = $cprops; Children = $childList }

    $props = @{
        Title = $Title; Modal = [bool]$Modal; ContentJson = ($panel | ConvertTo-Json -Depth 32 -Compress)
        Resizable = [bool]$Resizable; Topmost = [bool]$Topmost; Position = $Position
        NoScroll = [bool]$NoScroll; HideTitleBar = [bool]$HideTitleBar
    }
    foreach ($p in 'Width', 'Height', 'MinWidth', 'MinHeight', 'Icon', 'X', 'Y', 'TitleBarColor', 'TitleBarText') {
        if ($PSBoundParameters.ContainsKey($p)) { $props[$p] = $PSBoundParameters[$p] }
    }
    Add-UICanvasControlInternal -Type 'WindowTemplate' -Name $Name -Properties $props
}

# Application status/footer bar: muted left text + right-aligned links (string = label, or @{ Text; Action/Page }).
function Add-UICanvasFooter {
    [CmdletBinding()]
    param([string]$Name, [string]$LeftText, [object[]]$Links, [hashtable]$Properties)
    $props = @{}
    if ($LeftText) { $props.LeftText = $LeftText }
    $linkMeta = @()
    foreach ($l in $Links) {
        if ($l -is [string]) { $linkMeta += ([ordered]@{ text = $l; evt = '' } | ConvertTo-Json -Compress) }
        else {
            $evt = ''
            if ($l.Action)   { $evt = '__ftl_' + [guid]::NewGuid().ToString('N').Substring(0, 8); Add-UICanvasControlInternal -Type 'Event' -Name $evt -Action ([scriptblock]$l.Action) | Out-Null }
            elseif ($l.Page) { $evt = '__ftl_' + [guid]::NewGuid().ToString('N').Substring(0, 8); Add-UICanvasControlInternal -Type 'Event' -Name $evt -Action ([scriptblock]::Create("Show-UICanvasPage '$($l.Page -replace "'", "''")'")) | Out-Null }
            $linkMeta += ([ordered]@{ text = [string]$l.Text; evt = $evt } | ConvertTo-Json -Compress)
        }
    }
    $props.LinksJson = '[' + ($linkMeta -join ',') + ']'
    _FwdCanvas -Type 'Footer' -Bound $PSBoundParameters -Props $props
}

# ── Buttons ──────────────────────────────────────────────────────────────────
function Add-UICanvasButton {
    [CmdletBinding()]
    param([Parameter(Position = 0)][object]$Label, [string]$Name,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$Action, [ValidateSet('Standard', 'Secondary', 'Accent', 'Primary', 'Subtle', 'Gradient')][string]$Style,
        # Optional leading glyph: a friendly name (play, back, next, refresh, shield, server...),
        # a hex code point (0xE768 / U+E768 / E768), or a literal glyph character.
        [string]$Icon,
        # Page to navigate to on click: a page Title, a numeric index, or a relative
        # keyword (Next / Prev / Previous / Back / First / Last). Ignored if -Action is given.
        [string]$NavigateTo,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Style')) { $props.Style = $Style }
    if ($PSBoundParameters.ContainsKey('Icon'))  { $props.Icon = $Icon }

    # If NavigateTo is given (and no explicit Action), synthesize a navigation action.
    if ($PSBoundParameters.ContainsKey('NavigateTo') -and -not $PSBoundParameters.ContainsKey('Action')) {
        $escaped = $NavigateTo -replace "'", "''"
        $PSBoundParameters['Action'] = [scriptblock]::Create("Show-UICanvasPage '$escaped'")
    }

    _FwdCanvas -Type 'Button' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasIcon {
    [CmdletBinding()]
    param([Parameter(Position = 0)][string]$Icon, [string]$Name,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [double]$FontSize, [string]$Foreground,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Icon'))       { $props.Icon = $Icon }
    if ($PSBoundParameters.ContainsKey('FontSize'))   { $props.FontSize = $FontSize }
    if ($PSBoundParameters.ContainsKey('Foreground')) { $props.Foreground = $Foreground }
    _FwdCanvas -Type 'Icon' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasBadge {
    [CmdletBinding()]
    param([Parameter(Position = 0)][object]$Label, [string]$Name, [object]$Value,
        [ValidateSet('Neutral', 'Info', 'Success', 'Warning', 'Error')][string]$Severity = 'Neutral',
        [string]$Icon,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [double]$FontSize, [string]$Foreground, [string]$Background,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{ Severity = $Severity }
    if ($PSBoundParameters.ContainsKey('Icon'))       { $props.Icon = $Icon }    # leading icon (glyph or PNG)
    if ($PSBoundParameters.ContainsKey('FontSize'))   { $props.FontSize = $FontSize }
    if ($PSBoundParameters.ContainsKey('Foreground')) { $props.Foreground = $Foreground }
    if ($PSBoundParameters.ContainsKey('Background')) { $props.Background = $Background }
    _FwdCanvas -Type 'Badge' -Bound $PSBoundParameters -Props $props
}

# ── Dashboard cards ────────────────────────────────────────────────────────────
function Add-UICanvasMetricCard {
    [CmdletBinding()]
    param([Parameter(Position = 0)][string]$Caption, [string]$Name, [object]$Value,
        [ValidateSet('Up', 'Down', 'Stable')][string]$Trend, [string]$Delta,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Background, [string]$Foreground, [double]$FontSize,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Trend'))      { $props.Trend = $Trend }
    if ($PSBoundParameters.ContainsKey('Delta'))      { $props.Delta = $Delta }
    if ($PSBoundParameters.ContainsKey('Background')) { $props.Background = $Background }
    if ($PSBoundParameters.ContainsKey('Foreground')) { $props.Foreground = $Foreground }
    if ($PSBoundParameters.ContainsKey('FontSize'))   { $props.FontSize = $FontSize }
    if ($PSBoundParameters.ContainsKey('Caption'))    { $PSBoundParameters['Label'] = $Caption }  # caption -> Label
    _FwdCanvas -Type 'MetricCard' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasStatusCard {
    [CmdletBinding()]
    param([Parameter(Position = 0)][string]$Title, [string]$Name, [string[]]$Items,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Background, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Items'))      { $props.Items = $Items }        # "Label|State" entries
    if ($PSBoundParameters.ContainsKey('Background')) { $props.Background = $Background }
    if ($PSBoundParameters.ContainsKey('Title'))      { $PSBoundParameters['Label'] = $Title }
    _FwdCanvas -Type 'StatusCard' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasTableCard {
    [CmdletBinding()]
    param([Parameter(Position = 0)][string]$Title, [string]$Name, [string[]]$Columns, [object[]]$Rows,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Background, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Columns'))    { $props.Columns = $Columns }
    if ($PSBoundParameters.ContainsKey('Rows'))       { $props.Rows = $Rows }           # array of row arrays
    if ($PSBoundParameters.ContainsKey('Background')) { $props.Background = $Background }
    if ($PSBoundParameters.ContainsKey('Title'))      { $PSBoundParameters['Label'] = $Title }
    _FwdCanvas -Type 'TableCard' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasChartCard {
    [CmdletBinding()]
    param([Parameter(Position = 0)][string]$Title, [string]$Name, [string[]]$Labels, [double[]]$Values,
        [string]$Series, [ValidateSet('Bar', 'Line', 'Area', 'Donut', 'Sparkline')][string]$Type,
        [object[]]$Datasets, [switch]$Spline, [string]$Bind,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Background, [string]$Foreground, [double]$ChartHeight,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Labels'))      { $props.Labels = $Labels }
    if ($PSBoundParameters.ContainsKey('Values'))      { $props.Values = $Values }
    if ($PSBoundParameters.ContainsKey('Series'))      { $props.Series = $Series }   # legend label
    if ($PSBoundParameters.ContainsKey('Type'))        { $props.ChartType = $Type }   # Bar (default) | Line | Area | Donut | Sparkline
    if ($Spline)                                       { $props.Spline = $true }       # smooth (curved) lines
    # Multi-series: each dataset is @{ Name=..; Color=..; Values=@(..) } -> JSON-array string (nested data can't
    # round-trip through the object-typed Properties dict; the engine parses it with MiniJson).
    if ($PSBoundParameters.ContainsKey('Datasets')) {
        $props.DatasetsJson = '[' + (($Datasets | ForEach-Object {
            [ordered]@{ name = $_.Name; color = $_.Color; values = @($_.Values) } | ConvertTo-Json -Depth 6 -Compress
        }) -join ',') + ']'
    }
    if ($PSBoundParameters.ContainsKey('Background'))  { $props.Background = $Background }
    if ($PSBoundParameters.ContainsKey('Foreground'))  { $props.Foreground = $Foreground }
    if ($PSBoundParameters.ContainsKey('ChartHeight')) { $props.ChartHeight = $ChartHeight }
    if ($PSBoundParameters.ContainsKey('Title'))       { $PSBoundParameters['Label'] = $Title }
    _FwdCanvas -Type 'ChartCard' -Bound $PSBoundParameters -Props $props
}

# ── Long-tail controls ─────────────────────────────────────────────────────────
function Add-UICanvasRating {
    [CmdletBinding()]
    param([string]$Name, [int]$Value, [int]$Max,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [double]$FontSize, [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Max'))      { $props.Max = $Max }
    if ($PSBoundParameters.ContainsKey('FontSize')) { $props.FontSize = $FontSize }
    _FwdCanvas -Type 'Rating' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasTimePicker {
    [CmdletBinding()]
    param([string]$Name, [string]$Value, [int]$StepMinutes,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('StepMinutes')) { $props.StepMinutes = $StepMinutes }
    _FwdCanvas -Type 'TimePicker' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasColorPicker {
    [CmdletBinding()]
    param([string]$Name, [string]$Value, [object]$Choices,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    _FwdCanvas -Type 'ColorPicker' -Bound $PSBoundParameters -Props @{}    # -Choices = the swatch palette
}

# ── Inputs ───────────────────────────────────────────────────────────────────
function Add-UICanvasTextBox {
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Value, [string]$Placeholder,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Placeholder')) { $props.Placeholder = $Placeholder }
    _FwdCanvas -Type 'TextBox' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasMultiLine {
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Value, [string]$Placeholder,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Placeholder')) { $props.Placeholder = $Placeholder }
    _FwdCanvas -Type 'MultiLine' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasPassword {
    [CmdletBinding()]
    param([string]$Name, [string]$Placeholder,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Placeholder')) { $props.Placeholder = $Placeholder }
    _FwdCanvas -Type 'Password' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasNumber {
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Value, [double]$Minimum, [double]$Maximum,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Minimum')) { $props.Minimum = $Minimum }
    if ($PSBoundParameters.ContainsKey('Maximum')) { $props.Maximum = $Maximum }
    _FwdCanvas -Type 'Numeric' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasDropdown {
    # Cascading/dynamic options: -OptionsScript recomputes the items whenever a -DependsOn field changes.
    # Omit -Choices and -ItemIcons on a cascading dropdown (its items come from the script via ItemsSource).
    # e.g. Add-UICanvasDropdown -Name dc -DependsOn region -OptionsScript { if ((Get-UICanvasValue region) -eq 'US') {'us-east','us-west'} else {'eu-1'} }
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Choices, [object]$Value, [string[]]$ItemIcons,
        [string[]]$DependsOn, [scriptblock]$OptionsScript,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}; if ($PSBoundParameters.ContainsKey('ItemIcons')) { $props.ItemIcons = $ItemIcons }
    _FwdCanvas -Type 'Dropdown' -Bound $PSBoundParameters -Props $props
    if ($PSBoundParameters.ContainsKey('OptionsScript')) { _WireCascade -Name $Name -DependsOn $DependsOn -OptionsScript $OptionsScript }
}

function Add-UICanvasListBox {
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Choices, [object]$Value, [string[]]$ItemIcons,
        [string[]]$DependsOn, [scriptblock]$OptionsScript,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}; if ($PSBoundParameters.ContainsKey('ItemIcons')) { $props.ItemIcons = $ItemIcons }
    _FwdCanvas -Type 'ListBox' -Bound $PSBoundParameters -Props $props
    if ($PSBoundParameters.ContainsKey('OptionsScript')) { _WireCascade -Name $Name -DependsOn $DependsOn -OptionsScript $OptionsScript }
}

function Add-UICanvasRadioGroup {
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Choices, [object]$Value,
        [ValidateSet('Vertical', 'Horizontal')][string]$Orientation = 'Vertical',
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Orientation')) { $props.Orientation = $Orientation }
    _FwdCanvas -Type 'RadioGroup' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasCheckbox {
    [CmdletBinding()]
    param([Parameter(Position = 0)][object]$Label, [string]$Name, [string]$Bind, [object]$Value, [string]$Icon,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}; if ($PSBoundParameters.ContainsKey('Icon')) { $props.Icon = $Icon }
    _FwdCanvas -Type 'Checkbox' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasToggle {
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Value,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    _FwdCanvas -Type 'Toggle' -Bound $PSBoundParameters -Props @{}
}

function Add-UICanvasSlider {
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Value, [double]$Minimum, [double]$Maximum, [double]$Step,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Minimum')) { $props.Minimum = $Minimum }
    if ($PSBoundParameters.ContainsKey('Maximum')) { $props.Maximum = $Maximum }
    if ($PSBoundParameters.ContainsKey('Step'))    { $props.Step = $Step }
    _FwdCanvas -Type 'Slider' -Bound $PSBoundParameters -Props $props
}

# ── Status / output ──────────────────────────────────────────────────────────
function Add-UICanvasProgressBar {
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Value, [double]$Minimum, [double]$Maximum,
        [string]$Fill,                       # fill colour (hex)
        [object]$Glow,                        # $true (glow in fill colour) or a hex colour for a neon glow
        [switch]$GlowPulse, [double]$GlowRadius,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Minimum'))    { $props.Minimum = $Minimum }
    if ($PSBoundParameters.ContainsKey('Maximum'))    { $props.Maximum = $Maximum }
    if ($PSBoundParameters.ContainsKey('Fill'))       { $props.Fill = $Fill }
    if ($PSBoundParameters.ContainsKey('Glow'))       { $props.Glow = "$Glow" }
    if ($PSBoundParameters.ContainsKey('GlowRadius')) { $props.GlowRadius = $GlowRadius }
    if ($GlowPulse)                                   { $props.GlowPulse = $true }
    _FwdCanvas -Type 'ProgressBar' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasConsole {
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Value,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    _FwdCanvas -Type 'Console' -Bound $PSBoundParameters -Props @{}
}

function Add-UICanvasImage {
    [CmdletBinding()]
    param([string]$Name, [Alias('Source')][object]$Value,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    _FwdCanvas -Type 'Image' -Bound $PSBoundParameters -Props @{}
}

# Escape hatch: splice raw WPF XAML into the canvas (for controls the cmdlet set doesn't cover —
# DataGrid, TreeView, custom templates, animations). Any x:Name'd element inside is registered with the
# bridge, so Set/Get-UICanvasValue work on it. The root tag's xmlns is auto-added if you omit it.
function Add-UICanvasXaml {
    [CmdletBinding()]
    param([string]$Name, [string]$Markup, [string]$Path,
        # Name -> scriptblock for x:Name'd controls INSIDE the markup (e.g. a <Button x:Name='go'/>).
        [hashtable]$Actions,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Properties')) { foreach ($k in $Properties.Keys) { $props[$k] = $Properties[$k] } }
    if ($PSBoundParameters.ContainsKey('Markup')) { $props.Markup = $Markup }
    if ($PSBoundParameters.ContainsKey('Path'))   { $props.Path = $Path }
    $fwd = @{ Type = 'Xaml' }
    foreach ($p in 'Name', 'X', 'Y', 'Width', 'Height', 'ZIndex', 'Tooltip', 'Visible', 'Enabled') {
        if ($PSBoundParameters.ContainsKey($p)) { $fwd[$p] = $PSBoundParameters[$p] }
    }
    if ($PSBoundParameters.ContainsKey('Actions')) { $fwd['XamlActions'] = $Actions }
    if ($props.Count -gt 0) { $fwd['Properties'] = $props }
    Add-UICanvasControlInternal @fwd
}

# Data-driven repeater: invokes -Template once per item, emitting its controls into the current container.
# e.g.  Add-UICanvasRepeater -Items $servers -Template { param($s) Add-UICanvasCard ... $s.Name ... }
function Add-UICanvasRepeater {
    [CmdletBinding()]
    param([Parameter(Mandatory, Position = 0)][object[]]$Items, [Parameter(Mandatory)][scriptblock]$Template)
    foreach ($item in $Items) { & $Template $item }
}

# Keyboard shortcut: binds a key gesture (e.g. 'Ctrl+S', 'F5', 'Ctrl+Shift+D') to an action. Invisible control.
function Add-UICanvasShortcut {
    [CmdletBinding()]
    param([Parameter(Mandatory, Position = 0)][string]$Key, [Parameter(Mandatory)][scriptblock]$Action)
    Add-UICanvasControlInternal -Type 'Shortcut' -Action $Action -Properties @{ Key = $Key }
}

# ── Reactive state ─────────────────────────────────────────────────────────────
# Seed a shared, reactive state store. Bind controls with -Bind 'key' (two-way for inputs) or -Bind 'text {key}'
# (one-way template). Update from an action with Set-UICanvasState; read with Get-UICanvasState; react with
# Watch-UICanvasState. Call New-UICanvasState once, right after Add-UICanvasPage.
function New-UICanvasState {
    [CmdletBinding()]
    param([Parameter(Mandatory, Position = 0)][hashtable]$State)
    Add-UICanvasControlInternal -Type 'StateInit' -Properties @{ StateJson = ($State | ConvertTo-Json -Depth 6 -Compress) }
}

# Run an action whenever a state key changes (the new value is available via Get-UICanvasState).
function Watch-UICanvasState {
    [CmdletBinding()]
    param([Parameter(Mandatory, Position = 0)][string]$Key, [Parameter(Mandatory)][scriptblock]$Action)
    Add-UICanvasControlInternal -Type 'StateWatcher' -Action $Action -Properties @{ Key = $Key }
}

# Sortable DataGrid. -Columns names the columns (bound to that property); -Items seeds rows. Populate/refresh
# live with  Set-UICanvasProperty -Name <name> -Property ItemsSource -Value $rows.  Read selection with Get-UICanvasValue.
function Add-UICanvasDataGrid {
    # -OnChange fires when the user selects a row; read the row with Get-UICanvasValue -Name <grid> and address
    # its columns by name (e.g. $row.Task). Enables master/detail: grid selection drives a detail pane.
    [CmdletBinding()]
    param([string]$Name, [string[]]$Columns, [object[]]$Items, [string]$BindItems,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Columns')) { $props.Columns = $Columns }
    # Complex rows are passed as a JSON-array STRING (DataContractJsonSerializer can't round-trip nested
    # objects in an object-typed Properties dict); the engine parses it back. Forced array for 5.1.
    if ($PSBoundParameters.ContainsKey('Items')) {
        $props.ItemsJson = '[' + (($Items | ForEach-Object { $_ | ConvertTo-Json -Depth 6 -Compress }) -join ',') + ']'
    }
    # -BindItems 'key': rows follow a state list — Set-UICanvasState rebuilds the grid (auto add/remove).
    if ($PSBoundParameters.ContainsKey('BindItems')) { $props.BindItems = $BindItems }
    _FwdCanvas -Type 'DataGrid' -Bound $PSBoundParameters -Props $props
}

# TreeView from nested nodes: @{ Text='Sites'; Expanded=$true; Children=@(@{Text='HQ'}, ...) }.
function Add-UICanvasTreeView {
    [CmdletBinding()]
    param([string]$Name, [object[]]$Nodes,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Nodes')) {
        $props.NodesJson = '[' + (($Nodes | ForEach-Object { $_ | ConvertTo-Json -Depth 10 -Compress }) -join ',') + ']'
    }
    _FwdCanvas -Type 'TreeView' -Bound $PSBoundParameters -Props $props
}

# Editable, type-to-filter combo (auto-suggest). Value = current text.
function Add-UICanvasAutoSuggest {
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Choices, [object]$Value,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties)
    _FwdCanvas -Type 'AutoSuggest' -Bound $PSBoundParameters -Props @{}
}

# Lightweight Markdown viewer (headings, **bold**, *italic*, `code`, - bullets). -Text or -Path.
function Add-UICanvasMarkdown {
    [CmdletBinding()]
    param([string]$Name, [string]$Text, [string]$Path,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Text')) { $props.Text = $Text }
    if ($PSBoundParameters.ContainsKey('Path')) { $props.Path = $Path }
    _FwdCanvas -Type 'Markdown' -Bound $PSBoundParameters -Props $props
}

# ── Shapes ───────────────────────────────────────────────────────────────────
function Add-UICanvasRectangle {
    [CmdletBinding()]
    param([string]$Name, [string]$Fill, [string]$Stroke, [double]$StrokeThickness, [double]$CornerRadius,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Fill'))            { $props.Fill = $Fill }
    if ($PSBoundParameters.ContainsKey('Stroke'))          { $props.Stroke = $Stroke }
    if ($PSBoundParameters.ContainsKey('StrokeThickness')) { $props.StrokeThickness = $StrokeThickness }
    if ($PSBoundParameters.ContainsKey('CornerRadius'))    { $props.CornerRadius = $CornerRadius }
    _FwdCanvas -Type 'Rectangle' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasEllipse {
    [CmdletBinding()]
    param([string]$Name, [string]$Fill, [string]$Stroke, [double]$StrokeThickness,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Fill'))            { $props.Fill = $Fill }
    if ($PSBoundParameters.ContainsKey('Stroke'))          { $props.Stroke = $Stroke }
    if ($PSBoundParameters.ContainsKey('StrokeThickness')) { $props.StrokeThickness = $StrokeThickness }
    _FwdCanvas -Type 'Ellipse' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasLine {
    [CmdletBinding()]
    param([string]$Name, [double]$X, [double]$Y, [double]$X2, [double]$Y2,
        [string]$Stroke, [double]$StrokeThickness, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('X2'))              { $props.X2 = $X2 }
    if ($PSBoundParameters.ContainsKey('Y2'))              { $props.Y2 = $Y2 }
    if ($PSBoundParameters.ContainsKey('Stroke'))          { $props.Stroke = $Stroke }
    if ($PSBoundParameters.ContainsKey('StrokeThickness')) { $props.StrokeThickness = $StrokeThickness }
    _FwdCanvas -Type 'Line' -Bound $PSBoundParameters -Props $props
}

# ── Containers ───────────────────────────────────────────────────────────────
function Add-UICanvasPanel {
    [CmdletBinding()]
    param([string]$Name, [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [switch]$Card, [string]$Background, [double]$CornerRadius,
        # Auto-layout for children: Canvas = free-form X/Y (default); Stack/VStack/HStack/Grid/Wrap arrange them.
        [ValidateSet('Canvas', 'Stack', 'VStack', 'HStack', 'Grid', 'Wrap')][string]$Layout,
        [int]$Columns, [string]$ColumnWidths, [double]$Spacing, [double]$Padding,
        [scriptblock]$Action,   # makes the whole panel/card clickable (fires like a button)
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties,
        [Parameter(Mandatory)][scriptblock]$Children)

    $childList = [System.Collections.Generic.List[hashtable]]::new()
    $Global:_PoshUICanvas.TargetStack.Push($childList)
    try   { & $Children } finally { [void]$Global:_PoshUICanvas.TargetStack.Pop() }

    $props = @{}
    if ($PSBoundParameters.ContainsKey('Properties')) { foreach ($k in $Properties.Keys) { $props[$k] = $Properties[$k] } }
    foreach ($p in 'Background', 'CornerRadius', 'Layout', 'Columns', 'ColumnWidths', 'Spacing', 'Padding') {
        if ($PSBoundParameters.ContainsKey($p)) { $props[$p] = $PSBoundParameters[$p] }
    }

    $fwd = @{ Type = $(if ($Card) { 'Card' } else { 'Panel' }); Children = $childList }
    foreach ($p in 'Name', 'X', 'Y', 'Width', 'Height', 'ZIndex', 'Action', 'Tooltip', 'Visible', 'Enabled') {
        if ($PSBoundParameters.ContainsKey($p)) { $fwd[$p] = $PSBoundParameters[$p] }
    }
    if ($props.Count -gt 0) { $fwd['Properties'] = $props }
    Add-UICanvasControlInternal @fwd
}

function Add-UICanvasCard {
    [CmdletBinding()]
    param([string]$Name, [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Background, [double]$CornerRadius,
        [ValidateSet('Canvas', 'Stack', 'VStack', 'HStack', 'Grid', 'Wrap')][string]$Layout,
        [int]$Columns, [string]$ColumnWidths, [double]$Spacing, [double]$Padding,
        [scriptblock]$Action,   # makes the whole card clickable (fires like a button)
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties,
        [Parameter(Mandatory)][scriptblock]$Children)
    $fwd = @{ Card = $true }
    foreach ($p in 'Name', 'X', 'Y', 'Width', 'Height', 'ZIndex', 'Background', 'CornerRadius', 'Layout', 'Columns', 'ColumnWidths', 'Spacing', 'Padding', 'Action', 'Tooltip', 'Visible', 'Enabled', 'Properties', 'Children') {
        if ($PSBoundParameters.ContainsKey($p)) { $fwd[$p] = $PSBoundParameters[$p] }
    }
    Add-UICanvasPanel @fwd
}

# Tabbed container. Children must be Add-UICanvasTab blocks; each becomes one tab page.
# Read/switch the active tab with Get-UICanvasValue / Set-UICanvasValue (index, or the tab's header text).
#   Add-UICanvasTabs -Name t -Children {
#       Add-UICanvasTab 'Summary' -Children { Add-UICanvasLabel 'hi' }
#       Add-UICanvasTab 'Details' -Layout VStack -Children { ... }
#   }
function Add-UICanvasTabs {
    [CmdletBinding()]
    param([string]$Name, [int]$SelectedIndex,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties,
        [Parameter(Mandatory)][scriptblock]$Children)

    $childList = [System.Collections.Generic.List[hashtable]]::new()
    $Global:_PoshUICanvas.TargetStack.Push($childList)
    try { & $Children } finally { [void]$Global:_PoshUICanvas.TargetStack.Pop() }

    $props = @{}
    if ($PSBoundParameters.ContainsKey('SelectedIndex')) { $props.SelectedIndex = $SelectedIndex }
    if ($PSBoundParameters.ContainsKey('Properties')) { foreach ($k in $Properties.Keys) { $props[$k] = $Properties[$k] } }

    $fwd = @{ Type = 'Tabs'; Children = $childList }
    if ($props.Count -gt 0) { $fwd.Properties = $props }
    foreach ($p in 'Name', 'X', 'Y', 'Width', 'Height', 'ZIndex', 'OnChange', 'Tooltip', 'Visible', 'Enabled') {
        if ($PSBoundParameters.ContainsKey($p)) { $fwd[$p] = $PSBoundParameters[$p] }
    }
    Add-UICanvasControlInternal @fwd
}

# One tab page inside Add-UICanvasTabs. The positional label is the tab header.
function Add-UICanvasTab {
    [CmdletBinding()]
    param([Parameter(Mandatory, Position = 0)][string]$Label, [string]$Name,
        [ValidateSet('VStack', 'HStack', 'Grid', 'Wrap', 'Canvas')][string]$Layout = 'VStack',
        [int]$Columns, [string]$ColumnWidths, [double]$Spacing, [object]$Padding,
        [switch]$Disabled, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties,
        [Parameter(Mandatory)][scriptblock]$Children)

    $childList = [System.Collections.Generic.List[hashtable]]::new()
    $Global:_PoshUICanvas.TargetStack.Push($childList)
    try { & $Children } finally { [void]$Global:_PoshUICanvas.TargetStack.Pop() }

    $props = @{ Layout = $Layout }
    foreach ($p in 'Columns', 'ColumnWidths', 'Spacing', 'Padding') {
        if ($PSBoundParameters.ContainsKey($p)) { $props[$p] = $PSBoundParameters[$p] }
    }
    if ($Disabled) { $props.Disabled = $true }
    if ($PSBoundParameters.ContainsKey('Properties')) { foreach ($k in $Properties.Keys) { $props[$k] = $Properties[$k] } }

    $fwd = @{ Type = 'Tab'; Children = $childList; Properties = $props; Label = $Label }
    foreach ($p in 'Name', 'Tooltip', 'Visible', 'Enabled') {
        if ($PSBoundParameters.ContainsKey($p)) { $fwd[$p] = $PSBoundParameters[$p] }
    }
    Add-UICanvasControlInternal @fwd
}

# Classic menu bar. -Items takes nested hashtables; a leaf's Action fires when clicked, Text '-' is a separator.
#   Add-UICanvasMenu -Items @(
#       @{ Text = 'File'; Items = @(
#            @{ Text = 'Open';  Gesture = 'Ctrl+O'; Action = { ... } }
#            @{ Text = '-' }
#            @{ Text = 'Exit';  Action = { Submit-UICanvas } } ) }
#       @{ Text = 'Help'; Items = @(@{ Text = 'About'; Action = { ... } }) }
#   )
function Add-UICanvasMenu {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object[]]$Items, [string]$Name, [string]$Background,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties)

    # Each actionable leaf becomes a hidden 'Event' control the engine fires by name (same as toolbar actions).
    function script:_MenuNode($n) {
        $o = [ordered]@{ text = [string]$n.Text }
        if ($n.Icon) { $o.icon = [string]$n.Icon }
        if ($n.Gesture) { $o.gesture = [string]$n.Gesture }
        if ($n.Disabled) { $o.disabled = $true }
        if ($null -ne $n.Checked) { $o.checked = [bool]$n.Checked }
        if ($n.Items) {
            $o.items = @(foreach ($k in $n.Items) { _MenuNode $k })
        }
        elseif ($n.Action -or $n.Page) {
            $evt = if ($n.Name) { [string]$n.Name } else { '__mnu_' + [guid]::NewGuid().ToString('N').Substring(0, 8) }
            $act = if ($n.Action) { [scriptblock]$n.Action } else { [scriptblock]::Create("Show-UICanvasPage '$($n.Page -replace "'", "''")'") }
            Add-UICanvasControlInternal -Type 'Event' -Name $evt -Action $act | Out-Null
            $o.evt = $evt
        }
        $o
    }

    $props = @{ ItemsJson = (ConvertTo-Json @(foreach ($i in $Items) { _MenuNode $i }) -Depth 12 -Compress) }
    if ($PSBoundParameters.ContainsKey('Background')) { $props.Background = $Background }
    _FwdCanvas -Type 'Menu' -Bound $PSBoundParameters -Props $props
}

# Drag handle that resizes adjacent Grid rows/columns. Put it in its own Grid cell between the two panes.
# Default resizes COLUMNS; -Orientation Horizontal resizes rows.
function Add-UICanvasGridSplitter {
    [CmdletBinding()]
    param([string]$Name, [ValidateSet('Vertical', 'Horizontal')][string]$Orientation = 'Vertical',
        [double]$Thickness, [string]$Background,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties)
    $props = @{ Orientation = $Orientation }
    if ($PSBoundParameters.ContainsKey('Thickness'))  { $props.Thickness = $Thickness }
    if ($PSBoundParameters.ContainsKey('Background')) { $props.Background = $Background }
    _FwdCanvas -Type 'GridSplitter' -Bound $PSBoundParameters -Props $props
}

# Scales its content to fit the available space (vector scaling, so text stays crisp).
function Add-UICanvasViewbox {
    [CmdletBinding()]
    param([string]$Name,
        [ValidateSet('Uniform', 'Fill', 'UniformToFill', 'None')][string]$Stretch = 'Uniform',
        [ValidateSet('Both', 'UpOnly', 'DownOnly')][string]$StretchDirection = 'Both',
        [ValidateSet('VStack', 'HStack', 'Grid', 'Wrap', 'Canvas')][string]$Layout = 'VStack',
        [double]$Spacing,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties,
        [Parameter(Mandatory)][scriptblock]$Children)

    $childList = [System.Collections.Generic.List[hashtable]]::new()
    $Global:_PoshUICanvas.TargetStack.Push($childList)
    try { & $Children } finally { [void]$Global:_PoshUICanvas.TargetStack.Pop() }

    $props = @{ Stretch = $Stretch; StretchDirection = $StretchDirection; Layout = $Layout }
    if ($PSBoundParameters.ContainsKey('Spacing')) { $props.Spacing = $Spacing }
    if ($PSBoundParameters.ContainsKey('Properties')) { foreach ($k in $Properties.Keys) { $props[$k] = $Properties[$k] } }

    $fwd = @{ Type = 'Viewbox'; Children = $childList; Properties = $props }
    foreach ($p in 'Name', 'X', 'Y', 'Width', 'Height', 'ZIndex', 'Tooltip', 'Visible', 'Enabled') {
        if ($PSBoundParameters.ContainsKey($p)) { $fwd[$p] = $PSBoundParameters[$p] }
    }
    Add-UICanvasControlInternal @fwd
}

function Add-UICanvasExpander {
    [CmdletBinding()]
    param([Parameter(Position = 0)][object]$Label, [string]$Name,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [bool]$IsExpanded = $true, [string]$Background, [double]$CornerRadius,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties,
        [Parameter(Mandatory)][scriptblock]$Children)

    $childList = [System.Collections.Generic.List[hashtable]]::new()
    $Global:_PoshUICanvas.TargetStack.Push($childList)
    try   { & $Children } finally { [void]$Global:_PoshUICanvas.TargetStack.Pop() }

    $props = @{ IsExpanded = $IsExpanded }
    if ($PSBoundParameters.ContainsKey('Background'))   { $props.Background = $Background }
    if ($PSBoundParameters.ContainsKey('CornerRadius')) { $props.CornerRadius = $CornerRadius }

    $fwd = @{ Type = 'Expander'; Children = $childList; Properties = $props }
    foreach ($p in 'Name', 'Label', 'X', 'Y', 'Width', 'Height', 'ZIndex', 'Tooltip', 'Visible', 'Enabled') {
        if ($PSBoundParameters.ContainsKey($p)) { $fwd[$p] = $PSBoundParameters[$p] }
    }
    Add-UICanvasControlInternal @fwd
}

# ── Long-tail controls ─────────────────────────────────────────────────────────
function Add-UICanvasSeparator {
    [CmdletBinding()]
    param([string]$Name, [double]$X, [double]$Y, [double]$Width, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties)
    _FwdCanvas -Type 'Separator' -Bound $PSBoundParameters -Props @{}
}

function Add-UICanvasDatePicker {
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Value,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    _FwdCanvas -Type 'DatePicker' -Bound $PSBoundParameters -Props @{}
}

function Add-UICanvasProgressRing {
    [CmdletBinding()]
    param([string]$Name, [string]$Foreground, [double]$Thickness,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Foreground')) { $props.Foreground = $Foreground }
    if ($PSBoundParameters.ContainsKey('Thickness'))  { $props.Thickness = $Thickness }
    _FwdCanvas -Type 'ProgressRing' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasNumberBox {
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Value, [double]$Minimum, [double]$Maximum, [double]$Step,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Minimum')) { $props.Minimum = $Minimum }
    if ($PSBoundParameters.ContainsKey('Maximum')) { $props.Maximum = $Maximum }
    if ($PSBoundParameters.ContainsKey('Step'))    { $props.Step = $Step }
    _FwdCanvas -Type 'NumberBox' -Bound $PSBoundParameters -Props $props
}

function Add-UICanvasDropDownButton {
    [CmdletBinding()]
    param([Parameter(Position = 0)][object]$Label, [string]$Name, [object]$Choices, [object]$Value,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [hashtable]$Properties)
    _FwdCanvas -Type 'DropDownButton' -Bound $PSBoundParameters -Props @{}
}

function Add-UICanvasRichEdit {
    [CmdletBinding()]
    param([string]$Name, [string]$Bind, [object]$Value, [string]$Placeholder,
        [double]$X, [double]$Y, [double]$Width, [double]$Height, [int]$ZIndex,
        [scriptblock]$OnChange, [string]$Tooltip, [bool]$Visible, [bool]$Enabled, [int]$Refresh, [hashtable]$Properties)
    $props = @{}
    if ($PSBoundParameters.ContainsKey('Placeholder')) { $props.Placeholder = $Placeholder }
    _FwdCanvas -Type 'RichEdit' -Bound $PSBoundParameters -Props $props
}
