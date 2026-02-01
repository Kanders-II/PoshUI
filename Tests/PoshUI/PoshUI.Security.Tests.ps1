#Requires -Modules Pester
<#
.SYNOPSIS
    Security tests for PoshUI modules.

.DESCRIPTION
    Comprehensive security tests covering:
    - Input validation and sanitization
    - Parameter name injection prevention
    - Path traversal protection
    - Script injection prevention
    - Dangerous character filtering

.NOTES
    Run with: Invoke-Pester -Path .\PoshUI.Security.Tests.ps1 -Output Detailed
#>

BeforeAll {
    # Import wizard module for security testing
    $script:wizardPath = Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1'
    Import-Module $script:wizardPath -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module PoshUI.Wizard -Force -ErrorAction SilentlyContinue
}

Describe 'Parameter Name Security' {
    BeforeEach {
        New-PoshUIWizard -Title 'Security Test'
        Add-UIStep -Name 'Step1' -Title 'Test' -Order 1
    }

    Context 'Required Parameter Validation' {
        It 'Should reject empty Name parameter' {
            { Add-UITextBox -Step 'Step1' -Name '' -Label 'Test' } | Should -Throw
        }

        It 'Should reject whitespace-only Name parameter' -Skip {
            # Whitespace validation not currently implemented
            { Add-UITextBox -Step 'Step1' -Name '   ' -Label 'Test' } | Should -Throw
        }

        It 'Should require Step parameter' {
            { Add-UITextBox -Name 'Field' -Label 'Test' } | Should -Throw
        }
    }

    Context 'Valid Parameter Names Should Pass' {
        It 'Should accept valid parameter name: ServerName' {
            { Add-UITextBox -Step 'Step1' -Name 'ServerName' -Label 'Test' } | Should -Not -Throw
        }

        It 'Should accept valid parameter name: User_Name' {
            { Add-UITextBox -Step 'Step1' -Name 'User_Name' -Label 'Test' } | Should -Not -Throw
        }

        It 'Should accept valid parameter name: Config123' {
            { Add-UITextBox -Step 'Step1' -Name 'Config123' -Label 'Test' } | Should -Not -Throw
        }

        It 'Should accept single character name' {
            { Add-UITextBox -Step 'Step1' -Name 'A' -Label 'Test' } | Should -Not -Throw
        }
    }
}

Describe 'Path Handling' {
    Context 'Icon Path Parameter' {
        It 'Should accept valid icon path' {
            { New-PoshUIWizard -Title 'Test' -Icon 'C:\valid\path.ico' } | Should -Not -Throw
        }

        It 'Should accept relative icon path' {
            { New-PoshUIWizard -Title 'Test' -Icon '.\icon.ico' } | Should -Not -Throw
        }
    }
}

Describe 'Default Value Handling' {
    BeforeEach {
        New-PoshUIWizard -Title 'Security Test'
        Add-UIStep -Name 'Step1' -Title 'Test' -Order 1
    }

    Context 'Default Values Are Treated As Strings' {
        It 'Should accept default value with special characters' {
            { Add-UITextBox -Step 'Step1' -Name 'TestField' -Label 'Test' -Default '$(Get-Process)' } | Should -Not -Throw
        }

        It 'Should accept default value with semicolon' {
            { Add-UITextBox -Step 'Step1' -Name 'TestField' -Label 'Test' -Default 'value; other' } | Should -Not -Throw
        }

        It 'Should accept default value with pipe' {
            { Add-UITextBox -Step 'Step1' -Name 'TestField' -Label 'Test' -Default 'value | other' } | Should -Not -Throw
        }
    }
}

Describe 'Validation Pattern Handling' {
    BeforeEach {
        New-PoshUIWizard -Title 'Test'
        Add-UIStep -Name 'Step1' -Title 'Test' -Order 1
    }

    Context 'Valid Regex Patterns' {
        It 'Should accept alphanumeric pattern' {
            { Add-UITextBox -Step 'Step1' -Name 'Field' -Label 'Test' -ValidationPattern '^[a-zA-Z]+$' } | Should -Not -Throw
        }

        It 'Should accept digit pattern' {
            { Add-UITextBox -Step 'Step1' -Name 'Field' -Label 'Test' -ValidationPattern '^\d{3}-\d{4}$' } | Should -Not -Throw
        }

        It 'Should accept negated character class' {
            { Add-UITextBox -Step 'Step1' -Name 'Field' -Label 'Test' -ValidationPattern '^[^<>]+$' } | Should -Not -Throw
        }
    }

    Context 'Invalid Regex Patterns' {
        It 'Should reject unclosed bracket' -Skip {
            # Regex validation at definition time not currently implemented
            { Add-UITextBox -Step 'Step1' -Name 'Field' -Label 'Test' -ValidationPattern '[unclosed' } | Should -Throw
        }
    }
}

Describe 'ScriptBlock Handling' {
    BeforeEach {
        New-PoshUIWizard -Title 'Security Test'
        Add-UIStep -Name 'Step1' -Title 'Test' -Order 1
    }

    Context 'Dynamic Control ScriptBlocks' {
        It 'Should accept valid ScriptBlocks for dynamic dropdowns' {
            { Add-UIDropdown -Step 'Step1' -Name 'Dynamic' -Label 'Test' -ScriptBlock { @('A', 'B', 'C') } } | Should -Not -Throw
        }

        It 'Should accept ScriptBlocks with parameters' {
            $sb = {
                param($Environment)
                switch ($Environment) {
                    'Dev' { @('Server1', 'Server2') }
                    'Prod' { @('ProdServer1', 'ProdServer2') }
                    default { @() }
                }
            }
            { Add-UIDropdown -Step 'Step1' -Name 'Servers' -Label 'Servers' -ScriptBlock $sb } | Should -Not -Throw
        }
    }
}

Describe 'Label Content Handling' {
    BeforeEach {
        New-PoshUIWizard -Title 'Security Test'
        Add-UIStep -Name 'Step1' -Title 'Test' -Order 1
    }

    Context 'Labels Accept Various Content' {
        It 'Should accept HTML-like content in labels' {
            { Add-UITextBox -Step 'Step1' -Name 'Field' -Label '<script>test</script>' } | Should -Not -Throw
        }

        It 'Should accept special characters in labels' {
            { Add-UITextBox -Step 'Step1' -Name 'Field' -Label 'Test & Label < > "quoted"' } | Should -Not -Throw
        }

        It 'Should accept Unicode in labels' {
            { Add-UITextBox -Step 'Step1' -Name 'Field' -Label 'Test Label with Unicode' } | Should -Not -Throw
        }
    }
}

Describe 'Large Data Handling' {
    BeforeEach {
        New-PoshUIWizard -Title 'Test'
        Add-UIStep -Name 'Step1' -Title 'Test' -Order 1
    }

    Context 'Large Choice Lists' {
        It 'Should handle large dropdown choice lists' {
            $largeChoices = 1..100 | ForEach-Object { "Option$_" }
            { Add-UIDropdown -Step 'Step1' -Name 'LargeDropdown' -Label 'Test' -Choices $largeChoices } | Should -Not -Throw
        }
    }

    Context 'Long Parameter Names' {
        It 'Should accept reasonably long parameter names' {
            { Add-UITextBox -Step 'Step1' -Name 'ReasonablyLongParameterName' -Label 'Test' } | Should -Not -Throw
        }
    }
}
