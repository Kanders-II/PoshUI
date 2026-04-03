# Version History

All notable changes to the PoshUI project are documented here.

## [1.3.0] - 2026-03-23

### Dual-Mode Custom Themes & Theme Toggle

PoshUI now supports **independent color palettes for light and dark modes** via `Set-UITheme -Light $hash -Dark $hash`. Users can toggle between themes at runtime using the **sun/moon button** in the title bar, and custom colors persist across toggles.

**22 overridable color slots:** `Background`, `ContentBackground`, `CardBackground`, `SidebarBackground`, `SidebarText`, `SidebarHighlight`, `TextPrimary`, `TextSecondary`, `AccentColor`, `ButtonBackground`, `ButtonForeground`, `InputBackground`, `InputBorder`, `BorderColor`, `TitleBarBackground`, `TitleBarText`, `SuccessColor`, `WarningColor`, `ErrorColor`, `HeadingForeground`, `BodyForeground`, `SecondaryForeground`

```powershell
Set-UITheme -Light @{
    Background       = '#FFF0F5'
    AccentColor      = '#E91E63'
    SidebarBackground = '#880E4F'
} -Dark @{
    Background       = '#1A1A2E'
    AccentColor      = '#00BFA5'
    SidebarBackground = '#0A1A18'
}
```

### PNG Icon Support

All modules now support colored PNG/ICO images as icons, replacing or supplementing monochrome Segoe MDL2 glyphs.

- `Add-UIStep -IconPath` - Sidebar step icons (Wizard, Dashboard, Workflow)
- `Add-UIMetricCard -IconPath` - Metric card icons (Dashboard)
- `Add-UICard -IconPath` - Info card icons (Dashboard)
- `Add-UIBanner -IconPath` - Banner overlay icons
- `Set-UIBranding -SidebarHeaderIcon` - Full-color sidebar header logos
- `Set-UIBranding -WindowTitleIcon` - Window title bar icon

PNG icons fall back to glyph icons automatically if the file path is invalid.

### New Examples

- `Test-CustomTheme-Dashboard.ps1` - Dual-mode themes with PNG icons
- `Test-CustomTheme-Workflow.ps1` - Dual-mode themes with PNG icons
- `Wizard-EmojiIcons.ps1` - PNG emoji icons in sidebar steps

### Bug Fixes

- Theme toggle now re-applies custom color palettes when switching modes
- `ConvertTo-UIScript` correctly maps `IconPath`, `Icon`, and other properties from `Add-UICard`
- `JsonDefinitionLoader` extracts `IconPath` from card Properties dictionary

---

## [1.0.0] - 2026-01-15

### 🎉 Initial Public Release

PoshUI v1.0.0 is the first public release of a PowerShell UI framework for building professional Windows 11-style wizards, dashboards, and workflows.

### 📦 Three Independent Modules

- **PoshUI.Wizard**: Step-by-step data collection with 12+ built-in controls
- **PoshUI.Dashboard**: Real-time monitoring with metric cards, charts, and tables
- **PoshUI.Workflow**: Multi-task automation with reboot/resume capability

### 🎯 Core Features

**Dashboard Cards:**
- `Add-UIMetricCard` - KPI metrics with trends and targets
- `Add-UIChartCard` - Data visualization (Line, Bar, Area, Pie)
- `Add-UITableCard` - Tabular data display
- `Add-UICard` - Informational content

**Developer Tools:**
- `Get-PoshUIDashboard` - Inspect and debug dashboard structure
- BannerStyle presets - Simplified banner creation
- Verbose output - Detailed ScriptBlock execution feedback
- Enhanced error messages - Context-aware suggestions

**Workflow Capabilities:**
- Reboot & Resume - Workflows save state and continue after restart
- Auto-Progress - Progress bar updates from script output
- Workflow Context - Access `$PoshUIWorkflow` in tasks

### 🏗️ Architecture

- **JSON Serialization**: Dashboard & Workflow modules use JSON for PowerShell-to-C# communication
- **AST Parsing**: Wizard module uses AST parsing for compatibility
- **Shared Engine**: All modules use the same WPF-based `PoshUI.exe` engine
- **Zero Dependencies**: No third-party libraries or NuGet packages

### 🧪 Testing

- 152 automated tests (58 PowerShell + 94 C#)
- Comprehensive test runners in `Tests/` folder
- CI/CD ready with JUnit XML output

### 📚 Documentation

- Complete cmdlet reference
- Dashboard cards reference
- Troubleshooting guide
- Working examples for all modules
- Get started guide

### 🎨 UI Features

- Light/Dark theme support with Auto detection
- Windows 11-style modern interface
- Live execution console
- Real-time data refresh
- Category filtering for cards

### 💻 Platform

- Windows PowerShell 5.1
- .NET Framework 4.8
- Windows 10/11 and Server 2016+
- Single executable distribution
