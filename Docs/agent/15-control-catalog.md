# 15 - Control catalogue

Every control `Type` the Canvas engine can build, which `-Properties` keys it **actually reads**, and
the cmdlet that emits it. **Generated** from `Launcher\Designer\catalog.json` - do not edit by hand;
run `Launcher\Designer\Tools\Extract-CanvasCatalog.ps1` then `Build-CanvasCatalog.ps1`.

Why this exists: a key the factory does not read is **accepted and ignored**, not rejected. `ImageWidth`
on an `Image` rendered the icon at full bleed; `-Padding` on a stack rendered a blank page. This table
is the ground truth the engine-side `[unused property]` warning checks against.

## Keys every control honours

| Group | Keys |
|---|---|
| Style | `Background` `FontFamily` `FontSize` `FontWeight` `Foreground` `HAlign` `HorizontalAlignment` `Margin` `Opacity` `VAlign` `VerticalAlignment` |
| Glow | `Fill` `Foreground` `Glow` `GlowPulse` `GlowRadius` |
| Geometry (top-level fields, not Properties) | `X` `Y` `Width` `Height` `ZIndex` |
| Container-only | `Background` `BackgroundImage` `BackgroundOpacity` `BackgroundStretch` `Columns` `ColumnWidths` `CornerRadius` `HoverBackground` `Layout` `Padding` `Scroll` `SelectedIndex` `Stagger` `Stretch` `StretchDirection` |
| Read off a CHILD by its parent (Grid/Dock) | `Column` `ColumnSpan` `Dock` `Row` `RowSpan` |

**Layouts** (case-insensitive; anything else falls back to absolute Canvas): `dock`, `grid`, `horizontal`, `hstack`, `stack`, `vertical`, `vstack`, `wrap`.
`Dock` is valid on `Add-UICanvasPage` only - `Add-UICanvasPanel -Layout Dock` fails its ValidateSet.

## Container

### `GridSplitter`

Draggable divider; must occupy its own Grid cell between two panes.

Cmdlet: `Add-UICanvasGridSplitter`

```powershell
Add-UICanvasGridSplitter -Orientation Vertical
```

| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `Orientation` | String | Vertical \| Horizontal |
| `Thickness` | Double |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Panel`

Generic container. -Layout picks Canvas (absolute X/Y) or a flow layout. NOTE: Dock is a PAGE layout and is rejected here; -Padding on a stack layout has rendered pages blank - inset with child Margins instead.

Cmdlet: `Add-UICanvasPanel` - container

```powershell
Add-UICanvasPanel -Layout VStack -Spacing 8 -Children { Add-UICanvasLabel 'Inside' }
```

| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | Int32 |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | Double |  |
| `HoverBackground` | String |  |
| `HoverScale` | String |  |
| `HoverScaleMs` | String |  |
| `Layout` | String | Canvas \| Stack \| VStack \| HStack \| Grid \| Wrap |
| `Padding` | Double |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |
| `Spacing` * | Double |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Card`

Panel with card chrome (background, radius, shadow). -Action makes the whole card clickable.

Cmdlet: `Add-UICanvasCard` - container

```powershell
Add-UICanvasCard -Layout VStack -Spacing 8 -Children { Add-UICanvasLabel 'Card' }
```

| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | Int32 |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | Double |  |
| `HoverBackground` | String |  |
| `HoverScale` | String |  |
| `HoverScaleMs` | String |  |
| `Layout` | String | Canvas \| Stack \| VStack \| HStack \| Grid \| Wrap |
| `Padding` | Double |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |
| `Spacing` * | Double |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Group`

Engine spelling of a plain Panel. No cmdlet emits it; Add-UICanvasControl -Type Group reaches it.


| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `HoverScale` | String |  |
| `HoverScaleMs` | String |  |
| `Layout` | String |  |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `ControlGroup`

Engine spelling of a Card (gets card chrome). No cmdlet emits it; prefer Add-UICanvasCard.


| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `HoverScale` | String |  |
| `HoverScaleMs` | String |  |
| `Layout` | String |  |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Expander`

