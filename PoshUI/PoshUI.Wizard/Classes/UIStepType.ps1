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

The step type is automatically inferred from the controls added to the step,
but can be explicitly set via the Add-UIStep -Type parameter.
#>

enum UIStepType {
    # Standard wizard step with input controls
    # Supports: TextBox, Password, Checkbox, Toggle, Dropdown, ListBox,
    #           FilePath, FolderPath, Numeric, Date, OptionGroup, MultiLine, Card, Banner
    Wizard = 0

    # Dashboard step with visualization and interactive cards
    # Supports: MetricCard - KPIs with trends, targets, progress bars
    #           GraphCard - Bar, Line, Area, Pie charts with auto-scaling
    #           DataGridCard - Sortable, filterable tables with CSV/TXT export
    #           ScriptCard - Interactive PowerShell script execution with live output
    #           VisualizationCard - Generic visualization container
    #           Card - Info/display cards
    #           Banner - Header/info banners
    Dashboard = 1

    # Workflow step with sequential task execution
    # Supports: Task progress tracking, output streaming, approval gates, reboot handling
    Workflow = 2

    # Legacy compatibility types (map to Wizard or Dashboard)
    GenericForm = 0   # Maps to Wizard
    CardGrid = 1      # Maps to Dashboard
}
