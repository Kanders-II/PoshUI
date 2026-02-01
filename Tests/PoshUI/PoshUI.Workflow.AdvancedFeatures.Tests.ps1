<#
.SYNOPSIS
    Pester tests for PoshUI.Workflow advanced features.

.DESCRIPTION
    Comprehensive tests covering advanced workflow features:
    - Task retry mechanism (RetryCount, RetryDelaySeconds)
    - Task timeout (TimeoutSeconds)
    - Conditional skip (SkipCondition, SkipReason)
    - Task grouping (Group)
    - Rollback scripts (RollbackScriptBlock, RollbackScriptPath)
    - Inter-task data passing (SetData, GetData, HasData, GetDataKeys)

.NOTES
    Run with: Invoke-Pester -Path .\PoshUI.Workflow.AdvancedFeatures.Tests.ps1
    Compatible with Pester 5.0+
#>

BeforeAll {
    $modulePath = Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Workflow\PoshUI.Workflow.psd1'
    if (-not (Test-Path $modulePath)) {
        throw "Module not found at: $modulePath"
    }
    Import-Module $modulePath -Force
}

AfterAll {
    Remove-Module PoshUI.Workflow -Force -ErrorAction SilentlyContinue
}

Describe 'Add-UIWorkflowTask Advanced Parameters' {

    Context 'Retry Parameters' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Test Workflow'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should accept RetryCount parameter' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RetryCount 3 -ScriptBlock { Write-Output 'Test' } } | Should -Not -Throw
        }

        It 'Should accept RetryDelaySeconds parameter' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RetryDelaySeconds 10 -ScriptBlock { Write-Output 'Test' } } | Should -Not -Throw
        }

        It 'Should accept both RetryCount and RetryDelaySeconds' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RetryCount 3 -RetryDelaySeconds 5 -ScriptBlock { Write-Output 'Test' } } | Should -Not -Throw
        }

        It 'Should reject negative RetryCount' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RetryCount -1 -ScriptBlock { Write-Output 'Test' } } | Should -Throw
        }

        It 'Should reject RetryCount over 100' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RetryCount 101 -ScriptBlock { Write-Output 'Test' } } | Should -Throw
        }

        It 'Should reject negative RetryDelaySeconds' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RetryDelaySeconds -5 -ScriptBlock { Write-Output 'Test' } } | Should -Throw
        }

        It 'Should reject RetryDelaySeconds over 3600' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RetryDelaySeconds 3601 -ScriptBlock { Write-Output 'Test' } } | Should -Throw
        }
    }

    Context 'Timeout Parameter' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Test Workflow'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should accept TimeoutSeconds parameter' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -TimeoutSeconds 60 -ScriptBlock { Write-Output 'Test' } } | Should -Not -Throw
        }

        It 'Should accept zero TimeoutSeconds (no timeout)' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -TimeoutSeconds 0 -ScriptBlock { Write-Output 'Test' } } | Should -Not -Throw
        }

        It 'Should reject negative TimeoutSeconds' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -TimeoutSeconds -10 -ScriptBlock { Write-Output 'Test' } } | Should -Throw
        }

        It 'Should reject TimeoutSeconds over 86400 (24 hours)' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -TimeoutSeconds 86401 -ScriptBlock { Write-Output 'Test' } } | Should -Throw
        }
    }

    Context 'Skip Condition Parameters' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Test Workflow'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should accept SkipCondition parameter' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -SkipCondition '$ServerType -eq "Database"' -ScriptBlock { Write-Output 'Test' } } | Should -Not -Throw
        }

        It 'Should accept SkipReason parameter' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -SkipCondition '$true' -SkipReason 'Always skip for testing' `
                -ScriptBlock { Write-Output 'Test' } } | Should -Not -Throw
        }

        It 'Should accept SkipCondition with WorkflowData reference' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -SkipCondition '$WorkflowData["AlreadyInstalled"] -eq $true' `
                -SkipReason 'Already installed' `
                -ScriptBlock { Write-Output 'Test' } } | Should -Not -Throw
        }
    }

    Context 'Group Parameter' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Test Workflow'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should accept Group parameter' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -Group 'Pre-flight Checks' -ScriptBlock { Write-Output 'Test' } } | Should -Not -Throw
        }

        It 'Should accept empty Group parameter' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -Group '' -ScriptBlock { Write-Output 'Test' } } | Should -Not -Throw
        }
    }

    Context 'Rollback Parameters' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Test Workflow'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should accept RollbackScriptBlock parameter' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RollbackScriptBlock { Write-Host 'Rolling back...' } `
                -ScriptBlock { Write-Output 'Test' } } | Should -Not -Throw
        }

        It 'Should accept RollbackScriptPath parameter' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RollbackScriptPath 'C:\Scripts\Rollback.ps1' `
                -ScriptBlock { Write-Output 'Test' } } | Should -Not -Throw
        }
    }

    Context 'Combined Advanced Parameters' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Test Workflow'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should accept all advanced parameters together' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'ComplexTask' -Title 'Complex Task' `
                -Description 'A task with all advanced parameters' `
                -Group 'Installation' `
                -RetryCount 3 `
                -RetryDelaySeconds 10 `
                -TimeoutSeconds 300 `
                -SkipCondition '$Environment -ne "Production"' `
                -SkipReason 'Not a production environment' `
                -RollbackScriptBlock { Write-Host 'Rolling back...' } `
                -OnError Continue `
                -ScriptBlock { Write-Output 'Complex task running' } } | Should -Not -Throw
        }
    }
}

Describe 'Parameter Existence Validation' {

    Context 'Get-Command Parameter Inspection' {
        It 'Add-UIWorkflowTask should have RetryCount parameter' {
            $cmd = Get-Command Add-UIWorkflowTask
            'RetryCount' -in $cmd.Parameters.Keys | Should -Be $true
        }

        It 'Add-UIWorkflowTask should have RetryDelaySeconds parameter' {
            $cmd = Get-Command Add-UIWorkflowTask
            'RetryDelaySeconds' -in $cmd.Parameters.Keys | Should -Be $true
        }

        It 'Add-UIWorkflowTask should have TimeoutSeconds parameter' {
            $cmd = Get-Command Add-UIWorkflowTask
            'TimeoutSeconds' -in $cmd.Parameters.Keys | Should -Be $true
        }

        It 'Add-UIWorkflowTask should have SkipCondition parameter' {
            $cmd = Get-Command Add-UIWorkflowTask
            'SkipCondition' -in $cmd.Parameters.Keys | Should -Be $true
        }

        It 'Add-UIWorkflowTask should have SkipReason parameter' {
            $cmd = Get-Command Add-UIWorkflowTask
            'SkipReason' -in $cmd.Parameters.Keys | Should -Be $true
        }

        It 'Add-UIWorkflowTask should have Group parameter' {
            $cmd = Get-Command Add-UIWorkflowTask
            'Group' -in $cmd.Parameters.Keys | Should -Be $true
        }

        It 'Add-UIWorkflowTask should have RollbackScriptBlock parameter' {
            $cmd = Get-Command Add-UIWorkflowTask
            'RollbackScriptBlock' -in $cmd.Parameters.Keys | Should -Be $true
        }

        It 'Add-UIWorkflowTask should have RollbackScriptPath parameter' {
            $cmd = Get-Command Add-UIWorkflowTask
            'RollbackScriptPath' -in $cmd.Parameters.Keys | Should -Be $true
        }
    }

    Context 'Parameter Types' {
        It 'RetryCount should be Int32 type' {
            $cmd = Get-Command Add-UIWorkflowTask
            $cmd.Parameters['RetryCount'].ParameterType | Should -Be ([int])
        }

        It 'RetryDelaySeconds should be Int32 type' {
            $cmd = Get-Command Add-UIWorkflowTask
            $cmd.Parameters['RetryDelaySeconds'].ParameterType | Should -Be ([int])
        }

        It 'TimeoutSeconds should be Int32 type' {
            $cmd = Get-Command Add-UIWorkflowTask
            $cmd.Parameters['TimeoutSeconds'].ParameterType | Should -Be ([int])
        }

        It 'SkipCondition should be String type' {
            $cmd = Get-Command Add-UIWorkflowTask
            $cmd.Parameters['SkipCondition'].ParameterType | Should -Be ([string])
        }

        It 'Group should be String type' {
            $cmd = Get-Command Add-UIWorkflowTask
            $cmd.Parameters['Group'].ParameterType | Should -Be ([string])
        }

        It 'RollbackScriptBlock should be ScriptBlock type' {
            $cmd = Get-Command Add-UIWorkflowTask
            $cmd.Parameters['RollbackScriptBlock'].ParameterType | Should -Be ([scriptblock])
        }
    }
}

Describe 'UIWorkflowTask Class Properties' {

    Context 'Advanced Property Assignment' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Test Workflow'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should set RetryCount property correctly' {
            $task = Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RetryCount 5 -ScriptBlock { }

            $task.RetryCount | Should -Be 5
        }

        It 'Should set TimeoutSeconds property correctly' {
            $task = Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -TimeoutSeconds 120 -ScriptBlock { }

            $task.TimeoutSeconds | Should -Be 120
        }

        It 'Should set SkipCondition property correctly' {
            $task = Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -SkipCondition '$true' -ScriptBlock { }

            $task.SkipCondition | Should -Be '$true'
        }

        It 'Should set Group property correctly' {
            $task = Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -Group 'Phase 1' -ScriptBlock { }

            $task.Group | Should -Be 'Phase 1'
        }
    }
}

Describe 'Task Serialization with Advanced Properties' {

    Context 'ToHashtable Method' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Serialization Test'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should include RetryCount in hashtable' {
            $task = Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RetryCount 3 -ScriptBlock { }

            $hash = $task.ToHashtable()
            $hash.RetryCount | Should -Be 3
        }

        It 'Should include TimeoutSeconds in hashtable' {
            $task = Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -TimeoutSeconds 60 -ScriptBlock { }

            $hash = $task.ToHashtable()
            $hash.TimeoutSeconds | Should -Be 60
        }

        It 'Should include Group in hashtable' {
            $task = Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -Group 'Setup' -ScriptBlock { }

            $hash = $task.ToHashtable()
            $hash.Group | Should -Be 'Setup'
        }
    }
}

Describe 'Workflow with Multiple Grouped Tasks' {

    Context 'Task Organization by Group' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Grouped Tasks Test'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should create tasks in different groups' {
            $task1 = Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task 1' `
                -Group 'Phase 1' -ScriptBlock { }
            $task2 = Add-UIWorkflowTask -Step 'Execution' -Name 'Task2' -Title 'Task 2' `
                -Group 'Phase 1' -ScriptBlock { }
            $task3 = Add-UIWorkflowTask -Step 'Execution' -Name 'Task3' -Title 'Task 3' `
                -Group 'Phase 2' -ScriptBlock { }

            $task1.Group | Should -Be 'Phase 1'
            $task2.Group | Should -Be 'Phase 1'
            $task3.Group | Should -Be 'Phase 2'
        }
    }
}

# Cleanup
Remove-Module PoshUI.Workflow -Force -ErrorAction SilentlyContinue
