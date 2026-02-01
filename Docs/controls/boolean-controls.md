# Boolean Controls

Boolean controls allow users to toggle settings on or off, representing simple true/false values.

## Checkbox

The classic input for binary options. Ideal for simple configuration flags.

### Basic Usage

```powershell
Add-UICheckbox -Step 'Config' -Name 'EnableLogging' -Label 'Enable detailed logging'
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-Default` | Boolean | Initial state: `$true` (checked) or `$false` (unchecked). |
| `-Mandatory` | Switch | If set, the checkbox *must* be checked to proceed (useful for "I Accept" terms). |

---

## Toggle Switch

A modern, Windows 11-style sliding switch. Functionally identical to a checkbox but visually more distinct for "on/off" settings.

### Basic Usage

```powershell
Add-UIToggle -Step 'Features' -Name 'AdvancedMode' -Label 'Advanced Features' -Default $true
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-Default` | Boolean | Initial state: `$true` (On) or `$false` (Off). |

## Technical Note

Both controls return standard PowerShell boolean types in the result object:
- **Checkbox**: Returns `$true` or `$false`.
- **Toggle**: Returns `$true` or `$false`.

Next: [Numeric & Date Controls](./numeric-date-controls.md)
