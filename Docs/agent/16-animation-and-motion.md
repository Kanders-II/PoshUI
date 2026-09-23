# 16 — Animation & Motion

Motion in Canvas comes from two places: a handful of **declarative knobs** on ordinary controls, and a
**XAML island** when you need a real timeline. This file covers both, the property paths that actually work,
a catalogue of techniques, and how to verify motion without watching it.

> **Read [07](07-patterns-gotchas-recipes.md) §"When to reach for a XAML island" first.** It decides
> *whether* to open an island. This file is about what to do once you have.

---

## 1. What the engine gives you without an island

Prefer these. They theme correctly, they survive layout changes, and they cost you nothing.

| Knob | Where | Effect |
|------|-------|--------|
| `Stagger = <ms>` | `-Properties` on a **container** | Children fade/slide in, cascaded by N ms each. One-shot, on page entry. |
| `HoverScale = 1.04`, `HoverScaleMs` | `-Properties` on a card/panel | Animated centred zoom on mouse-over. |
| `Glow`, `GlowPulse`, `GlowRadius` | `-Properties`, and `-Glow` on some controls | Coloured bloom; `GlowPulse` animates it. |
| `Spin` | `Set-UICanvasProperty <name> Spin $true` | Continuous rotation on any icon/image (busy indicator). |
| `Set-UICanvasAnimate` | runtime, inside an `-Action` | Animates `Opacity`/`Width`/`Height`/`X`/`Y` on a named control, with `-Easing`. |
| Page transitions | automatic | `TransitionContentControl` fades + slides on every page change. |

### Where they stop

These limits are the entire reason islands exist. Know them before you reach:

- `Set-UICanvasAnimate` covers **five properties only** — no rotation, no scale, no colour, no blur, no clip.
- **`Glow` is not runtime-settable.** `Set-UICanvasProperty` has no case for it and a `TextBlock` has no such
  CLR property for the reflection fallback to find. If a label needs two glow states, build **two labels** and
  flip `Visible`.
- **There is no author-facing hover event.** `HoverScale` is the whole hover vocabulary.
- **Nothing sequences.** `Stagger` cascades children of one container; it cannot express "A, then B 300ms
  later, then C while B is still moving". A `Storyboard` can.
- `HoverBackground` **captures its brush at build time**. Combine it with a runtime `Background` swap and
  mouse-out restores the stale colour. `HoverScale` has no such problem.

---

## 2. The island contract

```powershell
Add-UICanvasXaml [-Name <s>] -Markup <xaml-string> [-Height <d>]
                 [-Actions @{ innerName = { ... } }]
```

The engine calls `XamlReader.Parse` on your markup and splices the result in. Consequences:

- **Any `x:Name`'d element inside is registered with the bridge**, including the root — so
  `Set-UICanvasProperty` / `Get-UICanvasValue` reach into the island, and `-Actions` binds a scriptblock to a
  named `ButtonBase` inside it. An island is not a dead end.
- **Theme brushes resolve** via `{DynamicResource ...}`.
- You are in **raw WPF**: the engine's layout helpers, control set and theming defaults stop at the boundary.

### Always declare the namespaces

The engine injects `xmlns` when it finds none, but be explicit — it is self-documenting and immune to the
injector's regex:

```xml
<Grid x:Name="root" Background="Transparent"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
```

### Storyboards must self-start

**Canvas actions run OFF the UI thread.** PowerShell therefore cannot call `.Begin()` on a `Storyboard`:
doing so means handing the dispatcher a delegate, which PowerShell can only invoke while its runspace is
free — and the runspace is busy running the very scriptblock that asked. It deadlocks or throws
"no Runspace available".

A **declarative trigger has no thread affinity to satisfy**, so it is the only reliable way in:

```xml
<Grid.Triggers>
  <EventTrigger RoutedEvent="FrameworkElement.Loaded">
    <BeginStoryboard>
      <Storyboard>  <!-- add Duration + RepeatBehavior="Forever" to loop -->
        ...
      </Storyboard>
    </BeginStoryboard>
  </EventTrigger>
</Grid.Triggers>
```

