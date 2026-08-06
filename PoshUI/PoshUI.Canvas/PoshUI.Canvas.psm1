#Requires -Version 5.1

# PoshUI.Canvas — free-form, absolute-positioned canvas apps for the v1 (.NET 4.8 / WPF) engine.
# Authors place controls by X/Y with Add-UICanvas* cmdlets; Show-PoshUICanvas serializes the
# definition to JSON and launches PoshUI.exe (the WPF Launcher), which renders a Canvas page.
#
# This module is self-contained (no dependency on the Wizard/Dashboard modules). It mirrors the
# WinUI 3 PoshUI3.Canvas authoring surface so the same scripts/Designer output run on both engines.

$script:ModuleRoot = $PSScriptRoot

# This module's renderer features (flow modes, XAML hatch, motion/async/toasts/dialogs, diagnostics) need a
# matching engine. Warn at import if the PoshUI.exe we'd launch is older than this minimum.
$script:MinEngineVersion = [version]'1.4.0'
try {
    $parentRoot = Split-Path $PSScriptRoot -Parent
    $engine = @(
        (Join-Path $parentRoot 'bin\PoshUI.exe'),
        (Join-Path $PSScriptRoot 'bin\PoshUI.exe'),
        (Join-Path (Split-Path $parentRoot -Parent) 'Launcher\bin\Debug\PoshUI.exe'),
        (Join-Path (Split-Path $parentRoot -Parent) 'Launcher\bin\Release\PoshUI.exe')
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($engine) {
        $fv = (Get-Item $engine).VersionInfo.FileVersion
        if ($fv -and ([version]$fv) -lt $script:MinEngineVersion) {
            Write-Warning "PoshUI.Canvas needs engine >= $($script:MinEngineVersion) but found $fv at '$engine'. Newer cmdlets (e.g. Set-UICanvasAnimate, Add-UICanvasXaml, Show-UICanvasToast) may not work. Rebuild the Launcher."
        }
    }
} catch { }

$publicFolder  = Join-Path $PSScriptRoot 'Public'
$privateFolder = Join-Path $PSScriptRoot 'Private'

foreach ($folder in @($privateFolder, $publicFolder)) {
    if (-not (Test-Path $folder)) { continue }
    Get-ChildItem -Path $folder -Filter '*.ps1' -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
        try { . $_.FullName }
        catch { Write-Error "Failed to import $($_.FullName): $_" }
    }
}

# Export the public authoring + runtime surface.
Export-ModuleMember -Function @(
    'New-PoshUICanvas', 'Add-UICanvasPage', 'Show-PoshUICanvas', 'Set-UITheme',
    'Add-UICanvasControl',
    'Add-UICanvasLabel', 'Add-UICanvasButton', 'Add-UICanvasTextBox', 'Add-UICanvasMultiLine',
    'Add-UICanvasPassword', 'Add-UICanvasNumber', 'Add-UICanvasDropdown', 'Add-UICanvasListBox',
    'Add-UICanvasRadioGroup', 'Add-UICanvasCheckbox', 'Add-UICanvasToggle', 'Add-UICanvasSlider',
    'Add-UICanvasProgressBar', 'Add-UICanvasConsole', 'Add-UICanvasImage', 'Add-UICanvasHyperlink',
    'Add-UICanvasBanner', 'Add-UICanvasToolbar', 'Add-UICanvasFooter', 'New-UICanvasWindow', 'Add-UICanvasIcon', 'Add-UICanvasBadge', 'Add-UICanvasXaml', 'Add-UICanvasRepeater', 'Add-UICanvasShortcut', 'New-UICanvasState', 'Watch-UICanvasState', 'Add-UICanvasDataGrid', 'Add-UICanvasTreeView', 'Add-UICanvasAutoSuggest', 'Add-UICanvasMarkdown', 'Add-UICanvasRectangle', 'Add-UICanvasEllipse', 'Add-UICanvasLine',
    'Add-UICanvasCard', 'Add-UICanvasPanel', 'Add-UICanvasExpander',
    'Add-UICanvasTabs', 'Add-UICanvasTab', 'Add-UICanvasMenu', 'Add-UICanvasGridSplitter', 'Add-UICanvasViewbox',
    'Add-UICanvasMetricCard', 'Add-UICanvasStatusCard', 'Add-UICanvasTableCard', 'Add-UICanvasChartCard',
    'Add-UICanvasSeparator', 'Add-UICanvasDatePicker', 'Add-UICanvasProgressRing',
    'Add-UICanvasNumberBox', 'Add-UICanvasDropDownButton', 'Add-UICanvasRichEdit',
    'Add-UICanvasRating', 'Add-UICanvasTimePicker', 'Add-UICanvasColorPicker',
    'Add-UICanvasWorkflowStep', 'Add-UICanvasWorkflow', 'Add-UICanvasScriptCard', 'Add-UICanvasWizardNav', 'Add-UICanvasWizardSteps',
    'Add-UICanvasFolderPicker', 'Add-UICanvasFilePicker',
    'Set-UICanvasValue', 'Set-UICanvasProperty', 'Get-UICanvasValue', 'Show-UICanvasPage', 'Submit-UICanvas',
    'Lock-UICanvasNavigation', 'Unlock-UICanvasNavigation', 'Set-UICanvasState', 'Get-UICanvasState', 'Set-UICanvasAnimate', 'Start-UICanvasAsync', 'Show-UICanvasToast', 'Show-UICanvasDialog', 'Show-UICanvasFlyout', 'Show-UICanvasWindow', 'Close-UICanvasWindow', 'Select-UICanvasFolder', 'Select-UICanvasFile'
)
