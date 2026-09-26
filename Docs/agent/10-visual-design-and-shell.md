# 10 — Visual Design & the App Shell

Files 00 and 09 decide what an app does and how it is structured. This file decides how it **looks**: the
visual system, a custom window shell (own top bar, icon-rail navigation), an intro splash, and page
composition. It is the difference between a tidy tool and one that looks like `Showcase-Release.ps1`.

Everything here is lifted from that showcase, where it is proven, and was re-verified by rendering it and
looking at the result. **The code blocks in this file, run in order, are one complete app** — copy them
top to bottom as a starting point, then change the tokens, sections and page bodies.

Use the full shell for an app people live in (a console, a toolbox, a monitor). For a single form, a
themed standard window (§2 alone) is enough — a chromeless shell on a three-field dialog is overdesign.

---

## 1. Principles

1. **One surface family.** Background, card and inset are three steps of the same dark hue. Nothing else is
   a surface colour.
2. **One brand colour, used sparingly.** Primary buttons, the active navigation item, focus, one key number.
   If everything is accent, nothing is.
3. **Semantic colours mean one thing each.** Green = healthy/done, amber = attention, red = failed/destructive.
   Never use them as decoration.
4. **Contrast is not optional.** Body text at `#E2E8F0`, secondary text at `#94A3B8` — anything darker than
   that on a near-black background reads as disabled. The most common reason a generated app looks "cheap"
   is secondary text that is too faint.
5. **Hierarchy from size and weight, not colour.** A page has one heading, section labels, then content.
6. **Consistent geometry.** One corner radius family (10–12 for cards, 6–8 for controls), one spacing scale.
7. **Motion shows state or arrival** — entrance cascades, a live pulse, a hover lift. One "hero moment" per
   app at most (the splash or the hero panel), and nothing decorative that loops on a working screen.

---

## 2. The visual system

### Tokens and theme
Define the palette once as variables, hand it to `Set-UITheme`, and use the same variables in every XAML
island so the custom pieces match the engine's controls.

```powershell
Import-Module .\PoshUI\PoshUI.Canvas\PoshUI.Canvas.psd1 -Force

# ── tokens ───────────────────────────────────────────────────────────────────
$bg = '#070B12'; $card = '#0D1420'; $inset = '#080C13'; $line = '#1E293B'   # surfaces + hairlines
$text = '#E2E8F0'; $label2 = '#94A3B8'; $muted = '#64748B'; $dim = '#5B6B82'  # text, strongest to weakest
$brand = '#2289F7'; $accent = '#7DC4FF'                                      # brand + its light tint
$ok = '#4ADE80'; $warn = '#FCD34D'; $bad = '#F87171'                          # semantic
$cyan = '#22D3EE'; $magenta = '#F472B6'                                       # chart / category accents
$mono = 'Cascadia Mono, Consolas, Courier New'
$glyphs = 'Segoe Fluent Icons, Segoe MDL2 Assets'
$AppName = 'Contoso Ops'

# Escape anything interpolated into XAML - even values this script owns.
function Esc([string]$s) {
    if ($null -eq $s) { return '' }
    $s.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;').Replace('"', '&quot;').Replace("'", '&apos;')
}

# -HideTitleBar: the app draws its own top bar (section 3). -WindowTitleIcon <png> sets the taskbar and
# Alt-Tab icon, which still matters with the title bar hidden.
New-PoshUICanvas -Title $AppName -Theme Dark -HideTitleBar -Width 1180 -Height 820 -MinWidth 980 -MinHeight 680 | Out-Null
Set-UITheme @{
    AccentColor        = $brand; AccentLight = $accent; AccentDark = '#0B62C4'
    Background         = $bg; ContentBackground = $bg; CardBackground = $card
    TitleBarBackground = $bg; TitleBarText = $text; BorderColor = $line
    TextPrimary        = $text; TextSecondary = $muted
    InputBackground    = $inset; InputBorder = $brand
} | Out-Null
```

### Type scale
| Role | Size / weight | Colour |
|---|---|---|
| Page heading | 22 Bold | `$text` |
| Page subtitle | 13 | `$label2` |
| Section label | 12, ALL CAPS | `$accent` |
| Card title | 16 SemiBold | `$text` |
| Body | 12–13 | `$label2` (secondary) or `$text` |
| Metadata, versions, timestamps | 10–11, `$mono` | `$dim` |

