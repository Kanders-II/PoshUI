# Cmdlet Reference

Complete reference for all PoshUI cmdlets organized by module.

---

## PoshUI.Wizard

Build step-by-step guided interfaces for configuration, deployment, and setup tasks.

### New-PoshUIWizard

Initializes a new wizard definition. Call this first before adding steps or controls.

```powershell
New-PoshUIWizard -Title "Server Configuration"
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Title` | String | Yes | Window title and sidebar header text. |
| `-Description` | String | No | Purpose description (not displayed, for documentation). |
| `-Icon` | String | No | Window icon. Path to `.png`/`.ico` file or Segoe MDL2 glyph (`'&#xE1D3;'`). |
| `-SidebarHeaderText` | String | No | Custom branding text in the sidebar header area. |
| `-SidebarHeaderIcon` | String | No | Icon next to sidebar header text. File path or Segoe MDL2 glyph. |
| `-SidebarHeaderIconOrientation` | String | No | Position of icon relative to text: `Left` (default), `Right`, `Top`, `Bottom`. |
| `-Theme` | String | No | Color theme: `Auto` (follows Windows), `Light`, `Dark`. Default: `Auto`. |
| `-AllowCancel` | Boolean | No | Show cancel/close button. Default: `$true`. |
| `-LogPath` | String | No | Custom path for execution logs. Default: `$env:LOCALAPPDATA\PoshUI\Logs\`. |

---

### Show-PoshUIWizard

Displays the wizard window and executes the optional script body when the user clicks Finish.

```powershell
Show-PoshUIWizard -ScriptBody {
    Write-Host "Creating VM: $VMName"
    # Your automation code here
}
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-ScriptBody` | ScriptBlock | No | Code to execute after user completes the wizard. Has access to all control values as variables (e.g., `$VMName`, `$Memory`). |
| `-DefaultValues` | Hashtable | No | Pre-populate controls with values. Keys match control names. Example: `@{ VMName = 'Server01'; Memory = 4096 }` |
| `-NonInteractive` | Switch | No | Skip UI display, use only `-DefaultValues`. Useful for testing or automated runs. |
| `-ShowConsole` | Boolean | No | Show live execution console during `-ScriptBody` execution. Default: `$true`. |
| `-Theme` | String | No | Override the theme set in `New-PoshUIWizard` for this execution. |
| `-OutputFormat` | String | No | Result format: `Object` (default), `JSON`, `Hashtable`. |
| `-RequireSignedScripts` | Switch | No | Enforce Authenticode signature verification on generated scripts. For high-security environments. |

---

### Add-UIStep

Adds a page (step) to the wizard. Users navigate through steps using Next/Back buttons.

```powershell
Add-UIStep -Name "Config" -Title "Configuration" -Icon "&#xE713;"
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Name` | String | Yes | Unique identifier. Use this name when adding controls to the step. |
| `-Title` | String | Yes | Display text shown in sidebar and step header. |
| `-Description` | String | No | Subtitle text shown below the title. |
| `-Icon` | String | No | Segoe MDL2 icon glyph (e.g., `'&#xE713;'` for Settings). |
| `-Order` | Int | No | Display order. Lower numbers appear first. Auto-assigned if omitted. |
| `-Type` | String | No | Step type: `Wizard` (default), `Dashboard`, `Workflow`. |
| `-Skippable` | Switch | No | Allow users to skip this step. |

---

### Set-UIBranding

Customizes the visual appearance of the wizard window.

```powershell
Set-UIBranding -WindowTitle "Contoso Setup" -AccentColor "#0078D4"
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-WindowTitle` | String | No | Override the window title bar text. |
| `-SidebarHeaderText` | String | No | Branding text in sidebar header. |
| `-SidebarHeaderIcon` | String | No | Icon for sidebar header. |
| `-LogoPath` | String | No | Path to logo image file for sidebar. |
| `-Theme` | String | No | Theme override: `Auto`, `Light`, `Dark`. |
| `-AllowCancel` | Boolean | No | Enable/disable close button. |

---

## Wizard Controls

Controls are input fields that collect data from users. Each control creates a PowerShell parameter accessible in your `-ScriptBody`.

### Add-UITextBox

Single-line text input field.

```powershell
Add-UITextBox -Step "Config" -Name "ServerName" -Label "Server Name" -Mandatory
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Name of the step to add this control to. |
| `-Name` | String | Yes | Control identifier. Becomes the variable name in `-ScriptBody` (e.g., `$ServerName`). |
| `-Label` | String | Yes | Text displayed above the input field. |
| `-Default` | String | No | Pre-filled value. |
| `-Mandatory` | Switch | No | User must enter a value to proceed. |
| `-Placeholder` | String | No | Greyed hint text shown when field is empty. |
| `-MaxLength` | Int | No | Maximum character limit. |
| `-ValidationPattern` | String | No | Regex pattern the input must match. Example: `'^[A-Za-z0-9-]+$'` |
| `-ValidationMessage` | String | No | Error message shown when validation fails. |
| `-Width` | Int | No | Control width in pixels. |
| `-HelpText` | String | No | Tooltip text on hover. |

