# 05 — Theming

`Set-UITheme` applies colors/typography to the current canvas. Call it **after `New-PoshUICanvas`** and before
(or interleaved with) adding controls.

```
Set-UITheme [-Theme <hashtable>] [-Light <hashtable>] [-Dark <hashtable>]
  [-Preset Dark|Light|Midnight|Slate]
  [-Accent Indigo|Emerald|Sky|Amber|Rose|Violet|Cyan|Orange]
  [-Mode Light|Dark|Auto]
```

## One-call presets (recommended)
```powershell
Set-UITheme -Preset Slate -Accent Sky        # a complete look in one line
```
- **`-Preset`** applies a curated full palette and sets the base mode:
  - `Dark` — deep indigo/navy (the default look).
  - `Midnight` — near-black blue.
  - `Slate` — slate gray-blue.
  - `Light` — light surfaces, dark text.
- **`-Accent`** recolors just the accent (buttons, highlights): `Indigo|Emerald|Sky|Amber|Rose|Violet|Cyan|Orange`.
- Compose order: **preset → accent → explicit `-Theme` slots** (explicit slots always win):
  ```powershell
  Set-UITheme -Preset Slate -Accent Sky -Theme @{ CardBackground = '#10192E' }
  ```

## Slot overrides (full control)
Pass a hashtable of `slot = '#hex'`. Recognized slots:

| Slot | Themes |
|------|--------|
| `AccentColor` | primary brand / buttons / highlights (also `AccentLight`, `AccentDark`) |
| `Background` | app/window background |
| `ContentBackground` | content area background |
| `CardBackground` | cards, chart cards |
| `TitleBarBackground` / `TitleBarText` | custom title bar |
| `BorderColor` | borders/dividers |
| `TextPrimary` | headings/body text |
| `TextSecondary` | muted/secondary text |
| `ButtonBackground` / `ButtonForeground` | standard buttons |
| `InputBackground` / `InputBorder` | text inputs |
| `SidebarBackground` / `SidebarText` / `SidebarHighlight` | nav rail |
| `FontFamily` | base font (non-color slot) |

```powershell
Set-UITheme @{
    AccentColor = '#6366F1'; Background = '#0A0E1A'; ContentBackground = '#0A0E1A'
    CardBackground = '#141A2E'; TitleBarBackground = '#0A0E1A'; BorderColor = '#243049'
    TextPrimary = '#E5E9F5'; TextSecondary = '#94A3B8'
}
```

## Per-mode overrides
Provide light/dark variants; the active mode picks one:
```powershell
Set-UITheme -Mode Auto -Light @{ Background = '#F4F6FB'; TextPrimary = '#1E2433' } `
                       -Dark  @{ Background = '#0A0E1A'; TextPrimary = '#E5E9F5' }
```

## Switching light/dark while the app runs
The window switches **in place** - nothing restarts, nothing typed is lost, and it works mid-run. Every control
already on screen recolours, including colours from the theme slots above.

```powershell
Add-UICanvasButton -Name btnTheme '◐' -Style Subtle -Tooltip 'Switch between light and dark' -Action {
    $now = Set-UICanvasTheme Toggle      # Light | Dark | Toggle; returns the mode now in force
}
$mode = Get-UICanvasTheme                # 'Light' or 'Dark'
```
The window's own title bar has the same toggle; a chromeless app (`-HideTitleBar`) supplies its own as above.
To remember the choice between launches, save `$now` somewhere the next launch reads and pass it to
`New-PoshUICanvas -Theme`.

## Colour maps - when you set colours yourself
Slots cover what PoshUI styles. Colours **you** put on controls (`-Background '#0D1420'`,
`Set-UICanvasProperty x Foreground '#F87171'`, `GradientFrom`, `Glow`) are written for one mode. A colour map
says what each becomes in the other mode, and PoshUI swaps them on every switch - in both directions, for
controls already on screen and for colours an action sets later:

```powershell
Set-UITheme -Dark  @{ Background = '#0A0E14'; CardBackground = '#0D1420'; TextPrimary = '#E2E8F0' } `
            -Light @{ Background = '#B6BBC6'; CardBackground = '#CDD0D9'; TextPrimary = '#0B1430' } `
            -ColorMap @{ '#0D1420' = '#CDD0D9'; '#E2E8F0' = '#0B1430'; '#F87171' = '#7A1515' } `
            -ColorMapMode Light       # the mode the map translates INTO (default Light: page authored dark)
```
- Author the page **once**, in one palette; list every colour it uses in the map (a colour missing from the map
  simply keeps its authored value in the other mode).
- **Glows** are hidden in the mapped mode by default - a glow needs a dark surface, and on a pale one it smudges
  the text. `-KeepGlow` keeps them, recoloured through the map, for a mapped palette that is still dark.
- Raw XAML (`Add-UICanvasXaml`) is not reached by the map; use theme resources in it
  (`{DynamicResource CardBackgroundBrush}`) so it follows the switch.
- Pick light-mode text colours for at least 4.5:1 contrast against the light page *and* cards - easy to miss
  when a dark design's pale text is mapped one-for-one.

## Notes
- **Tooltips** are themed app-wide automatically (dark card, not the white OS popup).
- All colors are hex (`#RGB`, `#RRGGBB`, or `#AARRGGBB`). Non-hex values for color slots are rejected with a warning.
- Set the title-bar slots if you customize the window chrome, or it may render with the default color.
- The demos typically pair a preset with a couple of explicit slots, e.g.:
  ```powershell
  New-PoshUICanvas -Title 'App' -Theme Dark | Out-Null
  Set-UITheme -Preset Slate -Accent Sky | Out-Null
  ```

