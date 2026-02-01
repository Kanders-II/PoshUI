# Get-ScriptParameters.ps1 - Parses a PowerShell script and extracts parameter metadata for UI generation

function Get-ScriptParameters {
    <#
    .SYNOPSIS
    Parses a PowerShell script file or scriptblock and extracts parameter definitions.
    
    .DESCRIPTION
    Uses PowerShell's AST (Abstract Syntax Tree) to parse script parameters and their
    attributes, converting them into control definitions for the CardGrid flyout UI.
    Supports type inference, ValidateSet, ValidateRange, ValidatePattern, and more.
    
    .PARAMETER ScriptPath
    Path to a .ps1 file to parse.
    
    .PARAMETER ScriptBlock
    A scriptblock to parse.
    
    .OUTPUTS
    Array of PSCustomObject with parameter metadata.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ParameterSetName = 'Path')]
        [ValidateScript({ Test-Path $_ -PathType Leaf })]
        [string]$ScriptPath,
        
        [Parameter(Mandatory, ParameterSetName = 'ScriptBlock')]
        [scriptblock]$ScriptBlock
    )
    
    begin {
        Write-Verbose "Get-ScriptParameters: Parsing script parameters"
    }
    
    process {
        try {
            # Get the AST
            $tokens = $null
            $errors = $null
            
            if ($ScriptPath) {
                Write-Verbose "Parsing script file: $ScriptPath"
                $ast = [System.Management.Automation.Language.Parser]::ParseFile(
                    $ScriptPath, 
                    [ref]$tokens, 
                    [ref]$errors
                )
            }
            else {
                Write-Verbose "Parsing scriptblock"
                $ast = [System.Management.Automation.Language.Parser]::ParseInput(
                    $ScriptBlock.ToString(), 
                    [ref]$tokens, 
                    [ref]$errors
                )
            }
            
            if ($errors.Count -gt 0) {
                Write-Warning "Script parsing encountered errors: $($errors | ForEach-Object { $_.Message })"
            }
            
            # Find the param block
            $paramBlock = $ast.FindAll({ 
                $args[0] -is [System.Management.Automation.Language.ParamBlockAst] 
            }, $true) | Select-Object -First 1
            
            if (-not $paramBlock) {
                Write-Verbose "No param block found in script"
                return @()
            }
            
            $parameters = @()
            
            foreach ($param in $paramBlock.Parameters) {
                $paramName = $param.Name.VariablePath.UserPath
                Write-Verbose "Processing parameter: $paramName"
                
                $paramInfo = [PSCustomObject]@{
                    Name            = $paramName
                    Type            = 'TextBox'  # Default control type
                    ParameterType   = 'String'
                    Label           = Convert-CamelCaseToSpaces -Name $paramName
                    Default         = $null
                    Mandatory       = $false
                    ValidateSet     = @()
                    ValidatePattern = $null
                    ValidationMessage = $null
                    HelpText        = $null
                    Min             = $null
                    Max             = $null
                    Step            = $null
                    IsSwitch        = $false
                    IsSecureString  = $false
                    IsPath          = $false
                    PathType        = 'None'  # File, Folder, None
                    Multiline       = $false
                    Rows            = 3
                }
                
                # Get the type constraint
                $typeConstraint = $param.StaticType
                if ($typeConstraint) {
                    $paramInfo.ParameterType = $typeConstraint.Name
                    
                    # Map .NET types to control types
                    switch ($typeConstraint.Name) {
                        'SwitchParameter' { 
                            $paramInfo.Type = 'Toggle'
                            $paramInfo.IsSwitch = $true
                            $paramInfo.Default = $false
                        }
                        'Boolean' { 
                            $paramInfo.Type = 'Checkbox'
                            $paramInfo.Default = $false
                        }
                        'Bool' { 
                            $paramInfo.Type = 'Checkbox'
                            $paramInfo.Default = $false
                        }
                        'Int32' { 
                            $paramInfo.Type = 'Numeric'
                            $paramInfo.Default = 0
                            $paramInfo.Step = 1
                        }
                        'Int64' { 
                            $paramInfo.Type = 'Numeric'
                            $paramInfo.Default = 0
                            $paramInfo.Step = 1
                        }
                        'Int' { 
                            $paramInfo.Type = 'Numeric'
                            $paramInfo.Default = 0
                            $paramInfo.Step = 1
                        }
                        'Double' { 
                            $paramInfo.Type = 'Numeric'
                            $paramInfo.Default = 0.0
                            $paramInfo.Step = 0.1
                        }
                        'Decimal' { 
                            $paramInfo.Type = 'Numeric'
                            $paramInfo.Default = 0.0
                            $paramInfo.Step = 0.01
                        }
                        'Single' { 
                            $paramInfo.Type = 'Numeric'
                            $paramInfo.Default = 0.0
                            $paramInfo.Step = 0.1
                        }
                        'DateTime' { 
                            $paramInfo.Type = 'Date'
                            $paramInfo.Default = $null
                        }
                        'SecureString' { 
                            $paramInfo.Type = 'Password'
                            $paramInfo.IsSecureString = $true
                        }
                        'PSCredential' { 
                            $paramInfo.Type = 'Password'
                            $paramInfo.IsSecureString = $true
                            $paramInfo.Label = "$($paramInfo.Label) Password"
                        }
                        'String[]' {
                            $paramInfo.Type = 'TextBox'
                            $paramInfo.Multiline = $true
                            $paramInfo.HelpText = "Enter one value per line"
                        }
                        default { 
                            $paramInfo.Type = 'TextBox'
                        }
                    }
                }
                
                # Process attributes
                foreach ($attr in $param.Attributes) {
                    $attrTypeName = $attr.TypeName.Name
                    
                    switch ($attrTypeName) {
                        'Parameter' {
                            # Check for Mandatory
                            $mandatoryArg = $attr.NamedArguments | 
                                Where-Object { $_.ArgumentName -eq 'Mandatory' }
                            if ($mandatoryArg) {
                                $argValue = $mandatoryArg.Argument.Extent.Text
                                $paramInfo.Mandatory = $argValue -eq '$true' -or $argValue -eq '1'
                            }
                            
                            # Check for HelpMessage
                            $helpArg = $attr.NamedArguments | 
                                Where-Object { $_.ArgumentName -eq 'HelpMessage' }
                            if ($helpArg) {
                                $paramInfo.HelpText = $helpArg.Argument.Value
                            }
                        }
                        
                        'ValidateSet' {
                            # Extract choices from ValidateSet
                            $choices = @()
                            foreach ($arg in $attr.PositionalArguments) {
                                if ($arg.Value) { 
                                    $choices += $arg.Value 
                                }
                                elseif ($arg.Extent) { 
                                    $choices += $arg.Extent.Text.Trim("'`"") 
                                }
                            }
                            $paramInfo.ValidateSet = $choices
                            $paramInfo.Type = 'Dropdown'
                        }
                        
                        'ValidateRange' {
                            if ($attr.PositionalArguments.Count -ge 2) {
                                $minArg = $attr.PositionalArguments[0]
                                $maxArg = $attr.PositionalArguments[1]
                                
                                if ($minArg.Value -ne $null) {
                                    $paramInfo.Min = $minArg.Value
                                } elseif ($minArg.Extent) {
                                    $paramInfo.Min = [double]$minArg.Extent.Text
                                }
                                
                                if ($maxArg.Value -ne $null) {
                                    $paramInfo.Max = $maxArg.Value
                                } elseif ($maxArg.Extent) {
                                    $paramInfo.Max = [double]$maxArg.Extent.Text
                                }
                            }
                            # Ensure numeric type for range validation
                            if ($paramInfo.Type -eq 'TextBox') {
                                $paramInfo.Type = 'Numeric'
                            }
                        }
                        
                        'ValidatePattern' {
                            if ($attr.PositionalArguments.Count -ge 1) {
                                $patternArg = $attr.PositionalArguments[0]
                                if ($patternArg.Value) {
                                    $paramInfo.ValidatePattern = $patternArg.Value
                                } elseif ($patternArg.Extent) {
                                    $paramInfo.ValidatePattern = $patternArg.Extent.Text.Trim("'`"")
                                }
                            }
                        }
                        
                        'ValidateLength' {
                            # Could be used for text length validation
                            if ($attr.PositionalArguments.Count -ge 2) {
                                # Min and max length - store for validation
                                $paramInfo | Add-Member -NotePropertyName 'MinLength' -NotePropertyValue $attr.PositionalArguments[0].Value -Force
                                $paramInfo | Add-Member -NotePropertyName 'MaxLength' -NotePropertyValue $attr.PositionalArguments[1].Value -Force
                            }
                        }
                        
                        'ValidateNotNullOrEmpty' {
                            # Treat as mandatory for UI purposes
                            $paramInfo.Mandatory = $true
                        }
                        
                        'Alias' {
                            # Could use first alias as display label if more readable
                            $aliasArg = $attr.PositionalArguments | Select-Object -First 1
                            if ($aliasArg -and $aliasArg.Value -and $aliasArg.Value -match '\s') {
                                $paramInfo.Label = $aliasArg.Value
                            }
                        }
                    }
                }
                
                # Check for default value
                if ($param.DefaultValue) {
                    $defaultText = $param.DefaultValue.Extent.Text
                    
                    # Parse the default value based on type
                    if ($defaultText -match '^\$true$') {
                        $paramInfo.Default = $true
                    }
                    elseif ($defaultText -match '^\$false$') {
                        $paramInfo.Default = $false
                    }
                    elseif ($defaultText -match '^\$null$') {
                        $paramInfo.Default = $null
                    }
                    elseif ($defaultText -match '^[''"](.+)[''"]$') {
                        $paramInfo.Default = $Matches[1]
                    }
                    elseif ($defaultText -match '^(-?\d+\.?\d*)$') {
                        $paramInfo.Default = [double]$Matches[1]
                    }
                    elseif ($defaultText -match '^\(Get-Date\)') {
                        # Default to today for date parameters
                        $paramInfo.Default = (Get-Date).ToString('yyyy-MM-dd')
                    }
                    else {
                        # Store as string for complex defaults
                        $paramInfo.Default = $defaultText
                    }
                }
                
                # Detect path parameters by name convention
                $lowerName = $paramInfo.Name.ToLower()
                if ($lowerName -match 'folder|directory|dir$') {
                    $paramInfo.Type = 'FolderPath'
                    $paramInfo.PathType = 'Folder'
                    $paramInfo.IsPath = $true
                }
                elseif ($lowerName -match 'file|path$' -and $lowerName -notmatch 'filepath') {
                    # Only override if not already a password or other specific type
                    if ($paramInfo.Type -eq 'TextBox') {
                        $paramInfo.Type = 'FilePath'
                        $paramInfo.PathType = 'File'
                        $paramInfo.IsPath = $true
                    }
                }
                elseif ($lowerName -eq 'filepath' -or $lowerName -match 'scriptpath|configpath|logpath') {
                    $paramInfo.Type = 'FilePath'
                    $paramInfo.PathType = 'File'
                    $paramInfo.IsPath = $true
                }
                
                $parameters += $paramInfo
                Write-Verbose "  → Type: $($paramInfo.Type), Mandatory: $($paramInfo.Mandatory), Default: $($paramInfo.Default)"
            }
            
            return $parameters
        }
        catch {
            Write-Error "Failed to parse script parameters: $($_.Exception.Message)"
            throw
        }
    }
}

function Convert-CamelCaseToSpaces {
    <#
    .SYNOPSIS
    Converts a CamelCase or PascalCase string to a space-separated title case string.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name
    )
    
    # Insert space before capital letters (but not at start)
    $spaced = $Name -creplace '([a-z])([A-Z])', '$1 $2'
    
    # Insert space before numbers
    $spaced = $spaced -creplace '([a-zA-Z])(\d)', '$1 $2'
    
    # Insert space after numbers followed by letters
    $spaced = $spaced -creplace '(\d)([a-zA-Z])', '$1 $2'
    
    # Capitalize first letter of each word
    $result = (Get-Culture).TextInfo.ToTitleCase($spaced.ToLower())
    
    return $result
}

