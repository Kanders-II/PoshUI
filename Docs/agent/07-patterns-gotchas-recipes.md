# 07 — Patterns, Gotchas & Recipes

## Critical gotchas (these cause silent failures)

1. **UTF-8 with BOM** for any non-ASCII content (see README rule 2). No BOM → garbled/blank in Windows PowerShell 5.1.

2. **`$Name` (and other param) shadowing inside `-Children { }`.** Container cmdlets declare `-Name`, `-Width`,
   `-Value`, etc. A function-local `$Name` is shadowed inside the block. Alias first:
   ```powershell
   function Build-Section { param([string]$Name)
       $sec = $Name                         # alias BEFORE the -Children block
       Add-UICanvasCard -Children { Add-UICanvasLabel "Section: $sec" }
   }
   ```

3. **No `-Padding` on a stack-layout page.** It can break the layout root and blank the whole page. Use a child
   `Margin` for top spacing: `Add-UICanvasLabel 'Title' -Properties @{ Margin = '0,8,0,0' }`.

4. **Runtime vs authoring cmdlets.** `New-UICanvasState`, `Watch-UICanvasState`, `Add-UICanvasShortcut`,
   `Add-UICanvasWizardNav`, `Add-UICanvasWorkflowStep` are **authoring** (build the page). `Set-/Get-UICanvasState`,
   `Set-/Get-UICanvasValue`, `Set-UICanvasProperty`, `Show-UICanvasToast/Dialog/Flyout`, `Show-UICanvasPage`,
   `Submit-UICanvas`, `Start-UICanvasAsync`, `Set-UICanvasAnimate` are **runtime** (call from actions).

5. **`-Refresh` is in seconds**, and it only fires when `-Label`/`-Value` is a **scriptblock**.

6. **Nested data goes in the dedicated param**, not `-Properties`: grid `-Items`, tree `-Nodes`, chart
   `-Datasets`. Putting nested objects in the `-Properties` hashtable does not round-trip.

7. **Donut/center positioning, alignment in Canvas:** under a `Canvas` layout root, `VAlign`/`HAlign` are
   ignored (use `-X/-Y`). Under stack/grid layouts, use `VAlign`/`HAlign`.

8. **Before rebuilding/iterating, kill a running instance** of the engine if you launched one, or a locked DLL
   ships a stale build. When testing: stop `PoshUI` process, then relaunch.

9. **A clickable row should be a `Panel`, not a `Card`.** Cards always draw a 1px border and a card
   background — that is unconditional, so `-Background 'Transparent'` on a Card will not remove the box.
   `Add-UICanvasPanel -Action { }` is just as clickable (hand cursor, transparent hit-test area) and draws
   no chrome; `HoverScale` works on both.

10. **`-Refresh` with a `-Label` scriptblock evaluates it ONCE IN THE HOST** at build time, to seed the
    initial text — before the engine even launches. A "run once" guard based on a marker file therefore
    fires in the host first and the real in-app tick then short-circuits. Either clear the guard after
    building the canvas and before `Show-PoshUICanvas`, or arm on the first tick and act on the second.

## Idioms

- **Live label:** `Add-UICanvasLabel -Refresh 1 -Label { Get-Date -Format HH:mm:ss }`.
- **Disable a button while working:** `Set-UICanvasProperty go Enabled $false` … `…$true`.
- **Busy spinner:** `Set-UICanvasProperty icon Spin $true` / `$false`, or `Add-UICanvasProgressRing`.
- **Two-column form:** a `-Layout Grid -Columns 2 -Spacing 12` panel; each child takes a column.
- **App shell:** a `-Layout Dock` page with a left nav `Panel` (docked) and a content `Panel` filling the rest.
- **Cards from data:** `Add-UICanvasRepeater` inside a grid panel (static) or a `DataGrid -BindItems` (live).

---

