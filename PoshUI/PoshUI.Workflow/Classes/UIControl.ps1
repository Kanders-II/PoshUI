# UIControl.ps1 - UI control definition class

class UIControl {
    [string]$Name
    [string]$Label
    [string]$Type
    [object]$Default
    [bool]$Mandatory = $false
    [string]$ValidationPattern
    [string]$ValidationMessage
    [string]$HelpText
    [int]$Width
    [hashtable]$Properties
    [array]$Choices
    
    # Constructor
    UIControl() {
        $this.Properties = @{}
    }

    UIControl([string]$Name, [string]$Label, [string]$Type) {
        $this.Name = $Name
        $this.Label = $Label
        $this.Type = $Type
        $this.Properties = @{}
    }
    
    # Methods
    [void]SetProperty([string]$Key, [object]$Value) {
        $this.Properties[$Key] = $Value
    }

    [object]GetPropertyOrDefault([string]$Key, [object]$Default = $null) {
        if ($this.Properties.ContainsKey($Key)) {
            return $this.Properties[$Key]
        }
        return $Default
    }
    
    [object]GetProperty([string]$Key) {
        return $this.Properties[$Key]
    }
    
    [void]SetChoices([array]$Choices) {
        $this.Choices = $Choices
    }
    
    [void]SetValidation([string]$Pattern, [string]$Message) {
        $this.ValidationPattern = $Pattern
        $this.ValidationMessage = $Message
    }
    
    [hashtable]Validate() {
        $errors = @()
        $warnings = @()
        
        if ([string]::IsNullOrWhiteSpace($this.Name)) {
            $errors += "Control name is required"
        }
        
        if ([string]::IsNullOrWhiteSpace($this.Label)) {
            $errors += "Control label is required"
        }
        
        if ([string]::IsNullOrWhiteSpace($this.Type)) {
            $errors += "Control type is required"
        }
        
        return @{
            IsValid = ($errors.Count -eq 0)
            Errors = $errors
            Warnings = $warnings
        }
    }
    
    [string]ToString() {
        return "UIControl: '$($this.Label)' (Name: $($this.Name), Type: $($this.Type))"
    }
}
