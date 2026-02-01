function Invoke-PoshUIExe {
    <#
    .SYNOPSIS
        Safely invokes the PoshUI.exe with a script file.

    .DESCRIPTION
        Executes PoshUI.exe with proper security checks:
        - Validates exe path exists
        - Verifies code signature (if signature mode enabled)
        - Uses proper argument escaping
        - Returns exit code

        This function ensures the exe invocation is secure and doesn't
        introduce command injection vulnerabilities.

    .PARAMETER ScriptPath
        Full path to the PowerShell script to execute (legacy - use DefinitionPath instead)

    .PARAMETER DefinitionPath
        Full path to the definition file (.json or .ps1) to execute

    .PARAMETER AppDebug
        If specified, enables debug mode in PoshUI.exe

    .PARAMETER Wait
        If specified, waits for the UI to complete

    .OUTPUTS
        [PSCustomObject] Object with ExitCode and Output properties

    .EXAMPLE
        $result = Invoke-PoshUIExe -DefinitionPath "C:\Temp\ui.json" -Wait
        if ($result.ExitCode -eq 0) {
            Write-Host "Output: $($result.Output)"
        }
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [Parameter(Mandatory = $false)]
        [ValidateScript({
            if (-not (Test-Path $_)) {
                throw "Definition file not found: $_"
            }
            if ($_ -notmatch '\.(json|ps1)$') {
                throw "File must have .json or .ps1 extension: $_"
            }
            $true
        })]
        [string]$ScriptPath,  # Legacy parameter name - accepts JSON or PS1 files
        
        [Parameter(Mandatory = $false)]
        [ValidateScript({
            if (-not (Test-Path $_)) {
                throw "Definition file not found: $_"
            }
            if ($_ -notmatch '\.(json|ps1)$') {
                throw "File must have .json or .ps1 extension: $_"
            }
            $true
        })]
        [string]$DefinitionPath,
        
        [Parameter()]
        [switch]$AppDebug,

        [Parameter()]
        [switch]$Wait
    )
    
    try {
        # Determine which path to use
        if ($DefinitionPath) {
            $filePath = $DefinitionPath
        } elseif ($ScriptPath) {
            $filePath = $ScriptPath
        } else {
            throw "Either -ScriptPath or -DefinitionPath must be provided"
        }
        
        # Get exe path relative to module root
        # Structure: PoshUI/PoshUI.Wizard/Private/Security/ - module root is PoshUI/PoshUI.Wizard/
        $modulePath = $script:ModuleRoot
        if (-not $modulePath) {
            # PSScriptRoot is .../Private/Security, go up to get module root
            $modulePath = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
        }
        
        # Parent folder is PoshUI/ where the shared bin/ folder resides
        $parentPath = Split-Path $modulePath -Parent
        $workspacePath = Split-Path $parentPath -Parent
        
        # Try multiple possible exe locations
        $exePaths = @(
            # Parent's bin folder (PoshUI/bin/) - main location
            (Join-Path $parentPath 'bin\PoshUI.exe'),
            # Module's own bin folder (for standalone deployment)
            (Join-Path $modulePath 'bin\PoshUI.exe'),
            # Launcher output folders (development)
            (Join-Path $workspacePath 'Launcher\bin\Release\PoshUI.exe'),
            (Join-Path $workspacePath 'Launcher\bin\Debug\PoshUI.exe')
        )
        
        $exePath = $null
        foreach ($path in $exePaths) {
            if (Test-Path $path) {
                $exePath = $path
                Write-Verbose "Found PoshUI.exe at: $exePath"
                break
            }
        }
        
        if (-not $exePath) {
            throw "PoshUI.exe not found. Searched locations:`n" + ($exePaths -join "`n")
        }
        
        # Verify exe signature if signature mode is enabled
        $signatureMode = $env:POSHWIZARD_SIGNATURE_MODE
        if ($signatureMode -eq 'Enforce') {
            Write-Verbose "Signature mode is Enforce, verifying exe signature..."
            
            $signature = Get-AuthenticodeSignature -FilePath $exePath -ErrorAction Stop
            if ($signature.Status -ne 'Valid') {
                throw "PoshUI.exe signature is invalid. Status: $($signature.Status). Cannot execute in Enforce mode."
            }
            
            Write-Verbose "Exe signature verified: $($signature.SignerCertificate.Subject)"
        } elseif ($signatureMode -eq 'Warn') {
            Write-Verbose "Signature mode is Warn, checking exe signature..."
            
            $signature = Get-AuthenticodeSignature -FilePath $exePath -ErrorAction SilentlyContinue
            if ($signature -and $signature.Status -ne 'Valid') {
                Write-Warning "PoshUI.exe signature is invalid. Status: $($signature.Status). Proceeding anyway (Warn mode)."
            }
        }
        
        # Build argument list safely (no string concatenation)
        $arguments = [System.Collections.Generic.List[string]]::new()
        $arguments.Add($filePath)
        
        if ($AppDebug) {
            $arguments.Add('--debug')
        }
        
        Write-Verbose "Invoking PoshUI.exe with arguments: $($arguments -join ' ')"
        
        # Execute the process
        if ($Wait) {
            # Create a temp file to capture stdout
            $outputFile = [System.IO.Path]::GetTempFileName()
            Write-Verbose "Output will be captured to: $outputFile"
            
            $process = Start-Process -FilePath $exePath `
                                    -ArgumentList $arguments `
                                    -Wait `
                                    -PassThru `
                                    -RedirectStandardOutput $outputFile `
                                    -NoNewWindow `
                                    -ErrorAction Stop
            
            $exitCode = $process.ExitCode
            Write-Verbose "PoshUI.exe exited with code: $exitCode"
            
            # Read captured output
            $output = $null
            if (Test-Path $outputFile) {
                $output = Get-Content $outputFile -Raw
                Remove-Item $outputFile -Force -ErrorAction SilentlyContinue
                Write-Verbose "Captured output length: $($output.Length) characters"
            }
            
            # Return structured result
            return [PSCustomObject]@{
                ExitCode = $exitCode
                Output = $output
            }
        } else {
            # Non-blocking execution
            Start-Process -FilePath $exePath `
                         -ArgumentList $arguments `
                         -ErrorAction Stop
            
            Write-Verbose "PoshUI.exe launched (non-blocking)"
            return [PSCustomObject]@{
                ExitCode = 0
                Output = $null
            }
        }
        
    } catch {
        Write-Error "Failed to invoke PoshUI.exe: $_"
        throw
    }
}

