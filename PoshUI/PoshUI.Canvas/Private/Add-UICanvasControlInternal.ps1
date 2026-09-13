# Shared internal helper: builds a canvas control hashtable (free-form X/Y), serializes any
# Action/OnChange scriptblocks to text, and adds it to the active target — the current panel's
# Children list (inside an -Children block) or the current canvas page's Controls list.

function Add-UICanvasControlInternal {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Type,
        [string]$Name,
        [string]$Label,
        [object]$Value,
        [Nullable[double]]$X,
        [Nullable[double]]$Y,
        [Nullable[double]]$Width,
        [Nullable[double]]$Height,
        [Nullable[int]]$ZIndex,
        [string[]]$Choices,
        [scriptblock]$Action,
        [scriptblock]$OnChange,
        [string]$Tooltip,
        [Nullable[bool]]$Visible,
        [Nullable[bool]]$Enabled,
        [hashtable]$Properties,
        [hashtable]$XamlActions,
        [System.Collections.Generic.List[hashtable]]$Children
    )

    if (-not $Global:_PoshUICanvas -or -not $Global:_PoshUICanvas.CurrentStep) {
        throw "No canvas page active. Call New-PoshUICanvas or Add-UICanvasPage first."
    }

    $ctrl = @{ Type = $Type }
    if ($Name)                                   { $ctrl.Name = $Name }
    if ($PSBoundParameters.ContainsKey('Label')) { $ctrl.Label = $Label }
    if ($null -ne $X)                            { $ctrl.X = [double]$X }
    if ($null -ne $Y)                            { $ctrl.Y = [double]$Y }
    if ($null -ne $Width)                        { $ctrl.Width = [double]$Width }
    if ($null -ne $Height)                       { $ctrl.Height = [double]$Height }
    if ($null -ne $ZIndex)                       { $ctrl.ZIndex = [int]$ZIndex }
    if ($Choices)                                { $ctrl.Choices = $Choices }
    if ($Tooltip)                                { $ctrl.Tooltip = $Tooltip }
    if ($null -ne $Visible)                      { $ctrl.Visible = [bool]$Visible }
    if ($null -ne $Enabled)                      { $ctrl.Enabled = [bool]$Enabled }
    if ($Children)                               { $ctrl.Children = $Children }
    if ($null -ne $Value)                        { $ctrl.Default = $Value }

    if ($Action) {
        $ctrl.Action = $Action.ToString().Trim()
        if (-not $ctrl.ContainsKey('Events')) { $ctrl.Events = @('Clicked') }
    }
    if ($OnChange) {
        $ctrl.OnChange = $OnChange.ToString().Trim()
        $existing = @($ctrl['Events'])
        $ctrl.Events = @($existing + 'ValueChanged' | Where-Object { $_ })
    }

    if ($XamlActions) {
        $map = @{}
        foreach ($k in $XamlActions.Keys) { $map[$k] = ([scriptblock]$XamlActions[$k]).ToString().Trim() }
        $ctrl.XamlActions = $map
    }

    # ── Author-time validation ────────────────────────────────────────────────────────────────────
    # Cheap checks for mistakes the engine would otherwise accept and silently drop. Warnings only -
    # never terminating - so an unusual-but-deliberate call still works.
    if ($Properties) {
        # Nested objects do not survive serialisation into the definition. Flat arrays of scalars DO
        # (Columns/Labels/Values are folded into the bag by the card cmdlets), so only a dictionary or
        # object - at any depth - is flagged.
        $isNested = {
            param($x)
            if ($x -is [System.Collections.IDictionary] -or $x -is [System.Management.Automation.PSCustomObject]) { return $true }
            if ($x -is [System.Collections.IEnumerable] -and $x -isnot [string]) { foreach ($e in $x) { if (& $isNested $e) { return $true } } }
            $false
        }
        foreach ($k in $Properties.Keys) {
            $v = $Properties[$k]
            if (& $isNested $v) {
                Write-Warning ((
                    "PoshUI: -Properties['{0}'] on {1}{2} is a nested object. Nested values do not " +
                    "round-trip through the UI definition; use the dedicated parameter " +
                    "(-Nodes/-Datasets/-Items/-Choices) instead.") -f $k, $Type, $(if ($Name) { " '$Name'" }))
            }
        }
    }

    if ($Properties) {
        $bag = @{}
        foreach ($k in $Properties.Keys) { $bag[$k] = $Properties[$k] }
        $ctrl.Properties = $bag
    }

    $target = $null
    if ($Global:_PoshUICanvas.TargetStack -and $Global:_PoshUICanvas.TargetStack.Count -gt 0) {
        $target = $Global:_PoshUICanvas.TargetStack.Peek()
    }
    else {
        $target = $Global:_PoshUICanvas.CurrentStep.Controls
    }
    $target.Add($ctrl)
    $ctrl
}
