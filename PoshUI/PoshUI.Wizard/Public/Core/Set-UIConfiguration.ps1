function Set-UIConfiguration {
    <#
    .SYNOPSIS
    Sets global configuration options for PoshUI.

    .DESCRIPTION
    Configures global settings that apply to all PoshUI instances.
    Settings are stored in the registry and persist across sessions.

    .PARAMETER DefaultTheme
    Default theme for new UI instances. Valid values: 'Light', 'Dark', 'Auto'.

    .PARAMETER DefaultTemplate
    Default template for new UI instances. Valid values: 'Wizard', 'Dashboard'.

    .PARAMETER DefaultGridColumns
    Default number of grid columns for Dashboard template (1-6).

    .PARAMETER EnableTelemetry
    Enable or disable telemetry collection.

    .PARAMETER LogLevel
    Logging level. Valid values: 'None', 'Error', 'Warning', 'Info', 'Verbose'.

    .PARAMETER LogPath
    Path where log files should be stored.

    .PARAMETER AutoCleanupEnabled
    Enable automatic cleanup of stale sessions.

    .PARAMETER AutoCleanupHours
    Number of hours before a session is considered stale (default: 24).

    .PARAMETER EnableEventHistory
    Enable event history tracking for debugging.

    .EXAMPLE
    Set-UIConfiguration -DefaultTheme 'Dark' -LogLevel 'Info'

    Sets the default theme to dark and enables info-level logging.

    .EXAMPLE
    Set-UIConfiguration -AutoCleanupEnabled $true -AutoCleanupHours 48

    Enables auto-cleanup with 48-hour threshold.
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter()]
        [ValidateSet('Light', 'Dark', 'Auto')]
        [string]$DefaultTheme,

        [Parameter()]
        [ValidateSet('Wizard', 'Dashboard')]
        [string]$DefaultTemplate,

        [Parameter()]
        [ValidateRange(1, 6)]
        [int]$DefaultGridColumns,

        [Parameter()]
        [bool]$EnableTelemetry,

        [Parameter()]
        [ValidateSet('None', 'Error', 'Warning', 'Info', 'Verbose')]
        [string]$LogLevel,

        [Parameter()]
        [string]$LogPath,

        [Parameter()]
        [bool]$AutoCleanupEnabled,

        [Parameter()]
        [ValidateRange(1, 168)]
        [int]$AutoCleanupHours,

        [Parameter()]
        [bool]$EnableEventHistory
    )

    begin {
        $registryPath = "HKCU:\Software\PoshUI\Configuration"
        
        if (-not (Test-Path $registryPath)) {
            New-Item -Path $registryPath -Force | Out-Null
        }
    }

    process {
        try {
            $changes = @()

            if ($PSBoundParameters.ContainsKey('DefaultTheme')) {
                if ($PSCmdlet.ShouldProcess("DefaultTheme", "Set to $DefaultTheme")) {
                    Set-ItemProperty -Path $registryPath -Name 'DefaultTheme' -Value $DefaultTheme
                    $changes += "DefaultTheme = $DefaultTheme"
                }
            }

            if ($PSBoundParameters.ContainsKey('DefaultTemplate')) {
                if ($PSCmdlet.ShouldProcess("DefaultTemplate", "Set to $DefaultTemplate")) {
                    Set-ItemProperty -Path $registryPath -Name 'DefaultTemplate' -Value $DefaultTemplate
                    $changes += "DefaultTemplate = $DefaultTemplate"
                }
            }

            if ($PSBoundParameters.ContainsKey('DefaultGridColumns')) {
                if ($PSCmdlet.ShouldProcess("DefaultGridColumns", "Set to $DefaultGridColumns")) {
                    Set-ItemProperty -Path $registryPath -Name 'DefaultGridColumns' -Value $DefaultGridColumns
                    $changes += "DefaultGridColumns = $DefaultGridColumns"
                }
            }

            if ($PSBoundParameters.ContainsKey('EnableTelemetry')) {
                if ($PSCmdlet.ShouldProcess("EnableTelemetry", "Set to $EnableTelemetry")) {
                    Set-ItemProperty -Path $registryPath -Name 'EnableTelemetry' -Value ([int]$EnableTelemetry)
                    $changes += "EnableTelemetry = $EnableTelemetry"
                }
            }

            if ($PSBoundParameters.ContainsKey('LogLevel')) {
                if ($PSCmdlet.ShouldProcess("LogLevel", "Set to $LogLevel")) {
                    Set-ItemProperty -Path $registryPath -Name 'LogLevel' -Value $LogLevel
                    $changes += "LogLevel = $LogLevel"
                }
            }

            if ($PSBoundParameters.ContainsKey('LogPath')) {
                if ($PSCmdlet.ShouldProcess("LogPath", "Set to $LogPath")) {
                    Set-ItemProperty -Path $registryPath -Name 'LogPath' -Value $LogPath
                    $changes += "LogPath = $LogPath"
                }
            }

            if ($PSBoundParameters.ContainsKey('AutoCleanupEnabled')) {
                if ($PSCmdlet.ShouldProcess("AutoCleanupEnabled", "Set to $AutoCleanupEnabled")) {
                    Set-ItemProperty -Path $registryPath -Name 'AutoCleanupEnabled' -Value ([int]$AutoCleanupEnabled)
                    $changes += "AutoCleanupEnabled = $AutoCleanupEnabled"
                }
            }

            if ($PSBoundParameters.ContainsKey('AutoCleanupHours')) {
                if ($PSCmdlet.ShouldProcess("AutoCleanupHours", "Set to $AutoCleanupHours")) {
                    Set-ItemProperty -Path $registryPath -Name 'AutoCleanupHours' -Value $AutoCleanupHours
                    $changes += "AutoCleanupHours = $AutoCleanupHours"
                }
            }

            if ($PSBoundParameters.ContainsKey('EnableEventHistory')) {
                if ($PSCmdlet.ShouldProcess("EnableEventHistory", "Set to $EnableEventHistory")) {
                    Set-ItemProperty -Path $registryPath -Name 'EnableEventHistory' -Value ([int]$EnableEventHistory)
                    $changes += "EnableEventHistory = $EnableEventHistory"
                    
                    # Update UIEvents class if loaded
                    if ([UIEvents]) {
                        [UIEvents]::SetHistoryEnabled($EnableEventHistory)
                    }
                }
            }

            if ($changes.Count -gt 0) {
                Write-Verbose "Configuration updated: $($changes -join ', ')"
                Write-Host "[OK] Configuration updated successfully" -ForegroundColor Green
            }
            else {
                Write-Warning "No configuration changes specified"
            }
        }
        catch {
            Write-Error "Failed to set configuration: $($_.Exception.Message)"
            throw
        }
    }
}