## Recipe A — Form that returns values
```powershell
Import-Module .\PoshUI\PoshUI.Canvas\PoshUI.Canvas.psd1 -Force
New-PoshUICanvas -Title 'New user' -Theme Dark -Width 520 -Height 420 | Out-Null
Set-UITheme -Preset Slate -Accent Indigo | Out-Null

Add-UICanvasPage -Title 'New user' -Layout VStack | Out-Null
Add-UICanvasLabel 'Create a user' -FontSize 20 -FontWeight Bold -Properties @{ Margin='0,8,0,4' }
Add-UICanvasCard -Layout VStack -Spacing 12 -Padding 18 -Children {
    Add-UICanvasTextBox  -Name userName -Placeholder 'User name'
    Add-UICanvasDropdown -Name role -Choices @('Admin','Operator','Read-only') -Value 'Operator'
    Add-UICanvasCheckbox 'Send welcome email' -Name welcome -Value $true
    Add-UICanvasPanel -Layout HStack -Spacing 10 -Children {
        Add-UICanvasButton 'Create' -Style Accent -Action {
            if (-not (Get-UICanvasValue userName)) { Show-UICanvasToast -Message 'Name required' -Severity warning; return }
            Submit-UICanvas
        }
        Add-UICanvasButton 'Cancel' -Style Secondary -Action { Submit-UICanvas }
    }
}
$r = Show-PoshUICanvas
"User: $($r.userName), role $($r.role), welcome=$($r.welcome)"
```

## Recipe B — Reactive counter + computed label
```powershell
New-PoshUICanvas -Title 'Reactive' -Theme Dark -Width 520 -Height 320 | Out-Null
Add-UICanvasPage -Title 'Reactive' -Layout VStack | Out-Null
New-UICanvasState @{ count = 0; name = 'World' }

Add-UICanvasLabel -Bind 'Hello {name}, count is {count}' -FontSize 17 -FontWeight SemiBold
Add-UICanvasTextBox -Bind 'name' -Width 280
Add-UICanvasPanel -Layout HStack -Spacing 10 -Children {
    Add-UICanvasButton '+1' -Style Accent -Action { Set-UICanvasState count ((Get-UICanvasState count) + 1) }
    Add-UICanvasButton 'Reset' -Style Subtle -Action { Set-UICanvasState count 0 }
}
Watch-UICanvasState -Key 'count' -Action {
    if ((Get-UICanvasState count) -ge 5) { Show-UICanvasToast -Message 'Reached 5!' -Severity success }
}
Show-PoshUICanvas | Out-Null
```

## Recipe C — Live dashboard (charts + grid streaming from state)
```powershell
New-PoshUICanvas -Title 'Dashboard' -Theme Dark -Width 1000 -Height 640 | Out-Null
Set-UITheme -Preset Slate -Accent Sky | Out-Null
Add-UICanvasPage -Title 'Dash' -Layout VStack | Out-Null

New-UICanvasState @{
    cpu  = @(20,35,28,50,42,66)
    rows = @( @{ Host='web-01'; Status='Online' } )
}
Add-UICanvasLabel -Name ticker -Refresh 2 -Foreground '#34D399' -Properties @{ Margin='0,6,0,0' } -Label {
    $c = @(Get-UICanvasState cpu) + (Get-Random -Min 20 -Max 90); if ($c.Count -gt 12) { $c = $c[-12..-1] }
    Set-UICanvasState cpu $c
    "● live $(Get-Date -Format HH:mm:ss)"
}
Add-UICanvasPanel -Layout Grid -Columns 2 -Spacing 12 -Children {
    Add-UICanvasChartCard 'CPU' -Name cpuChart -Type Area -Spline -Bind 'cpu' -Foreground '#38BDF8' -ChartHeight 130
    Add-UICanvasCard -Layout VStack -Spacing 6 -Padding 14 -Children {
        Add-UICanvasLabel 'Servers' -FontWeight SemiBold
        Add-UICanvasDataGrid -Name grid -BindItems 'rows' -Height 130
        Add-UICanvasButton 'Add server' -Style Accent -Action {
            $r = @(Get-UICanvasState rows); $r += @{ Host=('node-{0:D2}' -f ($r.Count+1)); Status='Provisioning' }
            Set-UICanvasState rows $r
        }
    }
}
Show-PoshUICanvas | Out-Null
```

