function Get-PoshUIDashboard {
    <#
    .SYNOPSIS
    Retrieves the current PoshUI dashboard definition for inspection and debugging.
    
    .DESCRIPTION
    Returns the current dashboard definition including all steps, controls, and properties.
    Useful for debugging, inspecting the dashboard structure, and troubleshooting rendering issues.
    
    .PARAMETER IncludeProperties
    Include detailed property information for each control.
    
    .PARAMETER StepName
    Filter to a specific step by name.
    
    .PARAMETER AsJson
    Return the dashboard definition as JSON (same format sent to the C# frontend).
    
    .EXAMPLE
    Get-PoshUIDashboard
    
    Returns a summary of the current dashboard with steps and control counts.
    
    .EXAMPLE
    Get-PoshUIDashboard -IncludeProperties
    
    Returns detailed information including all control properties.
    
    .EXAMPLE
    Get-PoshUIDashboard -StepName "SystemOverview"
    
    Returns information for a specific step only.
    
    .EXAMPLE
    Get-PoshUIDashboard -AsJson | Out-File dashboard.json
    
    Exports the dashboard definition as JSON for inspection.
    
    .OUTPUTS
    PSCustomObject representing the dashboard definition, or JSON string if -AsJson is specified.
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
            Write-Warning "No dashboard initialized. Call New-PoshUIDashboard first."
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
            Write-Error "Failed to retrieve dashboard definition: $($_.Exception.Message)"
            throw
        }
    }
}

Set-Alias -Name 'Get-PoshUI' -Value 'Get-PoshUIDashboard'