Collapsible section.

Cmdlet: `Add-UICanvasExpander` - container

```powershell
Add-UICanvasExpander -Label 'More' -IsExpanded $false -Children { Add-UICanvasLabel 'Hidden' }
```

| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | Double |  |
| `HoverBackground` | String |  |
| `Layout` | String |  |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |
| `IsExpanded` * | Boolean |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `CardExpander`

Engine type 'CardExpander'. No authoring cmdlet emits it directly.


| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `Layout` | String |  |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Stack`

Engine type 'Stack'. No authoring cmdlet emits it directly.


| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `Layout` | String |  |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `VStack`

Engine type 'VStack'. No authoring cmdlet emits it directly.


| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `Layout` | String |  |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `HStack`

Engine type 'HStack'. No authoring cmdlet emits it directly.


| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `Layout` | String |  |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Grid`

Engine type 'Grid'. No authoring cmdlet emits it directly.


| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `Layout` | String |  |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Wrap`

Engine type 'Wrap'. No authoring cmdlet emits it directly.


| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `Layout` | String |  |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Dock`

Engine type 'Dock'. No authoring cmdlet emits it directly.


| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `Layout` | String |  |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Tabs`

Tab strip; children must be Add-UICanvasTab. Value is the selected index.

Cmdlet: `Add-UICanvasTabs` - container

```powershell
Add-UICanvasTabs -Name tabs1 -Children { Add-UICanvasTab 'One' -Children { Add-UICanvasLabel 'A' }; Add-UICanvasTab 'Two' -Children { Add-UICanvasLabel 'B' } }
```

| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `Layout` | String |  |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | Int32 |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `TabControl`

Engine type 'TabControl'. No authoring cmdlet emits it directly.


| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `Layout` | String |  |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Tab`

One page of a Tabs control. Only valid inside Add-UICanvasTabs.

Cmdlet: `Add-UICanvasTab` - container

```powershell
Add-UICanvasTab 'Tab' -Children { Add-UICanvasLabel 'content' }
```

| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | Int32 |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `Layout` | String | VStack \| HStack \| Grid \| Wrap \| Canvas |
| `Padding` | Object |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |
| `Spacing` * | Double |  |
| `Disabled` * | SwitchParameter |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `TabItem`

Engine type 'TabItem'. No authoring cmdlet emits it directly.


| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `Layout` | String |  |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String |  |
| `StretchDirection` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Viewbox`

Scales its single child to fit.

Cmdlet: `Add-UICanvasViewbox` - container

```powershell
Add-UICanvasViewbox -Stretch Uniform -Children { Add-UICanvasLabel 'Scaled' -FontSize 40 }
```

| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `BackgroundImage` | String |  |
| `BackgroundOpacity` | String |  |
| `BackgroundStretch` | String |  |
| `Columns` | String |  |
| `ColumnWidths` | String |  |
| `CornerRadius` | String |  |
| `HoverBackground` | String |  |
| `Layout` | String | VStack \| HStack \| Grid \| Wrap \| Canvas |
| `Padding` | String |  |
| `Scroll` | String |  |
| `SelectedIndex` | String |  |
| `Stagger` | String |  |
| `Stretch` | String | Uniform \| Fill \| UniformToFill \| None |
| `StretchDirection` | String | Both \| UpOnly \| DownOnly |
| `Spacing` * | Double |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

## Display

### `Label` (also `TextBlock`)

Static or refreshing text. -Label { } with -Refresh re-evaluates on a timer; Selectable renders a read-only TextBox so log text can be copied.

Cmdlet: `Add-UICanvasLabel`

```powershell
Add-UICanvasLabel 'Label' -Name lbl1
```

| Property | Type | Values |
|---|---|---|
| `FontSize` | Double |  |
| `FontWeight` | String |  |
| `Foreground` | String |  |
| `Selectable` | String |  |
| `Bind` * | String |  |
| `Clock` * | String |  |
| `ClockStop` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Icon` (also `FontIcon`)

