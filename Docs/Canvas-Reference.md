# PoshUI Canvas — Capability & Control Reference (.NET 4.8 WPF)

> Module `PoshUI.Canvas` **1.2.0** · requires engine `PoshUI.exe` **≥ 1.4.0** (checked at import).

Free-form, responsive, themeable UI apps authored in PowerShell and rendered by the v1 WPF engine
(`PoshUI.exe`). The WPF engine **software-renders**, so it does not require GPU compositing and runs on
virtual machines, remote sessions, and locked-down hardware. Module: `PoshUI\PoshUI.Canvas`.

```powershell
Import-Module .\PoshUI\PoshUI.Canvas\PoshUI.Canvas.psd1
New-PoshUICanvas -Title 'Demo' -Theme Dark -Navigation None -Width 1100 -Height 720
Add-UICanvasPage -Title 'Home' -Layout Dock
# ... add controls ...
$result = Show-PoshUICanvas        # launches PoshUI.exe; returns collected values on close
```

## App & pages
| Cmdlet | Key params |
|---|---|
| `New-PoshUICanvas` | `-Title -Theme Light/Dark/Auto -Navigation None/Sidebar/Compact/Top -Width -Height -MinWidth -MinHeight` |
| `Add-UICanvasPage` | `-Title -Icon -Layout Canvas/Dock/Grid/VStack/HStack/Wrap -Columns -ColumnWidths -Spacing -Padding` |
| `Set-UITheme` | `@{ slot = '#hex' }` (+ `-Light/-Dark` per-mode), see Theming |
| `Show-PoshUICanvas` | `-NoWait` (returns process) — otherwise returns the result object |

### Layout roots (responsive)
A page's `-Layout` makes it reflow instead of using absolute X/Y:
- **Dock** — children pin to edges via `-Properties @{ Dock='Left/Right/Top/Bottom' }`; the last child fills. (A nav rail docks left; content fills + scrolls.)
- **Grid** — `-Columns N` or `-ColumnWidths 'Auto,*,2*,120'`; place children with `@{ Row=; Column=; ColumnSpan=; RowSpan= }`.
- **VStack / HStack / Wrap** — stack/flow with `-Spacing`.
- **Canvas** (default) — absolute X/Y; the window auto-fits the content.

Containers (`Add-UICanvasCard`, `Add-UICanvasPanel`) take the same `-Layout/-Columns/-ColumnWidths/-Spacing/-Padding`, plus `-Properties @{ HAlign=; VAlign=; Margin='l,t,r,b'; HoverBackground='#hex' }`.

## Controls
**Input:** `TextBox, MultiLine, Password, Number, NumberBox, Dropdown, ListBox, RadioGroup, Checkbox, Toggle, Slider, DatePicker, TimePicker, Rating, ColorPicker, RichEdit`
**Display:** `Label, Icon, Badge, Banner, ProgressBar, ProgressRing, Console, Image, Hyperlink, Separator`
**Buttons:** `Button (-Style Accent/Secondary/Subtle/Gradient, -Icon, -NavigateTo), DropDownButton`
**Containers:** `Card, Panel, Expander, Tabs/Tab, Viewbox` (+ `GridSplitter` for resizable panes)
**Chrome:** `Toolbar, Footer, Menu` (nested menu bar — see Layout / chrome controls below)
**Data:** `DataGrid (-Columns -Items/-BindItems -OnChange), TreeView, Repeater`
**Dashboard cards:** `MetricCard (-Caption -Value -Trend Up/Down/Stable -Delta), ChartCard (-Labels -Values), StatusCard (-Items 'Label|State'), TableCard (-Columns -Rows), ScriptCard (off-gate runner)`

Common params: `-Name` (addressable at runtime), `-Value` (initial), `-X/-Y/-Width/-Height`, `-Tooltip`, `-Visible/-Enabled`, `-Refresh <sec>` (re-run a `{ }` value script live), `-Action { }` / `-OnChange { }`.

