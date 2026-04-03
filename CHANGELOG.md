# Changelog

All notable changes to PoshUI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.3.0] - 2026-03-23

### Dual-Mode Custom Themes

- **`Set-UITheme -Light $hash -Dark $hash`** - Define independent color palettes for light and dark modes using PowerShell hashtables; the engine applies the correct palette automatically based on the active theme
- **Theme toggle button** - Sun/moon icon in the title bar allows users to switch between light and dark themes at runtime; custom color palettes persist across toggles
- **22 color slots** - Override any combination of: `Background`, `ContentBackground`, `CardBackground`, `SidebarBackground`, `SidebarText`, `SidebarHighlight`, `TextPrimary`, `TextSecondary`, `AccentColor`, `ButtonBackground`, `ButtonForeground`, `InputBackground`, `InputBorder`, `BorderColor`, `TitleBarBackground`, `TitleBarText`, `SuccessColor`, `WarningColor`, `ErrorColor`, `HeadingForeground`, `BodyForeground`, `SecondaryForeground`
- Partial overrides supported - only specify the slots you want to change; unspecified slots use the built-in theme defaults

### PNG Icon Support

- **`Add-UIStep -IconPath`** - Display colored PNG/ICO images in the sidebar instead of monochrome Segoe MDL2 glyphs; supported in Wizard, Dashboard, and Workflow modules
- **`Add-UIMetricCard -IconPath`** - Display PNG icons on metric cards in dashboards; falls back to glyph icon when no path is specified
- **`Add-UICard -IconPath`** - Display PNG icons on info cards in dashboards
- **`Add-UIBanner -IconPath`** - PNG image rendering for banner icons (previously glyph-only in practice)
- **`Set-UIBranding -SidebarHeaderIcon`** - Sidebar header now accepts PNG/ICO file paths for full-color branding logos
- **`Set-UIBranding -WindowTitleIcon`** - Window title bar icon accepts PNG/ICO file paths
- **Automatic fallback** - When `-IconPath` is specified, the PNG image takes priority; if the file is missing or invalid, the glyph icon (`-Icon`) is displayed instead
- **High-quality rendering** - All PNG icons use `BitmapScalingMode.HighQuality` for crisp display at any size

### New Converters

- `FilePathToImageSourceConverter` - Converts file path strings to WPF `BitmapImage` with `OnLoad` caching and thread-safe `Freeze()`; returns null gracefully for missing or invalid paths

### New Properties

- `MetricCardViewModel.IconPath` (`string`) - File path for PNG icon on metric cards
- `MetricCardViewModel.HasIconPath` (`bool`) - Computed visibility flag for PNG vs glyph icon toggle
- `CardViewModel.IconPath` (`string`) - File path for PNG icon on info cards
- `StepItem.IconFilePath` (`string`) - File path for PNG icon in sidebar steps

### Carousel PNG Icons

- **Per-slide PNG icons** - Carousel banners now support PNG icons via `IconPath` property in slide hashtables; icons appear beside slide title and subtitle
- **Icon customization** - Per-slide control of `IconSize`, `IconColor`, `IconPosition` properties
- **Font customization** - Per-slide control of `TitleFontSize`, `SubtitleFontSize`, `TitleFontWeight` properties
- **Cross-module support** - Works in Wizard (`-CarouselItems`), Dashboard (`-CarouselSlides`), and Workflow (`-CarouselItems`) modules

### Documentation Updates

- **New screenshots** - Added 4 new screenshots showing PNG icons in wizards, dashboards, and ScriptCards
- **PNG icon documentation** - Comprehensive documentation for PNG icon support across all modules with code examples
- **Custom theme documentation** - Detailed guides for dual-mode custom themes with hashtable syntax
- **Carousel documentation** - Updated carousel-clickable-links.md with PNG icon properties and examples
- **Module guides** - Updated Wizard, Dashboard, and Workflow about pages with v1.3.0 features
- **Icons8 attribution** - Added required attribution for Icons8 icons used in examples
- **Fluent Emoji attribution** - Added attribution for Microsoft Fluent Emoji icons
- **VitePress updates** - Rebuilt documentation site with all v1.3.0 content

### Bug Fixes

