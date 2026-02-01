#Requires -Modules Pester
<#
.SYNOPSIS
    Serialization tests for PoshUI modules.

.DESCRIPTION
    Tests covering JSON serialization and deserialization:
    - Control configuration serialization
    - Carousel slides serialization
    - Complex nested object handling
    - Round-trip serialization integrity

.NOTES
    Run with: Invoke-Pester -Path .\PoshUI.Serialization.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $wizardPath = Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1'
    Import-Module $wizardPath -Force
}

AfterAll {
    Remove-Module PoshUI.Wizard -Force -ErrorAction SilentlyContinue
}

Describe 'JSON Serialization' {
    Context 'Basic Type Serialization' {
        It 'Should serialize string values correctly' {
            $obj = @{ Name = 'TestValue'; Description = 'Test description with special chars: <>&"' }
            $json = $obj | ConvertTo-Json -Compress
            $json | Should -Not -BeNullOrEmpty
            $restored = $json | ConvertFrom-Json
            $restored.Name | Should -Be 'TestValue'
        }

        It 'Should serialize numeric values correctly' {
            $obj = @{ Integer = 42; Float = 3.14; Negative = -100 }
            $json = $obj | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.Integer | Should -Be 42
            $restored.Float | Should -Be 3.14
            $restored.Negative | Should -Be -100
        }

        It 'Should serialize boolean values correctly' {
            $obj = @{ True = $true; False = $false }
            $json = $obj | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.True | Should -Be $true
            $restored.False | Should -Be $false
        }

        It 'Should serialize null values correctly' {
            $obj = @{ NullValue = $null; EmptyString = '' }
            $json = $obj | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.NullValue | Should -BeNullOrEmpty
        }
    }

    Context 'Array Serialization' {
        It 'Should serialize string arrays' {
            $obj = @{ Choices = @('Option1', 'Option2', 'Option3') }
            $json = $obj | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.Choices.Count | Should -Be 3
            $restored.Choices[0] | Should -Be 'Option1'
        }

        It 'Should serialize mixed arrays' {
            $obj = @{ Mixed = @('String', 42, $true, $null) }
            $json = $obj | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.Mixed.Count | Should -Be 4
        }

        It 'Should serialize empty arrays' {
            $obj = @{ Empty = @() }
            $json = $obj | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.Empty | Should -BeNullOrEmpty
        }
    }

    Context 'Nested Object Serialization' {
        It 'Should serialize nested hashtables' {
            $obj = @{
                Outer = @{
                    Inner = @{
                        Value = 'Deep'
                    }
                }
            }
            $json = $obj | ConvertTo-Json -Depth 10 -Compress
            $restored = $json | ConvertFrom-Json
            $restored.Outer.Inner.Value | Should -Be 'Deep'
        }

        It 'Should handle deep nesting' {
            $obj = @{ Level1 = @{ Level2 = @{ Level3 = @{ Level4 = @{ Level5 = 'DeepValue' } } } } }
            $json = $obj | ConvertTo-Json -Depth 10 -Compress
            $restored = $json | ConvertFrom-Json
            $restored.Level1.Level2.Level3.Level4.Level5 | Should -Be 'DeepValue'
        }
    }
}

Describe 'Carousel Slides Serialization' {
    Context 'Carousel Slide Structure' {
        It 'Should serialize carousel slides as JSON string' {
            $slides = @(
                @{ Title = 'Slide 1'; Subtitle = 'First'; BackgroundColor = '#3498DB' },
                @{ Title = 'Slide 2'; Subtitle = 'Second'; BackgroundColor = '#9B59B6' },
                @{ Title = 'Slide 3'; Subtitle = 'Third'; BackgroundColor = '#2ECC71' }
            )
            
            $json = $slides | ConvertTo-Json -Depth 5 -Compress
            $json | Should -Not -BeNullOrEmpty
            
            $restored = $json | ConvertFrom-Json
            $restored.Count | Should -Be 3
            $restored[0].Title | Should -Be 'Slide 1'
            $restored[1].BackgroundColor | Should -Be '#9B59B6'
        }

        It 'Should handle slides with all properties' {
            $slides = @(
                @{
                    Title = 'Complete Slide'
                    Subtitle = 'All properties'
                    BackgroundColor = '#3498DB'
                    TitleColor = '#FFFFFF'
                    SubtitleColor = '#E0E0E0'
                    IconGlyph = '&#xE8B9;'
                    IconSize = 48
                    IconColor = '#FFFFFF'
                }
            )
            
            $json = $slides | ConvertTo-Json -Depth 5 -Compress
            $restored = $json | ConvertFrom-Json
            $restored[0].IconGlyph | Should -Be '&#xE8B9;'
            $restored[0].IconSize | Should -Be 48
        }

        It 'Should handle slides with missing optional properties' {
            $slides = @(
                @{ Title = 'Minimal Slide' }
            )
            
            $json = $slides | ConvertTo-Json -Depth 5 -Compress
            $restored = $json | ConvertFrom-Json
            $restored[0].Title | Should -Be 'Minimal Slide'
            $restored[0].Subtitle | Should -BeNullOrEmpty
        }
    }
}

