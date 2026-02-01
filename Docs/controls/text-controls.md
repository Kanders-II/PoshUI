# Text Controls

Text controls are used to collect string-based information from users, ranging from short names to multi-line descriptions and secure passwords.

## TextBox

The standard single-line input control for names, server addresses, or short descriptions.

### Basic Usage

```powershell
Add-UITextBox -Step 'Config' -Name 'ServerName' -Label 'Server Name' -Placeholder 'e.g. SRV-01'
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-Placeholder` | String | Faded text shown when the field is empty. |
| `-MaxLength` | Int | Limits the number of characters the user can type. |
| `-ValidationPattern` | String | Regex to validate input (e.g., `'^[A-Z0-9]+$'`). |
| `-ValidationMessage` | String | Message shown when the regex fails. |

---

## MultiLine

A larger text area designed for notes, scripts, or long descriptions. It supports automatic word wrap and vertical scrolling.

### Basic Usage

```powershell
Add-UIMultiLine -Step 'Config' -Name 'Notes' -Label 'Deployment Notes' -Rows 6
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-Rows` | Int | The initial height of the control in text lines (default: 4). |

---

## Password

A secure input control that masks characters as they are typed. It returns a `SecureString` object for safety.

### Basic Usage

```powershell
Add-UIPassword -Step 'Credentials' -Name 'AdminPassword' -Label 'Administrator Password'
```

### Features

- **Encryption**: Values are handled as `SecureString` throughout the framework.
- **Reveal Button**: An optional eye icon allows users to briefly view the password they typed.
- **Complexity Validation**: Use `-ValidationPattern` to enforce strong password policies.

::: warning
Always handle password results as `SecureString`. Avoid converting them back to plain text unless absolutely necessary for your target cmdlet.
:::

Next: [Selection Controls](./selection-controls.md)
