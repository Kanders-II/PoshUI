# Custom Themes

PoshUI provides two approaches for custom theming:

1. **`Set-UITheme` with hashtables** *(v1.3.0)* - Define color palettes for light and dark modes using simple PowerShell hashtables. This is the **recommended approach** for most users.
2. **XAML theme files** - Full control over WPF resources for advanced customization.

## Quick Start: Dual-Mode Hashtable Themes *(v1.3.0)*

The simplest way to customize PoshUI's appearance is with `Set-UITheme`. Define independent color palettes for light and dark modes:

```powershell
# Import your module
Import-Module PoshUI.Dashboard

# Initialize
New-PoshUIDashboard -Title 'My Dashboard'

# Define custom themes for both modes
Set-UITheme -Light @{
    Background        = '#FFF0F5'
    AccentColor       = '#E91E63'
    SidebarBackground = '#880E4F'
    SidebarText       = '#FFFFFF'
    SidebarHighlight  = '#F48FB1'
    ButtonBackground  = '#E91E63'
    ButtonForeground  = '#FFFFFF'
    CardBackground    = '#FFFFFF'
} -Dark @{
    Background        = '#1A1A2E'
    AccentColor       = '#00BFA5'
    SidebarBackground = '#0A1A18'
    SidebarText       = '#E0F2F1'
    SidebarHighlight  = '#B2DFDB'
    ButtonBackground  = '#00BFA5'
    ButtonForeground  = '#000000'
    CardBackground    = '#16213E'
}

# Add steps, cards, etc.
Add-UIStep -Name 'Overview' -Title 'Dashboard'
# ...
Show-PoshUIDashboard
```

### How It Works

- The engine detects the active theme (light or dark) and applies the matching palette
- Users can toggle between themes at runtime using the **sun/moon button** in the title bar
- Custom colors persist across toggles
- Only specify the slots you want to change; unspecified slots use built-in defaults

### Available Color Slots (24 total)

| Slot | Description | Light Default | Dark Default |
|------|-------------|---------------|--------------|
| `AccentColor` | Primary accent color | `#0078D4` | `#0078D4` |
| `AccentDark` | Accent hover state (auto-derived if omitted) | `#005A9E` | `#005A9E` |
| `AccentDarker` | Accent pressed state (auto-derived if omitted) | `#004578` | `#004578` |
| `AccentLight` | Light accent variant (auto-derived if omitted) | `#4A9EE0` | `#4A9EE0` |
| `Background` | Outermost window background | `#E8ECF0` | `#1E1E2E` |
| `ContentBackground` | Main content area | `#F0F3F6` | `#252535` |
| `CardBackground` | Card surfaces | `#FFFFFF` | `#2D2D3D` |
| `SidebarBackground` | Sidebar panel | `#1A202C` | `#16161E` |
| `SidebarText` | Sidebar text | `#FFFFFF` | `#FFFFFF` |
| `SidebarHighlight` | Active sidebar highlight | `#4A9EE0` | `#4A9EE0` |
| `TextPrimary` | Primary text | `#1A202C` | `#E8ECF0` |
| `TextSecondary` | Secondary text | `#4A5568` | `#A0AEC0` |
| `ButtonBackground` | Button fill | `#0078D4` | `#0078D4` |
| `ButtonForeground` | Button text | `#FFFFFF` | `#FFFFFF` |
| `InputBackground` | Input field background | `#FFFFFF` | `#2D2D3D` |
| `InputBorder` | Input field border | `#0078D4` | `#0078D4` |
| `BorderColor` | General borders | `#CBD5E0` | `#3D3D4D` |
| `TitleBarBackground` | Title bar | `#1A202C` | `#16161E` |
| `TitleBarText` | Title bar text | `#FFFFFF` | `#FFFFFF` |
| `SuccessColor` | Success indicators | `#107C10` | `#4CAF50` |
| `WarningColor` | Warning indicators | `#FFB900` | `#FF9800` |
| `ErrorColor` | Error indicators | `#E81123` | `#F44336` |
| `FontFamily` | Global font family name | `Segoe UI` | `Segoe UI` |
| `CornerRadius` | Control corner radius in pixels | `8` | `8` |

## Advanced: XAML Theme Resources

For full control over WPF resources, you can reference the internal resource keys that PoshUI uses. This is for advanced users who understand WPF ResourceDictionary concepts.

::: warning
The recommended approach for most users is the `Set-UITheme` hashtable method described above. XAML resource keys are documented here for reference only.
:::

### Using the Template

The easiest way to start is with the included template:

```powershell
# Copy the template to your project
Copy-Item "$PSScriptRoot\..\..\Templates\CustomTheme-Template.xaml" "C:\MyProject\CompanyTheme.xaml"
```

