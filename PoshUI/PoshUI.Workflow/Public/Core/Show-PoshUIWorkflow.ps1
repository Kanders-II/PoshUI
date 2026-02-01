function Show-PoshUIWorkflow {
    <#
    .SYNOPSIS
    Displays the Workflow UI and executes the workflow tasks.

    .DESCRIPTION
    Serializes the current Workflow UI definition to JSON,
    launches the PoshUI executable, and returns the results.
    
    .PARAMETER DefaultValues
    Hashtable of default values to pre-populate in the UI form.
    Keys should match the control names.

    .PARAMETER NonInteractive
    Run the UI in non-interactive mode using only the default values.
    The UI will not be displayed.

    .PARAMETER Theme
    Override the theme for this UI execution.
    Valid values are 'Light', 'Dark', or 'Auto'.
    
    .PARAMETER OutputFormat
    Format for the returned results. Valid values are 'Object', 'JSON', 'Hashtable'.
    Default is 'Object'.
    
    .EXAMPLE
    $result = Show-PoshUIWorkflow

    Shows the Workflow UI and returns results.

    .EXAMPLE
    $defaults = @{ ServerName = 'SQL01'; Environment = 'Production' }
    $result = Show-PoshUIWorkflow -DefaultValues $defaults

    Shows the Workflow UI with pre-populated default values.

    .OUTPUTS
    PSCustomObject containing the UI results and workflow execution status.

    .NOTES
    This function requires that New-PoshUIWorkflow has been called and at least one step has been added.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [hashtable]$DefaultValues = @{},
        
        [Parameter()]
        [switch]$NonInteractive,
        
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
        Write-Verbose "Starting Show-PoshUIWorkflow"

        # Ensure UI is initialized
        if (-not $script:CurrentWorkflow) {
            throw "No UI initialized. Call New-PoshUIWorkflow first."
        }

        # Ensure UI has steps
        if ($script:CurrentWorkflow.Steps.Count -eq 0) {
            throw "UI has no steps. Add at least one step using Add-UIStep."
        }
    }
    
    process {
        $context = $null
        $tempScriptPath = $null
        
        try {
            # Initialize context
            $context = Initialize-UIContext -Wizard $script:CurrentWorkflow
            
            # Override theme if specified
            if ($PSBoundParameters.ContainsKey('Theme')) {
                $script:CurrentWorkflow.Theme = $Theme
            }
            
            # Capture original calling script name for log file naming
            $callingScript = $null
            $callStack = Get-PSCallStack
            Write-Verbose "Examining call stack for original script..."
            foreach ($frame in $callStack) {
                Write-Verbose "  Frame: $($frame.ScriptName) (Function: $($frame.FunctionName))"
                if ($frame.ScriptName -and $frame.ScriptName -match '\.ps1$') {
                    $normalizedPath = $frame.ScriptName -replace '\\','/'
                    $isModulePath = $normalizedPath -match 'PoshUI(\.Workflow|\.Wizard|\.Dashboard)?/(Public|Private|Classes)/'
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

                $brandingUpdate = @{ OriginalScriptName = $originalScriptName }
                if ($originalScriptDirectory) {
                    $brandingUpdate.OriginalScriptPath = $originalScriptDirectory
                }
                $script:CurrentWorkflow.SetBranding($brandingUpdate)
                Write-Verbose "Original script name for logging: $originalScriptName (from $originalScriptFullPath)"
            }
            
            # Generate PowerShell script for AST parsing
            Write-Verbose "Generating PowerShell script from UI definition for AST parsing"
            
            $generatedScript = ConvertTo-UIScript -Definition $script:CurrentWorkflow
            
            # Debug: Output generated script to temp file for inspection
            $debugScriptPath = Join-Path $env:TEMP "PoshUI_GeneratedScript_Debug.ps1"
            $generatedScript | Out-File $debugScriptPath -Encoding UTF8
            Write-Verbose "Debug: Generated script saved to $debugScriptPath"

            # Create secure temporary script file
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
                    throw "Script signature verification failed. Status: $($signature.Status)"
                }
                Write-Verbose "Script signature valid: $($signature.SignerCertificate.Subject)"
                $env:POSHUI_SIGNATURE_MODE = 'Enforce'
            } else {
                $env:POSHUI_SIGNATURE_MODE = 'Disabled'
            }

            try {
                # Execute the UI
                Write-Verbose "Invoking PoshUI.exe with secure execution (AST parsing mode)"
                $result = Invoke-PoshUIExe -DefinitionPath $tempScriptPath -Wait -AppDebug:$AppDebug
            } finally {
                Remove-Item Env:\POSHUI_SIGNATURE_MODE -ErrorAction SilentlyContinue
            }
            
            if ($result.ExitCode -ne 0) {
                throw "UI execution failed with exit code: $($result.ExitCode)"
            }
            
            # Check for result file
            $resultFilePath = [System.IO.Path]::ChangeExtension($tempScriptPath, '.result.json')
            Write-Verbose "Checking for result file: $resultFilePath"
            
            $jsonResult = $null
            if (Test-Path $resultFilePath) {
                Write-Verbose "Result file found, reading contents"
                $jsonResult = Get-Content $resultFilePath -Raw
                Remove-Item $resultFilePath -Force -ErrorAction SilentlyContinue
            }
            elseif ($result.Output) {
                Write-Verbose "No result file, using stdout"
                $jsonResult = $result.Output.Trim()
            }
            
            # Parse and return results
            if ($jsonResult) {
                Write-Verbose "Raw result: $jsonResult"
                
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
            if ($context) {
                Clear-UIContext -Context $context
            }
        }
    }

    end {
        Write-Verbose "Show-PoshUIWorkflow completed"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Show-PoshWorkflow' -Value 'Show-PoshUIWorkflow'
