@{
    RootModule = 'PoshUI.Workflow.psm1'
    ModuleVersion = '1.0.0'
    GUID = 'b2c3d4e5-f6a7-8901-bcde-f23456789012'
    Author = 'A Solution IT LLC'
    CompanyName = 'A Solution IT LLC'
    Copyright = '(c) 2025 A Solution IT LLC. All rights reserved.'
    Description = 'PoshUI.Workflow - PowerShell module for creating interactive workflow UIs with sequential task execution. Part of PoshUI suite.'

    PowerShellVersion = '5.1'
    CompatiblePSEditions = @('Desktop')
    DotNetFrameworkVersion = '4.8'
    
    # Functions to export from this module
    FunctionsToExport = @(
        # Core functions
        'New-PoshUIWorkflow',
        'Show-PoshUIWorkflow',

        # Step management
        'Add-UIStep',

        # Workflow controls
        'Add-UIWorkflowTask',

        # Display controls
        'Add-UIBanner',
        'Add-UICard',

        # Input controls
        'Add-UITextBox',
        'Add-UIDropdown',
        'Add-UICheckbox',
        'Add-UINumeric',
        'Add-UIFilePath',
        'Add-UIFolderPath',

        # Configuration
        'Set-UIBranding',
        'Set-UITheme',
        'Set-UIConfiguration',
        'Get-UIConfiguration',

        # State management
        'Save-UIWorkflowState',
        'Get-UIWorkflowState',
        'Test-UIWorkflowState',
        'Clear-UIWorkflowState',
        'Resume-UIWorkflow',
        'Clear-PoshUIState',
        'Clear-PoshUIRegistryState',
        'Clear-PoshUIFileState'
    )
    
    CmdletsToExport = @()
    VariablesToExport = @()

    # Aliases for backward compatibility
    AliasesToExport = @(
        'New-PoshWorkflow',
        'Show-PoshWorkflow',
        'Add-WorkflowStep',
        'Add-WorkflowTask',
        'Set-WorkflowBranding',
        'Set-WorkflowTheme'
    )
    
    PrivateData = @{
        PSData = @{
            Tags = @('Workflow', 'UI', 'Tasks', 'Interactive', 'PowerShell', 'WPF', 'Automation')
            LicenseUri = 'https://github.com/asolutionit/PoshUI/blob/main/LICENSE'
            ProjectUri = 'https://github.com/asolutionit/PoshUI'
            ReleaseNotes = 'PoshUI.Workflow - Workflow template module for sequential task execution with progress tracking.'
        }
    }
}
