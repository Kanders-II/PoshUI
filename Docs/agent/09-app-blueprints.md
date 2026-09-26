# 09 — App Blueprints by Objective

File 00 explains the paradigms. This file applies them: for each common **objective**, the paradigms to
combine, the page shape, where each piece of work runs, the controls to reach for, and the traps specific to
that kind of app. Pick the closest blueprint, say which one you are following, and adapt it. Most real apps
are one primary blueprint with a second one bolted on (a monitor with a toolbox page, an explorer with an
export job).

Every blueprint's **content** — the tools, fixes, profiles, steps or queries it works on — is staged by an
admin as folders with descriptors, not written into the code; file 11 shows the shape for each blueprint
and the back-end contract every operation follows.

Every blueprint assumes the golden rules and file 00. The three execution lanes are written as
**gate** (`-Action`/`-OnChange`/`-Refresh`), **async** (`Start-UICanvasAsync`) and **engine**
(`Add-UICanvasWorkflow -Engine`).

The sketches show one page each and assume the app around them:
```powershell
Import-Module .\PoshUI\PoshUI.Canvas\PoshUI.Canvas.psd1 -Force
New-PoshUICanvas -Title 'My tool' -Theme Dark -Width 1100 -Height 760 -MinWidth 900 -MinHeight 600 | Out-Null
Set-UITheme -Preset Slate -Accent Sky | Out-Null     # not optional in practice - see below
# ... pages ...
Show-PoshUICanvas | Out-Null
```
Two layout facts every blueprint depends on, both checked by rendering these sketches:
- **Call `Set-UITheme`.** Without it the base dark palette draws `-Style Accent` buttons pale grey with white
  text, which is unreadable.
- **Grids and consoles need room.** In a `VStack` they get no height: a DataGrid collapses to its header row
  and a console to one line. Either give them `-Height`, or make the page `-Layout Dock` and put them **last**
  so they fill. On a Dock page every other child needs `-Properties @{ Dock = 'Top' }` (or `Bottom`) —
  undocked children go to the **left**.

| # | Objective | Primary paradigms | Typical shape |
|---|---|---|---|
| 1 | Form that does something | Reactive state, collect-then-act | One page |
| 2 | Admin toolbox | Multi-page, ScriptCards, async | Sidebar or rail, one page per area |
| 3 | Live monitor | Reactive state, polling, charts | One dense page, optional drill-down |
| 4 | API browser | Async, data-driven, master/detail | Search bar, grid, detail pane |
| 5 | Data explorer / report | Data-driven, declarative filters | Filters, grid, detail, export |
| 6 | Guided setup | Wizard, validation gating | Steps, review, run |
| 7 | Long-running job | Engine pipeline | Configure, run with live log, summary |
| 8 | Self-service kiosk | Event-driven, big targets | One page of large cards |
| 9 | Settings editor | Reactive state, diff-then-save | Grouped form, review changes |
| 10 | File / batch processor | Async batch, data-driven results | Pick, preview, process, results |
| 11 | Launcher / hub | Data-driven (Repeater), search | Card grid |
| 12 | Log / event viewer | Async streaming, filters | Toolbar, streaming console |

---

## 1. Form that does something

**Objective.** Collect a handful of inputs, validate them, then perform one action: create an account,
request access, register a device, file a ticket.

- **Paradigms:** reactive state for the inputs and a live summary line; collect-then-act (00 §4.5).
- **Shape:** one `VStack` page. Inputs grouped in a card, a one-line summary bound to state, one accent button.
- **Lanes:** validation and the confirm dialog on the gate; the action itself in async if it touches the
  network or takes more than a moment.
- **Controls:** `Add-UICanvasTextBox`, `Add-UICanvasDropdown`, `Add-UICanvasCheckbox`, `Add-UICanvasDatePicker`,
  `Add-UICanvasPassword`, `Add-UICanvasLabel -Bind '…{key}…'`, `Show-UICanvasDialog`.