> **Do not** try to start a storyboard from a `Style` trigger's `EnterActions` so PowerShell can flip a
> property to fire it. `Storyboard.TargetName` resolution inside a `Style` is scoped to the template
> namescope, and outside a `ControlTemplate` it does not reliably find your named elements. Use `Loaded`.

### Put eases in resources

A storyboard with 70 animations should not repeat the easing function 70 times:

```xml
<Grid.Resources>
  <CubicEase x:Key="eo"  EasingMode="EaseOut"/>
  <CubicEase x:Key="eio" EasingMode="EaseInOut"/>
  <SineEase  x:Key="sio" EasingMode="EaseInOut"/>
  <BackEase  x:Key="bo"  EasingMode="EaseOut" Amplitude="0.6"/>
</Grid.Resources>
...
<DoubleAnimation ... EasingFunction="{StaticResource eo}"/>
```

---

## 3. Property paths that work

`Storyboard.TargetProperty` takes a **path from the named element**. Prefer that over giving a `Freezable`
(a `Transform`, a `GradientStop`, a `Brush`) its own `x:Name` and targeting it directly — freezable name
registration is the fragile part, and the path form sidesteps it entirely.

| Target | `TargetProperty` | Animation type |
|--------|------------------|----------------|
| Fade | `Opacity` | `DoubleAnimation` |
| Move / scale / rotate (single transform) | `RenderTransform.X`, `.Y`, `.ScaleX`, `.ScaleY`, `.Angle` | `DoubleAnimation` |
| Move **and** scale (a `TransformGroup`) | `RenderTransform.Children[0].ScaleX`, `RenderTransform.Children[1].X` | `DoubleAnimation` |
| Blur | `Effect.Radius` (on a `BlurEffect`) | `DoubleAnimation` |
| Glow size / strength | `Effect.BlurRadius`, `Effect.Opacity` (on a `DropShadowEffect`) | `DoubleAnimation` |
| Gradient mask travel | `OpacityMask.StartPoint`, `OpacityMask.EndPoint` | **`PointAnimation`** |
| Circular clip | `Clip.RadiusX`, `Clip.RadiusY` (on an `EllipseGeometry`) | `DoubleAnimation` |
| Stroke draw-on / progress / flow | `StrokeDashOffset` | `DoubleAnimation` |

**`RenderTransformOrigin="0.5,0.5"`** on the element, or scales and rotations happen about its top-left
corner. This is the single most common reason a "zoom" looks like it is sliding away.

**Transform composition order matters.** In a `TransformGroup`, children apply in order, so a
`TranslateTransform` placed *after* a `ScaleTransform` translates in final/parent units — you do not have to
divide your offsets by the scale factor. Put the scale first and the maths stays readable.

### Two-value animations are not enough for a pulse

`From`/`To` describes one ramp. "Come up fast, then bleed away" is three values, which needs keyframes:

```xml
<DoubleAnimationUsingKeyFrames Storyboard.TargetName="ring" Storyboard.TargetProperty="Opacity" BeginTime="0:0:0.1">
  <LinearDoubleKeyFrame Value="0"    KeyTime="0:0:0"/>
  <LinearDoubleKeyFrame Value="0.5"  KeyTime="0:0:0.25"/>
  <LinearDoubleKeyFrame Value="0"    KeyTime="0:0:1.4"/>
</DoubleAnimationUsingKeyFrames>
```

`SplineDoubleKeyFrame` with a `KeySpline` gives per-segment easing. `AutoReverse="True"` +
`RepeatBehavior="Forever"` on a single animation is the cheapest breathing/idle loop there is, and it can
run on its **own** repeat inside a storyboard that otherwise plays once.

---

## 4. Generating markup from PowerShell: the score pattern

Anything repeated per-glyph, per-slice or per-ring should be generated, not hand-written. The discipline
that keeps this maintainable:

