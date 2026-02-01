// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Launcher.Controls
{
    public partial class RevealPasswordBox : UserControl
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(RevealPasswordBox),
                new FrameworkPropertyMetadata(string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnPasswordChanged));

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public RevealPasswordBox()
        {
            InitializeComponent();
            Loaded += RevealPasswordBox_Loaded;
        }

        private void RevealPasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            // Wire events
            PlainBox.TextChanged += PlainBox_TextChanged;
            PwdBox.PasswordChanged += PwdBox_PasswordChanged;

            if (RevealToggleButton is ToggleButton t)
            {
                t.Checked += (s, a) => { 
                    if (ToggleText != null) ToggleText.Text = "👁‍🗨"; 
                    SyncPlainFromPwd(); 
                };
                t.Unchecked += (s, a) => { 
                    if (ToggleText != null) ToggleText.Text = "👁"; 
                    SyncPwdFromPlain(); 
                };
            }

            // Initialize inner boxes from DP
            SyncPlainFromPwd();
        }

        private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (RevealPasswordBox)d;
            ctrl.SyncInnerFromDp((string)e.NewValue ?? string.Empty);
        }

        private void SyncInnerFromDp(string value)
        {
            if (PlainBox.Text != value)
                PlainBox.Text = value;
            if (PwdBox.Password != value)
                PwdBox.Password = value;
        }

        private void SyncPlainFromPwd()
        {
            var value = PwdBox.Password ?? string.Empty;
            if (PlainBox.Text != value)
                PlainBox.Text = value;
            if (Password != value)
                Password = value;
        }

        private void SyncPwdFromPlain()
        {
            var value = PlainBox.Text ?? string.Empty;
            if (PwdBox.Password != value)
                PwdBox.Password = value;
            if (Password != value)
                Password = value;
        }

        private void PlainBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RevealToggleButton.IsChecked == true)
            {
                SyncPwdFromPlain();
            }
        }

        private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (RevealToggleButton.IsChecked != true)
            {
                SyncPlainFromPwd();
            }
        }
    }
}
