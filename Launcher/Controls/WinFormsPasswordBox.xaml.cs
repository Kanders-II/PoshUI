// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using Launcher.Services;
using WinForms = System.Windows.Forms;

namespace Launcher.Controls
{
    public partial class WinFormsPasswordBox : UserControl
    {
        private WinForms.MaskedTextBox _maskedTextBox;

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(WinFormsPasswordBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnPasswordChanged));

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        private void ApplyThemeColorsToMaskedTextBox()
        {
            if (_maskedTextBox == null)
                return;

            // Try to fetch WPF theme brushes; fall back to sensible defaults
            var overlayBg = TryFindResource("TextBoxBackgroundBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x30));
            var baseBg = TryFindResource("ContentBackgroundBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xF6));
            var fgBrush = TryFindResource("BodyForegroundBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(0xF0, 0xF3, 0xF7));

            // If overlay is translucent, composite it over the base content background so WinForms BackColor matches WPF appearance
            Color resolvedBg = overlayBg.Color.A < 255 ? CompositeOver(overlayBg.Color, baseBg.Color) : overlayBg.Color;

            _maskedTextBox.BackColor = System.Drawing.Color.FromArgb(resolvedBg.A, resolvedBg.R, resolvedBg.G, resolvedBg.B);
            _maskedTextBox.ForeColor = System.Drawing.Color.FromArgb(fgBrush.Color.A, fgBrush.Color.R, fgBrush.Color.G, fgBrush.Color.B);
            _maskedTextBox.BorderStyle = WinForms.BorderStyle.None;

            // Force redraw in case theme switched while visible
            _maskedTextBox.Invalidate();
            _maskedTextBox.Refresh();
        }

        private static Color CompositeOver(Color overlay, Color under)
        {
            double a = overlay.A / 255.0;
            byte r = (byte)Math.Round(overlay.R * a + under.R * (1 - a));
            byte g = (byte)Math.Round(overlay.G * a + under.G * (1 - a));
            byte b = (byte)Math.Round(overlay.B * a + under.B * (1 - a));
            return Color.FromArgb(255, r, g, b);
        }

        public WinFormsPasswordBox()
        {
            InitializeComponent();
            Loaded += WinFormsPasswordBox_Loaded;
            Unloaded += WinFormsPasswordBox_Unloaded;
            // React to theme changes from MainWindow
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
        }

        private void WinFormsPasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (_maskedTextBox != null)
                return;

            _maskedTextBox = new WinForms.MaskedTextBox
            {
                UseSystemPasswordChar = true,
                Dock = WinForms.DockStyle.Fill,
                Text = Password,
                BorderStyle = WinForms.BorderStyle.None,
                Margin = new System.Windows.Forms.Padding(0)
            };
            _maskedTextBox.TextChanged += MaskedTextBox_TextChanged;

            // Find the named WindowsFormsHost in XAML
            var hostControl = (WindowsFormsHost)this.FindName("Host");
            if (hostControl != null)
                hostControl.Child = _maskedTextBox;

            // Apply theme colors to the WinForms control initially
            ApplyThemeColorsToMaskedTextBox();

            // Update again when visibility changes (e.g., after theme toggle/reload)
            IsVisibleChanged += (s, args) => ApplyThemeColorsToMaskedTextBox();
        }

        private void MaskedTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_maskedTextBox.Text != Password)
                Password = _maskedTextBox.Text;
        }

        private void ThemeManager_ThemeChanged(object sender, EventArgs e)
        {
            ApplyThemeColorsToMaskedTextBox();
        }

        private void WinFormsPasswordBox_Unloaded(object sender, RoutedEventArgs e)
        {
            ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
        }

        private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (WinFormsPasswordBox)d;
            if (ctrl._maskedTextBox != null && ctrl._maskedTextBox.Text != (string)e.NewValue)
            {
                ctrl._maskedTextBox.Text = (string)e.NewValue;
            }
        }

        // NEW handlers for ToggleButton Checked/Unchecked
        private void RevealToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_maskedTextBox != null)
            {
                _maskedTextBox.UseSystemPasswordChar = false; // Reveal password
            }
            if (RevealLabel != null)
            {
                RevealLabel.Text = "Hide";
            }
        }

        private void RevealToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_maskedTextBox != null)
            {
                _maskedTextBox.UseSystemPasswordChar = true; // Mask password
            }
            if (RevealLabel != null)
            {
                RevealLabel.Text = "Show";
            }
        }
    }
} 