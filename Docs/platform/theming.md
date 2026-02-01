# Platform Theming

PoshUI features a native Windows 11-style theming system that supports both Light and Dark modes. The theme can be configured to follow the system settings or forced to a specific style.

| Light Theme | Dark Theme |
|-------------|------------|
| ![Dashboard Light](../images/visualization/Dashboard_Charts_Light_1.png) | ![Dashboard Dark](../images/visualization/Dashboard_Dark_.png) |

## Theme Modes

| Mode | Description |
|------|-------------|
| `Auto` | **(Default)** Automatically detects the Windows system theme from the registry. |
| `Light` | Forces a high-contrast light theme with white backgrounds and grey borders. |
| `Dark` | Forces a modern dark theme with deep charcoal backgrounds and vibrant accents. |

## Setting the Theme

You can set the theme during initialization or later using branding cmdlets.

### During Initialization
```powershell
New-PoshUIWizard -Title "Setup" -Theme "Dark"
```

### Using Set-UIBranding
```powershell
Set-UIBranding -Theme "Light"
```

## How It Works

The C# engine (`PoshUI.exe`) uses WPF **ResourceDictionaries** to manage themes. When the theme is set to `Auto`, the engine reads the following registry key to determine the user's preference:

- **Key**: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`
- **Value**: `AppsUseLightTheme` (0 = Dark, 1 = Light)

## UI Elements Affected

The theming system applies consistently to all visual elements:
- **Window Chrome**: Title bar and borders.
- **Sidebar**: Background, selection highlights, and text.
- **Form Controls**: Backgrounds, borders, focus states, and placeholder text.
- **Dashboard Cards**: Backgrounds, shadows, and metric values.
- **Execution Console**: Background and text colors (optimized for readability).

## Custom Colors

While PoshUI uses a standard color palette for consistency, some controls allow custom hex color overrides:

```powershell
Add-UICard -Step 'Config' -Title 'Success' -BackgroundColor '#0078D4' -ContentColor '#FFFFFF'
```

::: tip
When using custom colors, ensure they maintain high contrast for accessibility in both light and dark environments.
:::

Next: [Validation](./validation.md)
