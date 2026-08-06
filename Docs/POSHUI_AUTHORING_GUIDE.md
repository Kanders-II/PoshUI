# PoshUI Authoring Guide & Best Practices

::: warning Scope
This guide covers the three **original** modules — Wizard, Dashboard and Workflow — as of PoshUI 1.3.1.
It does **not** cover **PoshUI.Canvas** (added in 1.4.0), which has its own authoring documentation:
[About Canvas](./canvas/about.md), the [Capability Reference](./Canvas-Reference.md), and the
[Agent Authoring Guide](./agent/README.md) for LLM-assisted authoring.
:::

> A practical, accurate reference for writing PoshUI scripts — Wizards, Dashboards, and
> Workflows — with a deep focus on the Dashboard module and **ScriptCards** (including
> dot-sourcing external PowerShell files).
>
> Every cmdlet signature and example in this guide was extracted from and **executed against**
> the PoshUI source in this repo (PoshUI 1.3.1). Examples are validated to build without error.
> This document is designed to be fed to an LLM as authoring context.

---

## 1. Mental model

PoshUI is a **PowerShell-module front end** over a compiled **.NET Framework 4.8 / WPF executable** (`PoshUI.exe`). You describe a UI with familiar PowerShell cmdlets; the module serializes that description and hands it to the EXE, which renders a Windows 11–style window and streams results back.

There are **three independent modules**. Pick exactly one per script.

| Module | Use it for | Entry cmdlet |
|---|---|---|
| **PoshUI.Wizard** | Step-by-step **input forms** with validation, then run logic | `New-PoshUIWizard` |
| **PoshUI.Dashboard** | **Monitoring + clickable tools** (cards: metrics, charts, tables, status, **ScriptCards**) | `New-PoshUIDashboard` |
| **PoshUI.Workflow** | **Sequential task execution** with progress, approval gates, reboot/resume | `New-PoshUIWorkflow` |

### The universal build pattern

Every script follows the same shape:

```powershell
Import-Module .\PoshUI\PoshUI.Dashboard\PoshUI.Dashboard.psd1   # 1. one module only

New-PoshUIDashboard -Title '...'        # 2. create the definition
Set-UIBranding   ...                    # 3. (optional) branding
Set-UITheme      ...                    # 4. (optional) custom colors
Add-UIStep       -Name '...' ...        # 5. one or more steps (= pages)
Add-UI<Control>  -Step '...' ...        # 6. controls/cards on each step
Show-PoshUIDashboard                    # 7. launch the window (blocks until closed)
```

> **How it runs under the hood:** Wizard/Workflow generate a PowerShell `param()`-block script that the EXE AST-parses; Dashboard serializes to JSON. You normally never see this — but it explains two rules below (safe identifiers, and "values are data, not code").

---

## 2. Golden rules (read before generating any script)

1. **Import only one PoshUI module per script.** All three export overlapping names (`Add-UIStep`, `Set-UIBranding`, `Add-UICard`, …). Importing two clobbers cmdlets.
2. **`-Name` is an internal identifier; `-Title`/`-Label` is what the user sees.** A control/step `-Name` must be a **safe identifier**: start with a letter or underscore, then letters/digits/underscores, max 64 chars. (`ServerName`, `Disk_Check1` ✓ — `Disk Check`, `1st`, `evil'; calc` ✗.) The generator rejects unsafe names.
3. **`-Name` must be unique within its step.**
4. **Icons** are either a **Segoe MDL2 glyph** in the form `'&#xE713;'`, or a path to a **PNG/ICO** via `-IconPath`. See `Docs/FLUENT_ICONS_REFERENCE.md` for glyphs. `-IconPath` wins over `-Icon` when both are given.
5. **Use here-strings (`@"..."@`) for multi-line text** (card content, descriptions) rather than `` `n ``.
6. **Values are treated as data, not code.** PoshUI escapes every value you pass into the generated script, so apostrophes and special characters are safe. Do not expect a value to "execute."
7. **Never put secrets in `-DefaultParameters`/defaults or persisted state.** Secret-named fields and `SecureString`/`PSCredential` are auto-redacted before any state is written, but don't rely on it — pass secrets at runtime.
8. **`Show-*` blocks** until the user closes the window and returns results. For headless/CI validation of a definition, build it and inspect with `Get-PoshUIDashboard` / `Get-PoshUIDashboard -AsJson` instead of launching.

---

## 3. Dashboard module (primary focus)

A Dashboard is a set of **steps**, each rendered as a **page** (sidebar entry) containing a responsive **grid of cards**.

### 3.1 Create the dashboard

```powershell
New-PoshUIDashboard -Title 'Admin Tools' `
    -Description 'Ops console' `
    -GridColumns 3 `          # 1–6 cards per row (default 3)
    -Theme 'Dark'             # Light | Dark | Auto (default Auto = follow system)
```

