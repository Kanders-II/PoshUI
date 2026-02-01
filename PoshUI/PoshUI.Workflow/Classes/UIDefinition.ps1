# UIDefinition.ps1 - UI definition class for PoshUI.Workflow

<#
.SYNOPSIS
Defines the structure and configuration of a PoshUI Workflow interface.

.DESCRIPTION
UIDefinition class represents a complete UI with all its steps, controls, branding,
and configuration. This version is for the Workflow template with sequential task execution.
#>

class UIDefinition {
    [string]$Title
    [string]$Description
    [string]$Icon
    [string]$SidebarHeaderText
    [string]$WindowTitleIcon
    [string]$SidebarHeaderIcon
    [string]$SidebarHeaderIconOrientation = 'Left'
    [string]$Theme = 'Auto'
    [bool]$AllowCancel = $true
    [array]$Steps
    [hashtable]$Branding
    [hashtable]$GlobalActions
    [hashtable]$Variables
    [scriptblock]$ScriptBody

    # Template/View Mode properties - hardcoded to Workflow for this module
    [string]$ViewMode = 'Workflow'
    [string]$Template = 'Workflow'
    [int]$GridColumns = 3

    # State persistence
    [string]$StateFilePath
    
    # Constructor
    UIDefinition() {
        $this.Steps = @()
        $this.Branding = @{}
        $this.GlobalActions = @{}
        $this.Variables = @{}
    }

    UIDefinition([string]$Title) {
        $this.Title = $Title
        $this.Steps = @()
        $this.Branding = @{}
        $this.GlobalActions = @{}
        $this.Variables = @{}
    }
    
    # Methods
    [void]AddStep([UIStep]$Step) {
        if ($this.Steps | Where-Object Name -eq $Step.Name) {
            throw "Step with name '$($Step.Name)' already exists"
        }
        $this.Steps += $Step
    }

    [UIStep]GetStep([string]$Name) {
        $step = $this.Steps | Where-Object Name -eq $Name
        if (-not $step) {
            throw "Step with name '$Name' not found"
        }
        return $step
    }

    [bool]HasStep([string]$Name) {
        return $null -ne ($this.Steps | Where-Object Name -eq $Name)
    }

    [UIStep]GetCurrentStep() {
        if ($this.Steps.Count -eq 0) {
            return $null
        }
        return $this.Steps[-1]
    }
    
    [void]SetBranding([hashtable]$BrandingOptions) {
        foreach ($key in $BrandingOptions.Keys) {
            $this.Branding[$key] = $BrandingOptions[$key]
        }
    }
    
    [void]SetScriptBody([scriptblock]$ScriptBody) {
        $this.ScriptBody = $ScriptBody
    }
    
    [hashtable]Validate() {
        $errors = @()
        $warnings = @()
        
        if ([string]::IsNullOrWhiteSpace($this.Title)) {
            $errors += "Workflow title is required"
        }
        
        if ($this.Steps.Count -eq 0) {
            $errors += "At least one step is required"
        }
        
        $stepNames = $this.Steps | Group-Object Name | Where-Object Count -gt 1
        foreach ($duplicate in $stepNames) {
            $errors += "Duplicate step name: '$($duplicate.Name)'"
        }
        
        return @{
            IsValid = ($errors.Count -eq 0)
            Errors = $errors
            Warnings = $warnings
        }
    }
    
    [hashtable]ToState() {
        # Convert steps to serializable format
        $stepsData = @()
        foreach ($step in $this.Steps) {
            $stepHash = @{
                Name = $step.Name
                Title = $step.Title
                Order = $step.Order
                Type = $step.Type
            }
            if ($step.Tasks) {
                $stepHash['Tasks'] = @($step.Tasks | ForEach-Object { $_.ToHashtable() })
            }
            if ($step.Controls) {
                $stepHash['Controls'] = @($step.Controls | ForEach-Object { $_.ToHashtable() })
            }
            $stepsData += $stepHash
        }

        return @{
            Title = $this.Title
            Description = $this.Description
            Theme = $this.Theme
            ViewMode = $this.ViewMode
            Template = $this.Template
            Steps = $stepsData
            Variables = $this.Variables
            Branding = $this.Branding
            SchemaVersion = 1
            LastSaveTime = [datetime]::Now
        }
    }

    [string]ToString() {
        return "UIDefinition: '$($this.Title)' ($($this.Steps.Count) steps)"
    }
}
