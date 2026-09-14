# PoshUI.Workflow — Cmdlet Reference

Complete reference for every cmdlet exported by the **PoshUI.Workflow** module (sequential task execution with progress, approval gates, and reboot/resume). See [POSHUI_AUTHORING_GUIDE.md](../POSHUI_AUTHORING_GUIDE.md) for concepts and the `$PoshUIWorkflow` task API.

_Auto-generated from the module's runtime metadata and comment-based help (PoshUI 1.3.1). Parameter tables list every non-common parameter._

## Cmdlet index

- [`Add-UIBanner`](#add-uibanner) — Adds a banner control to a wizard step.
- [`Add-UICard`](#add-uicard) — Adds an informational card control to a wizard step.
- [`Add-UICheckbox`](#add-uicheckbox) — Adds a checkbox control to a UI step.
- [`Add-UIDropdown`](#add-uidropdown) — Adds a dropdown selection control to a UI step.
- [`Add-UIFilePath`](#add-uifilepath) — Adds a file path selector control to the current UI step.
- [`Add-UIFolderPath`](#add-uifolderpath) — Adds a folder path selector control to the current UI step.
- [`Add-UINumeric`](#add-uinumeric) — Adds a numeric input control to a UI step.
- [`Add-UIStep`](#add-uistep) — Adds a new step to the current Workflow UI.
- [`Add-UITextBox`](#add-uitextbox) — Adds a text input control to a UI step.
- [`Add-UIWorkflowTask`](#add-uiworkflowtask) — Adds a task to a Workflow step for sequential execution.
- [`Clear-UIWorkflowState`](#clear-uiworkflowstate) — Securely removes saved workflow state files.
- [`Get-UIWorkflowState`](#get-uiworkflowstate) — Loads a saved workflow state from an encrypted file.
- [`New-PoshUIWorkflow`](#new-poshuiworkflow) — Initializes a new PoshUI Workflow definition.
- [`Register-UIWorkflowResumeTask`](#register-uiworkflowresumetask) — Registers a scheduled task to auto-resume the workflow after system reboot.
- [`Resume-UIWorkflow`](#resume-uiworkflow) — Resumes a workflow from saved state.
- [`Save-UIWorkflowState`](#save-uiworkflowstate) — Saves the current workflow state to an encrypted file for later resume.
- [`Set-UIBranding`](#set-uibranding) — Configures branding and appearance settings for the UI.
- [`Set-UITheme`](#set-uitheme) — Applies a custom color theme to the UI using a simple PowerShell hashtable.
- [`Show-PoshUIWorkflow`](#show-poshuiworkflow) — Displays the Workflow UI and executes the workflow tasks.
- [`Test-UIWorkflowState`](#test-uiworkflowstate) — Tests if a saved workflow state exists.
- [`Unregister-UIWorkflowResumeTask`](#unregister-uiworkflowresumetask) — Removes the scheduled task created for workflow auto-resume.

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

## Add-UIDropdown

Adds a dropdown selection control to a UI step.

Creates a dropdown (ComboBox) control that allows users to select from a predefined list of options.
This control generates ValidateSet attributes in the resulting script for parameter validation.
Can also create dynamic dropdowns using scriptblocks that generate choices at runtime.

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

## Add-UINumeric

Adds a numeric input control to a UI step.

Creates a numeric spinner that enforces optional minimum/maximum bounds and step size.

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

## Add-UIStep

Adds a new step to the current Workflow UI.

Creates a new UI step that can contain controls or workflow tasks.
Steps are displayed in order and can be of different types (Wizard for input, Workflow for task execution).

```
Add-UIStep [-Name] <String> [-Title] <String> [-Description <String>] [-Order <Int32>] [-Type <String>] [-Icon <String>] [-IconPath <String>] [-Skippable] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Name` | string | Yes |  |  | Unique name for the step. This is used internally to reference the step. |
| `-Title` | string | Yes |  |  | Display title for the step shown in the sidebar and step header. |
| `-Description` | string | No |  |  | Optional description displayed below the title. |
| `-Order` | int | No |  | 0 | Numeric order for the step. Steps are displayed in ascending order. If not specified, steps are ordered by the sequence they are added. |
| `-Type` | string | No | `Wizard`, `Workflow` |  | Type of step to create. Valid values: - Wizard: Standard input form - supports input controls like TextBox, Dropdown, etc. - Workflow: Sequential task execution with progress tracking (default for Workflow template) |
| `-Icon` | string | No |  |  | Optional icon for the step in the sidebar. Must be a Segoe MDL2 icon glyph. Format: '&#xE1D3;' (e.g., '&#xE968;' for Network, '&#xE72E;' for Shield) |
| `-IconPath` | string | No |  |  | Optional path to a colored PNG icon file for the step in the sidebar. When specified, the colored PNG image is displayed instead of the Segoe MDL2 glyph. Supports PNG, ICO, and other image formats. |
| `-Skippable` | switch | No |  |  | Whether this step can be skipped by the user. |

**Examples**

```powershell
Add-UIStep -Name "Execution" -Title "Execution" -Order 1 -Type Workflow
```
Adds a workflow execution step.

```powershell
Add-UIStep -Name "Config" -Title "Configuration" -Order 1 -Type Wizard
```
Adds a configuration step with input controls.


---

## Add-UITextBox

Adds a text input control to a UI step.

Creates a text input field that allows users to enter string values.
Supports validation, default values, and various formatting options.

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

## Add-UIWorkflowTask

Adds a task to a Workflow step for sequential execution.

Creates a new workflow task that will be executed as part of a workflow sequence.
Tasks can be normal execution tasks or approval gates that pause for user input.
Tasks execute sequentially and support progress tracking and real-time output streaming.

```
Add-UIWorkflowTask [-Step] <String> [-Name] <String> [-Title] <String> [[-Description] <String>] [[-Order] <Int32>] [[-Icon] <String>] [[-IconPath] <String>] [[-ScriptBlock] <ScriptBlock>] [[-ScriptPath] <String>] [[-Arguments] <Hashtable>] [[-TaskType] <String>] [[-OnError] <String>] [[-ApprovalMessage] <String>] [[-ApproveButtonText] <String>] [[-RejectButtonText] <String>] [-RequireReason] [[-TimeoutMinutes] <Int32>] [[-DefaultTimeoutAction] <String>] [[-RetryCount] <Int32>] [[-RetryDelaySeconds] <Int32>] [[-TimeoutSeconds] <Int32>] [[-SkipCondition] <String>] [[-SkipReason] <String>] [[-Group] <String>] [[-RollbackScriptBlock] <ScriptBlock>] [[-RollbackScriptPath] <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Step` | string | Yes |  |  | Name of the Workflow step to add this task to. |
| `-Name` | string | Yes |  |  | Unique name for the task. Used internally for identification. |
| `-Title` | string | Yes |  |  | Display title for the task shown in the workflow UI. |
| `-Description` | string | No |  |  | Optional description displayed below the title. |
| `-Order` | int | No |  | 0 | Numeric order for the task. Tasks execute in ascending order. If not specified, tasks are ordered by the sequence they are added. |
| `-Icon` | string | No |  |  | Optional icon glyph for the task. Must be a Segoe MDL2 icon glyph. |
| `-IconPath` | string | No |  |  |  |
| `-ScriptBlock` | scriptblock | No |  |  | PowerShell script block to execute for this task. The script block has access to $PoshUIWorkflow for progress updates. |
| `-ScriptPath` | string | No |  |  | Path to a PowerShell script file to execute for this task. Alternative to ScriptBlock for larger scripts. |
| `-Arguments` | hashtable | No |  |  | Hashtable of arguments to pass to the script. |
| `-TaskType` | string | No | `Normal`, `ApprovalGate` | Normal | Type of task: Normal (default) or ApprovalGate. ApprovalGate tasks pause execution and wait for user approval. |
| `-OnError` | string | No | `Stop`, `Continue` | Stop | How to handle errors: Stop (default) halts pipeline, Continue proceeds to next task. |
| `-ApprovalMessage` | string | No |  |  | Message to display when task is an ApprovalGate. |
| `-ApproveButtonText` | string | No |  | Approve | Custom text for the approve button (default: 'Approve'). |
| `-RejectButtonText` | string | No |  | Reject | Custom text for the reject button (default: 'Reject'). |
| `-RequireReason` | switch | No |  |  | If true, user must provide a reason when rejecting. |
| `-TimeoutMinutes` | int | No |  | 0 | Optional timeout for approval gates. 0 means no timeout. |
| `-DefaultTimeoutAction` | string | No | `None`, `Approve`, `Reject` |  | Action to take on timeout: None, Approve, or Reject. |
| `-RetryCount` | int | No | 0–100 | 0 | Number of times to retry the task if it fails. Default is 0 (no retry). |
| `-RetryDelaySeconds` | int | No | 0–3600 | 5 | Delay in seconds between retry attempts. Default is 5 seconds. |
| `-TimeoutSeconds` | int | No | 0–86400 | 0 | Maximum time in seconds the task can run before timing out. 0 means no timeout. |
| `-SkipCondition` | string | No |  |  | PowerShell expression as a STRING that, if true, causes the task to be skipped. The condition is evaluated at runtime during workflow execution, not when the workflow is defined. IMPORTANT: This parameter expects a STRING (in quotes), not a scriptblock (in braces). Use quotes: -SkipCondition '$ServerType -ne "Database"' NOT braces: -SkipCondition { $ServerType -ne "Database" } Can reference wizard results ($ParameterName) and workflow data ($WorkflowData['key']). The string approach allows the condition to be evaluated in the proper runtime context with access to user input values collected during wizard execution. |
| `-SkipReason` | string | No |  |  | Message shown when task is skipped by condition. |
| `-Group` | string | No |  |  | Group/phase name for organizing tasks visually. |
| `-RollbackScriptBlock` | scriptblock | No |  |  | PowerShell script to execute if rollback is requested after this task fails. |
| `-RollbackScriptPath` | string | No |  |  | Path to a PowerShell script to execute if rollback is requested after this task fails. |

**Examples**

```powershell
Add-UIWorkflowTask -Step "Execution" -Name "Install" -Title "Install Software" -Order 1 -ScriptBlock {
    $PoshUIWorkflow.UpdateProgress(10, "Starting installation...")
    Start-Sleep -Seconds 2
    $PoshUIWorkflow.UpdateProgress(100, "Installation complete")
}
```
Creates a task that installs software with progress updates.

```powershell
Add-UIWorkflowTask -Step "Execution" -Name "Confirm" -Title "Confirm Changes" -Order 1 `
    -TaskType ApprovalGate -ApprovalMessage "Apply all changes?" `
    -ApproveButtonText "Yes, Apply" -RejectButtonText "Cancel"
```
Creates an approval gate that requires user confirmation before proceeding.

```powershell
Add-UIWorkflowTask -Step "Execution" -Name "Download" -Title "Download Files" -Order 1 `
    -RetryCount 3 -RetryDelaySeconds 10 -TimeoutSeconds 300 -ScriptBlock {
    # Download with retry support
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $DestPath
}
```
Creates a task with retry (3 attempts, 10 second delay) and 5 minute timeout.

```powershell
Add-UIWorkflowTask -Step "Execution" -Name "InstallSQL" -Title "Install SQL Server" -Order 2 `
    -SkipCondition '$ServerType -ne "Database"' -SkipReason "Not a database server" -ScriptBlock {
    # SQL installation logic
}
```
Creates a task that is skipped unless ServerType is "Database".
Note: SkipCondition uses QUOTES (string) not braces (scriptblock) because it's evaluated at runtime.

```powershell
Add-UIWorkflowTask -Step "Execution" -Name "ConfigureDB" -Title "Configure Database" -Order 3 `
    -Group "Database Setup" -ScriptBlock {
    $installPath = $PoshUIWorkflow.GetData('SQLInstallPath')
    # Use data from previous task
}
```
Creates a task in the "Database Setup" group that uses data from a previous task.


---

## Clear-UIWorkflowState

Securely removes saved workflow state files.

Deletes workflow state files from the specified or default locations.
Use this after a workflow completes successfully or to reset a failed workflow.

Security features:
- Secure wipe option (overwrites with random data before deletion)
- Searches both encrypted (.dat) and legacy (.json) files

```
Clear-UIWorkflowState [[-Path] <String>] [-All] [-SecureWipe] [-WhatIf] [-Confirm] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Path` | string | No |  |  | Optional custom path for the state file to remove. If not specified, removes state files from all default locations. |
| `-All` | switch | No |  |  | If specified, removes state files from all default locations. |
| `-SecureWipe` | switch | No |  |  | If specified, overwrites the file with random data before deletion. This prevents potential data recovery of sensitive workflow state. |

**Examples**

```powershell
Clear-UIWorkflowState
```
Removes the state file from the default location.

```powershell
Clear-UIWorkflowState -Path "C:\Temp\MyWorkflowState.dat"
```
Removes a specific state file.

```powershell
Clear-UIWorkflowState -All -SecureWipe
```
Securely removes state files from all default locations.


---

## Get-UIWorkflowState

Loads a saved workflow state from an encrypted file.

Deserializes a workflow state from an encrypted file that was saved by Save-UIWorkflowState.
Returns the state as a hashtable that can be used to restore workflow execution.

Security features:
- DPAPI decryption (CurrentUser scope)
- HMAC-SHA256 integrity validation (detects tampering)
- Supports both encrypted (.dat) and legacy plain (.json) files

```
Get-UIWorkflowState [[-Path] <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Path` | string | No |  |  | Optional custom path for the state file. If not specified, searches default locations: - $env:LOCALAPPDATA\PoshUI\PoshUI_Workflow_State.dat (encrypted, preferred) - $env:LOCALAPPDATA\PoshUI\PoshUI_Workflow_State.json (legacy plain) - $env:PROGRAMDATA\PoshUI\PoshUI_Workflow_State.dat (encrypted) - $env:PROGRAMDATA\PoshUI\PoshUI_Workflow_State.json (legacy plain) |

**Examples**

```powershell
$state = Get-UIWorkflowState
```
Loads workflow state from the default location.

```powershell
$state = Get-UIWorkflowState -Path "C:\Temp\MyWorkflowState.dat"
```
Loads workflow state from a custom location.


---

## New-PoshUIWorkflow

Initializes a new PoshUI Workflow definition.

Creates a new Workflow UI context that can be populated with steps and workflow tasks.
This function must be called before adding any steps or tasks to the UI.

```
New-PoshUIWorkflow [-Title] <String> [-Description <String>] [-Icon <String>] [-SidebarHeaderText <String>] [-WindowTitleIcon <String>] [-SidebarHeaderIcon <String>] [-SidebarHeaderIconOrientation <String>] [-Theme <String>] [-AllowCancel <Boolean>] [-LogPath <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Title` | string | Yes |  |  | The title of the UI that will be displayed in the window title bar. |
| `-Description` | string | No |  |  | Optional description of the UI's purpose. |
| `-Icon` | string | No |  |  | Optional path to an icon file (PNG, ICO) to display in the UI. Can also be a Segoe MDL2 icon glyph in the format '&#xE1D3;'. |
| `-SidebarHeaderText` | string | No |  |  | Optional text to display in the sidebar header for branding. |
| `-WindowTitleIcon` | string | No |  |  |  |
| `-SidebarHeaderIcon` | string | No |  |  | Optional icon for the sidebar header. Can be a file path or Segoe MDL2 glyph. |
| `-SidebarHeaderIconOrientation` | string | No | `Left`, `Right`, `Top`, `Bottom` | Left | Optional orientation for the sidebar icon relative to the text. Supported values: Left (default), Right, Top, Bottom. |
| `-Theme` | string | No | `Light`, `Dark`, `Auto` | Auto | The theme to use for the UI. Valid values are 'Light', 'Dark', or 'Auto'. Default is 'Auto' which follows the system theme. |
| `-AllowCancel` | bool | No |  | True | Whether to allow users to cancel the UI. Default is $true. |
| `-LogPath` | string | No |  |  |  |

**Examples**

```powershell
New-PoshUIWorkflow -Title "Server Deployment Workflow"
```
Creates a new Workflow UI with the specified title.

```powershell
New-PoshUIWorkflow -Title "System Setup" -Description "Automated system configuration" -Theme Dark
```
Creates a new Workflow UI with title, description, and dark theme.


---

## Register-UIWorkflowResumeTask

Registers a scheduled task to auto-resume the workflow after system reboot.

Creates a Windows scheduled task that runs the workflow script at system startup
to automatically resume execution after a reboot. The task is configured to:
- Run at system startup (before user logon if running as SYSTEM)
- Run with highest privileges if needed
- Self-delete after the workflow completes

```
Register-UIWorkflowResumeTask [[-ScriptPath] <String>] [[-TaskName] <String>] [-RunAsSystem] [[-PowerShellPath] <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-ScriptPath` | string | No |  |  | Path to the workflow script to run after reboot. If not specified, uses the current script path from the workflow state. |
| `-TaskName` | string | No |  |  | Name for the scheduled task. Defaults to `PoshUI_WorkflowResume_<WorkflowId>`. |
| `-RunAsSystem` | switch | No |  |  | If specified, the task runs as SYSTEM account (requires admin privileges). Otherwise, runs as the current user at logon. |
| `-PowerShellPath` | string | No |  |  | Path to PowerShell executable. Defaults to pwsh.exe for PowerShell 7+, or powershell.exe for Windows PowerShell. |

**Examples**

```powershell
Register-UIWorkflowResumeTask
```
Registers a task to resume the current workflow script after reboot.

```powershell
Register-UIWorkflowResumeTask -RunAsSystem
```
Registers a task that runs as SYSTEM at startup (requires admin).


---

## Resume-UIWorkflow

Resumes a workflow from saved state.

Restores a workflow from a saved state file and prepares it for continued execution.
The workflow will skip already completed tasks and resume from the last pending task.

```
Resume-UIWorkflow [[-Path] <String>] [[-State] <Hashtable>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Path` | string | No |  |  | Optional custom path for the state file. If not specified, searches default locations. |
| `-State` | hashtable | No |  |  | Optional pre-loaded state hashtable from Get-UIWorkflowState. |

**Examples**

```powershell
if (Test-UIWorkflowState) {
    Resume-UIWorkflow
    Show-PoshUIWorkflow
}
```
Resumes workflow from saved state if one exists.

```powershell
$state = Get-UIWorkflowState -Path "C:\Temp\state.json"
Resume-UIWorkflow -State $state
```
Resumes workflow from a pre-loaded state.


---

## Save-UIWorkflowState

Saves the current workflow state to an encrypted file for later resume.

Serializes the current workflow state including task progress, wizard results,
and execution state to an encrypted file. This enables resuming workflow execution
after a reboot or script restart.

Security features:
- DPAPI encryption (CurrentUser scope - only this user can decrypt)
- HMAC-SHA256 integrity validation (detects tampering)
- Restrictive ACLs (current user only)
- Secure file extension (.dat instead of .json)

```
Save-UIWorkflowState [[-Path] <String>] [[-Workflow] <Object>] [-NoEncryption] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Path` | string | No |  |  | Optional custom path for the state file. If not specified, uses the default location in $env:LOCALAPPDATA\PoshUI\PoshUI_Workflow_State.dat |
| `-Workflow` | object | No |  |  | Optional UIWorkflow object to save. If not specified, saves the current workflow. |
| `-NoEncryption` | switch | No |  |  | If specified, saves state as plain JSON without encryption. NOT recommended for production use. Useful for debugging only. |

**Examples**

```powershell
Save-UIWorkflowState
```
Saves the current workflow state (encrypted) to the default location.

```powershell
Save-UIWorkflowState -Path "C:\Temp\MyWorkflowState.dat"
```
Saves the current workflow state (encrypted) to a custom location.

```powershell
Save-UIWorkflowState -NoEncryption
```
Saves state as plain JSON (for debugging only).


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

## Set-UITheme

Applies a custom color theme to the UI using a simple PowerShell hashtable.

Configures the UI color scheme by accepting a hashtable of named color slots.
No XAML knowledge required - just provide hex color values for the slots you want to override.
Any slots not specified will use the base theme defaults (Light or Dark).

Supports three usage patterns:
1. Single theme: Set-UITheme @{ AccentColor = '#FF6B35' }
2. Dual themes: Set-UITheme -Light @{...} -Dark @{...}
3. Mixed: Set-UITheme @{ AccentColor = '#FF6B35' } -Light @{...} -Dark @{...}

Must be called after New-PoshUIWorkflow.
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

## Show-PoshUIWorkflow

Displays the Workflow UI and executes the workflow tasks.

Serializes the current Workflow UI definition to JSON,
launches the PoshUI executable, and returns the results.

```
Show-PoshUIWorkflow [[-DefaultValues] <Hashtable>] [-NonInteractive] [[-Theme] <String>] [[-OutputFormat] <String>] [-AppDebug] [-RequireSignedScripts] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-DefaultValues` | hashtable | No |  | @{} | Hashtable of default values to pre-populate in the UI form. Keys should match the control names. |
| `-NonInteractive` | switch | No |  |  | Run the UI in non-interactive mode using only the default values. The UI will not be displayed. |
| `-Theme` | string | No | `Light`, `Dark`, `Auto` |  | Override the theme for this UI execution. Valid values are 'Light', 'Dark', or 'Auto'. |
| `-OutputFormat` | string | No | `Object`, `JSON`, `Hashtable` | Object | Format for the returned results. Valid values are 'Object', 'JSON', 'Hashtable'. Default is 'Object'. |
| `-AppDebug` | switch | No |  |  |  |
| `-RequireSignedScripts` | switch | No |  |  |  |

**Examples**

```powershell
$result = Show-PoshUIWorkflow
```
Shows the Workflow UI and returns results.

```powershell
$defaults = @{ ServerName = 'SQL01'; Environment = 'Production' }
$result = Show-PoshUIWorkflow -DefaultValues $defaults
```
Shows the Workflow UI with pre-populated default values.


---

## Test-UIWorkflowState

Tests if a saved workflow state exists.

Checks if a workflow state file exists at the specified or default location.
Useful for determining if a workflow should resume from a previous execution.

```
Test-UIWorkflowState [[-Path] <String>] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-Path` | string | No |  |  | Optional custom path for the state file. If not specified, searches default locations: - $env:LOCALAPPDATA\PoshUI\PoshUI_Workflow_State.dat (encrypted, preferred) - $env:LOCALAPPDATA\PoshUI\PoshUI_Workflow_State.json (legacy plain) - $env:PROGRAMDATA\PoshUI\PoshUI_Workflow_State.dat (encrypted) - $env:PROGRAMDATA\PoshUI\PoshUI_Workflow_State.json (legacy plain) |

**Examples**

```powershell
if (Test-UIWorkflowState) {
    Write-Host "Resuming from saved state..."
}
```
Checks if a saved state exists and acts accordingly.

```powershell
Test-UIWorkflowState -Path "C:\Temp\MyWorkflowState.dat"
```
Checks if a state file exists at the specified path.


---

## Unregister-UIWorkflowResumeTask

Removes the scheduled task created for workflow auto-resume.

Unregisters the Windows scheduled task that was created to auto-resume
the workflow after a system reboot. This should be called when the
workflow completes successfully or is cancelled.

```
Unregister-UIWorkflowResumeTask [[-TaskName] <String>] [-RemoveAll] [<CommonParameters>]
```

| Parameter | Type | Required | Accepted values | Default | Description |
|---|---|---|---|---|---|
| `-TaskName` | string | No |  |  | Name of the scheduled task to remove. If not specified, uses the task name stored during Register-UIWorkflowResumeTask or searches for tasks matching the PoshUI_WorkflowResume_* pattern. |
| `-RemoveAll` | switch | No |  |  | If specified, removes all PoshUI workflow resume tasks (useful for cleanup). |

**Examples**

```powershell
Unregister-UIWorkflowResumeTask
```
Removes the current workflow's resume task.

```powershell
Unregister-UIWorkflowResumeTask -RemoveAll
```
Removes all PoshUI workflow resume tasks.