Other useful params: `-Icon` (window/taskbar icon, glyph or path), `-SidebarHeaderText`, `-SidebarHeaderIcon`, `-SidebarHeaderIconOrientation` (`Left|Right|Top|Bottom`), `-AllowCancel`, `-LogPath`.

### 3.2 Branding & theme

```powershell
Set-UIBranding -WindowTitle 'Admin' `
    -SidebarHeaderText 'Quick Tools' `
    -SidebarHeaderIcon '&#xE713;' `
    -SidebarHeaderIconOrientation 'Top'

# Custom colors — independent palettes for light and dark (any subset of 24 slots)
Set-UITheme -Light @{ AccentColor = '#FF6B35' } `
            -Dark  @{ AccentColor = '#FF6B35'; Background = '#1A1A2E' }
```

Color slots (hex `#RRGGBB` or `#AARRGGBB`): `AccentColor, AccentDark, AccentDarker, AccentLight, Background, ContentBackground, CardBackground, SidebarBackground, SidebarText, SidebarHighlight, TextPrimary, TextSecondary, ButtonBackground, ButtonForeground, InputBackground, InputBorder, BorderColor, TitleBarBackground, TitleBarText, SuccessColor, WarningColor, ErrorColor`, plus non-color `FontFamily, CornerRadius`.

### 3.3 Add a step (= a dashboard page)

```powershell
Add-UIStep -Name 'Tools' -Title 'Administrative Tools' -Type 'Dashboard' -Icon '&#xE770;'
```

> For dashboards, **always pass `-Type 'Dashboard'`**. Add multiple steps to get multiple pages in the sidebar. A banner at the top of a page:
>
> ```powershell
> Add-UIBanner -Step 'Tools' -Name 'B1' -Title 'Administrative Tools' `
>     -Subtitle 'Common monitoring and maintenance tasks' -BackgroundColor '#0078D4'
> ```

### 3.4 Card catalog

All `Add-UI*Card` cmdlets share `-Step -Name -Title` (all required) plus `-Description`, `-Icon`/`-IconPath`, `-Category`.

| Cmdlet | Purpose | Key params |
|---|---|---|
| `Add-UIScriptCard` | **Clickable tool** that runs a script with a parameter dialog + live console | `-ScriptBlock` *or* `-ScriptPath`, `-DefaultParameters`, `-Category`, `-Tags` |
| `Add-UIMetricCard` | Single KPI value, trend, gauge, target bar | `-Value` (number **or** scriptblock), `-Unit`, `-Format`, `-Trend`, `-Target`, `-ShowGauge`, `-ShowSparkline`, `-SparklineData`, `-RefreshScript` |
| `Add-UIChartCard` | Line/Bar/Area/Pie/Donut chart | `-ChartType`, `-Data` (array/hashtable **or** scriptblock), `-ShowLegend`, `-RefreshScript` |
| `Add-UITableCard` | Sortable/filterable/exportable data grid | `-Data` (objects **or** scriptblock), `-RefreshScript` |
| `Add-UIStatusCard` | List of items with auto-colored status dots | `-Data` (`@(@{Label;Status})`), `-RefreshScript` |
| `Add-UICard` | Static info panel (text/links/gradient) | `-Content`, `-LinkUrl`, `-BackgroundColor`, `-GradientStart/End`, … |

**Live data:** for `MetricCard`, `ChartCard`, `TableCard`, `StatusCard`, if you pass a **scriptblock** to `-Value`/`-Data`, it is run once for the initial render and automatically reused as the card's **refresh** action (the refresh button re-runs it). You can also pass an explicit `-RefreshScript`.

**StatusCard auto-coloring** (by the `Status` string): green = `Online/Running/Healthy/OK/Active/Up/Connected/Success`; amber = `Warning/Degraded/Slow/Pending/Starting`; red = `Offline/Stopped/Error/Critical/Down/Failed/Disconnected`; gray = `Maintenance/Disabled/Unknown`.

```powershell
# Metric (static and dynamic)
Add-UIMetricCard -Step 'Tools' -Name 'CPU' -Title 'CPU Usage' -Value 42 -Unit '%' -Trend 'up' -Target 80
Add-UIMetricCard -Step 'Tools' -Name 'Mem' -Title 'Memory' -Value { 11 } -Unit 'GB' -Target 16 -ShowGauge

