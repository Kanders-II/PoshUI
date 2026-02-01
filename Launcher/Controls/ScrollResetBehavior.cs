// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Windows;
using System.Windows.Controls;

namespace Launcher.Controls
{
    /// <summary>
    /// Attached behavior to reset ScrollViewer position when DataContext changes
    /// </summary>
    public static class ScrollResetBehavior
    {
        public static readonly DependencyProperty ResetOnDataContextChangeProperty =
            DependencyProperty.RegisterAttached(
                "ResetOnDataContextChange",
                typeof(bool),
                typeof(ScrollResetBehavior),
                new PropertyMetadata(false, OnResetOnDataContextChangeChanged));

        public static void SetResetOnDataContextChange(DependencyObject element, bool value)
        {
            element.SetValue(ResetOnDataContextChangeProperty, value);
        }

        public static bool GetResetOnDataContextChange(DependencyObject element)
        {
            return (bool)element.GetValue(ResetOnDataContextChangeProperty);
        }

        private static void OnResetOnDataContextChangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = d as ScrollViewer;
            if (scrollViewer == null) return;

            if ((bool)e.NewValue)
            {
                scrollViewer.DataContextChanged += ScrollViewer_DataContextChanged;
            }
            else
            {
                scrollViewer.DataContextChanged -= ScrollViewer_DataContextChanged;
            }
        }

        private static void ScrollViewer_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                // Reset scroll position to top when DataContext changes
                scrollViewer.ScrollToTop();
                scrollViewer.ScrollToLeftEnd();
            }
        }
    }
}