```powershell
# 1. ONE table of milliseconds. This is the whole score; nothing else carries timing.
$T = @{
    LogoBegin  = 0;   LogoDur   = 480
    SheenBegin = 200; SheenDur  = 820
    TitleBegin = 700; TitleStep = 16; TitleDur = 240
}
if ($Slow) { foreach ($k in @($T.Keys)) { $T[$k] = [math]::Round($T[$k] * 2.5) } }

# 2. ms -> the "h:m:s.fff" form WPF's TimeSpan converter wants.
function Ts([double]$ms) { '0:0:{0:0.000}' -f ($ms / 1000.0) }

# 3. Escape anything interpolated into markup, even values you believe you own.
function Esc([string]$s) {
    if ($null -eq $s) { return '' }
    $s.Replace('&','&amp;').Replace('<','&lt;').Replace('>','&gt;').Replace('"','&quot;').Replace("'",'&apos;')
}

# 4. StringBuilder for the repeated parts; splice with $(...) into one here-string.
$els = New-Object System.Text.StringBuilder
$anims = New-Object System.Text.StringBuilder
```

Give the script **`-Slow`** (multiply every duration) and **`-Loop`** (`Duration` + `RepeatBehavior="Forever"`
on the storyboard) switches. Iterating on timing by restarting a one-shot animation is miserable; these two
flags cost four lines and pay for themselves immediately.

> **A per-item step multiplies by collection length.** A 25ms cascade over a 33-character title runs 825ms
> before the last glyph even starts. Pick the step from `step × count`, not from how it feels in isolation,
> or later acts will land while an earlier one is still playing.

---

## 5. Technique catalogue

All of these are verified against the engine. Colours are placeholders.

### Sheen sweep (raster-safe "light across the logo")
A **second copy** of the image stacked on the first, carrying a glow, clipped by an `OpacityMask` whose
gradient slides across. `SpreadMethod="Pad"` pads outside the gradient span with its **transparent** end
stops, so the glow copy is invisible except inside the travelling band.

```xml
<Grid>
  <Image Source="{...}" Height="52" Stretch="Uniform"/>
  <Image x:Name="sheen" Source="{...}" Height="52" Stretch="Uniform" IsHitTestVisible="False">
    <Image.Effect><DropShadowEffect Color="#22D3EE" BlurRadius="24" ShadowDepth="0"/></Image.Effect>
    <Image.OpacityMask>
      <LinearGradientBrush StartPoint="-0.45,0" EndPoint="-0.1,0" SpreadMethod="Pad">
        <GradientStop Color="#00FFFFFF" Offset="0"/>
        <GradientStop Color="#FFFFFFFF" Offset="0.5"/>
        <GradientStop Color="#00FFFFFF" Offset="1"/>
      </LinearGradientBrush>
    </Image.OpacityMask>
  </Image>
</Grid>
```
Animate **both** ends so the band keeps its width:
```xml
<PointAnimation Storyboard.TargetName="sheen" Storyboard.TargetProperty="OpacityMask.StartPoint"
                From="-0.45,0" To="1.0,0" Duration="0:0:0.82" EasingFunction="{StaticResource sio}"/>
<PointAnimation Storyboard.TargetName="sheen" Storyboard.TargetProperty="OpacityMask.EndPoint"
                From="-0.1,0" To="1.45,0" Duration="0:0:0.82" EasingFunction="{StaticResource sio}"/>
```

### Scanline build (hard wipe)
Same mask mechanism, **two** stops and a near-zero span, oriented vertically. Pad makes everything above the
span opaque and everything below transparent, so this is a hard reveal, not a fade. Ride a glowing 1.5px
`Rectangle` on the boundary via `RenderTransform.Y`.

### Focus pull (entrance)
`BlurEffect.Radius` 16→0 and scale 1.14→1 collapsing together over ~700ms, `EaseOut`. Reads as a lens
finding focus rather than a plain fade. Put the `BlurEffect` on a **parent** `Grid` if the child already
carries a `DropShadowEffect` — **an element has exactly one `Effect`**; nest to stack them.

