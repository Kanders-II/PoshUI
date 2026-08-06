# Flow modes for Canvas — Workflow (sequential step runner) and Wizard (validated paging).
# Implemented entirely on canvas primitives + the in-proc runtime cmdlets (no engine changes):
# steps are rendered as canvas controls and a single composed Action drives them via the bridge.

# ── Path pickers (textbox + native Browse dialog) ──────────────────────────────
function Add-UICanvasFolderPicker {
    <#
    .SYNOPSIS
    A folder path field with a Browse button that opens a native folder dialog and fills the textbox.
    .EXAMPLE
    Add-UICanvasFolderPicker -Name drvPath -Placeholder 'Driver folder...' -Description 'Select the driver folder'
    #>
    [CmdletBinding()]
    param([string]$Name, [string]$Value = '', [string]$Placeholder = 'Select a folder...',
        [string]$Description = 'Select a folder', [string]$ButtonLabel = 'Browse...', [hashtable]$Properties)
    $pkName = $Name   # avoid $Name shadowing inside -Children
    $escName = $pkName -replace "'", "''"; $escDesc = $Description -replace "'", "''"
    $browse = [scriptblock]::Create("`$p = Select-UICanvasFolder -Description '$escDesc'; if (`$p) { Set-UICanvasValue -Name '$escName' -Value `$p }")
    $pp = @{}; if ($Properties) { foreach ($k in $Properties.Keys) { $pp[$k] = $Properties[$k] } }
    Add-UICanvasPanel -Layout Grid -ColumnWidths '*,Auto' -Properties $pp -Children {
        Add-UICanvasTextBox -Name $pkName -Value $Value -Placeholder $Placeholder -Properties @{ Column = 0; Margin = '0,0,8,0' }
        Add-UICanvasButton $ButtonLabel -Icon 'folder' -Style Secondary -Action $browse -Properties @{ Column = 1 }
    }
}

function Add-UICanvasFilePicker {
    <#
    .SYNOPSIS
    A file path field with a Browse button that opens a native open-file dialog and fills the textbox.
    .EXAMPLE
    Add-UICanvasFilePicker -Name wimPath -Title 'Select install.wim' -Filter 'Windows image (*.wim;*.esd)|*.wim;*.esd'
    #>
    [CmdletBinding()]
    param([string]$Name, [string]$Value = '', [string]$Placeholder = 'Select a file...',
        [string]$Title = 'Select a file', [string]$Filter = 'All files (*.*)|*.*',
        [string]$ButtonLabel = 'Browse...', [hashtable]$Properties)
    $pkName = $Name
    $escName = $pkName -replace "'", "''"; $escTitle = $Title -replace "'", "''"; $escFilter = $Filter -replace "'", "''"
    $browse = [scriptblock]::Create("`$p = Select-UICanvasFile -Title '$escTitle' -Filter '$escFilter'; if (`$p) { Set-UICanvasValue -Name '$escName' -Value `$p }")
    $pp = @{}; if ($Properties) { foreach ($k in $Properties.Keys) { $pp[$k] = $Properties[$k] } }
    Add-UICanvasPanel -Layout Grid -ColumnWidths '*,Auto' -Properties $pp -Children {
        Add-UICanvasTextBox -Name $pkName -Value $Value -Placeholder $Placeholder -Properties @{ Column = 0; Margin = '0,0,8,0' }
        Add-UICanvasButton $ButtonLabel -Icon 'folder' -Style Secondary -Action $browse -Properties @{ Column = 1 }
    }
}

