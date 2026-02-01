// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Globalization;
using System.Windows.Data;

namespace Launcher.Converters
{
    /// <summary>
    /// Converts progress percentage (0-100) and container width to pixel width.
    /// </summary>
    public class ProgressPercentageToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return 0.0;

            try
            {
                // values[0] = ProgressPercentage (0-100)
                // values[1] = ActualWidth of container
                if (values[0] is double percentage && values[1] is double containerWidth)
                {
                    double width = (percentage / 100.0) * containerWidth;
                    return Math.Max(0, Math.Min(width, containerWidth));
                }
            }
            catch
            {
                // Return 0 on any error
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
