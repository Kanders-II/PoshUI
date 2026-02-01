# Path Controls

Path controls provide a standardized way for users to select files and folders on their local system using native Windows dialogs.

## FilePath

The `FilePath` control displays a text input with a browse button (`...`) that opens a standard file picker dialog.

### Basic Usage

```powershell
Add-UIFilePath -Step 'Config' -Name 'InputFile' -Label 'Select Source File' `
               -Filter '*.ps1' `
               -DialogTitle 'Select Script to Upload'
```

### Features

- **Extension Filtering**: Use the `-Filter` parameter to restrict the types of files users can see (e.g., `'*.json'`, `'*.log;*.txt'`).
- **Existence Validation**: Use the `-ValidateExists` switch to ensure the user selects a file that actually exists before they can proceed.
- **Custom Titles**: Personalize the dialog window title with `-DialogTitle`.

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-Filter` | String | File extension filter (e.g., `'*.txt'`). |
| `-DialogTitle` | String | Title of the browse dialog window. |
| `-ValidateExists` | Switch | Ensures the selected file exists. |

---

## FolderPath

The `FolderPath` control allows users to select a directory on their system.

### Basic Usage

```powershell
Add-UIFolderPath -Step 'Config' -Name 'ExportPath' -Label 'Backup Location' `
                 -Default 'C:\Backups'
```

### Features

- **Modern Browser**: Uses a professional Vista-style folder picker with breadcrumb navigation.
- **Manual Entry**: Users can still type or paste paths directly into the text field.

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-Default` | String | Initial folder path. |

Next: [Display Controls](./display-controls.md)
