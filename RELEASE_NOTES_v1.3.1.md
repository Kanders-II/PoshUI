# 🔒 PoshUI v1.3.1 — Security Hardening & Bug Fixes

> **Drop-in upgrade.** No changes to the public cmdlet API — all existing scripts work without modification.

---

## 🛡️ Security Fixes

### Script Injection Prevention in `ConvertTo-UIScript`
`ConvertTo-UIScript` (PoshUI.Wizard and PoshUI.Workflow) now escapes **every interpolated value** — titles, labels, choices, defaults, branding keys/values, step metadata, and card/banner properties — for safe embedding in single-quoted PowerShell literals, and validates control/step names as safe identifiers.

Previously, a value containing a single quote (or one sourced from dynamic/external data) could break out of the generated string and execute arbitrary code when the wizard ran.

### Repaired Value Escaper
`ConvertTo-SafeScriptValue`'s boolean branch used `return if (...)`, which threw a runtime error. Fixed across all three modules.

### Removed Predictable Script Disclosure
`Show-PoshUIWizard` / `Show-PoshUIWorkflow` no longer write the generated script to a predictable `%TEMP%\PoshUI_*.ps1` path on every run. The debug dump is now gated behind `-AppDebug` and written via a hardened secure-temp helper (cryptographically random filename + restrictive ACL).

### Stronger Workflow-State Integrity
`Protect-WorkflowState` now uses **DPAPI authenticated encryption** instead of an HMAC keyed on guessable values (username + computer name). State files are written in the new `POSHUI_STATE_V2` format; legacy `V1` files are still read.

### Signature Policy No Longer Downgraded
Calling `Show-*` without `-RequireSignedScripts` no longer forces `POSHUI_SIGNATURE_MODE=Disabled` over a stricter environment or organization policy setting — the prior value is respected and restored.

### Temp-Directory Hardening
`New-SecureTempFile` / `New-SecureTempScript` now re-assert the restrictive directory ACL on **every run**, not only when the directory is first created.

### Secret Redaction in Persisted State
`Set-UIState` now redacts fields with secret-sounding names (`password`, `token`, `credential`, etc.) and any `SecureString` / `PSCredential` values before writing form data to the registry.

---

## 🐛 Bug Fixes

- **Fixed OptionGroup attribute being dropped** — a stray `} else {` in `ConvertTo-UIScript` made the `[WizardOptionGroup(...)]` attribute (choices + orientation) unreachable; OptionGroup controls now render correctly in generated scripts
- **Fixed Date default double-append** — a `DateTime` default value was emitted twice, producing a malformed parameter default; date defaults now render exactly once (`DateTime` formatted as `yyyy-MM-dd`, string values passed through unchanged)
- **Fixed verbose logging** — corrected `${var.Length}` string interpolation in workflow state encryption log messages

---

## 📦 What's Included

```
PoshUI/
├── PoshUI.Wizard/          # Wizard module (hardened script generator)
├── PoshUI.Dashboard/       # Dashboard module
├── PoshUI.Workflow/        # Workflow module (DPAPI state, hardened generator)
├── Examples/               # All v1.3.0 examples (unchanged)
├── Docs/                   # Documentation site
├── bin/                    # Signed PoshUI.exe v1.3.1
└── README.md
```

---

## ⚠️ Upgrade Note

Workflow state files saved by v1.3.1 use the new **`POSHUI_STATE_V2`** format and **cannot be read by v1.3.0 or earlier**. Any in-progress workflows should be completed before upgrading. State files written by older versions continue to work normally.

---

## 🚀 Installation

1. Download the release package
2. Extract to your preferred location
3. Import the module you need:

```powershell
# For Wizards
Import-Module .\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1

# For Dashboards
Import-Module .\PoshUI\PoshUI.Dashboard\PoshUI.Dashboard.psd1

# For Workflows
Import-Module .\PoshUI\PoshUI.Workflow\PoshUI.Workflow.psd1
```

---

## 🛠️ System Requirements

| Requirement | Version |
|---|---|
| Operating System | Windows 10/11 (64-bit) |
| PowerShell | Windows PowerShell 5.1 |
| .NET Framework | 4.8 (included with Windows 10/11) |
| Permissions | User-level (no admin required for most features) |

---

## 📖 Documentation

Full documentation: **https://kanders-ii.github.io/PoshUI/**

---

## 🤝 Getting Help

- **Documentation:** https://kanders-ii.github.io/PoshUI/
- **Issues:** [GitHub Issues](https://github.com/Kanders-II/PoshUI/issues)
- **Examples:** Check the `Examples/` folder for working demos

---

*Made with ❤️ for the PowerShell Community*

[Documentation](https://kanders-ii.github.io/PoshUI/) • [GitHub](https://github.com/Kanders-II/PoshUI) • [Report Issue](https://github.com/Kanders-II/PoshUI/issues)
