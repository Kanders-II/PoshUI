# Changelog

All notable changes to PoshUI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - 2026-01-31

### 🎉 Initial Release

First public release of **PoshUI** - a PowerShell UI framework for building professional Windows 11-style wizards, dashboards, and workflows.

### Core Modules

**PoshUI.Wizard**
- Step-by-step guided interfaces for configuration and deployment tasks
- 12+ input controls: TextBox, Dropdown, Password, Date, File/Folder pickers, and more
- Dynamic controls with cascading dropdowns and scriptblock data sources
- Built-in validation with regex patterns, mandatory fields, and min/max values
- Live execution console with real-time output streaming

**PoshUI.Dashboard**
- Card-based monitoring interfaces with real-time data visualization
- MetricCards - Display KPIs with icons, trends, and progress indicators
- ChartCards - Bar, Line, Area, and Pie charts with live data
- DataGridCards - Sortable, filterable tables with CSV/TXT export
- ScriptCards - Turn PowerShell scripts into clickable tools for end users
- Category filtering and live refresh capabilities

**PoshUI.Workflow**
- Multi-task workflow execution with progress tracking
- Reboot/resume capability with encrypted state management (DPAPI)
- Auto-progress tracking based on script output
- Workflow context object (`$PoshUIWorkflow`) for task orchestration
- Secure state storage with HMAC-SHA256 integrity verification

### Platform Features

- **Light/Dark Themes** - Auto-detect system theme or force Light/Dark mode
- **Windows 11 Styling** - Modern Fluent Design with translucent effects
- **CMTrace Logging** - Enterprise-ready audit trails for all executions
- **Zero Dependencies** - No third-party libraries or NuGet packages required
- **PowerShell 5.1** - Compatible with Windows PowerShell 5.1 (included with Windows)
- **.NET Framework 4.8** - Pre-installed on Windows 10/11

### Technical Details

- **Architecture**: PowerShell modules + WPF executable hybrid
- **Communication**: JSON serialization for Dashboard, AST parsing for Wizard
- **Security**: SecureString password handling, DPAPI encryption, restrictive ACLs
- **Compatibility**: Windows 10/11, Windows Server 2016+ (x64)

### Examples Included

- `Demo-AllControls.ps1` - Showcase of all control types
- `Demo-HyperV-CreateVM.ps1` - Production-ready VM creation wizard
- `Demo-Dashboard.ps1` - Live system monitoring dashboard
- `Demo-DynamicControls.ps1` - Cascading dropdowns and dynamic data
- `Demo-Workflow.ps1` - Multi-step automation with reboot/resume

### Documentation

- Complete VitePress documentation site
- Cmdlet reference with examples
- Module guides for Wizards, Dashboards, and Workflows
- Control library documentation
- Real-world use cases and patterns

---

## Version Numbering

PoshUI follows [Semantic Versioning](https://semver.org/):
- **Major** - Breaking changes
- **Minor** - New features (backwards compatible)
- **Patch** - Bug fixes and minor improvements
