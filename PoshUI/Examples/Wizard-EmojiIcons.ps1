<#
.SYNOPSIS
    Wizard demo showcasing 3D Emoji PNG icons on every possible UI element.

.DESCRIPTION
    IT Helpdesk Ticket wizard that uses colored 3D emoji PNG icons from
    C:\Projects\PoshUI\Assets\Icons\Emoji on every placement point:
    - Window title bar icon
    - Sidebar header icon
    - Step sidebar icons
    - Banner icons, background images, and overlay images
    - Info card icons and images

    This demonstrates that PoshUI supports full-color PNG images anywhere
    a glyph icon would normally appear.

.NOTES
    Company: Kanders-II
    Requires: PoshUI.Wizard module, Emoji icon assets
    Encoding: ASCII only (PowerShell 5.1 compatible)

.EXAMPLE
    .\Wizard-EmojiIcons.ps1
#>

# ========================================
# MODULE IMPORT
# ========================================

$modulePath = Join-Path $PSScriptRoot '..\PoshUI.Wizard\PoshUI.Wizard.psd1'
Import-Module $modulePath -Force

# ========================================
# EMOJI ICON PATHS
# ========================================

$emojiBase = Join-Path $PSScriptRoot '\Emoji\'


# Branding icons
$iconTicket         = Join-Path $emojiBase 'admission_tickets_3d.png'
$iconRobot          = Join-Path $emojiBase 'robot_3d.png'

# Step sidebar icons
$iconWave           = Join-Path $emojiBase 'winking_face_3d.png'
$iconPerson         = Join-Path $emojiBase 'bust_in_silhouette_3d.png'
$iconComputer       = Join-Path $emojiBase 'desktop_computer_3d.png'
$iconWrench         = Join-Path $emojiBase 'hammer_and_wrench_3d.png'
$iconClipboard      = Join-Path $emojiBase 'clipboard_3d.png'
$iconRocket         = Join-Path $emojiBase 'rocket_3d.png'

# Banner icons
$iconSparkles       = Join-Path $emojiBase 'sparkles_3d.png'
$iconMagnify        = Join-Path $emojiBase 'magnifying_glass_tilted_left_3d.png'
$iconGear           = Join-Path $emojiBase 'gear_3d.png'
$iconShield         = Join-Path $emojiBase 'shield_3d.png'
$iconCheckMark      = Join-Path $emojiBase 'check_mark_button_3d.png'
$iconParty          = Join-Path $emojiBase 'party_popper_3d.png'

# Banner overlay / background images
$iconGlobe          = Join-Path $emojiBase 'globe_with_meridians_3d.png'
$iconCloud          = Join-Path $emojiBase 'cloud_3d.png'
$iconLaptop         = Join-Path $emojiBase 'laptop_3d.png'
$iconBrain          = Join-Path $emojiBase 'brain_3d.png'
$iconBullseye       = Join-Path $emojiBase 'bullseye_3d.png'
$iconTrophy         = Join-Path $emojiBase 'trophy_3d.png'

# Card icons
$iconInfo           = Join-Path $emojiBase 'information_3d.png'
$iconLightBulb      = Join-Path $emojiBase 'light_bulb_3d.png'
$iconWarning        = Join-Path $emojiBase 'warning_3d.png'
$iconFire           = Join-Path $emojiBase 'fire_3d.png'
$iconStar           = Join-Path $emojiBase 'glowing_star_3d.png'
$iconLock           = Join-Path $emojiBase 'locked_3d.png'
$iconKey            = Join-Path $emojiBase 'key_3d.png'
$iconBug            = Join-Path $emojiBase 'bug_3d.png'
$iconLink           = Join-Path $emojiBase 'link_3d.png'
$iconMemo           = Join-Path $emojiBase 'memo_3d.png'
$iconPackage        = Join-Path $emojiBase 'package_3d.png'
$iconClock          = Join-Path $emojiBase 'alarm_clock_3d.png'
$iconCamera         = Join-Path $emojiBase 'camera_3d.png'
$iconTools          = Join-Path $emojiBase 'toolbox_3d.png'
$iconNetwork        = Join-Path $emojiBase 'satellite_antenna_3d.png'
$iconEmail          = Join-Path $emojiBase 'e-mail_3d.png'
$iconFolder         = Join-Path $emojiBase 'file_folder_3d.png'
$iconPrinter        = Join-Path $emojiBase 'printer_3d.png'

