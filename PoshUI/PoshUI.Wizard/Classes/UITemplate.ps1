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

Future templates (v2.1+):
- Form: Single-page form layout
- Timeline: Timeline-based workflow visualization
#>

enum UITemplate {
    # Step-by-step navigation with input controls (default)
    # Optimized for guided workflows and data collection
    Wizard = 0

    # Card-based visualization with metrics, charts, and data grids
    # Optimized for monitoring, reporting, and dashboards
    Dashboard = 1

    # Future templates (commented out until v2.1+)
    # Form = 2        # Single-page form layout
    # Timeline = 3    # Timeline-based workflow visualization
}