### Per-glyph cascade
One `TextBlock` per character in a horizontal `StackPanel`, each with `Opacity="0"` and a
`TranslateTransform Y="7"`, animated with a `BeginTime` offset of `index × step`. Give **spaces** an unnamed
`Text="&#160;"` block with no animation, so a gap never arrives late. Set the font on the panel with
**`TextElement.FontFamily`** — `StackPanel.FontFamily` does not exist and will fail the parse.

### Slice choreography (per-piece motion from one raster)
`CroppedBitmap` cuts a bitmap into pieces with no re-authoring of the asset; each piece is then an ordinary
`Image` with its own transform, so parts can arrive independently.

```xml
<Image x:Name="slA" Height="46" Stretch="Uniform" Opacity="0">
  <Image.RenderTransform><TranslateTransform X="-60" Y="0"/></Image.RenderTransform>
  <Image.Source><CroppedBitmap Source="C:\path\mark.png" SourceRect="0,0,318,126"/></Image.Source>
</Image>
```
`SourceRect` is `x,y,w,h` in **source pixels**. This is the closest a raster gets to per-letter animation.

### Chromatic split
Three copies of the same image, each with a differently coloured `DropShadowEffect`, offset a few px and
converging to zero. The glow supplies the colour — no pixel shader, no tinted assets.

### Iris reveal
`UIElement.Clip` takes a `Geometry`, and `EllipseGeometry`'s radii animate straight off the element:

```xml
<Grid x:Name="host" Width="230" Height="46">
  <Grid.Clip><EllipseGeometry Center="115,23" RadiusX="0" RadiusY="0"/></Grid.Clip>
  <Image Source="{...}" Stretch="Uniform"/>
</Grid>
```
`Center` is in the clipped element's own coordinates, so the host needs a fixed `Width`/`Height`.

### Reflection
A second copy with `ScaleY="-1"` and a vertical gradient `OpacityMask`. Static in most designs; sliding it
up into place is what sells it as a surface rather than a decal.

### The `StrokeDashOffset` trio
One primitive, three jobs — the highest-value technique in this file.

> **`StrokeDashArray` is measured in multiples of `StrokeThickness`, not pixels.** A 360px path at
> thickness 2 needs a dash of **180** units to cover itself. Getting this wrong is the usual reason a
> draw-on does nothing visible.

1. **Draw-on.** Dash = whole path length, animate offset → 0. The effect a raster logo cannot have until
   someone traces it; works today on any `Path`, `Ellipse` or `Rectangle` geometry you author.
2. **Progress.** Size the dash to exactly one lap of a circle (`2πr / thickness`) and `StrokeDashOffset`
   becomes a real progress value. Add `<RotateTransform Angle="-90"/>` to start at twelve o'clock.
3. **Marching ants.** Never stop animating it. Animate over exactly one dash+gap cycle (`4 3` → `7`) with
   `RepeatBehavior="Forever"` so the loop is seamless. One `DoubleAnimation` for the clearest
   "this is live / being watched" affordance there is.

### Ring pings and breathing glow
Concentric `Ellipse`es on a shared centre, `RenderTransformOrigin="0.5,0.5"`, scaling out with keyframed
opacity, staggered by `BeginTime`. For idle/working state, animate `Effect.BlurRadius` with
`AutoReverse="True" RepeatBehavior="Forever"` — nothing moves, and it still reads as alive.

---

## 6. Timing and easing

| Effect | Typical duration | Easing | Why |
|--------|-----------------|--------|-----|
| Fade in | 250–450ms | `CubicEase EaseOut` | Arrives quickly, settles softly. |
| Entrance scale / settle | 500–750ms | `CubicEase EaseOut` | |
| Travelling highlight (sheen, scanline) | 800–1300ms | **`SineEase EaseInOut`** | See below. |
| Draw-out / wipe | 450–550ms | `CubicEase EaseOut` | |
| Move between two positions | 600–750ms | `CubicEase EaseInOut` | Symmetric move, symmetric ease. |
| Pop / badge arrival | 300–400ms | `BackEase EaseOut`, `Amplitude` 0.6–0.7 | Slight overshoot. |