# Validate all icon paths exist
$allIcons = @(
    $iconTicket, $iconRobot, $iconWave, $iconPerson, $iconComputer,
    $iconWrench, $iconClipboard, $iconRocket, $iconSparkles, $iconMagnify,
    $iconGear, $iconShield, $iconCheckMark, $iconParty, $iconGlobe,
    $iconCloud, $iconLaptop, $iconBrain, $iconBullseye, $iconTrophy,
    $iconInfo, $iconLightBulb, $iconWarning, $iconFire, $iconStar,
    $iconLock, $iconKey, $iconBug, $iconLink, $iconMemo,
    $iconPackage, $iconClock, $iconCamera, $iconTools, $iconNetwork,
    $iconEmail, $iconFolder, $iconPrinter
)

$missing = $allIcons | Where-Object { -not (Test-Path $_) }
if ($missing) {
    Write-Host "Missing emoji icons:" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
    throw "Missing $($missing.Count) emoji icon file(s). Ensure C:\Projects\PoshUI\Assets\Icons\Emoji exists."
}

Write-Host ""
Write-Host "+--------------------------------------+" -ForegroundColor Cyan
Write-Host "|   IT Helpdesk Ticket Wizard          |" -ForegroundColor Cyan
Write-Host "|   3D Emoji PNG Icons Demo            |" -ForegroundColor Cyan
Write-Host "|   $($allIcons.Count) icons loaded                   |" -ForegroundColor Cyan
Write-Host "+--------------------------------------+" -ForegroundColor Cyan
Write-Host ""

# ========================================
# INITIALIZE WIZARD
# ========================================

New-PoshUIWizard -Title 'IT Helpdesk Ticket' `
    -Description 'Submit a support request with full emoji icon coverage' `
    -Theme 'Light'

# ========================================
# CUSTOM THEMES - Dark and Light
# ========================================

# Light theme - light backgrounds with dark text
$lightTheme = @{
    AccentColor          = '#0078D4'
    AccentDark           = '#005A9E'
    AccentLight          = '#4DA3E8'
    Background           = '#F3F3F3'
    ContentBackground    = '#FFFFFF'
    CardBackground       = '#FAFAFA'
    SidebarBackground    = '#1B1B2F'
    SidebarText          = '#C0C0D0'
    SidebarHighlight     = '#0078D4'
    TextPrimary          = '#1A1A2E'
    TextSecondary        = '#666680'
    ButtonBackground     = '#0078D4'
    ButtonForeground     = '#FFFFFF'
    InputBackground      = '#FFFFFF'
    InputBorder          = '#0078D4'
    BorderColor          = '#E0E0E8'
    TitleBarBackground   = '#1B1B2F'
    TitleBarText         = '#FFFFFF'
    SuccessColor         = '#00C853'
    WarningColor         = '#FFB300'
    ErrorColor           = '#FF1744'
}

# Dark theme - dark backgrounds with light text
$darkTheme = @{
    AccentColor          = '#0078D4'
    AccentDark           = '#005A9E'
    AccentLight          = '#4DA3E8'
    Background           = '#F5F5F5'
    ContentBackground    = '#FFFFFF'
    CardBackground       = '#FAFAFA'
    SidebarBackground    = '#2B2B2B'
    SidebarText          = '#F0F0F0'
    SidebarHighlight     = '#4DA3E8'
    TextPrimary          = '#000000'
    TextSecondary        = '#333333'
    ButtonBackground     = '#0078D4'
    ButtonForeground     = '#FFFFFF'
    InputBackground      = '#FFFFFF'
    InputBorder          = '#666666'
    BorderColor          = '#CCCCCC'
    TitleBarBackground   = '#1F1F1F'
    TitleBarText         = '#FFFFFF'
    SuccessColor         = '#00C853'
    WarningColor         = '#FFB300'
    ErrorColor           = '#FF1744'
}

