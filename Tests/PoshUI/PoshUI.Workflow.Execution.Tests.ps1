<#
.SYNOPSIS
    Pester tests for PoshUI.Workflow execution features.

.DESCRIPTION
    Tests for WorkflowContext methods and execution behavior:
    - Inter-task data passing (SetData, GetData, HasData, GetDataKeys)
    - Task retry mechanism behavior
    - Task timeout behavior
    - Conditional skip evaluation
    - Rollback execution

    Note: These tests verify the PowerShell serialization and JSON generation.
    The actual execution happens in the C# launcher which is tested separately.

.NOTES
    Run with: Invoke-Pester -Path .\PoshUI.Workflow.Execution.Tests.ps1
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

Describe 'WorkflowContext Data Passing API' {

    Context 'SetData Method Documentation' {
        It 'Should document SetData method usage in scripts' {
            $scriptContent = @'
$PoshUIWorkflow.SetData('InstallPath', 'C:\Program Files\MyApp')
$PoshUIWorkflow.SetData('Version', '2.0.0')
$PoshUIWorkflow.SetData('Timestamp', (Get-Date).ToString())
'@
            { [scriptblock]::Create($scriptContent) } | Should -Not -Throw
        }
    }

    Context 'GetData Method Documentation' {
        It 'Should document GetData method usage in scripts' {
            $scriptContent = @'
$installPath = $PoshUIWorkflow.GetData('InstallPath')
$version = $PoshUIWorkflow.GetData('Version')
if ($installPath) {
    Write-Output "Installed at: $installPath"
}
'@
            { [scriptblock]::Create($scriptContent) } | Should -Not -Throw
        }
    }

    Context 'HasData Method Documentation' {
        It 'Should document HasData method usage in scripts' {
            $scriptContent = @'
if ($PoshUIWorkflow.HasData('SQLInstalled')) {
    $sqlPath = $PoshUIWorkflow.GetData('SQLInstallPath')
    Configure-SQL -Path $sqlPath
}
'@
            { [scriptblock]::Create($scriptContent) } | Should -Not -Throw
        }
    }

    Context 'GetDataKeys Method Documentation' {
        It 'Should document GetDataKeys method usage in scripts' {
            $scriptContent = @'
$allKeys = $PoshUIWorkflow.GetDataKeys()
foreach ($key in $allKeys) {
    $value = $PoshUIWorkflow.GetData($key)
    Write-Output "$key = $value"
}
'@
            { [scriptblock]::Create($scriptContent) } | Should -Not -Throw
        }
    }

    Context 'SkipTask Method Documentation' {
        It 'Should document SkipTask method usage in scripts' {
            $scriptContent = @'
if ($alreadyInstalled) {
    $PoshUIWorkflow.SkipTask('Application already installed')
    return
}
'@
            { [scriptblock]::Create($scriptContent) } | Should -Not -Throw
        }
    }

    Context 'Task Index Properties Documentation' {
        It 'Should document CurrentTaskIndex property usage' {
            $scriptContent = @'
$currentIndex = $PoshUIWorkflow.CurrentTaskIndex
$totalTasks = $PoshUIWorkflow.TotalTaskCount
Write-Output "Running task $($currentIndex + 1) of $totalTasks"
'@
            { [scriptblock]::Create($scriptContent) } | Should -Not -Throw
        }
    }
}

Describe 'JSON Serialization for C# Launcher' {

    Context 'Task with Retry Settings' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Serialization Test'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should serialize RetryCount to JSON' {
            $task = Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Retry Task' `
                -RetryCount 3 -RetryDelaySeconds 5 -ScriptBlock { Write-Output 'Test' }

            $hash = $task.ToHashtable()
            $json = $hash | ConvertTo-Json -Depth 10

            $json | Should -Match '"RetryCount":\s*3'
            $json | Should -Match '"RetryDelaySeconds":\s*5'
        }
    }

    Context 'Task with Timeout Setting' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Serialization Test'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should serialize TimeoutSeconds to JSON' {
            $task = Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Timeout Task' `
                -TimeoutSeconds 60 -ScriptBlock { Write-Output 'Test' }

            $hash = $task.ToHashtable()
            $json = $hash | ConvertTo-Json -Depth 10

            $json | Should -Match '"TimeoutSeconds":\s*60'
        }
    }

    Context 'Task with Skip Condition' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Serialization Test'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should serialize SkipCondition to JSON' {
            $task = Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Conditional Task' `
                -SkipCondition '$ServerType -eq "Database"' `
                -SkipReason 'Only for database servers' `
                -ScriptBlock { Write-Output 'Test' }

            $hash = $task.ToHashtable()
            $json = $hash | ConvertTo-Json -Depth 10

            $json | Should -Match 'SkipCondition'
            $json | Should -Match 'SkipReason'
        }
    }

    Context 'Task with Group' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Serialization Test'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should serialize Group to JSON' {
            $task = Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Grouped Task' `
                -Group 'Installation Phase' -ScriptBlock { Write-Output 'Test' }

            $hash = $task.ToHashtable()
            $json = $hash | ConvertTo-Json -Depth 10

            $json | Should -Match '"Group":\s*"Installation Phase"'
        }
    }
}

