# 🚀 PoshUI v1.5.0 — Ship Your Script as an App

Two things in this release: **PoshUI.exe can now be your app's launcher**, so users start a PoshUI app from an
icon instead of a `.ps1`; and **light/dark switching is live**, which fixes cards that ignored a theme toggle
([#7](https://github.com/Kanders-II/PoshUI/issues/7)).

**Nothing breaks.** Existing scripts run as they are. Without an app settings file, PoshUI.exe behaves
exactly as before.

---

## 🖱️ Double-Click to Start: the App Launcher

Until now, starting a PoshUI app meant running a `.ps1` file. Double-clicking one opens Notepad, so authors
wrapped it in a `.cmd` file that flashed a console and showed the batch-file icon.

Now one command makes the app its own exe:

```powershell
New-PoshUIApp -Config .\MyApp.app.json -Destination D:\Out\MyApp
```

```
MyApp\
  MyApp.exe          PoshUI.exe, copied byte for byte (still signed)
  MyApp.exe.config
  MyApp.app.json     which script, icon, admin, shortcuts
  MyApp.ps1
```

The settings file is all there is to configure:

```json
{
  "Name": "My App",
  "Script": "MyApp.ps1",
  "RequireAdmin": true,
  "Icon": "assets\\myapp.ico",
  "LogDirectory": "logs",
  "Shortcuts": [
    { "Location": "Desktop",   "Name": "My App" },
    { "Location": "StartMenu", "Name": "My App", "Folder": "My Company" }
  ]
}
```

What the user gets when they double-click `MyApp.exe` or its shortcut:

- **One process.** The script runs inside PoshUI, so there's no `powershell.exe` and no console window.
  `Show-PoshUICanvas` opens the window in the same process and returns results as before.
- **Your icon** on the window, the taskbar and the shortcuts. A `.ico` or a square `.png` both work.
- **A UAC prompt only if needed.** With `"RequireAdmin": true` the launcher relaunches itself elevated, and
  cancelling just exits.
- **Arguments pass through.** `MyApp.exe -Live` runs `MyApp.ps1 -Live`.
- **Failures are visible.** Script output goes to the app's log. If the script fails before its window
  appears, the user sees that output in a message box instead of nothing happening. The script's `exit N`
  becomes the exe's exit code, which installers and task schedulers can check.

### Shortcuts

```
MyApp.exe --create-shortcuts               per-user Desktop and Start menu, as listed in the settings
MyApp.exe --create-shortcuts --all-users   for every user (run elevated, e.g. from an Intune or ConfigMgr install)
MyApp.exe --remove-shortcuts               remove them again
```

### No re-signing per app

A signature covers a file's contents, not its name. `New-PoshUIApp` copies the signed PoshUI.exe unchanged,
so **`MyApp.exe` is signed too**, and building an app never needs a code-signing certificate.

If you want the icon inside the exe file itself (as Explorer shows it), `-StampIcon` writes it in, together
with the UAC shield for admin apps. That changes the exe, so pair it with `-Sign`.

Paths in the settings must stay inside the app folder. Rooted paths, UNC paths and `..` are refused, because
the settings decide which script runs with the launcher's rights.

**Guide: [Staging & Delivery → Launching](https://kanders-ii.github.io/PoshUI/agent/11-staging-and-delivery#launching)**

---

## 🌗 Live Light / Dark Switching

The title-bar theme button, and the new `Set-UICanvasTheme`, now switch the whole app **in place**. Nothing
restarts, nothing typed is lost, and it's safe while work is running.

```powershell
Set-UICanvasTheme Toggle      # or Light / Dark
Get-UICanvasTheme             # 'Light' or 'Dark'
```

For an app written in one palette, `Set-UITheme -ColorMap` gives each of its own colours a counterpart in the
other theme. Glows and gradients are included, and you don't need a second copy of the UI:

```powershell
Set-UITheme -ColorMap @{ '#0A0E14' = '#8E939E'; '#22D3EE' = '#22356A' } -ColorMapMode Light
```

Glows are hidden in the mapped theme unless you add `-KeepGlow`.

---

## 🔧 Fixed

- **Cards didn't follow a theme toggle** ([#7](https://github.com/Kanders-II/PoshUI/issues/7)). After you
  toggled light/dark from the title bar, `Add-UICanvasCard` kept its old background until you moved to another
  page and back. The card read the theme colour once, when the page was built. Every themed colour is now live:
  cards, text and inputs change at once on the open page, and pages built later use the current theme. The
  issue's own script was used to verify it. It uses `Set-UITheme -Light @{ CardBackground = '#FFFFFF' } -Dark
  @{ CardBackground = '#2D2D30' }`, and the card now alternates between exactly those colours on every toggle.
- **Themed tabs.** `Add-UICanvasTabs` used to draw the stock light Windows tab control. Tabs now follow
  `Set-UITheme`.
- **Buttons inside a modal window run.** A `-Modal` window shown with `Show-UICanvasWindow` blocked its own
  buttons, `Close-UICanvasWindow` included. They work now.
- **Leftover diagnostics config.** `PoshUI.exe.config` no longer carries an unused trace-listener block from the
  old PoshWizard name.

---

## 📦 Versions

| Component | Version |
|---|---|
| PoshUI engine (`PoshUI.exe`) | **1.5.0** |
| PoshUI.Canvas | **1.5.0** (requires engine 1.5.0 or later for the launcher and live theming) |
| PoshUI.Wizard / Dashboard / Workflow | 1.3.1 (unchanged) |

`PoshUI.exe` is Authenticode-signed. Check it with `Get-AuthenticodeSignature .\bin\PoshUI.exe`. App exes
made by `New-PoshUIApp` carry the same signature.

---

## 📋 Requirements

| Requirement | Version |
|---|---|
| Operating System | Windows 10/11 (64-bit) |
| PowerShell | Windows PowerShell 5.1 |
| .NET Framework | 4.8 (included with Windows 10/11) |
| Permissions | User-level (admin only for apps that set `RequireAdmin`, or for `--all-users` shortcuts) |

---

## 📖 Documentation

Full documentation: **https://kanders-ii.github.io/PoshUI/**

- [Staging & Delivery](https://kanders-ii.github.io/PoshUI/agent/11-staging-and-delivery): the launcher, settings, shortcuts and signing
- [Theming](https://kanders-ii.github.io/PoshUI/agent/05-theming): live switching and colour maps

---

*Made with ❤️ for the PowerShell Community*

[Documentation](https://kanders-ii.github.io/PoshUI/) • [GitHub](https://github.com/Kanders-II/PoshUI) • [Report Issue](https://github.com/Kanders-II/PoshUI/issues)
