# 11 — Staging & Delivery: Apps That Other People Run

The context for everything you build with PoshUI: **an administrator builds an app, stages the content it
works on, and hands it to other people to run** — end users on their own machines, or other admins. The
person who runs the app is almost never the person who wrote it, cannot read the code, and usually cannot
ask the author anything. Design for that hand-off from the first line.

This file covers the three things that make the hand-off work: **staged content** (what the app acts on,
prepared by the admin, separate from the code), **back-end operations** (how work is done, reported and
logged so it can be supported remotely), and **delivery** (packaging, checking and shipping it).

---

## 1. The people involved

| Role | Who | What they need from the app |
|---|---|---|
| **Author** | The admin who writes the app | Code that never has to change when the content changes |
| **Stager** | Usually the same admin, preparing a copy for one audience, site or project | To add, remove and edit content by dropping in folders and editing a small file — then prove it is valid before shipping |
| **End-user runner** | Staff on their own machine; standard rights; not technical | Plain words, big obvious actions, safe defaults, nothing that can hurt them, a clear result, and a way to send details to IT |
| **Admin runner** | A colleague, help desk, another team; often elevated | Detail, speed, raw codes and logs, bulk actions, dry runs |

**Think in those roles when designing.** Every decision below follows from them:
- The end user cannot fix a missing file or an unexpected error — the stager must catch it *before* shipping,
  and the app must fail politely and log enough for IT to diagnose it later.
- The admin runner wants what the end user must not see — so one app can serve both, switched by config,
  not by maintaining two copies.
- The stager is not a developer every time — staging must be possible without opening the `.ps1`.

---

## 2. Code, content and config are three different things

```
MyApp\
  Start-MyApp.cmd        # what the runner double-clicks
  MyApp.ps1              # the front end: pages, bindings, actions          CODE   - author
  lib\Operations.ps1     # the back end: plain functions, no UI cmdlets     CODE   - author
  config.psd1            # this copy's settings: audience, branding, paths  CONFIG - stager
  content\               # what the app acts on, one folder per item        CONTENT- stager
    01-network-info\item.psd1 + Run.ps1
    02-clear-temp\  item.psd1 + Run.ps1
    ...
  assets\                # logo, icons
  runtime\               # PoshUI, vendored so the folder runs anywhere
    PoshUI.Canvas\       #   the module
    bin\PoshUI.exe       #   the engine (the module finds it here)
```

- **Code** changes when the author releases a new version.
- **Content** changes whenever the stager wants: add a folder, remove one, edit its descriptor.
- **Config** changes per copy: an end-user edition and an admin edition are the same code and content with
  a different `config.psd1`.
- **The app discovers its content at start** and builds its UI from it. Nothing in the code names an item.

---

## 3. Staged content

### The convention
One folder per item, each with a small descriptor (`item.psd1`) and whatever the item needs (a script, an
installer, a template, a data file). Folder names give the order (`01-`, `02-`) and a stable id.

```powershell
# content\02-clear-temp\item.psd1
@{
    Name          = 'Clear my temporary files'
    Description   = 'Frees space by removing temporary files older than 7 days. Safe to run any time.'
    Icon          = 'E74D'              # Segoe Fluent glyph code
    Script        = 'Run.ps1'           # relative to this folder
    Audience      = @('EndUser', 'Admin')
    RequiresAdmin = $false
    Confirm       = 'This removes temporary files older than 7 days. Continue?'
    Version       = '1.1'
}
```

The item's script does the work and **returns a result object** (section 4) — it does not touch the UI:

```powershell
# file: content\01-network-info\Run.ps1
$ip = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' } | Select-Object -First 1
if (-not $ip) { return [pscustomobject]@{ Status = 'Warning'; Message = 'No network address found. Check the cable or Wi-Fi.'; Data = $null } }
[pscustomobject]@{ Status = 'Success'; Message = "Your computer's address is $($ip.IPAddress)."; Data = $ip.IPAddress }
```

### Shapes by objective
The same idea fits every blueprint in file 09 — only the content changes:

