# 08 — Cheatsheet (every cmdlet, terse)

Universal control params (most controls): `-Name -X -Y -Width -Height -ZIndex -Tooltip -Visible -Enabled -Refresh -Properties`.

## Lifecycle / shell
```
New-PoshUICanvas -Title <s> [-Description] [-Theme Light|Dark|Auto] [-Navigation None|Sidebar|Compact|Top]
                 [-Width][-Height][-MinWidth][-MinHeight][-WindowTitleText][-WindowTitleIcon][-AllowCancel]
Add-UICanvasPage -Title <s> [-Description][-Icon] [-Layout Canvas|Dock|Grid|Stack|VStack|HStack|Wrap]
                 [-Columns <int>][-ColumnWidths <s>][-Spacing <d>][-Padding <s>]   # avoid -Padding on stacks
Set-UITheme [-Preset Dark|Light|Midnight|Slate] [-Accent Indigo|Emerald|Sky|Amber|Rose|Violet|Cyan|Orange]
            [-Theme @{slot=hex}] [-Light @{}] [-Dark @{}] [-Mode Light|Dark|Auto]
Show-PoshUICanvas [-NoWait] [-AppDebug]       # returns the result hashtable
```

## Containers
```
Add-UICanvasCard   [-Layout][-Columns][-ColumnWidths][-Spacing][-Padding][-Background][-CornerRadius] -Children { }
Add-UICanvasPanel  [-Card][-Layout][-Columns][-ColumnWidths][-Spacing][-Padding][-Background][-CornerRadius] -Children { }
Add-UICanvasExpander [-Label] [-IsExpanded <bool>][-Background][-CornerRadius] -Children { }
```

## Text & display
```
Add-UICanvasLabel [-Label <s|sb>] [-Bind][-FontSize][-FontWeight][-Foreground]
Add-UICanvasIcon  [-Icon <s>] [-FontSize][-Foreground]
Add-UICanvasBadge [-Label] [-Value][-Severity Neutral|Info|Success|Warning|Error][-Icon]
Add-UICanvasBanner [-Label] [-Value][-Severity Informational|Success|Warning|Error][-Icon][-Image]
Add-UICanvasHyperlink [-Label] [-NavigateUri]        # http/https/mailto/file only
Add-UICanvasMarkdown [-Text][-Path]
Add-UICanvasSeparator
```

## Buttons
```
Add-UICanvasButton [-Label] [-Action {}][-Style Standard|Secondary|Accent|Primary|Subtle][-Icon]
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
Add-UICanvasXaml   [-Markup <xaml>][-Path <file>]
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
Get-UICanvasValue <name>                          Set-UICanvasValue <name> <v>
Set-UICanvasProperty <name> <prop> <v>            # Enabled/Visible/Foreground/Background/Source/Spin/ItemsSource/...
Show-UICanvasPage <pageTitle|index>               Submit-UICanvas
Lock-UICanvasNavigation / Unlock-UICanvasNavigation
Start-UICanvasAsync { }                            # background work
Show-UICanvasToast  -Message <s> [-Severity info|success|warning|error] [-Duration <ms>]
Show-UICanvasDialog -Message <s> [-Title][-Prompt][-DefaultValue][-OkLabel][-CancelLabel]   # ->text/$true/$null
Show-UICanvasFlyout [-Target <name>] [-Items <s[]>] [-Title][-Message] [-Placement Bottom|Top|Left|Right|Mouse]  # ->item/$null
Set-UICanvasAnimate -Name <s> -Property Opacity|Width|Height|X|Y -To <d> [-From][-Duration][-Easing]
Select-UICanvasFolder [-Description]               Select-UICanvasFile [-Title][-Filter]
```

## Flows
```
# Wizard
Add-UICanvasWizardSteps -Steps <s[]> [-Current <s>]
Add-UICanvasWizardNav [-Next <page>][-Back <page>][-Require <s[]>][-Validate { 'err' | $null }][-NextLabel][-BackLabel][-NoBack]
# Workflow
Add-UICanvasWorkflowStep -Name <s> [-Detail] -Script { } [-ExpectedSeconds][-Retry][-TimeoutSeconds][-SkipWhen <cond>]   # Retry/Timeout/SkipWhen need -Engine
Add-UICanvasWorkflow [-Engine][-Name][-StartLabel][-StartIcon][-ShowLog][-AutoStart][-LockNavigation][-LockOnStart][-StepsHeight][-NoStartButton][-NoHeader][-StateFile]
#   -Engine (preferred): steps run on the engine executor, own runspace (UI stays live); step context $wf:
#   $wf.UpdateProgress(pct,msg) $wf.WriteOutput(txt,lvl) $wf.GetValue(n)/$wf.SetValue(n,v) $wf.SetData(k,v)/$wf.GetData(k) $wf.SkipTask(r) $wf.RequestReboot(r)

# ScriptCard (dashboard on-demand runner; runs OFF the gate, streams output live)
Add-UICanvasScriptCard <Title> -Script { } [-Name][-Detail][-RunLabel][-RunIcon][-OutputHeight][-Properties]

# Layout / chrome (1.4)
Add-UICanvasTabs [-Name][-SelectedIndex][-OnChange {}] -Children { Add-UICanvasTab <Header> [-Layout][-Disabled] -Children { } }
#   value = selected index; Set-UICanvasValue accepts an index OR a tab header string
Add-UICanvasMenu -Items @(@{Text='File';Items=@(@{Text='Open';Gesture='Ctrl+O';Action={}},@{Text='-'})})  # leaf: Icon/Gesture/Disabled/Checked/Name/Page
Add-UICanvasGridSplitter [-Orientation Vertical|Horizontal][-Thickness]   # own Grid cell BETWEEN two panes
Add-UICanvasViewbox [-Stretch Uniform|Fill|UniformToFill|None][-StretchDirection Both|UpOnly|DownOnly] -Children { }
# runner publishes: <wf>_status, <wf>_pct, <wf>_gauge, <wf>_start/_end, <step>_elapsed/_remaining
```

## Keyboard
```
Add-UICanvasShortcut '<gesture>' { }              # e.g. 'Ctrl+S', 'F5'
```

## Properties bag keys
`Margin '<L,T,R,B>'` · `VAlign`/`HAlign` · `CornerRadius` · `Background` · `HoverBackground` ·
`BackgroundImage`/`BackgroundStretch`/`BackgroundOpacity` · `Stagger <ms>` · `Scroll $true` · `ContentAlign` ·
`Layout` · `Stretch` · `IconSize` · `ImageWidth`.

## Reminders
- UTF-8 **with BOM** for non-ASCII. · `-Refresh` = seconds. · alias `$Name` before `-Children`. ·
  authoring vs runtime cmdlets. · run with WinPS 5.1. · log: `PoshUI\bin\logs\PoshUI.log`.
