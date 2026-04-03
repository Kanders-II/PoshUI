# 🎨 PoshUI v1.3.0 Release

## PNG Icons & Custom Themes

We're excited to announce **PoshUI v1.3.0** - bringing full-color PNG icons and powerful custom theming to all three modules!

This release transforms PoshUI's visual capabilities, letting you create truly branded, professional interfaces with custom color palettes and vibrant icons.

---

## 🆕 What's New in v1.3.0

### 🖼️ Full-Color PNG Icon Support

Say goodbye to monochrome glyphs! v1.3.0 introduces **PNG/ICO icon support** across all UI elements:

- **Sidebar Steps** - Display colorful emoji or custom PNG icons in navigation
- **Dashboard Cards** - Add branded icons to MetricCards and InfoCards
- **Carousel Banners** - Per-slide PNG icons with customizable size and position
- **Branding** - Use your company logo in the sidebar header and window title
- **Automatic Fallback** - Gracefully falls back to glyph icons if PNG files are missing

**Example:**
```powershell
# PNG emoji icons on wizard steps
Add-UIStep -Name 'Welcome' -Title 'Welcome' -IconPath 'C:\Icons\wave_emoji.png'

# PNG icons on dashboard metric cards
Add-UIMetricCard -Step 'Overview' -Name 'CPU' -Title 'CPU Usage' `
    -Value 45 -Unit '%' -IconPath 'C:\Icons\cpu_3d.png'

# PNG icons on carousel slides
$slides = @(
    @{
        Title = 'Performance Dashboard'
        Subtitle = 'Real-time monitoring'
        BackgroundColor = '#1B3A57'
        IconPath = 'C:\Icons\graph_3d.png'
    }
)
Add-UIBanner -Step 'Overview' -Title 'Dashboard' -CarouselSlides $slides
```

![Wizard with Emoji Icons](https://github.com/Kanders-II/PoshUI/raw/main/Docs/images/visualization/Wizard_EmojiIcons_Dark.png)

![Dashboard with PNG Icons](https://github.com/Kanders-II/PoshUI/raw/main/Docs/images/visualization/Dashboard_ComputerMaintenance_Dark.png)

---

### 🎨 Dual-Mode Custom Themes

Create **fully branded interfaces** with independent light and dark color palettes:

- **24 Color Slots** - Override any combination of colors (accent, background, sidebar, text, buttons, inputs, typography)
- **Dual-Mode Support** - Define separate palettes for light and dark themes
- **Runtime Toggle** - Users can switch themes with the sun/moon button in the title bar
- **Persistent Customization** - Custom colors persist across theme toggles
- **Partial Overrides** - Only specify the colors you want to change

**Example:**
```powershell
# Define custom brand colors for both themes
Set-UITheme -Light @{
    AccentColor       = '#E91E63'
    SidebarBackground = '#880E4F'
    ButtonBackground  = '#C2185B'
} -Dark @{
    AccentColor       = '#00BFA5'
    SidebarBackground = '#0A1A18'
    ButtonBackground  = '#00897B'
}
```

**Available Color Slots:**
`Background`, `ContentBackground`, `CardBackground`, `SidebarBackground`, `SidebarText`, `SidebarHighlight`, `TextPrimary`, `TextSecondary`, `AccentColor`, `ButtonBackground`, `ButtonForeground`, `InputBackground`, `InputBorder`, `BorderColor`, `TitleBarBackground`, `TitleBarText`, `SuccessColor`, `WarningColor`, `ErrorColor`, `HeadingForeground`, `BodyForeground`, `SecondaryForeground`

---

### 🎠 Enhanced Carousel Banners

Carousel banners now support **per-slide customization**:

- **PNG Icons** - Display icons beside slide titles via `IconPath` property
- **Icon Positioning** - Choose `Left` or `Right` icon placement
- **Font Customization** - Control `TitleFontSize`, `SubtitleFontSize`, `TitleFontWeight`
- **Cross-Module Support** - Works in Wizard, Dashboard, and Workflow modules

**Example:**
```powershell
$carouselItems = @(
    @{
        Title = 'Performance Dashboard'
        Subtitle = 'Real-time system monitoring'
        BackgroundColor = '#1B3A57'
        IconPath = (Join-Path $PSScriptRoot 'Icons\graph_3d.png')
        IconSize = 80
        IconPosition = 'Right'
    },
    @{
        Title = 'Gauge Controls'
        Subtitle = 'Radial gauges at a glance'
        BackgroundColor = '#DC2626'
        IconPath = (Join-Path $PSScriptRoot 'Icons\stopwatch_3d.png')
        TitleFontSize = '36'
        SubtitleFontSize = '18'
    }
)