### Spacing and shape
- Page content margin `24,14,24,16`; gap between cards 12; card padding 14–18.
- Card radius 10–12; buttons and inputs follow the theme.
- Icons: Segoe Fluent Icons glyphs (`&#xE80F;` home, `&#xE9D2;` chart, `&#xE768;` play, `&#xE70F;` edit,
  `&#xE713;` settings, `&#xE946;` info). Colour an icon with its category accent and add
  `Glow = <same colour>; GlowRadius = 10` to make it read as lit.

---

## 3. The shell: custom top bar

A chromeless window (`-HideTitleBar`) keeps an invisible 32 px **caption strip** at the top that swallows
mouse input so the window can be dragged by it. Your top bar sits inside that strip, so:

- every **button** in it sets `shell:WindowChrome.IsHitTestVisibleInChrome="True"`, or it will not receive
  clicks;
- everything else in the bar stays a drag handle — exactly the title-bar behaviour you want.

Minimise with `ShowWindowAsync`, not by setting `WindowState`: actions run off the UI thread, and handing the
dispatcher a property change from inside the running action can deadlock. Close with `Submit-UICanvas`.

Each page emits its own bar, so **button names carry the page** — the engine keys controls by name.

```powershell
$MinimizeAction = {
    try {
        if (-not ('AppShell.Win32' -as [type])) {
            Add-Type -Namespace AppShell -Name Win32 -MemberDefinition '[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool ShowWindowAsync(System.IntPtr hWnd, int nCmdShow);'
        }
        $h = [System.Diagnostics.Process]::GetCurrentProcess().MainWindowHandle
        if ($h -ne [IntPtr]::Zero) { [void][AppShell.Win32]::ShowWindowAsync($h, 6) }   # 6 = SW_MINIMIZE
    }
    catch { Show-UICanvasToast -Message "Minimize failed: $($_.Exception.Message)" -Severity warning }
}

# Returns @{ Xaml; Actions } for Add-UICanvasXaml.
function New-TopBar([string]$Page, [string]$Status = '') {
    $min = "tbMin$Page"; $close = "tbClose$Page"
    $initial = Esc $AppName.Substring(0, 1)
    $xaml = @"
<Border Background="$bg" BorderBrush="$line" BorderThickness="0,0,0,1" Height="40"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:shell="clr-namespace:System.Windows.Shell;assembly=PresentationFramework">
  <Border.Resources>
    <Style x:Key="tb" TargetType="Button">
      <Setter Property="Width" Value="34"/><Setter Property="Height" Value="28"/>
      <Setter Property="Margin" Value="2,0,0,0"/><Setter Property="Background" Value="Transparent"/>
      <Setter Property="Foreground" Value="#8B98AC"/><Setter Property="FontFamily" Value="$glyphs"/>
      <Setter Property="FontSize" Value="11"/><Setter Property="Cursor" Value="Hand"/>
      <Setter Property="shell:WindowChrome.IsHitTestVisibleInChrome" Value="True"/>
      <Setter Property="Template"><Setter.Value>
        <ControlTemplate TargetType="Button">
          <Border x:Name="bd" Background="{TemplateBinding Background}" CornerRadius="5">
            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
              <Setter TargetName="bd" Property="Background" Value="#16202E"/>
              <Setter Property="Foreground" Value="$text"/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value></Setter>
    </Style>
    <Style x:Key="tbClose" TargetType="Button" BasedOn="{StaticResource tb}">
      <Setter Property="Template"><Setter.Value>
        <ControlTemplate TargetType="Button">
          <Border x:Name="bd" Background="{TemplateBinding Background}" CornerRadius="5">
            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
              <Setter TargetName="bd" Property="Background" Value="#C42B1C"/>
              <Setter Property="Foreground" Value="#FFFFFF"/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value></Setter>
    </Style>
  </Border.Resources>
  <Grid Margin="14,0,6,0">
    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
    <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
      <Border Width="18" Height="18" CornerRadius="5" Background="$brand">
        <TextBlock Text="$initial" FontSize="11" FontWeight="Bold" Foreground="White"
                   HorizontalAlignment="Center" VerticalAlignment="Center"/>
      </Border>
      <TextBlock Text="$(Esc $AppName)" Margin="9,0,0,0" FontSize="12.5" FontWeight="SemiBold"
                 Foreground="$text" VerticalAlignment="Center"/>
      <TextBlock Text="&#xE76C;" FontFamily="$glyphs" FontSize="9" Foreground="$dim"
                 Margin="10,1,10,0" VerticalAlignment="Center"/>
      <TextBlock Text="$(Esc $Page)" FontSize="12.5" Foreground="$accent" VerticalAlignment="Center"/>
    </StackPanel>
    <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
      <TextBlock Text="$(Esc $Status)" FontFamily="$mono" FontSize="10" Foreground="$dim"
                 VerticalAlignment="Center" Margin="0,0,14,0"/>
      <Button x:Name="$min"   Style="{StaticResource tb}"      Content="&#xE921;" ToolTip="Minimize"/>
      <Button x:Name="$close" Style="{StaticResource tbClose}" Content="&#xE8BB;" ToolTip="Close"/>
    </StackPanel>
  </Grid>
</Border>
"@
    @{ Xaml = $xaml; Actions = @{ $min = $MinimizeAction; $close = { Submit-UICanvas } } }
}
```