```powershell
Add-UICanvasPage -Title 'New account' -Layout VStack -Spacing 12 | Out-Null
New-UICanvasState @{ user = ''; dept = 'IT'; notify = $true }
Add-UICanvasCard -Layout VStack -Spacing 10 -Children {
    Add-UICanvasTextBox -Name user -Bind 'user' -Placeholder 'User name'
    Add-UICanvasDropdown -Name dept -Bind 'dept' -Choices 'IT', 'HR', 'Finance'
    Add-UICanvasCheckbox 'Email their manager' -Name notify -Bind 'notify'
}
Add-UICanvasLabel -Bind 'Will create {user} in {dept}.'
Add-UICanvasButton 'Create' -Style Accent -Action {
    $u = Get-UICanvasState user
    if ($u -notmatch '^[a-z][a-z0-9.]{2,19}$') {
        Show-UICanvasToast 'User name: 3-20 lowercase letters, digits or dots.' -Severity warning; return
    }
    if (-not (Show-UICanvasDialog -Message "Create the account '$u'?" -Title 'Confirm')) { return }
    Start-UICanvasAsync {
        try {
            $u = Get-UICanvasState user
            # ... create the account ...
            Show-UICanvasToast "Created $u" -Severity success
        } catch { Show-UICanvasToast $_.Exception.Message -Severity error }
    }
}
```

**Traps.** Validating only in the action lets people fill in a whole form before learning the first field was
wrong — validate on `-OnChange` for anything with a format. Never leave the button enabled while the action
runs: disable it (`Set-UICanvasProperty btn Enabled $false`) and re-enable it in the async block's `finally`.

---

## 2. Admin toolbox

**Objective.** One window that gathers the tasks a team does every day — reset a password, clear a print
queue, check a mailbox, restart a service — instead of a folder of scripts.

- **Paradigms:** multi-page app; each task is an on-demand tool; async for anything remote.
- **Shape:** `New-PoshUICanvas -Navigation Sidebar` (or a custom rail), one page per **area** (Users,
  Devices, Printing), not one per task. Each page is a `Grid` of task cards.
- **Lanes:** `Add-UICanvasScriptCard` for tools whose output the user needs to read (it streams output and
  runs off the gate); async for quick remote actions that end in a toast.
- **Controls:** `Add-UICanvasScriptCard`, `Add-UICanvasCard -Action`, `Show-UICanvasFlyout` for per-item
  action menus, `Add-UICanvasBanner` for "you are not elevated", `Add-UICanvasShortcut` for power users.

**Design notes.**
- Put the target (computer name, user) in **one** shared input at the top of each page, bound to state, so
  every tool on the page acts on the same thing.
- Detect elevation once at start and disable, with a tooltip saying why, every tool that needs it — do not
  let a user discover it half-way through.
- Destructive tools confirm with a dialog that names the target.

**Traps.** Twenty tools on one page — group them. A tool that silently does nothing when the target is
unreachable — test reachability first and say so.

---

## 3. Live monitor / dashboard

**Objective.** Show the current state of a set of things — machines, services, queues, sensors — and make
problems obvious at a glance.

- **Paradigms:** reactive state as the single source; polling; charts and status cards bound to state.
- **Shape:** one dense page: a row of metric cards (the headline numbers), charts for trends, a grid or status
  card for the items, a small "last updated" line. Optional drill-down page per item.
- **Lanes:** one **ticker** on the gate that must stay cheap; anything slow (many machines, remote queries)
  moves to async, with the ticker only kicking it off.
- **Controls:** `Add-UICanvasMetricCard`, `Add-UICanvasChartCard -Bind`, `Add-UICanvasStatusCard`,
  `Add-UICanvasProgressBar -Bind` (with `Glow`/`GlowPulse` for "live"), `Add-UICanvasBadge`.

