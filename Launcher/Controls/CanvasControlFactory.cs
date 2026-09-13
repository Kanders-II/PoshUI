// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Launcher.Models;

namespace Launcher.Controls
{
    /// <summary>
    /// Builds WPF controls for a free-form Canvas page from <see cref="UIControlJson"/> definitions,
    /// positioned by absolute X/Y. Self-contained imperative factory (mirrors the WinUI 3 engine) so
    /// the same canvas definition renders on the v1 (.NET 4.8 / WPF) engine.
    /// </summary>
    public static class CanvasControlFactory
    {
        /// <summary>Builds a control (positioned) with value get/set hooks. onEvent(name, "Clicked"/"ValueChanged").</summary>
        public static CanvasControl Build(UIControlJson c, Action<string, string> onEvent)
        {
            // Controls with an Action/OnChange but no author Name still need to be addressable by the
            // bridge (which dispatches events by Name). Give them a stable synthetic name.
            if (string.IsNullOrEmpty(c.Name) && (!string.IsNullOrEmpty(c.Action) || !string.IsNullOrEmpty(c.OnChange)))
                c.Name = "__auto_" + Guid.NewGuid().ToString("N");

            var cc = new CanvasControl { Name = c.Name ?? string.Empty, Def = c };
            string type = (c.Type ?? "Label").Trim();

            // Tabs: each child becomes a tab page rather than being laid out into a shared panel, so this is
            // handled before the generic container path. The child's Label is its header; the control's value is
            // the selected index, so Get/Set-UICanvasValue can read or switch the active tab.
            if (type == "Tabs" || type == "TabControl")
            {
                var tabs = new TabControl();
                if (c.Children != null)
                {
                    foreach (var child in c.Children)
                    {
                        var childCc = Build(child, onEvent);
                        cc.Children.Add(childCc);
                        var item = new TabItem { Header = Str(child.Label) ?? "Tab", Content = childCc.Element };
                        if (PBoolF(child, "Disabled")) item.IsEnabled = false;   // via P() so the read is recorded for the linter
                        tabs.Items.Add(item);
                    }
                }
                if (tabs.Items.Count > 0) tabs.SelectedIndex = (int)PDbl(c, "SelectedIndex", 0);
                cc.GetValue = () => tabs.SelectedIndex;
                cc.SetValue = v =>
                {
                    int i;
                    if (int.TryParse(Str(v), NumberStyles.Any, CultureInfo.InvariantCulture, out i))
                    { if (i >= 0 && i < tabs.Items.Count) tabs.SelectedIndex = i; return; }
                    // Also accept a header string so a caller can switch tabs by name.
                    string want = Str(v);
                    for (int k = 0; k < tabs.Items.Count; k++)
                        if (string.Equals(Str(((TabItem)tabs.Items[k]).Header), want, StringComparison.OrdinalIgnoreCase)) { tabs.SelectedIndex = k; return; }
                };
                if (!string.IsNullOrEmpty(c.Name) && onEvent != null)
                {
                    var nm = c.Name;
                    tabs.SelectionChanged += (s, e) => { if (e.Source == tabs) onEvent(nm, "ValueChanged"); };
                }
                cc.Element = tabs;
                ApplyThemeDefaults(cc.Element);
                ApplyCommonStyle(c, cc.Element);
                Position(cc.Element, c);
                ApplyAccessibility(cc.Element, c);
                return cc;
            }

            if (IsContainerType(type))
            {
                string layout = ContainerLayout(type, c);
                Panel inner = CreateLayoutPanel(c, layout);
                int cols = (int)PDbl(c, "Columns", 2);
                if (cols < 1) cols = 1;
                int idx = 0;
                if (c.Children != null)
                {
                    foreach (var child in c.Children)
                    {
                        var childCc = Build(child, onEvent);
                        ApplyChildLayout(inner, childCc.Element, c, child, layout, ref idx, cols);
                        inner.Children.Add(childCc.Element);
                        cc.Children.Add(childCc);
                    }
                }
                double stagger = PDbl(c, "Stagger", 0);
                if (stagger > 0) ApplyStagger(inner, stagger);

                if (type == "Expander" || type == "CardExpander")
                    cc.Element = WrapExpander(c, inner);
                else if (type == "Viewbox")
                {
                    // Scales its content to fit the available space (vector scaling — text stays crisp).
                    var vb = new Viewbox { Child = inner };
                    string st = (PStr(c, "Stretch") ?? "Uniform").Trim();
                    vb.Stretch = st.Equals("Fill", StringComparison.OrdinalIgnoreCase) ? Stretch.Fill
                        : st.Equals("UniformToFill", StringComparison.OrdinalIgnoreCase) ? Stretch.UniformToFill
                        : st.Equals("None", StringComparison.OrdinalIgnoreCase) ? Stretch.None : Stretch.Uniform;
                    string sd = (PStr(c, "StretchDirection") ?? "Both").Trim();
                    vb.StretchDirection = sd.Equals("UpOnly", StringComparison.OrdinalIgnoreCase) ? StretchDirection.UpOnly
                        : sd.Equals("DownOnly", StringComparison.OrdinalIgnoreCase) ? StretchDirection.DownOnly : StretchDirection.Both;
                    cc.Element = vb;
                }
                else
                    cc.Element = WrapContainer(c, type == "Card" || type == "ControlGroup", inner);

                // Clickable container: a Card/Panel given an -Action fires like a button (e.g. selectable rows).
                // Interactive children (buttons) mark the event handled, so they win over the row click.
                if (!string.IsNullOrEmpty(c.Action) && onEvent != null)
                {
                    var elc = cc.Element; var nmc = c.Name;
                    elc.Cursor = System.Windows.Input.Cursors.Hand;
                    if (elc is Border cb && cb.Background == null) cb.Background = Brushes.Transparent; // hit-test empty areas
                    elc.MouseLeftButtonUp += (s, e) => onEvent(nmc, "Clicked");
                }
            }
            else
            {
                cc.Element = BuildCore(c, onEvent, cc);
                // Only derive value hooks from the element type when BuildCore didn't set custom ones.
                if (cc.GetValue == null && cc.SetValue == null) WireValue(cc.Element, cc);
            }

            ApplyThemeDefaults(cc.Element);   // theme brushes (skipped where a control set its own)
            ApplyCommonStyle(c, cc.Element);  // per-control Properties override the theme
            Position(cc.Element, c);
            ApplyAccessibility(cc.Element, c);
            return cc;
        }

        /// <summary>Convenience: just the positioned element (no value hooks).</summary>
        public static FrameworkElement BuildElement(UIControlJson c, Action<string, string> onEvent)
        {
            return Build(c, onEvent).Element;
        }

        /// <summary>Applies common Properties styling (Foreground/Background/Font/Opacity) to any control.
        /// TextBlock font/foreground are handled in ApplyText; this covers Control-derived elements too.</summary>
        private static void ApplyCommonStyle(UIControlJson c, FrameworkElement el)
        {
            if (el == null || c.Properties == null) return;

            double op = PDbl(c, "Opacity", -1);
            if (op >= 0) el.Opacity = op;

            // Layout hints (useful inside Stack/Grid/Wrap panels).
            string mg = PStr(c, "Margin");
            if (!string.IsNullOrEmpty(mg)) { var t = ParseThickness(mg); if (t.HasValue) el.Margin = t.Value; }
            string ha = PStr(c, "HAlign") ?? PStr(c, "HorizontalAlignment");
            if (!string.IsNullOrEmpty(ha)) el.HorizontalAlignment = ParseHAlign(ha);
            string va = PStr(c, "VAlign") ?? PStr(c, "VerticalAlignment");
            if (!string.IsNullOrEmpty(va)) el.VerticalAlignment = ParseVAlign(va);

            var fg = Brush(PStr(c, "Foreground"));
            var bg = Brush(PStr(c, "Background"));
            double fs = PDbl(c, "FontSize", 0);
            string fw = PStr(c, "FontWeight");
            string ff = PStr(c, "FontFamily");

            var ctl = el as Control;
            if (ctl != null)
            {
                if (fg != null) ctl.Foreground = fg;
                if (bg != null) ctl.Background = bg;
                if (fs > 0) ctl.FontSize = fs;
                if (!string.IsNullOrEmpty(ff)) ctl.FontFamily = new FontFamily(ff);
                if (!string.IsNullOrEmpty(fw)) ctl.FontWeight = ParseWeight(fw);
            }
            // TextBlock isn't a Control; ApplyText already handles it for Label, but cover the rest.
            var tbk = el as TextBlock;
            if (tbk != null)
            {
                if (fg != null) tbk.Foreground = fg;
                if (fs > 0) tbk.FontSize = fs;
                if (!string.IsNullOrEmpty(ff)) tbk.FontFamily = new FontFamily(ff);
                if (!string.IsNullOrEmpty(fw)) tbk.FontWeight = ParseWeight(fw);
            }

            ApplyGlow(c, el);
        }

        /// <summary>Fades + slides a panel's children in with an incremental per-item delay (premium "build" feel).</summary>
        /// <summary>Returns the element's RenderTransform as a TransformGroup, preserving whatever is
        /// already there. Stagger (a translate) and HoverScale (a scale) both animate the same element,
        /// so assigning RenderTransform directly would silently clobber whichever was applied first.</summary>
        private static TransformGroup EnsureTransformGroup(FrameworkElement el)
        {
            var grp = el.RenderTransform as TransformGroup;
            if (grp != null) return grp;
            grp = new TransformGroup();
            var existing = el.RenderTransform;
            var mt = existing as MatrixTransform;
            bool isIdentity = (existing == null) || (mt != null && mt.Matrix.IsIdentity);
            if (!isIdentity) grp.Children.Add(existing);
            el.RenderTransform = grp;
            return grp;
        }

