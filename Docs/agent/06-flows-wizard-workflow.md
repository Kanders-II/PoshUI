# 06 — Flows: Wizard & Workflow

Two higher-level patterns sit on top of pages: a **Wizard** (multi-page form with validation gating) and a
**Workflow** (a sequential task runner with progress, optionally surviving reboots).

---

## Wizard (multi-page form)
Use multiple `Add-UICanvasPage`s with `-Navigation Sidebar` (or `Top`), a step indicator, and gated Next/Back.

### Step indicator — `Add-UICanvasWizardSteps`
```
Add-UICanvasWizardSteps -Steps <string[]> [-Current <string>]
```
Renders the numbered step rail. Put it on each page (or in a shared shell).

### Navigation + validation — `Add-UICanvasWizardNav`
```
Add-UICanvasWizardNav [-Next <pageTitle>] [-Back <pageTitle>]
  [-Require <string[]>]        # names of controls that must be non-empty before Next
  [-Validate { <returns error string or $null> }]
  [-NextLabel <s>] [-BackLabel <s>] [-NoBack]
```
- `-Require @('userName','email')` blocks Next until those controls have values.
- `-Validate { if ((Get-UICanvasValue AdminPass).Length -lt 12) { 'Password must be 12+ chars.' } }` — return an
  error **string** to block (it's shown to the user) or `$null`/nothing to allow.
- `-NoBack` locks the page once Next succeeds (no returning).

```powershell
New-PoshUICanvas -Title 'Setup' -Navigation Sidebar | Out-Null

Add-UICanvasPage -Title 'Account' -Layout VStack | Out-Null
Add-UICanvasWizardSteps -Steps @('Account','Network','Review') -Current 'Account'
Add-UICanvasTextBox -Name userName -Placeholder 'User name'
Add-UICanvasPassword -Name pass
Add-UICanvasWizardNav -Next 'Network' -Require @('userName') -Validate {
    if ((Get-UICanvasValue userName) -notmatch '^\w{3,}$') { 'User name must be 3+ word characters.' }
}

Add-UICanvasPage -Title 'Network' -Layout VStack | Out-Null
Add-UICanvasWizardSteps -Steps @('Account','Network','Review') -Current 'Network'
# ...fields...
Add-UICanvasWizardNav -Next 'Review' -Back 'Account'

Add-UICanvasPage -Title 'Review' -Layout VStack | Out-Null
Add-UICanvasButton 'Finish' -Style Accent -Action { Submit-UICanvas }

Show-PoshUICanvas | Out-Null
```

---

## Workflow (sequential task runner)
Build a list of steps, then render the runner. Each step's scriptblock runs in order; the runner shows
per-step status and overall progress.

### Define steps — `Add-UICanvasWorkflowStep`
```
Add-UICanvasWorkflowStep -Name <string> [-Detail <string>] -Script { ... }
  [-ExpectedSeconds <int>]  # typical duration; drives a weighted overall % / ETA when set on every step
  [-Retry <int>]            # (-Engine only) retry the step this many times on failure before failing the run
  [-TimeoutSeconds <int>]   # (-Engine only) fail the step if it runs longer than this
  [-SkipWhen <condition>]   # (-Engine only) PowerShell condition string; truthy at run time -> step is skipped
```
Call once per step, in order, **before** `Add-UICanvasWorkflow`.

### Render the runner — `Add-UICanvasWorkflow`
```
Add-UICanvasWorkflow [-Name <id='wf'>] [-StartLabel <s>] [-StartIcon <s>] [-ShowLog]
  [-Engine]               # PREFERRED: run steps on the engine's workflow executor (own runspace, off the
                          #   serialized gate) — live progress/elapsed while a step runs, plus retry/timeout/
                          #   skip and engine-managed reboot-resume. UI renders identically either way.
  [-AutoStart]            # run automatically shortly after the window opens (no click)
  [-LockNavigation]       # block backtracking once the first step completes
  [-LockOnStart]          # block backtracking immediately when the run begins
  [-StepsHeight <int>]    # scroll the step rows within a fixed pixel height
  [-NoStartButton]        # don't render Start; place it yourself
  [-NoHeader]             # don't render the Status/% header + gauge; place them yourself
  [-StateFile <path>]     # persist progress for reboot-resume
```
Without `-Engine`, the runner executes as one composed action on the app's shared (serialized) runspace: it
works, but a long step blocks other actions and skips `-Refresh` ticks until it finishes. With `-Engine`, steps
run on the engine's workflow executor in **its own runspace**, so the rest of the UI stays live throughout.

### The `$wf` step context (`-Engine`)
Inside every step script (also available as `$PoshUIWorkflow`):
- `$wf.UpdateProgress(<0-100>, '<message>')` — live per-step progress (drives the overall gauge smoothly).
- `$wf.WriteOutput('<text>', 'INFO'|'WARN'|'ERR')` — append to the step's output/log.
- `$wf.GetValue('<controlName>')` / `$wf.SetValue('<controlName>', <v>)` — read/write canvas controls.
- `$wf.SetData('<key>', <v>)` / `$wf.GetData('<key>')` — pass data between steps.
- `$wf.SkipTask('<reason>')` — skip the current step.
- `$wf.RequestReboot('<reason>')` — save state and stop so the run resumes after a restart (see reboot-resume).

All the usual runtime cmdlets (`Set-UICanvasValue`, `Set-UICanvasProperty`, `Get-UICanvasValue`, …) also work
inside `-Engine` step scripts — the executor's runspace is seeded with them. Every form field on the canvas is
additionally available as a plain `$variable` (snapshot taken at run start).

### Per-step / overall state the runner publishes
Inside step scripts (and elsewhere) these named values are available/updated automatically (prefix = workflow `-Name`):
- `<wf>_status` — current status text · `<wf>_pct` — overall percent · `<wf>_gauge` — overall progress bar.
- `<wf>_start` / `<wf>_end` — run start/end timestamps; per-step `<step>_elapsed` / `<step>_remaining`.

A step script reports progress via `$wf.UpdateProgress` (or just by doing work); throw an error to fail the
step. Example (`-Engine`, with live sub-progress and a retry):
```powershell
Add-UICanvasWorkflowStep -Name 'Download payload' -Detail 'Fetch installer' -ExpectedSeconds 30 -Retry 2 -Script {
    1..10 | ForEach-Object { $wf.UpdateProgress($_ * 10, "downloading $($_*10)%"); Start-Sleep 1 }
}
Add-UICanvasWorkflowStep -Name 'Install' -Detail 'Silent install' -ExpectedSeconds 60 -Script {
    $wf.WriteOutput('installing...', 'INFO')
    # ... real work ...
}
Add-UICanvasWorkflowStep -Name 'Configure' -ExpectedSeconds 10 -SkipWhen '$env:SKIP_CONFIG -eq "1"' -Script {
    $wf.SetValue('statusLabel', 'configured')
}

Add-UICanvasWorkflow -Engine -Name deploy -AutoStart -LockOnStart -ShowLog
```

### Reboot-resume
Pass `-StateFile <path>`. The runner persists which step it reached; if the machine reboots mid-run (e.g. an OS
deployment), relaunching the app with the same `-StateFile` **resumes past the completed steps** and re-seeds
the gauge to the prior percent. Use `-LockNavigation`/`-LockOnStart` to prevent going back during a destructive run.

### Custom layout (`-NoHeader` / `-NoStartButton`)
For a bespoke final-execution screen, suppress the built-in header/start button and place the pieces yourself:
- With `-NoHeader`, add your own controls named `<wf>_status`, `<wf>_pct`, `<wf>_gauge` (a progress bar) so the
  runner can drive them.
- With `-NoStartButton` (legacy path), trigger the run from your own button via
  `$Global:_PoshUICanvas.WorkflowActions[<name>]`. With `-Engine`, the engine wires the click of the button
  named `<wf>_run` automatically — or use `-AutoStart`.

---

## ScriptCard (on-demand runner card — dashboards)
`Add-UICanvasScriptCard` is a one-cmdlet dashboard card that runs a script **off the UI gate** (its own
runspace via `Start-UICanvasAsync`) with a title, a live status (Idle → Running → Done/Failed), a Run button,
and a console that streams the script's output as it runs. A long task never freezes the rest of the app —
live `-Refresh` cards keep ticking.
```
Add-UICanvasScriptCard <Title> -Script { ... } [-Name <id>] [-Detail <s>]
  [-RunLabel <s='Run'>] [-RunIcon <s='play'>] [-OutputHeight <int=160>] [-Properties <hashtable>]
```
```powershell
Add-UICanvasScriptCard 'Collect logs' -Detail 'Zips the last 24h of logs' -Script {
    "starting..."
    # every output line streams into the card's console as it runs;
    # Set-UICanvasValue / Set-UICanvasProperty also work here (own runspace, bridge cmdlets seeded)
    "done"
}
```
Generated control names (drive them yourself if needed): `<name>_run` (button), `<name>_status` (label),
`<name>_out` (console).
