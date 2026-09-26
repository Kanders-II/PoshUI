# 08 — Cheatsheet (every cmdlet, terse)

Universal control params (most controls): `-Name -X -Y -Width -Height -ZIndex -Tooltip -Visible -Enabled -Refresh -Properties`.

## Lifecycle / shell
```
New-PoshUICanvas -Title <s> [-Description] [-Theme Light|Dark|Auto] [-Navigation None|Sidebar|Compact|Top]
                 [-Width][-Height][-MinWidth][-MinHeight][-WindowTitleText][-WindowTitleIcon][-AllowCancel][-HideTitleBar]
Add-UICanvasPage -Title <s> [-Description][-Icon] [-Layout Canvas|Dock|Grid|Stack|VStack|HStack|Wrap]
                 [-Columns <int>][-ColumnWidths <s>][-Spacing <d>][-Padding <s>]   # avoid -Padding on stacks
Set-UITheme [-Preset Dark|Light|Midnight|Slate] [-Accent Indigo|Emerald|Sky|Amber|Rose|Violet|Cyan|Orange]
            [-Theme @{slot=hex}] [-Light @{}] [-Dark @{}] [-Mode Light|Dark|Auto]
Show-PoshUICanvas [-NoWait] [-AppDebug] [-Validate]   # returns the result hashtable; -Validate builds without launching
```

## Containers
```
Add-UICanvasCard   [-Layout][-Columns][-ColumnWidths][-Spacing][-Padding][-Background][-CornerRadius] -Children { }
Add-UICanvasPanel  [-Card][-Layout][-Columns][-ColumnWidths][-Spacing][-Padding][-Background][-CornerRadius] -Children { }
Add-UICanvasExpander [-Label] [-IsExpanded <bool>][-Background][-CornerRadius] -Children { }
```

## Text & display
```
Add-UICanvasLabel [-Label <s|sb>] [-Bind][-FontSize][-FontWeight][-Foreground][-Clock <ctl>][-ClockStop <ctl>]  # -Clock: engine-ticked mm:ss from a control holding UTC ticks
Add-UICanvasIcon  [-Icon <s>] [-FontSize][-Foreground]
Add-UICanvasBadge [-Label] [-Value][-Severity Neutral|Info|Success|Warning|Error][-Icon]
Add-UICanvasBanner [-Label] [-Value][-Severity Informational|Success|Warning|Error][-Icon][-Image]
Add-UICanvasHyperlink [-Label] [-NavigateUri]        # http/https/mailto/file only
Add-UICanvasMarkdown [-Text][-Path]
Add-UICanvasSeparator
```

## Buttons
```
Add-UICanvasButton [-Label] [-Action {}][-NavigateTo <title|index|Next|Prev|First|Last>][-Style Standard|Secondary|Accent|Primary|Subtle|Gradient][-Icon]
Add-UICanvasDropDownButton [-Label] [-Choices][-Value][-OnChange {}]
```

## Inputs
```
Add-UICanvasTextBox    [-Bind][-Value][-Placeholder][-OnChange {}]
Add-UICanvasMultiLine  [-Bind][-Value][-Placeholder][-OnChange {}]
Add-UICanvasRichEdit   [-Bind][-Value][-Placeholder][-OnChange {}]
Add-UICanvasPassword   [-Placeholder]                       # DPAPI/SecureString
Add-UICanvasAutoSuggest[-Bind][-Choices][-Value][-OnChange {}]
Add-UICanvasNumber     [-Bind][-Value][-Minimum][-Maximum][-OnChange {}]
Add-UICanvasNumberBox  [-Bind][-Value][-Minimum][-Maximum][-Step][-OnChange {}]
Add-UICanvasDropdown   [-Bind][-Choices][-Value][-ItemIcons][-OnChange {}][-DependsOn <s[]>][-OptionsScript {}]  # cascade: items recompute when DependsOn fields change
Add-UICanvasListBox    [-Bind][-Choices][-Value][-ItemIcons][-OnChange {}]
Add-UICanvasRadioGroup [-Bind][-Choices][-Value][-Orientation Vertical|Horizontal]
Add-UICanvasCheckbox   [-Label] [-Bind][-Value <bool>][-Icon][-OnChange {}]
Add-UICanvasToggle     [-Bind][-Value <bool>][-OnChange {}]
Add-UICanvasSlider     [-Bind][-Value][-Minimum][-Maximum][-Step][-OnChange {}]
Add-UICanvasRating     [-Value <int>][-Max][-FontSize][-OnChange {}]
Add-UICanvasDatePicker [-Bind][-Value][-OnChange {}]
Add-UICanvasTimePicker [-Value][-StepMinutes][-OnChange {}]
Add-UICanvasColorPicker[-Value][-Choices][-OnChange {}]
```

