# 01 — Architecture & the Authoring Model

## What it is
- **Authoring** happens in **Windows PowerShell 5.1**: you call `Add-UICanvas*` cmdlets which build an
  in-memory **definition** (a tree of pages → controls). `Show-PoshUICanvas` serializes that definition to a
  temp `.json` and launches the **WPF engine** (`PoshUI.exe`) to render it.
- The engine hosts a single **in-process Windows PowerShell runspace** (the "bridge"). Your `-Action` /
  `-OnChange` scriptblocks run there, full-privilege, in the same process as the UI. UI mutations they request
  are marshaled onto the WPF dispatcher thread.
- Module: `PoshUI.Canvas` (ModuleVersion 1.2.0). It checks the engine is **≥ 1.4.0** at import.

## The authoring lifecycle (always this order)
```powershell
Import-Module .\PoshUI\PoshUI.Canvas\PoshUI.Canvas.psd1 -Force   # 1. load the module
New-PoshUICanvas -Title 'App' -Theme Dark | Out-Null              # 2. create the canvas (+ first page)
Set-UITheme -Preset Slate -Accent Sky | Out-Null                 # 3. (optional) theme
Add-UICanvasPage -Title 'Home' -Layout VStack | Out-Null         # 4. configure the page(s)
Add-UICanvasLabel 'Hi'                                            # 5. add controls
# ... more controls ...
Show-PoshUICanvas | Out-Null                                     # 6. render & block until closed
```
`Show-PoshUICanvas` returns the collected control values once the window closes (see §Submitting).

## `New-PoshUICanvas`
```
New-PoshUICanvas -Title <string> [-Description <string>]
  [-Theme Light|Dark|Auto]                 # base theme mode (default Auto)
  [-Navigation None|Sidebar|Compact|Top]   # multi-page chrome (default None)
  [-SidebarHeaderText <s>] [-SidebarHeaderIcon <s>]
  [-WindowTitleIcon <s>] [-WindowTitleText <s>] [-AllowCancel <bool>]
  [-Width <d>] [-Height <d>] [-MinWidth <d>] [-MinHeight <d>]
```
- Auto-creates an initial page; your first `Add-UICanvasPage` **reuses** it.
- `-Navigation None` = single-page / free dashboard. `Sidebar`/`Top`/`Compact` add nav chrome for multi-page.

## Pages — `Add-UICanvasPage`
```
Add-UICanvasPage -Title <string> [-Description <s>] [-Icon <s>]
  [-Layout Canvas|Dock|Grid|Stack|VStack|HStack|Wrap]
  [-Columns <int>] [-ColumnWidths <s>] [-Spacing <double>] [-Padding <string>]
```
A page is a screen. Each page has a **layout root** that decides how its direct children are arranged:

| Layout | Behavior |
|--------|----------|
| `Canvas` (default) | **Absolute** positioning — every control needs `-X`/`-Y` (and usually `-Width`/`-Height`). |
| `VStack` | Vertical stack, top→bottom. Most common for forms/dashboards. |
| `HStack` | Horizontal stack, left→right. |
| `Stack` | Alias of VStack. |
| `Grid` | Uniform/`-ColumnWidths` grid; set `-Columns N`. Children flow across columns. |
| `Wrap` | Left→right, wrapping to the next line when full. |
| `Dock` | Dock children to edges (used for app shells: a nav rail docked left, content filling). |

- **`-Spacing`** = gap between children (stack/grid/wrap). **`-Columns`** = column count for Grid.
- ⚠️ Avoid `-Padding` on the page with stack layouts (can blank the page). Use a child `Margin` instead.

## Containers (nest controls)
Containers group and lay out child controls via a **`-Children { }`** scriptblock. Inside the block you call
more `Add-UICanvas*` cmdlets; they attach to the container instead of the page.

```powershell
Add-UICanvasCard -Layout VStack -Spacing 12 -Padding 18 -Children {
    Add-UICanvasLabel 'Card title' -FontWeight Bold
    Add-UICanvasPanel -Layout HStack -Spacing 8 -Children {
        Add-UICanvasButton 'OK'  -Style Accent
        Add-UICanvasButton 'No'  -Style Secondary
    }
}
```
- **`Add-UICanvasCard`** — a surfaced card (background + rounded corners + padding). Same layout params as a page.
- **`Add-UICanvasPanel`** — a transparent layout container (use `-Card` to give it a card surface). Same layout params.
- **`Add-UICanvasExpander -Label '...' [-IsExpanded $true]`** — a collapsible section with a `-Children` body.