## Images & icons
Anywhere an `-Icon` is accepted it takes **either a Segoe Fluent glyph name or an image path** (`.png/.jpg/.ico/...` or `pack://`), auto-detected:
```powershell
Add-UICanvasButton 'Go' -Icon 'rocket'            # glyph
Add-UICanvasButton 'Go' -Icon 'C:\logo.png'       # PNG
Add-UICanvasImage  -Source 'C:\logo.png' -Width 44 -Height 44
Add-UICanvasBadge  'ONLINE' -Severity Success -Icon 'C:\dot.png'
Add-UICanvasBanner 'Ready' -Value '...' -Icon 'check' -Image 'C:\hero.png'
Add-UICanvasDropdown -Choices $a -ItemIcons $icons     # per-item icons (value stays the string)
Add-UICanvasCard -Properties @{ BackgroundImage='C:\bg.png'; BackgroundStretch='UniformToFill'; BackgroundOpacity=0.3 }
```
Any card/panel/row composes images directly (drop an `Add-UICanvasImage` inside `-Children`).

## Runtime (inside `-Action`/`-OnChange`/`-Refresh` scripts)
Run **in-app** via injected cmdlets (the engine hosts a STA runspace):
- `Set-UICanvasValue -Name x -Value v` / `Get-UICanvasValue -Name x`
- `Set-UICanvasProperty -Name x -Property <p> -Value v` — `Visible, Enabled, Value, Label/Text, AppendLine, Clear, Background, Foreground, Source, BackgroundImage, Spin, X/Y/Width/Height/Opacity`
- `Show-UICanvasPage 'Title'` (or index / `Next`/`Prev`) — navigation; **values persist across pages**
- `Submit-UICanvas` — collect values → `result.json` → close window

The triggering button is **disabled while its action runs** (busy state). On window close the runtime
**disposes** (timers stopped, runspace released).

## Flow modes (Wizard / Workflow)
Canvas can operate as a **wizard** (validated paging) or a **workflow** (sequential execution engine) — built
on the primitives above, multi-page, with value persistence.

**Workflow** — declare steps, get the runner (spinner → ✓, red-halt on failure, progress + log):
```powershell
Add-UICanvasWorkflowStep 'Apply Image' -Detail 'Extracting WIM.' -Script { ... } `
    [-ExpectedSeconds 60] [-Retry 2] [-TimeoutSeconds 300] [-SkipWhen '<condition>']   # repeat per step
Add-UICanvasWorkflow -Engine -Name deploy -StartLabel 'Start' -ShowLog `
    [-AutoStart] [-LockNavigation] [-StateFile 'C:\ProgramData\MyApp\state.json']
```
- `-Engine` — **preferred**: steps run on the engine's workflow executor in its own runspace (off the app's
  serialized gate), so progress/elapsed stay live during a long step; enables per-step `-Retry`,
  `-TimeoutSeconds`, `-SkipWhen`. Step scripts get `$wf`: `UpdateProgress(pct,msg)`, `WriteOutput(txt,lvl)`,
  `GetValue/SetValue(controlName)`, `SetData/GetData(key)`, `SkipTask(reason)`, `RequestReboot(reason)`.
- `-ShowLog` — adds a live log console the runner writes to.
- `-AutoStart` — runs once automatically (guarded off live status, so no double-fire).
- `-LockNavigation` — after the first step completes, **block backtracking** to earlier pages.
- `-LockOnStart` — block backtracking immediately when the run **begins** (before step 1).
- `-StateFile <path>` — **reboot-resume**: writes `{name,completed}` after each step to a *non-volatile* path
  (one that survives the restart); on relaunch it pre-marks done steps and resumes; clears the file on completion.

**ScriptCard** — on-demand dashboard runner (runs off the UI gate; output streams into its console live;
status flips Idle → Running → Done/Failed; long tasks never freeze live cards):
```powershell
Add-UICanvasScriptCard 'Collect logs' -Detail 'Zips the last 24h' -Script { "working..."; "done" } `
    [-Name <id>] [-RunLabel 'Run'] [-OutputHeight 160]