---

### Add-UIPassword

Secure password input with masked characters.

```powershell
Add-UIPassword -Step "Security" -Name "AdminPassword" -Label "Administrator Password" -Mandatory -MinLength 8
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Step name. |
| `-Name` | String | Yes | Control identifier. Returns a `SecureString` in `-ScriptBody`. |
| `-Label` | String | Yes | Display label. |
| `-Mandatory` | Switch | No | Require input. |
| `-MinLength` | Int | No | Minimum password length. |
| `-ValidationPattern` | String | No | Regex for password complexity. Example: `'^(?=.*[A-Z])(?=.*\d).{8,}$'` (uppercase + digit + 8 chars). |
| `-ValidationScript` | ScriptBlock | No | Custom validation logic. Receives password as `$_`. Return `$true` if valid. |
| `-ValidationMessage` | String | No | Error message for failed validation. |
| `-ShowRevealButton` | Boolean | No | Show eye icon to reveal password. Default: `$true`. |
| `-Width` | Int | No | Control width in pixels. |
| `-HelpText` | String | No | Tooltip text. |

---

### Add-UIDropdown

Dropdown selection list (single choice).

```powershell
Add-UIDropdown -Step "Config" -Name "Environment" -Label "Target Environment" `
    -Choices @('Development', 'Staging', 'Production') -Default 'Development'
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Step name. |
| `-Name` | String | Yes | Control identifier. |
| `-Label` | String | Yes | Display label. |
| `-Choices` | String[] | Yes* | Static list of options. *Required unless using `-ScriptBlock`. |
| `-ScriptBlock` | ScriptBlock | No | Dynamic choices from PowerShell. See [Dynamic Controls](./controls/dynamic-controls.md). |
| `-DependsOn` | String[] | No | Controls that trigger re-evaluation of `-ScriptBlock`. |
| `-Default` | String | No | Pre-selected value (must be in choices). |
| `-Mandatory` | Switch | No | Require selection. |
| `-Width` | Int | No | Control width in pixels. |
| `-HelpText` | String | No | Tooltip text. |

---

### Add-UIListBox

Multi-line selection list (single or multiple selection).

```powershell
Add-UIListBox -Step "Config" -Name "Features" -Label "Select Features" `
    -Choices @('Web Server', 'Database', 'Monitoring', 'Backup') -MultiSelect
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Step name. |
| `-Name` | String | Yes | Control identifier. Returns `String` (single) or `String[]` (multi). |
| `-Label` | String | Yes | Display label. |
| `-Choices` | String[] | Yes* | Static list of options. *Required unless using `-ScriptBlock`. |
| `-ScriptBlock` | ScriptBlock | No | Dynamic choices from PowerShell. |
| `-DependsOn` | String[] | No | Controls that trigger re-evaluation. |
| `-Default` | Object | No | Pre-selected value(s). Array for multi-select. |
| `-MultiSelect` | Switch | No | Allow selecting multiple items. |
| `-Mandatory` | Switch | No | Require at least one selection. |
| `-Height` | Int | No | ListBox height in pixels. Default: 150. |
| `-Width` | Int | No | Control width in pixels. |
| `-HelpText` | String | No | Tooltip text. |

---

### Add-UICheckbox

Boolean checkbox (checked/unchecked).

```powershell
Add-UICheckbox -Step "Config" -Name "EnableSSL" -Label "Enable SSL/TLS encryption" -Default $true
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Step name. |
| `-Name` | String | Yes | Control identifier. Returns `$true` or `$false`. |
| `-Label` | String | Yes | Text displayed next to checkbox. |
| `-Default` | Boolean | No | Initial checked state. Default: `$false`. |
| `-Mandatory` | Switch | No | Checkbox must be checked to proceed. |
| `-HelpText` | String | No | Tooltip text. |

