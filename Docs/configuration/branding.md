# Branding Configuration

PoshUI provides extensive branding options to make your wizards, dashboards, and workflows feel like native components of your organization's toolset.

## Core Branding Cmdlet

The `Set-UIBranding` cmdlet is the primary tool for customizing the visual identity of your UI.

```powershell
Set-UIBranding -WindowTitle "Corporate Provisioning" `
               -SidebarHeaderText "Global IT Ops" `
               -SidebarHeaderIcon "C:\Branding\logo.png" `
               -SidebarHeaderIconOrientation 'Top' `
               -Theme "Auto"
```

## Customizable Elements

### 1. Window Title
The `-WindowTitle` parameter sets the text displayed in the Windows taskbar and the title bar of the application window.

### 2. Window Title Icon
The `-WindowTitleIcon` parameter sets the icon displayed in the title bar and taskbar. Accepts PNG, ICO, or Segoe MDL2 glyph codes.

```powershell
Set-UIBranding -WindowTitleIcon 'C:\Branding\app-icon.png'
```

### 3. Sidebar Header
The sidebar is the primary navigation element. You can customize:
- **Header Text**: The large title at the top of the sidebar.
- **Header Icon**: A Segoe MDL2 glyph **or PNG/ICO file path** shown next to the text. *(PNG support added in v1.3.0)*
- **Icon Orientation**: Position the icon `'Left'`, `'Right'`, `'Top'`, or `'Bottom'` relative to the text.

```powershell
# Glyph icon
Set-UIBranding -SidebarHeaderIcon '&#xE710;'

# PNG image (v1.3.0+)
Set-UIBranding -SidebarHeaderIcon 'C:\Branding\logo.png' `
               -SidebarHeaderIconOrientation 'Top'
```

### 4. Application Icon
Set the window and taskbar icon using the `-Icon` parameter on the initialization cmdlets (`New-PoshUIWizard`, etc.). This supports `.png` and `.ico` files.

```powershell
New-PoshUIWizard -Title "Setup" -Icon "C:\Branding\logo.png"
```

## Theming

PoshUI supports a high-fidelity theming system:
- **Auto**: Follows the user's Windows Light/Dark preference.
- **Light**: A clean, high-contrast professional look.
- **Dark**: A modern, low-light theme with vibrant accents.

### Theme Toggle *(v1.3.0)*

A **sun/moon toggle button** in the title bar allows users to switch between light and dark themes at runtime. Custom color palettes persist across toggles.

### Dual-Mode Custom Themes *(v1.3.0)*

Use `Set-UITheme` to define independent color palettes for light and dark modes:

```powershell
Set-UITheme -Light @{
    Background        = '#FFF0F5'
    AccentColor       = '#E91E63'
    SidebarBackground = '#880E4F'
    SidebarText       = '#FFFFFF'
} -Dark @{
    Background        = '#1A1A2E'
    AccentColor       = '#00BFA5'
    SidebarBackground = '#0A1A18'
    SidebarText       = '#E0F2F1'
}
```

You only need to specify the color slots you want to override. Unspecified slots use the built-in theme defaults. See [Custom Themes](../platform/custom-themes.md) for the full list of 22 overridable color slots.

## Best Practices for Branding

1. **Keep it Concise**: Sidebar header text should be short (1-3 words) to avoid wrapping.
2. **Use Clear Icons**: Choose icons that represent the function of the tool (e.g., a shield for security, a server for deployment).
3. **Use PNG for Color**: When monochrome glyphs aren't enough, use full-color PNG icons with the `-IconPath` parameter.
4. **Contrast Matters**: If you use custom colors in cards, ensure they remain readable in both light and dark modes.
5. **Test Both Themes**: When using dual-mode custom themes, verify your branding looks correct in both light and dark modes.

Next: [Icons Reference](./icons.md)
