function Add-UIStep {
    <#
    .SYNOPSIS
    Adds a new step to the current UI.

    .DESCRIPTION
    Creates a new UI step that can contain controls and defines the structure of the UI.
    Steps are displayed in order and can be of different types (Wizard or Dashboard).
    
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
    - Wizard: Standard input form (default) - supports input controls like TextBox, Dropdown, etc.
    - Dashboard: Dashboard view with ScriptCards and visualization cards (MetricCard, GraphCard, DataGridCard)
    
    .PARAMETER Icon
    Optional icon for the step in the sidebar. Must be a Segoe MDL2 icon glyph.
    Format: '&#xE1D3;' (e.g., '&#xE968;' for Network, '&#xE72E;' for Shield)
    Note: Image file paths are NOT supported for sidebar step icons.
    See Docs/FLUENT_ICONS_REFERENCE.md for available glyphs.
    
    .PARAMETER Skippable
    Whether this step can be skipped by the user.
    
    .EXAMPLE
    Add-UIStep -Name "ServerConfig" -Title "Server Configuration" -Order 1

    Adds a basic input form step.

    .EXAMPLE
    Add-UIStep -Name "Welcome" -Title "Welcome" -Order 1 -Icon "&#xE8BC;" -Description "Get started"

    Adds a welcome step with a home icon.

    .OUTPUTS
    UIStep object representing the created step.

    .NOTES
    This function requires that New-PoshUI has been called first to initialize the UI context.
    #>
    [CmdletBinding()]
    # [OutputType([UIStep])] # Commented out to avoid type loading issues
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
        [ValidateSet('Wizard', 'Dashboard')]
        [string]$Type,
        
        [Parameter()]
        [string]$Icon,
        
        [Parameter()]
        [switch]$Skippable
    )
    
    begin {
        Write-Verbose "Adding UI step: $Name ($Title)"

        # Ensure UI is initialized
        if (-not $script:CurrentWizard) {
            throw "No UI initialized. Call New-PoshUI first."
        }
    }

    process {
        try {
            # Auto-assign order if not specified
            if (-not $PSBoundParameters.ContainsKey('Order')) {
                $Order = $script:CurrentWizard.Steps.Count + 1
            }

            # Use wizard's Template as default type if not specified
            if (-not $PSBoundParameters.ContainsKey('Type')) {
                $Type = $script:CurrentWizard.Template
            }

            # Check for duplicate step name
            if ($script:CurrentWizard.HasStep($Name)) {
                throw "Step with name '$Name' already exists"
            }

            # Create new step
            $step = [UIStep]::new($Name, $Title, $Order)
            $step.Description = $Description
            $step.Type = $Type
            $step.Icon = $Icon
            $step.Skippable = $Skippable.IsPresent

            # Add to UI
            $script:CurrentWizard.AddStep($step)

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
Set-Alias -Name 'Add-WizardStep' -Value 'Add-UIStep'