Do **not** use `Add-UICanvasToolbar` for a slim bar: its height is effectively fixed at ~54 px, and a smaller
`BarHeight` clips its content.

---

## 4. The shell: icon-rail navigation

A 64 px rail on the left: glyph over a short caption, tooltips with the shortcut, the active item marked by a
pill and an edge bar that **grow in** when the page arrives, and a small lift on hover. It is built as an
island rather than `-Navigation Compact` so it appears only on the pages that want it (not the splash), and so
it can animate.

```powershell
$Sections = @(
    @{ Key = 'Overview'; Glyph = 'E80F'; Label = 'Overview'; Shortcut = 'Ctrl+1' }
    @{ Key = 'Reports';  Glyph = 'E9D2'; Label = 'Reports';  Shortcut = 'Ctrl+2' }
    @{ Key = 'Settings'; Glyph = 'E713'; Label = 'Settings'; Shortcut = 'Ctrl+3' }
)

function New-Rail([string]$Active) {
    $items = New-Object System.Text.StringBuilder
    $actions = @{}
    foreach ($s in $Sections) {
        $n = "rail$Active$($s.Key)"
        $isActive = ($s.Key -eq $Active)
        $actions[$n] = [scriptblock]::Create("Show-UICanvasPage '$($s.Key)'")
        # Only the ACTIVE button gets a local Foreground: a local value outranks the style's hover trigger.
        $fg = if ($isActive) { " Foreground=`"$text`"" } else { '' }
        $glow = if ($isActive) { "<TextBlock.Effect><DropShadowEffect Color=`"$brand`" BlurRadius=`"12`" ShadowDepth=`"0`" Opacity=`"0.95`"/></TextBlock.Effect>" } else { '' }
        $indicator = if ($isActive) {
            @"
        <Border x:Name="railPill" Margin="6,0" CornerRadius="8" Background="#2A2289F7" RenderTransformOrigin="0.5,0.5">
          <Border.RenderTransform><ScaleTransform ScaleX="0.6" ScaleY="0"/></Border.RenderTransform>
        </Border>
        <Rectangle x:Name="railBar" Width="3" Height="24" RadiusX="1.5" RadiusY="1.5" Fill="$brand"
                   HorizontalAlignment="Left" VerticalAlignment="Center" RenderTransformOrigin="0.5,0.5">
          <Rectangle.RenderTransform><ScaleTransform ScaleY="0"/></Rectangle.RenderTransform>
        </Rectangle>
"@
        } else { '' }
        [void]$items.AppendLine(@"
      <Grid Margin="0,2">
$indicator
        <Button x:Name="$n" Style="{StaticResource rb}"$fg ToolTip="$(Esc $s.Label)   ($($s.Shortcut))">
          <StackPanel>
            <TextBlock Text="&#x$($s.Glyph);" FontFamily="$glyphs" FontSize="17" HorizontalAlignment="Center">$glow</TextBlock>
            <TextBlock Text="$(Esc $s.Label)" FontSize="9.5" Margin="0,4,0,0" HorizontalAlignment="Center"/>
          </StackPanel>
        </Button>
      </Grid>
"@)
    }
    $about = "rail${Active}About"
    $actions[$about] = { Show-UICanvasDialog -Title 'About' -OkLabel 'Close' -Message 'Contoso Ops - built with PoshUI Canvas.' }

    $xaml = @"
<Border Background="$bg" BorderBrush="$line" BorderThickness="0,0,1,0"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:shell="clr-namespace:System.Windows.Shell;assembly=PresentationFramework">
  <Border.Resources>
    <Style x:Key="rb" TargetType="Button">
      <Setter Property="Height" Value="54"/><Setter Property="Margin" Value="6,0"/>
      <Setter Property="Foreground" Value="#8B98AC"/><Setter Property="Background" Value="Transparent"/>
      <Setter Property="Cursor" Value="Hand"/><Setter Property="FocusVisualStyle" Value="{x:Null}"/>
      <Setter Property="shell:WindowChrome.IsHitTestVisibleInChrome" Value="True"/>
      <Setter Property="Template"><Setter.Value>
        <ControlTemplate TargetType="Button">
          <Border x:Name="bd" Background="{TemplateBinding Background}" CornerRadius="8">
            <ContentPresenter x:Name="cp" HorizontalAlignment="Center" VerticalAlignment="Center">
              <ContentPresenter.RenderTransform><TranslateTransform Y="0"/></ContentPresenter.RenderTransform>
            </ContentPresenter>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
              <Setter TargetName="bd" Property="Background" Value="#141C2A"/>
              <Setter Property="Foreground" Value="$text"/>
            </Trigger>
            <EventTrigger RoutedEvent="MouseEnter"><BeginStoryboard><Storyboard>
              <DoubleAnimation Storyboard.TargetName="cp" Storyboard.TargetProperty="RenderTransform.Y" To="-2" Duration="0:0:0.18">
                <DoubleAnimation.EasingFunction><BackEase EasingMode="EaseOut" Amplitude="0.8"/></DoubleAnimation.EasingFunction>
              </DoubleAnimation>
            </Storyboard></BeginStoryboard></EventTrigger>
            <EventTrigger RoutedEvent="MouseLeave"><BeginStoryboard><Storyboard>
              <DoubleAnimation Storyboard.TargetName="cp" Storyboard.TargetProperty="RenderTransform.Y" To="0" Duration="0:0:0.2"/>
            </Storyboard></BeginStoryboard></EventTrigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value></Setter>
    </Style>
  </Border.Resources>
  <Grid Margin="0,10,0,10">
    <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
    <StackPanel Grid.Row="0">
$($items.ToString().TrimEnd())
    </StackPanel>
    <StackPanel Grid.Row="2">
      <Rectangle Height="1" Fill="$line" Margin="14,0,14,8"/>
      <Button x:Name="$about" Style="{StaticResource rb}" Height="44" ToolTip="About">
        <TextBlock Text="&#xE946;" FontFamily="$glyphs" FontSize="15"/>
      </Button>
    </StackPanel>
  </Grid>
  <Border.Triggers>
    <EventTrigger RoutedEvent="FrameworkElement.Loaded"><BeginStoryboard><Storyboard>
      <DoubleAnimation Storyboard.TargetName="railPill" Storyboard.TargetProperty="RenderTransform.ScaleY" From="0" To="1" Duration="0:0:0.38">
        <DoubleAnimation.EasingFunction><BackEase EasingMode="EaseOut" Amplitude="0.5"/></DoubleAnimation.EasingFunction>
      </DoubleAnimation>
      <DoubleAnimation Storyboard.TargetName="railPill" Storyboard.TargetProperty="RenderTransform.ScaleX" From="0.6" To="1" Duration="0:0:0.38">
        <DoubleAnimation.EasingFunction><CubicEase EasingMode="EaseOut"/></DoubleAnimation.EasingFunction>
      </DoubleAnimation>
      <DoubleAnimation Storyboard.TargetName="railBar" Storyboard.TargetProperty="RenderTransform.ScaleY" From="0" To="1" BeginTime="0:0:0.08" Duration="0:0:0.3">
        <DoubleAnimation.EasingFunction><BackEase EasingMode="EaseOut" Amplitude="0.6"/></DoubleAnimation.EasingFunction>
      </DoubleAnimation>
    </Storyboard></BeginStoryboard></EventTrigger>
  </Border.Triggers>
</Border>
"@
    @{ Xaml = $xaml; Actions = $actions }
}
```

The storyboard names `railPill`/`railBar`, which exist only for the active item — so exactly one item
animates. Every page's rail must have exactly one active item, or the storyboard throws on load.

---

## 5. The shell: page frame and helpers

Every section page is the same frame: **top bar docked top, rail docked left, content filling the rest.**
Use a **Dock page**: on a stack page the engine scrolls the whole page, so the bar and rail would scroll away
with the content (and a scrolling panel inside it adds a second scrollbar). On a Dock page only the fill
child scrolls — and the fill child is the **literal last** one declared, so the content panel stays last.
`Stagger` on the content panel cascades the page in on every visit.

```powershell
$script:ShortcutsAdded = $false
function Add-ShellPage([string]$Page, [scriptblock]$Body, [string]$Status = '') {
    Add-UICanvasPage -Title $Page -Layout Dock | Out-Null
    $tb = New-TopBar -Page $Page -Status $Status
    Add-UICanvasXaml -Name "topbar$Page" -Markup $tb.Xaml -Actions $tb.Actions -Height 40 -Properties @{ Dock = 'Top' } | Out-Null
    $rail = New-Rail -Active $Page
    Add-UICanvasXaml -Name "rail$Page" -Markup $rail.Xaml -Actions $rail.Actions -Width 64 -Properties @{ Dock = 'Left' } | Out-Null
    # Shortcuts are app-wide and re-register on every visit to the page that declares them: declare them once.
    if (-not $script:ShortcutsAdded) {
        foreach ($s in $Sections) { Add-UICanvasShortcut $s.Shortcut -Action ([scriptblock]::Create("Show-UICanvasPage '$($s.Key)'")) }
        $script:ShortcutsAdded = $true
    }
    $contentName = "content$Page"   # alias before -Children: container cmdlets shadow $Name
    Add-UICanvasPanel -Name $contentName -Layout VStack -Spacing 0 `
        -Properties @{ Margin = '24,14,24,16'; Stagger = 60 } -Children $Body | Out-Null   # no Scroll: the Dock fill child already scrolls
}

