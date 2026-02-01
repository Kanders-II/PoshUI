# UIStepType.ps1 - Step type enumeration for PoshUI

<#
.SYNOPSIS
Defines the available step types for PoshUI steps.

.DESCRIPTION
UIStepType enum specifies the type of content and controls displayed in a step.
This determines which controls and layouts are appropriate for the step.

.NOTES
Step types correspond to templates:
- Wizard: Standard input controls (TextBox, Dropdown, Checkbox, etc.)
- Dashboard: Visualization cards (MetricCard, GraphCard, DataGridCard, etc.)
- Workflow: Sequential task execution with progress tracking
#>

enum UIStepType {
    # Standard wizard step with input controls
    Wizard = 0

    # Dashboard step with visualization and interactive cards
    Dashboard = 1

    # Workflow step with sequential task execution
    Workflow = 2

    # Legacy compatibility types
    GenericForm = 0
    CardGrid = 1
}
