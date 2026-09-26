# 00 — Design Paradigms: How to Think About a PoshUI App

Read this before the API. Files 01–16 say *what each cmdlet does*. This file says *how to shape an app*:
which module to reach for, which structural paradigm fits the job, how far an app can reach, and the
architectures that keep a real tool maintainable. **Canvas is the default choice** for anything new; the
other three modules are covered here only so you know when they are the better fit.

---

## 1. The mental model (everything else follows from this)

```
 your .ps1  ──describes──►  definition (pages → controls)  ──JSON──►  PoshUI.exe (WPF engine)
 (authoring, PS 5.1)                                                  │
                                                                      ├─ renders the UI
                                                                      └─ hosts a PowerShell runspace
                                                                         that runs your -Action /
                                                                         -OnChange / -Refresh blocks
```

Three consequences an author must internalise:

1. **The script describes; the engine renders.** `Add-UICanvas*` calls only build a definition. Nothing is
   on screen until `Show-PoshUICanvas`. You never touch WPF objects directly.
2. **Behaviour runs in the engine, not in your script.** Action scriptblocks execute in the engine's
   runspace. They do not see your script's functions or variables. Anything an action needs is dot-sourced
   inside it, baked in with `[scriptblock]::Create`, or passed through reactive state or a file.
3. **There are three execution lanes, and choosing the lane is a design decision:**

| Lane | Runs | Use for | Never use for |
|---|---|---|---|
| **Gate** (the UI runspace) | `-Action`, `-OnChange`, `-Refresh`, `-OptionsScript` | Reading and writing controls, quick logic, navigation, dialogs | Anything slow: it is serialized, so one slow action freezes every click and every live card |
| **Async** (a fresh runspace per call) | `Start-UICanvasAsync { }` | API calls, scans, anything over ~100 ms | Relying on variables or `$global:` from the gate: it starts empty |
| **Engine executor** | `Add-UICanvasWorkflow -Engine` steps, `Add-UICanvasScriptCard` | Multi-step operations with progress, retry, timeouts, reboot-resume | Short interactive logic |

---

## 2. Choosing the module

| If the app is… | Use | Why |
|---|---|---|
| **Anything custom** — a tool, a console, a dashboard with interaction, a branded front end | **Canvas** | Free-form layout, reactive state, runtime cmdlets, three execution lanes |
| A linear "fill in the form, then run" setup | Canvas (wizard pattern, §3.6) **or** the Wizard module | Canvas if it needs anything beyond steps; Wizard if the steps are all it needs |
| A shelf of existing `.ps1` tools with parameter dialogs | Dashboard (`Add-UIScriptCard -ScriptPath`) | It AST-parses each script's `param()` block and builds the dialog for you |
| An unattended multi-task run with approval gates | Canvas `-Engine` workflow **or** the Workflow module | Canvas if it lives inside a larger app |

**Never mix the APIs in one script.** Canvas (`Add-UICanvas*`) and the original modules (`Add-UIStep`,
`Add-UITextBox`, `Add-UIMetricCard`, …) are separate cmdlet sets that build different definitions.

### The one paradigm Canvas does not have: script-first
The original modules can generate a UI **from a script**: they AST-parse the `param()` block and map
`[ValidateSet]` to a dropdown, `[switch]` to a toggle, `[SecureString]` to a password box, `= default` to a
prefilled value, `[Parameter(Mandatory)]` to a required field. The script is the source of truth; the UI is
derived. Canvas is the reverse — the UI is authored, and scripts are attached to it. If the ask is "put a GUI
on these existing scripts without rewriting them", that is the Dashboard ScriptCard, not Canvas.

---

## 3. Canvas structural paradigms

These are not exclusive. A good app usually combines three or four. Pick deliberately for each screen.

### 3.1 Event-driven (imperative)
Controls raise events; handlers read and write other controls by name.
```powershell
Add-UICanvasTextBox -Name host
Add-UICanvasButton 'Ping' -Style Accent -Action {
    $ok = Test-Connection (Get-UICanvasValue -Name host) -Count 1 -Quiet
    Set-UICanvasValue result ($(if ($ok) { 'Online' } else { 'Unreachable' }))
}
Add-UICanvasLabel '' -Name result
```
**Fits:** small forms, one-off buttons. **Breaks down** when many controls must stay consistent — every
handler has to remember every dependent control. That is the signal to move to 3.2.