> **A travelling highlight must not use `EaseOut`.** `EaseOut` front-loads: the band covers ~96% of its
> sweep in the first two thirds and then visibly *stalls* at the far edge. `SineEase EaseInOut` distributes
> the travel and the stall disappears. This is the most common motion bug in this whole file.

**Order the acts so none lands inside another**, unless the overlap is deliberate. Compute the real end of
every act (remember `step × count` for cascades) and lay the next `BeginTime` after it.

**Budget.** A first-run sequence of ~1.5–2.0s is comfortable. Beyond that you are spending the user's time
on every launch. Anything looping forever on a page someone has to *work* on will become irritating —
loop decorative motion two or three times and stop.

---

## 7. Sequencing outside the island

A `Storyboard` orchestrates one island. To coordinate an island with the rest of the app — delay body
content, auto-advance a page — you need a hook. There is one, and it is not documented as such.

### `-Refresh` is a page-load hook

`CanvasBridge.StartRefresh` starts the `DispatcherTimer` **and then calls the tick once immediately**, so
the very first evaluation happens at control registration. A hidden refreshing label is therefore a load
hook. Use an interval you never intend to fire again:

```powershell
Add-UICanvasLabel -Name kick -Visible $false -Refresh 3600 -Label {
    # runs once, at page load, on the bridge runspace
    ''
}
```

### `Start-UICanvasAsync` buys millisecond precision

`-Refresh` is **integer seconds**. `Start-UICanvasAsync` runs on its own runspace with the cmdlet bootstrap
loaded, so it can sleep precisely and then call runtime cmdlets:

```powershell
Start-UICanvasAsync {
    Start-Sleep -Milliseconds 2130
    Show-UICanvasPage 'MainPage'
}
```

`Show-UICanvasPage` works under `-Navigation None` — `NavigateToCanvasPage` walks the page list by
title / index / `next` and ignores the nav chrome. **Navigate by title**: it is index-agnostic, so inserting
a page at position 0 later cannot break it.

### It fires on re-entry too, but it can be skipped when the runspace is busy

Navigating to a page builds a **new** `CanvasViewModel` and re-registers its controls, so the hook fires on
every entry, including navigating *back*. Measured: a two-page ping-pong driven purely by load hooks bounces
continuously, about every 1.26s, for as long as you leave it running.

The one caveat is that the immediate tick opens with `if (!_gate.Wait(0)) return;` — it is **skipped when the
runspace is busy**. In a quiet app that never bites, but if something long-running may hold the gate when the
page opens, the tick is silently lost. Where that matters, arm from the navigating control's own action
instead, which is not subject to the check:

```powershell
Add-UICanvasButton 'Replay intro' -Action {
    Start-UICanvasAsync { Start-Sleep -Milliseconds 3250; Show-UICanvasPage 'Main' }
    Show-UICanvasPage 'Splash'
}
```

Either way, give the user a visible control to move on with, so a lost tick can never strand them.

> `$global:` does **not** cross into `Start-UICanvasAsync`, which gets its own runspace. A guard set in the
> bridge runspace cannot cancel a timer already dispatched — make the timer's target idempotent instead.

> **Reading the log:** `===> CurrentPage set. Index: N` is written on only some navigation paths. The line
> that appears for every one is `--> OnPropertyChanged triggered for CurrentPage`. Grepping for the former
> alone will make a working navigation loop look like it stopped.

### Guard such a hook with a global, not canvas state

The bridge reuses **one runspace** for every `ValueScript`, so `$global:` persists across ticks and page
re-entry. Canvas state is seeded by whichever page calls `New-UICanvasState` — which may be declared *after*
the page that reads it, leaving the guard reading `$null`.

