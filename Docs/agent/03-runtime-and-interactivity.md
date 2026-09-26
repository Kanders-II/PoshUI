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
  legitimate cross-page/defensive read, not an error). Values typed on a page you have navigated away from are
  still returned: the engine snapshots each page's values when you leave it.
- Setting a control that doesn't exist (or isn't on the current page) writes a `no control named '...'` line to
  the engine log. Add **`-Quiet`** to `Set-UICanvasValue` / `Set-UICanvasProperty` when that is expected — e.g.
  a background job updating a progress bar on a page the user may have left.
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

### What happens to async work when the window closes

Closing the window **stops** in-flight `Start-UICanvasAsync` pipelines. It does not wait for them, and it
cannot roll them back — so treat the close as a cancellation point:

- Persist progress **as you go**, not at the end. A long job that only writes its result on the last line
  loses everything if the operator closes the window.
- External processes you started (`msiexec`, `wusa`, a PSADT package) are separate processes and keep
  running to completion regardless. Stopping the pipeline does not stop them.
- Don't rely on a `finally` block in the async scriptblock for cleanup that must happen. A stopped
  pipeline may not run it.

This is deliberate. Before, async pipelines kept executing after the window was gone: the run continued
invisibly, and the process never exited — every leaked `PoshUI.exe` also stranded the `powershell.exe`
host blocked on `WaitForExit()`, and it held a file lock on `PoshUI.exe` that blocked redeploys.

### Why the runtime cmdlets can no-op during teardown

Every runtime cmdlet marshals to the UI thread, and most do it with a **blocking** call. During shutdown
that is a deadlock waiting to happen — `Runspace.Close()` waits for pipelines while a pipeline waits for
the UI thread — so the bridge closes a gate first and marshalling calls become no-ops. Consequences for
authors:

- A `Get-UICanvasValue` issued while the window is closing returns `$null`, not the value. Capture what
  you need **before** long work, not after it.
- `Submit-UICanvas` writes its result file *before* asking the window to close, so submitted values are
  safe. Values you only read after a close request are not.

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

The dialog draws its **own** title bar (accent tick, title, `✕`) and follows the theme. It does not use OS
window chrome — Windows draws that in the *system* theme, which put a light strip above dark content and
made the dialog the only light surface in a dark app. Consequences: the `✕` returns `$null`, exactly like
Cancel, and the dialog is dragged by its title area (the button strip is not a drag handle).

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

### Secondary windows
A second window for a detail view, a tool, or a modal form. Define it at authoring time with
`New-UICanvasWindow`; show it from any action with `Show-UICanvasWindow`; an action running **inside** it closes
it with `Close-UICanvasWindow`.
```
New-UICanvasWindow [-Name] <s> -Content { Add-UICanvas* ... }
  [-Title <s>] [-Width <d>] [-Height <d>] [-MinWidth <d>] [-MinHeight <d>] [-Modal] [-Topmost]
  [-Resizable <bool>] [-Position CenterOwner|CenterScreen|Manual] [-X <d>] [-Y <d>] [-Icon <png>]
  [-Layout VStack|Stack|HStack|Grid|Wrap|Canvas] [-Columns <int>] [-Spacing <d>] [-Padding <d>]
  [-NoScroll] [-HideTitleBar] [-TitleBarColor <hex>] [-TitleBarText <hex>]
Show-UICanvasWindow [-Name] <s>
Close-UICanvasWindow
```
```powershell
New-UICanvasWindow -Name details -Title 'Server details' -Width 520 -Height 380 -Modal -Content {
    Add-UICanvasLabel -Bind 'Details for {selected}' -FontSize 16 -FontWeight SemiBold
    Add-UICanvasTextBox -Name note -Placeholder 'Add a note'
    Add-UICanvasButton 'Save and close' -Style Accent -Action {
        Set-UICanvasState lastNote (Get-UICanvasValue -Name note)
        Close-UICanvasWindow
    }
}
Add-UICanvasButton 'Open details' -Action { Show-UICanvasWindow details }
```
- The window shares the app's runspace, **reactive state** and theme — bind its controls to state keys to pass
  data in and out (above, `{selected}` in, `lastNote` out).
- **`-Modal`** disables the main window until the modal closes; its own buttons work normally (including one
  calling `Close-UICanvasWindow`). `Show-UICanvasWindow` **returns immediately** for both kinds of window, so do
  not put code after it that expects the window to have closed — react to what the window did from the window's
  own actions, or through state (`Watch-UICanvasState`, or a label bound to the key it sets).
- On **engine 1.4.1 and earlier**, `Show-UICanvasWindow` on a modal window blocked the action queue until the
  window closed, so buttons inside a modal never ran. If you must support that engine, leave `-Modal` off and use
  `-Topmost` instead.
- `-TitleBarColor` / `-TitleBarText` tint the Windows 11 caption to match the theme — without them the caption
  stays in the system (usually light) colours.
- Define windows **before** `Show-PoshUICanvas`, like pages; they are not pages and don't appear in navigation.

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

Those five properties are the whole runtime animation surface — no rotation, scale, colour, blur or clip,
and nothing that **sequences** ("A, then B 300ms later, while C is still moving"). For any of that, use a
`Storyboard` inside an `Add-UICanvasXaml` island: see [16-animation-and-motion.md](16-animation-and-motion.md).

## Keyboard shortcuts
```powershell
Add-UICanvasShortcut 'Ctrl+S' -Action { Submit-UICanvas }   # authoring-time registration
Add-UICanvasShortcut 'F5' -Action { Set-UICanvasState refresh ((Get-UICanvasState refresh) + 1) }
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