---

### Add-UIToggle

Toggle switch (on/off) - visual alternative to checkbox.

```powershell
Add-UIToggle -Step "Config" -Name "AutoStart" -Label "Start service automatically" -Default $true
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Step name. |
| `-Name` | String | Yes | Control identifier. Returns `$true` or `$false`. |
| `-Label` | String | Yes | Display label. |
| `-Default` | Boolean | No | Initial state. Default: `$false`. |
| `-OnLabel` | String | No | Text when ON. Default: "On". |
| `-OffLabel` | String | No | Text when OFF. Default: "Off". |
| `-Mandatory` | Switch | No | Toggle must be ON to proceed. |
| `-HelpText` | String | No | Tooltip text. |

---

### Add-UINumeric

Number input with optional spinner controls.

```powershell
Add-UINumeric -Step "Config" -Name "Memory" -Label "Memory (GB)" -Minimum 1 -Maximum 64 -Default 4
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Step name. |
| `-Name` | String | Yes | Control identifier. Returns `Int` or `Double`. |
| `-Label` | String | Yes | Display label. |
| `-Default` | Object | No | Initial value. |
| `-Minimum` | Object | No | Lowest allowed value. |
| `-Maximum` | Object | No | Highest allowed value. |
| `-Increment` | Object | No | Step amount for spinner buttons. Default: 1. |
| `-DecimalPlaces` | Int | No | Decimal precision. Default: 0 (integers). |
| `-Mandatory` | Switch | No | Require value entry. |
| `-Width` | Int | No | Control width in pixels. |
| `-HelpText` | String | No | Tooltip text. |

---

### Add-UIDate

Calendar date picker.

```powershell
Add-UIDate -Step "Schedule" -Name "StartDate" -Label "Start Date" `
    -Minimum (Get-Date) -Maximum (Get-Date).AddDays(30)
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Step name. |
| `-Name` | String | Yes | Control identifier. Returns `DateTime`. |
| `-Label` | String | Yes | Display label. |
| `-Default` | DateTime | No | Initial selected date. |
| `-Minimum` | DateTime | No | Earliest selectable date. |
| `-Maximum` | DateTime | No | Latest selectable date. |
| `-Format` | String | No | Display format string. Example: `'yyyy-MM-dd'`. |
| `-Mandatory` | Switch | No | Require date selection. |
| `-Width` | Int | No | Control width in pixels. |
| `-HelpText` | String | No | Tooltip text. |

---

### Add-UIFilePath

File path input with browse button.

```powershell
Add-UIFilePath -Step "Config" -Name "ConfigFile" -Label "Configuration File" `
    -Filter "*.json" -ValidateExists
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Step name. |
| `-Name` | String | Yes | Control identifier. Returns file path string. |
| `-Label` | String | Yes | Display label. |
| `-Default` | String | No | Pre-filled path. |
| `-Filter` | String | No | File type filter. Examples: `'*.ps1'`, `'*.log;*.txt'`. Default: `'All Files|*.*'`. |
| `-DialogTitle` | String | No | Custom title for the file picker dialog. |
| `-ValidateExists` | Switch | No | Require the selected file to exist. |
| `-Mandatory` | Switch | No | Require path entry. |
| `-HelpText` | String | No | Tooltip text. |

---

### Add-UIFolderPath

Folder path input with browse button.

