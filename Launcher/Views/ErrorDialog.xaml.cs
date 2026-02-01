// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Windows;

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
