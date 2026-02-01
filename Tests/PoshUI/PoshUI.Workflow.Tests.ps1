#Requires -Modules Pester
<#
.SYNOPSIS
    Pester tests for PoshUI.Workflow module.

.DESCRIPTION
    Comprehensive tests covering:
    - Module loading and cmdlet exports
    - Workflow creation and task execution
    - Task types and approval gates

.NOTES
    Run with: Invoke-Pester -Path .\PoshUI.Workflow.Tests.ps1 -Output Detailed
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

Describe 'PoshUI.Workflow Module Loading' {
    Context 'Module Import' {
        It 'Should import without errors' {
            { Import-Module $modulePath -Force } | Should -Not -Throw
        }

        It 'Should export cmdlets' {
            $commands = Get-Command -Module PoshUI.Workflow
            $commands.Count | Should -BeGreaterThan 0
        }
    }

    Context 'Core Cmdlets Exist' {
        It 'Should export New-PoshUIWorkflow' {
            Get-Command -Name New-PoshUIWorkflow -Module PoshUI.Workflow | Should -Not -BeNullOrEmpty
        }

        It 'Should export Show-PoshUIWorkflow' {
            Get-Command -Name Show-PoshUIWorkflow -Module PoshUI.Workflow | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UIStep' {
            Get-Command -Name Add-UIStep -Module PoshUI.Workflow | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UIWorkflowTask' {
            Get-Command -Name Add-UIWorkflowTask -Module PoshUI.Workflow | Should -Not -BeNullOrEmpty
        }
    }
}

Describe 'PoshUI.Workflow Core Functionality' {
    BeforeEach {
        Remove-Variable -Name CurrentWorkflow -Scope Script -ErrorAction SilentlyContinue
    }

    Context 'New-PoshUIWorkflow' {
        It 'Should create workflow with Title parameter' {
            { New-PoshUIWorkflow -Title 'Test Workflow' } | Should -Not -Throw
        }

        It 'Should fail without Title parameter' {
            { New-PoshUIWorkflow } | Should -Throw
        }

        It 'Should accept Description parameter' {
            { New-PoshUIWorkflow -Title 'Test' -Description 'Test workflow description' } | Should -Not -Throw
        }

        It 'Should accept Theme parameter' {
            { New-PoshUIWorkflow -Title 'Test' -Theme 'Dark' } | Should -Not -Throw
        }
    }

    Context 'Add-UIStep for Workflow' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Test Workflow'
        }

        It 'Should add workflow step' {
            { Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow } | Should -Not -Throw
        }

        It 'Should add welcome step' {
            { Add-UIStep -Name 'Welcome' -Title 'Welcome' -Order 1 } | Should -Not -Throw
        }
    }
}

Describe 'PoshUI.Workflow Task Management' {
    BeforeEach {
        New-PoshUIWorkflow -Title 'Test Workflow'
        Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
    }

    Context 'Add-UIWorkflowTask' {
        It 'Should add task with ScriptBlock' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'First Task' -Order 1 -ScriptBlock { Write-Output 'Test' } } | Should -Not -Throw
        }

        It 'Should fail without Step parameter' {
            { Add-UIWorkflowTask -Name 'Task1' -Title 'Task' -Order 1 -ScriptBlock { } } | Should -Throw
        }

        It 'Should fail without Name parameter' {
            { Add-UIWorkflowTask -Step 'Execution' -Title 'Task' -Order 1 -ScriptBlock { } } | Should -Throw
        }

        It 'Should accept Description parameter' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' -Order 1 -Description 'Task description' -ScriptBlock { } } | Should -Not -Throw
        }

        It 'Should accept ScriptBlock parameter' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' -Order 1 -ScriptBlock { Write-Host 'Test' } } | Should -Not -Throw
        }
    }

    Context 'Task Types' {
        It 'Should accept Normal task type' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' -Order 1 -TaskType Normal -ScriptBlock { } } | Should -Not -Throw
        }

        It 'Should accept ApprovalGate task type' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Approval' -Title 'Approval Gate' -Order 1 -TaskType ApprovalGate -ApprovalMessage 'Please approve' } | Should -Not -Throw
        }
    }

    Context 'Error Handling Options' {
        It 'Should accept OnError Stop' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' -Order 1 -ScriptBlock { } -OnError Stop } | Should -Not -Throw
        }

        It 'Should accept OnError Continue' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' -Order 1 -ScriptBlock { } -OnError Continue } | Should -Not -Throw
        }
    }
}

Describe 'PoshUI.Workflow Banner Support' {
    BeforeEach {
        New-PoshUIWorkflow -Title 'Test Workflow'
        Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
    }

    Context 'Add-UIBanner in Workflow' {
        It 'Should add banner to workflow step' {
            { Add-UIBanner -Step 'Execution' -Title 'Workflow Banner' } | Should -Not -Throw
        }

        It 'Should accept gradient parameters' {
            { Add-UIBanner -Step 'Execution' -Title 'Banner' -GradientStart '#3498DB' -GradientEnd '#2C3E50' } | Should -Not -Throw
        }

        It 'Should accept carousel items' {
            $slides = @(
                @{ Title = 'Step 1'; BackgroundColor = '#3498DB' },
                @{ Title = 'Step 2'; BackgroundColor = '#E74C3C' }
            )
            { Add-UIBanner -Step 'Execution' -Title 'Carousel' -CarouselItems $slides -AutoRotate } | Should -Not -Throw
        }
    }
}