### 3.2 Reactive state (MVVM-lite) — the default for anything non-trivial
One shared store; controls **bind** to keys; changing a key updates everything bound to it.
```powershell
New-UICanvasState @{ total = 0; selected = 'none' }
Add-UICanvasLabel -Bind 'Selected: {selected} — {total} items'   # computed template, one-way
Add-UICanvasDropdown -Name pick -Bind 'selected' -Choices 'a','b'   # two-way
Add-UICanvasProgressBar -Bind 'total' -Maximum 100
```
Handlers now change **data**, never controls: `Set-UICanvasState total 42`. The UI follows.
- Two-way on inputs, one-way on labels/progress/console/charts, `{key}` templates on labels,
  `-BindItems` for grid rows.
- `Watch-UICanvasState` for side effects. It is registered per page visit and never removed, so prefer
  `-OnChange` on the input when you can.
- It is not full MVVM (no view models, no commands), but it gives the main benefit: **state is the single
  source of truth, the view is a function of it.**

### 3.3 Declarative dependencies
State the relationship, not the plumbing. Cascading options recompute themselves:
```powershell
Add-UICanvasDropdown -Name region -Choices 'US','EU'
Add-UICanvasDropdown -Name dc -DependsOn region -OptionsScript {
    switch (Get-UICanvasValue -Name region) { 'US' { 'us-east-1','us-west-2' } 'EU' { 'eu-west-1' } }
}
```
**Fits:** any field whose options depend on another field. No `-OnChange` wiring, no ordering bugs.

### 3.4 Data-driven / templated UI
The data decides how many controls exist.
- `Add-UICanvasRepeater -Items $servers -Template { param($s) Add-UICanvasCard … }` — one card per item,
  at authoring time.
- `Add-UICanvasDataGrid -BindItems 'rows'` — rows follow a state list at runtime.
- Charts with `-Bind` re-render (with their grow-in animation) when the bound data changes.
- **Master/detail:** a grid's `-OnChange` reads the selected row and fills a detail pane.

### 3.5 Multi-page application (SPA-style)
Pages are screens; navigation is either engine chrome (`New-PoshUICanvas -Navigation Sidebar|Top|Compact`)
or your own (buttons with `-NavigateTo`, `Show-UICanvasPage`, a custom rail in `Add-UICanvasXaml`).
- **A page is rebuilt on every visit.** Its `-Refresh` scriptblocks fire immediately on entry — that is the
  page-load hook.
- **Registrations accumulate.** Timers (`-Refresh`), watchers and shortcuts registered on a page are added
  again on each visit and live until the app closes. Register shortcuts once, and throttle tickers with a
  timestamp in `$global:`. The gate runspace persists for the life of the app, so a `$global:` set in one
  action is visible to the next — but never to your authoring script, and never to an async block.
- Layout choice per page: `Dock` for an app shell or anything with one big grid/console (only the
  **literal last** child fills; earlier children need `-Properties @{ Dock = 'Top' }` or they dock left),
  `VStack` for a scrolling document (grids and consoles in it need an explicit `-Height`), `Grid` with `-ColumnWidths '3*,*'` for proportional dashboards, `Canvas` only for
  pixel-exact designs.

### 3.6 Guided flow (wizard)
`Add-UICanvasWizardSteps` shows progress; `Add-UICanvasWizardNav -Require/-Validate` gates Next until the
page is valid; `Lock-UICanvasNavigation` stops back-tracking once something irreversible starts.
**Fits:** collect → confirm → execute.

### 3.7 Live and asynchronous
- **Polling:** `-Refresh <seconds>` on a label/metric/chart value scriptblock. Keep it to milliseconds of work.
- **Background work:** `Start-UICanvasAsync { }` — its own runspace; runtime cmdlets inside it marshal
  results back to the UI. Wrap it in `try/catch` and report failures with a toast: an unhandled error only
  reaches the engine log.
- **Streaming tools:** `Add-UICanvasScriptCard` runs a script off the gate and streams its output live.
- **Engine-driven motion** such as `Add-UICanvasLabel -Clock` ticks without touching the runspace at all.

### 3.8 Engine-native pipeline
`Add-UICanvasWorkflow -Engine` with `Add-UICanvasWorkflowStep` records: each step has `-Retry`,
`-TimeoutSeconds`, `-SkipWhen`, and `-StateFile` gives reboot-resume. The workflow is **declared**, not
hand-coded; the engine runs it on its own executor and keeps the UI live. **Fits:** installs, provisioning,
remediation — anything that takes minutes and can fail part-way.

### 3.9 Hybrid: XAML islands
`Add-UICanvasXaml -Markup '…'` (or `-Path` to a `.xaml` file) splices raw WPF into a page. `x:Name`'d
elements are registered, so `Get/Set-UICanvasValue` work on them, and `-Actions @{ name = { } }` wires
**button clicks** to scriptblocks. Use it for what the cmdlets don't cover: custom templates, Storyboard
animation, 3D, a bespoke navigation rail.
**Limits:** the root must be a visual element (not a `ResourceDictionary`); no `x:Class` or code-behind
event attributes; only button clicks are wired back; relative paths inside the file don't resolve against
it. Keep islands small and self-contained.

