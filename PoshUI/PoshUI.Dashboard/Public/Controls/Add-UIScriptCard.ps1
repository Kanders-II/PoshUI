function Add-UIScriptCard {
    <#
    .SYNOPSIS
    Adds an executable script card to a UI step in CardGrid view mode.
    
    .DESCRIPTION
    Creates a card that represents a PowerShell script with its own parameters in Dashboard view mode.
    When clicked, the card opens a dialog with the script's parameters (auto-discovered
    from the script's param block) and an execution console showing real-time output.
    
    .PARAMETER Step
    Name of the step to add this card to. The step must already exist.
    
    .PARAMETER Name
    Unique name for the card. This is used internally to reference the card.
    
    .PARAMETER Title
    Display title for the card shown in the UI.
    
    .PARAMETER Description
    Short description shown below the card title.
    
    .PARAMETER Icon
    Optional icon to display on the card. Can be:
    - Segoe MDL2 icon glyph in format '&#xE1D3;' (e.g., '&#xE77B;' for User)
    - Emoji characters (e.g., 'ðŸ–¥ï¸', 'ðŸ“', 'âš™ï¸')
    
    .PARAMETER ScriptPath
    Path to a .ps1 file to execute. Parameters are auto-discovered from the script's param block.
    
    .PARAMETER ScriptBlock
    Inline scriptblock to execute. Parameters are auto-discovered from the scriptblock.
    
    .PARAMETER DefaultParameters
    Hashtable of default parameter values to pre-populate in the UI.
    These override any defaults defined in the script itself.
    
    .PARAMETER Category
    Category for grouping/filtering cards in the CardGrid view.
    
    .PARAMETER Tags
    Array of tags for additional filtering capabilities.
    
    .EXAMPLE
    Add-UIScriptCard -Step "Tools" -Name "CreateUser" -Title "Create User" `
        -Description "Create a new local user account" `
        -Icon "&#xE77B;" `
        -ScriptPath ".\Scripts\New-LocalUser.ps1"
    
    Adds a script card that executes an external PowerShell script with auto-discovered parameters.
    
    .EXAMPLE
    Add-UIScriptCard -Step "Tools" -Name "RestartIIS" -Title "Restart IIS" `
        -ScriptBlock { Restart-Service W3SVC -Force; "IIS Restarted" }
    
    Adds a simple action card with an inline scriptblock.
    
    .EXAMPLE
    Add-UIScriptCard -Step "Tools" -Name "DiskCheck" -Title "Check Disk Space" `
        -ScriptBlock {
            param([string]$Drive = "C")
            Get-PSDrive $Drive | Select-Object Used, Free, @{N='PercentFree';E={[math]::Round($_.Free/($_.Used+$_.Free)*100,1)}}
        } `
        -DefaultParameters @{ Drive = "C" }
    
    Adds a script card with a parameterized inline script and default values.
    
    .OUTPUTS
    UIControl object representing the created script card.
    
    .NOTES
    Script cards are designed for use with Dashboard view mode.
    Parameters are automatically discovered using PowerShell AST parsing.
    Each script card executes in an isolated runspace for safety.
    #>
    [CmdletBinding(DefaultParameterSetName = 'ScriptPath')]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string]$Step,
        
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullOrEmpty()]
        [string]$Name,
        
        [Parameter(Mandatory = $true, Position = 2)]
        [ValidateNotNullOrEmpty()]
        [string]$Title,
        
        [Parameter()]
        [string]$Description,
        
        [Parameter()]
        [string]$Icon,
        
        [Parameter(Mandatory = $true, ParameterSetName = 'ScriptPath')]
        [ValidateScript({ Test-Path $_ -PathType Leaf })]
        [string]$ScriptPath,
        
        [Parameter(Mandatory = $true, ParameterSetName = 'ScriptBlock')]
        [scriptblock]$ScriptBlock,
        
        [Parameter()]
        [hashtable]$DefaultParameters = @{},
        
        [Parameter()]
        [string]$Category,
        
        [Parameter()]
        [string[]]$Tags
    )
    
    begin {
        Write-Verbose "Adding ScriptCard control: $Name to step: $Step"
        
        # Ensure wizard is initialized
        if (-not $script:CurrentWizard) {
            throw "No UI initialized. Call New-PoshUI first."
        }
        
        # Ensure step exists
        if (-not $script:CurrentWizard.HasStep($Step)) {
            throw "Step '$Step' does not exist. Add the step first using Add-UIStep."
        }
    }
    
    process {
        try {
            # Get the step
            $wizardStep = $script:CurrentWizard.GetStep($Step)
            
            # Check for duplicate control name within the step
            if ($wizardStep.HasControl($Name)) {
                throw "Control with name '$Name' already exists in step '$Step'"
            }
            
            # Resolve script path if provided
            $resolvedScriptPath = $null
            $scriptSource = 'Inline'
            $scriptContent = $null
            
            if ($ScriptPath) {
                $resolvedScriptPath = (Resolve-Path $ScriptPath).Path
                $scriptSource = 'File'
                Write-Verbose "Script source: File - $resolvedScriptPath"
            }
            else {
                $scriptContent = $ScriptBlock.ToString()
                Write-Verbose "Script source: Inline scriptblock"
            }
            
            # Auto-discover parameters from the script
            $discoveredParams = @()
            try {
                if ($ScriptPath) {
                    $discoveredParams = Get-ScriptParameters -ScriptPath $resolvedScriptPath
                }
                else {
                    $discoveredParams = Get-ScriptParameters -ScriptBlock $ScriptBlock
                }
                Write-Verbose "Discovered $($discoveredParams.Count) parameters"
            }
            catch {
                Write-Warning "Failed to auto-discover parameters: $($_.Exception.Message)"
                $discoveredParams = @()
            }
            
            # Convert discovered parameters to control definitions
            $parameterControls = @()
            foreach ($param in $discoveredParams) {
                $controlDef = Convert-ParameterToControl -ParameterInfo $param -DefaultOverrides $DefaultParameters
                $parameterControls += $controlDef
                Write-Verbose "  Parameter: $($param.Name) â†’ $($param.Type) control"
            }
            
            # Create the control
            $control = [UIControl]::new($Name, $Title, 'ScriptCard')
            
            # Set card display properties
            $control.SetProperty('CardTitle', $Title)
            $control.SetProperty('CardDescription', $Description)
            
            if ($Icon) {
                $control.SetProperty('Icon', $Icon)
            }
            
            if ($Category) {
                $control.SetProperty('Category', $Category)
            }
            
            if ($Tags -and $Tags.Count -gt 0) {
                $control.SetProperty('Tags', $Tags -join ',')
            }
            
            # Set script source information
            $control.SetProperty('ScriptSource', $scriptSource)
            
            if ($scriptSource -eq 'File') {
                $control.SetProperty('ScriptPath', $resolvedScriptPath)
            }
            else {
                $control.SetProperty('ScriptBlock', $scriptContent)
            }
            
            # Store parameter control definitions as JSON for the WPF side
            if ($parameterControls.Count -gt 0) {
                $paramJson = $parameterControls | ConvertTo-Json -Depth 5 -Compress
                $control.SetProperty('ParameterControls', $paramJson)
            }
            else {
                $control.SetProperty('ParameterControls', '[]')
            }
            
            # Store default parameters
            if ($DefaultParameters.Count -gt 0) {
                $defaultsJson = $DefaultParameters | ConvertTo-Json -Depth 3 -Compress
                $control.SetProperty('DefaultParameters', $defaultsJson)
            }
            
            # Script cards don't participate in wizard parameter collection
            $control.Mandatory = $false
            
            # Add to step
            $wizardStep.AddControl($control)
            
            Write-Verbose "Successfully added ScriptCard control: $($control.ToString())"
            Write-Verbose "  Title: $Title"
            Write-Verbose "  Parameters: $($parameterControls.Count)"
            
            # Return the control object
            return $control
        }
        catch {
            Write-Error "Failed to add ScriptCard control '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }
    
    end {
        Write-Verbose "Add-UIScriptCard completed for: $Name"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardScriptCard' -Value 'Add-UIScriptCard'
