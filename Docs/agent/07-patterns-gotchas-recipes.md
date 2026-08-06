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