| Objective (09) | What the stager drops in | Descriptor carries |
|---|---|---|
| Admin toolbox / kiosk | `tools\` or `fixes\` — a folder per task with its script | name, blurb, icon, audience, needs-admin, confirm text |
| Launcher | `items.psd1` — one list of targets | name, blurb, icon, target, category |
| Guided setup | `profiles\*.psd1` — one per site or role | the defaults and locked choices for that profile |
| Long-running job | `steps\` — a folder per step, or one `steps.psd1` | order, script, expected seconds, retry, skip condition |
| Data explorer / reports | `queries\` — a folder per saved report | query script, columns, default filter |
| File processor | `rules\` or `templates\` | match pattern, action, output naming |
| Settings editor | `schema.psd1` — which settings exist | label, type, allowed values, help text, default |

### Rules for staged content
- **Validate everything at start, and keep going.** A broken item is skipped and reported — one bad folder
  must never stop the app opening. Admin runners see what was skipped and why; end users just don't see it.
- **Content is data first.** Prefer descriptors the stager edits over scripts they must write. When an item
  needs a script, the script follows the result contract.
- **Paths inside content are relative to the item's folder**, so the content folder can move (to a share, a
  USB stick, another site) intact.
- **Version items**, and show the version in the log for every run — it is the first question support asks.

---

## 4. Back-end operations

Everything that does work lives in `lib\` (and in content scripts), as plain functions that **never call UI
cmdlets**. The front end calls them and decides how to show the result. That separation is what lets the
same back end serve an end-user window, an admin window and a headless `-Check`.

### The contract
- **Every operation returns a result object** — `Status` (`Success`, `Warning`, `Failed`, `Skipped`),
  a `Message` written for the person who will read it, and optional `Data`. It **never throws** to its caller.
- **Every operation logs** start and end, with the item id, version, user and outcome.
- **Logs go to `%LOCALAPPDATA%\<AppId>\logs`**, not next to the app: the app may run from a read-only share,
  and a standard user cannot write to `Program Files`.
- **Destructive work honours a dry run** and checks elevation first — it reports `Skipped` with a reason
  rather than failing half-way.
- **Operations are safe to run twice** — check before changing, so a retry or a second click does no harm.
- **Long work** reports progress through a callback (`-OnProgress`) or runs as an `-Engine` workflow step
  (file 06), and saves state as it goes if closing the window must not lose it.

```powershell
# file: lib\Operations.ps1
# The back end. No UI cmdlets anywhere in this file - it must run the same in the app, in -Check and in a console.

function New-OpResult([string]$Status, [string]$Message, $Data = $null) {
    [pscustomobject]@{ Status = $Status; Message = $Message; Data = $Data; At = (Get-Date).ToString('s') }
}

function Write-OpLog([string]$LogDir, [string]$Text, [string]$Level = 'INFO') {
    try {
        if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Force -Path $LogDir | Out-Null }
        $line = '{0} [{1}] {2}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Level, $Text
        Add-Content -Path (Join-Path $LogDir ('{0:yyyy-MM-dd}.log' -f (Get-Date))) -Value $line -Encoding UTF8
    } catch { }   # logging must never be the thing that breaks a run
}

