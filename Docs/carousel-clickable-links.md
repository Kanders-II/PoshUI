# Carousel Clickable Links

## Overview

PoshUI carousels now support per-slide clickable links with background images. Each carousel slide can have its own URL that opens in the default browser when the user clicks the "Learn More →" button.

## Features

- **Per-Slide URLs**: Each carousel slide can have a unique `LinkUrl`
- **Clickable Control**: Toggle clickability per slide with the `Clickable` property
- **Background Images**: Add images to carousel slides with opacity control
- **Auto-Rotation**: Automatically rotate through slides with configurable intervals
- **Visual Feedback**: "Learn More →" button appears only on clickable slides
- **Cross-Module Support**: Works in Wizard, Workflow, and Dashboard modules

## Supported Modules

### Wizard Module (PoshUI.Wizard)
- Parameter: `-CarouselItems`
- Use with: `Add-UIBanner`

### Workflow Module (PoshUI.Workflow)
- Parameter: `-CarouselItems`
- Use with: `Add-UIBanner`

### Dashboard Module (PoshUI.Dashboard)
- Parameter: `-CarouselSlides`
- Use with: `Add-UIBanner`
- Note: Requires `-Title` parameter

## Carousel Slide Properties

Each carousel slide is a hashtable with the following properties:

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `Title` | String | No | `''` | Main title text displayed on the slide |
| `Subtitle` | String | No | `''` | Secondary text below the title |
| `Description` | String | No | `''` | Additional descriptive text |
| `BackgroundColor` | String | No | `#2D2D30` | Hex color code (e.g., `#0078D4`) |
| `BackgroundImagePath` | String | No | `''` | Path to background image file |
| `BackgroundImageOpacity` | Double | No | `0.3` | Image opacity (0.0 to 1.0). Use `1.0` for fully visible images |
| `BackgroundImageStretch` | String | No | `Uniform` | Image stretch mode: `Uniform` (centered), `UniformToFill` (fills banner), `Fill`, `None` |
| `LinkUrl` | String | No | `''` | URL to open when slide is clicked |
| `Clickable` | Boolean | No | `$false` | Enable/disable clickability |

## Usage Examples

### Basic Carousel (Wizard/Workflow)

```powershell
$carouselItems = @(
    @{
        Title = 'Welcome'
        Subtitle = 'Get started with PoshUI'
        BackgroundColor = '#0078D4'
        LinkUrl = 'https://github.com/ASolutionIT/PoshUI'
        Clickable = $true
    },
    @{
        Title = 'Documentation'
        Subtitle = 'Learn more about features'
        BackgroundColor = '#107C10'
        LinkUrl = 'https://docs.microsoft.com/powershell'
        Clickable = $true
    }
)

Add-UIBanner -Step "Welcome" `
    -CarouselItems $carouselItems `
    -AutoRotate $true `
    -RotateInterval 5000 `
    -Height 180
```

### Carousel with Background Images (Dashboard)

```powershell
$logoPath = Join-Path $PSScriptRoot 'Logo Files\png\Color logo - no background.png'

$carouselSlides = @(
    @{
        # Image-only slide (no text, no link)
        BackgroundImagePath = $logoPath
        BackgroundImageOpacity = 1.0
        BackgroundImageStretch = 'Uniform'
    },
    @{
        # Text slide with subtle background image
        Title = 'Dashboard Analytics'
        Subtitle = 'Monitor your infrastructure'
        BackgroundColor = '#107C10'
        BackgroundImagePath = $logoPath
        BackgroundImageOpacity = 0.1
        LinkUrl = 'https://docs.microsoft.com/powershell'
        Clickable = $true
    }
)

Add-UIBanner -Step "Overview" `
    -Title "Dashboard" `
    -CarouselSlides $carouselSlides `
    -AutoRotate $true `
    -RotateInterval 5000 `
    -Height 180