A glyph by name or MDL2 code point, or a PNG path.

Cmdlet: `Add-UICanvasIcon`

```powershell
Add-UICanvasIcon 'info' -FontSize 20
```

| Property | Type | Values |
|---|---|---|
| `FontSize` | Double |  |
| `Foreground` | String |  |
| `Height` | Double |  |
| `Icon` | String |  |
| `Width` | Double |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Badge` (also `Pill`, `Tag`)

Small status pill.

Cmdlet: `Add-UICanvasBadge`

```powershell
Add-UICanvasBadge 'Ready' -Severity Success
```

| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `FontSize` | Double |  |
| `Foreground` | String |  |
| `Icon` | String |  |
| `Severity` | String | Neutral \| Info \| Success \| Warning \| Error |
| `Style` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Markdown`

Rendered markdown from -Text or -Path.

Cmdlet: `Add-UICanvasMarkdown`

```powershell
Add-UICanvasMarkdown -Text '# Title'
```

| Property | Type | Values |
|---|---|---|
| `Path` | String |  |
| `Text` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Image`

Raster image from a path. Size it with -Width/-Height: the ImageWidth property is read only by Banner and is ignored here.

Cmdlet: `Add-UICanvasImage`

```powershell
Add-UICanvasImage -Source 'C:\path\logo.png' -Width 64 -Height 64
```

| Property | Type | Values |
|---|---|---|
| `Stretch` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Hyperlink`

Opens http/https/mailto/file URIs only.

Cmdlet: `Add-UICanvasHyperlink`

```powershell
Add-UICanvasHyperlink 'Docs' -NavigateUri 'https://example.com'
```

| Property | Type | Values |
|---|---|---|
| `Foreground` | String |  |
| `Glow` | String |  |
| `GlowRadius` | String |  |
| `HoverForeground` | String |  |
| `HoverGlow` | String |  |
| `NavigateUri` | String |  |
| `Underline` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Separator`

Horizontal rule.

Cmdlet: `Add-UICanvasSeparator`

```powershell
Add-UICanvasSeparator
```

_Reads no type-specific properties; the universal keys above still apply._

### `Banner` (also `InfoBar`)

Prominent header strip with optional icon and hero image.

Cmdlet: `Add-UICanvasBanner`

```powershell
Add-UICanvasBanner 'Heading' -Severity Informational
```

| Property | Type | Values |
|---|---|---|
| `Icon` | String |  |
| `IconSize` | String |  |
| `Image` | String |  |
| `ImageWidth` | String |  |
| `Severity` | String | Informational \| Success \| Warning \| Error |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

## Input

### `TextBox`

Single-line input. Value comes back in the result under -Name.

Cmdlet: `Add-UICanvasTextBox`

```powershell
Add-UICanvasTextBox -Name text1 -Placeholder 'Type here'
```

| Property | Type | Values |
|---|---|---|
| `Placeholder` | String |  |
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `MultiLine`

Multi-line text input.

Cmdlet: `Add-UICanvasMultiLine`

```powershell
Add-UICanvasMultiLine -Name notes1 -Placeholder 'Notes'
```

| Property | Type | Values |
|---|---|---|
| `Placeholder` | String |  |
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `RichEdit`

Emitted by Add-UICanvasRichEdit.

Cmdlet: `Add-UICanvasRichEdit`

```powershell
Add-UICanvasRichEdit -Name richedit1
```

| Property | Type | Values |
|---|---|---|
| `Placeholder` | String |  |
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Password`

Masked input. Returned as SecureString; DPAPI-protected in the result file. No -Value, no -OnChange by design.

Cmdlet: `Add-UICanvasPassword`

```powershell
Add-UICanvasPassword -Name pwd1 -Placeholder 'Password'
```