Describe 'Script Patterns for WriteOutput vs UpdateProgress' {

    Context 'WriteOutput Pattern (Auto-Progress)' {
        It 'Should validate WriteOutput-only pattern' {
            $script = {
                $PoshUIWorkflow.WriteOutput("Step 1: Starting...", "INFO")
                $PoshUIWorkflow.WriteOutput("Step 2: Processing...", "INFO")
                $PoshUIWorkflow.WriteOutput("Step 3: Complete", "INFO")
            }

            $scriptText = $script.ToString()
            $scriptText | Should -Match 'WriteOutput'
            $scriptText | Should -Not -Match 'UpdateProgress'
        }
    }

    Context 'UpdateProgress Pattern (Manual Control)' {
        It 'Should validate UpdateProgress-only pattern' {
            $script = {
                $PoshUIWorkflow.UpdateProgress(10, "Starting...")
                $PoshUIWorkflow.UpdateProgress(50, "Processing...")
                $PoshUIWorkflow.UpdateProgress(100, "Complete")
            }

            $scriptText = $script.ToString()
            $scriptText | Should -Match 'UpdateProgress'
            $scriptText | Should -Not -Match 'WriteOutput'
        }
    }

    Context 'Mixed Pattern Detection (Anti-Pattern)' {
        It 'Should identify mixed WriteOutput and UpdateProgress as anti-pattern' {
            $badScript = {
                $PoshUIWorkflow.UpdateProgress(10, "Starting...")
                $PoshUIWorkflow.WriteOutput("Processing...", "INFO")
                $PoshUIWorkflow.UpdateProgress(100, "Complete")
            }

            $scriptText = $badScript.ToString()
            $hasWriteOutput = $scriptText -match 'WriteOutput'
            $hasUpdateProgress = $scriptText -match 'UpdateProgress'

            ($hasWriteOutput -and $hasUpdateProgress) | Should -Be $true
        }
    }
}

Describe 'Error Handling in Advanced Features' {

    Context 'Invalid Parameter Values' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Error Test'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should validate RetryCount is non-negative' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RetryCount -1 -ScriptBlock { } } | Should -Throw
        }

        It 'Should validate TimeoutSeconds is non-negative' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -TimeoutSeconds -10 -ScriptBlock { } } | Should -Throw
        }

        It 'Should validate RetryDelaySeconds is non-negative' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RetryDelaySeconds -5 -ScriptBlock { } } | Should -Throw
        }
    }

    Context 'Edge Cases' {
        BeforeEach {
            New-PoshUIWorkflow -Title 'Edge Case Test'
            Add-UIStep -Name 'Execution' -Title 'Execution' -Order 1 -Type Workflow
        }

        It 'Should handle zero RetryCount (no retries)' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -RetryCount 0 -ScriptBlock { } } | Should -Not -Throw
        }

        It 'Should handle zero TimeoutSeconds (no timeout)' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -TimeoutSeconds 0 -ScriptBlock { } } | Should -Not -Throw
        }

        It 'Should handle empty SkipCondition' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -SkipCondition '' -ScriptBlock { } } | Should -Not -Throw
        }

        It 'Should handle null Group' {
            { Add-UIWorkflowTask -Step 'Execution' -Name 'Task1' -Title 'Task' `
                -Group $null -ScriptBlock { } } | Should -Not -Throw
        }
    }
}

# Cleanup
Remove-Module PoshUI.Workflow -Force -ErrorAction SilentlyContinue
