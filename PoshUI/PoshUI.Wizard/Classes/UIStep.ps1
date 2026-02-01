# UIStep.ps1 - UI step definition class for PowerShell 5.1+

class UIStep {
    [string]$Name
    [string]$Title
    [string]$Description
    [int]$Order
    [string]$Type = 'Wizard'  # Wizard (input forms), Dashboard (cards/visualizations)
    [string]$Icon
    [bool]$Skippable = $false
    [array]$Controls  # Changed from generic List to simple array
    [hashtable]$Properties
    [scriptblock]$ValidationScript
    
    # Constructor
    UIStep() {
        $this.Controls = @()
        $this.Properties = @{}
    }

    UIStep([string]$Name, [string]$Title, [int]$Order) {
        $this.Name = $Name
        $this.Title = $Title
        $this.Order = $Order
        $this.Controls = @()
        $this.Properties = @{}
    }
    
    # Methods
    [void]AddControl([UIControl]$Control) {
        if ($this.Controls | Where-Object Name -eq $Control.Name) {
            throw "Control with name '$($Control.Name)' already exists in step '$($this.Name)'"
        }
        $this.Controls += $Control
    }

    [UIControl]GetControl([string]$Name) {
        $control = $this.Controls | Where-Object Name -eq $Name
        if (-not $control) {
            throw "Control with name '$Name' not found in step '$($this.Name)'"
        }
        return $control
    }
    
    [bool]HasControl([string]$Name) {
        return $null -ne ($this.Controls | Where-Object Name -eq $Name)
    }
    
    [void]SetProperty([string]$Key, [object]$Value) {
        $this.Properties[$Key] = $Value
    }
    
    [object]GetProperty([string]$Key) {
        return $this.Properties[$Key]
    }
    
    [void]SetValidation([scriptblock]$ValidationScript) {
        $this.ValidationScript = $ValidationScript
    }
    
    [hashtable]Validate() {
        $errors = @()
        $warnings = @()
        
        # Validate basic properties
        if ([string]::IsNullOrWhiteSpace($this.Name)) {
            $errors += "Step name is required"
        }
        
        if ([string]::IsNullOrWhiteSpace($this.Title)) {
            $errors += "Step title is required"
        }
        
        if ($this.Order -lt 1) {
            $errors += "Step order must be greater than 0"
        }
        
        # Validate step type (support both new and legacy names for backward compatibility)
        $validTypes = @('Wizard', 'Dashboard', 'GenericForm', 'CardGrid')
        if ($this.Type -notin $validTypes) {
            $errors += "Invalid step type '$($this.Type)'. Valid types: Wizard, Dashboard"
        }
        
        return @{
            IsValid = ($errors.Count -eq 0)
            Errors = $errors
            Warnings = $warnings
        }
    }
    
    [string]ToString() {
        return "UIStep: '$($this.Title)' (Order: $($this.Order), Type: $($this.Type), Controls: $($this.Controls.Count))"
    }
}

