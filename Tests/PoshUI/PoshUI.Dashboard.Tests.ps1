#Requires -Modules Pester
<#
.SYNOPSIS
    Pester tests for PoshUI.Dashboard module.

.DESCRIPTION
    Comprehensive tests covering:
    - Module loading and cmdlet exports
    - Dashboard creation and card types
    - Banner functionality
    - Refresh and navigation

.NOTES
    Run with: Invoke-Pester -Path .\PoshUI.Dashboard.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $modulePath = Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Dashboard\PoshUI.Dashboard.psd1'
    if (-not (Test-Path $modulePath)) {
        throw "Module not found at: $modulePath"
    }
    Import-Module $modulePath -Force
}

AfterAll {
    Remove-Module PoshUI.Dashboard -Force -ErrorAction SilentlyContinue
}

Describe 'PoshUI.Dashboard Module Loading' {
    Context 'Module Import' {
        It 'Should import without errors' {
            { Import-Module $modulePath -Force } | Should -Not -Throw
        }

        It 'Should export cmdlets' {
            $commands = Get-Command -Module PoshUI.Dashboard
            $commands.Count | Should -BeGreaterThan 0
        }
    }

    Context 'Core Cmdlets Exist' {
        It 'Should export New-PoshUIDashboard' {
            Get-Command -Name New-PoshUIDashboard -Module PoshUI.Dashboard | Should -Not -BeNullOrEmpty
        }

        It 'Should export Show-PoshUIDashboard' {
            Get-Command -Name Show-PoshUIDashboard -Module PoshUI.Dashboard | Should -Not -BeNullOrEmpty
        }

        It 'Should export Get-PoshUIDashboard' {
            Get-Command -Name Get-PoshUIDashboard -Module PoshUI.Dashboard | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UIStep' {
            Get-Command -Name Add-UIStep -Module PoshUI.Dashboard | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Card Cmdlets Exist' {
        It 'Should export Add-UIBanner' {
            Get-Command -Name 'Add-UIBanner' -Module PoshUI.Dashboard -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UIMetricCard' {
            Get-Command -Name 'Add-UIMetricCard' -Module PoshUI.Dashboard -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UIChartCard' {
            Get-Command -Name 'Add-UIChartCard' -Module PoshUI.Dashboard -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UITableCard' {
            Get-Command -Name 'Add-UITableCard' -Module PoshUI.Dashboard -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UIScriptCard' {
            Get-Command -Name 'Add-UIScriptCard' -Module PoshUI.Dashboard -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It 'Should export Add-UICard' {
            Get-Command -Name 'Add-UICard' -Module PoshUI.Dashboard -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }
    }
}

Describe 'PoshUI.Dashboard Core Functionality' {
    BeforeEach {
        Remove-Variable -Name CurrentDashboard -Scope Script -ErrorAction SilentlyContinue
    }

    Context 'New-PoshUIDashboard' {
        It 'Should create dashboard with Title parameter' {
            { New-PoshUIDashboard -Title 'Test Dashboard' } | Should -Not -Throw
        }

        It 'Should fail without Title parameter' {
            { New-PoshUIDashboard } | Should -Throw
        }

        It 'Should accept Theme parameter' {
            { New-PoshUIDashboard -Title 'Test' -Theme 'Dark' } | Should -Not -Throw
        }

        It 'Should accept Description parameter' {
            { New-PoshUIDashboard -Title 'Test' -Description 'Test description' } | Should -Not -Throw
        }
    }

    Context 'Add-UIStep for Dashboard' {
        BeforeEach {
            New-PoshUIDashboard -Title 'Test Dashboard'
        }

        It 'Should add dashboard step' {
            { Add-UIStep -Name 'Page1' -Title 'First Page' -Order 1 } | Should -Not -Throw
        }

        It 'Should accept Type parameter' {
            { Add-UIStep -Name 'Page1' -Title 'First Page' -Order 1 -Type 'Dashboard' } | Should -Not -Throw
        }
    }
}

Describe 'PoshUI.Dashboard Card Controls' {
    BeforeEach {
        New-PoshUIDashboard -Title 'Test Dashboard'
        Add-UIStep -Name 'TestPage' -Title 'Test Page' -Order 1
    }

    Context 'Add-UIBanner' {
        It 'Should create banner with required parameters' {
            { Add-UIBanner -Step 'TestPage' -Name 'Banner1' -Title 'Dashboard Banner' } | Should -Not -Throw
        }

        It 'Should accept Subtitle' {
            { Add-UIBanner -Step 'TestPage' -Name 'Banner1' -Title 'Banner' -Subtitle 'Subtitle' } | Should -Not -Throw
        }

        It 'Should accept BackgroundColor' {
            { Add-UIBanner -Step 'TestPage' -Name 'Banner1' -Title 'Banner' -BackgroundColor '#2196F3' } | Should -Not -Throw
        }

        It 'Should accept Height parameter' {
            { Add-UIBanner -Step 'TestPage' -Name 'Banner1' -Title 'Banner' -Height 180 } | Should -Not -Throw
        }

        It 'Should create carousel banner' {
            $slides = @(
                @{ Title = 'Slide 1'; BackgroundColor = '#3498DB' },
                @{ Title = 'Slide 2'; BackgroundColor = '#E74C3C' }
            )
            { Add-UIBanner -Step 'TestPage' -Name 'Carousel' -Title 'Carousel' -CarouselSlides $slides -AutoRotate $true } | Should -Not -Throw
        }
    }

    Context 'Add-UIScriptCard' {
        It 'Should create script card with ScriptBlock' {
            { Add-UIScriptCard -Step 'TestPage' -Name 'Script1' -Title 'Status' -ScriptBlock { "System OK" } } | Should -Not -Throw
        }

        It 'Should accept Description parameter' {
            { Add-UIScriptCard -Step 'TestPage' -Name 'Script1' -Title 'Status' -Description 'Shows status' -ScriptBlock { Get-Date } } | Should -Not -Throw
        }
    }
}

Describe 'PoshUI.Dashboard Banner Image Features' {
    BeforeEach {
        New-PoshUIDashboard -Title 'Test Dashboard'
        Add-UIStep -Name 'TestPage' -Title 'Test Page' -Order 1
    }

    Context 'Overlay Image Properties' {
        It 'Should accept OverlayImagePath' {
            { Add-UIBanner -Step 'TestPage' -Name 'Banner1' -Title 'Banner' -OverlayImagePath 'C:\logo.png' } | Should -Not -Throw
        }

        It 'Should accept OverlayImageOpacity' {
            { Add-UIBanner -Step 'TestPage' -Name 'Banner1' -Title 'Banner' -OverlayImagePath 'C:\logo.png' -OverlayImageOpacity 0.8 } | Should -Not -Throw
        }

        It 'Should accept OverlayImageSize' {
            { Add-UIBanner -Step 'TestPage' -Name 'Banner1' -Title 'Banner' -OverlayImagePath 'C:\logo.png' -OverlayImageSize 100 } | Should -Not -Throw
        }

        It 'Should accept OverlayPosition' {
            { Add-UIBanner -Step 'TestPage' -Name 'Banner1' -Title 'Banner' -OverlayImagePath 'C:\logo.png' -OverlayPosition 'Right' } | Should -Not -Throw
        }
    }

    Context 'Background Image Properties' {
        It 'Should accept BackgroundImagePath' {
            { Add-UIBanner -Step 'TestPage' -Name 'Banner1' -Title 'Banner' -BackgroundImagePath 'C:\bg.png' } | Should -Not -Throw
        }

        It 'Should accept BackgroundImageOpacity' {
            { Add-UIBanner -Step 'TestPage' -Name 'Banner1' -Title 'Banner' -BackgroundImagePath 'C:\bg.png' -BackgroundImageOpacity 0.5 } | Should -Not -Throw
        }
    }

    Context 'Gradient Properties' {
        It 'Should accept GradientStart and GradientEnd' {
            { Add-UIBanner -Step 'TestPage' -Name 'Banner1' -Title 'Banner' -GradientStart '#3498DB' -GradientEnd '#2C3E50' } | Should -Not -Throw
        }

        It 'Should accept GradientAngle' {
            { Add-UIBanner -Step 'TestPage' -Name 'Banner1' -Title 'Banner' -GradientStart '#3498DB' -GradientEnd '#2C3E50' -GradientAngle 45 } | Should -Not -Throw
        }
    }

    Context 'BannerStyle Presets' {
        It 'Should accept BannerStyle parameter' {
            { Add-UIBanner -Step 'TestPage' -Title 'Banner' -BannerStyle 'Gradient' } | Should -Not -Throw
        }

        It 'Should accept all preset styles' {
            $styles = @('Default', 'Gradient', 'Image', 'Minimal', 'Hero', 'Accent')
            foreach ($style in $styles) {
                { Add-UIBanner -Step 'TestPage' -Title 'Banner' -BannerStyle $style } | Should -Not -Throw
            }
        }

        It 'Should accept BannerConfig parameter' {
            { Add-UIBanner -Step 'TestPage' -Title 'Banner' -BannerStyle 'Hero' -BannerConfig @{ Height = 350 } } | Should -Not -Throw
        }

        It 'Should apply preset defaults' {
            $banner = Add-UIBanner -Step 'TestPage' -Title 'Banner' -BannerStyle 'Gradient'
            $banner | Should -Not -BeNullOrEmpty
        }
    }
}

Describe 'PoshUI.Dashboard Type-Specific Card Cmdlets' {
    BeforeEach {
        New-PoshUIDashboard -Title 'Test Dashboard'
        Add-UIStep -Name 'TestPage' -Title 'Test Page' -Order 1
    }

    Context 'Add-UIMetricCard' {
        It 'Should create metric card with static value' {
            { Add-UIMetricCard -Step 'TestPage' -Name 'CPU' -Title 'CPU Usage' -Value 75 -Unit '%' } | Should -Not -Throw
        }

        It 'Should create metric card with ScriptBlock value' {
            { Add-UIMetricCard -Step 'TestPage' -Name 'Memory' -Title 'Memory' -Value { 50 } -Unit 'GB' } | Should -Not -Throw
        }

        It 'Should accept Trend parameter' {
            { Add-UIMetricCard -Step 'TestPage' -Name 'CPU' -Title 'CPU' -Value 75 -Trend 'up' } | Should -Not -Throw
        }

        It 'Should accept Target parameter' {
            { Add-UIMetricCard -Step 'TestPage' -Name 'CPU' -Title 'CPU' -Value 75 -Target 80 } | Should -Not -Throw
        }
    }

    Context 'Add-UIChartCard' {
        It 'Should create chart card with static data' {
            $data = @(@{Label='Jan'; Value=100}, @{Label='Feb'; Value=150})
            { Add-UIChartCard -Step 'TestPage' -Name 'Sales' -Title 'Sales' -ChartType 'Line' -Data $data } | Should -Not -Throw
        }

        It 'Should create chart card with ScriptBlock data' {
            { Add-UIChartCard -Step 'TestPage' -Name 'Processes' -Title 'Top Processes' -ChartType 'Bar' -Data { @(@{Label='A'; Value=10}) } } | Should -Not -Throw
        }

        It 'Should accept all ChartType values' {
            $types = @('Line', 'Bar', 'Area', 'Pie')
            foreach ($type in $types) {
                { Add-UIChartCard -Step 'TestPage' -Name "Chart_$type" -Title $type -ChartType $type -Data @(@{Label='X'; Value=1}) } | Should -Not -Throw
            }
        }
    }

    Context 'Add-UITableCard' {
        It 'Should create table card with static data' {
            $data = @(@{Name='Item1'; Value=100}, @{Name='Item2'; Value=200})
            { Add-UITableCard -Step 'TestPage' -Name 'Table' -Title 'Data Table' -Data $data } | Should -Not -Throw
        }

        It 'Should create table card with ScriptBlock data' {
            { Add-UITableCard -Step 'TestPage' -Name 'Processes' -Title 'Processes' -Data { Get-Process | Select-Object -First 5 Name, Id } } | Should -Not -Throw
        }
    }

    Context 'Add-UICard (InfoCard)' {
        It 'Should create info card' {
            { Add-UICard -Step 'TestPage' -Name 'Info' -Title 'Information' -Content 'Test content' } | Should -Not -Throw
        }

        It 'Should accept Category parameter' {
            { Add-UICard -Step 'TestPage' -Name 'Info' -Title 'Info' -Content 'Content' -Category 'Help' } | Should -Not -Throw
        }
    }
}

Describe 'PoshUI.Dashboard Get-PoshUIDashboard' {
    BeforeEach {
        New-PoshUIDashboard -Title 'Test Dashboard'
        Add-UIStep -Name 'Page1' -Title 'Page 1' -Order 1
        Add-UIMetricCard -Step 'Page1' -Name 'CPU' -Title 'CPU' -Value 50 -Unit '%'
    }

    Context 'Basic Retrieval' {
        It 'Should retrieve dashboard summary' {
            $dashboard = Get-PoshUIDashboard
            $dashboard | Should -Not -BeNullOrEmpty
            $dashboard.Title | Should -Be 'Test Dashboard'
        }

        It 'Should show step count' {
            $dashboard = Get-PoshUIDashboard
            $dashboard.TotalSteps | Should -Be 1
        }

        It 'Should show control count' {
            $dashboard = Get-PoshUIDashboard
            $dashboard.Steps[0].TotalControls | Should -Be 1
        }
    }

    Context 'IncludeProperties Switch' {
        It 'Should include properties when requested' {
            $dashboard = Get-PoshUIDashboard -IncludeProperties
            $dashboard.Steps[0].Controls[0].Properties | Should -Not -BeNullOrEmpty
        }
    }

    Context 'StepName Filter' {
        It 'Should filter to specific step' {
            $dashboard = Get-PoshUIDashboard -StepName 'Page1'
            $dashboard.Steps.Count | Should -Be 1
            $dashboard.Steps[0].Name | Should -Be 'Page1'
        }
    }

    Context 'AsJson Switch' {
        It 'Should return JSON string' {
            $json = Get-PoshUIDashboard -AsJson
            $json | Should -BeOfType [string]
            { $json | ConvertFrom-Json } | Should -Not -Throw
        }
    }
}

Describe 'PoshUI.Dashboard ScriptBlock Error Handling' {
    BeforeEach {
        New-PoshUIDashboard -Title 'Test Dashboard'
        Add-UIStep -Name 'TestPage' -Title 'Test Page' -Order 1
    }

    Context 'MetricCard ScriptBlock Errors' {
        It 'Should handle invalid ScriptBlock gracefully' {
            { Add-UIMetricCard -Step 'TestPage' -Name 'Bad' -Title 'Bad' -Value { Get-NonExistentCommand } -ErrorAction SilentlyContinue } | Should -Not -Throw
        }

        It 'Should provide detailed error message' {
            $error.Clear()
            Add-UIMetricCard -Step 'TestPage' -Name 'Bad' -Title 'Bad' -Value { throw 'Test error' } -ErrorAction SilentlyContinue
            $error[0].Exception.Message | Should -Match 'Failed to execute Value ScriptBlock'
        }
    }

    Context 'ChartCard ScriptBlock Errors' {
        It 'Should handle invalid ScriptBlock gracefully' {
            { Add-UIChartCard -Step 'TestPage' -Name 'Bad' -Title 'Bad' -ChartType 'Line' -Data { Get-NonExistentCommand } -ErrorAction SilentlyContinue } | Should -Not -Throw
        }
    }

    Context 'TableCard ScriptBlock Errors' {
        It 'Should handle invalid ScriptBlock gracefully' {
            { Add-UITableCard -Step 'TestPage' -Name 'Bad' -Title 'Bad' -Data { Get-NonExistentCommand } -ErrorAction SilentlyContinue } | Should -Not -Throw
        }
    }
}
