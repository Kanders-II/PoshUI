# 🎨 PoshUI v1.4.0 — Canvas: Free-Form Apps

PoshUI gains a **fourth module**. Where Wizard, Dashboard and Workflow each give you an opinionated frame,
**PoshUI.Canvas** gives you a blank page and ~85 cmdlets — lay out anything, anywhere, in a single `.ps1`.
No XAML, no MVVM, no project scaffolding, no build step. The script *is* the application.

This is an **additive** release. Existing Wizard, Dashboard and Workflow scripts are unaffected, and the new
workflow execution mode is opt-in.

---

## 🆕 PoshUI.Canvas

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

That is a complete, themed desktop application. Save it, run it, ship it.

- The **full WPF panel set** — Grid, VStack, HStack, Wrap, Dock, and absolute X/Y — with Card / Panel / Group /
  Expander / Tabs / Viewbox containers and scrolling.
- **~55 control types** — inputs, pickers, data grids, tree views, charts, consoles, banners, menus, shapes.
- Reactive state, animation, toasts, dialogs, flyouts, secondary windows, keyboard shortcuts and theming.
- A **raw XAML escape hatch** (`Add-UICanvasXaml`) for anything the cmdlets don't cover — `x:Name`'d elements
  stay fully drivable from PowerShell, so you can never get stuck.

---

## ⚙️ Engine-Native Workflow Runner

`Add-UICanvasWorkflow -Engine` runs steps on the engine's workflow executor **in its own runspace**, off the
app's serialized gate — so progress and elapsed time stay live *while* a long step runs, instead of freezing
until it finishes.

```powershell
Add-UICanvasWorkflowStep 'Download' -ExpectedSeconds 30 -Retry 2 -Script {
    1..10 | ForEach-Object { $wf.UpdateProgress($_ * 10, "downloading $($_*10)%"); Start-Sleep 1 }
}
Add-UICanvasWorkflow -Engine -Name deploy -AutoStart
```

- Per-step **`-Retry`**, **`-TimeoutSeconds`**, **`-SkipWhen`** and **`-ExpectedSeconds`** (weighted progress).
- Engine-managed **reboot-resume** via `-StateFile` — completed steps are pre-marked and the run continues.
- A **`$wf`** step context: `UpdateProgress`, `WriteOutput`, `GetValue`/`SetValue`, `SetData`/`GetData`,
  `SkipTask`, `RequestReboot`.
- **Opt-in.** Without `-Engine` the previous behaviour is preserved exactly, and the rendered UI is identical
  either way.

---

## 🧩 New Controls & Authoring

| Addition | What it gives you |
|---|---|
| **`Add-UICanvasScriptCard`** | Dashboard card that runs its script **off the UI gate**, streaming output live — a long job never freezes live cards. |
| **`Add-UICanvasTabs` / `Add-UICanvasTab`** | Tabbed container. Value is the selected index; `Set-UICanvasValue` also accepts a tab's header text. |
| **`Add-UICanvasMenu`** | Classic menu bar from nested items — submenus, separators, gestures, checkable and disabled entries. |
| **`Add-UICanvasGridSplitter`** | Drag to resize adjacent Grid columns or rows. |
| **`Add-UICanvasViewbox`** | Scales content to fit; vector scaling, so text stays crisp. |
| **`-DependsOn` / `-OptionsScript`** | Cascading dropdowns and list boxes — options recompute when a parent field changes, with no event wiring. |
| **`Add-UICanvasDataGrid -OnChange`** | Selecting a row raises `ValueChanged`, enabling master/detail panes. |

---

## 🖥️ Per-Monitor V2 DPI Awareness

The engine now declares Per-Monitor V2. Previously it was only System-DPI-aware: crisp on the primary monitor,
but bitmap-stretched (blurry) the moment a window moved to a monitor at a different scale factor, with no way
to recover short of restarting.

Windows now re-renders at the target monitor's DPI, adapts live to scaling changes, and correctly scales the
non-client area and child dialogs.