```powershell
Add-UIFolderPath -Step "Config" -Name "OutputPath" -Label "Output Folder" -Mandatory
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Step name. |
| `-Name` | String | Yes | Control identifier. Returns folder path string. |
| `-Label` | String | Yes | Display label. |
| `-Default` | String | No | Pre-filled path. |
| `-Mandatory` | Switch | No | Require path entry. |
| `-HelpText` | String | No | Tooltip text. |

---

### Add-UIOptionGroup

Radio button group (mutually exclusive options).

```powershell
Add-UIOptionGroup -Step "Config" -Name "InstallType" -Label "Installation Type" `
    -Options @('Typical', 'Custom', 'Minimal') -Default 'Typical'
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Step name. |
| `-Name` | String | Yes | Control identifier. |
| `-Label` | String | Yes | Group label displayed above options. |
| `-Options` | String[] | Yes | List of radio button labels. Minimum 2 options required. |
| `-Default` | String | No | Pre-selected option (must be in options list). |
| `-Orientation` | String | No | Layout: `Vertical` (default) or `Horizontal`. |
| `-Mandatory` | Switch | No | Require selection. |
| `-Width` | Int | No | Control width in pixels. |
| `-HelpText` | String | No | Tooltip text. |

---

### Add-UICard

Informational card for displaying tips, warnings, or contextual help.

```powershell
Add-UICard -Step "Welcome" -Title "Important" -Content "Back up your data before proceeding." `
    -Type "Warning" -Icon "&#xE7BA;"
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Step name. |
| `-Title` | String | No | Card title text. |
| `-Content` | String | No | Main card content/message. |
| `-Type` | String | No | Visual style: `Info` (default), `Success`, `Warning`, `Error`, `Tip`. |
| `-Icon` | String | No | Segoe MDL2 icon glyph. |
| `-IconPath` | String | No | Path to custom icon image. |
| `-ImagePath` | String | No | Path to display image in card. |
| `-ImageOpacity` | Double | No | Image transparency (0.0-1.0). Default: 1.0. |
| `-LinkUrl` | String | No | URL for clickable link. |
| `-LinkText` | String | No | Link display text. Default: "Learn more". |
| `-BackgroundColor` | String | No | Hex color for card background. |
| `-TitleColor` | String | No | Hex color for title text. |
| `-ContentColor` | String | No | Hex color for content text. |
| `-CornerRadius` | Int | No | Border radius in pixels. Default: 8. |
| `-GradientStart` | String | No | Starting color for gradient background. |
| `-GradientEnd` | String | No | Ending color for gradient background. |
| `-Width` | String | No | Card width (`'Auto'` or pixels). |
| `-Height` | String | No | Card height (`'Auto'` or pixels). |

---

### Add-UIBanner

Carousel banner with rotating slides.

```powershell
$slides = @(
    @{ Title = 'Welcome'; Subtitle = 'Getting started'; BackgroundColor = '#0078D4' }
    @{ Title = 'Step 2'; Subtitle = 'Configure settings'; BackgroundColor = '#107C10' }
)
Add-UIBanner -Step "Welcome" -CarouselItems $slides -AutoRotate $true
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Step name. |
| `-Title` | String | No | Static banner title (if not using carousel). |
| `-Subtitle` | String | No | Static banner subtitle. |
| `-CarouselItems` | Hashtable[] | No | Array of slide definitions. See below for properties. |
| `-AutoRotate` | Boolean | No | Auto-advance slides. Default: `$false`. |
| `-RotateInterval` | Int | No | Milliseconds between slides. Default: 5000. |
| `-BannerStyle` | String | No | Preset style: `Default`, `Gradient`, `Image`, `Minimal`, `Hero`, `Accent`. |
| `-BannerConfig` | Hashtable | No | Override preset properties (Height, TitleFontSize, etc.). |
| `-Height` | Int | No | Banner height in pixels. |
| `-BackgroundColor` | String | No | Hex background color. |
| `-BackgroundImagePath` | String | No | Path to background image. |
| `-BackgroundImageOpacity` | Double | No | Background image transparency (0.0-1.0). |

**Carousel Item Properties:**
- `Title` - Slide title text
- `Subtitle` - Slide subtitle text
- `BackgroundColor` - Hex color for this slide
- `BackgroundImagePath` - Image file path
- `BackgroundImageOpacity` - Image transparency
- `LinkUrl` - Clickable URL for this slide
- `Clickable` - Enable click navigation (`$true`/`$false`)

---

## PoshUI.Dashboard

Build card-based monitoring interfaces with live data refresh.

### New-PoshUIDashboard

Initializes a new dashboard definition.

```powershell
New-PoshUIDashboard -Title "System Monitor" -GridColumns 4
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Title` | String | Yes | Window title. |
| `-Description` | String | No | Dashboard description. |
| `-Icon` | String | No | Window icon (file path or Segoe MDL2 glyph). |
| `-SidebarHeaderText` | String | No | Branding text in sidebar. |
| `-SidebarHeaderIcon` | String | No | Sidebar branding icon. |
| `-SidebarHeaderIconOrientation` | String | No | Icon position: `Left`, `Right`, `Top`, `Bottom`. |
| `-Theme` | String | No | Theme: `Auto`, `Light`, `Dark`. |
| `-AllowCancel` | Boolean | No | Show close button. Default: `$true`. |
| `-GridColumns` | Int | No | Card columns (1-6). Default: 3. |
| `-LogPath` | String | No | Custom log directory path. |

