# PoshUI.Wizard — Cmdlet Reference

Complete reference for every cmdlet exported by the **PoshUI.Wizard** module (step-by-step input forms). See [POSHUI_AUTHORING_GUIDE.md](../POSHUI_AUTHORING_GUIDE.md) for concepts and patterns.

_Auto-generated from the module's runtime metadata and comment-based help (PoshUI 1.3.1). Parameter tables list every non-common parameter._

## Cmdlet index

- [`Add-UIBanner`](#add-uibanner) — Adds a banner control to a wizard step.
- [`Add-UICard`](#add-uicard) — Adds an informational card control to a wizard step.
- [`Add-UICheckbox`](#add-uicheckbox) — Adds a checkbox control to a UI step.
- [`Add-UIDate`](#add-uidate) — Adds a date picker control to a UI step.
- [`Add-UIDropdown`](#add-uidropdown) — Adds a dropdown selection control to a UI step.
- [`Add-UIFilePath`](#add-uifilepath) — Adds a file path selector control to the current UI step.
- [`Add-UIFolderPath`](#add-uifolderpath) — Adds a folder path selector control to the current UI step.
- [`Add-UIListBox`](#add-uilistbox) — Adds a ListBox selection control to a UI step.
- [`Add-UIMultiLine`](#add-uimultiline) — Adds a multi-line text area to a UI step.
- [`Add-UINumeric`](#add-uinumeric) — Adds a numeric input control to a UI step.
- [`Add-UIOptionGroup`](#add-uioptiongroup) — Adds an option group (radio button set) to a UI step.
- [`Add-UIPassword`](#add-uipassword) — Adds a secure password input control to a UI step.
- [`Add-UIStep`](#add-uistep) — Adds a new step to the current UI.
- [`Add-UITextBox`](#add-uitextbox) — Adds a text input control to a UI step.
- [`Add-UIToggle`](#add-uitoggle) — Adds a toggle switch control to a UI step.
- [`Clear-PoshUIFileState`](#clear-poshuifilestate) — Removes temporary files and logs created by PoshUI.
- [`Clear-PoshUIRegistryState`](#clear-poshuiregistrystate) — Clears PoshUI session state from the Windows Registry.
- [`Clear-PoshUIState`](#clear-poshuistate) — Clears all PoshUI state including registry entries, temporary files, and orphaned processes.
- [`Get-PoshUIWizard`](#get-poshuiwizard) — Retrieves the current PoshUI wizard definition for inspection and debugging.
- [`Get-UIConfiguration`](#get-uiconfiguration) — Retrieves global configuration options for PoshUI.
- [`New-PoshUIWizard`](#new-poshuiwizard) — Initializes a new PoshUI Wizard definition.
- [`Register-PoshUICleanupTask`](#register-poshuicleanuptask) — Registers a scheduled task to automatically clean up PoshUI state.
- [`Set-UIBranding`](#set-uibranding) — Configures branding and appearance settings for the UI.
- [`Set-UIConfiguration`](#set-uiconfiguration) — Sets global configuration options for PoshUI.
- [`Set-UITheme`](#set-uitheme) — Applies a custom color theme to the UI using a simple PowerShell hashtable.
- [`Show-PoshUIWizard`](#show-poshuiwizard) — Displays the Wizard UI and executes the associated script.
- [`Unregister-PoshUICleanupTask`](#unregister-poshuicleanuptask) — Removes the PoshUI automatic cleanup scheduled task.

---

## Add-UIBanner

Adds a banner control to a wizard step.

Creates a visual banner at the top of a wizard step. Banners can display titles,
descriptions, icons, images, buttons, progress indicators, and support many visual styles.

```
Add-UIBanner [-Step] <String> [-Title <String>] [-Subtitle <String>] [-Description <String>] [-BannerStyle <String>] [-BannerConfig <Hashtable>] [-Icon <String>] [-IconPath <String>] [-IconSize <Int32>] [-IconPosition <String>] [-IconColor <String>] [-IconAnimation <String>] [-BackgroundColor <String>] [-BackgroundImagePath <String>] [-BackgroundImageOpacity <Double>] [-BackgroundImageStretch <String>] [-GradientStart <String>] [-GradientEnd <String>] [-Height <Int32>] [-OverlayImagePath <String>] [-OverlayImageOpacity <Double>] [-OverlayPosition <String>] [-OverlayImageSize <Int32>] [-ButtonText <String>] [-ButtonIcon <String>] [-ButtonColor <String>] [-ProgressValue <Int32>] [-ProgressLabel <String>] [-CarouselItems <Array>] [-AutoRotate] [-RotateInterval <Int32>] [-TitleFontSize <String>] [-SubtitleFontSize <String>] [-TitleFontWeight <String>] [-TitleColor <String>] [-SubtitleColor <String>] [-FontFamily <String>] [-CornerRadius <Int32>] [-GradientAngle <Double>] [-LinkUrl <String>] [-Clickable] [-Style <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this banner to. The step must already exist. |
| `-Title` | string | No |  |  | Main title text for the banner. |
| `-Subtitle` | string | No |  |  | Subtitle text displayed below the title. |
| `-Description` | string | No |  |  | Additional description text. |
| `-BannerStyle` | string | No | `Default`, `Gradient`, `Image`, `Minimal`, `Hero`, `Accent` | Default | Preset style for the banner: Default, Gradient, Image, Minimal, Hero, Accent. This simplifies banner creation for common use cases (80% of scenarios). - Default: Standard banner with theme colors - Gradient: Blue gradient background (135 deg angle) - Image: Larger banner optimized for background images - Minimal: Compact banner with no shadow - Hero: Large centered banner for landing pages - Accent: Green accent color with hover effect |
| `-BannerConfig` | hashtable | No |  | @{} | Hashtable of advanced configuration options to override preset values. Allows fine-tuning of preset styles without specifying all parameters. Example: @{ Height = 250; TitleFontSize = 40; BackgroundColor = '#FF5722' } |
| `-Icon` | string | No |  |  | Segoe MDL2 Assets icon glyph (e.g., '&#xE8BC;' for star). Use HTML entity format. |
| `-IconPath` | string | No |  |  | Path to a custom icon image file. |
| `-IconSize` | int | No |  | 64 | Size of the icon in pixels. Default: 64. |
| `-IconPosition` | string | No | `Left`, `Right`, `Top`, `Bottom`, `Background` | Right | Position of the icon relative to text content. Valid values: 'Left', 'Right', 'Top', 'Bottom'. Default: 'Right'. |
| `-IconColor` | string | No |  | #40FFFFFF |  |
| `-IconAnimation` | string | No | `None`, `Pulse`, `Rotate`, `Bounce` |  |  |
| `-BackgroundColor` | string | No |  |  | Hex color code for banner background (e.g., '#2D2D30'). |
| `-BackgroundImagePath` | string | No |  |  | Path to background image file. |
| `-BackgroundImageOpacity` | double | No | 0–1 | 0.3 | Opacity of background image (0.0 to 1.0). Default: 0.3. |
| `-BackgroundImageStretch` | string | No | `Fill`, `Uniform`, `UniformToFill`, `None` | Uniform |  |
| `-GradientStart` | string | No |  |  | Starting color for gradient background. |
| `-GradientEnd` | string | No |  |  | Ending color for gradient background. |
| `-Height` | int | No |  | 180 | Banner height in pixels. Default: 180. |
| `-OverlayImagePath` | string | No |  |  |  |
| `-OverlayImageOpacity` | double | No | 0–1 | 0.5 |  |
| `-OverlayPosition` | string | No | `Left`, `Right`, `Center` | Right |  |
| `-OverlayImageSize` | int | No |  | 120 |  |
| `-ButtonText` | string | No |  |  | Text for an action button. |
| `-ButtonIcon` | string | No |  |  | Icon for the action button (Segoe MDL2 Assets glyph). |
| `-ButtonColor` | string | No |  | #0078D4 | Button background color. Default: '#0078D4'. |
| `-ProgressValue` | int | No |  | -1 | Progress percentage (0-100). Set to -1 to hide progress bar. Default: -1. |
| `-ProgressLabel` | string | No |  |  | Label text for the progress bar. |
| `-CarouselItems` | Array | No |  |  | Array of hashtables for carousel banners. Each item should have Title, Subtitle, Icon, etc. |
| `-AutoRotate` | switch | No |  |  | Enable automatic rotation for carousel banners. |
| `-RotateInterval` | int | No |  | 3000 | Rotation interval in milliseconds. Default: 3000. |
| `-TitleFontSize` | string | No |  |  | Enhanced styling properties |
| `-SubtitleFontSize` | string | No |  |  |  |
| `-TitleFontWeight` | string | No | `Normal`, `Medium`, `SemiBold`, `Bold`, `ExtraBold` |  |  |
| `-TitleColor` | string | No |  |  |  |
| `-SubtitleColor` | string | No |  |  |  |
| `-FontFamily` | string | No |  |  |  |
| `-CornerRadius` | int | No |  | 0 |  |
| `-GradientAngle` | double | No |  | 0 |  |
| `-LinkUrl` | string | No |  |  |  |
| `-Clickable` | switch | No |  |  |  |
| `-Style` | string | No | ``, `Info`, `Success`, `Warning`, `Error` |  |  |

**Examples**

```powershell
Add-UIBanner -Step "Welcome" -Title "Welcome to Setup" -Subtitle "Let's get started"
```
Creates a simple banner with default styling.

```powershell
Add-UIBanner -Step "Welcome" -Title "Welcome" -Subtitle "Let's begin" -BannerStyle "Gradient"
```
Creates a gradient banner using the preset style.

```powershell
Add-UIBanner -Step "Welcome" -Title "Hero Banner" -BannerStyle "Hero" `
    -BannerConfig @{ Height = 300; TitleFontSize = 48 }
```
Creates a hero banner with preset style and custom overrides.

```powershell
Add-UIBanner -Step "Progress" -Title "Installing..." -ProgressValue 45 -ProgressLabel "Installing components"
```
Creates a banner with a progress indicator.


---

## Add-UICard

Adds an informational card control to a wizard step.

Creates a visual card that can display information, icons, images, and links.
Cards are useful for displaying contextual help, tips, warnings, or related information.

```
Add-UICard [-Step] <String> [[-Title] <String>] [[-Content] <String>] [-Type <String>] [-Icon <String>] [-IconPath <String>] [-ImagePath <String>] [-ImageOpacity <Double>] [-LinkUrl <String>] [-LinkText <String>] [-BackgroundColor <String>] [-TitleColor <String>] [-ContentColor <String>] [-CornerRadius <Int32>] [-GradientStart <String>] [-GradientEnd <String>] [-Width <String>] [-Height <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this card to. The step must already exist. |
| `-Title` | string | No |  |  | Card title text. |
| `-Content` | string | No |  |  | Main content text for the card. |
| `-Type` | string | No | `Info`, `Success`, `Warning`, `Error`, `Tip` | Info | Card type affecting default styling. Valid values: 'Info', 'Success', 'Warning', 'Error', 'Tip'. Default: 'Info'. |
| `-Icon` | string | No |  |  | Segoe MDL2 Assets icon glyph (e.g., '&#xE946;' for info). Use HTML entity format. |
| `-IconPath` | string | No |  |  | Path to a custom icon image file. |
| `-ImagePath` | string | No |  |  | Path to an image file to display in the card. |
| `-ImageOpacity` | double | No | 0–1 | 1 | Opacity of the image (0.0 to 1.0). Default: 1.0. |
| `-LinkUrl` | string | No |  |  | URL for a clickable link. |
| `-LinkText` | string | No |  | Learn more | Display text for the link. Default: 'Learn more'. |
| `-BackgroundColor` | string | No |  |  | Hex color code for card background. |
| `-TitleColor` | string | No |  |  | Hex color code for title text. |
| `-ContentColor` | string | No |  |  | Hex color code for content text. |
| `-CornerRadius` | int | No |  | 8 | Corner radius in pixels for rounded corners. Default: 8. |
| `-GradientStart` | string | No |  |  | Starting color for gradient background. |
| `-GradientEnd` | string | No |  |  | Ending color for gradient background. |
| `-Width` | string | No |  |  | Card width in pixels or 'Auto' for automatic sizing. |
| `-Height` | string | No |  |  | Card height in pixels or 'Auto' for automatic sizing. |

**Examples**

```powershell
Add-UICard -Step "Config" -Title "Important" -Content "Make sure to back up your data before proceeding." -Type "Warning" -Icon "&#xE7BA;"
```
Creates a warning card with an icon.

```powershell
Add-UICard -Step "Welcome" -Title "Need Help?" -Content "Visit our documentation for detailed guides." -LinkUrl "https://docs.example.com" -LinkText "View Docs"
```
Creates an info card with a clickable link.


---

## Add-UICheckbox

Adds a checkbox control to a UI step.

Creates a checkbox control that allows users to select true/false values.
Checkboxes are ideal for boolean options and feature toggles.

**Aliases:** Add-WizardCheckbox

```
Add-UICheckbox [-Step] <String> [-Name] <String> [-Label] <String> [-Default <Boolean>] [-Mandatory] [-Width <Int32>] [-HelpText <String>] [-IconPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this control to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the control. This becomes the parameter name in the generated script. |
| `-Label` | string | Yes |  |  | Display label shown next to the checkbox. |
| `-Default` | bool | No |  |  | Default checked state for the checkbox. Default is $false. |
| `-Mandatory` | switch | No |  |  | Whether this checkbox must be checked to proceed. Useful for acceptance checkboxes. |
| `-Width` | int | No |  | 0 | Preferred width of the control in pixels. |
| `-HelpText` | string | No |  |  | Help text or tooltip to display for this control. |
| `-IconPath` | string | No |  |  |  |

**Examples**

```powershell
Add-UICheckbox -Step "Config" -Name "EnableLogging" -Label "Enable detailed logging"
```
Adds a basic checkbox for enabling logging.

```powershell
Add-UICheckbox -Step "Config" -Name "AcceptTerms" -Label "I accept the terms and conditions" -Mandatory
```
Adds a required checkbox that must be checked to proceed.

```powershell
Add-UICheckbox -Step "Config" -Name "CreateBackup" -Label "Create backup before installation" -Default $true
```
Adds a checkbox that is checked by default.


---

## Add-UIDate

Adds a date picker control to a UI step.

Creates a calendar-based input that supports optional minimum/maximum bounds and a display format.

**Aliases:** Add-WizardDate

```
Add-UIDate [-Step] <String> [-Name] <String> [-Label] <String> [-Default <Nullable`1>] [-Minimum <Nullable`1>] [-Maximum <Nullable`1>] [-Format <String>] [-Mandatory] [-Width <Int32>] [-HelpText <String>] [-IconPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this control to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the control. This becomes the parameter name in the generated script. |
| `-Label` | string | Yes |  |  | Display label shown next to the date picker. |
| `-Default` | Nullable`1 | No |  |  | Default selected date. Must be within the specified range if provided. |
| `-Minimum` | Nullable`1 | No |  |  | Earliest date allowed. Leave unset to allow any past date. |
| `-Maximum` | Nullable`1 | No |  |  | Latest date allowed. Leave unset to allow any future date. |
| `-Format` | string | No |  |  | Optional custom display/export format (e.g. "yyyy-MM-dd"). When omitted the wizard defaults to ISO date. |
| `-Mandatory` | switch | No |  |  | Whether selecting a date is required before proceeding. |
| `-Width` | int | No |  | 0 | Preferred width of the control in pixels. |
| `-HelpText` | string | No |  |  | Help text or tooltip to display for this control. |
| `-IconPath` | string | No |  |  |  |

**Examples**

```powershell
Add-UIDate -Step "Config" -Name "InstallDate" -Label "Installation Date" -Minimum (Get-Date) -Maximum (Get-Date).AddDays(30)
```
Adds a date picker restricted to the next 30 days.


---

## Add-UIDropdown

Adds a dropdown selection control to a UI step.

Creates a dropdown (ComboBox) control that allows users to select from a predefined list of options.
This control generates ValidateSet attributes in the resulting script for parameter validation.
Can also create dynamic dropdowns using scriptblocks that generate choices at runtime.

**Aliases:** Add-WizardDropdown

```
Add-UIDropdown [-Step] <String> [-Name] <String> [-Label] <String> [[-Choices] <String[]>] [-ScriptBlock <ScriptBlock>] [-DependsOn <String[]>] [-Default <String>] [-Mandatory] [-Editable] [-Width <Int32>] [-HelpText <String>] [-IconPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this control to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the control. This becomes the parameter name in the generated script. |
| `-Label` | string | Yes |  |  | Display label shown next to the dropdown. |
| `-Choices` | string[] | No |  |  | Array of string values that will be available in the dropdown. Not required if ScriptBlock is provided. |
| `-ScriptBlock` | scriptblock | No |  |  | PowerShell script block that returns an array of choices dynamically. Dependencies are detected from param() declarations in the script block. |
| `-DependsOn` | string[] | No |  |  | Optional explicit array of parameter names this control depends on. If not specified, dependencies are auto-detected from the script block. |
| `-Default` | string | No |  |  | Default selected value. Must be one of the choices if specified. |
| `-Mandatory` | switch | No |  |  | Whether a selection is required. Users cannot proceed without selecting a value. |
| `-Editable` | switch | No |  |  | Whether users can type custom values in addition to selecting from the list. |
| `-Width` | int | No |  | 0 | Preferred width of the control in pixels. |
| `-HelpText` | string | No |  |  | Help text or tooltip to display for this control. |
| `-IconPath` | string | No |  |  |  |

**Examples**

```powershell
Add-UIDropdown -Step "Config" -Name "Environment" -Label "Target Environment:" -Choices @('Development', 'Testing', 'Production') -Mandatory
```
Adds a required dropdown for environment selection.

```powershell
Add-UIDropdown -Step "Config" -Name "LogLevel" -Label "Log Level:" -Choices @('Error', 'Warning', 'Information', 'Debug') -Default 'Information'
```
Adds a dropdown with a default selection.

```powershell
Add-UIDropdown -Step "Config" -Name "CustomOption" -Label "Custom Option:" -Choices @('Option1', 'Option2', 'Option3') -Editable
```
Adds an editable dropdown that allows custom values.

```powershell
Add-UIDropdown -Step "Config" -Name "Region" -Label "Region:" -ScriptBlock {
    param($Environment)
    if ($Environment -eq 'Production') {
        @('US-East-1', 'US-West-2', 'EU-Central-1')
    } else {
        @('Dev-Region-1', 'Dev-Region-2')
    }
} -Mandatory
```
Creates a dynamic dropdown that updates based on Environment selection.


---

## Add-UIFilePath

Adds a file path selector control to the current UI step.

Creates a text input with a browse button that allows users to select a file path.
The control includes a "..." button that opens a file picker dialog.

**Aliases:** Add-WizardFilePath

```
Add-UIFilePath [-Step] <String> [-Name] <String> [-Label] <String> [-Default <String>] [-Mandatory] [-Filter <String>] [-DialogTitle <String>] [-ValidateExists] [-HelpText <String>] [-IconPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  |  |
| `-Name` | string | Yes |  |  | Unique name for the control. This becomes the PowerShell parameter name. |
| `-Label` | string | Yes |  |  | Display label shown above the control. |
| `-Default` | string | No |  |  | Optional default file path value. |
| `-Mandatory` | switch | No |  |  | Whether this field is required. Default is $false. |
| `-Filter` | string | No |  | All Files\|*.* | File filter for the browse dialog (simple extension format). Examples: "*.ps1" or "*.log;*.txt" Note: Automatically converted to dialog format with description |
| `-DialogTitle` | string | No |  |  | Custom title for the file picker dialog. |
| `-ValidateExists` | switch | No |  |  | Whether to validate that the selected file exists before allowing progression. |
| `-HelpText` | string | No |  |  | Optional help text displayed as a tooltip. |
| `-IconPath` | string | No |  |  |  |

**Examples**

```powershell
Add-UIFilePath -Step "Config" -Name "ConfigFile" -Label "Configuration File" -Default "C:\config.json"
```
Adds a file path selector with a default value.

```powershell
Add-UIFilePath -Step "Config" -Name "ScriptPath" -Label "PowerShell Script" -Mandatory
```
Adds a required file path selector.


---

## Add-UIFolderPath

Adds a folder path selector control to the current UI step.

Creates a text input with a browse button that allows users to select a folder path.
The control includes a "..." button that opens a folder picker dialog.

**Aliases:** Add-WizardFolderPath

```
Add-UIFolderPath [-Step] <String> [-Name] <String> [-Label] <String> [-Default <String>] [-Mandatory] [-HelpText <String>] [-IconPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  |  |
| `-Name` | string | Yes |  |  | Unique name for the control. This becomes the PowerShell parameter name. |
| `-Label` | string | Yes |  |  | Display label shown above the control. |
| `-Default` | string | No |  |  | Optional default folder path value. |
| `-Mandatory` | switch | No |  |  | Whether this field is required. Default is $false. |
| `-HelpText` | string | No |  |  | Optional help text displayed as a tooltip. |
| `-IconPath` | string | No |  |  |  |

**Examples**

```powershell
Add-UIFolderPath -Step "Config" -Name "DataPath" -Label "Data Folder" -Default "C:\SQLData"
```
Adds a folder path selector with a default value.

```powershell
Add-UIFolderPath -Name "BackupPath" -Label "Backup Location" -Mandatory
```
Adds a required folder path selector.


---

## Add-UIListBox

Adds a ListBox selection control to a UI step.

Creates a ListBox control that displays a scrollable list of options.
Supports both single-select and multi-select modes.
This control generates ValidateSet attributes for single-select or string arrays for multi-select in the resulting script.
Can also create dynamic listboxes using scriptblocks that generate choices at runtime.

**Aliases:** Add-WizardListBox

```
Add-UIListBox [-Step] <String> [-Name] <String> [-Label] <String> [[-Choices] <String[]>] [-ScriptBlock <ScriptBlock>] [-DependsOn <String[]>] [-Default <Object>] [-Mandatory] [-MultiSelect] [-Height <Int32>] [-Width <Int32>] [-HelpText <String>] [-IconPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this control to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the control. This becomes the parameter name in the generated script. |
| `-Label` | string | Yes |  |  | Display label shown above the ListBox. |
| `-Choices` | string[] | No |  |  | Array of string values that will be available in the ListBox. Not required if ScriptBlock is provided. |
| `-ScriptBlock` | scriptblock | No |  |  | PowerShell script block that returns an array of choices dynamically. Dependencies are detected from param() declarations in the script block. |
| `-DependsOn` | string[] | No |  |  | Optional explicit array of parameter names this control depends on. If not specified, dependencies are auto-detected from the script block. |
| `-Default` | object | No |  |  | Default selected value(s). Must be one or more of the choices if specified. For multi-select, provide an array of values. |
| `-Mandatory` | switch | No |  |  | Whether a selection is required. Users cannot proceed without selecting a value. |
| `-MultiSelect` | switch | No |  |  | Whether users can select multiple values from the list. |
| `-Height` | int | No |  | 150 | Preferred height of the ListBox in pixels. Default is 150. |
| `-Width` | int | No |  | 0 | Preferred width of the control in pixels. |
| `-HelpText` | string | No |  |  | Help text or tooltip to display for this control. |
| `-IconPath` | string | No |  |  |  |

**Examples**

```powershell
Add-UIListBox -Step "Config" -Name "Features" -Label "Select Features:" -Choices @('Web Server', 'Database', 'Cache', 'Monitoring') -MultiSelect
```
Adds a multi-select ListBox for feature selection.

```powershell
Add-UIListBox -Step "Config" -Name "Priority" -Label "Priority Level:" -Choices @('Low', 'Medium', 'High', 'Critical') -Default 'Medium' -Mandatory
```
Adds a required single-select ListBox with a default selection.

```powershell
Add-UIListBox -Step "Config" -Name "Regions" -Label "Deployment Regions:" -Choices @('US-East', 'US-West', 'EU-Central', 'APAC') -MultiSelect -Default @('US-East', 'US-West') -Height 200
```
Adds a multi-select ListBox with multiple defaults and custom height.

```powershell
Add-UIListBox -Step "Config" -Name "Features" -Label "Select Features:" -ScriptBlock {
    param($Environment)
    if ($Environment -eq 'Production') {
        @('Logging', 'Monitoring', 'Caching', 'LoadBalancing')
    } else {
        @('Logging', 'DebugMode', 'VerboseErrors')
    }
} -MultiSelect
```
Creates a dynamic multi-select listbox that updates based on Environment selection.


---

## Add-UIMultiLine

Adds a multi-line text area to a UI step.

Creates a text input that expands vertically and supports optional row count
metadata. Unlike Add-WizardTextBox -Multiline, this helper surfaces explicit
multi-line metadata that the new UI templates and parser understand.

**Aliases:** Add-WizardMultiLine

```
Add-UIMultiLine [-Step] <String> [-Name] <String> [-Label] <String> [-Default <String>] [-Rows <Int32>] [-Mandatory] [-ValidationPattern <String>] [-ValidationMessage <String>] [-Width <Int32>] [-HelpText <String>] [-IconPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this control to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the control. This becomes the parameter name in the generated script. |
| `-Label` | string | Yes |  |  | Display label shown above the text area. |
| `-Default` | string | No |  |  | Default text to populate the control with. |
| `-Rows` | int | No | 1–50 | 4 | Suggested number of text rows to display. Default is 4. |
| `-Mandatory` | switch | No |  |  | Whether text input is required. Users cannot proceed without entering a value. |
| `-ValidationPattern` | string | No |  |  | Regular expression pattern to validate the input against. |
| `-ValidationMessage` | string | No |  |  | Custom error message to display when validation fails. |
| `-Width` | int | No |  | 0 | Preferred width of the control in pixels. |
| `-HelpText` | string | No |  |  | Help text or tooltip to display for this control. |
| `-IconPath` | string | No |  |  |  |

**Examples**

```powershell
Add-UIMultiLine -Step "Config" -Name "Notes" -Label "Deployment Notes" -Rows 6
```
Adds a multi-line text area showing six rows by default.


---

## Add-UINumeric

Adds a numeric input control to a UI step.

Creates a numeric spinner that enforces optional minimum/maximum bounds and step size.

**Aliases:** Add-WizardNumeric

```
Add-UINumeric [-Step] <String> [-Name] <String> [-Label] <String> [-Default <Nullable`1>] [-Minimum <Nullable`1>] [-Maximum <Nullable`1>] [-Increment <Nullable`1>] [-AllowDecimal] [-Format <String>] [-Mandatory] [-Width <Int32>] [-HelpText <String>] [-IconPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this control to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the control. This becomes the parameter name in the generated script. |
| `-Label` | string | Yes |  |  | Display label shown next to the numeric input. |
| `-Default` | Nullable`1 | No |  |  | Default numeric value. Must respect the specified minimum/maximum if provided. |
| `-Minimum` | Nullable`1 | No |  |  | Lowest permissible value. Leave unset for no lower bound. |
| `-Maximum` | Nullable`1 | No |  |  | Highest permissible value. Leave unset for no upper bound. |
| `-Increment` _(alias: StepSize)_ | Nullable`1 | No |  |  | Increment used by the spinner buttons. Defaults to 1 for integers or 0.1 when -AllowDecimal is specified. |
| `-AllowDecimal` | switch | No |  |  | Allow non-integer values. When omitted the wizard coerces to whole numbers. |
| `-Format` | string | No |  |  | Display format string for the number. Examples: "C2" (currency with 2 decimals), "P0" (percentage), "N2" (number with 2 decimals) |
| `-Mandatory` | switch | No |  |  | Whether a value is required. Users cannot proceed without entering a value. |
| `-Width` | int | No |  | 0 | Preferred width of the control in pixels. |
| `-HelpText` | string | No |  |  | Help text or tooltip to display for this control. |
| `-IconPath` | string | No |  |  |  |

**Examples**

```powershell
Add-UINumeric -Step "Config" -Name "CpuCount" -Label "CPU Cores" -Minimum 1 -Maximum 32 -Default 4
```
Adds an integer numeric input allowing 1-32 cores with a default of 4.


---

## Add-UIOptionGroup

Adds an option group (radio button set) to a UI step.

Creates a compact group of mutually exclusive options rendered as radio buttons.
Option groups are ideal when a small number of choices should be presented inline
instead of a dropdown.

**Aliases:** Add-WizardOptionGroup

```
Add-UIOptionGroup [-Step] <String> [-Name] <String> [-Label] <String> [-Options] <String[]> [-Default <String>] [-Orientation <String>] [-Mandatory] [-Width <Int32>] [-HelpText <String>] [-IconPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this control to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the control. This becomes the parameter name in the generated script. |
| `-Label` | string | Yes |  |  | Display label shown above the option group. |
| `-Options` | string[] | Yes |  |  | Array of string values that will be presented as individual radio buttons. |
| `-Default` | string | No |  |  | Default selected option. Must be one of the supplied options if specified. |
| `-Orientation` | string | No | `Vertical`, `Horizontal` | Vertical | Layout orientation for the radio buttons. Defaults to Vertical. |
| `-Mandatory` | switch | No |  |  | Whether a selection is required. Users cannot proceed without selecting a value. |
| `-Width` | int | No |  | 0 | Preferred width of the control in pixels. |
| `-HelpText` | string | No |  |  | Help text or tooltip to display for this control. |
| `-IconPath` | string | No |  |  |  |

**Examples**

```powershell
Add-UIOptionGroup -Step "Config" -Name "Environment" -Label "Target Environment" -Options @('Dev','Test','Prod') -Default 'Test'
```
Adds a horizontal radio button group for environment selection.


---

## Add-UIPassword

Adds a secure password input control to a UI step.

Creates a password field that securely collects sensitive information.
Returns a SecureString in the generated script. Supports optional reveal button and minimum length validation.

**Aliases:** Add-WizardPassword

```
Add-UIPassword [-Step] <String> [-Name] <String> [-Label] <String> [-Mandatory] [-MinLength <Int32>] [-ValidationPattern <String>] [-ValidationScript <ScriptBlock>] [-ValidationMessage <String>] [-ShowRevealButton <Boolean>] [-Width <Int32>] [-HelpText <String>] [-IconPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this control to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the control. This becomes the parameter name in the generated script. |
| `-Label` | string | Yes |  |  | Display label shown next to the password field. |
| `-Mandatory` | switch | No |  |  | Whether this field is required. Users cannot proceed without entering a value. |
| `-MinLength` | int | No |  | 0 | Minimum password length required. Shows validation error if not met. |
| `-ValidationPattern` | string | No |  |  | Regular expression pattern the password must match. Use this for pattern-based validation (e.g., must contain uppercase, lowercase, number, special char). |
| `-ValidationScript` | scriptblock | No |  |  | Script block that validates the password. Receives the password as $_ and should return $true if valid. This allows complex validation logic that regex cannot express. Note: The password is provided as a plain string to the script block for validation purposes only. |
| `-ValidationMessage` | string | No |  |  | Custom error message to display when validation fails. |
| `-ShowRevealButton` | bool | No |  | True | Whether to show the eye icon that reveals the password (default: $true). |
| `-Width` | int | No |  | 0 | Preferred width of the control in pixels. |
| `-HelpText` | string | No |  |  | Help text or tooltip to display for this control. |
| `-IconPath` | string | No |  |  |  |

**Examples**

```powershell
Add-UIPassword -Step "Security" -Name "AdminPassword" -Label "Administrator Password" -Mandatory
```
Adds a required password field.

```powershell
Add-UIPassword -Step "Security" -Name "Password" -Label "Password" -MinLength 8 -ShowRevealButton $false
```
Adds a password field with minimum length validation and no reveal button.

```powershell
Add-UIPassword -Step "Security" -Name "Password" -Label "Password" `
    -ValidationPattern '^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$' `
    -ValidationMessage "Password must be at least 12 characters and contain uppercase, lowercase, number, and special character"
```
Adds a password field with complex regex validation.

```powershell
Add-UIPassword -Step "Security" -Name "Password" -Label "Password" `
    -ValidationScript {
        $_ -match '[A-Z]' -and
        $_ -match '[a-z]' -and
        $_ -match '\d' -and
        $_ -notmatch 'password|admin|12345'
    } `
    -ValidationMessage "Password must contain uppercase, lowercase, number, and not common passwords"
```
Adds a password field with script block validation for complex rules.


---

## Add-UIStep

Adds a new step to the current UI.

Creates a new UI step that can contain controls and defines the structure of the UI.
Steps are displayed in order and can be of different types (Wizard or Dashboard).

**Aliases:** Add-WizardStep

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

## Add-UITextBox

Adds a text input control to a UI step.

Creates a text input field that allows users to enter string values.
Supports validation, default values, and various formatting options.

**Aliases:** Add-WizardTextBox

```
Add-UITextBox [-Step] <String> [-Name] <String> [-Label] <String> [-Default <String>] [-Placeholder <String>] [-Mandatory] [-Multiline] [-ValidationPattern <String>] [-ValidationMessage <String>] [-MaxLength <Int32>] [-Width <Int32>] [-HelpText <String>] [-IconPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this control to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the control. This becomes the parameter name in the generated script. |
| `-Label` | string | Yes |  |  | Display label shown next to the text input field. |
| `-Default` | string | No |  |  | Default value for the text input. |
| `-Placeholder` | string | No |  |  | Placeholder text shown when the field is empty. |
| `-Mandatory` | switch | No |  |  | Whether this field is required. Users cannot proceed without entering a value. |
| `-Multiline` | switch | No |  |  | Whether to create a multi-line text area instead of a single-line text box. |
| `-ValidationPattern` | string | No |  |  | Regular expression pattern to validate the input against. |
| `-ValidationMessage` | string | No |  |  | Custom error message to show when validation fails. |
| `-MaxLength` | int | No |  | 0 | Maximum number of characters allowed. |
| `-Width` | int | No |  | 0 | Preferred width of the control in pixels. |
| `-HelpText` | string | No |  |  | Help text or tooltip to display for this control. |
| `-IconPath` | string | No |  |  |  |

**Examples**

```powershell
Add-UITextBox -Step "Config" -Name "ServerName" -Label "Server Name:" -Mandatory
```
Adds a required text input for server name.

```powershell
Add-UITextBox -Step "Config" -Name "Description" -Label "Description:" -Multiline -MaxLength 500
```
Adds a multi-line text area with character limit.

```powershell
Add-UITextBox -Step "Config" -Name "Email" -Label "Email Address:" -ValidationPattern "^[^@]+@[^@]+\.[^@]+$" -ValidationMessage "Please enter a valid email address"
```
Adds a text input with email validation.


---

## Add-UIToggle

Adds a toggle switch control to a UI step.

Creates a toggle switch (styled checkbox) for boolean on/off states.
Visually distinct from standard checkboxes with a sliding toggle appearance.

**Aliases:** Add-WizardToggle

```
Add-UIToggle [-Step] <String> [-Name] <String> [-Label] <String> [-Default <Boolean>] [-Mandatory] [-Width <Int32>] [-HelpText <String>] [-IconPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the step to add this control to. The step must already exist. |
| `-Name` | string | Yes |  |  | Unique name for the control. This becomes the parameter name in the generated script. |
| `-Label` | string | Yes |  |  | Display label shown next to the toggle switch. |
| `-Default` _(alias: DefaultValue)_ | bool | No |  |  | Default state of the toggle (on/off, true/false). |
| `-Mandatory` | switch | No |  |  |  |
| `-Width` | int | No |  | 0 | Preferred width of the control in pixels. |
| `-HelpText` | string | No |  |  | Help text or tooltip to display for this control. |
| `-IconPath` | string | No |  |  |  |

**Examples**

```powershell
Add-UIToggle -Step "Config" -Name "EnableDebug" -Label "Enable Debug Mode" -DefaultValue $false
```
Adds a toggle switch for enabling debug mode, defaulting to off.

```powershell
Add-UIToggle -Step "Features" -Name "AdvancedMode" -Label "Advanced Features" -DefaultValue $true
```
Adds a toggle switch that defaults to on.


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

## Get-PoshUIWizard

Retrieves the current PoshUI wizard definition for inspection and debugging.

Returns the current wizard definition including all steps, controls, and properties.
Useful for debugging, inspecting the wizard structure, and troubleshooting rendering issues.

**Aliases:** Get-PoshWizard

```
Get-PoshUIWizard [-IncludeProperties] [[-StepName] <String>] [-AsJson] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-IncludeProperties` | switch | No |  |  | Include detailed property information for each control. |
| `-StepName` | string | No |  |  | Filter to a specific step by name. |
| `-AsJson` | switch | No |  |  | Return the wizard definition as JSON (same format sent to the C# frontend). |

**Examples**

```powershell
Get-PoshUIWizard
```
Returns a summary of the current wizard with steps and control counts.

```powershell
Get-PoshUIWizard -IncludeProperties
```
Returns detailed information including all control properties.

```powershell
Get-PoshUIWizard -StepName "Config"
```
Returns information for a specific step only.

```powershell
Get-PoshUIWizard -AsJson | Out-File wizard.json
```
Exports the wizard definition as JSON for inspection.


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

## New-PoshUIWizard

Initializes a new PoshUI Wizard definition.

Creates a new Wizard UI context that can be populated with steps and input controls.
This function must be called before adding any steps or controls to the UI.

```
New-PoshUIWizard [-Title] <String> [-Description <String>] [-Icon <String>] [-SidebarHeaderText <String>] [-WindowTitleIcon <String>] [-SidebarHeaderIcon <String>] [-SidebarHeaderIconOrientation <String>] [-Theme <String>] [-AllowCancel <Boolean>] [-LogPath <String>] [<CommonParameters>]
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
| `-LogPath` | string | No |  |  | Optional path to a custom log file for wizard execution logging. |

**Examples**

```powershell
New-PoshUIWizard -Title "Server Configuration Wizard"
```
Creates a new Wizard UI with the specified title.

```powershell
New-PoshUIWizard -Title "Database Setup" -Description "Configure database connection settings" -Theme Dark
```
Creates a new Wizard UI with title, description, and dark theme.


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

**Aliases:** Set-WizardBranding

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
   Applied to both light and dark modes. Theme toggle re-applies these overrides.
2. Dual themes: Set-UITheme -Light @{...} -Dark @{...}
   Separate overrides for each mode. Theme toggle switches between them.
3. Mixed: Set-UITheme @{ AccentColor = '#FF6B35' } -Light @{ Background = '#FFF' } -Dark @{ Background = '#111' }
   Shared base + mode-specific overrides (mode-specific keys win on conflict).

Must be called after New-PoshUIWizard (or New-PoshUIDashboard, New-PoshUIWorkflow, New-PoshUICanvas).
Works with all PoshUI modules: Wizard, Dashboard, Workflow and Canvas.

```
Set-UITheme [[-Theme] <Hashtable>] [-Light <Hashtable>] [-Dark <Hashtable>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Theme` | hashtable | No |  |  | A hashtable of color slot overrides applied to BOTH light and dark modes. When used alone, the theme toggle will re-apply these overrides after switching the base theme. Available color slots (all optional): ACCENT COLORS: AccentColor - Your brand/accent color (e.g., '#FF6B35') AccentDark - Hover state (auto-derived if omitted) AccentDarker - Pressed state (auto-derived if omitted) AccentLight - Light variant (auto-derived if omitted) BACKGROUNDS: Background - Window/app background ContentBackground - Main content area background CardBackground - Card and panel surfaces SIDEBAR: SidebarBackground - Navigation sidebar background SidebarText - Sidebar text color SidebarHighlight - Active sidebar item color (defaults to AccentColor) TEXT: TextPrimary - Headings and body text TextSecondary - Muted/secondary text BUTTONS: ButtonBackground - Primary button background (defaults to AccentColor) ButtonForeground - Primary button text color INPUTS: InputBackground - Text input field background InputBorder - Input focus border color (defaults to AccentColor) BORDERS: BorderColor - General border color TITLE BAR: TitleBarBackground - Window title bar background TitleBarText - Window title bar text SEMANTIC: SuccessColor - Green status indicator WarningColor - Amber warning indicator ErrorColor - Red error indicator TYPOGRAPHY: FontFamily - Global font family name (e.g., 'Segoe UI') CornerRadius - Control corner radius in pixels (e.g., 8) |
| `-Light` | hashtable | No |  |  | A hashtable of color slot overrides applied ONLY in light mode. These are merged on top of the base -Theme hashtable (if provided). |
| `-Dark` | hashtable | No |  |  | A hashtable of color slot overrides applied ONLY in dark mode. These are merged on top of the base -Theme hashtable (if provided). |

**Examples**

```powershell
Set-UITheme @{ AccentColor = '#FF6B35' }
```
Changes only the accent color to orange. Applied to both light and dark modes.
The theme toggle button will re-apply these overrides after switching.

```powershell
Set-UITheme -Light @{
    AccentColor       = '#0078D4'
    Background        = '#F5F5F5'
    TextPrimary       = '#1A1A2E'
} -Dark @{
    AccentColor       = '#4DA3E8'
    Background        = '#1A1A2E'
    TextPrimary       = '#E0E0E0'
}
```
Separate light and dark themes. The toggle switches between them.

```powershell
Set-UITheme @{ AccentColor = '#FF6B35'; FontFamily = 'Cascadia Code' } -Dark @{
    Background        = '#1A1210'
    ContentBackground = '#231C18'
    TextPrimary       = '#F5EDE8'
}
```
Shared accent color and font for both modes, with dark-specific backgrounds.


---

## Show-PoshUIWizard

Displays the Wizard UI and executes the associated script.

Serializes the current Wizard UI definition to JSON,
launches the PoshUI executable, and returns the results.

**Aliases:** Show-PoshWizard

```
Show-PoshUIWizard [[-ScriptBody] <ScriptBlock>] [[-DefaultValues] <Hashtable>] [-NonInteractive] [[-ShowConsole] <Boolean>] [[-Theme] <String>] [[-OutputFormat] <String>] [-AppDebug] [-RequireSignedScripts] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-ScriptBody` | scriptblock | No |  |  | Optional script block containing the logic to execute after collecting user input. If not provided, a default script that displays the collected parameters is used. |
| `-DefaultValues` | hashtable | No |  | @{} | Hashtable of default values to pre-populate in the UI form. Keys should match the control names. |
| `-NonInteractive` | switch | No |  |  | Run the UI in non-interactive mode using only the default values. The UI will not be displayed. |
| `-ShowConsole` | bool | No |  | True | Whether to show the live execution console during script execution. Default is $true. |
| `-Theme` | string | No | `Light`, `Dark`, `Auto` |  | Override the theme for this UI execution. Valid values are 'Light', 'Dark', or 'Auto'. |
| `-OutputFormat` | string | No | `Object`, `JSON`, `Hashtable` | Object | Format for the returned results. Valid values are 'Object', 'JSON', 'Hashtable'. Default is 'Object'. |
| `-AppDebug` | switch | No |  |  |  |
| `-RequireSignedScripts` | switch | No |  |  |  |

**Examples**

```powershell
$result = Show-PoshUI
```
Shows the UI with default script body and returns results.

```powershell
$result = Show-PoshUI -ScriptBody {
    Write-Host "Configuring server: $ServerName"
    # Perform configuration tasks
    return @{ Status = 'Success'; Message = 'Configuration completed' }
}
```
Shows the UI with custom script logic.

```powershell
$defaults = @{ ServerName = 'SQL01'; Environment = 'Production' }
$result = Show-PoshUI -DefaultValues $defaults -ScriptBody $configScript
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