# Apply both themes (supports light/dark mode toggle)
Set-UITheme -Dark $darkTheme -Light $lightTheme

# ========================================
# BRANDING (Window Title Icon + Sidebar Icon)
# ========================================

Set-UIBranding `
    -WindowTitle 'IT Helpdesk - Emoji Icons Demo' `
    -WindowTitleIcon $iconTicket `
    -SidebarHeaderText 'Helpdesk' `
    -SidebarHeaderIcon $iconRobot `
    -SidebarHeaderIconOrientation 'Top'

# ========================================
# STEP 1: Welcome
# ========================================

Add-UIStep -Name 'Welcome' -Title 'Welcome' -Order 1 `
    -IconPath $iconWave `
    -Description 'Getting started'

# Banner with icon + overlay image
Add-UIBanner -Step 'Welcome' `
    -Title 'IT Helpdesk Ticket System' `
    -Subtitle 'Every icon you see is a 3D emoji PNG - no font glyphs!' `
    -Height 180 `
    -TitleFontSize 28 `
    -SubtitleFontSize 14 `
    -BackgroundColor '#0078D4' `
    -GradientStart '#0078D4' `
    -GradientEnd '#4DA3E8' `
    -IconPath $iconSparkles `
    -IconPosition 'Right' `
    -IconSize 80 `
    -OverlayImagePath $iconGlobe `
    -OverlayImageOpacity 0.15 `
    -OverlayPosition 'Left' `
    -OverlayImageSize 200

# Welcome info card with icon
Add-UICard -Step 'Welcome' -Title 'Welcome to IT Support' -Type 'Info' `
    -IconPath $iconInfo `
    -Content @'
This wizard demonstrates PoshUI's ability to use full-color 3D emoji
PNG icons on every UI element:

- Window title bar icon (ticket emoji)
- Sidebar header icon (robot emoji)
- Step icons in the sidebar navigation
- Banner foreground and overlay icons
- Info card and tip card icons

Every icon comes from C:\Projects\PoshUI\Assets\Icons\Emoji
and is a high-quality 3D rendered PNG image.
'@

# Tip card with different icon
Add-UICard -Step 'Welcome' -Title 'PNG vs Glyph Icons' -Type 'Tip' `
    -IconPath $iconLightBulb `
    -Content @'
Traditional PoshUI uses Segoe MDL2 font glyphs (monochrome).
With PNG icon support, you get full-color images that make
your wizard look polished and professional.

PNG icons work on: Steps, Banners, Cards, Branding, and Overlays.
'@

# ========================================
# STEP 2: User Information
# ========================================

Add-UIStep -Name 'UserInfo' -Title 'Your Info' -Order 2 `
    -IconPath $iconPerson `
    -Description 'Contact details'

Add-UIBanner -Step 'UserInfo' `
    -Title 'Who Are You?' `
    -Subtitle 'We need your contact information to follow up on your ticket' `
    -Height 150 `
    -TitleFontSize 26 `
    -SubtitleFontSize 13 `
    -BackgroundColor '#6A1B9A' `
    -GradientStart '#6A1B9A' `
    -GradientEnd '#AB47BC' `
    -IconPath $iconMagnify `
    -IconPosition 'Right' `
    -IconSize 70 `
    -OverlayImagePath $iconCloud `
    -OverlayImageOpacity 0.12 `
    -OverlayPosition 'Left' `
    -OverlayImageSize 180

Add-UICard -Step 'UserInfo' -Title 'Contact Information Required' -Type 'Info' `
    -IconPath $iconEmail `
    -Content 'Please provide your name, email, and department so our support team can reach you.'

Add-UITextBox -Step 'UserInfo' -Name 'FullName' -Label 'Full Name' `
    -Mandatory -Placeholder 'e.g. John Smith'