## Progress / status / data
```
Add-UICanvasProgressBar [-Bind][-Value][-Minimum][-Maximum][-Fill <hex>][-Glow $true|<hex>][-GlowPulse][-GlowRadius <d>]
Add-UICanvasProgressRing [-Foreground][-Thickness]
Add-UICanvasConsole     [-Bind][-Value]
Add-UICanvasDataGrid    [-Columns][-Items <obj[]>][-BindItems <key>][-Height]
Add-UICanvasTreeView    [-Nodes <obj[]>][-Height]
Add-UICanvasMetricCard  [-Caption] [-Value][-Trend Up|Down|Stable][-Delta]
Add-UICanvasStatusCard  [-Title] [-Items <"Label|State"[]>][-Background]
Add-UICanvasTableCard   [-Title] [-Columns][-Rows][-Background]
Add-UICanvasChartCard   [-Title] [-Type Bar|Line|Area|Donut|Sparkline][-Labels][-Values][-Datasets][-Series][-Spline][-Bind][-Foreground][-ChartHeight]
```

## Images / xaml / iteration / shapes / pickers
```
Add-UICanvasImage  -Source|-Value <path> [-Properties @{Stretch=}]
Add-UICanvasXaml   [-Markup <xaml>][-Path <file>][-Actions @{ xamlName = { } }]   # -Actions binds a scriptblock to each x:Name'd control INSIDE the markup
Add-UICanvasRepeater -Items <obj[]> -Template { param($item) ... }
Add-UICanvasRectangle/Ellipse/Line [-Fill][-Stroke][-StrokeThickness][-CornerRadius]
Add-UICanvasFolderPicker [-Value][-Placeholder][-Description][-ButtonLabel]
Add-UICanvasFilePicker   [-Value][-Placeholder][-Title][-Filter][-ButtonLabel]
```

## Reactive state
```
New-UICanvasState @{ key = value; ... }          # authoring: seed the store
Watch-UICanvasState -Key <s> -Action { }         # authoring: react to a key
Set-UICanvasState  <name> <value>                # runtime: set + fan out
Get-UICanvasState  <name>                         # runtime: read
# binding params: -Bind 'key' | -Bind 'tpl {key}' | -BindItems 'key' (DataGrid)
```

## Runtime (inside -Action / -OnChange)
```
Get-UICanvasValue <name>                          Set-UICanvasValue <name> <v> [-Quiet]
Set-UICanvasProperty <name> <prop> <v> [-Quiet]   # Enabled/Visible/Foreground/Background/Source/Spin/ItemsSource/AppendLine/...
Show-UICanvasPage <pageTitle|index>               Submit-UICanvas
Lock-UICanvasNavigation / Unlock-UICanvasNavigation
Start-UICanvasAsync { }                            # background work
Show-UICanvasToast  -Message <s> [-Severity info|success|warning|error] [-Duration <ms>]
Show-UICanvasDialog -Message <s> [-Title][-Prompt][-DefaultValue][-OkLabel][-CancelLabel]   # ->text/$true/$null
Show-UICanvasFlyout [-Target <name>] [-Items <s[]>] [-Title][-Message] [-Placement Bottom|Top|Left|Right|Mouse]  # ->item/$null
Set-UICanvasAnimate -Name <s> -Property Opacity|Width|Height|X|Y -To <d> [-From][-Duration][-Easing]
Select-UICanvasFolder [-Description]               Select-UICanvasFile [-Title][-Filter]
Show-UICanvasWindow <name>                         Close-UICanvasWindow            # close = from inside that window
```

## Flows
```
# Wizard
Add-UICanvasWizardSteps -Steps <s[]> [-Current <s>]
Add-UICanvasWizardNav [-Next <page>][-Back <page>][-Require <s[]>][-Validate { 'err' | $null }][-NextLabel][-BackLabel][-NoBack]
# Workflow
Add-UICanvasWorkflowStep -Name <s> [-Detail] -Script { } [-ExpectedSeconds][-Retry][-TimeoutSeconds][-SkipWhen <cond>][-OnClick {}]   # Retry/Timeout/SkipWhen need -Engine
Add-UICanvasWorkflow [-Engine][-Name][-StartLabel][-StartIcon][-ShowLog][-AutoStart][-LockNavigation][-LockOnStart][-StepsHeight][-Compact][-NoStartButton][-NoHeader][-StateFile]
#   -Engine (preferred): steps run on the engine executor, own runspace (UI stays live); step context $wf:
#   $wf.UpdateProgress(pct,msg) $wf.WriteOutput(txt,lvl) $wf.GetValue(n)/$wf.SetValue(n,v) $wf.SetData(k,v)/$wf.GetData(k) $wf.SkipTask(r) $wf.RequestReboot(r)

# ScriptCard (dashboard on-demand runner; runs OFF the gate, streams output live)
Add-UICanvasScriptCard <Title> -Script { } [-Name][-Detail][-RunLabel][-RunIcon][-OutputHeight][-Properties]

# Layout / chrome (1.4)
Add-UICanvasTabs [-Name][-SelectedIndex][-OnChange {}] -Children { Add-UICanvasTab <Header> [-Layout][-Disabled] -Children { } }
#   value = selected index; Set-UICanvasValue accepts an index OR a tab header string
Add-UICanvasMenu -Items @(@{Text='File';Items=@(@{Text='Open';Gesture='Ctrl+O';Action={}},@{Text='-'})})  # leaf: Icon/Gesture/Disabled/Checked/Name/Page
Add-UICanvasGridSplitter [-Orientation Vertical|Horizontal][-Thickness]   # own Grid cell BETWEEN two panes
Add-UICanvasViewbox [-Stretch Uniform|Fill|UniformToFill|None][-StretchDirection Both|UpOnly|DownOnly][-Layout][-Spacing] -Children { }
Add-UICanvasToolbar [-Brand][-BrandIcon][-Status][-Links <page titles>][-Active <title>][-Actions @(@{Icon;Tooltip;Action={}|Page})]   # ~54px, fixed
Add-UICanvasFooter [-LeftText][-Links @('text' | @{Text;Action={}} | @{Text;Page})]
New-UICanvasWindow <name> -Content { } [-Title][-Width][-Height][-MinWidth][-MinHeight][-Modal][-Topmost][-Resizable <bool>]
                   [-Position CenterOwner|CenterScreen|Manual][-X][-Y][-Icon][-Layout][-Columns][-Spacing][-Padding]
                   [-NoScroll][-HideTitleBar][-TitleBarColor][-TitleBarText]   # template; open with Show-UICanvasWindow
# runner publishes: <wf>_status, <wf>_pct, <wf>_gauge, <wf>_start/_end, <step>_elapsed/_remaining
```

