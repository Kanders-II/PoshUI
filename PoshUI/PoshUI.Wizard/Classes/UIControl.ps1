# UIControl.ps1 - UI control definition class

class UIControl {
    [string]$Name
    [string]$Label
    [string]$Type  # TextBox, Password, Checkbox, Toggle, Dropdown, DropdownFromCsv, FilePath, FolderPath, Card, Numeric, Date, OptionGroup
    [object]$Default
    [bool]$Mandatory = $false
    [string]$ValidationPattern
    [string]$ValidationMessage
    [string]$HelpText
    [int]$Width
    [hashtable]$Properties
    [array]$Choices  # For dropdowns
    
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
        
        # Validate basic properties
        if ([string]::IsNullOrWhiteSpace($this.Name)) {
            $errors += "Control name is required"
        }
        
        if ([string]::IsNullOrWhiteSpace($this.Label)) {
            $errors += "Control label is required"
        }
        
        if ([string]::IsNullOrWhiteSpace($this.Type)) {
            $errors += "Control type is required"
        }
        
        # Validate control type
        $validTypes = @('TextBox', 'Password', 'Checkbox', 'Toggle', 'Dropdown', 'DropdownFromCsv', 'FilePath', 'FolderPath', 'Card', 'Numeric', 'Date', 'OptionGroup')
        if ($this.Type -notin $validTypes) {
            $errors += "Invalid control type '$($this.Type)'. Valid types: $($validTypes -join ', ')"
        }
        
        # Type-specific validation
        switch ($this.Type) {
            'Dropdown' {
                if (-not $this.Choices -or $this.Choices.Count -eq 0) {
                    $errors += "Dropdown control must have choices defined"
                }
            }
            'DropdownFromCsv' {
                $csvPath = $this.GetProperty('CsvPath')
                $valueColumn = $this.GetProperty('ValueColumn')
                if ([string]::IsNullOrWhiteSpace($csvPath)) {
                    $errors += "DropdownFromCsv control must have CsvPath property"
                }
                if ([string]::IsNullOrWhiteSpace($valueColumn)) {
                    $errors += "DropdownFromCsv control must have ValueColumn property"
                }
            }
            'Card' {
                $title = $this.GetProperty('Title')
                $content = $this.GetProperty('Content')
                if ([string]::IsNullOrWhiteSpace($title)) {
                    $errors += "Card control must have Title property"
                }
                if ([string]::IsNullOrWhiteSpace($content)) {
                    $errors += "Card control must have Content property"
                }
            }
            'FilePath' {
                $filter = $this.GetProperty('Filter')
                if ([string]::IsNullOrWhiteSpace($filter)) {
                    $this.SetProperty('Filter', 'All Files (*.*)|*.*')
                }
            }
            'Numeric' {
                $step = $this.GetPropertyOrDefault('Step', 0)
                if ($null -ne $step -and $step -le 0) {
                    $errors += "Numeric control step must be greater than 0"
                }
            }
            'Date' {
                # No additional validation currently required
            }
            'OptionGroup' {
                if (-not $this.Choices -or $this.Choices.Count -eq 0) {
                    $errors += "OptionGroup control must define one or more options"
                }
            }
            'TextBox' {
                if ($this.GetPropertyOrDefault('Multiline', $false)) {
                    $rows = $this.GetPropertyOrDefault('Rows', 0)
                    if ($rows -and $rows -le 0) {
                        $errors += "Multi-line text area must specify Rows greater than 0"
                    }
                }
            }
        }
        
        # Validate validation pattern if provided
        if (-not [string]::IsNullOrWhiteSpace($this.ValidationPattern)) {
            try {
                [regex]::new($this.ValidationPattern) | Out-Null
            }
            catch {
                $errors += "Invalid validation pattern: $($_.Exception.Message)"
            }
        }
        
        # Validate width if provided
        if ($this.Width -lt 0) {
            $errors += "Width must be greater than or equal to 0"
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

