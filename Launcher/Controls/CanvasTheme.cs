// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Launcher.Controls
{
    /// <summary>Live theme state for canvas controls, so a dark/light switch reaches every control
    /// already on screen - not just the ones built after it.
    ///
    /// The problem it solves: canvas colours used to be resolved ONCE, when a control was built.
    /// Theme defaults went through <c>TryFindResource</c>, which returns the brush in force at that
    /// moment, and author colours (<c>-Background '#0D1420'</c>) were parsed into a fresh brush.
    /// Switching theme replaced the resource dictionary, but nothing held a reference to the new
    /// one, so a card kept its old background until its page happened to be rebuilt.
    ///
    /// Two kinds of live brush, both shared and recoloured IN PLACE on a theme change:
    ///   * Theme brushes, one per resource key (CardBackgroundBrush, BodyForegroundBrush...), which
    ///     follow whatever Set-UITheme -Light / -Dark put in that slot.
    ///   * Author brushes, one per colour string, which follow the canvas colour map
    ///     (Set-UITheme -ColorMap): a page authored in one palette names its colours once, and the
    ///     map says what each becomes in the other mode. Without a map they never change.
    ///
    /// Shared instances are safe because nothing in the canvas animates or edits a brush it was
    /// handed; everything that changes a colour assigns a new brush. All access is on the UI thread.</summary>
    internal static class CanvasTheme
    {
        private static readonly Dictionary<string, SolidColorBrush> _themeBrushes =
            new Dictionary<string, SolidColorBrush>(StringComparer.Ordinal);
        private static readonly Dictionary<string, SolidColorBrush> _authorBrushes =
            new Dictionary<string, SolidColorBrush>(StringComparer.OrdinalIgnoreCase);
        // "<key>@<alpha>" -> (resource key, live translucent brush). See ForKeyTint.
        private static readonly Dictionary<string, KeyValuePair<string, SolidColorBrush>> _tints =
            new Dictionary<string, KeyValuePair<string, SolidColorBrush>>(StringComparer.Ordinal);
        private static readonly List<KeyValuePair<WeakReference<DropShadowEffect>, string>> _glows =
            new List<KeyValuePair<WeakReference<DropShadowEffect>, string>>();
        private static bool _keepGlow;   // -KeepGlow: recolour glows through the map instead of hiding them
        private static readonly List<KeyValuePair<WeakReference<GradientStop>, string>> _stops =
            new List<KeyValuePair<WeakReference<GradientStop>, string>>();

        private static Dictionary<string, Color> _map;   // authored colour -> colour in the mapped mode
        private static bool _mapAppliesInLight = true;

        private static bool? _isLight;

        /// <summary>True while the light base theme is active. Read from the window's resources until a
        /// theme apply reports it - an app that sets no theme at all never triggers that report.</summary>
        public static bool IsLight
        {
            get { return _isLight ?? !DarkDictionaryLoaded(); }
            private set { _isLight = value; }
        }

        private static bool DarkDictionaryLoaded()
        {
            try
            {
                var app = Application.Current;
                if (app == null) return true;   // the canvas default is dark
                foreach (var d in app.Resources.MergedDictionaries)
                    if (d.Source != null && (d.Source.OriginalString ?? "").EndsWith("/Assets/DarkTheme.xaml", StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }
            catch { return true; }
        }

        /// <summary>True when the current mode is the one the colour map translates into.</summary>
        private static bool MapActive { get { return _map != null && _map.Count > 0 && IsLight == _mapAppliesInLight; } }

        /// <summary>Registers the canvas colour map. <paramref name="appliesIn"/> is the mode the map
        /// translates INTO ("Light" by default: the page is authored in its dark palette).</summary>
        public static void SetColorMap(IDictionary<string, string> map, string appliesIn, bool keepGlow = false)
        {
            _mapAppliesInLight = !string.Equals(appliesIn, "Dark", StringComparison.OrdinalIgnoreCase);
            _keepGlow = keepGlow;
            _map = null;
            if (map == null || map.Count == 0) return;
            _map = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in map)
            {
                Color to;
                if (string.IsNullOrWhiteSpace(kv.Key) || !TryParse(kv.Value, out to)) continue;
                _map[Normalize(kv.Key)] = to;
            }
        }

        /// <summary>A live brush for a theme resource key. Follows the key's value across theme
        /// switches. A non-solid resource (a gradient) is returned as-is - it cannot be recoloured in
        /// place, and none of the canvas defaults are gradients.</summary>
        public static Brush ForKey(string resourceKey, string fallbackHex)
        {
            SolidColorBrush live;
            if (_themeBrushes.TryGetValue(resourceKey, out live)) return live;

            var current = Find(resourceKey);
            if (current != null && !(current is SolidColorBrush)) return current;

            Color c;
            var solid = current as SolidColorBrush;
            if (solid != null) c = solid.Color;
            else if (!TryParse(fallbackHex, out c)) return null;

            live = new SolidColorBrush(c);
            _themeBrushes[resourceKey] = live;
            return live;
        }

        /// <summary>A live, translucent brush derived from a theme key: that key's colour at
        /// <paramref name="alpha"/> (0-255). For overlays that must read on any surface in either mode -
        /// a grid stripe tinted with the text colour shows on a dark page, a light card or an author
        /// background alike, where a fixed hex would vanish on one and drown the text on another.</summary>
        public static Brush ForKeyTint(string resourceKey, string fallbackHex, byte alpha)
        {
            var id = resourceKey + "@" + alpha;
            KeyValuePair<string, SolidColorBrush> tint;
            if (_tints.TryGetValue(id, out tint)) return tint.Value;

            Color c;
            var solid = Find(resourceKey) as SolidColorBrush;
            if (solid != null) c = solid.Color;
            else if (!TryParse(fallbackHex, out c)) return null;

            var live = new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
            _tints[id] = new KeyValuePair<string, SolidColorBrush>(resourceKey, live);
            return live;
        }

        /// <summary>A live brush for an author colour string ('#0D1420', '#80FFFFFF', 'White').
        /// Returns null for an empty or unparseable value, exactly as the old parser did.</summary>
        public static Brush ForColor(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var key = Normalize(value);
            SolidColorBrush live;
            if (_authorBrushes.TryGetValue(key, out live)) return live;

            Color c;
            if (!TryParse(value, out c)) return null;
            live = new SolidColorBrush(Resolve(key, c));
            _authorBrushes[key] = live;
            return live;
        }

        /// <summary>An author colour as it should render right now (mapped when the map is active).
        /// For places that need a Color rather than a brush; pair with Track() to keep it live.</summary>
        public static Color ResolveColor(string value, Color fallback)
        {
            Color c;
            if (!TryParse(value, out c)) return fallback;
            return Resolve(Normalize(value), c);
        }

        /// <summary>Keeps a glow in step with the theme. A glow is a light-on-dark device, so by default it
        /// is hidden while the colour map is active; with -KeepGlow (a mapped palette that is still dark
        /// enough for glows to read) it stays, recoloured through the map like any other colour.</summary>
        public static void TrackGlow(DropShadowEffect fx, string authoredColor)
        {
            if (fx == null) return;
            _glows.Add(new KeyValuePair<WeakReference<DropShadowEffect>, string>(new WeakReference<DropShadowEffect>(fx), authoredColor));
            ApplyGlow(fx, authoredColor);
        }

        private static void ApplyGlow(DropShadowEffect fx, string authoredColor)
        {
            fx.Opacity = (MapActive && !_keepGlow) ? 0 : 0.95;
            if (!string.IsNullOrWhiteSpace(authoredColor)) fx.Color = ResolveColor(authoredColor, fx.Color);
        }

        /// <summary>Keeps a gradient stop's colour in step with the colour map.</summary>
        public static void TrackStop(GradientStop stop, string authoredColor)
        {
            if (stop == null || string.IsNullOrWhiteSpace(authoredColor)) return;
            _stops.Add(new KeyValuePair<WeakReference<GradientStop>, string>(new WeakReference<GradientStop>(stop), authoredColor));
            stop.Color = ResolveColor(authoredColor, stop.Color);
        }

        /// <summary>Called after the base theme and its overrides have been applied. Recolours every
        /// live brush in place, which repaints every control holding one.</summary>
        public static void OnThemeApplied(bool isLight)
        {
            IsLight = isLight;

            // A brush that something froze (a Style setter, an animation) can no longer be recoloured.
            // Skip it rather than throw - one frozen brush used to abort this loop and leave every control
            // after it in the old theme - and hand out a fresh live brush from now on.
            var frozen = new List<string>();
            foreach (var kv in _themeBrushes)
            {
                var now = Find(kv.Key) as SolidColorBrush;
                if (now == null) continue;
                if (kv.Value.IsFrozen) frozen.Add(kv.Key); else kv.Value.Color = now.Color;
            }
            foreach (var k in frozen) _themeBrushes.Remove(k);

            frozen.Clear();
            foreach (var kv in _tints)
            {
                var now = Find(kv.Value.Key) as SolidColorBrush;
                if (now == null) continue;
                var b = kv.Value.Value;
                if (b.IsFrozen) { frozen.Add(kv.Key); continue; }
                b.Color = Color.FromArgb(b.Color.A, now.Color.R, now.Color.G, now.Color.B);
            }
            foreach (var k in frozen) _tints.Remove(k);

            frozen.Clear();
            foreach (var kv in _authorBrushes)
            {
                Color authored;
                if (kv.Value.IsFrozen) { frozen.Add(kv.Key); continue; }
                if (TryParse(kv.Key, out authored)) kv.Value.Color = Resolve(kv.Key, authored);
            }
            foreach (var k in frozen) _authorBrushes.Remove(k);

            for (int i = _glows.Count - 1; i >= 0; i--)
            {
                DropShadowEffect fx;
                if (!_glows[i].Key.TryGetTarget(out fx) || fx.IsFrozen) { _glows.RemoveAt(i); continue; }
                ApplyGlow(fx, _glows[i].Value);
            }

            for (int i = _stops.Count - 1; i >= 0; i--)
            {
                GradientStop stop;
                if (!_stops[i].Key.TryGetTarget(out stop) || stop.IsFrozen) { _stops.RemoveAt(i); continue; }
                stop.Color = ResolveColor(_stops[i].Value, stop.Color);
            }

            // Things brushes can't reach - an OS-drawn title bar - follow through this.
            var changed = ThemeChanged;
            if (changed != null) try { changed(isLight); } catch { }
        }

        /// <summary>Raised after a theme is applied (true = light). For what lives outside WPF's resources.</summary>
        public static event Action<bool> ThemeChanged;

        private static Color Resolve(string normalizedKey, Color authored)
        {
            Color mapped;
            if (MapActive && _map.TryGetValue(normalizedKey, out mapped)) return mapped;
            return authored;
        }

        private static Brush Find(string key)
        {
            try { var app = Application.Current; return app == null ? null : app.TryFindResource(key) as Brush; }
            catch { return null; }
        }

        // '#abc123' and 'ABC123' are the same colour and must share one brush and one map entry.
        private static string Normalize(string value)
        {
            var s = value.Trim();
            if (s.Length > 0 && s[0] != '#' && IsHex(s)) s = "#" + s;
            return s.ToUpperInvariant();
        }

        private static bool IsHex(string s)
        {
            if (s.Length != 3 && s.Length != 6 && s.Length != 8) return false;
            foreach (var ch in s) if (!Uri.IsHexDigit(ch)) return false;
            return true;
        }

        private static bool TryParse(string value, out Color c)
        {
            c = default(Color);
            if (string.IsNullOrWhiteSpace(value)) return false;
            try
            {
                var s = value.Trim();
                if (s[0] != '#' && IsHex(s)) s = "#" + s;
                c = (Color)ColorConverter.ConvertFromString(s);
                return true;
            }
            catch { return false; }
        }
    }
}