---

### Show-PoshUIDashboard

Displays the dashboard window.

```powershell
Show-PoshUIDashboard -Theme Dark
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-ScriptBody` | ScriptBlock | No | Code to execute (typically not needed for dashboards). |
| `-DefaultValues` | Hashtable | No | Pre-populated control values. |
| `-NonInteractive` | Switch | No | Run without displaying UI. |
| `-ShowConsole` | Boolean | No | Show execution console. Default: `$true`. |
| `-Theme` | String | No | Override theme for this execution. |
| `-OutputFormat` | String | No | Result format: `Object`, `JSON`, `Hashtable`. |
| `-RequireSignedScripts` | Switch | No | Enforce script signature verification. |

---

### Get-PoshUIDashboard

Inspects the current dashboard definition. Useful for debugging.

```powershell
Get-PoshUIDashboard -IncludeProperties
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-IncludeProperties` | Switch | No | Show all control properties. |
| `-StepName` | String | No | Filter to specific step. |
| `-AsJson` | Switch | No | Output as JSON string. |

---

### Add-UIMetricCard

**Dashboard Module Only** - Displays a single numeric KPI with optional progress bar and automatic trend tracking.

```powershell
Add-UIMetricCard -Step "Dashboard" -Name "CPU" -Title "CPU Usage" `
    -Value { (Get-CimInstance Win32_Processor).LoadPercentage } `
    -Unit "%" -Target 80
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | **Yes** | Step name. |
| `-Name` | String | **Yes** | Unique card identifier. |
| `-Title` | String | **Yes** | Card header text. |
| `-Value` | Object | **Yes** | Number or ScriptBlock returning a number. ScriptBlocks enable auto-refresh. |
| `-Description` | String | No | Subtitle below title. |
| `-Unit` | String | No | Unit suffix (e.g., `%`, `GB`, `items`). |
| `-Format` | String | No | Number format: `N0` (integer), `N2` (2 decimals), `P0` (percent). Default: `N0`. |
| `-Target` | Double | No | Target value. Shows progress bar when specified. |
| `-MinValue` | Double | No | Progress bar minimum. Default: `0`. |
| `-MaxValue` | Double | No | Progress bar maximum. Default: `100`. |
| `-Icon` | String | No | Segoe MDL2 icon glyph (e.g., `'&#xE7C4;'`). |
| `-Category` | String | No | Category for filtering. Default: `"General"`. |
| `-RefreshScript` | ScriptBlock | No | Custom refresh logic. Automatically set if `-Value` is a ScriptBlock. |

**Note:** Trend indicators (up/down/stable arrows) are **automatically calculated** by comparing current and previous values during refresh. You do not specify the trend direction manually.

---

### Add-UIChartCard

**Dashboard Module Only** - Displays data visualization (Line, Bar, Area, Pie charts).

```powershell
$data = @(
    @{Month='Jan'; Sales=100}
    @{Month='Feb'; Sales=150}
    @{Month='Mar'; Sales=120}
)
Add-UIChartCard -Step "Dashboard" -Name "SalesChart" -Title "Sales Trend" `
    -ChartType "Line" -Data $data
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | **Yes** | Step name. |
| `-Name` | String | **Yes** | Unique card identifier. |
| `-Title` | String | **Yes** | Card header text. |
| `-Data` | Object | **Yes** | Array of objects or ScriptBlock returning data. |
| `-ChartType` | String | No | Chart type: `Line` (default), `Bar`, `Area`, `Pie`. |
| `-Description` | String | No | Subtitle below title. |
| `-ShowLegend` | Boolean | No | Display chart legend. Default: `$true`. |
| `-ShowTooltip` | Boolean | No | Show tooltips on hover. Default: `$true`. |
| `-Icon` | String | No | Segoe MDL2 icon glyph. |
| `-Category` | String | No | Category for filtering. Default: `"General"`. |
| `-RefreshScript` | ScriptBlock | No | Custom refresh logic. Automatically set if `-Data` is a ScriptBlock. |

---

### Add-UITableCard

**Dashboard Module Only** - Displays tabular data in a sortable and filterable grid.

```powershell
Add-UITableCard -Step "Dashboard" -Name "Services" -Title "Windows Services" `
    -Data { Get-Service | Select-Object -First 10 Name, Status, StartType }
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | **Yes** | Step name. |
| `-Name` | String | **Yes** | Unique card identifier. |
| `-Title` | String | **Yes** | Card header text. |
| `-Data` | Object | **Yes** | Array of objects or ScriptBlock returning data. |
| `-Description` | String | No | Subtitle below title. |
| `-Icon` | String | No | Segoe MDL2 icon glyph. |
| `-Category` | String | No | Category for filtering. Default: `"General"`. |
| `-RefreshScript` | ScriptBlock | No | Custom refresh logic. Automatically set if `-Data` is a ScriptBlock. |

---

### Add-UIScriptCard

**Dashboard Module Only** - Interactive card that runs PowerShell scripts when clicked. Script parameters are automatically converted to UI controls.

```powershell
Add-UIScriptCard -Step "Tools" -Name "DiskCleanup" -Title "Disk Cleanup" `
    -Description "Clear temporary files" -Icon "&#xE74D;" `
    -ScriptBlock {
        param(
            [Parameter(Mandatory)]
            [ValidateSet('C','D','E')]
            [string]$Drive,

            [switch]$IncludeSystemFiles
        )
        # Cleanup logic here
        Write-Host "Cleaning drive $Drive..."
    }
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | **Yes** | Step name. |
| `-Name` | String | **Yes** | Unique card identifier. |
| `-Title` | String | **Yes** | Card header text. |
| `-ScriptPath` | String | **Yes*** | Path to `.ps1` file. *Either `-ScriptPath` or `-ScriptBlock` is required. |
| `-ScriptBlock` | ScriptBlock | **Yes*** | Inline script. *Either `-ScriptPath` or `-ScriptBlock` is required. |
| `-Description` | String | No | Card description shown below title. |
| `-Icon` | String | No | Segoe MDL2 icon glyph (e.g., `'&#xE74D;'`). |
| `-DefaultParameters` | Hashtable | No | Default values for script parameters. |
| `-Category` | String | No | Category for filtering. Default: `"General"`. |
| `-Tags` | String[] | No | Additional filter tags. |

