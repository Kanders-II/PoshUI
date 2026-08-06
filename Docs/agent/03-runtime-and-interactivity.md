# 03 — Runtime API & Interactivity

Everything here runs **inside an `-Action` / `-OnChange` scriptblock** (or a `-Refresh` value scriptblock /
workflow step) while the app is running. At authoring time these cmdlets are no-op stubs that warn. Actions run
on the engine's in-process runspace, serialized, full-privilege; UI changes marshal to the dispatcher.

## The action model
- `-Action { }` on a button (and similar) runs when clicked.
- `-OnChange { }` on an input runs when its value changes.
- Inside, you read/update controls by `-Name`, mutate shared state, navigate, show toasts/dialogs/flyouts, run
  background work, etc.
- Actions can be long-running; prefer `Start-UICanvasAsync` for heavy work so the UI stays responsive.

## Read & write control values
```powershell
$v = Get-UICanvasValue -Name userName        # current value of control 'userName'
Set-UICanvasValue userName 'admin'           # set it
Set-UICanvasProperty go 'Enabled' $false     # set ANY property: Enabled, Visible, Foreground, Background,
                                             #   Source/Image, Spin, ItemsSource, Value, Text, ...
```
- `Get-UICanvasValue` returns `null` for a control that isn't on the current page yet (this is fine — it's a
  legitimate cross-page/defensive read, not an error).
- `Set-UICanvasProperty <name> Spin $true` continuously rotates any icon/image (busy indicator); `$false` stops it.

## Reactive state (the modern way)
A shared store: change one key and every bound control updates itself — no per-control `Set-UICanvasValue`.

### Seed the store (authoring, once, after Add-UICanvasPage)
```powershell
New-UICanvasState @{ count = 0; name = 'World'; rows = @() }
```

### Bind controls (`-Bind`)
| Form | Meaning |
|------|---------|
| `-Bind 'key'` on an **input** (TextBox/Dropdown/Checkbox/Slider/Toggle/ListBox/RadioGroup/DatePicker/Number) | **Two-way**: user edits update `state.key`; `Set-UICanvasState` updates the control. |
| `-Bind 'key'` on a **Label/ProgressBar/Console/Chart** | **One-way**: the control shows `state.key` and redraws when it changes. |
| `-Bind 'text {key} more {k2}'` on a Label | **Computed template**: re-rendered whenever any referenced key changes. |
| `-BindItems 'key'` on a **DataGrid** | Rows follow a state **list**; `Set-UICanvasState key <newlist>` rebuilds rows (auto add/remove). |

### Mutate & react (runtime)
```powershell
Set-UICanvasState count ((Get-UICanvasState count) + 1)   # fans out to every bound control/template/watcher
$n = Get-UICanvasState name

Watch-UICanvasState -Key 'count' -Action {                 # side-effects on change (authoring-time registration)
    if ((Get-UICanvasState count) -ge 5) { Show-UICanvasToast -Message 'High!' -Severity success }
}
```
- `New-UICanvasState` and `Watch-UICanvasState` are **authoring** cmdlets (call them while building the page).
- `Set-UICanvasState` / `Get-UICanvasState` are **runtime** cmdlets (call from actions/value-scripts).
- The imperative API (`Set-UICanvasValue`) still works; reactivity is additive.

### Collection-reactive grid (full pattern)
```powershell
New-UICanvasState @{ rows = @( @{ Host='web-01'; Role='Web' } ) }
Add-UICanvasDataGrid -Name grid -BindItems 'rows'
Add-UICanvasButton 'Add' -Action {
    $r = @(Get-UICanvasState rows); $r += @{ Host='node-02'; Role='Worker' }
    Set-UICanvasState rows $r          # grid grows a row
}
```

## Navigation (multi-page)
```powershell
Show-UICanvasPage 'Review'            # navigate by page title (or index)
Submit-UICanvas                       # close the window & finalize the result set
Lock-UICanvasNavigation               # disable back-tracking (e.g. after starting a destructive run)
Unlock-UICanvasNavigation
```

## Async / non-blocking work
```powershell
Add-UICanvasButton 'Scan' -Action {
    Start-UICanvasAsync {
        $items = Get-ChildItem C:\ -Recurse -EA SilentlyContinue | Select -First 1000
        Set-UICanvasState fileCount $items.Count    # update UI from the background job
        Show-UICanvasToast -Message 'Scan complete' -Severity success
    }
}
```
`Start-UICanvasAsync { }` runs the scriptblock on its own runspace/thread so the UI thread stays responsive.
Use the runtime cmdlets inside it to push results back.