## Recipe D — Action flyout menu
```powershell
New-PoshUICanvas -Title 'Flyout' -Theme Dark -Width 480 -Height 240 | Out-Null
Add-UICanvasPage -Title 'Flyout' -Layout VStack | Out-Null
Add-UICanvasButton 'Server actions ▾' -Name actionsBtn -Style Accent -Action {
    $choice = Show-UICanvasFlyout -Target actionsBtn -Title 'Server' -Items @('Restart','Shut down','View logs') -Placement Bottom
    if ($choice) { Show-UICanvasToast -Message "Chose: $choice" -Severity success }
}
Show-PoshUICanvas | Out-Null
```

## Running & verifying
- Run: `powershell.exe -NoProfile -File MyApp.ps1` (Windows PowerShell 5.1, not pwsh 7).
- The engine logs to `PoshUI\bin\logs\PoshUI.log`. Lines with `[diag]` flag would-be-silent failures (e.g.
  `Set-UICanvasValue: no control named 'x'`) — useful when a control name is wrong.
- Set env `POSHUI_CANVAS_DIAG=1` to also surface those as toasts (strict mode).

## Diagnosing "it built fine and looks wrong"

The engine accepts a `-Properties` bag and reads the keys it understands. Anything else used to be
dropped in silence, which turned author mistakes into wrong-looking screens with a clean log and exit
code 0. The factory now reports them.

After each page is built it logs, at warning level, every `-Properties` key **nothing in the build
path ever read**:

```
[unused property] Panel > Image: 'ImageWidth' was supplied but nothing read it -
                  it is only read for a Banner hero image; size a plain Image with -Width/-Height
[unused property] Panel > Label: 'Margins' was supplied but nothing read it.
```

Check `PoshUI\bin\logs\PoshUI.log` first whenever a screen renders wrong. It catches typos and — more
usefully — keys that are real for a *different* control type, which is why they look correct in review.

The module also warns at author time when a `-Properties` value is a nested object, since nested values
do not round-trip into the definition; use the dedicated parameter (`-Nodes`, `-Datasets`, `-Items`,
`-Choices`) instead.

**This does not catch everything.** A key that IS read but has no effect in that position still passes
silently. Rules for those were prototyped and withdrawn because they fired on known-good code — for
example `-Padding` on a `VStack` card is correct and common, so warning about "Padding on a stack"
produced seven false positives in one small app. A diagnostic that cries wolf is worse than none.

## When to reach for a XAML island

`Add-UICanvasXaml` is not a last resort, but it is not free either: inside an island you leave the
engine's layout, theming helpers and control set behind, and take on raw WPF.

Use an island when the screen needs something the properties bag genuinely does not model:

- motion driven by **hover** (there is no author-facing hover event; `EventTrigger` + `Storyboard` is)
- **clipping**, opacity masks, transforms
- custom-drawn instruments (gauges, arcs, dials)
- an exact reproduction of a supplied design

Keep on engine controls: chrome, navigation, forms, data widgets, and anything that has to run
PowerShell — though `-Actions` now binds scriptblocks to named controls inside an island too, so a
tile strip can slide on hover *and* stay clickable.

Decide the boundary **before** building the screen. Retrofitting an island around a finished layout
costs more than planning one, and a page that has quietly become 90% island is a signal that the
screen wanted to be in-process WPF all along.

Once the boundary is decided, [16-animation-and-motion.md](16-animation-and-motion.md) covers what to do
inside one: storyboard mechanics (they must self-start — PowerShell cannot call `.Begin()` from an action),
the `Storyboard.TargetProperty` paths that actually work, a catalogue of techniques, and how to verify the
result headlessly.
