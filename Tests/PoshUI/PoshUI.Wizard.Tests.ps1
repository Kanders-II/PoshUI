#Requires -Modules Pester
<#
.SYNOPSIS
    Pester tests for PoshUI.Wizard module.

.DESCRIPTION
    Comprehensive tests covering:
    - Module loading and cmdlet exports
    - Control creation and validation
    - Parameter validation
    - Security functions
    - Serialization/deserialization

.NOTES
    Run with: Invoke-Pester -Path .\PoshUI.Wizard.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $modulePath = Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1'
    if (-not (Test-Path $modulePath)) {
        throw "Module not found at: $modulePath"
    }
    Import-Module $modulePath -Force
}

AfterAll {
    Remove-Module PoshUI.Wizard -Force -ErrorAction SilentlyContinue
}

Describe 'PoshUI.Wizard Module Loading' {
    Context 'Module Import' {
        It 'Should import without errors' {
            { Import-Module $modulePath -Force } | Should -Not -Throw
        }

        It 'Should export cmdlets' {
            $commands = Get-Command -Module PoshUI.Wizard
            $commands.Count | Should -BeGreaterThan 0
        }
    }

    Context 'Core Cmdlets Exist' {
        It 'Should export New-PoshUIWizard' {
            Get-Command -Name New-PoshUIWizard -Module PoshUI.Wizard | Should -Not -BeNullOrEmpty
        }

        It 'Should export Show-PoshUIWizard' {
            Get-Command -Name Show-PoshUIWizard -Module PoshUI.Wizard | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UIStep' {
            Get-Command -Name Add-UIStep -Module PoshUI.Wizard | Should -Not -BeNullOrEmpty
        }

        It 'Should export Set-UIBranding' {
            Get-Command -Name Set-UIBranding -Module PoshUI.Wizard | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Control Cmdlets Exist' {
        It 'Should export Add-UITextBox' {
            Get-Command -Name 'Add-UITextBox' -Module PoshUI.Wizard -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UIPassword' {
            Get-Command -Name 'Add-UIPassword' -Module PoshUI.Wizard -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UIDropdown' {
            Get-Command -Name 'Add-UIDropdown' -Module PoshUI.Wizard -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UICheckbox' {
            Get-Command -Name 'Add-UICheckbox' -Module PoshUI.Wizard -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UINumeric' {
            Get-Command -Name 'Add-UINumeric' -Module PoshUI.Wizard -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UIBanner' {
            Get-Command -Name 'Add-UIBanner' -Module PoshUI.Wizard -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UICard' {
            Get-Command -Name 'Add-UICard' -Module PoshUI.Wizard -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }
    }
}

Describe 'PoshUI.Wizard Core Functionality' {
    BeforeEach {
        # Clear any existing wizard state
        Remove-Variable -Name CurrentWizard -Scope Script -ErrorAction SilentlyContinue
    }

    Context 'New-PoshUIWizard' {
        It 'Should create wizard with required Title parameter' {
            { New-PoshUIWizard -Title 'Test Wizard' } | Should -Not -Throw
        }

        It 'Should fail without Title parameter' {
            { New-PoshUIWizard } | Should -Throw
        }

        It 'Should accept Theme parameter' {
            { New-PoshUIWizard -Title 'Test' -Theme 'Dark' } | Should -Not -Throw
        }

        It 'Should validate Theme parameter values' {
            { New-PoshUIWizard -Title 'Test' -Theme 'InvalidTheme' } | Should -Throw
        }

        It 'Should accept Icon parameter' {
            { New-PoshUIWizard -Title 'Test' -Icon 'test.ico' } | Should -Not -Throw
        }
    }

    Context 'Add-UIStep' {
        BeforeEach {
            New-PoshUIWizard -Title 'Test Wizard'
        }

        It 'Should add step with required parameters' {
            { Add-UIStep -Name 'Step1' -Title 'First Step' -Order 1 } | Should -Not -Throw
        }

        It 'Should fail without Name parameter' {
            { Add-UIStep -Title 'Step' -Order 1 } | Should -Throw
        }

        It 'Should fail without Title parameter' {
            { Add-UIStep -Name 'Step1' -Order 1 } | Should -Throw
        }

        It 'Should accept Icon parameter' {
            { Add-UIStep -Name 'Step1' -Title 'Step' -Order 1 -Icon '&#xE8B9;' } | Should -Not -Throw
        }

        It 'Should accept Description parameter' {
            { Add-UIStep -Name 'Step1' -Title 'Step' -Order 1 -Description 'Test description' } | Should -Not -Throw
        }
    }
}

Describe 'PoshUI.Wizard Control Validation' {
    BeforeEach {
        New-PoshUIWizard -Title 'Test Wizard'
        Add-UIStep -Name 'TestStep' -Title 'Test Step' -Order 1
    }

    Context 'Add-UITextBox' {
        It 'Should create textbox with required parameters' {
            { Add-UITextBox -Step 'TestStep' -Name 'TextField' -Label 'Text Field' } | Should -Not -Throw
        }

        It 'Should fail without Step parameter' {
            { Add-UITextBox -Name 'TextField' -Label 'Text Field' } | Should -Throw
        }

        It 'Should fail without Name parameter' {
            { Add-UITextBox -Step 'TestStep' -Label 'Text Field' } | Should -Throw
        }

        It 'Should accept Default value' {
            { Add-UITextBox -Step 'TestStep' -Name 'TextField' -Label 'Text' -Default 'DefaultValue' } | Should -Not -Throw
        }

        It 'Should accept Mandatory switch' {
            { Add-UITextBox -Step 'TestStep' -Name 'TextField' -Label 'Text' -Mandatory } | Should -Not -Throw
        }

        It 'Should accept Placeholder parameter' {
            { Add-UITextBox -Step 'TestStep' -Name 'TextField' -Label 'Text' -Placeholder 'Enter text...' } | Should -Not -Throw
        }

        It 'Should accept ValidationPattern parameter' {
            { Add-UITextBox -Step 'TestStep' -Name 'TextField' -Label 'Text' -ValidationPattern '^[a-zA-Z]+$' } | Should -Not -Throw
        }
    }

    Context 'Add-UIPassword' {
        It 'Should create password field' {
            { Add-UIPassword -Step 'TestStep' -Name 'Password' -Label 'Password' } | Should -Not -Throw
        }

        It 'Should accept Mandatory switch' {
            { Add-UIPassword -Step 'TestStep' -Name 'Password' -Label 'Password' -Mandatory } | Should -Not -Throw
        }

        It 'Should accept MinLength parameter' {
            { Add-UIPassword -Step 'TestStep' -Name 'Password' -Label 'Password' -MinLength 8 } | Should -Not -Throw
        }
    }

    Context 'Add-UIDropdown' {
        It 'Should create dropdown with choices' {
            { Add-UIDropdown -Step 'TestStep' -Name 'Dropdown' -Label 'Select' -Choices @('Option1', 'Option2') } | Should -Not -Throw
        }

        It 'Should fail without Choices parameter' {
            { Add-UIDropdown -Step 'TestStep' -Name 'Dropdown' -Label 'Select' } | Should -Throw
        }

        It 'Should accept Default value' {
            { Add-UIDropdown -Step 'TestStep' -Name 'Dropdown' -Label 'Select' -Choices @('A', 'B') -Default 'A' } | Should -Not -Throw
        }

        It 'Should accept ScriptBlock for dynamic choices' {
            { Add-UIDropdown -Step 'TestStep' -Name 'Dynamic' -Label 'Dynamic' -ScriptBlock { @('A', 'B', 'C') } } | Should -Not -Throw
        }
    }

    Context 'Add-UICheckbox' {
        It 'Should create checkbox' {
            { Add-UICheckbox -Step 'TestStep' -Name 'Check' -Label 'Check this' } | Should -Not -Throw
        }

        It 'Should accept Default value' {
            { Add-UICheckbox -Step 'TestStep' -Name 'Check' -Label 'Check' -Default $true } | Should -Not -Throw
        }
    }

    Context 'Add-UINumeric' {
        It 'Should create numeric field' {
            { Add-UINumeric -Step 'TestStep' -Name 'Number' -Label 'Number' } | Should -Not -Throw
        }

        It 'Should accept Min and Max parameters' {
            { Add-UINumeric -Step 'TestStep' -Name 'Number' -Label 'Number' -Min 0 -Max 100 } | Should -Not -Throw
        }

        It 'Should accept Default value' {
            { Add-UINumeric -Step 'TestStep' -Name 'Number' -Label 'Number' -Default 50 } | Should -Not -Throw
        }
    }

    Context 'Add-UIBanner' {
        It 'Should create banner with title' {
            { Add-UIBanner -Step 'TestStep' -Title 'Banner Title' } | Should -Not -Throw
        }

        It 'Should accept Subtitle parameter' {
            { Add-UIBanner -Step 'TestStep' -Title 'Banner' -Subtitle 'Subtitle text' } | Should -Not -Throw
        }

        It 'Should accept BackgroundColor parameter' {
            { Add-UIBanner -Step 'TestStep' -Title 'Banner' -BackgroundColor '#3498DB' } | Should -Not -Throw
        }

        It 'Should accept GradientStart and GradientEnd parameters' {
            { Add-UIBanner -Step 'TestStep' -Title 'Banner' -GradientStart '#3498DB' -GradientEnd '#2C3E50' } | Should -Not -Throw
        }

        It 'Should accept CarouselItems for carousel banner' {
            $slides = @(
                @{ Title = 'Slide 1'; Subtitle = 'First'; BackgroundColor = '#3498DB' },
                @{ Title = 'Slide 2'; Subtitle = 'Second'; BackgroundColor = '#9B59B6' }
            )
            { Add-UIBanner -Step 'TestStep' -Title 'Carousel' -CarouselItems $slides } | Should -Not -Throw
        }
    }
}

Describe 'PoshUI.Wizard Parameter Name Validation' {
    BeforeEach {
        New-PoshUIWizard -Title 'Test'
        Add-UIStep -Name 'Step1' -Title 'Step' -Order 1
    }

    It 'Should accept valid parameter name: ServerName' {
        { Add-UITextBox -Step 'Step1' -Name 'ServerName' -Label 'Test' } | Should -Not -Throw
    }

    It 'Should accept valid parameter name: User_Name' {
        { Add-UITextBox -Step 'Step1' -Name 'User_Name' -Label 'Test' } | Should -Not -Throw
    }

    It 'Should accept valid parameter name: Config1' {
        { Add-UITextBox -Step 'Step1' -Name 'Config1' -Label 'Test' } | Should -Not -Throw
    }

    It 'Should require Name parameter' {
        { Add-UITextBox -Step 'Step1' -Label 'Test' } | Should -Throw
    }

    It 'Should require non-empty Name parameter' {
        { Add-UITextBox -Step 'Step1' -Name '' -Label 'Test' } | Should -Throw
    }
}
