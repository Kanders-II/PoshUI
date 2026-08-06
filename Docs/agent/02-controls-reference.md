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
Add-UICanvasButton [-Label] <string> [-Name <s>] [-Action { }]
  [-Style Standard|Secondary|Accent|Primary|Subtle] [-Icon <s>]
```
- `-Action { }` runs (on the bridge runspace) when clicked.
- `-Style`: `Accent`/`Primary` = filled brand; `Secondary` = outlined; `Subtle` = quiet/ghost; `Standard` = default.
- `-Icon` = a PNG path or glyph shown before the label. `-Properties @{ ContentAlign = 'Left' }` left-aligns content.
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