# ── Wizard: validated Back/Next paging ─────────────────────────────────────────
function Add-UICanvasWizardNav {
    <#
    .SYNOPSIS
    Renders a wizard footer (Back + Next/Finish). Next is GATED: it validates the current page's required
    fields (and an optional -Validate script) and only advances if valid, showing an inline error otherwise.
    .PARAMETER Next      Page to advance to. Omit for the last page -> the button becomes 'Finish' (submits).
    .PARAMETER Back      Page to go back to (no validation). Omit to hide Back.
    .PARAMETER Require   Names of controls that must be non-empty before advancing.
    .PARAMETER Validate  Optional in-app scriptblock; OUTPUT a non-empty string to block with that message.
    .EXAMPLE
    Add-UICanvasWizardNav -Next 'Drivers' -Require 'ComputerName','AdminPass' -Validate {
        if (([string](Get-UICanvasValue -Name AdminPass)).Length -lt 12) { 'Password must be at least 12 characters.' }
    }
    #>
    [CmdletBinding()]
    param(
        [string]$Next,
        [string]$Back,
        [string[]]$Require,
        [scriptblock]$Validate,
        [string]$NextLabel = 'Next',
        [string]$BackLabel = 'Back',
        [switch]$NoBack,   # once Next succeeds, lock navigation so this page can't be returned to
        [string]$Name = 'wiz'
    )
    $wzName = $Name   # avoid $Name shadowing inside -Children (container cmdlets have their own -Name)
    $bk = $Back; $bkLabel = $BackLabel
    $nextLbl = if ($Next) { $NextLabel } else { 'Finish' }

    Add-UICanvasLabel '' -Name "${wzName}_error" -Foreground '#F87171' -FontSize 12 -Visible $false | Out-Null

    $L = [System.Collections.Generic.List[string]]::new()
    $L.Add("Set-UICanvasProperty -Name '${wzName}_error' -Property Visible -Value `$false")
    $L.Add("`$__errs = @()")
    foreach ($r in $Require) {
        $L.Add("if ([string]::IsNullOrWhiteSpace([string](Get-UICanvasValue -Name '$r'))) { `$__errs += '$r' }")
    }
    $L.Add("if (`$__errs.Count) { Set-UICanvasProperty -Name '${wzName}_error' -Property Text -Value ('Please complete: ' + (`$__errs -join ', ')); Set-UICanvasProperty -Name '${wzName}_error' -Property Visible -Value `$true; return }")
    if ($Validate) {
        $L.Add("`$__msg = & { $($Validate.ToString()) }")
        $L.Add("if (`$__msg) { Set-UICanvasProperty -Name '${wzName}_error' -Property Text -Value ([string](`$__msg | Select-Object -Last 1)); Set-UICanvasProperty -Name '${wzName}_error' -Property Visible -Value `$true; return }")
    }
    if ($NoBack) { $L.Add("Lock-UICanvasNavigation") }   # commit: no returning to this page once advanced
    if ($Next) { $L.Add("Show-UICanvasPage '$Next'") } else { $L.Add("Submit-UICanvas") }
    $nextAction = [scriptblock]::Create(($L -join "`n"))

    Add-UICanvasPanel -Layout Grid -ColumnWidths 'Auto,*,Auto' -Children {
        # Name the Back button nav_* so an active navigation lock greys it out (and it's blocked functionally).
        if ($bk) { Add-UICanvasButton $bkLabel -Name "nav_${wzName}_back" -Style Secondary -NavigateTo $bk -Properties @{ Column = 0 } }
        Add-UICanvasButton $nextLbl -Icon 'next' -Style Accent -Action $nextAction -Properties @{ Column = 2 }
    }
}

function Add-UICanvasWizardSteps {
    <#
    .SYNOPSIS
    Renders a step indicator (numbered chips) with the current step highlighted.
    .EXAMPLE
    Add-UICanvasWizardSteps -Steps 'Config','Drivers','Apps','Deploy' -Current 'Drivers'
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string[]]$Steps, [string]$Current)
    $cur = $Current
    Add-UICanvasPanel -Layout HStack -Spacing 8 -Children {
        $i = 0
        foreach ($st in $Steps) {
            $sev = if ($st -eq $cur) { 'Info' } else { 'Neutral' }
            Add-UICanvasBadge ("{0}. {1}" -f (++$i), $st) -Severity $sev
        }
    }
}