# Chart
$data = @(@{Month='Jan';Sales=100}, @{Month='Feb';Sales=150}, @{Month='Mar';Sales=120})
Add-UIChartCard -Step 'Tools' -Name 'Sales' -Title 'Sales' -ChartType 'Bar' -Data $data
Add-UIChartCard -Step 'Tools' -Name 'ProcCPU' -Title 'Top Procs' -ChartType 'Line' `
    -Data { Get-Process | Select-Object -First 5 Name, CPU }

# Table (live)
Add-UITableCard -Step 'Tools' -Name 'Svcs' -Title 'Services' `
    -Data { Get-Service | Select-Object -First 20 Name, Status, StartType }

# Status
Add-UIStatusCard -Step 'Tools' -Name 'Health' -Title 'Service Health' -Icon '&#xE770;' -Data @(
    @{Label='Active Directory'; Status='Online'}
    @{Label='DNS';              Status='Warning'}
    @{Label='Legacy App';       Status='Offline'}
)

# Info panel
Add-UICard -Step 'Tools' -Name 'Notes' -Title 'Notes' -Icon '&#xE946;' -BackgroundColor '#107C10' -Content @"
Guidelines:
- Run health checks before maintenance
- Escalate anything red
"@
```

---

## 4. ScriptCard deep-dive

A **ScriptCard** is the heart of an actionable dashboard. It renders as a tile; when the user **clicks it**, PoshUI:

1. **Auto-discovers the script's parameters** (by AST-parsing its `param()` block),
2. opens a **dialog with one input control per parameter**, pre-filled with defaults,
3. **executes the script in its own runspace** with the user's values, and
4. streams **real-time console output** into the dialog.

ScriptCards do **not** participate in form collection — they're independent, on-demand tools.

### 4.1 Two sources (mutually exclusive)

```powershell
# (a) Inline scriptblock
Add-UIScriptCard -Step 'Tools' -Name 'DiskCheck' -Title 'Check Disk' `
    -Description 'View disk usage' -Category 'Monitoring' -Icon '&#xE7C4;' `
    -ScriptBlock {
        param(
            [ValidateSet('C','D','E')][string]$DriveLetter = 'C',
            [switch]$Detailed
        )
        $d = Get-PSDrive -Name $DriveLetter
        "Free on ${DriveLetter}: {0:N1} GB" -f ($d.Free/1GB)
        if ($Detailed) { $d | Format-List }
    }

# (b) External .ps1 file — parameters discovered from the file's param() block
Add-UIScriptCard -Step 'Tools' -Name 'FileTool' -Title 'File Processor' `
    -Category 'Advanced' -ScriptPath 'C:\Tools\Process-Files.ps1'
```

`-DefaultParameters @{ ... }` pre-fills/overrides the dialog; `-Category` and `-Tags` drive filtering in the grid.

```powershell
Add-UIScriptCard -Step 'Tools' -Name 'Pinger' -Title 'Ping' `
    -ScriptBlock { param([string]$HostName='localhost',[int]$Count=4) Test-Connection $HostName -Count $Count } `
    -DefaultParameters @{ HostName = '8.8.8.8'; Count = 2 }
```

