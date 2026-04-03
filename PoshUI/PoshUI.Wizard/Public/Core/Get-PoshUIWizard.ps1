function Get-PoshUIWizard {
    <#
    .SYNOPSIS
    Retrieves the current PoshUI wizard definition for inspection and debugging.
    
    .DESCRIPTION
    Returns the current wizard definition including all steps, controls, and properties.
    Useful for debugging, inspecting the wizard structure, and troubleshooting rendering issues.
    
    .PARAMETER IncludeProperties
    Include detailed property information for each control.
    
    .PARAMETER StepName
    Filter to a specific step by name.
    
    .PARAMETER AsJson
    Return the wizard definition as JSON (same format sent to the C# frontend).
    
    .EXAMPLE
    Get-PoshUIWizard
    
    Returns a summary of the current wizard with steps and control counts.
    
    .EXAMPLE
    Get-PoshUIWizard -IncludeProperties
    
    Returns detailed information including all control properties.
    
    .EXAMPLE
    Get-PoshUIWizard -StepName "Config"
    
    Returns information for a specific step only.
    
    .EXAMPLE
    Get-PoshUIWizard -AsJson | Out-File wizard.json
    
    Exports the wizard definition as JSON for inspection.
    
    .OUTPUTS
    PSCustomObject representing the wizard definition, or JSON string if -AsJson is specified.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [switch]$IncludeProperties,
        
        [Parameter()]
        [string]$StepName,
        
        [Parameter()]
        [switch]$AsJson
    )
    
    begin {
        if (-not $script:CurrentWizard) {
            Write-Warning "No wizard initialized. Call New-PoshUIWizard first."
            return
        }
    }
    
    process {
        try {
            if ($AsJson) {
                # Return serialized JSON
                $json = Serialize-UIDefinition -Definition $script:CurrentWizard
                return $json
            }
            
            # Filter to specific step if requested
            $stepsToShow = $script:CurrentWizard.Steps
            if ($StepName) {
                $stepsToShow = $script:CurrentWizard.Steps | Where-Object { $_.Name -eq $StepName }
                if (-not $stepsToShow) {
                    Write-Warning "Step '$StepName' not found. Available steps: $($script:CurrentWizard.Steps.Name -join ', ')"
                    return
                }
            }
            
            # Build summary object
            $summary = [PSCustomObject]@{
                Title = $script:CurrentWizard.Title
                Theme = $script:CurrentWizard.Theme
                TotalSteps = $script:CurrentWizard.Steps.Count
                Steps = @()
            }
            
            foreach ($step in $stepsToShow) {
                $stepInfo = [PSCustomObject]@{
                    Name = $step.Name
                    Title = $step.Title
                    Order = $step.Order
                    Type = $step.Type
                    Description = $step.Description
                    Icon = $step.Icon
                    TotalControls = $step.Controls.Count
                    Controls = @()
                }
                
                foreach ($control in $step.Controls) {
                    $controlInfo = [PSCustomObject]@{
                        Name = $control.Name
                        Type = $control.Type
                        Label = $control.Label
                        Mandatory = $control.Mandatory
                    }
                    
                    if ($IncludeProperties) {
                        $controlInfo | Add-Member -NotePropertyName 'Properties' -NotePropertyValue $control.Properties
                    }
                    
                    $stepInfo.Controls += $controlInfo
                }
                
                $summary.Steps += $stepInfo
            }
            
            return $summary
        }
        catch {
            Write-Error "Failed to retrieve wizard definition: $($_.Exception.Message)"
            throw
        }
    }
}

Set-Alias -Name 'Get-PoshWizard' -Value 'Get-PoshUIWizard'