- **Fixed theme toggle not applying custom colors** - Theme toggle now correctly re-applies the light or dark custom palette when switching modes
- **Fixed InfoCard property name mismatch** - `ConvertTo-UIScript` now correctly reads `IconPath`, `Icon`, `ImagePath`, `LinkUrl`, `LinkText`, and `BackgroundColor` from `Add-UICard` controls (previously only checked `Card`-prefixed property names)
- **Fixed MetricCard IconPath not flowing through JSON pipeline** - `JsonDefinitionLoader` now extracts `IconPath` from the card `Properties` dictionary for all card types
- **Fixed carousel PNG icons not rendering** - Added `[OnDeserialized]` callback to restore property defaults that `DataContractJsonSerializer` bypasses (fixes `IconSize=0` issue)

---

## [1.2.0] - 2026-03-01

### Dashboard Visual Polish

- **Accent color bar** - 3px left-edge colored accent bar on all card types (MetricCard, GraphCard, DataGridCard, ScriptCard, InfoCard, StatusIndicatorCard); bound to `AccentBrush` property with Windows blue default
- **Loading shimmer overlay** - Pulsing opacity overlay on refreshable cards (MetricCard, GraphCard, DataGridCard) while `IsRefreshing` is true; uses `CardShimmerOverlay` shared style with SineEase animation
- **Card styling** - Subtle drop shadow on all dashboard cards via `DashboardCardStyle`
- Note: Entrance fade/slide and hover scale animations were removed due to stack overflow crashes caused by frozen Freezable sharing in WPF Style setters with complex nested XAML

### New Card Features

- **MetricCard sparkline** - Mini line chart rendered via custom `SparklineControl` Canvas control; provide `SparklineData` (array of doubles) to display trend history with accent-colored fill
- **Donut chart** - New `ChartType = 'Donut'` on GraphCard renders a pie chart with center hole and total value label; extends existing Pie chart with `IsDonutChart` and `FormattedTotal` properties
- **Gauge/radial progress** - New `ShowGauge` property on MetricCard renders a 270-degree radial arc via custom `GaugeControl`; shows value and unit centered inside the arc
- **StatusIndicator card** - New `StatusIndicatorCardViewModel` with colored status dots; auto-colors items based on common status strings (Online=green, Warning=amber, Offline=red, Maintenance=gray); use `Type = 'StatusCard'` with `Data` array of `@{ Label; Status }` objects

### Dashboard Layout

- Dashboard cards use `WrapPanel` for responsive wrapping layout
- Note: `DashboardGridPanel` (uniform column grid) was developed but removed due to infinite layout recursion when used with `ItemsControl` container generation; may be revisited in future with proper measure/arrange cycle protection

### Sidebar Polish

- **Active page indicator** - Animated 3px left accent bar on the current sidebar step with fade in/out transitions
- **Hover effect** - Subtle background highlight on sidebar step hover using `IconButtonHoverBrush`
- **Current step highlight** - Subtle semi-transparent background on the active step for visual clarity

### New Controls

- `Launcher\Controls\SparklineControl.cs` - Lightweight Canvas-based sparkline with `Data`, `Stroke`, `StrokeThickness`, `ShowFill` properties; auto-scales to data range with translucent fill area
- `Launcher\Controls\GaugeControl.cs` - Radial gauge Canvas with `Value`, `MinValue`, `MaxValue`, `GaugeBrush`, `TrackBrush`, `Thickness` properties; renders 270-degree arc with rounded caps
- `Launcher\Controls\DashboardGridPanel.cs` - Responsive WPF Panel with `MinColumnWidth`, `MaxColumns`, `RowSpacing` properties; auto-flows children in uniform grid columns

### New ViewModels

- `StatusIndicatorCardViewModel` - Dashboard card for service/item status lists with `Items` collection of `StatusItem` objects
- `StatusItem` - Model with `Label`, `Status`, `StatusColor` properties and `StatusItem.Create()` factory with auto-color mapping

### New Properties

- `MetricCardViewModel.SparklineData` (`ObservableCollection<double>`) - Data points for sparkline mini-chart
- `MetricCardViewModel.HasSparkline` - Computed visibility flag
- `MetricCardViewModel.ShowGauge` - Toggle between numeric display and radial gauge
- `GraphCardViewModel.IsDonutChart` - Computed flag for Donut chart type
- `GraphCardViewModel.FormattedTotal` - Formatted sum of pie/donut slice values
- `CardData.SparklineData` (`double[]`) - JSON input for sparkline data
- `CardData.ShowGauge` (`bool?`) - JSON input for gauge mode

---

## [1.1.0] - 2026-03-01

### 🐛 Bug Fixes

- **Fixed FlyoutWindow crash** - Removed illegal XAML data binding on `RichTextBox.Document` (not a DependencyProperty in .NET Framework 4.8); document is now set in code-behind

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
