# 🚀 PoshUI v1.5.1 — Launchpad: How to Structure a PoshUI App

Every PoshUI example so far has been a single script. **Launchpad** is a learning example: a complete app laid
out as a project, so you can see how to build on PoshUI before you build something of your own. A front end that
only draws, a back end with no UI in it, a content folder other people drop scripts into, a check script, tests,
a launcher exe, and a guide that walks through every layer. Copy it, swap the sample scripts for yours, and keep
the structure.

This release also fixes light/dark switching on pages with a data grid, unreadable data grid rows
([#10](https://github.com/Kanders-II/PoshUI/issues/10)), app launchers started from PowerShell 7, the folder
picker's dialog, the number field's buttons, and the Hello example that shipped broken in 1.5.0.

**Nothing breaks.** Existing scripts run unchanged. Two additions: `Add-UICanvasMarkdown -BasePath` (pictures in
markdown), and `Add-UICanvasDataGrid` takes `-Properties @{ SelectionMode = 'Multiple' }`.

---

## 🚀 Launchpad

`Examples\Launchpad\` in the release package. Double-click `Launchpad.exe`.

<p align="center">
  <img src="https://raw.githubusercontent.com/Kanders-II/PoshUI/main/Images/launchpad.gif" alt="Launchpad, the learning example: a splash screen, pages built from plain scripts, a workflow paused for approval and resumed from the history page, and a live switch to the light theme" width="900">
</p>

Everything in that clip is plain PowerShell and PoshUI cmdlets. You don't build a page for each script: Launchpad
reads a script's comment help and `param()` block and builds its page and form for you. The scripts and workflows
inside are stand-ins, there to show how that works; replace them with your own.

What it shows you how to build:

- **Pages built from content.** Drop a folder into `Content\scripts\` and it appears: the script's comment help
  becomes the card, its `param()` becomes the form (text, number, toggle, dropdown, file and folder pickers),
  `Write-Host` streams into the output, and the last object it returns is a coloured result.
- **Workflows** that chain scripts with decisions: a step's result picks the next step, a step can ask a
  question, ask for more input, show rows to pick from, or wait for an approval. **Decide later** leaves a run
  waiting on disk; it carries on from that step when someone approves it, even after the app was closed.
- **Run history** that a page reads back when you return to it, and a **Settings** page that clears it (never a
  live run, and never a waiting one unless you say so).
- Live light and dark, search and filters, and a splash screen.

### The structure is the point

```
Launchpad\
  Launchpad.exe, .app.json, .config   the launcher - the only things at the top
  Content\scripts\<Name>\<Name>.ps1   a script, and its README.md how-to
  Content\workflows\<Name>.psd1       a workflow, and its .md how-to
  App\Launchpad.ps1                   FRONT END - draws, never works
  App\lib\Runner.ps1                  BACK END  - no UI cmdlet in it
  App\ui\                             frame, colours, workflow pages, splash
  App\config.psd1  Check.ps1  Tests\  settings, the gate, the engine's tests
```

- **Content never needs a code change.** Categories, forms, icons, "used by", everything on the cards is read
  from the scripts and workflows.
- **The back end runs anywhere**: in the window, in `Check.ps1`, in a console, in 30 Pester tests.
- **Drop-ins are checked.** A script that doesn't parse, a name that clashes, a required value left empty, a
  `Read-Host` that would hang: each is caught and explained instead of breaking a run.
- **Safety is a setting.** `RequireSigned` runs only signed scripts. `TimeoutMinutes` stops a runaway. Every
  run has Cancel.

**New guide: [Structuring an App: Building Launchpad](https://kanders-ii.github.io/PoshUI/agent/17-structuring-an-app).**
How to write a script for it, how workflows route, the five ideas the app is built on, and how to make it
yours.

---

## 🔧 Fixed

- **Light/dark switching stopped part-way on any page with a data grid.** Since 1.5.0, theme colours are shared
  live brushes. The data grid's styles froze them, so the next switch failed: the page and title bar changed,
  but cards, text and the grid stayed in the old theme. Styles no longer freeze them, and a frozen brush can no
  longer stop a switch.
- **Data grid rows were unreadable** ([#10](https://github.com/Kanders-II/PoshUI/issues/10)). Every other row
  was a fixed dark navy. Stripes, hover and selection now use tints of the theme's own colours and follow the
  toggle. Use `-Properties @{ AlternatingRowBackground = 'None' }` or a colour to change them.
- **Data grid columns collapsed** on pages that don't give the grid a width: the headers vanished and cells
  were cut to two letters. Columns now fit their content there, and still fill the width when there is one.
- **App launchers started from PowerShell 7** could not find built-in cmdlets such as
  `Import-PowerShellDataFile`. A `MyApp.exe` started from a `pwsh` console, deploy script or installer now runs
  with Windows PowerShell's module path.
- **The folder picker opened the old tree-style "Browse For Folder" window.** `Add-UICanvasFolderPicker` and
  `Select-UICanvasFolder` now open the standard Explorer folder dialog, as the wizard always has.
- **The number field's up/down buttons were unthemed grey blocks.** They are now flat chevrons inside the field
  that follow the theme.
- **Pictures in how-tos.** `![caption](diagram.png)` in a markdown file now finds the image beside the `.md` and
  shows it as a figure with its caption. Before, relative images never loaded.
- **Secondary windows had a white title bar on a dark app.** They now follow the theme, and switch with it.
- **The Hello example in 1.5.0 opened with only its label.** A missing line break had joined two commands, so
  the text box and buttons never appeared.

## ✨ Added

- `Add-UICanvasDataGrid -Properties @{ SelectionMode = 'Multiple' }`: Ctrl/Shift-click selection.
  `Get-UICanvasValue` returns the selected rows; setting the value to `$true` selects all, `$null` clears.
- The release package ships the example launchers (`Launchpad.exe`, `Hello.exe`) pre-built. They are unchanged
  copies of the signed `PoshUI.exe`, so they carry its signature.

---

## 📦 Versions

| Component | Version |
|---|---|
| PoshUI engine (`PoshUI.exe`) | **1.5.1** |
| PoshUI.Canvas | **1.5.1** (requires engine 1.5.1) |
| PoshUI.Wizard / Dashboard / Workflow | 1.3.1 (unchanged) |

`PoshUI.exe` is Authenticode-signed, and so are the example launchers built from it. Check with
`Get-AuthenticodeSignature .\bin\PoshUI.exe`.

---

## 📋 Requirements

| Requirement | Version |
|---|---|
| Operating System | Windows 10/11 or Windows Server 2016+ (64-bit) |
| PowerShell | Windows PowerShell 5.1 |
| .NET Framework | 4.8 (included with Windows 10/11) |
| Permissions | User-level (admin only for apps that set `RequireAdmin`) |

---

## 📖 Documentation

Full documentation: **https://kanders-ii.github.io/PoshUI/**

- [Structuring an App: Building Launchpad](https://kanders-ii.github.io/PoshUI/agent/17-structuring-an-app)
- [Staging & Delivery](https://kanders-ii.github.io/PoshUI/agent/11-staging-and-delivery): launchers, shortcuts, signing

---

*Made with ❤️ for the PowerShell Community*

[Documentation](https://kanders-ii.github.io/PoshUI/) • [GitHub](https://github.com/Kanders-II/PoshUI) • [Report Issue](https://github.com/Kanders-II/PoshUI/issues)
