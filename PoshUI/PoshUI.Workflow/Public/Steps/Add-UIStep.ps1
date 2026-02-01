function Add-UIStep {
    <#
    .SYNOPSIS
    Adds a new step to the current Workflow UI.

    .DESCRIPTION
    Creates a new UI step that can contain controls or workflow tasks.
    Steps are displayed in order and can be of different types (Wizard for input, Workflow for task execution).
    
    .PARAMETER Name
    Unique name for the step. This is used internally to reference the step.
    
    .PARAMETER Title
    Display title for the step shown in the sidebar and step header.
    
    .PARAMETER Description
    Optional description displayed below the title.
    
    .PARAMETER Order
    Numeric order for the step. Steps are displayed in ascending order.
    If not specified, steps are ordered by the sequence they are added.
    
    .PARAMETER Type
    Type of step to create. Valid values:
    - Wizard: Standard input form - supports input controls like TextBox, Dropdown, etc.
    - Workflow: Sequential task execution with progress tracking (default for Workflow template)
    
    .PARAMETER Icon
    Optional icon for the step in the sidebar. Must be a Segoe MDL2 icon glyph.
    Format: '&#xE1D3;' (e.g., '&#xE968;' for Network, '&#xE72E;' for Shield)
    
    .PARAMETER Skippable
    Whether this step can be skipped by the user.
    
    .EXAMPLE
    Add-UIStep -Name "Execution" -Title "Execution" -Order 1 -Type Workflow

    Adds a workflow execution step.

    .EXAMPLE
    Add-UIStep -Name "Config" -Title "Configuration" -Order 1 -Type Wizard

    Adds a configuration step with input controls.

    .OUTPUTS
    UIStep object representing the created step.

    .NOTES
    This function requires that New-PoshUIWorkflow has been called first to initialize the UI context.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string]$Name,
        
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullOrEmpty()]
        [string]$Title,
        
        [Parameter()]
        [string]$Description,
        
        [Parameter()]
        [int]$Order,
        
        [Parameter()]
        [ValidateSet('Wizard', 'Workflow')]
        [string]$Type,
        
        [Parameter()]
        [string]$Icon,
        
        [Parameter()]
        [switch]$Skippable
    )
    
    begin {
        Write-Verbose "Adding UI step: $Name ($Title)"

        # Ensure UI is initialized
        if (-not $script:CurrentWorkflow) {
            throw "No UI initialized. Call New-PoshUIWorkflow first."
        }
    }

    process {
        try {
            # Auto-assign order if not specified
            if (-not $PSBoundParameters.ContainsKey('Order')) {
                $Order = $script:CurrentWorkflow.Steps.Count + 1
            }

            # Use Wizard as default type if not specified (Workflow must be explicit)
            if (-not $PSBoundParameters.ContainsKey('Type')) {
                $Type = 'Wizard'
            }

            # Check for duplicate step name
            if ($script:CurrentWorkflow.HasStep($Name)) {
                throw "Step with name '$Name' already exists"
            }

            # Create new step
            $step = [UIStep]::new($Name, $Title, $Order)
            $step.Description = $Description
            $step.Type = $Type
            $step.Icon = $Icon
            $step.Skippable = $Skippable.IsPresent

            # Initialize workflow container for Workflow steps
            if ($Type -eq 'Workflow') {
                $workflow = [UIWorkflow]::new($Title)
                $step.SetProperty('Workflow', $workflow)
            }

            # Add to UI
            $script:CurrentWorkflow.AddStep($step)

            Write-Verbose "Successfully added step: $($step.ToString())"

            # Return the step object to support method chaining
            return $step
        }
        catch {
            Write-Error "Failed to add UI step '$Name': $($_.Exception.Message)"
            throw
        }
    }

    end {
        Write-Verbose "Add-UIStep completed for: $Name"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WorkflowStep' -Value 'Add-UIStep'