```

**Cascading options** — a Dropdown/ListBox whose items recompute when other fields change:
```powershell
Add-UICanvasDropdown -Name dc -DependsOn region -OptionsScript {
    if ((Get-UICanvasValue -Name region) -eq 'US') { 'us-east-1','us-west-2' } else { 'eu-central-1' }
}
```

**Wizard** — validated Back/Next paging:
```powershell
Add-UICanvasWizardSteps -Steps 'Account','Network','Confirm' -Current 'Account'    # step-chip indicator
Add-UICanvasWizardNav -Next 'Network' -Back $null -Require 'ComputerName','AdminPass' -Validate {
    if (([string](Get-UICanvasValue -Name AdminPass)).Length -lt 12) { 'Password must be at least 12 characters.' }
}
```
- `-Require` — control names that must be non-empty before Next advances (else inline error, no advance).
- `-Validate { }` — output a non-empty string to block with that message.
- Omit `-Next` → the button becomes **Finish** (submits). `-Back` adds a (validation-free) Back button.
- `-NoBack` — once Next succeeds, **lock navigation** (one-way wizard): you can't return to this/earlier pages,
  and the Back button greys out. (The lock persists across pages — rebuilt `nav_*` buttons stay disabled.)

**Navigation lock** (used by `-LockNavigation`, also callable directly from an Action):
`Lock-UICanvasNavigation` / `Unlock-UICanvasNavigation` — no-back guard; also disables rail buttons named `nav_*`.

## Theming
`Set-UITheme` slots drive the engine theme (canvas cards follow them; defaults fall back to the dark palette):
`AccentColor, Background, ContentBackground, CardBackground, BorderColor, TextPrimary, TextSecondary,
SuccessColor, WarningColor, ErrorColor, SidebarBackground, SidebarText, FontFamily, CornerRadius`.

## Result & secrets
`Show-PoshUICanvas` returns an object of `Name → value` for every visited page (merged). **Password**
controls are **DPAPI-protected** (CurrentUser) on disk and returned to the caller as a **`SecureString`**;
the temp `result.json` is deleted after read.

## Security / trust model
- **Definition load is gated** — the engine validates the `.json` path before loading: local paths only (no UNC /
  network shares), no `..` traversal, and a `.ps1`/`.json` extension (`SecurityValidator.ValidateScriptPath`).
- **Trust boundary = whoever runs the script.** Canvas `-Action`/`-OnChange`/`Start-UICanvasAsync` blocks and
  `Add-UICanvasXaml` markup run **in-process with full PowerShell/WPF privileges** — the same trust as the `.ps1`
  itself. So: do **not** build actions or XAML markup from untrusted runtime input. `XamlReader.Parse` can
  instantiate arbitrary types; only feed it markup you authored.
- **Secrets never hit the log.** `[diag]` and action logs record control *names*/properties, never values.
  (Caveat: an action that *throws with a secret in the message* will log that message — don't echo secrets in errors.)
- **Hardening:** the JSON parser for `DataGrid`/`TreeView` data is depth-limited (no stack-overflow from runaway
  nesting); a bad parse degrades to empty rather than crashing.

## Accessibility
Every control exposes an automation **Name** (from its label/name) and **HelpText** (from its tooltip) for screen
readers / UI automation; standard inputs are keyboard-focusable and tab in document order.

## Sizing / DPI
All sizes (`-Width`, `-Height`, `-FontSize`, `-X`/`-Y`, `-StepsHeight`, …) are **logical units (1/96")**, not
physical pixels — the engine scales them, so apps render crisply at 125/150/200%. The engine declares
**Per-Monitor V2** DPI awareness: windows re-render at the target monitor's DPI instead of bitmap-stretching
when moved between monitors with different scaling, and adapt live to scaling changes.
- Higher scaling = **less** logical space (1920×1080 @150% → **1280×720**). Design against the smallest logical
  size you target (~1280×700 if laptops are in scope), not physical resolution.
- Prefer proportional layout (`-ColumnWidths '3*,*'`, `Dock`, `Stretch`) over fixed heights; scroll the one
  growable region (`-StepsHeight`, `-Properties @{ Scroll = 'Vertical' }`).
- Set `New-PoshUICanvas -MinWidth -MinHeight` so the window can't be resized into a broken layout.

## Portable / offline deployment
The engine software-renders, so no DWM/GPU compositing is required. Notes for constrained environments:
- The module resolves the engine at `<module-parent>\bin\PoshUI.exe`, so the module + `bin\` can be copied
  anywhere and run in place.
- Temp/result files use `%TEMP%` — point it at a writable location if the default is read-only.
- Local-file & `pack://` images render offline; `http(s)` sources need the network up.

