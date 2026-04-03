# Branding & Customization

PoshUI allows you to customize the look and feel of your wizards to match your corporate identity or project theme.

## Using Set-UIBranding

The `Set-UIBranding` cmdlet is the primary way to configure the visual aspects of your UI. It should be called after `New-PoshUIWizard`.

```powershell
Set-UIBranding -WindowTitle "Cloud Provisioning" `
               -SidebarHeaderText "IT Operations" `
               -SidebarHeaderIcon "&#xE710;" `
               -Theme "Auto"
```

### Branding Parameters

| Parameter | Description |
|-----------|-------------|
| `-WindowTitle` | The title displayed in the window title bar. |
| `-SidebarHeaderText` | Large text shown at the top of the sidebar. |
| `-SidebarHeaderIcon` | Segoe MDL2 icon glyph for the header. |
| `-SidebarHeaderIconOrientation` | Position of the icon relative to text (`Left`, `Right`, `Top`, `Bottom`). |
| `-ShowSidebarHeaderIcon` | Boolean to toggle icon visibility. |
| `-Theme` | Visual style: `Auto` (follows Windows), `Light`, or `Dark`. |
| `-AllowCancel` | Whether the user can close the wizard via the 'X' or Cancel button. |

## UI Theming

PoshUI supports a high-quality Dark and Light theme designed to look native on Windows 11.

- **Auto (Default)**: Automatically detects Windows system settings and applies the appropriate theme.
- **Light**: Force light mode with high-contrast text and clean backgrounds.
- **Dark**: Force dark mode with deep grey backgrounds and vibrant accent colors.

```powershell
# Force dark theme for a technical audience
Set-UIBranding -Theme "Dark"
```

## Custom Icons

Throughout the wizard, you can use **Segoe MDL2 Assets** glyphs or **full-color PNG images** *(v1.3.0)*.

### Glyph Icons (Built-in)
Segoe MDL2 Assets are the same icons used by Windows itself, ensuring a professional and consistent appearance.

### PNG Icons *(v1.3.0)*
Use the `-IconPath` parameter for full-color PNG or ICO images instead of monochrome glyphs:

```powershell
# PNG icon on a step
Add-UIStep -Name 'Config' -Title 'Configuration' -IconPath 'C:\Icons\gear_3d.png'

# PNG branding logo
Set-UIBranding -SidebarHeaderIcon 'C:\Icons\company_logo.png'
```

Icons can be applied to:
- Steps (shown in the sidebar) - via `-Icon` (glyph) or `-IconPath` (PNG)
- Branding header - via `-SidebarHeaderIcon` (accepts glyph or file path)
- Information cards - via `-IconPath`
- Banners and carousel slides - via `-IconPath`

::: tip
See the [Icons Reference](../configuration/icons.md) for glyph codes and PNG icon details.
:::

## Dual-Mode Custom Themes *(v1.3.0)*

Define independent color palettes for light and dark modes using `Set-UITheme`:

```powershell
Set-UITheme -Light @{
    AccentColor       = '#E91E63'
    SidebarBackground = '#880E4F'
} -Dark @{
    AccentColor       = '#00BFA5'
    SidebarBackground = '#0A1A18'
}
```

Users can toggle between themes at runtime using the **sun/moon button** in the title bar. Custom colors persist across toggles.

See [Custom Themes](../platform/custom-themes.md) for the full list of 22 color slots.

## Window Management

You can control basic window behavior using branding settings:

- **Cancel Protection**: Set `-AllowCancel $false` for critical tasks that must not be interrupted.
- **Window Icon**: Use `-Icon` on `New-PoshUIWizard` to set the taskbar and window icon using a `.png` or `.ico` file.

Next: [Execution & ScriptBody](./execution.md)
