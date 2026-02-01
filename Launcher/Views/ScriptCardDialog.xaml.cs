// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Launcher.ViewModels;

namespace Launcher.Views
{
    /// <summary>
    /// Converts object values to boolean for toggle switches.
    /// </summary>
    public class ObjectToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            if (value is bool b) return b;
            if (value is string s) return bool.TryParse(s, out bool result) && result;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    /// <summary>
    /// Dialog window for executing a script card with parameters.
    /// </summary>
    public partial class ScriptCardDialog : Window
    {
        public ScriptCardDialog()
        {
            InitializeComponent();
        }

        public ScriptCardDialog(ScriptCardViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeBtn.Content = "\uE922"; // Maximize icon
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeBtn.Content = "\uE923"; // Restore icon
            }
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var param = button?.DataContext as ViewModels.ScriptCardParameter;
            if (param == null) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = $"Select {param.Label}",
                Filter = "All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                param.Value = dialog.FileName;
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var param = button?.DataContext as ViewModels.ScriptCardParameter;
            if (param == null) return;

            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = $"Select {param.Label}",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                param.Value = dialog.SelectedPath;
            }
        }
    }
}


