function Clear-PoshUIFileState {
    <#
    .SYNOPSIS
    Removes temporary files and logs created by PoshUI.

    .DESCRIPTION
    Cleans up temporary script files, connection info files, and optionally log files
    created by PoshUI in $env:TEMP\PoshUI and the module's logs directory.

    .PARAMETER IncludeLogs
    Also remove log files. By default, logs are retained.

    .PARAMETER LogRetentionDays
    Number of days to retain log files. Default is 30 days.

    .PARAMETER Force
    Skip confirmation prompts and retry locked files.

    .EXAMPLE
    Clear-PoshUIFileState

    Removes temporary files but keeps logs.

    .EXAMPLE
    Clear-PoshUIFileState -IncludeLogs -LogRetentionDays 7

    Removes temp files and logs older than 7 days.

    .NOTES
    Locations cleaned:
    - $env:TEMP\PoshUI\*.ps1 (generated scripts)
    - $env:TEMP\PoshUI\*_connection.json (connection info)
    - $PSScriptRoot\..\..\logs\PoshUI-*.log (if IncludeLogs)
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter()]
        [switch]$IncludeLogs,

        [Parameter()]
        [ValidateRange(1, 365)]
        [int]$LogRetentionDays = 30,

        [Parameter()]
        [switch]$Force
    )

    begin {
        Write-Verbose "Starting file state cleanup"
        $filesRemoved = 0
        $logFilesRemoved = 0
        $tempPath = Join-Path $env:TEMP "PoshUI"
    }

    process {
        try {
            # Clean temporary script files
            if (Test-Path $tempPath) {
                Write-Verbose "Cleaning temporary files in $tempPath"

                # Get all temp files
                $tempFiles = Get-ChildItem -Path $tempPath -File -ErrorAction SilentlyContinue

                foreach ($file in $tempFiles) {
                    if ($Force -or $PSCmdlet.ShouldProcess($file.Name, "Remove temporary file")) {
                        try {
                            Remove-Item -Path $file.FullName -Force -ErrorAction Stop
                            $filesRemoved++
                            Write-Verbose "Removed: $($file.Name)"
                        }
                        catch {
                            # File might be locked, try again with retry logic
                            if ($Force) {
                                Start-Sleep -Milliseconds 100
                                try {
                                    Remove-Item -Path $file.FullName -Force -ErrorAction Stop
                                    $filesRemoved++
                                    Write-Verbose "Removed (retry): $($file.Name)"
                                }
                                catch {
                                    Write-Warning "Could not remove locked file: $($file.Name) - $_"
                                }
                            }
                            else {
                                Write-Warning "Could not remove file: $($file.Name) - $_"
                            }
                        }
                    }
                }

                # Try to remove the directory if empty
                $remainingFiles = Get-ChildItem -Path $tempPath -ErrorAction SilentlyContinue
                if ($remainingFiles.Count -eq 0) {
                    try {
                        Remove-Item -Path $tempPath -Force -ErrorAction Stop
                        Write-Verbose "Removed empty temp directory"
                    }
                    catch {
                        Write-Verbose "Could not remove temp directory: $_"
                    }
                }
            }
            else {
                Write-Verbose "No temporary directory found at $tempPath"
            }

            # Clean log files if requested
            if ($IncludeLogs) {
                $logPath = Join-Path $PSScriptRoot "..\..\logs"
                if (Test-Path $logPath) {
                    Write-Verbose "Cleaning log files in $logPath"

                    $cutoffDate = (Get-Date).AddDays(-$LogRetentionDays)
                    $logFiles = Get-ChildItem -Path $logPath -Filter "PoshUI-*.log" -ErrorAction SilentlyContinue |
                        Where-Object { $_.LastWriteTime -lt $cutoffDate }

                    foreach ($logFile in $logFiles) {
                        if ($Force -or $PSCmdlet.ShouldProcess($logFile.Name, "Remove log file")) {
                            try {
                                Remove-Item -Path $logFile.FullName -Force -ErrorAction Stop
                                $logFilesRemoved++
                                Write-Verbose "Removed log: $($logFile.Name)"
                            }
                            catch {
                                Write-Warning "Could not remove log file: $($logFile.Name) - $_"
                            }
                        }
                    }
                }
                else {
                    Write-Verbose "No log directory found at $logPath"
                }
            }

            return @{
                FilesRemoved = $filesRemoved
                LogFilesRemoved = $logFilesRemoved
            }
        }
        catch {
            Write-Error "Failed to clean file state: $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "File cleanup completed: $filesRemoved files, $logFilesRemoved logs"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Clear-PoshWizardFileState' -Value 'Clear-PoshUIFileState'