```powershell
$kick = [scriptblock]::Create(@"
if (-not `$global:MyAppArmed) {
    `$global:MyAppArmed = `$true
    Start-UICanvasAsync { Start-Sleep -Milliseconds 2130; Show-UICanvasPage 'MainPage' }
}
''
"@)
```

> If the guard is never reset, re-entering the page replays the animation but **never re-arms** the timer,
> stranding the user. Any control that navigates *back* to such a page must clear the flag first.

### Known gap: there is no delay for `Stagger`

`Stagger` fires at page load. There is no supported way to hold engine controls back until an island's
storyboard finishes. Either accept the overlap, or move that content into the island.

---

## 8. Verifying motion without watching it

You can — and should — prove an animation works before showing anyone. Parsing is not enough:
**`Storyboard.TargetName` and property-path failures surface only at run time.**

1. **Parse.** `[Windows.Markup.XamlReader]::Parse($xaml)` in a `-STA` PowerShell. Catches typos, bad
   property names (`StackPanel.FontFamily`), namespace mistakes.
2. **Run it headless and assert the end state.** Host the parsed element in an off-screen `Window`, pump
   dispatcher frames past the sequence end, then read the animated properties back. If a target failed to
   resolve, the value is still at its start.
3. **Capture frames.** `RenderTargetBitmap.Render()` at chosen timestamps gives real PNGs of the sequence —
   the only way to check *ordering* and overlap.

```powershell
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase
$el = [Windows.Markup.XamlReader]::Parse($xaml)
$w = New-Object Windows.Window
$w.WindowStyle='None'; $w.ShowInTaskbar=$false; $w.Left=-6000; $w.Top=-6000
$w.Width=1280; $w.Height=300; $w.Content=$el
$w.Show()
$sw = [Diagnostics.Stopwatch]::StartNew()
function Step {
    $f = New-Object Windows.Threading.DispatcherFrame
    [Windows.Threading.Dispatcher]::CurrentDispatcher.BeginInvoke(
        [Windows.Threading.DispatcherPriority]::Background, [action]{ $f.Continue = $false }) | Out-Null
    [Windows.Threading.Dispatcher]::PushFrame($f)
}
while ($sw.ElapsedMilliseconds -lt 2000) { Step }
"opacity = {0}" -f $el.FindName('logo').Opacity      # want 1
$w.Close()
```

> **Drive the clock with a `Stopwatch`, not `Start-Sleep`.** Sleep granularity plus render cost makes
> captured frames land far later than requested, and you will "fix" timings that were never wrong.

> **Zoom before you fix.** A downscaled thumbnail merges adjacent per-glyph glows into what looks like a
> solid slab. Crop and magnify the region before concluding an effect is broken.

---

## 9. Gotchas

- **One `Effect` per element.** Nest elements to combine blur and glow.
- **`StrokeDashArray` is in multiples of `StrokeThickness`.**
- **`TextElement.FontFamily`** on a panel; `FontFamily` is not a panel property.
- **`RenderTransformOrigin="0.5,0.5"`** for anything that scales or rotates.
- **Per-glyph text loses kerning.** Fine in a monospace face, visibly loose in a proportional one.
- **`Duration` is required on a looping `Storyboard`** for `RepeatBehavior="Forever"` to have a cycle length.
  Add a tail pause to the duration or the loop restarts the instant the last animation ends.
- **Image paths containing commas parse fine** in a XAML `Source` attribute. No escaping needed.
- **An island stretches to its container.** Give `Add-UICanvasXaml` an explicit `-Height` when it must
  occupy vertical space in a `VStack`, which otherwise sizes to content.

---

## 10. Accessibility and cost

- The engine has a **`DisableAnimations`** flag in the definition that gates page transitions and dialog
  animations. **It is exposed by `Set-UIBranding` in the Dashboard module and has no `PoshUI.Canvas`
  equivalent**, and — more importantly — **it does not reach inside a XAML island**, because a storyboard
  there runs purely in WPF with no engine involvement. If reduced motion matters to your audience, gate the
  island yourself: build the markup with the animations omitted, or emit a static tree instead.
- The engine **software-renders** so it works on VMs and remote sessions. `BlurEffect` and
  `DropShadowEffect` are the expensive primitives there, and they cost per-element. A dozen glows animating
  at once on a remote session is a real cost; a handful is not. Prefer one glow on a parent to one glow on
  each of thirty children.
- **Motion should mean something.** A pulsing border that says "running" earns its cost. Decoration on a
  page an operator uses fifty times a day does not.