## Keyboard
```
Add-UICanvasShortcut '<gesture>' -Action { }      # e.g. 'Ctrl+S', 'F5'. -Action must be named
```

## Properties bag keys
`Margin '<L,T,R,B>'` · `VAlign`/`HAlign` · `CornerRadius` · `Background` · `HoverBackground` ·
`HoverScale <1.04>`/`HoverScaleMs` ·
`BackgroundImage`/`BackgroundStretch`/`BackgroundOpacity` · `Stagger <ms>` · `Scroll $true` · `ContentAlign` ·
`Layout` · `Stretch` · `IconSize` · `ImageWidth`.

## Motion  (full treatment: [16](16-animation-and-motion.md))
```
# declarative, no island needed
-Properties @{ Stagger = 70 }                     # container: children cascade in, one-shot at page entry
-Properties @{ HoverScale = 1.04; HoverScaleMs = 150 }
-Properties @{ Glow = '#34D399'; GlowRadius = 16; GlowPulse = $true }
Set-UICanvasProperty <name> Spin $true            # continuous rotation on an icon/image
Set-UICanvasAnimate -Name <s> -Property Opacity|Width|Height|X|Y -To <d> [-From][-Duration][-Easing]
```
That is the whole runtime surface: **no rotation, scale, colour, blur, clip — and nothing that sequences.**
For a real timeline use a WPF `Storyboard` inside `Add-UICanvasXaml`:
```xml
<Grid.Triggers><EventTrigger RoutedEvent="FrameworkElement.Loaded"><BeginStoryboard>
  <Storyboard>            <!-- + Duration and RepeatBehavior="Forever" to loop -->
    <DoubleAnimation Storyboard.TargetName="x" Storyboard.TargetProperty="Opacity" From="0" To="1"
                     BeginTime="0:0:0.2" Duration="0:0:0.45" EasingFunction="{StaticResource eo}"/>
  </Storyboard>
</BeginStoryboard></EventTrigger></Grid.Triggers>
```
- **Must self-start from `Loaded`.** Canvas actions run off the UI thread, so PowerShell cannot call `.Begin()`.
- Animatable paths: `Opacity` · `RenderTransform.X|Y|ScaleX|ScaleY|Angle` · `RenderTransform.Children[0].ScaleX`
  (`TransformGroup`) · `Effect.Radius` (blur) · `Effect.BlurRadius`/`Effect.Opacity` (glow) ·
  `OpacityMask.StartPoint`/`EndPoint` (**`PointAnimation`**) · `Clip.RadiusX`/`RadiusY` · `StrokeDashOffset`.
- `RenderTransformOrigin="0.5,0.5"` or it scales about the top-left. · `StrokeDashArray` is in **multiples of
  `StrokeThickness`**. · `TextElement.FontFamily` on a panel, not `FontFamily`. · one `Effect` per element (nest).
- Page-load hook: **`-Refresh` fires once at registration.** Pair with
  `Start-UICanvasAsync { Start-Sleep -Milliseconds N; Show-UICanvasPage '<title>' }` for ms-precision sequencing.

## Reminders
- UTF-8 **with BOM** for non-ASCII. · `-Refresh` = seconds, **and fires once at registration**. · alias `$Name`
  before `-Children`. · authoring vs runtime cmdlets. · run with WinPS 5.1. · log: `PoshUI\bin\logs\PoshUI.log`.