**Supported Parameter Types in Scripts:**
| PowerShell Type | UI Control |
|-----------------|------------|
| `[string]` | Text box |
| `[bool]` / `[switch]` | Toggle switch |
| `[int]` / `[double]` | Numeric spinner |
| `[DateTime]` | Date picker |
| `[ValidateSet()]` | Dropdown |
| `[WizardFilePath()]` | File browser |
| `[WizardFolderPath()]` | Folder browser |

---

## PoshUI.Workflow

Orchestrate multi-step automated processes with progress tracking and reboot/resume.

### New-PoshUIWorkflow

Initializes a new workflow definition.

```powershell
New-PoshUIWorkflow -Title "Server Deployment" -Theme Dark
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Title` | String | Yes | Window title. |
| `-Description` | String | No | Workflow description. |
| `-Icon` | String | No | Window icon (file path or Segoe MDL2 glyph). |
| `-SidebarHeaderText` | String | No | Branding text in sidebar. |
| `-SidebarHeaderIcon` | String | No | Sidebar branding icon. |
| `-SidebarHeaderIconOrientation` | String | No | Icon position: `Left`, `Right`, `Top`, `Bottom`. |
| `-Theme` | String | No | Theme: `Auto`, `Light`, `Dark`. |
| `-AllowCancel` | Boolean | No | Show close button. Default: `$true`. |
| `-LogPath` | String | No | Custom log directory path. |

---

### Show-PoshUIWorkflow

Displays the workflow window and executes tasks.

```powershell
Show-PoshUIWorkflow
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-DefaultValues` | Hashtable | No | Pre-populated control values. |
| `-NonInteractive` | Switch | No | Run without displaying UI. |
| `-Theme` | String | No | Override theme for this execution. |
| `-OutputFormat` | String | No | Result format: `Object`, `JSON`, `Hashtable`. |
| `-AppDebug` | Switch | No | Enable debugging features. |
| `-RequireSignedScripts` | Switch | No | Enforce script signature verification. |

---

### Add-UIWorkflowTask

Adds an executable task to the workflow. Tasks run sequentially with progress tracking.

