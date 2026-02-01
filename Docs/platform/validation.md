# Validation

PoshUI includes a built-in validation engine that ensures user input meets your requirements before the automation logic proceeds. This reduces errors and improves the reliability of your scripts.

## Types of Validation

### 1. Mandatory Fields
The most basic form of validation. Marking a control as `-Mandatory` ensures the user provides a value.

```powershell
Add-UITextBox -Step 'User' -Name 'Email' -Label 'Email Address' -Mandatory
```

- **Effect**: The "Next" or "Finish" button remains disabled until all mandatory fields on the current step are filled.

### 2. Regex Pattern Validation
Available for `TextBox`, `MultiLine`, and `Password` controls. You can specify a regular expression that the input must match.

```powershell
Add-UITextBox -Step 'Config' -Name 'ServerName' -Label 'Server Name' `
              -ValidationPattern '^[A-Z]{3}-[0-9]{3}$' `
              -ValidationMessage 'Format must be AAA-000 (e.g. SRV-101)'
```

- **ValidationMessage**: This custom text is displayed in a tooltip or error label when the input doesn't match the pattern.

### 3. Range Validation
Specific to `Numeric` and `Date` controls.

```powershell
# Numeric Range
Add-UINumeric -Step 'Config' -Name 'InstanceCount' -Minimum 1 -Maximum 10

# Date Range
Add-UIDate -Step 'Schedule' -Name 'StartDate' -Minimum (Get-Date)
```

- **Effect**: The UI prevents the user from selecting values outside these bounds.

### 4. Choice Validation
For `Dropdown` and `ListBox` controls, the user is restricted to the options provided in the `-Choices` or `-ScriptBlock` parameters.

## Live Feedback

PoshUI provides real-time feedback as the user types:
- **Visual Cues**: Invalid fields are highlighted with a red border.
- **Error Messages**: Hovering over an invalid field shows the `ValidationMessage`.
- **Button State**: Navigation buttons automatically enable/disable based on the validity of the current step.

## Server-Side Validation

While UI validation catches immediate errors, you should always perform additional logic checks within your `ScriptBody` or task scripts.

```powershell
Show-PoshUIWizard -ScriptBody {
    if (-not (Test-Connection $ServerName -Count 1 -Quiet)) {
        throw "Server $ServerName is not reachable. Please check the name and try again."
    }
}
```

Next: [Branding (Configuration)](../configuration/branding.md)
