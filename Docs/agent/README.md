# PoshUI Canvas — Agent Authoring Guide

This is a complete, self-contained reference for authoring **PoshUI Canvas** apps: free-form, themeable
desktop UIs written in **Windows PowerShell 5.1** and rendered by the **.NET Framework 4.8 / WPF** engine
(`PoshUI.exe`). The engine software-renders, so it needs no GPU compositing and runs on virtual machines,
remote sessions, and locked-down hardware.

It is written to be fed to an LLM so an agent can generate correct, working Canvas apps using every feature.
Read the **Golden Rules** below first — they prevent the most common silent failures — then use the numbered
files as a reference.

## Document set
| File | Covers |
|------|--------|
| [01-architecture-and-authoring.md](01-architecture-and-authoring.md) | Engine model, the authoring lifecycle, pages, layout roots, containers, the `-Properties` bag, anchoring. |
| [02-controls-reference.md](02-controls-reference.md) | Every `Add-UICanvas*` control with its real signature, an example, and notes. |
| [03-runtime-and-interactivity.md](03-runtime-and-interactivity.md) | The action model and **every runtime cmdlet**: value/property get-set, reactive state + binding, navigation, async, toast/dialog/flyout, pickers, animation, shortcuts. |
| [04-charts.md](04-charts.md) | The full chart system: bar/line/area/donut/sparkline, multi-series, spline, reactive binding. |
| [05-theming.md](05-theming.md) | `Set-UITheme`: presets, accent presets, every color slot, light/dark mode, tooltips. |
| [06-flows-wizard-workflow.md](06-flows-wizard-workflow.md) | Multi-step wizard (validation gating), the workflow runner (`-Engine`, `$wf`, retry/timeout/skip, reboot-resume), and the on-demand ScriptCard. |
| [07-patterns-gotchas-recipes.md](07-patterns-gotchas-recipes.md) | The hard rules, anti-patterns, and complete copy-paste apps. |
| [08-cheatsheet.md](08-cheatsheet.md) | Terse index of every cmdlet and its parameters for fast lookup. |

## Golden Rules (read these first)

1. **Lifecycle is fixed.** Every app is exactly: `Import-Module` → `New-PoshUICanvas` → (optional `Set-UITheme`)
   → `Add-UICanvasPage` → one or more `Add-UICanvas*` calls → `Show-PoshUICanvas`. Nothing renders until
   `Show-PoshUICanvas`.

2. **Save scripts UTF-8 *with BOM*** if they contain any non-ASCII character (●, —, ▾, box-drawing, emoji,
   accented text). Windows PowerShell 5.1 misreads UTF-8-without-BOM and the app breaks. In an editor,
   choose "UTF-8 with BOM". Programmatically:
   ```powershell
   [System.IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding $true))
   ```

3. **Never reference a function-local `$Name` variable inside a `-Children { }` block.** The container cmdlets
   define their own `$Name` parameter, which **shadows** yours, so `$Name` is empty/wrong inside the block.
   Alias it first: `$myName = $Name` and use `$myName` inside the block. (Same for any param name a container
   also has: `$Width`, `$Height`, `$Value`, etc.)

4. **Runtime cmdlets only work inside an `-Action`/`-OnChange` scriptblock** (i.e. while the app is running).
   `Set-UICanvasValue`, `Get-UICanvasValue`, `Set-UICanvasState`, `Show-UICanvasToast`, `Show-UICanvasFlyout`,
   etc. are no-ops at authoring time (they emit a warning). Build static UI with `Add-UICanvas*`; mutate it at
   runtime with the runtime cmdlets.

5. **Do not put `-Padding` on `Add-UICanvasPage`** when using a `VStack`/stack layout — it can break the page
   layout root and blank the whole page. For top spacing, put a `Margin` on the first child instead:
   `-Properties @{ Margin = '0,8,0,0' }`.

6. **`-Refresh` is in *seconds*.** `-Refresh 2` ticks every 2 seconds and re-evaluates the control's
   scriptblock `-Label`/`-Value`.

7. **Reference controls by `-Name`.** Give any control you'll read or update a `-Name`, then address it with
   `Get-UICanvasValue -Name x` / `Set-UICanvasValue x <v>` / `Set-UICanvasProperty x <prop> <v>`.

8. **Pass complex/nested data as the dedicated parameter, not inside `-Properties`.** Grids (`-Items`),
   trees (`-Nodes`), charts (`-Datasets`) accept nested objects; the cmdlet serializes them correctly. Putting
   nested objects directly into the `-Properties` hashtable does **not** round-trip.

## Minimal app (the canonical skeleton)
```powershell
Import-Module .\PoshUI\PoshUI.Canvas\PoshUI.Canvas.psd1 -Force

New-PoshUICanvas -Title 'Hello' -Theme Dark -Width 600 -Height 400 | Out-Null
Set-UITheme -Preset Slate -Accent Sky | Out-Null

Add-UICanvasPage -Title 'Home' -Layout VStack | Out-Null
Add-UICanvasLabel 'Hello, Canvas' -FontSize 22 -FontWeight Bold
Add-UICanvasButton 'Click me' -Name go -Style Accent -Action {
    Show-UICanvasToast -Message 'Clicked!' -Severity success
}

Show-PoshUICanvas | Out-Null
```
Run it: `powershell.exe -NoProfile -File MyApp.ps1` (Windows PowerShell 5.1).
