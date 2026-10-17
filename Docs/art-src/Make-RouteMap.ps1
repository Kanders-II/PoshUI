param([string]$Out)
# The route map for "A website won't load": its steps as boxes, success going down, trouble branching right.
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
$W = 1280; $H = 760
function Box($x, $y, $w, $title, $sub, $kind) {
    $fill = @{ script = '#0F1B2E'; ask = '#10233A'; end = '#0F2A1C'; fail = '#2A1414' }[$kind]
    $edge = @{ script = '#2289F7'; ask = '#7DC4FF'; end = '#4ADE80'; fail = '#F87171' }[$kind]
    @"
  <Border Canvas.Left="$x" Canvas.Top="$y" Width="$w" Height="78" CornerRadius="12" Background="$fill" BorderBrush="$edge" BorderThickness="2">
    <StackPanel VerticalAlignment="Center" Margin="16,0">
      <TextBlock Text="$title" FontSize="22" FontWeight="SemiBold" Foreground="#E2E8F0"/>
      <TextBlock Text="$sub" FontSize="15" Foreground="#94A3B8" Margin="0,3,0,0"/>
    </StackPanel>
  </Border>
"@
}
function Arrow($x1, $y1, $x2, $y2, $label, $colour, [switch]$Elbow) {
    $pts = if ($Elbow) { "$x1,$y1 $x2,$y1 $x2,$y2" } else { "$x1,$y1 $x2,$y2" }
    $ax = $x2; $ay = $y2
    $head = if ($y2 -gt $y1 -and ($x1 -eq $x2 -or $Elbow)) { "$($ax-8),$($ay-12) $ax,$ay $($ax+8),$($ay-12)" } elseif ($x2 -gt $x1) { "$($ax-12),$($ay-8) $ax,$ay $($ax-12),$($ay+8)" } else { "$($ax+12),$($ay-8) $ax,$ay $($ax+12),$($ay+8)" }
    $lx = if ($Elbow) { ($x1 + $x2) / 2 - 30 } elseif ($x1 -eq $x2) { $x1 + 10 } else { ($x1 + $x2) / 2 - 30 }
    $ly = if ($Elbow) { $y1 - 26 } elseif ($x1 -eq $x2) { ($y1 + $y2) / 2 - 12 } else { $y1 - 26 }
    @"
  <Polyline Points="$pts" Stroke="$colour" StrokeThickness="2.5"/>
  <Polygon Points="$head" Fill="$colour"/>
  <TextBlock Canvas.Left="$lx" Canvas.Top="$ly" Text="$label" FontSize="14" Foreground="$colour" FontFamily="Cascadia Mono, Consolas"/>
"@
}
$ok = '#4ADE80'; $bad = '#F87171'; $ask = '#7DC4FF'
$xaml = @"
<Canvas xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Width="$W" Height="$H" Background="#070B12">
  <TextBlock Canvas.Left="40" Canvas.Top="26" Text="A website won't load - the route it takes" FontSize="26" FontWeight="Bold" Foreground="#E2E8F0"/>
  <TextBlock Canvas.Left="40" Canvas.Top="62" Text="Each step's result picks the next. Down is good news; right is something to fix." FontSize="16" Foreground="#94A3B8"/>
$(Box 40 120 300 'net' 'Is there a connection?' 'script')
$(Box 40 300 300 'dns' 'Can the name be looked up?' 'script')
$(Box 40 480 300 'site' 'Does the site answer?' 'script')
$(Box 40 650 300 'Works from here' 'clear the browser cache' 'end')
$(Box 470 120 330 'wifi' 'You: is Wi-Fi on?' 'ask')
$(Box 920 120 320 'adapter' 'Adapter on but not connecting' 'fail')
$(Box 470 300 330 'flush  ->  dns2' 'Clear the DNS cache, try again' 'script')
$(Box 920 300 320 'other  ->  dns3' 'You: try another DNS server' 'ask')
$(Box 470 480 330 'Down or blocked' 'quote WEB1 to IT' 'fail')
$(Box 920 480 320 'DNS problem' 'quote DNS2 / DNS3 / DNS4' 'fail')
$(Arrow 190 198 190 300 'ok' $ok)
$(Arrow 190 378 190 480 'ok' $ok)
$(Arrow 190 558 190 650 'ok' $ok)
$(Arrow 340 159 470 159 'failed' $bad)
$(Arrow 800 159 920 159 'yes' $ask)
$(Arrow 340 339 470 339 'failed' $bad)
$(Arrow 800 339 920 339 'fails' $bad)
$(Arrow 340 519 470 519 'failed' $bad)
$(Arrow 1080 378 1080 480 'fails' $bad)
  <Polyline Points="635,378 635,430 260,430 260,480" Stroke="$ok" StrokeThickness="2.5" StrokeDashArray="3 2"/>
  <Polygon Points="252,468 260,480 268,468" Fill="$ok"/>
  <TextBlock Canvas.Left="420" Canvas.Top="404" Text="fixed" FontSize="14" Foreground="$ok" FontFamily="Cascadia Mono, Consolas"/>
</Canvas>
"@
$v = [Windows.Markup.XamlReader]::Parse($xaml)
$v.Measure([Windows.Size]::new($W, $H)); $v.Arrange([Windows.Rect]::new(0, 0, $W, $H)); $v.UpdateLayout()
$bmp = [Windows.Media.Imaging.RenderTargetBitmap]::new($W, $H, 96, 96, [Windows.Media.PixelFormats]::Pbgra32); $bmp.Render($v)
$enc = [Windows.Media.Imaging.PngBitmapEncoder]::new(); $enc.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bmp))
$fs = [IO.File]::Create($Out); $enc.Save($fs); $fs.Close()
"wrote $Out"