| Property | Type | Values |
|---|---|---|
| `Placeholder` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Numeric`

Numeric spinner. Emitted by Add-UICanvasNumber; Minimum/Maximum/Step live in Properties.

Cmdlet: `Add-UICanvasNumber`

```powershell
Add-UICanvasNumber -Name num1 -Value 1 -Minimum 0 -Maximum 100
```

| Property | Type | Values |
|---|---|---|
| `Maximum` | Double |  |
| `Minimum` | Double |  |
| `Step` | String |  |
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `NumberBox`

Emitted by Add-UICanvasNumberBox.

Cmdlet: `Add-UICanvasNumberBox`

```powershell
Add-UICanvasNumberBox -Name numberbox1
```

| Property | Type | Values |
|---|---|---|
| `Maximum` | Double |  |
| `Minimum` | Double |  |
| `Step` | Double |  |
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Dropdown` (also `ComboBox`)

Single-select list. -DependsOn + -OptionsScript make it cascade from other fields.

Cmdlet: `Add-UICanvasDropdown`

```powershell
Add-UICanvasDropdown -Name pick1 -Choices @('One','Two','Three') -Value 'One'
```

| Property | Type | Values |
|---|---|---|
| `ItemIcons` | String[] |  |
| `Bind` * | String |  |
| `DependsOn` * | String[] |  |
| `OptionsScript` * | ScriptBlock |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `ListBox`

Visible list of choices.

Cmdlet: `Add-UICanvasListBox`

```powershell
Add-UICanvasListBox -Name list1 -Choices @('A','B','C')
```

| Property | Type | Values |
|---|---|---|
| `ItemIcons` | String[] |  |
| `Bind` * | String |  |
| `DependsOn` * | String[] |  |
| `OptionsScript` * | ScriptBlock |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `RadioGroup`

Mutually exclusive choices as radio buttons.

Cmdlet: `Add-UICanvasRadioGroup`

```powershell
Add-UICanvasRadioGroup -Name radio1 -Choices @('Yes','No') -Value 'Yes'
```

| Property | Type | Values |
|---|---|---|
| `Orientation` | String | Vertical \| Horizontal |
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Checkbox`

Boolean with a label.

Cmdlet: `Add-UICanvasCheckbox`

```powershell
Add-UICanvasCheckbox 'Enable' -Name chk1 -Value $true
```

| Property | Type | Values |
|---|---|---|
| `Icon` | String |  |
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `CheckBox`

Boolean with a label.

Cmdlet: `Add-UICanvasCheckbox`

```powershell
Add-UICanvasCheckbox 'Enable' -Name chk1 -Value $true
```

| Property | Type | Values |
|---|---|---|
| `Icon` | String |  |
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Toggle` (also `ToggleSwitch`)

Boolean as a switch.

Cmdlet: `Add-UICanvasToggle`

```powershell
Add-UICanvasToggle -Name tgl1 -Value $false
```

| Property | Type | Values |
|---|---|---|
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Rating`

Emitted by Add-UICanvasRating.

Cmdlet: `Add-UICanvasRating`

```powershell
Add-UICanvasRating -Name rating1
```

| Property | Type | Values |
|---|---|---|
| `FontSize` | Double |  |
| `Max` | Int32 |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `ColorPicker`

Emitted by Add-UICanvasColorPicker.

Cmdlet: `Add-UICanvasColorPicker`

```powershell
Add-UICanvasColorPicker -Name colorpicker1
```

_Reads no type-specific properties; the universal keys above still apply._

### `TimePicker`

Emitted by Add-UICanvasTimePicker.

Cmdlet: `Add-UICanvasTimePicker`

```powershell
Add-UICanvasTimePicker -Name timepicker1
```

| Property | Type | Values |
|---|---|---|
| `StepMinutes` | Int32 |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Slider`

Ranged value.

Cmdlet: `Add-UICanvasSlider`

```powershell
Add-UICanvasSlider -Name sld1 -Minimum 0 -Maximum 100 -Value 50
```

| Property | Type | Values |
|---|---|---|
| `Maximum` | Double |  |
| `Minimum` | Double |  |
| `Step` | Double |  |
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `AutoSuggest`

Emitted by Add-UICanvasAutoSuggest.

Cmdlet: `Add-UICanvasAutoSuggest`

```powershell
Add-UICanvasAutoSuggest -Name autosuggest1
```