Add-UIBanner -Step 'Overview' -CarouselItems $carouselItems -AutoRotate $true
```

---

## 🐛 Bug Fixes

- **Fixed theme toggle not applying custom colors** - Theme toggle now correctly re-applies custom palettes
- **Fixed InfoCard property name mismatch** - `ConvertTo-UIScript` now correctly reads `IconPath`, `Icon`, `ImagePath`, etc.
- **Fixed MetricCard IconPath not flowing through JSON** - `JsonDefinitionLoader` now properly extracts `IconPath` from card properties
- **Fixed carousel PNG icons not rendering** - Added `[OnDeserialized]` callback to restore property defaults

---

## 📦 What's Included

```
PoshUI/
├── PoshUI.Wizard/          # Wizard module with PNG icons & custom themes
├── PoshUI.Dashboard/       # Dashboard module with PNG icons & custom themes
├── PoshUI.Workflow/        # Workflow module with PNG icons & custom themes
├── Examples/               # Updated examples with PNG icons
│   ├── Wizard-EmojiIcons.ps1
│   ├── Dashboard-ComputerMaintenance.ps1
│   ├── Dashboard-MultiPageDashboard.ps1
│   └── ... and more!
├── Examples/Icon8/         # 200+ PNG icons from Icons8
├── Examples/Emoji/         # Microsoft Fluent 3D emoji PNGs
├── Docs/                   # Updated documentation
├── bin/                    # Signed PoshUI.exe v1.3.0
└── README.md
```

---

## 🚀 Quick Start

### Installation

1. Download the release package
2. Extract to your preferred location
3. Import the module you need:

```powershell
# For Wizards
Import-Module .\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1

# For Dashboards
Import-Module .\PoshUI\PoshUI.Dashboard\PoshUI.Dashboard.psd1

# For Workflows
Import-Module .\PoshUI\PoshUI.Workflow\PoshUI.Workflow.psd1
```

### Your First Custom-Themed Wizard with PNG Icons

```powershell
Import-Module .\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1

# Set custom brand colors
Set-UITheme -Light @{
    AccentColor = '#E91E63'
    SidebarBackground = '#880E4F'
} -Dark @{
    AccentColor = '#00BFA5'
    SidebarBackground = '#0A1A18'
}

New-PoshUIWizard -Title "Branded Wizard" -Description "With custom theme and PNG icons"

# Add step with PNG icon
Add-UIStep -Name "Welcome" -Title "Welcome" -Order 1 `
    -IconPath "C:\Icons\wave_emoji.png"

Add-UITextBox -Step "Welcome" -Name "UserName" -Label "Your Name" -Mandatory

Show-PoshUIWizard -ScriptBody {
    Write-Host "Hello, $UserName! Enjoy the new look!"
}
```

---

## 🎯 Perfect For

- **Corporate Branding** - Match your company's color scheme and logo
- **Professional Tools** - Create polished interfaces with custom icons
- **User-Friendly Dashboards** - Colorful icons make metrics easier to understand
- **Themed Wizards** - Light/dark modes with consistent branding
- **Self-Service Portals** - Beautiful interfaces users will love

---

## 📖 Documentation

Full documentation available at: **https://kanders-ii.github.io/PoshUI/**

- **PNG Icons Guide** - How to use PNG/ICO icons across all modules
- **Custom Themes Guide** - Complete color slot reference and examples
- **Carousel Documentation** - Per-slide customization options
- **Updated Examples** - New demo scripts showcasing v1.3.0 features

---

## 🛠️ System Requirements

- **Operating System**: Windows 10/11 (64-bit)
- **PowerShell**: Windows PowerShell 5.1
- **.NET Framework**: 4.8 (included with Windows 10/11)
- **Permissions**: User-level (no admin required for most features)

---

## 🤝 Getting Help

- **Documentation**: https://kanders-ii.github.io/PoshUI/
- **Issues**: [GitHub Issues](https://github.com/Kanders-II/PoshUI/issues)
- **Examples**: Check the `Examples/` folder for working demos

---

## 🙏 Icon Attributions

This release includes example PNG icons from:

- **Icons8** - https://icons8.com (License: https://icons8.com/license)
- **Microsoft Fluent Emoji** - https://github.com/microsoft/fluentui-emoji (MIT License)

---

## 💡 What's Next?

We're planning:
- PowerShell Gallery publishing
- Additional visualization controls
- More example icon sets
- Community-contributed themes and templates

---

## 🙏 Thank You

Thank you for using PoshUI! This project is built to help IT professionals create better tools for their teams.

If PoshUI helps you build something cool, we'd love to hear about it! Share your creations, report bugs, or suggest features on GitHub.

**Happy Automating!**  
*- Kanders-II*

---

## 📄 License

PoshUI is released under the MIT License. See [LICENSE](https://github.com/Kanders-II/PoshUI/blob/main/LICENSE) for details.

**Made with ❤️ for the PowerShell Community**

[Documentation](https://kanders-ii.github.io/PoshUI/) • [GitHub](https://github.com/Kanders-II/PoshUI) • [Report Issue](https://github.com/Kanders-II/PoshUI/issues)