```

**Note:** Default `BackgroundImageOpacity` is `0.3` (30% visible). Set to `1.0` for fully visible images.

## Carousel Parameters

### AutoRotate
- **Type**: Boolean
- **Default**: `$false`
- **Description**: Enable automatic slide rotation

### RotateInterval
- **Type**: Integer (milliseconds)
- **Default**: `3000`
- **Range**: 1000-10000
- **Description**: Time between slide transitions

### Height
- **Type**: Integer (pixels)
- **Default**: `180`
- **Description**: Carousel banner height

### NavigationStyle (Dashboard only)
- **Type**: String
- **Values**: `Dots`, `Arrows`, `None`
- **Default**: `Dots`
- **Description**: Navigation control style

## Visual Behavior

### Clickable Slides
- Display a "Learn More →" button in the bottom-right corner
- Button has semi-transparent dark background with white border
- Hover effect darkens the background
- Clicking opens the `LinkUrl` in the default browser

### Non-Clickable Slides
- No "Learn More →" button is displayed
- Slides can still be navigated using carousel controls

## Implementation Details

### C# Classes

**BannerSlide** (`Launcher\ViewModels\BannerViewModel.cs`)
```csharp
[DataMember]
public string LinkUrl { get; set; } = string.Empty;

[DataMember]
public bool Clickable { get; set; } = false;

public bool HasLink
{
    get { return Clickable && !string.IsNullOrEmpty(LinkUrl); }
}
```

### XAML Binding

The "Learn More →" button binds to:
- `Command`: `OpenSlideLinkCommand`
- `CommandParameter`: `CurrentSlide.LinkUrl`
- `Visibility`: Controlled by `CurrentSlide.HasLink`

### Command Handler

**OpenSlideLinkCommand** in `BannerViewModel.cs`:
```csharp
OpenSlideLinkCommand = new RelayCommand(param => OpenSlideLink(param?.ToString()));

private void OpenSlideLink(string url)
{
    if (string.IsNullOrEmpty(url)) return;
    
    try
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        LoggingService.Error($"Failed to open URL '{url}': {ex.Message}", 
            component: "BannerViewModel");
    }
}
```

## Examples

### Wizard-AllControls.ps1
- Demonstrates carousel with 3 slides
- Slides 2 and 3 have clickable links
- First slide has no link (Clickable = $false)

### Workflow-Reboot.ps1
- Demonstrates carousel in workflow context
- All 3 slides are clickable
- Auto-rotates every 4 seconds

### Dashboard-MultiPageDashboard.ps1
- Demonstrates carousel with background images
- All 3 slides are clickable
- Uses `-CarouselSlides` parameter (Dashboard-specific)
- Auto-rotates every 5 seconds

## Troubleshooting

### Carousel Not Displaying
- Ensure at least 2 slides are provided
- Check that `CarouselItems` or `CarouselSlides` parameter is used
- Verify slide properties are correctly formatted as hashtables

### Links Not Working
- Verify `Clickable = $true` is set on the slide
- Check that `LinkUrl` is a valid URL
- Ensure the URL includes the protocol (e.g., `https://`)
- Check application logs for error messages

### Background Images Not Showing
- Verify `BackgroundImagePath` points to an existing file
- Check file permissions
- Adjust `BackgroundImageOpacity` if image is too faint
- Try different `BackgroundImageStretch` values

### Carousel Not Auto-Rotating
- Ensure `AutoRotate = $true` is set
- Verify `RotateInterval` is within valid range (1000-10000 ms)
- Check that carousel has at least 2 slides

## Logging

Carousel operations are logged to the PoshUI log file at:
```
%LOCALAPPDATA%\PoshUI\Logs\PoshUI_*.log
```

Look for messages containing:
- "Opening URL: ..." - When a link is clicked
- "Successfully opened URL: ..." - Successful URL opening
- "Failed to open URL: ..." - URL opening errors
- "Carousel slide: ..." - Slide properties during initialization

## Performance Considerations

- Background images should be optimized for web use (compressed, reasonable dimensions)
- Large images may impact carousel responsiveness
- Opacity values between 0.1 and 0.3 work best for subtle backgrounds
- Auto-rotation intervals below 2000ms may feel too fast for users

## Compatibility

- **PowerShell Version**: 5.1+ (Windows PowerShell) or 7.4+ (PowerShell Core)
- **Operating System**: Windows 7+ (Framework 4.8) or Windows 10/11 (Core)
- **Modules**: All three PoshUI modules (Wizard, Workflow, Dashboard)
