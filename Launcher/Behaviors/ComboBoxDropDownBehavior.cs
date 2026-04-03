// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Launcher.Behaviors
{
    /// <summary>
    /// Attached behavior that fixes ComboBox dropdown text color in WPF Popups.
    /// WPF Popups create a separate visual tree that cannot reliably resolve
    /// DynamicResource brushes from custom theme overlays in .NET Framework 4.8.
    /// This behavior programmatically applies the correct foreground and background
    /// from Application.Current.Resources when the dropdown opens.
    /// </summary>
    public static class ComboBoxDropDownBehavior
    {
        public static readonly DependencyProperty FixPopupColorsProperty =
            DependencyProperty.RegisterAttached(
                "FixPopupColors",
                typeof(bool),
                typeof(ComboBoxDropDownBehavior),
                new PropertyMetadata(false, OnFixPopupColorsChanged));

        public static bool GetFixPopupColors(DependencyObject obj)
        {
            return (bool)obj.GetValue(FixPopupColorsProperty);
        }

        public static void SetFixPopupColors(DependencyObject obj, bool value)
        {
            obj.SetValue(FixPopupColorsProperty, value);
        }

        private static void OnFixPopupColorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var comboBox = d as ComboBox;
            if (comboBox == null) return;

            if ((bool)e.NewValue)
            {
                comboBox.DropDownOpened += ComboBox_DropDownOpened;
            }
            else
            {
                comboBox.DropDownOpened -= ComboBox_DropDownOpened;
            }
        }

        private static void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // Resolve brushes from Application.Current.Resources — guaranteed correct
            // because ApplyThemeInternal merges custom overrides into Application resources
            var foregroundBrush = Application.Current.TryFindResource("BodyForegroundBrush") as SolidColorBrush;
            var backgroundBrush = Application.Current.TryFindResource("TextBoxBackgroundBrush") as SolidColorBrush;

            if (foregroundBrush != null)
            {
                for (int i = 0; i < comboBox.Items.Count; i++)
                {
                    var container = comboBox.ItemContainerGenerator.ContainerFromIndex(i) as ComboBoxItem;
                    if (container != null)
                    {
                        container.Foreground = foregroundBrush;
                    }
                }
            }

            // Fix popup border background
            if (backgroundBrush != null)
            {
                var popup = comboBox.Template.FindName("PART_Popup", comboBox) as System.Windows.Controls.Primitives.Popup;
                if (popup != null)
                {
                    var popupBorder = popup.Child as Border;
                    if (popupBorder != null)
                    {
                        popupBorder.Background = backgroundBrush;
                    }
                }
            }
        }
    }
}