Add-UITextBox -Step 'UserInfo' -Name 'Email' -Label 'Email Address' `
    -Mandatory -Placeholder 'john.smith@company.com'

Add-UIDropdown -Step 'UserInfo' -Name 'Department' -Label 'Department' `
    -Choices @('Engineering', 'Sales', 'Marketing', 'Finance', 'HR', 'Operations', 'Executive') `
    -Default 'Engineering' `
    -Mandatory

Add-UIDropdown -Step 'UserInfo' -Name 'Location' -Label 'Office Location' `
    -Choices @('New York', 'San Francisco', 'London', 'Tokyo', 'Remote') `
    -Default 'Remote'

# ========================================
# STEP 3: Issue Details
# ========================================

Add-UIStep -Name 'Issue' -Title 'Issue' -Order 3 `
    -IconPath $iconComputer `
    -Description 'Describe your problem'

Add-UIBanner -Step 'Issue' `
    -Title 'What Went Wrong?' `
    -Subtitle 'Select the category and describe the issue in detail' `
    -Height 150 `
    -TitleFontSize 26 `
    -SubtitleFontSize 13 `
    -BackgroundColor '#C62828' `
    -GradientStart '#C62828' `
    -GradientEnd '#EF5350' `
    -IconPath $iconBug `
    -IconPosition 'Right' `
    -IconSize 70 `
    -OverlayImagePath $iconLaptop `
    -OverlayImageOpacity 0.10 `
    -OverlayPosition 'Left' `
    -OverlayImageSize 160

Add-UICard -Step 'Issue' -Title 'Issue Classification' -Type 'Warning' `
    -IconPath $iconWarning `
    -Content 'Accurate classification helps us route your ticket to the right team faster.'

Add-UIDropdown -Step 'Issue' -Name 'Category' -Label 'Issue Category' `
    -Choices @(
        'Hardware - Laptop/Desktop',
        'Hardware - Peripherals',
        'Software - Installation',
        'Software - Crash/Error',
        'Network - Connectivity',
        'Network - VPN',
        'Email - Outlook',
        'Printing',
        'Security - Access Request',
        'Security - Incident',
        'Other'
    ) `
    -Default 'Software - Crash/Error' `
    -Mandatory

Add-UIDropdown -Step 'Issue' -Name 'Priority' -Label 'Priority Level' `
    -Choices @('Low - Can wait', 'Medium - Affecting work', 'High - Blocking work', 'Critical - System down') `
    -Default 'Medium - Affecting work' `
    -Mandatory

Add-UITextBox -Step 'Issue' -Name 'Subject' -Label 'Short Summary' `
    -Mandatory `
    -Placeholder 'Brief description of the issue'

Add-UIMultiLine -Step 'Issue' -Name 'Description' -Label 'Detailed Description' `
    -Rows 5 `
    -Mandatory `
    -Placeholder 'Describe the issue in detail: what happened, when it started, error messages, etc.'

# ========================================
# STEP 4: Environment & Troubleshooting
# ========================================

Add-UIStep -Name 'Environment' -Title 'Environment' -Order 4 `
    -IconPath $iconWrench `
    -Description 'System details'

