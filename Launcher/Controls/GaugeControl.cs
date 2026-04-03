// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Launcher.Controls
{
    /// <summary>
    /// A radial gauge control that renders a semi-circular arc showing progress 0-100%.
    /// Displays a 270-degree arc with a colored fill portion and a center value label.
    /// </summary>
    public class GaugeControl : Canvas
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(GaugeControl),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register(nameof(MinValue), typeof(double), typeof(GaugeControl),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(GaugeControl),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GaugeBrushProperty =
            DependencyProperty.Register(nameof(GaugeBrush), typeof(Brush), typeof(GaugeControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TrackBrushProperty =
            DependencyProperty.Register(nameof(TrackBrush), typeof(Brush), typeof(GaugeControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register(nameof(Thickness), typeof(double), typeof(GaugeControl),
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double MinValue
        {
            get => (double)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public Brush GaugeBrush
        {
            get => (Brush)GetValue(GaugeBrushProperty);
            set => SetValue(GaugeBrushProperty, value);
        }

        public Brush TrackBrush
        {
            get => (Brush)GetValue(TrackBrushProperty);
            set => SetValue(TrackBrushProperty, value);
        }

        public double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            double size = Math.Min(w, h);
            double radius = (size - Thickness) / 2;
            double centerX = w / 2;
            double centerY = h / 2;

            if (radius <= 0) return;

            // Gauge arc spans 270 degrees (from 135 to 405 / -225 to 45)
            double startAngle = 135;
            double sweepAngle = 270;

            double range = MaxValue - MinValue;
            if (range <= 0) range = 100;
            double fraction = Math.Max(0, Math.Min(1, (Value - MinValue) / range));
            double valueSweep = fraction * sweepAngle;

            var trackBrush = TrackBrush ?? new SolidColorBrush(Color.FromArgb(40, 128, 128, 128));
            var gaugeBrush = GaugeBrush ?? new SolidColorBrush(Color.FromRgb(0, 120, 212));

            // Draw track (background arc)
            var trackPen = new Pen(trackBrush, Thickness);
            trackPen.StartLineCap = PenLineCap.Round;
            trackPen.EndLineCap = PenLineCap.Round;
            DrawArc(dc, centerX, centerY, radius, startAngle, sweepAngle, trackPen);

            // Draw value arc
            if (valueSweep > 0.5)
            {
                var valuePen = new Pen(gaugeBrush, Thickness);
                valuePen.StartLineCap = PenLineCap.Round;
                valuePen.EndLineCap = PenLineCap.Round;
                DrawArc(dc, centerX, centerY, radius, startAngle, valueSweep, valuePen);
            }
        }

        private void DrawArc(DrawingContext dc, double cx, double cy, double r, double startDeg, double sweepDeg, Pen pen)
        {
            double startRad = startDeg * Math.PI / 180;
            double endRad = (startDeg + sweepDeg) * Math.PI / 180;

            var startPt = new Point(cx + r * Math.Cos(startRad), cy + r * Math.Sin(startRad));
            var endPt = new Point(cx + r * Math.Cos(endRad), cy + r * Math.Sin(endRad));

            bool isLargeArc = sweepDeg > 180;

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(startPt, false, false);
                ctx.ArcTo(endPt, new Size(r, r), 0, isLargeArc, SweepDirection.Clockwise, true, false);
            }
            geometry.Freeze();

            dc.DrawGeometry(null, pen, geometry);
        }
    }
}
