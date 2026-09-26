# 02 — Controls Reference

Every `Add-UICanvas*` control with its real parameters, an example, and notes. All controls also accept the
**universal parameters** (`-Name -X -Y -Width -Height -ZIndex -Tooltip -Visible -Enabled -Refresh -Properties`)
unless noted. Parameters shown are the control-specific ones.

Conventions:
- `-Label`/`-Value` accept a **string** *or* a **scriptblock** (with `-Refresh N`, the scriptblock re-runs every
  N seconds for live values).
- `-Bind 'key'` ties the control to a reactive state key (see [03](03-runtime-and-interactivity.md)).
- `-OnChange { }` fires when the user changes an input's value.

---

## Text & display

### Add-UICanvasLabel
```
Add-UICanvasLabel [-Label] <string|scriptblock> [-Name <s>] [-Bind <s>]
  [-FontSize <d>] [-FontWeight <s>] [-Foreground <hex>]
```
Static or live text. `-FontWeight` = `Normal|SemiBold|Bold`. With `-Bind`, the label can be a key (`-Bind 'count'`)
or a template (`-Bind 'Hello {name}'`) that re-renders on change.
```powershell
Add-UICanvasLabel 'Status: ready' -FontSize 14 -Foreground '#94A3B8'
Add-UICanvasLabel -Name clock -Refresh 1 -Label { Get-Date -Format 'HH:mm:ss' }
```

**Elapsed clock (`-Clock` / `-ClockStop`).** `-Clock <controlName>` names a control that holds a start time as
**UTC ticks**; the label then shows elapsed `mm:ss`, updated twice a second by the engine itself — no runspace
call, so it keeps ticking while a long action or step holds the gate (a `-Refresh` label would freeze).
`-ClockStop <controlName>` names a control holding the end ticks; once it is set, the clock freezes at the
final time. The workflow runner publishes `<wf>_start` / `<wf>_end` for exactly this; for your own timer, keep
the ticks in hidden controls:
```powershell
Add-UICanvasTextBox -Name jobStart -Visible $false
Add-UICanvasTextBox -Name jobEnd -Visible $false
Add-UICanvasLabel -Name elapsed '00:00' -Clock jobStart -ClockStop jobEnd -FontSize 20
Add-UICanvasButton 'Start' -Action { Set-UICanvasValue jobStart ([DateTime]::UtcNow.Ticks) }
Add-UICanvasButton 'Stop'  -Action { Set-UICanvasValue jobEnd ([DateTime]::UtcNow.Ticks) }
```

### Add-UICanvasIcon
```
Add-UICanvasIcon [-Icon] <string> [-Name <s>] [-FontSize <d>] [-Foreground <hex>]
```
An icon by glyph name, MDL2 char, or PNG path.

### Add-UICanvasBadge
```
Add-UICanvasBadge [-Label] <string> [-Value <o>] [-Severity Neutral|Info|Success|Warning|Error] [-Icon <s>]
```
A small status pill. `-Icon` may be a PNG path or glyph.

### Add-UICanvasBanner
```
Add-UICanvasBanner [-Label] <string> [-Value <o>] [-Severity Informational|Success|Warning|Error]
  [-Icon <s>] [-Image <s>]
```
A prominent header strip. `-Icon` = leading status icon (PNG/glyph), `-Image` = a hero illustration on the right.

### Add-UICanvasHyperlink
```
Add-UICanvasHyperlink [-Label] <string> [-NavigateUri <url>]
```
Opens the URL in the default browser. **Security:** only `http`/`https`/`mailto`/`file` schemes open; anything
else is blocked and logged.

### Add-UICanvasMarkdown
```
Add-UICanvasMarkdown [-Text <string>] [-Path <file>]
```
Renders Markdown to a native WPF FlowDocument (no browser needed). Supports headings (`#`/`##`/`###`),
`**bold**`, `*italic*`, `` `code` ``, fenced ```` ``` ```` code blocks, `> blockquotes`, `---` rules, unordered
(`-`/`*`) and ordered (`1.`) lists, **links** `[text](url)` (scheme-gated), and **images** `![alt](path)`
(local paths render inline; alt text is the fallback).

### Add-UICanvasSeparator
```
Add-UICanvasSeparator [-Name <s>] [-X <d>] [-Y <d>] [-Width <d>] [-ZIndex <int>]
```
A horizontal rule.

---

## Buttons

### Add-UICanvasButton
```
Add-UICanvasButton [-Label] <string> [-Name <s>] [-Action { }] [-NavigateTo <page>]
  [-Style Standard|Secondary|Accent|Primary|Subtle|Gradient] [-Icon <s>]
