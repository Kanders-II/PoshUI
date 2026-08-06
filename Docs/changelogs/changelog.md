# Version History

All notable changes to the PoshUI project are documented here.

## [1.4.0] - Unreleased

### PoshUI.Canvas — Free-Form Apps

A fourth module joins Wizard, Dashboard and Workflow. Where those give you an opinionated shell, **Canvas gives
you a blank page and ~75 cmdlets**: lay out anything, anywhere, in a single `.ps1`. No XAML, no MVVM, no project
scaffolding, no build step — the script *is* the application.

```powershell
Import-Module PoshUI.Canvas
New-PoshUICanvas -Title 'Hello' -Theme Dark
Add-UICanvasPage -Title 'Home' -Layout VStack
Add-UICanvasTextBox -Name who -Value 'World'
Add-UICanvasButton 'Greet' -Style Accent -Action {
    Show-UICanvasToast -Message ("Hello, " + (Get-UICanvasValue -Name who))
}
Show-PoshUICanvas
```

Canvas covers the full WPF panel set (Grid, VStack, HStack, Wrap, Dock, absolute X/Y), ~55 control types,
reactive state, charts, animation, toasts/dialogs/flyouts, secondary windows, keyboard shortcuts, and theming.
A single page can express wizard, dashboard and workflow patterns together.

### Engine-Native Workflow Runner — `Add-UICanvasWorkflow -Engine`

Canvas workflows can now run on the engine's workflow executor instead of as a composed script action. Steps
execute **on their own runspace**, off the app's serialized gate — so progress and elapsed time stay live while a
long step runs, instead of freezing until it completes.

- Per-step **`-Retry`**, **`-TimeoutSeconds`**, **`-SkipWhen`**, and `-ExpectedSeconds` (weighted overall progress)
- Engine-managed **reboot-resume** via `-StateFile` — completed steps are pre-marked and the run continues
- New **`$wf`** step context: `UpdateProgress`, `WriteOutput`, `GetValue`/`SetValue`, `SetData`/`GetData`,
  `SkipTask`, `RequestReboot`
- The executor's runspace is seeded with the Canvas runtime cmdlets, so existing step scripts work unchanged
- Opt-in: without `-Engine` the previous behavior is preserved exactly, and the rendered UI is identical either way

```powershell
Add-UICanvasWorkflowStep 'Download' -ExpectedSeconds 30 -Retry 2 -Script {
    1..10 | ForEach-Object { $wf.UpdateProgress($_ * 10, "downloading $($_*10)%"); Start-Sleep 1 }
}
Add-UICanvasWorkflow -Engine -Name deploy -AutoStart
```

### ScriptCard — On-Demand Runner (`Add-UICanvasScriptCard`)

A dashboard card that runs a script **off the UI gate** in its own runspace, with a title, live status
(Idle → Running → Done/Failed), a Run button, and a console that streams the script's output as it runs. Long
tasks no longer block live `-Refresh` cards or other actions.

```powershell
Add-UICanvasScriptCard 'Collect logs' -Detail 'Zips the last 24h' -Script {
    "starting..."; "done"
}
```

### Real-World Example Apps

Two complete tools that do real local work, replacing the simulated showcases as the reference for what Canvas
builds. Both keep the showcase design language (chromeless toolbar + footer, pre-flight → live execution) and
both stay useful **without administrator rights** — read-only views always work, and privileged actions are
visibly greyed with a one-click "Relaunch as administrator".

- **`Endpoint-TaskOrchestrator.ps1`** — Windows Task Scheduler triage and orchestration:
  - **Cross-task triage in one view** — every task that failed, missed runs, or went stale. Task Scheduler shows
    "Last Run Result" one task at a time; this shows all of them at once, with the raw code **decoded** to plain
    English (unknown codes are reported as hex, never guessed at).
  - **Real orchestration** — runs a chosen sequence of tasks in order with per-task retry, timeout and skip, via
    `Add-UICanvasWorkflow -Engine`. Task Scheduler fires tasks independently on triggers and cannot express
    "B only after A succeeds".
  - **Click a row to inspect it** — a detail pane shows **what the task actually runs** (its executable and
    arguments), its triggers, principal and decoded last result. A failing task is usually a bad path or a
    missing exe, and Task Scheduler buries that several clicks deep.
  - **Per-task actions** — Run now, Enable, Disable and Export XML on the selected task. Every write action is
    refused without elevation, and refuses `\Microsoft\*` tasks unless you explicitly tick "Allow Windows
    tasks" — those belong to Windows and breaking them breaks the OS. Export is read-only, so it always works.
  - **Security triage** — non-Microsoft tasks and tasks running as SYSTEM with Highest privileges, a common
    persistence location that Task Scheduler has no view for.