function Add-PageHeading([string]$Heading, [string]$Sub) {
    Add-UICanvasLabel $Heading -FontSize 22 -FontWeight Bold -Foreground $text | Out-Null
    Add-UICanvasLabel $Sub -FontSize 13 -Foreground $label2 -Properties @{ Margin = '0,2,0,4' } | Out-Null
}
function Add-SectionLabel([string]$Label, [string]$Top = '20') {
    Add-UICanvasLabel $Label.ToUpper() -FontSize 12 -Foreground $accent -Properties @{ Margin = "0,$Top,0,8" } | Out-Null
}
```

---

## 6. Intro splash that advances by itself

A splash is a page with one full-size island and a storyboard that starts on `Loaded`, plus a **load hook**
that navigates away when the sequence ends:

- A hidden label with `-Refresh` evaluates its scriptblock **once immediately** at registration — that is the
  page-load hook. Its interval (3600 s) is never meant to fire.
- From the hook, `Start-UICanvasAsync { Start-Sleep -Milliseconds N; Show-UICanvasPage 'Overview' }` gives
  millisecond timing without blocking the UI.
- Guard with `$__PoshUICanvasBridge`: `-Refresh` blocks are also evaluated once in the authoring script,
  before the engine exists, where runtime cmdlets only warn.
- Guard with a `$global:` flag so returning to the splash does not arm a second timer.
- Always offer a way past it (an Enter button), and keep the whole thing under ~3.5 s.

```powershell
function New-SplashXaml {
    @"
<Grid Background="$bg" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Ellipse Width="520" Height="520" Opacity="0.35" IsHitTestVisible="False">
    <Ellipse.Fill><RadialGradientBrush>
      <GradientStop Color="#552289F7" Offset="0"/><GradientStop Color="#002289F7" Offset="1"/>
    </RadialGradientBrush></Ellipse.Fill>
  </Ellipse>
  <StackPanel x:Name="lockup" HorizontalAlignment="Center" VerticalAlignment="Center" Opacity="0" RenderTransformOrigin="0.5,0.5">
    <StackPanel.RenderTransform><ScaleTransform ScaleX="0.92" ScaleY="0.92"/></StackPanel.RenderTransform>
    <Border Width="84" Height="84" CornerRadius="20" Background="$brand" HorizontalAlignment="Center">
      <Border.Effect><DropShadowEffect Color="$brand" BlurRadius="40" ShadowDepth="0" Opacity="0.8"/></Border.Effect>
      <TextBlock Text="$(Esc $AppName.Substring(0,1))" FontSize="44" FontWeight="Bold" Foreground="White"
                 HorizontalAlignment="Center" VerticalAlignment="Center"/>
    </Border>
    <TextBlock Text="$(Esc $AppName)" FontSize="30" FontWeight="SemiBold" Foreground="$text"
               HorizontalAlignment="Center" Margin="0,22,0,0"/>
    <TextBlock x:Name="tag" Text="OPERATIONS CONSOLE" FontFamily="$mono" FontSize="11" Foreground="$accent"
               HorizontalAlignment="Center" Margin="0,8,0,0" Opacity="0"/>
  </StackPanel>
  <Grid.Triggers>
    <EventTrigger RoutedEvent="FrameworkElement.Loaded"><BeginStoryboard><Storyboard>
      <DoubleAnimation Storyboard.TargetName="lockup" Storyboard.TargetProperty="Opacity" From="0" To="1" BeginTime="0:0:0.2" Duration="0:0:0.6"/>
      <DoubleAnimation Storyboard.TargetName="lockup" Storyboard.TargetProperty="RenderTransform.ScaleX" From="0.92" To="1" BeginTime="0:0:0.2" Duration="0:0:0.8">
        <DoubleAnimation.EasingFunction><BackEase EasingMode="EaseOut" Amplitude="0.4"/></DoubleAnimation.EasingFunction>
      </DoubleAnimation>
      <DoubleAnimation Storyboard.TargetName="lockup" Storyboard.TargetProperty="RenderTransform.ScaleY" From="0.92" To="1" BeginTime="0:0:0.2" Duration="0:0:0.8">
        <DoubleAnimation.EasingFunction><BackEase EasingMode="EaseOut" Amplitude="0.4"/></DoubleAnimation.EasingFunction>
      </DoubleAnimation>
      <DoubleAnimation Storyboard.TargetName="tag" Storyboard.TargetProperty="Opacity" From="0" To="1" BeginTime="0:0:0.9" Duration="0:0:0.5"/>
      <DoubleAnimation Storyboard.TargetName="lockup" Storyboard.TargetProperty="Opacity" To="0" BeginTime="0:0:2.3" Duration="0:0:0.35"/>
    </Storyboard></BeginStoryboard></EventTrigger>
  </Grid.Triggers>
</Grid>
"@
}

