// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Launcher.Converters
{
    public class StepStatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                bool isCompleted = (bool)value;
                // Use the accent color for completed steps, gray for incomplete
                return new SolidColorBrush(isCompleted ? 
                    Color.FromRgb(0, 120, 212) :  // #0078D4
                    Color.FromRgb(200, 200, 200)); // #C8C8C8
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 