### 3.10 Secondary windows and transient surfaces
`New-UICanvasWindow` + `Show-UICanvasWindow` for a second window; `Show-UICanvasDialog` (confirm or prompt,
returns a value), `Show-UICanvasFlyout` (anchored choice), `Show-UICanvasToast` (non-blocking feedback).
**Rule:** anything destructive goes through a dialog that says exactly what will be affected.

---

## 4. Architectures around a Canvas app

The paradigms above shape a screen. These shape the **app as a whole**, whatever it does. A single-file app
is fine up to a few hundred lines; past that, apply these. File 09 shows how each kind of app uses them.
Remember who the app is for: an admin builds and **stages** it, and other people — end users or other
admins — **run** it. File 11 covers staged content, the back-end contract and delivery.

### 4.1 Separate the UI from the logic (the most important one)
The Canvas script is **a front end**. The code that does the real work — talks to the API, queries the
machine, transforms the data — lives in plain PowerShell functions that **do not call any UI cmdlet**:
```
 App.ps1 (Canvas)  ── inputs ──►  lib\*.ps1 (plain functions)  ── returns data / reports progress
       ▲                                  │
       └──── state updates, toasts ◄──────┘   (the UI decides how to show results)
```
- Logic functions **return objects**; the action decides what to do with them (`Set-UICanvasState`, a
  toast, a grid refresh). For long work, pass in scriptblock callbacks (`-OnProgress`, `-OnLog`) that the UI
  supplies and a console caller can replace with `Write-Host`.
- Payoff: the logic is testable and runnable from a plain console, reusable by other scripts, and the UI can
  be redesigned without touching it.
- Because actions run in the engine's runspace, each action or async block **dot-sources the logic by
  absolute path**. Bake the path in at authoring time:
  ```powershell
  $lib = (Join-Path $PSScriptRoot 'lib\Inventory.ps1') -replace "'", "''"   # a path may contain a quote
  Add-UICanvasButton 'Refresh' -Action ([scriptblock]::Create(@"
      Start-UICanvasAsync { . '$lib'; Set-UICanvasState rows @(Get-InventoryRows) }
  "@))
  ```

### 4.2 A small, predictable project layout
```
 MyApp\
   MyApp.ps1          # the Canvas front end: pages, bindings, actions — thin
   lib\*.ps1          # logic, one file per concern (API client, data access, formatting)
   config.psd1        # endpoints, paths, thresholds, feature switches — never hardcoded
   assets\            # icons, logos, images
```
Ship the PoshUI modules and `bin\PoshUI.exe` next to it (or require them installed) — nothing else is needed.

### 4.3 Configuration outside the code
Anything that differs between environments or users — URLs, server lists, paths, limits, which features are
on — lives in `config.psd1`, read once with `Import-PowerShellDataFile`. The script never embeds it, and
**secrets never go in it** (see §6).

### 4.4 State is the contract between the UI and the logic
Seed every value the UI shows with `New-UICanvasState`, bind controls to it, and have actions write results
into it. Then the logic never needs to know control names, and a redesigned page keeps working as long as it
binds the same keys.

### 4.5 Collect, then act
For any action with consequences — changes a machine, deletes, sends, bulk-edits — collect and validate
every input first, show a review of exactly what will happen, confirm with `Show-UICanvasDialog`, then run
without stopping to ask. Offer a dry run where it is cheap.

---

## 5. Reach: what a Canvas app can and cannot do

**Rule of thumb: if Windows PowerShell 5.1 can do it as the current user, a button can do it.**

| Reaches | Examples |
|---|---|
| Local machine | Files, registry, services, processes, event logs, scheduled tasks, installers, DISM, BitLocker |
| Management data | WMI/CIM, performance counters, inventory |
| Remote machines | `Invoke-Command`/WinRM, CIM sessions, shares |
| Directory and platforms | ActiveDirectory, Group Policy, ConfigMgr, Hyper-V, Exchange — any module with a 5.1 build |
| Web and data | REST (`Invoke-RestMethod`), SQL via .NET, CSV/JSON/XML, COM (Excel) |
| .NET | Anything in .NET Framework 4.8, plus `Add-Type` C# |

| Edge | What to do |
|---|---|
| Runs as the launching user | Detect elevation; gate write actions; offer an elevated relaunch. Never fail mid-action. |
| Windows PowerShell 5.1 runtime | PS7-only modules won't load in the runspace. Shell out to `pwsh.exe` for those. |
| The gate is serialized | Anything slow goes to async, a ScriptCard, or an `-Engine` workflow. |
| Registrations accumulate across page visits | Throttle tickers, register shortcuts once, prefer `-OnChange` over watchers. |
| Single-user Windows desktop app | Not a web app or service; no incoming webhooks. Reach other machines through remoting and APIs. |

