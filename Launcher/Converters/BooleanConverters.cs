// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Launcher.Converters
{
    /// <summary>
    /// Inverts a boolean value.
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return false;
        }
    }

    /// <summary>
    /// Converts null to false, everything else to true.
    /// Used to check if a binding (like Choices) is not null.
    /// </summary>
    public class NullToFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CountToTrueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }

            if (value is int intValue)
            {
                return intValue > 0;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (var _ in enumerable)
                {
                    return true;
                }
                return false;
            }

            if (int.TryParse(value.ToString(), out int parsed))
            {
                return parsed > 0;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EqualityToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
            {
                return value == null;
            }

            return Equals(value?.ToString(), parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                return parameter;
            }

            return Binding.DoNothing;
        }
    }

    public class RowCountToHeightConverter : IValueConverter
    {
        private const double DefaultRowHeight = 26.0;
        private const double MinimumHeight = 60.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return MinimumHeight;
            }

            if (!int.TryParse(value.ToString(), out int rows) || rows <= 0)
            {
                rows = 4; // sensible default
            }

            double calculated = rows * DefaultRowHeight;
            return Math.Max(calculated, MinimumHeight);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns Visible if string is not null/empty, otherwise Collapsed.
    /// </summary>
    public class NotNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string) 
                ? System.Windows.Visibility.Visible 
                : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return System.Windows.Visibility.Collapsed;

            string stringValue = value.ToString();
            string parameterString = parameter.ToString();

            // Split parameter by pipe character to get multiple valid values
            string[] validValues = parameterString.Split('|');

            foreach (string validValue in validValues)
            {
                if (string.Equals(stringValue, validValue.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return System.Windows.Visibility.Visible;
                }
            }

            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a numeric value to a height for bar charts.
    /// Uses the parameter to specify max value, or auto-scales based on value magnitude.
    /// </summary>
    public class ValueToHeightConverter : IValueConverter
    {
        private const double MaxHeight = 120.0;
        private const double MinHeight = 8.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return MinHeight;

            double numericValue;
            if (!double.TryParse(value.ToString(), out numericValue))
                return MinHeight;

            // Auto-scale: find appropriate max based on value magnitude
            double maxValue = 100.0;
            if (parameter != null)
            {
                double paramMax;
                if (double.TryParse(parameter.ToString(), out paramMax))
                {
                    maxValue = paramMax;
                }
            }
            else
            {
                // Auto-detect scale based on value magnitude
                if (numericValue > 1000) maxValue = 2000;
                else if (numericValue > 500) maxValue = 1000;
                else if (numericValue > 200) maxValue = 500;
                else if (numericValue > 100) maxValue = 250;
                else maxValue = 100;
            }

            double height = (numericValue / maxValue) * MaxHeight;
            return Math.Max(MinHeight, Math.Min(height, MaxHeight));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// MultiValueConverter that scales a value based on the max of all values in the collection.
    /// Parameters: value, double[] allValues
    /// </summary>
    public class ValueToScaledHeightConverter : IMultiValueConverter
    {
        private const double MaxHeight = 120.0;
        private const double MinHeight = 8.0;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return MinHeight;

            double numericValue;
            if (!double.TryParse(values[0]?.ToString(), out numericValue))
                return MinHeight;

            double maxValue = 100.0;
            // values[1] can be a double (MaxValue) or double[] (all values)
            if (values[1] is double maxVal)
            {
                maxValue = maxVal;
            }
            else if (values[1] is double[] allValues && allValues.Length > 0)
            {
                maxValue = allValues.Max();
            }
            
            if (maxValue == 0) maxValue = 100;

            double height = (numericValue / maxValue) * MaxHeight;
            return Math.Max(MinHeight, Math.Min(height, MaxHeight));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Helper to calculate rounded MaxValue matching Y-axis labels
    /// </summary>
    internal static class ChartScaleHelper
    {
        public static double GetRoundedMaxValue(double actualMax)
        {
            if (actualMax <= 0) return 100;
            if (actualMax <= 100) return 100;
            if (actualMax <= 200) return 200;
            if (actualMax <= 500) return 500;
            if (actualMax <= 1000) return 1000;
            if (actualMax <= 2000) return 2000;
            return Math.Ceiling(actualMax / 1000) * 1000;
        }
    }

    /// <summary>
    /// Converts an array of values to a PointCollection for Line/Area charts.
    /// </summary>
    public class ValuesToPointsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var points = new System.Windows.Media.PointCollection();
            
            double[] values = value as double[];
            if (values == null || values.Length == 0)
                return points;

            double width = 280.0;
            double height = 140.0;
            double padding = 20.0;
            
            if (parameter is string paramStr)
            {
                string[] parts = paramStr.Split(',');
                if (parts.Length >= 2)
                {
                    double.TryParse(parts[0], out width);
                    double.TryParse(parts[1], out height);
                }
            }

            // Use rounded MaxValue to match Y-axis labels
            double actualMax = values.Length > 0 ? values.Max() : 100;
            double maxValue = ChartScaleHelper.GetRoundedMaxValue(actualMax);
            
            double usableWidth = width - (padding * 2);
            double usableHeight = height - padding;
            double stepX = usableWidth / Math.Max(1, values.Length - 1);

            for (int i = 0; i < values.Length; i++)
            {
                double x = padding + (i * stepX);
                double y = usableHeight - ((values[i] / maxValue) * usableHeight) + 5;
                points.Add(new System.Windows.Point(x, y));
            }

            return points;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts chart values to a collection of pie slice geometries.
    /// </summary>
    public class ValuesToPieSlicesConverter : IValueConverter
    {
        private static readonly string[] PieColors = { "#2196F3", "#4CAF50", "#FF9800", "#E91E63", "#9C27B0", "#00BCD4", "#795548", "#607D8B" };
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var slices = new System.Collections.ObjectModel.ObservableCollection<PieSliceInfo>();
            
            double[] values = value as double[];
            if (values == null || values.Length == 0)
                return slices;

            double total = values.Sum();
            if (total == 0) return slices;

            double centerX = 50;
            double centerY = 50;
            double radius = 45;
            double currentAngle = -90; // Start from top

            for (int i = 0; i < values.Length; i++)
            {
                double percentage = values[i] / total;
                double sweepAngle = percentage * 360;
                
                // Calculate start and end points
                double startAngleRad = currentAngle * Math.PI / 180;
                double endAngleRad = (currentAngle + sweepAngle) * Math.PI / 180;
                
                double startX = centerX + radius * Math.Cos(startAngleRad);
                double startY = centerY + radius * Math.Sin(startAngleRad);
                double endX = centerX + radius * Math.Cos(endAngleRad);
                double endY = centerY + radius * Math.Sin(endAngleRad);
                
                var geometry = new System.Windows.Media.PathGeometry();
                var figure = new System.Windows.Media.PathFigure
                {
                    StartPoint = new System.Windows.Point(centerX, centerY),
                    IsClosed = true
                };
                
                figure.Segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(startX, startY), true));
                figure.Segments.Add(new System.Windows.Media.ArcSegment(
                    new System.Windows.Point(endX, endY),
                    new System.Windows.Size(radius, radius),
                    0,
                    sweepAngle > 180,
                    System.Windows.Media.SweepDirection.Clockwise,
                    true));
                
                geometry.Figures.Add(figure);
                
                slices.Add(new PieSliceInfo
                {
                    Geometry = geometry,
                    Color = PieColors[i % PieColors.Length],
                    Value = values[i],
                    Percentage = $"{percentage * 100:F0}%"
                });
                
                currentAngle += sweepAngle;
            }

            return slices;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PieSliceInfo
    {
        public System.Windows.Media.PathGeometry Geometry { get; set; }
        public string Color { get; set; } = "#2196F3";
        public double Value { get; set; }
        public string Percentage { get; set; } = "";
        public string TooltipText => $"Value: {Value:N0} ({Percentage})";
    }

    /// <summary>
    /// Data point info for line/area chart dots with position and tooltip
    /// </summary>
    public class ChartDataPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Value { get; set; }
        public string Label { get; set; } = "";
        public string Color { get; set; } = "#4CAF50"; // Default green
        public string TooltipText => string.IsNullOrEmpty(Label) ? $"Value: {Value:N0}" : $"{Label}: {Value:N0}";
    }

    /// <summary>
    /// Converts chart values to positioned data points for rendering dots
    /// </summary>
    public class ValuesToDataPointsConverter : IValueConverter
    {
        private static readonly string[] ChartColors = { "#2196F3", "#4CAF50", "#FF9800", "#E91E63", "#9C27B0", "#00BCD4", "#FFC107", "#795548" };
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var points = new System.Collections.ObjectModel.ObservableCollection<ChartDataPoint>();
            
            double[] values = value as double[];
            if (values == null || values.Length == 0)
                return points;

            double width = 280.0;
            double height = 140.0;
            double padding = 20.0;
            
            // Use rounded MaxValue to match Y-axis labels
            double actualMax = values.Length > 0 ? values.Max() : 100;
            double maxValue = ChartScaleHelper.GetRoundedMaxValue(actualMax);
            
            double usableWidth = width - (padding * 2);
            double usableHeight = height - padding;
            double stepX = usableWidth / Math.Max(1, values.Length - 1);

            for (int i = 0; i < values.Length; i++)
            {
                double x = padding + (i * stepX);
                double y = usableHeight - ((values[i] / maxValue) * usableHeight) + 5;
                points.Add(new ChartDataPoint
                {
                    X = x,
                    Y = y,
                    Value = values[i],
                    Label = "",
                    Color = ChartColors[i % ChartColors.Length]
                });
            }

            return points;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an array of values to area chart PathGeometry.
    /// </summary>
    public class ValuesToAreaPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var geometry = new System.Windows.Media.PathGeometry();
            
            double[] values = value as double[];
            if (values == null || values.Length == 0)
                return geometry;

            double width = 280.0;
            double height = 140.0;
            double padding = 20.0;
            
            // Use rounded MaxValue to match Y-axis labels
            double actualMax = values.Length > 0 ? values.Max() : 100;
            double maxValue = ChartScaleHelper.GetRoundedMaxValue(actualMax);
            
            double usableWidth = width - (padding * 2);
            double usableHeight = height - padding;
            double stepX = usableWidth / Math.Max(1, values.Length - 1);
            double baseline = height;

            var figure = new System.Windows.Media.PathFigure
            {
                StartPoint = new System.Windows.Point(padding, baseline),
                IsClosed = true
            };

            // First point at baseline then up to first data point
            double firstY = usableHeight - ((values[0] / maxValue) * usableHeight) + 5;
            figure.Segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(padding, firstY), true));

            // Data points
            for (int i = 1; i < values.Length; i++)
            {
                double x = padding + (i * stepX);
                double y = usableHeight - ((values[i] / maxValue) * usableHeight) + 5;
                figure.Segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(x, y), true));
            }

            // Close at baseline
            double lastX = padding + ((values.Length - 1) * stepX);
            figure.Segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(lastX, baseline), true));

            geometry.Figures.Add(figure);
            return geometry;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}