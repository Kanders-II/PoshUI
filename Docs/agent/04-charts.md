# 04 — Charts

One cmdlet, `Add-UICanvasChartCard`, renders all chart types.

```
Add-UICanvasChartCard [-Title] <string> [-Name <s>]
  [-Type Bar|Line|Area|Donut|Sparkline]      # default Bar
  [-Labels <string[]>]                        # x-axis / segment labels
  [-Values <double[]>]                        # single-series data
  [-Datasets <object[]>]                      # multi-series (overrides -Values)
  [-Series <string>]                          # legend label for a single series
  [-Spline]                                   # smooth (curved) lines
  [-Bind <stateKey>]                          # reactive data binding
  [-Foreground <hex>]                         # single-series color
  [-ChartHeight <double>]                     # plot height in px
  [-Refresh <int>] [-Properties @{ ... }]
```

## Types
| Type | Looks like |
|------|-----------|
| `Bar` (default) | Vertical bars that **grow in** (animated). Gridlines, a y-axis, per-bar tooltips, value labels, gradient fill, hover glow. Multi-series → **grouped** bars. |
| `Line` | Polyline through points (`-Spline` = smooth), animated **draw-on** left→right, point dots with tooltips. |
| `Area` | Like Line with a gradient fill under the curve. |
| `Donut` | Each value is an arc segment (palette-colored, with a gap) + a centered **total** + a value/percent **legend**. |
| `Sparkline` | Compact, axis-less trend line + a dot on the last point. Great inside small cards. |

All non-donut/sparkline charts get a y-axis scaled to a "nice" round maximum, horizontal gridlines, themed
tooltips, and a legend.

## Single series
```powershell
Add-UICanvasChartCard 'Throughput' -Type Bar -Series 'req/s' `
  -Labels @('00','04','08','12','16','20') -Values @(38,22,64,91,73,50) `
  -Foreground '#6366F1' -ChartHeight 150
```

## Multi-series (`-Datasets`)
Each dataset is `@{ Name=<legend>; Color=<hex>; Values=@(...) }`. Labels are shared across series.
```powershell
Add-UICanvasChartCard 'Requests vs errors' -Type Bar -Labels @('00','04','08','12','16','20') -ChartHeight 150 -Datasets @(
    @{ Name='req/s';  Color='#38BDF8'; Values=@(38,22,64,91,73,50) }
    @{ Name='errors'; Color='#F472B6'; Values=@(4,7,3,9,6,5) }
)
# Bar -> grouped bars; Line/Area -> overlaid series; both get a multi-entry legend.
```

## Donut & sparkline
```powershell
Add-UICanvasChartCard 'Disk usage' -Type Donut -Labels @('Used','Free','Cache') -Values @(220,140,60) -ChartHeight 160
Add-UICanvasChartCard -Type Sparkline -Values @(20,35,28,50,42,66,58,72) -Foreground '#34D399' -ChartHeight 46
```

## Reactive charts (`-Bind`)
Bind a chart to a state key and it redraws when the key changes. Two value shapes are accepted:
- a **number array** → replaces the single series,
- a **datasets array** (`@(@{name;color;values}, …)`) → replaces **all** series.

```powershell
New-UICanvasState @{ live = @(20,45,30,60,40,75) }
Add-UICanvasChartCard -Name liveChart -Type Area -Spline -Bind 'live' -Labels @('1','2','3','4','5','6') -Foreground '#38BDF8' -ChartHeight 130

# update from anywhere:
Set-UICanvasState live (1..6 | ForEach-Object { Get-Random -Min 10 -Max 100 })
```

### Live dashboard pattern (all charts streaming)
Seed each chart's data in state, bind each chart, and drive a hidden **`-Refresh` ticker** that pushes fresh
data on a timer:
```powershell
New-UICanvasState @{
    bars = @(@{ name='req/s'; color='#38BDF8'; values=@(38,22,64,91,73,50) })
    cpu  = @(20,35,28,50,42)
}
Add-UICanvasChartCard -Name bars -Type Bar  -Bind 'bars' -Labels @('00','04','08','12','16','20') -ChartHeight 130
Add-UICanvasChartCard -Name cpu  -Type Sparkline -Bind 'cpu' -Foreground '#34D399' -ChartHeight 46

Add-UICanvasLabel -Name ticker -Refresh 2 -Label {     # streams every 2s
    Set-UICanvasState bars @(@{ name='req/s'; color='#38BDF8'; values=(1..6 | %{ Get-Random -Min 20 -Max 95 }) })
    $c = @(Get-UICanvasState cpu) + (Get-Random -Min 20 -Max 90); if ($c.Count -gt 14) { $c = $c[-14..-1] }
    Set-UICanvasState cpu $c
    "● live $(Get-Date -Format HH:mm:ss)"
}
```

## Notes
- Empty/`$null` data → a "No data" placeholder.
- `-ChartHeight` controls plot height; the card sizes around it. Put a chart in a `-Layout Grid -Columns N`
  panel to lay several side-by-side.
- Hover a bar/point/segment for a themed tooltip showing `label · series: value` (donut also shows percent).