```
- `-Action { }` runs (on the bridge runspace) when clicked.
- `-NavigateTo` goes to a page with no action code: a page **title**, a numeric **index**, or a relative keyword
  `Next`, `Prev`/`Previous`/`Back`, `First`, `Last`. It is ignored when `-Action` is also given — to navigate
  after doing work, call `Show-UICanvasPage` at the end of the action instead.
- `-Style`: `Accent`/`Primary` = filled brand; `Secondary` = outlined; `Subtle` = quiet/ghost; `Gradient` = brand
  gradient fill; `Standard` = default. Call `Set-UITheme` first — on the unthemed dark palette `Accent` is unreadable.
- `-Icon` = a friendly name (`play`, `back`, `next`, `refresh`, `shield`, `server`, …), a hex code point
  (`E768`, `0xE768`), a glyph character, or a PNG path. `-Properties @{ ContentAlign = 'Left' }` left-aligns content.
```powershell
Add-UICanvasButton 'Deploy' -Name go -Style Accent -Icon 'play' -Action {
    Set-UICanvasProperty go 'Enabled' $false
    Show-UICanvasToast -Message 'Deploying…' -Severity info
}
```

### Add-UICanvasDropDownButton
```
Add-UICanvasDropDownButton [-Label] <string> [-Choices <array>] [-Value <o>] [-OnChange { }]
```
A button that opens a built-in menu of `-Choices`; the picked item becomes its value. (For an anchored popup
menu driven by an action, see `Show-UICanvasFlyout` in [03](03-runtime-and-interactivity.md).)

---

## App chrome & structure

The pieces that turn a page into an application: a header, a footer, a menu bar, tabs, resizable panes and
content that scales to fit. For a fully custom look (slim top bar, animated icon rail) see
[10](10-visual-design-and-shell.md); these are the built-in versions.

### Add-UICanvasToolbar
```
Add-UICanvasToolbar [-Name <s>] [-Brand <s>] [-BrandIcon <s>] [-Status <s>]
  [-Links <string[]>] [-Active <pageTitle>] [-Actions <hashtable[]>]
```
An application header: brand and a status pill on the left, navigation links in the centre, action icons on the
right.
- `-Links` — page titles; each link navigates to the page of the same title. `-Active` highlights the current one.
- `-Actions` — `@{ Icon = '<glyph>'; Tooltip = '...'; Action = { ... } }`, or `Page = '<title>'` instead of
  `Action`, or `Image = '<png>'` (e.g. an avatar) instead of `Icon`. Add `Name = '...'` to address it later.
- Its height is fixed at about 54 px; for a slimmer bar build one in an island (file 10).
```powershell
Add-UICanvasToolbar -Brand 'Contoso Ops' -BrandIcon 'server' -Status 'Connected' `
    -Links 'Overview', 'Reports', 'Settings' -Active 'Overview' -Actions @(
        @{ Icon = 'refresh'; Tooltip = 'Refresh'; Action = { Show-UICanvasToast 'Refreshed' -Severity info } }
        @{ Icon = 'settings'; Tooltip = 'Settings'; Page = 'Settings' }
    )
```

### Add-UICanvasFooter
```
Add-UICanvasFooter [-Name <s>] [-LeftText <s>] [-Links <object[]>]
```
A status/footer bar: muted text on the left, links on the right. A link is a plain string (display only) or
`@{ Text = '...'; Action = { ... } }` / `@{ Text = '...'; Page = '<title>' }`.
```powershell
Add-UICanvasFooter -LeftText 'v1.2.0  |  Signed in as CONTOSO\admin' -Links @(
    @{ Text = 'Help'; Action = { Show-UICanvasDialog -Title 'Help' -Message 'Call 1234.' -OkLabel 'Close' } }
    @{ Text = 'Settings'; Page = 'Settings' }
)
```
On a Dock page, give it `-Properties @{ Dock = 'Bottom' }` and declare it before the fill child.

