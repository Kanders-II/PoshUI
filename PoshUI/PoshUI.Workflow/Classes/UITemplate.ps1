# UITemplate.ps1 - Template type enumeration for PoshUI

<#
.SYNOPSIS
Defines the available UI template types for PoshUI.

.DESCRIPTION
UITemplate enum specifies the template/layout mode for the UI.
Each template provides a different user experience and is optimized for different use cases.

.NOTES
Current templates (v2.0):
- Wizard: Step-by-step navigation with input controls
- Dashboard: Card-based visualization with metrics and charts
- Workflow: Sequential task execution with progress tracking
#>

enum UITemplate {
    # Step-by-step navigation with input controls
    Wizard = 0

    # Card-based visualization with metrics, charts, and data grids
    Dashboard = 1

    # Sequential task execution with progress tracking
    Workflow = 2
}