- **`Endpoint-SupportDesk.ps1`** — end-user self-service, aimed at the calls a helpdesk actually gets:
  - **"Why is my computer slow?"** — the top complaint. Reports memory/disk pressure, days since restart, and
    the apps consuming the most memory and CPU, so the user can act or tell IT what to look at.
  - **Restart this computer** — pending-reboot is *detected* on the status page, so it is *actionable* here
    (confirmation dialog, 60-second countdown, and it tells you how to cancel).
  - **"Am I up to date?"** and **Renew my network address**, plus fixes for temp/cache/policy/mapped drives.
    Cache clearing **discovers which caches exist** rather than hardcoding paths. Service-level fixes (print
    spooler, DNS) are visibly greyed and refuse politely when unelevated; everything else works as a standard user.
  - **Support bundle** — collects diagnostics into a zip on the Desktop to attach to a ticket. A collector that
    fails is logged and skipped rather than losing the bundle, and nothing is uploaded anywhere.

### Fixed / improved

- **`Add-UICanvasDataGrid` gained `-OnChange`**, and the engine now raises `ValueChanged` when a grid row is
  selected. Selection was always readable via `Get-UICanvasValue`, but nothing fired on select — so a grid could
  not drive a detail pane. Master/detail now works in any Canvas app.
- **`Examples/Lib/EndpointUI.ps1`** — shared chrome, elevation detection, the result-code decoder and the task
  snapshot helpers. It also documents the key constraint both apps work around: **action and workflow-step
  scriptblocks run in the engine process's runspace**, which shares no functions or variables with the authoring
  script, so every action is composed from a prelude that dot-sources this library.

### Native Layout Controls — Tabs, Menu, GridSplitter, Viewbox

Four controls that previously required dropping to raw XAML are now first-class cmdlets.

```powershell
Add-UICanvasTabs -Name main -Children {
    Add-UICanvasTab 'Summary' -Children { Add-UICanvasLabel 'hello' }
    Add-UICanvasTab 'Details' -Layout Grid -ColumnWidths '2*,Auto,3*' -Children {
        Add-UICanvasCard -Properties @{ Column = 0 } -Children { Add-UICanvasLabel 'left' }
        Add-UICanvasGridSplitter -Properties @{ Column = 1 }      # drag to resize both panes
        Add-UICanvasCard -Properties @{ Column = 2 } -Children { Add-UICanvasLabel 'right' }
    }
    Add-UICanvasTab 'Locked' -Disabled -Children { Add-UICanvasLabel 'unreachable' }
}
```

- **`Add-UICanvasTabs` / `Add-UICanvasTab`** — a tabbed container. Controls inside a tab are registered
  normally, so `Get-UICanvasValue`/`Set-UICanvasValue` reach them. The Tabs control's own value is the selected
  index, and `Set-UICanvasValue` also accepts a tab's header text — so you can switch tabs from any action.
- **`Add-UICanvasMenu`** — a classic menu bar from nested `-Items`, with submenus to any depth, separators
  (`Text = '-'`), keyboard-gesture hints, icons, checkable and disabled entries. Each leaf's `Action` fires
  like a button (or use `Page` to navigate).
- **`Add-UICanvasGridSplitter`** — drag handle that resizes adjacent Grid columns (or rows with
  `-Orientation Horizontal`). Place it in its own Grid cell between the two panes.
- **`Add-UICanvasViewbox`** — scales its content to fit the available space. Vector scaling, so text stays crisp.

