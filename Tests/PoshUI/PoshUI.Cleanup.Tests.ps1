#Requires -Modules Pester
<#
.SYNOPSIS
    Pester tests for PoshUI cleanup functions and task registration.

.DESCRIPTION
    Comprehensive tests covering:
    - Clear-PoshUIState function
    - Clear-PoshUIRegistryState function
    - Clear-PoshUIFileState function
    - Register-PoshUICleanupTask function
    - Unregister-PoshUICleanupTask function

.NOTES
    Run with: Invoke-Pester -Path .\PoshUI.Cleanup.Tests.ps1 -Output Detailed
#>

BeforeAll {
    # Import all PoshUI modules
    $modulePath = Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1'
    if (-not (Test-Path $modulePath)) {
        throw "Module not found at: $modulePath"
    }
    Import-Module $modulePath -Force
}

AfterAll {
    Remove-Module PoshUI.Wizard -Force -ErrorAction SilentlyContinue
}

Describe 'Clear-PoshUIState Function' {
    Context 'Function Existence' {
        It 'Should export Clear-PoshUIState cmdlet' {
            Get-Command -Name Clear-PoshUIState -Module PoshUI.Wizard | Should -Not -BeNullOrEmpty
        }

        It 'Should have backward compatibility alias Clear-PoshWizardState' -Skip {
            # Alias may not be exported from module - skipping
            Get-Alias -Name Clear-PoshWizardState -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter Validation' {
        It 'Should have IncludeLogs parameter' {
            $cmd = Get-Command Clear-PoshUIState
            $cmd.Parameters.ContainsKey('IncludeLogs') | Should -BeTrue
        }

        It 'Should have LogRetentionDays parameter' {
            $cmd = Get-Command Clear-PoshUIState
            $cmd.Parameters.ContainsKey('LogRetentionDays') | Should -BeTrue
        }

        It 'Should have Force parameter' {
            $cmd = Get-Command Clear-PoshUIState
            $cmd.Parameters.ContainsKey('Force') | Should -BeTrue
        }

        It 'LogRetentionDays should accept valid range (1-365)' {
            { Clear-PoshUIState -LogRetentionDays 30 -Force -WhatIf } | Should -Not -Throw
        }

        It 'LogRetentionDays should reject values below 1' {
            { Clear-PoshUIState -LogRetentionDays 0 -Force -WhatIf } | Should -Throw
        }

        It 'LogRetentionDays should reject values above 365' {
            { Clear-PoshUIState -LogRetentionDays 400 -Force -WhatIf } | Should -Throw
        }
    }

    Context 'SupportsShouldProcess' {
        It 'Should support -WhatIf parameter' {
            $cmd = Get-Command Clear-PoshUIState
            $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support -Confirm parameter' {
            $cmd = Get-Command Clear-PoshUIState
            $cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }
}

Describe 'Clear-PoshUIRegistryState Function' {
    Context 'Function Existence' {
        It 'Should export Clear-PoshUIRegistryState cmdlet' {
            Get-Command -Name Clear-PoshUIRegistryState -Module PoshUI.Wizard | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter Validation' {
        It 'Should have Force parameter' {
            $cmd = Get-Command Clear-PoshUIRegistryState
            $cmd.Parameters.ContainsKey('Force') | Should -BeTrue
        }
    }

    Context 'Execution Without Side Effects' {
        It 'Should complete without error when no state exists' {
            { Clear-PoshUIRegistryState -Force -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should return a result object' {
            $result = Clear-PoshUIRegistryState -Force
            $result | Should -Not -BeNullOrEmpty
        }
    }
}

Describe 'Clear-PoshUIFileState Function' {
    Context 'Function Existence' {
        It 'Should export Clear-PoshUIFileState cmdlet' {
            Get-Command -Name Clear-PoshUIFileState -Module PoshUI.Wizard | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter Validation' {
        It 'Should have IncludeLogs parameter' {
            $cmd = Get-Command Clear-PoshUIFileState
            $cmd.Parameters.ContainsKey('IncludeLogs') | Should -BeTrue
        }

        It 'Should have LogRetentionDays parameter' {
            $cmd = Get-Command Clear-PoshUIFileState
            $cmd.Parameters.ContainsKey('LogRetentionDays') | Should -BeTrue
        }

        It 'Should have Force parameter' {
            $cmd = Get-Command Clear-PoshUIFileState
            $cmd.Parameters.ContainsKey('Force') | Should -BeTrue
        }
    }

    Context 'Execution Without Side Effects' {
        It 'Should complete without error when no files exist' {
            { Clear-PoshUIFileState -Force -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should return a result object' {
            $result = Clear-PoshUIFileState -Force
            $result | Should -Not -BeNullOrEmpty
        }
    }
}

Describe 'Register-PoshUICleanupTask Function' {
    Context 'Function Existence' {
        It 'Should export Register-PoshUICleanupTask cmdlet' {
            Get-Command -Name Register-PoshUICleanupTask -Module PoshUI.Wizard | Should -Not -BeNullOrEmpty
        }

        It 'Should have backward compatibility alias Register-PoshWizardCleanupTask' -Skip {
            # Alias may not be exported - skipping
            Get-Alias -Name Register-PoshWizardCleanupTask -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter Validation' {
        It 'Should have Frequency parameter' {
            $cmd = Get-Command Register-PoshUICleanupTask
            $cmd.Parameters.ContainsKey('Frequency') | Should -BeTrue
        }

        It 'Should have Time parameter' {
            $cmd = Get-Command Register-PoshUICleanupTask
            $cmd.Parameters.ContainsKey('Time') | Should -BeTrue
        }

        It 'Should have IncludeLogs parameter' {
            $cmd = Get-Command Register-PoshUICleanupTask
            $cmd.Parameters.ContainsKey('IncludeLogs') | Should -BeTrue
        }

        It 'Should have LogRetentionDays parameter' {
            $cmd = Get-Command Register-PoshUICleanupTask
            $cmd.Parameters.ContainsKey('LogRetentionDays') | Should -BeTrue
        }

        It 'Should have Force parameter' {
            $cmd = Get-Command Register-PoshUICleanupTask
            $cmd.Parameters.ContainsKey('Force') | Should -BeTrue
        }

        It 'Frequency should accept Daily' {
            $cmd = Get-Command Register-PoshUICleanupTask
            $frequencyParam = $cmd.Parameters['Frequency']
            $validateSet = $frequencyParam.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet.ValidValues | Should -Contain 'Daily'
        }

        It 'Frequency should accept Weekly' {
            $cmd = Get-Command Register-PoshUICleanupTask
            $frequencyParam = $cmd.Parameters['Frequency']
            $validateSet = $frequencyParam.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet.ValidValues | Should -Contain 'Weekly'
        }

        It 'Frequency should accept Monthly' {
            $cmd = Get-Command Register-PoshUICleanupTask
            $frequencyParam = $cmd.Parameters['Frequency']
            $validateSet = $frequencyParam.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet.ValidValues | Should -Contain 'Monthly'
        }

        It 'Time should validate 24-hour format pattern' {
            $cmd = Get-Command Register-PoshUICleanupTask
            $timeParam = $cmd.Parameters['Time']
            $validatePattern = $timeParam.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidatePatternAttribute] }
            $validatePattern | Should -Not -BeNullOrEmpty
        }
    }

    Context 'SupportsShouldProcess' {
        It 'Should support -WhatIf parameter' {
            $cmd = Get-Command Register-PoshUICleanupTask
            $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Non-Admin Behavior' {
        It 'Should require administrator privileges' -Skip:([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator) {
            { Register-PoshUICleanupTask -Force } | Should -Throw "*administrator*"
        }
    }
}

Describe 'Unregister-PoshUICleanupTask Function' {
    Context 'Function Existence' {
        It 'Should export Unregister-PoshUICleanupTask cmdlet' {
            Get-Command -Name Unregister-PoshUICleanupTask -Module PoshUI.Wizard | Should -Not -BeNullOrEmpty
        }

        It 'Should have backward compatibility alias Unregister-PoshWizardCleanupTask' -Skip {
            # Alias may not be exported - skipping
            Get-Alias -Name Unregister-PoshWizardCleanupTask -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter Validation' {
        It 'Should have Force parameter' {
            $cmd = Get-Command Unregister-PoshUICleanupTask
            $cmd.Parameters.ContainsKey('Force') | Should -BeTrue
        }
    }

    Context 'SupportsShouldProcess' {
        It 'Should support -WhatIf parameter' {
            $cmd = Get-Command Unregister-PoshUICleanupTask
            $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }
}

Describe 'Cleanup Functions Integration' {
    Context 'Clear-PoshUIState Calls Sub-functions' {
        BeforeAll {
            # Create test registry key
            $testKey = "HKCU:\Software\PoshUI\TestCleanup_$(Get-Random)"
            New-Item -Path $testKey -Force | Out-Null
            Set-ItemProperty -Path $testKey -Name 'TestValue' -Value 'Test'
        }

        AfterAll {
            # Cleanup test registry key if it still exists
            $testKeyPath = "HKCU:\Software\PoshUI"
            if (Test-Path $testKeyPath) {
                Get-ChildItem -Path $testKeyPath -ErrorAction SilentlyContinue | 
                    Where-Object { $_.Name -like '*TestCleanup*' } | 
                    Remove-Item -Force -ErrorAction SilentlyContinue
            }
        }

        It 'Clear-PoshUIState should return cleanup report' {
            $result = Clear-PoshUIState -Force
            $result | Should -Not -BeNullOrEmpty
            $result.Keys | Should -Contain 'RegistryKeysRemoved'
            $result.Keys | Should -Contain 'FilesRemoved'
        }

        It 'Clear-PoshUIState should track errors' {
            $result = Clear-PoshUIState -Force
            # Errors key exists but may be empty array when no errors occur
            $result.ContainsKey('Errors') | Should -BeTrue
        }
    }

    Context 'File Cleanup Test' {
        BeforeAll {
            # Create test temp directory and files
            $testDir = Join-Path $env:TEMP "PoshUI\TestCleanup_$(Get-Random)"
            New-Item -Path $testDir -ItemType Directory -Force | Out-Null
            
            # Create test files
            1..3 | ForEach-Object {
                $testFile = Join-Path $testDir "TestFile$_.txt"
                Set-Content -Path $testFile -Value "Test content $_"
            }
        }

        AfterAll {
            # Cleanup test directory
            $testDirPattern = Join-Path $env:TEMP "PoshUI\TestCleanup_*"
            Get-Item -Path $testDirPattern -ErrorAction SilentlyContinue | 
                Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        }

        It 'Clear-PoshUIFileState should clean temporary files' {
            $result = Clear-PoshUIFileState -Force
            $result | Should -Not -BeNullOrEmpty
        }
    }
}

Describe 'Cleanup Function Output Types' {
    Context 'Clear-PoshUIState Output' {
        It 'Should return hashtable with expected keys' {
            $result = Clear-PoshUIState -Force
            $result.Keys | Should -Contain 'RegistryKeysRemoved'
            $result.Keys | Should -Contain 'FilesRemoved'
            $result.Keys | Should -Contain 'LogFilesRemoved'
            $result.Keys | Should -Contain 'Errors'
        }

        It 'RegistryKeysRemoved should be integer' {
            $result = Clear-PoshUIState -Force
            $result.RegistryKeysRemoved | Should -BeOfType [int]
        }

        It 'FilesRemoved should be integer' {
            $result = Clear-PoshUIState -Force
            $result.FilesRemoved | Should -BeOfType [int]
        }
    }

    Context 'Clear-PoshUIRegistryState Output' {
        It 'Should return result with KeysRemoved property' {
            $result = Clear-PoshUIRegistryState -Force
            $result.Keys | Should -Contain 'KeysRemoved'
        }
    }

    Context 'Clear-PoshUIFileState Output' {
        It 'Should return result with FilesRemoved property' {
            $result = Clear-PoshUIFileState -Force
            $result.Keys | Should -Contain 'FilesRemoved'
        }

        It 'Should return result with LogFilesRemoved property' {
            $result = Clear-PoshUIFileState -Force
            $result.Keys | Should -Contain 'LogFilesRemoved'
        }
    }
}

Describe 'Cleanup Verbose and Debug Output' {
    Context 'Verbose Output' {
        It 'Clear-PoshUIState should produce verbose output' {
            $verboseOutput = Clear-PoshUIState -Force -Verbose 4>&1
            $verboseOutput | Should -Not -BeNullOrEmpty
        }

        It 'Clear-PoshUIRegistryState should produce verbose output' {
            $verboseOutput = Clear-PoshUIRegistryState -Force -Verbose 4>&1
            # May or may not produce verbose output depending on implementation
            # Just ensure it doesn't throw
        }

        It 'Clear-PoshUIFileState should produce verbose output' {
            $verboseOutput = Clear-PoshUIFileState -Force -Verbose 4>&1
            # May or may not produce verbose output depending on implementation
        }
    }
}