| Property | Type | Values |
|---|---|---|
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `DatePicker` (also `CalendarView`)

Date input.

Cmdlet: `Add-UICanvasDatePicker`

```powershell
Add-UICanvasDatePicker -Name date1
```

| Property | Type | Values |
|---|---|---|
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

## Action

### `Button`

Runs an -Action scriptblock in the app runspace. -NavigateTo is sugar for Show-UICanvasPage.

Cmdlet: `Add-UICanvasButton`

```powershell
Add-UICanvasButton 'Button' -Name btn1 -Style Accent -Action { Show-UICanvasToast -Message 'clicked' }
```

| Property | Type | Values |
|---|---|---|
| `ContentAlign` | String |  |
| `GradientFrom` | String |  |
| `GradientTo` | String |  |
| `Icon` | String |  |
| `Style` | String | Standard \| Secondary \| Accent \| Primary \| Subtle \| Gradient |
| `NavigateTo` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Menu` (also `MenuBar`)

Menu bar from nested @{Text; Items; Action} hashtables.


| Property | Type | Values |
|---|---|---|
| `Background` | String |  |
| `ItemsJson` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Toolbar`

Docked app toolbar with brand, links and actions. Links become hidden Event controls with generated names.


| Property | Type | Values |
|---|---|---|
| `ActionsJson` | String |  |
| `Active` | String |  |
| `Background` | String |  |
| `BarHeight` | String |  |
| `Brand` | String |  |
| `BrandIcon` | String |  |
| `LinksJson` | String |  |
| `Status` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Footer`

Docked status/footer strip.


| Property | Type | Values |
|---|---|---|
| `LeftText` | String |  |
| `LinksJson` | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `DropDownButton` (also `SplitButton`)

Emitted by Add-UICanvasDropDownButton.

Cmdlet: `Add-UICanvasDropDownButton`

```powershell
Add-UICanvasDropDownButton 'DropDownButton' -Name dropdownbutton1
```

_Reads no type-specific properties; the universal keys above still apply._

## Data

### `ProgressBar`

Bound or static progress. -Glow/-GlowPulse for a live indicator.

Cmdlet: `Add-UICanvasProgressBar`

```powershell
Add-UICanvasProgressBar -Name prog1 -Value 40 -Glow $true
```

| Property | Type | Values |
|---|---|---|
| `Fill` | String |  |
| `Foreground` | String |  |
| `Glow` | Object |  |
| `GlowPulse` | SwitchParameter |  |
| `GlowRadius` | Double |  |
| `Maximum` | Double |  |
| `Minimum` | Double |  |
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Console`

Monospace output pane; append with Set-UICanvasProperty `<name>` AppendLine.

Cmdlet: `Add-UICanvasConsole`

```powershell
Add-UICanvasConsole -Name log1 -Height 160
```