```powershell
New-UICanvasState @{ cpu = 0; mem = 0; stamp = '' }
Add-UICanvasProgressBar -Bind 'cpu' -Maximum 100
Add-UICanvasLabel -Bind 'CPU {cpu}%  |  Memory {mem}%  |  updated {stamp}'
# The ticker: hidden, cheap, and throttled - the page re-registers it on every visit.
Add-UICanvasLabel -Name ticker -Visible $false -Refresh 5 -Label {
    if ($global:MonLast -and ((Get-Date) - $global:MonLast).TotalSeconds -lt 4) { return '' }
    $global:MonLast = Get-Date
    $os = Get-CimInstance Win32_OperatingSystem
    Set-UICanvasState mem ([int](100 - 100 * $os.FreePhysicalMemory / $os.TotalVisibleMemorySize))
    Set-UICanvasState cpu ([int](Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average)
    Set-UICanvasState stamp (Get-Date -Format 'HH:mm:ss')
    ''
}
```

**Design notes.** Colour only means something if it is rare: neutral by default, amber and red for the few
things that need attention. Show *when* the data was fetched. Pick the slowest refresh the job allows —
5–30 seconds, not 1.

**Traps.** A ticker that queries twenty servers on the gate freezes the window every tick. Timers pile up
when the user revisits the page — the throttle above is not optional on a multi-page app.

---

## 4. API browser

**Objective.** A native front end over a REST API: look things up, inspect them, occasionally change them.

- **Paradigms:** async for every call; data-driven grid; master/detail.
- **Shape:** a search bar (input + button) above a grid; selecting a row fills a detail pane beside or below
  it. Writes (create, update, delete) open a form or dialog.
- **Lanes:** everything network-bound in async. Selection handling on the gate, which then starts an async
  fetch for the detail.
- **Controls:** `Add-UICanvasDataGrid -BindItems -OnChange`, `Add-UICanvasMarkdown` or labels for the detail,
  `Add-UICanvasProgressRing` while loading, `Add-UICanvasPassword` for a key prompt.

File 00 §6 is the full recipe (TLS, JSON depth, secrets, paging, rate limits). The shape of the detail fetch:

```powershell
Add-UICanvasDataGrid -Name grid -Columns id, name, status -BindItems 'rows' -OnChange {
    Start-UICanvasAsync {
        try {
            $row = Get-UICanvasValue -Name grid
            if (-not $row) { return }
            [Net.ServicePointManager]::SecurityProtocol = 'Tls12'
            $d = Invoke-RestMethod "https://api.example.com/v1/items/$([uri]::EscapeDataString($row.id))"
            Set-UICanvasState detail ($d | ConvertTo-Json -Depth 5)
        } catch { Show-UICanvasToast $_.Exception.Message -Severity error }
    }
}
Add-UICanvasConsole -Bind 'detail'
```

**Traps.** Fetching every detail up front instead of on selection. Losing a user's typed search when a slow
response for an *older* search arrives last — stamp each request and ignore stale ones.

---

## 5. Data explorer / report

**Objective.** Let someone slice a dataset — inventory, audit results, licence usage, a CSV export — filter
it, look at one record, and take the result away.

- **Paradigms:** declarative filters (cascades); data-driven grid; master/detail; export as a job.
- **Shape:** a filter strip across the top (dropdowns that cascade, a text filter, a date range), the grid
  filling the rest (`Dock` page, grid last), a detail pane or flyout, an Export button.
- **Lanes:** loading the source in async once; filtering on the gate **only** if the set is small (a few
  thousand rows), otherwise async; export in async.
- **Controls:** `Add-UICanvasDropdown -DependsOn -OptionsScript`, `Add-UICanvasTextBox -OnChange`,
  `Add-UICanvasDatePicker`, `Add-UICanvasDataGrid`, `Add-UICanvasChartCard` for a summary, `Select-UICanvasFolder`.

