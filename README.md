<div align="center">

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="Images/Color%20logo%20-%20no%20background.png">
  <source media="(prefers-color-scheme: light)" srcset="Images/Color%20logo%20with%20background.png">
  <img alt="PoshUI Logo" src="Images/Color%20logo%20-%20no%20background.png">
</picture>



[![Version](https://img.shields.io/badge/version-1.4.0-blue.svg)](https://github.com/Kanders-II/PoshUI/releases)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-purple.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net48)
[![PowerShell](https://img.shields.io/badge/PowerShell-5.1-blue.svg)](https://docs.microsoft.com/en-us/powershell/)

</div>

---

## Welcome to PoshUI

PoshUI is a PowerShell UI framework that brings professional interfaces to your automation scripts. Create step-by-step wizards, live monitoring dashboards, and automated workflows—all with PowerShell cmdlets you already know.

This is my contribution to the PowerShell community, combining the flexibility of PowerShell with the polish of WPF to create something that IT professionals can actually use in their daily work. Built with object-oriented programming principles, it focuses on maintainable, modular code while keeping the end-user experience simple and intuitive.

**What PoshUI Does:**  
Turn your PowerShell automation into professional Windows 11-style wizards, dashboards, workflows, and free-form apps—using familiar PowerShell cmdlets. No WPF, XAML, or C# knowledge required.

**Who It's For:**  
IT professionals, system administrators, and DevOps engineers who want to make their automation accessible to help desk teams, colleagues, and end users who prefer a GUI over a command line.

---

## Four PowerShell Modules

PoshUI provides four independent modules, each designed for a specific use case:

### PoshUI.Canvas *(new in v1.4.0)*
**Free-form apps** — lay out anything, anywhere, with no fixed shell. Where the other three modules give you an
opinionated frame, Canvas gives you a blank page and ~85 cmdlets.

A single `.ps1` **is** the application — no XAML, no MVVM, no project scaffolding, no build step:

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

Canvas can express all three of the patterns below in one page — plus tabs, menus, splitters, live reactive
state, charts, animation, secondary windows, and a raw-XAML escape hatch for anything the cmdlets don't cover.

Its **engine-native workflow runner** (`-Engine`) runs steps on their own runspace, so progress and elapsed time
stay live during a long step, with per-step retry, timeouts, skip conditions and reboot-resume.

Because it's flat, declarative, single-file and build-free, Canvas is also an unusually good target to develop
for **with an AI assistant** — see the [Agent Authoring Guide](Docs/agent/README.md), a context pack written to
be handed to a model. → **[About Canvas](Docs/canvas/about.md)**

### PoshUI.Wizard
**Step-by-step guided interfaces** for configuration, deployment, and setup tasks.  
Perfect for collecting user input with validation, then executing your automation logic.

**Full-color PNG emoji icons** in sidebar navigation, banners, and cards *(v1.3.0)*:

![Wizard Emoji Icons](Docs/images/visualization/Wizard_EmojiIcons_Dark.png)

### PoshUI.Dashboard
**Real-time monitoring interfaces** with metrics, charts, and interactive tools.  
Build card-based dashboards that display KPIs, visualize data, and turn scripts into clickable tools.

![Dashboard Example](Docs/images/visualization/Dashboard_ComputerMaintenance_Dark.png)

**ScriptCards** turn PowerShell scripts into clickable tools for end users — with PNG icons:

![ScriptCard Example](Docs/images/visualization/Dashboard_ScriptCards_Dark.png)

### PoshUI.Workflow
**Multi-step automation** with progress tracking and reboot/resume capabilities.  
Orchestrate complex processes like server deployments, software installations, and maintenance tasks.

![Workflow Example](Docs/images/visualization/Workflow.png)

---

## Quick Start

### Installation

**Requirements:**
- Windows 10/11 or Windows Server 2016+ (x64)
- .NET Framework 4.8 (pre-installed on Windows 10+)
- Windows PowerShell 5.1 (included with Windows)

**Download:**
1. Download the latest release from [GitHub Releases](https://github.com/Kanders-II/PoshUI/releases)
2. Extract and unblock files: `Get-ChildItem -Recurse | Unblock-File`

**Or build from source.** This repository holds the source. The PowerShell modules run as they are; the WPF
engine they drive (`PoshUI.exe`) is built from `Launcher\`, which is SDK-style with no third-party packages, so
the .NET SDK alone is enough:

```powershell
git clone https://github.com/Kanders-II/PoshUI.git
cd PoshUI
dotnet build Launcher\Launcher.csproj -c Release
```

The build writes `PoshUI\bin\PoshUI.exe` — where the modules already look for it, so there is nothing to copy
afterwards. See [Building from Source](Docs/development/building-from-source.md).

### Simple Example

```powershell
# Import the Wizard module
Import-Module .\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1

# Create a wizard
New-PoshUIWizard -Title 'Server Setup' -Theme 'Auto'

# Add a step
Add-UIStep -Name 'Config' -Title 'Configuration' -Order 1

# Add controls
Add-UITextBox -Step 'Config' -Name 'ServerName' -Label 'Server Name' -Mandatory
Add-UIDropdown -Step 'Config' -Name 'Environment' -Label 'Environment' `
    -Choices @('Dev', 'Test', 'Prod') -Mandatory

# Execute with optional script body
Show-PoshUIWizard -ScriptBody {
    Write-Host "Configuring $ServerName in $Environment..." -ForegroundColor Cyan
    # Your automation logic here
}
```

---

## Key Features

- **12+ Input Controls** - TextBox, Dropdown, Password, Date, File/Folder pickers, and more
- **Dashboard Visualization** - MetricCards, Charts (Bar/Line/Area/Pie), DataGrids, ScriptCards
- **Dynamic Controls** - Cascading dropdowns with scriptblock data sources
- **Workflow Automation** - Multi-task execution with progress tracking and reboot/resume
- **Light/Dark Themes** - Auto-detect system theme or force Light/Dark mode
- **Theme Toggle** - Sun/moon button in title bar to switch themes at runtime *(v1.3.0)*
- **Dual-Mode Custom Themes** - Define independent color palettes for light and dark modes *(v1.3.0)*
- **PNG Icon Support** - Full-color PNG/ICO icons on steps, cards, banners, and branding *(v1.3.0)*
- **Live Execution Console** - Real-time output display during script execution
- **CMTrace Logging** - Enterprise-ready audit trails
- **Security Hardened** - Injection-safe script generation, DPAPI-encrypted workflow state, ACL-restricted temp files, and optional signed-script enforcement *(v1.3.1)*
- **Free-Form Canvas** - Build any layout from ~85 cmdlets in a single script, no XAML or build step *(v1.4.0)*
- **Engine-Native Workflow Runner** - Steps run on their own runspace with live progress, retry, timeouts, skip conditions, and reboot-resume *(v1.4.0)*
- **Off-Gate Script Cards** - Long-running tasks stream output live without freezing the rest of the app *(v1.4.0)*
- **Declarative Cascading Fields** - Dropdown/ListBox options recompute from other fields with no event wiring *(v1.4.0)*
- **Tabs, Menus, Splitters & Viewbox** - Native cmdlets for the layout chrome that used to require raw XAML *(v1.4.0)*
- **Master/Detail Grids** - `Add-UICanvasDataGrid -OnChange` fires on row select so a grid can drive a detail pane *(v1.4.0)*
- **Per-Monitor V2 DPI** - Stays crisp when moved between monitors with different scaling *(v1.4.0)*
- **Raw XAML Escape Hatch** - Splice any WPF control in, name it, and hand it an `-Actions` scriptblock, so a control the cmdlets do not cover still behaves like one that does *(v1.4.0)*
- **Authoring Diagnostics** - A `-Properties` key nothing read is logged by name instead of being silently dropped, with a hint when the key is real for a different control *(v1.4.0)*
- **Zero Dependencies** - No third-party libraries or NuGet packages

---

## Documentation

**Full documentation:** [https://kanders-ii.github.io/PoshUI](https://kanders-ii.github.io/PoshUI)

The complete documentation includes:
- **Cmdlet Reference** - All PowerShell cmdlets with examples
- **Module Guides** - Canvas, Wizards, Dashboards, and Workflows
- **Control Library** - 12+ input and visualization controls
- **Examples** - Real-world use cases and patterns

**Canvas:** [About Canvas](Docs/canvas/about.md) · [Capability Reference](Docs/Canvas-Reference.md) ·
[Agent Authoring Guide](Docs/agent/README.md) (context pack for building Canvas apps with an AI assistant)

---

## Contributing

PoshUI is my contribution to the PowerShell community. Contributions, feedback, and suggestions from others are welcome!

**How to contribute:**
- Report bugs or request features via [GitHub Issues](https://github.com/Kanders-II/PoshUI/issues)
- Submit pull requests for improvements
- Share your use cases and examples

---

## Icon Attributions

### Icons8

The documentation screenshots use icons provided by [Icons8](https://icons8.com). Icons8 icons are used under their [licensing terms](https://icons8.com/license). If you use these icons in your own projects, please provide appropriate attribution to Icons8.

### Microsoft Fluent Emoji

The documentation screenshots use 3D emoji icons from the [Microsoft Fluent Emoji](https://github.com/microsoft/fluentui-emoji) repository. Fluent Emoji is published by Microsoft under the [MIT License](https://github.com/microsoft/fluentui-emoji/blob/main/LICENSE). These high-quality 3D rendered PNG icons are ideal for use with PoshUI's `-IconPath` parameter.

---

## License

MIT License - See [LICENSE](LICENSE) file for details.

**Maintained by [Kanders-II](https://github.com/Kanders-II)**
