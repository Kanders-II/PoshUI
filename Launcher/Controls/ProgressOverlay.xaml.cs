// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Windows;
using System.Windows.Controls;

namespace Launcher.Controls
{
    /// <summary>
    /// Progress overlay control that displays a semi-transparent layer with a spinner and message.
    /// Used to provide visual feedback during slow operations.
    /// </summary>
    public partial class ProgressOverlay : UserControl
    {
        public static readonly DependencyProperty ShowOverlayProperty =
            DependencyProperty.Register(
                nameof(ShowOverlay),
                typeof(bool),
                typeof(ProgressOverlay),
                new PropertyMetadata(false));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(ProgressOverlay),
                new PropertyMetadata("Loading..."));

        public ProgressOverlay()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets whether the progress overlay is visible.
        /// </summary>
        public bool ShowOverlay
        {
            get => (bool)GetValue(ShowOverlayProperty);
            set => SetValue(ShowOverlayProperty, value);
        }

        /// <summary>
        /// Gets or sets the message to display in the progress overlay.
        /// </summary>
        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }
    }
}
