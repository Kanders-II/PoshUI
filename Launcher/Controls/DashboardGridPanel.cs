// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Windows;
using System.Windows.Controls;

namespace Launcher.Controls
{
    /// <summary>
    /// A responsive grid panel for Dashboard cards that auto-calculates columns
    /// based on available width and a target minimum column width.
    /// Cards flow left-to-right, top-to-bottom with uniform column widths.
    /// </summary>
    public class DashboardGridPanel : Panel
    {
        public static readonly DependencyProperty MinColumnWidthProperty =
            DependencyProperty.Register(nameof(MinColumnWidth), typeof(double), typeof(DashboardGridPanel),
                new FrameworkPropertyMetadata(280.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty MaxColumnsProperty =
            DependencyProperty.Register(nameof(MaxColumns), typeof(int), typeof(DashboardGridPanel),
                new FrameworkPropertyMetadata(6, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty RowSpacingProperty =
            DependencyProperty.Register(nameof(RowSpacing), typeof(double), typeof(DashboardGridPanel),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double MinColumnWidth
        {
            get => (double)GetValue(MinColumnWidthProperty);
            set => SetValue(MinColumnWidthProperty, value);
        }

        public int MaxColumns
        {
            get => (int)GetValue(MaxColumnsProperty);
            set => SetValue(MaxColumnsProperty, value);
        }

        public double RowSpacing
        {
            get => (double)GetValue(RowSpacingProperty);
            set => SetValue(RowSpacingProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            int columnCount = CalculateColumns(availableSize.Width);
            double colWidth = double.IsPositiveInfinity(availableSize.Width) ? MinColumnWidth : availableSize.Width / columnCount;

            var childConstraint = new Size(colWidth, double.PositiveInfinity);

            // Track row heights: each row has columnCount cells
            int rowCount = (int)Math.Ceiling((double)InternalChildren.Count / columnCount);
            if (rowCount == 0) rowCount = 1;
            double[] rowHeights = new double[rowCount];

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var child = InternalChildren[i];
                child.Measure(childConstraint);
                int row = i / columnCount;
                if (child.DesiredSize.Height > rowHeights[row])
                    rowHeights[row] = child.DesiredSize.Height;
            }

            double totalHeight = 0;
            for (int r = 0; r < rowHeights.Length; r++)
            {
                totalHeight += rowHeights[r];
                if (r < rowHeights.Length - 1) totalHeight += RowSpacing;
            }

            double totalWidth = double.IsPositiveInfinity(availableSize.Width) ? colWidth * columnCount : availableSize.Width;
            return new Size(totalWidth, totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            int columnCount = CalculateColumns(finalSize.Width);
            double colWidth = finalSize.Width / columnCount;

            int rowCount = (int)Math.Ceiling((double)InternalChildren.Count / columnCount);
            if (rowCount == 0) rowCount = 1;
            double[] rowHeights = new double[rowCount];

            // Compute row heights from measured sizes
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                int row = i / columnCount;
                if (InternalChildren[i].DesiredSize.Height > rowHeights[row])
                    rowHeights[row] = InternalChildren[i].DesiredSize.Height;
            }

            // Arrange children
            double y = 0;
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                int row = i / columnCount;
                int col = i % columnCount;

                if (col == 0 && row > 0)
                {
                    y += rowHeights[row - 1] + RowSpacing;
                }

                double x = col * colWidth;
                InternalChildren[i].Arrange(new Rect(x, y, colWidth, rowHeights[row]));
            }

            double totalHeight = 0;
            for (int r = 0; r < rowHeights.Length; r++)
            {
                totalHeight += rowHeights[r];
                if (r < rowHeights.Length - 1) totalHeight += RowSpacing;
            }

            return new Size(finalSize.Width, totalHeight);
        }

        private int CalculateColumns(double availableWidth)
        {
            if (double.IsPositiveInfinity(availableWidth) || availableWidth <= 0)
                return 1;

            int cols = Math.Max(1, (int)Math.Floor(availableWidth / MinColumnWidth));
            return Math.Min(cols, MaxColumns);
        }
    }
}
