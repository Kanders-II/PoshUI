# Argument Completers for PoshWizard Module
# Provides IntelliSense for dynamic values

# Step Name Completer - Shows available steps in current wizard
$StepNameCompleter = {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
    
    if ($script:CurrentWizard -and $script:CurrentWizard.Steps) {
        $script:CurrentWizard.Steps.Title |
            Where-Object { $_ -like "$wordToComplete*" } |
            ForEach-Object {
                [System.Management.Automation.CompletionResult]::new(
                    $_,
                    $_,
                    'ParameterValue',
                    "Step: $_"
                )
            }
    }
}

# Theme Completer - Shows available themes
$ThemeCompleter = {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
    
    $themes = @('Light', 'Dark', 'Auto')
    $themes | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
        [System.Management.Automation.CompletionResult]::new(
            $_,
            $_,
            'ParameterValue',
            "Theme: $_"
        )
    }
}

# Icon Glyph Completer - Shows common Segoe MDL2 icons
$IconGlyphCompleter = {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
    
    $commonIcons = @{
        'Home' = '&#xE80F;'
        'Settings' = '&#xE713;'
        'Save' = '&#xE74E;'
        'Folder' = '&#xE8B7;'
        'Document' = '&#xE8A5;'
        'People' = '&#xE716;'
        'Calendar' = '&#xE787;'
        'Mail' = '&#xE715;'
        'Server' = '&#xE968;'
        'Database' = '&#xE1C3;'
        'Cloud' = '&#xE753;'
        'Lock' = '&#xE72E;'
        'Key' = '&#xE8D7;'
        'Checkmark' = '&#xE73E;'
        'Error' = '&#xE783;'
        'Warning' = '&#xE7BA;'
        'Info' = '&#xE946;'
    }
    
    $commonIcons.GetEnumerator() |
        Where-Object { $_.Key -like "$wordToComplete*" } |
        ForEach-Object {
            [System.Management.Automation.CompletionResult]::new(
                $_.Value,
                $_.Key,
                'ParameterValue',
                "Icon: $($_.Key) - $($_.Value)"
            )
        }
}

# Register the argument completers
Register-ArgumentCompleter -CommandName 'Add-WizardTextBox', 'Add-WizardPassword', 'Add-WizardCheckbox', 'Add-WizardToggle', 'Add-WizardDropdown', 'Add-WizardFilePath', 'Add-WizardFolderPath', 'Add-WizardCard' -ParameterName 'Step' -ScriptBlock $StepNameCompleter

Register-ArgumentCompleter -CommandName 'Show-PoshWizard', 'Set-WizardTheme' -ParameterName 'Theme' -ScriptBlock $ThemeCompleter

Register-ArgumentCompleter -CommandName 'Add-WizardStep', 'Add-WizardWelcome' -ParameterName 'IconGlyph' -ScriptBlock $IconGlyphCompleter

Write-Verbose "PoshWizard argument completers registered"

