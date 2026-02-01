@{
    RootModule = 'PoshUI.Wizard.psm1'
    ModuleVersion = '1.0.0'
    GUID = 'a1b2c3d4-e5f6-7890-abcd-ef1234567891'
    Author = 'A Solution IT LLC'
    CompanyName = 'A Solution IT LLC'
    Copyright = '(c) 2025 A Solution IT LLC. All rights reserved.'
    Description = 'PoshUI.Wizard - PowerShell module for creating interactive wizard UIs with input controls. Part of PoshUI suite.'

    PowerShellVersion = '5.1'
    CompatiblePSEditions = @('Desktop')
    DotNetFrameworkVersion = '4.8'
    
    # Functions to export from this module
    FunctionsToExport = @(
        # Core functions
        'New-PoshUIWizard',
        'Show-PoshUIWizard',

        # Step management
        'Add-UIStep',

        # Input controls
        'Add-UITextBox',
        'Add-UIPassword',
        'Add-UICheckbox',
        'Add-UIToggle',
        'Add-UIDropdown',
        'Add-UIListBox',
        'Add-UIFilePath',
        'Add-UIFolderPath',
        'Add-UINumeric',
        'Add-UIDate',
        'Add-UIOptionGroup',
        'Add-UIMultiLine',

        # Display controls
        'Add-UIBanner',
        'Add-UICard',

        # Configuration
        'Set-UIBranding',
        'Set-UITheme',
        'Set-UIConfiguration',
        'Get-UIConfiguration',

        # Maintenance and cleanup
        'Clear-PoshUIState',
        'Clear-PoshUIRegistryState',
        'Clear-PoshUIFileState',
        'Register-PoshUICleanupTask',
        'Unregister-PoshUICleanupTask'
    )
    
    CmdletsToExport = @()
    VariablesToExport = @()

    # Aliases for backward compatibility
    AliasesToExport = @(
        'New-PoshWizard',
        'Show-PoshWizard',
        'Add-WizardStep',
        'Add-WizardTextBox',
        'Add-WizardPassword',
        'Add-WizardCheckbox',
        'Add-WizardToggle',
        'Add-WizardDropdown',
        'Add-WizardListBox',
        'Add-WizardFilePath',
        'Add-WizardFolderPath',
        'Add-WizardNumeric',
        'Add-WizardDate',
        'Add-WizardOptionGroup',
        'Add-WizardMultiLine',
        'Add-WizardBanner',
        'Add-WizardCard',
        'Set-WizardBranding',
        'Set-WizardTheme'
    )
    
    PrivateData = @{
        PSData = @{
            Tags = @('Wizard', 'UI', 'Forms', 'Interactive', 'PowerShell', 'WPF', 'Input')
            LicenseUri = 'https://github.com/asolutionit/PoshUI/blob/main/LICENSE'
            ProjectUri = 'https://github.com/asolutionit/PoshUI'
            ReleaseNotes = 'PoshUI.Wizard - Wizard template module for step-by-step input collection.'
        }
    }
}
