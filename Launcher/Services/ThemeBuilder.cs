using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Launcher.Services
{
    /// <summary>
    /// Builds a WPF ResourceDictionary from a flat dictionary of color overrides.
    /// Maps friendly slot names (e.g. "AccentColor") to internal WPF resource keys
    /// (e.g. "PrimaryColor", "PrimaryBrush", "SystemAccentColorBrush").
    /// </summary>
    public static class ThemeBuilder
    {
        /// <summary>
        /// Build a ResourceDictionary from theme overrides.
        /// Returns null if overrides is null/empty.
        /// </summary>
        public static ResourceDictionary BuildResourceDictionary(Dictionary<string, string> overrides)
        {
            if (overrides == null || overrides.Count == 0)
                return null;

            var dict = new ResourceDictionary();
            // Tag it so MainWindow can identify and re-apply on theme toggle
            dict["_PoshUICustomTheme"] = true;

            // Normalize keys to case-insensitive lookup
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in overrides)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                    map[kvp.Key.Trim()] = kvp.Value.Trim();
            }

            // Auto-derive accent shades if only AccentColor is provided
            if (map.ContainsKey("AccentColor"))
            {
                Color accent = ParseColor(map["AccentColor"]);
                if (!map.ContainsKey("AccentDark"))
                    map["AccentDark"] = ColorToHex(DarkenColor(accent, 0.20));
                if (!map.ContainsKey("AccentDarker"))
                    map["AccentDarker"] = ColorToHex(DarkenColor(accent, 0.35));
                if (!map.ContainsKey("AccentLight"))
                    map["AccentLight"] = ColorToHex(LightenColor(accent, 0.25));
            }

            // Apply color overrides using the slot mapping
            ApplyColorSlot(dict, map, "AccentColor", new[] { "PrimaryColor" });
            ApplyColorSlot(dict, map, "AccentDark", new[] { "PrimaryDarkColor" });
            ApplyColorSlot(dict, map, "AccentDarker", new[] { "PrimaryDarkerColor" });
            ApplyColorSlot(dict, map, "AccentLight", new[] { "PrimaryLightColor" });

            // Accent brushes (derived from accent colors)
            ApplyBrushSlot(dict, map, "AccentColor", new[] { "SystemAccentColorBrush", "PrimaryBrush" });
            ApplyBrushSlot(dict, map, "AccentDark", new[] { "PrimaryDarkBrush" });
            ApplyBrushSlot(dict, map, "AccentDarker", new[] { "PrimaryDarkerBrush" });
            ApplyBrushSlot(dict, map, "AccentLight", new[] { "PrimaryLightBrush" });

            // Surface & Background brushes
            ApplyBrushSlot(dict, map, "Background", new[] { "AppBackgroundBrush" });
            ApplyBrushSlot(dict, map, "ContentBackground", new[] { "ContentBackgroundBrush", "BackgroundBrush" });
            ApplyBrushSlot(dict, map, "CardBackground", new[] { "CardBackgroundBrush", "ChartBackgroundBrush" });

            // Sidebar
            ApplyBrushSlot(dict, map, "SidebarBackground", new[] { "SidebarBackgroundBrush" });
            ApplyBrushSlot(dict, map, "SidebarText", new[] { "SidebarTextBrush" });
            ApplyBrushSlot(dict, map, "SidebarHighlight", new[] { "SidebarHighlightBrush" });
            // Default SidebarHighlight to AccentColor if not specified
            if (!map.ContainsKey("SidebarHighlight") && map.ContainsKey("AccentColor"))
                ApplyBrushDirect(dict, map["AccentColor"], new[] { "SidebarHighlightBrush" });

            // Text brushes
            ApplyBrushSlot(dict, map, "TextPrimary", new[] { "HeadingForegroundBrush", "BodyForegroundBrush", "TitleForegroundBrush" });
            ApplyBrushSlot(dict, map, "TextSecondary", new[] { "SecondaryForegroundBrush", "SecondaryBrush" });

            // Button brushes
            ApplyBrushSlot(dict, map, "ButtonBackground", new[] { "ButtonBackgroundBrush" });
            ApplyBrushSlot(dict, map, "ButtonForeground", new[] { "ButtonForegroundBrush" });
            // Default ButtonBackground to AccentColor if not specified
            if (!map.ContainsKey("ButtonBackground") && map.ContainsKey("AccentColor"))
                ApplyBrushDirect(dict, map["AccentColor"], new[] { "ButtonBackgroundBrush" });
            // Default ButtonHover/Pressed to accent shades
            if (map.ContainsKey("AccentDark"))
                ApplyBrushDirect(dict, map["AccentDark"], new[] { "ButtonHoverBrush" });
            if (map.ContainsKey("AccentDarker"))
                ApplyBrushDirect(dict, map["AccentDarker"], new[] { "ButtonPressedBrush" });

            // Input controls
            ApplyBrushSlot(dict, map, "InputBackground", new[] { "TextBoxBackgroundBrush" });
            ApplyBrushSlot(dict, map, "InputBorder", new[] { "TextBoxFocusBorderBrush" });
            // Default InputBorder to AccentColor if not specified
            if (!map.ContainsKey("InputBorder") && map.ContainsKey("AccentColor"))
                ApplyBrushDirect(dict, map["AccentColor"], new[] { "TextBoxFocusBorderBrush" });

            // Borders
            ApplyBrushSlot(dict, map, "BorderColor", new[] { "BorderBrush", "BorderLightBrush" });

            // Title bar
            ApplyBrushSlot(dict, map, "TitleBarBackground", new[] { "TitleBarBackgroundBrush" });
            ApplyBrushSlot(dict, map, "TitleBarText", new[] { "TitleBarTextBrush" });

            // Semantic colors
            ApplyColorAndBrush(dict, map, "SuccessColor", "SuccessColor", "SuccessBrush");
            ApplyColorAndBrush(dict, map, "WarningColor", "WarningColor", "WarningBrush");
            ApplyColorAndBrush(dict, map, "ErrorColor", "ErrorColor", "ErrorBrush");

            // Progress/workflow accent (defaults to AccentColor)
            if (map.ContainsKey("AccentColor"))
            {
                ApplyBrushDirect(dict, map["AccentColor"], new[] { "ProgressForegroundBrush", "TaskActiveAccentBrush" });
            }

            // Workflow card brushes (if background overrides exist)
            if (map.ContainsKey("CardBackground"))
            {
                ApplyBrushDirect(dict, map["CardBackground"], new[] { "WorkflowCardBackgroundBrush" });
            }
            if (map.ContainsKey("BorderColor"))
            {
                ApplyBrushDirect(dict, map["BorderColor"], new[] { "WorkflowCardBorderBrush" });
            }

            // FontFamily
            string fontFamily;
            if (map.TryGetValue("FontFamily", out fontFamily))
            {
                // FontFamily is not a brush/color - we store it as a string resource
                dict["ThemeFontFamily"] = new FontFamily(fontFamily);
            }

            // CornerRadius
            string cornerRadiusStr;
            if (map.TryGetValue("CornerRadius", out cornerRadiusStr))
            {
                double cr;
                if (double.TryParse(cornerRadiusStr, NumberStyles.Any, CultureInfo.InvariantCulture, out cr))
                {
                    dict["ThemeCornerRadius"] = new CornerRadius(cr);
                }
            }

            LoggingService.Info(string.Format("ThemeBuilder: Built ResourceDictionary with {0} override slots", map.Count), component: "ThemeBuilder");
            return dict;
        }

        /// <summary>
        /// Serialize theme overrides dictionary to a JSON-compatible string for branding transport.
        /// Uses simple key=value format that DataContractJsonSerializer can handle.
        /// </summary>
        public static Dictionary<string, string> ParseFromBrandingHashtable(object brandingValue)
        {
            if (brandingValue == null)
                return null;

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // The PowerShell module serializes ThemeOverrides as a nested hashtable in the branding JSON
            // which arrives as Dictionary<string, object> after JSON deserialization
            var dictObj = brandingValue as Dictionary<string, object>;
            if (dictObj != null)
            {
                foreach (var kvp in dictObj)
                {
                    if (kvp.Value != null)
                        result[kvp.Key] = kvp.Value.ToString();
                }
                return result;
            }

            // Also handle Dictionary<string, string> directly
            var dictStr = brandingValue as Dictionary<string, string>;
            if (dictStr != null)
            {
                foreach (var kvp in dictStr)
                {
                    if (kvp.Value != null)
                        result[kvp.Key] = kvp.Value;
                }
                return result;
            }

            return null;
        }

        #region Color Helpers

        private static void ApplyColorSlot(ResourceDictionary dict, Dictionary<string, string> map, string slotName, string[] colorKeys)
        {
            string value;
            if (!map.TryGetValue(slotName, out value))
                return;

            Color color = ParseColor(value);
            foreach (var key in colorKeys)
            {
                dict[key] = color;
            }
        }

        private static void ApplyBrushSlot(ResourceDictionary dict, Dictionary<string, string> map, string slotName, string[] brushKeys)
        {
            string value;
            if (!map.TryGetValue(slotName, out value))
                return;

            ApplyBrushDirect(dict, value, brushKeys);
        }

        private static void ApplyBrushDirect(ResourceDictionary dict, string colorHex, string[] brushKeys)
        {
            Color color = ParseColor(colorHex);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            foreach (var key in brushKeys)
            {
                dict[key] = brush;
            }
        }

        private static void ApplyColorAndBrush(ResourceDictionary dict, Dictionary<string, string> map, string slotName, string colorKey, string brushKey)
        {
            string value;
            if (!map.TryGetValue(slotName, out value))
                return;

            Color color = ParseColor(value);
            dict[colorKey] = color;

            var brush = new SolidColorBrush(color);
            brush.Freeze();
            dict[brushKey] = brush;
        }

        private static Color ParseColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return Colors.Transparent;

            hex = hex.Trim();
            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                LoggingService.Warn(string.Format("ThemeBuilder: Invalid color '{0}', using transparent", hex), component: "ThemeBuilder");
                return Colors.Transparent;
            }
        }

        private static Color DarkenColor(Color color, double factor)
        {
            byte r = (byte)Math.Max(0, color.R * (1.0 - factor));
            byte g = (byte)Math.Max(0, color.G * (1.0 - factor));
            byte b = (byte)Math.Max(0, color.B * (1.0 - factor));
            return Color.FromArgb(color.A, r, g, b);
        }

        private static Color LightenColor(Color color, double factor)
        {
            byte r = (byte)Math.Min(255, color.R + (255 - color.R) * factor);
            byte g = (byte)Math.Min(255, color.G + (255 - color.G) * factor);
            byte b = (byte)Math.Min(255, color.B + (255 - color.B) * factor);
            return Color.FromArgb(color.A, r, g, b);
        }

        private static string ColorToHex(Color color)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        #endregion
    }
}