### Add-UICanvasMenu
```
Add-UICanvasMenu -Items <hashtable[]> [-Name <s>] [-Background <hex>]
```
A classic menu bar. Each item is `@{ Text; Items }` (a submenu) or a leaf `@{ Text; Action }` / `@{ Text; Page }`.
Leaves also take `Gesture` (the shortcut text shown — register the key itself with `Add-UICanvasShortcut`),
`Icon`, `Disabled = $true` and `Checked = $true|$false`. `Text = '-'` is a separator.
```powershell
Add-UICanvasMenu -Items @(
    @{ Text = 'File'; Items = @(
        @{ Text = 'Export...'; Gesture = 'Ctrl+E'; Action = { Show-UICanvasToast 'Exported' -Severity success } }
        @{ Text = '-' }
        @{ Text = 'Exit'; Action = { Submit-UICanvas } } ) }
    @{ Text = 'View'; Items = @(
        @{ Text = 'Reports'; Page = 'Reports' }
        @{ Text = 'Show hidden'; Checked = $false; Action = { } } ) }
)
Add-UICanvasShortcut 'Ctrl+E' -Action { Show-UICanvasToast 'Exported' -Severity success }
```

### Add-UICanvasTabs / Add-UICanvasTab
```
Add-UICanvasTabs [-Name <s>] [-SelectedIndex <int>] [-OnChange { }] -Children { Add-UICanvasTab ... }
Add-UICanvasTab [-Label] <header> [-Layout VStack|HStack|Grid|Wrap|Canvas] [-Columns <int>]
  [-ColumnWidths <s>] [-Spacing <d>] [-Padding <o>] [-Disabled] -Children { ... }
```
A tab container; its children must be `Add-UICanvasTab` blocks, each one tab page with its own layout.
Read or switch the active tab with `Get-UICanvasValue` / `Set-UICanvasValue` (an index or the header text);
`-OnChange` fires when the user switches tab.
```powershell
Add-UICanvasTabs -Name details -Children {
    Add-UICanvasTab 'Summary' -Spacing 8 -Children {
        Add-UICanvasLabel 'Everything at a glance.'
    }
    Add-UICanvasTab 'Hardware' -Layout Grid -Columns 2 -Spacing 8 -Children {
        Add-UICanvasMetricCard 'CPU' -Value '8 cores'
        Add-UICanvasMetricCard 'RAM' -Value '32 GB'
    }
    Add-UICanvasTab 'Audit' -Disabled -Children { Add-UICanvasLabel 'Coming soon' }
}
```
Tabs keep every page's controls alive, so a value typed on one tab is still readable from another.

> **Known issue (engine 1.4.1):** the tab strip is not themed yet — it renders in the stock light Windows style,
> which is hard to read in a dark app. Until it is, prefer a **segmented switch**: a row of buttons that show one
> panel and hide the others with `Set-UICanvasProperty <panel> Visible $true|$false`, or a Dock page with an icon
> rail (file 10).

### Add-UICanvasGridSplitter
```
Add-UICanvasGridSplitter [-Name <s>] [-Orientation Vertical|Horizontal] [-Thickness <d>] [-Background <hex>]
```
A drag handle that resizes the panes either side of it. Put it in its **own** column of a `Grid` panel, between
the two panes, and give that column a fixed width. `Vertical` (default) resizes columns; `Horizontal` resizes rows.
```powershell
Add-UICanvasPanel -Layout Grid -ColumnWidths '280,6,*' -Children {
    Add-UICanvasListBox -Name servers -Choices 'web-01', 'web-02', 'db-01' -Properties @{ Column = 0 }
    Add-UICanvasGridSplitter -Properties @{ Column = 1 }
    Add-UICanvasLabel 'Details for the selected server' -Properties @{ Column = 2; Margin = '12,0,0,0' }
}
```

