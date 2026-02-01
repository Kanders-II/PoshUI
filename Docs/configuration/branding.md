# Branding Configuration

PoshUI provides extensive branding options to make your wizards, dashboards, and workflows feel like native components of your organization's toolset.

## Core Branding Cmdlet

The `Set-UIBranding` cmdlet is the primary tool for customizing the visual identity of your UI.

```powershell
Set-UIBranding -WindowTitle "Corporate Provisioning" `
               -SidebarHeaderText "Global IT Ops" `
               -SidebarHeaderIcon "&#xE710;" `
               -Theme "Auto"
```

## Customizable Elements

### 1. Window Title
The `-WindowTitle` parameter sets the text displayed in the Windows taskbar and the title bar of the application window.

### 2. Sidebar Header
The sidebar is the primary navigation element. You can customize:
- **Header Text**: The large title at the top of the sidebar.
- **Header Icon**: A Segoe MDL2 glyph shown next to the text.
- **Icon Orientation**: Position the icon `'Left'`, `'Right'`, `'Top'`, or `'Bottom'` relative to the text.

### 3. Application Icon
Set the window and taskbar icon using the `-Icon` parameter on the initialization cmdlets (`New-PoshUIWizard`, etc.). This supports `.png` and `.ico` files.

```powershell
New-PoshUIWizard -Title "Setup" -Icon "C:\Branding\logo.png"
```

## Theming

PoshUI supports a high-fidelity theming system:
- **Auto**: Follows the user's Windows Light/Dark preference.
- **Light**: A clean, high-contrast professional look.
- **Dark**: A modern, low-light theme with vibrant accents.

## Best Practices for Branding

1. **Keep it Concise**: Sidebar header text should be short (1-3 words) to avoid wrapping.
2. **Use Clear Icons**: Choose icons that represent the function of the tool (e.g., a shield for security, a server for deployment).
3. **Contrast Matters**: If you use custom colors in cards, ensure they remain readable in both light and dark modes.

Next: [Icons Reference](./icons.md)
