@{
    RootModule        = 'PoshUI.Canvas.psm1'
    ModuleVersion     = '1.2.0'
    GUID              = 'b2c7d9e0-5a14-4f3b-9c2a-71d8e6a0c4f2'
    Author            = 'Kanders-II'
    CompanyName       = 'Kanders-II'
    Copyright         = '(c) 2025 Kanders-II. All rights reserved.'
    Description       = 'Free-form, absolute-positioned Canvas apps for the PoshUI v1 (.NET 4.8 / WPF) engine.'
    PowerShellVersion = '5.1'
    FunctionsToExport = @(
        'New-PoshUICanvas', 'Add-UICanvasPage', 'Show-PoshUICanvas', 'Set-UITheme', 'Add-UICanvasControl',
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
        'Add-UICanvasFolderPicker', 'Add-UICanvasFilePicker', 'Select-UICanvasFolder', 'Select-UICanvasFile',
        'Set-UICanvasValue', 'Set-UICanvasProperty', 'Get-UICanvasValue', 'Show-UICanvasPage', 'Submit-UICanvas',
        'Lock-UICanvasNavigation', 'Unlock-UICanvasNavigation', 'Set-UICanvasState', 'Get-UICanvasState', 'Set-UICanvasAnimate', 'Start-UICanvasAsync', 'Show-UICanvasToast', 'Show-UICanvasDialog', 'Show-UICanvasFlyout', 'Show-UICanvasWindow', 'Close-UICanvasWindow'
    )
    CmdletsToExport   = @()
    VariablesToExport = @()
    AliasesToExport   = @()
}