### Add-UICanvasViewbox
```
Add-UICanvasViewbox [-Stretch Uniform|Fill|UniformToFill|None] [-StretchDirection Both|UpOnly|DownOnly]
  [-Layout VStack|HStack|Grid|Wrap|Canvas] [-Spacing <d>] -Children { ... }
```
Scales its content to fit the space it is given — vector scaling, so text stays crisp. Use it for a kiosk
number, a big status readout, or a fixed design that must fill any window. `DownOnly` shrinks to fit but never
enlarges. Give it an explicit `-Height` (or a Dock fill slot) so it has a size to scale to.
```powershell
Add-UICanvasViewbox -Height 160 -Children {
    Add-UICanvasLabel -Name bigCount '248' -FontSize 72 -FontWeight Bold
}
```

---

## Text input

### Add-UICanvasTextBox
```
Add-UICanvasTextBox [-Name <s>] [-Bind <s>] [-Value <o>] [-Placeholder <s>] [-OnChange { }]
```
Single-line text. Value = the text.

### Add-UICanvasMultiLine
```
Add-UICanvasMultiLine [-Name <s>] [-Bind <s>] [-Value <o>] [-Placeholder <s>] [-OnChange { }]
```
Multi-line text area.

### Add-UICanvasRichEdit
```
Add-UICanvasRichEdit [-Name <s>] [-Bind <s>] [-Value <o>] [-Placeholder <s>] [-OnChange { }]
```
A richer multi-line editor.

### Add-UICanvasPassword
```
Add-UICanvasPassword [-Name <s>] [-Placeholder <s>]
```
Masked input with a reveal (eye) toggle. **The value is DPAPI-protected** (CurrentUser) and returned as a
SecureString — never logged in plaintext. Read it via the result set, not `Get-UICanvasValue`.

### Add-UICanvasAutoSuggest
```
Add-UICanvasAutoSuggest [-Name <s>] [-Bind <s>] [-Choices <array>] [-Value <o>] [-OnChange { }]
```
Editable, type-to-filter combo with a dropdown chevron. Value = the current text.

### Add-UICanvasNumber / Add-UICanvasNumberBox
```
Add-UICanvasNumber   [-Name <s>] [-Bind <s>] [-Value <o>] [-Minimum <d>] [-Maximum <d>] [-OnChange { }]
Add-UICanvasNumberBox [-Name <s>] [-Bind <s>] [-Value <o>] [-Minimum <d>] [-Maximum <d>] [-Step <d>] [-OnChange { }]
```
Numeric inputs; `NumberBox` adds spinner `-Step`.

---

## Selection & boolean

### Add-UICanvasDropdown
```
Add-UICanvasDropdown [-Name <s>] [-Bind <s>] [-Choices <array>] [-Value <o>] [-ItemIcons <string[]>] [-OnChange { }]
  [-DependsOn <string[]>] [-OptionsScript { ... }]
```
Combo box. `-ItemIcons` = a parallel array of PNG paths/glyphs aligned to `-Choices`.

**Cascading options**: `-DependsOn` + `-OptionsScript` make the items recompute automatically whenever any of
the named fields changes — no `-OnChange` wiring. The script's output becomes the new item list. On a
cascading dropdown, omit `-Choices` and `-ItemIcons` (items come from the script).
```powershell
Add-UICanvasDropdown -Name region -Choices 'US','EU','APAC'
Add-UICanvasDropdown -Name dc -DependsOn region -OptionsScript {
    switch (Get-UICanvasValue -Name region) {
        'US' { 'us-east-1','us-west-2' }  'EU' { 'eu-central-1','eu-west-1' }  'APAC' { 'ap-southeast-1' }
    }
}
```

### Add-UICanvasListBox
```
Add-UICanvasListBox [-Name <s>] [-Bind <s>] [-Choices <array>] [-Value <o>] [-ItemIcons <string[]>] [-OnChange { }]
  [-DependsOn <string[]>] [-OptionsScript { ... }]
```
Single-select list. Supports `-ItemIcons` and `-Height`. `-DependsOn`/`-OptionsScript` cascade exactly as on
`Add-UICanvasDropdown` above.

