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

## Notes
- **Tooltips** are themed app-wide automatically (dark card, not the white OS popup).
- All colors are hex (`#RGB`, `#RRGGBB`, or `#AARRGGBB`). Non-hex values for color slots are rejected with a warning.
- Set the title-bar slots if you customize the window chrome, or it may render with the default color.
- The demos typically pair a preset with a couple of explicit slots, e.g.:
  ```powershell
  New-PoshUICanvas -Title 'App' -Theme Dark | Out-Null
  Set-UITheme -Preset Slate -Accent Sky | Out-Null
  ```