## Transient surfaces

### Toast
```powershell
Show-UICanvasToast -Message 'Saved' -Severity success      # info|success|warning|error ; -Duration <ms> (default 3000)
```
A non-blocking overlay notification that auto-dismisses.

### Dialog (modal, returns a value)
```powershell
$ok = Show-UICanvasDialog -Message 'Wipe disk 0?' -Title 'Confirm'        # $true (OK) / $null (Cancel)
$name = Show-UICanvasDialog -Message 'Name?' -Prompt -DefaultValue 'PC1'  # entered text / $null
Show-UICanvasDialog -Message 'Done.' -OkLabel 'Close' -CancelLabel 'Hide'
```
Blocks until dismissed; returns text (prompt+OK), `$true` (confirm+OK), or `$null` (cancel).

### Flyout (lightweight anchored popup, returns the chosen item)
```powershell
Add-UICanvasButton 'Actions ▾' -Name actionsBtn -Action {
    $choice = Show-UICanvasFlyout -Target actionsBtn -Title 'Server' -Items @('Restart','Shut down','Logs') -Placement Bottom
    if ($choice) { Show-UICanvasToast -Message "Chose: $choice" -Severity success }
}
# info popover (no items):
Show-UICanvasFlyout -Target helpBtn -Title 'About' -Message 'A flyout is an anchored popup…' -Placement Top
```
```
Show-UICanvasFlyout [-Target <controlName>] [-Items <string[]>] [-Title <s>] [-Message <s>]
  [-Placement Bottom|Top|Left|Right|Mouse]
```
- Anchored to the named control (omit `-Target` → at the mouse). **Dismisses on click-away.**
- Returns the clicked item, or `$null` if dismissed. Synchronous (blocks the action until closed), like a dialog.
- Use for action/context menus and detail popovers — lighter than a modal dialog.

## Native file/folder pickers
```powershell
$dir  = Select-UICanvasFolder -Description 'Pick a folder'
$file = Select-UICanvasFile -Title 'Pick a WIM' -Filter 'Images (*.wim;*.esd)|*.wim;*.esd'
```
(Or use the composite controls `Add-UICanvasFolderPicker` / `Add-UICanvasFilePicker` for a field+Browse button.)

## Animation
```powershell
Set-UICanvasAnimate -Name card -Property Opacity -To 1 -From 0 -Duration 250 -Easing CubicOut
Set-UICanvasAnimate -Name panel -Property Y -To 0 -From 12 -Duration 220
```
Animates `Opacity`/`Width`/`Height`/`X`/`Y` on a named control. `-Easing` ∈ `Linear|CubicOut|CubicInOut|...`.
Containers also support a one-shot **stagger-in** via `-Properties @{ Stagger = 60 }` (children cascade in).

## Keyboard shortcuts
```powershell
Add-UICanvasShortcut 'Ctrl+S' { Submit-UICanvas }           # authoring-time registration
Add-UICanvasShortcut 'F5' { Set-UICanvasState refresh ((Get-UICanvasState refresh) + 1) }
```
Registers a global gesture whose action runs like any other.

## Live values without state: `-Refresh`
Any control whose `-Label`/`-Value` is a **scriptblock**, combined with `-Refresh <seconds>`, re-evaluates on a
timer. Useful for clocks, counters, or a hidden "ticker" that streams data into state:
```powershell
Add-UICanvasLabel -Name ticker -Refresh 2 -Label {
    Set-UICanvasState cpu (Get-Random -Min 10 -Max 90)      # side-effect: update state every 2s
    "updated $(Get-Date -Format HH:mm:ss)"
}
```

## Security model (what an agent must respect)
- The engine validates the definition path: **local only**, no UNC, no `..` traversal, `.ps1`/`.json` only.
- **Passwords** are DPAPI-protected and returned as SecureString; never echoed/logged in plaintext.
- **Hyperlinks/markdown links** open only safe schemes (`http`/`https`/`mailto`/`file`).
- Actions, async, and spliced XAML run **in-process, full-privilege** — the trust boundary is *whoever runs the
  `.ps1`*. Don't author actions that execute untrusted input.