```powershell
Add-UICanvasPage -Title 'Services' -Layout Dock | Out-Null      # Dock: the grid (last) fills the page
New-UICanvasState @{ filter = ''; rows = @(); detail = 'Select a row.' }
Add-UICanvasPanel -Layout HStack -Spacing 8 -Properties @{ Dock = 'Top'; Margin = '0,0,0,10' } -Children {
    Add-UICanvasTextBox -Name filter -Bind 'filter' -Placeholder 'Name contains...' -Width 320
    Add-UICanvasButton 'Search' -Style Accent -Action {
        Start-UICanvasAsync {
            try {
                $f = Get-UICanvasState filter
                $rows = Get-Service | Where-Object { $_.DisplayName -like "*$f*" } |
                    Select-Object Name, DisplayName, @{ n = 'Status'; e = { "$($_.Status)" } }
                Set-UICanvasState rows @($rows)
            } catch { Show-UICanvasToast $_.Exception.Message -Severity error }
        }
    }
}
Add-UICanvasLabel -Bind '{detail}' -Properties @{ Dock = 'Bottom'; Margin = '0,10,0,0' }
Add-UICanvasDataGrid -Name grid -Columns Name, DisplayName, Status -BindItems 'rows' -OnChange {
    $r = Get-UICanvasValue -Name grid
    if ($r) { Set-UICanvasState detail "$($r.DisplayName) is $($r.Status)." }
}
```
Order matters on a Dock page: the detail line is declared **before** the grid so the grid stays the last
child and takes the remaining space.

**Design notes.** Convert enums and dates to strings before they reach the grid (as `Status` above) — they
display and sort predictably. Show the row count next to the filters. Export exactly what is on screen, and
say where the file went.

**Traps.** Putting the whole dataset in state and filtering it on every keystroke on the gate. Grids with
thirty columns — pick the six that matter and put the rest in the detail pane.

---

## 6. Guided setup

**Objective.** Walk someone who does this rarely through a configuration with several dependent choices:
onboarding a site, preparing a machine, setting up a service.

- **Paradigms:** wizard; validation gating; declarative cascades; collect-then-act.
- **Shape:** one page per step with `Add-UICanvasWizardSteps` and `Add-UICanvasWizardNav`; a **Review** page
  that restates every choice in plain language; a final page that runs the work (blueprint 7 if it is long).
- **Lanes:** validation on the gate (`-Validate`, `-Require`); the final action in async or the engine.
- **Controls:** everything in file 06's wizard section; `Lock-UICanvasNavigation` once work starts.

**Design notes.** Order steps from the choice that constrains most to the one that constrains least. Default
every field to the common answer. Explain *why* a field matters in one line under it, not in a help page.

**Traps.** Doing work between steps — nothing should change until the user confirms on Review. Letting Back
return to a step after irreversible work has started.

---

## 7. Long-running job

**Objective.** Run a sequence of steps that takes minutes — a migration, a bulk change, a report build, a
backup, an install — with visible progress, and cope with a step failing.

- **Paradigms:** engine-native pipeline; collect-then-act; UI/logic separation.
- **Shape:** a short configuration page (or none), a run page with the workflow runner and its log, and a
  summary of what happened.
- **Lanes:** **engine** for the steps (`Add-UICanvasWorkflow -Engine`); each step reports through `$wf`.
- **Controls:** `Add-UICanvasWorkflowStep -Retry -TimeoutSeconds -SkipWhen -ExpectedSeconds`,
  `Add-UICanvasWorkflow -Engine -ShowLog -LockOnStart`, and `-StateFile` only if the job can span a restart.

```powershell
Add-UICanvasWorkflowStep -Name 'Export' -Detail 'Read the source' -ExpectedSeconds 20 -Retry 2 -Script {
    $wf.UpdateProgress(10, 'connecting')
    # ... read the source; throw to fail the step ...
    $wf.SetData('count', 1250)
}
Add-UICanvasWorkflowStep -Name 'Transform' -ExpectedSeconds 40 -Script {
    $wf.WriteOutput("processing $($wf.GetData('count')) records", 'INFO')
}
Add-UICanvasWorkflowStep -Name 'Publish' -ExpectedSeconds 15 -TimeoutSeconds 120 -Script { }
Add-UICanvasWorkflow -Engine -Name job -ShowLog -LockOnStart
```

**Design notes.** Give every step an `-ExpectedSeconds` so the overall bar is honest. Make each step safe to
run twice, because a retry will. End with a summary that says what changed, not just "done".

**Traps.** Running the steps as a plain action on the gate — the window freezes for the whole job. Relying
on script variables inside steps — pass data with `$wf.SetData`/`GetData`.

---

## 8. Self-service kiosk