function Test-IsAdmin {
    ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Discover and validate the staged content. Returns EVERY item, valid or not, so problems can be reported.
function Get-StagedItem([string]$ContentPath) {
    foreach ($dir in @(Get-ChildItem -LiteralPath $ContentPath -Directory -ErrorAction SilentlyContinue | Sort-Object Name)) {
        $problem = $null; $d = $null
        $descPath = Join-Path $dir.FullName 'item.psd1'
        if (-not (Test-Path $descPath)) { $problem = 'no item.psd1' }
        else {
            try { $d = Import-PowerShellDataFile $descPath } catch { $problem = "item.psd1 does not parse: $($_.Exception.Message)" }
        }
        if (-not $problem) { foreach ($k in 'Name', 'Description', 'Script') { if (-not $d[$k]) { $problem = "item.psd1 has no $k"; break } } }
        if (-not $problem -and -not (Test-Path (Join-Path $dir.FullName $d.Script))) { $problem = "script '$($d.Script)' not found" }
        $ok = -not $problem
        [pscustomobject]@{
            Id            = $dir.Name
            Valid         = $ok
            Problem       = $problem
            Name          = $(if ($ok) { $d.Name } else { $dir.Name })
            Description   = $(if ($ok) { $d.Description } else { '' })
            Icon          = $(if ($ok -and $d.Icon) { $d.Icon } else { 'E9CE' })
            Script        = $(if ($ok) { Join-Path $dir.FullName $d.Script } else { $null })
            Audience      = $(if ($ok -and $d.Audience) { @($d.Audience) } else { @('EndUser', 'Admin') })
            RequiresAdmin = [bool]($ok -and $d.RequiresAdmin)
            Confirm       = $(if ($ok) { $d.Confirm } else { $null })
            Version       = $(if ($ok -and $d.Version) { $d.Version } else { '' })
        }
    }
}

# Run one item. Always returns a result, always logs, never throws.
function Invoke-StagedItem($Item, [string]$LogDir, [switch]$DryRun) {
    Write-OpLog $LogDir "START $($Item.Id) v$($Item.Version) dryRun=$DryRun user=$env:USERNAME"
    try {
        if (-not $Item.Valid) { $r = New-OpResult 'Failed' "This item is not set up correctly: $($Item.Problem)" }
        elseif ($Item.RequiresAdmin -and -not (Test-IsAdmin)) { $r = New-OpResult 'Skipped' 'This needs administrator rights.' }
        elseif ($DryRun) { $r = New-OpResult 'Skipped' "Dry run - would run $(Split-Path $Item.Script -Leaf)." }
        else {
            $out = @(& $Item.Script) | Select-Object -Last 1
            $r = if ($out -and $out.PSObject.Properties['Status']) { $out } else { New-OpResult 'Success' 'Done.' $out }
        }
    }
    catch { $r = New-OpResult 'Failed' $_.Exception.Message }
    $level = if ($r.Status -eq 'Failed') { 'ERROR' } else { 'INFO' }
    Write-OpLog $LogDir "END   $($Item.Id) -> $($r.Status): $($r.Message)" $level
    $r
}
```

---

## 5. Config: the stager's controls

```powershell
# config.psd1 - one per copy of the app. The stager edits this; the code never changes.
@{
    AppId       = 'ContosoSelfHelp'      # names the log folder under %LOCALAPPDATA%
    Title       = 'Contoso Self-Help'
    Tagline     = 'Fix common problems yourself. Everything here is safe to run.'
    Version     = '1.0.0'
    Audience    = 'EndUser'              # EndUser | Admin - which items and how much detail this copy shows
    ContentPath = 'content'              # relative to the app, or a full path (e.g. a share)
    DryRun      = $false                 # $true: every item reports what it would do, and changes nothing
    SupportText = 'If this did not fix it, click "Send details to IT" and call the service desk on 1234.'
}
```

Put in config anything a stager might reasonably change for one audience: branding, audience, where
content lives, feature switches, defaults and **locked** choices. Leave out anything that is really a code
change. **Never put secrets in config** — see file 00 §6.

---

## 6. The front end: built from the content

The front end reads config, discovers content, and builds one card per item the audience may see. Items
that need rights the runner lacks are shown disabled with a reason rather than hidden, so users learn that
IT can help. Broken items are reported to admin runners only.

```powershell
# file: MyApp.ps1
[CmdletBinding()]
param([switch]$Check)   # -Check: validate config and content, print a report, exit 0/1. No window.

$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot
. (Join-Path $Root 'lib\Operations.ps1')
$Config = Import-PowerShellDataFile (Join-Path $Root 'config.psd1')
$Content = if ([IO.Path]::IsPathRooted($Config.ContentPath)) { $Config.ContentPath } else { Join-Path $Root $Config.ContentPath }
$LogDir = Join-Path $env:LOCALAPPDATA "$($Config.AppId)\logs"
$Items = @(Get-StagedItem $Content)

if ($Check) {
    foreach ($i in $Items) {
        '{0,-4} {1,-22} {2}' -f $(if ($i.Valid) { 'OK' } else { 'BAD' }), $i.Id, $(if ($i.Valid) { "$($i.Name)  [$($i.Audience -join ',')]" } else { $i.Problem })
    }
    if (@($Items | Where-Object { -not $_.Valid }).Count) { exit 1 } else { exit 0 }
}

Import-Module (Join-Path $Root 'runtime\PoshUI.Canvas\PoshUI.Canvas.psd1') -Force

# Values baked into action text are escaped: they come from the stager's files, not from code.
$q = { param($s) ([string]$s) -replace "'", "''" }
$libQ = & $q (Join-Path $Root 'lib\Operations.ps1'); $contentQ = & $q $Content; $logQ = & $q $LogDir
$dry = [bool]$Config.DryRun

function New-RunAction($Item) {
    $confirm = if ($Item.Confirm) { "if (-not (Show-UICanvasDialog -Title 'Please confirm' -Message '$(& $q $Item.Confirm)')) { return }" } else { '' }
    [scriptblock]::Create(@"
$confirm
Set-UICanvasState status 'Working on: $(& $q $Item.Name)...'
Start-UICanvasAsync {
    try {
        . '$libQ'
        `$it = Get-StagedItem '$contentQ' | Where-Object Id -eq '$(& $q $Item.Id)'
        `$r = Invoke-StagedItem `$it '$logQ' -DryRun:`$$dry
        Set-UICanvasState status ('{0}: {1}' -f `$it.Name, `$r.Message)
        `$sev = switch (`$r.Status) { 'Success' { 'success' } 'Failed' { 'error' } default { 'warning' } }
        Show-UICanvasToast `$r.Message -Severity `$sev
    } catch { Show-UICanvasToast `$_.Exception.Message -Severity error }
}
"@)
}

$isAdmin = Test-IsAdmin
$audience = $Config.Audience
$visible = @($Items | Where-Object { $_.Valid -and ($_.Audience -contains $audience) })
$broken = @($Items | Where-Object { -not $_.Valid })

New-PoshUICanvas -Title $Config.Title -Theme Dark -Width 1000 -Height 700 -MinWidth 760 -MinHeight 520 | Out-Null
Set-UITheme -Preset Slate -Accent Sky | Out-Null
Add-UICanvasPage -Title 'Home' -Layout VStack -Spacing 10 | Out-Null
New-UICanvasState @{ status = 'Ready. Pick something to run.' }

Add-UICanvasLabel $Config.Title -FontSize 22 -FontWeight Bold
Add-UICanvasLabel $Config.Tagline -FontSize 13 -Foreground '#94A3B8'
if ($dry) { Add-UICanvasBanner 'Dry run: nothing on this computer will be changed.' -Severity Warning }
if ($audience -eq 'Admin' -and $broken.Count) {
    Add-UICanvasBanner ('Skipped {0} staged item(s): {1}' -f $broken.Count, (($broken | ForEach-Object { "$($_.Id) - $($_.Problem)" }) -join '; ')) -Severity Error
}

Add-UICanvasPanel -Layout Grid -Columns 3 -Spacing 12 -Children {
    foreach ($it in $visible) {
        $blocked = $it.RequiresAdmin -and -not $isAdmin
        $glyph = [string][char][Convert]::ToInt32($it.Icon, 16)
        $tip = if ($blocked) { 'Needs administrator rights - ask your IT team.' } else { $it.Description }
        $cardArgs = @{ Layout = 'VStack'; Spacing = 6; Padding = 16; Tooltip = $tip; Enabled = (-not $blocked)
            Properties = @{ CornerRadius = 12; HoverScale = 1.03 } }
        if (-not $blocked) { $cardArgs.Action = New-RunAction $it }
        $itName = $it.Name; $itDesc = $it.Description   # aliased: -Children blocks shadow common names
        Add-UICanvasCard @cardArgs -Children {
            Add-UICanvasIcon $glyph -FontSize 22 -Foreground $(if ($blocked) { '#5B6B82' } else { '#7DC4FF' }) -Properties @{ HAlign = 'Left' }
            Add-UICanvasLabel $itName -FontSize 15 -FontWeight SemiBold -Foreground $(if ($blocked) { '#8B98AC' } else { '#E2E8F0' })
            Add-UICanvasLabel $itDesc -FontSize 12 -Foreground '#94A3B8'
            # Say WHY it is unavailable, on the card itself - a disabled card alone just looks broken.
            if ($blocked) { Add-UICanvasLabel 'Needs administrator rights - ask IT' -FontSize 11 -Foreground '#FCD34D' }
        }
    }
}

Add-UICanvasLabel -Bind '{status}' -FontSize 13 -Properties @{ Margin = '0,8,0,0' }
Add-UICanvasLabel $Config.SupportText -FontSize 12 -Foreground '#94A3B8'
Add-UICanvasButton 'Send details to IT' -Style Secondary -Properties @{ HAlign = 'Left' } -Action ([scriptblock]::Create(@"
    try {
        `$zip = Join-Path ([Environment]::GetFolderPath('Desktop')) ('$(& $q $Config.AppId)-details-{0:yyyyMMdd-HHmm}.zip' -f (Get-Date))
        Compress-Archive -Path '$logQ\*' -DestinationPath `$zip -Force
        Show-UICanvasToast "Saved `$(Split-Path `$zip -Leaf) to your desktop. Attach it to your ticket." -Severity success
    } catch { Show-UICanvasToast "No details to send yet: `$(`$_.Exception.Message)" -Severity warning }
"@))
Add-UICanvasLabel ('{0} v{1}  |  logs: {2}' -f $Config.Title, $Config.Version, $LogDir) -FontSize 10 -Foreground '#5B6B82'

Show-PoshUICanvas | Out-Null
```

For an **admin** audience, the same pattern grows: a grid of items with status, a dry-run toggle, bulk
"run selected", raw result data in a console, and the log tail (blueprints 2, 7 and 12). The discovery,
validation, contract and logging do not change.

---

## 7. Checking before you hand it over

The stager's gate, in order:

1. **`MyApp.ps1 -Check`** — validates config and every content item without opening a window; exit code 0
   means everything is valid. Run it after every content change.
2. **A headless build of the UI** — `Show-PoshUICanvas -Validate` (or set `$env:POSHUI_CANVAS_NOLAUNCH = '1'`)
   builds the whole definition without launching the engine, so a typo in a cmdlet fails here, not on a
   user's machine.
3. **Run it as the runner.** Log on as (or `runas /user:`) a standard user, from where they will run it,
   and click every item. Then look at every page — a clean log does not mean it looks right.
4. **Read the log** it wrote under `%LOCALAPPDATA%\<AppId>\logs` — it is what you will have to diagnose from.

---

## 8. Delivery

### Launching
The runner double-clicks a `.cmd`, never a `.ps1` (which opens in Notepad by default):

```bat
@echo off
rem Start-MyApp.cmd - no console window left behind, STA for WPF.
start "" powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -WindowStyle Hidden -File "%~dp0MyApp.ps1" %*
```

### Channels
| Channel | Notes |
|---|---|
| Zip / USB | Tell the runner to extract it first and **unblock** it (`Get-ChildItem -Recurse \| Unblock-File`) — downloaded files carry a mark that makes PowerShell refuse them |
| File share | Works for a handful of users; for many, copy it locally first (the launcher can `robocopy` to `%LOCALAPPDATA%` and start from there) — share hiccups mid-run are hard to diagnose |
| Intune / ConfigMgr | Install the folder to `%ProgramFiles%\<AppId>` (read-only for users — hence logs in `%LOCALAPPDATA%`) and put a Start-menu shortcut to the `.cmd` |
| Admin toolkit | Copy the folder; admins run `.\MyApp.ps1` or the `.cmd` elevated |

### Execution policy and signing
- `-ExecutionPolicy Bypass` on the command line works **unless Group Policy enforces a policy** — then it is
  ignored. In `AllSigned` environments, **Authenticode-sign** `MyApp.ps1`, `lib\*.ps1` and every content
  script (`Set-AuthenticodeSignature` with a code-signing certificate). `PoshUI.exe` is already signed.
- Re-sign after every change, including the stager's content edits.

### Versioning and updates
- Show the version on screen and write it to every log line; bump it on every release.
- Update by replacing the folder, never by editing a copy that is in use.
- Content can be updated on its own (a new `content\` folder) as long as `-Check` passes against the
  current code.

---

## 9. Checklist

**For the author**
- [ ] No item, path or setting is named in code — it is all discovered from content or read from config.
- [ ] `lib\` has no UI cmdlets; every operation returns a result and logs; nothing throws to the UI.
- [ ] Logs under `%LOCALAPPDATA%`; a "send details to IT" action exists.
- [ ] `-Check` exists and exits non-zero on any problem.

**For the stager**
- [ ] `-Check` passes. The headless build passes.
- [ ] Clicked through as a standard user, from the real location, and looked at every page.
- [ ] Audience, dry run, branding and support text in `config.psd1` are right for these runners.
- [ ] Signed, if the environment requires it. Version bumped.

**For whoever runs it**
- [ ] Every message is written for them (end user: plain words; admin: detail and codes).
- [ ] Anything they cannot do is visibly disabled with the reason — not silently missing, not failing.
