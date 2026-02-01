# 🎉 PoshUI v1.0.0 Release

<div align="center">

![PoshUI Logo](Images/Color%20logo%20-%20no%20background.png)

**Professional PowerShell UIs Made Simple**

[![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)](https://github.com/asolutionit/PoshUI/releases)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Documentation](https://img.shields.io/badge/docs-online-brightgreen.svg)](https://asolutionit.github.io/PoshUI/)

</div>

---

## Welcome to PoshUI v1.0.0!

We're excited to share the first official release of PoshUI with the PowerShell community! 

PoshUI transforms your PowerShell scripts into professional Windows 11-style interfaces—no WPF, XAML, or C# knowledge required. Just use familiar PowerShell cmdlets to create wizards, dashboards, and workflows that your team will actually enjoy using.

---

## 📦 What's in This Release

### Three Powerful Modules

**🧙 PoshUI.Wizard** - Step-by-step guided interfaces  
Create configuration wizards, deployment tools, and setup assistants with 20+ control types, dynamic dropdowns, and real-time validation.

![Wizard Example](Docs/images/visualization/Wizard_Dark_1.png)

**📊 PoshUI.Dashboard** - Real-time monitoring interfaces  
Build beautiful card-based dashboards with live metrics, charts, and interactive ScriptCards that turn your PowerShell scripts into clickable tools.

![Dashboard Example](Docs/images/visualization/Dashboard_Charts_Light_1.png)

**ScriptCards** turn your scripts into clickable tools:

![ScriptCard Example](Docs/images/visualization/ScriptCard_Dark_2.png)

**⚙️ PoshUI.Workflow** - Automated task orchestration  
Chain PowerShell tasks together with approval gates, error handling, state persistence, and automatic resume-after-reboot capabilities.

![Workflow Example](Docs/images/visualization/Workflow_Dark_Charts.png)

---

## ✨ Key Features

- **Zero Learning Curve** - If you know PowerShell, you already know PoshUI
- **20+ Control Types** - TextBox, Dropdown, Password, Date, File/Folder pickers, and more
- **Dynamic Controls** - Cascading dropdowns with scriptblock data sources
- **Live Execution** - Real-time console output during wizard execution
- **Beautiful Themes** - Auto/Light/Dark themes with Windows 11 styling
- **State Persistence** - Workflows survive reboots and resume automatically
- **Approval Gates** - Built-in checkpoints for critical operations
- **Rich Documentation** - Comprehensive guides and working examples

---

## 🚀 Quick Start

### Installation

1. **Download** the release package
2. **Extract** to your preferred location
3. **Import** the module you need:

```powershell
# For Wizards
Import-Module .\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1

# For Dashboards
Import-Module .\PoshUI\PoshUI.Dashboard\PoshUI.Dashboard.psd1

# For Workflows
Import-Module .\PoshUI\PoshUI.Workflow\PoshUI.Workflow.psd1
```

### Your First Wizard (5 lines of code!)

```powershell
Import-Module .\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1

New-PoshUIWizard -Title "Hello PoshUI" -Description "My first wizard"
Add-UIStep -Name "Welcome" -Title "Welcome" -Order 1
Add-UITextBox -Step "Welcome" -Name "UserName" -Label "Your Name" -Mandatory
Show-PoshUIWizard -ScriptBody {
    Write-Host "Hello, $UserName! Welcome to PoshUI!"
}
```

---

## 📚 What's Included

```
PoshUI/
├── PoshUI.Wizard/          # Wizard module
├── PoshUI.Dashboard/       # Dashboard module  
├── PoshUI.Workflow/        # Workflow module
├── Examples/               # 12 working demo scripts
│   ├── Wizard-AllControls.ps1
│   ├── Wizard-DynamicControls.ps1
│   ├── Dashboard-MultiPageDashboard.ps1
│   ├── Workflow-Complete.ps1
│   └── ... and more!
├── Docs/                   # Full documentation
├── Launcher/               # Pre-built PoshUI.exe
└── README.md              # Getting started guide
```

---

## 🎯 Perfect For

- **Help Desk Tools** - Give your team GUI interfaces for common tasks
- **Deployment Wizards** - Guide users through complex configurations
- **Monitoring Dashboards** - Visualize system health and metrics
- **Automated Workflows** - Chain tasks with approval gates and error handling
- **Self-Service Portals** - Empower users to run automation safely

---

## 📖 Documentation

**Full documentation available at:** [https://asolutionit.github.io/PoshUI/](https://asolutionit.github.io/PoshUI/)

- **Wizards Guide** - Controls, validation, dynamic data sources
- **Dashboards Guide** - Cards, metrics, charts, ScriptCards
- **Workflows Guide** - Tasks, approval gates, state management
- **Examples** - 12 working demos to learn from

---

## 🛠️ System Requirements

- **Operating System:** Windows 10/11 (64-bit)
- **PowerShell:** Windows PowerShell 5.1
- **.NET Framework:** 4.8 (included with Windows 10/11)
- **Permissions:** User-level (no admin required for most features)

---

## 🤝 Getting Help

- **Documentation:** [https://asolutionit.github.io/PoshUI/](https://asolutionit.github.io/PoshUI/)
- **Issues:** [GitHub Issues](https://github.com/ASolutionIT/PoshUI/issues)
- **Examples:** Check the `Examples/` folder for working demos

---

## 💡 What's Next?

This is just the beginning! We're planning:

- PowerShell Gallery publishing
- Additional control types and visualizations
- Enhanced theming and customization
- Community-contributed examples and templates

---

## 🙏 Thank You

Thank you for trying PoshUI! This project is my contribution to the PowerShell community—built to help IT professionals create better tools for their teams.

If PoshUI helps you build something cool, we'd love to hear about it! Share your creations, report bugs, or suggest features on GitHub.

**Happy Automating!**  
*- A Solution IT LLC*

---

## 📄 License

PoshUI is released under the MIT License. See [LICENSE](LICENSE) for details.

---

<div align="center">

**Made with ❤️ for the PowerShell Community**

[Documentation](https://asolutionit.github.io/PoshUI/) • [GitHub](https://github.com/ASolutionIT/PoshUI) • [Report Issue](https://github.com/ASolutionIT/PoshUI/issues)

</div>
