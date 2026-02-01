function Get-UIConfiguration {
    <#
    .SYNOPSIS
    Retrieves global configuration options for PoshUI.

    .DESCRIPTION
    Gets current global settings that apply to all PoshUI instances.
    Settings are stored in the registry and persist across sessions.

    .PARAMETER Name
    Specific configuration setting name to retrieve. If not specified, returns all settings.

    .EXAMPLE
    Get-UIConfiguration

    Returns all configuration settings as a hashtable.

    .EXAMPLE
    Get-UIConfiguration -Name 'DefaultTheme'

    Returns only the DefaultTheme setting value.

    .OUTPUTS
    Hashtable of configuration settings or specific setting value.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [ValidateSet('DefaultTheme', 'DefaultTemplate', 'DefaultGridColumns', 'EnableTelemetry',
                     'LogLevel', 'LogPath', 'AutoCleanupEnabled', 'AutoCleanupHours', 'EnableEventHistory')]
        [string]$Name
    )

    begin {
        $registryPath = "HKCU:\Software\PoshUI\Configuration"
    }

    process {
        try {
            # Default configuration values
            $defaults = @{
                DefaultTheme = 'Auto'
                DefaultTemplate = 'Wizard'
                DefaultGridColumns = 3
                EnableTelemetry = $false
                LogLevel = 'Warning'
                LogPath = Join-Path $env:TEMP 'PoshUI\Logs'
                AutoCleanupEnabled = $true
                AutoCleanupHours = 24
                EnableEventHistory = $false
            }

            # Get current configuration from registry
            $config = @{}
            
            if (Test-Path $registryPath) {
                $registryValues = Get-ItemProperty -Path $registryPath -ErrorAction SilentlyContinue
                
                foreach ($key in $defaults.Keys) {
                    if ($null -ne $registryValues.$key) {
                        # Convert boolean values stored as integers
                        if ($key -in @('EnableTelemetry', 'AutoCleanupEnabled', 'EnableEventHistory')) {
                            $config[$key] = [bool]$registryValues.$key
                        }
                        else {
                            $config[$key] = $registryValues.$key
                        }
                    }
                    else {
                        $config[$key] = $defaults[$key]
                    }
                }
            }
            else {
                $config = $defaults
            }

            # Return specific setting or all settings
            if ($PSBoundParameters.ContainsKey('Name')) {
                return $config[$Name]
            }
            else {
                return $config
            }
        }
        catch {
            Write-Error "Failed to get configuration: $($_.Exception.Message)"
            throw
        }
    }
}
