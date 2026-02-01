// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Launcher.Converters
{
    /// <summary>
    /// Converts a hex color string (e.g., "#107C10") to a SolidColorBrush.
    /// </summary>
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var colorString = value as string;
            if (!string.IsNullOrWhiteSpace(colorString))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorString);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    // Return null to allow XAML default to apply
                }
            }
            
            // Return null to allow DynamicResource bindings to work
            return System.Windows.DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var brush = value as SolidColorBrush;
            if (brush != null)
            {
                return brush.Color.ToString();
            }
            return "#2D2D30";
        }
    }
}