| Property | Type | Values |
|---|---|---|
| `Bind` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Workflow`

Engine-native step runner (Add-UICanvasWorkflow -Engine). Steps carry retry/timeout/skip; runner publishes `<wf>`_status/_pct.

Cmdlet: `Add-UICanvasWorkflow`

| Property | Type | Values |
|---|---|---|
| `StepsJson` | String |  |
| `Title` | String |  |
| `Engine` * | SwitchParameter |  |
| `StartLabel` * | String |  |
| `StartIcon` * | String |  |
| `ShowLog` * | SwitchParameter |  |
| `AutoStart` * | SwitchParameter |  |
| `LockNavigation` * | SwitchParameter |  |
| `LockOnStart` * | SwitchParameter |  |
| `StepsHeight` * | Int32 |  |
| `Compact` * | SwitchParameter |  |
| `NoStartButton` * | SwitchParameter |  |
| `NoHeader` * | SwitchParameter |  |
| `StateFile` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `DataGrid`

Tabular data. Pass rows with -Items (serialised to ItemsJson) or bind with -BindItems.

Cmdlet: `Add-UICanvasDataGrid`

```powershell
Add-UICanvasDataGrid -Name grid1 -Columns @('Name','Status') -Items @(@{Name='a';Status='ok'})
```

| Property | Type | Values |
|---|---|---|
| `Columns` | String[] |  |
| `ItemsJson` | String |  |
| `Items` * | Object[] |  |
| `BindItems` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `TreeView`

Hierarchical nodes via -Nodes.

Cmdlet: `Add-UICanvasTreeView`

```powershell
Add-UICanvasTreeView -Name tree1 -Nodes @(@{Text='Root';Children=@(@{Text='Child'})})
```

| Property | Type | Values |
|---|---|---|
| `NodesJson` | String |  |
| `Nodes` * | Object[] |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `ProgressRing`

Indeterminate spinner.

Cmdlet: `Add-UICanvasProgressRing`

```powershell
Add-UICanvasProgressRing
```

| Property | Type | Values |
|---|---|---|
| `Foreground` | String |  |
| `Thickness` | Double |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `MetricCard`

Dashboard KPI tile with trend.

Cmdlet: `Add-UICanvasMetricCard`

```powershell
Add-UICanvasMetricCard 'Servers' -Value 12 -Trend Up -Delta '+2'
```

| Property | Type | Values |
|---|---|---|
| `CornerRadius` | String |  |
| `Fill` | String |  |
| `Stroke` | String |  |
| `StrokeThickness` | String |  |
| `Caption` * | String |  |
| `Trend` * | String | Up \| Down \| Stable |
| `Delta` * | String |  |
| `Background` * | String |  |
| `Foreground` * | String |  |
| `FontSize` * | Double |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `StatusCard`

List of 'Label|State' rows.

Cmdlet: `Add-UICanvasStatusCard`

```powershell
Add-UICanvasStatusCard -Title 'Services' -Items @('Web|ok','DB|warn')
```

| Property | Type | Values |
|---|---|---|
| `CornerRadius` | String |  |
| `Fill` | String |  |
| `Stroke` | String |  |
| `StrokeThickness` | String |  |
| `Title` * | String |  |
| `Items` * | String[] |  |
| `Background` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `TableCard`

Small table inside a card.

Cmdlet: `Add-UICanvasTableCard`

```powershell
Add-UICanvasTableCard -Title 'Summary' -Columns @('K','V') -Rows @(@('a','1'))
```

| Property | Type | Values |
|---|---|---|
| `CornerRadius` | String |  |
| `Fill` | String |  |
| `Stroke` | String |  |
| `StrokeThickness` | String |  |
| `Title` * | String |  |
| `Columns` * | String[] |  |
| `Rows` * | Object[] |  |
| `Background` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `ChartCard`

Bar/Line/Area/Donut/Sparkline. Donut renders a total plus a legend - use a XAML ring for a single reading.

Cmdlet: `Add-UICanvasChartCard`

```powershell
Add-UICanvasChartCard -Title 'Trend' -Type Line -Labels @('a','b','c') -Values @(1,3,2)
```

| Property | Type | Values |
|---|---|---|
| `CornerRadius` | String |  |
| `Fill` | String |  |
| `Stroke` | String |  |
| `StrokeThickness` | String |  |
| `Title` * | String |  |
| `Labels` * | String[] |  |
| `Values` * | Double[] |  |
| `Series` * | String |  |
| `Type` * | String | Bar \| Line \| Area \| Donut \| Sparkline |
| `Datasets` * | Object[] |  |
| `Spline` * | SwitchParameter |  |
| `Bind` * | String |  |
| `Background` * | String |  |
| `Foreground` * | String |  |
| `ChartHeight` * | Double |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

## Shape

### `Rectangle`

Filled/stroked rectangle. Canvas-layout pages only for X/Y.

Cmdlet: `Add-UICanvasRectangle`

```powershell
Add-UICanvasRectangle -X 0 -Y 0 -Width 120 -Height 40 -Fill '#1F3864'
```

| Property | Type | Values |
|---|---|---|
| `CornerRadius` | Double |  |
| `Fill` | String |  |
| `Stroke` | String |  |
| `StrokeThickness` | Double |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Ellipse`