        private static void ApplyStagger(Panel panel, double perItemMs)
        {
            int idx = 0;
            foreach (var obj in panel.Children)
            {
                var child = obj as FrameworkElement;
                if (child == null) { idx++; continue; }
                var tt = new System.Windows.Media.TranslateTransform(0, 12);
                EnsureTransformGroup(child).Children.Add(tt);
                child.Opacity = 0;
                var begin = TimeSpan.FromMilliseconds(idx * perItemMs);
                var ease = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
                child.BeginAnimation(UIElement.OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300))) { BeginTime = begin, EasingFunction = ease });
                tt.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new System.Windows.Media.Animation.DoubleAnimation(12, 0, new Duration(TimeSpan.FromMilliseconds(300))) { BeginTime = begin, EasingFunction = ease });
                idx++;
            }
        }

        /// <summary>Accessibility: expose a name (label/name) + help text (tooltip) to screen readers / UI automation.</summary>
        private static void ApplyAccessibility(FrameworkElement el, UIControlJson c)
        {
            if (el == null) return;
            string nm = Str(c.Label);
            if (string.IsNullOrEmpty(nm)) nm = c.Name;
            if (!string.IsNullOrEmpty(nm)) System.Windows.Automation.AutomationProperties.SetName(el, nm);
            if (!string.IsNullOrEmpty(c.Tooltip)) System.Windows.Automation.AutomationProperties.SetHelpText(el, c.Tooltip);
        }

        /// <summary>Optional neon glow on ANY control: -Properties @{ Glow='#34D399'; GlowRadius=16; GlowPulse=$true }.
        /// Glow=$true uses the control's Foreground/Fill colour (or accent).</summary>
        private static void ApplyGlow(UIControlJson c, FrameworkElement el)
        {
            string glow = PStr(c, "Glow");
            if (string.IsNullOrEmpty(glow) || el == null) return;
            string gc = (string.Equals(glow, "true", StringComparison.OrdinalIgnoreCase) || glow == "1")
                ? (PStr(c, "Foreground") ?? PStr(c, "Fill") ?? "#6366F1") : glow;
            try
            {
                var col = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(gc);
                var fx = new System.Windows.Media.Effects.DropShadowEffect { Color = col, BlurRadius = PDbl(c, "GlowRadius", 14), ShadowDepth = 0, Opacity = 0.95 };
                el.Effect = fx;
                if (AsBool(PStr(c, "GlowPulse")))
                {
                    var pulse = new System.Windows.Media.Animation.DoubleAnimation(fx.BlurRadius * 0.5, fx.BlurRadius * 1.4, new Duration(TimeSpan.FromSeconds(1.1)))
                    { AutoReverse = true, RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever, EasingFunction = new System.Windows.Media.Animation.SineEase() };
                    fx.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, pulse);
                }
            }
            catch { }
        }

        /// <summary>Applies the app's themed brushes (DynamicResource) so canvas controls match the
        /// active dark/light theme. Uses SetIfUnset so a control that set its own brush (e.g. Console,
        /// Accent button) keeps it, and per-control Properties (applied later) still win.</summary>
        private static void ApplyThemeDefaults(FrameworkElement el)
        {
            // Controls that carry an explicit Style (e.g. our templated buttons / combobox) are fully
            // self-described; applying local brush defaults here would clobber the style's triggers.
            var styled = el as Control;
            if (styled != null && styled.Style != null) return;

            var ctl = el as Control;
            if (ctl != null && (el is TextBox || el is PasswordBox || el is ComboBox || el is ListBox || el is DatePicker))
            {
                SetIfUnset(ctl, Control.BackgroundProperty, "TextBoxBackgroundBrush");
                SetIfUnset(ctl, Control.ForegroundProperty, "BodyForegroundBrush");
                SetIfUnset(ctl, Control.BorderBrushProperty, "BorderBrush");
                if (ctl.ReadLocalValue(Control.MinHeightProperty) == DependencyProperty.UnsetValue) ctl.MinHeight = 30;
                if (ctl.ReadLocalValue(Control.PaddingProperty) == DependencyProperty.UnsetValue && (el is TextBox || el is PasswordBox))
                    ctl.Padding = new Thickness(6, 3, 6, 3);
                if (ctl.ReadLocalValue(Control.VerticalContentAlignmentProperty) == DependencyProperty.UnsetValue)
                    ctl.VerticalContentAlignment = VerticalAlignment.Center;
            }
            else if (el is Button btn)
            {
                SetIfUnset(btn, Control.BackgroundProperty, "ButtonBackgroundBrush");
                SetIfUnset(btn, Control.ForegroundProperty, "ButtonForegroundBrush");
                SetIfUnset(btn, Control.BorderBrushProperty, "BorderBrush");
                if (btn.ReadLocalValue(Control.PaddingProperty) == DependencyProperty.UnsetValue) btn.Padding = new Thickness(14, 5, 14, 5);
                if (btn.ReadLocalValue(Control.MinHeightProperty) == DependencyProperty.UnsetValue) btn.MinHeight = 30;
            }
            else if (el is CheckBox || el is RadioButton || el is Expander)
            {
                SetIfUnset((Control)el, Control.ForegroundProperty, "BodyForegroundBrush");
            }
            else if (el is TextBlock tbk)
            {
                if (tbk.ReadLocalValue(TextBlock.ForegroundProperty) == DependencyProperty.UnsetValue)
                    tbk.SetResourceReference(TextBlock.ForegroundProperty, "BodyForegroundBrush");
            }
        }

        private static void SetIfUnset(Control ctl, DependencyProperty dp, string resourceKey)
        {
            if (ctl.ReadLocalValue(dp) == DependencyProperty.UnsetValue)
                ctl.SetResourceReference(dp, resourceKey);
        }

        private static Stretch ParseStretch(string s)
        {
            if (string.Equals(s, "UniformToFill", StringComparison.OrdinalIgnoreCase)) return Stretch.UniformToFill;
            if (string.Equals(s, "Fill", StringComparison.OrdinalIgnoreCase)) return Stretch.Fill;
            if (string.Equals(s, "None", StringComparison.OrdinalIgnoreCase)) return Stretch.None;
            return Stretch.Uniform;
        }

        private static readonly string[] ImageExts = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".ico", ".webp", ".tif", ".tiff" };

        /// <summary>True when a token looks like an image file/URL (so it should render as a bitmap, not a font glyph).</summary>
        internal static bool IsImageSource(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            if (s.StartsWith("pack://", StringComparison.OrdinalIgnoreCase)) return true;
            int q = s.IndexOf('?');
            string path = q >= 0 ? s.Substring(0, q) : s;
            foreach (var e in ImageExts) if (path.EndsWith(e, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Loads an image source (file path, file://, http(s)://, or pack://). Decoded on load so the
        /// file isn't locked; returns null on any failure (caller renders empty rather than crashing).</summary>
        internal static System.Windows.Media.Imaging.BitmapImage MakeImageSource(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            try
            {
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(s.Trim(), UriKind.RelativeOrAbsolute);
                bmp.EndInit();
                return bmp;
            }
            catch { return null; }
        }

        /// <summary>Builds an icon element: a bitmap when the token is an image path/URL (PNG icon support),
        /// otherwise a Segoe Fluent font glyph.</summary>
        private static FrameworkElement BuildIconElement(string token, double size, Brush fg)
        {
            if (IsImageSource(token))
            {
                var img = new Image
                {
                    Stretch = Stretch.Uniform,
                    Width = size,
                    Height = size,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var src = MakeImageSource(token);
                if (src != null) img.Source = src;
                return img;
            }
            string glyph = ResolveGlyph(token);
            var t = new TextBlock
            {
                Text = glyph,
                FontFamily = IconFont,
                FontSize = size,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0)
            };
            if (fg != null) t.Foreground = fg;

            // Centre on the glyph's INK, not its text metrics. A TextBlock's box spans the font's
            // full ascent/descent, so Center alignment leaves an icon visibly high inside a round
            // node. Measure where the drawn pixels actually sit and offset by the difference.
            //
            // The offset is a RenderTransform, NOT a margin. A symmetric margin (-dx,+dx) keeps the
            // measured size right but makes one side negative, and a negative margin overlaps the
            // neighbouring element - which silently ate the gap between an icon and its label in
            // every HStack. A transform moves the glyph after layout, so nothing else shifts.
            try
            {
                var typeface = new Typeface(IconFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                var ft = new FormattedText(glyph, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                                           typeface, size, Brushes.Black, 1.0);
                var ink = ft.BuildGeometry(new Point(0, 0)).Bounds;
                if (!ink.IsEmpty && ft.Width > 0 && ft.Height > 0)
                {
                    double dx = (ink.Left + ink.Width / 2.0) - ft.Width / 2.0;
                    double dy = (ink.Top + ink.Height / 2.0) - ft.Height / 2.0;
                    if (Math.Abs(dx) > 0.01 || Math.Abs(dy) > 0.01)
                    {
                        // Through the shared group so this composes with Spin's RotateTransform.
                        EnsureTransformGroup(t).Children.Add(new TranslateTransform(-dx, -dy));
                    }
                }
            }
            catch { }
            return t;
        }

        private static Thickness? ParseThickness(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var parts = s.Split(',');
            double[] v = new double[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                if (!double.TryParse(parts[i].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out v[i])) return null;
            if (v.Length == 1) return new Thickness(v[0]);
            if (v.Length == 2) return new Thickness(v[0], v[1], v[0], v[1]);
            if (v.Length == 4) return new Thickness(v[0], v[1], v[2], v[3]);
            return null;
        }

        private static HorizontalAlignment ParseHAlign(string s)
        {
            if (string.Equals(s, "Left", StringComparison.OrdinalIgnoreCase)) return HorizontalAlignment.Left;
            if (string.Equals(s, "Right", StringComparison.OrdinalIgnoreCase)) return HorizontalAlignment.Right;
            if (string.Equals(s, "Center", StringComparison.OrdinalIgnoreCase)) return HorizontalAlignment.Center;
            return HorizontalAlignment.Stretch;
        }

        private static VerticalAlignment ParseVAlign(string s)
        {
            if (string.Equals(s, "Top", StringComparison.OrdinalIgnoreCase)) return VerticalAlignment.Top;
            if (string.Equals(s, "Bottom", StringComparison.OrdinalIgnoreCase)) return VerticalAlignment.Bottom;
            if (string.Equals(s, "Center", StringComparison.OrdinalIgnoreCase)) return VerticalAlignment.Center;
            return VerticalAlignment.Stretch;
        }

        private static FontWeight ParseWeight(string fw)
        {
            if (string.Equals(fw, "Bold", StringComparison.OrdinalIgnoreCase)) return FontWeights.Bold;
            if (string.Equals(fw, "SemiBold", StringComparison.OrdinalIgnoreCase)) return FontWeights.SemiBold;
            if (string.Equals(fw, "Light", StringComparison.OrdinalIgnoreCase)) return FontWeights.Light;
            if (string.Equals(fw, "Medium", StringComparison.OrdinalIgnoreCase)) return FontWeights.Medium;
            return FontWeights.Normal;
        }

        private static void Position(FrameworkElement el, UIControlJson c)
        {
            Canvas.SetLeft(el, c.X);
            Canvas.SetTop(el, c.Y);
            if (c.Width > 0) el.Width = c.Width;
            if (c.Height > 0) el.Height = c.Height;
            if (c.ZIndex != 0) Canvas.SetZIndex(el, c.ZIndex);
            if (!string.IsNullOrEmpty(c.Tooltip)) el.ToolTip = c.Tooltip;
            if (c.Visible.HasValue && !c.Visible.Value) el.Visibility = Visibility.Collapsed;
            if (c.Enabled.HasValue && !c.Enabled.Value) el.IsEnabled = false;
        }

        /// <summary>Derives value get/set from the built WPF element type (for the bridge).</summary>
        private static void WireValue(FrameworkElement el, CanvasControl cc)
        {
            if (el is TextBox tb) { cc.GetValue = () => tb.Text; cc.SetValue = v => tb.Text = Str(v) ?? ""; }
            else if (el is PasswordBox pb) { cc.GetValue = () => pb.Password; cc.SetValue = v => pb.Password = Str(v) ?? ""; }
            else if (el is ComboBox cb) { cc.GetValue = () => cb.SelectedItem; cc.SetValue = v => cb.SelectedItem = Str(v); }
            else if (el is ListBox lb) { cc.GetValue = () => lb.SelectedItem; cc.SetValue = v => lb.SelectedItem = Str(v); }
            else if (el is DataGrid dg) { cc.GetValue = () => dg.SelectedItem; cc.SetValue = v => dg.SelectedItem = v; }
            else if (el is CheckBox ch) { cc.GetValue = () => ch.IsChecked == true; cc.SetValue = v => ch.IsChecked = AsBool(v); }
            else if (el is Slider sl) { cc.GetValue = () => sl.Value; cc.SetValue = v => sl.Value = AsDbl(v, 0); }
            else if (el is ProgressBar pbar) { cc.GetValue = () => pbar.Value; cc.SetValue = v => pbar.Value = AsDbl(v, 0); }
            else if (el is DatePicker dp) { cc.GetValue = () => dp.SelectedDate; cc.SetValue = v => dp.SelectedDate = AsDate(v); }
            else if (el is TextBlock t) { cc.GetValue = () => t.Text; cc.SetValue = v => t.Text = Str(v) ?? ""; }
        }

        private static FrameworkElement BuildCore(UIControlJson c, Action<string, string> onEvent, CanvasControl cc)
        {
            string type = (c.Type ?? "Label").Trim();
            string name = c.Name ?? string.Empty;

            switch (type)
            {
                case "Label":
                case "TextBlock":
                    {
                        string labelText = Str(c.Label) ?? Str(c.Default) ?? "";

                        // -Properties @{ Selectable = $true }: a WPF TextBlock cannot be selected, so
                        // render a read-only, borderless, transparent TextBox instead. It looks the same
                        // but supports click-drag selection and Ctrl+C - which matters for log output the
                        // operator needs to copy. Background/Padding/MinHeight/BorderBrush are set locally
                        // because ApplyThemeDefaults uses SetIfUnset and would otherwise style this as an
                        // input field (notably MinHeight 30, which would space log lines far apart).
                        if (AsBool(PStr(c, "Selectable")))
                        {
                            var tb = new TextBox
                            {
                                Text = labelText,
                                IsReadOnly = true,
                                IsReadOnlyCaretVisible = false,
                                IsTabStop = false,
                                TextWrapping = TextWrapping.Wrap,
                                BorderThickness = new Thickness(0),
                                BorderBrush = Brushes.Transparent,
                                Background = Brushes.Transparent,
                                Padding = new Thickness(0),
                                MinHeight = 0,
                                VerticalContentAlignment = VerticalAlignment.Top,
                                Cursor = System.Windows.Input.Cursors.IBeam
                            };
                            return tb;   // ApplyCommonStyle handles Foreground/FontSize/FontFamily
                        }

                        var t = new TextBlock { Text = labelText, TextWrapping = TextWrapping.Wrap };
                        ApplyText(c, t);
                        return t;
                    }
                case "Button":
                    return BuildButton(c, name, onEvent);
                case "Icon":
                case "FontIcon":
                    {
                        // A PNG/image path renders as a bitmap; otherwise a font glyph. Image icons honor
                        // Width/Height/FontSize (square) so small PNG icons sit inline like glyphs.
                        string tok = PStr(c, "Icon") ?? Str(c.Label) ?? Str(c.Default);
                        double size = PDbl(c, "Width", PDbl(c, "Height", PDbl(c, "FontSize", 16)));
                        return BuildIconElement(tok, size, Brush(PStr(c, "Foreground")));
                    }
                case "Badge":
                case "Pill":
                case "Tag":
                    return BuildBadge(c, cc);
                case "TextBox":
                    return BuildTextInput(c, name, onEvent, cc, false);
                case "MultiLine":
                case "RichEdit":
                    return BuildTextInput(c, name, onEvent, cc, true);
                case "Password":
                    return BuildPassword(c, name, cc);
                case "Numeric":
                case "NumberBox":
                    return BuildNumberBox(c, name, onEvent, cc);
                case "Dropdown":
                case "ComboBox":
                    {
                        var cb = new ComboBox();
                        var cbStyle = System.Windows.Application.Current != null
                            ? (System.Windows.Application.Current.TryFindResource("CanvasComboBoxStyle")
                               ?? System.Windows.Application.Current.TryFindResource("ToolbarComboBoxStyle")) as Style : null;
                        if (cbStyle != null) cb.Style = cbStyle;
                        var cbIcons = PStrList(c, "ItemIcons");
                        if (cbIcons.Count > 0 && c.Choices != null)   // per-item icons
                        {
                            for (int i = 0; i < c.Choices.Count; i++)
                                cb.Items.Add(new ComboBoxItem { Tag = c.Choices[i], Content = IconTextRow(i < cbIcons.Count ? cbIcons[i] : null, c.Choices[i]) });
                            if (c.Default != null) SelectByTag(cb, Str(c.Default));
                            if (onEvent != null) cb.SelectionChanged += (s, e) => onEvent(name, "ValueChanged");
                            if (cc != null) { cc.GetValue = () => SelectedTag(cb); cc.SetValue = v => SelectByTag(cb, Str(v)); }
                            return cb;
                        }
                        cb.ItemsSource = c.Choices;
                        if (c.Default != null) cb.SelectedItem = Str(c.Default);
                        if (onEvent != null) cb.SelectionChanged += (s, e) => onEvent(name, "ValueChanged");
                        return cb;
                    }
                case "ListBox":
                    {
                        var lb = new ListBox();
                        var lbIcons = PStrList(c, "ItemIcons");
                        if (lbIcons.Count > 0 && c.Choices != null)   // per-item icons
                        {
                            for (int i = 0; i < c.Choices.Count; i++)
                                lb.Items.Add(new ListBoxItem { Tag = c.Choices[i], Content = IconTextRow(i < lbIcons.Count ? lbIcons[i] : null, c.Choices[i]) });
                            if (c.Default != null) SelectByTag(lb, Str(c.Default));
                            if (onEvent != null) lb.SelectionChanged += (s, e) => onEvent(name, "ValueChanged");
                            if (cc != null) { cc.GetValue = () => SelectedTag(lb); cc.SetValue = v => SelectByTag(lb, Str(v)); }
                            return lb;
                        }
                        lb.ItemsSource = c.Choices;
                        if (c.Default != null) lb.SelectedItem = Str(c.Default);
                        if (onEvent != null) lb.SelectionChanged += (s, e) => onEvent(name, "ValueChanged");
                        return lb;
                    }
                case "RadioGroup":
                    {
                        var panel = new StackPanel
                        {
                            Orientation = string.Equals(PStr(c, "Orientation"), "Horizontal", StringComparison.OrdinalIgnoreCase)
                                ? Orientation.Horizontal : Orientation.Vertical
                        };
                        string group = "rg_" + (string.IsNullOrEmpty(name) ? Guid.NewGuid().ToString("N") : name);
                        if (c.Choices != null)
                            foreach (var ch in c.Choices)
                                panel.Children.Add(new RadioButton { Content = ch, GroupName = group, IsChecked = ch == Str(c.Default), Margin = new Thickness(0, 2, 8, 2) });
                        return panel;
                    }
                case "Checkbox":
                case "CheckBox":
                    {
                        string ci = PStr(c, "Icon");
                        return new CheckBox
                        {
                            IsChecked = AsBool(c.Default),
                            Content = string.IsNullOrEmpty(ci) ? (object)(Str(c.Label) ?? "") : IconTextRow(ci, Str(c.Label) ?? "")
                        };
                    }
                case "Toggle":
                case "ToggleSwitch":
                    return new CheckBox { Content = Str(c.Label) ?? "", IsChecked = AsBool(c.Default) };
                case "Rating":
                    return BuildRating(c, name, onEvent, cc);
                case "ColorPicker":
                    return BuildColorPicker(c, name, onEvent, cc);
                case "TimePicker":
                    {
                        var cb = new ComboBox();
                        var cbStyle = System.Windows.Application.Current != null
                            ? (System.Windows.Application.Current.TryFindResource("CanvasComboBoxStyle")
                               ?? System.Windows.Application.Current.TryFindResource("ToolbarComboBoxStyle")) as Style : null;
                        if (cbStyle != null) cb.Style = cbStyle;
                        int stepMin = (int)PDbl(c, "StepMinutes", 30); if (stepMin < 1) stepMin = 30;
                        var times = new List<string>();
                        for (int h = 0; h < 24; h++) for (int m = 0; m < 60; m += stepMin) times.Add(h.ToString("D2") + ":" + m.ToString("D2"));
                        cb.ItemsSource = times;
                        if (c.Default != null) cb.SelectedItem = Str(c.Default);
                        if (onEvent != null) cb.SelectionChanged += (s, e) => onEvent(name, "ValueChanged");
                        return cb;
                    }
                case "Slider":
                    {
                        var sl = new Slider { Minimum = PDbl(c, "Minimum", 0), Maximum = PDbl(c, "Maximum", 100), Value = AsDbl(c.Default, 0) };
                        double step = PDbl(c, "Step", 0);
                        if (step > 0) { sl.SmallChange = step; sl.LargeChange = step; sl.TickFrequency = step; sl.IsSnapToTickEnabled = true; }
                        if (onEvent != null) sl.ValueChanged += (s, e) => onEvent(name, "ValueChanged");
                        return sl;
                    }
                case "ProgressBar":
                    {
                        var pbar = new ProgressBar { Minimum = PDbl(c, "Minimum", 0), Maximum = PDbl(c, "Maximum", 100), Value = AsDbl(c.Default, 0), Height = c.Height > 0 ? c.Height : 6 };
                        string fillCol = PStr(c, "Foreground") ?? PStr(c, "Fill");
                        if (!string.IsNullOrEmpty(fillCol)) pbar.Foreground = Brush(fillCol);
                        return pbar;   // -Glow is applied generally in ApplyGlow (works on any control)
                    }
                case "Console":
                    {
                        var console = new TextBox
                        {
                            Text = Str(c.Default) ?? "", IsReadOnly = true, AcceptsReturn = true,
                            FontFamily = new FontFamily("Cascadia Mono, Consolas"), FontSize = 12,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(8)
                        };
                        // Theme-driven (was hardcoded near-black): adapts to the active dark/light theme.
                        console.SetResourceReference(Control.BackgroundProperty, "TextBoxBackgroundBrush");
                        console.SetResourceReference(Control.ForegroundProperty, "BodyForegroundBrush");
                        console.SetResourceReference(Control.BorderBrushProperty, "BorderBrush");
                        ApplyThinScrollBars(console);   // thin, semi-transparent scrollbar (not the chunky OS default)
                        return console;
                    }
                case "Menu":
                case "MenuBar":
                    return BuildMenu(c, onEvent);
                case "GridSplitter":
                    {
                        // Drag handle that resizes adjacent Grid rows/columns. Place it in a Grid cell between
                        // the two panes; Orientation 'Vertical' (default) resizes columns, 'Horizontal' rows.
                        bool horiz = string.Equals(PStr(c, "Orientation"), "Horizontal", StringComparison.OrdinalIgnoreCase);
                        var gs = new GridSplitter
                        {
                            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
                            Background = Brush(PStr(c, "Background")) ?? Brush("#2A3550"),
                            ShowsPreview = true
                        };
                        if (horiz)
                        {
                            gs.ResizeDirection = GridResizeDirection.Rows;
                            gs.HorizontalAlignment = HorizontalAlignment.Stretch;
                            gs.VerticalAlignment = VerticalAlignment.Center;
                            gs.Height = PDbl(c, "Thickness", 5);
                        }
                        else
                        {
                            gs.ResizeDirection = GridResizeDirection.Columns;
                            gs.VerticalAlignment = VerticalAlignment.Stretch;
                            gs.HorizontalAlignment = HorizontalAlignment.Center;
                            gs.Width = PDbl(c, "Thickness", 5);
                        }
                        return gs;
                    }
                case "Workflow":
                    return BuildWorkflow(c, cc);
                case "Xaml":
                    return BuildXaml(c, cc, onEvent);
                case "DataGrid":
                    return BuildDataGrid(c, cc, onEvent);
                case "TreeView":
                    return BuildTreeView(c, cc);
                case "AutoSuggest":
                    return BuildAutoSuggest(c, name, onEvent, cc);
                case "Markdown":
                    return BuildMarkdown(c);
                case "Image":
                    {
                        var img = new Image { Stretch = ParseStretch(PStr(c, "Stretch")) };
                        var bs = MakeImageSource(Str(c.Default));
                        if (bs != null) img.Source = bs;
                        return img;
                    }
                case "Hyperlink":
                    {
                        var link = new System.Windows.Documents.Hyperlink(new Run(Str(c.Label) ?? "Link"));
                        string nav = PStr(c, "NavigateUri");
                        if (!string.IsNullOrEmpty(nav))
                        {
                            link.NavigateUri = new Uri(nav, UriKind.RelativeOrAbsolute);
                            link.RequestNavigate += (s, e) => { OpenUri(nav); e.Handled = true; };
                        }
                        if (onEvent != null) link.Click += (s, e) => onEvent(name, "Clicked");
                        var tb = new TextBlock();
                        tb.Inlines.Add(link);

                        // A Hyperlink brings its own theme brush and ignores the Foreground set on
                        // the parent TextBlock, so an -Properties @{ Foreground = ... } would be
                        // silently dropped. Apply it to the inline itself.
                        var linkFg = Brush(PStr(c, "Foreground"));
                        if (linkFg != null) link.Foreground = linkFg;
                        if (PStr(c, "Underline") == "false") link.TextDecorations = null;

                        // Optional glow, and an optional colour/glow swap on hover. WPF gives a
                        // Hyperlink no hover affordance beyond the cursor, which is too subtle on a
                        // dark console where the link is already coloured.
                        var glowCol = PStr(c, "Glow");
                        var hoverFg = Brush(PStr(c, "HoverForeground"));
                        var hoverGlowCol = PStr(c, "HoverGlow");
                        double glowR = PDbl(c, "GlowRadius", 10);

                        Func<string, System.Windows.Media.Effects.Effect> mkGlow = col =>
                            string.IsNullOrEmpty(col) ? null : new System.Windows.Media.Effects.DropShadowEffect
                            { Color = ParseColor(col), BlurRadius = glowR, ShadowDepth = 0, Opacity = 0.95 };

                        var restEffect = mkGlow(glowCol);
                        if (restEffect != null) tb.Effect = restEffect;

                        if (hoverFg != null || !string.IsNullOrEmpty(hoverGlowCol))
                        {
                            var restFg = link.Foreground;
                            var hoverEffect = mkGlow(string.IsNullOrEmpty(hoverGlowCol) ? glowCol : hoverGlowCol);
                            link.MouseEnter += (s, e) =>
                            {
                                if (hoverFg != null) link.Foreground = hoverFg;
                                if (hoverEffect != null) tb.Effect = hoverEffect;
                            };
                            link.MouseLeave += (s, e) =>
                            {
                                link.Foreground = restFg;
                                tb.Effect = restEffect;
                            };
                        }
                        return tb;
                    }
                case "Separator":
                    return new Separator { Margin = new Thickness(0) };
                case "Shortcut":     // invisible: the bridge maps its key gesture to its Action
                case "StateInit":    // invisible: the bridge seeds reactive state from its StateJson
                case "StateWatcher": // invisible: the bridge runs its Action when the watched state key changes
                case "Event":        // invisible: a named Action a toolbar link/button fires via the bridge
                case "WindowTemplate": // invisible: carries a secondary window's content (Show-UICanvasWindow builds it)
                    return new FrameworkElement { Width = 0, Height = 0, Visibility = Visibility.Collapsed };
                case "Toolbar":
                    return BuildToolbar(c, onEvent);
                case "Footer":
                    return BuildFooter(c, onEvent);
                case "DatePicker":
                case "CalendarView":
                    {
                        var dp = new DatePicker { SelectedDate = AsDate(c.Default) };
                        if (onEvent != null) dp.SelectedDateChanged += (s, e) => onEvent(name, "ValueChanged");
                        return dp;
                    }
                case "ProgressRing":
                    return BuildProgressRing(c);
                case "DropDownButton":
                case "SplitButton":
                    return BuildDropDownButton(c, name, onEvent, cc);
                case "Banner":
                case "InfoBar":
                    return BuildBanner(c);
                case "MetricCard": return BuildMetricCard(c, cc);
                case "StatusCard": return BuildStatusCard(c);
                case "TableCard": return BuildTableCard(c);
                case "ChartCard": return BuildChartCard(c, cc);
                case "Rectangle":
                    return new Rectangle { Fill = Brush(PStr(c, "Fill") ?? "#6366F1"), Stroke = Brush(PStr(c, "Stroke")), StrokeThickness = PDbl(c, "StrokeThickness", 0), RadiusX = PDbl(c, "CornerRadius", 0), RadiusY = PDbl(c, "CornerRadius", 0) };
                case "Ellipse":
                    return new Ellipse { Fill = Brush(PStr(c, "Fill") ?? "#6366F1"), Stroke = Brush(PStr(c, "Stroke")), StrokeThickness = PDbl(c, "StrokeThickness", 0) };
                case "Line":
                    return new Line { X1 = 0, Y1 = 0, X2 = PDbl(c, "X2", 0) - c.X, Y2 = PDbl(c, "Y2", 0) - c.Y, Stroke = Brush(PStr(c, "Stroke") ?? "#6366F1"), StrokeThickness = PDbl(c, "StrokeThickness", 2) };
                case "Panel":
                case "Card":
                case "Group":
                case "ControlGroup":
                    // Containers are handled by Build() (which also registers children); unreachable here.
                    return WrapContainer(c, type == "Card" || type == "ControlGroup", new Canvas());
                default:
                    return new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(40, 0x63, 0x66, 0xF1)),
                        Padding = new Thickness(8, 4, 8, 4),
                        Child = new TextBlock { Text = type, Foreground = Brushes.White }
                    };
            }
        }

        private static readonly FontFamily IconFont = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");

        // Friendly icon name -> Segoe Fluent Icons / MDL2 Assets code point.
        private static readonly Dictionary<string, string> IconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "play", "\uE768" }, { "start", "\uE768" }, { "pause", "\uE769" }, { "stop", "\uE71A" },
            { "back", "\uE72B" }, { "previous", "\uE72B" }, { "next", "\uE72A" }, { "forward", "\uE72A" },
            { "refresh", "\uE72C" }, { "sync", "\uE895" }, { "settings", "\uE713" }, { "gear", "\uE713" },
            { "check", "\uE73E" }, { "accept", "\uE73E" }, { "complete", "\uE930" }, { "cancel", "\uE711" },
            { "close", "\uE711" }, { "add", "\uE710" }, { "delete", "\uE74D" }, { "save", "\uE74E" },
            { "download", "\uE896" }, { "upload", "\uE898" }, { "info", "\uE946" }, { "warning", "\uE7BA" },
            { "error", "\uE783" }, { "home", "\uE80F" }, { "folder", "\uE8B7" }, { "document", "\uE8A5" },
            { "search", "\uE721" }, { "power", "\uE7E8" }, { "lock", "\uE72E" }, { "shield", "\uEA18" },
            { "security", "\uEA18" }, { "server", "\uE968" }, { "device", "\uE968" }, { "disk", "\uEDA2" },
            { "storage", "\uEDA2" }, { "network", "\uEC05" }, { "app", "\uE71D" }, { "apps", "\uE71D" },
            { "user", "\uE77B" }, { "rocket", "\uE7B8" }, { "deploy", "\uE898" }, { "chip", "\uE950" },
            { "cpu", "\uE950" }, { "tools", "\uEC7A" }, { "driver", "\uE950" },
            { "bell", "\uE7E7" }, { "notification", "\uE7E7" }, { "alerts", "\uE7E7" },
            { "help", "\uE897" }, { "question", "\uE897" }, { "clock", "\uE917" }, { "time", "\uE917" },
            { "memory", "\uE964" }, { "avatar", "\uE77B" }, { "menu", "\uE700" }, { "filter", "\uE71C" },
            // Status glyphs (pipeline rows etc.)
            { "circle", "\uEA3A" }, { "pending", "\uEA3A" }, { "dot", "\uEA3B" },
            { "processing", "\uE72C" }, { "spinner", "\uE72C" }, { "running", "\uE72C" }, { "success", "\uE930" }
        };

        /// <summary>Resolves an icon token to a glyph: friendly name, hex code point (0xE768/U+E768/E768), or raw glyph.</summary>
        internal static string ResolveGlyph(string icon)
        {
            if (string.IsNullOrEmpty(icon)) return "";
            string key = icon.Trim();
            string mapped;
            if (IconMap.TryGetValue(key, out mapped)) return mapped;
            string hex = key;
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || hex.StartsWith("U+", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);
            int cp;
            if (hex.Length >= 4 && hex.Length <= 6 &&
                int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out cp))
                return char.ConvertFromUtf32(cp);
            return key; // assume already a literal glyph/text
        }

        /// <summary>Templated button (Primary/Accent, Secondary, Subtle) with optional leading icon.</summary>
        private static FrameworkElement BuildButton(UIControlJson c, string name, Action<string, string> onEvent)
        {
            var b = new Button();
            string variant = (PStr(c, "Style") ?? "").Trim();
            bool gradient = string.Equals(variant, "Gradient", StringComparison.OrdinalIgnoreCase);
            string styleKey;
            if (gradient ||
                string.Equals(variant, "Accent", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(variant, "Primary", StringComparison.OrdinalIgnoreCase))
                styleKey = "CanvasPrimaryButtonStyle";
            else if (string.Equals(variant, "Subtle", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(variant, "Transparent", StringComparison.OrdinalIgnoreCase))
                styleKey = "CanvasSubtleButtonStyle";
            else
                styleKey = "CanvasSecondaryButtonStyle";

            var style = System.Windows.Application.Current != null
                ? System.Windows.Application.Current.TryFindResource(styleKey) as Style : null;
            if (style != null) b.Style = style;

            if (gradient)   // horizontal accent gradient over the primary layout
            {
                string g1 = PStr(c, "GradientFrom") ?? "#5EA9FF";
                string g2 = PStr(c, "GradientTo") ?? "#2E7BFF";
                b.Background = new System.Windows.Media.LinearGradientBrush(ParseColor(g1), ParseColor(g2), new System.Windows.Point(0, 0), new System.Windows.Point(1, 0));
                b.Foreground = System.Windows.Media.Brushes.White;
            }

            string label = Str(c.Label) ?? "Button";
            string icon = PStr(c, "Icon");
            if (!string.IsNullOrEmpty(icon))
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                // PNG/image path -> bitmap icon; otherwise font glyph.
                var iconEl = BuildIconElement(icon, 16, null);
                iconEl.Margin = new Thickness(0, 0, string.IsNullOrEmpty(label) ? 0 : 8, 0);
                sp.Children.Add(iconEl);
                if (!string.IsNullOrEmpty(label))
                    sp.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
                b.Content = sp;
            }
            else
            {
                b.Content = label;
            }

            string ca = PStr(c, "ContentAlign");   // left/center/right — e.g. nav rails want Left
            if (!string.IsNullOrEmpty(ca)) b.HorizontalContentAlignment = ParseHAlign(ca);

            if (onEvent != null) b.Click += (s, e) => onEvent(name, "Clicked");
            return b;
        }

        /// <summary>Rounded status pill. Severity/Style picks the colour scheme; live-updatable text via cc.SetValue.</summary>
        private static FrameworkElement BuildBadge(UIControlJson c, CanvasControl cc)
        {
            string sev = (PStr(c, "Severity") ?? PStr(c, "Style") ?? "neutral").ToLowerInvariant();
            string bg, fg;
            switch (sev)
            {
                case "success": case "complete": case "ok": case "done": bg = "#14322A"; fg = "#34D399"; break;
                case "warning": case "processing": case "pending": case "running": bg = "#3A2E14"; fg = "#FBBF24"; break;
                case "error": case "failed": case "danger": bg = "#3A1E1E"; fg = "#F87171"; break;
                case "info": case "active": case "accent": bg = "#16203A"; fg = "#93C5FD"; break;
                default: bg = "#1E2942"; fg = "#94A3B8"; break;
            }
            var fgOv = PStr(c, "Foreground");
            var bgOv = PStr(c, "Background");
            var label = new TextBlock
            {
                Text = Str(c.Label) ?? Str(c.Default) ?? "",
                FontSize = PDbl(c, "FontSize", 11),
                FontWeight = FontWeights.SemiBold,
                Foreground = Brush(fgOv ?? fg),
                TextAlignment = TextAlignment.Center
            };
            // Optional leading icon (glyph or PNG).
            FrameworkElement content = label;
            string badgeIcon = PStr(c, "Icon");
            if (!string.IsNullOrEmpty(badgeIcon))
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal };
                var ic = BuildIconElement(badgeIcon, PDbl(c, "FontSize", 11) + 2, Brush(fgOv ?? fg));
                ic.Margin = new Thickness(0, 0, 5, 0);
                row.Children.Add(ic);
                row.Children.Add(label);
                content = row;
            }
            var border = new Border
            {
                Background = Brush(bgOv ?? bg),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 3, 10, 3),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Child = content
            };
            if (cc != null) { cc.GetValue = () => label.Text; cc.SetValue = v => label.Text = Str(v) ?? ""; }
            return border;
        }

        private static FrameworkElement BuildBanner(UIControlJson c)
        {
            string sev = (PStr(c, "Severity") ?? "Informational").ToLowerInvariant();
            string bg = sev == "success" ? "#14322A" : sev == "warning" ? "#3A2E14" : sev == "error" ? "#3A1E1E" : "#16203A";
            string fg = sev == "success" ? "#34D399" : sev == "warning" ? "#FBBF24" : sev == "error" ? "#F87171" : "#93C5FD";
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = Str(c.Label) ?? "", FontWeight = FontWeights.SemiBold, Foreground = Brush(fg) });
            var msg = Str(c.Default);
            if (!string.IsNullOrEmpty(msg)) stack.Children.Add(new TextBlock { Text = msg, Foreground = Brush("#C7D2FE"), TextWrapping = TextWrapping.Wrap });

            // Leading status icon (glyph/PNG) on the left, optional hero illustration on the right.
            var row = new DockPanel { LastChildFill = true };
            string bIcon = PStr(c, "Icon");
            if (!string.IsNullOrEmpty(bIcon))
            {
                var ic = BuildIconElement(bIcon, PDbl(c, "IconSize", 22), Brush(fg));
                ic.VerticalAlignment = VerticalAlignment.Top;
                ic.Margin = new Thickness(0, 1, 12, 0);
                DockPanel.SetDock(ic, Dock.Left);
                row.Children.Add(ic);
            }
            string hero = PStr(c, "Image");
            if (!string.IsNullOrEmpty(hero))
            {
                var him = new Image { Stretch = Stretch.Uniform, Width = PDbl(c, "ImageWidth", 72), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
                var hs = MakeImageSource(hero);
                if (hs != null) him.Source = hs;
                DockPanel.SetDock(him, Dock.Right);
                row.Children.Add(him);
            }
            row.Children.Add(stack);   // fills remaining width
            return new Border { Background = Brush(bg), BorderBrush = Brush(fg), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(12, 8, 12, 8), Child = row };
        }

        // ── Long-tail leaf controls (Rating / ColorPicker) ───────────────────────────────────────
        private static FrameworkElement BuildRating(UIControlJson c, string name, Action<string, string> onEvent, CanvasControl cc)
        {
            int maxStars = (int)PDbl(c, "Max", 5); if (maxStars < 1) maxStars = 5;
            double size = PDbl(c, "FontSize", 20);
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            var stars = new List<TextBlock>();
            int current = (int)AsDbl(c.Default, 0);
            System.Action<int> render = sel =>
            {
                for (int i = 0; i < stars.Count; i++)
                {
                    stars[i].Text = "";                            // FavoriteStarFill (filled) glyph
                    stars[i].Foreground = i < sel ? Brush("#FBBF24") : Brush("#475569");
                }
            };
            for (int i = 0; i < maxStars; i++)
            {
                int idx = i;
                var t = new TextBlock { FontFamily = IconFont, FontSize = size, Margin = new Thickness(0, 0, 4, 0), Cursor = System.Windows.Input.Cursors.Hand };
                t.MouseLeftButtonUp += (s, e) => { current = idx + 1; render(current); if (onEvent != null) onEvent(name, "ValueChanged"); };
                stars.Add(t); sp.Children.Add(t);
            }
            render(current);
            if (cc != null) { cc.GetValue = () => current; cc.SetValue = v => { current = (int)AsDbl(v, 0); render(current); }; }
            return sp;
        }

        private static FrameworkElement BuildColorPicker(UIControlJson c, string name, Action<string, string> onEvent, CanvasControl cc)
        {
            List<string> colors = (c.Choices != null && c.Choices.Count > 0)
                ? new List<string>(c.Choices)
                : new List<string> { "#6366F1", "#34D399", "#FBBF24", "#F87171", "#93C5FD", "#A78BFA", "#F472B6", "#94A3B8" };
            var wrap = new WrapPanel();
            var swatches = new List<Border>();
            string current = Str(c.Default) ?? colors[0];
            System.Action<string> render = sel =>
            {
                foreach (var b in swatches)
                    b.BorderBrush = string.Equals((string)b.Tag, sel, StringComparison.OrdinalIgnoreCase) ? Brush("#FFFFFF") : Brush("#1E2942");
            };
            foreach (var col in colors)
            {
                var b = new Border { Width = 26, Height = 26, Margin = new Thickness(0, 0, 6, 6), CornerRadius = new CornerRadius(5), Background = Brush(col), BorderThickness = new Thickness(2), Tag = col, Cursor = System.Windows.Input.Cursors.Hand };
                b.MouseLeftButtonUp += (s, e) => { current = col; render(current); if (onEvent != null) onEvent(name, "ValueChanged"); };
                swatches.Add(b); wrap.Children.Add(b);
            }
            render(current);
            if (cc != null) { cc.GetValue = () => current; cc.SetValue = v => { current = Str(v); render(current); }; }
            return wrap;
        }

        // ── Dashboard cards (Metric / Status / Table / Chart) — self-contained, themed. ───────────
        private static Border CardShell(UIControlJson c, FrameworkElement inner, double pad = 18)
        {
            return new Border
            {
                Child = inner,
                Padding = new Thickness(PDbl(c, "Padding", pad)),
                Background = CardBg(c),
                BorderBrush = CardBorder(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(PDbl(c, "CornerRadius", 8))
            };
        }

        private static string StateColor(string s)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "ok": case "online": case "success": case "healthy": case "up": case "active": case "running": return "#34D399";
                case "warn": case "warning": case "pending": case "degraded": return "#FBBF24";
                case "error": case "offline": case "failed": case "critical": case "down": return "#F87171";
                default: return "#94A3B8";
            }
        }

        private static FrameworkElement BuildMetricCard(UIControlJson c, CanvasControl cc)
        {
            var v = new StackPanel();
            string caption = Str(c.Label) ?? PStr(c, "Caption");
            if (!string.IsNullOrEmpty(caption))
                v.Children.Add(new TextBlock { Text = caption.ToUpperInvariant(), FontSize = 12, Foreground = Brush("#94A3B8"), Margin = new Thickness(0, 0, 0, 6) });
            var valueText = new TextBlock
            {
                Text = Str(c.Default) ?? PStr(c, "Value") ?? "—",
                FontSize = PDbl(c, "FontSize", 30),
                FontWeight = FontWeights.Bold,
                Foreground = Brush(PStr(c, "Foreground")) ?? TextPrimaryBrush()
            };
            v.Children.Add(valueText);
            if (cc != null) { cc.GetValue = () => valueText.Text; cc.SetValue = x => valueText.Text = Str(x) ?? "—"; }   // live -Refresh
            string trend = (PStr(c, "Trend") ?? "").ToLowerInvariant();
            string delta = PStr(c, "Delta");
            if (!string.IsNullOrEmpty(trend) || !string.IsNullOrEmpty(delta))
            {
                string glyph = trend == "up" ? "" : trend == "down" ? "" : "";   // up / down / dash
                string col = trend == "up" ? "#34D399" : trend == "down" ? "#F87171" : "#94A3B8";
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
                row.Children.Add(new TextBlock { Text = glyph, FontFamily = IconFont, FontSize = 13, Foreground = Brush(col), Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center });
                row.Children.Add(new TextBlock { Text = delta ?? "", FontSize = 13, Foreground = Brush(col), VerticalAlignment = VerticalAlignment.Center });
                v.Children.Add(row);
            }
            return CardShell(c, v);
        }

        private static FrameworkElement BuildStatusCard(UIControlJson c)
        {
            var v = new StackPanel();
            string title = Str(c.Label);
            if (!string.IsNullOrEmpty(title))
                v.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Foreground = TextPrimaryBrush(), Margin = new Thickness(0, 0, 0, 10) });
            foreach (var item in PStrList(c, "Items"))   // "Label|State"
            {
                if (item == null) continue;
                string lbl = item, st = "";
                int p = item.IndexOf('|'); if (p < 0) p = item.LastIndexOf(':');
                if (p > 0) { lbl = item.Substring(0, p).Trim(); st = item.Substring(p + 1).Trim(); }
                string col = StateColor(st);
                var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4), LastChildFill = true };
                var dot = new Ellipse { Width = 9, Height = 9, Fill = Brush(col), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
                DockPanel.SetDock(dot, Dock.Left); row.Children.Add(dot);
                var stTxt = new TextBlock { Text = st, Foreground = Brush(col), FontSize = 12, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
                DockPanel.SetDock(stTxt, Dock.Right); row.Children.Add(stTxt);
                row.Children.Add(new TextBlock { Text = lbl, Foreground = Brush("#C7D2FE"), VerticalAlignment = VerticalAlignment.Center });
                v.Children.Add(row);
            }
            return CardShell(c, v);
        }

        private static FrameworkElement BuildTableCard(UIControlJson c)
        {
            var outer = new StackPanel();
            string title = Str(c.Label);
            if (!string.IsNullOrEmpty(title))
                outer.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Foreground = TextPrimaryBrush(), Margin = new Thickness(0, 0, 0, 10) });
            var cols = PStrList(c, "Columns");
            int ncol = Math.Max(cols.Count, 1);
            var grid = new Grid();
            for (int i = 0; i < ncol; i++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            int r = 0;
            if (cols.Count > 0)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                for (int i = 0; i < cols.Count; i++)
                {
                    var t = new TextBlock { Text = cols[i], FontWeight = FontWeights.SemiBold, Foreground = Brush("#94A3B8"), FontSize = 12, Margin = new Thickness(0, 0, 12, 6) };
                    Grid.SetRow(t, 0); Grid.SetColumn(t, i); grid.Children.Add(t);
                }
                r = 1;
            }
            var rows = P(c, "Rows") as System.Collections.IEnumerable;
            if (rows != null && !(rows is string))
                foreach (var rowObj in rows)
                {
                    var cells = rowObj as System.Collections.IEnumerable;
                    if (cells == null || rowObj is string) continue;
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    int ci = 0;
                    foreach (var cell in cells)
                    {
                        if (ci >= ncol) break;
                        var t = new TextBlock { Text = Str(cell) ?? "", Foreground = Brush("#C7D2FE"), FontSize = 13, Margin = new Thickness(0, 0, 12, 5) };
                        Grid.SetRow(t, r); Grid.SetColumn(t, ci); grid.Children.Add(t); ci++;
                    }
                    r++;
                }
            outer.Children.Add(grid);
            return CardShell(c, outer);
        }

        private sealed class ChartSeries { public string Name; public System.Windows.Media.Color Color; public List<double> Values; }
        private static readonly string[] ChartPalette = { "#6366F1", "#34D399", "#38BDF8", "#FBBF24", "#F472B6", "#A78BFA", "#F87171", "#22D3EE" };

        private static FrameworkElement BuildChartCard(UIControlJson c, CanvasControl cc)
        {
            var outer = new StackPanel();
            string title = Str(c.Label);
            var labels = PStrList(c, "Labels");
            string type = (PStr(c, "ChartType") ?? "bar").ToLowerInvariant();
            bool spline = AsBool(P(c, "Spline"));
            double chartH = PDbl(c, "ChartHeight", 120);
            var seriesList = ParseChartSeries(c);

            bool sparkline = type == "sparkline";
            // Title + legend row (legend lists every series; hidden for bare sparklines).
            if (!sparkline && (!string.IsNullOrEmpty(title) || seriesList.Count > 0))
            {
                var head = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 0, 0, 12) };
                var legend = new StackPanel { Orientation = Orientation.Horizontal };
                var legendSource = (type == "donut") ? null : seriesList;
                if (legendSource != null)
                    foreach (var s in legendSource)
                    {
                        if (string.IsNullOrEmpty(s.Name)) continue;
                        var chip = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 0, 0) };
                        chip.Children.Add(new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(2), Background = new System.Windows.Media.SolidColorBrush(s.Color), Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
                        chip.Children.Add(new TextBlock { Text = s.Name, FontSize = 11, Foreground = TextSecondaryBrush(), VerticalAlignment = VerticalAlignment.Center });
                        legend.Children.Add(chip);
                    }
                if (legend.Children.Count > 0) { DockPanel.SetDock(legend, Dock.Right); head.Children.Add(legend); }
                if (!string.IsNullOrEmpty(title)) head.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Foreground = TextPrimaryBrush() });
                outer.Children.Add(head);
            }

            // The body lives in a host so reactive updates (-Bind) can swap it without rebuilding the card.
            var host = new System.Windows.Controls.Decorator();
            Action render = () => host.Child = BuildChartBody(seriesList, labels, type, chartH, spline);
            render();
            outer.Children.Add(host);

            if (cc != null)
            {
                cc.GetValue = () => seriesList.Count > 0 ? (object)seriesList[0].Values : null;
                // Reactive: either a number array (replaces series 0) OR a datasets array (replaces all series).
                cc.SetValue = v =>
                {
                    var ds = ToDatasets(v);
                    if (ds != null && ds.Count > 0) { seriesList.Clear(); seriesList.AddRange(ds); render(); return; }
                    var nv = ToDoubleList(v);
                    if (nv != null) { if (seriesList.Count == 0) seriesList.Add(new ChartSeries { Color = ParseColor(PStr(c, "Foreground") ?? ChartPalette[0]), Values = nv }); else seriesList[0].Values = nv; render(); }
                };
            }
            return CardShell(c, outer);
        }

        /// <summary>Reactive datasets: a collection of {name,color,values} dicts -> multiple series. Null if not that shape.</summary>
        private static List<ChartSeries> ToDatasets(object v)
        {
            var wrap = v as System.Management.Automation.PSObject; if (wrap != null) v = wrap.BaseObject;
            var en = v as System.Collections.IEnumerable; if (en == null || v is string) return null;
            var outp = new List<ChartSeries>(); int idx = 0;
            foreach (var o in en)
            {
                var item = o is System.Management.Automation.PSObject p ? p.BaseObject : o;
                var d = item as System.Collections.IDictionary; if (d == null) return null;   // not a datasets array
                var vals = ToDoubleList(DictVal(d, "values")) ?? new List<double>();
                string col = Str(DictVal(d, "color")); string name = Str(DictVal(d, "name"));
                outp.Add(new ChartSeries { Name = string.IsNullOrEmpty(name) ? null : name, Color = ParseColor(string.IsNullOrEmpty(col) ? ChartPalette[idx % ChartPalette.Length] : col), Values = vals });
                idx++;
            }
            return outp.Count > 0 ? outp : null;
        }

        private static object DictVal(System.Collections.IDictionary d, string key)
        {
            if (d.Contains(key)) return d[key];
            foreach (var k in d.Keys) if (string.Equals(Str(k), key, StringComparison.OrdinalIgnoreCase)) return d[k];
            return null;
        }

        /// <summary>Single series (Labels/Values/Foreground/Series) or multi-series (DatasetsJson = [{name,color,values}]).</summary>
        private static List<ChartSeries> ParseChartSeries(UIControlJson c)
        {
            var outp = new List<ChartSeries>();
            string dj = PStr(c, "DatasetsJson");
            if (!string.IsNullOrWhiteSpace(dj))
            {
                int idx = 0;
                foreach (var o in JsonList(c, "DatasetsJson"))
                {
                    var d = o as IDictionary<string, object>; if (d == null) continue;
                    var vals = new List<double>();
                    object vo; if (d.TryGetValue("values", out vo) && vo is System.Collections.IEnumerable en && !(vo is string))
                        foreach (var x in en) { double dd; if (double.TryParse(Str(x), NumberStyles.Any, CultureInfo.InvariantCulture, out dd)) vals.Add(dd); }
                    object cn; string col = d.TryGetValue("color", out cn) ? Str(cn) : null;
                    object nm; string name = d.TryGetValue("name", out nm) ? Str(nm) : null;
                    outp.Add(new ChartSeries { Name = name, Color = ParseColor(string.IsNullOrEmpty(col) ? ChartPalette[idx % ChartPalette.Length] : col), Values = vals });
                    idx++;
                }
            }
            else
            {
                outp.Add(new ChartSeries { Name = PStr(c, "Series"), Color = ParseColor(PStr(c, "Foreground") ?? ChartPalette[0]), Values = PDblList(c, "Values") });
            }
            return outp;
        }

        private static System.Windows.Media.Color ParseColor(string hex)
        {
            try { return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex); }
            catch { return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6366F1"); }
        }

        private static List<double> ToDoubleList(object v)
        {
            var wrap = v as System.Management.Automation.PSObject; if (wrap != null) v = wrap.BaseObject;
            var en = v as System.Collections.IEnumerable; if (en == null || v is string) return null;
            var list = new List<double>();
            foreach (var o in en) { double d; if (double.TryParse(Str(o is System.Management.Automation.PSObject p ? p.BaseObject : o), NumberStyles.Any, CultureInfo.InvariantCulture, out d)) list.Add(d); }
            return list;
        }

        /// <summary>Builds just the plot body (below the title/legend), dispatching on chart type. Empty -> placeholder.</summary>
        private static FrameworkElement BuildChartBody(List<ChartSeries> series, List<string> labels, string type, double chartH, bool spline)
        {
            bool any = false; foreach (var s in series) if (s.Values != null && s.Values.Count > 0) { any = true; break; }
            if (!any) return ChartEmptyState(chartH);
            if (type == "donut") return BuildDonut(series.Count > 0 ? series[0].Values : new List<double>(), labels, chartH);
            if (type == "sparkline") return BuildSparkline(series.Count > 0 ? series[0] : null, chartH);

            double max = 1; foreach (var s in series) foreach (var v in s.Values) if (v > max) max = v;
            double niceMax = NiceCeil(max);
            const int steps = 4; const double axisW = 34;
            int n = 0; foreach (var s in series) n = Math.Max(n, s.Values.Count); n = Math.Max(n, labels.Count); n = Math.Max(n, 1);

            var body = new StackPanel();
            var plotRow = new DockPanel { Height = chartH, LastChildFill = true };
            var axis = new Grid { Width = axisW };
            DockPanel.SetDock(axis, Dock.Left);
            for (int g = 0; g <= steps; g++)
            {
                double frac = (double)g / steps, y = chartH * frac;
                axis.Children.Add(new TextBlock { Text = FmtNum(niceMax * (1 - frac)), FontSize = 9, Foreground = TextSecondaryBrush(), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, Math.Max(0, y - 7), 7, 0) });
            }
            plotRow.Children.Add(axis);

            var plot = new Grid();
            for (int g = 0; g <= steps; g++)
            {
                double y = chartH * ((double)g / steps);
                plot.Children.Add(new Border { Height = 1, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, y, 0, 0), Background = Brush(g == steps ? "#2A3A5C" : "#1A2540") });
            }
            if (type == "line" || type == "area") plot.Children.Add(BuildLineLayer(series, labels, niceMax, chartH, type == "area", spline));
            else plot.Children.Add(BuildBarsLayer(series, labels, niceMax, chartH, n));
            plotRow.Children.Add(plot);
            body.Children.Add(plotRow);

            if (labels.Count > 0)
            {
                var xrow = new DockPanel { Margin = new Thickness(0, 6, 0, 0), LastChildFill = true };
                var spacer = new Border { Width = axisW }; DockPanel.SetDock(spacer, Dock.Left); xrow.Children.Add(spacer);
                var lr = new Grid();
                for (int i = 0; i < n; i++) lr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                for (int i = 0; i < labels.Count; i++) { var t = new TextBlock { Text = labels[i], FontSize = 11, Foreground = Brush("#94A3B8"), TextAlignment = TextAlignment.Center }; Grid.SetColumn(t, i); lr.Children.Add(t); }
                xrow.Children.Add(lr);
                body.Children.Add(xrow);
            }
            return body;
        }

        private static FrameworkElement ChartEmptyState(double chartH)
        {
            return new Border
            {
                Height = chartH,
                Child = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        new TextBlock { Text = "", FontFamily = IconFont, FontSize = 24, Foreground = TextSecondaryBrush(), HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.6 },
                        new TextBlock { Text = "No data", FontSize = 12, Foreground = TextSecondaryBrush(), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) }
                    }
                }
            };
        }

        /// <summary>Grouped bars (one cluster per label, one bar per series). Value labels shown only for a single series.</summary>
        private static FrameworkElement BuildBarsLayer(List<ChartSeries> series, List<string> labels, double niceMax, double chartH, int n)
        {
            var grid = new Grid();
            for (int i = 0; i < n; i++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            int k = Math.Max(series.Count, 1);
            for (int i = 0; i < n; i++)
            {
                var cluster = new Grid { Margin = new Thickness(4, 0, 4, 0) };
                for (int j = 0; j < k; j++) cluster.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                for (int j = 0; j < k; j++)
                {
                    var s = series[j]; if (s.Values == null || i >= s.Values.Count) continue;
                    double val = s.Values[i]; double h = Math.Max(2, chartH * (val / niceMax));
                    var col = s.Color;
                    var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };
                    if (k == 1) stack.Children.Add(new TextBlock { Text = FmtNum(val), FontSize = 10, Foreground = TextSecondaryBrush(), TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 3) });
                    var grad = new System.Windows.Media.LinearGradientBrush(System.Windows.Media.Color.FromArgb(255, col.R, col.G, col.B), System.Windows.Media.Color.FromArgb(140, col.R, col.G, col.B), new System.Windows.Point(0, 0), new System.Windows.Point(0, 1));
                    var bar = new Border { Height = h, Background = grad, CornerRadius = new CornerRadius(4, 4, 0, 0), Margin = new Thickness(k == 1 ? 3 : 1.5, 0, k == 1 ? 3 : 1.5, 0), Cursor = System.Windows.Input.Cursors.Hand, ToolTip = ((labels.Count > i ? labels[i] + " · " : "") + (string.IsNullOrEmpty(s.Name) ? "" : s.Name + ": ") + FmtNum(val)) };
                    var bcopy = bar;
                    bar.MouseEnter += (a, b) => bcopy.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = col, BlurRadius = 14, ShadowDepth = 0, Opacity = 0.9 };
                    bar.MouseLeave += (a, b) => bcopy.Effect = null;
                    var grow = new System.Windows.Media.Animation.DoubleAnimation(0, h, new System.Windows.Duration(System.TimeSpan.FromMilliseconds(420))) { BeginTime = System.TimeSpan.FromMilliseconds(i * 45 + j * 15), EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut } };
                    bar.BeginAnimation(FrameworkElement.HeightProperty, grow);
                    stack.Children.Add(bar);
                    Grid.SetColumn(stack, j); cluster.Children.Add(stack);
                }
                Grid.SetColumn(cluster, i); grid.Children.Add(cluster);
            }
            return grid;
        }

        /// <summary>Rounds a value up to a "nice" axis maximum (1/2/5 × 10ⁿ) so gridlines land on round numbers.</summary>
        private static double NiceCeil(double x)
        {
            if (x <= 0) return 1;
            double exp = Math.Floor(Math.Log10(x));
            double f = x / Math.Pow(10, exp);
            double nf = f <= 1 ? 1 : f <= 2 ? 2 : f <= 5 ? 5 : 10;
            return nf * Math.Pow(10, exp);
        }

        /// <summary>One or more line/area series on a Canvas that re-lays-out on resize. Supports spline + draw-on.</summary>
        private static FrameworkElement BuildLineLayer(List<ChartSeries> series, List<string> labels, double niceMax, double chartH, bool area, bool spline)
        {
            var canvas = new Canvas { Height = chartH, Background = System.Windows.Media.Brushes.Transparent, ClipToBounds = false };
            System.Windows.SizeChangedEventHandler redraw = (s, e) =>
            {
                canvas.Children.Clear();
                double w = canvas.ActualWidth; if (w <= 0) return;
                foreach (var ser in series)
                {
                    var vals = ser.Values; int n = vals == null ? 0 : vals.Count; if (n == 0) continue;
                    var col = ser.Color;
                    Func<int, System.Windows.Point> pt = idx => new System.Windows.Point(w * (idx + 0.5) / n, chartH * (1 - vals[idx] / niceMax));
                    var pts = new List<System.Windows.Point>(); for (int i = 0; i < n; i++) pts.Add(pt(i));
                    if (area && n >= 1)
                    {
                        var fig = new System.Windows.Media.PathFigure { StartPoint = pts[0] };
                        AddPathSegments(fig, pts, spline);
                        fig.Segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(pts[n - 1].X, chartH), false));
                        fig.Segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(pts[0].X, chartH), false));
                        fig.IsClosed = true;
                        canvas.Children.Add(new System.Windows.Shapes.Path { Data = new System.Windows.Media.PathGeometry(new[] { fig }), Fill = new System.Windows.Media.LinearGradientBrush(System.Windows.Media.Color.FromArgb(120, col.R, col.G, col.B), System.Windows.Media.Color.FromArgb(8, col.R, col.G, col.B), new System.Windows.Point(0, 0), new System.Windows.Point(0, 1)) });
                    }
                    if (n >= 2)
                    {
                        var fig = new System.Windows.Media.PathFigure { StartPoint = pts[0] };
                        AddPathSegments(fig, pts, spline);
                        var path = new System.Windows.Shapes.Path { Data = new System.Windows.Media.PathGeometry(new[] { fig }), Stroke = new System.Windows.Media.SolidColorBrush(col), StrokeThickness = 2.5, StrokeLineJoin = System.Windows.Media.PenLineJoin.Round, StrokeStartLineCap = System.Windows.Media.PenLineCap.Round, StrokeEndLineCap = System.Windows.Media.PenLineCap.Round };
                        canvas.Children.Add(path);
                        // Draw-on: reveal left-to-right via an animated clip.
                        var clip = new System.Windows.Media.RectangleGeometry(new System.Windows.Rect(0, 0, 0, chartH)); path.Clip = clip;
                        clip.BeginAnimation(System.Windows.Media.RectangleGeometry.RectProperty, new System.Windows.Media.Animation.RectAnimation(new System.Windows.Rect(0, 0, 0, chartH), new System.Windows.Rect(0, 0, w, chartH), new System.Windows.Duration(System.TimeSpan.FromMilliseconds(650))) { EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut } });
                    }
                    for (int i = 0; i < n; i++)
                    {
                        var p = pts[i];
                        var dot = new System.Windows.Shapes.Ellipse { Width = 9, Height = 9, Fill = new System.Windows.Media.SolidColorBrush(col), Stroke = Brush("#0A0E1A"), StrokeThickness = 2, Cursor = System.Windows.Input.Cursors.Hand, ToolTip = ((labels.Count > i ? labels[i] + " · " : "") + (string.IsNullOrEmpty(ser.Name) ? "" : ser.Name + ": ") + FmtNum(vals[i])) };
                        Canvas.SetLeft(dot, p.X - 4.5); Canvas.SetTop(dot, p.Y - 4.5);
                        var dcopy = dot;
                        dot.MouseEnter += (a, b) => dcopy.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = col, BlurRadius = 12, ShadowDepth = 0, Opacity = 0.9 };
                        dot.MouseLeave += (a, b) => dcopy.Effect = null;
                        canvas.Children.Add(dot);
                    }
                }
            };
            canvas.SizeChanged += redraw;
            return canvas;
        }

        /// <summary>Appends straight or smooth (Catmull-Rom → Bézier) segments through the points to a figure.</summary>
        private static void AddPathSegments(System.Windows.Media.PathFigure fig, List<System.Windows.Point> p, bool spline)
        {
            int n = p.Count;
            if (!spline || n < 3) { for (int i = 1; i < n; i++) fig.Segments.Add(new System.Windows.Media.LineSegment(p[i], true)); return; }
            for (int i = 0; i < n - 1; i++)
            {
                var p0 = p[i == 0 ? 0 : i - 1]; var p1 = p[i]; var p2 = p[i + 1]; var p3 = p[i + 2 >= n ? n - 1 : i + 2];
                var c1 = new System.Windows.Point(p1.X + (p2.X - p0.X) / 6.0, p1.Y + (p2.Y - p0.Y) / 6.0);
                var c2 = new System.Windows.Point(p2.X - (p3.X - p1.X) / 6.0, p2.Y - (p3.Y - p1.Y) / 6.0);
                fig.Segments.Add(new System.Windows.Media.BezierSegment(c1, c2, p2, true));
            }
        }

        /// <summary>Donut chart: palette-coloured arc segments (with a small gap) + centred total + a value/percent legend.</summary>
        private static FrameworkElement BuildDonut(List<double> values, List<string> labels, double chartH)
        {
            double total = 0; foreach (var v in values) total += v;
            var grid = new Grid { Height = chartH };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(chartH + 8) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Ring + centred total share a Grid cell (a Canvas ignores alignment, so the total can't be centred in it).
            var ringCell = new Grid { Width = chartH, Height = chartH, HorizontalAlignment = HorizontalAlignment.Left };
            var ring = new Canvas { Width = chartH, Height = chartH };
            double cx = chartH / 2, cy = chartH / 2, rOut = chartH / 2 - 2, rIn = rOut * 0.62;
            double gap = values.Count > 1 ? 2.0 : 0.0;   // small gap between segments
            double start = -90;
            for (int i = 0; i < values.Count && total > 0; i++)
            {
                double sweep = 360.0 * values[i] / total;
                var col = ParseColor(ChartPalette[i % ChartPalette.Length]);
                double a0 = start + gap / 2, a1 = start + sweep - gap / 2;
                if (a1 > a0)
                {
                    var seg = DonutSegment(cx, cy, rIn, rOut, a0, a1, col);
                    seg.ToolTip = (labels.Count > i ? labels[i] + ": " : "") + FmtNum(values[i]) + "  (" + (100.0 * values[i] / total).ToString("0") + "%)";
                    seg.Cursor = System.Windows.Input.Cursors.Hand;
                    var scopy = seg; var ccol = col;
                    seg.MouseEnter += (a, b) => scopy.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = ccol, BlurRadius = 16, ShadowDepth = 0, Opacity = 0.95 };
                    seg.MouseLeave += (a, b) => scopy.Effect = null;
                    ring.Children.Add(seg);
                }
                start += sweep;
            }
            var totalBlock = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            totalBlock.Children.Add(new TextBlock { Text = FmtNum(total), FontSize = 22, FontWeight = FontWeights.Bold, Foreground = TextPrimaryBrush(), TextAlignment = TextAlignment.Center });
            totalBlock.Children.Add(new TextBlock { Text = "total", FontSize = 11, Foreground = TextSecondaryBrush(), TextAlignment = TextAlignment.Center });
            ringCell.Children.Add(ring);
            ringCell.Children.Add(totalBlock);
            Grid.SetColumn(ringCell, 0); grid.Children.Add(ringCell);

            // Legend: swatch | name | value | (percent) — column-aligned.
            var leg = new Grid { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(18, 0, 0, 0) };
            leg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            leg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            leg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            leg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (int i = 0; i < values.Count; i++)
            {
                leg.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var sw = new Border { Width = 11, Height = 11, CornerRadius = new CornerRadius(3), Background = new System.Windows.Media.SolidColorBrush(ParseColor(ChartPalette[i % ChartPalette.Length])), Margin = new Thickness(0, 5, 10, 5), VerticalAlignment = VerticalAlignment.Center };
                var nm = new TextBlock { Text = labels.Count > i ? labels[i] : "Item " + (i + 1), FontSize = 12.5, Foreground = TextPrimaryBrush(), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 12, 5) };
                var vl = new TextBlock { Text = FmtNum(values[i]), FontSize = 12.5, FontWeight = FontWeights.SemiBold, Foreground = TextPrimaryBrush(), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 8, 5) };
                var pc = new TextBlock { Text = total > 0 ? (100.0 * values[i] / total).ToString("0") + "%" : "", FontSize = 11.5, Foreground = TextSecondaryBrush(), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
                Grid.SetRow(sw, i); Grid.SetColumn(sw, 0); leg.Children.Add(sw);
                Grid.SetRow(nm, i); Grid.SetColumn(nm, 1); leg.Children.Add(nm);
                Grid.SetRow(vl, i); Grid.SetColumn(vl, 2); leg.Children.Add(vl);
                Grid.SetRow(pc, i); Grid.SetColumn(pc, 3); leg.Children.Add(pc);
            }
            Grid.SetColumn(leg, 1); grid.Children.Add(leg);
            return grid;
        }

        private static System.Windows.Shapes.Path DonutSegment(double cx, double cy, double rIn, double rOut, double startDeg, double endDeg, System.Windows.Media.Color col)
        {
            Func<double, double, System.Windows.Point> pol = (r, deg) => { double a = deg * Math.PI / 180.0; return new System.Windows.Point(cx + r * Math.Cos(a), cy + r * Math.Sin(a)); };
            bool large = (endDeg - startDeg) > 180;
            var fig = new System.Windows.Media.PathFigure { StartPoint = pol(rOut, startDeg) };
            fig.Segments.Add(new System.Windows.Media.ArcSegment(pol(rOut, endDeg), new System.Windows.Size(rOut, rOut), 0, large, System.Windows.Media.SweepDirection.Clockwise, true));
            fig.Segments.Add(new System.Windows.Media.LineSegment(pol(rIn, endDeg), true));
            fig.Segments.Add(new System.Windows.Media.ArcSegment(pol(rIn, startDeg), new System.Windows.Size(rIn, rIn), 0, large, System.Windows.Media.SweepDirection.Counterclockwise, true));
            fig.IsClosed = true;
            return new System.Windows.Shapes.Path { Data = new System.Windows.Media.PathGeometry(new[] { fig }), Fill = new System.Windows.Media.SolidColorBrush(col) };
        }

        /// <summary>Compact sparkline: a smooth area+line with a dot on the last point, no axis/grid/labels.</summary>
        private static FrameworkElement BuildSparkline(ChartSeries ser, double chartH)
        {
            var canvas = new Canvas { Height = chartH, Background = System.Windows.Media.Brushes.Transparent, ClipToBounds = false };
            if (ser == null || ser.Values == null || ser.Values.Count == 0) return canvas;
            var vals = ser.Values; var col = ser.Color;
            double min = double.MaxValue, max = double.MinValue; foreach (var v in vals) { if (v < min) min = v; if (v > max) max = v; }
            double range = max - min; if (range <= 0) range = 1;
            System.Windows.SizeChangedEventHandler redraw = (s, e) =>
            {
                canvas.Children.Clear();
                double w = canvas.ActualWidth; int n = vals.Count; if (w <= 0) return;
                Func<int, System.Windows.Point> pt = idx => new System.Windows.Point(n <= 1 ? w / 2 : w * idx / (n - 1), 4 + (chartH - 8) * (1 - (vals[idx] - min) / range));
                var pts = new List<System.Windows.Point>(); for (int i = 0; i < n; i++) pts.Add(pt(i));
                var afig = new System.Windows.Media.PathFigure { StartPoint = pts[0] }; AddPathSegments(afig, pts, true);
                afig.Segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(pts[n - 1].X, chartH), false));
                afig.Segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(pts[0].X, chartH), false)); afig.IsClosed = true;
                canvas.Children.Add(new System.Windows.Shapes.Path { Data = new System.Windows.Media.PathGeometry(new[] { afig }), Fill = new System.Windows.Media.LinearGradientBrush(System.Windows.Media.Color.FromArgb(90, col.R, col.G, col.B), System.Windows.Media.Color.FromArgb(0, col.R, col.G, col.B), new System.Windows.Point(0, 0), new System.Windows.Point(0, 1)) });
                if (n >= 2) { var lf = new System.Windows.Media.PathFigure { StartPoint = pts[0] }; AddPathSegments(lf, pts, true); canvas.Children.Add(new System.Windows.Shapes.Path { Data = new System.Windows.Media.PathGeometry(new[] { lf }), Stroke = new System.Windows.Media.SolidColorBrush(col), StrokeThickness = 2, StrokeLineJoin = System.Windows.Media.PenLineJoin.Round }); }
                var last = pts[n - 1];
                var dot = new System.Windows.Shapes.Ellipse { Width = 7, Height = 7, Fill = new System.Windows.Media.SolidColorBrush(col) };
                Canvas.SetLeft(dot, last.X - 3.5); Canvas.SetTop(dot, last.Y - 3.5); canvas.Children.Add(dot);
            };
            canvas.SizeChanged += redraw;
            return canvas;
        }

        /// <summary>Application header/toolbar: brand (icon+text) + status pill on the left, nav links in the
        /// centre, action icon buttons on the right. Links/actions fire named Actions via the bridge (onEvent).</summary>
        private static FrameworkElement BuildToolbar(UIControlJson c, Action<string, string> onEvent)
        {
            double barPad = PDbl(c, "BarHeight", 0) > 0 ? Math.Max(4, (PDbl(c, "BarHeight", 0) - 38) / 2 + 4) : 8;
            var host = new Border { Padding = new Thickness(16, barPad, 16, barPad), BorderThickness = new Thickness(0, 0, 0, 1) };
            host.SetResourceReference(Border.BackgroundProperty, "SidebarBackgroundBrush");
            host.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
            var bgOv = PStr(c, "Background");                 // let authors match the toolbar to the app background
            if (!string.IsNullOrEmpty(bgOv)) host.Background = Brush(bgOv);
            double barH = PDbl(c, "BarHeight", 0); if (barH > 0) host.Height = barH;
            var bar = new DockPanel { LastChildFill = true };

            // Left: brand icon + brand text + status pill
            var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            string brandIcon = PStr(c, "BrandIcon");
            if (!string.IsNullOrEmpty(brandIcon)) { var ic = BuildIconElement(brandIcon, 22, null); ic.Margin = new Thickness(0, 0, 10, 0); ic.VerticalAlignment = VerticalAlignment.Center; left.Children.Add(ic); }
            string brand = PStr(c, "Brand");
            if (!string.IsNullOrEmpty(brand)) { var tb = new TextBlock { Text = brand, FontSize = 16, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center }; tb.SetResourceReference(TextBlock.ForegroundProperty, "HeadingForegroundBrush"); left.Children.Add(tb); }
            string status = PStr(c, "Status");
            if (!string.IsNullOrEmpty(status))
            {
                var pill = new Border { CornerRadius = new CornerRadius(12), Padding = new Thickness(12, 3, 12, 3), Margin = new Thickness(16, 0, 0, 0), BorderThickness = new Thickness(1), VerticalAlignment = VerticalAlignment.Center };
                pill.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
                pill.SetResourceReference(Border.BackgroundProperty, "CardBackgroundBrush");
                var pt = new TextBlock { Text = status, FontSize = 11, FontWeight = FontWeights.SemiBold }; pt.SetResourceReference(TextBlock.ForegroundProperty, "SecondaryForegroundBrush");
                pill.Child = pt; left.Children.Add(pill);
            }
            DockPanel.SetDock(left, Dock.Left); bar.Children.Add(left);

            // Right: action icon buttons
            var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            foreach (var o in JsonList(c, "ActionsJson"))
            {
                var d = o as System.Collections.IDictionary; if (d == null) continue;
                string evt = Str(DictGet(d, "evt")), img = Str(DictGet(d, "image")), icon = Str(DictGet(d, "icon")), tip = Str(DictGet(d, "tooltip"));
                FrameworkElement content;
                if (!string.IsNullOrEmpty(img)) { var bs = MakeImageSource(img); content = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(14), ClipToBounds = true, Child = new Image { Source = bs, Stretch = Stretch.UniformToFill } }; }
                else content = BuildIconElement(string.IsNullOrEmpty(icon) ? "dot" : icon, 18, null);
                var btn = new Button { Content = content, Width = 38, Height = 38, Margin = new Thickness(3, 0, 0, 0), Background = System.Windows.Media.Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, ToolTip = string.IsNullOrEmpty(tip) ? null : tip };
                var st = System.Windows.Application.Current != null ? System.Windows.Application.Current.TryFindResource("CanvasIconButtonStyle") as Style : null;
                if (st != null) btn.Style = st;
                if (!string.IsNullOrEmpty(evt) && onEvent != null) { var e2 = evt; btn.Click += (s, e) => onEvent(e2, "Clicked"); }
                right.Children.Add(btn);
            }
            DockPanel.SetDock(right, Dock.Right); bar.Children.Add(right);

            // Centre: nav links (fills)
            var center = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            string active = PStr(c, "Active");
            foreach (var o in JsonList(c, "LinksJson"))
            {
                var d = o as System.Collections.IDictionary; if (d == null) continue;
                string text = Str(DictGet(d, "text")), evt = Str(DictGet(d, "evt"));
                bool isActive = string.Equals(text, active, StringComparison.OrdinalIgnoreCase);
                var txt = new TextBlock { Text = text, FontSize = 14, FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal };
                txt.SetResourceReference(TextBlock.ForegroundProperty, isActive ? "HeadingForegroundBrush" : "SecondaryForegroundBrush");
                var under = new Border { Height = 2, Margin = new Thickness(0, 5, 0, 0), Background = isActive ? null : System.Windows.Media.Brushes.Transparent };
                if (isActive) under.SetResourceReference(Border.BackgroundProperty, "PrimaryBrush");
                var stackL = new StackPanel { Children = { txt, under } };
                var lb = new Button { Content = stackL, Margin = new Thickness(12, 0, 12, 0), Background = System.Windows.Media.Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, Padding = new Thickness(2, 4, 2, 0) };
                var st = System.Windows.Application.Current != null ? System.Windows.Application.Current.TryFindResource("CanvasIconButtonStyle") as Style : null;
                if (st != null) lb.Style = st;
                if (!string.IsNullOrEmpty(evt) && onEvent != null) { var e2 = evt; lb.Click += (s, e) => onEvent(e2, "Clicked"); }
                center.Children.Add(lb);
            }
            bar.Children.Add(center);
            host.Child = bar;
            return host;
        }

        /// <summary>Application status/footer bar: muted left text + right-aligned links (each fires a named Action).</summary>
        private static FrameworkElement BuildFooter(UIControlJson c, Action<string, string> onEvent)
        {
            var host = new Border { Padding = new Thickness(16, 8, 16, 8), BorderThickness = new Thickness(0, 1, 0, 0) };
            host.SetResourceReference(Border.BackgroundProperty, "SidebarBackgroundBrush");
            host.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
            var bar = new DockPanel { LastChildFill = false };
            string leftText = PStr(c, "LeftText");
            if (!string.IsNullOrEmpty(leftText)) { var t = new TextBlock { Text = leftText, FontSize = 11, VerticalAlignment = VerticalAlignment.Center }; t.SetResourceReference(TextBlock.ForegroundProperty, "SecondaryForegroundBrush"); DockPanel.SetDock(t, Dock.Left); bar.Children.Add(t); }
            var right = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var o in JsonList(c, "LinksJson"))
            {
                var d = o as System.Collections.IDictionary; if (d == null) continue;
                string text = Str(DictGet(d, "text")), evt = Str(DictGet(d, "evt"));
                var txt = new TextBlock { Text = text, FontSize = 11, Margin = new Thickness(18, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Cursor = System.Windows.Input.Cursors.Hand };
                txt.SetResourceReference(TextBlock.ForegroundProperty, "SecondaryForegroundBrush");
                if (!string.IsNullOrEmpty(evt) && onEvent != null) { var e2 = evt; txt.MouseLeftButtonUp += (s, e) => onEvent(e2, "Clicked"); }
                right.Children.Add(txt);
            }
            DockPanel.SetDock(right, Dock.Right); bar.Children.Add(right);
            host.Child = bar;
            return host;
        }

        /// <summary>Opens a hyperlink target via the shell, but ONLY for safe schemes (http/https/mailto/file).
        /// Blocks arbitrary schemes so a crafted link can't shell-launch an executable or custom protocol handler.</summary>
        internal static void OpenUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri)) return;
            Uri u;
            if (!Uri.TryCreate(uri.Trim(), UriKind.Absolute, out u)) { Launcher.Services.LoggingService.Warn("Hyperlink blocked (not an absolute URI): " + uri, component: "Canvas"); return; }
            string s = u.Scheme.ToLowerInvariant();
            if (s != "http" && s != "https" && s != "mailto" && s != "file")
            { Launcher.Services.LoggingService.Warn("Hyperlink blocked (disallowed scheme '" + s + "'): " + uri, component: "Canvas"); return; }
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(u.AbsoluteUri) { UseShellExecute = true }); }
            catch (Exception ex) { Launcher.Services.LoggingService.Warn("Hyperlink open failed: " + ex.Message, component: "Canvas"); }
        }

        /// <summary>Formats a chart value: integer when whole, else one decimal.</summary>
        private static string FmtNum(double d)
        {
            return d == Math.Floor(d) ? ((long)d).ToString(CultureInfo.InvariantCulture) : d.ToString("0.0", CultureInfo.InvariantCulture);
        }

        /// <summary>Reads a Properties value as a list of doubles (e.g. chart Values).</summary>
        private static List<double> PDblList(UIControlJson c, string key)
        {
            var list = new List<double>();
            var v = P(c, key);
            var en = v as System.Collections.IEnumerable;
            if (en != null && !(v is string))
                foreach (var o in en) { double d; if (double.TryParse(Str(o), NumberStyles.Any, CultureInfo.InvariantCulture, out d)) list.Add(d); }
            return list;
        }

        /// <summary>Reads a Properties value as a list of strings (e.g. parallel ItemIcons aligned to Choices).</summary>
        private static List<string> PStrList(UIControlJson c, string key)
        {
            var list = new List<string>();
            var v = P(c, key);
            var en = v as System.Collections.IEnumerable;
            if (en != null && !(v is string))
                foreach (var o in en) list.Add(o == null ? null : o.ToString());
            return list;
        }

        // Tag-based selection for icon'd ComboBox/ListBox items (item Content is a row; Tag holds the value).
        private static void SelectByTag(System.Windows.Controls.Primitives.Selector sel, string tag)
        {
            foreach (var o in sel.Items)
            {
                var item = o as ContentControl;
                if (item != null && string.Equals(Str(item.Tag), tag, StringComparison.Ordinal)) { sel.SelectedItem = o; return; }
            }
        }
        private static object SelectedTag(System.Windows.Controls.Primitives.Selector sel)
        {
            var item = sel.SelectedItem as ContentControl;
            return item == null ? null : item.Tag;
        }

        /// <summary>A horizontal [icon][text] row for list/dropdown items with per-item icons.</summary>
        private static StackPanel IconTextRow(string icon, string text)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            if (!string.IsNullOrEmpty(icon))
            {
                var ic = BuildIconElement(icon, 16, null);
                ic.Margin = new Thickness(0, 0, 8, 0);
                sp.Children.Add(ic);
            }
            sp.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
            return sp;
        }

        // ── Layout containers (Stack / Grid / Wrap / Canvas) ───────────────────────
        private static bool IsContainerType(string type)
        {
            switch (type)
            {
                case "Panel": case "Card": case "Group": case "ControlGroup":
                case "Expander": case "CardExpander":
                case "Stack": case "VStack": case "HStack": case "Grid": case "Wrap": case "Dock":
                case "Tabs": case "TabControl":   // children are Tab/TabItem, each wrapped as a tab page
                case "Tab": case "TabItem":       // one tab page: a normal container, header from its Label
                case "Viewbox":                   // single-child container that scales its content to fit
                    return true;
                default: return false;
            }
        }

        /// <summary>Resolves a container's layout: implied by type (Stack/HStack/Grid/Wrap) or its -Layout
        /// property; Panel/Card default to free-form Canvas (absolute X/Y).</summary>
        private static string ContainerLayout(string type, UIControlJson c)
        {
            if (type == "Stack" || type == "VStack") return "VStack";
            if (type == "HStack") return "HStack";
            if (type == "Grid") return "Grid";
            if (type == "Wrap") return "Wrap";
            if (type == "Dock") return "Dock";
            string l = PStr(c, "Layout");
            return string.IsNullOrEmpty(l) ? "Canvas" : l.Trim();
        }

        private static Panel CreateLayoutPanel(UIControlJson c, string layout)
        {
            switch ((layout ?? "Canvas").ToLowerInvariant())
            {
                case "vstack": case "stack": case "vertical": return new StackPanel { Orientation = Orientation.Vertical };
                case "hstack": case "horizontal": return new StackPanel { Orientation = Orientation.Horizontal };
                case "wrap": return new WrapPanel();
                case "dock": return new DockPanel { LastChildFill = true };
                case "grid":
                    {
                        var g = new Grid();
                        string cw = PStr(c, "ColumnWidths");   // e.g. "Auto,*,Auto" or "40,*,120" or "*,2*"
                        if (!string.IsNullOrEmpty(cw))
                            foreach (var spec in cw.Split(','))
                                g.ColumnDefinitions.Add(new ColumnDefinition { Width = ParseGridLength(spec.Trim()) });
                        else
                        {
                            int cols = (int)PDbl(c, "Columns", 2); if (cols < 1) cols = 1;
                            for (int i = 0; i < cols; i++) g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        }
                        return g;
                    }
                default: return new Canvas();
            }
        }

        private static GridLength ParseGridLength(string s)
        {
            if (string.IsNullOrEmpty(s) || string.Equals(s, "Auto", StringComparison.OrdinalIgnoreCase)) return GridLength.Auto;
            if (s == "*") return new GridLength(1, GridUnitType.Star);
            if (s.EndsWith("*"))
            {
                double f;
                return double.TryParse(s.Substring(0, s.Length - 1), NumberStyles.Any, CultureInfo.InvariantCulture, out f)
                    ? new GridLength(f, GridUnitType.Star) : new GridLength(1, GridUnitType.Star);
            }
            double px;
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out px) ? new GridLength(px) : GridLength.Auto;
        }

        /// <summary>Positions a child within its container's layout panel (Grid row/col, Stack/Wrap spacing).</summary>
        private static void ApplyChildLayout(Panel inner, FrameworkElement el, UIControlJson container, UIControlJson child, string layout, ref int idx, int cols)
        {
            double spacing = PDbl(container, "Spacing", 0);

            var grid = inner as Grid;
            if (grid != null)
            {
                int ncols = grid.ColumnDefinitions.Count > 0 ? grid.ColumnDefinitions.Count : cols;

                int row, col;
                int rp = ChildInt(child, "Row", -1), cp = ChildInt(child, "Column", -1);
                if (rp >= 0 || cp >= 0) { row = rp < 0 ? 0 : rp; col = cp < 0 ? 0 : cp; }
                else { row = idx / ncols; col = idx % ncols; }
                while (grid.RowDefinitions.Count <= row) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Grid.SetRow(el, row); Grid.SetColumn(el, col);
                int cspan = ChildInt(child, "ColumnSpan", 1); if (cspan > 1) Grid.SetColumnSpan(el, cspan);
                int rspan = ChildInt(child, "RowSpan", 1); if (rspan > 1) Grid.SetRowSpan(el, rspan);
                if (spacing > 0 && el.ReadLocalValue(FrameworkElement.MarginProperty) == DependencyProperty.UnsetValue)
                    el.Margin = new Thickness(spacing / 2);
                idx++;
            }
            else if (inner is DockPanel)
            {
                // Child "Dock" property pins it to an edge; the last child fills (LastChildFill).
                string dock = PStr(child, "Dock");
                if (!string.IsNullOrEmpty(dock))
                {
                    switch (dock.Trim().ToLowerInvariant())
                    {
                        case "left": DockPanel.SetDock(el, Dock.Left); break;
                        case "right": DockPanel.SetDock(el, Dock.Right); break;
                        case "top": DockPanel.SetDock(el, Dock.Top); break;
                        case "bottom": DockPanel.SetDock(el, Dock.Bottom); break;
                    }
                }
                if (spacing > 0 && el.ReadLocalValue(FrameworkElement.MarginProperty) == DependencyProperty.UnsetValue)
                    el.Margin = new Thickness(spacing / 2);
                idx++;
            }
            else if (inner is StackPanel || inner is WrapPanel)
            {
                bool horizontal = (inner is StackPanel sp && sp.Orientation == Orientation.Horizontal) || inner is WrapPanel;
                if (spacing > 0 && el.ReadLocalValue(FrameworkElement.MarginProperty) == DependencyProperty.UnsetValue)
                    el.Margin = horizontal ? new Thickness(0, 0, spacing, 0) : new Thickness(0, 0, 0, spacing);
                idx++;
            }
            // Canvas layout: children keep their own X/Y (already applied in Position()).
        }

        private static int ChildInt(UIControlJson c, string key, int dflt)
        {
            object v = P(c, key);
            if (v == null) return dflt;
            int r;
            return int.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out r) ? r : dflt;
        }

        /// <summary>Sortable DataGrid. -Columns sets explicit text columns (else auto-generate); -Items seeds rows.
        /// Populate/refresh live with Set-UICanvasProperty Name ItemsSource $rows. Selection via Get-UICanvasValue.</summary>
        private static FrameworkElement BuildDataGrid(UIControlJson c, CanvasControl cc, Action<string, string> onEvent)
        {
            var dg = new DataGrid
            {
                IsReadOnly = true,
                AutoGenerateColumns = true,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.None,   // modern: no cell grid lines
                CanUserSortColumns = true,
                CanUserResizeRows = false,
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                RowBackground = System.Windows.Media.Brushes.Transparent,
                AlternatingRowBackground = Brush("#0F1730"),
                RowHeight = 40,
                ColumnHeaderHeight = 42,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                FontSize = 13
            };
            dg.SetResourceReference(Control.ForegroundProperty, "BodyForegroundBrush");

            // Header: flat, uppercase-feel via colour/weight, single bottom rule, no separators.
            var hdr = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            hdr.Setters.Add(new Setter(Control.BackgroundProperty, System.Windows.Media.Brushes.Transparent));
            hdr.Setters.Add(new Setter(Control.ForegroundProperty, TextSecondaryBrush()));
            hdr.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
            hdr.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
            hdr.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 0, 12, 0)));
            hdr.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            hdr.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
            hdr.Setters.Add(new Setter(Control.BorderBrushProperty, CardBorder()));
            dg.ColumnHeaderStyle = hdr;

            // Rows: subtle hover, accent-tinted selection, thin separator, no cell borders.
            var rowStyle = new Style(typeof(System.Windows.Controls.DataGridRow));
            var hov = new Trigger { Property = System.Windows.Controls.DataGridRow.IsMouseOverProperty, Value = true };
            hov.Setters.Add(new Setter(System.Windows.Controls.DataGridRow.BackgroundProperty, Brush("#16203A")));
            rowStyle.Triggers.Add(hov);
            dg.RowStyle = rowStyle;

            var cell = new Style(typeof(System.Windows.Controls.DataGridCell));
            cell.Setters.Add(new Setter(System.Windows.Controls.DataGridCell.BorderThicknessProperty, new Thickness(0)));
            cell.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 0, 12, 0)));
            cell.Setters.Add(new Setter(Control.ForegroundProperty, TextPrimaryBrush()));
            cell.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            var selT = new Trigger { Property = System.Windows.Controls.DataGridCell.IsSelectedProperty, Value = true };
            selT.Setters.Add(new Setter(System.Windows.Controls.DataGridCell.BackgroundProperty, Brush("#2A2F66")));
            selT.Setters.Add(new Setter(Control.ForegroundProperty, System.Windows.Media.Brushes.White));
            selT.Setters.Add(new Setter(System.Windows.Controls.DataGridCell.BorderBrushProperty, System.Windows.Media.Brushes.Transparent));
            cell.Triggers.Add(selT);
            dg.CellStyle = cell;

            var cols = PStrList(c, "Columns");
            if (cols.Count > 0)
            {
                dg.AutoGenerateColumns = false;
                foreach (var col in cols)
                    dg.Columns.Add(new DataGridTextColumn { Header = col, Binding = new System.Windows.Data.Binding(col), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            }
            var items = ItemsToView(JsonList(c, "ItemsJson")) as System.Collections.IEnumerable;
            if (items != null) dg.ItemsSource = items;

            if (cc != null) { cc.GetValue = () => dg.SelectedItem; cc.SetValue = v => dg.SelectedItem = v; }

            // Selecting a row raises ValueChanged, so an -OnChange script can drive a master/detail pane.
            // Read the row with Get-UICanvasValue; its columns are accessible by name (e.g. $row.Task).
            if (!string.IsNullOrEmpty(c.Name) && onEvent != null)
            {
                var gname = c.Name;
                dg.SelectionChanged += (s, e) => { if (e.OriginalSource == dg) onEvent(gname, "ValueChanged"); };
            }

            // Wrap with a centred "No data" overlay shown whenever the grid has no rows (incl. reactive -BindItems).
            var host = new Grid();
            host.Children.Add(dg);
            var empty = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
                Children =
                {
                    new TextBlock { Text = "", FontFamily = IconFont, FontSize = 22, Foreground = TextSecondaryBrush(), HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.6 },
                    new TextBlock { Text = "No data", FontSize = 12, Foreground = TextSecondaryBrush(), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) }
                }
            };
            host.Children.Add(empty);
            System.Action sync = () => empty.Visibility = dg.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            sync();
            ((System.Collections.Specialized.INotifyCollectionChanged)dg.Items).CollectionChanged += (s, e) => sync();
            return host;
        }

        /// <summary>TreeView from nested nodes: @{ Text='x'; Children=@(...) }. Selection text via Get-UICanvasValue.</summary>
        private static FrameworkElement BuildTreeView(UIControlJson c, CanvasControl cc)
        {
            var tv = new TreeView { BorderThickness = new Thickness(0) };
            tv.SetResourceReference(Control.BackgroundProperty, "CardBackgroundBrush");
            tv.SetResourceReference(Control.ForegroundProperty, "BodyForegroundBrush");
            foreach (var n in JsonList(c, "NodesJson"))
                tv.Items.Add(BuildTreeNode(n));
            if (cc != null)
            {
                cc.GetValue = () => { var it = tv.SelectedItem as TreeViewItem; return it != null ? it.Header : null; };
                cc.SetValue = v => { };
            }
            return tv;
        }

        private static TreeViewItem BuildTreeNode(object node)
        {
            var d = node as System.Collections.IDictionary;
            string text = d != null ? Str(DictGet(d, "Text") ?? DictGet(d, "Header") ?? DictGet(d, "Name")) : Str(node);
            var tvi = new TreeViewItem { Header = text, IsExpanded = d != null && AsBool(DictGet(d, "Expanded")) };
            if (d != null)
            {
                var kids = DictGet(d, "Children") as System.Collections.IEnumerable;
                if (kids != null) foreach (var k in kids) tvi.Items.Add(BuildTreeNode(k));
            }
            return tvi;
        }

        /// <summary>Auto-suggest = a themed TextBox + a filtering Popup list (the default editable ComboBox's
        /// white chrome can't be reliably re-themed). Value is the current text; click a suggestion to fill.</summary>
        private static FrameworkElement BuildAutoSuggest(UIControlJson c, string name, Action<string, string> onEvent, CanvasControl cc)
        {
            var choices = new System.Collections.Generic.List<string>();
            if (c.Choices != null) foreach (var ch in c.Choices) choices.Add(Str(ch));

            var host = new Grid();

            var tb = new TextBox { Text = Str(c.Default) ?? "", Padding = new Thickness(8, 6, 34, 6), MinHeight = 30, BorderThickness = new Thickness(1) };
            tb.SetResourceReference(Control.BackgroundProperty, "TextBoxBackgroundBrush");
            tb.SetResourceReference(Control.ForegroundProperty, "BodyForegroundBrush");
            tb.SetResourceReference(Control.BorderBrushProperty, "BorderBrush");
            tb.CaretBrush = TextPrimaryBrush();

            // Dropdown chevron overlaid on the right edge — opens the full list without typing.
            var chev = new System.Windows.Controls.Primitives.ToggleButton
            {
                Content = "\uE70D",
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 10,
                Width = 30,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Foreground = TextSecondaryBrush(),
                Focusable = false
            };

            var list = new ListBox { MaxHeight = 220, BorderThickness = new Thickness(1) };
            list.SetResourceReference(Control.ForegroundProperty, "BodyForegroundBrush");
            list.Background = CardBg(c);
            list.BorderBrush = CardBorder();

            var popup = new System.Windows.Controls.Primitives.Popup
            {
                PlacementTarget = host,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                Child = list
            };
            popup.Closed += (s, e) => chev.IsChecked = false;

            Action<bool> show = all =>
            {
                string q = all ? "" : (tb.Text ?? "");
                list.Items.Clear();
                foreach (var ch in choices)
                    if (q.Length == 0 || ch.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) list.Items.Add(ch);
                popup.Width = host.ActualWidth > 0 ? host.ActualWidth : 220;
                popup.IsOpen = list.Items.Count > 0;
            };
            bool selecting = false;
            tb.TextChanged += (s, e) => { if (!selecting && tb.IsKeyboardFocusWithin) show(false); };
            tb.GotKeyboardFocus += (s, e) => show(false);
            tb.LostKeyboardFocus += (s, e) => tb.Dispatcher.BeginInvoke((Action)(() => { if (!list.IsKeyboardFocusWithin && chev.IsChecked != true) popup.IsOpen = false; }));
            chev.Click += (s, e) => { if (chev.IsChecked == true) show(true); else popup.IsOpen = false; };
            list.PreviewMouseLeftButtonUp += (s, e) =>
            {
                if (list.SelectedItem != null)
                {
                    selecting = true; tb.Text = Str(list.SelectedItem); selecting = false;
                    tb.CaretIndex = tb.Text.Length; popup.IsOpen = false; tb.Focus();
                    if (onEvent != null) onEvent(name, "ValueChanged");
                }
            };

            if (cc != null) { cc.GetValue = () => tb.Text; cc.SetValue = v => { selecting = true; tb.Text = Str(v) ?? ""; selecting = false; }; }

            host.Children.Add(tb);
            host.Children.Add(chev);
            host.Children.Add(popup);   // popups take no layout space; render as an overlay
            return host;
        }

        /// <summary>Lightweight Markdown viewer (headings, bold/italic/code, bullets, links) → FlowDocument.</summary>
        private static FrameworkElement BuildMarkdown(UIControlJson c)
        {
            string md = PStr(c, "Text");
            string path = PStr(c, "Path");
            if (string.IsNullOrEmpty(md) && !string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            { try { md = System.IO.File.ReadAllText(path); } catch { } }
            var viewer = new FlowDocumentScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Document = MarkdownToFlow(md ?? "") };
            viewer.SetResourceReference(Control.ForegroundProperty, "BodyForegroundBrush");
            viewer.Background = System.Windows.Media.Brushes.Transparent;
            return viewer;
        }

        private static System.Windows.Documents.FlowDocument MarkdownToFlow(string md)
        {
            var doc = new System.Windows.Documents.FlowDocument { FontFamily = new FontFamily("Segoe UI"), FontSize = 13, PagePadding = new Thickness(4) };
            doc.SetResourceReference(System.Windows.Documents.FlowDocument.ForegroundProperty, "BodyForegroundBrush");
            var lines = (md ?? "").Replace("\r\n", "\n").Split('\n');
            System.Windows.Documents.List bullets = null, ordered = null;
            int i = 0;
            while (i < lines.Length)
            {
                string line = lines[i].TrimEnd();

                // ``` fenced code block ``` — consume through the closing fence.
                if (line.TrimStart().StartsWith("```"))
                {
                    var sb = new System.Text.StringBuilder(); i++;
                    while (i < lines.Length && !lines[i].TrimStart().StartsWith("```")) { sb.AppendLine(lines[i]); i++; }
                    i++;   // skip closing fence
                    bullets = ordered = null;
                    doc.Blocks.Add(MdCodeBlock(sb.ToString().TrimEnd('\n', '\r')));
                    continue;
                }
                // unordered list "- " / "* "
                if (line.StartsWith("- ") || line.StartsWith("* "))
                {
                    ordered = null;
                    if (bullets == null) { bullets = new System.Windows.Documents.List { MarkerStyle = System.Windows.TextMarkerStyle.Disc, Margin = new Thickness(0, 2, 0, 6) }; doc.Blocks.Add(bullets); }
                    var li = new System.Windows.Documents.ListItem(); var p = new System.Windows.Documents.Paragraph(); MdInline(p, line.Substring(2)); li.Blocks.Add(p); bullets.ListItems.Add(li);
                    i++; continue;
                }
                // ordered list "1. "
                var om = System.Text.RegularExpressions.Regex.Match(line, @"^(\d+)\.\s+(.*)$");
                if (om.Success)
                {
                    bullets = null;
                    if (ordered == null) { ordered = new System.Windows.Documents.List { MarkerStyle = System.Windows.TextMarkerStyle.Decimal, Margin = new Thickness(0, 2, 0, 6) }; doc.Blocks.Add(ordered); }
                    var li = new System.Windows.Documents.ListItem(); var p = new System.Windows.Documents.Paragraph(); MdInline(p, om.Groups[2].Value); li.Blocks.Add(p); ordered.ListItems.Add(li);
                    i++; continue;
                }
                bullets = ordered = null;
                if (string.IsNullOrWhiteSpace(line)) { i++; continue; }

                if (line == "---" || line == "***" || line == "___") { doc.Blocks.Add(MdHr()); }
                else if (line.StartsWith("> ")) { doc.Blocks.Add(MdQuote(line.Substring(2))); }
                else if (line.StartsWith("### ")) { doc.Blocks.Add(MdHead(line.Substring(4), 15)); }
                else if (line.StartsWith("## ")) { doc.Blocks.Add(MdHead(line.Substring(3), 18)); }
                else if (line.StartsWith("# ")) { doc.Blocks.Add(MdHead(line.Substring(2), 22)); }
                else { var p = new System.Windows.Documents.Paragraph { Margin = new Thickness(0, 0, 0, 6) }; MdInline(p, line); doc.Blocks.Add(p); }
                i++;
            }
            return doc;
        }

        private static System.Windows.Documents.Paragraph MdHead(string text, double size)
        {
            var p = new System.Windows.Documents.Paragraph { FontSize = size, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 8, 0, 4) };
            MdInline(p, text); return p;
        }

        private static System.Windows.Documents.Block MdCodeBlock(string code)
        {
            var p = new System.Windows.Documents.Paragraph(new Run(code))
            { FontFamily = new FontFamily("Cascadia Mono, Consolas"), FontSize = 12.5, Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 4, 0, 8) };
            p.SetResourceReference(System.Windows.Documents.Paragraph.BackgroundProperty, "TextBoxBackgroundBrush");
            return p;
        }

        private static System.Windows.Documents.Block MdHr()
        {
            return new System.Windows.Documents.BlockUIContainer(new Border { Height = 1, Margin = new Thickness(0, 8, 0, 8), Background = CardBorder() });
        }

        private static System.Windows.Documents.Block MdQuote(string text)
        {
            var p = new System.Windows.Documents.Paragraph
            { Margin = new Thickness(0, 2, 0, 8), Padding = new Thickness(12, 4, 4, 4), BorderThickness = new Thickness(3, 0, 0, 0), BorderBrush = Brush("#6366F1"), Foreground = TextSecondaryBrush() };
            MdInline(p, text); return p;
        }

        // One pass over inline markdown: ![img](src), [link](url), **bold**, `code`, *italic*. Emits plain Runs
        // for the gaps between matches.
        private static readonly System.Text.RegularExpressions.Regex MdInlineRx = new System.Text.RegularExpressions.Regex(
            @"(?<img>!\[(?<alt>[^\]]*)\]\((?<src>[^)\s]+)\))" +
            @"|(?<link>\[(?<ltext>[^\]]+)\]\((?<url>[^)\s]+)\))" +
            @"|(?<bold>\*\*(?<btext>.+?)\*\*)" +
            @"|(?<code>`(?<ctext>[^`]+)`)" +
            @"|(?<ital>\*(?<itext>[^*]+)\*)",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        private static void MdInline(System.Windows.Documents.Paragraph p, string text)
        {
            int pos = 0;
            foreach (System.Text.RegularExpressions.Match m in MdInlineRx.Matches(text))
            {
                if (m.Index > pos) p.Inlines.Add(new System.Windows.Documents.Run(text.Substring(pos, m.Index - pos)));
                if (m.Groups["img"].Success)
                {
                    var bs = MakeImageSource(m.Groups["src"].Value);
                    if (bs != null)
                    {
                        // DownOnly: render at natural size, only shrinking images larger than the caps (never upscale).
                        var im = new Image { Source = bs, Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, MaxHeight = 220, MaxWidth = 360, Margin = new Thickness(0, 2, 0, 2) };
                        string alt = m.Groups["alt"].Value; if (!string.IsNullOrEmpty(alt)) im.ToolTip = alt;
                        p.Inlines.Add(new System.Windows.Documents.InlineUIContainer(im));
                    }
                    else p.Inlines.Add(new System.Windows.Documents.Run(m.Groups["alt"].Value));   // fallback to alt text
                }
                else if (m.Groups["link"].Success)
                {
                    string url = m.Groups["url"].Value;
                    var h = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run(m.Groups["ltext"].Value));
                    try { h.NavigateUri = new Uri(url, UriKind.RelativeOrAbsolute); } catch { }
                    h.RequestNavigate += (s, e) => { OpenUri(url); e.Handled = true; };
                    p.Inlines.Add(h);
                }
                else if (m.Groups["bold"].Success) p.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run(m.Groups["btext"].Value)));
                else if (m.Groups["code"].Success) p.Inlines.Add(new System.Windows.Documents.Run(m.Groups["ctext"].Value) { FontFamily = new FontFamily("Cascadia Mono, Consolas") });
                else if (m.Groups["ital"].Success) p.Inlines.Add(new System.Windows.Documents.Italic(new System.Windows.Documents.Run(m.Groups["itext"].Value)));
                pos = m.Index + m.Length;
            }
            if (pos < text.Length) p.Inlines.Add(new System.Windows.Documents.Run(text.Substring(pos)));
        }

        /// <summary>Parses a JSON-string Properties value (e.g. DataGrid ItemsJson / TreeView NodesJson) into a
        /// list of nested Dictionary&lt;string,object&gt;/object[] — DataContractJsonSerializer can't round-trip
        /// arbitrary nested objects in an object-typed dict, so complex data is passed as a JSON string instead.</summary>
        /// <summary>Builds a classic menu bar from ItemsJson: [{text, evt, items:[...]}]. Items nest to any depth;
        /// a leaf with an 'evt' fires that hidden Event control's action, and text "-" renders a separator.</summary>
        private static FrameworkElement BuildMenu(UIControlJson c, Action<string, string> onEvent)
        {
            var menu = new Menu { Background = Brush(PStr(c, "Background")) ?? System.Windows.Media.Brushes.Transparent };
            foreach (var o in JsonList(c, "ItemsJson"))
            {
                var mi = BuildMenuItem(o, onEvent);
                if (mi != null) menu.Items.Add(mi);
            }
            return menu;
        }

        /// <summary>One menu entry (recursive). Returns a Separator for "-", otherwise a MenuItem.</summary>
        private static Control BuildMenuItem(object node, Action<string, string> onEvent)
        {
            var d = node as System.Collections.IDictionary;
            if (d == null) return null;
            string text = Str(DictGet(d, "text"));
            if (text == "-" || text == "---") return new Separator();

            var item = new MenuItem { Header = text ?? "" };
            string icon = Str(DictGet(d, "icon"));
            if (!string.IsNullOrEmpty(icon)) item.Icon = BuildIconElement(icon, 14, null);
            string gesture = Str(DictGet(d, "gesture"));
            if (!string.IsNullOrEmpty(gesture)) item.InputGestureText = gesture;
            object dis = DictGet(d, "disabled");
            if (dis != null && AsBool(dis)) item.IsEnabled = false;
            object chk = DictGet(d, "checked");
            if (chk != null) { item.IsCheckable = true; item.IsChecked = AsBool(chk); }

            // Children first: a parent with sub-items opens a submenu instead of firing.
            var kids = DictGet(d, "items") as System.Collections.IEnumerable;
            if (kids != null && !(kids is string))
                foreach (var k in kids)
                {
                    var sub = BuildMenuItem(k, onEvent);
                    if (sub != null) item.Items.Add(sub);
                }

            string evt = Str(DictGet(d, "evt"));
            if (item.Items.Count == 0 && !string.IsNullOrEmpty(evt) && onEvent != null)
            {
                var e2 = evt;
                item.Click += (s, e) => onEvent(e2, "Clicked");
            }
            return item;
        }

        private static System.Collections.Generic.List<object> JsonList(UIControlJson c, string key)
        {
            var outp = new System.Collections.Generic.List<object>();
            try
            {
                string json = PStr(c, key);
                if (string.IsNullOrWhiteSpace(json)) return outp;
                var parsed = MiniJson.Parse(json);
                if (parsed is System.Collections.IEnumerable en && !(parsed is string) && !(parsed is System.Collections.IDictionary))
                    foreach (var o in en) outp.Add(o);
                else if (parsed != null) outp.Add(parsed);
            }
            catch { }
            return outp;
        }

        /// <summary>Tiny self-contained JSON parser (System.Web.Extensions/JavaScriptSerializer pulls in System.Web,
        /// which the dotnet-SDK net48 build can't resolve — MC1000 in the XAML compiler). Objects → case-insensitive
        /// Dictionary&lt;string,object&gt;, arrays → List&lt;object&gt;, numbers → double.</summary>
        internal static class MiniJson
        {
            private const int MaxDepth = 64;   // guard against stack overflow from deeply nested / runaway JSON
            public static object Parse(string s) { int i = 0; return Val(s, ref i, 0); }
            private static object Val(string s, ref int i, int depth)
            {
                if (depth > MaxDepth) throw new FormatException("JSON nesting too deep");
                Ws(s, ref i); char c = s[i];
                if (c == '{') return Obj(s, ref i, depth);
                if (c == '[') return Arr(s, ref i, depth);
                if (c == '"') return Str(s, ref i);
                if (c == 't') { i += 4; return true; }
                if (c == 'f') { i += 5; return false; }
                if (c == 'n') { i += 4; return null; }
                return Num(s, ref i);
            }
            private static System.Collections.Generic.Dictionary<string, object> Obj(string s, ref int i, int depth)
            {
                var d = new System.Collections.Generic.Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                i++; Ws(s, ref i); if (s[i] == '}') { i++; return d; }
                while (true) { Ws(s, ref i); string k = Str(s, ref i); Ws(s, ref i); i++; /* ':' */ d[k] = Val(s, ref i, depth + 1); Ws(s, ref i); if (s[i] == ',') { i++; continue; } i++; break; }
                return d;
            }
            private static System.Collections.Generic.List<object> Arr(string s, ref int i, int depth)
            {
                var a = new System.Collections.Generic.List<object>();
                i++; Ws(s, ref i); if (s[i] == ']') { i++; return a; }
                while (true) { a.Add(Val(s, ref i, depth + 1)); Ws(s, ref i); if (s[i] == ',') { i++; continue; } i++; break; }
                return a;
            }
            private static string Str(string s, ref int i)
            {
                var sb = new System.Text.StringBuilder(); i++;
                while (s[i] != '"')
                {
                    char c = s[i++];
                    if (c == '\\')
                    {
                        char e = s[i++];
                        switch (e) { case 'n': sb.Append('\n'); break; case 't': sb.Append('\t'); break; case 'r': sb.Append('\r'); break; case 'b': sb.Append('\b'); break; case 'f': sb.Append('\f'); break; case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break; default: sb.Append(e); break; }
                    }
                    else sb.Append(c);
                }
                i++; return sb.ToString();
            }
            private static object Num(string s, ref int i)
            {
                int start = i; while (i < s.Length && "-+.eE0123456789".IndexOf(s[i]) >= 0) i++;
                double d; double.TryParse(s.Substring(start, i - start), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d); return d;
            }
            private static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
        }

        private static object DictGet(System.Collections.IDictionary d, string key)
        {
            if (d == null) return null;
            foreach (System.Collections.DictionaryEntry e in d) if (string.Equals(Str(e.Key), key, StringComparison.OrdinalIgnoreCase)) return e.Value;
            return null;
        }

        /// <summary>Turns a list of dictionary rows (post-JSON) into a DataView so {Binding Col} resolves.</summary>
        private static object ItemsToView(System.Collections.Generic.List<object> items)
        {
            if (items == null || items.Count == 0) return null;
            var first = items[0] as System.Collections.IDictionary;
            if (first == null) return items;   // already bindable
            var cols = new System.Collections.Generic.List<string>();
            foreach (System.Collections.DictionaryEntry e in first) cols.Add(Str(e.Key));
            var dt = new System.Data.DataTable();
            foreach (var cn in cols) dt.Columns.Add(cn, typeof(object));
            foreach (var it in items)
            {
                var d = it as System.Collections.IDictionary; if (d == null) continue;
                var row = dt.NewRow();
                foreach (var cn in cols) { var cv = DictGet(d, cn); row[cn] = cv ?? (object)System.DBNull.Value; }
                dt.Rows.Add(row);
            }
            return dt.DefaultView;
        }

        /// <summary>Escape hatch: parse raw WPF XAML at runtime and splice it into the canvas. Any x:Name'd
        /// element inside is registered with the bridge (by Name) so Set/Get-UICanvasValue and actions work on
        /// it, letting authors use controls the cmdlet set doesn't cover (DataGrid, TreeView, custom templates).</summary>
        private static FrameworkElement BuildXaml(UIControlJson c, CanvasControl cc, Action<string, string> onEvent)
        {
            string markup = PStr(c, "Markup");
            string path = PStr(c, "Path");
            if (string.IsNullOrEmpty(markup) && !string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                try { markup = System.IO.File.ReadAllText(path); } catch { }
            }
            if (string.IsNullOrWhiteSpace(markup))
                return new TextBlock { Text = "[Xaml: no -Markup or -Path]", Foreground = Brush("#F87171") };

            // Inject the WPF namespaces if the author didn't (so a bare <Grid>…</Grid> parses).
            if (markup.IndexOf("xmlns", StringComparison.OrdinalIgnoreCase) < 0)
            {
                var m = System.Text.RegularExpressions.Regex.Match(markup, @"<\s*([A-Za-z0-9_.:]+)");
                if (m.Success)
                    markup = markup.Insert(m.Index + m.Length,
                        " xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
            }

            FrameworkElement root;
            try { root = System.Windows.Markup.XamlReader.Parse(markup) as FrameworkElement; }
            catch (Exception ex)
            {
                return new TextBlock { Text = "[Xaml parse error] " + ex.Message, Foreground = Brush("#F87171"), TextWrapping = TextWrapping.Wrap };
            }
            if (root == null)
                return new TextBlock { Text = "[Xaml: root is not a FrameworkElement]", Foreground = Brush("#F87171") };

            RegisterNamedXaml(root, cc, onEvent, c.XamlActions);
            return root;
        }

        /// <summary>Walks a parsed XAML logical tree (INCLUDING the root) and registers every x:Name'd element
        /// with the bridge — the named control is often the markup root, so it must be registered too.</summary>
        private static void RegisterNamedXaml(DependencyObject node, CanvasControl parentCc, Action<string, string> onEvent, Dictionary<string, string> xamlActions = null)
        {
            var fe = node as FrameworkElement;
            if (fe != null && !string.IsNullOrEmpty(fe.Name))
            {
                var childDef = new UIControlJson { Name = fe.Name };
                string xaScript;
                if (xamlActions != null && xamlActions.TryGetValue(fe.Name, out xaScript)) childDef.Action = xaScript;
                var childCc = new CanvasControl { Name = fe.Name, Element = fe, Def = childDef };
                WireValue(fe, childCc);
                var bb = fe as System.Windows.Controls.Primitives.ButtonBase;
                if (bb != null && onEvent != null) bb.Click += (s, e) => onEvent(fe.Name, "Clicked");
                parentCc.Children.Add(childCc);
            }
            foreach (var obj in System.Windows.LogicalTreeHelper.GetChildren(node))
            {
                var d = obj as DependencyObject;
                if (d != null) RegisterNamedXaml(d, parentCc, onEvent, xamlActions);
            }
        }

        /// <summary>Password field with a reveal (eye) toggle: a masked PasswordBox + an overlaid TextBox kept in sync.</summary>
        private static FrameworkElement BuildPassword(UIControlJson c, string name, CanvasControl cc)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pb = new PasswordBox { Padding = new Thickness(8, 6, 8, 6) };
            var tb = new TextBox { Padding = new Thickness(8, 6, 8, 6), Visibility = Visibility.Collapsed };
            // Theme both halves explicitly — the top-level element is a Grid, so ApplyThemeDefaults (which only
            // themes a bare TextBox/PasswordBox) doesn't reach them; without this they render with a white default.
            foreach (Control fld in new Control[] { pb, tb })
            {
                fld.SetResourceReference(Control.BackgroundProperty, "TextBoxBackgroundBrush");
                fld.SetResourceReference(Control.ForegroundProperty, "BodyForegroundBrush");
                fld.SetResourceReference(Control.BorderBrushProperty, "BorderBrush");
                fld.MinHeight = 30;
                fld.VerticalContentAlignment = VerticalAlignment.Center;
            }
            Grid.SetColumn(pb, 0); Grid.SetColumn(tb, 0);

            bool syncing = false;
            pb.PasswordChanged += (s, e) => { if (syncing) return; syncing = true; tb.Text = pb.Password; syncing = false; };
            tb.TextChanged += (s, e) => { if (syncing) return; syncing = true; pb.Password = tb.Text; syncing = false; };

            var reveal = new System.Windows.Controls.Primitives.ToggleButton
            {
                Content = "\uE7B3",   // Segoe MDL2 RedEye
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 14,
                Width = 34,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Show / hide",
                Foreground = TextSecondaryBrush()
            };
            Grid.SetColumn(reveal, 1);
            reveal.Checked += (s, e) => { tb.Visibility = Visibility.Visible; pb.Visibility = Visibility.Collapsed; reveal.Foreground = AccentBrush(); };
            reveal.Unchecked += (s, e) => { tb.Visibility = Visibility.Collapsed; pb.Visibility = Visibility.Visible; reveal.Foreground = TextSecondaryBrush(); };

            grid.Children.Add(pb);
            grid.Children.Add(tb);
            grid.Children.Add(reveal);

            if (cc != null)
            {
                cc.GetValue = () => pb.Visibility == Visibility.Visible ? pb.Password : tb.Text;
                cc.SetValue = v => { syncing = true; pb.Password = Str(v) ?? ""; tb.Text = Str(v) ?? ""; syncing = false; };
            }
            return grid;
        }

        /// <summary>Applies the thin, transparent, auto-hiding canvas scrollbar style to a ScrollViewer.</summary>
        /// <summary>Restyles a control's inner scrollbars to the thin, auto-hiding, semi-transparent Canvas style
        /// (so consoles / multi-line inputs don't show the chunky default OS scrollbar).</summary>
        internal static void ApplyThinScrollBars(Control ctl)
        {
            var thin = System.Windows.Application.Current != null ? System.Windows.Application.Current.TryFindResource("CanvasThinScrollBarStyle") as Style : null;
            if (thin != null) ctl.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = thin;
        }

        internal static void ApplyCanvasScroll(ScrollViewer sv)
        {
            var st = System.Windows.Application.Current != null
                ? System.Windows.Application.Current.TryFindResource("CanvasScrollViewerStyle") as Style : null;
            if (st != null) sv.Style = st;
        }

        /// <summary>Wraps a container's child panel in card/panel chrome (background, border, padding).</summary>
        private static FrameworkElement WrapContainer(UIControlJson c, bool card, FrameworkElement inner)
        {
            double pad = PDbl(c, "Padding", card ? 12 : 0);
            // Optional internal scroll: a -Height + Scroll makes a fixed-height scrollable region (e.g. a long step list).
            FrameworkElement content = inner;
            string scroll = PStr(c, "Scroll");
            if (!string.IsNullOrEmpty(scroll) && !scroll.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                bool horiz = scroll.Equals("horizontal", StringComparison.OrdinalIgnoreCase) || scroll.Equals("both", StringComparison.OrdinalIgnoreCase);
                bool vert = !scroll.Equals("horizontal", StringComparison.OrdinalIgnoreCase);
                var sv = new ScrollViewer
                {
                    Content = inner,
                    VerticalScrollBarVisibility = vert ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
                    HorizontalScrollBarVisibility = horiz ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled
                };
                ApplyCanvasScroll(sv);
                content = sv;
            }
            var border = new Border { Child = content, Padding = new Thickness(pad) };
            if (card)
            {
                border.Background = CardBg(c);
                border.BorderBrush = CardBorder();
                border.BorderThickness = new Thickness(1);
                border.CornerRadius = new CornerRadius(PDbl(c, "CornerRadius", 8));
            }
            else if (!string.IsNullOrEmpty(PStr(c, "Background")))
            {
                border.Background = Brush(PStr(c, "Background"));
                border.CornerRadius = new CornerRadius(PDbl(c, "CornerRadius", 0));
            }
            // Background image (ImageBrush behind the content) — something composition can't do.
            var bgImg = PStr(c, "BackgroundImage");
            if (!string.IsNullOrEmpty(bgImg))
            {
                var src = MakeImageSource(bgImg);
                if (src != null)
                {
                    border.Background = new ImageBrush(src)
                    {
                        Stretch = ParseStretch(PStr(c, "BackgroundStretch") ?? "UniformToFill"),
                        Opacity = PDbl(c, "BackgroundOpacity", 1)
                    };
                    if (border.CornerRadius == default(CornerRadius))
                        border.CornerRadius = new CornerRadius(PDbl(c, "CornerRadius", card ? 8 : 0));
                }
            }
            // Hover feedback for interactive cards/rows.
            string hoverBg = PStr(c, "HoverBackground");
            if (!string.IsNullOrEmpty(hoverBg))
            {
                if (border.Background == null) border.Background = Brushes.Transparent;   // so hit-testing fires over empty areas
                var normal = border.Background;
                var hover = Brush(hoverBg);
                border.MouseEnter += (s, e) => border.Background = hover;
                border.MouseLeave += (s, e) => border.Background = normal;
                border.Cursor = System.Windows.Input.Cursors.Hand;
            }
            ApplyHoverScale(c, border);
            return border;
        }

        /// <summary>Optional hover zoom on a card/panel: -Properties @{ HoverScale=1.03; HoverScaleMs=140 }.
        /// Animates a centred ScaleTransform so the card grows in place instead of nudging its neighbours,
        /// and animates back out on leave. Unlike HoverBackground this composes with a runtime Background
        /// swap, because it never captures the brush.</summary>
        private static void ApplyHoverScale(UIControlJson c, FrameworkElement el)
        {
            double scale = PDbl(c, "HoverScale", 0);
            if (el == null || scale <= 0 || Math.Abs(scale - 1.0) < 0.0001) return;

            var st = new ScaleTransform(1.0, 1.0);
            el.RenderTransformOrigin = new Point(0.5, 0.5);
            EnsureTransformGroup(el).Children.Add(st);   // compose, don't clobber Stagger's translate
            var dur = new Duration(TimeSpan.FromMilliseconds(PDbl(c, "HoverScaleMs", 140)));

            Action<double> animate = to =>
            {
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                st.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(to, dur) { EasingFunction = ease });
                st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(to, dur) { EasingFunction = ease });
            };
            el.MouseEnter += (s, e) => animate(scale);
            el.MouseLeave += (s, e) => animate(1.0);

            // A transparent background makes the whole card hit-testable, so the zoom
            // triggers over empty space too and not only on the child text.
            var b = el as Border;
            if (b != null && b.Background == null) b.Background = Brushes.Transparent;
            el.Cursor = System.Windows.Input.Cursors.Hand;
        }

        /// <summary>Wraps a container's child panel in an Expander (header from Label, expanded by default).</summary>
        private static FrameworkElement WrapExpander(UIControlJson c, FrameworkElement inner)
        {
            bool expanded = true;
            object e;
            if (c.Properties != null && c.Properties.TryGetValue("IsExpanded", out e)) expanded = AsBool(e);
            var border = new Border
            {
                Background = CardBg(c),
                BorderBrush = CardBorder(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(PDbl(c, "CornerRadius", 8)),
                Child = inner
            };
            return new Expander
            {
                Header = Str(c.Label) ?? "",
                IsExpanded = expanded,
                Content = border,
                Foreground = Brush("#E2E8F0")
            };
        }

        /// <summary>NumberBox: a TextBox with up/down spinner buttons honoring Minimum/Maximum/Step.</summary>
        private static FrameworkElement BuildNumberBox(UIControlJson c, string name, Action<string, string> onEvent, CanvasControl cc)
        {
            double min = PDbl(c, "Minimum", double.NegativeInfinity);
            double max = PDbl(c, "Maximum", double.PositiveInfinity);
            double step = PDbl(c, "Step", 1);
            if (step <= 0) step = 1;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var box = new TextBox { Text = Str(c.Default) ?? "0", VerticalContentAlignment = VerticalAlignment.Center, MinHeight = 30, Padding = new Thickness(6, 3, 6, 3) };
            box.SetResourceReference(Control.BackgroundProperty, "TextBoxBackgroundBrush");
            box.SetResourceReference(Control.ForegroundProperty, "BodyForegroundBrush");
            box.SetResourceReference(Control.BorderBrushProperty, "BorderBrush");
            Grid.SetColumn(box, 0);
            grid.Children.Add(box);

            var spin = new StackPanel { Orientation = Orientation.Vertical, Width = 20 };
            Grid.SetColumn(spin, 1);
            var up = new RepeatButton { Content = "\u25B2", FontSize = 7, Padding = new Thickness(0) };
            var down = new RepeatButton { Content = "\u25BC", FontSize = 7, Padding = new Thickness(0) };
            spin.Children.Add(up);
            spin.Children.Add(down);
            grid.Children.Add(spin);

            Action<double> nudge = delta =>
            {
                double cur = AsDbl(box.Text, 0) + delta;
                if (cur < min) cur = min;
                if (cur > max) cur = max;
                box.Text = cur.ToString(CultureInfo.InvariantCulture);
            };
            up.Click += (s, e) => nudge(step);
            down.Click += (s, e) => nudge(-step);
            if (onEvent != null) box.TextChanged += (s, e) => onEvent(name, "ValueChanged");

            if (cc != null) { cc.GetValue = () => AsDbl(box.Text, 0); cc.SetValue = v => box.Text = Str(v) ?? "0"; }
            return grid;
        }

        /// <summary>ProgressRing: an indeterminate rotating arc (WinUI has no WPF equivalent).</summary>
        private static FrameworkElement BuildProgressRing(UIControlJson c)
        {
            double size = c.Width > 0 ? c.Width : (c.Height > 0 ? c.Height : 32);
            double thickness = PDbl(c, "Thickness", 3);
            var color = Brush(PStr(c, "Foreground") ?? "#6366F1");

            double r = (size - thickness) / 2;
            double cx = size / 2, cy = size / 2;
            var fig = new PathFigure { StartPoint = new Point(cx, cy - r) };
            fig.Segments.Add(new ArcSegment(new Point(cx + r, cy), new Size(r, r), 0, false, SweepDirection.Clockwise, true));
            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            var arc = new System.Windows.Shapes.Path
            {
                Data = geo, Stroke = color, StrokeThickness = thickness, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round
            };
            var rotate = new RotateTransform(0, cx, cy);
            arc.RenderTransform = rotate;

            var canvas = new Canvas { Width = size, Height = size };
            canvas.Children.Add(arc);

            var anim = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(1))) { RepeatBehavior = RepeatBehavior.Forever };
            arc.Loaded += (s, e) => rotate.BeginAnimation(RotateTransform.AngleProperty, anim);
            return canvas;
        }

        /// <summary>DropDownButton: a button that opens a ContextMenu of Choices; selection fires ValueChanged.</summary>
        private static FrameworkElement BuildDropDownButton(UIControlJson c, string name, Action<string, string> onEvent, CanvasControl cc)
        {
            string selected = Str(c.Default);
            var btn = new Button { Content = (Str(c.Label) ?? selected ?? "Select") + "  \u25BE" };
            var menu = new ContextMenu();
            if (c.Choices != null)
            {
                foreach (var choice in c.Choices)
                {
                    var item = new MenuItem { Header = choice };
                    string ch = choice;
                    item.Click += (s, e) =>
                    {
                        selected = ch;
                        btn.Content = ch + "  \u25BE";
                        if (onEvent != null) onEvent(name, "ValueChanged");
                    };
                    menu.Items.Add(item);
                }
            }
            btn.Click += (s, e) => { menu.PlacementTarget = btn; menu.IsOpen = true; };
            if (cc != null) { cc.GetValue = () => selected; cc.SetValue = v => { selected = Str(v); btn.Content = (selected ?? "Select") + "  \u25BE"; }; }
            return btn;
        }

        /// <summary>Themed single-/multi-line text input with an overlaid placeholder hint (WPF has no
        /// native placeholder). Sets value hooks directly and themes the inner TextBox.</summary>
        private static FrameworkElement BuildTextInput(UIControlJson c, string name, Action<string, string> onEvent, CanvasControl cc, bool multiline)
        {
            var box = new TextBox
            {
                Text = Str(c.Default) ?? "",
                MinHeight = 30,
                Padding = new Thickness(6, 3, 6, 3),
                VerticalContentAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center
            };
            if (multiline) { box.AcceptsReturn = true; box.TextWrapping = TextWrapping.Wrap; box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; }
            box.SetResourceReference(Control.BackgroundProperty, "TextBoxBackgroundBrush");
            box.SetResourceReference(Control.ForegroundProperty, "BodyForegroundBrush");
            box.SetResourceReference(Control.BorderBrushProperty, "BorderBrush");
            if (onEvent != null) box.TextChanged += (s, e) => onEvent(name, "ValueChanged");
            if (cc != null) { cc.GetValue = () => box.Text; cc.SetValue = v => box.Text = Str(v) ?? ""; }

            string placeholder = PStr(c, "Placeholder");
            if (string.IsNullOrEmpty(placeholder)) return box;

            var grid = new Grid();
            grid.Children.Add(box);
            var hint = new TextBlock
            {
                Text = placeholder,
                Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x94, 0xA6)),
                FontStyle = FontStyles.Italic,
                IsHitTestVisible = false,
                Margin = new Thickness(8, 0, 8, 0),
                VerticalAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center
            };
            grid.Children.Add(hint);
            Action sync = () => hint.Visibility = string.IsNullOrEmpty(box.Text) ? Visibility.Visible : Visibility.Collapsed;
            sync();
            box.TextChanged += (s, e) => sync();
            return grid;
        }

        // ── helpers ──────────────────────────────────────────────────────────────
        private static FrameworkElement WireChange(Control ctl, string name, Action<string, string> onEvent)
        {
            if (onEvent != null && ctl is TextBox tb) tb.TextChanged += (s, e) => onEvent(name, "ValueChanged");
            return ctl;
        }

        private static void ApplyText(UIControlJson c, TextBlock t)
        {
            double fs = PDbl(c, "FontSize", 0);
            if (fs > 0) t.FontSize = fs;
            string fw = PStr(c, "FontWeight");
            if (string.Equals(fw, "Bold", StringComparison.OrdinalIgnoreCase)) t.FontWeight = FontWeights.Bold;
            else if (string.Equals(fw, "SemiBold", StringComparison.OrdinalIgnoreCase)) t.FontWeight = FontWeights.SemiBold;
            var fg = Brush(PStr(c, "Foreground"));
            if (fg != null) t.Foreground = fg;
        }

        /// <summary>Walks a built control tree and logs author mistakes the engine would otherwise swallow.
        ///
        /// This is the diagnostic for the framework's worst failure mode: unrecognised or inapplicable
        /// input is not rejected, it is accepted and ignored, so the app renders wrong with a clean log
        /// and exit code 0. Two checks:
        ///
        ///   1. UNREAD keys - a -Properties key nothing in the build path ever asked for. Catches typos
        ///      ('Margins'), and keys that are real for a DIFFERENT control (ImageWidth is read for a
        ///      Banner hero image but not a plain Image, which rendered icons at full bleed).
        ///
        ///   2. INAPPLICABLE keys - read, but meaningless in this position, so read-tracking alone cannot
        ///      see them. Padding in the bag on a stack is the expensive one: it produced pages with the
        ///      correct control count and a blank screen.</summary>
        public static void WarnUnreadProperties(UIControlJson c, Action<string> warn, string path = null,
                                                string parentLayout = null)
        {
            if (c == null || warn == null) return;

            string here = string.IsNullOrEmpty(c.Name)
                ? (string.IsNullOrEmpty(c.Type) ? "?" : c.Type)
                : c.Type + " '" + c.Name + "'";
            string full = string.IsNullOrEmpty(path) ? here : path + " > " + here;

            // A WindowTemplate is a stored blueprint, not a built control: its own Title/Width/Height are
            // read when Show-UICanvasWindow materialises it, long after this walk - so they can never be
            // marked read here. Its CHILDREN are built like anything else, so they are still walked.
            if (string.Equals(c.Type, "WindowTemplate", StringComparison.OrdinalIgnoreCase))
            {
                if (c.Children != null)
                    foreach (var wc in c.Children) WarnUnreadProperties(wc, warn, full, null);
                return;
            }

            string layout = IsContainerType(c.Type) ? ContainerLayout(c.Type, c) : null;


            if (c.Properties != null && c.Properties.Count > 0)
            {
                foreach (var kv in c.Properties)
                {
                    if (IgnoredPropertyKeys.Contains(kv.Key)) continue;

                    // Supplied, but nothing in the build path ever asked for it.
                    if (c.ReadKeys == null || !c.ReadKeys.Contains(kv.Key))
                    {
                        string hint;
                        PropertyHints.TryGetValue(kv.Key, out hint);
                        warn("[unused property] " + full + ": '" + kv.Key + "' was supplied but nothing read it"
                             + (string.IsNullOrEmpty(hint) ? "." : " - " + hint));
                        continue;
                    }

                }
            }

            if (c.Children != null)
                foreach (var child in c.Children)
                    WarnUnreadProperties(child, warn, full, layout);
        }


        /// <summary>Keys consumed outside the factory (module-side or bridge-side), so silence is correct.</summary>
        private static readonly HashSet<string> IgnoredPropertyKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Read by the module or the bridge rather than the factory, so silence is correct here.
                "Markup", "Path", "StateJson", "StateFile", "ContentJson", "ActionsJson",
                "DatasetsJson", "ItemsJson", "LinksJson", "NodesJson",
                "Bind", "BindItems", "Key", "AutoStart", "LockNavigation", "Modal", "Topmost",
                "Resizable", "NoScroll", "Position", "HideTitleBar", "TitleBarColor", "TitleBarText",

                // A refreshing control's tick is wired by CanvasBridge.StartRefresh, which the factory never sees.

                "ValueScript", "RefreshInterval", "ClockStop",
            };

        /// <summary>Specific advice for keys that are real elsewhere, which is why they look right.</summary>
        private static readonly Dictionary<string, string> PropertyHints =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "ImageWidth",  "it is only read for a Banner hero image; size a plain Image with -Width/-Height" },
                { "HoverScale",  "supported on cards/panels; check the control type" },
                { "Stagger",     "supported on containers; check the control type" },
                { "BackgroundImage", "supported on containers; check the control type" },
            };
        private static object P(UIControlJson c, string key)
        {
            // Record every key the factory asks for, so WarnUnreadProperties can report the ones the
            // author supplied that nothing ever looked at.
            if (c.ReadKeys == null) c.ReadKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            c.ReadKeys.Add(key);
            object v;
            if (c.Properties != null && c.Properties.TryGetValue(key, out v)) return v;
            return null;
        }
        // ── Engine-native Workflow control (headless) ──────────────────────────────
        /// <summary>Builds the Type="Workflow" control's WorkflowViewModel + ExpectedSeconds from the serialized step
        /// list, but renders NOTHING: the module (-Engine) renders the visible step cards/header/run button, and the
        /// bridge's StartWorkflow runs a WorkflowExecutor over cc.Workflow and mirrors task state onto those named controls.</summary>
        private static FrameworkElement BuildWorkflow(UIControlJson c, CanvasControl cc)
        {
            var vm = new Launcher.ViewModels.WorkflowViewModel(PStr(c, "Title") ?? "Workflow", null);
            var expected = new List<int>();
            int order = 0;
            foreach (var s in ParseWorkflowSteps(PStr(c, "StepsJson")))
            {
                vm.AddTask(new Launcher.ViewModels.WorkflowTaskViewModel
                {
                    Name = s.Name,
                    Title = s.Name,
                    Description = s.Detail,
                    Order = order++,
                    ScriptBlockString = s.Script,
                    RetryCount = s.Retry,
                    TimeoutSeconds = s.TimeoutSeconds,
                    SkipCondition = s.SkipWhen
                });
                expected.Add(s.ExpectedSeconds);
            }
            cc.Workflow = vm;
            cc.WorkflowExpectedSeconds = expected;
            return new Grid { Width = 0, Height = 0, Visibility = Visibility.Collapsed };   // invisible host
        }

        private static List<Launcher.Models.UIWorkflowStepJson> ParseWorkflowSteps(string json)
        {
            var list = new List<Launcher.Models.UIWorkflowStepJson>();
            if (string.IsNullOrWhiteSpace(json)) return list;
            try
            {
                var settings = new System.Runtime.Serialization.Json.DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };
                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(List<Launcher.Models.UIWorkflowStepJson>), settings);
                using (var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
                {
                    var parsed = ser.ReadObject(ms) as List<Launcher.Models.UIWorkflowStepJson>;
                    if (parsed != null) list = parsed;
                }
            }
            catch { }
            return list;
        }

        private static DataTemplate WorkflowStepTemplate(bool compact)
        {
            int pad = compact ? 8 : 12;
            int nameFs = compact ? 13 : 14;
            string xaml =
                "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">" +
                "<DataTemplate.Resources><BooleanToVisibilityConverter x:Key=\"B2V\"/></DataTemplate.Resources>" +
                "<Border Background=\"#141B2E\" CornerRadius=\"6\" Padding=\"" + pad + "\" Margin=\"0,0,0,6\">" +
                "<Grid><Grid.ColumnDefinitions><ColumnDefinition Width=\"Auto\"/><ColumnDefinition Width=\"*\"/><ColumnDefinition Width=\"Auto\"/></Grid.ColumnDefinitions>" +
                "<TextBlock Grid.Column=\"0\" Text=\"{Binding StatusIcon}\" Foreground=\"{Binding StatusColor}\" FontFamily=\"Segoe MDL2 Assets\" FontSize=\"16\" VerticalAlignment=\"Center\" Margin=\"0,0,10,0\"/>" +
                "<StackPanel Grid.Column=\"1\" VerticalAlignment=\"Center\">" +
                "<TextBlock Text=\"{Binding Name}\" Foreground=\"#E5E7EB\" FontWeight=\"SemiBold\" FontSize=\"" + nameFs + "\"/>" +
                "<TextBlock Text=\"{Binding Description}\" Foreground=\"#94A3B8\" FontSize=\"11\" TextWrapping=\"Wrap\" Visibility=\"{Binding HasDescription, Converter={StaticResource B2V}}\"/>" +
                "<ProgressBar Height=\"3\" Margin=\"0,4,0,0\" Minimum=\"0\" Maximum=\"100\" Value=\"{Binding ProgressPercent, Mode=OneWay}\" IsIndeterminate=\"{Binding IsIndeterminate, Mode=OneWay}\" Visibility=\"{Binding ShowProgressBar, Converter={StaticResource B2V}}\"/>" +
                "</StackPanel>" +
                "<TextBlock Grid.Column=\"2\" Text=\"{Binding StatusText}\" Foreground=\"{Binding StatusColor}\" VerticalAlignment=\"Center\" FontSize=\"12\" Margin=\"8,0,0,0\"/>" +
                "</Grid></Border></DataTemplate>";
            return (DataTemplate)System.Windows.Markup.XamlReader.Parse(xaml);
        }

        private static bool PBoolF(UIControlJson c, string key)
        {
            var s = PStr(c, key);
            return !string.IsNullOrEmpty(s) && (string.Equals(s, "True", StringComparison.OrdinalIgnoreCase) || s == "1");
        }

        private static string PStr(UIControlJson c, string key) { var v = P(c, key); return v == null ? null : v.ToString(); }
        private static double PDbl(UIControlJson c, string key, double dflt) { return AsDbl(P(c, key), dflt); }

        private static string Str(object v) { return v == null ? null : v.ToString(); }
        private static bool AsBool(object v)
        {
            if (v is bool b) return b;
            bool r;
            return v != null && bool.TryParse(v.ToString(), out r) && r;
        }
        private static double AsDbl(object v, double dflt)
        {
            if (v == null) return dflt;
            if (v is double d) return d;
            double p;
            return double.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out p) ? p : dflt;
        }
        private static DateTime? AsDate(object v)
        {
            if (v == null) return null;
            if (v is DateTime dt) return dt;
            DateTime p;
            return DateTime.TryParse(v.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out p) ? p : (DateTime?)null;
        }

        private static Brush Brush(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return null;
            try { return (Brush)new BrushConverter().ConvertFromString(hex.StartsWith("#") ? hex : "#" + hex); }
            catch { return null; }
        }

        /// <summary>Resolves a themed brush from the app's merged Set-UITheme resources (ThemeBuilder keys),
        /// falling back to a hex default so the look is unchanged when no theme override is set.</summary>
        private static Brush ThemeBrush(string resourceKey, string fallbackHex)
        {
            try
            {
                var app = System.Windows.Application.Current;
                if (app != null) { var b = app.TryFindResource(resourceKey) as Brush; if (b != null) return b; }
            }
            catch { }
            return Brush(fallbackHex);
        }

        // Canvas surface/text defaults — themed, with the original dark hex as fallback.
        private static Brush CardBg(UIControlJson c) { return Brush(PStr(c, "Background")) ?? ThemeBrush("CardBackgroundBrush", "#141A2E"); }
        private static Brush CardBorder() { return ThemeBrush("BorderBrush", "#1E2942"); }
        private static Brush TextPrimaryBrush() { return ThemeBrush("BodyForegroundBrush", "#E5E7EB"); }
        private static Brush TextSecondaryBrush() { return ThemeBrush("SecondaryForegroundBrush", "#94A3B8"); }
        private static Brush AccentBrush() { return ThemeBrush("PrimaryBrush", "#6366F1"); }
    }
}