## Pickers
- `Add-UICanvasFolderPicker -Name -Placeholder -Description` — textbox + Browse (shell folder dialog).
- `Add-UICanvasFilePicker -Name -Title -Filter` — textbox + Browse (open-file dialog).
  Read the chosen path from an action with `Get-UICanvasValue -Name <name>`. (Shell dialogs may be limited in
  minimal or stripped-down Windows environments.)

## Flow cmdlet options (additions)
- `Add-UICanvasWorkflow`: `-NoStartButton` (publish the run action to `$Global:_PoshUICanvas.WorkflowActions[Name]` and place the button yourself), `-NoHeader` (omit the inline Status/%/gauge — render `<name>_status`/`<name>_pct`/`<name>_gauge` where you want), `-StepsHeight <px>` (scrollable step list), `-AutoStart` (begin ~2s after the page is shown), `-LockOnStart`, `-StateFile` (reboot-resume). The runner also writes `<name>_elapsed`/`<name>_remaining` each step for live timers.
- `Add-UICanvasWizardNav`: `-NoBack`, `-Require`, `-Validate {}`.

## Repeater (data-driven controls)
`Add-UICanvasRepeater -Items $collection -Template { param($item) ... }` — invokes the template once per item,
emitting its controls into the current container. One-liner for lists of cards/rows.

## Native data controls (1.1)
- `Add-UICanvasDataGrid -Name -Columns Host,Role,Cpu [-Items $rows]` — sortable grid (click headers). Seed with
  `-Items` (array of objects) or populate/refresh live with `Set-UICanvasProperty -Name g -Property ItemsSource -Value $rows`
  (PowerShell objects auto-coerce to a `DataView`). `Get-UICanvasValue` returns the selected row.
- `Add-UICanvasTreeView -Name -Nodes @(@{ Text='Sites'; Expanded=$true; Children=@(@{Text='HQ'}) })` — nested tree;
  `Get-UICanvasValue` returns the selected node's text.
- `Add-UICanvasAutoSuggest -Name -Choices @(...) [-Value]` — editable, type-to-filter combo; value is the current text.
- `Add-UICanvasMarkdown -Name (-Text '# md' | -Path file.md)` — lightweight viewer (headings, `**bold**`, `*italic*`,
  `` `code` ``, `- bullets`).

## Layout / chrome controls (1.4)
```powershell
# Tabbed container. Value = selected index; Set-UICanvasValue also accepts a header string.
Add-UICanvasTabs -Name t [-SelectedIndex 0] [-OnChange { }] -Children {
    Add-UICanvasTab 'Summary' [-Layout VStack] [-Spacing] [-Padding] [-Disabled] -Children { ... }
    Add-UICanvasTab 'Details' -Children { ... }
}

# Menu bar. Nested -Items; a leaf's Action fires on click; Text '-' is a separator.
Add-UICanvasMenu -Items @(
    @{ Text = 'File'; Items = @(
         @{ Text = 'Open'; Gesture = 'Ctrl+O'; Action = { ... } }
         @{ Text = '-' }
         @{ Text = 'Exit'; Action = { Submit-UICanvas } } ) }
)   # per-item: Icon, Gesture, Disabled, Checked, Name, Page (navigate instead of Action)

# Resizable panes: put the splitter in its own Grid cell BETWEEN the two panes.
# Default resizes columns; -Orientation Horizontal resizes rows.
Add-UICanvasGridSplitter [-Orientation Vertical|Horizontal] [-Thickness 5] -Properties @{ Column = 1 }

# Scale content to fit (vector scaling - text stays crisp).
Add-UICanvasViewbox [-Stretch Uniform|Fill|UniformToFill|None] [-StretchDirection Both|UpOnly|DownOnly] -Children { ... }
```

