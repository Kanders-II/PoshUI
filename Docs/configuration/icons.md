# Icons Reference

PoshUI supports two types of icons: **Segoe MDL2 Assets** glyphs (monochrome, built into Windows) and **PNG/ICO image files** (full-color). You can use either type across wizards, dashboards, and workflows.

## Glyph Icons (Segoe MDL2)

Glyph icons are specified using their HTML entity code (e.g., `&#xE710;`). They are monochrome and inherit the theme's accent color.

```powershell
# Glyph icon on a step
Add-UIStep -Name 'Home' -Title 'Introduction' -Icon '&#xE8BC;'
```

## PNG Icons

*Added in v1.3.0*

Use the `-IconPath` parameter to display full-color PNG or ICO images instead of monochrome glyphs. PNG icons are ideal for branded interfaces, emoji-style visuals, or any scenario where color and detail matter.

```powershell
# PNG icon on a step
Add-UIStep -Name 'Config' -Title 'Configuration' -IconPath 'C:\Icons\gear_3d.png'

# PNG icon on a metric card
Add-UIMetricCard -Step 'Overview' -Name 'CPU' -Title 'CPU Usage' `
    -Value 45 -Unit '%' -IconPath 'C:\Icons\rocket_3d.png'

# PNG icon on an info card
Add-UICard -Step 'Details' -Name 'Info' -Title 'Server Info' `
    -Content 'Details here' -IconPath 'C:\Icons\globe_3d.png'
```

### Where PNG Icons Are Supported

| Cmdlet | Parameter | Description |
|--------|-----------|-------------|
| `Add-UIStep` | `-IconPath` | Sidebar navigation icons (Wizard, Dashboard, Workflow) |
| `Add-UIMetricCard` | `-IconPath` | Metric card title icons |
| `Add-UICard` | `-IconPath` | Info card title icons |
| `Add-UIBanner` | `-IconPath` | Banner overlay icons |
| `Add-UIBanner` | Carousel slide `IconPath` | Per-slide PNG icons in carousel banners |
| `Set-UIBranding` | `-SidebarHeaderIcon` | Sidebar header logo (accepts file paths) |
| `Set-UIBranding` | `-WindowTitleIcon` | Window title bar icon |

### Automatic Fallback

When `-IconPath` is specified, the PNG image takes priority. If the file is missing or cannot be loaded, the glyph icon (`-Icon`) is displayed instead. This means you can safely specify both:

```powershell
Add-UIStep -Name 'Config' -Title 'Configuration' `
    -Icon '&#xE713;' `
    -IconPath 'C:\Icons\gear_3d.png'
# Shows PNG if file exists, falls back to gear glyph if not
```

### Supported Formats

- `.png` (recommended - supports transparency)
- `.ico` (Windows icon format)
- `.jpg` / `.jpeg`
- `.bmp`

### PNG Icons on Carousel Slides

Carousel banners support per-slide PNG icons via the `IconPath` property in the slide hashtable. The icon appears beside the slide title and subtitle (default position: `Right`).

```powershell
$carouselItems = @(
    @{
        Title = 'Performance Dashboard'
        Subtitle = 'Real-time system monitoring'
        BackgroundColor = '#1B3A57'
        IconPath = (Join-Path $PSScriptRoot 'Icons\graph_3d.png')
    },
    @{
        Title = 'Gauge Controls'
        Subtitle = 'Radial gauges at a glance'
        BackgroundColor = '#DC2626'
        IconPath = (Join-Path $PSScriptRoot 'Icons\stopwatch_3d.png')
    }
)

# Wizard/Workflow: use -CarouselItems
Add-UIBanner -Step 'Overview' -CarouselItems $carouselItems -AutoRotate $true

# Dashboard: use -CarouselSlides (requires -Title)
Add-UIBanner -Step 'Overview' -Title 'Dashboard' -CarouselSlides $carouselItems -AutoRotate $true
```

Additional per-slide icon properties:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IconPath` | String | `''` | Path to PNG/ICO file |
| `IconSize` | Integer | `64` | Icon width/height in pixels |
| `IconColor` | String | `#40FFFFFF` | Tint color (glyph icons only) |
| `IconPosition` | String | `Right` | Position: `Left` or `Right` |

### Tips for PNG Icons

- **Size**: 32x32 to 128x128 pixels recommended; icons are scaled automatically
- **Transparency**: Use PNG with alpha transparency for best results on both light and dark themes
- **3D Emoji**: Microsoft Fluent 3D emoji PNGs work great as colorful step and card icons
- **Icons8**: [Icons8](https://icons8.com) provides a large library of high-quality PNG icons suitable for dashboards and tools

## Glyph Icons: How to Use

Icons are specified using their HTML entity code. You can apply them to:
- **Steps**: In the sidebar navigation via `-Icon`.
- **Header**: In the branding section.
- **Cards**: In banners, visualization cards, and script cards.

## Common Icons

| Icon | Glyph | Code | Description |
|------|-------|------|-------------|
| **Home** | &#xE8BC; | `&#xE8BC;` | Landing or welcome steps |
| **Settings** | &#xE713; | `&#xE713;` | Configuration or setup |
| **User** | &#xE77B; | `&#xE77B;` | User accounts or permissions |
| **Server** | &#xEB51; | `&#xEB51;` | Infrastructure or hosts |
| **Database** | &#xE1D3; | `&#xE1D3;` | SQL or data storage |
| **Network** | &#xE968; | `&#xE968;` | Connectivity or firewall |
| **Security** | &#xE72E; | `&#xE72E;` | Encryption or auditing |
| **Cloud** | &#xE753; | `&#xE753;` | Azure or remote services |
| **Clock** | &#xE823; | `&#xE823;` | Scheduling or history |
| **Success** | &#xE73E; | `&#xE73E;` | Completion or healthy state |
| **Warning** | &#xE7BA; | `&#xE7BA;` | Alerts or important notices |
| **Error** | &#xE783; | `&#xE783;` | Failures or critical issues |

## Dashboard-Specific Icons

| Icon | Glyph | Code | Description |
|------|-------|------|-------------|
| **CPU** | &#xE7C4; | `&#xE7C4;` | Processor metrics |
| **RAM** | &#xE8B7; | `&#xE8B7;` | Memory metrics |
| **Storage** | &#xEDA2; | `&#xEDA2;` | Disk space or I/O |
| **Globe** | &#xE12B; | `&#xE12B;` | Web or public traffic |

## Finding More Icons

You can find the full list of available icons and their codes on the [Microsoft Segoe MDL2 Assets documentation](https://docs.microsoft.com/en-us/windows/uwp/design/style/segoe-ui-symbol-font).

::: tip
When browsing the Microsoft documentation, take the **Unicode hex value** (e.g., `E710`) and wrap it in the HTML entity format: `&#x<VALUE>;` (e.g., `&#xE710;`).
:::

Next: [Best Practices](./best-practices.md)