Add-UICanvasPage -Title 'Splash' -Layout VStack | Out-Null
Add-UICanvasXaml -Name splash -Markup (New-SplashXaml) -Height 700 | Out-Null
Add-UICanvasPanel -Layout HStack -Properties @{ HAlign = 'Center' } -Children {
    Add-UICanvasButton 'Enter' -Style Accent -Action { Show-UICanvasPage 'Overview' }
} | Out-Null
$kick = {
    if (-not $__PoshUICanvasBridge) { return '' }
    if (-not $global:AppSplashArmed) {
        $global:AppSplashArmed = $true
        Start-UICanvasAsync { Start-Sleep -Milliseconds 2700; Show-UICanvasPage 'Overview' }
    }
    ''
}
Add-UICanvasLabel -Name splashKick -Visible $false -Refresh 3600 -Label $kick | Out-Null
```

---

## 7. Page composition

A section page reads top to bottom: **heading → (hero) → section label → content grid → section label →
content grid**. Cards are the unit; a whole card can be the click target.

### A hero panel (one per app)
A gradient card with a soft brand glow, an eyebrow line, a headline, a paragraph and a row of pills that
cascade in. It explains the app on arrival — the place for the one hero moment.

```powershell
function New-HeroXaml([string]$Eyebrow, [string]$Headline, [string]$Body, [string[]]$Pills) {
    $pillEls = New-Object System.Text.StringBuilder; $anims = New-Object System.Text.StringBuilder
    for ($i = 0; $i -lt $Pills.Count; $i++) {
        $n = "hP$i"; $at = '0:0:{0:0.00}' -f (0.5 + 0.07 * $i)
        [void]$pillEls.AppendLine("<Border x:Name=`"$n`" CornerRadius=`"12`" Padding=`"12,5`" Margin=`"0,0,8,8`" Background=`"#12203A`" BorderBrush=`"#2A3F66`" BorderThickness=`"1`" Opacity=`"0`"><TextBlock Text=`"$(Esc $Pills[$i])`" FontSize=`"11.5`" Foreground=`"$accent`"/></Border>")
        [void]$anims.AppendLine("<DoubleAnimation Storyboard.TargetName=`"$n`" Storyboard.TargetProperty=`"Opacity`" From=`"0`" To=`"1`" BeginTime=`"$at`" Duration=`"0:0:0.3`"/>")
    }
    @"
<Border CornerRadius="14" BorderBrush="$line" BorderThickness="1" ClipToBounds="True"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Border.Background>
    <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
      <GradientStop Color="#0F1C33" Offset="0"/><GradientStop Color="#0A0F18" Offset="0.62"/><GradientStop Color="$card" Offset="1"/>
    </LinearGradientBrush>
  </Border.Background>
  <Grid>
    <Ellipse Width="380" Height="380" HorizontalAlignment="Left" VerticalAlignment="Top" Margin="-90,-120,0,0" Opacity="0.35" IsHitTestVisible="False">
      <Ellipse.Fill><RadialGradientBrush><GradientStop Color="#662289F7" Offset="0"/><GradientStop Color="#002289F7" Offset="1"/></RadialGradientBrush></Ellipse.Fill>
    </Ellipse>
    <StackPanel x:Name="hText" Margin="28,24,28,18" Opacity="0">
      <StackPanel.RenderTransform><TranslateTransform Y="10"/></StackPanel.RenderTransform>
      <TextBlock Text="$(Esc $Eyebrow)" FontFamily="$mono" FontSize="11" Foreground="$accent"/>
      <TextBlock Text="$(Esc $Headline)" FontSize="26" FontWeight="Bold" Foreground="$text" Margin="0,8,0,0" TextWrapping="Wrap"/>
      <TextBlock Text="$(Esc $Body)" FontSize="13.5" Foreground="$label2" Margin="0,10,0,16" TextWrapping="Wrap" MaxWidth="720" HorizontalAlignment="Left"/>
      <WrapPanel>
$($pillEls.ToString().TrimEnd())
      </WrapPanel>
    </StackPanel>
  </Grid>
  <Border.Triggers>
    <EventTrigger RoutedEvent="FrameworkElement.Loaded"><BeginStoryboard><Storyboard>
      <DoubleAnimation Storyboard.TargetName="hText" Storyboard.TargetProperty="Opacity" From="0" To="1" Duration="0:0:0.45"/>
      <DoubleAnimation Storyboard.TargetName="hText" Storyboard.TargetProperty="RenderTransform.Y" From="10" To="0" Duration="0:0:0.5">
        <DoubleAnimation.EasingFunction><CubicEase EasingMode="EaseOut"/></DoubleAnimation.EasingFunction>
      </DoubleAnimation>
$($anims.ToString().TrimEnd())
    </Storyboard></BeginStoryboard></EventTrigger>
  </Border.Triggers>
</Border>
"@
}
```

### Putting it together
Clickable cards (`-Action` on a card) make navigation feel like content; `HoverScale` confirms they are
clickable. Metric cards give the headline numbers. Note the leading commas in the card table: without them
PowerShell flattens `@( @(…) @(…) )` into one list of strings.

```powershell
Add-ShellPage 'Overview' -Status 'v1.0  ·  connected' -Body {
    Add-UICanvasXaml -Name hero -Height 225 -Markup (New-HeroXaml -Eyebrow 'OPERATIONS CONSOLE  ·  CONTOSO' `
        -Headline 'Everything your team runs, in one window.' `
        -Body 'Check the estate at a glance, drill into a report, and change settings safely - every action confirms before it touches anything.' `
        -Pills @('Live health', 'Reports', 'Safe changes', 'Keyboard shortcuts')) | Out-Null

    Add-SectionLabel 'Go to'
    Add-UICanvasPanel -Layout Grid -Columns 2 -Spacing 12 -Children {
        $cards = @(
            , @('Reports', 'E9D2', $cyan, 'Filter, inspect and export the estate report.')
            , @('Settings', 'E713', $magenta, 'Change configuration with a reviewed diff before saving.')
        )
        for ($i = 0; $i -lt $cards.Count; $i++) {
            $c = $cards[$i]; $cTitle = $c[0]; $cGlyph = [string][char][Convert]::ToInt32($c[1], 16); $cCol = $c[2]; $cText = $c[3]
            Add-UICanvasCard -Layout VStack -Spacing 8 -Padding 18 -Action ([scriptblock]::Create("Show-UICanvasPage '$cTitle'")) `
                -Properties @{ Column = $i; CornerRadius = 12; HoverScale = 1.03 } -Children {
                Add-UICanvasPanel -Layout HStack -Spacing 10 -Children {
                    Add-UICanvasIcon $cGlyph -FontSize 18 -Foreground $cCol -Properties @{ VAlign = 'Center'; Glow = $cCol; GlowRadius = 10 }
                    Add-UICanvasLabel $cTitle -FontSize 16 -FontWeight SemiBold -Foreground $text -Properties @{ VAlign = 'Center' }
                }
                Add-UICanvasLabel $cText -FontSize 12 -Foreground $label2
                Add-UICanvasLabel 'Open  >' -FontSize 12 -FontWeight SemiBold -Foreground $cCol -Properties @{ Margin = '0,4,0,0' }
            }
        }
    }

    Add-SectionLabel 'Estate'
    Add-UICanvasPanel -Layout Grid -Columns 3 -Spacing 12 -Children {
        Add-UICanvasMetricCard 'Servers online' -Value '248' -Trend Up -Delta '+3' -Properties @{ Column = 0 }
        Add-UICanvasMetricCard 'Open alerts' -Value '4' -Trend Down -Delta '-2' -Properties @{ Column = 1 }
        Add-UICanvasMetricCard 'Patch compliance' -Value '97%' -Trend Up -Delta '+1%' -Properties @{ Column = 2 }
    }
}