Describe 'Control Configuration Serialization' {
    Context 'TextBox Control' {
        It 'Should serialize textbox configuration' {
            $control = @{
                Type = 'TextBox'
                Name = 'ServerName'
                Label = 'Server Name'
                Default = 'SERVER01'
                Mandatory = $true
                Placeholder = 'Enter server name'
                ValidationPattern = '^[A-Z0-9]+$'
                ValidationMessage = 'Must be uppercase alphanumeric'
            }
            
            $json = $control | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.Type | Should -Be 'TextBox'
            $restored.Mandatory | Should -Be $true
            $restored.ValidationPattern | Should -Be '^[A-Z0-9]+$'
        }
    }

    Context 'Dropdown Control' {
        It 'Should serialize dropdown with choices' {
            $control = @{
                Type = 'Dropdown'
                Name = 'Environment'
                Label = 'Environment'
                Choices = @('Development', 'Staging', 'Production')
                Default = 'Development'
            }
            
            $json = $control | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.Choices.Count | Should -Be 3
            $restored.Default | Should -Be 'Development'
        }

        It 'Should serialize dropdown with ScriptBlock reference' {
            $control = @{
                Type = 'Dropdown'
                Name = 'DynamicField'
                Label = 'Dynamic'
                DataSourceScriptBlock = 'param($Env) switch($Env) { "Dev" { @("A","B") } default { @() } }'
                Dependencies = @('Environment')
            }
            
            $json = $control | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.DataSourceScriptBlock | Should -Not -BeNullOrEmpty
            $restored.Dependencies.Count | Should -Be 1
        }
    }

    Context 'Banner Control' {
        It 'Should serialize banner with all image properties' {
            $banner = @{
                Type = 'Banner'
                Title = 'Welcome'
                Subtitle = 'Get started'
                BackgroundColor = '#3498DB'
                GradientStart = '#2C3E50'
                GradientEnd = '#3498DB'
                GradientAngle = 135
                OverlayImagePath = 'C:\logo.png'
                OverlayImageOpacity = 0.8
                OverlayImageSize = 80
                OverlayPosition = 'Right'
                Height = 160
            }
            
            $json = $banner | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.GradientAngle | Should -Be 135
            $restored.OverlayImageOpacity | Should -Be 0.8
            $restored.OverlayPosition | Should -Be 'Right'
        }
    }
}

Describe 'Special Character Handling' {
    Context 'Unicode Characters' {
        It 'Should handle Unicode in strings' {
            $obj = @{ Text = 'Test with Unicode: 日本語 中文 العربية' }
            $json = $obj | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.Text | Should -Match '日本語'
        }

        It 'Should handle emoji characters' {
            $obj = @{ Emoji = 'Status: ✅ ❌ ⚠️ 🔄' }
            $json = $obj | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.Emoji | Should -Match '✅'
        }
    }

    Context 'Escape Sequences' {
        It 'Should handle newlines' {
            $obj = @{ MultiLine = "Line1`nLine2`nLine3" }
            $json = $obj | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.MultiLine | Should -Match 'Line1'
        }

        It 'Should handle tabs' {
            $obj = @{ Tabbed = "Col1`tCol2`tCol3" }
            $json = $obj | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.Tabbed | Should -Not -BeNullOrEmpty
        }

        It 'Should handle quotes' {
            $obj = @{ Quoted = 'He said "Hello" and ''Goodbye''' }
            $json = $obj | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.Quoted | Should -Match 'Hello'
        }

        It 'Should handle backslashes' {
            $obj = @{ Path = 'C:\Users\Admin\Documents' }
            $json = $obj | ConvertTo-Json -Compress
            $restored = $json | ConvertFrom-Json
            $restored.Path | Should -Be 'C:\Users\Admin\Documents'
        }
    }
}