---

## 6. Pattern: wrapping an API

Canvas is a strong fit for a native front end over a REST API. The shape:
```powershell
Add-UICanvasButton 'Load' -Style Accent -Action {
    Start-UICanvasAsync {
        try {
            [Net.ServicePointManager]::SecurityProtocol = 'Tls12'    # 5.1 may default lower
            $owner = [uri]::EscapeDataString((Get-UICanvasValue -Name owner))   # read inputs HERE
            $r = Invoke-RestMethod "https://api.example.com/v1/items?owner=$owner"
            Set-UICanvasState rows @($r.items)                       # grid bound with -BindItems 'rows'
            Show-UICanvasToast "Loaded $(@($r.items).Count)" -Severity success
        } catch { Show-UICanvasToast $_.Exception.Message -Severity error }
    }
}
```
Rules:
- **Every network call goes in `Start-UICanvasAsync`.** Never on the gate.
- **Read user input inside the async block** with `Get-UICanvasValue` / `Get-UICanvasState` — the runtime
  cmdlets work there. Do not paste input into script text with `[scriptblock]::Create`: a quote in the input
  breaks the script, or injects code. Bake in only **your own** constants, such as the path of an API-client
  `.ps1` to dot-source. The async runspace sees none of the gate's variables or functions.
- **Catch and surface every error**; an unhandled one reaches only the engine log.
- **Force TLS 1.2** in each runspace that calls out. **Always pass `ConvertTo-Json -Depth 10`** for request
  bodies — the default depth of 2 silently flattens them.
- **Secrets:** never in the script. Prompt once (`Add-UICanvasPassword` or `Show-UICanvasDialog -Prompt`),
  store as a `PSCredential` with `Export-Clixml` (DPAPI: same user, same machine), or use SecretManagement.
  On-prem Windows-auth APIs: `Invoke-RestMethod -UseDefaultCredentials`.
- **Shapes that work:** browse → detail (grid `-OnChange` fetches the detail), live dashboard (throttled
  `-Refresh` against a cheap endpoint), form → POST, paging with a progress bar, honouring `Retry-After` on 429.

---

## 7. Decision guide

Each row names the blueprint in file 09 that works it through in full.

| The request sounds like… | Build it as | Blueprint |
|---|---|---|
| "Put a GUI on this script" | Dashboard ScriptCard (`-ScriptPath`) — script-first | — |
| "A form that collects X and does Y" | Bound inputs → validation → review → act (§4.5) | 1 |
| "One place for our admin tools" | Multi-page toolbox, ScriptCards, async actions | 2 |
| "Show me the state of N things, live" | Reactive state + throttled `-Refresh` + charts/grids | 3 |
| "Front end for this API" | Async calls, state-bound grid, master/detail (§6) | 4 |
| "Let me search and filter this data" | Filter inputs → async query → grid → detail → export | 5 |
| "Walk someone through setting this up" | Wizard steps with gated Next, review, then act | 6 |
| "Run this long job and show progress" | `-Engine` workflow with retry and a live log | 7 |
| "Something end users can click themselves" | Big-button self-service, plain language, no jargon | 8 |
| "Edit these settings safely" | Load → bound form → diff → confirm → save | 9 |
| "Process a batch of files" | Pick → preview → async batch with per-item results | 10 |
| "A menu that launches our other tools" | Card grid of launch targets, search, favourites | 11 |
| "Watch this log / event stream" | Tail with async polling, filters, highlight, pause | 12 |
| "Make it look impressive" | The visual system and custom shell in file 10; Stagger, HoverScale, Glow; one hero moment (file 16) | — |

## 8. Anti-patterns

- **Logic in the UI.** Actions that contain the real work instead of calling an engine. Untestable, and
  the UI can't be changed without risk.
- **Slow work on the gate.** A `Get-ChildItem -Recurse` or a web call in a plain `-Action`. The window
  freezes and every live card stops.
- **Imperative sprawl.** Twenty `Set-UICanvasValue` calls to keep controls consistent. Use state.
- **Assuming the async block can see your variables.** It can't.
- **Watchers and timers on a page users revisit**, with no throttle.
- **Fixed pixel layout** for something that should resize. Use Dock/Grid/stack layouts and star widths.
- **Decorative motion everywhere.** Animate to show state (working, live, arrived), not to decorate a screen
  someone uses fifty times a day.
- **Inventing cmdlets or parameters.** If the API doesn't cover it, use `Add-UICanvasXaml`.