```powershell
Add-UIWorkflowTask -Step "Execution" -Name "InstallIIS" -Title "Install IIS" -Order 1 `
    -ScriptBlock {
        $PoshUIWorkflow.UpdateProgress(10, "Starting IIS installation...")
        Install-WindowsFeature -Name Web-Server
        $PoshUIWorkflow.UpdateProgress(100, "IIS installation complete")
    }
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Step` | String | Yes | Name of Workflow step to add task to. |
| `-Name` | String | Yes | Unique task identifier. |
| `-Title` | String | Yes | Task display name in UI. |
| `-Description` | String | No | Task description. |
| `-Order` | Int | No | Execution order. Auto-assigned if omitted. |
| `-Icon` | String | No | Segoe MDL2 icon glyph. |
| `-ScriptBlock` | ScriptBlock | Yes* | Task code. *Required for normal tasks. |
| `-ScriptPath` | String | No | Path to `.ps1` file (alternative to ScriptBlock). |
| `-Arguments` | Hashtable | No | Arguments to pass to the script. |
| `-TaskType` | String | No | `Normal` (default) or `ApprovalGate`. |
| `-OnError` | String | No | Error handling: `Stop` (halt workflow) or `Continue`. |

**Advanced Task Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-RetryCount` | Int | No | Number of retry attempts on failure. Default: 0 (no retry). |
| `-RetryDelaySeconds` | Int | No | Seconds to wait between retries. Default: 5. |
| `-TimeoutSeconds` | Int | No | Max execution time in seconds. 0 = no timeout. |
| `-SkipCondition` | String | No | PowerShell expression. If true, task is skipped. Can reference wizard values (`$ParamName`) or workflow data (`$WorkflowData['key']`). |
| `-SkipReason` | String | No | Message shown when task is skipped. |
| `-Group` | String | No | Group/phase name for visual organization. |
| `-RollbackScriptBlock` | ScriptBlock | No | Cleanup code to run if this task fails. |
| `-RollbackScriptPath` | String | No | Path to rollback `.ps1` file. |

**Approval Gate Parameters (when `-TaskType ApprovalGate`):**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-ApprovalMessage` | String | Yes | Message displayed to user. |
| `-ApproveButtonText` | String | No | Approve button text. Default: "Approve". |
| `-RejectButtonText` | String | No | Reject button text. Default: "Reject". |
| `-RequireReason` | Switch | No | Require reason when rejecting. |
| `-TimeoutMinutes` | Int | No | Auto-timeout (0 = no timeout). |
| `-DefaultTimeoutAction` | String | No | Action on timeout: `None`, `Approve`, `Reject`. |

---

### Save-UIWorkflowState

Saves workflow state for reboot/resume scenarios. Uses DPAPI encryption.

```powershell
Save-UIWorkflowState
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Path` | String | No | Custom state file path. Default: `$env:LOCALAPPDATA\PoshUI\PoshUI_Workflow_State.dat` |
| `-Workflow` | UIWorkflow | No | Specific workflow to save. Default: current workflow. |
| `-NoEncryption` | Switch | No | Save as plain JSON (debugging only, not recommended). |

---

### Resume-UIWorkflow

Resumes a workflow from saved state. Completed tasks are skipped.

```powershell
if (Test-UIWorkflowState) {
    Resume-UIWorkflow
    Show-PoshUIWorkflow
}
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Path` | String | No | Path to state file. |
| `-State` | Hashtable | No | Pre-loaded state from `Get-UIWorkflowState`. |

---

### Test-UIWorkflowState

Checks if saved workflow state exists.

```powershell
if (Test-UIWorkflowState) {
    Write-Host "Resumable workflow found"
}
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Path` | String | No | Path to check. Default: standard location. |

---

### Get-UIWorkflowState

Retrieves saved workflow state.

```powershell
$state = Get-UIWorkflowState
$state.CurrentTaskIndex  # See which task to resume
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Path` | String | No | Path to state file. |

---

### Clear-UIWorkflowState

Removes saved workflow state file.

```powershell
Clear-UIWorkflowState
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Path` | String | No | Path to state file. |

---

## Next Steps

- [Get Started Guide](./get-started.md)
- [Wizard Controls](./controls/about.md)
- [Dashboard Cards](./dashboard-cards-reference.md)
- [Workflow Tasks](./workflows/tasks.md)
- [Dynamic Data Sources](./controls/dynamic-controls.md)
- [Carousel Banners](./carousel-clickable-links.md)