**Objective.** A tool for **non-technical users** to fix their own common problems: clear the cache, map a
drive, reset a printer, collect logs for the help desk.

- **Paradigms:** event-driven; one action per card; plain language.
- **Shape:** one page, a `Grid` of large cards, each with an icon, a plain title ("My printer isn't
  printing"), one sentence, and the whole card clickable. No sidebar, no jargon, no grids.
- **Lanes:** each fix in async, with a progress ring on the card and a toast in plain words at the end.
- **Controls:** `Add-UICanvasCard -Action` with `HoverScale`, `Add-UICanvasIcon`, `Add-UICanvasProgressRing`,
  `Show-UICanvasToast`, `Show-UICanvasDialog` before anything they would notice (closing their apps).

**Design notes.** Assume a standard user: every fix must work unelevated, or not be offered. Say what will
happen before it happens ("This will close Outlook"). Offer "Send details to the help desk" as the last card.

**Traps.** Error messages written for administrators. Fixes that need a reboot without saying so.

---

## 9. Settings editor

**Objective.** Let someone change a configuration — an app's config file, registry settings, a policy
set — safely, without editing the file by hand.

- **Paradigms:** reactive state as the working copy; declarative cascades; diff-then-save.
- **Shape:** tabs or expanders grouping the settings, each field with a one-line explanation; a footer with
  **Review changes** and **Save**; a review that lists only what changed, old → new.
- **Lanes:** load and save on the gate for a local file; async for anything remote.
- **Controls:** the input controls with `-Bind`, `Add-UICanvasTabs`, `Add-UICanvasExpander`,
  `Add-UICanvasNumberBox` with `-Minimum/-Maximum`, `Show-UICanvasDialog` for the diff.

**Design notes.** Load the original into state twice — `original` and `current` — so the diff is a
comparison, not a guess. Back up before writing, and write atomically (to a temp file, then move). Validate
the whole set, not just each field, before saving.

**Traps.** Saving on every change. Losing unknown keys from the file because the form only knows the ones it
shows — merge changes into what was loaded, never regenerate the file from the form.

---

## 10. File / batch processor

**Objective.** Apply an operation to many files or items — rename, convert, compress, hash, import — and
show what happened to each.

- **Paradigms:** collect-then-act; async batch; data-driven results.
- **Shape:** pick the source (`Add-UICanvasFolderPicker`) and options → **Preview** fills a grid with what
  would happen → **Run** processes them, updating each row's status and an overall progress bar → a
  summary with counts and a way to retry the failures.
- **Lanes:** preview and run in async. Update state every N items, not every item, for large batches.
- **Controls:** `Add-UICanvasFolderPicker`, `Add-UICanvasFilePicker`, `Add-UICanvasDataGrid -BindItems`,
  `Add-UICanvasProgressBar -Bind`, `Add-UICanvasCheckbox` for "dry run".

**Design notes.** Default to a dry run for anything destructive. Never stop the batch on one failure: record
it on the row and continue. Keep the results grid after the run so the user can see exactly which items failed.

**Traps.** Pushing 10,000 state updates — batch them. Reading the whole folder recursively on the gate to
count files.

---

## 11. Launcher / hub

**Objective.** A single front door to other tools, scripts, websites and documents, often driven by a
config file so it can change without code changes.

- **Paradigms:** data-driven (`Add-UICanvasRepeater` over a config list); search.
- **Shape:** optional search box, then a `Grid` or `Wrap` of cards generated from `config.psd1`: icon, name,
  one line, click to launch. Optionally grouped by category with tabs.
- **Lanes:** launching on the gate (`Start-Process` returns immediately).
- **Controls:** `Add-UICanvasRepeater`, `Add-UICanvasCard -Action` with `HoverScale`,
  `Add-UICanvasAutoSuggest` for search, `Add-UICanvasHyperlink` for web targets.

```powershell
$items = (Import-PowerShellDataFile (Join-Path $PSScriptRoot 'config.psd1')).Items
Add-UICanvasPanel -Layout Wrap -Spacing 12 -Children {
    Add-UICanvasRepeater -Items $items -Template {
        param($it)
        $target = $it.Target -replace "'", "''"
        Add-UICanvasCard -Width 220 -Layout VStack -Spacing 4 -Properties @{ HoverScale = 1.04 } `
            -Action ([scriptblock]::Create("Start-Process '$target'")) -Children {
                Add-UICanvasLabel $it.Name -FontWeight Bold
                Add-UICanvasLabel $it.Blurb -FontSize 11
            }
    }
}
```

**Design notes.** The config file is the product — keep it simple enough for a non-developer to edit. Check
each target exists at start and grey out the ones that don't.

**Traps.** Referencing `$it` inside the card's `-Action` — actions run in the engine and `$it` doesn't exist
there, which is why the sketch bakes the target into the script text (it comes from **your** config, not
from user input).

---

## 12. Log / event viewer

**Objective.** Watch a log file or event stream as it grows, filter it, and spot problems.

- **Paradigms:** async streaming; declarative filters; pause/resume via state.
- **Shape:** a `Dock` page: a toolbar (source, filter text, level dropdown, Pause toggle, Clear) docked top,
  the console filling the rest (last child).
- **Lanes:** one async pipeline that follows the source for the life of the window; filters read from
  state on each line so changing them needs no restart.
- **Controls:** `Add-UICanvasConsole`, `Set-UICanvasProperty <console> AppendLine <text>` (appends and
  scrolls), `Add-UICanvasToggle -Bind 'paused'`, `Add-UICanvasTextBox -Bind 'filter'`.

```powershell
Add-UICanvasPage -Title 'Log' -Layout Dock | Out-Null
New-UICanvasState @{ paused = $false; filter = '' }
$log = 'C:\ProgramData\MyApp\app.log' -replace "'", "''"
Add-UICanvasPanel -Layout HStack -Spacing 8 -Properties @{ Dock = 'Top'; Margin = '0,0,0,10' } -Children {
  Add-UICanvasTextBox -Name filter -Bind 'filter' -Placeholder 'Filter' -Width 260
  Add-UICanvasToggle -Name paused -Bind 'paused' -Properties @{ VAlign = 'Center' }
  Add-UICanvasLabel 'Pause' -Properties @{ VAlign = 'Center' }
  Add-UICanvasButton 'Follow' -Name follow -Style Accent -Action ([scriptblock]::Create(@"
    Set-UICanvasProperty follow Enabled `$false -Quiet
    Start-UICanvasAsync {
        try {
            Get-Content -LiteralPath '$log' -Tail 200 -Wait | ForEach-Object {
                if (Get-UICanvasState paused) { return }
                `$f = Get-UICanvasState filter
                if (-not `$f -or `$_ -like "*`$f*") { Set-UICanvasProperty logView AppendLine `$_ -Quiet }
            }
        } catch { Show-UICanvasToast `$_.Exception.Message -Severity error }
    }
"@))
}
Add-UICanvasConsole -Name logView       # last child: fills the page
```
Closing the window stops the pipeline, so the follow loop needs no exit condition. Disabling the button stops
a second click starting a second follower.

**Design notes.** Cap what the console holds (clear it periodically, or show the last N lines) — an
unbounded text box slows down over hours. Highlight by level with a prefix (`[ERROR]`) since the console is
one colour.

**Traps.** Starting a second follower each time the button is clicked — disable the button once following.
Polling the file on a `-Refresh` timer on the gate instead of streaming in async.

---

## Combining blueprints

| Combination | How it usually looks |
|---|---|
| Monitor + toolbox | Monitor page first; clicking an item opens a flyout of actions from the toolbox |
| Explorer + batch | The explorer's current selection becomes the batch processor's input |
| Setup + long job | Wizard pages collect; the final page is the engine workflow |
| Launcher + kiosk | Self-service fixes as cards alongside launch targets, for end users |
| API browser + settings | The detail pane opens an editor for the selected record, with diff-then-save |

Keep **one** primary blueprint per app and treat the others as features of it. An app that is equally a
monitor, a toolbox and an explorer is three apps in one window, and reads like it.
