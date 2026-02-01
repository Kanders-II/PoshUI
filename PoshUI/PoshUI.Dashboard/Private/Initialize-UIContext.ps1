# Initialize-UIContext.ps1 - Internal context management functions

function Initialize-UIContext {
    <#
    .SYNOPSIS
    Initializes the UI context and validates the environment.

    .DESCRIPTION
    Internal function that sets up the UI execution environment,
    validates dependencies, and prepares for UI execution.

    .PARAMETER Wizard
    The UIDefinition object to initialize context for.

    .OUTPUTS
    Hashtable containing the initialized context.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [object]$Wizard  # Use [object] to avoid type conflicts on module reload
    )

    Write-Verbose "Initializing UI context for: $($Wizard.Title)"

    try {
        # Validate UI definition
        $validation = $Wizard.Validate()
        if (-not $validation.IsValid) {
            $errorMessage = "UI validation failed:`n" + ($validation.Errors -join "`n")
            throw $errorMessage
        }

        # Log warnings if any
        if ($validation.Warnings.Count -gt 0) {
            foreach ($warning in $validation.Warnings) {
                Write-Warning $warning
            }
        }

        # Check for PoshUI executable in multiple locations
        # Structure: PoshUI/PoshUI.Dashboard/ - parent is PoshUI/ where bin/ resides
        $parentRoot = Split-Path $script:ModuleRoot -Parent
        $workspaceRoot = Split-Path $parentRoot -Parent
        $possiblePaths = @(
            # Parent's bin folder (PoshUI/bin/) - main location
            (Join-Path $parentRoot "bin\PoshUI.exe"),
            # Module's own bin folder (for standalone deployment)
            (Join-Path $script:ModuleRoot "bin\PoshUI.exe"),
            # Launcher output folders (development)
            (Join-Path $workspaceRoot "Launcher\bin\Release\PoshUI.exe"),
            (Join-Path $workspaceRoot "Launcher\bin\Debug\PoshUI.exe")
        )
        
        $exePath = $null
        foreach ($path in $possiblePaths) {
            if (Test-Path $path) {
                $exePath = $path
                break
            }
        }
        
        if (-not $exePath) {
            $searchedPaths = $possiblePaths -join "`n  "
            throw "PoshUI executable not found. Searched:`n  $searchedPaths"
        }

        # Create context object
        $context = @{
            Wizard = $Wizard
            ExecutablePath = $exePath
            TempDirectory = [System.IO.Path]::GetTempPath()
            StartTime = Get-Date
            SessionId = [System.Guid]::NewGuid().ToString()
        }

        Write-Verbose "UI context initialized successfully"
        Write-Verbose "Executable: $($context.ExecutablePath)"
        Write-Verbose "Session ID: $($context.SessionId)"

        return $context
    }
    catch {
        Write-Error "Failed to initialize UI context: $($_.Exception.Message)"
        throw
    }
}

function Clear-UIContext {
    <#
    .SYNOPSIS
    Cleans up UI context and temporary resources.

    .DESCRIPTION
    Internal function that performs cleanup after UI execution,
    removing temporary files and resetting context variables.

    .PARAMETER Context
    The context object to clean up.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [hashtable]$Context
    )

    Write-Verbose "Cleaning up UI context"

    try {
        if ($Context -and $Context.ContainsKey('TempFiles')) {
            foreach ($tempFile in $Context.TempFiles) {
                # Remove temp file securely
                if (Test-Path $tempFile) {
                    Remove-Item -Path $tempFile -Force -ErrorAction SilentlyContinue
                    Write-Verbose "Removed temp file: $tempFile"
                }
            }
        }

        # Clear module-level variables if needed
        if ($Context -and $Context.ContainsKey('SessionId')) {
            Write-Verbose "Cleaned up session: $($Context.SessionId)"
        }
    }
    catch {
        Write-Warning "Error during context cleanup: $($_.Exception.Message)"
    }
}

