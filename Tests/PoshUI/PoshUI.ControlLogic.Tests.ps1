#Requires -Modules Pester
<#
.SYNOPSIS
    Control logic tests for PoshUI modules.

.DESCRIPTION
    Tests covering control behavior and logic:
    - Mandatory field validation
    - Default value handling
    - Numeric range validation
    - Password complexity rules
    - Dynamic control dependencies

.NOTES
    Run with: Invoke-Pester -Path .\PoshUI.ControlLogic.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $wizardPath = Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1'
    Import-Module $wizardPath -Force
}

AfterAll {
    Remove-Module PoshUI.Wizard -Force -ErrorAction SilentlyContinue
}

Describe 'TextBox Control Logic' {
    BeforeEach {
        New-PoshUIWizard -Title 'Control Logic Test'
        Add-UIStep -Name 'Step1' -Title 'Test Step' -Order 1
    }

    Context 'Mandatory Field Behavior' {
        It 'Should accept Mandatory switch' {
            { Add-UITextBox -Step 'Step1' -Name 'Required' -Label 'Required Field' -Mandatory } | Should -Not -Throw
        }

        It 'Should work without Mandatory switch' {
            { Add-UITextBox -Step 'Step1' -Name 'Optional' -Label 'Optional Field' } | Should -Not -Throw
        }
    }

    Context 'Default Value Handling' {
        It 'Should accept Default value' {
            { Add-UITextBox -Step 'Step1' -Name 'WithDefault' -Label 'Field' -Default 'DefaultValue' } | Should -Not -Throw
        }

        It 'Should accept empty default value' {
            { Add-UITextBox -Step 'Step1' -Name 'EmptyDefault' -Label 'Field' -Default '' } | Should -Not -Throw
        }
    }

    Context 'Validation Pattern' {
        It 'Should accept validation pattern' {
            { Add-UITextBox -Step 'Step1' -Name 'Validated' -Label 'Field' -ValidationPattern '^[A-Z]+$' } | Should -Not -Throw
        }

        It 'Should accept validation message' {
            { Add-UITextBox -Step 'Step1' -Name 'Validated' -Label 'Field' -ValidationPattern '^[A-Z]+$' -ValidationMessage 'Must be uppercase' } | Should -Not -Throw
        }
    }
}

Describe 'Numeric Control Logic' {
    BeforeEach {
        New-PoshUIWizard -Title 'Numeric Test'
        Add-UIStep -Name 'Step1' -Title 'Test Step' -Order 1
    }

    Context 'Range Validation' {
        It 'Should accept Min value' {
            { Add-UINumeric -Step 'Step1' -Name 'Number' -Label 'Number' -Min 0 } | Should -Not -Throw
        }

        It 'Should accept Max value' {
            { Add-UINumeric -Step 'Step1' -Name 'Number' -Label 'Number' -Max 100 } | Should -Not -Throw
        }

        It 'Should accept both Min and Max values' {
            { Add-UINumeric -Step 'Step1' -Name 'Number' -Label 'Number' -Min 10 -Max 50 } | Should -Not -Throw
        }

        It 'Should accept negative ranges' {
            { Add-UINumeric -Step 'Step1' -Name 'Number' -Label 'Number' -Min -100 -Max -10 } | Should -Not -Throw
        }
    }

    Context 'Default Value' {
        It 'Should accept Default value' {
            { Add-UINumeric -Step 'Step1' -Name 'Number' -Label 'Number' -Default 50 } | Should -Not -Throw
        }
    }
}

