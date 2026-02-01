function Test-SafeParameterName {
    <#
    .SYNOPSIS
        Validates a PowerShell parameter name for security.
    
    .DESCRIPTION
        Ensures parameter names follow safe patterns and don't contain
        potentially dangerous terms that could lead to code injection.
        
        Validation rules:
        - Must start with a letter
        - Can only contain letters, numbers, and underscores
        - Maximum 64 characters
        - Cannot contain blacklisted terms
    
    .PARAMETER Name
        The parameter name to validate
    
    .PARAMETER ThrowOnInvalid
        If true, throws an exception on validation failure.
        If false, returns $false on validation failure.
    
    .EXAMPLE
        Test-SafeParameterName -Name "ServerName"
        Returns $true
    
    .EXAMPLE
        Test-SafeParameterName -Name "Server'; Invoke-Expression 'evil" -ThrowOnInvalid
        Throws exception
    #>
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [AllowEmptyString()]
        [string]$Name,
        
        [Parameter()]
        [switch]$ThrowOnInvalid
    )
    
    process {
        # Check for null/empty
        if ([string]::IsNullOrWhiteSpace($Name)) {
            if ($ThrowOnInvalid) {
                throw "Parameter name cannot be null or empty"
            }
            return $false
        }
        
        # Check regex pattern: must start with letter, alphanumeric + underscore, max 64
        if ($Name -notmatch '^[a-zA-Z][a-zA-Z0-9_]{0,63}$') {
            if ($ThrowOnInvalid) {
                throw "Invalid parameter name '$Name'. Must start with letter, contain only alphanumeric and underscore, max 64 characters"
            }
            Write-Warning "Invalid parameter name pattern: $Name"
            return $false
        }
        
        # Blacklist dangerous terms
        $blacklist = @(
            'Invoke',
            'Expression', 
            'Command',
            'ScriptBlock',
            'Script',
            'Eval',
            'Execute'
        )
        
        foreach ($term in $blacklist) {
            if ($Name -like "*$term*") {
                if ($ThrowOnInvalid) {
                    throw "Parameter name '$Name' contains prohibited term: $term"
                }
                Write-Warning "Parameter name contains blacklisted term '$term': $Name"
                return $false
            }
        }
        
        # Additional check: no special characters that might be used in injection
        $dangerousChars = @(';', '|', '&', '$', '`', '"', "'", '<', '>', '(', ')', '{', '}', '[', ']')
        foreach ($char in $dangerousChars) {
            if ($Name.Contains($char)) {
                if ($ThrowOnInvalid) {
                    throw "Parameter name '$Name' contains dangerous character: $char"
                }
                Write-Warning "Parameter name contains dangerous character '$char': $Name"
                return $false
            }
        }
        
        Write-Verbose "Parameter name validation passed: $Name"
        return $true
    }
}

