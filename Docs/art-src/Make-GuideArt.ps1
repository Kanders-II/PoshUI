param([string]$OutDir)
# The two drawn-only diagrams for the "Structuring an App" guide (the others are on screenshots: Make-GuideShots.ps1), drawn in Launchpad's own colours. Each is a WPF canvas rendered
# to PNG at 1.5x, so it stays crisp on high-DPI screens.
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
New-Item -ItemType Directory -Force $OutDir | Out-Null
$ns = 'xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"'
$C = @{ bg = '#070B12'; card = '#0D1420'; inset = '#0A111C'; line = '#1E293B'; text = '#E2E8F0'; label = '#94A3B8'; muted = '#64748B'
        brand = '#2289F7'; accent = '#7DC4FF'; orange = '#FB923C'; amber = '#FCD34D'; ok = '#4ADE80'; fail = '#F87171' }
function Esc([string]$s) { [Security.SecurityElement]::Escape($s) }
function Render([string]$name, [int]$w, [int]$h, [string]$body) {
    $x = "<Canvas $ns Width=`"$w`" Height=`"$h`" Background=`"$($C.bg)`">$body</Canvas>"
    $v = [Windows.Markup.XamlReader]::Parse($x)
    $v.Measure([Windows.Size]::new($w, $h)); $v.Arrange([Windows.Rect]::new(0, 0, $w, $h)); $v.UpdateLayout()
    $s = 1.5
    $bmp = [Windows.Media.Imaging.RenderTargetBitmap]::new([int]($w * $s), [int]($h * $s), 96 * $s, 96 * $s, [Windows.Media.PixelFormats]::Pbgra32); $bmp.Render($v)
    $enc = [Windows.Media.Imaging.PngBitmapEncoder]::new(); $enc.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bmp))
    $p = Join-Path $OutDir "$name.png"; $fs = [IO.File]::Create($p); $enc.Save($fs); $fs.Close(); "wrote $p"
}
function T($x, $y, $text, $size = 16, $fg = $C.text, $weight = 'Normal', $w = 0, $mono = $false) {
    $wa = if ($w) { " Width=`"$w`" TextWrapping=`"Wrap`"" } else { '' }
    $ff = if ($mono) { ' FontFamily="Cascadia Mono, Consolas"' } else { ' FontFamily="Segoe UI"' }
    if ($text.StartsWith('{')) { $text = '{}' + $text }   # XAML would read {..} as a markup extension
    "<TextBlock Canvas.Left=`"$x`" Canvas.Top=`"$y`" Text=`"$(Esc $text)`" FontSize=`"$size`" Foreground=`"$fg`" FontWeight=`"$weight`"$wa$ff/>"
}
function Box($x, $y, $w, $h, $fill = $C.card, $edge = $C.line, $r = 14, $thick = 1.5) {
    "<Border Canvas.Left=`"$x`" Canvas.Top=`"$y`" Width=`"$w`" Height=`"$h`" CornerRadius=`"$r`" Background=`"$fill`" BorderBrush=`"$edge`" BorderThickness=`"$thick`"/>"
}
function Icon($x, $y, $code, $size = 26, $fg = $C.brand) {
    "<TextBlock Canvas.Left=`"$x`" Canvas.Top=`"$y`" Text=`"&#x$code;`" FontFamily=`"Segoe Fluent Icons, Segoe MDL2 Assets`" FontSize=`"$size`" Foreground=`"$fg`"/>"
}
function Arrow($x1, $y1, $x2, $y2, $fg = $C.label, $thick = 2.5) {
    $dx = $x2 - $x1; $dy = $y2 - $y1; $len = [math]::Sqrt($dx * $dx + $dy * $dy); $ux = $dx / $len; $uy = $dy / $len
    $bx = $x2 - 13 * $ux; $by = $y2 - 13 * $uy; $px = -$uy * 7; $py = $ux * 7
    "<Line X1=`"$x1`" Y1=`"$y1`" X2=`"$bx`" Y2=`"$by`" Stroke=`"$fg`" StrokeThickness=`"$thick`"/>" +
    "<Polygon Points=`"$x2,$y2 $($bx+$px),$($by+$py) $($bx-$px),$($by-$py)`" Fill=`"$fg`"/>"
}
function Dot($x, $y, $n, $fg) {
    "<Border Canvas.Left=`"$($x)`" Canvas.Top=`"$($y)`" Width=`"26`" Height=`"26`" CornerRadius=`"13`" Background=`"$fg`"><TextBlock Text=`"$n`" FontSize=`"14`" FontWeight=`"Bold`" Foreground=`"$($C.bg)`" HorizontalAlignment=`"Center`" VerticalAlignment=`"Center`"/></Border>"
}

# 1. Two worlds: content and code -------------------------------------------------------------------------
$b = (T 40 28 'Two kinds of file, changed by different people' 26 $C.text 'Bold') +
     (T 40 66 'Keep them apart and the app never needs touching when the team adds a script.' 16 $C.label)
$b += (Box 40 120 520 330 $C.card $C.ok) + (Icon 70 148 'E8B7' 30 $C.ok) + (T 116 148 'Content' 26 $C.text 'SemiBold') +
      (T 116 184 'Changes every week. Anyone on the team.' 15 $C.label)
$y = 236; foreach ($row in @(@('E943', 'Scripts', 'one folder each, with a how-to'), @('E9D5', 'Workflows', 'scripts plus questions and approvals'), @('E8A5', 'How-tos and pictures', 'markdown beside each one'))) {
    $b += (Icon 76 ($y + 2) $row[0] 20 $C.ok) + (T 112 $y $row[1] 18 $C.text 'SemiBold') + (T 112 ($y + 26) $row[2] 14 $C.label); $y += 66 }
$b += (Box 680 120 520 330 $C.card $C.brand) + (Icon 710 148 'E713' 30 $C.brand) + (T 756 148 'The app' 26 $C.text 'SemiBold') +
      (T 756 184 'Changes rarely. Whoever looks after it.' 15 $C.label)
$y = 236; foreach ($row in @(@('E7F4', 'Pages', 'Scripts, Workflows, Activity, Settings'), @('E9F5', 'The engine', 'finds, checks and runs things'), @('E790', 'The look', 'every colour in one file'))) {
    $b += (Icon 716 ($y + 2) $row[0] 20 $C.brand) + (T 752 $y $row[1] 18 $C.text 'SemiBold') + (T 752 ($y + 26) $row[2] 14 $C.label); $y += 66 }
$b += (Arrow 600 285 676 285 $C.accent 3) + (Arrow 640 315 564 315 $C.accent 3) +
      (T 584 258 'reads' 14 $C.accent 'SemiBold') + (T 580 322 'shows' 14 $C.accent 'SemiBold')
$b += (Box 40 480 1160 60 $C.inset $C.line 12) + (Icon 64 496 'E946' 22 $C.amber) +
      (T 104 496 'Drop a script into Content and it appears in the app, with its form and its how-to. No code changes.' 17 $C.text)
Render 'two-worlds' 1240 570 $b

# 2. Five kinds of step ----------------------------------------------------------------------------------
$b = (T 40 28 'A workflow is a list of steps. There are five kinds.' 26 $C.text 'Bold') +
     (T 40 66 'One runs a script. The other four stop and involve a person - the run waits for them.' 16 $C.label)
$kinds = @(
    @('E756', 'Script', 'runs one of your scripts', 'Test-Network', $C.brand),
    @('E897', 'Ask', 'a question with set answers', 'Is Wi-Fi on?  Yes / No', $C.accent),
    @('E70F', 'Input', 'asks for a value, mid-run', 'Which DNS server?', $C.accent),
    @('E762', 'Review', 'shows rows; you pick some', 'Which servers go in?', $C.orange),
    @('EA18', 'Approve', 'Approve, Reject or Decide later', 'Publish 14 scripts?', $C.amber))
$x = 40; foreach ($k in $kinds) {
    $b += (Box $x 120 220 250 $C.card $k[4] 14 2) + (Icon ($x + 24) 146 $k[0] 34 $k[4]) +
          (T ($x + 24) 200 $k[1] 24 $C.text 'SemiBold') + (T ($x + 24) 236 $k[2] 15 $C.label 'Normal' 176) +
          (Box ($x + 20) 300 180 46 $C.inset $C.line 8) + (T ($x + 32) 312 $k[3] 13 $C.text 'Normal' 160)
    $x += 236 }
$b += (Box 40 396 1160 62 $C.inset $C.line 12) + (Icon 64 414 'E8AB' 22 $C.ok) +
      (T 104 413 'Each step''s result picks the next one: Success goes here, Failed goes there, Yes goes somewhere else.' 17 $C.text)
Render 'step-kinds' 1240 490 $b
