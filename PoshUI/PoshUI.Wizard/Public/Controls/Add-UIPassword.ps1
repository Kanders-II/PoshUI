function Add-UIPassword {
    <#
    .SYNOPSIS
    Adds a secure password input control to a UI step.
    
    .DESCRIPTION
    Creates a password field that securely collects sensitive information.
    Returns a SecureString in the generated script. Supports optional reveal button and minimum length validation.
    
    .PARAMETER Step
    Name of the step to add this control to. The step must already exist.
    
    .PARAMETER Name
    Unique name for the control. This becomes the parameter name in the generated script.
    
    .PARAMETER Label
    Display label shown next to the password field.
    
    .PARAMETER Mandatory
    Whether this field is required. Users cannot proceed without entering a value.
    
    .PARAMETER MinLength
    Minimum password length required. Shows validation error if not met.
    
    .PARAMETER ValidationPattern
    Regular expression pattern the password must match.
    Use this for pattern-based validation (e.g., must contain uppercase, lowercase, number, special char).
    
    .PARAMETER ValidationScript
    Script block that validates the password. Receives the password as $_ and should return $true if valid.
    This allows complex validation logic that regex cannot express.
    Note: The password is provided as a plain string to the script block for validation purposes only.
    
    .PARAMETER ValidationMessage
    Custom error message to display when validation fails.
    
    .PARAMETER ShowRevealButton
    Whether to show the eye icon that reveals the password (default: $true).
    
    .PARAMETER Width
    Preferred width of the control in pixels.
    
    .PARAMETER HelpText
    Help text or tooltip to display for this control.
    
    .EXAMPLE
    Add-UIPassword -Step "Security" -Name "AdminPassword" -Label "Administrator Password" -Mandatory
    
    Adds a required password field.
    
    .EXAMPLE
    Add-UIPassword -Step "Security" -Name "Password" -Label "Password" -MinLength 8 -ShowRevealButton $false
    
    Adds a password field with minimum length validation and no reveal button.
    
    .EXAMPLE
    Add-UIPassword -Step "Security" -Name "Password" -Label "Password" `
        -ValidationPattern '^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$' `
        -ValidationMessage "Password must be at least 12 characters and contain uppercase, lowercase, number, and special character"
    
    Adds a password field with complex regex validation.
    
    .EXAMPLE
    Add-UIPassword -Step "Security" -Name "Password" -Label "Password" `
        -ValidationScript { 
            $_ -match '[A-Z]' -and 
            $_ -match '[a-z]' -and 
            $_ -match '\d' -and 
            $_ -notmatch 'password|admin|12345'
        } `
        -ValidationMessage "Password must contain uppercase, lowercase, number, and not common passwords"
    
    Adds a password field with script block validation for complex rules.
    
    .OUTPUTS
    UIControl object representing the created password input.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string]$Step,
        
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullOrEmpty()]
        [string]$Name,
        
        [Parameter(Mandatory = $true, Position = 2)]
        [ValidateNotNullOrEmpty()]
        [string]$Label,
        
        [Parameter()]
        [switch]$Mandatory,
        
        [Parameter()]
        [int]$MinLength,
        
        [Parameter()]
        [string]$ValidationPattern,
        
        [Parameter()]
        [scriptblock]$ValidationScript,
        
        [Parameter()]
        [string]$ValidationMessage,
        
        [Parameter()]
        [bool]$ShowRevealButton = $true,
        
        [Parameter()]
        [int]$Width,
        
        [Parameter()]
        [string]$HelpText
    )
    
    begin {
        Write-Verbose "Adding Password control: $Name to step: $Step"
        
        if (-not $script:CurrentWizard) {
            throw "No UI initialized. Call New-PoshUI first."
        }
        
        if (-not $script:CurrentWizard.HasStep($Step)) {
            throw "Step '$Step' does not exist. Add the step first using Add-UIStep."
        }
    }
    
    process {
        try {
            $wizardStep = $script:CurrentWizard.GetStep($Step)
            
            if ($wizardStep.HasControl($Name)) {
                throw "Control with name '$Name' already exists in step '$Step'"
            }
            
            # Create the control with SecureString type
            $control = [UIControl]::new($Name, $Label, 'Password')
            $control.Mandatory = $Mandatory.IsPresent
            $control.HelpText = $HelpText
            $control.Width = $Width
            
            # Set validation properties
            if ($PSBoundParameters.ContainsKey('ValidationPattern')) {
                $control.ValidationPattern = $ValidationPattern
            }
            if ($PSBoundParameters.ContainsKey('ValidationScript')) {
                # Store script block as string for serialization
                $control.SetProperty('ValidationScript', $ValidationScript.ToString())
            }
            if ($PSBoundParameters.ContainsKey('ValidationMessage')) {
                $control.ValidationMessage = $ValidationMessage
            }
            
            # Set password-specific properties
            if ($PSBoundParameters.ContainsKey('MinLength')) {
                $control.SetProperty('MinLength', $MinLength)
            }
            if ($PSBoundParameters.ContainsKey('ShowRevealButton')) {
                $control.SetProperty('ShowRevealButton', $ShowRevealButton)
            }
            
            # Add to step
            $wizardStep.AddControl($control)
            
            Write-Verbose "Successfully added Password control: $($control.ToString())"
            
            return $control
        }
        catch {
            Write-Error "Failed to add Password control '$Name' to step '$Step': $($_.Exception.Message)"
            throw
        }
    }
    
    end {
        Write-Verbose "Add-UIPassword completed for: $Name"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardPassword' -Value 'Add-UIPassword'