Both Card and Panel accept: `-Layout`, `-Columns`, `-ColumnWidths`, `-Spacing`, `-Padding`, `-Background`,
`-CornerRadius`, plus the universal `-Name`/`-X`/`-Y`/`-Width`/`-Height`/`-ZIndex`/`-Properties`.

> ⚠️ **`$Name` shadowing:** container cmdlets declare a `-Name` parameter, so a *function-local* `$Name` you
> defined outside is shadowed to empty **inside** the `-Children` block. Alias before the block:
> `$wfName = $Name` then use `$wfName`. The same applies to `$Width`, `$Value`, etc.

## Universal parameters (on nearly every control)
| Param | Meaning |
|-------|---------|
| `-Name <string>` | Handle for runtime get/set. Give one to anything you'll read or mutate. |
| `-X -Y` | Absolute position (only used under a `Canvas` layout root). |
| `-Width -Height` | Explicit size (optional under stack/grid; required-ish under Canvas). |
| `-ZIndex <int>` | Stacking order under Canvas layout. |
| `-Tooltip <string>` | Hover tooltip (themed dark popup). |
| `-Visible <bool>` `-Enabled <bool>` | Initial visibility / enabled state. |
| `-Refresh <int>` | Seconds; re-evaluates a scriptblock `-Label`/`-Value` on a timer (live values). |
| `-Properties <hashtable>` | Extra rendering options (see below). |

## The `-Properties` bag
A hashtable of extra, lower-level rendering options passed straight to the renderer. Common keys:

| Key | Type / values | Effect |
|-----|---------------|--------|
| `Margin` | `'L,T,R,B'` or `'all'` | Outer margin, e.g. `'0,8,0,0'`. |
| `VAlign` / `VerticalAlignment` | `Top\|Center\|Bottom\|Stretch` | Vertical alignment in its slot. |
| `HAlign` / `HorizontalAlignment` | `Left\|Center\|Right\|Stretch` | Horizontal alignment. |
| `CornerRadius` | number | Rounded corners (cards/panels/buttons). |
| `Background` | hex | Background color. |
| `HoverBackground` | hex | Background on mouse-over (cards). |
| `BackgroundImage` | path | Raster background for a container. |
| `BackgroundStretch` | `Uniform\|UniformToFill\|Fill\|None` | How the background image scales. |
| `BackgroundOpacity` | 0..1 | Background image opacity. |
| `Stagger` | ms (number) | On a container: children fade/slide in, staggered by N ms each. |
| `Scroll` | `$true` | Wrap a container's content in an auto-hiding scroll viewer. |
| `ContentAlign` | `Left\|Center\|Right` | Button content alignment. |
| `Layout` | layout name | Force a layout on a card created via a card cmdlet that lacks `-Layout`. |

Example:
```powershell
Add-UICanvasCard -Layout VStack -Spacing 10 -Properties @{
    Margin = '0,12,0,0'; CornerRadius = 10; HoverBackground = '#1A2236'
    BackgroundImage = 'C:\assets\bg.png'; BackgroundStretch = 'UniformToFill'; BackgroundOpacity = 0.5
    Stagger = 60
} -Children { ... }
```

## Images & icons
- **PNG/JPG** are supported everywhere an image or icon is taken: `Add-UICanvasImage -Source <path>`,
  button `-Icon <path>`, `Add-UICanvasIcon <path>`, badge/banner/checkbox `-Icon <path>`, dropdown/listbox
  `-ItemIcons @(<path>,...)`, container `BackgroundImage`.
- An **icon** value can be a **PNG path** OR a **named glyph** (e.g. `'play'`, `'shield'`) OR a Segoe MDL2
  glyph character. Local image paths render at natural size (capped); markdown images fall back to alt text if
  the path fails.

## Anchoring & responsiveness
- Under **Canvas** layout, controls are absolutely placed (`-X/-Y/-Width/-Height`) and you manage layout
  yourself. Best for pixel-perfect or fixed designs.
- Under **VStack/HStack/Grid/Wrap/Dock**, the engine reflows children responsively when the window resizes —
  prefer these for anything that should adapt. Mix containers to build complex layouts (e.g. a `Dock` page with
  a left nav `Panel` and a `VStack` content `Panel` filling the rest).

## Submitting & return values
- Controls with a `-Name` contribute their current value to the result set.
- `Submit-UICanvas` (call from an action) closes the window and finalizes the result.
- `Show-PoshUICanvas` returns that result hashtable to the authoring script after the window closes:
  ```powershell
  $result = Show-PoshUICanvas
  $result.userName   # value of the control named 'userName'
  ```
- `Show-PoshUICanvas -NoWait` launches without blocking; `-AppDebug` enables verbose engine logging.
