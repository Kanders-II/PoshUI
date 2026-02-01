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

Throughout the wizard, you can use **Segoe MDL2 Assets**. These are the same icons used by Windows itself, ensuring a professional and consistent appearance.

Icons can be applied to:
- Steps (shown in the sidebar)
- Branding header
- Information cards

::: tip
See the [Icons Reference](../configuration/icons.md) for common glyph codes.
:::

## Window Management

You can control basic window behavior using branding settings:

- **Cancel Protection**: Set `-AllowCancel $false` for critical tasks that must not be interrupted.
- **Window Icon**: Use `-Icon` on `New-PoshUIWizard` to set the taskbar and window icon using a `.png` or `.ico` file.

Next: [Execution & ScriptBody](./execution.md)
