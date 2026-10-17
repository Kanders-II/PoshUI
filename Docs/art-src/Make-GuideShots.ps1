param([string]$OutDir, [string]$Shots)
# Guide figures built on real Launchpad screenshots: numbered boxes on the screen, the same numbers on the code or
# the notes. Plus the passing-values diagram, redrawn so each reference sits on the arrow that carries it.
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
New-Item -ItemType Directory -Force $OutDir | Out-Null
$ns = 'xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"'
$C = @{ bg = '#070B12'; card = '#0D1420'; inset = '#0A111C'; line = '#1E293B'; text = '#E2E8F0'; label = '#94A3B8'; muted = '#64748B'
        brand = '#2289F7'; accent = '#7DC4FF'; orange = '#FB923C'; amber = '#FCD34D'; ok = '#4ADE80'; fail = '#F87171' }
function Esc([string]$s) { [Security.SecurityElement]::Escape($s) }
function T($x, $y, $text, $size = 16, $fg = $C.text, $weight = 'Normal', $w = 0, $mono = $false) {
    $wa = if ($w) { " Width=`"$w`" TextWrapping=`"Wrap`"" } else { '' }
    $ff = if ($mono) { ' FontFamily="Cascadia Mono, Consolas"' } else { ' FontFamily="Segoe UI"' }
    if ($text.StartsWith('{')) { $text = '{}' + $text }
    "<TextBlock Canvas.Left=`"$x`" Canvas.Top=`"$y`" Text=`"$(Esc $text)`" FontSize=`"$size`" Foreground=`"$fg`" FontWeight=`"$weight`"$wa$ff/>"
}
function Box($x, $y, $w, $h, $fill = $C.card, $edge = $C.line, $r = 14, $thick = 1.5) {
    "<Border Canvas.Left=`"$x`" Canvas.Top=`"$y`" Width=`"$w`" Height=`"$h`" CornerRadius=`"$r`" Background=`"$fill`" BorderBrush=`"$edge`" BorderThickness=`"$thick`"/>"
}
function Icon($x, $y, $code, $size = 26, $fg = $C.brand) {
    "<TextBlock Canvas.Left=`"$x`" Canvas.Top=`"$y`" Text=`"&#x$code;`" FontFamily=`"Segoe Fluent Icons, Segoe MDL2 Assets`" FontSize=`"$size`" Foreground=`"$fg`"/>"
}
function Arrow($x1, $y1, $x2, $y2, $fg = $C.label, $thick = 2.5, $dash = '') {
    $dx = $x2 - $x1; $dy = $y2 - $y1; $len = [math]::Sqrt($dx * $dx + $dy * $dy); $ux = $dx / $len; $uy = $dy / $len
    $bx = $x2 - 13 * $ux; $by = $y2 - 13 * $uy; $px = -$uy * 7; $py = $ux * 7
    $da = if ($dash) { " StrokeDashArray=`"$dash`"" } else { '' }
    "<Line X1=`"$x1`" Y1=`"$y1`" X2=`"$bx`" Y2=`"$by`" Stroke=`"$fg`" StrokeThickness=`"$thick`"$da/>" +
    "<Polygon Points=`"$x2,$y2 $($bx+$px),$($by+$py) $($bx-$px),$($by-$py)`" Fill=`"$fg`"/>"
}
function Dot($x, $y, $n, $fg = $C.orange) {
    "<Border Canvas.Left=`"$x`" Canvas.Top=`"$y`" Width=`"28`" Height=`"28`" CornerRadius=`"14`" Background=`"$fg`"><TextBlock Text=`"$n`" FontSize=`"15`" FontWeight=`"Bold`" Foreground=`"$($C.bg)`" HorizontalAlignment=`"Center`" VerticalAlignment=`"Center`"/></Border>"
}
# A screenshot, cropped and scaled onto the canvas. Ring/At map a screenshot pixel rectangle to canvas units.
function Shot($file, $cx, $cy, $cw, $ch, $x, $y, $w) {
    $s = $w / $cw
    $o = [pscustomobject]@{ File = $file; Crop = @($cx, $cy, $cw, $ch); X = $x; Y = $y; S = $s }
    $o | Add-Member ScriptMethod At { param($sx, $sy) @(($this.X + ($sx - $this.Crop[0]) * $this.S), ($this.Y + ($sy - $this.Crop[1]) * $this.S)) }
    $o
}
function ImgTag($shot) {
    $w = $shot.Crop[2] * $shot.S; $h = $shot.Crop[3] * $shot.S
    (Box ($shot.X - 1) ($shot.Y - 1) ($w + 2) ($h + 2) $C.bg $C.line 6 1) +
    "<Image Tag=`"$($script:images.Count)`" Canvas.Left=`"$($shot.X)`" Canvas.Top=`"$($shot.Y)`" Width=`"$w`" Height=`"$h`" Stretch=`"Fill`"/>"
    $script:images.Add($shot)
}
function Ring($shot, $x1, $y1, $x2, $y2, $n, $badge = 'left') {
    $a = $shot.At($x1, $y1); $b = $shot.At($x2, $y2)
    $r = "<Border Canvas.Left=`"$($a[0] - 4)`" Canvas.Top=`"$($a[1] - 4)`" Width=`"$($b[0] - $a[0] + 8)`" Height=`"$($b[1] - $a[1] + 8)`" CornerRadius=`"8`" BorderBrush=`"$($C.orange)`" BorderThickness=`"3`"/>"
    $bx = if ($badge -eq 'left') { $a[0] - 18 } else { $b[0] - 10 }
    $r + (Dot $bx ($a[1] - 18) $n)
}
function Render([string]$name, [int]$w, [int]$h, [string]$body) {
    $v = [Windows.Markup.XamlReader]::Parse("<Canvas $ns Width=`"$w`" Height=`"$h`" Background=`"$($C.bg)`">$body</Canvas>")
    foreach ($el in $v.Children) {
        if ($el -is [Windows.Controls.Image]) {
            $sh = $script:images[[int]$el.Tag]
            $bi = [Windows.Media.Imaging.BitmapImage]::new(); $bi.BeginInit(); $bi.CacheOption = 'OnLoad'
            $bi.UriSource = [Uri]::new((Join-Path $Shots $sh.File)); $bi.EndInit()
            $el.Source = [Windows.Media.Imaging.CroppedBitmap]::new($bi, [Windows.Int32Rect]::new($sh.Crop[0], $sh.Crop[1], $sh.Crop[2], $sh.Crop[3]))
            [Windows.Media.RenderOptions]::SetBitmapScalingMode($el, 'HighQuality')
        }
    }
    $v.Measure([Windows.Size]::new($w, $h)); $v.Arrange([Windows.Rect]::new(0, 0, $w, $h)); $v.UpdateLayout()
    $s = 1.5
    $bmp = [Windows.Media.Imaging.RenderTargetBitmap]::new([int]($w * $s), [int]($h * $s), 96 * $s, 96 * $s, [Windows.Media.PixelFormats]::Pbgra32); $bmp.Render($v)
    $enc = [Windows.Media.Imaging.PngBitmapEncoder]::new(); $enc.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bmp))
    $p = Join-Path $OutDir "$name.png"; $fs = [IO.File]::Create($p); $enc.Save($fs); $fs.Close(); "wrote $p"
}
function Legend($x, $y, $w, $rows) {
    $o = ''; foreach ($r in $rows) { $o += (Dot $x $y $r[0]) + (T ($x + 42) ($y + 1) $r[1] 17 $C.text 'SemiBold' ($w - 42)) + (T ($x + 42) ($y + 28) $r[2] 14 $C.label 'Normal' ($w - 42)); $y += $r[3] }
    $o
}

# 1. A script and the page it becomes ---------------------------------------------------------------------
$script:images = [Collections.Generic.List[object]]::new()
$b = (T 40 28 'Write an ordinary script. Launchpad builds the page.' 26 $C.text 'Bold') +
     (T 40 66 'Test-Port.ps1 on the left, the page it becomes underneath. The numbers match.' 16 $C.label)
$b += (Box 40 112 600 452 $C.inset $C.line 12)
$code = @(
    @('<#', $C.muted, 0), @('.SYNOPSIS', $C.accent, 1), @('    Checks whether a computer answers on a TCP port...', $C.text, 0),
    @('.COMPONENT', $C.accent, 0), @('    Network', $C.text, 0), @('#>', $C.muted, 0),
    @('param(', $C.text, 2), @('    [Parameter(Mandatory)][string]$Computer,', $C.text, 0), @('    [int]$Port = 443', $C.text, 0), @(')', $C.text, 0),
    @('', $C.text, 0), @('Write-Host "Trying $Computer on port $Port..."', $C.text, 3), @('', $C.text, 0),
    @('[pscustomobject]@{', $C.text, 4), @('    Status  = ''Failed''', $C.fail, 0), @('    Message = "$Computer does not answer..."', $C.text, 0), @('    Data    = @{ Open = $false } }', $C.text, 0))
$y = 132; foreach ($l in $code) { $b += (T 92 $y $l[0] 15 $l[1] 'Normal' 0 $true); if ($l[2]) { $b += (Dot 54 ($y - 3) $l[2]) }; $y += 25 }
$b += Legend 680 124 520 @(
    @(1, 'The help becomes the title', '.SYNOPSIS is the page subtitle and the card text. .COMPONENT picks the group.', 108),
    @(2, 'param() becomes the form', 'A [string] is a box, an [int] a number. Mandatory: Run waits until it is filled in.', 108),
    @(3, 'Write-Host goes to the output', 'Line by line, while it runs. Kept with the run in Activity.', 108),
    @(4, 'The returned object is the result', 'Status sets the colour, Message the sentence. The README''s "Reading the results" shows under it.', 108))
$tp = Shot 'testport.png' 160 100 1600 760 40 600 1160
$b += ImgTag $tp
$b += (Ring $tp 172 118 1035 195 1) + (Ring $tp 208 268 782 492 2) + (Ring $tp 852 268 1718 330 3) + (Ring $tp 210 590 782 842 4)
Render 'script-anatomy' 1240 1190 $b

# 2. Decide later, on the real page -----------------------------------------------------------------------
$script:images = [Collections.Generic.List[object]]::new()
$b = (T 40 28 'Decide later: the run waits, saved to disk' 26 $C.text 'Bold') +
     (T 40 66 'Publish the scripts, paused at its approval. Close the app; it is still here tomorrow.' 16 $C.label)
$pw = Shot 'publish-waiting.png' 30 30 1770 1200 40 112 1160
$b += ImgTag $pw
# keep the machine's own paths out of the picture
$p1 = $pw.At(866, 786); $p2 = $pw.At(1712, 812)
$b += "<Border Canvas.Left=`"$($p1[0])`" Canvas.Top=`"$($p1[1])`" Width=`"$($p2[0] - $p1[0])`" Height=`"$($p2[1] - $p1[1])`" Background=`"#05080D`"/>" +
      (T ($p1[0] + 5) ($p1[1] + 0) 'Packaged 15 scripts into %TEMP%\Launchpad-scripts-1.0.0' 11.4 '#E2E8F0' 'Normal' 0 $true)
# the approval window, laid over the empty bottom of the output
$dl = Shot 'publish-approve.png' 0 0 870 444 0 0 1
$dw = 450; $dl.S = $dw / 870; $q = $pw.At(960, 850); $dl.X = $q[0]; $dl.Y = $q[1]
$b += "<Border Canvas.Left=`"$($dl.X - 8)`" Canvas.Top=`"$($dl.Y - 8)`" Width=`"$($dw + 16)`" Height=`"$(444 * $dl.S + 16)`" CornerRadius=`"10`" Background=`"$($C.bg)`" BorderBrush=`"$($C.amber)`" BorderThickness=`"2`"/>"
$b += ImgTag $dl
$b += (Ring $dl 303 355 491 404 1) + (Ring $pw 222 966 770 1036 2) + (Ring $pw 1425 44 1522 74 3 'right') + (Ring $pw 852 340 1165 392 4)
$b += Legend 40 920 560 @(
    @(1, 'Decide later', 'At an approval, instead of Approve or Reject.', 84),
    @(2, 'The run stops at that step', 'Steps 1 to 4, and what they found, are saved.', 84))
$b += Legend 640 920 560 @(
    @(3, 'The top bar counts it', 'Even after the app is closed and opened again.', 84),
    @(4, 'Approve or Reject when ready', 'Here, or from Activity. It carries on from step 5.', 84))
Render 'decide-later' 1240 1090 $b

# 3. How values travel: the reference rides on the arrow ---------------------------------------------------
$script:images = [Collections.Generic.List[object]]::new()
$b = (T 40 28 'How one step hands its findings to the next' 26 $C.text 'Bold') +
     (T 40 66 'Each step leaves a result. A later step names it in curly braces, and gets it.' 16 $C.label)
function Step($y, $icon, $fg, $id, $what, $leaves) {
    (Box 40 $y 470 86 $C.card $fg 14 2) + (Icon 62 ($y + 18) $icon 22 $fg) + (T 98 ($y + 14) $id 21 $C.text 'SemiBold') +
    (T 98 ($y + 48) $what 15 $C.label) + (T 300 ($y + 18) 'leaves' 12 $C.muted 'SemiBold') + (T 300 ($y + 36) $leaves 15 $C.ok 'Normal' 196)
}
function Hand($y, $ref, $note) {
    (Arrow 150 $y 150 ($y + 52) $C.accent 3) + (T 176 ($y + 4) $ref 17 $C.accent 'SemiBold' 0 $true) + (T 176 ($y + 28) $note 14 $C.label)
}
$b += Step 116 'E756' $C.brand 'lint' 'checks every script' 'a row per script'
$b += Hand 204 '{lint.Data}' 'the rows go into the pick list'
$b += Step 258 'E762' $C.orange 'pick' 'you tick which go' 'the rows you ticked'
$b += Hand 346 '{pick.Data.Path}' 'the Path of every ticked row'
$b += Step 400 'E756' $C.brand 'pack' 'zips those files' 'the zip, and a Count'
$b += Hand 488 '{pack.Data.Count}' 'filled into the question as text'
$b += Step 542 'EA18' $C.amber 'approve' 'asks before publishing' 'who said yes, and when'
# the form at the start feeds two steps
$b += (Box 760 116 440 120 $C.card $C.line 14) + (T 784 132 'Before the run: the form' 15 $C.muted 'SemiBold') +
      (T 784 164 'Version' 14 $C.label) + (Box 784 186 392 34 $C.inset $C.line 6) + (T 798 192 '1.0.0' 15 $C.text)
$b += "<Polyline Points=`"980,236 980,443 530,443`" Stroke=`"$($C.accent)`" StrokeThickness=`"2.5`" StrokeDashArray=`"3 2`"/>" +
      "<Polygon Points=`"512,443 526,436 526,450`" Fill=`"$($C.accent)`"/>" +
      (T 560 392 '{input.Version}' 17 $C.accent 'SemiBold' 0 $true) + (T 560 416 'the version, for pack and the question' 14 $C.label)
$b += "<Polyline Points=`"1040,236 1040,585 530,585`" Stroke=`"$($C.accent)`" StrokeThickness=`"2.5`" StrokeDashArray=`"3 2`"/>" +
      "<Polygon Points=`"512,585 526,578 526,592`" Fill=`"$($C.accent)`"/>"
$b += (Box 560 640 640 56 $C.inset $C.amber 10) + (T 580 646 'What the person sees at approve' 12 $C.muted 'SemiBold') +
      (T 580 666 'Publish 15 scripts as version 1.0.0?' 16 $C.text 'SemiBold')
$b += "<Line X1=`"510`" Y1=`"610`" X2=`"556`" Y2=`"660`" Stroke=`"$($C.amber)`" StrokeThickness=`"2`"/>"
Render 'passing-values' 1240 730 $b
