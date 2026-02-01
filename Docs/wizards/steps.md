# Working with Steps

Steps are the building blocks of your PoshUI interfaces. They represent individual pages or screens that the user navigates through.

## Adding Steps

Use the `Add-UIStep` cmdlet to add a new step to your wizard or dashboard.

```powershell
Add-UIStep -Name 'StepName' -Title 'Step Title' -Order 1
```

### Parameters

| Parameter | Description |
|-----------|-------------|
| `-Name` | Unique identifier for the step (required). |
| `-Title` | Display title shown in the sidebar and header (required). |
| `-Description` | Optional subtitle or description. |
| `-Order` | Numeric display order. Defaults to the sequence added. |
| `-Icon` | Segoe MDL2 icon glyph (e.g., `'&#xE1D3;'`). |
| `-Type` | The type of step: `'Wizard'` (default) or `'Dashboard'`. |
| `-Skippable` | Switch that allows users to skip this step. |

## Step Types

PoshUI supports two primary step types:

### Wizard Steps (Default)
Standard input forms where you can add text boxes, dropdowns, checkboxes, and other interactive controls. These are designed for data collection.

### Dashboard Steps
Grid-based views designed for visualization. Instead of input controls, you add MetricCards, GraphCards, and DataGridCards to these steps.

## Icons

Step icons are displayed in the sidebar. They must use **Segoe MDL2 Assets** glyphs.

```powershell
# Add a step with a network icon
Add-UIStep -Name 'Network' -Title 'Network Config' -Icon '&#xE968;'
```

::: tip
See the [Icons Reference](../configuration/icons.md) for a full list of available glyphs.
:::

## Navigation Flow

By default, steps are displayed in the order of their `-Order` parameter (or the order they were added). Users navigate using the **Next** and **Back** buttons.

- **Mandatory Fields**: The "Next" button is disabled until all mandatory controls on the current step are filled.
- **Skippable Steps**: If a step is marked as `-Skippable`, the user can proceed even if mandatory fields are empty.

Next: [Branding & Customization](./branding.md)
