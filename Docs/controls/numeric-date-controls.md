# Numeric & Date Controls

These controls allow users to provide precise numeric values or select dates using a visual calendar.

## Numeric (Number Spinner)

The `Numeric` control provides a spinner-style input for numbers, allowing users to increment or decrement values using arrows or by typing directly.

### Basic Usage

```powershell
Add-UINumeric -Step 'Config' -Name 'Cores' -Label 'CPU Cores' -Minimum 1 -Maximum 32 -Default 4
```

### Features

- **Bounds Enforcement**: Use `-Minimum` and `-Maximum` to restrict the allowable range.
- **Increment Control**: Use `-Increment` to define the step size when using the arrows (default is 1).
- **Decimal Support**: Use the `-AllowDecimal` switch to allow non-integer values.
- **Formatting**: Use the `-Format` parameter to display values as currency, percentage, or specific decimal counts (e.g., `'C2'`, `'P0'`, `'N2'`).

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-Minimum` | Double | The lowest allowed value. |
| `-Maximum` | Double | The highest allowed value. |
| `-Increment` | Double | The value added/subtracted by spinner buttons. |
| `-AllowDecimal`| Switch | Allows floating-point numbers. |
| `-Format` | String | Standard .NET numeric format string. |

---

## Date Picker

The `Date` control provides a text input that opens a calendar dropdown when clicked, ensuring users select a valid date in the correct format.

### Basic Usage

```powershell
Add-UIDate -Step 'Config' -Name 'ScheduleDate' -Label 'Deployment Date' `
           -Minimum (Get-Date) `
           -Maximum (Get-Date).AddMonths(1)
```

### Features

- **Range Restriction**: Use `-Minimum` and `-Maximum` to limit the dates users can pick (e.g., prevent past dates).
- **Custom Formatting**: Use the `-Format` parameter to change how the date is displayed (e.g., `'yyyy-MM-dd'`).
- **Standard Type**: Returns a standard PowerShell `[DateTime]` object in the result.

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-Minimum` | DateTime | Earliest selectable date. |
| `-Maximum` | DateTime | Latest selectable date. |
| `-Format` | String | Custom date display format. |

Next: [Path Controls](./path-controls.md)
