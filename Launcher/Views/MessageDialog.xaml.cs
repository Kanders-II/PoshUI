// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Input;

namespace Launcher.Views
{
    /// <summary>
    /// Themed message dialog that replaces MessageBox for a consistent UI experience.
    /// </summary>
    public partial class MessageDialog : Window
    {
        public enum MessageType
        {
            Information,
            Warning,
            Error,
            Question
        }

        public enum MessageDialogResult
        {
            Primary,
            Secondary,
            None
        }

        public MessageDialogResult Result { get; private set; } = MessageDialogResult.None;

        public MessageDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Shows a themed message dialog.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="title">The dialog title</param>
        /// <param name="type">The type of message (affects icon)</param>
        /// <param name="primaryButtonText">Text for the primary button (default: "OK")</param>
        /// <param name="secondaryButtonText">Text for secondary button (null to hide)</param>
        /// <param name="owner">Owner window</param>
        /// <returns>DialogResult indicating which button was clicked</returns>
        public static MessageDialogResult Show(
            string message, 
            string title, 
            MessageType type = MessageType.Information,
            string primaryButtonText = "OK",
            string secondaryButtonText = null,
            Window owner = null)
        {
            var dialog = new MessageDialog();
            dialog.Owner = owner ?? Application.Current.MainWindow;
            dialog.Title = title;
            dialog.TitleText.Text = title;
            dialog.MessageText.Text = message;
            dialog.PrimaryButton.Content = primaryButtonText;

            // Show appropriate icon based on type
            switch (type)
            {
                case MessageType.Warning:
                    dialog.WarningIcon.Visibility = Visibility.Visible;
                    break;
                case MessageType.Error:
                    dialog.ErrorIcon.Visibility = Visibility.Visible;
                    break;
                case MessageType.Question:
                    dialog.QuestionIcon.Visibility = Visibility.Visible;
                    break;
                case MessageType.Information:
                default:
                    dialog.InfoIcon.Visibility = Visibility.Visible;
                    break;
            }

            // Configure secondary button
            if (!string.IsNullOrEmpty(secondaryButtonText))
            {
                dialog.SecondaryButton.Content = secondaryButtonText;
                dialog.SecondaryButton.Visibility = Visibility.Visible;
            }

            dialog.ShowDialog();
            return dialog.Result;
        }

        /// <summary>
        /// Shows a simple information message.
        /// </summary>
        public static void ShowInfo(string message, string title = "Information", Window owner = null)
        {
            Show(message, title, MessageType.Information, "OK", null, owner);
        }

        /// <summary>
        /// Shows a warning message.
        /// </summary>
        public static void ShowWarning(string message, string title = "Warning", Window owner = null)
        {
            Show(message, title, MessageType.Warning, "OK", null, owner);
        }

        /// <summary>
        /// Shows an error message.
        /// </summary>
        public static void ShowError(string message, string title = "Error", Window owner = null)
        {
            Show(message, title, MessageType.Error, "OK", null, owner);
        }

        /// <summary>
        /// Shows a confirmation dialog with Yes/No buttons.
        /// </summary>
        /// <returns>True if user clicked Yes, false otherwise</returns>
        public static bool ShowConfirmation(string message, string title = "Confirm", Window owner = null)
        {
            var result = Show(message, title, MessageType.Question, "Yes", "No", owner);
            return result == MessageDialogResult.Primary;
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageDialogResult.Primary;
            Close();
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageDialogResult.Secondary;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageDialogResult.None;
            Close();
        }
    }
}
