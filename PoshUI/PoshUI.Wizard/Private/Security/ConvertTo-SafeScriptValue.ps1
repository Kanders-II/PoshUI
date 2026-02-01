function ConvertTo-SafeScriptValue {
    <#
    .SYNOPSIS
        Converts a value to a safe PowerShell script representation.
    
    .DESCRIPTION
        Properly escapes and formats values for inclusion in generated PowerShell scripts.
        This prevents code injection by ensuring all user-provided values are properly escaped.
        
        SECURITY: Never use this for SecureString values - they must be handled separately.
    
    .PARAMETER Value
        The value to convert
    
    .PARAMETER Type
        The PowerShell type (String, Int, Bool, Switch, etc.)
    
    .EXAMPLE
        ConvertTo-SafeScriptValue -Value "O'Reilly" -Type 'String'
        Returns: 'O''Reilly'
    
    .EXAMPLE
        ConvertTo-SafeScriptValue -Value 42 -Type 'Int'
        Returns: 42
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [AllowNull()]
        $Value,
        
        [Parameter(Mandatory)]
        [ValidateSet('String', 'Int', 'Int32', 'Int64', 'Bool', 'Boolean', 'Switch', 'Double', 'Decimal', 'Array')]
        [string]$Type
    )
    
    process {
        # Handle null
        if ($null -eq $Value) {
            return '$null'
        }
        
        switch ($Type) {
            'String' {
                # Convert to string and validate length
                $stringValue = $Value.ToString()
                
                if ($stringValue.Length -gt 10000) {
                    Write-Warning "String value exceeds maximum length of 10000 characters. Truncating."
                    $stringValue = $stringValue.Substring(0, 10000)
                }
                
                # Escape single quotes by doubling them (PowerShell string escaping)
                $escaped = $stringValue.Replace("'", "''")
                
                # Check for potential injection attempts in the escaped string
                if ($escaped -match '[\x00-\x08\x0B\x0C\x0E-\x1F]') {
                    throw "String contains control characters that could be dangerous"
                }
                
                return "'$escaped'"
            }
            
            { $_ -in @('Int', 'Int32', 'Int64') } {
                # Validate numeric
                $intValue = 0
                if (-not [int]::TryParse($Value, [ref]$intValue)) {
                    throw "Value '$Value' is not a valid integer"
                }
                return $intValue.ToString()
            }
            
            { $_ -in @('Double', 'Decimal') } {
                # Validate numeric
                $numValue = 0.0
                if (-not [double]::TryParse($Value, [ref]$numValue)) {
                    throw "Value '$Value' is not a valid number"
                }
                return $numValue.ToString()
            }
            
            { $_ -in @('Bool', 'Boolean') } {
                # Convert to boolean
                if ($Value -is [bool]) {
                    return if ($Value) { '$true' } else { '$false' }
                }
                
                # Try parsing string representation
                $boolValue = $false
                if ([bool]::TryParse($Value, [ref]$boolValue)) {
                    return if ($boolValue) { '$true' } else { '$false' }
                }
                
                # Default to false for safety
                Write-Warning "Could not parse boolean value '$Value', defaulting to `$false"
                return '$false'
            }
            
            'Switch' {
                # Switch parameters are not included in default values
                # They're added at invocation if true
                return $null
            }
            
            'Array' {
                # Convert array to PowerShell array syntax
                if ($Value -is [array]) {
                    $elements = foreach ($item in $Value) {
                        if ($item -is [string]) {
                            ConvertTo-SafeScriptValue -Value $item -Type 'String'
                        } elseif ($item -is [int]) {
                            ConvertTo-SafeScriptValue -Value $item -Type 'Int'
                        } elseif ($item -is [bool]) {
                            ConvertTo-SafeScriptValue -Value $item -Type 'Bool'
                        } else {
                            ConvertTo-SafeScriptValue -Value $item.ToString() -Type 'String'
                        }
                    }
                    return "@(" + ($elements -join ', ') + ")"
                }
                throw "Value is not an array"
            }
            
            default {
                throw "Unsupported type: $Type"
            }
        }
    }
}

