# ValidationHelpers.ps1 - Validation and error message helpers for improved developer experience

function Test-UIStepExists {
    <#
    .SYNOPSIS
    Validates that a UI step exists and provides helpful error message with available steps.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$StepName,
        
        [Parameter(Mandatory = $true)]
        [object]$UIDefinition,
        
        [Parameter()]
        [string]$Context = "Add control"
    )
    
    if (-not $UIDefinition.HasStep($StepName)) {
        $availableSteps = $UIDefinition.Steps | Select-Object -ExpandProperty Name
        $stepList = if ($availableSteps) { 
            "`n`nAvailable steps:`n  - " + ($availableSteps -join "`n  - ")
        } else {
            "`n`nNo steps have been defined yet. Create a step first using Add-UIStep."
        }
        
        throw "$Context failed: Step '$StepName' does not exist.$stepList"
    }
}

function Test-UIControlNameUnique {
    <#
    .SYNOPSIS
    Validates that a control name is unique within a step.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$ControlName,
        
        [Parameter(Mandatory = $true)]
        [object]$UIStep,
        
        [Parameter(Mandatory = $true)]
        [string]$StepName,
        
        [Parameter()]
        [string]$Context = "Add control"
    )
    
    if ($UIStep.HasControl($ControlName)) {
        $existingControls = $UIStep.Controls | Select-Object -ExpandProperty Name
        $controlList = if ($existingControls) {
            "`n`nExisting controls in step '$StepName':`n  - " + ($existingControls -join "`n  - ")
        } else {
            ""
        }
        
        throw "$Context failed: Control with name '$ControlName' already exists in step '$StepName'.$controlList"
    }
}

function Test-UIParameterValidation {
    <#
    .SYNOPSIS
    Validates parameter combinations and provides helpful error messages.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$BoundParameters,
        
        [Parameter(Mandatory = $true)]
        [string[]]$RequiredOneOf,
        
        [Parameter()]
        [string]$Context = "Parameter validation"
    )
    
    $providedParams = @()
    foreach ($param in $RequiredOneOf) {
        if ($BoundParameters.ContainsKey($param)) {
            $providedParams += $param
        }
    }
    
    if ($providedParams.Count -eq 0) {
        $paramList = "`n`nOne of the following parameters is required:`n  - " + ($RequiredOneOf -join "`n  - ")
        throw "$Context failed: Missing required parameter.$paramList"
    }
    
    if ($providedParams.Count -gt 1) {
        $paramList = "`n`nProvided parameters: " + ($providedParams -join ", ")
        throw "$Context failed: Only one of the following parameters can be specified: $($RequiredOneOf -join ', ').$paramList"
    }
}

function Test-ValidateSetValue {
    <#
    .SYNOPSIS
    Validates that a value is in a set of allowed values and provides helpful error message.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value,
        
        [Parameter(Mandatory = $true)]
        [string[]]$AllowedValues,
        
        [Parameter(Mandatory = $true)]
        [string]$ParameterName,
        
        [Parameter()]
        [string]$Context = "Parameter validation"
    )
    
    if ($Value -notin $AllowedValues) {
        $valueList = "`n`nAllowed values for -$ParameterName`:`n  - " + ($AllowedValues -join "`n  - ")
        throw "$Context failed: Invalid value '$Value' for parameter -$ParameterName.$valueList"
    }
}

function Test-ScriptBlockOrValue {
    <#
    .SYNOPSIS
    Validates that either a scriptblock or a value is provided (but not both).
    #>
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$BoundParameters,
        
        [Parameter(Mandatory = $true)]
        [string]$ScriptBlockParam,
        
        [Parameter(Mandatory = $true)]
        [string]$ValueParam,
        
        [Parameter()]
        [string]$Context = "Parameter validation"
    )
    
    $hasScriptBlock = $BoundParameters.ContainsKey($ScriptBlockParam)
    $hasValue = $BoundParameters.ContainsKey($ValueParam)
    
    if (-not $hasScriptBlock -and -not $hasValue) {
        throw "$Context failed: Either -$ScriptBlockParam or -$ValueParam must be provided."
    }
    
    if ($hasScriptBlock -and $hasValue) {
        throw "$Context failed: Cannot specify both -$ScriptBlockParam and -$ValueParam. Choose one approach."
    }
}

function Get-UIValidationErrorMessage {
    <#
    .SYNOPSIS
    Formats a validation error message with context and suggestions.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,
        
        [Parameter()]
        [string[]]$Suggestions,
        
        [Parameter()]
        [string]$Context
    )
    
    $errorMsg = $Message
    
    if ($Context) {
        $errorMsg = "[$Context] $errorMsg"
    }
    
    if ($Suggestions) {
        $errorMsg += "`n`nSuggestions:`n  - " + ($Suggestions -join "`n  - ")
    }
    
    return $errorMsg
}