Add-UIBanner -Step 'Environment' `
    -Title 'Technical Environment' `
    -Subtitle 'Help us understand your setup for faster troubleshooting' `
    -Height 150 `
    -TitleFontSize 26 `
    -SubtitleFontSize 13 `
    -BackgroundColor '#00695C' `
    -GradientStart '#00695C' `
    -GradientEnd '#26A69A' `
    -IconPath $iconGear `
    -IconPosition 'Right' `
    -IconSize 70 `
    -OverlayImagePath $iconBrain `
    -OverlayImageOpacity 0.10 `
    -OverlayPosition 'Left' `
    -OverlayImageSize 160

Add-UICard -Step 'Environment' -Title 'System Information' -Type 'Info' `
    -IconPath $iconTools `
    -Content 'These details help our technicians diagnose the issue without extra back-and-forth.'

Add-UIDropdown -Step 'Environment' -Name 'OS' -Label 'Operating System' `
    -Choices @('Windows 11', 'Windows 10', 'macOS Sonoma', 'macOS Ventura', 'Linux Ubuntu', 'Linux RHEL', 'Other') `
    -Default 'Windows 11'

Add-UIDropdown -Step 'Environment' -Name 'DeviceType' -Label 'Device Type' `
    -Choices @('Company Laptop', 'Company Desktop', 'Personal Device', 'Virtual Machine', 'Server') `
    -Default 'Company Laptop'

Add-UITextBox -Step 'Environment' -Name 'AssetTag' -Label 'Asset Tag (if known)' `
    -Placeholder 'e.g. ASSET-12345'

Add-UIDropdown -Step 'Environment' -Name 'NetworkType' -Label 'Network Connection' `
    -Choices @('Corporate WiFi', 'Corporate Ethernet', 'Home WiFi', 'VPN', 'Mobile Hotspot', 'No Connection') `
    -Default 'Corporate WiFi'

Add-UICard -Step 'Environment' -Title 'Troubleshooting Already Tried?' -Type 'Tip' `
    -IconPath $iconLightBulb `
    -Content 'Let us know if you have already tried rebooting, reinstalling, or any other steps.'

Add-UICheckbox -Step 'Environment' -Name 'TriedReboot' -Label 'I have tried restarting the device'
Add-UICheckbox -Step 'Environment' -Name 'TriedReinstall' -Label 'I have tried reinstalling the software'
Add-UICheckbox -Step 'Environment' -Name 'TriedGoogling' -Label 'I have searched for solutions online'

# ========================================
# STEP 5: Attachments & Evidence
# ========================================

Add-UIStep -Name 'Evidence' -Title 'Evidence' -Order 5 `
    -IconPath $iconClipboard `
    -Description 'Screenshots and logs'

Add-UIBanner -Step 'Evidence' `
    -Title 'Attach Evidence' `
    -Subtitle 'Screenshots, logs, and error messages speed up resolution' `
    -Height 150 `
    -TitleFontSize 26 `
    -SubtitleFontSize 13 `
    -BackgroundColor '#E65100' `
    -GradientStart '#E65100' `
    -GradientEnd '#FF9800' `
    -IconPath $iconCamera `
    -IconPosition 'Right' `
    -IconSize 70 `
    -OverlayImagePath $iconBullseye `
    -OverlayImageOpacity 0.10 `
    -OverlayPosition 'Left' `
    -OverlayImageSize 160

Add-UICard -Step 'Evidence' -Title 'Helpful Attachments' -Type 'Info' `
    -IconPath $iconFolder `
    -Content @'
Attaching evidence dramatically speeds up ticket resolution:

- Screenshots of error messages
- Log files from the application
- Network diagnostic outputs
- Screen recordings of the issue

Use the file pickers below to attach relevant files.
'@

Add-UIFilePath -Step 'Evidence' -Name 'Screenshot' `
    -Label 'Screenshot (PNG/JPG)' `
    -Filter 'Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif'

Add-UIFilePath -Step 'Evidence' -Name 'LogFile' `
    -Label 'Log File (optional)' `
    -Filter 'Log files|*.log;*.txt;*.csv'

Add-UIMultiLine -Step 'Evidence' -Name 'ErrorMessage' `
    -Label 'Error Message Text (copy/paste)' `
    -Rows 4 `
    -Placeholder 'Paste any error messages here...'

Add-UICard -Step 'Evidence' -Title 'No Screenshots?' -Type 'Tip' `
    -IconPath $iconStar `
    -Content 'If you cannot capture a screenshot, describe the visual state of the screen in the error message field above.'

# ========================================
# STEP 6: Submit
# ========================================

Add-UIStep -Name 'Submit' -Title 'Submit' -Order 6 `
    -IconPath $iconRocket `
    -Description 'Review and submit'