## Raw XAML escape hatch
`Add-UICanvasXaml -Markup '<DataGrid x:Name="grid" .../>'` (or `-Path file.xaml`) splices arbitrary **WPF** markup
in — for controls the cmdlet set doesn't cover (DataGrid, TreeView, TabControl, custom templates). Every `x:Name`'d
element (including the root) is registered with the bridge, so `Set/Get-UICanvasValue` and `Set-UICanvasProperty`
work on it. Setting `ItemsSource` to a PowerShell object collection auto-coerces to a `DataView` so `{Binding Col}`
binds; selection reads back as a `DataRowView`. Namespaces are auto-injected if omitted. Notes: WPF dialect (not
portable to the WinUI 3 build); buttons *inside* the markup don't run script — wire interactions from normal canvas
controls; the markup uses explicit colors or `{DynamicResource <key>}` (it doesn't auto-inherit the theme).

## Motion
`Set-UICanvasAnimate -Name -Property -To [-From] [-Duration 250] [-Easing CubicOut]` — animates `Opacity`/`Width`/
`Height`/`X`/`Y`. Easings: `Cubic`/`Quad`/`Sine`/`Back`/`Bounce`/`Elastic` × `In`/`Out`/`InOut`, or `Linear`.
Implicit: progress bars **tween** to new values; pages **fade in** when shown.
- **Stagger:** a container `-Properties @{ Stagger = 70 }` fades + slides its children in with a 70 ms per-item delay.
- **Glow (any control):** `-Properties @{ Glow='#34D399'; GlowRadius=16; GlowPulse=$true }` (or `Glow=$true` to use the
  control's own colour). `Add-UICanvasProgressBar` also takes `-Fill/-Glow/-GlowPulse/-GlowRadius`.

## Reactive state
A shared, reactive store: change one key and every bound control updates itself (no per-control `Set-UICanvasValue`).
```powershell
New-UICanvasState @{ count = 0; name = 'World' }          # call once, after Add-UICanvasPage
Add-UICanvasLabel  -Bind 'Hello {name}, count is {count}'  # computed template (one-way; re-renders on change)
Add-UICanvasTextBox -Bind 'name'                            # two-way: typing updates state.name
Add-UICanvasLabel  -Bind 'count'                            # one-way display of a key
Add-UICanvasButton '+1' -Action { Set-UICanvasState count ((Get-UICanvasState count) + 1) }
Watch-UICanvasState -Key 'count' -Action { if ((Get-UICanvasState count) -ge 5) { Show-UICanvasToast 'High!' } }
```
- `-Bind 'key'` = bind a control to a state key (two-way on inputs: TextBox/ComboBox/Checkbox/Slider/Toggle/ListBox/DatePicker).
- `-Bind '...{key}...'` = a one-way computed template (re-renders when any referenced key changes).
- `Set-/Get-UICanvasState`, `Watch-UICanvasState` — mutate / read / react. The store is shared across pages.
- The imperative API (`Set-UICanvasValue` etc.) still works; reactivity is additive.

## Collection binding (reactive lists)
Bind a DataGrid's rows to a state **list** — mutate the list and the grid auto-adds/removes rows.
```powershell
New-UICanvasState @{ rows = @( @{ Host='web-01'; Role='Web' } ) }
Add-UICanvasDataGrid -Name grid -BindItems 'rows'        # ItemsSource follows state.rows
# ...in any action:
$r = @(Get-UICanvasState rows); $r += @{ Host='node-02'; Role='Worker' }
Set-UICanvasState rows $r                                 # grid rebuilds itself
```
- `-BindItems 'key'` (DataGrid) — `ItemsSource` tracks a state list; rows are coerced from `[pscustomobject]`/hashtable/dictionary rows. Columns auto-generate from the data.
- Scalar `-Bind` (above) and collection `-BindItems` compose freely; pair with a `Watch-UICanvasState` for side-effects.

## Charts
`Add-UICanvasChartCard -Labels @(...) -Values @(...)` renders an interactive chart:
- `-Type Bar` (default) **| Line | Area | Donut | Sparkline**. Bars grow in (animated); line/area draw on left-to-right; `-Spline` curves them; donut shows a centred total + value/percent legend; sparkline is a compact axis-less trend.
- **Multi-series:** `-Datasets @(@{ Name='req/s'; Color='#38BDF8'; Values=@(...) }, @{ Name='errors'; ... })` → grouped bars or overlaid lines with a multi-entry legend.
- **Reactive:** `-Bind 'key'` makes the chart redraw from the state store — push a number array (replaces the series) or a datasets array (replaces all series) via `Set-UICanvasState`. Combine with a `-Refresh` ticker for a live dashboard.
- **Gridlines + a y-axis** scaled to a "nice" maximum; **tooltips**, bar **value labels**, gradient fills, **hover glow**; empty data shows a "No data" placeholder.
- `-Series 'name'` legend label (single series); `-Foreground` series color; `-ChartHeight` plot height.

## Theme presets
`Set-UITheme` takes one-call presets in addition to slot overrides:
- `-Preset Dark | Light | Midnight | Slate` — a complete curated palette (also sets the base mode).
- `-Accent Indigo | Emerald | Sky | Amber | Rose | Violet | Cyan | Orange` — recolors just the accent.
- Explicit `-Theme @{ ... }` slots still win, e.g. `Set-UITheme -Preset Slate -Accent Sky -Theme @{ CardBackground = '#10192E' }`.

## Empty / themed-chrome niceties
- **Tooltips** are themed app-wide (dark card, not the white OS popup).
- **DataGrid** shows a centred "No data" placeholder when it has no rows (incl. reactive `-BindItems`).

## Markdown & hyperlinks
`Add-UICanvasMarkdown -Text <md>` (or `-Path file.md`) renders to a native WPF FlowDocument (no browser needed):
- Headings (`#`/`##`/`###`), **bold**, *italic*, `inline code`, fenced ` ``` ` code blocks, `> blockquotes`, `---` rules.
- Unordered (`-`/`*`) and ordered (`1.`) lists.
- **Links** `[text](url)` and **images** `![alt](path)` — local image paths load inline (alt text is the fallback).
- `Add-UICanvasHyperlink -NavigateUri <url>` is a standalone link control.
- **Security:** links open via the shell only for safe schemes (`http`/`https`/`mailto`/`file`); anything else is blocked and logged, so a crafted link can't launch an executable or custom protocol.

## Keyboard shortcuts
`Add-UICanvasShortcut -Key 'Ctrl+S' -Action { … }` (gestures like `F5`, `Ctrl+Shift+D`) — invisible; fires its action
on the gesture anywhere in the window.

## Async (non-blocking)
`Start-UICanvasAsync { ... }` runs the scriptblock on its **own runspace + thread** so long work doesn't freeze the
UI; the runtime cmdlets (`Set-UICanvasValue`, `Set-UICanvasProperty`, `Show-UICanvasToast`, …) still update the UI
from inside it. Use it for any action that takes more than a moment.

## Toasts & dialogs
- `Show-UICanvasToast -Message [-Severity info|success|warning|error] [-Duration 3000]` — transient notification.
- `Show-UICanvasDialog -Message [-Title] [-Prompt -DefaultValue] [-OkLabel] [-CancelLabel]` — modal; returns the
  entered text (`-Prompt` + OK), `$true` (confirm + OK), or `$null` (Cancel).

## Diagnostics
Would-be-silent failures (unknown control name, unsettable property, async errors) are logged to the engine log as
`[diag] …`. Set `POSHUI_CANVAS_DIAG=1` to also **toast** them while developing. The smoke harness
(`PoshUI\Examples\Smoke-Test.ps1`) launches each demo headless and fails on any `[diag]`/exception (exit code for CI).
