# PoshUI Canvas

**Describe an app in plain PowerShell — get a real native Windows app.**
No XAML. No MVVM. No project scaffolding. No build step.

Canvas is the free-form authoring layer of PoshUI. A single `.ps1` file *is* the application: you declare
controls with ordinary PowerShell cmdlets, and the PoshUI engine renders them as real WPF controls and runs
your PowerShell behind them.

```powershell
Import-Module PoshUI.Canvas
New-PoshUICanvas -Title 'Hello' -Theme Dark
Add-UICanvasPage -Title 'Home' -Layout VStack
Add-UICanvasLabel 'What is your name?' -FontSize 18 -FontWeight Bold
Add-UICanvasTextBox -Name who -Value 'World'
Add-UICanvasButton 'Greet' -Style Accent -Action {
    Show-UICanvasToast -Message ("Hello, " + (Get-UICanvasValue -Name who))
}
Show-PoshUICanvas
```

That is a complete, working, themed desktop application. Save it, run it, ship it.

---

## Developing for Canvas with AI

Canvas contains no AI, and none is required to use it — it is a PowerShell authoring layer and a WPF engine.
What it *is* is an unusually good **target** to develop for with an AI assistant, because of properties it has
for entirely unrelated reasons.

Most UI frameworks are hostile to code generation: nested XAML, code-behind files, MVVM plumbing, project
files, package restore, a compiler. An assistant has to get *all* of it right, and you can't tell whether it
worked until a build succeeds. Canvas has none of those failure points.

**1. A small, closed vocabulary.** ~75 cmdlets with consistent parameter conventions (`-Name`, `-Value`,
`-Action`, `-Properties`, `-Refresh`). The entire API fits comfortably in a model's context — so the AI is
working from the real surface, not guessing.

**2. Flat and declarative.** Controls are emitted in reading order. There is no separation between markup,
code-behind, and view-model to keep in sync — the class of mistake LLMs make most often simply doesn't exist.

**3. One file, zero scaffolding.** No `dotnet new`, no `npm install`, no folder layout to hallucinate. The
model emits one script; you run it.

**4. No build step.** Generate → run → look at it → refine. The iteration loop is seconds, which is exactly
the loop AI-assisted development needs.

