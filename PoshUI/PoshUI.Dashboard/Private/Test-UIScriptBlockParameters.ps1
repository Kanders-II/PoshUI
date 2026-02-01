function Test-UIScriptBlockParameters {
    <#
    .SYNOPSIS
        Validates that a script block's parameters match available UI parameters.

    .DESCRIPTION
        Extracts parameters from a script block using AST parsing and checks that all
        parameters exist in the UI parameter definition. Reports missing dependencies.

    .PARAMETER ScriptBlock
        The script block to analyze for parameters.

    .PARAMETER AvailableParameters
        Array of parameter names that are available in the UI.

    .EXAMPLE
        $sb = { param($Environment, $Region) Get-Service }
        Test-UIScriptBlockParameters -ScriptBlock $sb -AvailableParameters @('Environment', 'Region', 'Server')

        Validates that the script block parameters exist in the available parameters list.

    .EXAMPLE
        $sb = { param($MissingParam) Get-Content $MissingParam }
        Test-UIScriptBlockParameters -ScriptBlock $sb -AvailableParameters @('Environment')

        Reports that 'MissingParam' is not available in the UI.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ScriptBlock]$ScriptBlock,
        
        [Parameter(Mandatory)]
        [string[]]$AvailableParameters
    )
    
    $result = [PSCustomObject]@{
        IsValid = $false
        ScriptBlockParameters = @()
        MissingParameters = @()
        AvailableParameters = $AvailableParameters
        Warnings = [System.Collections.Generic.List[string]]::new()
        Errors = [System.Collections.Generic.List[string]]::new()
    }
    
    try {
        # Parse the script block using AST
        $ast = [System.Management.Automation.Language.Parser]::ParseInput($ScriptBlock.ToString(), [ref]$null, [ref]$null)
        
        # Find the param block
        $paramBlock = $ast.FindAll({
            $args[0] -is [System.Management.Automation.Language.ParamBlockAst]
        }, $true) | Select-Object -First 1
        
        if ($null -eq $paramBlock) {
            Write-Verbose "No param() block found in script block."
            $result.IsValid = $true
            return $result
        }
        
        # Extract parameter names
        $scriptParams = $paramBlock.Parameters | ForEach-Object {
            $_.Name.VariablePath.UserPath
        }
        
        $result.ScriptBlockParameters = $scriptParams
        
        if ($scriptParams.Count -eq 0) {
            Write-Verbose "Param block exists but contains no parameters."
            $result.IsValid = $true
            return $result
        }
        
        # Check each parameter against available parameters (case-insensitive)
        $availableParamsLower = $AvailableParameters | ForEach-Object { $_.ToLower() }
        
        foreach ($param in $scriptParams) {
            if ($param.ToLower() -notin $availableParamsLower) {
                $result.MissingParameters += $param
                $result.Errors.Add("Parameter '$param' is not defined in the wizard.")
            }
        }
        
        if ($result.MissingParameters.Count -eq 0) {
            $result.IsValid = $true
        } else {
            $result.IsValid = $false
        }
        
    } catch {
        $result.Errors.Add("Failed to parse script block: $($_.Exception.Message)")
        $result.IsValid = $false
    }
    
    # Display results
    if ($result.IsValid) {
        Write-Host "[OK] " -ForegroundColor Green -NoNewline
        Write-Host "Script block parameters validation PASSED" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] " -ForegroundColor Red -NoNewline
        Write-Host "Script block parameters validation FAILED" -ForegroundColor Red
    }
    
    if ($result.ScriptBlockParameters.Count -gt 0) {
        Write-Host "  Script block parameters: $($result.ScriptBlockParameters -join ', ')"
    } else {
        Write-Host "  Script block has no parameters"
    }
    
    Write-Host "  Available wizard parameters: $($AvailableParameters -join ', ')"
    
    if ($result.MissingParameters.Count -gt 0) {
        Write-Host ""
        Write-Host "Missing Parameters:" -ForegroundColor Red
        foreach ($missing in $result.MissingParameters) {
            Write-Host "  [ERR] $missing is not defined in the wizard" -ForegroundColor Red
        }
        Write-Host ""
        Write-Host "Suggestion:" -ForegroundColor Yellow
        Write-Host "  Add the missing parameters to your wizard script or remove them from the script block." -ForegroundColor Yellow
    }
    
    if ($result.Errors.Count -gt 0) {
        Write-Host ""
        Write-Host "Errors:" -ForegroundColor Red
        foreach ($errorMsg in $result.Errors) {
            Write-Host "  [ERR] $errorMsg" -ForegroundColor Red
        }
    }
    
    return $result
}