### 4.2 Parameter auto-discovery — how to control the generated inputs

Design your `param()` block deliberately; this table is exactly how PoshUI maps it to controls:

| In your `param()` | Becomes this control |
|---|---|
| `[string]` | TextBox |
| `[int]` / `[long]` / `[double]` / `[decimal]` | Numeric (with step) |
| `[bool]` | Checkbox |
| `[switch]` | Toggle |
| `[datetime]` | Date picker |
| `[SecureString]` / `[PSCredential]` | Password (masked) |
| `[string[]]` | Multi-line TextBox (one value per line) |
| `[ValidateSet('A','B')]` | **Dropdown** of the allowed values |
| `[ValidateRange(1,10)]` | Numeric bounded 1–10 |
| `[ValidatePattern('...')]` | TextBox with regex validation |
| `[ValidateScript({ Test-Path $_ -PathType Leaf })]` | **File picker** |
| `[ValidateScript({ Test-Path $_ -PathType Container })]` | **Folder picker** |
| Param named `*Folder`/`*Directory`/`*Dir` | Folder picker |
| Param named `*File`/`*Path`/`ScriptPath`/`ConfigPath`/`LogPath` | File picker |

Also honored: `[Parameter(Mandatory)]` and `[ValidateNotNullOrEmpty()]` mark the field required; `[Parameter(HelpMessage='...')]` becomes the field's tooltip; a `= default` literal pre-fills the input. Parameter names are auto-prettified for labels (`DriveLetter` → "Drive Letter").

> **Tip for the LLM:** to get a clean dialog, give every parameter a sensible default, a `ValidateSet`/`ValidateRange` where it applies, and a `HelpMessage`. Use type and validation to choose the control — you rarely need to configure the UI separately.

### 4.3 Dot-sourcing PowerShell files into ScriptCards

ScriptCards run in their **own runspace** inside `PoshUI.exe`, separate from the dashboard script. Two consequences drive the best practice:

- Functions defined in your dashboard script are **not** automatically available inside a card.
- `$PSScriptRoot` / `$PSCommandPath` and the working directory are **not guaranteed** inside the card's runspace, so **relative** dot-source paths are unreliable.

**Best practice: bake an absolute path to your library and dot-source it inside the card.** Keep reusable logic in a `.ps1` of functions, resolve its absolute path at *build time*, and embed that into the card's scriptblock with `[scriptblock]::Create()` + a here-string.