The template contains every customizable resource key with documentation comments explaining what each one controls.

### Minimal XAML Example

You don't need to override everything. A minimal theme that only changes the accent color:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- Change the accent color to corporate orange -->
    <Color x:Key="PrimaryColor">#FF6B35</Color>
    <Color x:Key="PrimaryDarkColor">#E05A2B</Color>
    <Color x:Key="PrimaryDarkerColor">#C44A20</Color>
    <Color x:Key="PrimaryLightColor">#FF8F5E</Color>

    <!-- Update brushes that reference accent colors -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}"/>
    <SolidColorBrush x:Key="ButtonBackgroundBrush" Color="{StaticResource PrimaryColor}"/>
    <SolidColorBrush x:Key="ButtonHoverBrush" Color="{StaticResource PrimaryDarkColor}"/>
    <SolidColorBrush x:Key="TextBoxFocusBorderBrush" Color="{StaticResource PrimaryColor}"/>
</ResourceDictionary>
```

## Resource Key Reference

### Accent Colors

| Key | Description | Default |
|-----|-------------|---------|
| `PrimaryColor` | Main brand/accent color | `#0078D4` |
| `PrimaryDarkColor` | Hover state | `#005A9E` |
| `PrimaryDarkerColor` | Pressed state | `#004578` |
| `PrimaryLightColor` | Light highlights | `#4A9EE0` |

### Semantic Colors

| Key | Description | Default |
|-----|-------------|---------|
| `SuccessColor` | Success indicators | `#107C10` |
| `WarningColor` | Warning indicators | `#FFB900` |
| `ErrorColor` | Error indicators | `#E81123` |
| `InfoColor` | Info indicators | `#0078D4` |

### Surface & Background Brushes

| Key | Description | Default |
|-----|-------------|---------|
| `AppBackgroundBrush` | Outermost window background | `#E8ECF0` |
| `ContentBackgroundBrush` | Main content area | `#F0F3F6` |
| `CardBackgroundBrush` | Card surfaces | `#FFFFFF` |
| `SidebarBackgroundBrush` | Sidebar panel | `#1A202C` |

### Text Brushes

| Key | Description | Default |
|-----|-------------|---------|
| `HeadingForegroundBrush` | Heading text | Gray900 |
| `BodyForegroundBrush` | Body text | Gray700 |
| `SecondaryForegroundBrush` | Secondary text | Gray600 |
| `DisabledForegroundBrush` | Disabled text | Gray500 |

### Button Brushes

| Key | Description | Default |
|-----|-------------|---------|
| `ButtonBackgroundBrush` | Primary button background | PrimaryColor |
| `ButtonForegroundBrush` | Primary button text | `#FFFFFF` |
| `ButtonHoverBrush` | Button hover state | PrimaryDarkColor |
| `ButtonPressedBrush` | Button pressed state | PrimaryDarkerColor |

### Input Brushes

| Key | Description | Default |
|-----|-------------|---------|
| `TextBoxBackgroundBrush` | Input field background | `#FFFFFF` |
| `TextBoxFocusBorderBrush` | Focused input border | PrimaryColor |
| `BorderBrush` | General borders | Gray300 |

For the complete list of all resource keys, see the template file at `PoshUI\Templates\CustomTheme-Template.xaml`.

## Combining with Theme Mode

`Set-UITheme` works alongside `Set-UIBranding -Theme`. The base theme (Light/Dark/Auto) is set via branding, and color overrides are layered on top:

```powershell
# Set base theme
Set-UIBranding -Theme "Dark"

# Layer custom colors on top
Set-UITheme -Dark @{ AccentColor = '#00BFA5'; SidebarBackground = '#0A1A18' }
```

Built-in theme modes:

```powershell
Set-UIBranding -Theme "Dark"   # Built-in dark theme
Set-UIBranding -Theme "Light"  # Built-in light theme
Set-UIBranding -Theme "Auto"   # Follow Windows system setting
```

## Disabling Animations

PoshUI includes smooth transition animations for step navigation, sidebar collapse, and dialog open/close. You can disable these for accessibility or performance:

```powershell
Set-UIBranding -Theme "Dark" -DisableAnimations
```

## Error Handling

If PoshUI cannot load your custom theme file (invalid XAML, missing file, etc.), it:

1. Logs a warning with the error details
2. Falls back to the built-in Light theme
3. Continues running normally

Check the PoshUI log file for theme loading diagnostics.

## Tips

- **Start small**: Override just the accent colors first, then expand
- **Test both modes**: Verify your theme looks good on different monitor sizes
- **Use StaticResource**: Reference your `Color` keys in `SolidColorBrush` definitions for consistency
- **Keep the file**: Store your theme file alongside your scripts for easy distribution

---

Next: [Logging](./logging.md)