**5. It can't get stuck.** If a control doesn't exist as a cmdlet, `Add-UICanvasXaml` splices raw WPF XAML
into the tree (and any `x:Name`'d element still works with `Get/Set-UICanvasValue`). The AI always has a way
through.

**6. It produces working tools, not mockups.** The generated app *executes real PowerShell* — query systems,
run installers, orchestrate multi-step jobs with retry and reboot-resume. You get an internal tool, not a
picture of one.

**7. There is already an LLM context pack.** [`Docs/agent/`](../agent/README.md) is a self-contained authoring
guide written explicitly to be fed to a model, including the "golden rules" that prevent the common silent
failures.

::: tip The workflow
Paste the [agent guide](../agent/README.md) into your assistant, describe the tool you want in plain English,
save the `.ps1` it returns, and run it. Iterate by describing changes.
:::

---

## How it works

**Authoring (PowerShell).** Each `Add-UICanvas*` call appends a control definition to an in-memory tree. The
lifecycle is fixed and simple:

```
Import-Module → New-PoshUICanvas → [Set-UITheme] → Add-UICanvasPage → Add-UICanvas* … → Show-PoshUICanvas
```

Nothing renders until `Show-PoshUICanvas`, which serializes the tree and launches the engine.

**Rendering (the engine).** `PoshUI.exe` (.NET Framework 4.8 / WPF) reads the definition and builds genuine
WPF controls through its control factory. It **software-renders**, so it does not depend on GPU compositing —
it runs on virtual machines, remote sessions, and locked-down hardware where modern compositor-based UI stacks
fall over.

**The bridge (live interactivity).** An in-process PowerShell runspace connects your scripts to the live UI.
Inside any `-Action`, `-OnChange`, or refresh script you can read and drive the running app:
`Get-UICanvasValue`, `Set-UICanvasValue`, `Set-UICanvasProperty`, reactive state, navigation, dialogs.

### The four execution substrates

Understanding these explains the performance characteristics of anything you build:

| Substrate | Runs on | Use it for |
|---|---|---|
| **Event action** (`-Action`, `-OnChange`) | shared runspace, serialized | clicks, form submits, quick work |
| **Polled refresh** (`-Refresh`) | shared runspace (skips a tick if busy) | live metrics, clocks, status |
| **Async** (`Start-UICanvasAsync`) | its own runspace, off-gate | long/background work |
| **Workflow** (`-Engine`) | the engine's workflow executor, off-gate | orchestrated multi-step jobs |

Event actions and refresh share one serialized runspace, so **long work belongs in `Start-UICanvasAsync`, an
`Add-UICanvasScriptCard`, or a workflow** — all of which run off that gate and keep the UI live.

---

## What it can do

**Controls** — text, buttons, dropdowns, list boxes, radio groups, checkboxes, toggles, sliders, number
boxes, password fields (DPAPI-protected), date/time pickers, color picker, rating, auto-suggest, file/folder
pickers, markdown, images, hyperlinks, icons, badges, separators, shapes, consoles, data grids, tree views,
progress bars/rings, banners, and menu bars.

**Layout** — Grid, VStack, HStack, Wrap, Dock, and absolute X/Y positioning, with Card / Panel / Group /
Expander / **Tabs** / **Viewbox** containers, a **GridSplitter** for resizable panes, and scrolling.
(The full WPF panel set.)

**Three app shapes, one toolkit:**

- **Forms & wizards** — multi-page flows with gated Back/Next, required-field and custom validation, step
  indicators, navigation locking, and **cascading dropdowns** (`-DependsOn` / `-OptionsScript`) that recompute
  their options when a parent field changes.
- **Dashboards** — metric, status, table and chart cards that auto-refresh, reactive state binding, data-driven
  repeaters, and **`Add-UICanvasScriptCard`** — an on-demand runner card that executes off the UI gate and
  streams its output live, so a long job never freezes the rest of the dashboard.
- **Workflows** — declare steps, get an engine-driven runner with live progress, **retry, timeouts, skip
  conditions, and reboot-resume** (state persists across a restart and the run continues where it left off).

**Everything else** — theming (dark/light, presets, per-slot overrides), charts (bar/line/area/donut/
sparkline), animation, toasts, dialogs, flyouts, secondary windows, keyboard shortcuts, and — for anything the
cmdlets don't cover — a [raw XAML escape hatch](#the-escape-hatch-raw-xaml).

**Deployment** — a single script plus the engine. No external dependencies, no runtime to install, fully
offline, and air-gap friendly.

---

## Sizing, DPI & multiple monitors

**Every size you write is a logical unit, not a physical pixel.** `-Width 600`, `-FontSize 14`,
`-StepsHeight 300`, `-X`/`-Y` — all device-independent units (1/96"). The engine scales them for you, so a
Canvas app renders correctly and **crisply** at 125% / 150% / 200% display scaling with no work on your part.

The engine declares **Per-Monitor V2 DPI awareness**, so a window moved between monitors with different scaling
**re-renders at the new monitor's DPI** rather than being bitmap-stretched, and it adapts live if you change
display scaling while the app is running.

### The one thing to design for: logical space shrinks as scaling rises

Higher scaling means *fewer* logical units to lay out in — this catches people out:

| Physical resolution | Scaling | Logical space you actually get |
|---|---|---|
| 1920 × 1080 | 100% | 1920 × 1080 |
| 1920 × 1080 | **150%** | **1280 × 720** |
| 2560 × 1440 | 150% | 1707 × 960 |
| 3840 × 2160 | 200% | 1920 × 1080 |

A 1080p laptop at 150% gives you only **720 logical pixels of height**. So
`New-PoshUICanvas -Width 1280 -Height 740` — which looks modest — **will not fit** on that display.

### Rules that hold up across monitors

1. **Budget for the smallest logical size you'll target** — ~`1280 × 700` is a safe floor if laptops are in scope.
   Don't size against physical pixels.
2. **Prefer proportional layout over fixed sizes.** Grid star sizing (`-ColumnWidths '3*,*'`), `Dock`, and
   `HAlign/VAlign = 'Stretch'` reflow as space changes; fixed `-Height` values clip.
3. **Set window minimums** so the window can't be dragged into a broken state:
   `New-PoshUICanvas -Width 1100 -Height 700 -MinWidth 900 -MinHeight 600`.
4. **Scroll the tall region, not the page.** Give the one growable list a fixed height with internal scrolling
   (e.g. a workflow's `-StepsHeight 300`, or `-Properties @{ Scroll = 'Vertical' }`) so the surrounding layout
   stays put.
5. **Test at 150%.** It's the most common laptop default and the tightest realistic case.

---

## The escape hatch: raw XAML

Canvas says "no XAML" because you never *have* to write it — the cmdlets cover the common surface. But the
door is open when you need it, and that's what makes the parity claim literal: **anything WPF can render,
Canvas can render.**

`Add-UICanvasXaml` splices raw WPF markup straight into the control tree. Crucially, any element you give an
`x:Name` is **registered with the bridge**, so it behaves like a first-class control — `Get-UICanvasValue`,
`Set-UICanvasValue` and `Set-UICanvasProperty` all work on it.

```powershell
# No cmdlet wraps MediaElement, so drop to XAML for it - and still drive it from PowerShell.
Add-UICanvasXaml -Markup @'
<StackPanel>
  <MediaElement x:Name="player" Height="240" LoadedBehavior="Manual" />
  <TextBlock x:Name="nowPlaying" Margin="0,8,0,0" Text="nothing loaded" />
</StackPanel>
'@

# The x:Name'd elements are live controls - drive them like any other:
Add-UICanvasButton 'Load clip' -Style Accent -Action {
    $f = Select-UICanvasFile -Title 'Pick a video' -Filter 'Video (*.mp4)|*.mp4'
    if ($f) {
        Set-UICanvasProperty -Name player -Property Source -Value $f
        Set-UICanvasValue -Name nowPlaying -Value "loaded $f"
    }
}
```

The root element's `xmlns` is added automatically if you omit it, and `-Path` loads markup from a file instead
of inline. (Tabs, menus and splitters now have native cmdlets — see above — so reach for XAML for things like
`MediaElement`, custom control templates and styles, or anything else you'd use in a normal WPF app.) Then go
straight back to cmdlets for the rest of the page: mixing the two freely is the intended workflow, not a fallback.

---

## Using it

### A form that does something

```powershell
Import-Module PoshUI.Canvas
New-PoshUICanvas -Title 'Service Restart' -Theme Dark
Add-UICanvasPage -Title 'Main' -Layout VStack

Add-UICanvasLabel 'Restart a service' -FontSize 20 -FontWeight Bold
Add-UICanvasDropdown -Name svc -Choices (Get-Service | Select-Object -First 20 -Expand Name)
Add-UICanvasButton 'Restart' -Style Accent -Action {
    $s = Get-UICanvasValue -Name svc
    Restart-Service -Name $s -ErrorAction Stop
    Show-UICanvasToast -Message "$s restarted" -Severity success
}
Show-PoshUICanvas
```

### A dashboard that stays responsive

```powershell
# Live, polled value
Add-UICanvasLabel -Name clock -Label { Get-Date -Format 'HH:mm:ss' } -Refresh 1

# On-demand work that runs OFF the UI gate and streams output
Add-UICanvasScriptCard 'Collect logs' -Detail 'Zips the last 24h of logs' -Script {
    "starting..."
    # ...real work; every line appears in the card's console as it runs
    "done"
}
```

### A workflow with retry and live progress

```powershell
Add-UICanvasWorkflowStep 'Download' -ExpectedSeconds 30 -Retry 2 -Script {
    1..10 | ForEach-Object { $wf.UpdateProgress($_ * 10, "downloading $($_*10)%"); Start-Sleep 1 }
}
Add-UICanvasWorkflowStep 'Install' -ExpectedSeconds 60 -Script {
    $wf.WriteOutput('installing...', 'INFO')
}
Add-UICanvasWorkflow -Engine -Name deploy -StateFile 'C:\ProgramData\MyApp\state.json'
```

Steps receive `$wf` (the workflow context): `UpdateProgress`, `WriteOutput`, `GetValue`/`SetValue` for canvas
controls, `SetData`/`GetData` to pass data between steps, `SkipTask`, and `RequestReboot` — which saves state
so the run resumes automatically after a restart.

### Cascading fields

```powershell
Add-UICanvasDropdown -Name region -Choices 'US','EU','APAC'
Add-UICanvasDropdown -Name dc -DependsOn region -OptionsScript {
    switch (Get-UICanvasValue -Name region) {
        'US'   { 'us-east-1','us-west-2' }
        'EU'   { 'eu-central-1','eu-west-1' }
        'APAC' { 'ap-southeast-1' }
    }
}
```

---

## Two rules that prevent most problems

1. **Save scripts as UTF-8 *with BOM*** if they contain any non-ASCII character (—, ●, emoji, accents).
   Windows PowerShell 5.1 misreads UTF-8 without a BOM and the app breaks.
2. **Don't reference a local `$Name` inside a `-Children { }` block** — container cmdlets have their own
   `-Name` parameter which shadows it. Alias it first (`$myName = $Name`).

The [agent guide](../agent/README.md) has the complete list.

---

## Next steps

- **[Agent Authoring Guide](../agent/README.md)** — the full LLM context pack: architecture, every control,
  the runtime, charts, theming, flows, gotchas, and a cheatsheet.
- **[Capability Reference](../Canvas-Reference.md)** — terse index of every capability.
- **Examples** — `PoshUI\Examples\` ships runnable demos: a workflow demo with live progress and retry, a
  dashboard demo with an off-gate ScriptCard, a cascading-dropdown demo, and a layout demo (tabs, menu,
  splitter, viewbox).
- **Real-world apps** — two complete tools, not mockups, both usable without administrator rights:
  - **`Endpoint-TaskOrchestrator.ps1`** — Windows Task Scheduler triage and orchestration. Surfaces every task
    that failed, missed runs or went stale in *one* view (Task Scheduler shows Last Run Result one task at a
    time, as a raw hex code, which this decodes), runs a chosen sequence of tasks with retry/timeout/skip via
    the engine workflow, and flags non-Microsoft and SYSTEM+Highest-privilege tasks for persistence triage.
  - **`Endpoint-SupportDesk.ps1`** — end-user self-service. Plain-language device health, one-click fixes for
    the common helpdesk calls (privilege-split, so cache/profile fixes work unelevated), and a **support
    bundle** that collects diagnostics into a zip on the Desktop to attach to a ticket. Nothing is uploaded.
