# Display Controls

Display controls are used to provide information, branding, or feedback to the user without collecting specific input data.

## Banner

The `Banner` control creates a visual header at the top of a wizard or dashboard step. It supports titles, subtitles, icons, and even progress bars.

### Basic Usage

```powershell
Add-UIBanner -Step 'Welcome' -Title 'Welcome to the Setup' `
             -Subtitle 'This wizard will guide you through the process.' `
             -Icon '&#xE8BC;' -Type 'info'
```

### Key Features

- **Styling**: Choose from preset types (`info`, `success`, `warning`, `error`) or customize colors.
- **Icons & Images**: Use Segoe MDL2 glyphs or point to a custom image file.
- **Progress**: Show a task progress bar within the banner using `-ProgressValue`.
- **Carousel**: Display multiple messages that rotate automatically using `-CarouselItems`.

---

## Card (Info Display)

Informational cards are used to display boxes of text, tips, or links. They are ideal for breaking up long forms with helpful context.

### Basic Usage

```powershell
Add-UICard -Step 'Config' -Title 'Important Note' `
           -Content 'Please ensure you have administrative rights before proceeding.' `
           -Type 'Warning' -Icon '&#xE7BA;'
```

### Key Features

- **Types**: Styled variants for `Info`, `Success`, `Warning`, `Error`, and `Tip`.
- **Links**: Add clickable web links using `-LinkUrl` and `-LinkText`.
- **Customization**: Control background colors, corner radius, and text colors.

## Technical Note

Display controls do **not** return values in the wizard results hashtable. They are purely for visual communication.

Next: [Dynamic Controls](./dynamic-controls.md)