Add-ShellPage 'Reports' -Body {
    Add-PageHeading 'Reports' 'Filter the estate, inspect a machine, export what you see.'
    Add-SectionLabel 'Coming next'
    Add-UICanvasLabel 'Build this page from blueprint 5 in 09-app-blueprints.' -Foreground $label2
}
Add-ShellPage 'Settings' -Body {
    Add-PageHeading 'Settings' 'Every change is reviewed before it is saved.'
    Add-SectionLabel 'Coming next'
    Add-UICanvasLabel 'Build this page from blueprint 9 in 09-app-blueprints.' -Foreground $label2
}

Show-PoshUICanvas | Out-Null
```

---

## 8. Checklist before calling it done

- [ ] `Set-UITheme` called with the palette; the same tokens used in every island.
- [ ] Secondary text is `$label2` (or lighter), never `$muted` for anything a user must read.
- [ ] One heading per page, section labels in accent caps, consistent card radius and spacing.
- [ ] Shell pages are **Dock** pages with the content panel last; every top-bar and rail button has
      `IsHitTestVisibleInChrome`; button names include the page.
- [ ] Exactly one active rail item per page; shortcuts declared once.
- [ ] Splash under ~3.5 s, has a skip button, guarded against double-arming.
- [ ] Motion: entrance cascade and hover only on working pages; no endless decorative loops.
- [ ] **Looked at it.** Launch it and view every page — a clean engine log does not mean the page looks
      right. Check it at the minimum window size too.