### Add-UICanvasRadioGroup
```
Add-UICanvasRadioGroup [-Name <s>] [-Bind <s>] [-Choices <array>] [-Value <o>] [-Orientation Vertical|Horizontal]
```

### Add-UICanvasCheckbox
```
Add-UICanvasCheckbox [-Label] <string> [-Name <s>] [-Bind <s>] [-Value <bool>] [-Icon <s>] [-OnChange { }]
```
Value = `$true`/`$false`.

### Add-UICanvasToggle
```
Add-UICanvasToggle [-Name <s>] [-Bind <s>] [-Value <bool>] [-OnChange { }]
```
A switch.

### Add-UICanvasSlider
```
Add-UICanvasSlider [-Name <s>] [-Bind <s>] [-Value <o>] [-Minimum <d>] [-Maximum <d>] [-Step <d>] [-OnChange { }]
```

### Add-UICanvasRating
```
Add-UICanvasRating [-Name <s>] [-Value <int>] [-Max <int>] [-FontSize <d>] [-OnChange { }]
```
A 0..Max star rating.

---

## Date / time / color

### Add-UICanvasDatePicker
```
Add-UICanvasDatePicker [-Name <s>] [-Bind <s>] [-Value <o>] [-OnChange { }]
```

### Add-UICanvasTimePicker
```
Add-UICanvasTimePicker [-Name <s>] [-Value <s>] [-StepMinutes <int>] [-OnChange { }]
```

### Add-UICanvasColorPicker
```
Add-UICanvasColorPicker [-Name <s>] [-Value <s>] [-Choices <array>] [-OnChange { }]
```

---

## Progress & status

### Add-UICanvasProgressBar
```
Add-UICanvasProgressBar [-Name <s>] [-Bind <s>] [-Value <o>] [-Minimum <d>] [-Maximum <d>]
  [-Fill <hex>] [-Glow $true|<hex>] [-GlowPulse] [-GlowRadius <d>]
```
- `-Fill` sets the bar color. `-Glow $true` = neon glow in the fill color; `-Glow '#34D399'` = a specific glow color.
- `-GlowPulse` animates the glow; `-GlowRadius` sets its size.
```powershell
Add-UICanvasProgressBar -Name dep -Value 0 -Fill '#34D399' -Glow '#34D399' -GlowPulse -GlowRadius 16
# later, in an action: Set-UICanvasValue dep 65
```

### Add-UICanvasProgressRing
```
Add-UICanvasProgressRing [-Name <s>] [-Foreground <hex>] [-Thickness <d>]
```
An indeterminate spinner. (`Set-UICanvasProperty <name> Spin $true/$false` toggles spin on any icon/image too.)

### Add-UICanvasConsole
```
Add-UICanvasConsole [-Name <s>] [-Bind <s>] [-Value <o>]
```
A themed monospace log/output area. Append live via `Set-UICanvasValue`/`-Refresh` or bind to state.
To add one line and scroll to it: `Set-UICanvasProperty <name> AppendLine '<text>'` — the cheapest way to
stream a log from an async block, since it doesn't resend the whole text. The console is one colour, so
mark levels in the text (`[ERROR] …`).

---

## Images & raw XAML

### Add-UICanvasImage
```
Add-UICanvasImage [-Name <s>] -Source|-Value <path> [-Properties @{ Stretch = 'Uniform' }]
```
Renders a PNG/JPG/etc. `-Source` is an alias of `-Value`.

### Add-UICanvasXaml  (escape hatch)
```
Add-UICanvasXaml [-Name <s>] [-Markup <xaml-string>] [-Path <xaml-file>]
```
Splices raw WPF XAML for anything the cmdlets don't cover. Named elements inside (including the root) are
registered and addressable via `Set-UICanvasProperty`/`Get-UICanvasValue`. Theme brushes resolve via
`{DynamicResource ...}`. Use sparingly.

---

## Data controls (Tier-1)

