# UIFactory.ps1 - Factory pattern for creating UI components

<#
.SYNOPSIS
Factory class for creating UI components with standardized patterns.

.DESCRIPTION
Provides factory methods for creating UI controls, steps, and definitions
with proper initialization and validation. Supports extensibility through
registered custom types.

.NOTES
Company: A Solution IT LLC
Version: 2.0.0
#>

class UIFactory {
    # Static registry for custom control types
    static [hashtable]$CustomControlTypes = @{}
    static [hashtable]$CustomStepTypes = @{}
    static [hashtable]$CustomTemplates = @{}
    
    # Create a UI control with validation
    static [UIControl] CreateControl([string]$Type, [hashtable]$Properties) {
        # Validate control type
        $validTypes = @('TextBox', 'Password', 'Checkbox', 'Toggle', 'Dropdown', 'ListBox', 
                       'FilePath', 'FolderPath', 'Numeric', 'Date', 'OptionGroup', 'MultiLine',
                       'Card', 'Banner', 'ScriptCard', 'VisualizationCard')
        
        if ($Type -notin $validTypes -and -not [UIFactory]::CustomControlTypes.ContainsKey($Type)) {
            throw "Invalid control type: $Type. Valid types: $($validTypes -join ', ')"
        }
        
        # Check for custom control type
        if ([UIFactory]::CustomControlTypes.ContainsKey($Type)) {
            $factory = [UIFactory]::CustomControlTypes[$Type]
            return & $factory $Properties
        }
        
        # Create standard control
        $control = [UIControl]::new()
        $control.Type = $Type
        
        # Apply properties
        foreach ($key in $Properties.Keys) {
            if ($control.PSObject.Properties.Name -contains $key) {
                $control.$key = $Properties[$key]
            }
        }
        
        return $control
    }
    
    # Create a UI step with validation
    static [UIStep] CreateStep([string]$Name, [string]$Title, [hashtable]$Properties) {
        if ([string]::IsNullOrWhiteSpace($Name)) {
            throw "Step name cannot be empty"
        }
        
        if ([string]::IsNullOrWhiteSpace($Title)) {
            throw "Step title cannot be empty"
        }
        
        # Determine order
        $order = if ($Properties.ContainsKey('Order')) { $Properties['Order'] } else { 1 }
        
        # Create step
        $step = [UIStep]::new($Name, $Title, $order)
        
        # Apply additional properties
        foreach ($key in $Properties.Keys) {
            if ($key -ne 'Order' -and $step.PSObject.Properties.Name -contains $key) {
                $step.$key = $Properties[$key]
            }
        }
        
        return $step
    }
    
    # Create a UI definition with template support
    static [UIDefinition] CreateUI([string]$Title, [string]$Template, [hashtable]$Properties) {
        if ([string]::IsNullOrWhiteSpace($Title)) {
            throw "UI title cannot be empty"
        }
        
        # Validate template
        $validTemplates = @('Wizard', 'Dashboard')
        if ($Template -and $Template -notin $validTemplates -and -not [UIFactory]::CustomTemplates.ContainsKey($Template)) {
            throw "Invalid template: $Template. Valid templates: $($validTemplates -join ', ')"
        }
        
        # Create UI definition
        $ui = [UIDefinition]::new($Title)
        
        # Set template
        if ($Template) {
            $ui.Template = $Template
            $ui.ViewMode = $Template
        }
        
        # Apply properties
        foreach ($key in $Properties.Keys) {
            if ($ui.PSObject.Properties.Name -contains $key) {
                $ui.$key = $Properties[$key]
            }
        }
        
        # Apply custom template configuration if registered
        if ([UIFactory]::CustomTemplates.ContainsKey($Template)) {
            $templateConfig = [UIFactory]::CustomTemplates[$Template]
            & $templateConfig $ui
        }
        
        return $ui
    }
    
    # Register a custom control type
    static [void] RegisterControlType([string]$TypeName, [scriptblock]$Factory) {
        if ([string]::IsNullOrWhiteSpace($TypeName)) {
            throw "Control type name cannot be empty"
        }
        
        if ($null -eq $Factory) {
            throw "Factory scriptblock cannot be null"
        }
        
        [UIFactory]::CustomControlTypes[$TypeName] = $Factory
    }
    
    # Register a custom template
    static [void] RegisterTemplate([string]$TemplateName, [scriptblock]$Configuration) {
        if ([string]::IsNullOrWhiteSpace($TemplateName)) {
            throw "Template name cannot be empty"
        }
        
        if ($null -eq $Configuration) {
            throw "Configuration scriptblock cannot be null"
        }
        
        [UIFactory]::CustomTemplates[$TemplateName] = $Configuration
    }
    
    # Unregister a custom control type
    static [void] UnregisterControlType([string]$TypeName) {
        if ([UIFactory]::CustomControlTypes.ContainsKey($TypeName)) {
            [UIFactory]::CustomControlTypes.Remove($TypeName)
        }
    }
    
    # Unregister a custom template
    static [void] UnregisterTemplate([string]$TemplateName) {
        if ([UIFactory]::CustomTemplates.ContainsKey($TemplateName)) {
            [UIFactory]::CustomTemplates.Remove($TemplateName)
        }
    }
    
    # Get registered custom control types
    static [string[]] GetCustomControlTypes() {
        return [UIFactory]::CustomControlTypes.Keys
    }
    
    # Get registered custom templates
    static [string[]] GetCustomTemplates() {
        return [UIFactory]::CustomTemplates.Keys
    }
}
