// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Launcher.Converters
{
    /// <summary>
    /// Converts a color string (e.g., "#0078D4") to a Color.
    /// Used for gradient stops and other Color properties.
    /// </summary>
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var colorString = value as string;
                if (string.IsNullOrEmpty(colorString))
                {
                    // Return default from parameter or transparent
                    if (parameter is string defaultColor && !string.IsNullOrEmpty(defaultColor))
                    {
                        return (Color)ColorConverter.ConvertFromString(defaultColor);
                    }
                    return Colors.Transparent;
                }

                return (Color)ColorConverter.ConvertFromString(colorString);
            }
            catch
            {
                return Colors.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an integer to a CornerRadius with uniform radius.
    /// </summary>
    public class IntToCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                double radius = 8; // default
                
                if (value is int intVal)
                    radius = intVal;
                else if (value is double doubleVal)
                    radius = doubleVal;
                else if (value is string strVal && double.TryParse(strVal, out double parsed))
                    radius = parsed;

                return new CornerRadius(radius);
            }
            catch
            {
                return new CornerRadius(8);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Multi-value converter that creates a LinearGradientBrush from two color strings.
    /// Values[0] = GradientStart color string
    /// Values[1] = GradientEnd color string
    /// </summary>
    public class GradientBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Length < 2)
                    return Brushes.Transparent;

                var startColor = values[0] as string;
                var endColor = values[1] as string;

                if (string.IsNullOrEmpty(startColor) || string.IsNullOrEmpty(endColor))
                    return Brushes.Transparent;

                var brush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };

                brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(startColor), 0));
                brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(endColor), 1));

                return brush;
            }
            catch
            {
                return Brushes.Transparent;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
