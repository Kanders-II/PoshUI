# PoshUI.Dashboard — Cmdlet Reference

Complete reference for every cmdlet exported by the **PoshUI.Dashboard** module (monitoring dashboards, ScriptCards, and visualization cards). See [POSHUI_AUTHORING_GUIDE.md](../POSHUI_AUTHORING_GUIDE.md) for concepts and the ScriptCard deep-dive.

_Auto-generated from the module's runtime metadata and comment-based help (PoshUI 1.3.1). Parameter tables list every non-common parameter._

## Cmdlet index

- [`Add-UIBanner`](#add-uibanner) — Adds a banner component to a UI step for displaying hero content.
- [`Add-UICard`](#add-uicard) — Adds an informational card control to a UI step.
- [`Add-UIChartCard`](#add-uichartcard) — Adds a chart card (graph visualization) to a UI step.
- [`Add-UIMetricCard`](#add-uimetriccard) — Adds a metric card (KPI display) to a UI step.
- [`Add-UIScriptCard`](#add-uiscriptcard) — Adds an executable script card to a UI step in CardGrid view mode.
- [`Add-UIStatusCard`](#add-uistatuscard) — Adds a status indicator card to a UI step.
- [`Add-UIStep`](#add-uistep) — Adds a new step to the current UI.
- [`Add-UITableCard`](#add-uitablecard) — Adds a data grid card (table visualization) to a UI step.
- [`Clear-PoshUIFileState`](#clear-poshuifilestate) — Removes temporary files and logs created by PoshUI.
- [`Clear-PoshUIRegistryState`](#clear-poshuiregistrystate) — Clears PoshUI session state from the Windows Registry.
- [`Clear-PoshUIState`](#clear-poshuistate) — Clears all PoshUI state including registry entries, temporary files, and orphaned processes.
- [`Get-PoshUIDashboard`](#get-poshuidashboard) — Retrieves the current PoshUI dashboard definition for inspection and debugging.
- [`Get-UIConfiguration`](#get-uiconfiguration) — Retrieves global configuration options for PoshUI.
- [`New-PoshUIDashboard`](#new-poshuidashboard) — Initializes a new PoshUI Dashboard definition.
- [`Register-PoshUICleanupTask`](#register-poshuicleanuptask) — Registers a scheduled task to automatically clean up PoshUI state.
- [`Set-UIBranding`](#set-uibranding) — Configures branding and appearance settings for the UI.
- [`Set-UIConfiguration`](#set-uiconfiguration) — Sets global configuration options for PoshUI.
- [`Set-UITheme`](#set-uitheme) — Applies a custom color theme to the UI using a simple PowerShell hashtable.
- [`Show-PoshUIDashboard`](#show-poshuidashboard) — Displays the Dashboard UI and executes the associated script.
- [`Unregister-PoshUICleanupTask`](#unregister-poshuicleanuptask) — Removes the PoshUI automatic cleanup scheduled task.

---

## Add-UIBanner

Adds a banner component to a UI step for displaying hero content.

Creates a highly customizable banner with title, subtitle, optional icon, background image,
gradients, interactive elements, and responsive design. Banners are ideal for welcome screens,
section headers, dashboards, and promotional content.

```
Add-UIBanner [-Step] <String> [[-Name] <String>] [-Title] <String> [-Subtitle <String>] [-Description <String>] [-Category <String>] [-Style <String>] [-BannerStyle <String>] [-BannerConfig <Hashtable>] [-Height <Int32>] [-Width <Int32>] [-FullWidth] [-Layout <String>] [-ContentAlignment <String>] [-VerticalAlignment <String>] [-Padding <String>] [-CornerRadius <Int32>] [-TitleFontSize <Int32>] [-SubtitleFontSize <Int32>] [-DescriptionFontSize <Int32>] [-TitleFontWeight <String>] [-SubtitleFontWeight <String>] [-FontFamily <String>] [-TitleColor <String>] [-SubtitleColor <String>] [-DescriptionColor <String>] [-TitleAllCaps] [-TitleLetterSpacing <Double>] [-BackgroundColor <String>] [-BackgroundImagePath <String>] [-BackgroundImageOpacity <Double>] [-BackgroundImageStretch <String>] [-GradientStart <String>] [-GradientEnd <String>] [-GradientAngle <Int32>] [-BorderColor <String>] [-BorderThickness <Int32>] [-ShadowIntensity <String>] [-Opacity <Double>] [-Icon <String>] [-IconPath <String>] [-IconSize <Int32>] [-IconPosition <String>] [-IconColor <String>] [-IconAnimation <String>] [-OverlayImagePath <String>] [-OverlayImageOpacity <Double>] [-OverlayPosition <String>] [-OverlayImageSize <Int32>] [-Clickable] [-ClickAction <ScriptBlock>] [-LinkUrl <String>] [-LinkText <String>] [-HoverEffect <String>] [-ButtonText <String>] [-ButtonIcon <String>] [-ButtonColor <String>] [-ButtonTextColor <String>] [-ShowCloseButton] [-BadgeText <String>] [-BadgeColor <String>] [-BadgeTextColor <String>] [-BadgePosition <String>] [-ProgressValue <Int32>] [-ProgressLabel <String>] [-ProgressColor <String>] [-ProgressBackgroundColor <String>] [-Responsive] [-SmallTitleFontSize <Int32>] [-SmallSubtitleFontSize <Int32>] [-SmallHeight <Int32>] [-SmallIconSize <Int32>] [-ResponsiveBreakpoint <Int32>] [-EntranceAnimation <String>] [-AnimationDuration <Int32>] [-CarouselSlides <Hashtable[]>] [-AutoRotate <Boolean>] [-RotateInterval <Int32>] [-NavigationStyle <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this banner to. |
| `-Name` | string | No |  |  | Unique name for the banner. |
| `-Title` | string | Yes |  |  | Main title text displayed prominently. |
| `-Subtitle` | string | No |  |  | Secondary text displayed below the title. |
| `-Description` | string | No |  |  | Additional descriptive text below the subtitle. |
| `-Category` | string | No |  | General | Category for grouping/filtering banners (default: General). |
| `-Style` | string | No | ``, `Info`, `Success`, `Warning`, `Error` |  |  |
| `-BannerStyle` | string | No | `Default`, `Gradient`, `Image`, `Minimal`, `Hero`, `Accent` | Default | Preset style for the banner: Default, Gradient, Image, Minimal, Hero, Accent. This simplifies banner creation for common use cases (80% of scenarios). - Default: Standard banner with theme colors - Gradient: Blue gradient background (135 deg angle) - Image: Larger banner optimized for background images - Minimal: Compact banner with no shadow - Hero: Large centered banner for landing pages - Accent: Green accent color with hover effect |
| `-BannerConfig` | hashtable | No |  | @{} | Hashtable of advanced configuration options to override preset values. Allows fine-tuning of preset styles without specifying all parameters. Example: @{ Height = 250; TitleFontSize = 40; BackgroundColor = '#FF5722' } |
| `-Height` | int | No | 80–600 | 180 | Height of the banner in pixels (default: 180). |
| `-Width` | int | No | 200–2000 | 700 | Width of the banner in pixels (default: 700). |
| `-FullWidth` | switch | No |  |  | Stretch banner to full available width. |
| `-Layout` | string | No | `Left`, `Center`, `Right` | Left | Content layout: Left, Center, Right (default: Left). |
| `-ContentAlignment` | string | No | `Left`, `Center`, `Right` | Left | Text alignment within content area: Left, Center, Right (default: Left). |
| `-VerticalAlignment` | string | No | `Top`, `Center`, `Bottom` | Center |  |
| `-Padding` | string | No |  | 32,24 |  |
| `-CornerRadius` | int | No | 0–50 | 12 | Border corner radius in pixels (default: 12). |
| `-TitleFontSize` | int | No | 12–72 | 32 | Font size for the title (default: 32). |
| `-SubtitleFontSize` | int | No | 10–36 | 16 | Font size for the subtitle (default: 16). |
| `-DescriptionFontSize` | int | No | 10–24 | 14 |  |
| `-TitleFontWeight` | string | No | `Normal`, `Medium`, `SemiBold`, `Bold`, `ExtraBold` | Bold | Font weight for title: Normal, Medium, SemiBold, Bold, ExtraBold (default: Bold). |
| `-SubtitleFontWeight` | string | No | `Normal`, `Medium`, `SemiBold`, `Bold` | Normal |  |
| `-FontFamily` | string | No |  | Segoe UI | Font family for all text (default: Segoe UI). |
| `-TitleColor` | string | No |  | #FFFFFF | Title text color (default: #FFFFFF). |
| `-SubtitleColor` | string | No |  | #B0B0B0 | Subtitle text color (default: #B0B0B0). |
| `-DescriptionColor` | string | No |  | #909090 | Description text color (default: #909090). |
| `-TitleAllCaps` | switch | No |  |  | Display title in all uppercase letters. |
| `-TitleLetterSpacing` | double | No | 0–10 | 0 |  |
| `-BackgroundColor` | string | No |  | #2D2D30 | Background color (default: #2D2D30). |
| `-BackgroundImagePath` | string | No |  |  | Optional path to a background image. |
| `-BackgroundImageOpacity` | double | No | 0–1 | 0.3 | Opacity of the background image (default: 0.3). |
| `-BackgroundImageStretch` | string | No | `Fill`, `Uniform`, `UniformToFill`, `None` | Uniform |  |
| `-GradientStart` | string | No |  |  | Start color for gradient background. |
| `-GradientEnd` | string | No |  |  | End color for gradient background. |
| `-GradientAngle` | int | No | 0–360 | 90 | Angle of the gradient in degrees (default: 90). |
| `-BorderColor` | string | No |  | Transparent |  |
| `-BorderThickness` | int | No | 0–10 | 0 |  |
| `-ShadowIntensity` | string | No | `None`, `Light`, `Medium`, `Heavy` | Medium | Shadow effect intensity: None, Light, Medium, Heavy (default: Medium). |
| `-Opacity` | double | No | 0–1 | 1 |  |
| `-Icon` _(alias: BannerIcon)_ | string | No |  |  | Optional icon glyph (e.g., '&#xE950;'). |
| `-IconPath` | string | No |  |  | Path to an image file to use as icon instead of glyph. |
| `-IconSize` | int | No | 16–200 | 64 | Size of the icon in pixels (default: 64). |
| `-IconPosition` | string | No | `Left`, `Right`, `Top`, `Bottom`, `Background` | Right | Position of the icon: Left, Right, Top, Bottom, Background (default: Right). |
| `-IconColor` | string | No |  | #40FFFFFF | Color of the icon glyph (default: #40FFFFFF). |
| `-IconAnimation` | string | No | `None`, `Pulse`, `Rotate`, `Bounce` |  | Animation for the icon: None, Pulse, Rotate, Bounce (default: None). |
| `-OverlayImagePath` | string | No |  |  | Path to an overlay image displayed on the banner. |
| `-OverlayImageOpacity` | double | No | 0–1 | 0.5 | Opacity of the overlay image (default: 0.5). |
| `-OverlayPosition` | string | No | `Left`, `Right`, `Center` | Right | Position of overlay image: Left, Right, Center (default: Right). |
| `-OverlayImageSize` | int | No | 40–400 | 120 | Size of the overlay image in pixels (default: 120). |
| `-Clickable` | switch | No |  |  | Make the entire banner clickable. |
| `-ClickAction` | scriptblock | No |  |  | ScriptBlock to execute when banner is clicked. |
| `-LinkUrl` | string | No |  |  | URL to open when banner is clicked. |
| `-LinkText` | string | No |  |  |  |
| `-HoverEffect` | string | No | `None`, `Lift`, `Glow`, `Zoom`, `Darken` |  | Effect on hover: None, Lift, Glow, Zoom, Darken (default: None). |
| `-ButtonText` | string | No |  |  | Text for an action button on the banner. |
| `-ButtonIcon` | string | No |  |  | Icon glyph for the action button. |
| `-ButtonColor` | string | No |  | #0078D4 | Background color of the action button (default: #0078D4). |
| `-ButtonTextColor` | string | No |  | #FFFFFF |  |
| `-ShowCloseButton` | switch | No |  |  |  |
| `-BadgeText` | string | No |  |  | Text for a small badge/label on the banner. |
| `-BadgeColor` | string | No |  | #FF5722 | Background color of the badge (default: #FF5722). |
| `-BadgeTextColor` | string | No |  | #FFFFFF |  |
| `-BadgePosition` | string | No | `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight` | TopRight | Position of the badge: TopLeft, TopRight, BottomLeft, BottomRight (default: TopRight). |
| `-ProgressValue` | int | No | -1–100 | -1 | =============================================================================== Progress Indicator =============================================================================== |
| `-ProgressLabel` | string | No |  |  |  |
| `-ProgressColor` | string | No |  | #0078D4 |  |
| `-ProgressBackgroundColor` | string | No |  | #40FFFFFF |  |
| `-Responsive` | switch | No |  |  | =============================================================================== Responsive Design =============================================================================== |
| `-SmallTitleFontSize` | int | No | 12–48 | 24 |  |
| `-SmallSubtitleFontSize` | int | No | 10–24 | 14 |  |
| `-SmallHeight` | int | No | 80–300 | 140 |  |
| `-SmallIconSize` | int | No | 16–100 | 48 |  |
| `-ResponsiveBreakpoint` | int | No | 300–800 | 500 |  |
| `-EntranceAnimation` | string | No | `None`, `FadeIn`, `SlideIn`, `ZoomIn` |  | Entrance animation: None, FadeIn, SlideIn, ZoomIn (default: None). |
| `-AnimationDuration` | int | No | 100–2000 | 300 | Duration of entrance animation in milliseconds (default: 300). |
| `-CarouselSlides` | Hashtable[] | No |  |  | =============================================================================== Carousel =============================================================================== |
| `-AutoRotate` | bool | No |  |  |  |
| `-RotateInterval` | int | No | 1000–10000 | 3000 |  |
| `-NavigationStyle` | string | No | `Dots`, `Arrows`, `None` | Dots |  |

**Examples**

```powershell
Add-UIBanner -Step "Welcome" -Title "Welcome to Setup" -Subtitle "Let's get started"
```
Creates a simple banner with default styling.

```powershell
Add-UIBanner -Step "Dashboard" -Title "System Dashboard" -Subtitle "Monitor your infrastructure" `
    -BannerStyle "Gradient"
```
Creates a gradient banner using the preset style (blue gradient, 200px height).

```powershell
Add-UIBanner -Step "Dashboard" -Title "Hero Banner" -Subtitle "Welcome" `
    -BannerStyle "Hero" `
    -BannerConfig @{ Height = 350; TitleFontSize = 52 }
```
Creates a hero banner with preset style and custom overrides.

```powershell
Add-UIBanner -Step "Dashboard" -Name "CustomBanner" `
    -Title "System Dashboard" `
    -Subtitle "Monitor your infrastructure" `
    -GradientStart "#0078D4" `
    -GradientEnd "#004578" `
    -GradientAngle 135 `
    -Height 220 `
    -TitleFontSize 36 `
    -HoverEffect "Lift"
```
Creates a fully customized banner with individual parameters (advanced usage).

```powershell
Add-UIBanner -Step "Promo" -Name "PromoBanner" `
    -Title "NEW FEATURE" `
    -TitleAllCaps `
    -Subtitle "Check out our latest update" `
    -ButtonText "Learn More" `
    -ButtonIcon "&#xE8A7;" `
    -BadgeText "NEW" `
    -BadgePosition "TopRight" `
    -Clickable `
    -LinkUrl "https://example.com"
```

---

## Add-UICard

Adds an informational card control to a UI step.

Creates a card control that displays formatted text, instructions, or information.
Cards are rendered as visually distinct panels and are perfect for providing
context, guidelines, warnings, or helpful tips within a UI step.

```
Add-UICard [-Step] <String> [-Name] <String> [[-Title] <String>] [[-Content] <String>] [-Icon <String>] [-IconPath <String>] [-ImagePath <String>] [-ImageOpacity <Double>] [-LinkUrl <String>] [-LinkText <String>] [-BackgroundColor <String>] [-TitleColor <String>] [-ContentColor <String>] [-CornerRadius <Int32>] [-GradientStart <String>] [-GradientEnd <String>] [-Category <String>] [-Style <String>] [-Subtitle <String>] [-Collapsible] [-AccentColor <String>] [-ButtonText <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this card to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the card. This is used internally to reference the card. |
| `-Title` | string | No |  |  | Title displayed at the top of the card. |
| `-Content` | string | No |  |  | The main content text to display in the card. Supports multi-line text. Best practice: Use here-strings (@"..."@) for multi-line content instead of backtick-n. You can use bullet points (-), numbers, and formatting for better readability. |
| `-Icon` | string | No |  |  | Optional icon to display in the card header. Can be: - Segoe MDL2 icon glyph in format '&#xE1D3;' (e.g., '&#xE946;' for Info) - Emoji characters (e.g., '[list]', '[i]', '[!]') |
| `-IconPath` | string | No |  |  | Path to an image file to display as an icon next to the title (32x32px). |
| `-ImagePath` | string | No |  |  | Path to a background image for the card. |
| `-ImageOpacity` | double | No | 0–1 | 1 | Opacity of the background image (0.0 to 1.0). Default is 1.0. |
| `-LinkUrl` | string | No |  |  | URL to open when the link is clicked. |
| `-LinkText` | string | No |  |  | Text to display for the clickable link. Default is 'Learn more...'. |
| `-BackgroundColor` | string | No |  |  | Background color for the card (e.g., '#107C10'). |
| `-TitleColor` | string | No |  |  | Color for the title text (e.g., '#FFFFFF'). |
| `-ContentColor` | string | No |  |  | Color for the content text (e.g., '#B0B0B0'). |
| `-CornerRadius` | int | No | 0–50 | 8 | Corner radius for the card border (0-50). Default is 8. |
| `-GradientStart` | string | No |  |  | Starting color for gradient background (e.g., '#0078D4'). |
| `-GradientEnd` | string | No |  |  | Ending color for gradient background (e.g., '#004578'). |
| `-Category` | string | No |  |  |  |
| `-Style` | string | No | `Info`, `Success`, `Warning`, `Error`, `Hero` | Info |  |
| `-Subtitle` | string | No |  |  |  |
| `-Collapsible` | switch | No |  |  |  |
| `-AccentColor` | string | No |  |  |  |
| `-ButtonText` | string | No |  |  |  |

**Examples**

```powershell
Add-UICard -Step "Config" -Name "InfoCard" -Title "Important Information" -Content @"
Please read the following guidelines before proceeding:
```
- Requirement 1
- Requirement 2
- Requirement 3
"@

Adds a simple informational card with bullet points using here-string.

```powershell
Add-UICard -Step "Setup" -Name "TipsCard" -Title "[i] Pro Tips" -Content @"
Here are some tips for optimal configuration:
```
1. Use strong passwords
2. Enable backup options
3. Test before deploying
"@

Adds a tips card with emoji icon and numbered list using here-string.

```powershell
Add-UICard -Step "Network" -Name "NetworkInfo" -Title "Network Requirements" -Icon "&#xE968;" -Content @"
Ensure the following network requirements are met:
```
- Port 443 must be open
- DNS resolution configured
- Proxy settings (if applicable)
"@

Adds a card with a Segoe MDL2 network icon using here-string.


---

## Add-UIChartCard

Adds a chart card (graph visualization) to a UI step.

Creates a chart card that displays data as a line, bar, area, or pie chart.
Chart cards are ideal for visualizing trends, comparisons, and distributions.

```
Add-UIChartCard [-Step] <String> [-Name] <String> [-Title] <String> [-Description <String>] [-ChartType <String>] -Data <Object> [-ShowLegend <Boolean>] [-ShowTooltip <Boolean>] [-Icon <String>] [-IconPath <String>] [-Category <String>] [-RefreshScript <ScriptBlock>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this card to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the card. |
| `-Title` | string | Yes |  |  | Display title for the card. |
| `-Description` | string | No |  |  | Short description shown below the card title. |
| `-ChartType` | string | No | `Line`, `Bar`, `Area`, `Pie`, `Donut` | Line | Type of chart: 'Line', 'Bar', 'Area', or 'Pie'. Default is 'Line'. |
| `-Data` | object | Yes |  |  | Data to display. Can be an array of objects, a hashtable, or a ScriptBlock that returns data. When a ScriptBlock is provided, it is executed once for initial display and automatically used as RefreshScript. |
| `-ShowLegend` | bool | No |  | True | Whether to display the chart legend. Default is $true. |
| `-ShowTooltip` | bool | No |  | True | Whether to display tooltips on hover. Default is $true. |
| `-Icon` | string | No |  |  | Optional icon glyph (e.g., '&#xE7C4;'). |
| `-IconPath` | string | No |  |  | Optional path to a PNG icon file. If both Icon and IconPath are specified, IconPath takes precedence. |
| `-Category` | string | No |  | General | Category for grouping/filtering cards. |
| `-RefreshScript` | scriptblock | No |  |  | PowerShell script block to re-fetch the data. If not specified and Data is a ScriptBlock, Data is used as RefreshScript. |

**Examples**

```powershell
$data = @(
    @{Month='Jan'; Sales=100; Profit=20}
    @{Month='Feb'; Sales=150; Profit=35}
    @{Month='Mar'; Sales=120; Profit=25}
)
Add-UIChartCard -Step "Dashboard" -Name "Sales" -Title "Sales Trend" -ChartType "Line" -Data $data
```
Adds a line chart showing sales data.

```powershell
Add-UIChartCard -Step "Dashboard" -Name "ProcessCPU" -Title "Process CPU Usage" -ChartType "Bar" `
    -Data { Get-Process | Select-Object -First 10 Name, CPU }
```
Adds a bar chart with dynamic data from a script block.


---

## Add-UIMetricCard

Adds a metric card (KPI display) to a UI step.

Creates a metric card that displays a single numeric value with optional unit, trend indicator, and target progress bar.
Metric cards are ideal for displaying KPIs, system metrics, and performance indicators.

```
Add-UIMetricCard [-Step] <String> [-Name] <String> [-Title] <String> [-Description <String>] -Value <Object> [-Unit <String>] [-Format <String>] [-Trend <String>] [-TrendValue <Double>] [-Target <Double>] [-SparklineData <Double[]>] [-ShowGauge] [-ShowSparkline] [-MinValue <Double>] [-MaxValue <Double>] [-Icon <String>] [-IconPath <String>] [-Category <String>] [-RefreshScript <ScriptBlock>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this card to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the card. |
| `-Title` | string | Yes |  |  | Display title for the card. |
| `-Description` | string | No |  |  | Short description shown below the card title. |
| `-Value` | object | Yes |  |  | The numeric value to display. Can be a number or a ScriptBlock that returns a number. When a ScriptBlock is provided, it is executed once for initial display and automatically used as RefreshScript. |
| `-Unit` | string | No |  |  | Unit suffix (e.g., %, GB, items). |
| `-Format` | string | No |  | N0 | Number format string (e.g., 'N0', 'N2', 'P0'). Default is 'N0'. |
| `-Trend` | string | No | `up`, `down`, `stable`, `` |  | Trend indicator: 'up', 'down', or 'stable'. |
| `-TrendValue` | double | No |  | 0 | Numeric trend value to display with the trend indicator. |
| `-Target` | double | No |  | 0 | Target value for progress bar. If specified, a progress bar is displayed. |
| `-SparklineData` | Double[] | No |  |  |  |
| `-ShowGauge` | switch | No |  |  |  |
| `-ShowSparkline` | switch | No |  |  |  |
| `-MinValue` | double | No |  | 0 | Minimum value for the progress bar. Default is 0. |
| `-MaxValue` | double | No |  | 100 | Maximum value for the progress bar. Default is 100. |
| `-Icon` | string | No |  |  | Optional icon glyph (e.g., '&#xE7C4;'). |
| `-IconPath` | string | No |  |  | Optional path to a colored PNG icon file for the metric card. When specified, the PNG image is displayed instead of the Segoe MDL2 glyph. Supports PNG, ICO, and other image formats. |
| `-Category` | string | No |  | General | Category for grouping/filtering cards. |
| `-RefreshScript` | scriptblock | No |  |  | PowerShell script block to re-fetch the value. If not specified and Value is a ScriptBlock, Value is used as RefreshScript. |

**Examples**

```powershell
Add-UIMetricCard -Step "Dashboard" -Name "CPU" -Title "CPU Usage" -Value 75.5 -Unit "%" -Trend "up" -Target 80
```
Adds a metric card showing CPU usage with trend and target.

```powershell
Add-UIMetricCard -Step "Dashboard" -Name "Memory" -Title "Memory Usage" `
    -Value { (Get-CimInstance Win32_OperatingSystem).TotalVisibleMemorySize / 1MB } `
    -Unit "GB" -Target 16
```
Adds a metric card with dynamic value from a script block.


---

## Add-UIScriptCard

Adds an executable script card to a UI step in CardGrid view mode.

Creates a card that represents a PowerShell script with its own parameters in Dashboard view mode.
When clicked, the card opens a dialog with the script's parameters (auto-discovered
from the script's param block) and an execution console showing real-time output.

```
Add-UIScriptCard [-Step] <String> [-Name] <String> [-Title] <String> [-Description <String>] [-Icon <String>] [-IconPath <String>] -ScriptPath <String> [-DefaultParameters <Hashtable>] [-Category <String>] [-Tags <String[]>] [<CommonParameters>]

Add-UIScriptCard [-Step] <String> [-Name] <String> [-Title] <String> [-Description <String>] [-Icon <String>] [-IconPath <String>] -ScriptBlock <ScriptBlock> [-DefaultParameters <Hashtable>] [-Category <String>] [-Tags <String[]>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this card to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the card. This is used internally to reference the card. |
| `-Title` | string | Yes |  |  | Display title for the card shown in the UI. |
| `-Description` | string | No |  |  | Short description shown below the card title. |
| `-Icon` | string | No |  |  | Optional icon to display on the card. Can be: - Segoe MDL2 icon glyph in format '&#xE1D3;' (e.g., '&#xE77B;' for User) - Emoji characters (e.g., fire, clipboard, warning icons) |
| `-IconPath` | string | No |  |  |  |
| `-ScriptPath` | string | Yes |  |  | Path to a .ps1 file to execute. Parameters are auto-discovered from the script's param block. |
| `-ScriptBlock` | scriptblock | Yes |  |  | Inline scriptblock to execute. Parameters are auto-discovered from the scriptblock. |
| `-DefaultParameters` | hashtable | No |  | @{} | Hashtable of default parameter values to pre-populate in the UI. These override any defaults defined in the script itself. |
| `-Category` | string | No |  |  | Category for grouping/filtering cards in the CardGrid view. |
| `-Tags` | string[] | No |  |  | Array of tags for additional filtering capabilities. |

**Examples**

```powershell
Add-UIScriptCard -Step "Tools" -Name "CreateUser" -Title "Create User" `
    -Description "Create a new local user account" `
    -Icon "&#xE77B;" `
    -ScriptPath ".\Scripts\New-LocalUser.ps1"
```
Adds a script card that executes an external PowerShell script with auto-discovered parameters.

```powershell
Add-UIScriptCard -Step "Tools" -Name "RestartIIS" -Title "Restart IIS" `
    -ScriptBlock { Restart-Service W3SVC -Force; "IIS Restarted" }
```
Adds a simple action card with an inline scriptblock.

```powershell
Add-UIScriptCard -Step "Tools" -Name "DiskCheck" -Title "Check Disk Space" `
    -ScriptBlock {
        param([string]$Drive = "C")
        Get-PSDrive $Drive | Select-Object Used, Free, @{N='PercentFree';E={[math]::Round($_.Free/($_.Used+$_.Free)*100,1)}}
    } `
    -DefaultParameters @{ Drive = "C" }
```
Adds a script card with a parameterized inline script and default values.


---

## Add-UIStatusCard

Adds a status indicator card to a UI step.

Creates a status indicator card that displays a list of items with colored status dots.
Status colors are automatically assigned based on common status strings:
- Green: Online, Running, Healthy, OK, Active, Up, Connected, Success
- Amber: Warning, Degraded, Slow, Pending, Starting
- Red: Offline, Stopped, Error, Critical, Down, Failed, Disconnected
- Gray: Maintenance, Disabled, Unknown

```
Add-UIStatusCard [-Step] <String> [-Name] <String> [-Title] <String> [-Description <String>] -Data <Object[]> [-Icon <String>] [-Category <String>] [-RefreshScript <ScriptBlock>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this card to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the card. |
| `-Title` | string | Yes |  |  | Display title for the card. |
| `-Description` | string | No |  |  | Short description shown below the card title. |
| `-Data` | object[] | Yes |  |  | Array of hashtables with Label and Status keys. Example: @( @{Label='DNS Server'; Status='Online'}, @{Label='DHCP'; Status='Warning'} ) |
| `-Icon` | string | No |  |  | Optional icon glyph (e.g., '&#xE770;'). |
| `-Category` | string | No |  | General | Category for grouping/filtering cards. |
| `-RefreshScript` | scriptblock | No |  |  | PowerShell script block to re-fetch the status data. |

**Examples**

```powershell
Add-UIStatusCard -Step "Dashboard" -Name "Services" -Title "Core Services" `
    -Icon '&#xE770;' -Data @(
        @{Label='Active Directory'; Status='Online'}
        @{Label='DNS Server'; Status='Online'}
        @{Label='DHCP Server'; Status='Warning'}
    )
```
Adds a status card showing service health with colored dots.


---

## Add-UIStep

Adds a new step to the current UI.

Creates a new UI step that can contain controls and defines the structure of the UI.
Steps are displayed in order and can be of different types (Wizard or Dashboard).

```
Add-UIStep [-Name] <String> [-Title] <String> [-Description <String>] [-Order <Int32>] [-Type <String>] [-Icon <String>] [-IconPath <String>] [-Skippable] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Name` | string | Yes |  |  | Unique name for the step. This is used internally to reference the step. |
| `-Title` | string | Yes |  |  | Display title for the step shown in the sidebar and step header. |
| `-Description` | string | No |  |  | Optional description displayed below the title. |
| `-Order` | int | No |  | 0 | Numeric order for the step. Steps are displayed in ascending order. If not specified, steps are ordered by the sequence they are added. |
| `-Type` | string | No | `Wizard`, `Dashboard` |  | Type of step to create. Valid values: - Wizard: Standard input form (default) - supports input controls like TextBox, Dropdown, etc. - Dashboard: Dashboard view with ScriptCards and visualization cards (MetricCard, GraphCard, DataGridCard) |
| `-Icon` | string | No |  |  | Optional icon for the step in the sidebar. Must be a Segoe MDL2 icon glyph. Format: '&#xE1D3;' (e.g., '&#xE968;' for Network, '&#xE72E;' for Shield) See Docs/FLUENT_ICONS_REFERENCE.md for available glyphs. |
| `-IconPath` | string | No |  |  | Optional path to a colored PNG icon file for the step in the sidebar. When specified, the colored PNG image is displayed instead of the Segoe MDL2 glyph. Supports PNG, ICO, and other image formats. |
| `-Skippable` | switch | No |  |  | Whether this step can be skipped by the user. |

**Examples**

```powershell
Add-UIStep -Name "ServerConfig" -Title "Server Configuration" -Order 1
```
Adds a basic input form step.

```powershell
Add-UIStep -Name "Welcome" -Title "Welcome" -Order 1 -Icon "&#xE8BC;" -Description "Get started"
```
Adds a welcome step with a home icon.


---

## Add-UITableCard

Adds a data grid card (table visualization) to a UI step.

Creates a data grid card that displays tabular data with sorting, filtering, and export capabilities.
Data grid cards are ideal for displaying lists, logs, and structured data.

```
Add-UITableCard [-Step] <String> [-Name] <String> [-Title] <String> [-Description <String>] -Data <Object> [-Icon <String>] [-IconPath <String>] [-Category <String>] [-RefreshScript <ScriptBlock>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this card to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the card. |
| `-Title` | string | Yes |  |  | Display title for the card. |
| `-Description` | string | No |  |  | Short description shown below the card title. |
| `-Data` | object | Yes |  |  | Data to display as a table. Can be an array of objects or a ScriptBlock that returns data. When a ScriptBlock is provided, it is executed once for initial display and automatically used as RefreshScript. |
| `-Icon` | string | No |  |  | Optional icon glyph (e.g., '&#xE7C4;'). |
| `-IconPath` | string | No |  |  | Optional path to a PNG icon file. If both Icon and IconPath are specified, IconPath takes precedence. |
| `-Category` | string | No |  | General | Category for grouping/filtering cards. |
| `-RefreshScript` | scriptblock | No |  |  | PowerShell script block to re-fetch the data. If not specified and Data is a ScriptBlock, Data is used as RefreshScript. |

**Examples**

```powershell
$processes = Get-Process | Select-Object -First 20 Name, Id, CPU, Memory
Add-UITableCard -Step "Dashboard" -Name "Processes" -Title "Running Processes" -Data $processes
```
Adds a data grid showing process information.

```powershell
Add-UITableCard -Step "Dashboard" -Name "Services" -Title "Windows Services" `
    -Data { Get-Service | Select-Object Name, Status, StartType }
```
Adds a data grid with dynamic data from a script block.


---

## Clear-PoshUIFileState

Removes temporary files and logs created by PoshUI.

Cleans up temporary script files, connection info files, and optionally log files
created by PoshUI in $env:TEMP\PoshUI and the module's logs directory.

```
Clear-PoshUIFileState [-IncludeLogs] [[-LogRetentionDays] <Int32>] [-Force] [-WhatIf] [-Confirm] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-IncludeLogs` | switch | No |  |  | Also remove log files. By default, logs are retained. |
| `-LogRetentionDays` | int | No | 1–365 | 30 | Number of days to retain log files. Default is 30 days. |
| `-Force` | switch | No |  |  | Skip confirmation prompts and retry locked files. |

**Examples**

```powershell
Clear-PoshUIFileState
```
Removes temporary files but keeps logs.

```powershell
Clear-PoshUIFileState -IncludeLogs -LogRetentionDays 7
```
Removes temp files and logs older than 7 days.


---

## Clear-PoshUIRegistryState

Clears PoshUI session state from the Windows Registry.

Removes all registry keys under HKCU:\Software\PoshUI\Sessions that contain
session state data. Detects and removes stale sessions from crashed UIs.

```
Clear-PoshUIRegistryState [[-SessionId] <String>] [[-OlderThan] <Int32>] [-Force] [-WhatIf] [-Confirm] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-SessionId` | string | No |  |  | Optional specific session ID to remove. If not specified, all sessions are removed. |
| `-OlderThan` | int | No |  | 0 | Only remove sessions older than this many hours. Default is to remove all. |
| `-Force` | switch | No |  |  | Skip confirmation prompts. |

**Examples**

```powershell
Clear-PoshUIRegistryState
```
Removes all PoshUI registry sessions.

```powershell
Clear-PoshUIRegistryState -OlderThan 24
```
Removes sessions older than 24 hours.

```powershell
Clear-PoshUIRegistryState -SessionId "12345-67890"
```
Removes a specific session.


---

## Clear-PoshUIState

Clears all PoshUI state including registry entries, temporary files, and orphaned processes.

Master cleanup function that removes all PoshUI-related resources:
- Registry keys in HKCU:\Software\PoshUI
- Temporary script files in $env:TEMP\PoshUI
- Log files older than specified retention period
- Stale session data from crashed UIs

```
Clear-PoshUIState [-IncludeLogs] [[-LogRetentionDays] <Int32>] [-Force] [-WhatIf] [-Confirm] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-IncludeLogs` | switch | No |  |  | Also remove log files. By default, logs are retained. |
| `-LogRetentionDays` | int | No | 1–365 | 30 | Number of days to retain log files. Default is 30 days. |
| `-Force` | switch | No |  |  | Skip confirmation prompts. |

**Examples**

```powershell
Clear-PoshUIState
```
Clears all PoshUI state except logs.

```powershell
Clear-PoshUIState -IncludeLogs -LogRetentionDays 7 -Force
```
Clears all state including logs older than 7 days without confirmation.


---

## Get-PoshUIDashboard

Retrieves the current PoshUI dashboard definition for inspection and debugging.

Returns the current dashboard definition including all steps, controls, and properties.
Useful for debugging, inspecting the dashboard structure, and troubleshooting rendering issues.

```
Get-PoshUIDashboard [-IncludeProperties] [[-StepName] <String>] [-AsJson] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-IncludeProperties` | switch | No |  |  | Include detailed property information for each control. |
| `-StepName` | string | No |  |  | Filter to a specific step by name. |
| `-AsJson` | switch | No |  |  | Return the dashboard definition as JSON (same format sent to the C# frontend). |

**Examples**

```powershell
Get-PoshUIDashboard
```
Returns a summary of the current dashboard with steps and control counts.

```powershell
Get-PoshUIDashboard -IncludeProperties
```
Returns detailed information including all control properties.

```powershell
Get-PoshUIDashboard -StepName "SystemOverview"
```
Returns information for a specific step only.

```powershell
Get-PoshUIDashboard -AsJson | Out-File dashboard.json
```
Exports the dashboard definition as JSON for inspection.


---

## Get-UIConfiguration

Retrieves global configuration options for PoshUI.

Gets current global settings that apply to all PoshUI instances.
Settings are stored in the registry and persist across sessions.

```
Get-UIConfiguration [[-Name] <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Name` | string | No | `DefaultTheme`, `DefaultTemplate`, `DefaultGridColumns`, `EnableTelemetry`, `LogLevel`, `LogPath`, `AutoCleanupEnabled`, `AutoCleanupHours`, `EnableEventHistory` |  | Specific configuration setting name to retrieve. If not specified, returns all settings. |

**Examples**

```powershell
Get-UIConfiguration
```
Returns all configuration settings as a hashtable.

```powershell
Get-UIConfiguration -Name 'DefaultTheme'
```
Returns only the DefaultTheme setting value.


---

## New-PoshUIDashboard

Initializes a new PoshUI Dashboard definition.

Creates a new Dashboard UI context that can be populated with steps and visualization cards.
This function must be called before adding any steps or cards to the UI.

```
New-PoshUIDashboard [-Title] <String> [-Description <String>] [-Icon <String>] [-SidebarHeaderText <String>] [-WindowTitleIcon <String>] [-SidebarHeaderIcon <String>] [-SidebarHeaderIconOrientation <String>] [-Theme <String>] [-AllowCancel <Boolean>] [-GridColumns <Int32>] [-LogPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Title` | string | Yes |  |  | The title of the UI that will be displayed in the window title bar. |
| `-Description` | string | No |  |  | Optional description of the UI's purpose. |
| `-Icon` | string | No |  |  | Optional path to an icon file (PNG, ICO) to display in the UI. Can also be a Segoe MDL2 icon glyph in the format '&#xE1D3;' (e.g., Database icon). |
| `-SidebarHeaderText` | string | No |  |  | Optional text to display in the sidebar header for branding. |
| `-WindowTitleIcon` | string | No |  |  |  |
| `-SidebarHeaderIcon` | string | No |  |  | Optional icon for the sidebar header. Can be a file path or Segoe MDL2 glyph (e.g., '&#xE1D3;'). |
| `-SidebarHeaderIconOrientation` | string | No | `Left`, `Right`, `Top`, `Bottom` | Left | Optional orientation for the sidebar icon relative to the text. Supported values: Left (default), Right, Top, Bottom. |
| `-Theme` | string | No | `Light`, `Dark`, `Auto` | Auto | The theme to use for the UI. Valid values are 'Light', 'Dark', or 'Auto'. Default is 'Auto' which follows the system theme. |
| `-AllowCancel` | bool | No |  | True | Whether to allow users to cancel the UI. Default is $true. |
| `-GridColumns` | int | No | 1–6 | 3 | Number of columns for Dashboard grid layout. Default is 3. Valid range: 1-6. |
| `-LogPath` | string | No |  |  | Optional path to a custom log file for dashboard execution logging. |

**Examples**

```powershell
New-PoshUIDashboard -Title "System Dashboard"
```
Creates a new Dashboard UI with the specified title.

```powershell
New-PoshUIDashboard -Title "Sales Dashboard" -Description "Real-time sales metrics" -GridColumns 4
```
Creates a new Dashboard UI with title, description, and 4-column grid layout.


---

## Register-PoshUICleanupTask

Registers a scheduled task to automatically clean up PoshUI state.

Creates a Windows scheduled task that periodically runs Clear-PoshUIState
to prevent accumulation of orphaned resources from crashed UI sessions.

```
Register-PoshUICleanupTask [[-Frequency] <String>] [[-Time] <String>] [-IncludeLogs] [[-LogRetentionDays] <Int32>] [-Force] [-WhatIf] [-Confirm] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Frequency` | string | No | `Daily`, `Weekly`, `Monthly` | Weekly | How often to run the cleanup. Valid values: Daily, Weekly, Monthly Default is Weekly. |
| `-Time` | string | No |  | 02:00 | Time of day to run cleanup in 24-hour format (e.g., "02:00") Default is 2:00 AM. |
| `-IncludeLogs` | switch | No |  |  | Configure the task to also clean log files. |
| `-LogRetentionDays` | int | No | 1–365 | 30 | Number of days to retain log files. Default is 30. |
| `-Force` | switch | No |  |  | Overwrite existing task if it exists. |

**Examples**

```powershell
Register-PoshUICleanupTask
```
Registers a weekly cleanup task at 2:00 AM.

```powershell
Register-PoshUICleanupTask -Frequency Daily -Time "03:00" -IncludeLogs
```
Registers a daily cleanup at 3:00 AM including log cleanup.


---

## Set-UIBranding

Configures branding and appearance settings for the UI.

Sets visual customization options including window title, sidebar header, icons, and theme.
Must be called after New-PoshUI.

```
Set-UIBranding [[-WindowTitle] <String>] [[-WindowTitleIcon] <String>] [[-SidebarHeaderText] <String>] [[-SidebarHeaderIcon] <String>] [[-SidebarHeaderIconOrientation] <String>] [[-ShowSidebarHeaderIcon] <Boolean>] [[-Theme] <String>] [-DisableAnimations] [[-AllowCancel] <Boolean>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-WindowTitle` | string | No |  |  | The title displayed in the window title bar. |
| `-WindowTitleIcon` | string | No |  |  | Path to an image file (PNG, ICO, etc.) to display in the window title bar. |
| `-SidebarHeaderText` | string | No |  |  | Text displayed in the sidebar header area. |
| `-SidebarHeaderIcon` | string | No |  |  | Segoe MDL2 icon glyph for the sidebar header (e.g., '&#xE8BC;'). |
| `-SidebarHeaderIconOrientation` | string | No | `Left`, `Right`, `Top`, `Bottom` | Left | Position of the sidebar icon relative to text: 'Left', 'Right', 'Top', or 'Bottom'. |
| `-ShowSidebarHeaderIcon` | bool | No |  | True | Whether to display the sidebar header icon. |
| `-Theme` | string | No | `Light`, `Dark`, `Auto` | Auto | Visual theme: 'Light', 'Dark', or 'Auto' (system default). |
| `-DisableAnimations` | switch | No |  |  | When specified, disables all UI transition animations (step transitions, sidebar, dialogs, hover effects). Useful for accessibility or low-performance environments. |
| `-AllowCancel` | bool | No |  | True | Whether users can cancel the UI (default: $true). |

**Examples**

```powershell
Set-UIBranding -WindowTitle "Server Setup" -SidebarHeaderText "Company Name" -SidebarHeaderIcon "&#xE8BC;"
```
Sets basic branding with custom title and sidebar.

```powershell
Set-UIBranding -WindowTitle "Deployment Wizard" -Theme "Dark" -AllowCancel $false
```
Sets dark theme and prevents cancellation.

```powershell
Set-UIBranding -Theme "Dark" -DisableAnimations
```
Uses dark theme with all animations disabled.
Use Set-UITheme for custom color overrides.


---

## Set-UIConfiguration

Sets global configuration options for PoshUI.

Configures global settings that apply to all PoshUI instances.
Settings are stored in the registry and persist across sessions.

```
Set-UIConfiguration [[-DefaultTheme] <String>] [[-DefaultTemplate] <String>] [[-DefaultGridColumns] <Int32>] [[-EnableTelemetry] <Boolean>] [[-LogLevel] <String>] [[-LogPath] <String>] [[-AutoCleanupEnabled] <Boolean>] [[-AutoCleanupHours] <Int32>] [[-EnableEventHistory] <Boolean>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-DefaultTheme` | string | No | `Light`, `Dark`, `Auto` |  | Default theme for new UI instances. Valid values: 'Light', 'Dark', 'Auto'. |
| `-DefaultTemplate` | string | No | `Wizard`, `Dashboard` |  | Default template for new UI instances. Valid values: 'Wizard', 'Dashboard'. |
| `-DefaultGridColumns` | int | No | 1–6 | 0 | Default number of grid columns for Dashboard template (1-6). |
| `-EnableTelemetry` | bool | No |  |  | Enable or disable telemetry collection. |
| `-LogLevel` | string | No | `None`, `Error`, `Warning`, `Info`, `Verbose` |  | Logging level. Valid values: 'None', 'Error', 'Warning', 'Info', 'Verbose'. |
| `-LogPath` | string | No |  |  | Path where log files should be stored. |
| `-AutoCleanupEnabled` | bool | No |  |  | Enable automatic cleanup of stale sessions. |
| `-AutoCleanupHours` | int | No | 1–168 | 0 | Number of hours before a session is considered stale (default: 24). |
| `-EnableEventHistory` | bool | No |  |  | Enable event history tracking for debugging. |

**Examples**

```powershell
Set-UIConfiguration -DefaultTheme 'Dark' -LogLevel 'Info'
```
Sets the default theme to dark and enables info-level logging.

```powershell
Set-UIConfiguration -AutoCleanupEnabled $true -AutoCleanupHours 48
```
Enables auto-cleanup with 48-hour threshold.


---

## Set-UITheme

Applies a custom color theme to the UI using a simple PowerShell hashtable.

Configures the UI color scheme by accepting a hashtable of named color slots.
No XAML knowledge required - just provide hex color values for the slots you want to override.
Any slots not specified will use the base theme defaults (Light or Dark).

Supports three usage patterns:
1. Single theme: Set-UITheme @{ AccentColor = '#FF6B35' }
2. Dual themes: Set-UITheme -Light @{...} -Dark @{...}
3. Mixed: Set-UITheme @{ AccentColor = '#FF6B35' } -Light @{...} -Dark @{...}

Must be called after New-PoshUIDashboard.
See Set-UITheme in PoshUI.Wizard for full slot documentation.

```
Set-UITheme [[-Theme] <Hashtable>] [-Light <Hashtable>] [-Dark <Hashtable>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Theme` | hashtable | No |  |  | A hashtable of color slot overrides applied to BOTH light and dark modes. |
| `-Light` | hashtable | No |  |  | A hashtable of color slot overrides applied ONLY in light mode. |
| `-Dark` | hashtable | No |  |  | A hashtable of color slot overrides applied ONLY in dark mode. |

**Examples**

```powershell
Set-UITheme @{ AccentColor = '#FF6B35' }
```
```powershell
Set-UITheme -Light @{ Background = '#F5F5F5' } -Dark @{ Background = '#1A1A2E' }
```

---

## Show-PoshUIDashboard

Displays the Dashboard UI and executes the associated script.

Serializes the current Dashboard UI definition to JSON,
launches the PoshUI executable, and returns the results.

```
Show-PoshUIDashboard [[-ScriptBody] <ScriptBlock>] [[-DefaultValues] <Hashtable>] [-NonInteractive] [[-ShowConsole] <Boolean>] [[-Theme] <String>] [[-OutputFormat] <String>] [-RequireSignedScripts] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-ScriptBody` | scriptblock | No |  |  | Optional script block containing the logic to execute after collecting user input. If not provided, a default script that displays the collected parameters is used. |
| `-DefaultValues` | hashtable | No |  | @{} | Hashtable of default values to pre-populate in the UI form. Keys should match the control names. |
| `-NonInteractive` | switch | No |  |  | Run the UI in non-interactive mode using only the default values. The UI will not be displayed. |
| `-ShowConsole` | bool | No |  | True | Whether to show the live execution console during script execution. Default is $true. |
| `-Theme` | string | No | `Light`, `Dark`, `Auto` |  | Override the theme for this UI execution. Valid values are 'Light', 'Dark', or 'Auto'. |
| `-OutputFormat` | string | No | `Object`, `JSON`, `Hashtable` | Object | Format for the returned results. Valid values are 'Object', 'JSON', 'Hashtable'. Default is 'Object'. |
| `-RequireSignedScripts` | switch | No |  |  |  |

**Examples**

```powershell
$result = Show-PoshUIDashboard
```
Shows the UI with default script body and returns results.

```powershell
$result = Show-PoshUIDashboard -ScriptBody {
    Write-Host "Configuring server: $ServerName"
    # Perform configuration tasks
    return @{ Status = 'Success'; Message = 'Configuration completed' }
}
```
Shows the UI with custom script logic.

```powershell
$defaults = @{ ServerName = 'SQL01'; Environment = 'Production' }
$result = Show-PoshUIDashboard -DefaultValues $defaults -ScriptBody $configScript
```
Shows the UI with pre-populated default values.


---

## Unregister-PoshUICleanupTask

Removes the PoshUI automatic cleanup scheduled task.

Unregisters the scheduled task created by Register-PoshUICleanupTask.

```
Unregister-PoshUICleanupTask [-Force] [-WhatIf] [-Confirm] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Force` | switch | No |  |  | Skip confirmation prompt. |

**Examples**

```powershell
Unregister-PoshUICleanupTask
```
Removes the PoshUI cleanup task.



