// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Launcher.Behaviors
{
    /// <summary>
    /// Attached behavior that makes a FrameworkElement resizable by dragging its edges.
    /// </summary>
    public static class ResizableBehavior
    {
        private const double ResizeMargin = 8.0;
        private const double MinWidth = 200.0;
        private const double MinHeight = 150.0;
        private const double MaxWidth = 800.0;
        private const double MaxHeight = 600.0;

        #region IsResizable Attached Property

        public static readonly DependencyProperty IsResizableProperty =
            DependencyProperty.RegisterAttached(
                "IsResizable",
                typeof(bool),
                typeof(ResizableBehavior),
                new PropertyMetadata(false, OnIsResizableChanged));

        public static bool GetIsResizable(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsResizableProperty);
        }

        public static void SetIsResizable(DependencyObject obj, bool value)
        {
            obj.SetValue(IsResizableProperty, value);
        }

        #endregion

        #region Private State

        private static FrameworkElement _resizingElement;
        private static Point _startPoint;
        private static double _startWidth;
        private static double _startHeight;
        private static ResizeDirection _resizeDirection;

        private enum ResizeDirection
        {
            None,
            Right,
            Bottom,
            BottomRight
        }

        #endregion

        private static void OnIsResizableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element != null)
            {
                if ((bool)e.NewValue)
                {
                    element.MouseMove += Element_MouseMove;
                    element.MouseLeftButtonDown += Element_MouseLeftButtonDown;
                    element.MouseLeftButtonUp += Element_MouseLeftButtonUp;
                    element.MouseLeave += Element_MouseLeave;
                }
                else
                {
                    element.MouseMove -= Element_MouseMove;
                    element.MouseLeftButtonDown -= Element_MouseLeftButtonDown;
                    element.MouseLeftButtonUp -= Element_MouseLeftButtonUp;
                    element.MouseLeave -= Element_MouseLeave;
                }
            }
        }

        private static void Element_MouseMove(object sender, MouseEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element == null)
                return;

            var position = e.GetPosition(element);
            var direction = GetResizeDirection(element, position);

            // Update cursor based on position
            if (_resizingElement == null)
            {
                switch (direction)
                {
                    case ResizeDirection.Right:
                        element.Cursor = Cursors.SizeWE;
                        break;
                    case ResizeDirection.Bottom:
                        element.Cursor = Cursors.SizeNS;
                        break;
                    case ResizeDirection.BottomRight:
                        element.Cursor = Cursors.SizeNWSE;
                        break;
                    default:
                        element.Cursor = Cursors.Arrow;
                        break;
                }
            }
            else if (_resizingElement == element)
            {
                // Currently resizing
                var currentPoint = e.GetPosition(element.Parent as IInputElement);
                var deltaX = currentPoint.X - _startPoint.X;
                var deltaY = currentPoint.Y - _startPoint.Y;

                if (_resizeDirection == ResizeDirection.Right || _resizeDirection == ResizeDirection.BottomRight)
                {
                    var newWidth = Math.Max(MinWidth, Math.Min(MaxWidth, _startWidth + deltaX));
                    element.Width = newWidth;
                }

                if (_resizeDirection == ResizeDirection.Bottom || _resizeDirection == ResizeDirection.BottomRight)
                {
                    var newHeight = Math.Max(MinHeight, Math.Min(MaxHeight, _startHeight + deltaY));
                    element.Height = newHeight;
                }
            }
        }

        private static void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element == null)
                return;

            var position = e.GetPosition(element);
            var direction = GetResizeDirection(element, position);

            if (direction != ResizeDirection.None)
            {
                _resizingElement = element;
                _startPoint = e.GetPosition(element.Parent as IInputElement);
                _startWidth = element.ActualWidth;
                _startHeight = element.ActualHeight;
                _resizeDirection = direction;
                element.CaptureMouse();
                e.Handled = true;
            }
        }

        private static void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null && _resizingElement == element)
            {
                _resizingElement = null;
                _resizeDirection = ResizeDirection.None;
                element.ReleaseMouseCapture();
            }
        }

        private static void Element_MouseLeave(object sender, MouseEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null && _resizingElement == null)
            {
                element.Cursor = Cursors.Arrow;
            }
        }

        private static ResizeDirection GetResizeDirection(FrameworkElement element, Point position)
        {
            var width = element.ActualWidth;
            var height = element.ActualHeight;

            bool nearRight = position.X >= width - ResizeMargin && position.X <= width;
            bool nearBottom = position.Y >= height - ResizeMargin && position.Y <= height;

            if (nearRight && nearBottom)
                return ResizeDirection.BottomRight;
            if (nearRight)
                return ResizeDirection.Right;
            if (nearBottom)
                return ResizeDirection.Bottom;

            return ResizeDirection.None;
        }
    }
}