# ── Dashboard: on-demand script runner card ────────────────────────────────────
function Add-UICanvasScriptCard {
    <#
    .SYNOPSIS
    A dashboard card that RUNS a script off the UI gate (its own runspace via Start-UICanvasAsync): a title, a
    live status, a Run button, and an output console. The script's output + errors stream into the console as it
    runs, so a long-running task never freezes the rest of the dashboard (live cards keep refreshing).
    .DESCRIPTION
    The Run button's action launches the script through Start-UICanvasAsync, which spins a fresh runspace with the
    bridge cmdlets available — so inside the script you can also call Set-UICanvasValue / Set-UICanvasProperty to
    update other controls. Status flips Idle -> Running -> Done/Failed automatically.
    .EXAMPLE
    Add-UICanvasScriptCard 'Disk cleanup' -Detail 'Clear %TEMP%' -Script {
        Get-ChildItem $env:TEMP -File | Remove-Item -Force -EA SilentlyContinue
        "cleaned $((Get-ChildItem $env:TEMP).Count) items"
    }
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)][string]$Title,
        [Parameter(Mandatory)][scriptblock]$Script,
        [string]$Name,
        [string]$Detail,
        [string]$RunLabel = 'Run',
        [string]$RunIcon = 'play',
        [int]$OutputHeight = 160,
        [hashtable]$Properties
    )
    if (-not $Name) { $Name = 'sc_' + ($Title -replace '[^A-Za-z0-9]', '') }
    $scName = $Name                       # avoid $Name shadowing inside -Children (containers own -Name)
    $outName = "${scName}_out"; $statName = "${scName}_status"; $ttl = $Title; $dtl = $Detail
    $runLbl = $RunLabel; $runIco = $RunIcon; $outH = $OutputHeight

    # Off-gate runner: stream every output stream of the user script into the console; flip the status label.
    $body = @"
Set-UICanvasValue -Name '$statName' -Value 'Running'
Set-UICanvasProperty -Name '$statName' -Property Foreground -Value '#FBBF24'
Set-UICanvasProperty -Name '$outName' -Property AppendLine -Value ('[' + (Get-Date -Format 'HH:mm:ss') + '] starting...') -Quiet
try {
  & {
$($Script.ToString())
  } *>&1 | ForEach-Object { Set-UICanvasProperty -Name '$outName' -Property AppendLine -Value ([string]`$_) -Quiet }
  Set-UICanvasValue -Name '$statName' -Value 'Done'
  Set-UICanvasProperty -Name '$statName' -Property Foreground -Value '#34D399'
} catch {
  Set-UICanvasProperty -Name '$outName' -Property AppendLine -Value ('ERROR: ' + `$_.Exception.Message)
  Set-UICanvasValue -Name '$statName' -Value 'Failed'
  Set-UICanvasProperty -Name '$statName' -Property Foreground -Value '#F87171'
}
"@
    # The button action (runs on the gate for an instant) just launches the off-gate async runner and returns.
    $runAction = [scriptblock]::Create("Start-UICanvasAsync { $body }")

    $pp = @{}; if ($Properties) { foreach ($k in $Properties.Keys) { $pp[$k] = $Properties[$k] } }
    Add-UICanvasCard -Layout VStack -Spacing 6 -Padding 14 -Properties $pp -Children {
        Add-UICanvasPanel -Layout Grid -ColumnWidths '*,Auto' -Children {
            Add-UICanvasLabel $ttl -FontSize 15 -FontWeight SemiBold -Properties @{ Column = 0; VAlign = 'Center' }
            Add-UICanvasLabel 'Idle' -Name $statName -FontSize 12 -Foreground '#94A3B8' -Properties @{ Column = 1; VAlign = 'Center' }
        }
        if ($dtl) { Add-UICanvasLabel $dtl -FontSize 12 -Foreground '#94A3B8' }
        Add-UICanvasButton $runLbl -Name "${scName}_run" -Icon $runIco -Style Accent -Action $runAction -Properties @{ HAlign = 'Left' }
        Add-UICanvasConsole -Name $outName -Height $outH -Value '[ready]'
    }
}

# ── Workflow: declarative sequential step runner ───────────────────────────────
function Add-UICanvasWorkflowStep {
    <#
    .SYNOPSIS
    Declares one workflow step (name + detail + an in-app script). Call repeatedly, then Add-UICanvasWorkflow.
    .EXAMPLE
    Add-UICanvasWorkflowStep 'Prepare Disk' -Detail 'Wiping partitions.' -Script { Start-Sleep 1 }
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)][string]$Name,
        [string]$Detail,
        [Parameter(Mandatory)][scriptblock]$Script,
        [scriptblock]$OnClick,        # optional: makes this step's row clickable (e.g. to filter a log to this step)
        [int]$ExpectedSeconds,        # optional: typical duration; drives a weighted ETA when set on every step
        [int]$Retry,                  # (-Engine) times to retry the step on failure before failing the run
        [int]$TimeoutSeconds,         # (-Engine) fail the step if it runs longer than this
        [string]$SkipWhen             # (-Engine) PowerShell condition; if truthy at run time, the step is skipped
    )
    if (-not $Global:_PoshUICanvas) { throw "No canvas active. Call New-PoshUICanvas first." }
    if (-not $Global:_PoshUICanvas.PendingWfSteps) {
        $Global:_PoshUICanvas.PendingWfSteps = [System.Collections.Generic.List[hashtable]]::new()
    }
    $step = @{ Name = $Name; Detail = $Detail; Script = $Script.ToString() }
    if ($OnClick) { $step.OnClick = $OnClick.ToString() }
    if ($PSBoundParameters.ContainsKey('ExpectedSeconds')) { $step.ExpectedSeconds = $ExpectedSeconds }
    if ($PSBoundParameters.ContainsKey('Retry'))           { $step.Retry = $Retry }
    if ($PSBoundParameters.ContainsKey('TimeoutSeconds'))  { $step.TimeoutSeconds = $TimeoutSeconds }
    if ($SkipWhen)                                          { $step.SkipWhen = $SkipWhen }
    $Global:_PoshUICanvas.PendingWfSteps.Add($step)
}

