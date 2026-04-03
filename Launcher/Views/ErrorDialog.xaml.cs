// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Launcher.ViewModels;

namespace Launcher.Views
{
    /// <summary>
    /// Error dialog window for displaying detailed error messages with copy functionality.
    /// </summary>
    public partial class ErrorDialog : Window
    {
        public string ErrorTitle { get; set; }
        public string ErrorMessage { get; set; }

        public ErrorDialog(string title, string errorMessage)
        {
            InitializeComponent();
            
            ErrorTitle = title ?? "Error";
            ErrorMessage = errorMessage ?? "An unknown error occurred.";
            Title = ErrorTitle; // Set the window title
            
            DataContext = this;
            
            if (MainWindowViewModel.AnimationsEnabled)
            {
                this.Loaded += (s, args) =>
                {
                    if (this.Content is FrameworkElement rootElement)
                    {
                        rootElement.RenderTransformOrigin = new Point(0.5, 0.5);
                        rootElement.RenderTransform = new ScaleTransform(0.95, 0.95);
                        rootElement.Opacity = 0;

                        var duration = TimeSpan.FromMilliseconds(200);
                        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

                        rootElement.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = easing });
                        ((ScaleTransform)rootElement.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.95, 1.0, duration) { EasingFunction = easing });
                        ((ScaleTransform)rootElement.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.95, 1.0, duration) { EasingFunction = easing });
                    }
                };
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ErrorMessage);
                MessageBox.Show(
                    "Error details copied to clipboard.", 
                    "Copied", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show(
                    "Failed to copy to clipboard.", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