> **Sizing note:** Canvas sizes are logical units (1/96"), so higher scaling leaves *less* space to lay out in —
> a 1080p laptop at 150% gives you only 1280×720. Prefer proportional layout and set `-MinWidth`/`-MinHeight`.

---

## 🤖 Building Canvas Apps with AI

Canvas contains no AI and needs none. But because it is flat, declarative, single-file and build-free, with a
small closed cmdlet vocabulary, it is an unusually good **target** to develop for with an AI assistant.

`Docs/agent/` is a self-contained context pack written to be handed to a model — architecture, every control,
the runtime, charts, theming, flows, and the rules that prevent the common silent failures.

---

## 🐛 Bug Fixes

- **Cross-page value reads returned `$null`.** `Get-UICanvasValue` only searched the currently rendered page, so
  a field entered on an earlier page read as empty from a later page's action. It now falls back to the
  persisted cross-page snapshot.
- **Dynamic string lists rendered as `System.Data.DataRowView`.** Setting `ItemsSource` at runtime to a list of
  strings wrapped each item in a DataView, because a string exposes a `Length` property and looked like a
  single-column row. Scalars now pass through as plain items.
- **`-SkipWhen` deadlocked the UI.** Skip conditions were evaluated synchronously on the UI thread, so a
  condition calling a bridge cmdlet — the documented pattern — blocked forever. They now evaluate off-thread.
- Per-step progress bars and the overall gauge no longer attempt a two-way binding against read-only sources.
- **CI reported success without running any tests.** The Pester task passed when it discovered zero tests, which
  it always did on a clean clone. It now fails loudly when the test path is missing or empty.

---

## 📦 What's Included

```
PoshUI/
├── PoshUI.Canvas/          # NEW - free-form app module (~85 cmdlets)
├── PoshUI.Wizard/          # Wizard module
├── PoshUI.Dashboard/       # Dashboard module
├── PoshUI.Workflow/        # Workflow module
├── Examples/
│   ├── Canvas-*.ps1        # Feature demos (workflow, dashboard, cascade, layout, wizard, ...)
│   ├── Endpoint-*.ps1      # NEW - the two real-world apps
│   └── Lib/EndpointUI.ps1  # NEW - shared chrome + helpers
├── Docs/
│   ├── canvas/             # NEW - what Canvas is and how it works
│   └── agent/              # NEW - LLM context pack for authoring Canvas apps
├── bin/                    # PoshUI.exe v1.4.0 (Per-Monitor V2 DPI aware)
└── README.md
```

---

## ⚠️ Upgrade Notes

- **Nothing breaks.** Canvas is a new module; the existing three are unchanged. `-Engine` is opt-in, so existing
  Canvas workflows keep their current behaviour until you add the switch.
- **The engine binary changed** (DPI manifest + Canvas controls). If you stage `PoshUI.exe` yourself, take the
  new one along with its `PoshUI.exe.config` — both halves of the DPI opt-in are required.
- **Save Canvas scripts as UTF-8 *with BOM*** if they contain any non-ASCII character. Windows PowerShell 5.1
  misreads UTF-8 without a BOM and the app breaks.

---

## 🚀 Installation

1. Download the release package
2. Extract to your preferred location
3. Import the module you need:

```powershell
# For free-form Canvas apps (new in 1.4.0)
Import-Module .\PoshUI\PoshUI.Canvas\PoshUI.Canvas.psd1

# For Wizards
Import-Module .\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1

# For Dashboards
Import-Module .\PoshUI\PoshUI.Dashboard\PoshUI.Dashboard.psd1

# For Workflows
Import-Module .\PoshUI\PoshUI.Workflow\PoshUI.Workflow.psd1
```

Then try a demo:

```powershell
.\PoshUI\Examples\Canvas-Layout-Demo.ps1
.\PoshUI\Examples\Endpoint-SupportDesk.ps1
```

---

## 🛠️ System Requirements

| Requirement | Version |
|---|---|
| Operating System | Windows 10/11 (64-bit) |
| PowerShell | Windows PowerShell 5.1 |
| .NET Framework | 4.8 (included with Windows 10/11) |
| Permissions | User-level (no admin required for most features) |

---

## 📖 Documentation

Full documentation: **https://kanders-ii.github.io/PoshUI/**

- [About Canvas](https://kanders-ii.github.io/PoshUI/canvas/about) — what it is, how it works, how to use it
- Agent Authoring Guide — `Docs/agent/README.md` in the package

---

## 🤝 Getting Help

- **Documentation:** https://kanders-ii.github.io/PoshUI/
- **Issues:** [GitHub Issues](https://github.com/Kanders-II/PoshUI/issues)
- **Examples:** Check the `Examples/` folder for working demos

---

*Made with ❤️ for the PowerShell Community*

[Documentation](https://kanders-ii.github.io/PoshUI/) • [GitHub](https://github.com/Kanders-II/PoshUI) • [Report Issue](https://github.com/Kanders-II/PoshUI/issues)
