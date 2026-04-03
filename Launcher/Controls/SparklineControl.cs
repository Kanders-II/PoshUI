// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Launcher.Controls
{
    /// <summary>
    /// A lightweight sparkline mini-chart control for MetricCards.
    /// Renders an ObservableCollection of doubles as a smooth polyline.
    /// </summary>
    public class SparklineControl : Canvas
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(ObservableCollection<double>), typeof(SparklineControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnDataChanged));

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(SparklineControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(SparklineControl),
                new FrameworkPropertyMetadata(1.5, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowFillProperty =
            DependencyProperty.Register(nameof(ShowFill), typeof(bool), typeof(SparklineControl),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public ObservableCollection<double> Data
        {
            get => (ObservableCollection<double>)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public bool ShowFill
        {
            get => (bool)GetValue(ShowFillProperty);
            set => SetValue(ShowFillProperty, value);
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (SparklineControl)d;

            if (e.OldValue is ObservableCollection<double> oldCol)
            {
                oldCol.CollectionChanged -= ctrl.DataCollectionChanged;
            }
            if (e.NewValue is ObservableCollection<double> newCol)
            {
                newCol.CollectionChanged += ctrl.DataCollectionChanged;
            }

            ctrl.InvalidateVisual();
        }

        private void DataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var data = Data;
            if (data == null || data.Count < 2) return;

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            double min = double.MaxValue;
            double max = double.MinValue;
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i] < min) min = data[i];
                if (data[i] > max) max = data[i];
            }

            double range = max - min;
            if (range < 0.0001) range = 1; // avoid division by zero for flat data

            double padding = 2;
            double drawH = h - padding * 2;
            double stepX = w / (data.Count - 1);

            var brush = Stroke ?? new SolidColorBrush(Color.FromRgb(0, 120, 212));
            var pen = new Pen(brush, StrokeThickness);
            pen.StartLineCap = PenLineCap.Round;
            pen.EndLineCap = PenLineCap.Round;
            pen.LineJoin = PenLineJoin.Round;

            // Build geometry
            var lineGeometry = new StreamGeometry();
            using (var ctx = lineGeometry.Open())
            {
                double x0 = 0;
                double y0 = h - padding - ((data[0] - min) / range * drawH);
                ctx.BeginFigure(new Point(x0, y0), false, false);

                for (int i = 1; i < data.Count; i++)
                {
                    double x = i * stepX;
                    double y = h - padding - ((data[i] - min) / range * drawH);
                    ctx.LineTo(new Point(x, y), true, true);
                }
            }
            lineGeometry.Freeze();

            // Draw filled area under the line
            if (ShowFill && brush is SolidColorBrush scb)
            {
                var fillGeometry = new StreamGeometry();
                using (var ctx = fillGeometry.Open())
                {
                    double x0 = 0;
                    double y0 = h - padding - ((data[0] - min) / range * drawH);
                    ctx.BeginFigure(new Point(x0, h), true, true);
                    ctx.LineTo(new Point(x0, y0), false, false);

                    for (int i = 1; i < data.Count; i++)
                    {
                        double x = i * stepX;
                        double y = h - padding - ((data[i] - min) / range * drawH);
                        ctx.LineTo(new Point(x, y), true, true);
                    }

                    ctx.LineTo(new Point((data.Count - 1) * stepX, h), false, false);
                }
                fillGeometry.Freeze();

                var fillColor = Color.FromArgb(40, scb.Color.R, scb.Color.G, scb.Color.B);
                var fillBrush = new SolidColorBrush(fillColor);
                fillBrush.Freeze();
                dc.DrawGeometry(fillBrush, null, fillGeometry);
            }

            // Draw the line
            dc.DrawGeometry(null, pen, lineGeometry);
        }
    }
}