Add-UIBanner -Step 'Submit' `
    -Title 'Ready to Submit!' `
    -Subtitle 'Review your information below, then click Finish to create the ticket' `
    -Height 180 `
    -TitleFontSize 28 `
    -SubtitleFontSize 14 `
    -BackgroundColor '#1B5E20' `
    -GradientStart '#1B5E20' `
    -GradientEnd '#43A047' `
    -IconPath $iconParty `
    -IconPosition 'Right' `
    -IconSize 80 `
    -OverlayImagePath $iconTrophy `
    -OverlayImageOpacity 0.12 `
    -OverlayPosition 'Left' `
    -OverlayImageSize 180

Add-UICard -Step 'Submit' -Title 'What Happens Next' -Type 'Info' `
    -IconPath $iconClock `
    -Content @'
After you submit this ticket:

1. You will receive a confirmation email with your ticket number
2. A technician will be assigned within 1 business hour
3. Priority tickets are escalated automatically
4. You can track status at helpdesk.company.com

Expected response times:
- Critical: 15 minutes
- High: 1 hour
- Medium: 4 hours
- Low: Next business day
'@

Add-UICard -Step 'Submit' -Title 'Security Notice' -Type 'Warning' `
    -IconPath $iconShield `
    -Content 'Never include passwords or sensitive credentials in ticket descriptions. Use the secure credential portal for access requests.'

Add-UICard -Step 'Submit' -Title 'Self-Service Options' -Type 'Tip' `
    -IconPath $iconKey `
    -Content @'
Some issues can be resolved immediately:

- Password resets: selfservice.company.com/password
- Software requests: catalog.company.com
- VPN setup guide: wiki.company.com/vpn
- Printer setup: wiki.company.com/printers
'@

# ========================================
# SCRIPT BODY (runs when Finish is clicked)
# ========================================

$scriptBody = {
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "  HELPDESK TICKET SUBMITTED SUCCESSFULLY" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Ticket Details:" -ForegroundColor Cyan
    Write-Host "  Name:        $FullName" -ForegroundColor White
    Write-Host "  Email:       $Email" -ForegroundColor White
    Write-Host "  Department:  $Department" -ForegroundColor White
    Write-Host "  Location:    $Location" -ForegroundColor White
    Write-Host ""
    Write-Host "Issue:" -ForegroundColor Cyan
    Write-Host "  Category:    $Category" -ForegroundColor White
    Write-Host "  Priority:    $Priority" -ForegroundColor White
    Write-Host "  Subject:     $Subject" -ForegroundColor White
    Write-Host "  Description: $Description" -ForegroundColor White
    Write-Host ""
    Write-Host "Environment:" -ForegroundColor Cyan
    Write-Host "  OS:          $OS" -ForegroundColor White
    Write-Host "  Device:      $DeviceType" -ForegroundColor White
    Write-Host "  Asset Tag:   $AssetTag" -ForegroundColor White
    Write-Host "  Network:     $NetworkType" -ForegroundColor White
    Write-Host "  Rebooted:    $TriedReboot" -ForegroundColor White
    Write-Host "  Reinstalled: $TriedReinstall" -ForegroundColor White
    Write-Host "  Googled:     $TriedGoogling" -ForegroundColor White
    Write-Host ""
    Write-Host "Evidence:" -ForegroundColor Cyan
    Write-Host "  Screenshot:  $Screenshot" -ForegroundColor White
    Write-Host "  Log File:    $LogFile" -ForegroundColor White
    Write-Host "  Error Msg:   $ErrorMessage" -ForegroundColor White
    Write-Host ""
    Write-Host "Ticket Number: HLP-$(Get-Random -Minimum 10000 -Maximum 99999)" -ForegroundColor Yellow
    Write-Host ""
}

# ========================================
# LAUNCH WIZARD
# ========================================

Write-Host "Launching IT Helpdesk Ticket wizard with emoji icons..." -ForegroundColor Cyan
Write-Host ""

Show-PoshUIWizard -ScriptBody $scriptBody