`MediaElement` remains XAML-only via `Add-UICanvasXaml` (see
[the escape hatch](../canvas/about.md#the-escape-hatch-raw-xaml)).

### Declarative Cascading Fields (`-DependsOn` / `-OptionsScript`)

`Add-UICanvasDropdown` and `Add-UICanvasListBox` can recompute their options whenever another field changes —
no `-OnChange` wiring. Change-detected, so it never clobbers the user's current selection.

```powershell
Add-UICanvasDropdown -Name region -Choices 'US','EU','APAC'
Add-UICanvasDropdown -Name dc -DependsOn region -OptionsScript {
    switch (Get-UICanvasValue -Name region) {
        'US' { 'us-east-1','us-west-2' }  'EU' { 'eu-central-1' }  'APAC' { 'ap-southeast-1' }
    }
}
```

### Per-Monitor V2 DPI Awareness

The engine now declares **Per-Monitor V2** DPI awareness. Previously it was only System-DPI-aware: crisp at the
primary monitor's scaling, but Windows bitmap-stretched it (blurry text and controls) as soon as a window moved
to a monitor using a different scale factor, with no way to recover short of restarting.

Windows now re-renders the window at the target monitor's DPI, so it stays sharp across mixed-DPI multi-monitor
setups, adapts live when display scaling changes, and correctly scales the non-client area (title bar/borders)
and child dialogs.

Sizes in Canvas were always logical units (1/96") rather than physical pixels — see
[Sizing, DPI & multiple monitors](../canvas/about.md#sizing-dpi-multiple-monitors) for the practical
consequence: higher scaling leaves *less* logical space, so a 1080p laptop at 150% gives you only 1280×720 to
lay out in. Prefer proportional layout and set `-MinWidth`/`-MinHeight`.

### Fixed

- **A workflow using `-SkipWhen` could deadlock the whole app.** The skip condition was evaluated synchronously
  on the UI thread, so a condition that called a bridge cmdlet (e.g. `-SkipWhen '-not [bool](Get-UICanvasValue
  -Name x)'`) tried to marshal back to the very thread that was blocked waiting for it. The run froze with the
  previous step stuck on "RUNNING" and the window unresponsive. Conditions are now evaluated off the UI thread.
- **Dynamic item lists rendered as `System.Data.DataRowView`.** Setting a control's `ItemsSource` at runtime to a
  list of strings wrapped each item in a DataView, because a string exposes a `Length` property and was treated
  as a single-column row. Scalars (strings, numbers, booleans) now pass through as plain items.
- **`Get-UICanvasValue` returned `$null` for controls on a previous page.** Values entered on an earlier page are
  snapshotted when navigating away, but the lookup only searched the currently-rendered controls. It now falls
  back to that snapshot, so cross-page reads work from any action.
- Per-step progress bars and the overall gauge no longer attempt a two-way binding against read-only sources.

### Documentation

- **[About Canvas](../canvas/about.md)** — what Canvas is, how it works (authoring → engine → bridge), the four
  execution substrates, and the raw-XAML escape hatch
- **[Agent Authoring Guide](../agent/README.md)** — a self-contained context pack for building Canvas apps with
  an AI assistant, including the rules that prevent the common silent failures
- **[Capability Reference](../Canvas-Reference.md)** — terse index of every Canvas capability
- New runnable examples: engine workflow (live progress + retry), dashboard with an off-gate ScriptCard, and
  cascading dropdowns

## [1.3.0] - 2026-03-23

### Dual-Mode Custom Themes & Theme Toggle

PoshUI now supports **independent color palettes for light and dark modes** via `Set-UITheme -Light $hash -Dark $hash`. Users can toggle between themes at runtime using the **sun/moon button** in the title bar, and custom colors persist across toggles.

**22 overridable color slots:** `Background`, `ContentBackground`, `CardBackground`, `SidebarBackground`, `SidebarText`, `SidebarHighlight`, `TextPrimary`, `TextSecondary`, `AccentColor`, `ButtonBackground`, `ButtonForeground`, `InputBackground`, `InputBorder`, `BorderColor`, `TitleBarBackground`, `TitleBarText`, `SuccessColor`, `WarningColor`, `ErrorColor`, `HeadingForeground`, `BodyForeground`, `SecondaryForeground`

```powershell
Set-UITheme -Light @{
    Background       = '#FFF0F5'
    AccentColor      = '#E91E63'
    SidebarBackground = '#880E4F'
} -Dark @{
    Background       = '#1A1A2E'
    AccentColor      = '#00BFA5'
    SidebarBackground = '#0A1A18'
}
```

### PNG Icon Support

All modules now support colored PNG/ICO images as icons, replacing or supplementing monochrome Segoe MDL2 glyphs.

- `Add-UIStep -IconPath` - Sidebar step icons (Wizard, Dashboard, Workflow)
- `Add-UIMetricCard -IconPath` - Metric card icons (Dashboard)
- `Add-UICard -IconPath` - Info card icons (Dashboard)
- `Add-UIBanner -IconPath` - Banner overlay icons
- `Set-UIBranding -SidebarHeaderIcon` - Full-color sidebar header logos
- `Set-UIBranding -WindowTitleIcon` - Window title bar icon

PNG icons fall back to glyph icons automatically if the file path is invalid.

### New Examples

- `Test-CustomTheme-Dashboard.ps1` - Dual-mode themes with PNG icons
- `Test-CustomTheme-Workflow.ps1` - Dual-mode themes with PNG icons
- `Wizard-EmojiIcons.ps1` - PNG emoji icons in sidebar steps

### Bug Fixes

- Theme toggle now re-applies custom color palettes when switching modes
- `ConvertTo-UIScript` correctly maps `IconPath`, `Icon`, and other properties from `Add-UICard`
- `JsonDefinitionLoader` extracts `IconPath` from card Properties dictionary

---

## [1.0.0] - 2026-01-15

### 🎉 Initial Public Release

PoshUI v1.0.0 is the first public release of a PowerShell UI framework for building professional Windows 11-style wizards, dashboards, and workflows.

### 📦 Three Independent Modules

- **PoshUI.Wizard**: Step-by-step data collection with 12+ built-in controls
- **PoshUI.Dashboard**: Real-time monitoring with metric cards, charts, and tables
- **PoshUI.Workflow**: Multi-task automation with reboot/resume capability

### 🎯 Core Features

**Dashboard Cards:**
- `Add-UIMetricCard` - KPI metrics with trends and targets
- `Add-UIChartCard` - Data visualization (Line, Bar, Area, Pie)
- `Add-UITableCard` - Tabular data display
- `Add-UICard` - Informational content

**Developer Tools:**
- `Get-PoshUIDashboard` - Inspect and debug dashboard structure
- BannerStyle presets - Simplified banner creation
- Verbose output - Detailed ScriptBlock execution feedback
- Enhanced error messages - Context-aware suggestions

**Workflow Capabilities:**
- Reboot & Resume - Workflows save state and continue after restart
- Auto-Progress - Progress bar updates from script output
- Workflow Context - Access `$PoshUIWorkflow` in tasks

### 🏗️ Architecture

- **JSON Serialization**: Dashboard & Workflow modules use JSON for PowerShell-to-C# communication
- **AST Parsing**: Wizard module uses AST parsing for compatibility
- **Shared Engine**: All modules use the same WPF-based `PoshUI.exe` engine
- **Zero Dependencies**: No third-party libraries or NuGet packages

### 🧪 Testing

- 152 automated tests (58 PowerShell + 94 C#)
- Comprehensive test runners in `Tests/` folder
- CI/CD ready with JUnit XML output

### 📚 Documentation

- Complete cmdlet reference
- Dashboard cards reference
- Troubleshooting guide
- Working examples for all modules
- Get started guide

### 🎨 UI Features

- Light/Dark theme support with Auto detection
- Windows 11-style modern interface
- Live execution console
- Real-time data refresh
- Category filtering for cards

### 💻 Platform

- Windows PowerShell 5.1
- .NET Framework 4.8
- Windows 10/11 and Server 2016+
- Single executable distribution