function Convert-ParameterToControl {
    <#
    .SYNOPSIS
    Converts a parameter info object to a WizardControl-compatible hashtable.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$ParameterInfo,
        
        [Parameter()]
        [hashtable]$DefaultOverrides = @{}
    )
    
    $control = @{
        Name       = $ParameterInfo.Name
        Label      = $ParameterInfo.Label
        Type       = $ParameterInfo.Type
        Mandatory  = $ParameterInfo.Mandatory
        Default    = if ($DefaultOverrides.ContainsKey($ParameterInfo.Name)) { 
                        $DefaultOverrides[$ParameterInfo.Name] 
                     } else { 
                        $ParameterInfo.Default 
                     }
        HelpText   = $ParameterInfo.HelpText
    }
    
    # Add type-specific properties
    switch ($ParameterInfo.Type) {
        'Dropdown' {
            $control['Choices'] = $ParameterInfo.ValidateSet
        }
        'Numeric' {
            if ($null -ne $ParameterInfo.Min) { $control['Min'] = $ParameterInfo.Min }
            if ($null -ne $ParameterInfo.Max) { $control['Max'] = $ParameterInfo.Max }
            if ($null -ne $ParameterInfo.Step) { $control['Step'] = $ParameterInfo.Step }
        }
        'TextBox' {
            if ($ParameterInfo.ValidatePattern) {
                $control['ValidationPattern'] = $ParameterInfo.ValidatePattern
                $control['ValidationMessage'] = if ($null -ne $ParameterInfo.ValidationMessage) { $ParameterInfo.ValidationMessage } else { "Invalid format" }
            }
            if ($ParameterInfo.Multiline) {
                $control['Multiline'] = $true
                $control['Rows'] = $ParameterInfo.Rows
            }
        }
        'FilePath' {
            $control['Filter'] = '*.*'
        }
    }
    
    return $control
}