```powershell
# Lib\DiskTools.ps1  -> contains:  function Get-DiskReport { param([string]$DriveLetter='C') ... }

# In your dashboard script — resolve the absolute path WHILE BUILDING the dashboard:
$libPath = Join-Path $PSScriptRoot 'Lib\DiskTools.ps1'   # absolute, resolved now

Add-UIScriptCard -Step 'Tools' -Name 'DiskReport' -Title 'Disk Report' -Category 'Storage' `
    -ScriptBlock ([scriptblock]::Create(@"
param([ValidateSet('C','D','E')][string]`$DriveLetter = 'C')
. '$libPath'                       # absolute path baked in at build time
Get-DiskReport -DriveLetter `$DriveLetter
"@))
```

Notes on the here-string:
- Use a **double-quoted** here-string so `$libPath` expands into the literal path, but **escape every runtime `$`** you want to survive into the card as `` `$ `` (e.g. `` `$DriveLetter ``).
- Auto-discovery still works: it parses the `param()` block at the top, so `DriveLetter` becomes a dropdown.

**Alternative (often cleaner): make each tool a self-contained `.ps1`.** Put the `param()` block at the top and reference it with `-ScriptPath`. If that script needs shared helpers, dot-source them by absolute path from inside the script:

```powershell
# C:\Tools\New-CompanyUser.ps1
param(
    [Parameter(Mandatory)][string]$UserName,
    [ValidateSet('IT','Sales','Ops')][string]$Department = 'IT'
)
. 'C:\Tools\Lib\ADHelpers.ps1'     # absolute path
New-CompanyUser -UserName $UserName -Department $Department
```

```powershell
Add-UIScriptCard -Step 'Users' -Name 'NewUser' -Title 'Create User' -ScriptPath 'C:\Tools\New-CompanyUser.ps1'
```

**Anti-patterns to avoid in cards:** relying on `$PSScriptRoot`; dot-sourcing a relative path (`. .\Lib\x.ps1`); assuming a module imported by the dashboard script is loaded in the card (import it inside the card, or dot-source the functions).

---

## 5. Wizard module (high level)

Input-form flow: collect validated values across steps, then run a `-ScriptBody` where each control `-Name` is available as a `$variable`.

```powershell
Import-Module .\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1

New-PoshUIWizard -Title 'Server Setup' -Theme 'Auto'
Set-UIBranding   -WindowTitle 'Server Setup' -SidebarHeaderText 'Acme' -SidebarHeaderIcon '&#xE8BC;'

Add-UIStep -Name 'Config' -Title 'Configuration' -Order 1 -Icon '&#xE713;'

Add-UICard      -Step 'Config' -Title 'Welcome' -Content 'Fill in the fields below.'   # NOTE: no -Name in Wizard
Add-UITextBox   -Step 'Config' -Name 'ServerName'  -Label 'Server Name' -Mandatory
Add-UIDropdown  -Step 'Config' -Name 'Environment' -Label 'Environment' -Choices @('Dev','Test','Prod') -Mandatory
Add-UIOptionGroup -Step 'Config' -Name 'Edition' -Label 'Edition' -Options @('Standard','Datacenter') -Orientation 'Horizontal' -Default 'Standard'
Add-UINumeric   -Step 'Config' -Name 'CpuCount' -Label 'CPU Cores' -Minimum 1 -Maximum 64 -Default 4
Add-UIDate      -Step 'Config' -Name 'GoLive'   -Label 'Go-Live Date' -Default ([datetime]'2026-07-01')
Add-UIPassword  -Step 'Config' -Name 'AdminPwd' -Label 'Admin Password' -MinLength 8 -Mandatory

Show-PoshUIWizard -ScriptBody {
    Write-Host "Configuring $ServerName in $Environment ($Edition, $CpuCount cores)"
    # $AdminPwd is a [SecureString]
}
```

Common controls: `Add-UITextBox` (+`-Multiline`), `Add-UIPassword`, `Add-UICheckbox`, `Add-UIToggle`, `Add-UIDropdown`, `Add-UIListBox`, `Add-UIFilePath`, `Add-UIFolderPath`, `Add-UINumeric`, `Add-UIDate`, `Add-UIOptionGroup`, `Add-UIMultiLine`, plus display: `Add-UIBanner`, `Add-UICard`.

**Dynamic / cascading dropdowns** — pass a `-ScriptBlock` whose `param()` names another control; it re-evaluates when that control changes:

```powershell
Add-UIDropdown -Step 'S' -Name 'Env'    -Label 'Environment' -Choices @('Dev','Prod')
Add-UIDropdown -Step 'S' -Name 'Server' -Label 'Server' -ScriptBlock {
    param($Env)
    if ($Env -eq 'Prod') { @('PROD01','PROD02') } else { @('DEV01') }
}
```

---

## 6. Workflow module (high level)

Sequential tasks with progress, approval gates, error policy, and reboot/resume (state encrypted with DPAPI).

```powershell
Import-Module .\PoshUI\PoshUI.Workflow\PoshUI.Workflow.psd1

New-PoshUIWorkflow -Title 'Server Deployment' -Description 'Provision + configure'
Set-UIBranding     -WindowTitle 'Deployment' -SidebarHeaderText 'Acme Ops'

Add-UIStep -Name 'Deploy' -Title 'Deployment' -Order 1 -Type 'Workflow'

Add-UIWorkflowTask -Step 'Deploy' -Name 'Precheck' -Title 'Pre-flight checks' -Order 1 -ScriptBlock {
    $PoshUIWorkflow.UpdateProgress(50, 'Checking prerequisites...')
    $PoshUIWorkflow.WriteOutput('All prerequisites met', 'INFO')
}

Add-UIWorkflowTask -Step 'Deploy' -Name 'Approve' -Title 'Approval' -Order 2 `
    -TaskType 'ApprovalGate' -ApprovalMessage 'Proceed with deployment?' -RequireReason

Add-UIWorkflowTask -Step 'Deploy' -Name 'Install' -Title 'Install role' -Order 3 -OnError 'Stop' -ScriptBlock {
    $PoshUIWorkflow.UpdateProgress(10, 'Installing...')
    # ... real work ...
    $PoshUIWorkflow.UpdateProgress(100, 'Done')
}

Show-PoshUIWorkflow
```

**The `$PoshUIWorkflow` context object** (available inside every task scriptblock):

| Member | Purpose |
|---|---|
| `$PoshUIWorkflow.UpdateProgress(<percent>, '<message>')` | Update the task's progress bar + status text |
| `$PoshUIWorkflow.WriteOutput('<text>', '<level>')` | Write to the live console (`level` = `INFO`/`WARN`/`ERROR`) |
| `$PoshUIWorkflow.SetData('<key>', <value>)` | Persist a value for later tasks / after resume |
| `$PoshUIWorkflow.GetData('<key>')` | Read a value set earlier |
| `$PoshUIWorkflow.RequestReboot()` | Request a reboot; the workflow resumes at the next task afterward |

`Add-UIWorkflowTask` key params: `-ScriptBlock` *or* `-ScriptPath`, `-Order`, `-TaskType` (`Normal`/`ApprovalGate`), `-OnError` (`Stop`/`Continue`), `-Arguments`, `-ApprovalMessage`, `-RequireReason`. Workflow steps (`-Type Workflow`) can be mixed with normal input steps (`-Type Wizard`) to collect parameters before the task sequence.

---

## 7. Cross-module differences & gotchas

These trip people up because the same-named cmdlet differs by module:

- **`Add-UICard` name parameter:** Dashboard's `Add-UICard` **requires `-Name`**; Wizard's `Add-UICard` has **no `-Name`** (it auto-generates one) — pass `-Step -Title -Content` positionally/by name.
- **Implicit "current definition":** each `New-PoshUI*` sets the module's current definition; you don't pass it around. Only one definition is active at a time.
- **`Add-UIStep -Type`:** Dashboard pages need `-Type 'Dashboard'`; workflow task pages need `-Type 'Workflow'`; input forms are `-Type 'Wizard'` (default).
- **`Add-UIDate` / `Add-UINumeric` defaults/bounds:** `-Default`, `-Minimum`, `-Maximum` (and Numeric `-Increment`) are honored as of **1.3.1**. On older builds they were silently ignored — bump the modules if defaults don't appear.
- **ScriptCard runspace isolation:** see §4.3 — bake absolute paths; don't rely on `$PSScriptRoot` inside a card.
- **Validate headlessly:** `Show-*` opens a blocking GUI window. To check a definition without a display, build it then call `Get-PoshUIDashboard` / `Get-PoshUIDashboard -AsJson` (or for Wizard/Workflow, just build and avoid `Show`).

---

## 8. Complete, validated Dashboard example

A self-contained dashboard that combines every card type, both ScriptCard modes, and a dot-sourced library. (All builder calls in this script were executed successfully against PoshUI 1.3.1.)

```powershell
Import-Module .\PoshUI\PoshUI.Dashboard\PoshUI.Dashboard.psd1

# A reusable function library that cards will dot-source (absolute path baked at build time)
$libPath = Join-Path $PSScriptRoot 'Lib\DiskTools.ps1'

New-PoshUIDashboard -Title 'Admin Tools' -Description 'Ops console' -GridColumns 3 -Theme 'Dark'
Set-UIBranding -WindowTitle 'Admin' -SidebarHeaderText 'Quick Tools' -SidebarHeaderIcon '&#xE713;' -SidebarHeaderIconOrientation 'Top'
Set-UITheme    -Dark @{ AccentColor = '#FF6B35'; Background = '#1A1A2E' }

Add-UIStep   -Name 'Tools' -Title 'Administrative Tools' -Type 'Dashboard' -Icon '&#xE770;'
Add-UIBanner -Step 'Tools' -Name 'B1' -Title 'Administrative Tools' -Subtitle 'Common tasks' -BackgroundColor '#0078D4'

# Monitoring cards (live data via scriptblocks)
Add-UIMetricCard -Step 'Tools' -Name 'CPU' -Title 'CPU Usage' -Value 42 -Unit '%' -Trend 'up' -Target 80 -Icon '&#xE7C4;'
Add-UITableCard  -Step 'Tools' -Name 'Svcs' -Title 'Services' -Data { Get-Service | Select-Object -First 20 Name, Status }
Add-UIStatusCard -Step 'Tools' -Name 'Health' -Title 'Service Health' -Icon '&#xE770;' -Data @(
    @{Label='Active Directory'; Status='Online'}
    @{Label='DNS';              Status='Warning'}
)

# ScriptCard — inline, auto-discovered parameters
Add-UIScriptCard -Step 'Tools' -Name 'DiskCheck' -Title 'Check Disk' -Category 'Monitoring' -Icon '&#xE7C4;' -ScriptBlock {
    param([ValidateSet('C','D','E')][string]$DriveLetter = 'C', [switch]$Detailed)
    $d = Get-PSDrive -Name $DriveLetter
    "Free on ${DriveLetter}: {0:N1} GB" -f ($d.Free/1GB)
}

# ScriptCard — dot-sourced library (absolute path baked in)
Add-UIScriptCard -Step 'Tools' -Name 'DiskReport' -Title 'Disk Report' -Category 'Storage' `
    -ScriptBlock ([scriptblock]::Create(@"
param([ValidateSet('C','D','E')][string]`$DriveLetter = 'C')
. '$libPath'
Get-DiskReport -DriveLetter `$DriveLetter
"@))

Show-PoshUIDashboard
```

---

## 9. Prompt scaffold for an LLM (paste before your request)

> You are generating a PoshUI script (PowerShell + the PoshUI 1.3.1 modules). Follow these rules:
> 1. Import exactly one module: `PoshUI.Wizard` (input forms), `PoshUI.Dashboard` (monitoring + clickable tools), or `PoshUI.Workflow` (sequential tasks). Choose based on the task.
> 2. Structure: `New-PoshUI<X>` → optional `Set-UIBranding`/`Set-UITheme` → `Add-UIStep` (Dashboards use `-Type 'Dashboard'`, Workflows `-Type 'Workflow'`) → add controls/cards → `Show-PoshUI<X>`.
> 3. `-Name` is an internal identifier: start with a letter/underscore, only letters/digits/underscores, ≤64 chars, unique per step. `-Title`/`-Label` is the user-facing text.
> 4. Icons are Segoe MDL2 glyphs like `'&#xE713;'` or a PNG/ICO via `-IconPath`. Multi-line text uses here-strings.
> 5. Dashboard cards: `Add-UIScriptCard` (clickable tool), `Add-UIMetricCard`, `Add-UIChartCard`, `Add-UITableCard`, `Add-UIStatusCard`, `Add-UICard`. For live data pass a scriptblock to `-Value`/`-Data`.
> 6. For ScriptCards, design the `param()` block to control the auto-generated dialog: use `[ValidateSet]` for dropdowns, `[int]/[ValidateRange]` for numerics, `[switch]` for toggles, `[SecureString]` for passwords, `[ValidateScript({Test-Path $_ -PathType Leaf/Container})]` for file/folder pickers, `[Parameter(Mandatory)]` for required, `HelpMessage` for tooltips, and `= default` for prefilled values.
> 7. To reuse functions in a ScriptCard, dot-source a `.ps1` **by absolute path** baked at build time (do not rely on `$PSScriptRoot` inside a card). Prefer self-contained `.ps1` tools referenced via `-ScriptPath`.
> 8. Workflows expose `$PoshUIWorkflow` inside task scriptblocks: `.UpdateProgress(pct,'msg')`, `.WriteOutput('text','INFO')`, `.SetData('k',v)`, `.GetData('k')`, `.RequestReboot()`.
> 9. Wizard `Add-UICard` has no `-Name`; Dashboard `Add-UICard` requires `-Name`.
> 10. Never hardcode secrets; pass them at runtime.
