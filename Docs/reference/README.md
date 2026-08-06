# PoshUI Cmdlet Reference

Complete, per-module reference for **every** exported PoshUI cmdlet, with full parameter
tables (type, required, accepted values, default, description) and examples. These pages are
**generated from each module's runtime metadata and comment-based help** (PoshUI 1.3.1), so
they match the shipping code exactly.

For concepts, patterns, and the ScriptCard/dot-sourcing deep-dive, start with the
[**Authoring Guide**](../POSHUI_AUTHORING_GUIDE.md).

## Reference pages

| Module | Use for | Reference |
|---|---|---|
| **PoshUI.Wizard** | Step-by-step input forms with validation | [Wizard-Cmdlets.md](Wizard-Cmdlets.md) (27 cmdlets) |
| **PoshUI.Dashboard** | Monitoring dashboards, ScriptCards, visualization cards | [Dashboard-Cmdlets.md](Dashboard-Cmdlets.md) (20 cmdlets) |
| **PoshUI.Workflow** | Sequential tasks, approval gates, reboot/resume | [Workflow-Cmdlets.md](Workflow-Cmdlets.md) (21 cmdlets) |

> **Import exactly one module per script.** All three export overlapping names (`Add-UIStep`,
> `Set-UIBranding`, `Set-UITheme`, `Add-UICard`, `Add-UIBanner`, `Set-UIConfiguration`,
> `Get-UIConfiguration`, `Clear-PoshUI*`). The cmdlet may differ slightly between modules — for
> example **Dashboard `Add-UICard` requires `-Name`; Wizard `Add-UICard` does not** — so always
> consult the page for the module you imported.

## How to read the parameter tables

Each cmdlet section lists its synopsis, description, aliases, full syntax, a parameter table,
and examples. Table columns:

- **Required** — `Yes` if the parameter is `[Parameter(Mandatory)]` in at least one parameter set.
- **Accepted values** — the allowed values from a `[ValidateSet(...)]`, or the range from a
  `[ValidateRange(min,max)]`. Blank means any value of the listed type.
- **Default** — the value used when the parameter is omitted (from the cmdlet's own default).
- **Type** — `switch` = a flag (no value); `scriptblock` = `{ ... }`; `hashtable` = `@{ ... }`;
  `string[]`/`object[]` = arrays.

## Parameter attributes & validation (what to put in your scripts)

The reference tables expose three kinds of constraint the cmdlets enforce:

| You see in the table | Meaning in your call |
|---|---|
| Required = `Yes` | You must supply the parameter, or the cmdlet throws. |
| Accepted values = `` `A` ``, `` `B` `` | Only those literal values are accepted (`ValidateSet`). |
| Accepted values = `1–64` | Numeric value must fall in that inclusive range (`ValidateRange`). |
| Type = `switch` | Present = on; omit = off. Don't pass `$true`/`$false`. |

### ScriptCard parameter → control mapping (Dashboard)

`Add-UIScriptCard` is special: it auto-discovers the parameters of the script you give it
(`-ScriptBlock`/`-ScriptPath`) and builds an input dialog. The **attributes you put on your
script's `param()` block** drive the generated controls:

| Attribute / type on your script param | Generated control |
|---|---|
| `[ValidateSet('A','B')]` | Dropdown |
| `[int]` / `[ValidateRange(1,10)]` | Numeric (bounded) |
| `[switch]` | Toggle |
| `[bool]` | Checkbox |
| `[datetime]` | Date picker |
| `[SecureString]` / `[PSCredential]` | Password (masked) |
| `[string[]]` | Multi-line text |
| `[ValidateScript({ Test-Path $_ -PathType Leaf })]` | File picker |
| `[ValidateScript({ Test-Path $_ -PathType Container })]` | Folder picker |
| `[Parameter(Mandatory)]` / `[ValidateNotNullOrEmpty()]` | Field marked required |
| `[Parameter(HelpMessage='...')]` | Field tooltip |
| `= <literal>` default | Pre-filled value |

See the [Authoring Guide §4](../POSHUI_AUTHORING_GUIDE.md#4-scriptcard-deep-dive) for the full
ScriptCard treatment, including dot-sourcing external function libraries.

### The `$PoshUIWorkflow` task API (Workflow)

Inside an `Add-UIWorkflowTask -ScriptBlock { ... }`, the `$PoshUIWorkflow` object is available:
`UpdateProgress(<percent>, '<message>')`, `WriteOutput('<text>', '<INFO|WARN|ERROR>')`,
`SetData('<key>', <value>)`, `GetData('<key>')`, `RequestReboot()`.

## Regenerating these pages

The pages are produced by reflecting over the imported module (`Get-Command`, `Get-Help`,
parameter attributes). To regenerate after a code change, re-run the documentation generator
against each module — see the commit that introduced this folder for the generator script.
