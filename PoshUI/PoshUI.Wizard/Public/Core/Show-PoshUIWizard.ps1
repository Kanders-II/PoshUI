function Show-PoshUIWizard {
    <#
    .SYNOPSIS
    Displays the Wizard UI and executes the associated script.

    .DESCRIPTION
    Serializes the current Wizard UI definition to JSON,
    launches the PoshUI executable, and returns the results.
    
    .PARAMETER ScriptBody
    Optional script block containing the logic to execute after collecting user input.
    If not provided, a default script that displays the collected parameters is used.
    
    .PARAMETER DefaultValues
    Hashtable of default values to pre-populate in the UI form.
    Keys should match the control names.

    .PARAMETER NonInteractive
    Run the UI in non-interactive mode using only the default values.
    The UI will not be displayed.

    .PARAMETER ShowConsole
    Whether to show the live execution console during script execution.
    Default is $true.

    .PARAMETER Theme
    Override the theme for this UI execution.
    Valid values are 'Light', 'Dark', or 'Auto'.
    
    .PARAMETER OutputFormat
    Format for the returned results. Valid values are 'Object', 'JSON', 'Hashtable'.
    Default is 'Object'.
    
    .EXAMPLE
    $result = Show-PoshUI

    Shows the UI with default script body and returns results.

    .EXAMPLE
    $result = Show-PoshUI -ScriptBody {
        Write-Host "Configuring server: $ServerName"
        # Perform configuration tasks
        return @{ Status = 'Success'; Message = 'Configuration completed' }
    }

    Shows the UI with custom script logic.

    .EXAMPLE
    $defaults = @{ ServerName = 'SQL01'; Environment = 'Production' }
    $result = Show-PoshUI -DefaultValues $defaults -ScriptBody $configScript

    Shows the UI with pre-populated default values.

    .OUTPUTS
    PSCustomObject containing the UI results and any return values from the script body.

    .NOTES
    This function requires that New-PoshUI has been called and at least one step has been added.
    The function generates a temporary script file that is automatically cleaned up after execution.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [scriptblock]$ScriptBody,
        
        [Parameter()]
        [hashtable]$DefaultValues = @{},
        
        [Parameter()]
        [switch]$NonInteractive,
        
        [Parameter()]
        [bool]$ShowConsole = $true,
        
        [Parameter()]
        [ValidateSet('Light', 'Dark', 'Auto')]
        [string]$Theme,
        
        [Parameter()]
        [ValidateSet('Object', 'JSON', 'Hashtable')]
        [string]$OutputFormat = 'Object',

        [Parameter()]
        [switch]$AppDebug,

        [Parameter()]
        [switch]$RequireSignedScripts
    )
    
    begin {
        Write-Verbose "Starting Show-PoshUI"

        # Ensure UI is initialized
        if (-not $script:CurrentWizard) {
            throw "No UI initialized. Call New-PoshUIWizard first."
        }

        # Ensure UI has steps
        if ($script:CurrentWizard.Steps.Count -eq 0) {
            throw "UI has no steps. Add at least one step using Add-UIStep."
        }
    }
    
    process {
        $context = $null
        $tempScriptPath = $null
        
        try {
            # Initialize context
            $context = Initialize-UIContext -Wizard $script:CurrentWizard
            
            # Override theme if specified
            if ($PSBoundParameters.ContainsKey('Theme')) {
                $script:CurrentWizard.Theme = $Theme
            }
            
            # Set script body if provided
            if ($ScriptBody) {
                $script:CurrentWizard.SetScriptBody($ScriptBody)
            }
            
            # Capture original calling script name for log file naming
            # This ensures Module API scripts use the calling script name, not the temp script name
            # Look up the call stack to find the first .ps1 file that's NOT in the module directory
            $callingScript = $null
            $callStack = Get-PSCallStack
            Write-Verbose "Examining call stack for original script..."
            foreach ($frame in $callStack) {
                Write-Verbose "  Frame: $($frame.ScriptName) (Function: $($frame.FunctionName))"
                if ($frame.ScriptName -and $frame.ScriptName -match '\.ps1$') {
                    # Skip if it's from the module directory (handle both \ and / path separators)
                    $normalizedPath = $frame.ScriptName -replace '\\','/'
                    # Check for both PoshUI and PoshUI.Wizard/PoshUI.Dashboard module paths
                    $isModulePath = $normalizedPath -match 'PoshUI(\.Wizard|\.Dashboard)?/(Public|Private|Classes)/'
                    if (-not $isModulePath) {
                        $callingScript = $frame.ScriptName
                        Write-Verbose "  --> Found calling script: $callingScript"
                        break
                    }
                }
            }
            
            if ($callingScript) {
                $originalScriptFullPath = [System.IO.Path]::GetFullPath($callingScript)
                $originalScriptName = [System.IO.Path]::GetFileNameWithoutExtension($originalScriptFullPath)
                $originalScriptDirectory = [System.IO.Path]::GetDirectoryName($originalScriptFullPath)

                # Store it in branding so the exe can use it for log naming and placement
                # Use SetBranding method to ensure it's properly added
                $brandingUpdate = @{ OriginalScriptName = $originalScriptName }
                if ($originalScriptDirectory) {
                    $brandingUpdate.OriginalScriptPath = $originalScriptDirectory
                }
                $script:CurrentWizard.SetBranding($brandingUpdate)
                Write-Verbose "Original script name for logging: $originalScriptName (from $originalScriptFullPath)"
                if ($originalScriptDirectory) {
                    Write-Verbose "Original script directory for logging: $originalScriptDirectory"
                }
            } else {
                Write-Verbose "Could not determine original calling script name from call stack"
            }
            
            # Generate PowerShell script for AST parsing (matching v1.4.1 approach)
            Write-Verbose "Generating PowerShell script from UI definition for AST parsing"
            
            # Set ScriptBody if provided
            if ($ScriptBody) {
                $script:CurrentWizard.SetScriptBody($ScriptBody)
            }
            
            $generatedScript = ConvertTo-UIScript -Definition $script:CurrentWizard -ScriptBody $ScriptBody

            # Create SECURE temporary script file (using security framework)
            Write-Verbose "Creating secure temporary script file"
            $tempScriptPath = New-SecureTempFile -Content $generatedScript -Extension '.ps1'

            # Track temp file for cleanup
            if (-not $context.ContainsKey('TempFiles')) {
                $context['TempFiles'] = @()
            }
            $context.TempFiles += $tempScriptPath

            # Validate script signature if requested
            if ($RequireSignedScripts) {
                Write-Verbose "Validating script signature (RequireSignedScripts enabled)"

                $signature = Get-AuthenticodeSignature -FilePath $tempScriptPath

                if ($signature.Status -ne 'Valid') {
                    throw @"
Script signature verification failed.

Status: $($signature.Status)
SignerCertificate: $($signature.SignerCertificate.Subject)

The generated PowerShell script must be digitally signed with a valid Authenticode certificate.

For development/testing:
  - Remove -RequireSignedScripts switch
  - Or sign the calling script with Set-AuthenticodeSignature

For production:
  - Sign your wizard scripts with your organization's code signing certificate
  - Ensure certificate chain is valid and not expired

Script Path: $tempScriptPath
"@
                }

                Write-Verbose "Script signature valid: $($signature.SignerCertificate.Subject)"

                # Set environment variable to enforce signature checking in the executable
                $env:POSHUI_SIGNATURE_MODE = 'Enforce'
            } else {
                Write-Verbose "Script signature verification disabled (use -RequireSignedScripts to enable)"
                # Explicitly set to Disabled to override any system-level settings
                $env:POSHUI_SIGNATURE_MODE = 'Disabled'
            }

            try {
                # Execute the UI using SECURE invocation with script path
                Write-Verbose "Invoking PoshUI.exe with secure execution (AST parsing mode)"
                $result = Invoke-PoshUIExe -DefinitionPath $tempScriptPath -Wait -AppDebug:$AppDebug
            } finally {
                # Clear the environment variable
                Remove-Item Env:\POSHUI_SIGNATURE_MODE -ErrorAction SilentlyContinue
            }
            
            if ($result.ExitCode -ne 0) {
                throw "UI execution failed with exit code: $($result.ExitCode)"
            }
            
            # Check for result file (Module API mode)
            $resultFilePath = [System.IO.Path]::ChangeExtension($tempScriptPath, '.result.json')
            Write-Verbose "Checking for result file: $resultFilePath"
            
            $jsonResult = $null
            if (Test-Path $resultFilePath) {
                Write-Verbose "Result file found, reading contents"
                $jsonResult = Get-Content $resultFilePath -Raw
                # Clean up result file
                Remove-Item $resultFilePath -Force -ErrorAction SilentlyContinue
            }
            elseif ($result.Output) {
                # Fallback to stdout if available
                Write-Verbose "No result file, using stdout"
                $jsonResult = $result.Output.Trim()
            }
            
            # Parse and return results
            if ($jsonResult) {
                Write-Verbose "Raw result: $jsonResult"
                
                # Try to parse as JSON
                try {
                    switch ($OutputFormat) {
                        'JSON' {
                            return $jsonResult
                        }
                        'Hashtable' {
                            $parsedResult = ConvertFrom-Json $jsonResult -ErrorAction Stop
                            $hashtable = @{}
                            $parsedResult.PSObject.Properties | ForEach-Object {
                                $hashtable[$_.Name] = $_.Value
                            }
                            return $hashtable
                        }
                        'Object' {
                            return ConvertFrom-Json $jsonResult -ErrorAction Stop
                        }
                    }
                }
                catch {
                    Write-Warning "Could not parse UI output as JSON. Returning raw output."
                    Write-Verbose "Parse error: $_"
                    return $jsonResult
                }
            } else {
                Write-Warning "No result returned from UI execution"
                return $null
            }
        }
        catch {
            Write-Error "Failed to execute UI: $($_.Exception.Message)"
            throw
        }
        finally {
            # Cleanup
            if ($context) {
                Clear-UIContext -Context $context
            }
        }
    }

    end {
        Write-Verbose "Show-PoshUI completed"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Show-PoshWizard' -Value 'Show-PoshUIWizard'