Filled/stroked ellipse.

Cmdlet: `Add-UICanvasEllipse`

```powershell
Add-UICanvasEllipse -X 0 -Y 0 -Width 60 -Height 60 -Fill '#2FD4E6'
```

| Property | Type | Values |
|---|---|---|
| `Fill` | String |  |
| `Stroke` | String |  |
| `StrokeThickness` | Double |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `Line`

Line from X,Y to X2,Y2.

Cmdlet: `Add-UICanvasLine`

```powershell
Add-UICanvasLine -X 0 -Y 0 -X2 120 -Y2 0 -Stroke '#94A3B8'
```

| Property | Type | Values |
|---|---|---|
| `Stroke` | String |  |
| `StrokeThickness` | Double |  |
| `X2` | Double |  |
| `Y2` | Double |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

## Escape

### `Xaml`

Raw WPF markup island. {DynamicResource PrimaryBrush} etc. resolve; x:Name'd controls run PowerShell via -Actions. The designer treats the markup as opaque.

Cmdlet: `Add-UICanvasXaml`

```powershell
Add-UICanvasXaml -Name island1 -Markup '<Border Width="120" Height="40" Background="{DynamicResource CardBackgroundBrush}"/>'
```

| Property | Type | Values |
|---|---|---|
| `Markup` | String |  |
| `Path` | String |  |
| `Actions` * | Hashtable |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

## Logic

### `Shortcut`

Keyboard gesture bound to an action. No visual.

Cmdlet: `Add-UICanvasShortcut` - no visual

```powershell
Add-UICanvasShortcut 'Ctrl+S' -Action { Show-UICanvasToast -Message 'saved' }
```

| Property | Type | Values |
|---|---|---|
| `Key` * | String |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `StateInit`

Seeds the reactive state store (New-UICanvasState). No visual.

Cmdlet: `New-UICanvasState` - no visual

```powershell
New-UICanvasState @{ counter = 0 }
```

| Property | Type | Values |
|---|---|---|
| `State` * | Hashtable |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `StateWatcher`

Runs an action when a state key changes (Watch-UICanvasState). No visual.


```powershell
Watch-UICanvasState -Key counter -Action { }
```

_Reads no type-specific properties; the universal keys above still apply._

### `Event`

A named action fired by toolbar/footer/menu links. Synthesised by those cmdlets; not authored directly.

Cmdlet: `Add-UICanvasToolbar` - no visual

| Property | Type | Values |
|---|---|---|
| `Brand` * | String |  |
| `BrandIcon` * | String |  |
| `Status` * | String |  |
| `Links` * | String[] |  |
| `Active` * | String |  |
| `Actions` * | Object[] |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

### `WindowTemplate`

Carries a secondary window's content (New-UICanvasWindow). Opened with Show-UICanvasWindow.

Cmdlet: `New-UICanvasWindow` - no visual

```powershell
New-UICanvasWindow -Name dlg1 -Title 'Dialog' -Content { Add-UICanvasLabel 'Hello' }
```

| Property | Type | Values |
|---|---|---|
| `Title` * | String |  |
| `Modal` * | SwitchParameter |  |
| `Resizable` * | Boolean |  |
| `Topmost` * | SwitchParameter |  |
| `MinWidth` * | Double |  |
| `MinHeight` * | Double |  |
| `Icon` * | String |  |
| `Position` * | String | CenterOwner \| CenterScreen \| Manual |
| `NoScroll` * | SwitchParameter |  |
| `HideTitleBar` * | SwitchParameter |  |
| `TitleBarColor` * | String |  |
| `TitleBarText` * | String |  |
| `Layout` * | String | Canvas \| Stack \| VStack \| HStack \| Grid \| Wrap |
| `Columns` * | Int32 |  |
| `Spacing` * | Double |  |
| `Padding` * | Double |  |
| `Content` * | ScriptBlock |  |

<sub>* = a cmdlet parameter the module folds into the bag; the factory reads it under another path.</sub>

