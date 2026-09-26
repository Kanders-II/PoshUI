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
| [00-design-paradigms.md](00-design-paradigms.md) | **Start here.** How to shape an app: the mental model and the three execution lanes, choosing a module, the Canvas structural paradigms (event-driven, reactive state, declarative cascades, data-driven, multi-page, wizard, async, engine pipeline, XAML hybrid), the architectures around a real tool, reach and limits, wrapping an API, a decision guide and anti-patterns. |
| [01-architecture-and-authoring.md](01-architecture-and-authoring.md) | Engine model, the authoring lifecycle, pages, layout roots, containers, the `-Properties` bag, anchoring. |
| [02-controls-reference.md](02-controls-reference.md) | Every `Add-UICanvas*` control with its real signature, an example, and notes. |
| [03-runtime-and-interactivity.md](03-runtime-and-interactivity.md) | The action model and **every runtime cmdlet**: value/property get-set, reactive state + binding, navigation, async, toast/dialog/flyout, pickers, animation, shortcuts. |
| [04-charts.md](04-charts.md) | The full chart system: bar/line/area/donut/sparkline, multi-series, spline, reactive binding. |
| [05-theming.md](05-theming.md) | `Set-UITheme`: presets, accent presets, every color slot, light/dark mode, tooltips. |
| [06-flows-wizard-workflow.md](06-flows-wizard-workflow.md) | Multi-step wizard (validation gating), the workflow runner (`-Engine`, `$wf`, retry/timeout/skip, reboot-resume), and the on-demand ScriptCard. |
| [07-patterns-gotchas-recipes.md](07-patterns-gotchas-recipes.md) | The hard rules, anti-patterns, and complete copy-paste apps. |
| [08-cheatsheet.md](08-cheatsheet.md) | Terse index of every cmdlet and its parameters for fast lookup. |
| [09-app-blueprints.md](09-app-blueprints.md) | **Blueprints by objective.** Twelve kinds of app — form, admin toolbox, live monitor, API browser, data explorer, guided setup, long-running job, self-service kiosk, settings editor, file processor, launcher, log viewer — each with the paradigms to combine, the page shape, which lane each piece of work runs in, the controls to reach for, and the traps specific to it. |
| [10-visual-design-and-shell.md](10-visual-design-and-shell.md) | **How it looks.** Design principles, the visual system (tokens, type scale, spacing, contrast), a chromeless shell with a custom top bar and an animated icon rail, an intro splash that advances itself, the hero panel and page composition — its code blocks run as one complete app. |
| [11-staging-and-delivery.md](11-staging-and-delivery.md) | **Who it is for.** An admin builds and stages the app; end users or other admins run it. Code vs content vs config, staged content (a folder per item with a descriptor, discovered and validated at start), the back-end contract (result objects, logging, dry run, elevation), audience editions from config, the stager's `-Check`, packaging, launching, signing and delivery channels — built and run as a real package. |
| [15-control-catalog.md](15-control-catalog.md) | **Generated.** Every control `Type` the engine builds, the `-Properties` keys it actually reads, its cmdlet and ValidateSets, and a snippet that renders. The ground truth behind the `[unused property]` warning. |
| [16-animation-and-motion.md](16-animation-and-motion.md) | Motion: the declarative knobs, where they stop, and the XAML-island timeline. Storyboard mechanics, the property paths that work, a technique catalogue (sheen, focus pull, per-glyph cascade, slice, iris, draw-on), easing and timing, the page-load hook, and how to verify motion headlessly. |
| `Launcher\Designer\README.md` (in the repo, not on this site) | The **visual designer** (`PoshUI.exe --design App.ps1`): live preview through the real factory, inspector, drag/drop, edits spliced into the script. |

**00 is how to think; 09 is what to build for a given objective; 10 is how it should look; 11 is who stages
it and who runs it; 01–08 are the API.** Read 00, the matching blueprint in 09 and 11 before designing
anything, apply 10 for anything people will use daily, then use 01–08 to write it. Reach for **16** only when
the motion you want is more than `Stagger`, `HoverScale` and `Set-UICanvasAnimate` can express.

## Golden Rules (read these first)

1. **Lifecycle is fixed.** Every app is exactly: `Import-Module` → `New-PoshUICanvas` → `Set-UITheme`
   → `Add-UICanvasPage` → one or more `Add-UICanvas*` calls → `Show-PoshUICanvas`. Nothing renders until
   `Show-PoshUICanvas`. Treat `Set-UITheme` as required: without it the base dark palette draws
   `-Style Accent` buttons pale grey with white text.

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

6. **`-Refresh` is in *seconds*, and it fires once immediately.** `-Refresh 2` re-evaluates the control's
   scriptblock `-Label`/`-Value` every 2 seconds — **and evaluates it once at registration**, before the
   first interval elapses. That immediate tick is the closest thing Canvas has to a page-load hook; see
   [16](16-animation-and-motion.md) §7 for using it to sequence work at page entry.

7. **Reference controls by `-Name`.** Give any control you'll read or update a `-Name`, then address it with
   `Get-UICanvasValue -Name x` / `Set-UICanvasValue x <v>` / `Set-UICanvasProperty x <prop> <v>`.

8. **Pass complex/nested data as the dedicated parameter, not inside `-Properties`.** Grids (`-Items`),
   trees (`-Nodes`), charts (`-Datasets`) accept nested objects; the cmdlet serializes them correctly. Putting
   nested objects directly into the `-Properties` hashtable does **not** round-trip.

9. **Decide where each piece of work runs before writing it.** Quick UI logic on the gate (`-Action`,
   `-OnChange`); anything slow — network calls, scans, big queries — in `Start-UICanvasAsync`; multi-step jobs
   with progress and retry in `Add-UICanvasWorkflow -Engine`. See [00](00-design-paradigms.md) §1.

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