function Add-UICanvasWorkflow {
    <#
    .SYNOPSIS
    Renders the steps declared via Add-UICanvasWorkflowStep as a live runner: status header + progress bar +
    one row per step + a Start button. The Start button runs every step sequentially in-app (spinner while
    running, green check on success, red on failure which halts the run), updating progress as it goes.
    .PARAMETER Name      Prefix for the generated control names (default 'wf').
    .PARAMETER StartLabel  Text for the run button (default 'Start').
    .PARAMETER ShowLog   Also render a live log console the runner writes step progress to.
    #>
    [CmdletBinding()]
    param(
        [string]$Name = 'wf',
        [switch]$Engine,           # run steps on the engine-native WorkflowExecutor (own runspace; no gate/freeze;
                                   # steps use $wf.UpdateProgress/GetValue/SetValue; retry/timeout/skip supported)
        [string]$StartLabel = 'Start',
        [string]$StartIcon = 'play',
        [switch]$ShowLog,
        [switch]$AutoStart,   # run automatically shortly after the window opens (no click)
        [switch]$LockNavigation,   # once the first step completes, block backtracking to earlier pages
        [switch]$LockOnStart,      # block backtracking immediately when the run begins (before step 1)
        [int]$StepsHeight,         # if set, the step rows scroll within this fixed pixel height
        [switch]$Compact,          # tighter step rows (smaller padding/spacing) for dense pipelines
        [switch]$NoStartButton,    # don't render the Start button; place it yourself via $Global:_PoshUICanvas.WorkflowActions[Name]
        [switch]$NoHeader,         # don't render the Status/% header + gauge; place <name>_status/<name>_pct/<name>_gauge yourself
        # Reboot-resume: a non-volatile path (e.g. on the target disk, NOT WinPE's X:) where completed-step
        # state is written after each step. On relaunch the workflow pre-marks done steps and resumes from there.
        [string]$StateFile
    )
    $steps = $Global:_PoshUICanvas.PendingWfSteps
    if (-not $steps -or $steps.Count -eq 0) { throw "No workflow steps. Call Add-UICanvasWorkflowStep first." }
    $Global:_PoshUICanvas.PendingWfSteps = $null   # consume

    # IMPORTANT: use $wfName, not $Name, for control names. Inside an -Children { } block the container
    # cmdlets (Add-UICanvasCard/-Panel) have their OWN -Name parameter which shadows a local $Name, so
    # "${wfName}_status" would resolve to "_status" and the generated action couldn't find the control.
    $wfName = $Name
    $n = $steps.Count

    # Resume: how many steps already completed (from a prior run before a reboot).
    $done = 0
    if ($StateFile -and (Test-Path $StateFile)) {
        try {
            $st = Get-Content $StateFile -Raw | ConvertFrom-Json
            if ($st.name -eq $Name) { $done = [int]$st.completed }
        } catch { }
    }
    if ($done -gt $n) { $done = $n }

    # Optional weighted ETA: when every step declares -ExpectedSeconds, drive % and the remaining-time readout
    # from the plan (long steps move the bar proportionally) instead of a flat step count / elapsed guess.
    $expSecs = @($steps | ForEach-Object { if ($_.ExpectedSeconds) { [int]$_.ExpectedSeconds } else { 0 } })
    $expTotal = [int](($expSecs | Measure-Object -Sum).Sum)
    $weighted = ($expTotal -gt 0)
    $cumExp = @(0) * ($n + 1); for ($j = 0; $j -lt $n; $j++) { $cumExp[$j + 1] = $cumExp[$j] + $expSecs[$j] }
    $fmtSecs = { param($s) '{0:00}:{1:00}' -f [int]([math]::Floor([double]$s / 60)), ([int]([double]$s) % 60) }
    $initRemain = if ($weighted) { & $fmtSecs ($expTotal - $cumExp[$done]) } else { '00:00' }

    $initPct = if ($weighted) { if ($done -gt 0) { [int](100 * $cumExp[$done] / $expTotal) } else { 0 } } elseif ($done -gt 0) { [int](100 * $done / $n) } else { 0 }
    $initStatus = if ($done -ge $n -and $done -gt 0) { 'Complete' } elseif ($done -gt 0) { 'Resuming' } else { 'Idle' }
    $initStatusFg = if ($done -ge $n -and $done -gt 0) { '#34D399' } elseif ($done -gt 0) { '#93C5FD' } else { '#FBBF24' }

    # Status header + progress (suppressed by -NoHeader so the author can place these at the top of the page).
    if (-not $NoHeader) {
        Add-UICanvasPanel -Layout Grid -ColumnWidths 'Auto,Auto,*,Auto' -Children {
            Add-UICanvasLabel 'Status:' -FontWeight Bold -Properties @{ Column = 0; VAlign = 'Center'; Margin = '0,0,8,0' }
            Add-UICanvasLabel $initStatus -Name "${wfName}_status" -FontWeight Bold -Foreground $initStatusFg -Properties @{ Column = 1; VAlign = 'Center' }
            Add-UICanvasLabel "$initPct%" -Name "${wfName}_pct" -FontWeight Bold -Foreground '#A5B4FC' -Properties @{ Column = 3; VAlign = 'Center' }
        }
        Add-UICanvasProgressBar -Name "${wfName}_gauge" -Value $initPct
    }

    # Step rows (steps already completed on a prior run start out DONE). Optionally scrollable (-StepsHeight).
    $rowPad = if ($Compact) { 8 } else { 12 }
    $rowSpacing = if ($Compact) { 5 } else { 8 }
    $stepArgs = @{ Layout = 'VStack'; Spacing = $rowSpacing }
    if ($StepsHeight -gt 0) { $stepArgs.Height = $StepsHeight; $stepArgs.Properties = @{ Scroll = 'Vertical' } }
    Add-UICanvasPanel @stepArgs -Children {
        for ($i = 0; $i -lt $n; $i++) {
            $s = $steps[$i]; $k = $i
            $isDone = ($k -lt $done)
            $iconGlyph = if ($isDone) { [string][char]0x2713 } else { 'circle' }
            $iconFg = if ($isDone) { '#34D399' } else { '#64748B' }
            $statText = if ($isDone) { 'DONE' } else { 'PENDING' }
            $statFg = if ($isDone) { '#34D399' } else { '#64748B' }
            $cardArgs = @{ Layout = 'Grid'; ColumnWidths = 'Auto,*,Auto'; Spacing = 10; Padding = $rowPad; Properties = @{ HoverBackground = '#1A2236' } }
            if ($s.OnClick) { $cardArgs.Name = "${wfName}_row$k"; $cardArgs.Action = [scriptblock]::Create($s.OnClick) }
            $nameFs = if ($Compact) { 13 } else { 14 }
            Add-UICanvasCard @cardArgs -Children {
                Add-UICanvasIcon $iconGlyph -Name "${wfName}_icon$k" -Foreground $iconFg -FontSize 14 -Properties @{ Column = 0; VAlign = 'Center' }
                Add-UICanvasPanel -Layout VStack -Spacing 1 -Properties @{ Column = 1 } -Children {
                    Add-UICanvasLabel $s.Name -FontSize $nameFs -FontWeight SemiBold
                    if ($s.Detail) { Add-UICanvasLabel $s.Detail -FontSize 11 -Foreground '#94A3B8' }
                }
                Add-UICanvasLabel $statText -Name "${wfName}_stat$k" -Foreground $statFg -Properties @{ Column = 2; VAlign = 'Center' }
            }
        }
    }
    if ($ShowLog) { Add-UICanvasConsole -Name "${wfName}_log" -Height 160 -Value '[ready] Awaiting execution...' }

    # ── Engine-native execution: the cards/header/log above render EXACTLY as the legacy path; here we emit an
    # INVISIBLE Type='Workflow' host the engine runs on WorkflowExecutor (own runspace — no gate/freeze), driving
    # the same named controls. No composed action; step bodies (Set-<name>Progress/WLog) run unchanged because the
    # executor's runspace is seeded with the bridge cmdlets. ──
    if ($Engine) {
        # Keys MUST match the UIWorkflowStepJson DataMember names (DataContractJsonSerializer is case-sensitive).
        $stepObjs = foreach ($s in $steps) {
            [ordered]@{
                Name = [string]$s.Name; Detail = [string]$s.Detail; Script = [string]$s.Script
                ExpectedSeconds = [int]($s.ExpectedSeconds); Retry = [int]($s.Retry)
                TimeoutSeconds = [int]($s.TimeoutSeconds); SkipWhen = [string]$s.SkipWhen
            }
        }
        # Force a JSON ARRAY (PS 5.1 ConvertTo-Json unwraps a single-element array to a bare object).
        $stepsJson = '[' + (($stepObjs | ForEach-Object { $_ | ConvertTo-Json -Depth 6 -Compress }) -join ',') + ']'
        $hp = @{ StepsJson = $stepsJson; Title = $Name }
        if ($StateFile) { $hp.StateFile = $StateFile }
        if ($AutoStart) { $hp.AutoStart = $true }
        if ($LockNavigation -or $LockOnStart) { $hp.LockNavigation = $true }
        # Host Name = the workflow name so the engine derives the card control names (<name>_stat{i}, <name>_gauge, ...).
        _FwdCanvas -Type 'Workflow' -Bound @{ Name = $wfName; Visible = $false } -Props $hp
        # Run button with NO action — the engine wires its Click (suppressed by -NoStartButton / when -AutoStart).
        if (-not $NoStartButton) {
            Add-UICanvasButton $StartLabel -Name "${wfName}_run" -Icon $StartIcon -Style Accent -Properties @{ HAlign = 'Left'; Margin = '0,10,0,0' }
        }
        return
    }

    # Compose the runner action (one scriptblock the bridge executes on the Start click).
    $escState = if ($StateFile) { $StateFile -replace "'", "''" } else { '' }
    $escName = $Name -replace "'", "''"
    $L = [System.Collections.Generic.List[string]]::new()
    $L.Add("Set-UICanvasValue -Name '${wfName}_status' -Value 'In Progress'")
    $L.Add("Set-UICanvasProperty -Name '${wfName}_status' -Property Foreground -Value '#93C5FD'")
    # Stamp the run start (UTC ticks) and clear any prior end so an elapsed/ETA counter can tick live.
    $L.Add("Set-UICanvasValue -Name '${wfName}_start' -Value ([DateTime]::UtcNow.Ticks) -Quiet")
    $L.Add("Set-UICanvasValue -Name '${wfName}_end' -Value '' -Quiet")
    $L.Add('$wfStart = [DateTime]::UtcNow')   # local start so the runner can drive a live elapsed/ETA each step
    # Seed the gauge/% to the RESUMED baseline so a -Resume run reflects prior progress instead of sweeping from 0
    # (no-op on a fresh run where $initPct = 0). The gauge tweens, so this reads as a quick catch-up.
    $L.Add("Set-UICanvasProperty -Name '${wfName}_gauge' -Property Value -Value $initPct")
    $L.Add("Set-UICanvasValue -Name '${wfName}_pct' -Value '$initPct%'")
    if ($weighted) { $L.Add("Set-UICanvasValue -Name '${wfName}_remaining' -Value '$initRemain' -Quiet") }
    if ($LockOnStart) { $L.Add("Lock-UICanvasNavigation") }   # block backtracking the moment the run begins
    # Log writes are unconditional (no-op if no console named "<name>_log" exists) so the console can be placed
    # anywhere by the author; -ShowLog only controls whether the workflow renders its OWN console inline.
    $L.Add("Set-UICanvasProperty -Name '${wfName}_log' -Property AppendLine -Value '[run] starting...' -Quiet")
    for ($i = $done; $i -lt $n; $i++) {
        $pct = if ($weighted) { [int](100 * $cumExp[$i + 1] / $expTotal) } else { [int](100 * ($i + 1) / $n) }
        # pct at the START of this step: the previous step's end-pct (or the resumed baseline for the first
        # step this run). Lets a step interpolate the overall gauge/%/elapsed live via Set-<name>Progress
        # below instead of only jumping at completion.
        $pctStart = if ($weighted) { [int](100 * $cumExp[$i] / $expTotal) } else { [int](100 * $i / $n) }
        $L.Add("try {")
        $L.Add("  Set-UICanvasValue -Name '${wfName}_stat$i' -Value 'RUNNING'")
        $L.Add("  Set-UICanvasProperty -Name '${wfName}_stat$i' -Property Foreground -Value '#FBBF24'")
        # Swap to an asymmetric spinner glyph before spinning the icon - the prior 'circle' glyph is
        # rotationally symmetric, so Spin's rotation was invisible and the running step looked static.
        $L.Add("  Set-UICanvasValue -Name '${wfName}_icon$i' -Value ([char]0xE72C)")
        $L.Add("  Set-UICanvasProperty -Name '${wfName}_icon$i' -Property Spin -Value `$true")
        $L.Add("  Set-UICanvasProperty -Name '${wfName}_log' -Property AppendLine -Value ('[{0}] Running step {1}/{2}' -f (Get-Date -Format 'HH:mm:ss'), $($i + 1), $n) -Quiet")
        # Live sub-progress reporter: a step's Script can call Set-<name>Progress -Percent 0..100 [-Message ...]
        # to interpolate the overall gauge/%/elapsed/remaining WHILE it runs, instead of only at completion.
        # This runs inline in the SAME already-executing pipeline (no new PowerShell invocation), so it never
        # contends with the bridge's serializing gate the way a -Refresh tick would while this step holds it.
        # global: scope, matching this codebase's WLog convention - steps dot-source library scripts and
        # invoke -Progress/-Log callbacks from deep within their own function scopes (e.g. Invoke-OsdApplyImage's
        # polling loop); a plain local function here is not reliably visible from that far down the call stack.
        $L.Add("  function global:Set-${wfName}Progress {")
        $L.Add("    param([int]`$Percent = 0, [string]`$Message)")
        $L.Add("    if (`$Percent -lt 0) { `$Percent = 0 } elseif (`$Percent -gt 100) { `$Percent = 100 }")
        $L.Add("    `$__cur = [int]($pctStart + (($pct - $pctStart) * `$Percent / 100.0))")
        $L.Add("    Set-UICanvasProperty -Name '${wfName}_gauge' -Property Value -Value `$__cur")
        $L.Add("    Set-UICanvasValue -Name '${wfName}_pct' -Value (`$__cur.ToString() + '%')")
        $L.Add("    `$__sp = [DateTime]::UtcNow - `$wfStart")
        $L.Add("    Set-UICanvasValue -Name '${wfName}_elapsed' -Value ('{0:00}:{1:00}' -f [int]`$__sp.TotalMinutes, `$__sp.Seconds) -Quiet")
        if ($weighted) {
            $L.Add("    `$__remSecs = [Math]::Max(0, $expTotal - ($($cumExp[$i]) + ($($expSecs[$i]) * `$Percent / 100.0)))")
            $L.Add("    `$__rs = [TimeSpan]::FromSeconds(`$__remSecs)")
            $L.Add("    Set-UICanvasValue -Name '${wfName}_remaining' -Value ('{0:00}:{1:00}' -f [int]`$__rs.TotalMinutes, `$__rs.Seconds) -Quiet")
        }
        $L.Add("    if (`$Message) { Set-UICanvasValue -Name '${wfName}_op' -Value `$Message -Quiet }")
        $L.Add("  }")
        $L.Add("  & {")
        $L.Add($steps[$i].Script)
        $L.Add("  }")
        $L.Add("  Set-UICanvasProperty -Name '${wfName}_icon$i' -Property Spin -Value `$false")
        $L.Add("  Set-UICanvasValue -Name '${wfName}_icon$i' -Value ([char]0x2713)")
        $L.Add("  Set-UICanvasProperty -Name '${wfName}_icon$i' -Property Foreground -Value '#34D399'")
        $L.Add("  Set-UICanvasValue -Name '${wfName}_stat$i' -Value 'DONE'")
        $L.Add("  Set-UICanvasProperty -Name '${wfName}_stat$i' -Property Foreground -Value '#34D399'")
        $L.Add("  Set-UICanvasProperty -Name '${wfName}_gauge' -Property Value -Value $pct")
        $L.Add("  Set-UICanvasValue -Name '${wfName}_pct' -Value '$pct%'")
        # Drive the elapsed + estimated-remaining counters here (the -Refresh ticks are skipped while a step holds the gate).
        $L.Add("  `$sp = [DateTime]::UtcNow - `$wfStart")
        $L.Add("  Set-UICanvasValue -Name '${wfName}_elapsed' -Value ('{0:00}:{1:00}' -f [int]`$sp.TotalMinutes, `$sp.Seconds) -Quiet")
        if ($weighted) {
            # Plan-based ETA: seconds left = total expected minus what the completed steps were expected to take.
            $L.Add("  Set-UICanvasValue -Name '${wfName}_remaining' -Value '$(& $fmtSecs ($expTotal - $cumExp[$i + 1]))' -Quiet")
        }
        else {
            $L.Add("  `$rem = if ($pct -gt 0) { [Math]::Max(0, (`$sp.TotalSeconds * 100.0 / $pct) - `$sp.TotalSeconds) } else { 0 }")
            $L.Add("  `$rs = [TimeSpan]::FromSeconds(`$rem); Set-UICanvasValue -Name '${wfName}_remaining' -Value ('{0:00}:{1:00}' -f [int]`$rs.TotalMinutes, `$rs.Seconds) -Quiet")
        }
        if ($StateFile) { $L.Add("  try { Set-Content -Path '$escState' -Value (ConvertTo-Json @{ name = '$escName'; completed = $($i + 1) }) } catch {}") }
        if ($LockNavigation -and $i -eq $done) { $L.Add("  Lock-UICanvasNavigation") }   # no backtracking once underway
        $L.Add("} catch {")
        $L.Add("  Set-UICanvasProperty -Name '${wfName}_icon$i' -Property Spin -Value `$false")
        $L.Add("  Set-UICanvasValue -Name '${wfName}_icon$i' -Value ([char]0xE711)")
        $L.Add("  Set-UICanvasProperty -Name '${wfName}_icon$i' -Property Foreground -Value '#F87171'")
        $L.Add("  Set-UICanvasValue -Name '${wfName}_stat$i' -Value 'FAILED'")
        $L.Add("  Set-UICanvasProperty -Name '${wfName}_stat$i' -Property Foreground -Value '#F87171'")
        $L.Add("  Set-UICanvasValue -Name '${wfName}_status' -Value 'Failed'")
        $L.Add("  Set-UICanvasProperty -Name '${wfName}_status' -Property Foreground -Value '#F87171'")
        $L.Add("  Set-UICanvasProperty -Name '${wfName}_log' -Property AppendLine -Value ('[error] step $($i + 1): ' + `$_.Exception.Message)")
        $L.Add("  return")
        $L.Add("}")
    }
    if ($StateFile) { $L.Add("try { if (Test-Path '$escState') { Remove-Item -LiteralPath '$escState' -Force } } catch {}") }
    $L.Add("Set-UICanvasValue -Name '${wfName}_status' -Value 'Complete'")
    $L.Add("Set-UICanvasProperty -Name '${wfName}_status' -Property Foreground -Value '#34D399'")
    $L.Add("Set-UICanvasValue -Name '${wfName}_end' -Value ([DateTime]::UtcNow.Ticks) -Quiet")   # freeze the elapsed counter

    $body = $L -join "`n"
    $action = [scriptblock]::Create($body)
    # Publish the run action so the author can place the Start button anywhere (when -NoStartButton).
    if (-not $Global:_PoshUICanvas.WorkflowActions) { $Global:_PoshUICanvas.WorkflowActions = @{} }
    $Global:_PoshUICanvas.WorkflowActions[$wfName] = $action
    if (-not $NoStartButton) {
        Add-UICanvasButton $StartLabel -Name "${wfName}_run" -Icon $StartIcon -Style Accent -Action $action -Properties @{ HAlign = 'Left'; Margin = '0,10,0,0' }
    }

    # Optional: fire once automatically. Guard off the live status control (robust across the bridge's
    # per-event runspace instances, where $Global wouldn't persist): only start while still Idle/Resuming.
    # The gate serializes runs, so the first run flips status to 'In Progress'/'Complete' before the next tick.
    if ($AutoStart) {
        $auto = "if ((Get-UICanvasValue -Name '${wfName}_status') -in @('Idle','Resuming')) { $body }; ''"
        Add-UICanvasLabel -Label ([scriptblock]::Create($auto)) -Name "${wfName}_auto" -Refresh 2 -Visible $false
    }
}