### Add-UICanvasDataGrid
```
Add-UICanvasDataGrid [-Name <s>] [-Columns <string[]>] [-Items <object[]>] [-BindItems <stateKey>] [-Height <d>]
```
A modern data grid. Pass rows as **`-Items`** (array of `[pscustomobject]`/hashtables) — they serialize
correctly. Columns auto-generate, or pass `-Columns` for explicit ones. **`-BindItems 'key'`** makes the rows
follow a state list reactively (see [03](03-runtime-and-interactivity.md)). Shows a centered **"No data"**
placeholder when empty.
```powershell
Add-UICanvasDataGrid -Name grid -Items @(
    [pscustomobject]@{ Host='web-01'; Role='Web';      Status='Online' }
    [pscustomobject]@{ Host='db-01';  Role='Database'; Status='Online' }
)
```

### Add-UICanvasTreeView
```
Add-UICanvasTreeView [-Name <s>] [-Nodes <object[]>] [-Height <d>]
```
Nested tree. Each node = `@{ Text='Sites'; Expanded=$true; Children=@(@{ Text='HQ' }, ...) }`.

---

## Dashboard cards

### Add-UICanvasMetricCard
```
Add-UICanvasMetricCard [-Caption] <string> [-Value <o>] [-Trend Up|Down|Stable] [-Delta <s>]
```
A KPI tile: big value + caption + an optional trend arrow and delta.

### Add-UICanvasStatusCard
```
Add-UICanvasStatusCard [-Title] <string> [-Items <string[]>] [-Background <hex>]
```
A list of `"Label|State"` items, each with a colored status dot (e.g. `'API|Online'`, `'Disk|Warning'`).

### Add-UICanvasTableCard
```
Add-UICanvasTableCard [-Title] <string> [-Columns <string[]>] [-Rows <object[]>] [-Background <hex>]
```
A simple titled table card (lighter than DataGrid).

### Add-UICanvasChartCard
The full chart system — bar/line/area/donut/sparkline, multi-series, spline, reactive. See [04-charts.md](04-charts.md).

---

## Pickers (composite)

### Add-UICanvasFolderPicker
```
Add-UICanvasFolderPicker [-Name <s>] [-Value <s>] [-Placeholder <s>] [-Description <s>] [-ButtonLabel <s>]
```
A path textbox + Browse button (native folder dialog).

### Add-UICanvasFilePicker
```
Add-UICanvasFilePicker [-Name <s>] [-Value <s>] [-Placeholder <s>] [-Title <s>]
  [-Filter 'Label (*.ext)|*.ext'] [-ButtonLabel <s>]
```
A path textbox + Browse button (native open-file dialog).

---

## Iteration & shapes

### Add-UICanvasRepeater
```
Add-UICanvasRepeater -Items <object[]> -Template { param($item) ... }
```
Authoring-time loop: runs `-Template` once per item, emitting controls for each. Use inside a container's
`-Children` to build N cards/rows from data.
```powershell
Add-UICanvasPanel -Layout Grid -Columns 3 -Spacing 12 -Children {
    Add-UICanvasRepeater -Items $stats -Template { param($s)
        Add-UICanvasCard -Layout VStack -Children {
            Add-UICanvasLabel $s.Value -FontSize 22 -FontWeight Bold
            Add-UICanvasLabel $s.Title -Foreground '#94A3B8'
        }
    }
}
```
(Repeater is **static** — it builds once. For a live, data-driven list use a DataGrid with `-BindItems`.)

### Shapes
```
Add-UICanvasRectangle [-Fill <hex>] [-Stroke <hex>] [-StrokeThickness <d>] [-CornerRadius <d>]
Add-UICanvasEllipse   [-Fill <hex>] [-Stroke <hex>] [-StrokeThickness <d>]
Add-UICanvasLine      [-Stroke <hex>] [-StrokeThickness <d>]
```
Primitive shapes (mostly for Canvas-layout decoration).

---

## Generic escape: Add-UICanvasControl
```
Add-UICanvasControl -Type <string> [-Name <s>] [-Label <o>] [-Value <o>] [-Choices <o>]
  [-Action { }] [-OnChange { }] [-Properties @{ ... }]
```
The low-level cmdlet every typed cmdlet forwards to. Rarely needed directly; prefer the typed cmdlets.