Describe 'Password Control Logic' {
    BeforeEach {
        New-PoshUIWizard -Title 'Password Test'
        Add-UIStep -Name 'Step1' -Title 'Test Step' -Order 1
    }

    Context 'Password Validation Rules' {
        It 'Should accept MinLength parameter' {
            { Add-UIPassword -Step 'Step1' -Name 'Password' -Label 'Password' -MinLength 8 } | Should -Not -Throw
        }

        It 'Should accept ValidationPattern' {
            { Add-UIPassword -Step 'Step1' -Name 'Password' -Label 'Password' -ValidationPattern '^.{8,}$' } | Should -Not -Throw
        }

        It 'Should accept ValidationMessage' {
            { Add-UIPassword -Step 'Step1' -Name 'Password' -Label 'Password' -ValidationMessage 'Password must be complex' } | Should -Not -Throw
        }

        It 'Should accept ShowRevealButton with boolean' {
            { Add-UIPassword -Step 'Step1' -Name 'Password' -Label 'Password' -ShowRevealButton $true } | Should -Not -Throw
        }
    }
}

Describe 'Dropdown Control Logic' {
    BeforeEach {
        New-PoshUIWizard -Title 'Dropdown Test'
        Add-UIStep -Name 'Step1' -Title 'Test Step' -Order 1
    }

    Context 'Static Choices' {
        It 'Should accept choices array' {
            { Add-UIDropdown -Step 'Step1' -Name 'Select' -Label 'Select' -Choices @('A', 'B', 'C') } | Should -Not -Throw
        }

        It 'Should accept default selection' {
            { Add-UIDropdown -Step 'Step1' -Name 'Select' -Label 'Select' -Choices @('A', 'B', 'C') -Default 'B' } | Should -Not -Throw
        }
    }

    Context 'Dynamic Choices (ScriptBlock)' {
        It 'Should accept ScriptBlock for dynamic choices' {
            { Add-UIDropdown -Step 'Step1' -Name 'Dynamic' -Label 'Dynamic' -ScriptBlock { @('X', 'Y', 'Z') } } | Should -Not -Throw
        }

        It 'Should accept cascading dropdown with ScriptBlock' {
            Add-UIDropdown -Step 'Step1' -Name 'Parent' -Label 'Parent' -Choices @('A', 'B')
            { Add-UIDropdown -Step 'Step1' -Name 'Child' -Label 'Child' -ScriptBlock { param($Parent); @('A1', 'A2') } } | Should -Not -Throw
        }
    }
}

Describe 'Checkbox and Toggle Logic' {
    BeforeEach {
        New-PoshUIWizard -Title 'Boolean Test'
        Add-UIStep -Name 'Step1' -Title 'Test Step' -Order 1
    }

    Context 'Checkbox Default State' {
        It 'Should create checkbox without default' {
            { Add-UICheckbox -Step 'Step1' -Name 'Check' -Label 'Check' } | Should -Not -Throw
        }

        It 'Should accept true default' {
            { Add-UICheckbox -Step 'Step1' -Name 'Check' -Label 'Check' -Default $true } | Should -Not -Throw
        }
    }

    Context 'Toggle Default State' {
        It 'Should create toggle without default' {
            { Add-UIToggle -Step 'Step1' -Name 'Toggle' -Label 'Toggle' } | Should -Not -Throw
        }

        It 'Should accept true default' {
            { Add-UIToggle -Step 'Step1' -Name 'Toggle' -Label 'Toggle' -Default $true } | Should -Not -Throw
        }
    }
}

Describe 'Step Configuration Logic' {
    BeforeEach {
        New-PoshUIWizard -Title 'Step Test'
    }

    Context 'Step Ordering' {
        It 'Should accept multiple steps with different orders' {
            { 
                Add-UIStep -Name 'Third' -Title 'Third' -Order 3
                Add-UIStep -Name 'First' -Title 'First' -Order 1
                Add-UIStep -Name 'Second' -Title 'Second' -Order 2
            } | Should -Not -Throw
        }
    }

    Context 'Step Properties' {
        It 'Should accept step description' {
            { Add-UIStep -Name 'Step1' -Title 'Step' -Order 1 -Description 'Step description' } | Should -Not -Throw
        }

        It 'Should accept step icon' {
            { Add-UIStep -Name 'Step1' -Title 'Step' -Order 1 -Icon '&#xE8B9;' } | Should -Not -Throw
        }
    }
}
